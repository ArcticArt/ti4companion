using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace Ti4Companion.ApiService.Data;

/// <summary>
/// Loads the bilingual TI4 reference content from JSON seed files and upserts it into the
/// database. The JSON files in <c>Data/Seed</c> are the source of truth and are copied next
/// to the binary; editing them and restarting re-syncs the content tables. Session data is
/// never touched (faction/tech/objective ids are loose string references, not foreign keys),
/// so re-seeding is always safe.
/// </summary>
public static class ContentSeeder
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static string DefaultContentRoot =>
        Path.Combine(AppContext.BaseDirectory, "Data", "Seed");

    public static async Task SeedAsync(Ti4DbContext db, ILogger logger, string? contentRoot = null, CancellationToken ct = default)
    {
        contentRoot ??= DefaultContentRoot;

        await UpsertAsync<Faction>(db, Path.Combine(contentRoot, "factions.json"), f => f.Id, logger, ct);
        await UpsertAsync<StrategyCardDef, int>(db, Path.Combine(contentRoot, "strategycards.json"), s => s.Id, logger, ct);
        await UpsertAsync<ObjectiveDef>(db, Path.Combine(contentRoot, "objectives.json"), o => o.Id, logger, ct);
        await UpsertAsync<TechnologyDef>(db, Path.Combine(contentRoot, "technologies.json"), t => t.Id, logger, ct);
        await UpsertAsync<AgendaDef>(db, Path.Combine(contentRoot, "agendas.json"), a => a.Id, logger, ct);
        await UpsertAsync<Planet>(db, Path.Combine(contentRoot, "planets.json"), p => p.Id, logger, ct);

        await db.SaveChangesAsync(ct);
    }

    private static Task UpsertAsync<T>(Ti4DbContext db, string file, Func<T, string> key, ILogger logger, CancellationToken ct)
        where T : class
        => UpsertAsync<T, string>(db, file, key, logger, ct);

    private static async Task UpsertAsync<T, TKey>(Ti4DbContext db, string file, Func<T, TKey> key, ILogger logger, CancellationToken ct)
        where T : class
        where TKey : notnull
    {
        if (!File.Exists(file))
        {
            logger.LogWarning("Seed file not found, skipping: {File}", file);
            return;
        }

        List<T> items;
        await using (var stream = File.OpenRead(file))
        {
            items = await JsonSerializer.DeserializeAsync<List<T>>(stream, JsonOptions, ct) ?? new List<T>();
        }

        var set = db.Set<T>();
        var existing = await set.ToListAsync(ct);
        var existingByKey = existing.ToDictionary(key);
        var incomingKeys = new HashSet<TKey>();

        foreach (var item in items)
        {
            var k = key(item);
            incomingKeys.Add(k);
            if (existingByKey.TryGetValue(k, out var current))
            {
                db.Entry(current).CurrentValues.SetValues(item);
            }
            else
            {
                await set.AddAsync(item, ct);
            }
        }

        // Remove content rows no longer present in the JSON.
        foreach (var stale in existing.Where(e => !incomingKeys.Contains(key(e))))
        {
            set.Remove(stale);
        }

        logger.LogInformation("Seeded {Count} {Type} from {File}", items.Count, typeof(T).Name, Path.GetFileName(file));
    }
}
