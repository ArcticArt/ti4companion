using Microsoft.EntityFrameworkCore;

namespace Ti4Companion.ApiService.Data;

/// <summary>
/// The master reference-content database (<c>ti4master.db</c>). Holds the versioned, bilingual TI4
/// content — every row is one printing/revision of a logical item (see <c>MasterEntities.cs</c>).
/// Separate from the session DB (<see cref="Ti4DbContext"/> / <c>ti4.db</c>): content is canonical and
/// edited offline (bootstrapped once from the JSON, then maintained directly), sessions are runtime.
/// The API serves the highest <c>Version</c> per logical id; older revisions stay for history.
/// </summary>
public class MasterDbContext(DbContextOptions<MasterDbContext> options) : DbContext(options)
{
    public DbSet<Faction> Factions => Set<Faction>();
    public DbSet<StrategyCardDef> StrategyCards => Set<StrategyCardDef>();
    public DbSet<ObjectiveDef> Objectives => Set<ObjectiveDef>();
    public DbSet<TechnologyDef> Technologies => Set<TechnologyDef>();
    public DbSet<AgendaDef> Agendas => Set<AgendaDef>();
    public DbSet<Planet> Planets => Set<Planet>();
    public DbSet<UnitDef> Units => Set<UnitDef>();
    public DbSet<FactionAbility> FactionAbilities => Set<FactionAbility>();
    public DbSet<Leader> Leaders => Set<Leader>();
    public DbSet<Breakthrough> Breakthroughs => Set<Breakthrough>();
    public DbSet<FactionStartingUnit> FactionStartingUnits => Set<FactionStartingUnit>();
    public DbSet<TypeValue> TypeValues => Set<TypeValue>();
    public DbSet<PromissoryNote> PromissoryNotes => Set<PromissoryNote>();
    public DbSet<ActionCard> ActionCards => Set<ActionCard>();
    public DbSet<Exploration> Explorations => Set<Exploration>();
    public DbSet<Relic> Relics => Set<Relic>();
    public DbSet<GalacticEvent> GalacticEvents => Set<GalacticEvent>();
    public DbSet<FactionCard> FactionCards => Set<FactionCard>();
    public DbSet<SystemTile> SystemTiles => Set<SystemTile>();
    public DbSet<UnitAbilityEntry> UnitAbilities => Set<UnitAbilityEntry>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // Surrogate Guid PKs are assigned in the entity initializers → app-generated.
        foreach (var type in new[]
                 {
                     typeof(Faction), typeof(StrategyCardDef), typeof(ObjectiveDef), typeof(TechnologyDef),
                     typeof(AgendaDef), typeof(Planet), typeof(UnitDef), typeof(FactionAbility),
                     typeof(Leader), typeof(Breakthrough), typeof(FactionStartingUnit),
                     typeof(PromissoryNote), typeof(ActionCard), typeof(Exploration), typeof(Relic),
                     typeof(GalacticEvent), typeof(FactionCard), typeof(SystemTile),
                     typeof(UnitAbilityEntry),
                 })
        {
            b.Entity(type).Property<Guid>("Id").ValueGeneratedNever();
        }

        // Atomic unit abilities: a child of EITHER a UnitDef or a unit-upgrade TechnologyDef (one FK set).
        b.Entity<UnitDef>().HasMany(u => u.Abilities).WithOne()
            .HasForeignKey(a => a.UnitId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<TechnologyDef>().HasMany(t => t.Abilities).WithOne()
            .HasForeignKey(a => a.TechnologyId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<UnitAbilityEntry>().HasIndex(x => x.UnitId);
        b.Entity<UnitAbilityEntry>().HasIndex(x => x.TechnologyId);

        // One row per (logical id, version); the API serves the highest version per logical id.
        b.Entity<Faction>().HasIndex(x => new { x.Slug, x.Version }).IsUnique();
        b.Entity<StrategyCardDef>().HasIndex(x => new { x.Number, x.Version }).IsUnique();
        b.Entity<ObjectiveDef>().HasIndex(x => new { x.Slug, x.Version }).IsUnique();
        b.Entity<TechnologyDef>().HasIndex(x => new { x.Slug, x.Version }).IsUnique();
        b.Entity<AgendaDef>().HasIndex(x => new { x.Slug, x.Version }).IsUnique();
        b.Entity<Planet>().HasIndex(x => new { x.Slug, x.Version }).IsUnique();
        b.Entity<UnitDef>().HasIndex(x => new { x.Slug, x.Version }).IsUnique();
        b.Entity<FactionAbility>().HasIndex(x => new { x.Slug, x.Version }).IsUnique();
        b.Entity<Leader>().HasIndex(x => new { x.Slug, x.Version }).IsUnique();
        b.Entity<Breakthrough>().HasIndex(x => x.Slug).IsUnique();   // TE-only: no Version, slug is unique
        b.Entity<PromissoryNote>().HasIndex(x => new { x.Slug, x.Version }).IsUnique();
        b.Entity<ActionCard>().HasIndex(x => new { x.Slug, x.Version }).IsUnique();
        b.Entity<Exploration>().HasIndex(x => new { x.Slug, x.Version }).IsUnique();
        b.Entity<Relic>().HasIndex(x => new { x.Slug, x.Version }).IsUnique();
        b.Entity<GalacticEvent>().HasIndex(x => new { x.Slug, x.Version }).IsUnique();
        b.Entity<FactionCard>().HasIndex(x => new { x.Slug, x.Version }).IsUnique();
        // System tiles are keyed by the printed tile number (string); not versioned.
        b.Entity<SystemTile>().HasIndex(x => x.TileNumber).IsUnique();

        // Bilingual enum-value labels: composite key (Type, Value), no surrogate Guid.
        b.Entity<TypeValue>().HasKey(x => new { x.Type, x.Value });

        // Faction-scoped lookups for the per-faction child content.
        b.Entity<FactionAbility>().HasIndex(x => x.FactionId);
        b.Entity<Leader>().HasIndex(x => x.FactionId);
        b.Entity<Breakthrough>().HasIndex(x => x.FactionId);
        b.Entity<FactionStartingUnit>().HasIndex(x => x.FactionId);
        b.Entity<FactionCard>().HasIndex(x => x.FactionId);
        b.Entity<PromissoryNote>().HasIndex(x => x.FactionId);
    }
}
