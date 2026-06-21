using Microsoft.EntityFrameworkCore;

namespace Ti4Companion.ApiService.Data;

/// <summary>
/// Process-wide cache of faction initiative overrides (Naalu = 0, …), keyed by faction slug. Faction
/// content is static reference data in the master DB, so it is loaded once at startup
/// (<see cref="LoadAsync"/>) and read on the hot session-mutation path without re-querying the master DB
/// on every call. Picks the newest revision per slug. Refreshed only on restart — edit a faction's
/// override in the master DB and restart to apply.
/// </summary>
public static class FactionInitiative
{
    public static IReadOnlyDictionary<string, int?> Overrides { get; private set; } =
        new Dictionary<string, int?>();

    public static async Task LoadAsync(MasterDbContext master, CancellationToken ct = default)
        => Overrides = (await master.Factions.AsNoTracking().ToListAsync(ct))
            .GroupBy(f => f.Slug)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(f => f.Version).First().InitiativeOverride);
}
