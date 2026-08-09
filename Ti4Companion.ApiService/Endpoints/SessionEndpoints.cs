using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Ti4Companion.ApiService.Data;
using Ti4Companion.ApiService.Realtime;
using Ti4Companion.ApiService.Services;
using Ti4Companion.Shared;

namespace Ti4Companion.ApiService.Endpoints;

public static class SessionEndpoints
{
    private const Expansion AllExpansions =
        Expansion.Base | Expansion.ProphecyOfKings | Expansion.Codex | Expansion.ThundersEdge;

    public static IEndpointRouteBuilder MapSessionEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/sessions");

        // ---- Session lifecycle ----
        g.MapPost("/", CreateSession).RequireRateLimiting("session-create");
        g.MapGet("/{code}", GetByCode).RequireRateLimiting("session-read");
        // Same per-IP read limit as GET /{code}: /log is the other unauthenticated by-code lookup, so it
        // must be throttled too or it becomes an unmetered join-code enumeration oracle (404 vs 200).
        g.MapGet("/{code}/log", GetLog).RequireRateLimiting("session-read"); // match log (read-only; client shows it host-side)
        g.MapPatch("/{id:guid}", UpdateSession);
        g.MapDelete("/{id:guid}", DeleteSession);
        g.MapPost("/{id:guid}/display", SetDisplayMode);          // wall-display view switch (any player)
        g.MapPost("/{id:guid}/pause", PauseGame);                 // host pauses (locks all input; excluded from stats)
        g.MapPost("/{id:guid}/resume", ResumeGame);               // host resumes

        // While a session is paused, reject every mutation except resume (and reads). The host must resume first.
        g.AddEndpointFilter(async (ctx, next) =>
        {
            var http = ctx.HttpContext;
            var path = http.Request.Path.Value ?? "";
            if (!HttpMethods.IsGet(http.Request.Method)
                && !path.EndsWith("/resume", StringComparison.OrdinalIgnoreCase)
                && http.Request.RouteValues.TryGetValue("id", out var idv)
                && Guid.TryParse(idv?.ToString(), out var sid))
            {
                var db = http.RequestServices.GetRequiredService<Ti4DbContext>();
                if (await db.Sessions.Where(s => s.Id == sid).Select(s => s.Paused).FirstOrDefaultAsync())
                    return Results.Json(new { error = "Game is paused." }, statusCode: StatusCodes.Status423Locked);
            }
            return await next(ctx);
        });

        // ---- Phase / round flow ----
        g.MapPost("/{id:guid}/phase/start", StartGame);          // Setup -> Strategy
        g.MapPost("/{id:guid}/phase/action", StartActionPhase);  // Strategy -> Action
        g.MapPost("/{id:guid}/phase/status", EndActionPhase);    // Action -> Status
        g.MapPost("/{id:guid}/phase/agenda", StartAgendaPhase);  // Status -> Agenda
        g.MapPost("/{id:guid}/round/next", NextRound);           // Status/Agenda -> Strategy (round+1)

        // ---- Turn ----
        g.MapPost("/{id:guid}/active-strategy", SetActiveStrategy);
        g.MapPost("/{id:guid}/turn/active", SetActivePlayer);
        g.MapPost("/{id:guid}/turn/advance", AdvanceTurn);
        g.MapPost("/{id:guid}/turn/previous", PreviousTurn);

        // ---- Players ----
        g.MapPost("/{id:guid}/players", JoinSession);
        g.MapPatch("/{id:guid}/players/{playerId:guid}", UpdatePlayer);
        g.MapDelete("/{id:guid}/players/{playerId:guid}", RemovePlayer);
        g.MapPost("/{id:guid}/players/{playerId:guid}/pass", SetPassed);
        g.MapPost("/{id:guid}/players/{playerId:guid}/influence", SetInfluence); // agenda-phase influence (self/host)

        // ---- Strategy cards (per player) ----
        g.MapPost("/{id:guid}/players/{playerId:guid}/strategy-cards", AssignStrategyCard);
        g.MapDelete("/{id:guid}/players/{playerId:guid}/strategy-cards/{cardId:int}", UnassignStrategyCard);
        g.MapPost("/{id:guid}/players/{playerId:guid}/strategy-cards/{cardId:int}/used", SetStrategyCardUsed);

        g.MapPost("/{id:guid}/seat-order", SetSeatOrder);   // whole table order in one call (host)

        // ---- Status phase (scoring order + shared checklist) ----
        g.MapPost("/{id:guid}/players/{playerId:guid}/status-done", SetStatusDone);
        g.MapPost("/{id:guid}/status-step", SetStatusStep);

        // ---- Objectives ----
        g.MapPost("/{id:guid}/objectives", RevealObjective);
        g.MapPost("/{id:guid}/objectives/custom", RevealCustomObjective); // secret made public / hand-added
        g.MapDelete("/{id:guid}/objectives/{sessionObjectiveId:guid}", RemoveObjective);
        g.MapPost("/{id:guid}/objectives/{sessionObjectiveId:guid}/marker", SetObjectiveMarker); // Red Tape variant
        g.MapPost("/{id:guid}/objectives/{sessionObjectiveId:guid}/scores", ScoreObjective);
        g.MapDelete("/{id:guid}/objectives/{sessionObjectiveId:guid}/scores/{playerId:guid}", UnscoreObjective);

        // ---- Technologies (per player) ----
        g.MapPost("/{id:guid}/players/{playerId:guid}/technologies", AddTechnology);
        g.MapDelete("/{id:guid}/players/{playerId:guid}/technologies/{techId}", RemoveTechnology);

        // ---- Agenda phase ----
        g.MapPost("/{id:guid}/agenda", SetAgenda);              // reveal an agenda / clear it (deducts spent influence)
        g.MapPost("/{id:guid}/agenda/start", StartVoting);      // host opens the vote (open or face-down)
        g.MapPost("/{id:guid}/agenda/cancel", CancelVoting);    // host aborts → back to influence entry
        g.MapPost("/{id:guid}/agenda/reveal-totals", RevealVoteTotals); // totals only, no attribution
        g.MapPost("/{id:guid}/agenda/reveal", RevealVotes);     // host flips a face-down vote face-up
        g.MapPost("/{id:guid}/agenda/lock", LockVote);          // commit a vote (open or hidden); counts only once locked

        return app;
    }

    // -----------------------------------------------------------------------
    // Lifecycle
    // -----------------------------------------------------------------------

    // Public-hosting hardening: user-supplied free text (names, custom objectives, elect free-text)
    // is trimmed and length-capped server-side so it can't bloat the DB or the wall display.
    private static string Clamp(string s, int max = 60)
    {
        s = s.Trim();
        return s.Length <= max ? s : s[..max];
    }

    // Loose content-reference ids (faction/objective/tech/agenda slugs). Legit slugs are short; cap
    // the length so a rogue client can't persist a giant string that then ships in every state DTO.
    // Preserves null (= "no reference") — only a present value is trimmed/capped.
    private static string? ClampId(string? s) => string.IsNullOrWhiteSpace(s) ? s : Clamp(s!, 60);

    // A player colour must be a plain CSS hex (#rgb / #rrggbb / #rrggbbaa). ColorHex is interpolated
    // straight into inline style attributes on the wall + control views, so anything else would let a
    // joined client inject arbitrary CSS declarations onto every viewer's screen — reject it to the
    // fallback. Also bounds the stored length. See DEPLOY.md "Security review".
    private static readonly System.Text.RegularExpressions.Regex HexColor =
        new("^#[0-9a-fA-F]{3,8}$", System.Text.RegularExpressions.RegexOptions.Compiled);
    private static string SanitizeColor(string? hex, string fallback = "#cccccc")
        => hex is not null && HexColor.IsMatch(hex.Trim()) ? hex.Trim() : fallback;

    private static async Task<IResult> CreateSession(CreateSessionRequest req, Ti4DbContext db, IHubContext<SessionHub> hub, IConfiguration config, CancellationToken ct)
    {
        var deviceToken = string.IsNullOrWhiteSpace(req.DeviceToken) ? Guid.NewGuid().ToString("N") : req.DeviceToken;
        var session = new GameSession
        {
            JoinCode = await UniqueCodeAsync(db, ct),
            Name = string.IsNullOrWhiteSpace(req.Name) ? "Twilight Imperium" : Clamp(req.Name),
            DefaultLanguage = req.Language,
            ActiveExpansions = (req.ActiveExpansions ?? AllExpansions) | Expansion.Base,
            RetentionHours = config.GetValue("Ti4:DefaultRetentionHours", 168),
            Phase = GamePhase.Setup,
        };

        var host = new Player
        {
            SessionId = session.Id,
            Name = string.IsNullOrWhiteSpace(req.HostName) ? "Host" : Clamp(req.HostName),
            FactionId = ClampId(req.FactionId),
            ColorHex = SanitizeColor(req.ColorHex),
            SeatOrder = 0,
            IsHost = true,
            DeviceToken = deviceToken,
        };
        session.Players.Add(host);

        db.Sessions.Add(session);
        Log(db, session, SessionLogKind.PlayerJoin, host.Id, host.Id, detail: "host");
        await db.SaveChangesAsync(ct);

        var overrides = FactionInitiative.Overrides;
        var state = (await LoadGraphAsync(db, session.Id, ct))!.ToDto(overrides);
        return Results.Ok(new JoinResultDto(state, host.Id, deviceToken));
    }

    private static async Task<IResult> GetByCode(string code, Ti4DbContext db, CancellationToken ct)
    {
        var session = await LoadGraphByCodeAsync(db, SessionHub.Normalize(code), ct);
        if (session is null) return Results.NotFound();
        var overrides = FactionInitiative.Overrides;
        return Results.Ok(session.ToDto(overrides));
    }

    // The match log (chronological). Read-only like GetByCode; the client only surfaces it to the host.
    private static async Task<IResult> GetLog(string code, Ti4DbContext db, CancellationToken ct)
    {
        var session = await db.Sessions.AsNoTracking().FirstOrDefaultAsync(s => s.JoinCode == SessionHub.Normalize(code), ct);
        if (session is null) return Results.NotFound();
        // SQLite can't ORDER BY a DateTimeOffset in SQL, so sort in memory (as the cleanup worker does).
        var rows = await db.SessionLog.AsNoTracking()
            .Where(l => l.SessionId == session.Id)
            .ToListAsync(ct);
        return Results.Ok(rows.OrderBy(l => l.TimestampUtc).Select(l => l.ToDto()).ToList());
    }

    private static async Task<IResult> UpdateSession(Guid id, UpdateSessionRequest req, Ti4DbContext db, IHubContext<SessionHub> hub, HttpContext http, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        if (session is null) return Results.NotFound();
        if (!CallerIsHost(session, http)) return Forbidden(); // settings, phase, speaker → host only

        if (req.Name is not null) session.Name = Clamp(req.Name);
        if (req.Language is not null) session.DefaultLanguage = req.Language.Value;
        if (req.ActiveExpansions is not null) session.ActiveExpansions = req.ActiveExpansions.Value | Expansion.Base;
        if (req.ShowTechOverview is not null) session.ShowTechOverview = req.ShowTechOverview.Value;
        if (req.AllowEditAllPlayers is not null) session.AllowEditAllPlayers = req.AllowEditAllPlayers.Value;
        if (req.Phase is not null) session.Phase = req.Phase.Value;
        if (req.CurrentRound is > 0) session.CurrentRound = req.CurrentRound.Value;
        if (req.SpeakerPlayerId is not null && req.SpeakerPlayerId != session.SpeakerPlayerId)
        {
            session.SpeakerPlayerId = req.SpeakerPlayerId;
            Log(db, session, http, SessionLogKind.SpeakerSet, target: req.SpeakerPlayerId);
        }
        if (req.AgendaVotesHidden is not null) session.AgendaVotesHidden = req.AgendaVotesHidden.Value;
        // Turn timer: 0 = off, otherwise clamped to a sane 10 s … 2 h budget per player per round.
        if (req.TurnTimerSeconds is { } tt)
            session.TurnTimerSeconds = tt <= 0 ? 0 : Math.Clamp(tt, 10, 7200);
        // Strategy cards per player: only 0 (automatic), 1 or 2 are meaningful.
        if (req.StrategyCardsPerPlayer is { } cpp)
            session.StrategyCardsPerPlayer = cpp is 1 or 2 ? cpp : 0;
        if (req.RedTapeLite is not null) session.RedTapeLite = req.RedTapeLite.Value;
        if (req.PromptTechOnAction is not null) session.PromptTechOnAction = req.PromptTechOnAction.Value;

        return await SaveAndReturn(db, hub, session, ct);
    }

    private static async Task<IResult> DeleteSession(Guid id, Ti4DbContext db, IHubContext<SessionHub> hub, HttpContext http, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        if (session is null) return Results.NotFound();
        if (!CallerIsHost(session, http)) return Forbidden();
        var code = session.JoinCode;
        db.Sessions.Remove(session);
        await db.SaveChangesAsync(ct);
        await hub.NotifySessionChanged(code);
        return Results.NoContent();
    }

    // Wall-display view switch — open to any player so anyone can flip the shared screen.
    private static async Task<IResult> SetDisplayMode(Guid id, SetDisplayModeRequest req, Ti4DbContext db, IHubContext<SessionHub> hub, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        if (session is null) return Results.NotFound();
        session.DisplayMode = req.Mode;
        return await SaveAndReturn(db, hub, session, ct);
    }

    // Host pauses / resumes. The GamePaused→GameResumed interval is excluded from the statistics, and the
    // endpoint filter rejects every other mutation while paused (host must resume first).
    private static async Task<IResult> PauseGame(Guid id, Ti4DbContext db, IHubContext<SessionHub> hub, HttpContext http, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        if (session is null) return Results.NotFound();
        if (!CallerIsHost(session, http)) return Forbidden();
        if (!session.Paused) { session.Paused = true; Log(db, session, http, SessionLogKind.GamePaused); }
        return await SaveAndReturn(db, hub, session, ct);
    }

    private static async Task<IResult> ResumeGame(Guid id, Ti4DbContext db, IHubContext<SessionHub> hub, HttpContext http, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        if (session is null) return Results.NotFound();
        if (!CallerIsHost(session, http)) return Forbidden();
        if (session.Paused) { session.Paused = false; Log(db, session, http, SessionLogKind.GameResumed); }
        return await SaveAndReturn(db, hub, session, ct);
    }

    // -----------------------------------------------------------------------
    // Phase / round flow
    // -----------------------------------------------------------------------

    private static async Task<IResult> StartGame(Guid id, Ti4DbContext db, IHubContext<SessionHub> hub, HttpContext http, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        if (session is null) return Results.NotFound();
        if (!CallerIsHost(session, http)) return Forbidden();
        // Seat order + speaker must be settled, and everyone ready, before the game can start.
        if (session.Players.Count == 0 || session.Players.Any(p => !p.IsReady))
            return Results.BadRequest(new { error = "All players must be ready." });
        if (session.SpeakerPlayerId is null)
            return Results.BadRequest(new { error = "Choose a speaker before starting." });
        // The 2 starting public objectives are chosen physically and recorded by the host during
        // setup (see ObjectivesTab) — they are no longer auto-revealed here.
        session.Phase = GamePhase.Strategy;
        // Timeline markers for the statistics view: the match (and round 1) begin now.
        Log(db, session, SessionLogKind.RoundChange, GetCaller(session, http)?.Id, round: session.CurrentRound);
        Log(db, session, SessionLogKind.PhaseChange, GetCaller(session, http)?.Id, phase: GamePhase.Strategy, round: session.CurrentRound);
        return await SaveAndReturn(db, hub, session, ct);
    }

    private static async Task<IResult> StartActionPhase(Guid id, Ti4DbContext db, IHubContext<SessionHub> hub, HttpContext http, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        if (session is null) return Results.NotFound();
        if (!CallerIsHost(session, http)) return Forbidden();

        // Every player must have taken their full strategy-card allotment first (printed rule unless the
        // table pinned a count — see GameRules).
        var perPlayer = GameRules.StrategyCardsPerPlayer(session.Players.Count, session.StrategyCardsPerPlayer);
        if (session.Players.Count == 0 || session.Players.Any(p => p.StrategyCards.Count < perPlayer))
            return Results.BadRequest(new { error = "All players must pick their strategy card(s) first." });

        // Trade goods are settled only now (start of the action phase): each player collects the
        // goods accumulated on the card they picked, and every card no one picked gains 1 more.
        // Keeping them on the card through the whole strategy phase means a pick→unpick doesn't lose them.
        var picked = session.Players.SelectMany(p => p.StrategyCards.Select(c => c.StrategyCardId)).ToHashSet();
        for (var cardId = 1; cardId <= 8; cardId++)
        {
            if (picked.Contains(cardId))
            {
                var collected = session.StrategyCardStates.FirstOrDefault(s => s.StrategyCardId == cardId);
                if (collected is not null) collected.TradeGoods = 0; // the picking player collected them
            }
            else
            {
                GetOrCreateCardState(session, cardId).TradeGoods += 1;
            }
        }

        foreach (var p in session.Players)
        {
            p.HasPassed = false;
            foreach (var c in p.StrategyCards) c.IsExhausted = false;
        }

        var overrides = FactionInitiative.Overrides;
        session.Phase = GamePhase.Action;
        session.ActiveStrategyCardId = null;
        session.ActivePlayerId = TurnService.FirstActive(session, overrides);
        Log(db, session, SessionLogKind.PhaseChange, GetCaller(session, http)?.Id, phase: GamePhase.Action, round: session.CurrentRound);
        if (session.ActivePlayerId is Guid first) Log(db, session, SessionLogKind.TurnChange, GetCaller(session, http)?.Id, target: first);
        return await SaveAndReturn(db, hub, session, ct, overrides);
    }

    private static async Task<IResult> EndActionPhase(Guid id, Ti4DbContext db, IHubContext<SessionHub> hub, HttpContext http, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        if (session is null) return Results.NotFound();
        if (!CallerIsHost(session, http)) return Forbidden();
        if (session.Players.Any(p => !p.HasPassed))
            return Results.BadRequest(new { error = "All players must pass before ending the action phase." });

        session.Phase = GamePhase.Status;
        session.ActivePlayerId = null;
        session.ActiveStrategyCardId = null;
        // Fresh status phase: scoring starts over at the lowest initiative and the checklist is blank.
        session.StatusStepsDone = StatusStep.None;
        foreach (var p in session.Players) p.StatusDone = false;
        Log(db, session, SessionLogKind.PhaseChange, GetCaller(session, http)?.Id, phase: GamePhase.Status, round: session.CurrentRound);
        return await SaveAndReturn(db, hub, session, ct);
    }

    private static async Task<IResult> StartAgendaPhase(Guid id, Ti4DbContext db, IHubContext<SessionHub> hub, HttpContext http, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        if (session is null) return Results.NotFound();
        if (!CallerIsHost(session, http)) return Forbidden();
        session.Phase = GamePhase.Agenda;
        // Fresh agenda phase: clear the agenda/votes and reset every player's entered influence.
        session.CurrentAgendaId = null;
        session.VotingStarted = false;
        session.AgendaVotesHidden = false;
        session.AgendaTotalsRevealed = false;
        session.AgendaVotes.Clear();
        foreach (var p in session.Players) p.Influence = 0;
        Log(db, session, SessionLogKind.PhaseChange, GetCaller(session, http)?.Id, phase: GamePhase.Agenda, round: session.CurrentRound);
        return await SaveAndReturn(db, hub, session, ct);
    }

    private static async Task<IResult> NextRound(Guid id, Ti4DbContext db, IHubContext<SessionHub> hub, HttpContext http, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        if (session is null) return Results.NotFound();
        if (!CallerIsHost(session, http)) return Forbidden();

        LogAgendaResult(db, session);   // capture the last agenda's result before the round resets
        session.CurrentRound += 1;
        session.Phase = GamePhase.Strategy;
        session.ActivePlayerId = null;
        session.ActiveStrategyCardId = null;
        session.CurrentAgendaId = null;
        session.VotingStarted = false;
        session.AgendaVotesHidden = false;
        session.AgendaTotalsRevealed = false;
        session.AgendaVotes.Clear();
        session.StatusStepsDone = StatusStep.None;
        foreach (var p in session.Players)
        {
            p.HasPassed = false;
            p.StatusDone = false;
            p.Influence = 0;
            p.StrategyCards.Clear();
        }

        Log(db, session, SessionLogKind.RoundChange, GetCaller(session, http)?.Id, round: session.CurrentRound);
        Log(db, session, SessionLogKind.PhaseChange, GetCaller(session, http)?.Id, phase: GamePhase.Strategy, round: session.CurrentRound);
        return await SaveAndReturn(db, hub, session, ct);
    }

    // -----------------------------------------------------------------------
    // Turn
    // -----------------------------------------------------------------------

    private static async Task<IResult> SetActiveStrategy(Guid id, SetActiveStrategyCardRequest req, Ti4DbContext db, IHubContext<SessionHub> hub, HttpContext http, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        if (session is null) return Results.NotFound();
        if (!CallerCanActFor(session, http, session.ActivePlayerId ?? Guid.Empty)) return Forbidden();
        session.ActiveStrategyCardId = req.StrategyCardId;
        return await SaveAndReturn(db, hub, session, ct);
    }

    private static async Task<IResult> SetActivePlayer(Guid id, SetActivePlayerRequest req, Ti4DbContext db, IHubContext<SessionHub> hub, HttpContext http, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        if (session is null) return Results.NotFound();
        if (!CallerIsHost(session, http)) return Forbidden(); // jumping the active player is a host takeover
        session.ActivePlayerId = req.PlayerId;
        if (req.PlayerId is Guid p) Log(db, session, http, SessionLogKind.TurnChange, target: p);
        return await SaveAndReturn(db, hub, session, ct);
    }

    private static async Task<IResult> AdvanceTurn(Guid id, Ti4DbContext db, IHubContext<SessionHub> hub, HttpContext http, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        if (session is null) return Results.NotFound();
        if (!CallerCanActFor(session, http, session.ActivePlayerId ?? Guid.Empty)) return Forbidden();
        var overrides = FactionInitiative.Overrides;
        session.ActivePlayerId = TurnService.NextActive(session, overrides);
        session.ActiveStrategyCardId = null; // a new turn begins → close any played-action highlight
        if (session.ActivePlayerId is Guid next) Log(db, session, http, SessionLogKind.TurnChange, target: next);
        return await SaveAndReturn(db, hub, session, ct, overrides);
    }

    private static async Task<IResult> PreviousTurn(Guid id, Ti4DbContext db, IHubContext<SessionHub> hub, HttpContext http, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        if (session is null) return Results.NotFound();
        if (!CallerCanActFor(session, http, session.ActivePlayerId ?? Guid.Empty)) return Forbidden();
        var overrides = FactionInitiative.Overrides;
        session.ActivePlayerId = TurnService.PreviousActive(session, overrides);
        session.ActiveStrategyCardId = null; // a new turn begins → close any played-action highlight
        if (session.ActivePlayerId is Guid prev) Log(db, session, http, SessionLogKind.TurnChange, target: prev);
        return await SaveAndReturn(db, hub, session, ct, overrides);
    }

    // -----------------------------------------------------------------------
    // Players
    // -----------------------------------------------------------------------

    private static async Task<IResult> JoinSession(Guid id, JoinSessionRequest req, Ti4DbContext db, IHubContext<SessionHub> hub, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        if (session is null) return Results.NotFound();

        var deviceToken = string.IsNullOrWhiteSpace(req.DeviceToken) ? Guid.NewGuid().ToString("N") : req.DeviceToken;
        Player target;

        if (req.ClaimPlayerId is Guid claimId)
        {
            // Take over (claim) an existing seat. Any non-host player may be claimed; the host cannot.
            var claimed = session.Players.FirstOrDefault(p => p.Id == claimId);
            if (claimed is null) return Results.NotFound();
            if (claimed.IsHost) return Forbidden();
            // One device controls one seat: orphan any other player this device currently held.
            foreach (var other in session.Players.Where(p => p.Id != claimed.Id && p.DeviceToken == deviceToken))
                other.DeviceToken = Guid.NewGuid().ToString("N");
            claimed.DeviceToken = deviceToken;
            if (!string.IsNullOrWhiteSpace(req.Name)) claimed.Name = Clamp(req.Name);
            target = claimed;
        }
        else
        {
            var existing = session.Players.FirstOrDefault(p => p.DeviceToken == deviceToken);
            if (existing is not null)
            {
                if (!string.IsNullOrWhiteSpace(req.Name)) existing.Name = Clamp(req.Name);
                if (req.FactionId is not null) existing.FactionId = ClampId(req.FactionId);
                if (req.ColorHex is not null) existing.ColorHex = SanitizeColor(req.ColorHex, existing.ColorHex);
                target = existing;
            }
            else
            {
                // A brand-new seat — capped at 8 players. The join UI hides "create" at 8, so this is
                // only a backstop (a 400 the client turns into a refresh, never a hard error).
                if (session.Players.Count >= 8)
                    return Results.BadRequest(new { error = "This session already has 8 players." });
                target = new Player
                {
                    SessionId = session.Id,
                    Name = string.IsNullOrWhiteSpace(req.Name) ? "Player" : Clamp(req.Name),
                    FactionId = ClampId(req.FactionId),
                    ColorHex = SanitizeColor(req.ColorHex),
                    SeatOrder = session.Players.Count == 0 ? 0 : session.Players.Max(p => p.SeatOrder) + 1,
                    DeviceToken = deviceToken,
                };
                session.Players.Add(target);
                Log(db, session, SessionLogKind.PlayerJoin, target.Id, target.Id);
            }
        }

        session.LastActivityUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        await hub.NotifySessionChanged(session.JoinCode);

        var overrides = FactionInitiative.Overrides;
        return Results.Ok(new JoinResultDto(session.ToDto(overrides), target.Id, deviceToken));
    }

    private static async Task<IResult> UpdatePlayer(Guid id, Guid playerId, UpdatePlayerRequest req, Ti4DbContext db, MasterDbContext master, IHubContext<SessionHub> hub, HttpContext http, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        var player = session?.Players.FirstOrDefault(p => p.Id == playerId);
        if (session is null || player is null) return Results.NotFound();
        if (!CallerCanActFor(session, http, playerId)) return Forbidden();           // edit own profile, or host edits anyone
        if (req.SeatOrder is not null && !CallerIsHost(session, http)) return Forbidden(); // seat order = host only

        if (req.Name is not null) player.Name = Clamp(req.Name);
        if (req.ColorHex is not null) player.ColorHex = SanitizeColor(req.ColorHex, player.ColorHex);
        if (req.HasPassed is not null) player.HasPassed = req.HasPassed.Value;
        if (req.IsReady is not null) player.IsReady = req.IsReady.Value;
        if (req.SeatOrder is not null) player.SeatOrder = req.SeatOrder.Value;
        if (req.FactionId is not null)
        {
            var newFaction = string.IsNullOrWhiteSpace(req.FactionId) ? null : ClampId(req.FactionId);
            if (newFaction != player.FactionId)
            {
                var oldFaction = player.FactionId;
                player.FactionId = newFaction;
                await UpdateStartingTechnologiesAsync(master, session, player, oldFaction, newFaction, ct);
            }
        }

        return await SaveAndReturn(db, hub, session, ct);
    }

    private static async Task<IResult> RemovePlayer(Guid id, Guid playerId, Ti4DbContext db, IHubContext<SessionHub> hub, HttpContext http, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        var player = session?.Players.FirstOrDefault(p => p.Id == playerId);
        if (session is null || player is null) return Results.NotFound();
        // Host removes anyone; a non-host may remove only themselves (leave).
        if (!CallerCanActFor(session, http, playerId)) return Forbidden();

        session.Players.Remove(player);
        if (session.ActivePlayerId == playerId) session.ActivePlayerId = null;
        if (session.SpeakerPlayerId == playerId) session.SpeakerPlayerId = null;

        return await SaveAndReturn(db, hub, session, ct);
    }

    private static async Task<IResult> SetPassed(Guid id, Guid playerId, SetPassedRequest req, Ti4DbContext db, IHubContext<SessionHub> hub, HttpContext http, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        var player = session?.Players.FirstOrDefault(p => p.Id == playerId);
        if (session is null || player is null) return Results.NotFound();
        if (!CallerCanActFor(session, http, playerId)) return Forbidden();

        if (req.Passed)
        {
            // A player may only pass once they have performed all of their strategy actions.
            if (player.StrategyCards.Count > 0 && player.StrategyCards.Any(c => !c.IsExhausted))
                return Results.BadRequest(new { error = "Play all strategy actions before passing." });

            player.HasPassed = true;
            Log(db, session, http, SessionLogKind.Pass, target: playerId);
            var overrides = FactionInitiative.Overrides;
            session.ActivePlayerId = TurnService.NextActiveAfter(session, overrides, playerId);
            session.ActiveStrategyCardId = null; // a new turn begins → close any played-action highlight
            if (session.ActivePlayerId is Guid nxt) Log(db, session, http, SessionLogKind.TurnChange, target: nxt);
            return await SaveAndReturn(db, hub, session, ct, overrides);
        }

        player.HasPassed = false;
        return await SaveAndReturn(db, hub, session, ct);
    }

    // -----------------------------------------------------------------------
    // Strategy cards
    // -----------------------------------------------------------------------

    private static async Task<IResult> AssignStrategyCard(Guid id, Guid playerId, AssignStrategyCardRequest req, Ti4DbContext db, IHubContext<SessionHub> hub, HttpContext http, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        var player = session?.Players.FirstOrDefault(p => p.Id == playerId);
        if (session is null || player is null) return Results.NotFound();

        var maxCards = GameRules.StrategyCardsPerPlayer(session.Players.Count, session.StrategyCardsPerPlayer);
        // Pick order: speaker first, then clockwise by seat. A non-host may only take their own card
        // and only when it's their turn to pick; the host may pick for anyone.
        if (!CallerIsHost(session, http))
        {
            var caller = GetCaller(session, http);
            if (caller is null || caller.Id != playerId || TurnService.CurrentPicker(session, maxCards) != playerId)
                return Forbidden();
        }
        if (player.StrategyCards.Count >= maxCards && player.StrategyCards.All(c => c.StrategyCardId != req.StrategyCardId))
            return Results.BadRequest(new { error = $"Each player may pick at most {maxCards} strategy card(s)." });

        // Each strategy card belongs to a single player: remove it from anyone else first.
        foreach (var other in session.Players) other.StrategyCards.RemoveAll(c => c.StrategyCardId == req.StrategyCardId);

        player.StrategyCards.Add(new PlayerStrategyCard { SessionId = session.Id, PlayerId = player.Id, StrategyCardId = req.StrategyCardId });
        Log(db, session, http, SessionLogKind.StrategyPick, target: player.Id, detail: req.StrategyCardId.ToString());

        // Accumulated trade goods stay on the card until the action phase begins (see StartActionPhase),
        // so picking and then returning a card no longer discards them.
        return await SaveAndReturn(db, hub, session, ct);
    }

    private static async Task<IResult> UnassignStrategyCard(Guid id, Guid playerId, int cardId, Ti4DbContext db, IHubContext<SessionHub> hub, HttpContext http, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        var player = session?.Players.FirstOrDefault(p => p.Id == playerId);
        if (session is null || player is null) return Results.NotFound();
        if (!CallerCanActFor(session, http, playerId)) return Forbidden();
        player.StrategyCards.RemoveAll(c => c.StrategyCardId == cardId);
        Log(db, session, http, SessionLogKind.StrategyReturn, target: playerId, detail: cardId.ToString());
        return await SaveAndReturn(db, hub, session, ct);
    }

    private static async Task<IResult> SetStrategyCardUsed(Guid id, Guid playerId, int cardId, SetStrategyCardUsedRequest req, Ti4DbContext db, IHubContext<SessionHub> hub, HttpContext http, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        var player = session?.Players.FirstOrDefault(p => p.Id == playerId);
        var card = player?.StrategyCards.FirstOrDefault(c => c.StrategyCardId == cardId);
        if (session is null || player is null || card is null) return Results.NotFound();
        if (!CallerCanActFor(session, http, playerId)) return Forbidden(); // play your own action; host may play for others
        card.IsExhausted = req.Used;
        if (req.Used) Log(db, session, http, SessionLogKind.StrategyAction, target: playerId, detail: cardId.ToString());
        return await SaveAndReturn(db, hub, session, ct);
    }

    // -----------------------------------------------------------------------
    // Objectives
    // -----------------------------------------------------------------------

    private static async Task<IResult> RevealObjective(Guid id, RevealObjectiveRequest req, Ti4DbContext db, IHubContext<SessionHub> hub, HttpContext http, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        if (session is null) return Results.NotFound();
        if (!CallerIsHost(session, http)) return Forbidden();
        var objectiveId = ClampId(req.ObjectiveId) ?? "";
        if (!session.Objectives.Any(o => o.ObjectiveId == objectiveId))
        {
            session.Objectives.Add(new SessionObjective { SessionId = session.Id, ObjectiveId = objectiveId });
            Log(db, session, http, SessionLogKind.ObjectiveReveal, detail: objectiveId);
        }
        return await SaveAndReturn(db, hub, session, ct);
    }

    // Add a hand-entered objective (e.g. a scored secret made public). Host only.
    private static async Task<IResult> RevealCustomObjective(Guid id, RevealCustomObjectiveRequest req, Ti4DbContext db, IHubContext<SessionHub> hub, HttpContext http, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        if (session is null) return Results.NotFound();
        if (!CallerIsHost(session, http)) return Forbidden();
        if (string.IsNullOrWhiteSpace(req.Name)) return Results.BadRequest(new { error = "Name required." });
        session.Objectives.Add(new SessionObjective
        {
            SessionId = session.Id,
            ObjectiveId = "",
            CustomName = Clamp(req.Name, 100),
            CustomPoints = Math.Clamp(req.Points, 0, 10),
        });
        Log(db, session, http, SessionLogKind.ObjectiveReveal, detail: Clamp(req.Name, 100));
        return await SaveAndReturn(db, hub, session, ct);
    }

    private static async Task<IResult> RemoveObjective(Guid id, Guid sessionObjectiveId, Ti4DbContext db, IHubContext<SessionHub> hub, HttpContext http, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        var obj = session?.Objectives.FirstOrDefault(o => o.Id == sessionObjectiveId);
        if (session is null || obj is null) return Results.NotFound();
        if (!CallerIsHost(session, http)) return Forbidden();
        session.Objectives.Remove(obj);
        return await SaveAndReturn(db, hub, session, ct);
    }

    // Set the whole seat order at once (host). Players missing from the list keep their relative order
    // after the listed ones, so a stale client can't drop somebody off the table.
    private static async Task<IResult> SetSeatOrder(Guid id, SetSeatOrderRequest req, Ti4DbContext db, IHubContext<SessionHub> hub, HttpContext http, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        if (session is null) return Results.NotFound();
        if (!CallerIsHost(session, http)) return Forbidden();

        var seat = 0;
        foreach (var pid in req.PlayerIds.Distinct())
        {
            var p = session.Players.FirstOrDefault(x => x.Id == pid);
            if (p is not null) p.SeatOrder = seat++;
        }
        foreach (var p in session.Players.Where(p => !req.PlayerIds.Contains(p.Id)).OrderBy(p => p.SeatOrder))
            p.SeatOrder = seat++;

        return await SaveAndReturn(db, hub, session, ct);
    }

    // Status phase: mark a player done scoring so the turn moves to the next initiative. Self or host,
    // and reversible — someone who clicked too early gets their turn back.
    private static async Task<IResult> SetStatusDone(Guid id, Guid playerId, SetStatusDoneRequest req, Ti4DbContext db, IHubContext<SessionHub> hub, HttpContext http, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        var player = session?.Players.FirstOrDefault(p => p.Id == playerId);
        if (session is null || player is null) return Results.NotFound();
        if (!CallerIsHost(session, http) && GetCaller(session, http)?.Id != playerId) return Forbidden();
        player.StatusDone = req.Done;
        return await SaveAndReturn(db, hub, session, ct);
    }

    // Status phase: tick one of the shared post-scoring steps. Open to any device — it's a checklist on
    // the table, and whoever does the step should be able to tick it.
    private static async Task<IResult> SetStatusStep(Guid id, SetStatusStepRequest req, Ti4DbContext db, IHubContext<SessionHub> hub, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        if (session is null) return Results.NotFound();
        session.StatusStepsDone = req.Done
            ? session.StatusStepsDone | req.Step
            : session.StatusStepsDone & ~req.Step;
        return await SaveAndReturn(db, hub, session, ct);
    }

    // Red Tape variant: take the marker off an objective, or put it back. Open to any device like
    // scoring — it's a token on the table, not a privileged action.
    private static async Task<IResult> SetObjectiveMarker(Guid id, Guid sessionObjectiveId, SetObjectiveMarkerRequest req, Ti4DbContext db, IHubContext<SessionHub> hub, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        var obj = session?.Objectives.FirstOrDefault(o => o.Id == sessionObjectiveId);
        if (session is null || obj is null) return Results.NotFound();
        obj.MarkerRemoved = req.Removed;
        return await SaveAndReturn(db, hub, session, ct);
    }

    private static async Task<IResult> ScoreObjective(Guid id, Guid sessionObjectiveId, ScoreObjectiveRequest req, Ti4DbContext db, IHubContext<SessionHub> hub, HttpContext http, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        var obj = session?.Objectives.FirstOrDefault(o => o.Id == sessionObjectiveId);
        if (session is null || obj is null) return Results.NotFound();
        // Scoring is open to any device, but the scorer must be a real player in THIS session —
        // otherwise an anonymous caller could insert unbounded score rows with random GUIDs.
        if (session.Players.All(p => p.Id != req.PlayerId)) return Results.NotFound();
        // In the STATUS phase scoring runs in initiative order: only the player whose turn it is may
        // score (the host may act for anyone). Outside that phase — during the action phase, or an
        // ability that scores at another time — it stays open as before.
        if (session.Phase == GamePhase.Status && !CallerIsHost(session, http)
            && TurnService.CurrentScorer(session, FactionInitiative.Overrides) != req.PlayerId)
        {
            return Forbidden();
        }
        if (!obj.Scores.Any(s => s.PlayerId == req.PlayerId))
        {
            obj.Scores.Add(new ObjectiveScore { SessionObjectiveId = obj.Id, PlayerId = req.PlayerId });
            Log(db, session, http, SessionLogKind.ObjectiveScore, target: req.PlayerId,
                detail: string.IsNullOrEmpty(obj.ObjectiveId) ? obj.CustomName : obj.ObjectiveId);
        }
        return await SaveAndReturn(db, hub, session, ct);
    }

    private static async Task<IResult> UnscoreObjective(Guid id, Guid sessionObjectiveId, Guid playerId, Ti4DbContext db, IHubContext<SessionHub> hub, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        var obj = session?.Objectives.FirstOrDefault(o => o.Id == sessionObjectiveId);
        if (session is null || obj is null) return Results.NotFound();
        obj.Scores.RemoveAll(s => s.PlayerId == playerId);
        return await SaveAndReturn(db, hub, session, ct);
    }

    // -----------------------------------------------------------------------
    // Technologies
    // -----------------------------------------------------------------------

    private static async Task<IResult> AddTechnology(Guid id, Guid playerId, AddTechnologyRequest req, Ti4DbContext db, IHubContext<SessionHub> hub, HttpContext http, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        var player = session?.Players.FirstOrDefault(p => p.Id == playerId);
        if (session is null || player is null) return Results.NotFound();
        if (!CallerCanActFor(session, http, playerId)) return Forbidden();
        var techId = ClampId(req.TechnologyId);
        if (!string.IsNullOrEmpty(techId) && !player.Technologies.Any(t => t.TechnologyId == techId))
        {
            player.Technologies.Add(new PlayerTechnology { SessionId = session.Id, PlayerId = player.Id, TechnologyId = techId });
            Log(db, session, http, SessionLogKind.TechAdd, target: playerId, detail: techId);
        }
        return await SaveAndReturn(db, hub, session, ct);
    }

    private static async Task<IResult> RemoveTechnology(Guid id, Guid playerId, string techId, Ti4DbContext db, IHubContext<SessionHub> hub, HttpContext http, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        var player = session?.Players.FirstOrDefault(p => p.Id == playerId);
        if (session is null || player is null) return Results.NotFound();
        if (!CallerCanActFor(session, http, playerId)) return Forbidden();
        player.Technologies.RemoveAll(t => t.TechnologyId == techId);
        Log(db, session, http, SessionLogKind.TechRemove, target: playerId, detail: techId);
        return await SaveAndReturn(db, hub, session, ct);
    }

    // -----------------------------------------------------------------------
    // Agenda phase
    // -----------------------------------------------------------------------

    // Reveal an agenda (or clear it with a null id for "reveal a new agenda"). Host only. Leaving an
    // agenda that was voted on first spends each player's locked votes from their entered influence
    // (min 0), then resets to the influence-entry stage for the next agenda.
    private static async Task<IResult> SetAgenda(Guid id, SetAgendaRequest req, Ti4DbContext db, IHubContext<SessionHub> hub, HttpContext http, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        if (session is null) return Results.NotFound();
        if (!CallerIsHost(session, http)) return Forbidden();
        foreach (var v in session.AgendaVotes.Where(v => v.Votes > 0))
        {
            var p = session.Players.FirstOrDefault(pl => pl.Id == v.PlayerId);
            if (p is not null) p.Influence = Math.Max(0, p.Influence - v.Votes);
        }
        LogAgendaResult(db, session);   // summarise the concluding agenda before its votes are cleared
        session.CurrentAgendaId = ClampId(req.AgendaId);
        session.AgendaVotes.Clear();
        session.VotingStarted = false;     // back to influence entry until the host starts the vote
        session.AgendaVotesHidden = false;
        Log(db, session, http, SessionLogKind.AgendaReveal, detail: session.CurrentAgendaId ?? "");
        return await SaveAndReturn(db, hub, session, ct);
    }

    // Host opens the vote on the revealed agenda (open or face-down). Influence then locks.
    private static async Task<IResult> StartVoting(Guid id, StartVotingRequest req, Ti4DbContext db, IHubContext<SessionHub> hub, HttpContext http, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        if (session is null) return Results.NotFound();
        if (!CallerIsHost(session, http)) return Forbidden();
        if (session.CurrentAgendaId is null) return Results.BadRequest(new { error = "Reveal an agenda first." });
        session.VotingStarted = true;
        session.AgendaVotesHidden = req.Hidden;
        session.AgendaTotalsRevealed = false;
        session.AgendaVotes.Clear();
        Log(db, session, http, SessionLogKind.AgendaStartVote, detail: req.Hidden ? "hidden" : "open");
        return await SaveAndReturn(db, hub, session, ct);
    }

    // Host aborts the vote → back to influence entry (no influence is spent). Host only.
    private static async Task<IResult> CancelVoting(Guid id, Ti4DbContext db, IHubContext<SessionHub> hub, HttpContext http, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        if (session is null) return Results.NotFound();
        if (!CallerIsHost(session, http)) return Forbidden();
        session.VotingStarted = false;
        session.AgendaVotesHidden = false;
        session.AgendaTotalsRevealed = false;
        session.AgendaVotes.Clear();
        Log(db, session, http, SessionLogKind.AgendaCancel);
        return await SaveAndReturn(db, hub, session, ct);
    }

    // Intermediate step for a face-down vote: publish the TOTALS while keeping the attribution hidden
    // (Galactic Event / hidden agenda). Deliberately its own route rather than a second meaning for
    // /reveal — a double tap must not skip straight to the full reveal. Host only.
    private static async Task<IResult> RevealVoteTotals(Guid id, Ti4DbContext db, IHubContext<SessionHub> hub, HttpContext http, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        if (session is null) return Results.NotFound();
        if (!CallerIsHost(session, http)) return Forbidden();
        if (!AllVotesLocked(session))
            return Results.BadRequest(new { error = "All players must lock their vote first." });
        session.AgendaTotalsRevealed = true;
        Log(db, session, http, SessionLogKind.AgendaRevealTotals);
        return await SaveAndReturn(db, hub, session, ct);
    }

    /// <summary>Every player has a locked vote — the gate for both reveal steps.</summary>
    private static bool AllVotesLocked(GameSession session)
        => session.Players.Count > 0
           && session.Players.All(p => session.AgendaVotes.Any(v => v.PlayerId == p.Id && v.Locked));

    // Host flips a face-down vote face-up (reveal). Host only.
    private static async Task<IResult> RevealVotes(Guid id, Ti4DbContext db, IHubContext<SessionHub> hub, HttpContext http, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        if (session is null) return Results.NotFound();
        if (!CallerIsHost(session, http)) return Forbidden();
        // Same gate the client UI applies: a face-down vote is only flipped once EVERY player has
        // locked — otherwise late voters would cast into an already-open vote (secrecy broken).
        if (!AllVotesLocked(session))
            return Results.BadRequest(new { error = "All players must lock their vote first." });
        session.AgendaVotesHidden = false;
        session.AgendaTotalsRevealed = false; // fully open now, the intermediate step is over
        Log(db, session, http, SessionLogKind.AgendaReveal2);
        return await SaveAndReturn(db, hub, session, ct);
    }

    // Commit a vote (set + lock) atomically — used for both open and face-down voting. The choice
    // reaches the server only here, on lock, so a face-down vote can't be peeked beforehand. Self or
    // host; a locked vote can't change until the host cancels the vote. A vote counts only once locked.
    private static async Task<IResult> LockVote(Guid id, LockVoteRequest req, Ti4DbContext db, IHubContext<SessionHub> hub, HttpContext http, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        if (session is null) return Results.NotFound();
        if (!session.VotingStarted) return Results.BadRequest(new { error = "Voting has not started." });
        if (!CallerCanActFor(session, http, req.PlayerId)) return Forbidden();
        var vote = session.AgendaVotes.FirstOrDefault(v => v.PlayerId == req.PlayerId);
        if (vote?.Locked == true) return Forbidden();
        if (vote is null)
        {
            vote = new AgendaVote { SessionId = session.Id, PlayerId = req.PlayerId };
            session.AgendaVotes.Add(vote);
        }
        vote.Outcome = req.Outcome;
        vote.Votes = Math.Max(0, req.Votes);
        vote.Choice = req.Outcome == VoteOutcome.Abstain ? null : (string.IsNullOrWhiteSpace(req.Choice) ? null : Clamp(req.Choice, 100));
        vote.Locked = true;
        // Individual vote inputs are intentionally NOT logged — the agenda log shows only the reveal and a
        // single summarised result (see LogAgendaResult, emitted when the agenda concludes).
        return await SaveAndReturn(db, hub, session, ct);
    }

    // A player's available influence for the agenda phase. Self or host; only before voting starts.
    private static async Task<IResult> SetInfluence(Guid id, Guid playerId, SetInfluenceRequest req, Ti4DbContext db, IHubContext<SessionHub> hub, HttpContext http, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        var player = session?.Players.FirstOrDefault(p => p.Id == playerId);
        if (session is null || player is null) return Results.NotFound();
        if (!CallerCanActFor(session, http, playerId)) return Forbidden();
        if (session.VotingStarted) return Results.BadRequest(new { error = "Voting already started." });
        player.Influence = Math.Clamp(req.Influence, 0, 999);
        // Per-change influence is not logged (it was pure noise) — the agenda log keeps only reveal + result.
        return await SaveAndReturn(db, hub, session, ct);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    // -----------------------------------------------------------------------
    // Authorization (lightweight: the client sends its device token as a header)
    // -----------------------------------------------------------------------
    private const string DeviceTokenHeader = "X-Device-Token";

    private static Player? GetCaller(GameSession s, HttpContext http)
    {
        var token = http.Request.Headers[DeviceTokenHeader].ToString();
        return string.IsNullOrEmpty(token) ? null : s.Players.FirstOrDefault(p => p.DeviceToken == token);
    }

    /// <summary>The caller is the host (creator). Falls back to lowest-seat for legacy sessions with no host flag.</summary>
    private static bool CallerIsHost(GameSession s, HttpContext http)
    {
        var caller = GetCaller(s, http);
        if (caller is null) return false;
        return s.Players.Any(p => p.IsHost)
            ? caller.IsHost
            : s.Players.OrderBy(p => p.SeatOrder).FirstOrDefault()?.Id == caller.Id;
    }

    /// <summary>The host may act for anyone; everyone else only for themselves.</summary>
    private static bool CallerCanActFor(GameSession s, HttpContext http, Guid playerId)
        => CallerIsHost(s, http) || GetCaller(s, http)?.Id == playerId;

    private static IResult Forbidden() =>
        Results.Json(new { error = "Not allowed — host only." }, statusCode: StatusCodes.Status403Forbidden);

    // -----------------------------------------------------------------------
    // Match log — append a structured event; persisted by the next SaveChangesAsync (SaveAndReturn).
    // -----------------------------------------------------------------------
    private static void Log(Ti4DbContext db, GameSession s, SessionLogKind kind, Guid? actor,
        Guid? target = null, GamePhase? phase = null, int? round = null, string? detail = null)
        => db.SessionLog.Add(new SessionLogEntry
        {
            SessionId = s.Id, Kind = kind, ActorPlayerId = actor, TargetPlayerId = target,
            Phase = phase, Round = round, Detail = detail,
        });

    private static void Log(Ti4DbContext db, GameSession s, HttpContext http, SessionLogKind kind,
        Guid? target = null, GamePhase? phase = null, int? round = null, string? detail = null)
        => Log(db, s, kind, GetCaller(s, http)?.Id, target, phase, round, detail);

    // One summary entry for a concluding agenda (no per-vote logging). Detail packs the For/Against tally
    // and the leading elect candidate so the client can render either kind; see SessionLogKind.AgendaResult.
    private static void LogAgendaResult(Ti4DbContext db, GameSession session)
    {
        if (session.CurrentAgendaId is null) return;
        var locked = session.AgendaVotes.Where(v => v.Locked).ToList();
        if (locked.Count == 0) return;
        var forVotes = locked.Where(v => v.Outcome == VoteOutcome.For).Sum(v => v.Votes);
        var againstVotes = locked.Where(v => v.Outcome == VoteOutcome.Against).Sum(v => v.Votes);
        var tally = locked.Where(v => !string.IsNullOrEmpty(v.Choice) && v.Votes > 0)
            .GroupBy(v => v.Choice!).Select(g => new { Key = g.Key, Votes = g.Sum(v => v.Votes) })
            .OrderByDescending(t => t.Votes).ToList();
        var topKey = (tally.Count > 0 ? tally[0].Key : "").Replace("|", "/");
        var topVotes = tally.Count > 0 ? tally[0].Votes : 0;
        var runnerUp = tally.Count > 1 ? tally[1].Votes : 0;
        var detail = $"{session.CurrentAgendaId}|{forVotes}|{againstVotes}|{topKey}|{topVotes}|{runnerUp}";
        Log(db, session, SessionLogKind.AgendaResult, null, detail: detail);
    }

    private static IQueryable<GameSession> WithGraph(this Ti4DbContext db) =>
        db.Sessions
          .Include(s => s.Players).ThenInclude(p => p.StrategyCards)
          .Include(s => s.Players).ThenInclude(p => p.Technologies)
          .Include(s => s.Objectives).ThenInclude(o => o.Scores)
          .Include(s => s.StrategyCardStates)
          .Include(s => s.AgendaVotes);

    private static Task<GameSession?> LoadGraphAsync(Ti4DbContext db, Guid id, CancellationToken ct) =>
        db.WithGraph().FirstOrDefaultAsync(s => s.Id == id, ct);

    private static Task<GameSession?> LoadGraphByCodeAsync(Ti4DbContext db, string code, CancellationToken ct) =>
        db.WithGraph().FirstOrDefaultAsync(s => s.JoinCode == code, ct);

    private static StrategyCardState GetOrCreateCardState(GameSession session, int cardId)
    {
        var state = session.StrategyCardStates.FirstOrDefault(s => s.StrategyCardId == cardId);
        if (state is null)
        {
            state = new StrategyCardState { SessionId = session.Id, StrategyCardId = cardId };
            session.StrategyCardStates.Add(state);
        }
        return state;
    }

    /// <summary>
    /// Reconciles a player's starting technologies when their faction changes: removes the previous
    /// faction's fixed starting techs (so switching factions doesn't accumulate extras — e.g. picking
    /// the Titans then another faction) and adds the new faction's. Techs shared by both are re-added.
    /// </summary>
    private static async Task UpdateStartingTechnologiesAsync(MasterDbContext master, GameSession session, Player player, string? oldFactionId, string? newFactionId, CancellationToken ct)
    {
        if (oldFactionId is not null)
        {
            var oldFaction = await LatestFactionAsync(master, oldFactionId, ct);
            if (oldFaction is not null)
                foreach (var techId in oldFaction.StartingTechnologies)
                    player.Technologies.RemoveAll(t => t.TechnologyId == techId);
        }

        if (newFactionId is null) return;
        var newFaction = await LatestFactionAsync(master, newFactionId, ct);
        if (newFaction is null) return;
        foreach (var techId in newFaction.StartingTechnologies)
            if (!player.Technologies.Any(t => t.TechnologyId == techId))
                player.Technologies.Add(new PlayerTechnology { SessionId = session.Id, PlayerId = player.Id, TechnologyId = techId });
    }

    /// <summary>The newest revision of a faction by its logical slug (factions are keyed by a surrogate
    /// Guid in the master DB and may carry historical revisions).</summary>
    private static async Task<Faction?> LatestFactionAsync(MasterDbContext master, string slug, CancellationToken ct) =>
        (await master.Factions.AsNoTracking().Where(f => f.Slug == slug).ToListAsync(ct))
            .OrderByDescending(f => f.Version).FirstOrDefault();

    private static async Task<IResult> SaveAndReturn(Ti4DbContext db, IHubContext<SessionHub> hub, GameSession session, CancellationToken ct, IReadOnlyDictionary<string, int?>? overrides = null)
    {
        session.LastActivityUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        await hub.NotifySessionChanged(session.JoinCode);
        overrides ??= FactionInitiative.Overrides;
        return Results.Ok(session.ToDto(overrides));
    }

    private static async Task<string> UniqueCodeAsync(Ti4DbContext db, CancellationToken ct)
    {
        const string alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
        while (true)
        {
            var code = string.Create(5, alphabet, static (span, abc) =>
            {
                for (var i = 0; i < span.Length; i++) span[i] = abc[Random.Shared.Next(abc.Length)];
            });
            if (!await db.Sessions.AnyAsync(s => s.JoinCode == code, ct)) return code;
        }
    }
}
