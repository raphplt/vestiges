using System.Collections.Generic;

namespace Vestiges.Infrastructure;

/// <summary>
/// Faits de run qu'une condition de quête peut observer (plan 06 §9.7). Une condition nomme l'un d'eux, une cible et,
/// selon le fait, un réglage (<c>param</c>) ou une arme. Le suivi en run est fait par <c>Progression.QuestTracker</c>.
/// </summary>
public static class QuestStat
{
    public const string SovereignKills = "sovereign_kills";
    public const string SovereignCritKills = "sovereign_crit_kills";
    public const string BarrierDefeated = "barrier_defeated";
    public const string IndicibleDefeated = "indicible_defeated";
    public const string CrisesAfterIndicible = "crises_after_indicible";
    public const string MemorialsRevived = "memorials_revived";
    public const string CrisesSurvived = "crises_survived";
    public const string DistanceMeters = "distance_m";
    public const string WaymarksFound = "waymarks_found";
    public const string ErasedZoneKills = "erased_zone_kills";
    public const string MeleeKills = "melee_kills";
    public const string RangedKills = "ranged_kills";
    public const string WeaponKills = "weapon_kills";
    /// <summary>Armes tenues en même temps au niveau <c>param</c> ou plus.</summary>
    public const string WeaponsAtLevel = "weapons_at_level";
    public const string EventsSucceeded = "events_succeeded";
    public const string ZonesDiscovered = "zones_discovered";
    public const string ChestsOpened = "chests_opened";
    public const string Level = "level";
    public const string SlowedKills = "slowed_kills";
    public const string BurningKills = "burning_kills";
    public const string CritKills = "crit_kills";
    public const string EliteCritKills = "elite_crit_kills";
    /// <summary>Le moins d'Oublis portés pendant une Résurgence traversée en entier, au mieux sur la run.</summary>
    public const string OublisThroughCrisis = "oublis_through_crisis";
    public const string DamageTaken = "damage_taken";
    public const string HpHealed = "hp_healed";
    /// <summary>Éliminations à moins de <c>param</c> pixels du joueur.</summary>
    public const string CloseKills = "close_kills";
    public const string StillKills = "still_kills";
    /// <summary>Le plus d'éliminations dans une fenêtre de <c>param</c> secondes.</summary>
    public const string KillBurst = "kill_burst";
    /// <summary>Éliminations pendant que les PV sont sous la part <c>param</c> du maximum.</summary>
    public const string LowHpKills = "low_hp_kills";
    /// <summary>Secondes de run écoulées avant de passer sous la part <c>param</c> des PV max.</summary>
    public const string HealthyTime = "healthy_time";
    /// <summary>Résurgences traversées en perdant moins que la part <c>param</c> des PV max.</summary>
    public const string CleanCrises = "clean_crises";
    public const string EssenceHeld = "essence_held";
    public const string Peril = "peril";
    public const string Ascensions = "ascensions";

    /// <summary>Faits dont la condition exige un réglage <c>param</c>.</summary>
    public static readonly HashSet<string> WithParam = new()
    {
        WeaponsAtLevel, CloseKills, KillBurst, LowHpKills, HealthyTime, CleanCrises,
    };

    /// <summary>Comptes qui s'additionnent d'une run à l'autre : seuls admis par une quête de cumul.</summary>
    public static readonly HashSet<string> Cumulable = new()
    {
        SovereignKills, MemorialsRevived, CrisesSurvived, DistanceMeters, ErasedZoneKills, MeleeKills, RangedKills,
        WeaponKills, EventsSucceeded, ZonesDiscovered, ChestsOpened, SlowedKills, BurningKills, CritKills, DamageTaken,
        HpHealed, CloseKills, StillKills, LowHpKills, Ascensions,
    };

    public static readonly HashSet<string> All = new()
    {
        SovereignKills, SovereignCritKills, BarrierDefeated, IndicibleDefeated, CrisesAfterIndicible, MemorialsRevived,
        CrisesSurvived, DistanceMeters, WaymarksFound, ErasedZoneKills, MeleeKills, RangedKills, WeaponKills,
        WeaponsAtLevel, EventsSucceeded, ZonesDiscovered, ChestsOpened, Level, SlowedKills, BurningKills, CritKills,
        EliteCritKills, OublisThroughCrisis, DamageTaken, HpHealed, CloseKills, StillKills, KillBurst, LowHpKills,
        HealthyTime, CleanCrises, EssenceHeld, Peril, Ascensions,
    };
}
