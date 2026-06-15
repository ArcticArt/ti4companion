namespace Ti4Companion.Shared;

// ---------------------------------------------------------------------------
// Content DTOs (carry both languages so the client can switch instantly).
// ---------------------------------------------------------------------------

public record FactionDto(
    string Id, string Name, string NameDe, Expansion Expansion,
    string ColorHex, int? InitiativeOverride, string? IconPath,
    IReadOnlyList<string> StartingTechnologies);

public record StrategyCardDto(
    int Id, string Name, string NameDe, int Initiative, string ColorHex,
    string PrimaryText, string PrimaryTextDe,
    string SecondaryText, string SecondaryTextDe, string Version);

public record ObjectiveDto(
    string Id, string Name, string NameDe, string Requirement, string RequirementDe,
    int Points, ObjectiveStage Stage, Expansion Expansion, bool IsSecret);

public record TechnologyDto(
    string Id, string Name, string NameDe, TechColor Color, string Prerequisites,
    string Text, string TextDe, Expansion Expansion, string? FactionId);

public record AgendaDto(
    string Id, string Name, string NameDe, AgendaType Type, string Elect,
    string Text, string TextDe, Expansion Expansion, bool RemovedInPok);

public record PlanetDto(
    string Id, string Name, string NameDe, PlanetTrait Trait,
    int Resources, int Influence, string? HomeFactionId, bool Legendary, Expansion Expansion);

public record ContentBundleDto(
    IReadOnlyList<FactionDto> Factions,
    IReadOnlyList<StrategyCardDto> StrategyCards,
    IReadOnlyList<ObjectiveDto> Objectives,
    IReadOnlyList<TechnologyDto> Technologies,
    IReadOnlyList<AgendaDto> Agendas,
    IReadOnlyList<PlanetDto> Planets);

// ---------------------------------------------------------------------------
// Session / runtime state DTOs.
// ---------------------------------------------------------------------------

public record PlayerStrategyCardDto(int StrategyCardId, bool IsExhausted);

public record PlayerDto(
    Guid Id, string Name, string? FactionId, string ColorHex, int SeatOrder,
    bool HasPassed, bool IsReady, bool IsHost, int? Initiative,
    IReadOnlyList<PlayerStrategyCardDto> StrategyCards,
    IReadOnlyList<string> TechnologyIds);

/// <summary><paramref name="CustomName"/>/<paramref name="CustomPoints"/> are set for an objective added
/// by hand (e.g. a secret made public via "Classified Document Leaks") rather than from the content set.</summary>
public record SessionObjectiveDto(
    Guid Id, string ObjectiveId, IReadOnlyList<Guid> ScoredByPlayerIds,
    string? CustomName, int? CustomPoints);

public record StrategyCardStateDto(int StrategyCardId, int TradeGoods);

/// <summary><paramref name="Choice"/> is the elected candidate key for elect agendas
/// (player id / planet id / strategy-card number / law id / free text), null for For/Against.
/// <paramref name="Locked"/> = secret vote committed (can't be reopened until reset).</summary>
public record AgendaVoteDto(Guid PlayerId, VoteOutcome Outcome, int Votes, string? Choice, bool Locked);

public record SessionStateDto(
    Guid Id, string JoinCode, string Name,
    Language DefaultLanguage, Expansion ActiveExpansions,
    int CurrentRound, GamePhase Phase,
    Guid? SpeakerPlayerId, Guid? ActivePlayerId, int? ActiveStrategyCardId,
    string? CurrentAgendaId, bool AllowEditAllPlayers,
    bool ShowTechOverview, DisplayMode DisplayMode, bool AgendaVotesHidden, int RetentionHours,
    DateTimeOffset CreatedAtUtc, DateTimeOffset LastActivityUtc,
    IReadOnlyList<PlayerDto> Players,
    IReadOnlyList<SessionObjectiveDto> Objectives,
    IReadOnlyList<StrategyCardStateDto> StrategyCardStates,
    IReadOnlyList<AgendaVoteDto> AgendaVotes);

/// <summary>Returned when creating or joining a session: the state plus this device's identity.</summary>
public record JoinResultDto(SessionStateDto Session, Guid PlayerId, string DeviceToken);

// ---------------------------------------------------------------------------
// Request bodies.
// ---------------------------------------------------------------------------

public record CreateSessionRequest(
    string Name, Language Language, Expansion? ActiveExpansions,
    string HostName, string? FactionId, string? ColorHex, string? DeviceToken);

public record JoinSessionRequest(string Name, string? FactionId, string? ColorHex, string? DeviceToken);

public record UpdateSessionRequest(
    string? Name, Language? Language, Expansion? ActiveExpansions,
    bool? ShowTechOverview, bool? AllowEditAllPlayers,
    GamePhase? Phase, int? CurrentRound, Guid? SpeakerPlayerId,
    bool? AgendaVotesHidden = null);

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

public record CastVoteRequest(Guid PlayerId, VoteOutcome Outcome, int Votes, string? Choice);

/// <summary>Commit a secret vote: sets the vote and locks it in one atomic step, so the choice is
/// only transmitted on lock (nobody — not even the host — sees it beforehand).</summary>
public record LockVoteRequest(Guid PlayerId, VoteOutcome Outcome, int Votes, string? Choice);
