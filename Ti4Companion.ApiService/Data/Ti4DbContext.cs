using Microsoft.EntityFrameworkCore;

namespace Ti4Companion.ApiService.Data;

public class Ti4DbContext(DbContextOptions<Ti4DbContext> options) : DbContext(options)
{
    // Session state (reference content lives in MasterDbContext / ti4master.db)
    public DbSet<GameSession> Sessions => Set<GameSession>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<PlayerStrategyCard> PlayerStrategyCards => Set<PlayerStrategyCard>();
    public DbSet<StrategyCardState> StrategyCardStates => Set<StrategyCardState>();
    public DbSet<SessionObjective> SessionObjectives => Set<SessionObjective>();
    public DbSet<ObjectiveScore> ObjectiveScores => Set<ObjectiveScore>();
    public DbSet<PlayerTechnology> PlayerTechnologies => Set<PlayerTechnology>();
    public DbSet<AgendaVote> AgendaVotes => Set<AgendaVote>();
    public DbSet<SessionLogEntry> SessionLog => Set<SessionLogEntry>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // ---- Sessions ----
        // Guid keys are assigned in the entity initializers, so treat them as app-generated.
        foreach (var entity in new[]
                 {
                     typeof(GameSession), typeof(Player), typeof(PlayerStrategyCard),
                     typeof(StrategyCardState), typeof(SessionObjective), typeof(ObjectiveScore),
                     typeof(PlayerTechnology), typeof(AgendaVote), typeof(SessionLogEntry),
                 })
        {
            b.Entity(entity).Property<Guid>("Id").ValueGeneratedNever();
        }

        b.Entity<GameSession>().HasIndex(x => x.JoinCode).IsUnique();

        b.Entity<GameSession>()
            .HasMany(x => x.Players).WithOne(p => p.Session)
            .HasForeignKey(p => p.SessionId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<GameSession>()
            .HasMany(x => x.Objectives).WithOne(o => o.Session)
            .HasForeignKey(o => o.SessionId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<GameSession>()
            .HasMany(x => x.StrategyCardStates).WithOne(s => s.Session)
            .HasForeignKey(s => s.SessionId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<GameSession>()
            .HasMany(x => x.AgendaVotes).WithOne(v => v.Session)
            .HasForeignKey(v => v.SessionId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<Player>()
            .HasMany(p => p.StrategyCards).WithOne(s => s.Player)
            .HasForeignKey(s => s.PlayerId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<Player>()
            .HasMany(p => p.Technologies).WithOne(t => t.Player)
            .HasForeignKey(t => t.PlayerId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<SessionObjective>()
            .HasMany(o => o.Scores).WithOne(s => s.SessionObjective)
            .HasForeignKey(s => s.SessionObjectiveId).OnDelete(DeleteBehavior.Cascade);

        // Match log: no navigation on GameSession (kept out of the loaded graph), but cascade-deleted.
        b.Entity<SessionLogEntry>()
            .HasOne<GameSession>().WithMany()
            .HasForeignKey(l => l.SessionId).OnDelete(DeleteBehavior.Cascade);

        // Helpful lookups
        b.Entity<Player>().HasIndex(x => x.SessionId);
        b.Entity<PlayerStrategyCard>().HasIndex(x => new { x.SessionId, x.PlayerId });
        b.Entity<PlayerTechnology>().HasIndex(x => new { x.SessionId, x.PlayerId });
        b.Entity<SessionObjective>().HasIndex(x => x.SessionId);
        b.Entity<StrategyCardState>().HasIndex(x => new { x.SessionId, x.StrategyCardId }).IsUnique();
        b.Entity<AgendaVote>().HasIndex(x => new { x.SessionId, x.PlayerId }).IsUnique();
        b.Entity<SessionLogEntry>().HasIndex(x => new { x.SessionId, x.TimestampUtc });
    }
}
