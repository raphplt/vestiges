using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.UI;

/// <summary>Présentation commune des objets : rareté fixe, valeur par copie et conditions réellement configurées.</summary>
public static class ItemPresentation
{
    public static string RarityText(PassiveSouvenirData data) => RarityPalette.DisplayName(data.Rarity);
    public static Color Color(PassiveSouvenirData data) => RarityPalette.Main(data.Rarity);
    public static string CopiesText(PassiveSouvenirData data, int copies) => data.HasCopyLimit
        ? $"×{copies} / {data.MaxCopies}" : $"×{copies}";

    public static void ConfigureCard(ChoiceCard card, PassiveSouvenirData data)
    {
        card.Tag = RarityText(data);
        card.Rank = RarityArt.Rank(data.Rarity);
        card.Frame = Color(data);
        if (!string.IsNullOrEmpty(data.Icon) && ResourceLoader.Exists(data.Icon))
            card.Icon = GD.Load<Texture2D>(data.Icon);
    }

    public static void AddComparison(ChoiceCard card, PassiveSouvenirData data, int ownedCopies)
    {
        card.Lines.Add((Describe(data, 1), ChoiceStyle.TextColor));
        if (ownedCopies > 0)
        {
            card.Lines.Add(($"Porté ×{ownedCopies} : {Describe(data, ownedCopies)}", ChoiceStyle.TextColor));
            if (!data.IsCopyLimitReached(ownedCopies))
                card.Lines.Add(($"Avec une copie de plus : {Describe(data, ownedCopies + 1)}", UITheme.GoldBright));
        }
        if (data.Flavor.Length > 0)
            card.Lines.Add((data.Flavor, ChoiceStyle.TextDim));
    }

    /// <summary>
    /// Ce que fait l'objet à <paramref name="copies"/> copies : sa phrase du JSON, ou sa ligne de stat pour un objet de
    /// stat. Les limites techniques ne sont pas affichées (DECISIONS §172) ; en pause, l'état du moment s'ajoute.
    /// </summary>
    public static string Describe(PassiveSouvenirData data, int copies = 1, Player player = null)
    {
        if (copies <= 0) return "Non porté";
        if (data.Description.Length == 0)
            return StatLine(data, copies) + (data.Stat is "knockback" or "projectile_speed" or "orbital_speed" or "chain_range"
                or "homing_strength" or "chain_targets" ? CompatibleWeapons(data.Stat, player) : "");
        string text = ObjectText.Format(data.Description, Values(data, copies, player), data.Id);
        if (player == null)
            return text;
        return text + data.Stat switch
        {
            "stride_damage" => player.ObjectTriggers?.StrideCharges > 0 ? " Prête." : " En attente d’un dash.",
            "still_damage" => $" Actuel : +{Percent(player.ObjectStances?.StillDamageBonus ?? 0f)}.",
            "crowd_damage" => $" Proches : {player.ObjectStances?.CrowdCount ?? 0}.",
            _ => "",
        };
    }

    private static Dictionary<string, float> Values(PassiveSouvenirData data, int copies, Player player) =>
        ObjectText.Values(data, copies, player?.StatusDurationMultiplier ?? 1f);

    private static string CompatibleWeapons(string stat, Player player)
    {
        if (player == null) return "";
        List<string> names = new();
        foreach (WeaponInstance weapon in player.WeaponSlots)
        {
            // Une propriété absente ou nulle n’est pas créée par son multiplicateur.
            bool compatible = weapon.GetStat(stat) > 0f && (stat switch
            {
                "orbital_speed" => weapon.AttackPattern == AttackPatternKind.Orbital,
                "chain_range" or "chain_targets" => weapon.AttackPattern is AttackPatternKind.Chain or AttackPatternKind.Bounce
                    && (weapon.Base.Stats.ContainsKey(stat) || weapon.Base.Growth.ContainsKey(stat)),
                "homing_strength" => weapon.Base.Stats.ContainsKey(stat) || weapon.Base.Growth.ContainsKey(stat),
                "projectile_speed" => weapon.Category != WeaponCategory.Melee
                    && weapon.AttackPattern is not (AttackPatternKind.Orbital or AttackPatternKind.Chain)
                    && weapon.SpecialEffect?.Kind != SpecialEffectKind.SustainedCone,
                _ => true,
            });
            if (compatible) names.Add(weapon.Name);
        }
        return names.Count == 0 ? " Aucune arme équipée compatible." : " Armes équipées concernées : " + string.Join(", ", names) + ".";
    }

    /// <summary>Stats de l'objet à <paramref name="copies"/> copies, ou d'un Écusson à ce niveau.</summary>
    public static string StatLine(PassiveSouvenirData data, int copies)
    {
        List<string> values = new();
        foreach (PassiveEffectData effect in data.Effects)
        {
            float value = effect.Neutral + effect.Step * copies;
            values.Add($"{StatCatalog.Name(effect.Stat)} {StatCatalog.FormatBonus(effect.Stat, value, effect.Multiplicative)}");
        }
        return string.Join(" · ", values) + ".";
    }

    private static string Percent(float value) => N(value * 100f) + " %";
    private static string N(float value) => value.ToString("0.##", PlayerSheet.French);
}
