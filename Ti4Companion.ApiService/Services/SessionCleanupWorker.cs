using Microsoft.EntityFrameworkCore;
using Ti4Companion.ApiService.Data;

namespace Ti4Companion.ApiService.Services;

/// <summary>
/// Background worker that deletes inactive sessions. Each session is wiped once it has been idle
/// for longer than its own <see cref="GameSession.RetentionHours"/> (configurable per session).
/// At this scale the session table is tiny, so we load and filter in memory to avoid provider-
/// specific date arithmetic.
/// </summary>
public class SessionCleanupWorker(IServiceScopeFactory scopeFactory, ILogger<SessionCleanupWorker> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Let startup migration/seed finish first.
        try { await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); }
        catch (OperationCanceledException) { return; }

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
            var all = await db.Sessions.ToListAsync(ct);
            var stale = all
                .Where(s => s.RetentionHours > 0 && s.LastActivityUtc.AddHours(s.RetentionHours) < now)
                .ToList();

            if (stale.Count > 0)
            {
                db.Sessions.RemoveRange(stale);
                await db.SaveChangesAsync(ct);
                logger.LogInformation("Auto-wiped {Count} inactive session(s): {Codes}",
                    stale.Count, string.Join(", ", stale.Select(s => s.JoinCode)));
            }
        }
        catch (OperationCanceledException) { /* shutting down */ }
        catch (Exception ex)
        {
            logger.LogError(ex, "Session cleanup failed");
        }
    }

    private static async Task<bool> SafeWaitAsync(PeriodicTimer timer, CancellationToken ct)
    {
        try { return await timer.WaitForNextTickAsync(ct); }
        catch (OperationCanceledException) { return false; }
    }
}
