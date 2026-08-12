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

    public Task<InstanceDto?> GetInstanceAsync()
        => http.GetFromJsonAsync<InstanceDto>("api/instance");

    public Task<PushKeyDto?> GetPushKeyAsync()
        => http.GetFromJsonAsync<PushKeyDto>("api/push/key");

    public Task SubscribePushAsync(Guid id, PushSubscribeRequest req)
        => http.PostAsJsonAsync($"api/sessions/{id}/push", req);

    public Task UnsubscribePushAsync(Guid id, string endpoint)
        => http.PostAsJsonAsync($"api/sessions/{id}/push/remove", new PushSubscribeRequest(endpoint, "", ""));

    public async Task<SessionStateDto?> GetSessionAsync(string code)
    {
        var resp = await http.GetAsync($"api/sessions/{code}");
        if (resp.StatusCode == HttpStatusCode.NotFound) return null;
        // 429 (per-IP read rate limit): degrade gracefully — keep the current state, the next
        // SignalR event retries. Never throw into the hub callback / UI event handler.
        if (resp.StatusCode == HttpStatusCode.TooManyRequests) return null;
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

    public Task<SessionStateDto?> PauseGameAsync(Guid id) => PostFor<SessionStateDto>($"api/sessions/{id}/pause", null);
    public Task<SessionStateDto?> ResumeGameAsync(Guid id) => PostFor<SessionStateDto>($"api/sessions/{id}/resume", null);

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
    /// <summary>Record the permanent summary of a finished game. Fire-and-forget from the UI's point of
    /// view: it returns no session state, and a session that never became a game is simply not recorded.</summary>
    public Task RecordSummaryAsync(Guid id) => http.PostAsync($"api/sessions/{id}/summary", null);

    public Task<SessionStateDto?> SetSeatOrderAsync(Guid id, IReadOnlyList<Guid> playerIds)
        => PostFor($"api/sessions/{id}/seat-order", new SetSeatOrderRequest(playerIds));
    public Task<SessionStateDto?> SetStatusDoneAsync(Guid id, Guid playerId, bool done)
        => PostFor($"api/sessions/{id}/players/{playerId}/status-done", new SetStatusDoneRequest(done));
    public Task<SessionStateDto?> SetStatusStepAsync(Guid id, StatusStep step, bool done)
        => PostFor($"api/sessions/{id}/status-step", new SetStatusStepRequest(step, done));
    public Task<SessionStateDto?> AppointSpeakerAsync(Guid id, Guid playerId)
        => PostFor($"api/sessions/{id}/speaker", new SetSpeakerRequest(playerId));
    public Task<SessionStateDto?> SetSecondaryPlayersAsync(Guid id, IReadOnlyList<Guid> playerIds)
        => PostFor($"api/sessions/{id}/secondary", new SetSecondaryPlayersRequest(playerIds));
    public Task<SessionStateDto?> SetSecondaryDoneAsync(Guid id, Guid playerId)
        => PostFor($"api/sessions/{id}/players/{playerId}/secondary-done", new { });
    public Task<SessionStateDto?> CloseSecondaryAsync(Guid id)
        => PostFor($"api/sessions/{id}/secondary/close", new { });
    public Task<SessionStateDto?> StartCombatAsync(Guid id, Guid opponentId)
        => PostFor($"api/sessions/{id}/combat", new StartCombatRequest(opponentId));
    public Task<SessionStateDto?> EndCombatAsync(Guid id)
        => PostFor($"api/sessions/{id}/combat/end", new { });

    /// <summary>Archive the match: the summary is kept, the rest is dropped. <paramref name="reset"/> keeps the
    /// session as a fresh setup with the same table; otherwise it is deleted. Never throws — the caller is
    /// leaving the game either way.</summary>
    public async Task ArchiveSessionAsync(Guid id, bool reset)
    {
        try { await http.PostAsJsonAsync($"api/sessions/{id}/archive", new ArchiveSessionRequest(reset)); }
        catch { }
    }
    public Task<SessionStateDto?> SetStatusStageAsync(Guid id, StatusStage stage)
        => PostFor($"api/sessions/{id}/status-stage", new SetStatusStageRequest(stage));
    public Task<SessionStateDto?> SetObjectiveMarkerAsync(Guid id, Guid sessionObjectiveId, bool removed, bool over = false)
        => PostFor($"api/sessions/{id}/objectives/{sessionObjectiveId}/marker", new SetObjectiveMarkerRequest(removed, over));
    /// <summary>This player is done recording what they researched. When nobody is pending any more the
    /// prompt closes by itself and the clock starts again.</summary>
    public Task<SessionStateDto?> SetTechPromptDoneAsync(Guid id, Guid playerId)
        => PostFor($"api/sessions/{id}/players/{playerId}/tech-prompt-done", new { });

    /// <summary>Move the table on: the player who played the Technology card, or the host, ends the recording
    /// for everyone.</summary>
    public Task<SessionStateDto?> CloseTechPromptAsync(Guid id)
        => PostFor($"api/sessions/{id}/tech-prompt/close", new { });

    // Red Tape Lite's two questions. The app proposes, the table answers — nothing is removed or purged until
    // one of these is called with confirm: true.
    public Task<SessionStateDto?> AnswerRedTapePurgeAsync(Guid id, bool confirm)
        => PostFor($"api/sessions/{id}/redtape/purge", new RedTapeAnswerRequest(confirm));
    public Task<SessionStateDto?> AnswerRedTapeRandomAsync(Guid id, bool confirm)
        => PostFor($"api/sessions/{id}/redtape/random", new RedTapeAnswerRequest(confirm));
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
    /// <summary>Start a free vote: no agenda card, just a headline and what it elects.</summary>
    public Task<SessionStateDto?> StartFreeVoteAsync(Guid id, string title, ElectType elect)
        => PostFor($"api/sessions/{id}/agenda", new SetAgendaRequest(null, title, elect));
    public Task<SessionStateDto?> SetAgendaAsync(Guid id, string? agendaId)
        => PostFor($"api/sessions/{id}/agenda", new SetAgendaRequest(agendaId));
    public Task<SessionStateDto?> StartVotingAsync(Guid id, bool hidden)
        => PostForAllowBadRequest($"api/sessions/{id}/agenda/start", new StartVotingRequest(hidden));
    public Task<SessionStateDto?> CancelVotingAsync(Guid id)
        => PostFor<SessionStateDto>($"api/sessions/{id}/agenda/cancel", null);
    public Task<SessionStateDto?> RevealVoteTotalsAsync(Guid id)
        => PostFor<SessionStateDto>($"api/sessions/{id}/agenda/reveal-totals", null);
    public Task<SessionStateDto?> RevealVotesAsync(Guid id)
        => PostFor<SessionStateDto>($"api/sessions/{id}/agenda/reveal", null);
    public Task<SessionStateDto?> LockVoteAsync(Guid id, Guid playerId, VoteOutcome outcome, int votes, string? choice)
        => PostForAllowBadRequest($"api/sessions/{id}/agenda/lock", new LockVoteRequest(playerId, outcome, votes, choice));
    public Task<SessionStateDto?> SetInfluenceAsync(Guid id, Guid playerId, int influence)
        => PostForAllowBadRequest($"api/sessions/{id}/players/{playerId}/influence", new SetInfluenceRequest(playerId, influence));

    /// <summary>Report that a player's turn budget has run out, so the server can notify them. Changes no
    /// state and returns nothing — several devices notice the same second and the server drops duplicates.
    /// Never throws: a missed notification must not surface as an error in a ticking clock.</summary>
    public async Task ReportTimeUpAsync(Guid id, Guid playerId)
    {
        try { await http.PostAsync($"api/sessions/{id}/players/{playerId}/time-up", null); }
        catch { }
    }

    /// <summary>Activity counts for the start page. Null when unavailable (offline, rate-limited, an older
    /// server) — the page then simply shows nothing rather than an error.</summary>
    public async Task<ActivityDto?> GetActivityAsync()
    {
        try { return await http.GetFromJsonAsync<ActivityDto>("api/activity"); }
        catch { return null; }
    }

    // ---- Match log ----
    public async Task<IReadOnlyList<SessionLogEntryDto>> GetLogAsync(string code)
    {
        var resp = await http.GetAsync($"api/sessions/{code}/log");
        if (resp.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.TooManyRequests) return Array.Empty<SessionLogEntryDto>();
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<List<SessionLogEntryDto>>() ?? new();
    }

    // ---- helpers ----
    // A rule violation (400), a not-allowed call (403 — host only), a paused game (423), or a
    // rate-limit hit (429) returns null/default so the store refreshes to the authoritative state
    // instead of throwing an unhandled exception into the UI / SignalR callback.
    private static bool IsRejection(HttpResponseMessage r) =>
        r.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Forbidden
            or HttpStatusCode.Locked or HttpStatusCode.TooManyRequests; // 423 = paused, 429 = rate-limited

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
