using System.Reflection;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Tests;

/// <summary>
/// Statuts raccordés (planche 05 §8, S1b, DECISIONS §70) : le feu de la Lampe brûle, Fragile vaut pour les tics,
/// l'Épingle lit les entravés, une créature figée ne bouge ni n'attaque.
/// </summary>
public partial class ObjectsRegression
{
    private static readonly MethodInfo ProcessIgniteMethod = typeof(Enemy).GetMethod("ProcessIgnite", Private);
    private static readonly MethodInfo ProcessBleedMethod = typeof(Enemy).GetMethod("ProcessBleed", Private);
    private static readonly FieldInfo FreezeTimer = typeof(Enemy).GetField("_freezeTimer", Private);

    /// <summary>
    /// R1 : une créature dans le feu brûle, au DPS de l'ancien feu à tics de 5 PV toutes les 0,5 s, et encore 0,6 s
    /// après le dernier tic. Avant : 20 PV pour une créature qui reste sur la flaque (tics à 0,5, 1, 1,5 et 2 s), 10
    /// pour une créature qui en sort à 1,25 s ; après : 21 et 11. Flaque de 2,1 s : le dernier tic ne dépend pas d'un
    /// arrondi de float.
    /// </summary>
    private void CheckGroundFireBurns()
    {
        Setup();
        _player.AddWeapon(WeaponDataLoader.Get("memory_lantern"));
        AttackContext lamp = _player.BeginAttack(_player.WeaponSlots[1], 10f).As(DamageKind.DamageOverTime);
        Enemy stays = SpawnEnemyAt(new Vector2(-31000f, 17000f));
        Enemy leaves = SpawnEnemyAt(new Vector2(-33000f, 17000f));
        CurrentHp.SetValue(stays, 1000f);
        CurrentHp.SetValue(leaves, 1000f);
        RefreshEnemyCache();
        GroundFire fire = new(GetNode<GroupCache>("/root/GroupCache"));
        float burnSeconds = WeaponDataLoader.Get("memory_lantern").SpecialEffect.Get(SpecialEffectParam.GroundBurnSeconds);
        fire.Add(stays.GlobalPosition, 5f, 2.1f, 10f, burnSeconds, lamp);
        fire.Add(leaves.GlobalPosition, 5f, 2.1f, 10f, burnSeconds, lamp);
        const float step = 1f / 60f;
        bool burningInside = false;
        for (int frame = 0; frame < 180; frame++)
        {
            if (frame == 75)
            {
                leaves.Position += new Vector2(500f, 0f);
                RefreshEnemyCache();
            }
            fire.Process(step);
            ProcessIgniteMethod.Invoke(stays, new object[] { step });
            ProcessIgniteMethod.Invoke(leaves, new object[] { step });
            burningInside |= frame == 50 && stays.IsBurning;
        }
        float stayed = 1000f - Hp(stays);
        float left = 1000f - Hp(leaves);
        AttackContext source = (AttackContext)IgniteSource.GetValue(stays);
        // Tolérance de deux images (0,33 PV) : la Brûlure et la flaque ne tombent pas toujours sur la même image.
        Check(Near(burnSeconds, 0.6f) && burningInside && Mathf.Abs(stayed - 21f) < 0.35f && Mathf.Abs(left - 11f) < 0.35f && source.IsWeaponWork,
            $"Feu de la Lampe : la créature brûle (statut ordinaire, travail de l'arme, {burnSeconds:0.0} s après le feu) ; flaque entière {stayed:0.00} PV (avant : 20), sortie à 1,25 s {left:0.00} PV (avant : 10)");
        stays.QueueFree();
        leaves.QueueFree();
    }

    /// <summary>R2 : Fragile augmente les tics de Brûlure et de saignement du joueur, pas ceux d'une autre source.</summary>
    private void CheckFragileDamageOverTime()
    {
        Setup();
        AttackContext attack = _player.BeginAttack(_player.WeaponSlots[0], 10f);
        Enemy burning = SpawnEnemyAt(new Vector2(-35000f, 17000f));
        Enemy bleeding = SpawnEnemyAt(new Vector2(-37000f, 17000f));
        Enemy foreign = SpawnEnemyAt(new Vector2(-39000f, 17000f));
        foreach (Enemy enemy in new[] { burning, bleeding, foreign })
        {
            CurrentHp.SetValue(enemy, 1000f);
            enemy.ApplyFragile(0.2f, 5f, attack);
        }
        burning.ApplyIgnite(10f, 2f, attack);
        bleeding.ApplyBleed(10f, 2f, attack);
        foreign.ApplyIgnite(10f, 2f);
        ProcessIgniteMethod.Invoke(burning, new object[] { 0.5f });
        ProcessBleedMethod.Invoke(bleeding, new object[] { 0.5f });
        ProcessIgniteMethod.Invoke(foreign, new object[] { 0.5f });
        float burn = 1000f - Hp(burning);
        float bleed = 1000f - Hp(bleeding);
        float other = 1000f - Hp(foreign);
        Check(Near(burn, 6f) && Near(bleed, 6f) && Near(other, 5f),
            $"Fragile +20 % : tic de Brûlure {burn:0.00} et de saignement {bleed:0.00} au lieu de 5 ; source étrangère au joueur {other:0.00}");
        foreach (Enemy enemy in new[] { burning, bleeding, foreign })
            enemy.QueueFree();
    }

    /// <summary>R3 : l'Épingle à nourrice lit les entravés ; correction : son palier prolonge aussi une pause figée.</summary>
    private void CheckHinderedTargets()
    {
        Setup();
        Raise("epingle_a_nourrice", 1);
        Enemy enemy = SpawnEnemyAt(new Vector2(-41000f, 17000f));
        float plain = _player.ResolveHitDamage(enemy, 10f, false);
        enemy.ApplyDisorient(2f);
        float disoriented = _player.ResolveHitDamage(enemy, 10f, false);
        ProcessDisorientMethod(enemy, 3f);
        enemy.Freeze(1f);
        float frozen = _player.ResolveHitDamage(enemy, 10f, false);
        Check(Near(plain, 10f) && Near(disoriented, 10.4f) && Near(frozen, 10.4f),
            $"Épingle à nourrice niveau 1 : +4 % contre une cible désorientée ({disoriented:0.00}) ou figée ({frozen:0.00}), rien sinon ({plain:0.00})");

        FreezeTimer.SetValue(enemy, 0.5f);
        enemy.ExtendSlow(1f, 4f);
        bool extended = Near((float)FreezeTimer.GetValue(enemy), 1.5f);
        for (int i = 0; i < 5; i++)
            enemy.ExtendSlow(1f, 4f);
        Check(extended && Near((float)FreezeTimer.GetValue(enemy), 4f),
            "Épingle à nourrice palier 15 : un voisin seulement figé voit sa pause prolongée de 1 s, 4 s au plus");
        enemy.QueueFree();
    }

    private static void ProcessDisorientMethod(Enemy enemy, float delta) =>
        typeof(Enemy).GetMethod("ProcessDisorient", Private).Invoke(enemy, new object[] { delta, false });

    /// <summary>R4 : figée, une créature au contact n'attaque pas et un tireur ne marche pas ; libérée, elle reprend.</summary>
    private void CheckFrozenCreatures()
    {
        Setup();
        _player.DisableDefenseForTests();
        _player.Position = new Vector2(-45000f, 17000f);
        Enemy melee = SpawnEnemyAt(_player.Position + new Vector2(20f, 0f));
        Enemy shooter = GD.Load<PackedScene>("res://scenes/enemies/Enemy.tscn").Instantiate<Enemy>();
        AddChild(shooter);
        shooter.Initialize(EnemyDataLoader.Get("fading_spitter"), 1f, 1f);
        shooter.SetPhysicsProcess(false);
        shooter.Position = _player.Position + new Vector2(450f, 0f);
        FieldInfo player = typeof(Enemy).GetField("_player", Private);
        MethodInfo physics = typeof(Enemy).GetMethod("_PhysicsProcess");
        foreach (Enemy enemy in new[] { melee, shooter })
        {
            player.SetValue(enemy, _player);
            enemy.Freeze(2.5f);
        }
        float hp = _player.CurrentHp;
        for (int frame = 0; frame < 120; frame++)
        {
            physics.Invoke(melee, new object[] { 1.0 / 60.0 });
            physics.Invoke(shooter, new object[] { 1.0 / 60.0 });
        }
        float frozenLoss = hp - _player.CurrentHp;
        // Hors d'une image physique du moteur, MoveAndSlide ne déplace pas le corps : on lit la vitesse choisie par l'IA.
        float frozenWalk = shooter.Velocity.Length();
        FreezeTimer.SetValue(melee, 0f);
        FreezeTimer.SetValue(shooter, 0f);
        hp = _player.CurrentHp;
        for (int frame = 0; frame < 120; frame++)
        {
            physics.Invoke(melee, new object[] { 1.0 / 60.0 });
            physics.Invoke(shooter, new object[] { 1.0 / 60.0 });
        }
        float freeLoss = hp - _player.CurrentHp;
        float freeWalk = shooter.Velocity.Length();
        Check(Near(frozenLoss, 0f) && frozenWalk < 0.01f && freeLoss > 0f && freeWalk > 10f,
            $"Créature figée 2 s : aucun coup ({frozenLoss:0.0} PV) et tireur arrêté ({frozenWalk:0.0} px/s) ; libérées : {freeLoss:0.0} PV, {freeWalk:0} px/s");
        melee.QueueFree();
        shooter.QueueFree();
    }

    /// <summary>
    /// Cumul des Brûlures : intensité la plus forte, réserve de dégâts la plus grande ; une même Brûlure renouvelée
    /// repart pour sa durée. Feu (10 PV/s, 0,6 s : réserve 6) et Allumette (2,5 PV/s, 3 s : réserve 7,5), dans un
    /// ordre ou dans l'autre : 10 PV/s pendant 0,75 s, au compte du feu, pas 10 PV/s pendant 3 s.
    /// </summary>
    private void CheckBurnStacking()
    {
        Setup();
        _player.AddWeapon(WeaponDataLoader.Get("memory_lantern"));
        AttackContext lamp = _player.BeginAttack(_player.WeaponSlots[1], 10f).As(DamageKind.DamageOverTime);
        AttackContext match = lamp.As(DamageKind.Passive);
        Enemy fireFirst = SpawnEnemyAt(new Vector2(-47000f, 17000f));
        Enemy matchFirst = SpawnEnemyAt(new Vector2(-49000f, 17000f));
        Enemy renewed = SpawnEnemyAt(new Vector2(-51000f, 17000f));
        fireFirst.ApplyIgnite(10f, 0.6f, lamp);
        fireFirst.ApplyIgnite(2.5f, 3f, match);
        matchFirst.ApplyIgnite(2.5f, 3f, match);
        matchFirst.ApplyIgnite(10f, 0.6f, lamp);
        renewed.ApplyIgnite(2.5f, 3f, match);
        IgniteTimer.SetValue(renewed, 1f);
        renewed.ApplyIgnite(2.5f, 3f, match);
        bool ordered = true;
        foreach (Enemy enemy in new[] { fireFirst, matchFirst })
            ordered &= Near((float)IgniteDps.GetValue(enemy), 10f) && Near((float)IgniteTimer.GetValue(enemy), 0.75f)
                && ((AttackContext)IgniteSource.GetValue(enemy)).Kind == DamageKind.DamageOverTime;
        Check(ordered && Near((float)IgniteTimer.GetValue(renewed), 3f),
            "Brûlures du feu et de l'Allumette : 10 PV/s pendant 0,75 s au compte du feu, dans les deux ordres ; une Allumette renouvelée repart pour 3 s");
        foreach (Enemy enemy in new[] { fireFirst, matchFirst, renewed })
            enemy.QueueFree();
    }

    /// <summary>Un tic suit la règle d'un coup : réduit par un affixe, nul sur une créature terrée.</summary>
    private void CheckDamageOverTimeRules()
    {
        Setup();
        AttackContext attack = _player.BeginAttack(_player.WeaponSlots[0], 10f);
        Enemy armored = SpawnEnemyAt(new Vector2(-53000f, 17000f));
        Enemy burrowed = SpawnEnemyAt(new Vector2(-55000f, 17000f));
        typeof(EnemyModifiers).GetProperty("DamageTakenMultiplier").SetValue(armored.Modifiers, 0.5f);
        typeof(Enemy).GetField("_isBurrowed", Private).SetValue(burrowed, true);
        foreach (Enemy enemy in new[] { armored, burrowed })
        {
            CurrentHp.SetValue(enemy, 1000f);
            enemy.ApplyIgnite(10f, 2f, attack);
            ProcessIgniteMethod.Invoke(enemy, new object[] { 0.5f });
        }
        float reduced = 1000f - Hp(armored);
        float hidden = 1000f - Hp(burrowed);
        typeof(Enemy).GetField("_isBurrowed", Private).SetValue(burrowed, false);
        Check(Near(reduced, 2.5f) && Near(hidden, 0f),
            $"Tic de Brûlure : {reduced:0.00} PV sur une créature qui encaisse à 50 % (au lieu de 5), {hidden:0.00} sur une créature terrée");
        armored.QueueFree();
        burrowed.QueueFree();
    }

    /// <summary>Un mini-boss figé garde ses coups, comme il ignore le recul.</summary>
    private void CheckFrozenMiniboss()
    {
        Setup();
        _player.DisableDefenseForTests();
        _player.Position = new Vector2(-57000f, 17000f);
        Enemy boss = SpawnEnemyAt(_player.Position + new Vector2(20f, 0f));
        typeof(Enemy).GetField("_tier", Private).SetValue(boss, EnemyTier.Miniboss);
        typeof(Enemy).GetField("_player", Private).SetValue(boss, _player);
        boss.Freeze(2.5f);
        float hp = _player.CurrentHp;
        MethodInfo physics = typeof(Enemy).GetMethod("_PhysicsProcess");
        for (int frame = 0; frame < 120; frame++)
            physics.Invoke(boss, new object[] { 1.0 / 60.0 });
        Check(hp - _player.CurrentHp > 0f, $"Mini-boss figé : il frappe encore ({hp - _player.CurrentHp:0.0} PV en 2 s)");
        boss.QueueFree();
    }
}
