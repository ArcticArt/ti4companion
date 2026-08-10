namespace Ti4Companion.Shared;

/// <summary>
/// Rules the server (enforcement) and the client (UI gating) must agree on. They used to be written out
/// separately in both, which is exactly how the two drift apart.
/// </summary>
public static class GameRules
{
    /// <summary>
    /// How many strategy cards each player takes per round.
    /// <paramref name="option"/> <c>0</c> = automatic, i.e. the printed rule: two each with four players
    /// or fewer, otherwise one. <c>1</c> or <c>2</c> pin it — a four-player table can then deliberately
    /// play with a single card each ("Feast or Famine").
    /// </summary>
    public static int StrategyCardsPerPlayer(int playerCount, int option)
        => option is 1 or 2 ? option : playerCount <= 4 ? 2 : 1;

    /// <summary>The Technology strategy card's number (cards are keyed by their printed initiative 1–8).
    /// Used for the optional "record your technology" prompt after that action is played.</summary>
    public const int TechnologyStrategyCard = 7;

    /// <summary>Politics: the card whose primary appoints the speaker. Verified against /api/content.</summary>
    public const int PoliticsStrategyCard = 3;

    /// <summary>Imperial: the card whose primary may score a public objective.</summary>
    public const int ImperialStrategyCard = 8;
}
