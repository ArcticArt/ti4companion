using System.Text.RegularExpressions;
using Ti4Companion.Shared;

namespace Ti4Companion.Web.Components;

/// <summary>Presentation model for <see cref="UnitCardView"/>. Built by either <see cref="TechCard"/>
/// (for unit-upgrade techs, parsed from the tech text) or <see cref="UnitCard"/> (from a structured
/// <see cref="UnitDto"/>) so both render the card identically.</summary>
public record UnitCardVM(
    string Name,
    string AccentHex,
    UnitType UnitType,
    string? FactionId,
    string Prerequisites,
    string? Description,
    IReadOnlyList<UnitAbilityVM> Abilities,
    IReadOnlyList<UnitStatVM> Stats);

/// <summary>A ★ keyword ability, e.g. ("SUSTAIN DAMAGE", null) or ("BOMBARDMENT", "5").</summary>
public record UnitAbilityVM(string Keyword, string? Value);

/// <summary>A stat box. When <paramref name="MultCount"/> &gt; 1 the value is followed by that many
/// small <paramref name="MultIcon"/> images (the fighter/infantry "2 for 1" cost symbols, or the
/// combat dice) instead of a "(xN)" suffix.</summary>
public record UnitStatVM(string Label, string Value, string? MultIcon = null, int MultCount = 0);

/// <summary>Shared helpers for turning unit text into the bits a unit card shows: ★ keyword abilities,
/// stat boxes (with the fighter/infantry cost-multiplier and combat dice icons), and the prose.</summary>
public static class UnitText
{
    /// <summary>The TI4 combat-die burst, shown once per die on a multi-dice combat value.</summary>
    public const string DiceIcon = "DiceSymbol.png";

    /// <summary>Simplified cost symbol for the units built two-at-a-time; null for everything else.</summary>
    public static string? CostSymbol(UnitType t) => t switch
    {
        UnitType.Fighter => "FighterSymbol.png",
        UnitType.Infantry => "InfantrySymbol.png",
        _ => null,
    };

    /// <summary>Unit ability keywords that render as ★ bold bullets, longest first so multi-word
    /// keywords win over their prefixes.</summary>
    private static readonly string[] Keywords =
    {
        "ANTI-FIGHTER BARRAGE", "PLANETARY SHIELD", "SUSTAIN DAMAGE",
        "SPACE CANNON", "BOMBARDMENT", "PRODUCTION", "DEPLOY",
    };

    private static readonly string KwAlt = string.Join("|", Keywords.Select(Regex.Escape));

    // A leading ability chip: a keyword plus an optional value (5, 3(x3), 6 (x3), or X / N).
    private static readonly Regex LeadingAbilityRx =
        new($@"^({KwAlt})\b\s*(\d+\s*\(x\d+\)|\d+|X)?", RegexOptions.Compiled);

    // A leading stat chip at the start of a unit's text, in printed order.
    private static readonly Regex LeadingStatRx =
        new(@"^(Cost|Combat|Move|Capacity)\s+(\d+(?:\(x\d+\))?)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Splits a "1(x2)" / "3(x3)" value into its number and the multiplier.
    private static readonly Regex MultRx = new(@"^(\d+)\s*\(\s*x?(\d+)\s*\)$", RegexOptions.Compiled);

    /// <summary>Parse a unit-upgrade tech's <c>text</c> into stat boxes, ★ abilities and the prose
    /// description. The text is laid out "&lt;stats&gt;. &lt;ABILITIES&gt;. &lt;prose&gt;".</summary>
    public static (List<UnitStatVM> Stats, List<UnitAbilityVM> Abilities, string Description)
        ParseTechText(string? text, UnitType unitType, Func<string, string> statLabel)
    {
        var s = (text ?? string.Empty).Trim();
        var stats = new List<UnitStatVM>();

        Match m;
        while ((m = LeadingStatRx.Match(s)).Success)
        {
            stats.Add(MakeStat(m.Groups[1].Value, m.Groups[2].Value, unitType, statLabel));
            s = TrimSep(s[m.Length..]);
        }

        var (abilities, rest) = PeelAbilities(s);
        return (stats, abilities, rest);
    }

    /// <summary>Split a period/comma-separated keyword string (the ti4lookup "unit abilities" column,
    /// e.g. "SUSTAIN DAMAGE. BOMBARDMENT 5.") into keyword/value pairs.</summary>
    public static List<UnitAbilityVM> SplitAbilities(string? abilities)
    {
        var (list, _) = PeelAbilities(TrimSep((abilities ?? string.Empty).Trim()));
        return list;
    }

    /// <summary>Build the four stat boxes for a structured unit, in printed order, with the
    /// fighter/infantry cost-multiplier and combat dice rendered as icons.</summary>
    public static List<UnitStatVM> StructuredStats(
        UnitType unitType, int? cost, int producedCount, int? combat, int combatDice,
        int? move, int? capacity, Func<string, string> statLabel)
    {
        var stats = new List<UnitStatVM>();
        if (cost is { } c)
            stats.Add(producedCount > 1 && CostSymbol(unitType) is { } sym
                ? new UnitStatVM(statLabel("Cost"), c.ToString(), sym, producedCount)
                : new UnitStatVM(statLabel("Cost"), c.ToString()));
        if (combat is { } cb)
            stats.Add(combatDice > 1
                ? new UnitStatVM(statLabel("Combat"), cb.ToString(), DiceIcon, combatDice)
                : new UnitStatVM(statLabel("Combat"), cb.ToString()));
        if (move is { } mv) stats.Add(new UnitStatVM(statLabel("Move"), mv.ToString()));
        if (capacity is { } cap) stats.Add(new UnitStatVM(statLabel("Capacity"), cap.ToString()));
        return stats;
    }

    private static (List<UnitAbilityVM>, string) PeelAbilities(string s)
    {
        var abilities = new List<UnitAbilityVM>();
        Match m;
        while ((m = LeadingAbilityRx.Match(s)).Success)
        {
            var val = m.Groups[2].Success ? m.Groups[2].Value.Trim() : null;
            abilities.Add(new UnitAbilityVM(m.Groups[1].Value.Trim(), string.IsNullOrEmpty(val) ? null : val));
            s = TrimSep(s[m.Length..]);
        }
        return (abilities, s);
    }

    // Stat from a tech-text chip: split "1(x2)"/"3(x3)" into a number plus cost/dice icons.
    private static UnitStatVM MakeStat(string key, string value, UnitType unitType, Func<string, string> statLabel)
    {
        var label = statLabel(key);
        var mult = MultRx.Match(value);
        if (mult.Success)
        {
            var num = mult.Groups[1].Value;
            var n = int.Parse(mult.Groups[2].Value);
            if (key.Equals("Cost", StringComparison.OrdinalIgnoreCase) && CostSymbol(unitType) is { } sym)
                return new UnitStatVM(label, num, sym, n);
            if (key.Equals("Combat", StringComparison.OrdinalIgnoreCase))
                return new UnitStatVM(label, num, DiceIcon, n);
        }
        return new UnitStatVM(label, value);
    }

    private static string TrimSep(string s) => s.Trim(' ', ',', '.', ';');
}
