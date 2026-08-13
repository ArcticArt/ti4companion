using System.Net;
using System.Security.Cryptography;
using Lib.Net.Http.WebPush;
using Lib.Net.Http.WebPush.Authentication;
using Microsoft.EntityFrameworkCore;
using Ti4Companion.ApiService.Data;
using Ti4Companion.Shared;

namespace Ti4Companion.ApiService.Services;

/// <summary>The kinds of "you can do something now" a player is notified about, besides their own turn.</summary>
public enum PushAction { StrategyPick, Vote, Score, Technology, Secondary }

/// <summary>
/// Sends the "you're up" Web Push notifications.
///
/// Web Push rather than anything native: the notification has to reach a locked phone, and the app is a
/// PWA with a service worker already. On iOS this only works when the site was added to the home screen
/// (16.4+), which the client has to explain rather than silently do nothing.
///
/// Push is OFF unless a VAPID key pair is configured (<c>Ti4:Vapid:PublicKey/PrivateKey/Subject</c>). The
/// private key is a secret: it belongs in the systemd unit's environment, never in a committed
/// appsettings.json. With no keys, <see cref="Enabled"/> is false, the public-key endpoint returns empty and
/// the client hides the whole feature — no half-working state.
///
/// Sending is deliberately fire-and-forget from the request's point of view: a push service that is slow or
/// down must never delay or fail the turn change that triggered it.
/// </summary>
public class PushService(
    PushServiceClient client,
    IServiceScopeFactory scopes,
    IConfiguration config,
    ILogger<PushService> log)
{
    private readonly string _publicKey = config["Ti4:Vapid:PublicKey"] ?? "";
    private readonly string _privateKey = config["Ti4:Vapid:PrivateKey"] ?? "";
    private readonly string _subject = config["Ti4:Vapid:Subject"] ?? "";

    public bool Enabled => _publicKey.Length > 0 && _privateKey.Length > 0;

    /// <summary>The public key the browser needs to subscribe. Empty when push is not configured.</summary>
    public string PublicKey => Enabled ? _publicKey : "";

    /// <summary>
    /// Generate a VAPID key pair (base64url, P-256) — used by the one-off setup, not at runtime. Kept here
    /// so the format lives next to the code that consumes it: the public key is the uncompressed point
    /// (0x04 ‖ X ‖ Y), the private key is the raw scalar D.
    /// </summary>
    public static (string PublicKey, string PrivateKey) GenerateKeys()
    {
        using var ec = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var p = ec.ExportParameters(true);
        var pub = new byte[65];
        pub[0] = 0x04;
        p.Q.X!.CopyTo(pub, 1);
        p.Q.Y!.CopyTo(pub, 33);
        return (Base64Url(pub), Base64Url(p.D!));
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    /// <summary>
    /// Tell a player it is their turn. Runs detached on the thread pool with its own DbContext scope: the
    /// caller is inside a request that has already saved, and must not wait for a push service.
    /// </summary>
    public void NotifyTurn(Guid sessionId, Guid playerId, string playerName, string joinCode, int round, Language lang)
    {
        if (!Enabled) return;
        var payload = Payload(
            lang == Language.De ? $"{playerName}: du bist dran (Runde {round})"
                                : $"{playerName}: it's your turn (round {round})",
            joinCode, "turn");
        _ = Task.Run(() => SendToPlayerAsync(sessionId, playerId, payload, "turn"));
    }

    /// <summary>
    /// A player's turn budget has run out. Sent at most once per player per round: several devices notice the
    /// same second (the wall, the host, the player), and each of them reports it. The guard is in memory on
    /// purpose — losing it in a restart costs one duplicate notification, and nothing else.
    /// </summary>
    public void NotifyTimeUp(Guid sessionId, Guid playerId, string playerName, string joinCode, int round, Language lang)
    {
        if (!Enabled) return;
        if (_timeUpSent.Count > 10_000) _timeUpSent.Clear();      // a long-running process, not a leak
        if (!_timeUpSent.TryAdd((playerId, round), 0)) return;
        var payload = Payload(
            lang == Language.De ? $"{playerName}: Zeit abgelaufen (Runde {round})"
                                : $"{playerName}: your time is up (round {round})",
            joinCode, "timeup");
        _ = Task.Run(() => SendToPlayerAsync(sessionId, playerId, payload, "timeup"));
    }

    private readonly System.Collections.Concurrent.ConcurrentDictionary<(Guid Player, int Round), byte> _timeUpSent = new();

    /// <summary>
    /// A player can act on something that is not their turn: pick a strategy card, cast a vote, score in the
    /// status phase, record a technology, resolve a secondary. Same shape as <see cref="NotifyTurn"/> — the
    /// point of the feature is that a phone in a pocket learns about it, and "your turn" was only the first of
    /// those moments.
    /// <para>
    /// One <paramref name="kind"/> per moment, used as the push TOPIC as well, so a second notification of the
    /// same kind replaces the first instead of stacking. The wording lives here because the server is the only
    /// place that knows the session's language.
    /// </para></summary>
    public void NotifyAction(Guid sessionId, Guid playerId, string playerName, string joinCode,
        PushAction kind, Language lang)
    {
        if (!Enabled) return;
        var de = lang == Language.De;
        var body = kind switch
        {
            PushAction.StrategyPick => de ? $"{playerName}: Strategiekarte wählen" : $"{playerName}: pick a strategy card",
            PushAction.Vote => de ? $"{playerName}: abstimmen" : $"{playerName}: time to vote",
            PushAction.Score => de ? $"{playerName}: Aufträge werten" : $"{playerName}: score your objectives",
            PushAction.Technology => de ? $"{playerName}: Technologie erfassen" : $"{playerName}: record your technology",
            PushAction.Secondary => de ? $"{playerName}: Sekundärfähigkeit" : $"{playerName}: secondary ability",
            _ => de ? $"{playerName}: du bist gefragt" : $"{playerName}: your move",
        };
        var tag = kind.ToString().ToLowerInvariant();
        _ = Task.Run(() => SendToPlayerAsync(sessionId, playerId, Payload(body, joinCode, tag), tag));
    }

    private static string Payload(string body, string joinCode, string tag) =>
        System.Text.Json.JsonSerializer.Serialize(new { title = "TI4 Companion", body, code = joinCode, tag });

    private async Task SendToPlayerAsync(Guid sessionId, Guid playerId, string payload, string topic)
    {
        try
        {
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<Ti4DbContext>();
            var subs = await db.PushSubscriptions
                .Where(s => s.SessionId == sessionId && s.PlayerId == playerId && s.FailedAtUtc == null)
                .ToListAsync();
            if (subs.Count == 0) return;

            client.DefaultAuthentication = new VapidAuthentication(_publicKey, _privateKey)
            {
                Subject = string.IsNullOrWhiteSpace(_subject) ? "mailto:Frostforgestudio@proton.me" : _subject
            };

            var message = new PushMessage(payload)
            {
                // One pending notification of each kind per device is enough — a later one replaces the
                // earlier, so a phone that was off does not wake up to a stack of stale turns.
                Topic = topic,
                Urgency = PushMessageUrgency.High,
                TimeToLive = 600
            };

            foreach (var sub in subs)
            {
                var target = new Lib.Net.Http.WebPush.PushSubscription { Endpoint = sub.Endpoint };
                target.SetKey(PushEncryptionKeyName.P256DH, sub.P256dh);
                target.SetKey(PushEncryptionKeyName.Auth, sub.Auth);
                try
                {
                    await client.RequestPushMessageDeliveryAsync(target, message);
                }
                catch (PushServiceClientException ex)
                {
                    // 404/410 mean the browser threw the subscription away: retire it instead of retrying
                    // forever. Anything else is left alone — the push service may simply be down.
                    // Read the STATUS, never the message: this library's exception says just "Gone", so an
                    // earlier check for "410" in the text silently never matched (caught by the test).
                    var gone = ex.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone;
                    if (gone) sub.FailedAtUtc = DateTimeOffset.UtcNow;
                    log.LogWarning(ex, "Push delivery failed with {Status} ({State})",
                        ex.StatusCode, gone ? "subscription retired" : "transient");
                }
                catch (Exception ex)
                {
                    log.LogWarning(ex, "Push delivery failed (transient)");
                }
            }
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Nothing about a notification is worth surfacing into the game.
            log.LogWarning(ex, "Push send failed");
        }
    }
}
