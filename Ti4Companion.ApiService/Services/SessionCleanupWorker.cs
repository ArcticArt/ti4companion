using Microsoft.EntityFrameworkCore;
using Ti4Companion.ApiService.Data;

namespace Ti4Companion.ApiService.Services;

/// <summary>
/// Background worker that deletes inactive sessions. Each session is wiped once it has been idle
/// for longer than its own <see cref="GameSession.RetentionHours"/> (configurable per session), except
/// that a <see cref="GameSession.Paused"/> session gets the longer <c>Ti4:PausedRetentionHours</c>
/// window — a pause means an interrupted game somebody intends to resume.
/// At this scale the session table is tiny, so we load and filter in memory to avoid provider-
/// specific date arithmetic.
/// </summary>
public class SessionCleanupWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration config,
    ILogger<SessionCleanupWorker> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Let startup migration/seed finish first.
        try { await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); }
        catch (OperationCanceledException) { return; }

        // One line in the journal so the active windows are visible without reading the config file.
        logger.LogInformation(
            "Session cleanup active: default {DefaultHours} h after last activity, paused {PausedHours} h, checked every {Minutes} min",
            config.GetValue("Ti4:DefaultRetentionHours", 2160),
            config.GetValue("Ti4:PausedRetentionHours", 8760),
            Interval.TotalMinutes);

        using var timer = new PeriodicTimer(Interval);
        do
        {
            await CleanupAsync(stoppingToken);
        }
        while (await SafeWaitAsync(timer, stoppingToken));
    }

    private async Task CleanupAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<Ti4DbContext>();

            var now = DateTimeOffset.UtcNow;
            var pausedHours = config.GetValue("Ti4:PausedRetentionHours", 8760);
            // Load the graph: a session about to be deleted is summarised first, which needs its players
            // and objectives. Stale sessions are a handful at a time, so the extra joins are cheap.
            var all = await db.Sessions
                .Include(s => s.Players).ThenInclude(p => p.Technologies)
                .Include(s => s.Objectives).ThenInclude(o => o.Scores)
                .ToListAsync(ct);
            var stale = all
                .Where(s =>
                {
                    var hours = RetentionHoursFor(s, pausedHours);
                    return hours > 0 && s.LastActivityUtc.AddHours(hours) < now;
                })
                .ToList();

            if (stale.Count > 0)
            {
                // Last chance to keep a record: the session and its log go away with the delete below.
                var recorded = 0;
                foreach (var s in stale)
                {
                    if (await SessionSummaryService.TryRecordAsync(db, s, ct)) recorded++;
                }

                db.Sessions.RemoveRange(stale);
                await db.SaveChangesAsync(ct);
                logger.LogInformation("Auto-wiped {Count} inactive session(s), summarised {Recorded}: {Codes}",
                    stale.Count, recorded, string.Join(", ", stale.Select(s => s.JoinCode)));
            }
        }
        catch (OperationCanceledException) { /* shutting down */ }
        catch (Exception ex)
        {
            logger.LogError(ex, "Session cleanup failed");
        }
    }

    /// <summary>
    /// Effective inactivity window for one session. A stored 0 means "never wipe" and always wins;
    /// otherwise a paused session is held for at least <paramref name="pausedHours"/>, never less than
    /// its own window.
    /// </summary>
    private static int RetentionHoursFor(GameSession session, int pausedHours) =>
        session.RetentionHours <= 0 ? 0
            : session.Paused ? Math.Max(session.RetentionHours, pausedHours)
            : session.RetentionHours;

    private static async Task<bool> SafeWaitAsync(PeriodicTimer timer, CancellationToken ct)
    {
        try { return await timer.WaitForNextTickAsync(ct); }
        catch (OperationCanceledException) { return false; }
    }
}
