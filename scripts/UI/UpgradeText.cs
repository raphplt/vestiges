using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.UI;

/// <summary>Texte des cartes d'amélioration (level-up, Faille) : « avant → après » sur les valeurs effectives.</summary>
public static class UpgradeText
{
    /// <summary>Lignes de la carte : ce qui change (amélioration) ou ce que fait la nouveauté.</summary>
    public static List<(string, Color)> Describe(FragmentOption choice, Player player)
    {
        List<(string, Color)> lines = new();
        switch (choice.Type)
        {
            case "weapon_new":
            {
                WeaponData weapon = WeaponDataLoader.Get(choice.Id);
                string family = TranslationServer.Translate(weapon?.Type == "melee" ? "LEVELUP_MELEE" : "LEVELUP_RANGED");
                lines.Add(($"{weapon?.Summary}   ▸ {family}", ChoiceStyle.TextColor));
                break;
            }
            case "weapon_upgrade":
            {
                WeaponInstance weapon = FindWeapon(player, choice.Id);
                if (weapon == null)
                    break;
                WeaponInstance after = weapon.PreviewWith(choice.WeaponGains);
                foreach (StatGain gain in choice.WeaponGains)
                    lines.Add((DescribeStat(gain.Stat, player.GetWeaponStatForDisplay(weapon, gain.Stat),
                        player.GetWeaponStatForDisplay(after, gain.Stat)), ChoiceStyle.GainColor));
                break;
            }
            case "passive_new":
            {
                PassiveSouvenirData passive = PassiveSouvenirDataLoader.Get(choice.Id);
                if (passive?.PerLevel is { Length: > 0 })
                    lines.Add(($"{StatCatalog.Name(passive.Stat)}  {StatCatalog.FormatBonus(passive.Stat, passive.PerLevel[0], passive.ModifierType == "multiplicative")}", ChoiceStyle.GainColor));
                break;
            }
            case "passive_upgrade":
            {
                ActivePassiveSouvenir passive = FindPassive(player, choice.Id);
                if (passive == null)
                    break;
                bool multiplicative = passive.Data.ModifierType == "multiplicative";
                float next = passive.PreviewModifier(choice.PassiveGain, choice.PassiveLevels);
                lines.Add(($"{StatCatalog.Name(passive.Data.Stat)}  {StatCatalog.FormatBonus(passive.Data.Stat, passive.Modifier, multiplicative)}"
                    + $"  →  {StatCatalog.FormatBonus(passive.Data.Stat, next, multiplicative)}", ChoiceStyle.GainColor));
                break;
            }
        }
        return lines;
    }

    /// <summary>« Dégâts  14,2 → 16,8  +18 % » ; « Portée  +6 % » pour une stat qui ne se lit qu'en pourcentage.</summary>
    private static string DescribeStat(string stat, float before, float after)
    {
        string name = StatCatalog.Name(stat);
        return StatCatalog.Display(stat) switch
        {
            StatDisplay.Percent => $"{name}  {StatCatalog.FormatGain(before, after)}",
            StatDisplay.Count => $"{name}  {StatCatalog.Format(stat, before)} → {StatCatalog.Format(stat, after)}",
            _ => $"{name}  {StatCatalog.Format(stat, before)} → {StatCatalog.Format(stat, after)}   {StatCatalog.FormatGain(before, after)}",
        };
    }

    private static WeaponInstance FindWeapon(Player player, string id)
    {
        if (player == null)
            return null;
        foreach (WeaponInstance weapon in player.WeaponSlots)
            if (weapon.Id == id)
                return weapon;
        return null;
    }

    private static ActivePassiveSouvenir FindPassive(Player player, string id)
    {
        if (player == null)
            return null;
        foreach (ActivePassiveSouvenir passive in player.PassiveSlots)
            if (passive.Id == id)
                return passive;
        return null;
    }
}
