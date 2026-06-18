using Ti4Companion.ApiService.Data;
using Ti4Companion.ApiService.Services;
using Ti4Companion.Shared;

namespace Ti4Companion.ApiService;

public static class Mapping
{
    public static FactionDto ToDto(this Faction f)
        => new(f.Id, f.Name, f.NameDe, f.Expansion, f.ColorHex, f.InitiativeOverride, f.IconPath, f.StartingTechnologies);

    public static StrategyCardDto ToDto(this StrategyCardDef s)
        => new(s.Id, s.Name, s.NameDe, s.Initiative, s.ColorHex,
               s.PrimaryText, s.PrimaryTextDe, s.SecondaryText, s.SecondaryTextDe, s.Version);

    public static ObjectiveDto ToDto(this ObjectiveDef o)
        => new(o.Id, o.Name, o.NameDe, o.Requirement, o.RequirementDe, o.Points, o.Stage, o.Expansion, o.IsSecret);

    public static TechnologyDto ToDto(this TechnologyDef t)
        => new(t.Id, t.Name, t.NameDe, t.Color, t.Prerequisites, t.Text, t.TextDe, t.Expansion, t.FactionId, t.UnitType);

    public static AgendaDto ToDto(this AgendaDef a)
        => new(a.Id, a.Name, a.NameDe, a.Type, a.Elect, a.Text, a.TextDe, a.Expansion, a.RemovedInPok);

    public static PlanetDto ToDto(this Planet p)
        => new(p.Id, p.Name, p.NameDe, p.Trait, p.Resources, p.Influence, p.HomeFactionId, p.Legendary, p.Expansion);

    public static UnitDto ToDto(this UnitDef u)
        => new(u.Id, u.Name, u.NameDe, u.UnitType, u.FactionId, u.Cost, u.ProducedCount,
               u.Combat, u.CombatDice, u.Move, u.Capacity, u.Text, u.TextDe, u.UnitAbilities, u.Expansion);

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
                p.Influence))
            .ToList();

        var objectives = s.Objectives
            .OrderBy(o => o.RevealedAtUtc)
            .Select(o => new SessionObjectiveDto(
                o.Id, o.ObjectiveId, o.Scores.Select(x => x.PlayerId).ToList(),
                o.CustomName, o.CustomPoints))
            .ToList();

        var cardStates = s.StrategyCardStates
            .Where(c => c.TradeGoods > 0)
            .Select(c => new StrategyCardStateDto(c.StrategyCardId, c.TradeGoods))
            .ToList();

        var votes = s.AgendaVotes
            .Select(v => new AgendaVoteDto(v.PlayerId, v.Outcome, v.Votes, v.Choice, v.Locked))
            .ToList();

        return new SessionStateDto(
            s.Id, s.JoinCode, s.Name, s.DefaultLanguage, s.ActiveExpansions,
            s.CurrentRound, s.Phase, s.SpeakerPlayerId, s.ActivePlayerId, s.ActiveStrategyCardId,
            s.CurrentAgendaId, s.AllowEditAllPlayers, s.ShowTechOverview, s.DisplayMode, s.AgendaVotesHidden, s.VotingStarted, s.RetentionHours,
            s.CreatedAtUtc, s.LastActivityUtc,
            players, objectives, cardStates, votes);
    }
}
