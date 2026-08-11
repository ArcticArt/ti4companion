using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Ti4Companion.Shared;
using Ti4Companion.Web.Localization;

namespace Ti4Companion.Web.Services;

/// <summary>
/// Holds the shared session state and this device's identity, keeps a SignalR connection alive
/// (with auto-reconnect), and persists enough to resume a session across reloads.
/// </summary>
public class SessionStore(Ti4ApiClient api, BrowserStorage storage, Loc loc, NavigationManager nav) : IAsyncDisposable
{
    private const string KeyDevice = "ti4.device";
    private const string KeyCode = "ti4.code";      // legacy single session; only read now, for the carry-over
    private const string KeyPlayer = "ti4.player";  // legacy, see RecentAsync
    private const string KeyRecent = "ti4.recent";
    private const string KeyLang = "ti4.lang";
    private const string KeySenate = "ti4.senate";

    /// <summary>How many sessions the start page offers to pick up again.</summary>
    public const int MaxRecent = 10;

    private HubConnection? _hub;
    private string? _joinedCode;
    private bool _langPersistHooked;

    public Ti4ApiClient Api => api;
    public ContentBundleDto? Content { get; private set; }
    public SessionStateDto? Session { get; private set; }
    public Guid? MyPlayerId { get; private set; }
    public string? DeviceToken { get; private set; }
    public bool Connected => _hub?.State == HubConnectionState.Connected;

    public event Action? OnChange;

    /// <summary>
    /// Whether the start page draws the senate chamber behind itself. Per device and remembered (localStorage
    /// `ti4.senate`), because it is decoration: it costs about 130 elements and 24 photographs, and on a weak
    /// phone or a slow connection someone may simply not want it. Defaults to ON.
    /// </summary>
    public bool SenateEnabled { get; private set; } = true;

    public async Task SetSenateAsync(bool on)
    {
        if (SenateEnabled == on) return;
        SenateEnabled = on;
        await storage.SetAsync(KeySenate, on ? "1" : "0");
        OnChange?.Invoke();
    }

    public PlayerDto? Me => Session?.Players.FirstOrDefault(p => p.Id == MyPlayerId);

    // --- turn timer -------------------------------------------------------------------------------
    // The countdown is derived from the match log (which already carries every turn change and pause),
    // so the log is cached here and refreshed with the session — but ONLY while the option is on, so a
    // table that doesn't use the timer pays nothing.

    /// <summary>Host enabled a per-player time budget per round.</summary>
    public bool TurnTimerEnabled => (Session?.TurnTimerSeconds ?? 0) > 0;

    /// <summary>Cached match log, kept fresh while <see cref="TurnTimerEnabled"/>.</summary>
    public IReadOnlyList<SessionLogEntryDto> Log { get; private set; } = Array.Empty<SessionLogEntryDto>();

    /// <summary>Fires once a second while at least one timer component is on screen.</summary>
    private event Action? OnTick;
    private System.Threading.Timer? _tick;
    private int _tickSubscribers;

    /// <summary>Subscribe to the 1 Hz tick. The ticker only runs while something is subscribed, and the
    /// tick is deliberately separate from <see cref="OnChange"/> — re-rendering whole views every second
    /// would re-run the wall display's expensive card-fitting pass.</summary>
    public void SubscribeTick(Action handler)
    {
        OnTick += handler;
        if (++_tickSubscribers == 1)
            _tick = new System.Threading.Timer(_ => OnTick?.Invoke(), null, 1000, 1000);
    }

    public void UnsubscribeTick(Action handler)
    {
        OnTick -= handler;
        if (--_tickSubscribers <= 0)
        {
            _tickSubscribers = 0;
            _tick?.Dispose();
            _tick = null;
        }
    }

    /// <summary>Time each player has spent on turn in the current round, plus the live open segment.</summary>
    public MatchStats TimerStats() => MatchStats.Compute(Log, DateTimeOffset.UtcNow, CurrentPickerId);

    /// <summary>Remaining budget for a player this round; null when the timer is off.</summary>
    public TimeSpan? RemainingFor(MatchStats stats, Guid playerId)
    {
        if (Session is not { TurnTimerSeconds: > 0 } s) return null;
        var used = stats.PerPlayerRound.TryGetValue(playerId, out var u) ? u : TimeSpan.Zero;
        return TimeSpan.FromSeconds(s.TurnTimerSeconds) - used;
    }

    private async Task RefreshLogAsync()
    {
        if (Session is null || !TurnTimerEnabled) { Log = Array.Empty<SessionLogEntryDto>(); return; }
        Log = await api.GetLogAsync(Session.JoinCode);
    }

    /// <summary>A view asked the shell to open the technology tab (the optional prompt after playing the
    /// Technology strategy card). An event rather than a flag, so nothing has to be reset afterwards.</summary>
    public event Action? OnShowTechTab;

    public void ShowTechTab() => OnShowTechTab?.Invoke();

    /// <summary>Same idea for the objectives tab — the Imperial strategy action offers to score one.</summary>
    public event Action? OnShowObjectivesTab;

    public void ShowObjectivesTab() => OnShowObjectivesTab?.Invoke();

    // The action phase's "take over" toggle used to live here (so switching tabs wouldn't drop it). It is gone:
    // the host can always act for whoever is up — which is what the server allowed all along — and jumping to
    // another player is exactly what "previous turn"/"next turn" already do.

    /// <summary>This device asked to see the secondary-round popup although it is not addressed to it (the
    /// host opening it from the action view). Per device and not per component, because the popup lives in
    /// the shell while the button that opens it is in a tab.</summary>
    public bool ShowSecondary
    {
        get => _showSecondary;
        set { _showSecondary = value; OnChange?.Invoke(); }
    }
    private bool _showSecondary;

    /// <summary>This device controls the host player (the session creator).</summary>
    public bool IsHost => Me?.IsHost == true
        // Legacy sessions created before the host flag: fall back to lowest seat.
        || (Session is not null && MyPlayerId is not null && !Session.Players.Any(p => p.IsHost)
            && Session.Players.OrderBy(p => p.SeatOrder).FirstOrDefault()?.Id == MyPlayerId);

    /// <summary>Whose turn it is to pick a strategy card (speaker first, then clockwise by seat), or null when done.</summary>
    public Guid? CurrentPickerId
    {
        get
        {
            var s = Session;
            if (s is null || s.Players.Count == 0) return null;
            var seated = s.Players.OrderBy(p => p.SeatOrder).ToList();
            var start = s.SpeakerPlayerId is Guid sp ? seated.FindIndex(p => p.Id == sp) : 0;
            if (start < 0) start = 0;
            var order = Enumerable.Range(0, seated.Count).Select(i => seated[(start + i) % seated.Count]).ToList();
            var taken = order.Sum(p => p.StrategyCards.Count);
            return taken >= order.Count * MaxStrategyCards ? null : order[taken % order.Count].Id;
        }
    }

    /// <summary>Status phase: whose turn it is to score (initiative order), from the server.</summary>
    public Guid? StatusScorerId => Session?.StatusScorerId;

    /// <summary>
    /// Whether this device may score for a player right now. Outside the status phase scoring stays open
    /// (abilities score at other times); inside it, it follows initiative order — mirrors the server gate
    /// so the UI never offers a button the server would reject.
    /// </summary>
    public bool CanScoreFor(Guid playerId)
    {
        if (Session is not { } s) return false;
        if (s.Phase != GamePhase.Status) return true;
        return IsHost || s.StatusScorerId == playerId;
    }

    /// <summary>Red Tape variant: an objective whose marker is still on cannot be scored. Mirrors the
    /// server gate exactly, so the UI disables what the server would refuse (a 403/400 is a silent no-op
    /// on this client, which is how a player ends up tapping a dead button).</summary>
    public bool RedTapeBlocks(SessionObjectiveDto so)
        => RedTapeOn && (!so.MarkerRemoved || so.Purged);

    /// <summary>A Red Tape variant is in play (either of them — the tape behaves the same in both).</summary>
    public bool RedTapeOn => Session is not null && Session.RedTapeVariant != RedTapeVariant.None;

    /// <summary>
    /// The localization KEY explaining why this tape may not be pulled right now, or null when it may — the
    /// same gates the server enforces (<c>RedTape.WhyCannotRemove</c>), so the UI never offers a tap the
    /// server would refuse. A key rather than a sentence, so the store needs no localizer: the stage comes
    /// from the content bundle, the wording from the component.
    /// </summary>
    public string? RedTapeBlockKey(SessionObjectiveDto so)
    {
        if (Session is not { } s || !RedTapeOn) return null;
        if (so.Purged) return "redtape.purged";
        if (Objective(so.ObjectiveId)?.Stage != ObjectiveStage.StageII) return null;
        if (s.RedTapeVariant == RedTapeVariant.Bureaucracy && s.CurrentRound <= GameRules.RedTapeStageIILockedThrough)
            return "redtape.lockedRounds";
        if (s.RedTapeVariant == RedTapeVariant.Lite && ClearStageI < GameRules.RedTapeScorableStageI)
            return "redtape.lockedStageI";
        return null;
    }

    /// <summary>Stage I objectives whose tape is off (purged ones never score, so they don't count).</summary>
    public int ClearStageI => Session is null ? 0
        : Session.Objectives.Count(o => Objective(o.ObjectiveId)?.Stage == ObjectiveStage.StageI
                                        && o.MarkerRemoved && !o.Purged);

    // --- Red Tape Lite's two questions ------------------------------------------------------------------
    // Neither the purge nor the random removal happens on its own any more: the server proposes and the HOST
    // answers (RedTapeModal). Both are irreversible and both change who can still win, which is why they are
    // questions and not events.

    /// <summary>Objectives the app is proposing to purge, waiting for the host's answer (empty = none). Only
    /// what was already on the table when the fifth Stage I came clear is ever in here.</summary>
    public IReadOnlyList<SessionObjectiveDto> RedTapePurgeProposal => Session is null
        ? Array.Empty<SessionObjectiveDto>()
        : Session.Objectives.Where(o => o.PurgePending).ToList();

    /// <summary>A random removal is being asked about (nobody took the carrier card this round).</summary>
    public bool RedTapeRandomAsking => Session?.RedTapeRandomPendingRound > 0;

    /// <summary>Whether this device may edit the given player: self always, the host may edit anyone,
    /// or anyone when the session has open editing enabled.</summary>
    public bool CanEdit(Guid playerId)
        => Session is not null && (IsHost || Session.AllowEditAllPlayers || playerId == MyPlayerId);

    /// <summary>Strategy cards each player takes this round — the printed rule unless the table pinned a
    /// count. Same helper the server enforces with, so the two can't drift.</summary>
    public int MaxStrategyCards =>
        GameRules.StrategyCardsPerPlayer(Session?.Players.Count ?? 0, Session?.StrategyCardsPerPlayer ?? 0);

    /// <summary>Argent Flight's faction slug — it always votes first in the agenda phase.</summary>
    public const string ArgentFactionId = "argent";

    /// <summary>Agenda voting order: seat order with the speaker last, but Argent Flight always first (Zeal).</summary>
    public IReadOnlyList<PlayerDto> AgendaOrder()
    {
        var s = Session;
        if (s is null || s.Players.Count == 0) return Array.Empty<PlayerDto>();
        var seated = s.Players.OrderBy(p => p.SeatOrder).ToList();
        var spIdx = s.SpeakerPlayerId is Guid sp ? seated.FindIndex(p => p.Id == sp) : -1;
        // Base: seat order, but start just after the speaker so the speaker ends up last (if one is set).
        var order = spIdx < 0
            ? seated
            : Enumerable.Range(1, seated.Count).Select(i => seated[(spIdx + i) % seated.Count]).ToList();
        // Argent Flight always votes first (Zeal), regardless of seat/speaker.
        var argent = order.FirstOrDefault(p => p.FactionId == ArgentFactionId);
        if (argent is not null) order = order.Where(p => p.Id != argent.Id).Prepend(argent).ToList();
        return order;
    }

    public async Task InitAsync()
    {
        DeviceToken = await storage.GetAsync(KeyDevice);
        if (string.IsNullOrEmpty(DeviceToken))
        {
            DeviceToken = Guid.NewGuid().ToString("N");
            await storage.SetAsync(KeyDevice, DeviceToken);
        }
        api.SetDeviceToken(DeviceToken); // identify this device so the server can enforce host rights

        var langStr = await storage.GetAsync(KeyLang);
        if (Enum.TryParse<Language>(langStr, out var lang)) loc.SetLanguage(lang);

        SenateEnabled = await storage.GetAsync(KeySenate) != "0";   // absent = on

        if (!_langPersistHooked)
        {
            loc.OnChange += async () => await storage.SetAsync(KeyLang, loc.Lang.ToString());
            _langPersistHooked = true;
        }

        Content ??= await api.GetContentAsync();
    }

    /// <summary>Load the reference content bundle if it isn't already, for views that show content
    /// outside a session (e.g. the production planner).</summary>
    public async Task EnsureContentAsync()
    {
        if (Content is not null) return;
        Content = await api.GetContentAsync();
        OnChange?.Invoke();
    }

    // --- remembered sessions ----------------------------------------------------------------------
    // A device plays more than one game over time, and a group often runs a session over several evenings,
    // so "the last code" was too little: the start page lists the recent ones and each carries the SEAT this
    // device held there (see RecentSession — the device token alone cannot express that).

    /// <summary>Sessions this device has been in, newest first — at most <see cref="MaxRecent"/>.</summary>
    public async Task<List<RecentSession>> RecentAsync()
    {
        var raw = await storage.GetAsync(KeyRecent);
        List<RecentSession>? list = null;
        if (!string.IsNullOrWhiteSpace(raw))
        {
            try { list = JsonSerializer.Deserialize<List<RecentSession>>(raw); } catch { /* unreadable → start over */ }
        }
        list ??= new List<RecentSession>();
        // Before this list existed, one session was remembered under two flat keys. Carry that one over, or
        // an update mid-game would look to the table like their session had vanished.
        if (list.Count == 0)
        {
            var code = await storage.GetAsync(KeyCode);
            var player = await storage.GetAsync(KeyPlayer);
            if (!string.IsNullOrWhiteSpace(code) && Guid.TryParse(player, out var legacy))
                list.Add(new RecentSession(code, "", "", legacy, DateTimeOffset.UtcNow));
        }
        return list.OrderByDescending(r => r.LastSeen).Take(MaxRecent).ToList();
    }

    /// <summary>Record (or refresh) this device's seat in a session, and drop the oldest beyond the cap.</summary>
    private async Task RememberAsync(SessionStateDto s, Guid playerId)
    {
        var list = await RecentAsync();
        list.RemoveAll(r => string.Equals(r.Code, s.JoinCode, StringComparison.OrdinalIgnoreCase));
        var name = s.Players.FirstOrDefault(p => p.Id == playerId)?.Name ?? "";
        list.Insert(0, new RecentSession(s.JoinCode, s.Name, name, playerId, DateTimeOffset.UtcNow));
        if (list.Count > MaxRecent) list.RemoveRange(MaxRecent, list.Count - MaxRecent);
        await storage.SetAsync(KeyRecent, JsonSerializer.Serialize(list));
    }

    /// <summary>Forget a session — it is gone from the server (archived, wiped) or the code was wrong.</summary>
    public async Task ForgetRecentAsync(string code)
    {
        var list = await RecentAsync();
        if (list.RemoveAll(r => string.Equals(r.Code, code, StringComparison.OrdinalIgnoreCase)) == 0) return;
        await storage.SetAsync(KeyRecent, JsonSerializer.Serialize(list));
    }

    public async Task<SessionStateDto?> CreateAsync(CreateSessionRequest req)
    {
        var result = await api.CreateSessionAsync(req with { DeviceToken = DeviceToken });
        if (result is null) return null;
        await AdoptAsync(result);
        return result.Session;
    }

    public async Task<SessionStateDto?> JoinAsync(string code, JoinSessionRequest req)
    {
        var existing = await api.GetSessionAsync(code);
        if (existing is null) return null;
        var result = await api.JoinSessionAsync(existing.Id, req with { DeviceToken = DeviceToken });
        if (result is null) return null;
        await AdoptAsync(result);
        return result.Session;
    }

    public async Task AddLocalPlayerAsync(string name)
    {
        if (Session is null) return;
        await api.JoinSessionAsync(Session.Id, new JoinSessionRequest(name, null, null, Guid.NewGuid().ToString("N")));
        await RefreshAsync();
    }

    /// <summary>Connect to a session for viewing/controlling without (re)joining as a new player.</summary>
    public async Task<bool> ConnectAsync(string code)
    {
        Content ??= await api.GetContentAsync();
        var s = await api.GetSessionAsync(code);
        // Gone from the server (archived, or wiped by the retention worker): drop it from the list here, so
        // stale entries clean themselves up instead of collecting as dead rows on the start page.
        if (s is null) { await ForgetRecentAsync(code); Session = null; OnChange?.Invoke(); return false; }

        Session = s;

        // Which seat this device holds HERE — looked up per session, so opening an older game gives you back
        // the player you were in it rather than making you a stranger.
        var remembered = (await RecentAsync())
            .FirstOrDefault(r => string.Equals(r.Code, s.JoinCode, StringComparison.OrdinalIgnoreCase));
        if (remembered is not null && s.Players.Any(p => p.Id == remembered.PlayerId))
        {
            MyPlayerId = remembered.PlayerId;
            await RememberAsync(s, remembered.PlayerId);   // refresh the names and move it to the front
        }

        ApplySessionLanguageIfUnset(s);
        await EnsureHubAsync(s.JoinCode);
        await RefreshLogAsync(); // the turn timer needs the log right away, not only after the first change
        OnChange?.Invoke();
        return true;
    }

    public async Task Mutate(Task<SessionStateDto?> apiCall)
    {
        var s = await apiCall;
        if (s is not null)
        {
            Session = s;
            await RefreshLogAsync();
            OnChange?.Invoke();
        }
        else await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        if (Session is null) return;
        Session = await api.GetSessionAsync(Session.JoinCode);
        await RefreshLogAsync();
        OnChange?.Invoke();
    }

    /// <summary>
    /// Leave the current session on this device: disconnect and drop the live state. The entry in the recent
    /// list is deliberately KEPT — leaving and coming back later is the whole point of that list (use
    /// <see cref="ForgetRecentAsync"/> for a session that is really gone).
    /// </summary>
    public async Task LeaveAsync()
    {
        if (_hub is not null)
        {
            try { if (_joinedCode is not null) await _hub.InvokeAsync("LeaveSession", _joinedCode); } catch { }
            await _hub.DisposeAsync();
            _hub = null;
        }
        _joinedCode = null;
        Session = null;
        MyPlayerId = null;
        Log = Array.Empty<SessionLogEntryDto>();
        await storage.RemoveAsync(KeyCode);
        await storage.RemoveAsync(KeyPlayer);
        OnChange?.Invoke();
    }

    private async Task AdoptAsync(JoinResultDto result)
    {
        Session = result.Session;
        MyPlayerId = result.PlayerId;
        DeviceToken = result.DeviceToken;
        api.SetDeviceToken(result.DeviceToken);
        await storage.SetAsync(KeyDevice, result.DeviceToken);
        await RememberAsync(result.Session, result.PlayerId);
        ApplySessionLanguageIfUnset(result.Session);
        await EnsureHubAsync(result.Session.JoinCode);
        await RefreshLogAsync();
        OnChange?.Invoke();
    }

    private async void ApplySessionLanguageIfUnset(SessionStateDto s)
    {
        var stored = await storage.GetAsync(KeyLang);
        if (string.IsNullOrEmpty(stored)) loc.SetLanguage(s.DefaultLanguage);
    }

    private async Task EnsureHubAsync(string code)
    {
        if (_hub is null)
        {
            _hub = new HubConnectionBuilder()
                .WithUrl(nav.ToAbsoluteUri("hubs/session"))
                .WithAutomaticReconnect()
                .Build();

            _hub.On(SignalREvents.SessionChanged, async () => await RefreshAsync());
            _hub.Reconnected += async _ =>
            {
                if (_joinedCode is not null)
                {
                    try { await _hub.InvokeAsync("JoinSession", _joinedCode); } catch { }
                }
                await RefreshAsync();
            };
        }

        if (_hub.State == HubConnectionState.Disconnected) await _hub.StartAsync();

        // Switch groups if we were watching a different session.
        if (_joinedCode is not null && _joinedCode != code)
        {
            try { await _hub.InvokeAsync("LeaveSession", _joinedCode); } catch { }
        }
        try { await _hub.InvokeAsync("JoinSession", code); } catch { }
        _joinedCode = code;
    }

    // ---- Content lookups ----
    private Expansion Active => Session?.ActiveExpansions ?? Expansion.Base;

    public PlayerDto? PlayerById(Guid? id) => id is null ? null : Session?.Players.FirstOrDefault(p => p.Id == id);

    /// <summary>Which stage of the agenda phase we're in, derived from the session flags. Drives the
    /// agenda control UI (influence entry → agenda revealed → open/face-down voting).</summary>
    public AgendaStage CurrentAgendaStage
    {
        get
        {
            var s = Session;
            // A free vote (no agenda card) counts as "something is on the table" just like an agenda.
            if (s is null || (s.CurrentAgendaId is null && string.IsNullOrEmpty(s.CustomVoteTitle)))
                return AgendaStage.Influence;
            if (!s.VotingStarted) return AgendaStage.AgendaRevealed;
            if (!s.AgendaVotesHidden) return AgendaStage.VotingOpen;
            return s.AgendaTotalsRevealed ? AgendaStage.VotingHiddenTotals : AgendaStage.VotingHidden;
        }
    }

    /// <summary>True while a free vote (no agenda card) is on the table.</summary>
    public bool CustomVoteActive => !string.IsNullOrEmpty(Session?.CustomVoteTitle);

    /// <summary>What the thing on the table elects — from the agenda, or from the free vote. One place, so
    /// the control view and the wall can never disagree about which pickers to show.</summary>
    public ElectType AgendaElectKind
    {
        get
        {
            if (Session is not { } s) return ElectType.ForAgainst;
            if (!string.IsNullOrEmpty(s.CustomVoteTitle)) return s.CustomVoteElect ?? ElectType.ForAgainst;
            return Agenda(s.CurrentAgendaId) is { } a ? AgendaDisplay.ElectKind(a) : ElectType.ForAgainst;
        }
    }

    public FactionDto? Faction(string? id) => id is null ? null : Content?.Factions.FirstOrDefault(f => f.Id == id);
    public StrategyCardDto? Card(int id) => Content?.StrategyCards.FirstOrDefault(c => c.Id == id);
    public ObjectiveDto? Objective(string id) => Content?.Objectives.FirstOrDefault(o => o.Id == id);
    public TechnologyDto? Tech(string id) => Content?.Technologies.FirstOrDefault(t => t.Id == id);
    public AgendaDto? Agenda(string? id) => id is null ? null : Content?.Agendas.FirstOrDefault(a => a.Id == id);
    public PlanetDto? Planet(string? id) => id is null ? null : Content?.Planets.FirstOrDefault(p => p.Id == id);
    public UnitDto? Unit(string? id) => id is null ? null : Content?.Units.FirstOrDefault(u => u.Id == id);

    public IReadOnlyList<UnitDto> Units =>
        Content?.Units ?? (IReadOnlyList<UnitDto>)Array.Empty<UnitDto>();
    /// <summary>Buildable units (standard + faction Stufe I), filtered by the session's active expansions.</summary>
    public IEnumerable<UnitDto> ActiveUnits() =>
        (Content?.Units ?? Enumerable.Empty<UnitDto>()).Where(u => (Active & u.Expansion) != 0);

    public IReadOnlyList<PlanetDto> Planets =>
        Content?.Planets ?? (IReadOnlyList<PlanetDto>)Array.Empty<PlanetDto>();

    /// <summary>Planets eligible for a given planet-elect agenda, filtered by the session's active expansions.</summary>
    public IEnumerable<PlanetDto> PlanetsFor(ElectType kind)
    {
        var ps = (Content?.Planets ?? Enumerable.Empty<PlanetDto>()).Where(p => (Active & p.Expansion) != 0);
        return kind switch
        {
            ElectType.CulturalPlanet => ps.Where(p => p.Trait == PlanetTrait.Cultural),
            ElectType.HazardousPlanet => ps.Where(p => p.Trait == PlanetTrait.Hazardous),
            ElectType.IndustrialPlanet => ps.Where(p => p.Trait == PlanetTrait.Industrial),
            ElectType.NonHomePlanet => ps.Where(p => p.HomeFactionId is null && p.Id != "mecatol-rex"),
            ElectType.Planet => ps,
            _ => Enumerable.Empty<PlanetDto>(),
        };
    }

    public IEnumerable<FactionDto> ActiveFactions() =>
        (Content?.Factions ?? Enumerable.Empty<FactionDto>()).Where(f => (Active & f.Expansion) != 0);
    public IEnumerable<ObjectiveDto> ActiveObjectives() =>
        (Content?.Objectives ?? Enumerable.Empty<ObjectiveDto>()).Where(o => (Active & o.Expansion) != 0);
    public IEnumerable<TechnologyDto> ActiveTechnologies() =>
        (Content?.Technologies ?? Enumerable.Empty<TechnologyDto>()).Where(t => (Active & t.Expansion) != 0);

    public IEnumerable<AgendaDto> ActiveAgendas()
    {
        var pok = (Active & Expansion.ProphecyOfKings) != 0;
        return (Content?.Agendas ?? Enumerable.Empty<AgendaDto>())
            .Where(a => (Active & a.Expansion) != 0 && !(pok && a.RemovedInPok));
    }

    public IReadOnlyList<StrategyCardDto> StrategyCards =>
        Content?.StrategyCards ?? (IReadOnlyList<StrategyCardDto>)Array.Empty<StrategyCardDto>();

    public int TradeGoodsOn(int cardId) =>
        Session?.StrategyCardStates.FirstOrDefault(s => s.StrategyCardId == cardId)?.TradeGoods ?? 0;

    public Guid? CardOwner(int cardId) =>
        Session?.Players.FirstOrDefault(p => p.StrategyCards.Any(c => c.StrategyCardId == cardId))?.Id;

    public async ValueTask DisposeAsync()
    {
        if (_hub is not null) await _hub.DisposeAsync();
    }
}

internal static class SignalREvents
{
    public const string SessionChanged = "SessionChanged";
}

/// <summary>Stage of the agenda phase (see <see cref="SessionStore.CurrentAgendaStage"/>).</summary>
public enum AgendaStage
{
    /// <summary>No agenda revealed yet — players enter their available influence; host picks an agenda.</summary>
    Influence,
    /// <summary>Agenda revealed; host decides how to start the vote (open or face-down).</summary>
    AgendaRevealed,
    /// <summary>Open voting: drafts are locked one by one and shown as they lock.</summary>
    VotingOpen,
    /// <summary>Face-down voting: only "voted" shows until the host reveals.</summary>
    VotingHidden,
    /// <summary>Face-down voting, intermediate step: the totals are public, who voted what is not.</summary>
    VotingHiddenTotals
}
