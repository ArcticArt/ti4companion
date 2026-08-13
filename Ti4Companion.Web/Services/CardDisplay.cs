using Ti4Companion.Shared;
using Ti4Companion.Web.Localization;

namespace Ti4Companion.Web.Services;

/// <summary>
/// How a strategy card is NAMED on screen. One place, because the name is shown in eight of them (the action
/// view, the wall's pills and highlight, the secondary popup, the setup radio, the match log …) and a variant
/// that renames a card has to rename it everywhere or the table is looking for a card it cannot find.
/// </summary>
public static class CardDisplay
{
    /// <summary>The card's display name for this session.
    /// <para>
    /// <b>Bureaucracy: Red Tape</b> replaces one card with a version of its own, and when that is Imperial the
    /// card is called <i>Bureaucracy</i> — so the app calls it that too. Diplomacy deliberately keeps its name:
    /// the variant's author relabelled that version "Diplomacy" precisely because so many Xxcha abilities
    /// reference the card by name. <b>Red Tape Lite</b> replaces no card at all, so nothing is renamed there.
    /// </para></summary>
    public static string Name(Loc loc, SessionStateDto? session, StrategyCardDto card)
        => session is { RedTapeVariant: RedTapeVariant.Bureaucracy }
           && card.Id == session.RedTapeCardNumber
           && card.Id == GameRules.ImperialStrategyCard
            ? loc["redtape.bureaucracyCard"]
            : loc.Pick(card.Name, card.NameDe);
}
