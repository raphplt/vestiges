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
    }

    /// <summary>Valeur actuelle selon les copies ; les paramètres de durée du joueur sont pris en compte en pause.</summary>
    public static string Describe(PassiveSouvenirData data, int copies = 1, Player player = null)
    {
        if (copies <= 0) return "Non porté";
        float duration = player?.StatusDurationMultiplier ?? 1f;
        float amount = data.Effects[0].Step * copies;
        float P(string key) => data.Parameters.GetValueOrDefault(key);
        string Seconds(float seconds) => $"{N(seconds)} s";
        string Chance() => $"{Percent(P("chance"))} de base × coefficient de l’arme (au plus 1)";
        return data.Stat switch
        {
            "burn_chance" => $"{Chance()} de brûler ; {Percent(P("burn_damage_ratio"))} du coup/s pendant {Seconds(P("burn_seconds") * duration)}. Plafond commun : {N(P("applications_per_second") * copies)} applications/s.",
            "chill_chance" => $"{Chance()} de ralentir de {Percent(1f - P("slow_factor"))} pendant {Seconds(P("slow_seconds") * duration)}. {N(P("applications_per_second") * copies)} application(s)/s. Répit : {Seconds(P("reprieve_seconds"))}, élite {Seconds(P("elite_reprieve_seconds"))}.",
            "poison" => $"{Chance()} d’empoisonner : {Percent(P("damage_ratio"))} du coup sur {Seconds(P("seconds") * duration)}. {N(P("applications_per_second") * copies)} doses/s ; {N(P("doses_per_copy") * copies)} doses/cible, durées indépendantes.",
            "elite_slow" => $"{Chance()} de ralentir une élite de {Percent(1f - P("slow_factor"))} pendant {Seconds(Mathf.Min(P("max_seconds"), P("seconds") * duration))}. {copies} application(s)/{Seconds(P("window_seconds"))} ; même élite : {Seconds(P("target_cooldown_seconds"))} entre activations.",
            "burning_target_damage" => $"+{Percent(amount)} dégâts contre une cible en feu.",
            "slowed_target_damage" => $"+{Percent(amount)} dégâts contre une cible entravée.",
            "kill_explosion" => $"Une élimination d’arme : explosion à {Percent(amount)} du coup fatal, rayon {N(P("radius") * (player?.AoeMultiplier ?? 1f))}. Aucune réaction en chaîne.",
            "kill_heal" => $"Jusqu’à {N(P("heal_per_kill"))} PV par élimination d’arme ; plafond commun {N(amount)} PV/s. Aucun soin mis en réserve.",
            "stride_damage" => $"Après dash : prochaine attaque +{Percent(amount)} dégâts. Une charge pendant {Seconds(P("charge_seconds"))}, salve entière." + (player == null ? "" : player.ObjectTriggers?.StrideCharges > 0 ? " Prête." : " En attente d’un dash."),
            "level_heal" => $"Chaque niveau gagné : soin de {Percent(amount)} des PV max, limité aux PV manquants.",
            "crit_fragile" => $"Un critique : cible Fragile, +{Percent(amount)} dégâts reçus pendant {Seconds(P("fragile_seconds") * duration)}.",
            "crit_echo" => $"Un critique : {Percent(amount)} de ses dégâts vers une autre cible à moins de {N(P("radius"))} px.",
            "still_damage" => $"Immobile : +{Percent(amount)} dégâts par seconde, plafond +{Percent(P("damage_cap"))} atteint en {Seconds(P("damage_cap") / Mathf.Max(amount, 0.001f))}. Mouvement/dash : remise à zéro." + (player == null ? "" : $" Actuel : +{Percent(player.ObjectStances?.StillDamageBonus ?? 0f)}."),
            "dash_trail" => $"Traînée après dash : ralentit de {Percent(1f - P("slow_factor"))}, durée {Seconds(Mathf.Min(P("max_seconds"), P("base_seconds") * copies * duration))} (plafond {Seconds(P("max_seconds"))}).",
            "crowd_damage" => $"+{Percent(amount)} dégâts par ennemi à moins de {N(P("radius"))} px ; {N(P("max_enemies"))} ennemis au plus." + (player == null ? "" : $" Présents : {player.ObjectStances?.CrowdCount ?? 0}."),
            "high_hp_damage" => $"+{Percent(amount)} dégâts au-dessus de {Percent(P("threshold"))} des PV.",
            "low_hp_damage" => $"+{Percent(amount)} dégâts sous {Percent(P("threshold"))} des PV.",
            "essence_damage_cap" => $"+{Percent(P("damage_per_step"))} dégâts par {N(P("essence_per_step"))} Essence gardée, plafond +{Percent(amount)}.",
            "peril" => $"+{N(amount)} Péril acquis ; reste après oubli.",
            "revive" => $"{copies} résurrection(s) consommable(s), à {Percent(P("hp_ratio"))} des PV ; {Seconds(P("invulnerability_seconds"))} d’invulnérabilité. Inactif dans le Néant.",
            "dash_blink" => $"Dash instantané, distance ×{N(P("base_distance") + P("distance_per_copy") * (copies - 1))}. Les murs restent infranchissables.",
            "extra_cards" => $"+{N(amount)} choix à chaque montée de niveau.",
            "chest_pointers" => $"Indique les {N(Mathf.Min(P("pointers_per_copy") * copies, P("max_pointers")))} coffre(s) fermé(s) hors écran les plus proches. L’Oubli des repères masque ces flèches.",
            "item_lifetime" => $"+{Seconds(P("extra_seconds_per_copy") * copies)} de présence des objets au sol. L’Effacement les détruit toujours.",
            "free_chest" => $"{Percent(Mathf.Min(P("chance_per_copy") * copies, P("max_chance")))} de gratuité par coffre (plafond {Percent(P("max_chance"))}). Un seul jet mémorisé, même sans Essence ; la Chance n’agit pas dessus.",
            "chest_xp_pull" => $"Fermer le butin d’un nouveau coffre attire l’XP présente à moins de {N(P("radius_per_copy") * copies)} px du coffre. Une seule activation, aucune XP créée.",
            "chest_choices" => $"Chaque récompense de coffre : choisir un objet parmi au plus {N(1f + P("extra_choices_per_copy") * copies)} candidats distincts de la rareté tirée. Aucun objet supplémentaire reçu.",
            "cross_weapon_echo" => $"Deux armes différentes touchent une cible en {Seconds(P("window_seconds"))} : impact de {Percent(amount)} de la somme des deux coups. Recharge globale {Seconds(P("cooldown_seconds"))}, sans réaction en chaîne.",
            "damage_link" => $"Après dash : lien de {Seconds(P("duration_seconds"))} entre les deux dernières cibles touchées à moins de {N(P("radius"))} px. Transfère {Percent(Mathf.Min(amount, P("max_ratio")))} des dégâts d’arme (plafond {Percent(P("max_ratio"))}). Recharge {Seconds(P("cooldown_seconds"))}, sans récursion.",
            "dash_cancel_projectiles" => $"Chaque dash efface les {N(amount)} premiers projectiles ennemis rencontrés. Inactif contre contact, rayons, zones au sol et Néant.",
            "dash_recovery" => $"Après dash : restitue {Percent(amount)} des PV de combat perdus dans les {Seconds(P("history_seconds"))} précédentes. Recharge {Seconds(P("cooldown_seconds"))}. Pertes remboursées consommées, ni Néant ni résurrection.",
            "dash_decoy" => $"Après dash : souvenir au départ pendant {Seconds(P("seconds_per_copy") * copies)}, attire les créatures ordinaires à moins de {N(P("radius"))} px. Recharge {Seconds(P("cooldown_seconds"))}. Sans dégâts ; élites, boss et attaques engagées inchangés.",
            "knockback" or "projectile_speed" or "orbital_speed" or "chain_range" or "homing_strength" or "chain_targets" =>
                DescribeStats(data, copies) + CompatibleWeapons(data.Stat, player),
            _ => DescribeStats(data, copies),
        };
    }

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

    private static string DescribeStats(PassiveSouvenirData data, int copies)
    {
        List<string> values = new();
        foreach (PassiveEffectData effect in data.Effects)
        {
            float value = effect.Neutral + effect.Step * copies;
            values.Add($"{StatCatalog.Name(effect.Stat)} {StatCatalog.FormatBonus(effect.Stat, value, effect.Multiplicative)}");
        }
        return string.Join(" · ", values) + ". " + data.RuleText();
    }

    private static string Percent(float value) => N(value * 100f) + " %";
    private static string N(float value) => value.ToString("0.##", PlayerSheet.French);
}
