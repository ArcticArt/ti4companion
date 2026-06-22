using Ti4Companion.Shared;

namespace Ti4Companion.Web.Components;

/// <summary>Presentation model for <see cref="UnitCardView"/>. Built by either <see cref="TechCard"/>
/// (for unit-upgrade techs) or <see cref="UnitCard"/> (base units) from the structured stat fields and
/// atomic <see cref="UnitAbilityDto"/> list, so both render the card identically.</summary>
public record UnitCardVM(
    string Name,
    string AccentHex,
    UnitType UnitType,
    string? FactionId,
    string Prerequisites,
    string? Description,
    IReadOnlyList<UnitAbilityVM> Abilities,
    IReadOnlyList<UnitStatVM> Stats);

/// <summary>A ★ keyword ability, ready to render, e.g. ("SCHADENSRESISTENZ", null) or ("BOMBARDEMENT", "5")
/// or ("JÄGERABWEHR", "6 (x3)"). The keyword is already localized; the value is already formatted.</summary>
public record UnitAbilityVM(string Keyword, string? Value);

/// <summary>A stat box. When <paramref name="MultCount"/> &gt; 1 the value is followed by that many
/// small <paramref name="MultIcon"/> images (the fighter/infantry "2 for 1" cost symbols, or the
/// combat dice) instead of a "(xN)" suffix.</summary>
public record UnitStatVM(string Label, string Value, string? MultIcon = null, int MultCount = 0);

/// <summary>Helpers for building unit-card bits from structured data: stat boxes (with the
/// fighter/infantry cost-multiplier and combat dice icons) and the localized ★ keyword names.</summary>
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

    /// <summary>Localized keyword name (EN, DE) for a unit ability — the literal text of the ★ bullet.</summary>
    public static (string En, string De) KeywordNames(UnitAbility a) => a switch
    {
        UnitAbility.SustainDamage      => ("SUSTAIN DAMAGE", "SCHADENSRESISTENZ"),
        UnitAbility.AntiFighterBarrage => ("ANTI-FIGHTER BARRAGE", "JÄGERABWEHR"),
        UnitAbility.Bombardment        => ("BOMBARDMENT", "BOMBARDEMENT"),
        UnitAbility.SpaceCannon        => ("SPACE CANNON", "WELTRAUMKANONE"),
        UnitAbility.PlanetaryShield    => ("PLANETARY SHIELD", "PLANETARER SCHILD"),
        UnitAbility.Production          => ("PRODUCTION", "PRODUKTION"),
        UnitAbility.Deploy             => ("DEPLOY", "EINSATZ"),   // confirmed by user (seen on mechs)
        _ => ("", ""),
    };

    /// <summary>Format an ability's printed value, appending the "(xN)" multiplier (e.g. "6" + dice 3 →
    /// "6 (x3)"). Null value → null.</summary>
    public static string? FormatAbilityValue(string? value, int dice) =>
        value is null ? null : dice > 1 ? $"{value} (x{dice})" : value;

    /// <summary>Build the four stat boxes for a unit, in printed order, with the fighter/infantry
    /// cost-multiplier and combat dice rendered as icons.</summary>
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
}
