using Microsoft.EntityFrameworkCore;
using Ti4Companion.Shared;

namespace Ti4Companion.ApiService.Data;

/// <summary>
/// Process-wide cache of objective victory points and stages, keyed by objective slug. Same reasoning as
/// <see cref="FactionInitiative"/>: static reference content in the master DB, loaded once at startup, so
/// scoring a session summary — or applying a Red Tape rule that depends on Stage I vs Stage II — doesn't need
/// the master DB at hand in a session endpoint. Picks the newest revision per slug.
/// </summary>
public static class ObjectivePoints
{
    public static IReadOnlyDictionary<string, int> Points { get; private set; } = new Dictionary<string, int>();

    /// <summary>Stage per objective slug. The Red Tape variants gate Stage II removals on it.</summary>
    public static IReadOnlyDictionary<string, ObjectiveStage> Stages { get; private set; } = new Dictionary<string, ObjectiveStage>();

    public static async Task LoadAsync(MasterDbContext master, CancellationToken ct = default)
    {
        var latest = (await master.Objectives.AsNoTracking().ToListAsync(ct))
            .GroupBy(o => o.Slug)
            .Select(g => g.OrderByDescending(o => o.Version).First())
            .ToList();
        Points = latest.ToDictionary(o => o.Slug, o => o.Points);
        Stages = latest.ToDictionary(o => o.Slug, o => o.Stage);
    }

    /// <summary>Stage of a session objective, or null for a hand-added one (no stage of its own).</summary>
    public static ObjectiveStage? StageOf(string? objectiveId)
        => !string.IsNullOrEmpty(objectiveId) && Stages.TryGetValue(objectiveId, out var s) ? s : null;
}
