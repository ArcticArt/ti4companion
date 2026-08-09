using Microsoft.EntityFrameworkCore;
using Ti4Companion.ApiService.Data;
using Ti4Companion.Shared;

namespace Ti4Companion.ApiService.Services;

/// <summary>
/// Writes the permanent <see cref="SessionSummary"/> for a game. Called from two places, because either
/// can be the last thing that ever happens to a session: the host ending the game, and the cleanup worker
/// just before it deletes an inactive session. Recording is idempotent per session, so both may run.
/// </summary>
public static class SessionSummaryService
{
    /// <summary>A game only counts once it got past round 1 and lasted at least this long — otherwise the
    /// summary would fill up with abandoned setups (on the live box that was 32 of 36 rows).</summary>
    private static readonly TimeSpan MinimumSpan = TimeSpan.FromHours(1);

    /// <summary>Whether this session represents a game worth keeping a record of.</summary>
    public static bool IsWorthRecording(GameSession s)
        => s.CurrentRound > 1 && s.LastActivityUtc - s.CreatedAtUtc >= MinimumSpan;

    /// <summary>
    /// Create or update the summary for <paramref name="session"/>. Returns false when the session doesn't
    /// clear <see cref="IsWorthRecording"/>. Does NOT save — the caller commits, so this can join an
    /// existing transaction (e.g. the wipe that follows it).
    /// </summary>
    public static async Task<bool> TryRecordAsync(Ti4DbContext db, GameSession session, CancellationToken ct)
    {
        if (!IsWorthRecording(session)) return false;

        // The log lives in its own DbSet (not in the session graph).
        var log = await db.SessionLog.AsNoTracking()
            .Where(l => l.SessionId == session.Id)
            .ToListAsync(ct);
        var ordered = log.OrderBy(l => l.TimestampUtc).ToList();

        var startedAt = ordered.FirstOrDefault(l => l.Kind == SessionLogKind.PhaseChange)?.TimestampUtc;
        var paused = PausedSpan(ordered, session.LastActivityUtc);
        var netFrom = startedAt ?? session.CreatedAtUtc;
        var net = session.LastActivityUtc - netFrom - paused;
        if (net < TimeSpan.Zero) net = TimeSpan.Zero;

        var points = PointsPerPlayer(session);
        var top = points.Count == 0 ? 0 : points.Values.Max();
        // A shared top score has no single winner — leave it open rather than pick one arbitrarily.
        var leaders = points.Where(p => p.Value == top && top > 0).Select(p => p.Key).ToList();
        var winner = leaders.Count == 1 ? session.Players.FirstOrDefault(p => p.Id == leaders[0]) : null;

        var existing = await db.SessionSummaries
            .Include(x => x.Players)
            .FirstOrDefaultAsync(x => x.SessionId == session.Id, ct);

        var summary = existing ?? new SessionSummary { SessionId = session.Id };

        summary.JoinCode = session.JoinCode;
        summary.Name = session.Name;
        summary.CreatedAtUtc = session.CreatedAtUtc;
        summary.StartedAtUtc = startedAt;
        summary.LastActivityUtc = session.LastActivityUtc;
        summary.DurationSeconds = (int)net.TotalSeconds;
        summary.PausedSeconds = (int)paused.TotalSeconds;
        summary.RoundsReached = session.CurrentRound;
        summary.EndPhase = session.Phase;
        summary.PlayerCount = session.Players.Count;
        summary.DeviceCount = session.Players
            .Where(p => !string.IsNullOrEmpty(p.DeviceToken))
            .Select(p => p.DeviceToken!)
            .Distinct()
            .Count();
        summary.ObjectivesRevealed = session.Objectives.Count;
        summary.ActiveExpansions = session.ActiveExpansions;
        summary.DefaultLanguage = session.DefaultLanguage;
        summary.TurnTimerSeconds = session.TurnTimerSeconds;
        summary.StrategyCardsPerPlayer = session.StrategyCardsPerPlayer;
        summary.RedTapeLite = session.RedTapeLite;
        summary.WinnerName = winner?.Name;
        summary.WinnerFactionId = winner?.FactionId;
        summary.TopPoints = top;
        summary.RecordedAtUtc = DateTimeOffset.UtcNow;

        // Rebuild the player rows: simpler than diffing, and a re-record is rare.
        if (existing is not null) db.SessionSummaryPlayers.RemoveRange(existing.Players);
        summary.Players = session.Players
            .OrderBy(p => p.SeatOrder)
            .Select(p => new SessionSummaryPlayer
            {
                SessionSummaryId = summary.Id,
                Name = p.Name,
                FactionId = p.FactionId,
                ColorHex = p.ColorHex,
                SeatOrder = p.SeatOrder,
                Points = points.TryGetValue(p.Id, out var pts) ? pts : 0,
                TechnologyCount = p.Technologies.Count,
            })
            .ToList();

        if (existing is null) db.SessionSummaries.Add(summary);
        return true;
    }

    /// <summary>Total paused time, from the GamePaused → GameResumed pairs in the log (an unclosed pause
    /// runs to <paramref name="until"/>). Same rule the client's statistics use.</summary>
    private static TimeSpan PausedSpan(IReadOnlyList<SessionLogEntry> ordered, DateTimeOffset until)
    {
        var total = TimeSpan.Zero;
        DateTimeOffset? from = null;
        foreach (var e in ordered)
        {
            if (e.Kind == SessionLogKind.GamePaused) from ??= e.TimestampUtc;
            else if (e.Kind == SessionLogKind.GameResumed && from is { } f)
            {
                total += e.TimestampUtc - f;
                from = null;
            }
        }
        if (from is { } open && until > open) total += until - open;
        return total;
    }

    /// <summary>Victory points per player: the objective's printed points, or the hand-added ones.</summary>
    private static Dictionary<Guid, int> PointsPerPlayer(GameSession session)
    {
        var result = new Dictionary<Guid, int>();
        foreach (var o in session.Objectives)
        {
            var value = o.CustomPoints
                ?? (ObjectivePoints.Points.TryGetValue(o.ObjectiveId, out var p) ? p : 0);
            foreach (var score in o.Scores)
                result[score.PlayerId] = (result.TryGetValue(score.PlayerId, out var cur) ? cur : 0) + value;
        }
        return result;
    }
}
