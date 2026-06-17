using Ti4Companion.Shared;

namespace Ti4Companion.ApiService.Data;

// ---------------------------------------------------------------------------
// Reference / game content (seeded from JSON, filtered by active expansions).
// ---------------------------------------------------------------------------

public class Faction
{
    public string Id { get; set; } = "";          // slug, e.g. "naalu"
    public string Name { get; set; } = "";
    public string NameDe { get; set; } = "";
    public Expansion Expansion { get; set; }
    public string ColorHex { get; set; } = "#888888";
    /// <summary>Fixed initiative for factions that ignore strategy order (Naalu = 0).</summary>
    public int? InitiativeOverride { get; set; }
    /// <summary>Relative path to the faction icon, e.g. "factions/naalu.png".</summary>
    public string? IconPath { get; set; }
    /// <summary>Technology slugs this faction owns from the start (fixed picks only).</summary>
    public List<string> StartingTechnologies { get; set; } = new();
}

public class StrategyCardDef
{
    public int Id { get; set; }                    // 1..8, also the printed number
    public string Name { get; set; } = "";
    public string NameDe { get; set; } = "";
    public int Initiative { get; set; }
    public string ColorHex { get; set; } = "";
    public string PrimaryText { get; set; } = "";
    public string PrimaryTextDe { get; set; } = "";
    public string SecondaryText { get; set; } = "";
    public string SecondaryTextDe { get; set; } = "";
    public string Version { get; set; } = "";
}

public class ObjectiveDef
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string NameDe { get; set; } = "";
    public string Requirement { get; set; } = "";
    public string RequirementDe { get; set; } = "";
    public int Points { get; set; }
    public ObjectiveStage Stage { get; set; }
    public Expansion Expansion { get; set; }
    /// <summary>True for secret objectives (scored privately); used as candidates for
    /// "Elect Scored Secret Objective" agendas and the secret→public flow.</summary>
    public bool IsSecret { get; set; }
}

public class TechnologyDef
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string NameDe { get; set; } = "";
    public TechColor Color { get; set; }
    /// <summary>Prerequisite pips as color letters: B=Biotic, P=Propulsion, C=Cybernetic, W=Warfare.</summary>
    public string Prerequisites { get; set; } = "";
    public string Text { get; set; } = "";
    public string TextDe { get; set; } = "";
    public Expansion Expansion { get; set; }
    /// <summary>Faction slug for faction-specific technologies; null for the common tree.</summary>
    public string? FactionId { get; set; }
    /// <summary>For unit-upgrade techs (<see cref="TechColor.Unit"/>), which unit it represents — drives
    /// the card silhouette. <see cref="UnitType.None"/> for non-unit "Unit"-coloured techs.</summary>
    public UnitType UnitType { get; set; }
}

public class AgendaDef
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string NameDe { get; set; } = "";
    public AgendaType Type { get; set; }
    /// <summary>What the agenda elects (e.g. "Player", "Cultural Planet"), or empty for For/Against.</summary>
    public string Elect { get; set; } = "";
    public string Text { get; set; } = "";
    public string TextDe { get; set; } = "";
    public Expansion Expansion { get; set; }
    /// <summary>Base-game agendas that are removed when Prophecy of Kings is in play.</summary>
    public bool RemovedInPok { get; set; }
}

/// <summary>
/// A TI4 planet, used to populate the pickers for "Elect …Planet" agendas. Sourced from the
/// community planet data (base + Prophecy of Kings); the agenda pickers also allow free text so a
/// planet missing here can still be recorded.
/// </summary>
public class Planet
{
    public string Id { get; set; } = "";          // slug, e.g. "mecatol-rex"
    public string Name { get; set; } = "";
    public string NameDe { get; set; } = "";
    public PlanetTrait Trait { get; set; }
    public int Resources { get; set; }
    public int Influence { get; set; }
    /// <summary>Faction slug whose home system this planet belongs to; null for neutral planets.</summary>
    public string? HomeFactionId { get; set; }
    public bool Legendary { get; set; }
    public Expansion Expansion { get; set; }
}

/// <summary>
/// A buildable unit at its base level: the standard units and the faction "Stufe I" units (plus
/// flagships and mechs). Reference content for a future production planner; level-II upgrades are
/// modelled as <see cref="TechnologyDef"/> unit-colour techs instead. Sourced from ti4lookup
/// <c>units.csv</c> (Twilight's Fall alternate-mode variants excluded).
/// </summary>
public class UnitDef
{
    public string Id { get; set; } = "";          // slug, e.g. "carrier-i", "letani-warrior-i"
    public string Name { get; set; } = "";
    public string NameDe { get; set; } = "";
    public UnitType UnitType { get; set; }
    /// <summary>Faction slug for faction-specific units; null for the standard units.</summary>
    public string? FactionId { get; set; }
    /// <summary>Printed build cost; null for structures placed via Construction (PDS, space dock).</summary>
    public int? Cost { get; set; }
    /// <summary>Units produced per build: 2 for fighters/infantry, otherwise 1.</summary>
    public int ProducedCount { get; set; } = 1;
    /// <summary>Combat hit value; null for units that don't fight (structures).</summary>
    public int? Combat { get; set; }
    /// <summary>Number of combat dice (the "(xN)" on a combat value); 1 by default.</summary>
    public int CombatDice { get; set; } = 1;
    public int? Move { get; set; }
    public int? Capacity { get; set; }
    /// <summary>Prose abilities ("text abilities" on the card).</summary>
    public string Text { get; set; } = "";
    public string TextDe { get; set; } = "";
    /// <summary>Period-separated keyword abilities, e.g. "SUSTAIN DAMAGE. BOMBARDMENT 5.".</summary>
    public string UnitAbilities { get; set; } = "";
    public Expansion Expansion { get; set; }
}

// ---------------------------------------------------------------------------
// Session / runtime state.
// ---------------------------------------------------------------------------

public class GameSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string JoinCode { get; set; } = "";
    public string Name { get; set; } = "";
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastActivityUtc { get; set; } = DateTimeOffset.UtcNow;
    public Language DefaultLanguage { get; set; } = Language.En;
    public Expansion ActiveExpansions { get; set; } = Expansion.Base;
    public int CurrentRound { get; set; } = 1;
    public GamePhase Phase { get; set; } = GamePhase.Setup;
    public Guid? SpeakerPlayerId { get; set; }
    public Guid? ActivePlayerId { get; set; }
    /// <summary>Strategy card whose action is currently being resolved (drives the display emphasis).</summary>
    public int? ActiveStrategyCardId { get; set; }
    /// <summary>Agenda currently up for a vote, or null.</summary>
    public string? CurrentAgendaId { get; set; }
    /// <summary>When false (default), a device may only edit its own player; when true, anyone may edit anyone.</summary>
    public bool AllowEditAllPlayers { get; set; }
    /// <summary>Inactivity window after which the auto-wipe worker may delete this session. Server-set only.</summary>
    public int RetentionHours { get; set; } = 168;
    public bool ShowTechOverview { get; set; }
    /// <summary>What the wall display currently shows (any player may switch it).</summary>
    public DisplayMode DisplayMode { get; set; } = DisplayMode.Objectives;
    /// <summary>When true, agenda votes are cast face-down and only revealed when the host flips them.</summary>
    public bool AgendaVotesHidden { get; set; }

    public List<Player> Players { get; set; } = new();
    public List<SessionObjective> Objectives { get; set; } = new();
    public List<StrategyCardState> StrategyCardStates { get; set; } = new();
    public List<AgendaVote> AgendaVotes { get; set; } = new();
}

public class Player
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public GameSession? Session { get; set; }
    public string Name { get; set; } = "";
    public string? FactionId { get; set; }
    public string ColorHex { get; set; } = "#cccccc";
    public int SeatOrder { get; set; }
    public bool HasPassed { get; set; }
    /// <summary>True once the player has confirmed faction and colour and is ready to start.</summary>
    public bool IsReady { get; set; }
    /// <summary>The session creator. Stable regardless of seat order; grants host privileges.</summary>
    public bool IsHost { get; set; }
    public string? DeviceToken { get; set; }

    public List<PlayerStrategyCard> StrategyCards { get; set; } = new();
    public List<PlayerTechnology> Technologies { get; set; } = new();
}

public class PlayerStrategyCard
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public Guid PlayerId { get; set; }
    public Player? Player { get; set; }
    public int StrategyCardId { get; set; }
    /// <summary>True once the player has performed this card's strategic action this round.</summary>
    public bool IsExhausted { get; set; }
}

/// <summary>Per-session trade goods that have accumulated on a strategy card while it went unpicked.</summary>
public class StrategyCardState
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public GameSession? Session { get; set; }
    public int StrategyCardId { get; set; }
    public int TradeGoods { get; set; }
}

public class SessionObjective
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public GameSession? Session { get; set; }
    public string ObjectiveId { get; set; } = "";
    /// <summary>Set for a hand-added objective (e.g. a secret made public); <see cref="ObjectiveId"/> is then blank.</summary>
    public string? CustomName { get; set; }
    public int? CustomPoints { get; set; }
    public DateTimeOffset RevealedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public List<ObjectiveScore> Scores { get; set; } = new();
}

public class ObjectiveScore
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionObjectiveId { get; set; }
    public SessionObjective? SessionObjective { get; set; }
    public Guid PlayerId { get; set; }
    public DateTimeOffset ScoredAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public class PlayerTechnology
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public Guid PlayerId { get; set; }
    public Player? Player { get; set; }
    public string TechnologyId { get; set; } = "";
}

/// <summary>A player's vote on the current agenda.</summary>
public class AgendaVote
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public GameSession? Session { get; set; }
    public Guid PlayerId { get; set; }
    public VoteOutcome Outcome { get; set; }
    public int Votes { get; set; }
    /// <summary>Elected candidate key for elect agendas (player id / planet id / card number / law id /
    /// free text); null for plain For/Against votes.</summary>
    public string? Choice { get; set; }
    /// <summary>Secret voting: once locked, this vote can't be opened or changed by anyone (not even
    /// the host) until the host resets all votes — so passing the tablet around doesn't leak choices.</summary>
    public bool Locked { get; set; }
}
