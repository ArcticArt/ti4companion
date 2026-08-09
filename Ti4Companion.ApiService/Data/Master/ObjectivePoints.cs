using Microsoft.EntityFrameworkCore;

namespace Ti4Companion.ApiService.Data;

/// <summary>
/// Process-wide cache of objective victory points, keyed by objective slug. Same reasoning as
/// <see cref="FactionInitiative"/>: static reference content in the master DB, loaded once at startup, so
/// scoring a session summary doesn't need the master DB at hand. Picks the newest revision per slug.
/// </summary>
public static class ObjectivePoints
{
    public static IReadOnlyDictionary<string, int> Points { get; private set; } = new Dictionary<string, int>();

    public static async Task LoadAsync(MasterDbContext master, CancellationToken ct = default)
        => Points = (await master.Objectives.AsNoTracking().ToListAsync(ct))
            .GroupBy(o => o.Slug)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(o => o.Version).First().Points);
}
