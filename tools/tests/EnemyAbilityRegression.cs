using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Combat.Abilities;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Spawn;

namespace Vestiges.Tests;

/// <summary>
/// Banc isolé des capacités ennemies (Présage, bond du Charognard, charge, surgissement, cri), de la meute, du contrat des fiches
/// (fichier .Contract.cs) et du retour de coup : vrais Player et Enemy,
/// ticks pilotés par le banc, recharges forcées pour des scénarios déterministes.
/// </summary>
public partial class EnemyAbilityRegression : Node2D
{
    private const float Dt = 1f / 60f;
    private static readonly PackedScene EnemyScene = GD.Load<PackedScene>("res://scenes/enemies/Enemy.tscn");

    private Player _player;
    private readonly List<Enemy> _enemies = new();
    private int _failures;

    public override async void _Ready()
    {
        try
        {
            GetNode<GameManager>("/root/GameManager").ChangeState(GameManager.GameState.Run);
            _player = GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate<Player>();
            AddChild(_player);
            _player.InitializeCharacter(CharacterDataLoader.Get("traqueur"));
            _player.DisableDefenseForTests();
            _player.SetPhysicsProcess(false);
            _player.IsAIControlled = true;
            // Arme automatique coupée : seuls les effets des capacités ennemies sont observés.
            foreach (Node child in _player.GetChildren())
                if (child is Timer weaponTimer)
                    weaponTimer.Stop();
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

            RunContractChecks();
            await RunPackChecks();
            await RunOmenChecks();
            await RunPounceChecks();
            await RunHitFeedbackChecks();
            await RunPoolReuseChecks();
            await RunStatusVisualChecks();
            await RunDotNumberChecks();
            await RunImpactFxChecks();
            await RunPlayerStatusChecks();
            await RunExplosionWarningChecks();
            await RunChargeChecks();
            await RunBurrowChecks();
            await RunCryChecks();
            await RunAimedShotChecks();
            await RunKnockbackChecks();

            GD.Print($"[EnemyAbilityRegression] RESULT failures={_failures}");
            GetTree().Quit(_failures == 0 ? 0 : 1);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(2);
        }
    }

    private async Task RunOmenChecks()
    {
        // Fuite en ligne droite : la marque attend le joueur une seconde plus loin.
        Enemy omen = await SpawnReady("presage", new Vector2(-150f, 0f));
        await Flee(Vector2.Right, 10);
        ForceCooldown(omen, EnemyAbilityKind.OmenStrike);
        Vector2 expected = _player.Position + Vector2.Right * _player.Speed * 1f;
        await Step(1);
        GroundTelegraph marker = omen.GetNode<GroundTelegraph>("OmenMarker");
        Check(marker.Visible && marker.GlobalPosition.DistanceTo(expected) < 6f,
            $"Présage : marque à la position anticipée ({marker.GlobalPosition} ≈ {expected})");
        Check(omen.Velocity == Vector2.Zero, "Présage : immobile pendant l'incantation");
        float hp = _player.CurrentHp;
        await Step(62);
        Check(_player.CurrentHp < hp, $"Présage : fuite en ligne droite touchée (PV {hp} → {_player.CurrentHp})");
        Despawn(omen);

        // Changement de direction pendant l'annonce : aucune touche.
        omen = await SpawnReady("presage", new Vector2(-150f, 0f));
        await Flee(Vector2.Right, 10);
        await WaitHurtRecovery();
        ForceCooldown(omen, EnemyAbilityKind.OmenStrike);
        await Step(20);
        _player.AIInputOverride = Vector2.Down;
        hp = _player.CurrentHp;
        await Step(45);
        Check(_player.CurrentHp >= hp - 0.001f, "Présage : changement de direction évite l'impact");
        Despawn(omen);

        // Immobile : la marque tombe sur le joueur.
        omen = await SpawnReady("presage", new Vector2(-150f, 0f));
        _player.AIInputOverride = Vector2.Zero;
        await Step(10);
        await WaitHurtRecovery();
        ForceCooldown(omen, EnemyAbilityKind.OmenStrike);
        hp = _player.CurrentHp;
        await Step(65);
        Check(_player.CurrentHp < hp, "Présage : joueur immobile touché");
        Despawn(omen);

        // Hors incantation (recharge), le Présage ne tire aucun projectile classique.
        omen = await SpawnReady("presage", new Vector2(-150f, 0f));
        _player.AIInputOverride = Vector2.Zero;
        await Step(180);
        int projectiles = 0;
        foreach (Node child in GetTree().CurrentScene.GetChildren())
            projectiles += child is EnemyProjectile ? 1 : 0;
        Check(projectiles == 0, $"Présage : aucune attaque de base hors incantation ({projectiles} projectiles)");
        Despawn(omen);

        // Plafond partagé, lu dans les données : deux Présages prêts de plus que le plafond.
        int cap = Mathf.RoundToInt(EnemyDataLoader.Get("presage").Abilities[EnemyAbilityKind.OmenStrike].Number("max_simultaneous"));
        Enemy[] casters = new Enemy[cap + 2];
        for (int i = 0; i < casters.Length; i++)
            casters[i] = await SpawnReady("presage", new Vector2(-150f, -60f + 40f * i));
        foreach (Enemy caster in casters)
            ForceCooldown(caster, EnemyAbilityKind.OmenStrike);
        await Step(1);
        int visible = 0;
        foreach (Enemy caster in casters)
            visible += caster.GetNode<GroundTelegraph>("OmenMarker").Visible ? 1 : 0;
        Check(visible == cap, $"Présage : plafond de marques simultanées ({visible}/{cap})");

        // La mort d'un lanceur retire sa marque et libère une place.
        Enemy dying = Array.Find(casters, c => c.GetNode<GroundTelegraph>("OmenMarker").Visible);
        dying.TakeDamage(float.MaxValue);
        Check(!dying.GetNode<GroundTelegraph>("OmenMarker").Visible, "Présage : la mort annule la marque");
        await Step(1);
        visible = 0;
        foreach (Enemy caster in casters)
            visible += caster != dying && caster.GetNode<GroundTelegraph>("OmenMarker").Visible ? 1 : 0;
        Check(visible == cap, $"Présage : place libérée après la mort ({visible}/{cap})");
        foreach (Enemy caster in casters)
            Despawn(caster);
        await Step(1);
    }

    private async Task RunPounceChecks()
    {
        _player.AIInputOverride = Vector2.Zero;
        _player.Position = Vector2.Zero;
        await WaitHurtRecovery();

        // Joueur immobile : l'annonce montre la trajectoire, puis le bond le rattrape.
        Enemy pouncer = await SpawnReady("charognard", new Vector2(-100f, 0f));
        ForceCooldown(pouncer, EnemyAbilityKind.Pounce);
        Vector2 start = pouncer.Position;
        await Step(1);
        GroundTelegraph marker = pouncer.GetNode<GroundTelegraph>("PounceMarker");
        Check(marker.Visible && pouncer.Velocity == Vector2.Zero, "Bond : annonce visible, ennemi immobile");
        float hp = _player.CurrentHp;
        float peakSpeed = 0f;
        for (int tick = 0; tick < 36; tick++)
        {
            await Step(1);
            peakSpeed = Math.Max(peakSpeed, pouncer.Velocity.Length());
        }
        Check(_player.CurrentHp < hp, $"Bond : joueur immobile touché (PV {hp} → {_player.CurrentHp})");
        Check(peakSpeed > _player.Speed * 2f, $"Bond : plus rapide que le joueur ({peakSpeed:F0} px/s)");
        Check(Math.Abs(pouncer.Position.DistanceTo(start) - 120f) < 6f,
            $"Bond : distance stable ({pouncer.Position.DistanceTo(start):F1} px)");
        await Step(5);
        Check(pouncer.Velocity == Vector2.Zero, "Bond : récupération immobile après l'impact");
        Despawn(pouncer);

        // Pas de côté pendant l'annonce : direction verrouillée, bond évité.
        _player.Position = Vector2.Zero;
        await WaitHurtRecovery();
        pouncer = await SpawnReady("charognard", new Vector2(-100f, 0f));
        ForceCooldown(pouncer, EnemyAbilityKind.Pounce);
        await Step(1);
        _player.AIInputOverride = Vector2.Down;
        hp = _player.CurrentHp;
        await Step(36);
        Check(_player.CurrentHp >= hp - 0.001f, "Bond : pas de côté pendant l'annonce évite l'impact");
        Despawn(pouncer);
    }

    /// <summary>Plan 02 J1 : recul sur le visuel seul, retour au repos, chiffres additionnés par cible, critique à part.</summary>
    private async Task RunHitFeedbackChecks()
    {
        CombatPools pools = new() { Name = "CombatPools" };
        AddChild(pools);
        Enemy enemy = await SpawnReady("rodeur", new Vector2(80f, 0f));
        AnimatedSprite2D sprite = enemy.GetNode<AnimatedSprite2D>("Sprite");
        Polygon2D visual = enemy.GetNode<Polygon2D>("Visual");
        Node2D shown = sprite.Visible ? sprite : visual;
        // Un tick : l'ennemi repère le joueur, d'où vient le coup.
        await Step(1);
        Vector2 body = enemy.Position;

        enemy.TakeDamage(5f);
        Check(enemy.Position == body && shown.Position.X > 1f,
            $"Coup : recul du visuel ({shown.Position.X:F1} px) sans déplacer le corps");
        Check(shown.Scale.X > 1.1f && shown.Scale.Y < 0.9f, $"Coup : écrasement ({shown.Scale})");
        await Step(2);
        enemy.TakeDamage(7f);
        Check(VisibleNumbers(pools, out string texts) == 1 && texts == "12",
            $"Coups rapprochés : un seul chiffre additionné ({texts})");
        enemy.TakeDamage(20f, true);
        Check(VisibleNumbers(pools, out texts) == 2 && texts.Contains("20!"), $"Critique : chiffre distinct ({texts})");

        await Step(30);
        Check(shown.Position == Vector2.Zero && shown.Scale == Vector2.One, "Coup : visuel revenu au repos");
        if (sprite.Visible && sprite.Material is ShaderMaterial material)
            Check(material.GetShaderParameter("flash_amount").AsSingle() == 0f && sprite.SelfModulate == Colors.White,
                "Coup : flash éteint");
        enemy.SetWindupPose(true);
        enemy.TakeDamage(1f);
        await Step(30);
        Check(shown.Scale.IsEqualApprox(new Vector2(1.18f, 0.78f)), $"Coup pendant une annonce : posture conservée ({shown.Scale})");
        enemy.SetWindupPose(false);

        // Onde de montée de niveau (J4) : créature repoussée en apparence, corps immobile, sans flash.
        await Step(30);
        enemy.Position = _player.Position + new Vector2(60f, 0f);
        body = enemy.Position;
        CrowdIndex.MarkMoved();
        LevelUpFx.Play(_player);
        bool pushed = shown.Position.X > 1f && enemy.Position == body;
        bool noFlash = !(sprite.Visible && sprite.Material is ShaderMaterial shoveMaterial)
            || shoveMaterial.GetShaderParameter("flash_amount").AsSingle() == 0f;
        Check(pushed && noFlash, $"Montée de niveau : créature repoussée en apparence ({shown.Position.Length():F1} px), corps immobile, sans flash");
        await Step(30);
        Check(shown.Position == Vector2.Zero, "Montée de niveau : visuel revenu en place");
        Despawn(enemy);

        // Budget par frame : au-delà du plafond, les étoiles d'impact de la frame sont écartées.
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        long droppedBefore = FxBudget.DroppedCount(FxBudgetKind.Shapes);
        int stars = -VisibleFx(pools);
        for (int i = 0; i < 100; i++)
            pools.ShowHitFlash(_player.Position + new Vector2(i, 0f));
        stars += VisibleFx(pools);
        long dropped = FxBudget.DroppedCount(FxBudgetKind.Shapes) - droppedBefore;
        Check(stars > 0 && stars < 100 && stars + dropped == 100, $"Budget d'effets : {stars} étoiles jouées, {dropped} écartées sur 100");
        pools.QueueFree();
    }

    /// <summary>
    /// Plan 07 lot B, étape 5 : une créature rendue au pool en plein état temporaire (variante, affixe, brûlure,
    /// saignement, ralentissement, désorientation, traversée, enfouissement, recul) repart neuve à sa réutilisation.
    /// </summary>
    private async Task RunPoolReuseChecks()
    {
        EnemyPool pool = new() { Name = "EnemyPool", InitialSize = 0 };
        AddChild(pool);
        BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
        float Field(Enemy target, string name) => Convert.ToSingle(typeof(Enemy).GetField(name, flags).GetValue(target));

        Enemy dirty = pool.Get();
        AddChild(dirty);
        dirty.Initialize(EnemyDataLoader.Get("void_brute"), 1f, 1f);
        dirty.SetTicking(false);
        dirty.ApplyVariant(EnemyVariantDataLoader.GetVariant("aberration"),
            new List<EnemyAffixData> { EnemyVariantDataLoader.GetAffix("swift") });
        dirty.ApplyIgnite(5f, 10f);
        dirty.ApplyBleed(5f, 10f);
        dirty.ApplySlow(0.4f, 10f);
        dirty.ApplyDisorient(10f);
        dirty.StartTravel(Vector2.Right, 2f, 10f);
        typeof(Enemy).GetField("_isBurrowed", flags).SetValue(dirty, true);
        dirty.CollisionLayer = 0;
        dirty.Modulate = new Color(1f, 1f, 1f, 0.35f);
        dirty.TakeDamage(3f);
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        pool.Return(dirty);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        Enemy reused = pool.Get();
        Check(reused == dirty, "Pool : la créature rendue est bien celle qu'on réutilise");
        AddChild(reused);
        EnemyData rodeur = EnemyDataLoader.Get("rodeur");
        reused.Initialize(rodeur, 1f, 1f);
        reused.SetTicking(false);
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

        Check(!reused.Modifiers.IsVariant && reused.Modifiers.Affixes.Count == 0 && !reused.Modifiers.IsTraveling,
            "Pool : ni variante, ni affixe, ni traversée hérités");
        Check(Field(reused, "_igniteDps") == 0f && Field(reused, "_bleedDps") == 0f && Field(reused, "_slowFactor") == 1f
              && Field(reused, "_disorientTimer") == 0f, "Pool : brûlure, saignement, ralentissement et désorientation effacés");
        Check(Field(reused, "_speed") == rodeur.Stats.Speed && reused.HpRatio == 1f,
            $"Pool : vitesse et PV de la nouvelle fiche ({Field(reused, "_speed")} pour {rodeur.Stats.Speed})");
        Check(reused.Scale == Vector2.One && reused.Modulate == Colors.White && reused.CollisionLayer == 2,
            $"Pool : taille, opacité et collisions d'origine (échelle {reused.Scale}, alpha {reused.Modulate.A}, couche {reused.CollisionLayer})");
        Check(reused.GetNodeOrNull("AberrationAura") == null && reused.GetNodeOrNull("Nameplate") == null,
            "Pool : ni aura d'Aberration ni plaque de nom restées accrochées");
        AnimatedSprite2D sprite = reused.GetNode<AnimatedSprite2D>("Sprite");
        if (sprite.Visible && sprite.Material is ShaderMaterial material)
            Check(material.GetShaderParameter("aberration_amount").AsSingle() == 0f
                  && material.GetShaderParameter("flash_amount").AsSingle() == 0f, "Pool : shader sans aberration ni flash");
        Check(reused.IsInGroup("enemies") && reused.IsActive && !reused.IsDying, "Pool : créature active et recensée");

        reused.QueueFree();
        pool.QueueFree();
    }

    /// <summary>
    /// Brute du Vide (plan 07 lot B) : charge annoncée par un couloir au sol, seulement à portée, à la vitesse de la
    /// fiche, évitable d'un pas de côté pendant l'annonce, suivie d'une récupération immobile.
    /// </summary>
    private async Task RunChargeChecks()
    {
        _player.AIInputOverride = Vector2.Zero;
        _player.Position = Vector2.Zero;
        await WaitHurtRecovery();
        EnemyAbilityData charge = EnemyDataLoader.Get("void_brute").Abilities[EnemyAbilityKind.Charge];
        float range = charge.Number("trigger_range");
        float windupTicks = charge.Number("windup_seconds") / Dt;
        float leapTicks = charge.Number("leap_seconds") / Dt;
        float expectedSpeed = charge.Number("distance") / charge.Number("leap_seconds");

        Enemy brute = await SpawnReady("void_brute", new Vector2(range + 150f, 0f));
        GroundTelegraph marker = brute.GetNode<GroundTelegraph>("ChargeMarker");
        ForceCooldown(brute, EnemyAbilityKind.Charge);
        await Step(2);
        Check(!marker.Visible, $"Charge : hors de portée ({range + 150f:F0} px > {range:F0}), pas d'annonce");

        brute.Position = _player.Position + new Vector2(-range * 0.6f, 0f);
        ForceCooldown(brute, EnemyAbilityKind.Charge);
        await Step(1);
        Check(marker.Visible && brute.Velocity == Vector2.Zero, "Charge : annonce au sol, Brute immobile");
        float hp = _player.CurrentHp;
        float peakSpeed = 0f;
        for (int tick = 0; tick < windupTicks + leapTicks; tick++)
        {
            await Step(1);
            peakSpeed = Math.Max(peakSpeed, brute.Velocity.Length());
        }
        Check(Mathf.IsEqualApprox(peakSpeed, expectedSpeed, 2f), $"Charge : vitesse de la fiche ({peakSpeed:F0} px/s pour {expectedSpeed:F0})");
        Check(_player.CurrentHp < hp, $"Charge : joueur immobile touché (PV {hp} → {_player.CurrentHp})");
        await Step(5);
        Check(brute.Velocity == Vector2.Zero, "Charge : récupération immobile après la charge");
        Despawn(brute);

        _player.Position = Vector2.Zero;
        await WaitHurtRecovery();
        brute = await SpawnReady("void_brute", new Vector2(-range * 0.6f, 0f));
        ForceCooldown(brute, EnemyAbilityKind.Charge);
        await Step(1);
        _player.AIInputOverride = Vector2.Down;
        hp = _player.CurrentHp;
        await Step((int)(windupTicks + leapTicks));
        Check(_player.CurrentHp >= hp - 0.001f, "Charge : pas de côté pendant l'annonce évite le coup");
        _player.AIInputOverride = Vector2.Zero;
        Despawn(brute);
    }

    /// <summary>
    /// Rampant (plan 07 lot B) : enfoui, il ne touche pas et ne prend pas de coups ; son surgissement s'annonce au
    /// sol, frappe qui reste dans la zone et épargne qui s'en écarte.
    /// </summary>
    private async Task RunBurrowChecks()
    {
        _player.AIInputOverride = Vector2.Zero;
        _player.Position = Vector2.Zero;
        await WaitHurtRecovery();
        EnemyAbilityData burrow = EnemyDataLoader.Get("rampant").Abilities[EnemyAbilityKind.Burrow];
        int burrowTicks = Mathf.CeilToInt(burrow.Number("burrow_seconds") / Dt);
        int warningTicks = Mathf.CeilToInt(burrow.Number("warning_seconds") / Dt);

        Enemy rampant = await SpawnReady("rampant", new Vector2(-20f, 0f));
        GroundTelegraph marker = rampant.GetNode<GroundTelegraph>("BurrowMarker");
        ForceTimer(rampant, EnemyAbilityKind.Burrow, "_timer");
        await Step(1);
        float hp = _player.CurrentHp;
        float enemyHp = rampant.HpRatio;
        rampant.TakeDamage(50f);
        await Step(burrowTicks - 4);
        Check(rampant.HpRatio == enemyHp && rampant.CollisionLayer == 0, "Rampant : enfoui, ni dégâts reçus ni collision");
        Check(_player.CurrentHp >= hp - 0.001f, "Rampant : enfoui, aucun coup au contact");
        Check(!marker.Visible, "Rampant : pas d'annonce avant la fin de l'enfouissement");
        await Step(5);
        Check(marker.Visible && rampant.Velocity == Vector2.Zero, "Rampant : surgissement annoncé au sol, créature immobile");
        await Step(warningTicks + 1);
        Check(_player.CurrentHp < hp && rampant.CollisionLayer == 2, $"Rampant : surgissement sur le joueur resté dans la zone (PV {hp} → {_player.CurrentHp})");
        Despawn(rampant);

        _player.Position = Vector2.Zero;
        await WaitHurtRecovery();
        rampant = await SpawnReady("rampant", new Vector2(-20f, 0f));
        ForceTimer(rampant, EnemyAbilityKind.Burrow, "_timer");
        await Step(burrowTicks + 1);
        _player.AIInputOverride = Vector2.Right;
        hp = _player.CurrentHp;
        await Step(warningTicks + 1);
        Check(_player.CurrentHp >= hp - 0.001f, "Rampant : s'écarter pendant l'annonce évite le surgissement");
        _player.AIInputOverride = Vector2.Zero;
        Despawn(rampant);
    }

    /// <summary>
    /// Hurleur (plan 07 lot B) : cri annoncé au sol, créature immobile ; le tuer pendant l'annonce coupe l'appel,
    /// sinon les renforts de la fiche surgissent autour de lui.
    /// </summary>
    private async Task RunCryChecks()
    {
        EnemyPool pool = new() { Name = "EnemyPool", InitialSize = 0 };
        AddChild(pool);
        EnemyAbilityData cry = EnemyDataLoader.Get("hurleur").Abilities[EnemyAbilityKind.Cry];
        int windupTicks = Mathf.CeilToInt(cry.Number("windup_seconds") / Dt);
        int expected = Mathf.RoundToInt(cry.Number("reinforcements"));
        _player.Position = Vector2.Zero;

        Enemy hurleur = await SpawnReady("hurleur", new Vector2(-150f, 0f));
        GroundTelegraph marker = hurleur.GetNode<GroundTelegraph>("CryMarker");
        int before = CountEnemies();
        ForceCooldown(hurleur, EnemyAbilityKind.Cry);
        await Step(1);
        Check(marker.Visible && hurleur.Velocity == Vector2.Zero, "Hurleur : cri annoncé au sol, créature immobile");
        hurleur.TakeDamage(float.MaxValue);
        Check(!marker.Visible, "Hurleur : sa mort efface l'annonce");
        await Step(windupTicks + 2);
        Check(CountEnemies() - before <= 0, "Hurleur : tué pendant l'annonce, aucun renfort");
        Despawn(hurleur);
        await Step(40);

        hurleur = await SpawnReady("hurleur", new Vector2(-150f, 0f));
        before = CountEnemies();
        ForceCooldown(hurleur, EnemyAbilityKind.Cry);
        await Step(windupTicks + 2);
        int spawned = CountEnemies() - before;
        Check(spawned == expected, $"Hurleur : {spawned} renforts au terme du cri (fiche : {expected})");
        foreach (Node child in GetChildren())
            if (child is Enemy enemy && enemy != hurleur && !_enemies.Contains(enemy))
                enemy.QueueFree();
        Despawn(hurleur);
        pool.QueueFree();
    }

    /// <summary>
    /// Tir annoncé (plan 07 lot B) : le Cracheur s'arrête et vise avant de tirer, dans la direction verrouillée au début
    /// de l'annonce ; la Sentinelle montre sa portée au sol et ne vise qu'à l'intérieur, distance mesurée au sol.
    /// </summary>
    private async Task RunAimedShotChecks()
    {
        CombatPools pools = new() { Name = "CombatPools" };
        AddChild(pools);
        _player.AIInputOverride = Vector2.Zero;
        _player.Position = Vector2.Zero;
        await WaitHurtRecovery();
        int windupTicks = Mathf.CeilToInt(EnemyDataLoader.Get("fading_spitter").Abilities[EnemyAbilityKind.AimedShot].Number("windup_seconds") / Dt);

        Enemy spitter = await SpawnReady("fading_spitter", new Vector2(-150f, 0f));
        GroundTelegraph lane = spitter.GetNode<GroundTelegraph>("AimMarker");
        ForceCooldown(spitter, EnemyAbilityKind.AimedShot);
        await Step(1);
        Check(lane.Visible && spitter.Velocity == Vector2.Zero && ActiveProjectiles(pools).Count == 0,
            "Tir annoncé : couloir de visée, tireur immobile, rien de tiré pendant l'annonce");
        _player.AIInputOverride = Vector2.Down;
        await Step(windupTicks - 4);
        Sprite2D appearance = spitter.GetNode<Sprite2D>("ProjectileAppearance");
        Check(appearance.Visible && appearance.Texture != null && ActiveProjectiles(pools).Count == 0,
            "Tir annoncé : apparition visible avant le départ, sans projectile anticipé");
        await Step(5);
        _player.AIInputOverride = Vector2.Zero;
        var shots = ActiveProjectiles(pools);
        Vector2 heading = shots.Count > 0
            ? (Vector2)typeof(EnemyProjectile).GetField("_direction", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(shots[0])
            : Vector2.Zero;
        Check(shots.Count == 1 && heading.IsEqualApprox(Vector2.Right) && !lane.Visible,
            $"Tir annoncé : un projectile dans la direction verrouillée ({heading}), pas vers le joueur qui s'est écarté");
        Check(!appearance.Visible, "Tir annoncé : apparition effacée au départ");
        Despawn(spitter);

        float range = EnemyDataLoader.Get("wailing_sentinel").Stats.AttackRange;
        Enemy sentinel = await SpawnReady("wailing_sentinel", new Vector2(0f, -range * 0.8f));
        GroundTelegraph ring = sentinel.GetNode<GroundTelegraph>("RangeMarker");
        ForceCooldown(sentinel, EnemyAbilityKind.AimedShot);
        await Step(2);
        Check(!sentinel.GetNode<GroundTelegraph>("AimMarker").Visible && !ring.Visible,
            $"Sentinelle : à {range * 0.8f:F0} px au nord (soit {range * 1.6f:F0} px au sol), ni visée ni cercle");
        sentinel.Position = _player.Position + new Vector2(0f, -range * 0.4f);
        await Step(2);
        Check(ring.Visible && sentinel.GetNode<GroundTelegraph>("AimMarker").Visible,
            "Sentinelle : portée montrée au sol et visée quand le joueur y entre");
        Despawn(sentinel);
        ProjectileSprites.SpriteSet impactSet = ProjectileSprites.Get("web").Impact;
        pools.ShowProjectileImpact(new Vector2(80f, 40f), impactSet);
        ProjectileImpact impact = null;
        foreach (Node child in pools.GetChildren())
            if (child is ProjectileImpact found)
                impact = found;
        Check(impact != null && impact.Visible && impact.ZIndex == -1,
            "Impact : effet visible au sol, sans collision");
        // Le banc pilote les ticks ; on force ici le temps d'animation pour contrôler la réutilisation.
        impact?._Process(1.0);
        Check(impact != null && !impact.Visible && impact.ProcessMode == ProcessModeEnum.Disabled,
            "Impact : effet terminé retiré du traitement");
        int created = pools.CreatedCount;
        pools.ShowProjectileImpact(new Vector2(120f, 60f), impactSet);
        Check(pools.CreatedCount == created && impact != null && impact.Visible
            && impact.GlobalPosition == new Vector2(120f, 60f) && impact.Texture == impactSet.Get(0, 0),
            "Impact : même nœud recyclé, position et première frame réinitialisées");

        // Une capacité réutilisée ne doit pas transmettre la cadence du Hurleur au Cracheur suivant.
        Enemy recycled = await SpawnReady("hurleur", new Vector2(-150f, 0f));
        recycled.Initialize(EnemyDataLoader.Get("fading_spitter"), 1000f, 1f);
        recycled.SetTicking(false);
        var abilities = (Dictionary<EnemyAbilityKind, IEnemyAbility>)typeof(Enemy)
            .GetField("_abilityCache", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(recycled);
        IEnemyAbility recycledShot = abilities[EnemyAbilityKind.AimedShot];
        ForceCooldown(recycled, EnemyAbilityKind.AimedShot);
        _player.AIInputOverride = Vector2.Zero;
        int launches = 0;
        for (int tick = 0; tick < 180; tick++)
        {
            bool aiming = recycledShot.IsActive;
            await Step(1);
            if (aiming && !recycledShot.IsActive)
                launches++;
        }
        Check(launches == 2, $"Tir recyclé Hurleur → Cracheur : cadence d'origine retrouvée ({launches} tirs en 3 s)");
        Despawn(recycled);
        pools.QueueFree();
        await Step(1);
    }

    private static List<EnemyProjectile> ActiveProjectiles(Node root)
    {
        List<EnemyProjectile> found = new();
        foreach (Node node in root.FindChildren("*", "", true, false))
            if (node is EnemyProjectile { Visible: true } projectile)
                found.Add(projectile);
        return found;
    }

    private int CountEnemies()
    {
        int count = 0;
        foreach (Node child in GetChildren())
            count += child is Enemy { IsActive: true, IsDying: false } ? 1 : 0;
        return count;
    }

    /// <summary>Recul des armes (stat `knockback`) : la créature est vraiment repoussée, pas seulement son visuel.</summary>
    private async Task RunKnockbackChecks()
    {
        Enemy target = await SpawnReady("rodeur", new Vector2(60f, 0f));
        await Step(1);
        Vector2 before = target.Position;
        target.ApplyKnockback(Vector2.Right, 40f);
        await Step(20);
        float pushed = target.Position.X - before.X;
        Check(pushed > 25f, $"Recul : knockback 40 repousse la créature ({pushed:F1} px)");
        before = target.Position;
        for (int hit = 0; hit < 6; hit++)
            target.ApplyKnockback(Vector2.Right, 60f);
        await Step(40);
        pushed = target.Position.X - before.X;
        Check(pushed > 40f && pushed <= 80f, $"Recul : six coups de 60 plafonnés ({pushed:F1} px, plafond 80)");
        Despawn(target);
    }

    private static int VisibleFx(Node pools)
    {
        int count = 0;
        foreach (Node child in pools.GetChildren())
            if (child is PixelFx { Visible: true })
                count++;
        return count;
    }

    private static int VisibleNumbers(Node pools, out string texts)
    {
        int count = 0;
        texts = "";
        foreach (Node child in pools.GetChildren())
        {
            if (child is not DamageNumber { Visible: true } number)
                continue;
            count++;
            texts += (texts.Length > 0 ? "," : "") + number.GetNode<Label>("Label").Text;
        }
        return count;
    }

    // PV élevés par défaut : l'arme automatique du joueur ne doit pas éliminer les ennemis observés.
    private async Task<Enemy> SpawnReady(string id, Vector2 offsetFromPlayer, float hpScale = 1000f)
    {
        Enemy enemy = EnemyScene.Instantiate<Enemy>();
        AddChild(enemy);
        enemy.Initialize(EnemyDataLoader.Get(id), hpScale, 1f);
        enemy.SetTicking(false);
        enemy.Position = _player.Position + offsetFromPlayer;
        _enemies.Add(enemy);
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        return enemy;
    }

    private void Despawn(Enemy enemy)
    {
        _enemies.Remove(enemy);
        if (IsInstanceValid(enemy))
            enemy.QueueFree();
    }

    private static void ForceCooldown(Enemy enemy, EnemyAbilityKind abilityId) => ForceTimer(enemy, abilityId, "_cooldownTimer");

    private static void ForceTimer(Enemy enemy, EnemyAbilityKind abilityId, string field)
    {
        var cache = (Dictionary<EnemyAbilityKind, IEnemyAbility>)typeof(Enemy)
            .GetField("_abilityCache", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(enemy);
        IEnemyAbility ability = cache[abilityId];
        ability.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(ability, 0f);
    }

    private async Task Flee(Vector2 direction, int ticks)
    {
        _player.AIInputOverride = direction;
        await Step(ticks);
    }

    private async Task WaitHurtRecovery()
    {
        Vector2 input = _player.AIInputOverride;
        _player.AIInputOverride = Vector2.Zero;
        await Step(20);
        _player.AIInputOverride = input;
    }

    private async Task Step(int ticks)
    {
        for (int tick = 0; tick < ticks; tick++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            _player._PhysicsProcess(Dt);
            foreach (Enemy enemy in _enemies)
                if (IsInstanceValid(enemy) && enemy.IsActive)
                    enemy.PhysicsTick(Dt);
        }
    }

    private void Check(bool passed, string message)
    {
        GD.Print($"[EnemyAbilityRegression] {(passed ? "PASS" : "FAIL")} {message}");
        if (!passed) _failures++;
    }
}
