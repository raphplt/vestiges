using System.Collections.Generic;
using System.Reflection;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.Tests;

/// <summary>
/// Offre de la Propagation et corrections relevées par la planche des synergies (planche 05 §8, S1c, DECISIONS §70).
/// </summary>
public partial class ObjectsRegression
{
    /// <summary>R7 : la Propagation est offerte et active quand l'équipement ralentit ou désoriente vraiment.</summary>
    private void CheckPropagationOffer()
    {
        PerkSpecializationData propagation = PerkSpecializationDataLoader.Get("carry_control");
        HashSet<string> none = new();

        Setup();
        bool bowOnly = PerkSpecializationOffers.IsEligible(propagation, _player, none);
        Raise("glacon", 1);
        bool withIce = PerkSpecializationOffers.IsEligible(propagation, _player, none)
            && PerkSpecializationOffers.IsActive(propagation, _player);

        Setup();
        _player.AddWeapon(WeaponDataLoader.Get("clock_hand"));
        WeaponInstance clock = _player.WeaponSlots[1];
        bool slowingField = PerkSpecializationOffers.IsEligible(propagation, _player, none);
        typeof(WeaponInstance).GetField("_specialEffect", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(clock, clock.Base.SpecialEffect.With(new Dictionary<string, float> { [SpecialEffectParam.FreezeSeconds] = 0.5f }));
        bool freezingField = PerkSpecializationOffers.IsEligible(propagation, _player, none);

        Check(!bowOnly && withIce && slowingField && !freezingField,
            $"Propagation : offerte avec le Glaçon ({withIce}) ou le Chronomètre qui ralentit ({slowingField}) ; pas avec l'Arc seul ({bowOnly}) ni l'Arrêt sur image, qui fige ({freezingField})");
    }

    /// <summary>Le compteur de coups du Scalpel repart de zéro quand l'arme quitte l'inventaire.</summary>
    private void CheckScalpelCounterReset()
    {
        Setup();
        _player.AddWeapon(WeaponDataLoader.Get("surgeons_scalpel"));
        WeaponInstance scalpel = _player.WeaponSlots[1];
        Enemy enemy = SpawnEnemyAt(new Vector2(-59000f, 17000f));
        CurrentHp.SetValue(enemy, 1000f);
        for (int i = 0; i < 3; i++)
            _player.OnProjectileHit(enemy, 1f, false, scalpel, _player.BeginAttack(scalpel, 1f));
        Dictionary<string, int> counters = (Dictionary<string, int>)typeof(Player).GetField("_weaponHitCounters", Private).GetValue(_player);
        bool counted = counters.TryGetValue(scalpel.Id, out int before) && before == 3;
        _player.RemoveWeapon(1);
        Check(counted && !counters.ContainsKey(scalpel.Id),
            $"Scalpel : 3 coups comptés, compteur effacé quand l'arme part ({counters.ContainsKey(scalpel.Id)})");
        enemy.QueueFree();
    }

    /// <summary>Désorientée de nouveau avant la fin, une créature garde son cap au lieu de trembler sur place.</summary>
    private void CheckSteadyDisorientation()
    {
        Setup();
        Enemy enemy = SpawnEnemyAt(new Vector2(-61000f, 17000f));
        FieldInfo heading = typeof(Enemy).GetField("_disorientDirection", Private);
        enemy.ApplyDisorient(0.6f);
        Vector2 first = (Vector2)heading.GetValue(enemy);
        bool steady = true;
        for (int frame = 0; frame < 60; frame++)
        {
            enemy.ApplyDisorient(0.6f);
            steady &= (Vector2)heading.GetValue(enemy) == first;
        }
        bool recorded = Near((float)typeof(Enemy).GetField("_disorientDuration", Private).GetValue(enemy), 0.6f);
        typeof(Enemy).GetMethod("ProcessDisorient", Private).Invoke(enemy, new object[] { 1f, false });
        int changed = 0;
        for (int trial = 0; trial < 20; trial++)
        {
            Vector2 previous = (Vector2)heading.GetValue(enemy);
            enemy.ApplyDisorient(0.1f);
            changed += (Vector2)heading.GetValue(enemy) != previous ? 1 : 0;
            typeof(Enemy).GetMethod("ProcessDisorient", Private).Invoke(enemy, new object[] { 1f, false });
        }
        Check(steady && recorded && changed > 15,
            $"Désorientation renouvelée à chaque image (cône en Fréquence pirate) : même cap pendant 1 s, durée gardée pour la Pince ({recorded}) ; nouvelle désorientation après la fin : cap retiré {changed}/20");
        enemy.QueueFree();
    }
}
