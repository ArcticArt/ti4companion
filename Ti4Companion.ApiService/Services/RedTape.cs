using Ti4Companion.ApiService.Data;
using Ti4Companion.Shared;

namespace Ti4Companion.ApiService.Services;

/// <summary>
/// The Red Tape variants' rules, in one place, applied server-side.
///
/// Both are community variants and the app normally only tracks tokens — but the table asked for these to be
/// real, so this is the one corner where rules are enforced, and only for a session that chose a variant.
/// What is enforced and automated:
/// <list type="bullet">
/// <item>the first two Stage I objectives revealed start UNTAPED (they count as revealed);</item>
/// <item>Bureaucracy: no Stage II tape may come off in the first three rounds;</item>
/// <item>Lite: no Stage II tape until <see cref="ScorableStageI"/> Stage I objectives are clear;</item>
/// <item>Lite: when the fifth Stage I tape comes off, every remaining taped Stage I is PURGED — it can never
///       be scored and its tape can never be removed;</item>
/// <item>Lite: in a round where nobody took the carrier card, one tape comes off at random (round 1 right
///       after the strategy phase, afterwards when the status phase ends).</item>
/// </list>
/// Sources: "Bureaucracy: Red Tape for TI4" (WildFalkon) and "Red Tape Lite" (van nguyen) — the rulebook
/// insert and the card text as supplied by the user.
/// </summary>
public static class RedTape
{
    // The numbers live in GameRules — the client greys out exactly what this refuses.
    private const int UntapedAtSetup = GameRules.RedTapeUntapedAtSetup;
    private const int ScorableStageI = GameRules.RedTapeScorableStageI;
    private const int StageIILockedThrough = GameRules.RedTapeStageIILockedThrough;

    public static bool On(GameSession s) => s.RedTapeVariant != RedTapeVariant.None;

    private static bool IsStageI(SessionObjective o) => ObjectivePoints.StageOf(o.ObjectiveId) == ObjectiveStage.StageI;
    private static bool IsStageII(SessionObjective o) => ObjectivePoints.StageOf(o.ObjectiveId) == ObjectiveStage.StageII;

    /// <summary>
    /// Should a freshly revealed objective already be untaped? The first two Stage I on the table are the ones
    /// a normal game would have revealed at setup. A hand-added objective (a secret made public) is never
    /// taped — it was not part of the layout.
    /// </summary>
    public static bool RevealsUntaped(GameSession s, SessionObjective fresh)
    {
        if (!On(s)) return true;                                   // no variant → nothing is ever taped
        if (!string.IsNullOrEmpty(fresh.CustomName)) return true;   // hand-added, not part of the layout
        if (!IsStageI(fresh)) return false;
        var stageIBefore = s.Objectives.Count(o => o.Id != fresh.Id && IsStageI(o));
        return stageIBefore < UntapedAtSetup;
    }

    /// <summary>Why this tape may not be pulled right now, or null when it may. Only about the variant's
    /// gates — whether the caller is allowed to act at all is decided elsewhere.</summary>
    public static string? WhyCannotRemove(GameSession s, SessionObjective obj)
    {
        if (!On(s)) return null;
        if (obj.Purged) return "This objective was purged — its tape stays on.";
        if (!IsStageII(obj)) return null;

        if (s.RedTapeVariant == RedTapeVariant.Bureaucracy && s.CurrentRound <= StageIILockedThrough)
            return $"No Stage II objective in the first {StageIILockedThrough} rounds.";

        if (s.RedTapeVariant == RedTapeVariant.Lite && ClearStageI(s) < ScorableStageI)
            return $"No Stage II tape until the {ScorableStageI} scorable Stage I objectives are clear.";

        return null;
    }

    /// <summary>Stage I objectives whose tape is off (purged ones do not count — they never score).</summary>
    public static int ClearStageI(GameSession s)
        => s.Objectives.Count(o => IsStageI(o) && o.MarkerRemoved && !o.Purged);

    /// <summary>
    /// Red Tape Lite: once the fifth Stage I tape is off, the Stage I objectives still taped are purged —
    /// they can never be scored. Returns the ones purged by this call (for the log), empty when nothing
    /// changed. A no-op in Bureaucracy, where all five revealed Stage I are scorable.
    /// </summary>
    public static List<SessionObjective> ApplyPurge(GameSession s)
    {
        var purged = new List<SessionObjective>();
        if (s.RedTapeVariant != RedTapeVariant.Lite) return purged;
        if (ClearStageI(s) < ScorableStageI) return purged;
        foreach (var o in s.Objectives.Where(o => IsStageI(o) && !o.MarkerRemoved && !o.Purged))
        {
            o.Purged = true;
            purged.Add(o);
        }
        return purged;
    }

    /// <summary>Nobody holds the strategy card carrying the Red Tape ability this round — the condition for
    /// Lite's random removal.</summary>
    public static bool NobodyTookCarrier(GameSession s)
        => s.Players.All(p => p.StrategyCards.All(c => c.StrategyCardId != s.RedTapeCardNumber));

    /// <summary>
    /// Red Tape Lite's random removal, if it is due: nobody took the carrier card and it has not happened in
    /// this round yet. Returns the objective whose tape came off, or null. Deliberately skips anything the
    /// gates above forbid, so a random roll can never break a rule a player could not break either.
    /// </summary>
    public static SessionObjective? RemoveRandomTape(GameSession s)
    {
        if (s.RedTapeVariant != RedTapeVariant.Lite) return null;
        if (s.RedTapeRandomRound >= s.CurrentRound) return null;   // already done this round
        if (!NobodyTookCarrier(s)) return null;                     // someone took it → they choose instead

        var eligible = s.Objectives.Where(o => !o.MarkerRemoved && WhyCannotRemove(s, o) is null).ToList();
        s.RedTapeRandomRound = s.CurrentRound;                      // mark the round either way: it was its moment
        if (eligible.Count == 0) return null;
        var pick = eligible[Random.Shared.Next(eligible.Count)];
        pick.MarkerRemoved = true;
        return pick;
    }
}
