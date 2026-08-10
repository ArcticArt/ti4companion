namespace Ti4Companion.Shared;

// ---------------------------------------------------------------------------
// Content DTOs (carry both languages so the client can switch instantly).
// ---------------------------------------------------------------------------

// Every content DTO carries the coarse Expansion flag (for the client's active-expansion filtering)
// PLUS the granular provenance: Version (counts up across reprints) and Source (Base/PoK/Codex I-IV/TE).
// The API serves the newest revision per logical id, so the client still sees one row per item.

public record FactionDto(
    string Id, string Name, string NameDe, Expansion Expansion,
    string ColorHex, int? InitiativeOverride, string? IconPath,
    IReadOnlyList<string> StartingTechnologies, IReadOnlyList<PlayerColor> PreferredColors,
    FactionComplexity Complexity, int Commodities, string FlavorText, string FlavorTextDe,
    int Version = 1, ContentSource Source = ContentSource.Base);

/// <summary><paramref name="RevisionLabel"/> is the printed Ω marking, if any ("Ω" Codex, "ΩΩ" Thunder's
/// Edge), empty for the original printing.</summary>
public record StrategyCardDto(
    int Id, string Name, string NameDe, int Initiative, string ColorHex,
    string PrimaryText, string PrimaryTextDe,
    string SecondaryText, string SecondaryTextDe,
    string RevisionLabel = "", int Version = 1, ContentSource Source = ContentSource.Base);

/// <summary><paramref name="Stage"/> is <c>StageI</c>/<c>StageII</c> for public objectives or
/// <c>Secret</c> for secret ones (the old IsSecret flag is folded in). <paramref name="Phase"/> is the
/// scoring phase — usually <c>Status</c>, but a secret may score in another phase.</summary>
public record ObjectiveDto(
    string Id, string Name, string NameDe, string Requirement, string RequirementDe,
    int Points, ObjectiveStage Stage, GamePhase Phase, Expansion Expansion,
    int Version = 1, ContentSource Source = ContentSource.Base);

/// <summary>A technology. For unit-upgrade techs (<see cref="TechColor.Unit"/>) the stat boxes and ★
/// keyword <paramref name="Abilities"/> are structured (not parsed from <paramref name="Text"/>), so
/// <paramref name="Text"/>/<paramref name="TextDe"/> hold only the bilingual prose. Non-unit techs leave
/// the stats null and carry their full effect in <paramref name="Text"/>.</summary>
public record TechnologyDto(
    string Id, string Name, string NameDe, TechColor Color, string Prerequisites,
    string Text, string TextDe, Expansion Expansion, string? FactionId, UnitType UnitType,
    int? Cost, int ProducedCount, int? Combat, int CombatDice, int? Move, int? Capacity,
    IReadOnlyList<UnitAbilityDto> Abilities,
    int Version = 1, ContentSource Source = ContentSource.Base);

/// <summary>One atomic unit ability (a ★ keyword bullet on a unit / unit-upgrade card). The keyword name
/// is localized at render time from <see cref="UnitAbility"/>; <paramref name="Value"/> is the printed
/// value ("5" for BOMBARDMENT 5, "X" for PRODUCTION X; null for valueless keywords like SUSTAIN DAMAGE)
/// and <paramref name="Dice"/> is the "(xN)" multiplier (ANTI-FIGHTER BARRAGE 6(x3) → 3), else 1.</summary>
public record UnitAbilityDto(UnitAbility Ability, string? Value, int Dice = 1);

public record AgendaDto(
    string Id, string Name, string NameDe, AgendaType Type, string Elect,
    string Text, string TextDe, Expansion Expansion, bool RemovedInPok,
    int Version = 1, ContentSource Source = ContentSource.Base);

/// <summary>Planet names aren't translated (no NameDe). <paramref name="Trait2"/> is the second trait of a
/// dual-trait planet (or None); <paramref name="TechSkip1"/>/<paramref name="TechSkip2"/> are tech-specialty
/// colours (TE planets can have two); <paramref name="IsStation"/> marks a TE space station;
/// <paramref name="SystemTileId"/> references the system tile by its <c>TileNumber</c> (a string, e.g. "82A").</summary>
public record PlanetDto(
    string Id, string Name, PlanetTrait Trait, PlanetTrait Trait2,
    int Resources, int Influence, TechColor? TechSkip1, TechColor? TechSkip2,
    string? HomeFactionId, bool Legendary, string LegendaryEffect, string LegendaryEffectDe,
    bool IsStation, bool GrantsRelic, string? SystemTileId,
    string FlavorText, string FlavorTextDe, Expansion Expansion,
    int Version = 1, ContentSource Source = ContentSource.Base);

/// <summary>A buildable unit at its base level (standard units + faction "Stufe I" units, flagships and
/// mechs). Stats are structured for a future production planner; the level-II upgrades live as
/// <see cref="TechnologyDto"/> unit-colour techs. <paramref name="ProducedCount"/> is how many units one
/// build yields (2 for fighters/infantry, else 1); <paramref name="Cost"/> is the printed card cost for
/// that build (null for structures placed via Construction). <paramref name="Combat"/>/<paramref name="CombatDice"/>
/// split a "5(x2)" combat value; <paramref name="Abilities"/> is the atomic ★ keyword list (e.g.
/// SUSTAIN DAMAGE, BOMBARDMENT 5). Null Move/Capacity = not applicable (ground forces, structures).</summary>
public record UnitDto(
    string Id, string Name, string NameDe, UnitType UnitType, string? FactionId,
    int? Cost, int ProducedCount, int? Combat, int CombatDice, int? Move, int? Capacity,
    string Text, string TextDe, IReadOnlyList<UnitAbilityDto> Abilities, Expansion Expansion,
    int Version = 1, ContentSource Source = ContentSource.Base);

/// <summary>A named faction ability (e.g. Sol's "Orbital Drop"); a faction has one or more, ordered.</summary>
public record FactionAbilityDto(
    string Id, string FactionId, string Name, string NameDe, string Text, string TextDe, int Order,
    Expansion Expansion, int Version = 1, ContentSource Source = ContentSource.Base);

/// <summary>A Prophecy of Kings faction leader. <paramref name="UnlockCondition"/> is empty for Agents
/// (always unlocked); <paramref name="Subtitle"/> is the epithet shown under the name; <paramref
/// name="FlavorText"/> is the lore blurb.</summary>
public record LeaderDto(
    string Id, string FactionId, LeaderType LeaderType, string Name, string NameDe,
    string Subtitle, string SubtitleDe,
    string Text, string TextDe, string UnlockCondition, string UnlockConditionDe,
    string FlavorText, string FlavorTextDe,
    Expansion Expansion, int Version = 1, ContentSource Source = ContentSource.Base);

/// <summary>A Thunder's Edge faction Breakthrough (one per faction, TE-only so no version/source).
/// <paramref name="ConnectedColor1"/>/<paramref name="ConnectedColor2"/> are the two tech colours it
/// connects — both null for the Nekro Virus breakthrough.</summary>
public record BreakthroughDto(
    string Id, string FactionId, string Name, string NameDe, string Text, string TextDe,
    TechColor? ConnectedColor1, TechColor? ConnectedColor2);

/// <summary>A bilingual label for one value of a content enum (UnitType, TechColor, …) so the client can
/// show localized type names.</summary>
public record TypeValueDto(string Type, int Value, string Name, string NameDe);

/// <summary>A promissory note (faction-specific, or generic when <paramref name="FactionId"/> is null).</summary>
public record PromissoryNoteDto(
    string Id, string? FactionId, string Name, string NameDe, string Text, string TextDe,
    Expansion Expansion, int Version = 1, ContentSource Source = ContentSource.Base);

/// <summary>An action card.</summary>
public record ActionCardDto(
    string Id, string Name, string NameDe, string Text, string TextDe, string FlavorText, string FlavorTextDe,
    Expansion Expansion, int Version = 1, ContentSource Source = ContentSource.Base);

/// <summary>An exploration card. <paramref name="Deck"/> = Cultural / Hazardous / Industrial / Frontier.</summary>
public record ExplorationDto(
    string Id, string Deck, string Name, string NameDe, string Text, string TextDe,
    Expansion Expansion, int Version = 1, ContentSource Source = ContentSource.Base);

/// <summary>A relic.</summary>
public record RelicDto(
    string Id, string Name, string NameDe, string Text, string TextDe, string FlavorText, string FlavorTextDe,
    Expansion Expansion, int Version = 1, ContentSource Source = ContentSource.Base);

/// <summary>A Thunder's Edge Galactic Event.</summary>
public record GalacticEventDto(
    string Id, string Name, string NameDe, string Text, string TextDe,
    Expansion Expansion, int Version = 1, ContentSource Source = ContentSource.Base);

/// <summary>A faction-specific extra card (e.g. the Nekro Valefar Assimilators, or TE faction components).</summary>
public record FactionCardDto(
    string Id, string FactionId, string Name, string NameDe, string Text, string TextDe,
    Expansion Expansion, int Version = 1, ContentSource Source = ContentSource.Base);

/// <summary>One line of a faction's starting fleet: how many of a unit (by unit slug) it begins with.</summary>
public record FactionStartingUnitDto(string FactionId, string UnitId, int Count);

/// <summary>A system tile ("Systemtafel"). <paramref name="Id"/> is the printed tile number as a string
/// (e.g. "01", "82A", "125") — the double-sided / multi-system tiles use letters. Carries the tile's
/// anomaly (<paramref name="IsAnomaly"/> / <paramref name="Anomalies"/>), wormhole (<paramref name="Wormholes"/>)
/// and home-system (<paramref name="IsHomeSystem"/> / <paramref name="HomeFactionId"/>) info.</summary>
public record SystemTileDto(
    string Id, int SortOrder, SystemTileColor Color, bool IsHomeSystem, string? HomeFactionId,
    bool IsAnomaly, AnomalyType Anomalies, WormholeType Wormholes, bool IsHyperlane, bool IsFracture,
    string Description, string Planets, Expansion Expansion, ContentSource Source);

public record ContentBundleDto(
    IReadOnlyList<FactionDto> Factions,
    IReadOnlyList<StrategyCardDto> StrategyCards,
    IReadOnlyList<ObjectiveDto> Objectives,
    IReadOnlyList<TechnologyDto> Technologies,
    IReadOnlyList<AgendaDto> Agendas,
    IReadOnlyList<PlanetDto> Planets,
    IReadOnlyList<UnitDto> Units,
    IReadOnlyList<FactionAbilityDto> FactionAbilities,
    IReadOnlyList<LeaderDto> Leaders,
    IReadOnlyList<BreakthroughDto> Breakthroughs,
    IReadOnlyList<FactionStartingUnitDto> StartingUnits,
    IReadOnlyList<TypeValueDto> TypeValues,
    IReadOnlyList<PromissoryNoteDto> PromissoryNotes,
    IReadOnlyList<ActionCardDto> ActionCards,
    IReadOnlyList<ExplorationDto> Explorations,
    IReadOnlyList<RelicDto> Relics,
    IReadOnlyList<GalacticEventDto> GalacticEvents,
    IReadOnlyList<FactionCardDto> FactionCards,
    IReadOnlyList<SystemTileDto> SystemTiles);

// ---------------------------------------------------------------------------
// Session / runtime state DTOs.
// ---------------------------------------------------------------------------

public record PlayerStrategyCardDto(int StrategyCardId, bool IsExhausted);

public record PlayerDto(
    Guid Id, string Name, string? FactionId, string ColorHex, int SeatOrder,
    bool HasPassed, bool IsReady, bool IsHost, int? Initiative,
    IReadOnlyList<PlayerStrategyCardDto> StrategyCards,
    IReadOnlyList<string> TechnologyIds,
    int Influence,
    /// <summary>Status phase: done scoring, so the turn has moved on.</summary>
    bool StatusDone = false);

/// <summary><paramref name="CustomName"/>/<paramref name="CustomPoints"/> are set for an objective added
/// by hand (e.g. a secret made public via "Classified Document Leaks") rather than from the content set.</summary>
public record SessionObjectiveDto(
    Guid Id, string ObjectiveId, IReadOnlyList<Guid> ScoredByPlayerIds,
    string? CustomName, int? CustomPoints,
    /// <summary>Red Tape variant: the marker on this objective has been taken off.</summary>
    bool MarkerRemoved = false,
    /// <summary>Who scored this in the CURRENT round — the wall glows those. Computed server-side so the
    /// client doesn't reimplement "this round".</summary>
    IReadOnlyList<Guid>? ScoredThisRoundPlayerIds = null);

public record StrategyCardStateDto(int StrategyCardId, int TradeGoods);

/// <summary><paramref name="Choice"/> is the elected candidate key for elect agendas
/// (player id / planet id / strategy-card number / law id / free text), null for For/Against.
/// <paramref name="Locked"/> = secret vote committed (can't be reopened until reset).</summary>
public record AgendaVoteDto(Guid PlayerId, VoteOutcome Outcome, int Votes, string? Choice, bool Locked);

/// <summary>Votes per elected candidate (player id / planet id / card number / law id / free text).</summary>
public record AgendaChoiceTallyDto(string Choice, int Votes);

/// <summary>
/// Aggregate result of the current vote, WITHOUT attribution — the intermediate step of a face-down vote
/// (Galactic Event / hidden agenda), where the table sees the totals before it sees who voted how. The
/// server computes it precisely because the per-player rows stay redacted at that point.
/// </summary>
public record AgendaTotalsDto(int For, int Against, int Abstained, IReadOnlyList<AgendaChoiceTallyDto> Choices);

public record SessionStateDto(
    Guid Id, string JoinCode, string Name,
    Language DefaultLanguage, Expansion ActiveExpansions,
    int CurrentRound, GamePhase Phase,
    Guid? SpeakerPlayerId, Guid? ActivePlayerId, int? ActiveStrategyCardId,
    string? CurrentAgendaId, bool AllowEditAllPlayers,
    bool ShowTechOverview, DisplayMode DisplayMode, bool AgendaVotesHidden, bool VotingStarted, bool Paused, int RetentionHours,
    int TurnTimerSeconds, int StrategyCardsPerPlayer, bool RedTapeLite, bool PromptTechOnAction,
    DateTimeOffset CreatedAtUtc, DateTimeOffset LastActivityUtc,
    IReadOnlyList<PlayerDto> Players,
    IReadOnlyList<SessionObjectiveDto> Objectives,
    IReadOnlyList<StrategyCardStateDto> StrategyCardStates,
    IReadOnlyList<AgendaVoteDto> AgendaVotes,
    /// <summary>Face-down vote: totals are public, attribution is not (see <see cref="AgendaTotals"/>).</summary>
    bool AgendaTotalsRevealed = false,
    /// <summary>Set once the totals are public — while the votes themselves are still redacted.</summary>
    AgendaTotalsDto? AgendaTotals = null,
    /// <summary>Status phase: post-scoring steps the table has ticked off.</summary>
    StatusStep StatusStepsDone = StatusStep.None,
    /// <summary>Status phase: whose turn it is to score (initiative order), null when everyone is done.
    /// Derived server-side so the client can't drift from the rule the server enforces.</summary>
    Guid? StatusScorerId = null,
    /// <summary>Wall display: show the join QR code. On during setup, off once the game starts.</summary>
    bool ShowJoinQr = true,
    /// <summary>Status phase: which of the three stages the table is on (score → reveal → checklist).</summary>
    StatusStage StatusStage = StatusStage.Scoring);

/// <summary>A single match-log event. Structured (not pre-rendered) so the client localizes it and
/// the statistics view diffs the timeline kinds for durations. See <see cref="SessionLogKind"/>.</summary>
public record SessionLogEntryDto(
    Guid Id, DateTimeOffset TimestampUtc, SessionLogKind Kind,
    Guid? ActorPlayerId, Guid? TargetPlayerId, GamePhase? Phase, int? Round, string? Detail);

/// <summary>Returned when creating or joining a session: the state plus this device's identity.</summary>
public record JoinResultDto(SessionStateDto Session, Guid PlayerId, string DeviceToken);

// ---------------------------------------------------------------------------
// Request bodies.
// ---------------------------------------------------------------------------

public record CreateSessionRequest(
    string Name, Language Language, Expansion? ActiveExpansions,
    string HostName, string? FactionId, string? ColorHex, string? DeviceToken);

/// <summary>Join a session. With <paramref name="ClaimPlayerId"/> set, take over (claim) that existing
/// non-host seat instead of creating a new player; otherwise a new player is added (capped at 8).</summary>
public record JoinSessionRequest(string Name, string? FactionId, string? ColorHex, string? DeviceToken,
    Guid? ClaimPlayerId = null);

public record UpdateSessionRequest(
    string? Name, Language? Language, Expansion? ActiveExpansions,
    bool? ShowTechOverview, bool? AllowEditAllPlayers,
    GamePhase? Phase, int? CurrentRound, Guid? SpeakerPlayerId,
    bool? AgendaVotesHidden = null,
    /// <summary>Per-player time budget per round in seconds; 0 turns the turn timer off.</summary>
    int? TurnTimerSeconds = null,
    /// <summary>Strategy cards per player: 0 = automatic, 1 or 2 to pin it.</summary>
    int? StrategyCardsPerPlayer = null,
    /// <summary>Red Tape variant: removable marker on every revealed objective.</summary>
    bool? RedTapeLite = null,
    /// <summary>Offer a technology entry after the Technology strategy action.</summary>
    bool? PromptTechOnAction = null,
    /// <summary>Show the join QR code on the wall display (shared state — the wall is shared).</summary>
    bool? ShowJoinQr = null);

/// <summary>Red Tape variant: take the marker off an objective (or put it back).</summary>
public record SetObjectiveMarkerRequest(bool Removed);

/// <summary>Seat order as one list, in table order. Assigning it in a single call keeps the order
/// consistent — reordering player by player would leave duplicate seats visible in between.</summary>
public record SetSeatOrderRequest(IReadOnlyList<Guid> PlayerIds);

/// <summary>Status phase: this player is done scoring (or wants their turn back).</summary>
public record SetStatusDoneRequest(bool Done);

/// <summary>Status phase: tick one of the shared post-scoring steps off (or back on).</summary>
public record SetStatusStepRequest(StatusStep Step, bool Done);

/// <summary>Move the status phase to a stage. Absolute, not "next": a double tap must not skip a stage.</summary>
public record SetStatusStageRequest(StatusStage Stage);

public record SetDisplayModeRequest(DisplayMode Mode);

public record RevealCustomObjectiveRequest(string Name, int Points);

public record UpdatePlayerRequest(
    string? Name, string? FactionId, string? ColorHex, bool? HasPassed, bool? IsReady, int? SeatOrder);

public record AssignStrategyCardRequest(int StrategyCardId);

public record SetStrategyCardUsedRequest(bool Used);

/// <summary>Set or clear the strategy action currently being resolved (drives the display emphasis).</summary>
public record SetActiveStrategyCardRequest(int? StrategyCardId);

public record SetActivePlayerRequest(Guid? PlayerId);

public record SetPassedRequest(bool Passed);

public record RevealObjectiveRequest(string ObjectiveId);

public record ScoreObjectiveRequest(Guid PlayerId);

public record AddTechnologyRequest(string TechnologyId);

public record SetAgendaRequest(string? AgendaId);

/// <summary>A player's available influence for the agenda phase (entered before voting starts). Not a
/// cap on votes — action cards/abilities can exceed it; it's tracked only for display and the
/// auto-deduction when the next agenda is revealed.</summary>
public record SetInfluenceRequest(Guid PlayerId, int Influence);

/// <summary>Host starts the vote on the revealed agenda, open or face-down (<paramref name="Hidden"/>).</summary>
public record StartVotingRequest(bool Hidden);

/// <summary>Commit a vote: sets the vote and locks it in one atomic step. The choice is only
/// transmitted on lock, so in a face-down vote nobody — not even the host — sees it beforehand.
/// Used for both open and hidden voting (a vote counts only once locked).</summary>
public record LockVoteRequest(Guid PlayerId, VoteOutcome Outcome, int Votes, string? Choice);
