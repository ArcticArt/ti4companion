namespace Ti4Companion.Web.Services;

/// <summary>The fixed set of player colors available in the physical game (plastics / command sheets).</summary>
public static class Ti4Colors
{
    public record Swatch(string Name, string Hex);

    public static readonly IReadOnlyList<Swatch> All = new[]
    {
        new Swatch("Red", "#c0392b"),
        new Swatch("Orange", "#e07b1c"),
        new Swatch("Yellow", "#e0c341"),
        new Swatch("Green", "#3ba55c"),
        new Swatch("Blue", "#2a6fb5"),
        new Swatch("Purple", "#8e44ad"),
        new Swatch("Pink", "#e84393"),
        new Swatch("Black", "#34404f"),
    };

    /// <summary>True once a player has actually picked one of the palette colours (not the placeholder default).</summary>
    public static bool IsChosen(string? hex) =>
        !string.IsNullOrWhiteSpace(hex) && All.Any(c => string.Equals(c.Hex, hex, StringComparison.OrdinalIgnoreCase));
}
