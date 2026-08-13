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
/// <item>Lite: when the fifth Stage I tape comes off, every remaining taped Stage I is PROPOSED for purging —
///       the table confirms, and only then can it never be scored again;</item>
/// <item>Lite: in a round where nobody took the carrier card, a random removal is PROPOSED (round 1 right
///       after the strategy phase, afterwards when the status phase ends).</item>
/// </list>
/// <para>
/// Both of those used to happen on their own. They are irreversible and they change who can still win, so the
/// app now only ever asks: <see cref="ProposePurge"/>/<see cref="ProposeRandom"/> raise the question,
/// <see cref="ConfirmPurge"/>/<see cref="ConfirmRandom"/> and <see cref="DeclinePurge"/>/
/// <see cref="DeclineRandom"/> answer it. Nothing here mutates the game without having been asked first.
/// </para>
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

    /// <summary>Objectives currently proposed for purging (awaiting the table's answer).</summary>
    public static List<SessionObjective> PurgeProposal(GameSession s)
        => s.Objectives.Where(o => o.PurgePending).ToList();

    /// <summary>
    /// Red Tape Lite: the fifth Stage I tape has just come off, so the Stage I objectives still taped are
    /// PROPOSED for purging. Returns the ones newly flagged (empty when the moment is not now). Call it right
    /// after a tape came off; a no-op in Bureaucracy, where all five revealed Stage I are scorable.
    /// <para>
    /// It fires on the TRANSITION — exactly <see cref="ScorableStageI"/> clear, i.e. this removal was the
    /// fifth — and not on "five or more are clear". That difference is the point: as a standing condition it
    /// also swallowed every Stage I revealed afterwards, striking out a card the table had only just turned
    /// face-up. The rules call the victims "Stage I #6 and #7", which needs no numbering here: they are
    /// whatever is still taped at this moment, so it scales with however many the table laid out (seven → two,
    /// six → one).
    /// </para>
    /// </summary>
    public static List<SessionObjective> ProposePurge(GameSession s)
    {
        var proposed = new List<SessionObjective>();
        if (s.RedTapeVariant != RedTapeVariant.Lite) return proposed;
        if (ClearStageI(s) != ScorableStageI) return proposed;      // not the fifth → not the moment
        if (s.Objectives.Any(o => o.PurgePending)) return proposed;  // already asking
        foreach (var o in s.Objectives.Where(o => IsStageI(o) && !o.MarkerRemoved && !o.Purged))
        {
            o.PurgePending = true;
            proposed.Add(o);
        }
        return proposed;
    }

    /// <summary>The table said yes: the proposed objectives are out of the game. Returns them for the log.</summary>
    public static List<SessionObjective> ConfirmPurge(GameSession s)
    {
        var purged = PurgeProposal(s);
        foreach (var o in purged) { o.Purged = true; o.PurgePending = false; }
        return purged;
    }

    /// <summary>The table said no: drop the proposal and leave the objectives taped but alive. The moment does
    /// not come back on its own — a further Stage I tape takes the count past the fifth, so
    /// <see cref="ProposePurge"/> stays quiet unless a tape is put back and pulled again.</summary>
    public static void DeclinePurge(GameSession s)
    {
        foreach (var o in PurgeProposal(s)) o.PurgePending = false;
    }

    /// <summary>Nobody holds the strategy card carrying the Red Tape ability this round — the condition for
    /// Lite's random removal.</summary>
    public static bool NobodyTookCarrier(GameSession s)
        => s.Players.All(p => p.StrategyCards.All(c => c.StrategyCardId != s.RedTapeCardNumber));

    /// <summary>Tapes a random removal could legally take off — never one the gates above forbid, so a roll
    /// can never break a rule a player could not break either.</summary>
    private static List<SessionObjective> RandomEligible(GameSession s)
        => s.Objectives.Where(o => !o.MarkerRemoved && WhyCannotRemove(s, o) is null).ToList();

    /// <summary>
    /// Raise the question "nobody took the carrier card — take one tape off at random?" if it is due: Lite,
    /// not already settled this round, nobody holds the carrier, and there is actually a tape it could take.
    /// Returns true when the question is now open. Nothing is removed here.
    /// </summary>
    public static bool ProposeRandom(GameSession s)
    {
        if (s.RedTapeVariant != RedTapeVariant.Lite) return false;
        if (s.RedTapeRandomPendingRound != 0) return false;         // already asking
        if (s.RedTapeRandomRound >= s.CurrentRound) return false;    // already settled this round
        if (!NobodyTookCarrier(s)) return false;                     // someone took it → they choose instead
        // Don't ask a question whose only answer is "there is nothing to remove" — but do settle the round,
        // or every later phase boundary in it would ask again.
        if (RandomEligible(s).Count == 0) { s.RedTapeRandomRound = s.CurrentRound; return false; }
        s.RedTapeRandomPendingRound = s.CurrentRound;
        return true;
    }

    /// <summary>The table said yes: take one eligible tape off at random. Returns the objective, or null if
    /// nothing was pending or nothing is eligible any more.</summary>
    public static SessionObjective? ConfirmRandom(GameSession s)
    {
        if (s.RedTapeRandomPendingRound == 0) return null;
        // Settle the round the question was ASKED for, not the current one: NextRound raises it while still in
        // the old round and only then increments, so answering afterwards must not mark the new round as done.
        s.RedTapeRandomRound = s.RedTapeRandomPendingRound;
        s.RedTapeRandomPendingRound = 0;
        var eligible = RandomEligible(s);
        if (eligible.Count == 0) return null;
        var pick = eligible[Random.Shared.Next(eligible.Count)];
        pick.MarkerRemoved = true;
        return pick;
    }

    /// <summary>The table said no: nothing comes off, and the round is settled so it stops asking.</summary>
    public static void DeclineRandom(GameSession s)
    {
        if (s.RedTapeRandomPendingRound == 0) return;
        s.RedTapeRandomRound = s.RedTapeRandomPendingRound;
        s.RedTapeRandomPendingRound = 0;
    }
}
