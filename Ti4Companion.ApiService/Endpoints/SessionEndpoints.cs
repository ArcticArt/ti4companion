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
        g.MapPost("/", CreateSession);
        g.MapGet("/{code}", GetByCode);
        g.MapPatch("/{id:guid}", UpdateSession);
        g.MapDelete("/{id:guid}", DeleteSession);
        g.MapPost("/{id:guid}/display", SetDisplayMode);          // wall-display view switch (any player)

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

        // ---- Strategy cards (per player) ----
        g.MapPost("/{id:guid}/players/{playerId:guid}/strategy-cards", AssignStrategyCard);
        g.MapDelete("/{id:guid}/players/{playerId:guid}/strategy-cards/{cardId:int}", UnassignStrategyCard);
        g.MapPost("/{id:guid}/players/{playerId:guid}/strategy-cards/{cardId:int}/used", SetStrategyCardUsed);

        // ---- Objectives ----
        g.MapPost("/{id:guid}/objectives", RevealObjective);
        g.MapPost("/{id:guid}/objectives/custom", RevealCustomObjective); // secret made public / hand-added
        g.MapDelete("/{id:guid}/objectives/{sessionObjectiveId:guid}", RemoveObjective);
        g.MapPost("/{id:guid}/objectives/{sessionObjectiveId:guid}/scores", ScoreObjective);
        g.MapDelete("/{id:guid}/objectives/{sessionObjectiveId:guid}/scores/{playerId:guid}", UnscoreObjective);

        // ---- Technologies (per player) ----
        g.MapPost("/{id:guid}/players/{playerId:guid}/technologies", AddTechnology);
        g.MapDelete("/{id:guid}/players/{playerId:guid}/technologies/{techId}", RemoveTechnology);

        // ---- Agenda phase ----
        g.MapPost("/{id:guid}/agenda", SetAgenda);
        g.MapPost("/{id:guid}/agenda/vote", CastVote);
        g.MapPost("/{id:guid}/agenda/lock", LockVote);   // secret voting: commit a vote (host)
        g.MapPost("/{id:guid}/agenda/reset", ResetVotes); // clear all votes (host)

        return app;
    }

    // -----------------------------------------------------------------------
    // Lifecycle
    // -----------------------------------------------------------------------

    private static async Task<IResult> CreateSession(CreateSessionRequest req, Ti4DbContext db, IHubContext<SessionHub> hub, IConfiguration config, CancellationToken ct)
    {
        var deviceToken = string.IsNullOrWhiteSpace(req.DeviceToken) ? Guid.NewGuid().ToString("N") : req.DeviceToken;
        var session = new GameSession
        {
            JoinCode = await UniqueCodeAsync(db, ct),
            Name = string.IsNullOrWhiteSpace(req.Name) ? "Twilight Imperium" : req.Name.Trim(),
            DefaultLanguage = req.Language,
            ActiveExpansions = (req.ActiveExpansions ?? AllExpansions) | Expansion.Base,
            RetentionHours = config.GetValue("Ti4:DefaultRetentionHours", 168),
            Phase = GamePhase.Setup,
        };

        var host = new Player
        {
            SessionId = session.Id,
            Name = string.IsNullOrWhiteSpace(req.HostName) ? "Host" : req.HostName.Trim(),
            FactionId = req.FactionId,
            ColorHex = req.ColorHex ?? "#cccccc",
            SeatOrder = 0,
            IsHost = true,
            DeviceToken = deviceToken,
        };
        session.Players.Add(host);

        db.Sessions.Add(session);
        await db.SaveChangesAsync(ct);

        var overrides = await GetFactionOverridesAsync(db, ct);
        var state = (await LoadGraphAsync(db, session.Id, ct))!.ToDto(overrides);
        return Results.Ok(new JoinResultDto(state, host.Id, deviceToken));
    }

    private static async Task<IResult> GetByCode(string code, Ti4DbContext db, CancellationToken ct)
    {
        var session = await LoadGraphByCodeAsync(db, SessionHub.Normalize(code), ct);
        if (session is null) return Results.NotFound();
        var overrides = await GetFactionOverridesAsync(db, ct);
        return Results.Ok(session.ToDto(overrides));
    }

    private static async Task<IResult> UpdateSession(Guid id, UpdateSessionRequest req, Ti4DbContext db, IHubContext<SessionHub> hub, HttpContext http, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        if (session is null) return Results.NotFound();
        if (!CallerIsHost(session, http)) return Forbidden(); // settings, phase, speaker → host only

        if (req.Name is not null) session.Name = req.Name.Trim();
        if (req.Language is not null) session.DefaultLanguage = req.Language.Value;
        if (req.ActiveExpansions is not null) session.ActiveExpansions = req.ActiveExpansions.Value | Expansion.Base;
        if (req.ShowTechOverview is not null) session.ShowTechOverview = req.ShowTechOverview.Value;
        if (req.AllowEditAllPlayers is not null) session.AllowEditAllPlayers = req.AllowEditAllPlayers.Value;
        if (req.Phase is not null) session.Phase = req.Phase.Value;
        if (req.CurrentRound is > 0) session.CurrentRound = req.CurrentRound.Value;
        if (req.SpeakerPlayerId is not null) session.SpeakerPlayerId = req.SpeakerPlayerId;
        if (req.AgendaVotesHidden is not null) session.AgendaVotesHidden = req.AgendaVotesHidden.Value;
        if (req.VotingOrderReversed is not null) session.VotingOrderReversed = req.VotingOrderReversed.Value;

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
        return await SaveAndReturn(db, hub, session, ct);
    }

    private static async Task<IResult> StartActionPhase(Guid id, Ti4DbContext db, IHubContext<SessionHub> hub, HttpContext http, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        if (session is null) return Results.NotFound();
        if (!CallerIsHost(session, http)) return Forbidden();

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

        var overrides = await GetFactionOverridesAsync(db, ct);
        session.Phase = GamePhase.Action;
        session.ActiveStrategyCardId = null;
        session.ActivePlayerId = TurnService.FirstActive(session, overrides);
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
        return await SaveAndReturn(db, hub, session, ct);
    }

    private static async Task<IResult> StartAgendaPhase(Guid id, Ti4DbContext db, IHubContext<SessionHub> hub, HttpContext http, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        if (session is null) return Results.NotFound();
        if (!CallerIsHost(session, http)) return Forbidden();
        session.Phase = GamePhase.Agenda;
        session.CurrentAgendaId = null;
        session.AgendaVotes.Clear();
        session.VotingOrderReversed = false;
        foreach (var p in session.Players) p.Influence = 0; // players record their influence fresh each agenda phase
        return await SaveAndReturn(db, hub, session, ct);
    }

    private static async Task<IResult> NextRound(Guid id, Ti4DbContext db, IHubContext<SessionHub> hub, HttpContext http, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        if (session is null) return Results.NotFound();
        if (!CallerIsHost(session, http)) return Forbidden();

        session.CurrentRound += 1;
        session.Phase = GamePhase.Strategy;
        session.ActivePlayerId = null;
        session.ActiveStrategyCardId = null;
        session.CurrentAgendaId = null;
        session.AgendaVotes.Clear();
        session.VotingOrderReversed = false;
        foreach (var p in session.Players)
        {
            p.HasPassed = false;
            p.Influence = 0;
            p.StrategyCards.Clear();
        }

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
        return await SaveAndReturn(db, hub, session, ct);
    }

    private static async Task<IResult> AdvanceTurn(Guid id, Ti4DbContext db, IHubContext<SessionHub> hub, HttpContext http, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        if (session is null) return Results.NotFound();
        if (!CallerCanActFor(session, http, session.ActivePlayerId ?? Guid.Empty)) return Forbidden();
        var overrides = await GetFactionOverridesAsync(db, ct);
        session.ActivePlayerId = TurnService.NextActive(session, overrides);
        session.ActiveStrategyCardId = null; // a new turn begins → close any played-action highlight
        return await SaveAndReturn(db, hub, session, ct, overrides);
    }

    private static async Task<IResult> PreviousTurn(Guid id, Ti4DbContext db, IHubContext<SessionHub> hub, HttpContext http, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        if (session is null) return Results.NotFound();
        if (!CallerCanActFor(session, http, session.ActivePlayerId ?? Guid.Empty)) return Forbidden();
        var overrides = await GetFactionOverridesAsync(db, ct);
        session.ActivePlayerId = TurnService.PreviousActive(session, overrides);
        session.ActiveStrategyCardId = null; // a new turn begins → close any played-action highlight
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

        var existing = session.Players.FirstOrDefault(p => p.DeviceToken == deviceToken);
        if (existing is not null)
        {
            if (!string.IsNullOrWhiteSpace(req.Name)) existing.Name = req.Name.Trim();
            if (req.FactionId is not null) existing.FactionId = req.FactionId;
            if (req.ColorHex is not null) existing.ColorHex = req.ColorHex;
        }
        else
        {
            existing = new Player
            {
                SessionId = session.Id,
                Name = string.IsNullOrWhiteSpace(req.Name) ? "Player" : req.Name.Trim(),
                FactionId = req.FactionId,
                ColorHex = req.ColorHex ?? "#cccccc",
                SeatOrder = session.Players.Count == 0 ? 0 : session.Players.Max(p => p.SeatOrder) + 1,
                DeviceToken = deviceToken,
            };
            session.Players.Add(existing);
        }

        session.LastActivityUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        await hub.NotifySessionChanged(session.JoinCode);

        var overrides = await GetFactionOverridesAsync(db, ct);
        return Results.Ok(new JoinResultDto(session.ToDto(overrides), existing.Id, deviceToken));
    }

    private static async Task<IResult> UpdatePlayer(Guid id, Guid playerId, UpdatePlayerRequest req, Ti4DbContext db, IHubContext<SessionHub> hub, HttpContext http, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        var player = session?.Players.FirstOrDefault(p => p.Id == playerId);
        if (session is null || player is null) return Results.NotFound();
        if (!CallerCanActFor(session, http, playerId)) return Forbidden();           // edit own profile, or host edits anyone
        if (req.SeatOrder is not null && !CallerIsHost(session, http)) return Forbidden(); // seat order = host only

        if (req.Name is not null) player.Name = req.Name.Trim();
        if (req.ColorHex is not null) player.ColorHex = req.ColorHex;
        if (req.HasPassed is not null) player.HasPassed = req.HasPassed.Value;
        if (req.IsReady is not null) player.IsReady = req.IsReady.Value;
        if (req.SeatOrder is not null) player.SeatOrder = req.SeatOrder.Value;
        if (req.Influence is not null) player.Influence = Math.Max(0, req.Influence.Value);
        if (req.FactionId is not null)
        {
            var newFaction = string.IsNullOrWhiteSpace(req.FactionId) ? null : req.FactionId;
            if (newFaction != player.FactionId)
            {
                var oldFaction = player.FactionId;
                player.FactionId = newFaction;
                await UpdateStartingTechnologiesAsync(db, session, player, oldFaction, newFaction, ct);
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
            var overrides = await GetFactionOverridesAsync(db, ct);
            session.ActivePlayerId = TurnService.NextActiveAfter(session, overrides, playerId);
            session.ActiveStrategyCardId = null; // a new turn begins → close any played-action highlight
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

        var maxCards = session.Players.Count <= 4 ? 2 : 1;
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
        if (!session.Objectives.Any(o => o.ObjectiveId == req.ObjectiveId))
            session.Objectives.Add(new SessionObjective { SessionId = session.Id, ObjectiveId = req.ObjectiveId });
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
            CustomName = req.Name.Trim(),
            CustomPoints = Math.Clamp(req.Points, 0, 10),
        });
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

    private static async Task<IResult> ScoreObjective(Guid id, Guid sessionObjectiveId, ScoreObjectiveRequest req, Ti4DbContext db, IHubContext<SessionHub> hub, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        var obj = session?.Objectives.FirstOrDefault(o => o.Id == sessionObjectiveId);
        if (session is null || obj is null) return Results.NotFound();
        if (!obj.Scores.Any(s => s.PlayerId == req.PlayerId))
            obj.Scores.Add(new ObjectiveScore { SessionObjectiveId = obj.Id, PlayerId = req.PlayerId });
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
        if (!player.Technologies.Any(t => t.TechnologyId == req.TechnologyId))
            player.Technologies.Add(new PlayerTechnology { SessionId = session.Id, PlayerId = player.Id, TechnologyId = req.TechnologyId });
        return await SaveAndReturn(db, hub, session, ct);
    }

    private static async Task<IResult> RemoveTechnology(Guid id, Guid playerId, string techId, Ti4DbContext db, IHubContext<SessionHub> hub, HttpContext http, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        var player = session?.Players.FirstOrDefault(p => p.Id == playerId);
        if (session is null || player is null) return Results.NotFound();
        if (!CallerCanActFor(session, http, playerId)) return Forbidden();
        player.Technologies.RemoveAll(t => t.TechnologyId == techId);
        return await SaveAndReturn(db, hub, session, ct);
    }

    // -----------------------------------------------------------------------
    // Agenda phase
    // -----------------------------------------------------------------------

    private static async Task<IResult> SetAgenda(Guid id, SetAgendaRequest req, Ti4DbContext db, IHubContext<SessionHub> hub, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        if (session is null) return Results.NotFound();
        // Concluding the current agenda: the votes just cast spent influence, so deduct them before the
        // next agenda's vote begins (planets stay exhausted across the two agendas of one phase).
        foreach (var v in session.AgendaVotes)
        {
            var p = session.Players.FirstOrDefault(x => x.Id == v.PlayerId);
            if (p is not null && p.Influence > 0) p.Influence = Math.Max(0, p.Influence - v.Votes);
        }
        session.CurrentAgendaId = string.IsNullOrWhiteSpace(req.AgendaId) ? null : req.AgendaId;
        session.AgendaVotes.Clear(); // a freshly revealed agenda starts a new vote
        return await SaveAndReturn(db, hub, session, ct);
    }

    private static async Task<IResult> CastVote(Guid id, CastVoteRequest req, Ti4DbContext db, IHubContext<SessionHub> hub, HttpContext http, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        if (session is null) return Results.NotFound();
        if (!CallerCanActFor(session, http, req.PlayerId)) return Forbidden(); // cast your own vote; host may cast for others
        var vote = session.AgendaVotes.FirstOrDefault(v => v.PlayerId == req.PlayerId);
        if (vote?.Locked == true) return Forbidden(); // committed secret vote — host must reset to change it
        if (vote is null)
        {
            vote = new AgendaVote { SessionId = session.Id, PlayerId = req.PlayerId };
            session.AgendaVotes.Add(vote);
        }
        vote.Outcome = req.Outcome;
        vote.Votes = ClampVotes(session, req.PlayerId, req.Votes);
        // For elect agendas the choice carries the candidate; an abstention clears both weight and choice.
        vote.Choice = req.Outcome == VoteOutcome.Abstain ? null : (string.IsNullOrWhiteSpace(req.Choice) ? null : req.Choice.Trim());
        return await SaveAndReturn(db, hub, session, ct);
    }

    // Secret voting: commit a vote (set + lock) atomically. The choice reaches the server only here,
    // on lock — so nobody sees it beforehand. Self or host; a locked vote can't be changed until reset.
    private static async Task<IResult> LockVote(Guid id, LockVoteRequest req, Ti4DbContext db, IHubContext<SessionHub> hub, HttpContext http, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        if (session is null) return Results.NotFound();
        if (!CallerCanActFor(session, http, req.PlayerId)) return Forbidden();
        var vote = session.AgendaVotes.FirstOrDefault(v => v.PlayerId == req.PlayerId);
        if (vote?.Locked == true) return Forbidden();
        if (vote is null)
        {
            vote = new AgendaVote { SessionId = session.Id, PlayerId = req.PlayerId };
            session.AgendaVotes.Add(vote);
        }
        vote.Outcome = req.Outcome;
        vote.Votes = ClampVotes(session, req.PlayerId, req.Votes);
        vote.Choice = req.Outcome == VoteOutcome.Abstain ? null : (string.IsNullOrWhiteSpace(req.Choice) ? null : req.Choice.Trim());
        vote.Locked = true;
        return await SaveAndReturn(db, hub, session, ct);
    }

    // Clear all votes for the current agenda (host only) — e.g. to undo a mistake during secret voting.
    private static async Task<IResult> ResetVotes(Guid id, Ti4DbContext db, IHubContext<SessionHub> hub, HttpContext http, CancellationToken ct)
    {
        var session = await LoadGraphAsync(db, id, ct);
        if (session is null) return Results.NotFound();
        if (!CallerIsHost(session, http)) return Forbidden();
        session.AgendaVotes.Clear();
        return await SaveAndReturn(db, hub, session, ct);
    }

    // A player may never vote more influence than they recorded (0 = untracked → no cap).
    private static int ClampVotes(GameSession s, Guid playerId, int votes)
    {
        var n = Math.Max(0, votes);
        var influence = s.Players.FirstOrDefault(p => p.Id == playerId)?.Influence ?? 0;
        return influence > 0 ? Math.Min(n, influence) : n;
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

    private static async Task<Dictionary<string, int?>> GetFactionOverridesAsync(Ti4DbContext db, CancellationToken ct) =>
        await db.Factions.AsNoTracking().ToDictionaryAsync(f => f.Id, f => f.InitiativeOverride, ct);

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
    private static async Task UpdateStartingTechnologiesAsync(Ti4DbContext db, GameSession session, Player player, string? oldFactionId, string? newFactionId, CancellationToken ct)
    {
        if (oldFactionId is not null)
        {
            var oldFaction = await db.Factions.AsNoTracking().FirstOrDefaultAsync(f => f.Id == oldFactionId, ct);
            if (oldFaction is not null)
                foreach (var techId in oldFaction.StartingTechnologies)
                    player.Technologies.RemoveAll(t => t.TechnologyId == techId);
        }

        if (newFactionId is null) return;
        var newFaction = await db.Factions.AsNoTracking().FirstOrDefaultAsync(f => f.Id == newFactionId, ct);
        if (newFaction is null) return;
        foreach (var techId in newFaction.StartingTechnologies)
            if (!player.Technologies.Any(t => t.TechnologyId == techId))
                player.Technologies.Add(new PlayerTechnology { SessionId = session.Id, PlayerId = player.Id, TechnologyId = techId });
    }

    private static async Task<IResult> SaveAndReturn(Ti4DbContext db, IHubContext<SessionHub> hub, GameSession session, CancellationToken ct, Dictionary<string, int?>? overrides = null)
    {
        session.LastActivityUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        await hub.NotifySessionChanged(session.JoinCode);
        overrides ??= await GetFactionOverridesAsync(db, ct);
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
