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

/// <summary>
/// Granular provenance of a single piece of content — the exact printing it comes from. This is distinct
/// from the coarse <see cref="Expansion"/> [Flags] set used to filter content by a session's active
/// expansions: every content item carries BOTH a single <c>ContentSource</c> (its origin, so e.g. the
/// three printings of Construction can be told apart) and an <see cref="Expansion"/> (derived from the
/// source) for the "is this active in the session" checks. Codex 4 is reserved per request — only
/// Codex I–III have shipped so far. Keep the numeric values stable (persisted in the master DB).
/// </summary>
public enum ContentSource
{
    Base = 0,
    ProphecyOfKings = 1,
    Codex1 = 2,
    Codex2 = 3,
    Codex3 = 4,
    Codex4 = 5,
    ThundersEdge = 6
}

/// <summary>
/// A Prophecy of Kings leader slot. Each faction has one of each (Agent, Commander, Hero); the Council
/// Keleres and Thunder's Edge faction leaders still fit these three slots.
/// </summary>
public enum LeaderType
{
    Agent = 0,
    Commander = 1,
    Hero = 2
}

/// <summary>The 8 TI4 player colours. Used for a faction's preferred-colour ranking (players still pick a
/// hex from the palette; this is the named colour). Labelled bilingually in the <c>TypeValues</c> table.</summary>
public enum PlayerColor
{
    Purple = 0,
    Pink = 1,
    Red = 2,
    Black = 3,
    Blue = 4,
    Green = 5,
    Yellow = 6,
    Orange = 7
}

/// <summary>A faction's complexity / difficulty rating.</summary>
public enum FactionComplexity
{
    Low = 0,
    Moderate = 1,
    High = 2
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

/// <summary>
/// The status-phase steps that follow the scoring, as a shared checklist so the whole table sees the same
/// ticks. The list is the one the user gave (command tokens discarded/gained are one entry, as at their
/// table) — it is a reminder of the sequence, not a transcription of the rulebook, and nothing is enforced.
/// </summary>
[Flags]
public enum StatusStep
{
    None = 0,
    /// <summary>No longer part of the checklist — revealing is its own stage now
    /// (<see cref="StatusStage.RevealObjective"/>). The flag is kept (and set when that stage is left) so
    /// no stored value has to be rewritten.</summary>
    RevealObjective = 1,
    DrawActionCards = 2,
    CommandTokens = 4,
    ReadyCards = 8,
    RepairUnits = 16,
    ReturnStrategyCards = 32,
    /// <summary>Abilities that trigger during or at the end of the status phase (the Sol flagship
    /// <em>Genesis</em>, for example). Not a rulebook step — a reminder the table asked for, because it is
    /// the one thing that gets forgotten once the checklist is done.</summary>
    EndOfStatusAbilities = 64
}

/// <summary>Where the table is inside the status phase. Advanced by the host with "next" (and back), reset
/// whenever the phase is entered and on a round change. Scoring itself runs player by player inside
/// <see cref="Scoring"/> (see <c>TurnService.CurrentScorer</c>).</summary>
public enum StatusStage
{
    /// <summary>Each player in initiative order may score one objective by tapping its card.</summary>
    Scoring = 0,
    /// <summary>The next public objective is revealed and shown large on the wall.</summary>
    RevealObjective = 1,
    /// <summary>The remaining upkeep steps as a checklist, shown large on the wall.</summary>
    Checklist = 2
}

/// <summary>
/// Which Red Tape variant the table plays. Both are community variants, so the app tracks the tape and
/// blocks scoring a taped objective — it does not run the rest of the rules (purging, the random removal
/// timing, the Stage II gate); those stay with the table.
/// <para>
/// Sources: "Bureaucracy: Red Tape for TI4" by WildFalkon (BGG file 221470) and "Red Tape Lite" by
/// van nguyen (BGG thread 3553379).
/// </para>
/// </summary>
public enum RedTapeVariant
{
    None = 0,
    /// <summary>The full variant: every public objective face-up at setup, taped; the player who takes the
    /// carrier strategy card removes counters equal to the trade goods on it.</summary>
    Bureaucracy = 1,
    /// <summary>The leaner take: only the first two objectives start untaped, five Stage I can ever score,
    /// and if nobody takes the carrier card one counter comes off at random.</summary>
    Lite = 2
}

public enum ObjectiveStage
{
    StageI = 1,
    StageII = 2,
    /// <summary>Secret objective (scored privately). Folds in the old separate <c>IsSecret</c> flag —
    /// "secret" is just a third stage.</summary>
    Secret = 3
}

/// <summary>TI4 technology colors. <see cref="Unit"/> covers colorless unit upgrades; <see cref="None"/> is
/// for the few faction technologies that have NO colour at all (the Nekro Valefar Assimilators X/Y) — distinct
/// from <see cref="Unit"/> so they don't render as unit cards.</summary>
public enum TechColor
{
    Biotic = 0,
    Propulsion = 1,
    Cybernetic = 2,
    Warfare = 3,
    Unit = 4,
    None = 5
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

/// <summary>An atomic unit ability keyword (the ★ bullets on a unit / unit-upgrade card), stored
/// relationally (see <c>UnitAbilityEntry</c>) instead of parsed from free text, so it can be localized
/// for display. The keyword's printed value (BOMBARDMENT <b>5</b>) and "(xN)" multiplier live on the
/// entry, not here. Keep the numeric values stable (persisted in the master DB).</summary>
public enum UnitAbility
{
    None = 0,
    SustainDamage = 1,
    AntiFighterBarrage = 2,
    Bombardment = 3,
    SpaceCannon = 4,
    PlanetaryShield = 5,
    Production = 6,
    Deploy = 7
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

/// <summary>Wormhole types on a system tile ("Systemtafel"). <see cref="Flags"/> because a tile can carry
/// several at once (the flipped Mallice tile has Alpha+Beta+Gamma). Delta = the Ghosts of Creuss,
/// Epsilon = the Crimson Rebellion (both Thunder's Edge era for Epsilon).</summary>
[Flags]
public enum WormholeType
{
    None = 0,
    Alpha = 1,
    Beta = 2,
    Gamma = 4,
    Delta = 8,
    Epsilon = 16
}

/// <summary>Anomaly types on a system tile. <see cref="Flags"/> because a tile may carry more than one
/// (the Thunder's Edge Watchtower is a Gravity Rift + Asteroid Field). <see cref="MuaatSupernova"/> and
/// <see cref="EntropicScar"/> are distinct named anomalies; <see cref="Egress"/> is a Fracture-tile anomaly.</summary>
[Flags]
public enum AnomalyType
{
    None = 0,
    GravityRift = 1,
    Nebula = 2,
    Supernova = 4,
    AsteroidField = 8,
    MuaatSupernova = 16,
    EntropicScar = 32,
    Egress = 64
}

/// <summary>The printed colour of a system tile's back: Green = home system, Blue = planet system,
/// Red = anomaly/empty system. <see cref="None"/> for special tiles (Mecatol, hyperlanes, fracture).</summary>
public enum SystemTileColor
{
    None = 0,
    Green = 1,
    Blue = 2,
    Red = 3
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
    Tech = 2,
    /// <summary>Match statistics (timing). Set ONLY by the host "End game" action — it is deliberately
    /// not offered in the display-control segments, so players can't switch the wall to it mid-game.</summary>
    Statistics = 3,
    /// <summary>The join QR code, large, as its own wall area. In the agenda phase there is no right panel,
    /// so it renders as a centred overlay instead.</summary>
    JoinQr = 4
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
    VoteLock = 19,      // TargetPlayerId = voter, Detail = "outcome:votes:choice" (no longer emitted)
    InfluenceSet = 20,  // TargetPlayerId = player, Detail = influence value (no longer emitted)
    /// <summary>One summary per concluded agenda: Detail = "agendaId|for|against|topChoiceKey|topVotes|runnerUpVotes".
    /// The agenda-phase log shows only this + <see cref="AgendaReveal"/> (the per-change noise isn't logged).</summary>
    AgendaResult = 21,
    /// <summary>Host paused the game. <see cref="GameResumed"/> ends it. The interval between them is
    /// subtracted from all statistics durations (a pause doesn't count as play time).</summary>
    GamePaused = 22,
    GameResumed = 23,
    /// <summary>Host published the totals of a face-down vote without the attribution (intermediate step
    /// before <see cref="AgendaReveal2"/>).</summary>
    AgendaRevealTotals = 24,
    /// <summary>A player started working through a strategy card's SECONDARY ability (TargetPlayerId = that
    /// player, Detail = card number). Ended by <see cref="SecondaryDone"/>.
    ///
    /// These intervals OVERLAP each other and the active player's turn by design — that is precisely what
    /// makes "decision time on secondaries" separable from "time on turn" without asking the table for any
    /// extra bookkeeping. New numeric values, so no migration was needed; keep them stable, the statistics
    /// view diffs them.</summary>
    SecondaryStart = 25,
    SecondaryDone = 26,

    /// <summary>Red Tape Lite: nobody took the carrier card, so the app took one tape off at random
    /// (Detail = the objective). Logged because it is the one game action the app takes on its own —
    /// the table has to be able to see that it happened, and to which objective.</summary>
    RedTapeRandom = 27,

    /// <summary>Red Tape Lite: an objective was purged once five Stage I were clear (Detail = the
    /// objective). It can never be scored afterwards.</summary>
    RedTapePurge = 28,

    /// <summary>A combat was declared (Actor = who declared it, Target = the opponent) and resolved. The
    /// interval between the two is excluded from time-on-turn, the same way a pause is — see
    /// <c>MatchStats</c>. Keep the numeric values stable, the statistics read them.</summary>
    CombatStart = 29,
    CombatEnd = 30,

    /// <summary>A device took over an existing seat, the HOST's seat included (Target = that seat). Worth a
    /// line in the log precisely because it can hand the host role to another device: the table should be
    /// able to see when that happened, since there is no account behind it, only the join code.</summary>
    SeatClaim = 31,

    /// <summary>A player had the technology picker open (Target = that player) and closed it again. The
    /// interval is excluded from time-on-turn exactly like a combat is: looking up a technology in the app is
    /// the app's overhead, not the player's thinking time. Keep the numeric values stable — the statistics
    /// read them.
    /// <para><b>No longer emitted</b> since the per-player picker became the table-wide prompt below; kept
    /// because old logs contain them and the statistics still subtract them.</para></summary>
    TechPickStart = 32,
    TechPickEnd = 33,

    /// <summary>A tape the variant's timing rules held shut was removed anyway, on the host's explicit
    /// confirmation (Detail = the objective). Logged because the app was overruled: the table is the authority
    /// on its own game, but the rest of it should be able to see that it happened.</summary>
    RedTapeOverride = 34,

    /// <summary>The secondary round of a strategy action opened (Detail = card number) and closed again.
    /// <para>
    /// The per-player <see cref="SecondaryStart"/>/<see cref="SecondaryDone"/> entries say who was on the
    /// clock; these two bound the round itself, which is what the statistics need: a secondary ability
    /// happens BETWEEN two turns, so the interval is subtracted from the next active player's
    /// time-on-turn. Without a round-level entry the client cannot see the stretch where the round was
    /// open but nobody had been ticked off yet — which is exactly the stretch the host spends asking.
    /// Keep the numeric values stable.
    /// </para></summary>
    SecondaryRoundOpen = 35,
    SecondaryRoundClose = 36,

    /// <summary>The table is recording the technologies from a Technology action, and is done again
    /// (Detail = the card number on open). Like a secondary round this is a round-level bracket, and for the
    /// same reason: **the clock stands still while it is open**, so the interval is subtracted from
    /// time-on-turn. It closes when every player has said they are done, or when the player who played the
    /// card (or the host) moves the table on. Keep the numeric values stable.</summary>
    TechPromptOpen = 37,
    TechPromptClose = 38
}
