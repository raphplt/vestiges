using System.Reflection;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.Tests;

/// <summary>
/// Règle commune des synergies (planche 05 §8, S1a, DECISIONS §70) : ce qu'une arme fait (écho, forme, saignement,
/// feu) compte comme un coup et une élimination de cette arme ; ce qu'un objet produit ne déclenche jamais d'objet.
/// </summary>
public partial class ObjectsRegression
{
    private static readonly FieldInfo CurrentHp = typeof(Enemy).GetField("_currentHp", Private);
    private static readonly FieldInfo IgniteSource = typeof(Enemy).GetField("_igniteSource", Private);

    private void RefreshEnemyCache() =>
        CrowdIndex.MarkMoved();

    private Enemy SpawnEnemyAt(Vector2 position)
    {
        Enemy enemy = SpawnEnemy();
        enemy.Position = position;
        return enemy;
    }

    private void CheckSecondaryHits()
    {
        Setup();
        Raise("epingle_a_nourrice", 1);
        _player.AddWeapon(WeaponDataLoader.Get("childs_drawing"));
        WeaponInstance chalk = _player.WeaponSlots[1];
        Enemy primary = SpawnEnemyAt(new Vector2(-23000f, 17000f));
        Enemy neighbour = SpawnEnemyAt(primary.Position + new Vector2(20f, 0f));
        // Assez de PV pour tout le banc, assez peu pour qu'un float distingue 5,2 de 5.
        CurrentHp.SetValue(neighbour, 1000f);
        RefreshEnemyCache();

        // Forme des Craies : 50 % du coup brut, résolus sur la cible qu'elle touche.
        neighbour.ApplySlow(0.5f, 5f);
        float before = Hp(neighbour);
        _player.OnProjectileHit(primary, 10f, false, chalk, _player.BeginAttack(chalk, 10f), 10f);
        float slowedNeighbour = before - Hp(neighbour);
        ClearStatuses(neighbour);
        primary.ApplySlow(0.5f, 5f);
        before = Hp(neighbour);
        _player.OnProjectileHit(primary, _player.ResolveHitDamage(primary, 10f, false), false, chalk, _player.BeginAttack(chalk, 10f), 10f);
        float slowedPrimary = before - Hp(neighbour);
        Check(Near(slowedNeighbour, 5.2f) && Near(slowedPrimary, 5f),
            $"Forme des Craies : un coup de l'arme résolu sur sa cible ; Épingle +4 % si elle est ralentie ({slowedNeighbour:0.00}), rien de la cible principale ({slowedPrimary:0.00})");

        FieldInfo pending = typeof(Player).GetField("_lifestealPending", Private);
        Raise("paille_tordue", 1);
        ClearStatuses(primary);
        pending.SetValue(_player, 0f);
        _player.OnProjectileHit(primary, 0f, false, chalk, _player.BeginAttack(chalk, 10f), 10f);
        Check(Near((float)pending.GetValue(_player), 5f * _player.Lifesteal),
            $"Forme des Craies : ses dégâts nourrissent le vol de vie de la Paille tordue ({(float)pending.GetValue(_player):0.000})");

        Raise("allumette_humide", 30);
        _player.ObjectTriggers.Rng.Seed = 21;
        int burns = 0;
        const int trials = 2000;
        for (int i = 0; i < trials; i++)
        {
            ClearStatuses(neighbour);
            _player.OnProjectileHit(primary, 0f, false, chalk, _player.BeginAttack(chalk, 10f), 0.1f);
            burns += neighbour.IsBurning ? 1 : 0;
        }
        float share = burns / (float)trials;
        AttackContext burnSource = (AttackContext)IgniteSource.GetValue(neighbour);
        Check(share > 0.78f && share < 0.84f && burnSource.Kind == DamageKind.Passive,
            $"Forme des Craies : l'Allumette niveau 30 enflamme la cible de la forme ({share:P0}, 90 % × coefficient 0,9) ; la Brûlure reste celle d'un objet");
        primary.QueueFree();
        neighbour.QueueFree();
    }

    private void CheckWeaponKills()
    {
        Setup();
        Raise("petard_mouille", 15);
        Raise("de_a_coudre", 15);
        _player.DisableDefenseForTests();
        _player.TakeDamage(60f);
        _player.AddWeapon(WeaponDataLoader.Get("heavy_hammer"));
        WeaponInstance hammer = _player.WeaponSlots[1];
        AttackContext hit = _player.BeginAttack(hammer, 20f);
        MethodInfo process = typeof(ObjectTriggers).GetMethod("_Process");

        // Chaque scène : une victime à 1 PV et un voisin qui encaisse sans mourir, loin des autres scènes.
        Enemy[] victims = new Enemy[5];
        Enemy[] neighbours = new Enemy[5];
        for (int i = 0; i < victims.Length; i++)
        {
            victims[i] = SpawnEnemyAt(new Vector2(-23000f - 2000f * i, 21000f));
            neighbours[i] = SpawnEnemyAt(victims[i].Position + new Vector2(30f, 0f));
            CurrentHp.SetValue(victims[i], 1f);
            CurrentHp.SetValue(neighbours[i], 1000f);
        }
        RefreshEnemyCache();

        (float heal, float blast) Kill(int scene, System.Action killer)
        {
            float playerHp = _player.CurrentHp;
            float neighbourHp = Hp(neighbours[scene]);
            killer();
            float result = neighbourHp - Hp(neighbours[scene]);
            // Vide la seconde explosion du palier avant la scène suivante.
            process.Invoke(_player.ObjectTriggers, new object[] { 0.3 });
            return (_player.CurrentHp - playerHp, result);
        }

        (float heal, float blast) echo = Kill(0, () => victims[0].TakeDamage(2f, source: hit.As(DamageKind.SecondaryWeapon)));
        Check(Near(echo.heal, 1.5f) && Near(echo.blast, 3f),
            $"Élimination par un écho d'arme : le Dé à coudre soigne ({echo.heal:0.00}), le Pétard explose à 150 % du coup ({echo.blast:0.00})");

        victims[1].ApplyBleed(30f, 1f, hit);
        (float heal, float blast) bleed = Kill(1, () => typeof(Enemy).GetMethod("ProcessBleed", Private).Invoke(victims[1], new object[] { 0.05f }));
        Check(victims[1].IsDying && Near(bleed.heal, 1.5f) && Near(bleed.blast, 30f),
            $"Élimination par un saignement d'arme : soin ({bleed.heal:0.00}) et explosion à 150 % du coup d'arme qui l'a posé, pas du dernier tic ({bleed.blast:0.0})");

        GroundFire fire = new();
        fire.Add(victims[2].GlobalPosition, 5f, 2f, 10f, 0.5f, hit.As(DamageKind.DamageOverTime));
        (float heal, float blast) flame = Kill(2, () =>
        {
            fire.Process(0.5f);
            typeof(Enemy).GetMethod("ProcessIgnite", Private).Invoke(victims[2], new object[] { 0.2f });
        });
        Check(victims[2].IsDying && Near(flame.heal, 1.5f) && Near(flame.blast, 30f),
            $"Élimination dans le feu de la Lampe : une élimination par l'arme ({flame.heal:0.00} PV, explosion {flame.blast:0.0})");

        victims[3].ApplyIgnite(30f, 1f, hit.As(DamageKind.Passive));
        (float heal, float blast) burn = Kill(3, () => typeof(Enemy).GetMethod("ProcessIgnite", Private).Invoke(victims[3], new object[] { 0.05f }));
        Check(victims[3].IsDying && Near(burn.heal, 0f) && Near(burn.blast, 0f),
            "Élimination par la Brûlure de l'Allumette, un objet : ni soin ni explosion");

        // Le voisin tué par l'explosion ne réexplose pas : le troisième, hors de portée de la victime, reste intact.
        CurrentHp.SetValue(neighbours[4], 1f);
        Enemy third = SpawnEnemyAt(neighbours[4].Position + new Vector2(45f, 0f));
        RefreshEnemyCache();
        float thirdHp = Hp(third);
        (float heal, float blast) chain = Kill(4, () => victims[4].TakeDamage(20f, source: hit));
        Check(neighbours[4].IsDying && Near(chain.heal, 1.5f) && Near(Hp(third), thirdHp),
            $"Une mort par l'explosion du Pétard ne soigne pas et ne réexplose pas ({chain.heal:0.00} PV rendus, voisin suivant intact)");

        foreach (Enemy enemy in victims)
            enemy.QueueFree();
        foreach (Enemy enemy in neighbours)
            enemy.QueueFree();
        third.QueueFree();
    }
}
