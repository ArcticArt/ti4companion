using Ti4Companion.Shared;

namespace Ti4Companion.ApiService.Data;

// ---------------------------------------------------------------------------
// Session / runtime state.
//
// Reference / game content (factions, strategy cards, objectives, technologies, agendas, planets,
// units, leaders, breakthroughs, faction abilities/starting units) now lives in its own master DB —
// see Data/Master/MasterEntities.cs + MasterDbContext. Session entities reference content only by its
// loose string logical id (slug / strategy-card number), never by foreign key.
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
    /// <summary>A FREE vote with no agenda card: the title the host typed (null = none running). Mutually
    /// exclusive with <see cref="CurrentAgendaId"/> — the whole agenda machinery (influence, hidden/open,
    /// locking, totals, tally, wall) keys off the elect kind, so a free vote only has to supply that plus a
    /// headline instead of an agenda.</summary>
    public string? CustomVoteTitle { get; set; }

    /// <summary>What the free vote elects. Null unless <see cref="CustomVoteTitle"/> is set.</summary>
    public ElectType? CustomVoteElect { get; set; }

    /// <summary>When false (default), a device may only edit its own player; when true, anyone may edit anyone.</summary>
    public bool AllowEditAllPlayers { get; set; }
    /// <summary>Inactivity window after which the auto-wipe worker may delete this session (0 = keep
    /// forever). Server-set only, from <c>Ti4:DefaultRetentionHours</c>. A <see cref="Paused"/> session
    /// gets the longer <c>Ti4:PausedRetentionHours</c> window instead — see SessionCleanupWorker.</summary>
    public int RetentionHours { get; set; } = 2160;
    public bool ShowTechOverview { get; set; }
    /// <summary>What the wall display currently shows (any player may switch it).</summary>
    public DisplayMode DisplayMode { get; set; } = DisplayMode.JoinQr;   // a fresh session opens on the QR: people are still joining
    /// <summary>When true, agenda votes are cast face-down and only revealed when the host flips them.</summary>
    public bool AgendaVotesHidden { get; set; }

    /// <summary>Intermediate step of a face-down vote: the totals are public but not who voted what.
    /// Only meaningful while <see cref="AgendaVotesHidden"/> is still true; reset with the vote.</summary>
    public bool AgendaTotalsRevealed { get; set; }
    /// <summary>Agenda phase: false while players enter their available influence and the host picks an
    /// agenda; true once the host has started the vote (influence then locks). Cleared on cancel / new
    /// agenda / next round.</summary>
    public bool VotingStarted { get; set; }

    /// <summary>Host paused the game: all mutations are rejected (except resume) and the paused interval is
    /// excluded from the statistics.</summary>
    public bool Paused { get; set; }

    /// <summary>Time budget per player per round in seconds; <c>0</c> (the default) means no turn timer.
    /// Purely informational — running out is signalled, never enforced.</summary>
    public int TurnTimerSeconds { get; set; }

    /// <summary>Strategy cards per player per round: <c>0</c> (default) follows the printed rule, <c>1</c>
    /// or <c>2</c> pin it. See <see cref="GameRules.StrategyCardsPerPlayer"/>.</summary>
    public int StrategyCardsPerPlayer { get; set; }

    /// <summary>Which Red Tape variant is in play (<see cref="RedTapeVariant.None"/> = off). A taped
    /// objective carries a removable marker and cannot be scored.</summary>
    public RedTapeVariant RedTapeVariant { get; set; }

    /// <summary>Which strategy card carries the Red Tape ability. <b>Bureaucracy</b> replaces either
    /// Diplomacy (2) or Imperial (8) and the table decides which; <b>Lite</b> is always Diplomacy (it
    /// replaces no card, so there is nothing to choose). Settled by
    /// <see cref="GameRules.RedTapeCarrierCard"/> whenever the variant or the choice changes, so it can never
    /// disagree with the variant. 0 while no variant is set.</summary>
    public int RedTapeCardNumber { get; set; }

    /// <summary>Follow who is taking a strategy card's secondary ability, so each of them can stop their own
    /// clock (see the secondary round). Its own option rather than riding on the turn timer, but only
    /// offered while the timer is on — without a clock there is nothing for it to separate.</summary>
    public bool TrackSecondaryAbilities { get; set; }

    /// <summary>The round in which Red Tape Lite's random removal was ANSWERED (0 = never) — confirmed or
    /// declined, either way it is settled for that round. It is due once per round at most, and this is what
    /// makes that idempotent across the two places the status phase can end.</summary>
    public int RedTapeRandomRound { get; set; }

    /// <summary>The round a random removal is currently being ASKED about (0 = nothing pending). The app no
    /// longer takes the tape off by itself: it proposes, the table confirms (see
    /// <see cref="Services.RedTape.ProposeRandom"/>). Stored as the round rather than a bool because the
    /// proposal outlives the round change — <c>NextRound</c> raises it while still in the old round, so
    /// answering it afterwards must settle THAT round, not the new one.</summary>
    public int RedTapeRandomPendingRound { get; set; }

    /// <summary>Status phase: which of the post-scoring steps the table has ticked off. Reset when the
    /// status phase begins.</summary>
    public StatusStep StatusStepsDone { get; set; }

    /// <summary>Status phase: which of the three stages (score → reveal → checklist) the table is on. Reset
    /// with <see cref="StatusStepsDone"/>.</summary>
    public StatusStage StatusStage { get; set; }

    /// <summary>The strategy card whose secondary the table is working through, or null when no secondary
    /// round is open. Deliberately survives the turn advance: the other players keep resolving their
    /// secondary after the active player is done, and their clocks have to keep running until they say so.
    /// Closed when the next strategy action is played, or when the action phase / round ends.
    /// Only ever set while the turn timer is in use — without time tracking there is nothing to gain from
    /// tracking secondaries, and the app should not ask the table for input it has no use for.</summary>
    public int? SecondaryCardId { get; set; }

    /// <summary>Who played that primary. They (and the host) run the secondary round; a player may always
    /// declare their own secondary done.</summary>
    public Guid? SecondaryOwnerId { get; set; }

    /// <summary>Politics was played and no new speaker has been appointed yet. The turn cannot be ended
    /// until it is — the one place the app blocks a step, because the whole card is that appointment and
    /// forgetting it silently changes who picks first next round.</summary>
    public bool SpeakerPending { get; set; }

    /// <summary>A combat is being resolved: the player who declared it and their opponent (both null when
    /// no combat is running). The wall shows the two of them facing each other, and the turn clock stops —
    /// rolling dice against someone else is not "time on turn". See <c>MatchStats</c>.</summary>
    public Guid? CombatAId { get; set; }
    public Guid? CombatBId { get; set; }

    /// <summary>Offer to record a technology right after the Technology strategy action was played.
    /// A table decision — the app never forces the entry.</summary>
    public bool PromptTechOnAction { get; set; }

    /// <summary>Who currently has the technology picker open (null = nobody). Session state rather than a
    /// per-device flag for one reason: while it is open, that player's TIME ON TURN stops, exactly as a combat
    /// stops it — hunting for a technology in a list is the app's overhead, not the player's thinking time.
    /// A clock that only stopped on the device holding the popup would disagree with the wall.</summary>
    public Guid? TechPickPlayerId { get; set; }

    /// <summary>Superseded by <c>DisplayMode.JoinQr</c> (the QR became one of the wall areas, so a separate
    /// flag would be a second source of truth for one screen). Kept only so no migration has to drop a
    /// column; nothing reads it any more.</summary>
    public bool ShowJoinQr { get; set; } = true;

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
    /// <summary>Status phase: this player is done scoring, so the turn moves on in initiative order.
    /// Reset when the status phase begins.</summary>
    public bool StatusDone { get; set; }
    /// <summary>Strategy action: this player said they take the secondary and has not finished yet, so their
    /// clock runs alongside the active player's. Cleared by their own "done" and when the turn moves on.
    /// This is the only point where several players' clocks legitimately run at once — and therefore the
    /// only place "decision time on secondaries" can be measured without asking the table for extra input.</summary>
    public bool SecondaryPending { get; set; }
    /// <summary>True once the player has confirmed faction and colour and is ready to start.</summary>
    public bool IsReady { get; set; }
    /// <summary>The session creator. Stable regardless of seat order; grants host privileges.</summary>
    public bool IsHost { get; set; }
    /// <summary>Available influence the player entered for the agenda phase. Not a vote cap (action
    /// cards/abilities can exceed it); used for display and auto-deducted when the next agenda is
    /// revealed. Reset at the start of each agenda phase / round.</summary>
    public int Influence { get; set; }
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
    /// <summary>Red Tape variant: the tape that sits on this objective has been pulled off. Only shown while
    /// <see cref="GameSession.RedTapeVariant"/> is set; until then the objective cannot be scored — the one
    /// rule the app does enforce, and only for a table that chose the variant.</summary>
    public bool MarkerRemoved { get; set; }

    /// <summary>Red Tape Lite: purged once five Stage I objectives were clear — it can never be scored and its
    /// tape never comes off. See <see cref="Services.RedTape.ConfirmPurge"/>.</summary>
    public bool Purged { get; set; }

    /// <summary>Red Tape Lite: proposed for purging, awaiting the table's confirmation. Flagged at the moment
    /// the fifth Stage I tape came off and cleared either way when the question is answered.
    /// <para>
    /// This flag is the whole reason a purge cannot sweep up an objective revealed LATER: only what was
    /// already on the table when the fifth came clear is ever marked, so a later reveal can never be part of
    /// the set no matter how long the question goes unanswered.
    /// </para></summary>
    public bool PurgePending { get; set; }
    public List<ObjectiveScore> Scores { get; set; } = new();
}

/// <summary>
/// One Web Push subscription: a browser on one device, for one player in one session. The endpoint URL is
/// the identity a push service gives out, so it is the unique key — resubscribing the same browser updates
/// the row instead of piling up duplicates.
///
/// Deliberately NOT part of the session graph (no navigation from <see cref="GameSession"/>): it is written
/// and read on its own, and loading it with every session read would put encryption keys into every DTO
/// path for no reason. It IS deleted with the session, by the cleanup worker, since the subscription is
/// worthless afterwards.
/// </summary>
public class PushSubscription
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public Guid PlayerId { get; set; }
    /// <summary>Which device this subscription belongs to, so a player who moves phones can be cleaned up.</summary>
    public string DeviceToken { get; set; } = "";
    /// <summary>Push service URL. Unique — it identifies the browser instance.</summary>
    public string Endpoint { get; set; } = "";
    /// <summary>Client public key (base64url), from the browser's subscription.</summary>
    public string P256dh { get; set; } = "";
    /// <summary>Client auth secret (base64url), from the browser's subscription.</summary>
    public string Auth { get; set; } = "";
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>Set when the push service last rejected this subscription as gone (404/410), so a dead row
    /// is dropped rather than retried forever.</summary>
    public DateTimeOffset? FailedAtUtc { get; set; }
}

public class ObjectiveScore
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionObjectiveId { get; set; }
    public SessionObjective? SessionObjective { get; set; }
    public Guid PlayerId { get; set; }
    public DateTimeOffset ScoredAtUtc { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>Game round this was scored in, so the wall can glow what was scored *this* round. Stored
    /// rather than derived from <see cref="ScoredAtUtc"/> — the round boundary only exists in the match
    /// log, and the display should not have to diff a timeline to draw a highlight.</summary>
    public int Round { get; set; }
}

/// <summary>
/// Permanent record of a finished game. Deliberately **not** related to <see cref="GameSession"/> by a
/// foreign key: the session itself is auto-wiped after its retention window, and this row has to outlive
/// it so long-term statistics stay possible. It holds aggregates only — never the individual steps, so it
/// is not a copy of the match log.
/// <para>
/// No IP addresses and no device identifiers are stored. A MAC address cannot be collected at all (it
/// never leaves the visitor's own network segment); <see cref="DeviceCount"/> counts distinct device
/// tokens, which answers "how many devices were at the table" without keeping the tokens themselves.
/// </para>
/// </summary>
public class SessionSummary
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Id of the session this summarises. Unique, so a re-record updates instead of duplicating.</summary>
    public Guid SessionId { get; set; }
    public string JoinCode { get; set; } = "";
    public string Name { get; set; } = "";

    public DateTimeOffset CreatedAtUtc { get; set; }
    /// <summary>When the game itself began (first phase change), or null if it never started.</summary>
    public DateTimeOffset? StartedAtUtc { get; set; }
    public DateTimeOffset LastActivityUtc { get; set; }
    /// <summary>Net play time: start → last activity, with paused intervals removed.</summary>
    public int DurationSeconds { get; set; }
    /// <summary>How much of that span the game spent paused (already excluded from the duration).</summary>
    public int PausedSeconds { get; set; }

    public int RoundsReached { get; set; }
    public GamePhase EndPhase { get; set; }
    public int PlayerCount { get; set; }
    /// <summary>Distinct device tokens seen on this session — the closest honest answer to "how many devices".</summary>
    public int DeviceCount { get; set; }
    public int ObjectivesRevealed { get; set; }

    public Expansion ActiveExpansions { get; set; }
    public Language DefaultLanguage { get; set; }
    public int TurnTimerSeconds { get; set; }
    public int StrategyCardsPerPlayer { get; set; }
    /// <summary>Which Red Tape variant the table played (0 = none). Was a bool while there was only one.</summary>
    public RedTapeVariant RedTapeVariant { get; set; }

    /// <summary>Highest score; null when nobody scored or the top score is shared.</summary>
    public string? WinnerName { get; set; }
    public string? WinnerFactionId { get; set; }
    public int TopPoints { get; set; }

    public DateTimeOffset RecordedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public List<SessionSummaryPlayer> Players { get; set; } = new();
}

/// <summary>One player's outcome inside a <see cref="SessionSummary"/>.</summary>
public class SessionSummaryPlayer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionSummaryId { get; set; }
    public SessionSummary? Summary { get; set; }
    public string Name { get; set; } = "";
    public string? FactionId { get; set; }
    public string ColorHex { get; set; } = "";
    public int SeatOrder { get; set; }
    public int Points { get; set; }
    public int TechnologyCount { get; set; }
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

/// <summary>
/// One match-log event. Structured (not pre-rendered prose) so the client can localize it; the
/// statistics view diffs the timeline kinds (<see cref="SessionLogKind.PhaseChange"/> /
/// <see cref="SessionLogKind.RoundChange"/> / <see cref="SessionLogKind.TurnChange"/>) to derive
/// durations. Kept out of the session graph (<c>WithGraph()</c>) and loaded only for the log view, so
/// it never bloats the per-mutation round-trips. Cascade-deleted with its session.
/// </summary>
public class SessionLogEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public DateTimeOffset TimestampUtc { get; set; } = DateTimeOffset.UtcNow;
    public SessionLogKind Kind { get; set; }
    /// <summary>Player who performed the action (resolved from the caller's device token); null for
    /// server/system events or spectator-triggered ones.</summary>
    public Guid? ActorPlayerId { get; set; }
    /// <summary>Player the action targeted (e.g. the new active player, the scorer, the voter).</summary>
    public Guid? TargetPlayerId { get; set; }
    /// <summary>Phase context (set for <see cref="SessionLogKind.PhaseChange"/>).</summary>
    public GamePhase? Phase { get; set; }
    /// <summary>Round context.</summary>
    public int? Round { get; set; }
    /// <summary>Free-form detail: an id (card/objective/agenda/tech) or a small encoded value.</summary>
    public string? Detail { get; set; }
}
