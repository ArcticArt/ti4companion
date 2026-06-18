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
    private const string KeyCode = "ti4.code";
    private const string KeyPlayer = "ti4.player";
    private const string KeyLang = "ti4.lang";

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

    public PlayerDto? Me => Session?.Players.FirstOrDefault(p => p.Id == MyPlayerId);

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

    /// <summary>Whether this device may edit the given player: self always, the host may edit anyone,
    /// or anyone when the session has open editing enabled.</summary>
    public bool CanEdit(Guid playerId)
        => Session is not null && (IsHost || Session.AllowEditAllPlayers || playerId == MyPlayerId);

    public int MaxStrategyCards => (Session?.Players.Count ?? 0) <= 4 ? 2 : 1;

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

    public Task<string?> GetLastCodeAsync() => storage.GetAsync(KeyCode).AsTask();

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
        if (s is null) { Session = null; OnChange?.Invoke(); return false; }

        Session = s;

        var storedCode = await storage.GetAsync(KeyCode);
        var storedPlayer = await storage.GetAsync(KeyPlayer);
        if (storedCode == s.JoinCode && Guid.TryParse(storedPlayer, out var pid) && s.Players.Any(p => p.Id == pid))
        {
            MyPlayerId = pid;
        }

        ApplySessionLanguageIfUnset(s);
        await EnsureHubAsync(s.JoinCode);
        OnChange?.Invoke();
        return true;
    }

    public async Task Mutate(Task<SessionStateDto?> apiCall)
    {
        var s = await apiCall;
        if (s is not null) { Session = s; OnChange?.Invoke(); }
        else await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        if (Session is null) return;
        Session = await api.GetSessionAsync(Session.JoinCode);
        OnChange?.Invoke();
    }

    /// <summary>Fully leave the current session: disconnect, forget identity, clear persistence.</summary>
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
        await storage.SetAsync(KeyCode, result.Session.JoinCode);
        await storage.SetAsync(KeyPlayer, result.PlayerId.ToString());
        ApplySessionLanguageIfUnset(result.Session);
        await EnsureHubAsync(result.Session.JoinCode);
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
            if (s is null || s.CurrentAgendaId is null) return AgendaStage.Influence;
            if (!s.VotingStarted) return AgendaStage.AgendaRevealed;
            return s.AgendaVotesHidden ? AgendaStage.VotingHidden : AgendaStage.VotingOpen;
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
    VotingHidden
}
