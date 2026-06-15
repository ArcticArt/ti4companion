using Ti4Companion.ApiService.Data;

namespace Ti4Companion.ApiService.Services;

/// <summary>
/// Initiative and turn-order logic. Initiative is the lowest strategy-card number a player holds,
/// unless their faction overrides it (the Naalu Collective always act first with initiative 0).
/// Passed players are skipped during the action phase.
/// </summary>
public static class TurnService
{
    public static int? GetInitiative(Player p, IReadOnlyDictionary<string, int?> factionOverrides)
    {
        if (!string.IsNullOrEmpty(p.FactionId)
            && factionOverrides.TryGetValue(p.FactionId, out var ov) && ov.HasValue)
        {
            return ov.Value;
        }

        if (p.StrategyCards.Count == 0)
        {
            return null;
        }

        return p.StrategyCards.Min(s => s.StrategyCardId);
    }

    /// <summary>Players ordered by initiative; those without a card sort last (by seat).</summary>
    public static List<Player> InitiativeOrder(IEnumerable<Player> players, IReadOnlyDictionary<string, int?> overrides)
        => players
            .OrderBy(p => GetInitiative(p, overrides) ?? int.MaxValue)
            .ThenBy(p => p.SeatOrder)
            .ToList();

    /// <summary>The first player to act in the action phase: lowest initiative that has not passed.</summary>
    public static Guid? FirstActive(GameSession s, IReadOnlyDictionary<string, int?> overrides)
        => InitiativeOrder(s.Players, overrides).FirstOrDefault(p => !p.HasPassed)?.Id;

    /// <summary>The next player after the current active one (skipping passed players), wrapping around.</summary>
    public static Guid? NextActive(GameSession s, IReadOnlyDictionary<string, int?> overrides)
        => Step(s, overrides, +1);

    /// <summary>The previous player before the current active one (for undoing a misclick).</summary>
    public static Guid? PreviousActive(GameSession s, IReadOnlyDictionary<string, int?> overrides)
        => Step(s, overrides, -1);

    /// <summary>The next non-passed player after a given player in initiative order (used when that player passes).</summary>
    public static Guid? NextActiveAfter(GameSession s, IReadOnlyDictionary<string, int?> overrides, Guid fromPlayerId)
    {
        var order = InitiativeOrder(s.Players, overrides);
        if (order.Count == 0) return null;
        var idx = order.FindIndex(p => p.Id == fromPlayerId);
        if (idx < 0) return order.FirstOrDefault(p => !p.HasPassed)?.Id;
        for (var i = 1; i <= order.Count; i++)
        {
            var c = order[(idx + i) % order.Count];
            if (!c.HasPassed) return c.Id;
        }
        return null;
    }

    /// <summary>Players in pick/agenda order: starting with the speaker, then clockwise by seat.</summary>
    public static List<Player> SeatOrderFromSpeaker(GameSession s)
    {
        var seated = s.Players.OrderBy(p => p.SeatOrder).ToList();
        if (seated.Count == 0) return seated;
        var start = s.SpeakerPlayerId is Guid sp ? seated.FindIndex(p => p.Id == sp) : 0;
        if (start < 0) start = 0;
        return Enumerable.Range(0, seated.Count).Select(i => seated[(start + i) % seated.Count]).ToList();
    }

    /// <summary>
    /// Whose turn it is to pick a strategy card: speaker first, then clockwise, one card per round
    /// (so with 2 cards everyone takes their first before the speaker takes a second). Null once full.
    /// </summary>
    public static Guid? CurrentPicker(GameSession s, int maxCards)
    {
        var order = SeatOrderFromSpeaker(s);
        if (order.Count == 0) return null;
        var taken = order.Sum(p => p.StrategyCards.Count);
        return taken >= order.Count * maxCards ? null : order[taken % order.Count].Id;
    }

    private static Guid? Step(GameSession s, IReadOnlyDictionary<string, int?> overrides, int dir)
    {
        var order = InitiativeOrder(s.Players, overrides).Where(p => !p.HasPassed).ToList();
        if (order.Count == 0)
        {
            return null;
        }

        if (s.ActivePlayerId is null)
        {
            return order[0].Id;
        }

        var idx = order.FindIndex(p => p.Id == s.ActivePlayerId);
        if (idx < 0)
        {
            return order[0].Id;
        }

        return order[((idx + dir) % order.Count + order.Count) % order.Count].Id;
    }
}
