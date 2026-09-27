using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Combat.Abilities;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Tests;

/// <summary>
/// Banc isolé des capacités ennemies (Présage, bond du Charognard) et du retour de coup : vrais Player et Enemy,
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
            _player.SetPhysicsProcess(false);
            _player.IsAIControlled = true;
            // Arme automatique coupée : seuls les effets des capacités ennemies sont observés.
            foreach (Node child in _player.GetChildren())
                if (child is Timer weaponTimer)
                    weaponTimer.Stop();
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

            await RunOmenChecks();
            await RunPounceChecks();
            await RunHitFeedbackChecks();

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
        ForceCooldown(omen, "omen_strike");
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
        ForceCooldown(omen, "omen_strike");
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
        ForceCooldown(omen, "omen_strike");
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
        int cap = Mathf.RoundToInt(EnemyDataLoader.Get("presage").Abilities["omen_strike"].GetNumber("max_simultaneous", 3f));
        Enemy[] casters = new Enemy[cap + 2];
        for (int i = 0; i < casters.Length; i++)
            casters[i] = await SpawnReady("presage", new Vector2(-150f, -60f + 40f * i));
        foreach (Enemy caster in casters)
            ForceCooldown(caster, "omen_strike");
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
        ForceCooldown(pouncer, "pounce");
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
        ForceCooldown(pouncer, "pounce");
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
        LevelUpFx.Play(_player, GetNode<GroupCache>("/root/GroupCache"));
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
        enemy.SetPhysicsProcess(false);
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

    private static void ForceCooldown(Enemy enemy, string abilityId)
    {
        var cache = (Dictionary<string, IEnemyAbility>)typeof(Enemy)
            .GetField("_abilityCache", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(enemy);
        IEnemyAbility ability = cache[abilityId];
        ability.GetType().GetField("_cooldownTimer", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(ability, 0f);
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
                    enemy._PhysicsProcess(Dt);
        }
    }

    private void Check(bool passed, string message)
    {
        GD.Print($"[EnemyAbilityRegression] {(passed ? "PASS" : "FAIL")} {message}");
        if (!passed) _failures++;
    }
}
