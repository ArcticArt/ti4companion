using Microsoft.EntityFrameworkCore;
using Ti4Companion.ApiService.Data;
using Ti4Companion.Shared;

namespace Ti4Companion.ApiService.Endpoints;

public static class ContentEndpoints
{
    public static IEndpointRouteBuilder MapContentEndpoints(this IEndpointRouteBuilder app)
    {
        // All reference content in one call, read from the master DB. Carries both languages; the client
        // filters by the session's active expansions and picks the language locally. Only the NEWEST
        // revision of each logical item is returned (the older printings stay in the DB for history).
        app.MapGet("/api/content", async (MasterDbContext db, CancellationToken ct) =>
        {
            var factions = Latest(await db.Factions.AsNoTracking().ToListAsync(ct));
            var cards = Latest(await db.StrategyCards.AsNoTracking().ToListAsync(ct));
            var objectives = Latest(await db.Objectives.AsNoTracking().ToListAsync(ct));
            var techs = Latest(await db.Technologies.AsNoTracking().ToListAsync(ct));
            var agendas = Latest(await db.Agendas.AsNoTracking().ToListAsync(ct));
            var planets = Latest(await db.Planets.AsNoTracking().ToListAsync(ct));
            var units = Latest(await db.Units.AsNoTracking().ToListAsync(ct));
            var abilities = Latest(await db.FactionAbilities.AsNoTracking().ToListAsync(ct));
            var leaders = Latest(await db.Leaders.AsNoTracking().ToListAsync(ct));
            var breakthroughs = await db.Breakthroughs.AsNoTracking().ToListAsync(ct);   // TE-only, no revisions
            var startingUnits = await db.FactionStartingUnits.AsNoTracking().ToListAsync(ct);
            var typeValues = await db.TypeValues.AsNoTracking().ToListAsync(ct);
            var promissoryNotes = Latest(await db.PromissoryNotes.AsNoTracking().ToListAsync(ct));
            var actionCards = Latest(await db.ActionCards.AsNoTracking().ToListAsync(ct));
            var explorations = Latest(await db.Explorations.AsNoTracking().ToListAsync(ct));
            var relics = Latest(await db.Relics.AsNoTracking().ToListAsync(ct));
            var galacticEvents = Latest(await db.GalacticEvents.AsNoTracking().ToListAsync(ct));
            var factionCards = Latest(await db.FactionCards.AsNoTracking().ToListAsync(ct));
            var systemTiles = await db.SystemTiles.AsNoTracking().ToListAsync(ct);   // not versioned

            return Results.Ok(new ContentBundleDto(
                factions.OrderBy(f => f.Expansion).ThenBy(f => f.Name).Select(Mapping.ToDto).ToList(),
                cards.OrderBy(s => s.Initiative).Select(Mapping.ToDto).ToList(),
                objectives.OrderBy(o => o.Stage).ThenBy(o => o.Name).Select(Mapping.ToDto).ToList(),
                techs.OrderBy(t => t.Color).ThenBy(t => t.Prerequisites.Length).ThenBy(t => t.Name).Select(Mapping.ToDto).ToList(),
                agendas.OrderBy(a => a.Type).ThenBy(a => a.Name).Select(Mapping.ToDto).ToList(),
                planets.OrderBy(p => p.Name).Select(Mapping.ToDto).ToList(),
                units.OrderBy(u => u.UnitType).ThenBy(u => u.FactionId).ThenBy(u => u.Name).Select(Mapping.ToDto).ToList(),
                abilities.OrderBy(a => a.FactionId).ThenBy(a => a.Order).Select(Mapping.ToDto).ToList(),
                leaders.OrderBy(l => l.FactionId).ThenBy(l => l.LeaderType).Select(Mapping.ToDto).ToList(),
                breakthroughs.OrderBy(b => b.FactionId).Select(Mapping.ToDto).ToList(),
                startingUnits.Select(Mapping.ToDto).ToList(),
                typeValues.OrderBy(t => t.Type).ThenBy(t => t.Value).Select(Mapping.ToDto).ToList(),
                promissoryNotes.OrderBy(p => p.FactionId).ThenBy(p => p.Name).Select(Mapping.ToDto).ToList(),
                actionCards.OrderBy(a => a.Name).Select(Mapping.ToDto).ToList(),
                explorations.OrderBy(e => e.Deck).ThenBy(e => e.Name).Select(Mapping.ToDto).ToList(),
                relics.OrderBy(r => r.Name).Select(Mapping.ToDto).ToList(),
                galacticEvents.OrderBy(g => g.Name).Select(Mapping.ToDto).ToList(),
                factionCards.OrderBy(f => f.FactionId).ThenBy(f => f.Name).Select(Mapping.ToDto).ToList(),
                systemTiles.OrderBy(t => t.SortOrder).ThenBy(t => t.TileNumber).Select(Mapping.ToDto).ToList()));
        });

        return app;
    }

    /// <summary>Keep only the newest revision (highest <c>Version</c>) of each logical item. Content is
    /// small (a few hundred rows), so this runs in memory after a single load per type.</summary>
    private static List<T> Latest<T>(List<T> all) where T : IMasterContent =>
        all.GroupBy(x => x.LogicalKey)
           .Select(g => g.OrderByDescending(x => x.Version).First())
           .ToList();
}
