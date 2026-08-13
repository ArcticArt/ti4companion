using Ti4Companion.Shared;
using Ti4Companion.Web.Localization;

namespace Ti4Companion.Web.Services;

/// <summary>
/// Helpers for the agenda phase: turns an agenda's free-text <c>Elect</c> field into a typed
/// <see cref="ElectType"/>, lists the candidates a vote may back, and resolves a stored choice key
/// back into a human label. Shared by the control <c>AgendaView</c> and the wall <c>Display</c>.
/// </summary>
public static class AgendaDisplay
{
    public record Candidate(string Key, string Label);

    public static ElectType ElectKind(AgendaDto a) => (a.Elect ?? "").Trim() switch
    {
        "" => ElectType.ForAgainst,
        "Player" => ElectType.Player,
        "Cultural Planet" => ElectType.CulturalPlanet,
        "Hazardous Planet" => ElectType.HazardousPlanet,
        "Industrial Planet" => ElectType.IndustrialPlanet,
        "Planet" => ElectType.Planet,
        "Non-Home Planet Other Than Mecatol Rex" => ElectType.NonHomePlanet,
        "Law" => ElectType.Law,
        "Strategy Card" => ElectType.StrategyCard,
        "Scored Secret Objective" => ElectType.ScoredSecret,
        _ => ElectType.ForAgainst,
    };

    /// <summary>Elect types whose candidate list may be incomplete (planets) or untracked (secrets),
    /// so the picker also offers a free-text entry.</summary>
    public static bool AllowsFreeText(ElectType e) => e is ElectType.ScoredSecret
        or ElectType.Planet or ElectType.CulturalPlanet or ElectType.HazardousPlanet
        or ElectType.IndustrialPlanet or ElectType.NonHomePlanet;

    /// <summary>The candidates a vote may back for this agenda (empty for For/Against and pure free-text).</summary>
    public static IReadOnlyList<Candidate> Candidates(SessionStore store, Loc loc, AgendaDto a)
        => Candidates(store, loc, ElectKind(a));

    /// <summary>Same by elect KIND, so a free vote with no agenda card uses the identical pickers.</summary>
    public static IReadOnlyList<Candidate> Candidates(SessionStore store, Loc loc, ElectType kind)
    {
        switch (kind)
        {
            case ElectType.Player:
                return store.Session?.Players.OrderBy(p => p.SeatOrder)
                    .Select(p => new Candidate(p.Id.ToString(), p.Name)).ToList() ?? new List<Candidate>();
            case ElectType.StrategyCard:
                return store.StrategyCards.OrderBy(c => c.Initiative)
                    .Select(c => new Candidate(c.Id.ToString(), $"{c.Initiative} · {loc.Pick(c.Name, c.NameDe)}")).ToList();
            case ElectType.Law:
                return store.ActiveAgendas().Where(x => x.Type == AgendaType.Law)
                    .OrderBy(x => loc.Pick(x.Name, x.NameDe))
                    .Select(x => new Candidate(x.Id, loc.Pick(x.Name, x.NameDe))).ToList();
            case ElectType.Planet:
            case ElectType.CulturalPlanet:
            case ElectType.HazardousPlanet:
            case ElectType.IndustrialPlanet:
            case ElectType.NonHomePlanet:
                return store.PlanetsFor(kind).OrderBy(p => p.Name)
                    .Select(p => new Candidate(p.Id, p.Name)).ToList();   // planet names aren't translated
            case ElectType.ScoredSecret:
                // Offer the secret objectives (e.g. Classified Document Leaks elects the secret to make
                // public); free text is also allowed for anything not listed.
                return store.ActiveObjectives().Where(o => o.Stage == ObjectiveStage.Secret).OrderBy(o => loc.Pick(o.Name, o.NameDe))
                    .Select(o => new Candidate(o.Id, loc.Pick(o.Name, o.NameDe))).ToList();
            default:
                return new List<Candidate>();
        }
    }

    /// <summary>Resolve a stored choice key into a display label (falls back to the raw key for free text).</summary>
    public static string ChoiceLabel(SessionStore store, Loc loc, AgendaDto agenda, string? choice)
        => ChoiceLabel(store, loc, ElectKind(agenda), choice);

    /// <summary>Same by elect KIND (free vote with no agenda card).</summary>
    public static string ChoiceLabel(SessionStore store, Loc loc, ElectType kind, string? choice)
    {
        if (string.IsNullOrEmpty(choice)) return "—";
        switch (kind)
        {
            case ElectType.Player:
                return store.Session?.Players.FirstOrDefault(p => p.Id.ToString() == choice)?.Name ?? choice;
            case ElectType.StrategyCard:
                return int.TryParse(choice, out var cid) && store.Card(cid) is { } c ? loc.Pick(c.Name, c.NameDe) : choice;
            case ElectType.Law:
                return store.Agenda(choice) is { } law ? loc.Pick(law.Name, law.NameDe) : choice;
            case ElectType.Planet:
            case ElectType.CulturalPlanet:
            case ElectType.HazardousPlanet:
            case ElectType.IndustrialPlanet:
            case ElectType.NonHomePlanet:
                return store.Planet(choice) is { } pl ? pl.Name : choice;   // planet names aren't translated
            case ElectType.ScoredSecret:
                return store.Objective(choice) is { } o ? loc.Pick(o.Name, o.NameDe) : choice;
            default:
                return choice;
        }
    }
}
