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

    public static MatchStats Compute(IReadOnlyList<SessionLogEntryDto> log, DateTimeOffset now)
    {
        var ordered = log.OrderBy(l => l.TimestampUtc).ToList();
        var phaseChanges = ordered.Where(l => l.Kind == SessionLogKind.PhaseChange && l.Phase is not null).ToList();
        if (phaseChanges.Count == 0) return new MatchStats();

        var start = phaseChanges[0].TimestampUtc;

        // Time per phase type: each phase segment runs until the next phase change (or now).
        var phaseTotals = new Dictionary<GamePhase, TimeSpan>();
        for (var i = 0; i < phaseChanges.Count; i++)
        {
            var from = phaseChanges[i].TimestampUtc;
            var to = i + 1 < phaseChanges.Count ? phaseChanges[i + 1].TimestampUtc : now;
            var ph = phaseChanges[i].Phase!.Value;
            phaseTotals[ph] = Get(phaseTotals, ph) + (to - from);
        }

        // Round durations: each round runs until the next round starts (or now).
        var roundChanges = ordered.Where(l => l.Kind == SessionLogKind.RoundChange && l.Round is not null).ToList();
        var rounds = new List<RoundDuration>();
        for (var i = 0; i < roundChanges.Count; i++)
        {
            var from = roundChanges[i].TimestampUtc;
            var to = i + 1 < roundChanges.Count ? roundChanges[i + 1].TimestampUtc : now;
            rounds.Add(new RoundDuration(roundChanges[i].Round!.Value, to - from));
        }

        // Per-player time-on-turn: walk the timeline, crediting each action-phase interval to whoever
        // is the active player at its start.
        var perPlayer = new Dictionary<Guid, TimeSpan>();
        GamePhase? phase = null;
        Guid? active = null;
        var lastTs = start;
        foreach (var e in ordered)
        {
            if (e.TimestampUtc < start) continue; // pre-game (setup joins)
            if (phase == GamePhase.Action && active is Guid a)
                perPlayer[a] = Get(perPlayer, a) + (e.TimestampUtc - lastTs);
            switch (e.Kind)
            {
                case SessionLogKind.PhaseChange:
                    phase = e.Phase;
                    if (phase != GamePhase.Action) active = null;
                    break;
                case SessionLogKind.TurnChange:
                    active = e.TargetPlayerId;
                    break;
            }
            lastTs = e.TimestampUtc;
        }
        if (phase == GamePhase.Action && active is Guid last)
            perPlayer[last] = Get(perPlayer, last) + (now - lastTs);

        return new MatchStats
        {
            StartedUtc = start,
            Match = now - start,
            Rounds = rounds,
            PhaseTotals = phaseTotals,
            PerPlayer = perPlayer,
        };
    }

    private static TimeSpan Get<TKey>(Dictionary<TKey, TimeSpan> d, TKey k) where TKey : notnull
        => d.TryGetValue(k, out var v) ? v : TimeSpan.Zero;

    /// <summary>Compact duration formatting, e.g. "1h 04m", "12m 30s", "45s".</summary>
    public static string Format(TimeSpan t)
    {
        if (t < TimeSpan.Zero) t = TimeSpan.Zero;
        if (t.TotalHours >= 1) return $"{(int)t.TotalHours}h {t.Minutes:00}m";
        if (t.TotalMinutes >= 1) return $"{t.Minutes}m {t.Seconds:00}s";
        return $"{t.Seconds}s";
    }
}
