using Godot;

namespace Vestiges.Combat;

/// <summary>
/// Couleurs des effets de combat, tirées de la palette master et des palettes de biome
/// (doc/CHARTE-GRAPHIQUE.md §2-3). Aucune couleur d'effet n'est choisie ailleurs.
/// </summary>
public static class PixelPalette
{
    public static readonly Color DeepBlack = new("1A1A2E");
    public static readonly Color NightBlue = new("16213E");
    public static readonly Color WarmGrayDark = new("3A3535");
    public static readonly Color WarmGray = new("6B6161");
    public static readonly Color LightGray = new("9E9494");
    public static readonly Color OffWhite = new("E8E0D4");
    public static readonly Color ErasureWhite = new("F5F0EB");
    public static readonly Color HearthGold = new("D4A843");
    public static readonly Color FlameOrange = new("E07B39");
    public static readonly Color PlayerBlood = new("C4432B");
    public static readonly Color CreatureAcid = new("7FFF00");
    public static readonly Color EssenceCyan = new("5EC4C4");
    public static readonly Color Iridescent = new("2D1B3D");
    public static readonly Color IridescentHighlight = new("5A3A7A");
    public static readonly Color MistViolet = new("4A3066");
    public static readonly Color RustDark = new("6B3A24");
    public static readonly Color OxidizedCopper = new("5A9A8A");
    public static readonly Color GlassBlue = new("8AB8C4");
    public static readonly Color FlowerViolet = new("8B6BAE");
    public static readonly Color FlowerYellow = new("E0C84A");
    public static readonly Color SporeGreen = new("6ACA5A");
    public static readonly Color CanopyDark = new("2D5A27");

    private static readonly FxRamp[] Ramps =
    {
        new(ErasureWhite, OffWhite, HearthGold, RustDark),              // Physical
        new(OffWhite, HearthGold, FlameOrange, RustDark),               // Heavy
        new(OffWhite, EssenceCyan, OxidizedCopper, NightBlue),          // Essence
        new(GlassBlue, FlowerViolet, MistViolet, DeepBlack),            // Hybrid
        new(FlowerViolet, IridescentHighlight, Iridescent, DeepBlack),  // Void
        new(FlowerYellow, FlameOrange, PlayerBlood, RustDark),          // Fire
        new(CreatureAcid, SporeGreen, CanopyDark, Iridescent),          // Hostile
        new(ErasureWhite, FlowerYellow, HearthGold, RustDark),          // Crit
        new(FlameOrange, PlayerBlood, RustDark, DeepBlack),             // Blood
        new(LightGray, WarmGray, WarmGrayDark, DeepBlack),              // Stone
    };

    public static FxRamp Ramp(FxFamily family) => Ramps[(int)family];

    /// <summary>Famille nommée dans les données (`fx.family`, `fx_family`), sinon <paramref name="fallback"/>.</summary>
    public static FxFamily ParseFamily(string name, FxFamily fallback) => name switch
    {
        "physical" => FxFamily.Physical,
        "heavy" => FxFamily.Heavy,
        "essence" => FxFamily.Essence,
        "hybrid" => FxFamily.Hybrid,
        "void" => FxFamily.Void,
        "fire" => FxFamily.Fire,
        "hostile" => FxFamily.Hostile,
        "crit" => FxFamily.Crit,
        "blood" => FxFamily.Blood,
        "stone" => FxFamily.Stone,
        _ => fallback,
    };

    /// <summary>Famille d'un type de dégâts d'arme (physical, essence, hybrid).</summary>
    public static FxFamily FamilyForDamageType(string damageType) => damageType switch
    {
        "essence" => FxFamily.Essence,
        "hybrid" => FxFamily.Hybrid,
        _ => FxFamily.Physical,
    };
}
