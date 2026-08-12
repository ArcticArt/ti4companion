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

    /// <summary>Diplomacy: the card the Red Tape variants are published for. <b>Bureaucracy</b> replaces
    /// either this one or <see cref="ImperialStrategyCard"/> and the table picks which at setup;
    /// <b>Lite</b> is always this one (it replaces no card, so there is nothing to choose).</summary>
    public const int RedTapeDiplomacyCard = 2;

    /// <summary>Which strategy card carries the Red Tape ability, given the variant and the table's choice.
    /// Only Bureaucracy offers a choice — Lite is published for Diplomacy, full stop, so a stored value from
    /// a table that switched variants can never leave Lite pointing at Imperial.</summary>
    public static int RedTapeCarrierCard(RedTapeVariant variant, int chosen)
        => variant == RedTapeVariant.Bureaucracy && chosen == ImperialStrategyCard
            ? ImperialStrategyCard
            : RedTapeDiplomacyCard;

    // ---- Turn timer ------------------------------------------------------------------------------------

    /// <summary>The per-player round budget the server accepts, in seconds (<c>0</c> = off). These live here
    /// because setup now lets the host TYPE a number: an input whose bounds disagree with the clamp would
    /// quietly store something other than what was entered.</summary>
    public const int TurnTimerMinSeconds = 10;

    /// <inheritdoc cref="TurnTimerMinSeconds"/>
    public const int TurnTimerMaxSeconds = 2 * 60 * 60;

    /// <summary>The same bounds as whole minutes — the unit the setup field asks for. The minimum is a
    /// minute rather than <see cref="TurnTimerMinSeconds"/>, so the field cannot undershoot the clamp.</summary>
    public const int TurnTimerMinMinutes = 1;

    /// <inheritdoc cref="TurnTimerMinMinutes"/>
    public const int TurnTimerMaxMinutes = TurnTimerMaxSeconds / 60;

    // ---- Retention -------------------------------------------------------------------------------------

    /// <summary>Hours of inactivity before a session is wiped (90 days). The fallback for
    /// <c>Ti4:DefaultRetentionHours</c>, stamped onto each session when it is created.
    /// <para>
    /// It lives here because it used to be written out twice with DIFFERENT values — the creation path fell
    /// back to 168 while the cleanup worker fell back to (and logged) 2160. With appsettings.json present
    /// both read the same number, so nothing was visibly wrong; had the key ever gone missing, sessions
    /// would have been stamped 7 days while the log claimed 90.
    /// </para></summary>
    public const int DefaultRetentionHours = 2160;

    /// <summary>Hours of inactivity before a PAUSED session is wiped (one year). A pause means an
    /// interrupted match somebody means to come back to, so it is kept far longer.</summary>
    public const int PausedRetentionHours = 8760;

    // ---- Red Tape variants -----------------------------------------------------------------------------
    // The server enforces these (see RedTape) and the client greys out exactly the same things, so the
    // numbers live here — written out twice is how the two drift apart.

    /// <summary>Objectives that start UNTAPED: the variants tape everything except the first two Stage I,
    /// which count as revealed.</summary>
    public const int RedTapeUntapedAtSetup = 2;

    /// <summary>Red Tape Lite: only this many Stage I objectives can ever score; the rest are purged once
    /// they are clear.</summary>
    public const int RedTapeScorableStageI = 5;

    /// <summary>Bureaucracy: "you may not choose a Stage II objective in the first 3 rounds".</summary>
    public const int RedTapeStageIILockedThrough = 3;
}
