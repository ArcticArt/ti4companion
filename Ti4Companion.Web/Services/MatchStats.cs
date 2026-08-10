using Ti4Companion.Shared;

namespace Ti4Companion.Web.Services;

public record RoundDuration(int Round, TimeSpan Duration);

/// <summary>
/// Match statistics derived from the structured match log. The timeline kinds
/// (<see cref="SessionLogKind.PhaseChange"/> / <see cref="SessionLogKind.RoundChange"/> /
/// <see cref="SessionLogKind.TurnChange"/>) are diffed into durations: total match time, per-round
/// time, time per phase type, and per-player time-on-turn (action phases only). All "open" segments
/// (the current round/phase/turn) are measured up to <c>now</c>.
/// </summary>
public class MatchStats
{
    /// <summary>When the match (round 1, Strategy phase) began, or null if it hasn't started.</summary>
    public DateTimeOffset? StartedUtc { get; init; }
    public TimeSpan? Match { get; init; }
    public IReadOnlyList<RoundDuration> Rounds { get; init; } = Array.Empty<RoundDuration>();
    public IReadOnlyDictionary<GamePhase, TimeSpan> PhaseTotals { get; init; } = new Dictionary<GamePhase, TimeSpan>();
    public IReadOnlyDictionary<Guid, TimeSpan> PerPlayer { get; init; } = new Dictionary<Guid, TimeSpan>();

    /// <summary>Time on turn per player **within the current round only** — what the turn timer counts down.</summary>
    public IReadOnlyDictionary<Guid, TimeSpan> PerPlayerRound { get; init; } = new Dictionary<Guid, TimeSpan>();

    /// <summary>The round the current-round figures belong to (1 when no round change has been logged yet).</summary>
    public int CurrentRound { get; init; } = 1;

    /// <param name="currentPicker">Whose turn it is to pick a strategy card, if the strategy phase is running.
    /// The log only credits strategy time when a pick happens, so without this the player currently thinking
    /// would show a frozen timer.</param>
    public static MatchStats Compute(IReadOnlyList<SessionLogEntryDto> log, DateTimeOffset now, Guid? currentPicker = null)
    {
        var ordered = log.OrderBy(l => l.TimestampUtc).ToList();
        var phaseChanges = ordered.Where(l => l.Kind == SessionLogKind.PhaseChange && l.Phase is not null).ToList();
        if (phaseChanges.Count == 0) return new MatchStats();

        var start = phaseChanges[0].TimestampUtc;

        // Pause intervals (GamePaused → GameResumed, or → now if still paused) — subtracted from every duration.
        var pauses = new List<(DateTimeOffset From, DateTimeOffset To)>();
        DateTimeOffset? pauseStart = null;
        foreach (var e in ordered)
        {
            if (e.Kind == SessionLogKind.GamePaused) pauseStart ??= e.TimestampUtc;
            else if (e.Kind == SessionLogKind.GameResumed && pauseStart is { } ps) { pauses.Add((ps, e.TimestampUtc)); pauseStart = null; }
        }
        if (pauseStart is { } open) pauses.Add((open, now));

        // Combat intervals (CombatStart → CombatEnd, or → now while one is running). Subtracted from
        // TIME-ON-TURN only, not from the round/phase/match durations: resolving a battle with someone else
        // is not the active player's thinking time, but it is still time the table spent playing. That is
        // also exactly what "the clock pauses during combat" means for the turn timer.
        var combats = new List<(DateTimeOffset From, DateTimeOffset To)>();
        DateTimeOffset? combatStart = null;
        foreach (var e in ordered)
        {
            if (e.Kind == SessionLogKind.CombatStart) combatStart ??= e.TimestampUtc;
            else if (e.Kind == SessionLogKind.CombatEnd && combatStart is { } cs) { combats.Add((cs, e.TimestampUtc)); combatStart = null; }
        }
        if (combatStart is { } openCombat) combats.Add((openCombat, now));

        // Pauses, and pauses+combats, each MERGED into non-overlapping intervals — a pause declared during a
        // combat overlaps, and subtracting both would take that time off twice.
        var pauseOnly = Merge(pauses);
        var pauseAndCombat = Merge(pauses.Concat(combats));

        // Active duration of [from, to] with the excluded intervals taken out.
        static TimeSpan Span(DateTimeOffset from, DateTimeOffset to, List<(DateTimeOffset From, DateTimeOffset To)> exclude)
        {
            var d = to - from;
            foreach (var (xf, xt) in exclude)
            {
                var ov = (xt < to ? xt : to) - (xf > from ? xf : from);
                if (ov > TimeSpan.Zero) d -= ov;
            }
            return d < TimeSpan.Zero ? TimeSpan.Zero : d;
        }

        TimeSpan Active(DateTimeOffset from, DateTimeOffset to) => Span(from, to, pauseOnly);
        /// <summary>Time on turn: pauses AND combats removed (see the comment on `combats`).</summary>
        TimeSpan OnTurn(DateTimeOffset from, DateTimeOffset to) => Span(from, to, pauseAndCombat);

        // Time per phase type: each phase segment runs until the next phase change (or now), minus pauses.
        var phaseTotals = new Dictionary<GamePhase, TimeSpan>();
        for (var i = 0; i < phaseChanges.Count; i++)
        {
            var from = phaseChanges[i].TimestampUtc;
            var to = i + 1 < phaseChanges.Count ? phaseChanges[i + 1].TimestampUtc : now;
            var ph = phaseChanges[i].Phase!.Value;
            phaseTotals[ph] = Get(phaseTotals, ph) + Active(from, to);
        }

        // Round durations: each round runs until the next round starts (or now), minus pauses.
        var roundChanges = ordered.Where(l => l.Kind == SessionLogKind.RoundChange && l.Round is not null).ToList();
        var rounds = new List<RoundDuration>();
        for (var i = 0; i < roundChanges.Count; i++)
        {
            var from = roundChanges[i].TimestampUtc;
            var to = i + 1 < roundChanges.Count ? roundChanges[i + 1].TimestampUtc : now;
            rounds.Add(new RoundDuration(roundChanges[i].Round!.Value, Active(from, to)));
        }

        // The round currently being played — the turn timer resets with it.
        var currentRoundStart = roundChanges.Count > 0 ? roundChanges[^1].TimestampUtc : start;
        var currentRound = roundChanges.Count > 0 ? roundChanges[^1].Round!.Value : 1;

        // Same interval, but only the part that falls inside the current round.
        TimeSpan OnTurnThisRound(DateTimeOffset from, DateTimeOffset to)
            => to <= currentRoundStart ? TimeSpan.Zero : OnTurn(from > currentRoundStart ? from : currentRoundStart, to);

        // Per-player time: action-phase intervals → the active player; strategy-phase intervals leading up
        // to each pick → that picker (so the strategy phase is counted per player too). Pauses AND combats
        // excluded — see `combats`.
        var perPlayer = new Dictionary<Guid, TimeSpan>();
        var perPlayerRound = new Dictionary<Guid, TimeSpan>();
        GamePhase? phase = null;
        Guid? active = null;
        // Players whose clock is running for a secondary ability (see SecondaryStart/Done). This is a UNION
        // with the active player's clock, not a replacement: several clocks legitimately run at once, and a
        // secondary outlives the turn it belongs to — the point of the whole feature is that the next player
        // can start while the others are still resolving. The active player is never in this set (the server
        // keeps them out; their clock IS the turn clock).
        var secondaries = new HashSet<Guid>();

        void Credit(Guid pid, DateTimeOffset from, DateTimeOffset to)
        {
            perPlayer[pid] = Get(perPlayer, pid) + OnTurn(from, to);
            perPlayerRound[pid] = Get(perPlayerRound, pid) + OnTurnThisRound(from, to);
        }

        var lastTs = start;
        foreach (var e in ordered)
        {
            if (e.TimestampUtc < start) continue; // pre-game (setup joins)
            if (phase == GamePhase.Action && active is Guid a) Credit(a, lastTs, e.TimestampUtc);
            foreach (var pid in secondaries)
            {
                if (pid != active) Credit(pid, lastTs, e.TimestampUtc);
            }
            if (phase == GamePhase.Strategy && e.Kind == SessionLogKind.StrategyPick && e.TargetPlayerId is Guid pk)
            {
                Credit(pk, lastTs, e.TimestampUtc);
            }
            switch (e.Kind)
            {
                case SessionLogKind.PhaseChange:
                    phase = e.Phase;
                    if (phase != GamePhase.Action) active = null;
                    break;
                case SessionLogKind.TurnChange:
                    active = e.TargetPlayerId;
                    // Secondaries are NOT cleared here: they run on past the turn change by design. The
                    // server closes them when the next action is played or the phase ends, and each of
                    // those emits a SecondaryDone, which is what empties this set.
                    break;
                case SessionLogKind.SecondaryStart:
                    if (e.TargetPlayerId is Guid ss) secondaries.Add(ss);
                    break;
                case SessionLogKind.SecondaryDone:
                    if (e.TargetPlayerId is Guid sd) secondaries.Remove(sd);
                    break;
            }
            lastTs = e.TimestampUtc;
        }

        // The still-open segment: in the action phase it belongs to the active player, in the strategy
        // phase to whoever is on the clock to pick (nothing has been logged for them yet).
        var openOwner = phase switch
        {
            GamePhase.Action => active,
            GamePhase.Strategy => currentPicker,
            _ => null
        };
        if (openOwner is Guid owner) Credit(owner, lastTs, now);
        // Plus everyone still resolving a secondary — the same union as inside the loop.
        foreach (var pid in secondaries)
        {
            if (pid != openOwner) Credit(pid, lastTs, now);
        }

        return new MatchStats
        {
            StartedUtc = start,
            Match = Active(start, now),
            Rounds = rounds,
            PhaseTotals = phaseTotals,
            PerPlayer = perPlayer,
            PerPlayerRound = perPlayerRound,
            CurrentRound = currentRound,
        };
    }

    private static TimeSpan Get<TKey>(Dictionary<TKey, TimeSpan> d, TKey k) where TKey : notnull
        => d.TryGetValue(k, out var v) ? v : TimeSpan.Zero;

    /// <summary>Merge intervals so overlapping ones (a pause declared during a combat) are subtracted once.</summary>
    private static List<(DateTimeOffset From, DateTimeOffset To)> Merge(IEnumerable<(DateTimeOffset From, DateTimeOffset To)> spans)
    {
        var merged = new List<(DateTimeOffset From, DateTimeOffset To)>();
        foreach (var s in spans.Where(s => s.To > s.From).OrderBy(s => s.From))
        {
            if (merged.Count > 0 && s.From <= merged[^1].To)
            {
                if (s.To > merged[^1].To) merged[^1] = (merged[^1].From, s.To);
            }
            else merged.Add(s);
        }
        return merged;
    }

    /// <summary>Compact duration formatting, e.g. "1h 04m", "12m 30s", "45s".</summary>
    public static string Format(TimeSpan t)
    {
        if (t < TimeSpan.Zero) t = TimeSpan.Zero;
        if (t.TotalHours >= 1) return $"{(int)t.TotalHours}h {t.Minutes:00}m";
        if (t.TotalMinutes >= 1) return $"{t.Minutes}m {t.Seconds:00}s";
        return $"{t.Seconds}s";
    }
}
