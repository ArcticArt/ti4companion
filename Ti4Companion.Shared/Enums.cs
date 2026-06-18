namespace Ti4Companion.Shared;

/// <summary>
/// TI4 content origin. Used both as a single value on content (each item belongs to one set)
/// and as a [Flags] set on a session to describe which expansions are active.
/// </summary>
[Flags]
public enum Expansion
{
    None = 0,
    Base = 1,
    ProphecyOfKings = 2,
    Codex = 4,
    ThundersEdge = 8
}

/// <summary>The phases of a TI4 game round, in order.</summary>
public enum GamePhase
{
    Setup = 0,
    Strategy = 1,
    Action = 2,
    Status = 3,
    Agenda = 4
}

public enum ObjectiveStage
{
    StageI = 1,
    StageII = 2
}

/// <summary>TI4 technology colors. <see cref="Unit"/> covers colorless unit upgrades.</summary>
public enum TechColor
{
    Biotic = 0,
    Propulsion = 1,
    Cybernetic = 2,
    Warfare = 3,
    Unit = 4
}

/// <summary>
/// Which physical unit a unit-upgrade technology represents — drives the card art and lets a faction
/// variant (e.g. "Crimson Legionnaire II") reuse the base unit's silhouette. <see cref="None"/> is for
/// "Unit"-coloured techs that aren't actually unit upgrades (the Nekro Valefar Assimilators).
/// </summary>
public enum UnitType
{
    None = 0,
    Carrier = 1,
    Cruiser = 2,
    Destroyer = 3,
    Fighter = 4,
    Infantry = 5,
    Pds = 6,
    SpaceDock = 7,
    Dreadnought = 8,
    WarSun = 9,
    Flagship = 10,
    Mech = 11
}

public enum AgendaType
{
    Law = 0,
    Directive = 1
}

public enum VoteOutcome
{
    Abstain = 0,
    For = 1,
    Against = 2
}

/// <summary>
/// What an agenda elects, parsed from the agenda's free-text <c>Elect</c> field. <see cref="ForAgainst"/>
/// is the plain For/Against vote; the rest pick a target (player, planet, law, …) that the votes back.
/// </summary>
public enum ElectType
{
    ForAgainst = 0,
    Player = 1,
    Planet = 2,
    CulturalPlanet = 3,
    HazardousPlanet = 4,
    IndustrialPlanet = 5,
    NonHomePlanet = 6,
    Law = 7,
    StrategyCard = 8,
    ScoredSecret = 9
}

/// <summary>TI4 planet trait used by "Elect Cultural/Hazardous/Industrial Planet" agendas.</summary>
public enum PlanetTrait
{
    None = 0,
    Cultural = 1,
    Hazardous = 2,
    Industrial = 3
}

public enum Language
{
    En = 0,
    De = 1
}

/// <summary>What the wall display (`/display`) currently shows alongside the player list.</summary>
public enum DisplayMode
{
    Objectives = 0,
    Secondary = 1,   // strategy-card secondary abilities for un-exhausted cards
    Tech = 2
}

/// <summary>
/// Type of a match-log event. The log is structured (not pre-rendered prose) so the client can
/// localize it; <see cref="SessionLogEntry"/> carries the actor/target/phase/round/detail. The
/// timeline kinds (<see cref="PhaseChange"/>, <see cref="RoundChange"/>, <see cref="TurnChange"/>)
/// are what the statistics view diffs to derive match/round/phase/per-player durations — keep their
/// numeric values stable. <see cref="Generic"/> is a catch-all.
/// </summary>
public enum SessionLogKind
{
    Generic = 0,
    PhaseChange = 1,    // Phase = new phase, Round = current round
    RoundChange = 2,    // Round = new round
    TurnChange = 3,     // TargetPlayerId = new active player
    PlayerJoin = 4,
    PlayerUpdate = 5,
    SpeakerSet = 6,     // TargetPlayerId = new speaker
    StrategyPick = 7,   // TargetPlayerId = owner, Detail = card number
    StrategyReturn = 8,
    StrategyAction = 9, // Detail = card number (played) or empty (cleared)
    Pass = 10,          // TargetPlayerId = player who passed
    ObjectiveReveal = 11,  // Detail = objective id or custom name
    ObjectiveScore = 12,   // TargetPlayerId = scorer, Detail = session-objective label
    TechAdd = 13,       // TargetPlayerId = owner, Detail = tech id
    TechRemove = 14,
    AgendaReveal = 15,  // Detail = agenda id (or empty when cleared)
    AgendaStartVote = 16, // Detail = "hidden"/"open"
    AgendaCancel = 17,
    AgendaReveal2 = 18, // host flips hidden votes face-up
    VoteLock = 19,      // TargetPlayerId = voter, Detail = "outcome:votes:choice"
    InfluenceSet = 20   // TargetPlayerId = player, Detail = influence value
}
