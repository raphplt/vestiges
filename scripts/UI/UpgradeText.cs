using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.UI;

/// <summary>
/// Texte des cartes d'amélioration (level-up, Faille) : une ligne de gain en valeur, deux au plus (plan 23 R2). La deuxième regroupe les autres stats d'une amélioration à plusieurs stats.
/// Ni propriété, ni armes concernées, ni texte de palier : un palier atteint se signale par un badge.
/// </summary>
public static class UpgradeText
{
    /// <summary>Lignes de la carte, deux au plus : ce qui change (amélioration) ou ce que fait la nouveauté.</summary>
    public static List<(string, Color)> Describe(FragmentOption choice, Player player)
    {
        UpgradeParts parts = Parts(choice, player);
        List<(string, Color)> lines = new();
        if (parts.Label.Length > 0)
        {
            // Les objets espacent leur flèche, comme avant le découpage en morceaux.
            string arrow = choice.Type == "passive_upgrade" ? "  →  " : " → ";
            string value = parts.Before.Length > 0 ? $"{parts.Before}{arrow}{parts.After}" : parts.Gain;
            lines.Add((value.Length > 0 ? $"{parts.Label}  {value}" : parts.Label, ChoiceStyle.GainColor));
        }
        if (parts.Others.Length > 0)
            lines.Add((parts.Others, ChoiceStyle.GainColor));
        if (parts.Detail.Length > 0)
            lines.Add((parts.Detail, ChoiceStyle.TextColor));
        if (parts.Flavor.Length > 0)
            lines.Add((parts.Flavor, ChoiceStyle.TextDim));
        return lines;
    }

    /// <summary>
    /// Contenu de la carte en morceaux : la stat qui bouge (libellé, gain, valeurs avant et après quand elles se
    /// lisent), le regroupement des autres stats, la règle ou la description, l'ambiance. Le level-up au fil
    /// (plan 04 §11) montre le gain en grand et l'avant → après sur l'étiquette ; la Faille en fait des lignes.
    /// </summary>
    public static UpgradeParts Parts(FragmentOption choice, Player player)
    {
        UpgradeParts parts = new();
        switch (choice.Type)
        {
            case "weapon_new":
            {
                WeaponData weapon = WeaponDataLoader.Get(choice.Id);
                string family = weapon == null ? "" : TranslationServer.Translate(WeaponGrammar.LabelKey(weapon.Category));
                parts.Detail = $"{weapon?.Summary}   ▸ {family}";
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
                    // Une stat qui se compte montre le nombre propre de l'arme, celui que la carte monte, sans les
                    // projectiles ou perforations que les Écussons donnent à toutes les armes (DECISIONS §175).
                    bool own = StatCatalog.Display(stat) == StatDisplay.Count;
                    float before = own ? weapon.GetStat(stat) : player.GetWeaponStatForDisplay(weapon, stat);
                    float next = own ? after.GetStat(stat) : player.GetWeaponStatForDisplay(after, stat);
                    if (i == 0)
                    {
                        parts.Label = StatCatalog.Name(stat, weapon.Base);
                        parts.Gain = ShortGain(stat, before, next);
                        // Une stat qui ne se lit qu'en pourcentage n'a pas de valeur à montrer avant et après.
                        if (StatCatalog.Display(stat) != StatDisplay.Percent)
                        {
                            parts.Before = StatCatalog.Format(stat, before);
                            parts.After = StatCatalog.Format(stat, next);
                        }
                    }
                    else
                        others.Add($"{StatCatalog.Name(stat, weapon.Base)} {ShortGain(stat, before, next)}");
                }
                parts.Others = OthersLine(others);
                break;
            }
            case FragmentOption.AscensionType:
                parts.Label = choice.Ascension.Description;
                break;
            case FragmentOption.SurplusType:
            {
                WeaponInstance weapon = FindWeapon(player, choice.Id);
                float gain = LevelUpOfferConfig.Load().WeaponSurplusDamage;
                float now = weapon?.SurplusDamage ?? 0f;
                parts.Label = string.Format(TranslationServer.Translate("LEVELUP_SURPLUS_LINE"), Mathf.RoundToInt(now * 100f),
                    Mathf.RoundToInt((now + gain) * 100f));
                break;
            }
            case "passive_new":
            {
                PassiveSouvenirData passive = PassiveSouvenirDataLoader.Get(choice.Id);
                if (passive == null)
                    break;
                List<string> others = new();
                for (int i = 0; i < passive.Effects.Count; i++)
                {
                    PassiveEffectData effect = passive.Effects[i];
                    string bonus = StatCatalog.FormatBonus(effect.Stat, effect.Neutral + effect.Step, effect.Multiplicative);
                    if (i == 0)
                    {
                        parts.Label = StatCatalog.Name(effect.Stat);
                        parts.Gain = bonus;
                    }
                    else
                        others.Add($"{StatCatalog.Name(effect.Stat)}  {bonus}");
                }
                parts.Others = OthersLine(others);
                // La règle, chiffres compris, quand la ligne de stat ne suffit pas (DECISIONS §53), puis l'ambiance.
                if (passive.Description.Length > 0)
                    parts.Detail = passive.RuleText();
                parts.Flavor = passive.Flavor;
                break;
            }
            case PerkSpecializationOffers.OptionType:
            {
                PerkSpecializationData perk = PerkSpecializationDataLoader.Get(choice.Id);
                if (perk == null)
                    break;
                parts.Detail = perk.Description;
                if (!PerkSpecializationEffects.IsImplemented(perk.Effect))
                    parts.Flavor = TranslationServer.Translate("LEVELUP_PERK_PREVIEW");
                break;
            }
            case "passive_upgrade":
            {
                ActivePassiveSouvenir passive = FindPassive(player, choice.Id);
                if (passive == null)
                    break;
                List<string> others = new();
                // Le gain en valeur montre le premier effet qui bouge ; un effet inchangé ne s'affiche pas.
                bool first = true;
                for (int i = 0; i < passive.Data.Effects.Count; i++)
                {
                    PassiveEffectData effect = passive.Data.Effects[i];
                    float before = passive.Value(i);
                    float after = passive.ValueAfter(i, choice.PassiveGain, choice.IntegerGain);
                    if (Mathf.IsEqualApprox(before, after))
                        continue;
                    string gain = StatCatalog.FormatBonus(effect.Stat, (effect.Multiplicative ? 1f : 0f) + after - before, effect.Multiplicative);
                    if (first)
                    {
                        parts.Label = StatCatalog.Name(effect.Stat);
                        parts.Gain = gain;
                        parts.Before = StatCatalog.FormatBonus(effect.Stat, before, effect.Multiplicative);
                        parts.After = StatCatalog.FormatBonus(effect.Stat, after, effect.Multiplicative);
                    }
                    else
                        others.Add($"{StatCatalog.Name(effect.Stat)} {gain}");
                    first = false;
                }
                parts.Others = OthersLine(others);
                break;
            }
        }
        return parts;
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

    private static string OthersLine(List<string> others) => others.Count > 0
        ? string.Format(TranslationServer.Translate("LEVELUP_AND"), string.Join(", ", others))
        : "";

    /// <summary>Gain seul : « +10 % » de cadence, « +1 » perforation ; une stat en valeur se lit en écart, « +18 % ».</summary>
    private static string ShortGain(string stat, float before, float after) => StatCatalog.Display(stat) == StatDisplay.Count
        ? StatCatalog.FormatBonus(stat, after - before, false)
        : StatCatalog.FormatGain(before, after);

    private static WeaponInstance FindWeapon(Player player, string id)
    {
        if (player == null)
            return null;
        foreach (WeaponInstance weapon in player.WeaponSlots)
            if (weapon.Id == id)
                return weapon;
        return null;
    }

    private static ActivePassiveSouvenir FindPassive(Player player, string id) => player?.FindObject(id);
}
