using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.UI;

/// <summary>
/// Texte des cartes d'amélioration (level-up, Faille), à la manière de Megabonk (plan 23 R2) : une ligne de gain
/// en valeur, deux au plus. La deuxième regroupe les autres stats d'une amélioration à plusieurs stats.
/// Ni propriété, ni armes concernées, ni texte de palier : un palier atteint se signale par un badge.
/// </summary>
public static class UpgradeText
{
    /// <summary>Lignes de la carte, deux au plus : ce qui change (amélioration) ou ce que fait la nouveauté.</summary>
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
                if (weapon == null || choice.WeaponGains.Count == 0)
                    break;
                WeaponInstance after = weapon.PreviewWith(choice.WeaponGains);
                List<string> others = new();
                for (int i = 0; i < choice.WeaponGains.Count; i++)
                {
                    string stat = choice.WeaponGains[i].Stat;
                    float before = player.GetWeaponStatForDisplay(weapon, stat);
                    float next = player.GetWeaponStatForDisplay(after, stat);
                    if (i == 0)
                        lines.Add((FullGain(stat, weapon.Base, before, next), ChoiceStyle.GainColor));
                    else
                        others.Add(ShortGain(stat, weapon.Base, before, next));
                }
                AddOthers(lines, others);
                break;
            }
            case FragmentOption.AscensionType:
                lines.Add((choice.Ascension.Description, ChoiceStyle.GainColor));
                break;
            case "passive_new":
            {
                PassiveSouvenirData passive = PassiveSouvenirDataLoader.Get(choice.Id);
                if (passive == null)
                    break;
                List<string> others = new();
                for (int i = 0; i < passive.Effects.Count; i++)
                {
                    PassiveEffectData effect = passive.Effects[i];
                    string line = $"{StatCatalog.Name(effect.Stat)}  {StatCatalog.FormatBonus(effect.Stat, effect.Neutral + effect.Step, effect.Multiplicative)}";
                    if (i == 0)
                        lines.Add((line, ChoiceStyle.GainColor));
                    else
                        others.Add(line);
                }
                AddOthers(lines, others);
                // La règle, chiffres compris : la ligne de stat seule ne dit pas quand l'objet agit (DECISIONS §53).
                lines.Add((passive.RuleText(), ChoiceStyle.TextColor));
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
                List<string> others = new();
                // La ligne en valeur montre le premier effet qui bouge ; un effet inchangé ne s'affiche pas.
                bool first = true;
                for (int i = 0; i < passive.Data.Effects.Count; i++)
                {
                    PassiveEffectData effect = passive.Data.Effects[i];
                    float before = passive.Value(i);
                    float after = passive.ValueAfter(i, choice.PassiveGain);
                    if (Mathf.IsEqualApprox(before, after))
                        continue;
                    if (first)
                        lines.Add(($"{StatCatalog.Name(effect.Stat)}  {StatCatalog.FormatBonus(effect.Stat, before, effect.Multiplicative)}"
                            + $"  →  {StatCatalog.FormatBonus(effect.Stat, after, effect.Multiplicative)}", ChoiceStyle.GainColor));
                    else
                        others.Add($"{StatCatalog.Name(effect.Stat)} {StatCatalog.FormatBonus(effect.Stat, (effect.Multiplicative ? 1f : 0f) + after - before, effect.Multiplicative)}");
                    first = false;
                }
                AddOthers(lines, others);
                break;
            }
        }
        return lines;
    }

    /// <summary>Vrai si la carte fait atteindre à l'objet un palier codé : la carte le signale d'un badge doré.</summary>
    public static bool ReachesMilestone(FragmentOption choice, Player player)
    {
        if (choice.Type != "passive_upgrade" || FindPassive(player, choice.Id) is not { } passive)
            return false;
        int next = passive.Level + 1;
        foreach (ObjectMilestoneData milestone in passive.Data.Milestones)
            if (milestone.Level > passive.Level && milestone.Level <= next && ObjectMilestoneEffects.IsImplemented(milestone.Effect))
                return true;
        return false;
    }

    private static void AddOthers(List<(string, Color)> lines, List<string> others)
    {
        if (others.Count > 0)
            lines.Add((string.Format(TranslationServer.Translate("LEVELUP_AND"), string.Join(", ", others)), ChoiceStyle.GainColor));
    }

    /// <summary>« Dégâts  14,2 → 16,8 » ; « Portée  +6 % » pour une stat qui ne se lit qu'en pourcentage.</summary>
    private static string FullGain(string stat, WeaponData weapon, float before, float after) => StatCatalog.Display(stat) == StatDisplay.Percent
        ? $"{StatCatalog.Name(stat, weapon)}  {StatCatalog.FormatGain(before, after)}"
        : $"{StatCatalog.Name(stat, weapon)}  {StatCatalog.Format(stat, before)} → {StatCatalog.Format(stat, after)}";

    /// <summary>Gain seul, pour la ligne qui regroupe : « Cadence +10 % », « Perforation +1 ».</summary>
    private static string ShortGain(string stat, WeaponData weapon, float before, float after) => StatCatalog.Display(stat) == StatDisplay.Count
        ? $"{StatCatalog.Name(stat, weapon)} {StatCatalog.FormatBonus(stat, after - before, false)}"
        : $"{StatCatalog.Name(stat, weapon)} {StatCatalog.FormatGain(before, after)}";

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
