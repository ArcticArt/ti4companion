using System.Net;
using System.Net.Http.Json;
using Ti4Companion.Shared;

namespace Ti4Companion.Web.Services;

/// <summary>Typed wrapper around the REST API. All session mutations return the new shared state.</summary>
public class Ti4ApiClient(HttpClient http)
{
    private const string DeviceTokenHeader = "X-Device-Token";

    /// <summary>Identify this device on every request so the server can enforce host privileges.</summary>
    public void SetDeviceToken(string token)
    {
        http.DefaultRequestHeaders.Remove(DeviceTokenHeader);
        if (!string.IsNullOrEmpty(token)) http.DefaultRequestHeaders.Add(DeviceTokenHeader, token);
    }

    public Task<ContentBundleDto?> GetContentAsync()
        => http.GetFromJsonAsync<ContentBundleDto>("api/content");

    public async Task<SessionStateDto?> GetSessionAsync(string code)
    {
        var resp = await http.GetAsync($"api/sessions/{code}");
        if (resp.StatusCode == HttpStatusCode.NotFound) return null;
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<SessionStateDto>();
    }

    public Task<JoinResultDto?> CreateSessionAsync(CreateSessionRequest req)
        => PostFor<JoinResultDto>("api/sessions", req);

    public Task<JoinResultDto?> JoinSessionAsync(Guid id, JoinSessionRequest req)
        => PostFor<JoinResultDto>($"api/sessions/{id}/players", req);

    public Task<SessionStateDto?> UpdateSessionAsync(Guid id, UpdateSessionRequest req)
        => PatchFor($"api/sessions/{id}", req);

    public Task DeleteSessionAsync(Guid id) => http.DeleteAsync($"api/sessions/{id}");

    public Task<SessionStateDto?> SetDisplayModeAsync(Guid id, DisplayMode mode)
        => PostFor($"api/sessions/{id}/display", new SetDisplayModeRequest(mode));

    // ---- Phase / round flow ----
    public Task<SessionStateDto?> StartGameAsync(Guid id) => PostFor<SessionStateDto>($"api/sessions/{id}/phase/start", null);
    public Task<SessionStateDto?> StartActionPhaseAsync(Guid id) => PostFor<SessionStateDto>($"api/sessions/{id}/phase/action", null);
    public Task<SessionStateDto?> EndActionPhaseAsync(Guid id) => PostFor<SessionStateDto>($"api/sessions/{id}/phase/status", null);
    public Task<SessionStateDto?> StartAgendaPhaseAsync(Guid id) => PostFor<SessionStateDto>($"api/sessions/{id}/phase/agenda", null);
    public Task<SessionStateDto?> NextRoundAsync(Guid id) => PostFor<SessionStateDto>($"api/sessions/{id}/round/next", null);

    // ---- Turn ----
    public Task<SessionStateDto?> SetActiveStrategyAsync(Guid id, int? cardId)
        => PostFor($"api/sessions/{id}/active-strategy", new SetActiveStrategyCardRequest(cardId));
    public Task<SessionStateDto?> SetActivePlayerAsync(Guid id, Guid? playerId)
        => PostFor($"api/sessions/{id}/turn/active", new SetActivePlayerRequest(playerId));
    public Task<SessionStateDto?> AdvanceTurnAsync(Guid id) => PostFor<SessionStateDto>($"api/sessions/{id}/turn/advance", null);
    public Task<SessionStateDto?> PreviousTurnAsync(Guid id) => PostFor<SessionStateDto>($"api/sessions/{id}/turn/previous", null);

    // ---- Players ----
    public Task<SessionStateDto?> UpdatePlayerAsync(Guid id, Guid playerId, UpdatePlayerRequest req)
        => PatchFor($"api/sessions/{id}/players/{playerId}", req);
    public Task RemovePlayerAsync(Guid id, Guid playerId)
        => http.DeleteAsync($"api/sessions/{id}/players/{playerId}");
    public Task<SessionStateDto?> SetPassedAsync(Guid id, Guid playerId, bool passed)
        => PostForAllowBadRequest($"api/sessions/{id}/players/{playerId}/pass", new SetPassedRequest(passed));

    // ---- Strategy cards ----
    public Task<SessionStateDto?> AssignStrategyCardAsync(Guid id, Guid playerId, int cardId)
        => PostForAllowBadRequest($"api/sessions/{id}/players/{playerId}/strategy-cards", new AssignStrategyCardRequest(cardId));
    public Task UnassignStrategyCardAsync(Guid id, Guid playerId, int cardId)
        => http.DeleteAsync($"api/sessions/{id}/players/{playerId}/strategy-cards/{cardId}");
    public Task<SessionStateDto?> SetStrategyCardUsedAsync(Guid id, Guid playerId, int cardId, bool used)
        => PostFor($"api/sessions/{id}/players/{playerId}/strategy-cards/{cardId}/used", new SetStrategyCardUsedRequest(used));

    // ---- Objectives ----
    public Task<SessionStateDto?> RevealObjectiveAsync(Guid id, string objectiveId)
        => PostFor($"api/sessions/{id}/objectives", new RevealObjectiveRequest(objectiveId));
    public Task<SessionStateDto?> RevealCustomObjectiveAsync(Guid id, string name, int points)
        => PostFor($"api/sessions/{id}/objectives/custom", new RevealCustomObjectiveRequest(name, points));
    public Task RemoveObjectiveAsync(Guid id, Guid sessionObjectiveId)
        => http.DeleteAsync($"api/sessions/{id}/objectives/{sessionObjectiveId}");
    public Task<SessionStateDto?> ScoreObjectiveAsync(Guid id, Guid sessionObjectiveId, Guid playerId)
        => PostFor($"api/sessions/{id}/objectives/{sessionObjectiveId}/scores", new ScoreObjectiveRequest(playerId));
    public Task UnscoreObjectiveAsync(Guid id, Guid sessionObjectiveId, Guid playerId)
        => http.DeleteAsync($"api/sessions/{id}/objectives/{sessionObjectiveId}/scores/{playerId}");

    // ---- Technologies ----
    public Task<SessionStateDto?> AddTechnologyAsync(Guid id, Guid playerId, string techId)
        => PostFor($"api/sessions/{id}/players/{playerId}/technologies", new AddTechnologyRequest(techId));
    public Task RemoveTechnologyAsync(Guid id, Guid playerId, string techId)
        => http.DeleteAsync($"api/sessions/{id}/players/{playerId}/technologies/{Uri.EscapeDataString(techId)}");

    // ---- Agenda phase ----
    public Task<SessionStateDto?> SetAgendaAsync(Guid id, string? agendaId)
        => PostFor($"api/sessions/{id}/agenda", new SetAgendaRequest(agendaId));
    public Task<SessionStateDto?> StartVotingAsync(Guid id, bool hidden)
        => PostForAllowBadRequest($"api/sessions/{id}/agenda/start", new StartVotingRequest(hidden));
    public Task<SessionStateDto?> CancelVotingAsync(Guid id)
        => PostFor<SessionStateDto>($"api/sessions/{id}/agenda/cancel", null);
    public Task<SessionStateDto?> RevealVotesAsync(Guid id)
        => PostFor<SessionStateDto>($"api/sessions/{id}/agenda/reveal", null);
    public Task<SessionStateDto?> LockVoteAsync(Guid id, Guid playerId, VoteOutcome outcome, int votes, string? choice)
        => PostForAllowBadRequest($"api/sessions/{id}/agenda/lock", new LockVoteRequest(playerId, outcome, votes, choice));
    public Task<SessionStateDto?> SetInfluenceAsync(Guid id, Guid playerId, int influence)
        => PostForAllowBadRequest($"api/sessions/{id}/players/{playerId}/influence", new SetInfluenceRequest(playerId, influence));

    // ---- Match log ----
    public async Task<IReadOnlyList<SessionLogEntryDto>> GetLogAsync(string code)
    {
        var resp = await http.GetAsync($"api/sessions/{code}/log");
        if (resp.StatusCode == HttpStatusCode.NotFound) return Array.Empty<SessionLogEntryDto>();
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<List<SessionLogEntryDto>>() ?? new();
    }

    // ---- helpers ----
    // A 400 (rule violation) or 403 (not allowed — host only) returns null/default so the store
    // refreshes to the authoritative state instead of throwing.
    private static bool IsRejection(HttpResponseMessage r) =>
        r.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Forbidden;

    private async Task<T?> PostFor<T>(string url, object? body)
    {
        var resp = body is null ? await http.PostAsync(url, null) : await http.PostAsJsonAsync(url, body);
        if (IsRejection(resp)) return default;
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<T>();
    }

    private Task<SessionStateDto?> PostFor(string url, object body) => PostFor<SessionStateDto>(url, body);

    private Task<SessionStateDto?> PostForAllowBadRequest(string url, object body) => PostFor<SessionStateDto>(url, body);

    private async Task<SessionStateDto?> PatchFor(string url, object body)
    {
        var resp = await http.PatchAsJsonAsync(url, body);
        if (IsRejection(resp)) return null;
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<SessionStateDto>();
    }
}
