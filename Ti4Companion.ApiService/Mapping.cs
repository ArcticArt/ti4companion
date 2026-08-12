using Ti4Companion.ApiService.Data;
using Ti4Companion.ApiService.Services;
using Ti4Companion.Shared;

namespace Ti4Companion.ApiService;

public static class Mapping
{
    // Content rows expose their LOGICAL id (slug / strategy-card number) as the DTO Id; the surrogate
    // Guid PK stays internal to the master DB.
    public static FactionDto ToDto(this Faction f)
        => new(f.Slug, f.Name, f.NameDe, f.Expansion, f.ColorHex, f.InitiativeOverride, f.IconPath,
               f.StartingTechnologies, f.PreferredColors, f.Complexity, f.Commodities, f.FlavorText, f.FlavorTextDe, f.Version, f.Source);

    public static StrategyCardDto ToDto(this StrategyCardDef s)
        => new(s.Number, s.Name, s.NameDe, s.Initiative, s.ColorHex,
               s.PrimaryText, s.PrimaryTextDe, s.SecondaryText, s.SecondaryTextDe,
               s.RevisionLabel, s.Version, s.Source);

    public static ObjectiveDto ToDto(this ObjectiveDef o)
        => new(o.Slug, o.Name, o.NameDe, o.Requirement, o.RequirementDe, o.Points, o.Stage, o.Phase,
               o.Expansion, o.Version, o.Source);

    public static TechnologyDto ToDto(this TechnologyDef t)
        => new(t.Slug, t.Name, t.NameDe, t.Color, t.Prerequisites, t.Text, t.TextDe, t.Expansion,
               t.FactionId, t.UnitType, t.Cost, t.ProducedCount, t.Combat, t.CombatDice, t.Move, t.Capacity,
               t.Abilities.OrderBy(a => a.SortOrder).Select(ToDto).ToList(), t.Version, t.Source);

    public static UnitAbilityDto ToDto(this UnitAbilityEntry a)
        => new(a.Ability, a.Value, a.Dice);

    public static AgendaDto ToDto(this AgendaDef a)
        => new(a.Slug, a.Name, a.NameDe, a.Type, a.Elect, a.Text, a.TextDe, a.Expansion, a.RemovedInPok,
               a.Version, a.Source);

    public static PlanetDto ToDto(this Planet p)
        => new(p.Slug, p.Name, p.Trait, p.Trait2, p.Resources, p.Influence, p.TechSkip1, p.TechSkip2,
               p.HomeFactionId, p.Legendary, p.LegendaryEffect, p.LegendaryEffectDe, p.IsStation, p.GrantsRelic,
               p.SystemTileId, p.FlavorText, p.FlavorTextDe, p.Expansion, p.Version, p.Source);

    public static UnitDto ToDto(this UnitDef u)
        => new(u.Slug, u.Name, u.NameDe, u.UnitType, u.FactionId, u.Cost, u.ProducedCount,
               u.Combat, u.CombatDice, u.Move, u.Capacity, u.Text, u.TextDe,
               u.Abilities.OrderBy(a => a.SortOrder).Select(ToDto).ToList(), u.Expansion,
               u.Version, u.Source);

    public static FactionAbilityDto ToDto(this FactionAbility a)
        => new(a.Slug, a.FactionId, a.Name, a.NameDe, a.Text, a.TextDe, a.Order, a.Expansion, a.Version, a.Source);

    public static LeaderDto ToDto(this Leader l)
        => new(l.Slug, l.FactionId, l.LeaderType, l.Name, l.NameDe, l.Subtitle, l.SubtitleDe, l.Text, l.TextDe,
               l.UnlockCondition, l.UnlockConditionDe, l.FlavorText, l.FlavorTextDe, l.Expansion, l.Version, l.Source);

    public static BreakthroughDto ToDto(this Breakthrough b)
        => new(b.Slug, b.FactionId, b.Name, b.NameDe, b.Text, b.TextDe, b.ConnectedColor1, b.ConnectedColor2);

    public static TypeValueDto ToDto(this TypeValue t)
        => new(t.Type, t.Value, t.Name, t.NameDe);

    public static PromissoryNoteDto ToDto(this PromissoryNote p)
        => new(p.Slug, p.FactionId, p.Name, p.NameDe, p.Text, p.TextDe, p.Expansion, p.Version, p.Source);

    public static ActionCardDto ToDto(this ActionCard a)
        => new(a.Slug, a.Name, a.NameDe, a.Text, a.TextDe, a.FlavorText, a.FlavorTextDe, a.Expansion, a.Version, a.Source);

    public static ExplorationDto ToDto(this Exploration e)
        => new(e.Slug, e.Deck, e.Name, e.NameDe, e.Text, e.TextDe, e.Expansion, e.Version, e.Source);

    public static RelicDto ToDto(this Relic r)
        => new(r.Slug, r.Name, r.NameDe, r.Text, r.TextDe, r.FlavorText, r.FlavorTextDe, r.Expansion, r.Version, r.Source);

    public static GalacticEventDto ToDto(this GalacticEvent g)
        => new(g.Slug, g.Name, g.NameDe, g.Text, g.TextDe, g.Expansion, g.Version, g.Source);

    public static FactionCardDto ToDto(this FactionCard f)
        => new(f.Slug, f.FactionId, f.Name, f.NameDe, f.Text, f.TextDe, f.Expansion, f.Version, f.Source);

    public static FactionStartingUnitDto ToDto(this FactionStartingUnit s)
        => new(s.FactionId, s.UnitId, s.Count);

    public static SystemTileDto ToDto(this SystemTile t)
        => new(t.TileNumber, t.SortOrder, t.Color, t.IsHomeSystem, t.HomeFactionId,
               t.IsAnomaly, t.Anomalies, t.Wormholes, t.IsHyperlane, t.IsFracture,
               t.Description, t.Planets, t.Expansion, t.Source);

    public static SessionLogEntryDto ToDto(this SessionLogEntry l)
        => new(l.Id, l.TimestampUtc, l.Kind, l.ActorPlayerId, l.TargetPlayerId, l.Phase, l.Round, l.Detail);

    public static SessionStateDto ToDto(this GameSession s, IReadOnlyDictionary<string, int?> factionOverrides)
    {
        var players = s.Players
            .OrderBy(p => p.SeatOrder)
            .Select(p => new PlayerDto(
                p.Id, p.Name, p.FactionId, p.ColorHex, p.SeatOrder, p.HasPassed, p.IsReady, p.IsHost,
                TurnService.GetInitiative(p, factionOverrides),
                p.StrategyCards
                    .OrderBy(c => c.StrategyCardId)
                    .Select(c => new PlayerStrategyCardDto(c.StrategyCardId, c.IsExhausted))
                    .ToList(),
                p.Technologies.Select(t => t.TechnologyId).ToList(),
                p.Influence, p.StatusDone, p.SecondaryPending, p.TechPromptPending))
            .ToList();

        var objectives = s.Objectives
            .OrderBy(o => o.RevealedAtUtc)
            .Select(o => new SessionObjectiveDto(
                o.Id, o.ObjectiveId, o.Scores.Select(x => x.PlayerId).ToList(),
                o.CustomName, o.CustomPoints, o.MarkerRemoved, o.Purged, o.PurgePending,
                o.Scores.Where(x => x.Round == s.CurrentRound).Select(x => x.PlayerId).ToList()))
            .ToList();

        var cardStates = s.StrategyCardStates
            .Where(c => c.TradeGoods > 0)
            .Select(c => new StrategyCardStateDto(c.StrategyCardId, c.TradeGoods))
            .ToList();

        // Face-down voting guarantee: while a hidden vote is running, nobody — not even the host —
        // may see a committed vote's outcome/weight/choice until the host reveals. The client hides
        // it visually, but the DTO goes to every device (and any spectator with the join code), so we
        // must redact it server-side too: keep only PlayerId + Locked (drives "voted / waiting"),
        // and blank the rest. Once revealed (AgendaVotesHidden=false) the full votes flow.
        var redactVotes = s.VotingStarted && s.AgendaVotesHidden;
        var votes = s.AgendaVotes
            .Select(v => redactVotes
                ? new AgendaVoteDto(v.PlayerId, VoteOutcome.Abstain, 0, null, v.Locked)
                : new AgendaVoteDto(v.PlayerId, v.Outcome, v.Votes, v.Choice, v.Locked))
            .ToList();

        // Intermediate step of a face-down vote: the table may see the TOTALS without learning who voted
        // what. Since the per-player rows above stay redacted, the aggregate has to be computed here —
        // only locked votes count, exactly as in the final tally.
        AgendaTotalsDto? totals = null;
        if (s.VotingStarted && s.AgendaTotalsRevealed)
        {
            var locked = s.AgendaVotes.Where(v => v.Locked).ToList();
            totals = new AgendaTotalsDto(
                locked.Where(v => v.Outcome == VoteOutcome.For).Sum(v => v.Votes),
                locked.Where(v => v.Outcome == VoteOutcome.Against).Sum(v => v.Votes),
                locked.Count(v => v.Outcome == VoteOutcome.Abstain),
                locked.Where(v => !string.IsNullOrEmpty(v.Choice))
                    .GroupBy(v => v.Choice!)
                    .Select(g => new AgendaChoiceTallyDto(g.Key, g.Sum(v => v.Votes)))
                    .OrderByDescending(t => t.Votes)
                    .ToList());
        }

        return new SessionStateDto(
            s.Id, s.JoinCode, s.Name, s.DefaultLanguage, s.ActiveExpansions,
            s.CurrentRound, s.Phase, s.SpeakerPlayerId, s.ActivePlayerId, s.ActiveStrategyCardId,
            s.CurrentAgendaId, s.AllowEditAllPlayers, s.ShowTechOverview, s.DisplayMode, s.AgendaVotesHidden, s.VotingStarted, s.Paused, s.RetentionHours,
            s.TurnTimerSeconds, s.StrategyCardsPerPlayer, s.RedTapeVariant, s.RedTapeCardNumber, s.PromptTechOnAction,
            s.TrackSecondaryAbilities,
            s.CreatedAtUtc, s.LastActivityUtc,
            players, objectives, cardStates, votes,
            s.AgendaTotalsRevealed, totals,
            s.StatusStepsDone,
            // Only meaningful in the status phase; null elsewhere so the client can't mistake it for a turn.
            s.Phase == GamePhase.Status ? TurnService.CurrentScorer(s, factionOverrides) : null,
            s.ShowJoinQr, s.StatusStage, s.SecondaryCardId, s.SecondaryOwnerId, s.SpeakerPending,
            s.RedTapeRandomRound, s.RedTapeRandomPendingRound,
            s.CombatAId, s.CombatBId, s.TechPickPlayerId, s.TechPromptOpen, s.TechPromptOwnerId,
            s.RedTapeCarrierGoods, s.CustomVoteTitle, s.CustomVoteElect);
    }
}
