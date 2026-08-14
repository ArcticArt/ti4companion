using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Ti4Companion.ApiService.Data;
using Ti4Companion.ApiService.Endpoints;
using Ti4Companion.ApiService.Realtime;
using Lib.Net.Http.WebPush;
using Ti4Companion.ApiService.Services;
using Ti4Companion.Shared;

var builder = WebApplication.CreateBuilder(args);

// Aspire service defaults (OpenTelemetry, health checks, resilience, service discovery).
builder.AddServiceDefaults();

// SQLite — local files, no external database process (and no Docker). Two databases:
//  • ti4.db        — runtime session state (Ti4DbContext).
//  • ti4master.db  — the master reference content (MasterDbContext), bootstrapped once from the JSON.
var connectionString = builder.Configuration.GetConnectionString("ti4db") ?? "Data Source=ti4.db";
builder.Services.AddDbContext<Ti4DbContext>(options => options.UseSqlite(connectionString));

var masterConnectionString = builder.Configuration.GetConnectionString("ti4masterdb") ?? "Data Source=ti4master.db";
builder.Services.AddDbContext<MasterDbContext>(options => options.UseSqlite(masterConnectionString));

builder.Services.AddSignalR();
builder.Services.AddOpenApi();

// Public-hosting hardening (see DEPLOY.md "Security review"): per-IP rate limits so session
// creation can't be spammed and join codes can't be enumerated in bulk. Limits are generous —
// a whole game night behind one NAT IP stays far below them.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    // Creating sessions: 20 per 10 minutes per IP.
    options.AddPolicy("session-create", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(10) }));
    // Looking a session up by code (unauthenticated; every client refresh uses it): 600/min per IP —
    // plenty for 8 devices refreshing on every SignalR event, useless for scanning the code space.
    options.AddPolicy("session-read", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 600, Window = TimeSpan.FromMinutes(1) }));
});

// Background worker that wipes inactive sessions after their retention window.
builder.Services.AddHostedService<SessionCleanupWorker>();

// Web Push ("you're up"). PushServiceClient is an HttpClient, so it goes through the factory; PushService
// itself is a singleton and opens its own DbContext scope per send, because it runs detached from the
// request that triggered it. With no VAPID keys configured it reports Enabled=false and does nothing.
builder.Services.AddHttpClient<PushServiceClient>();
builder.Services.AddSingleton<PushService>();

// The operator's announcement, read from a file beside the session database (see NoticeService).
builder.Services.AddSingleton<NoticeService>();

var app = builder.Build();

// Apply migrations to both databases on startup. The master content DB is bootstrapped from the JSON
// only when it is empty (first run); thereafter it is canonical and never re-seeded.
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    var db = scope.ServiceProvider.GetRequiredService<Ti4DbContext>();
    await db.Database.MigrateAsync();
    await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;"); // better concurrency for SQLite

    var master = scope.ServiceProvider.GetRequiredService<MasterDbContext>();
    await master.Database.MigrateAsync();
    await master.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");

    // The master content DB (ti4master.db) is a committed artifact, edited directly — there is no JSON
    // bootstrap anymore. Warn loudly if it somehow came up empty (e.g. the file was lost/not restored).
    if (!await master.Factions.AnyAsync())
        logger.LogWarning("Master content DB has no content. ti4master.db is a committed artifact — restore it from source control (the JSON bootstrap has been removed).");

    // Cache the static faction initiative overrides so the session-mutation path doesn't hit the master DB.
    await FactionInitiative.LoadAsync(master);
    // Same for objective points — needed when a finished game is summarised.
    await ObjectivePoints.LoadAsync(master);
}

// Behind the reverse proxy (Apache/Caddy on the same box) the client address arrives in
// X-Forwarded-For; honour it so the per-IP rate limits see real clients, not 127.0.0.1.
// The defaults only trust loopback proxies, which matches the deployment.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
});
app.UseRateLimiter();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// In production the app sits behind Caddy (which terminates TLS) and listens on plain HTTP,
// so HTTPS redirection only applies to local development.
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Serve the Blazor WebAssembly client (hosted model: same origin as the API → no CORS).
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.MapGet("/api/ping", () => Results.Ok(new { status = "ok", time = DateTimeOffset.UtcNow }));

// REST API + real-time hub.
// Who am I? Empty on production, "TEST" on staging (set via Ti4__InstanceLabel in the systemd unit).
// Unauthenticated and unmetered on purpose: every client asks once at startup, before any session exists.
app.MapGet("/api/instance", (IConfiguration cfg) =>
    Results.Ok(new InstanceDto(cfg["Ti4:InstanceLabel"] ?? "")));

// Anything the operator wants to tell everybody, or an empty string. Every client polls this while it is
// open, so it is metered like the other public reads — and answered from a cached stat(), see NoticeService.
app.MapGet("/api/notice", (NoticeService notice) => Results.Ok(notice.Current()))
   .RequireRateLimiting("session-read");

// The VAPID public key a browser needs to subscribe for "you're up". Empty => push is not configured and
// the client hides the feature. The PRIVATE key never leaves the server (systemd environment, not appsettings).
app.MapGet("/api/push/key", (PushService push) => Results.Ok(new PushKeyDto(push.PublicKey)));

// How busy is the table tonight? Two COUNTS for the start page, nothing else: no codes, no names, no player
// numbers — an aggregate can't identify anybody, and the landing page is public. Rate-limited like the other
// public reads because it is trivially pollable.
app.MapGet("/api/activity", async (Ti4DbContext db, CancellationToken ct) =>
{
    // A GAME, not a session row. Two corrections to the first version, which simply counted sessions with
    // recent activity (2026-08-12):
    //   * a session that never got past SETUP is somebody who opened the app and walked away, not a game.
    //     On the live box that was the majority of rows.
    //   * a game the host ENDED and closed is deleted, so it dropped straight out of "played today" — the one
    //     kind of game we are most sure really happened. Its permanent SessionSummary survives the delete (no
    //     FK, on purpose), so it is counted from there.
    // The two sources overlap — "back to setup" leaves a session row AND a summary — so they are unioned by
    // session id rather than added up. A session is only ever counted once, which is also why a table that
    // plays two games under one code counts once: the summary is keyed by session and updated in place.
    //
    // Counted in MEMORY: the SQLite provider cannot translate a DateTimeOffset comparison (the same limitation
    // as "no ORDER BY on a DateTimeOffset" — it is stored as TEXT), so a WHERE on LastActivityUtc throws. Both
    // tables hold one row per session; the retention worker reads them the same way.
    var now = DateTimeOffset.UtcNow;
    var sessions = await db.Sessions.AsNoTracking()
        .Select(s => new { s.Id, s.Phase, s.LastActivityUtc }).ToListAsync(ct);
    // StartedAtUtc is the first phase change, i.e. exactly "this one got past setup" for a session that no
    // longer exists — the same line `Phase != Setup` draws for one that does. It matters because the host's
    // "end game" records a summary unconditionally, so a session abandoned during setup and then closed
    // would otherwise arrive here as a game.
    var finished = await db.SessionSummaries.AsNoTracking()
        .Select(s => new { s.SessionId, s.StartedAtUtc, s.LastActivityUtc, s.RecordedAtUtc }).ToListAsync(ct);

    // "Running" and "played today" are different questions, so they are counted differently
    // (2026-08-14, on request — a game the host had ended still showed as running for an hour):
    //   * played today INCLUDES finished games. That is the whole reason the summaries are unioned in:
    //     an ended-and-closed game has no session row left, and it is the kind we are surest about.
    //   * running EXCLUDES them. A finished game is not running, however recently it was touched.
    //
    // "Finished" = a summary exists for it. Deliberately that blunt: a first attempt tried to tell
    // "ended and idle" from "ended but playing on" by asking whether the session had been touched SINCE
    // the summary was recorded — which never excludes anything, because RECORDING the summary is itself
    // a mutation and bumps LastActivityUtc past it. Verified failing before this rewrite.
    // The remaining inaccuracy is the narrow case where a table declares the game over and keeps playing
    // anyway; it then stops counting as running. The app was told the game ended, so that reading is
    // defensible, and "back to setup" lands in Setup and is excluded by the line above regardless.
    var finishedIds = finished.Select(s => s.SessionId).ToHashSet();

    int Games(int hours, bool includeFinished)
    {
        var since = now.AddHours(-hours);
        var ids = new HashSet<Guid>();
        foreach (var s in sessions)
        {
            if (s.Phase == GamePhase.Setup || s.LastActivityUtc < since) continue;
            if (!includeFinished && finishedIds.Contains(s.Id)) continue;
            ids.Add(s.Id);
        }
        if (includeFinished)
            foreach (var s in finished)
                if (s.StartedAtUtc is not null && s.LastActivityUtc >= since) ids.Add(s.SessionId);
        return ids.Count;
    }

    return Results.Ok(new ActivityDto(Games(24, includeFinished: true), Games(1, includeFinished: false)));
}).RequireRateLimiting("session-read");

app.MapContentEndpoints();
app.MapSessionEndpoints();
app.MapHub<SessionHub>("/hubs/session");

// SPA fallback for the Blazor client routes.
app.MapFallbackToFile("index.html");

app.Run();
