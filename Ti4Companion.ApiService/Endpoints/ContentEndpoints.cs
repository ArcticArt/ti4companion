using Microsoft.EntityFrameworkCore;
using Ti4Companion.ApiService.Data;
using Ti4Companion.Shared;

namespace Ti4Companion.ApiService.Endpoints;

public static class ContentEndpoints
{
    public static IEndpointRouteBuilder MapContentEndpoints(this IEndpointRouteBuilder app)
    {
        // All reference content in one call. Carries both languages; the client filters by the
        // session's active expansions and picks the language locally.
        app.MapGet("/api/content", async (Ti4DbContext db, CancellationToken ct) =>
        {
            var factions = await db.Factions.AsNoTracking().OrderBy(f => f.Expansion).ThenBy(f => f.Name).ToListAsync(ct);
            var cards = await db.StrategyCards.AsNoTracking().OrderBy(s => s.Initiative).ToListAsync(ct);
            var objectives = await db.Objectives.AsNoTracking().OrderBy(o => o.Stage).ThenBy(o => o.Name).ToListAsync(ct);
            var techs = await db.Technologies.AsNoTracking().OrderBy(t => t.Color).ThenBy(t => t.Prerequisites.Length).ThenBy(t => t.Name).ToListAsync(ct);
            var agendas = await db.Agendas.AsNoTracking().OrderBy(a => a.Type).ThenBy(a => a.Name).ToListAsync(ct);
            var planets = await db.Planets.AsNoTracking().OrderBy(p => p.Name).ToListAsync(ct);
            var units = await db.Units.AsNoTracking().OrderBy(u => u.UnitType).ThenBy(u => u.FactionId).ThenBy(u => u.Name).ToListAsync(ct);

            return Results.Ok(new ContentBundleDto(
                factions.Select(Mapping.ToDto).ToList(),
                cards.Select(Mapping.ToDto).ToList(),
                objectives.Select(Mapping.ToDto).ToList(),
                techs.Select(Mapping.ToDto).ToList(),
                agendas.Select(Mapping.ToDto).ToList(),
                planets.Select(Mapping.ToDto).ToList(),
                units.Select(Mapping.ToDto).ToList()));
        });

        return app;
    }
}
