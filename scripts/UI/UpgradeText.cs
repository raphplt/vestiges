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
                if (passive == null)
                    break;
                foreach (PassiveEffectData effect in passive.Effects)
                    lines.Add(($"{StatCatalog.Name(effect.Stat)}  {StatCatalog.FormatBonus(effect.Stat, effect.ValueAt(1), effect.Multiplicative)}", ChoiceStyle.GainColor));
                AddMilestoneLines(lines, passive, 0, 1);
                break;
            }
            case PerkSpecializationOffers.OptionType:
            {
                PerkSpecializationData perk = PerkSpecializationDataLoader.Get(choice.Id);
                if (perk == null)
                    break;
                lines.Add((perk.Description, ChoiceStyle.TextColor));
                if (!PerkSpecializationEffects.IsImplemented(perk.Effect))
                    lines.Add((TranslationServer.Translate("LEVELUP_PERK_PREVIEW"), ChoiceStyle.TextDim));
                break;
            }
            case "passive_upgrade":
            {
                ActivePassiveSouvenir passive = FindPassive(player, choice.Id);
                if (passive == null)
                    break;
                int next = passive.LevelAfter(choice.PassiveLevels);
                foreach (PassiveEffectData effect in passive.Data.Effects)
                    lines.Add(($"{StatCatalog.Name(effect.Stat)}  {StatCatalog.FormatBonus(effect.Stat, effect.ValueAt(passive.Level), effect.Multiplicative)}"
                        + $"  →  {StatCatalog.FormatBonus(effect.Stat, effect.ValueAt(next), effect.Multiplicative)}", ChoiceStyle.GainColor));
                AddMilestoneLines(lines, passive.Data, passive.Level, next);
                break;
            }
        }
        return lines;
    }

    /// <summary>
    /// Paliers d'un objet qui passe du niveau <paramref name="level"/> à <paramref name="next"/> : ceux que la carte
    /// fait atteindre, sinon le prochain, pour qu'on le voie venir. Seuls les paliers codés sont annoncés.
    /// </summary>
    private static void AddMilestoneLines(List<(string, Color)> lines, PassiveSouvenirData data, int level, int next)
    {
        ObjectMilestoneData upcoming = null;
        bool reachedAny = false;
        foreach (ObjectMilestoneData milestone in data.Milestones)
        {
            if (milestone.Level <= level || !ObjectMilestoneEffects.IsImplemented(milestone.Effect))
                continue;
            if (milestone.Level <= next)
            {
                lines.Add((string.Format(TranslationServer.Translate("LEVELUP_MILESTONE_REACHED"), milestone.Level, milestone.Text), ChoiceStyle.GoldBright));
                reachedAny = true;
            }
            else
                upcoming ??= milestone;
        }
        if (!reachedAny && upcoming != null)
            lines.Add((string.Format(TranslationServer.Translate("LEVELUP_MILESTONE"), upcoming.Level, upcoming.Text), ChoiceStyle.TextDim));
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
