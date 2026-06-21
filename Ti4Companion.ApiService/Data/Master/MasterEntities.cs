using System.ComponentModel.DataAnnotations.Schema;
using Ti4Companion.Shared;

namespace Ti4Companion.ApiService.Data;

// ---------------------------------------------------------------------------
// Master reference content (lives in its own ti4master.db via MasterDbContext).
//
// Every content row is ONE printing/revision of a logical item. The revisions of an item share a
// LogicalKey (a slug, or the strategy-card number); Version counts up (1,2,3…) and Source records the
// exact origin (Base / PoK / Codex I-IV / Thunder's Edge). The coarse Expansion flag is derived from the
// source and kept so the client can still filter content by a session's active expansions. The API
// serves the highest Version per LogicalKey; older revisions stay in the DB for history.
//
// The primary key is a surrogate Guid (set in the initializer → ValueGeneratedNever). All cross-content
// references (player.FactionId, faction starting techs, faction abilities/leaders/breakthroughs, session
// objective/agenda/tech ids …) are loose string refs to the LOGICAL key (slug / number), never to the
// Guid — so a reference always resolves to the latest applicable revision.
// ---------------------------------------------------------------------------

/// <summary>Common shape of a versioned reference-content row. <see cref="LogicalKey"/> groups an item's
/// revisions and is not persisted (it projects the entity's natural key).</summary>
public interface IMasterContent
{
    Guid Id { get; }
    int Version { get; }
    ContentSource Source { get; }
    Expansion Expansion { get; }
    string LogicalKey { get; }
}

public class Faction : IMasterContent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Slug { get; set; } = "";          // logical id, e.g. "naalu"
    public int Version { get; set; } = 1;
    public ContentSource Source { get; set; }
    public Expansion Expansion { get; set; }

    public string Name { get; set; } = "";
    public string NameDe { get; set; } = "";
    public string ColorHex { get; set; } = "#888888";
    /// <summary>Fixed initiative for factions that ignore strategy order (Naalu = 0).</summary>
    public int? InitiativeOverride { get; set; }
    /// <summary>Relative path to the faction icon, e.g. "factions/naalu.png".</summary>
    public string? IconPath { get; set; }
    /// <summary>Technology slugs this faction owns from the start (fixed picks only). JSON TEXT column.</summary>
    public List<string> StartingTechnologies { get; set; } = new();
    /// <summary>Preferred player colours, ordered **most → least** preferred. JSON TEXT column.</summary>
    public List<PlayerColor> PreferredColors { get; set; } = new();
    /// <summary>Complexity / difficulty rating.</summary>
    public FactionComplexity Complexity { get; set; }
    /// <summary>The faction's commodity value (how many commodities it can hold).</summary>
    public int Commodities { get; set; }
    /// <summary>Lore / flavour text (the italic blurb on the faction sheet).</summary>
    public string FlavorText { get; set; } = "";
    public string FlavorTextDe { get; set; } = "";

    [NotMapped] public string LogicalKey => Slug;
}

public class StrategyCardDef : IMasterContent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int Number { get; set; }                 // 1..8, the printed number + logical id
    public int Version { get; set; } = 1;
    public ContentSource Source { get; set; }
    public Expansion Expansion { get; set; }

    public string Name { get; set; } = "";
    public string NameDe { get; set; } = "";
    public int Initiative { get; set; }
    public string ColorHex { get; set; } = "";
    public string PrimaryText { get; set; } = "";
    public string PrimaryTextDe { get; set; } = "";
    public string SecondaryText { get; set; } = "";
    public string SecondaryTextDe { get; set; } = "";
    /// <summary>Printed revision marking, if any (e.g. "Ω" Codex, "ΩΩ" Thunder's Edge); empty for the
    /// original printing. Distinct from <see cref="Source"/>: the displayed Ω badge.</summary>
    public string RevisionLabel { get; set; } = "";

    [NotMapped] public string LogicalKey => Number.ToString();
}

public class ObjectiveDef : IMasterContent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Slug { get; set; } = "";
    public int Version { get; set; } = 1;
    public ContentSource Source { get; set; }
    public Expansion Expansion { get; set; }

    public string Name { get; set; } = "";
    public string NameDe { get; set; } = "";
    public string Requirement { get; set; } = "";
    public string RequirementDe { get; set; } = "";
    public int Points { get; set; }
    /// <summary>Stage I / Stage II public objective, or <see cref="ObjectiveStage.Secret"/> for a secret
    /// objective (secrets are candidates for "Elect Scored Secret Objective" + the secret→public flow).</summary>
    public ObjectiveStage Stage { get; set; }
    /// <summary>The phase in which this objective is scored — usually <see cref="GamePhase.Status"/>, but a
    /// secret objective may score in another phase (Action / Agenda / …).</summary>
    public GamePhase Phase { get; set; } = GamePhase.Status;

    [NotMapped] public string LogicalKey => Slug;
}

public class TechnologyDef : IMasterContent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Slug { get; set; } = "";
    public int Version { get; set; } = 1;
    public ContentSource Source { get; set; }
    public Expansion Expansion { get; set; }

    public string Name { get; set; } = "";
    public string NameDe { get; set; } = "";
    public TechColor Color { get; set; }
    /// <summary>Prerequisite pips as colour letters: B=Biotic, P=Propulsion, C=Cybernetic, W=Warfare.</summary>
    public string Prerequisites { get; set; } = "";
    public string Text { get; set; } = "";
    public string TextDe { get; set; } = "";
    /// <summary>Faction slug for faction-specific technologies; null for the common tree.</summary>
    public string? FactionId { get; set; }
    /// <summary>For unit-upgrade techs (<see cref="TechColor.Unit"/>), which unit it represents.</summary>
    public UnitType UnitType { get; set; }

    [NotMapped] public string LogicalKey => Slug;
}

public class AgendaDef : IMasterContent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Slug { get; set; } = "";
    public int Version { get; set; } = 1;
    public ContentSource Source { get; set; }
    public Expansion Expansion { get; set; }

    public string Name { get; set; } = "";
    public string NameDe { get; set; } = "";
    public AgendaType Type { get; set; }
    /// <summary>What the agenda elects (e.g. "Player", "Cultural Planet"), or empty for For/Against.</summary>
    public string Elect { get; set; } = "";
    public string Text { get; set; } = "";
    public string TextDe { get; set; } = "";
    /// <summary>Base-game agendas removed when Prophecy of Kings is in play.</summary>
    public bool RemovedInPok { get; set; }

    [NotMapped] public string LogicalKey => Slug;
}

/// <summary>A TI4 planet (for the "Elect …Planet" agenda pickers). Dual-trait planets carry a
/// <see cref="Trait2"/>; the pickers should surface a planet under either of its traits.</summary>
public class Planet : IMasterContent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Slug { get; set; } = "";          // e.g. "mecatol-rex"
    public int Version { get; set; } = 1;
    public ContentSource Source { get; set; }
    public Expansion Expansion { get; set; }

    /// <summary>Planet name — TI4 planet names aren't translated, so there is **no** NameDe (same in both).</summary>
    public string Name { get; set; } = "";
    public PlanetTrait Trait { get; set; }
    /// <summary>Second trait for dual-trait planets (e.g. TE "Industrial Cultural"); None if single-trait.</summary>
    public PlanetTrait Trait2 { get; set; }
    public int Resources { get; set; }
    public int Influence { get; set; }
    /// <summary>Technology-specialty ("tech skip") colour, or null. Some TE planets have two (double skip).</summary>
    public TechColor? TechSkip1 { get; set; }
    public TechColor? TechSkip2 { get; set; }
    /// <summary>Faction slug whose home system this planet belongs to; null for neutral planets.</summary>
    public string? HomeFactionId { get; set; }
    public bool Legendary { get; set; }
    /// <summary>The legendary planet's ability text (empty if not legendary).</summary>
    public string LegendaryEffect { get; set; } = "";
    public string LegendaryEffectDe { get; set; } = "";
    /// <summary>True for a TE Space Station (kept alongside planets here, but not a normal planet).</summary>
    public bool IsStation { get; set; }
    /// <summary>True for a TE Fracture planet that grants a relic.</summary>
    public bool GrantsRelic { get; set; }
    /// <summary>Reference to the system tile this planet sits on ("Systemtafel" number); null if unknown.</summary>
    public int? SystemTileId { get; set; }
    public string FlavorText { get; set; } = "";
    public string FlavorTextDe { get; set; } = "";

    [NotMapped] public string LogicalKey => Slug;
}

/// <summary>A buildable unit at its base level (standard + faction "Stufe I" units, plus flagships and
/// mechs). Level-II upgrades live as <see cref="TechnologyDef"/> unit-colour techs.</summary>
public class UnitDef : IMasterContent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Slug { get; set; } = "";          // e.g. "carrier-i", "letani-warrior-i"
    public int Version { get; set; } = 1;
    public ContentSource Source { get; set; }
    public Expansion Expansion { get; set; }

    public string Name { get; set; } = "";
    public string NameDe { get; set; } = "";
    public UnitType UnitType { get; set; }
    /// <summary>Faction slug for faction-specific units; null for the standard units.</summary>
    public string? FactionId { get; set; }
    /// <summary>Printed build cost; null for structures placed via Construction (PDS, space dock).</summary>
    public int? Cost { get; set; }
    /// <summary>Units produced per build: 2 for fighters/infantry, otherwise 1.</summary>
    public int ProducedCount { get; set; } = 1;
    /// <summary>Combat hit value; null for units that don't fight.</summary>
    public int? Combat { get; set; }
    /// <summary>Number of combat dice (the "(xN)" on a combat value); 1 by default.</summary>
    public int CombatDice { get; set; } = 1;
    public int? Move { get; set; }
    public int? Capacity { get; set; }
    public string Text { get; set; } = "";
    public string TextDe { get; set; } = "";
    /// <summary>Period-separated keyword abilities, e.g. "SUSTAIN DAMAGE. BOMBARDMENT 5.".</summary>
    public string UnitAbilities { get; set; } = "";

    [NotMapped] public string LogicalKey => Slug;
}

// ---- New content types ----------------------------------------------------

/// <summary>A named faction ability (e.g. Sol's "Orbital Drop"). A faction has one or more, ordered.</summary>
public class FactionAbility : IMasterContent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Slug { get; set; } = "";          // e.g. "sol-orbital-drop"
    public int Version { get; set; } = 1;
    public ContentSource Source { get; set; }
    public Expansion Expansion { get; set; }

    /// <summary>Owning faction slug.</summary>
    public string FactionId { get; set; } = "";
    public string Name { get; set; } = "";
    public string NameDe { get; set; } = "";
    public string Text { get; set; } = "";
    public string TextDe { get; set; } = "";
    /// <summary>Display order within the faction.</summary>
    public int Order { get; set; }

    [NotMapped] public string LogicalKey => Slug;
}

/// <summary>A Prophecy of Kings faction leader (Agent / Commander / Hero).</summary>
public class Leader : IMasterContent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Slug { get; set; } = "";          // e.g. "sol-evelyn-delouis"
    public int Version { get; set; } = 1;
    public ContentSource Source { get; set; }
    public Expansion Expansion { get; set; }

    /// <summary>Owning faction slug.</summary>
    public string FactionId { get; set; } = "";
    public LeaderType LeaderType { get; set; }
    /// <summary>The leader's proper name (e.g. "Evelyn DeLouis").</summary>
    public string Name { get; set; } = "";
    public string NameDe { get; set; } = "";
    public string Text { get; set; } = "";
    public string TextDe { get; set; } = "";
    /// <summary>Unlock condition (Commanders/Heroes); empty for Agents (always unlocked).</summary>
    public string UnlockCondition { get; set; } = "";
    public string UnlockConditionDe { get; set; } = "";
    /// <summary>Lore / flavour text (the italic blurb on the leader card).</summary>
    public string FlavorText { get; set; } = "";
    public string FlavorTextDe { get; set; } = "";

    [NotMapped] public string LogicalKey => Slug;
}

/// <summary>A Thunder's Edge faction Breakthrough — every faction (incl. base/PoK) gets exactly one.
/// TE-only, so it carries **no** Version/Source (it is not <see cref="IMasterContent"/>). Each breakthrough
/// "connects" two technology colours (e.g. Biotic+Cybernetic); the Nekro Virus breakthrough connects none.</summary>
public class Breakthrough
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Slug { get; set; } = "";
    /// <summary>Owning faction slug.</summary>
    public string FactionId { get; set; } = "";
    public string Name { get; set; } = "";
    public string NameDe { get; set; } = "";
    public string Text { get; set; } = "";
    public string TextDe { get; set; } = "";
    /// <summary>The two technology colours this breakthrough connects; **both null** for the Nekro Virus one.</summary>
    public TechColor? ConnectedColor1 { get; set; }
    public TechColor? ConnectedColor2 { get; set; }
}

/// <summary>One line of a faction's starting fleet: how many of a given unit it begins with. A pure
/// relationship (faction slug → unit slug + count), not a revisable card, so it carries no Version.</summary>
public class FactionStartingUnit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Owning faction slug.</summary>
    public string FactionId { get; set; } = "";
    /// <summary>Unit slug (the base <see cref="UnitDef"/> logical id, e.g. "carrier-i" or a faction flagship).</summary>
    public string UnitId { get; set; } = "";
    public int Count { get; set; }
}

/// <summary>
/// A bilingual label for a single value of a content enum (e.g. <c>UnitType.Flagship</c> → "Flagship" /
/// "Flaggschiff"). One row per (<see cref="Type"/>, <see cref="Value"/>), seeded by the migration so the
/// DB describes its own enum columns and the client can show localized type names. Covers UnitType,
/// TechColor, PlanetTrait, AgendaType, ObjectiveStage, LeaderType, GamePhase and ContentSource.
/// </summary>
public class TypeValue
{
    /// <summary>The enum's name, e.g. "UnitType", "TechColor", "ObjectiveStage".</summary>
    public string Type { get; set; } = "";
    /// <summary>The enum's integer value.</summary>
    public int Value { get; set; }
    public string Name { get; set; } = "";
    public string NameDe { get; set; } = "";
}

// ---- More content card types (versioned like the rest; mostly empty templates to fill) -------------

/// <summary>A promissory note. Faction-specific (<see cref="FactionId"/> set) or generic
/// (null — Support for the Throne, Ceasefire, Trade Agreement, Political Secret, Alliance).</summary>
public class PromissoryNote : IMasterContent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Slug { get; set; } = "";
    public int Version { get; set; } = 1;
    public ContentSource Source { get; set; }
    public Expansion Expansion { get; set; }

    /// <summary>Owning faction slug, or null for a generic note.</summary>
    public string? FactionId { get; set; }
    public string Name { get; set; } = "";
    public string NameDe { get; set; } = "";
    public string Text { get; set; } = "";
    public string TextDe { get; set; } = "";

    [NotMapped] public string LogicalKey => Slug;
}

/// <summary>An action card.</summary>
public class ActionCard : IMasterContent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Slug { get; set; } = "";
    public int Version { get; set; } = 1;
    public ContentSource Source { get; set; }
    public Expansion Expansion { get; set; }

    public string Name { get; set; } = "";
    public string NameDe { get; set; } = "";
    public string Text { get; set; } = "";
    public string TextDe { get; set; } = "";
    public string FlavorText { get; set; } = "";
    public string FlavorTextDe { get; set; } = "";

    [NotMapped] public string LogicalKey => Slug;
}

/// <summary>An exploration card. <see cref="Deck"/> is its deck: Cultural / Hazardous / Industrial /
/// Frontier (free text — there is no Frontier value on <c>PlanetTrait</c>).</summary>
public class Exploration : IMasterContent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Slug { get; set; } = "";
    public int Version { get; set; } = 1;
    public ContentSource Source { get; set; }
    public Expansion Expansion { get; set; }

    /// <summary>Which exploration deck: Cultural / Hazardous / Industrial / Frontier.</summary>
    public string Deck { get; set; } = "";
    public string Name { get; set; } = "";
    public string NameDe { get; set; } = "";
    public string Text { get; set; } = "";
    public string TextDe { get; set; } = "";

    [NotMapped] public string LogicalKey => Slug;
}

/// <summary>A relic.</summary>
public class Relic : IMasterContent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Slug { get; set; } = "";
    public int Version { get; set; } = 1;
    public ContentSource Source { get; set; }
    public Expansion Expansion { get; set; }

    public string Name { get; set; } = "";
    public string NameDe { get; set; } = "";
    public string Text { get; set; } = "";
    public string TextDe { get; set; } = "";
    public string FlavorText { get; set; } = "";
    public string FlavorTextDe { get; set; } = "";

    [NotMapped] public string LogicalKey => Slug;
}

/// <summary>A Thunder's Edge Galactic Event.</summary>
public class GalacticEvent : IMasterContent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Slug { get; set; } = "";
    public int Version { get; set; } = 1;
    public ContentSource Source { get; set; }
    public Expansion Expansion { get; set; }

    public string Name { get; set; } = "";
    public string NameDe { get; set; } = "";
    public string Text { get; set; } = "";
    public string TextDe { get; set; } = "";

    [NotMapped] public string LogicalKey => Slug;
}

/// <summary>A faction-specific extra card / component — the cards only certain factions get (e.g. the
/// Nekro Valefar Assimilators, and the TE faction components for Bastion / Firmament / Obsidian /
/// Deepwrought). Distinct from a <see cref="FactionAbility"/> (a sheet ability) and a <see cref="Leader"/>.</summary>
public class FactionCard : IMasterContent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Slug { get; set; } = "";
    public int Version { get; set; } = 1;
    public ContentSource Source { get; set; }
    public Expansion Expansion { get; set; }

    /// <summary>Owning faction slug (e.g. nekro, bastion, firmament, obsidian, deepwrought).</summary>
    public string FactionId { get; set; } = "";
    public string Name { get; set; } = "";
    public string NameDe { get; set; } = "";
    public string Text { get; set; } = "";
    public string TextDe { get; set; } = "";

    [NotMapped] public string LogicalKey => Slug;
}
