using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.Tests;

/// <summary>
/// Banc des armes portées (plan 17, lot 1A, corrections) sur un vrai Player : notes de la Boîte à musique dès
/// l'équipement, effets au contact de l'arme qui a frappé (pas de la dernière qui a tiré), niveau unique après
/// l'Autel, bannissement qui écarte aussi les améliorations. Minuteurs d'armes coupés : le banc pilote les coups.
/// </summary>
public partial class WeaponRegression : Node2D
{
    private static readonly PackedScene EnemyScene = GD.Load<PackedScene>("res://scenes/enemies/Enemy.tscn");
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

    private Player _player;
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
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

            CheckOrbitalOnEquip();
            await CheckOrbitalHits();
            await CheckTargetLockOneLife();
            await CheckBounceSeeksFarthest();
            await CheckRemanenceEcho();
            await CheckHitEffectsFollowSource();
            CheckSingleLevel();
            CheckBanishUpgrade();
            CheckRarityDistribution();
            CheckOubliEffects();
            CheckUpgradeGains();
            CheckRangeAndZone();
            await CheckGroundFireOnGround();
            await CheckMeleeStrikeShape();
            CheckAscensions();
            CheckGrammarRejectsUnknownKeys();
            CheckContractRejectsBadData();
            CheckImpactSounds();
            await CheckCountForAllWeapons();
            await CheckVolleyOnSingleTarget();
            CheckSustainedConeSound();
            CheckOrbitalSound();
            CheckTemper();
            await CheckObstacleField();
            await CheckFastShotHits();

            GD.Print($"[WeaponRegression] RESULT failures={_failures}");
            GetTree().Quit(_failures == 0 ? 0 : 1);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(2);
        }
    }

    /// <summary>
    /// Flaque de feu de la Lanterne : portée mesurée au sol, comme l'ellipse dessinée. À 0,8 rayon à l'horizontale une
    /// créature brûle ; à 0,8 rayon à la verticale de l'écran (1,6 au sol), elle est hors de la flaque.
    /// </summary>
    private async Task CheckGroundFireOnGround()
    {
        const float radius = 40f;
        Vector2 center = _player.Position + new Vector2(0f, 300f);
        Enemy beside = EnemyScene.Instantiate<Enemy>();
        Enemy below = EnemyScene.Instantiate<Enemy>();
        foreach (Enemy enemy in new[] { beside, below })
        {
            AddChild(enemy);
            enemy.Initialize(EnemyDataLoader.Get("rodeur"), 1000f, 1f);
            enemy.SetTicking(false);
        }
        beside.Position = center + new Vector2(radius * 0.8f, 0f);
        below.Position = center + new Vector2(0f, radius * 0.8f);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GroundFire fire = new();
        fire.Add(center, 50f, 1f, radius, 0.5f);
        fire.Process(0.6f);
        Check(beside.IsBurning && !below.IsBurning,
            $"Flaque de feu mesurée au sol : à côté {(beside.IsBurning ? "brûle" : "intacte")}, en dessous {(below.IsBurning ? "brûle" : "intacte")}");
        beside.QueueFree();
        below.QueueFree();
    }

    /// <summary>
    /// Forme de touche de la mêlée (DECISIONS §99, §102) : un coup ne touche pas hors de sa forme dessinée.
    /// Allonge : la traînée de la Faucille est aplatie (<see cref="Iso.SlashSquash"/>) ; à 0,9 allonge à l'horizontale
    /// et sous le joueur dans l'ellipse, la créature est touchée ; à 1,1 allonge sur l'ellipse en dessous (0,81 à
    /// l'écran, dans l'ancien cercle), elle ne l'est pas ; l'estoc du Scalpel touche à 0,9 allonge en dessous.
    /// Largeur : l'ouverture de 90° se mesure sur l'ellipse, une créature à 33° de l'axe (41° sur l'ellipse) est
    /// touchée, à 40° (48°) elle ne l'est plus ; l'estoc touche la créature alignée derrière sa cible, pas celle à
    /// 30° de son trait.
    /// </summary>
    private async Task CheckMeleeStrikeShape()
    {
        Player player = GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate<Player>();
        AddChild(player);
        player.InitializeCharacter(CharacterDataLoader.Get("vagabond"));
        player.ProcessMode = ProcessModeEnum.Disabled;
        player.GlobalPosition = new Vector2(90000f, 90000f);
        MethodInfo attack = typeof(Player).GetMethod("OnWeaponAttackTimeout", Private);
        try
        {
            // Pose une créature par décalage (en allonges ; la première, la plus proche, est la cible), frappe une fois
            // et dit lesquelles sont touchées.
            async Task<bool[]> Strike(string weaponId, params Vector2[] offsets)
            {
                while (player.WeaponSlots.Count > 0)
                    player.RemoveWeapon(0);
                player.AddWeapon(WeaponDataLoader.Get(weaponId));
                float reach = player.GetWeaponStatForDisplay(player.WeaponSlots[0], "range");
                Enemy[] enemies = new Enemy[offsets.Length];
                for (int index = 0; index < offsets.Length; index++)
                {
                    enemies[index] = EnemyScene.Instantiate<Enemy>();
                    AddChild(enemies[index]);
                    enemies[index].Initialize(EnemyDataLoader.Get("rodeur"), 100000f, 1f);
                    enemies[index].SetTicking(false);
                    enemies[index].GlobalPosition = player.GlobalPosition + offsets[index] * reach;
                }
                CrowdIndex.MarkMoved();
                await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
                attack.Invoke(player, new object[] { 0 });
                bool[] hits = new bool[offsets.Length];
                for (int index = 0; index < offsets.Length; index++)
                {
                    hits[index] = enemies[index].HpRatio < 1f;
                    enemies[index].QueueFree();
                }
                return hits;
            }

            static Vector2 At(float degrees, float distance) => Vector2.FromAngle(Mathf.DegToRad(degrees)) * distance;

            bool beside = (await Strike("chipped_blade", new Vector2(0.9f, 0f)))[0];
            bool belowInside = (await Strike("chipped_blade", new Vector2(0f, 0.9f / Iso.SlashSquash)))[0];
            bool belowOutside = (await Strike("chipped_blade", new Vector2(0f, 1.1f / Iso.SlashSquash)))[0];
            bool thrustBelow = (await Strike("surgeons_scalpel", new Vector2(0f, 0.9f)))[0];
            Check(beside && belowInside && !belowOutside && thrustBelow,
                $"Mêlée, allonge : la touche s'arrête au bord du coup dessiné (à côté {beside}, dessous dans la traînée {belowInside}, "
                + $"dessous hors traînée {belowOutside}, estoc dessous {thrustBelow})");

            bool[] arc = await Strike("chipped_blade", new Vector2(0.4f, 0f), At(33f, 0.6f), At(40f, 0.6f));
            bool[] thrust = await Strike("surgeons_scalpel", new Vector2(0.4f, 0f), new Vector2(0.8f, 0f), At(30f, 0.6f));
            Check(arc[0] && arc[1] && !arc[2] && thrust[0] && thrust[1] && !thrust[2],
                $"Mêlée, largeur : ouverture mesurée sur la traînée (33° {arc[1]}, 40° {arc[2]}), estoc en trait "
                + $"(aligné derrière {thrust[1]}, à 30° {thrust[2]})");
        }
        finally
        {
            player.QueueFree();
        }
    }

    private void CheckOrbitalOnEquip()
    {
        _player.AddWeapon(WeaponDataLoader.Get("music_box"));
        List<Node2D> orbs = (List<Node2D>)typeof(Player).GetField("_orbitalProjectiles", Private).GetValue(_player);
        Check(orbs.Count > 0, $"Boîte à musique : {orbs.Count} notes dès l'équipement, sans attendre le minuteur");
    }

    /// <summary>
    /// Q8a (plan 26) : un tir guidé ou un tir de rafale en attente verrouillé sur une créature qui meurt puis revient du
    /// pool, ailleurs, ne suit pas sa nouvelle vie ; il se tourne vers la créature restante. Contre-épreuve : sans mort,
    /// le verrou tient.
    /// </summary>
    private async Task CheckTargetLockOneLife()
    {
        PackedScene projectileScene = GD.Load<PackedScene>("res://scenes/combat/Projectile.tscn");
        WeaponData compass = WeaponDataLoader.Get("compass_needle");
        Enemy locked = SpawnStill(new Vector2(120f, 0f));
        Enemy other = SpawnStill(new Vector2(0f, 150f));
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        Projectile homing = projectileScene.Instantiate<Projectile>();
        AddChild(homing);
        homing.Launch(Vector2.Zero, Vector2.Right, 1f, 100f, 10f, 0, false, _player, compass, null);
        homing.StopTicking();
        homing.SetHoming(1f, locked);
        homing.PhysicsTick(1f / 60f);
        Check(LockedTarget(homing, "_homingTarget") == locked, "Verrou : tir guidé sur sa cible tant qu'elle vit");

        Recycle(locked, new Vector2(5000f, 5000f));
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        homing.PhysicsTick(1f / 60f);
        Check(LockedTarget(homing, "_homingTarget") == other,
            "Verrou : la cible morte puis revenue du pool n'est plus suivie, le tir guidé prend la créature restante");

        Projectile burst = projectileScene.Instantiate<Projectile>();
        AddChild(burst);
        burst.Launch(Vector2.Zero, Vector2.Right, 1f, 100f, 10f, 0, false, _player, compass, null, launchDelay: 0.05f);
        burst.StopTicking();
        burst.AimAtDeparture(locked);
        Recycle(locked, new Vector2(-5000f, 5000f));
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        burst.PhysicsTick(0.1f);
        Vector2 direction = (Vector2)typeof(Projectile).GetField("_direction", Private).GetValue(burst);
        Vector2 toOther = (other.GlobalPosition - _player.GlobalPosition).Normalized();
        Check(direction.Dot(toOther) > 0.99f,
            $"Verrou : le tir de rafale dont la cible est revenue du pool part vers la créature restante ({direction})");

        homing.QueueFree();
        burst.QueueFree();
        locked.QueueFree();
        other.QueueFree();
    }

    /// <summary>
    /// Sacoche de lettres (plan 06 P2b) : après chaque touche, la lettre repart vers la créature la plus éloignée à
    /// portée au sol, jamais une déjà touchée, sans toucher celles qu'elle survole ; chaque rebond affaiblit le coup ;
    /// hors de portée, rien. Rendue au pool, elle ne garde ni rebonds ni destination.
    /// </summary>
    private async Task CheckBounceSeeksFarthest()
    {
        PackedScene projectileScene = GD.Load<PackedScene>("res://scenes/combat/Projectile.tscn");
        WeaponData satchel = WeaponDataLoader.Get("letter_satchel");
        Enemy first = SpawnStill(new Vector2(100f, 0f));
        Enemy near = SpawnStill(new Vector2(140f, 0f));
        Enemy far = SpawnStill(new Vector2(100f, 60f));
        Enemy outOfRange = SpawnStill(new Vector2(300f, 0f));
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        Projectile letter = projectileScene.Instantiate<Projectile>();
        AddChild(letter);
        bool released = false;
        letter.SetRelease(_ => released = true);
        letter.Launch(_player.GlobalPosition, Vector2.Right, 40f, 300f, 2f, 0, false, _player, satchel, null);
        letter.StopTicking();
        letter.SetBounce(2, 150f, 0.5f);
        Node2D firstDestination = null;
        for (int frame = 0; frame < 30 && firstDestination == null; frame++)
        {
            letter.PhysicsTick(1f / 60f);
            firstDestination = LockedTarget(letter, "_bounceTarget");
        }
        Check(firstDestination == far,
            $"Sacoche : après la première touche, la lettre vise la plus éloignée à portée au sol ({firstDestination?.Name})");
        for (int frame = 0; frame < 120 && !released; frame++)
        {
            letter.PhysicsTick(1f / 60f);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        float lostFirst = Lost(first), lostFar = Lost(far), lostNear = Lost(near);
        Check(released && lostFirst > 0f && lostFar > 0f && lostNear > 0f && outOfRange.HpRatio >= 1f,
            $"Sacoche : trois touches puis fin de course, la créature hors de portée intacte (PV perdus {lostFirst:0.#} {lostFar:0.#} {lostNear:0.#}, hors portée {Lost(outOfRange):0.#})");
        Check(lostFar < lostFirst && lostNear < lostFar,
            $"Sacoche : chaque rebond frappe moins fort ({lostFirst:0.#} > {lostFar:0.#} > {lostNear:0.#})");

        letter.Launch(_player.GlobalPosition, Vector2.Right, 1f, 300f, 2f, 0, false, _player, satchel, null);
        letter.StopTicking();
        int bounces = (int)typeof(Projectile).GetField("_bouncesRemaining", Private).GetValue(letter);
        Check(bounces == 0 && LockedTarget(letter, "_bounceTarget") == null,
            "Sacoche : un tir neuf du pool ne garde ni rebonds ni destination");

        letter.QueueFree();
        first.QueueFree();
        near.QueueFree();
        far.QueueFree();
        outOfRange.QueueFree();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    /// <summary>
    /// Double de l'Éveillée (plan 06 P3b) : armée, la rémanence répète le prochain tir depuis son point, à la moitié
    /// des dégâts, une seule fois ; elle attend un tir et ne répète pas la mêlée. Le module l'arme au départ d'une
    /// mobilité, à l'endroit quitté, et la retire au bout de 1,5 s.
    /// </summary>
    private async Task CheckRemanenceEcho()
    {
        CombatPools pools = new() { Name = "CombatPools" };
        AddChild(pools);
        Player player = GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate<Player>();
        AddChild(player);
        player.InitializeCharacter(CharacterDataLoader.Get("eveillee"));
        player.SetPhysicsProcess(false);
        player.IsAIControlled = true;
        player.ProcessMode = ProcessModeEnum.Disabled;
        player.GlobalPosition = new Vector2(80000f, 80000f);
        typeof(Player).GetField("_critChance", Private).SetValue(player, 0f);
        MethodInfo attack = typeof(Player).GetMethod("OnWeaponAttackTimeout", Private);
        FieldInfo shotDamage = typeof(Projectile).GetField("_damage", Private);
        Enemy enemy = SpawnStill(Vector2.Zero);
        enemy.GlobalPosition = player.GlobalPosition + new Vector2(60f, 60f);
        Vector2 origin = player.GlobalPosition + new Vector2(0f, 120f);
        Vector2? echoed = null;
        player.AttackEchoed += at => echoed = at;
        try
        {
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            while (player.WeaponSlots.Count > 0)
                player.RemoveWeapon(0);
            player.AddWeapon(WeaponDataLoader.Get("makeshift_bow"));

            List<(Vector2 From, float Damage)> Fire()
            {
                foreach (Node child in pools.GetChildren())
                    if (child is Projectile old && old.ProcessMode != ProcessModeEnum.Disabled)
                        old.StopTicking();
                HashSet<Node> before = new();
                foreach (Node child in pools.GetChildren())
                    if (child is Projectile { Visible: true } flying && flying.ProcessMode != ProcessModeEnum.Disabled)
                        before.Add(flying);
                attack.Invoke(player, new object[] { 0 });
                List<(Vector2, float)> shots = new();
                foreach (Node child in pools.GetChildren())
                    if (child is Projectile { Visible: true } shot && shot.ProcessMode != ProcessModeEnum.Disabled && !before.Contains(shot))
                        shots.Add((shot.GlobalPosition, (float)shotDamage.GetValue(shot)));
                return shots;
            }

            player.ArmAttackEcho(origin, 0.5f);
            List<(Vector2 From, float Damage)> first = Fire();
            (Vector2 From, float Damage) own = first.Find(shot => shot.From.IsEqualApprox(player.GlobalPosition));
            (Vector2 From, float Damage) repeated = first.Find(shot => shot.From.IsEqualApprox(origin));
            Check(first.Count == 2 && own.Damage > 0f && Mathf.IsEqualApprox(repeated.Damage, own.Damage * 0.5f)
                    && echoed == origin && !player.HasAttackEcho,
                $"Rémanence : le tir part aussi de la rémanence, à moitié des dégâts ({first.Count} tirs, {own.Damage:0.#} et {repeated.Damage:0.#})");
            List<(Vector2 From, float Damage)> second = Fire();
            Check(second.Count == 1, $"Rémanence : une seule fois ({second.Count} tir ensuite)");

            echoed = null;
            player.ArmAttackEcho(player.GlobalPosition + new Vector2(0f, 900f), 0.5f);
            List<(Vector2 From, float Damage)> unreachable = Fire();
            Check(unreachable.Count == 1 && player.HasAttackEcho && echoed == null,
                "Rémanence : sans cible à sa portée, elle ne tire pas et reste armée");
            player.DisarmAttackEcho();

            // Rafale sur une cible seule : les flèches qui attendent leur tour partiront de la rémanence.
            FieldInfo departureField = typeof(Projectile).GetField("_departureOrigin", Private);
            player.WeaponSlots[0].ApplyUpgrade(new[] { new StatGain("projectile_count", 2f) });
            player.ArmAttackEcho(origin, 0.5f);
            HashSet<Node> earlier = new();
            foreach (Node child in pools.GetChildren())
                if (child is Projectile { ProcessMode: not ProcessModeEnum.Disabled } old)
                    earlier.Add(old);
            attack.Invoke(player, new object[] { 0 });
            int waitingFromEcho = 0, waitingElsewhere = 0;
            foreach (Node child in pools.GetChildren())
            {
                if (child is not Projectile { Visible: false } waiting || waiting.ProcessMode == ProcessModeEnum.Disabled
                    || earlier.Contains(waiting))
                    continue;
                Vector2? from = (Vector2?)departureField.GetValue(waiting);
                waitingFromEcho += from == origin ? 1 : 0;
                waitingElsewhere += from == null ? 1 : 0;
            }
            Check(waitingFromEcho == 2 && waitingElsewhere == 2,
                $"Rémanence : sa rafale attend son tour et partira de la rémanence ({waitingFromEcho} de la rémanence, {waitingElsewhere} du joueur)");

            while (player.WeaponSlots.Count > 0)
                player.RemoveWeapon(0);
            player.AddWeapon(WeaponDataLoader.Get("chipped_blade"));
            echoed = null;
            player.ArmAttackEcho(origin, 0.5f);
            attack.Invoke(player, new object[] { 0 });
            Check(player.HasAttackEcho && echoed == null, "Rémanence : la mêlée n'est pas répétée, elle attend un tir");
            player.DisarmAttackEcho();

            RemanenceEcho module = new();
            module.Initialize(player, PerkDataLoader.GetAll().Find(perk => perk.Id == "eveillee_double").Echo);
            player.AddChild(module);
            Vector2 departure = player.GlobalPosition;
            module.Advance(1f / 60f);
            player.Mobility.Request();
            player.Mobility.Step(1f / 60f, Vector2.Right, 200f, 1f, true);
            player.GlobalPosition += new Vector2(20f, 0f);
            module.Advance(1f / 60f);
            Vector2 armedAt = (Vector2)typeof(Player).GetField("_echoOrigin", Private).GetValue(player);
            bool armedAtDeparture = player.HasAttackEcho && module.IsShown && armedAt == departure;
            for (int tick = 0; tick < 30; tick++)
                player.Mobility.Step(1f / 60f, Vector2.Zero, 200f, 1f, true);
            for (int tick = 0; tick < 95; tick++)
                module.Advance(1f / 60f);
            Check(armedAtDeparture && !player.HasAttackEcho && !module.IsShown,
                $"Rémanence : armée au départ de la mobilité, à l'endroit quitté ({armedAt}), retirée au bout de 1,5 s");
        }
        finally
        {
            enemy.QueueFree();
            player.QueueFree();
            pools.QueueFree();
        }
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
    }

    private static float Lost(Enemy enemy) => (1f - enemy.HpRatio) * enemy.MaxHp;

    private Enemy SpawnStill(Vector2 offset)
    {
        Enemy enemy = EnemyScene.Instantiate<Enemy>();
        AddChild(enemy);
        enemy.Initialize(EnemyDataLoader.Get("rodeur"), 1000f, 1f);
        enemy.SetTicking(false);
        enemy.GlobalPosition = _player.GlobalPosition + offset;
        return enemy;
    }

    /// <summary>La créature meurt et revient du pool comme une autre vie, loin d'ici.</summary>
    private void Recycle(Enemy enemy, Vector2 offset)
    {
        enemy.Reset();
        enemy.Initialize(EnemyDataLoader.Get("rodeur"), 1000f, 1f);
        enemy.SetTicking(false);
        enemy.GlobalPosition = _player.GlobalPosition + offset;
    }

    private static Node2D LockedTarget(Projectile projectile, string field) =>
        ((TargetLock)typeof(Projectile).GetField(field, Private).GetValue(projectile)).TryGet(out Node2D target) ? target : null;

    /// <summary>Un ennemi posé sur l'orbite est touché par les notes : leurs dégâts comptent pour la Boîte à musique.</summary>
    private async Task CheckOrbitalHits()
    {
        Enemy enemy = EnemyScene.Instantiate<Enemy>();
        AddChild(enemy);
        enemy.Initialize(EnemyDataLoader.Get("rodeur"), 1000f, 1f);
        enemy.SetTicking(false);
        float orbit = _player.GetWeaponStatForDisplay(FindSlot("music_box"), "range");
        enemy.Position = _player.Position + Iso.ToScreen(new Vector2(orbit, 0f));
        for (int frame = 0; frame < 180 && _player.GetDamageDealt("music_box") <= 0f; frame++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            _player._PhysicsProcess(1f / 60f);
        }
        Check(_player.GetDamageDealt("music_box") > 0f, $"Boîte à musique : les notes touchent ({_player.GetDamageDealt("music_box"):0} dégâts)");
        enemy.QueueFree();
    }

    private async Task CheckHitEffectsFollowSource()
    {
        _player.AddWeapon(WeaponDataLoader.Get("nail_mace"));
        WeaponInstance bow = FindSlot("makeshift_bow") ?? AddAndFind("makeshift_bow");
        WeaponInstance mace = FindSlot("nail_mace");
        // La masse vient de frapper : c'est elle l'« arme courante » quand la flèche touche.
        typeof(Player).GetField("_equippedWeapon", Private).SetValue(_player, mace);

        Enemy enemy = EnemyScene.Instantiate<Enemy>();
        AddChild(enemy);
        enemy.Initialize(EnemyDataLoader.Get("rodeur"), 1000f, 1f);
        enemy.SetTicking(false);
        enemy.Position = _player.Position + new Vector2(80f, 0f);
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

        _player.OnProjectileHit(enemy, 1f, false, bow);
        float bleed = (float)typeof(Enemy).GetField("_bleedTimer", Private).GetValue(enemy);
        Check(bleed <= 0f, $"flèche de l'arc après un coup de masse : pas de saignement emprunté (timer {bleed:F1} s)");

        _player.OnProjectileHit(enemy, 1f, false, mace);
        bleed = (float)typeof(Enemy).GetField("_bleedTimer", Private).GetValue(enemy);
        Check(bleed > 0f, $"coup de la masse : saignement appliqué (timer {bleed:F1} s)");
        enemy.QueueFree();
    }

    private void CheckSingleLevel()
    {
        WeaponInstance equipped = _player.EquippedWeapon;
        int before = _player.GetWeaponFragmentLevel(equipped.Id);
        RandomNumberGenerator rng = new() { Seed = 3 };
        bool rareOrBetter = true;
        for (int i = 0; i < 200; i++)
            rareOrBetter &= UpgradeRoller.RollRarityAtLeast(0f, "rare", rng).Rank >= UpgradeRoller.Get("rare").Rank;
        UpgradeRarity rarity = UpgradeRoller.RollRarityAtLeast(0f, "rare", rng);
        bool upgraded = _player.UpgradeWeapon(equipped.Id, UpgradeRoller.RollWeaponGains(equipped, rarity, rng));
        int after = _player.GetWeaponFragmentLevel(equipped.Id);
        Check(rareOrBetter && upgraded && after == before + 1 && after == _player.EquippedWeapon.Level,
            $"Mémorial : arme ravivée Rare au moins, badge et arme montent ensemble ({before} → {after}, arme {_player.EquippedWeapon.Level})");
    }

    private void CheckBanishUpgrade()
    {
        FragmentManager fragments = new() { Name = "FragmentManager" };
        AddChild(fragments);
        typeof(FragmentManager).GetMethod("CachePlayer", Private)?.Invoke(fragments, null);
        fragments.AddBanishes(1);
        int banishesBefore = fragments.BanishesRemaining;
        string banished = FindSlot("nail_mace").Id;
        typeof(FragmentManager).GetField("_currentLevel", Private)?.SetValue(fragments, 5);
        fragments.BanishFragment(banished);
        List<FragmentOption> pool = (List<FragmentOption>)typeof(FragmentManager).GetMethod("BuildFragmentPool", Private).Invoke(fragments, null);
        bool offered = pool.Exists(option => option.Id == banished);
        Check(!offered && fragments.BanishesRemaining == banishesBefore - 1,
            $"bannir l'amélioration de {banished} : absente de l'offre, bannissement consommé");
    }

    /// <summary>Oublis de carte : les effets du même nom s'additionnent et se publient ; un Oubli définitif ne se lève pas.</summary>
    private void CheckOubliEffects()
    {
        PerilManager peril = new();
        AddChild(peril);
        Dictionary<string, float> published = new();
        EventBus bus = GetNode<EventBus>("/root/EventBus");
        EventBus.OubliEffectChangedEventHandler handler = (effect, total) => published[effect] = total;
        bus.OubliEffectChanged += handler;
        OubliData path = OubliDataLoader.All[0];
        OubliData permanent = OubliDataLoader.All[0];
        foreach (OubliData oubli in OubliDataLoader.All)
        {
            if (oubli.Effect == "erasure_speed")
                path = oubli;
            if (oubli.Permanent)
                permanent = oubli;
        }

        peril.AddOubli(path);
        peril.AddOubli(path);
        float twice = published.GetValueOrDefault(path.Effect);
        bool lifted = peril.LiftOubli(peril.Oublis[0]);
        float once = published.GetValueOrDefault(path.Effect);
        peril.AddOubli(permanent);
        bool permanentLifted = peril.LiftOubli(peril.Oublis[^1]);
        bus.OubliEffectChanged -= handler;
        Check(Mathf.IsEqualApprox(twice, 2f * path.Amount) && lifted && Mathf.IsEqualApprox(once, path.Amount)
              && !permanentLifted && peril.Oublis.Count == 2,
            $"Oublis : {path.Id} ×2 = {twice:0.##}, levé → {once:0.##} ; {permanent.Id} définitif, levée refusée");
        peril.QueueFree();
    }

    /// <summary>
    /// 10 000 tirages : les poids de base sont respectés ; l'oubli de la zone fait un peu monter les raretés hautes, la
    /// Chance beaucoup (DECISIONS §53).
    /// </summary>
    private void CheckRarityDistribution()
    {
        RandomNumberGenerator rng = new() { Seed = 17 };
        const int draws = 10000;
        Dictionary<string, int> anchored = new();
        Dictionary<string, int> erased = new();
        Dictionary<string, int> lucky = new();
        float luckySteps = UpgradeRoller.BumpSteps(1f, Vestiges.World.ErasureManager.ErasureZonePhase.Anchored);
        float erasedSteps = UpgradeRoller.BumpSteps(0f, Vestiges.World.ErasureManager.ErasureZonePhase.Erased);
        for (int i = 0; i < draws; i++)
        {
            string a = UpgradeRoller.RollRarity(0f, rng).Id;
            anchored[a] = anchored.GetValueOrDefault(a) + 1;
            string e = UpgradeRoller.RollRarity(erasedSteps, rng).Id;
            erased[e] = erased.GetValueOrDefault(e) + 1;
            string l = UpgradeRoller.RollRarity(luckySteps, rng).Id;
            lucky[l] = lucky.GetValueOrDefault(l) + 1;
        }

        float totalWeight = 0f;
        foreach (UpgradeRarity rarity in UpgradeRoller.Rarities)
            totalWeight += rarity.Weight;
        bool matches = true;
        List<string> shares = new();
        foreach (UpgradeRarity rarity in UpgradeRoller.Rarities)
        {
            float expected = rarity.Weight / totalWeight;
            float measured = anchored.GetValueOrDefault(rarity.Id) / (float)draws;
            matches &= Mathf.Abs(measured - expected) < 0.015f;
            shares.Add($"{rarity.Id} {measured * 100f:0.0} % (attendu {expected * 100f:0.0}) / oubli {erased.GetValueOrDefault(rarity.Id) * 100f / draws:0.0} %");
        }
        int highAnchored = anchored.GetValueOrDefault("epic") + anchored.GetValueOrDefault("legendary");
        int highErased = erased.GetValueOrDefault("epic") + erased.GetValueOrDefault("legendary");
        GD.Print($"[WeaponRegression] raretés sur {draws} tirages : {string.Join(" ; ", shares)}");
        Check(matches, "raretés : poids de base respectés à 1,5 point près");
        int highLucky = lucky.GetValueOrDefault("epic") + lucky.GetValueOrDefault("legendary");
        Check(highErased > highAnchored * 1.3f && highLucky > highAnchored * 5,
            $"raretés : Épique et Légendaire sur {draws} tirages, {highAnchored} sans rien, {highErased} en zone Effacée, {highLucky} avec 1 de Chance");
    }

    /// <summary>
    /// Plan 23 R4 : une Légendaire touche trois stats, chacune au double de son pas relevé, ou +3 pour une stat
    /// entière ; une stat entière se tire dès la commune (+0,5) ; la décimale se joue à chaque attaque, sauf pour les
    /// orbes, qui ne comptent que la partie entière.
    /// </summary>
    private void CheckUpgradeGains()
    {
        WeaponInstance crossbow = new(WeaponDataLoader.Get("crossbow"));
        RandomNumberGenerator rng = new() { Seed = 5 };
        List<StatGain> gains = UpgradeRoller.RollWeaponGains(crossbow, UpgradeRoller.Get("legendary"), rng);
        bool amounts = gains.Count == 3;
        foreach (StatGain gain in gains)
        {
            WeaponUpgradeStatConfig config = WeaponUpgradeDataLoader.GetStatConfig(gain.Stat);
            amounts &= config.Integer ? Mathf.IsEqualApprox(gain.Amount, 3f) : Mathf.IsEqualApprox(gain.Amount, config.Step * 2f);
        }
        float damageBefore = crossbow.GetStat("damage");
        crossbow.ApplyUpgrade(gains);
        StatGain damageGain = gains.Find(g => g.Stat == "damage");
        float expectedDamage = damageGain.Stat == null ? damageBefore : damageBefore * (1f + damageGain.Amount);
        Check(amounts && Mathf.IsEqualApprox(crossbow.GetStat("damage"), expectedDamage) && crossbow.Level == 2
              && Mathf.IsEqualApprox(WeaponUpgradeDataLoader.GetStatConfig("damage").Step, 0.18f),
            $"Légendaire sur la Cloueuse : {gains.Count} stats au double du pas (dégâts +18 % par commune), +3 pour une stat entière");

        // Une commune finit par tirer la perforation, stat entière de la Cloueuse : +0,5.
        WeaponInstance nailer = new(WeaponDataLoader.Get("crossbow"));
        float pierceBefore = nailer.GetStat("projectile_pierce");
        StatGain pierce = default;
        for (int i = 0; i < 200 && pierce.Stat == null; i++)
            pierce = UpgradeRoller.RollWeaponGains(nailer, UpgradeRoller.Get("common"), rng).Find(g => g.Stat == "projectile_pierce");
        nailer.ApplyUpgrade(new[] { pierce });
        Check(pierce.Stat != null && Mathf.IsEqualApprox(nailer.GetStat("projectile_pierce"), pierceBefore + 0.5f)
              && StatCatalog.Format("projectile_pierce", nailer.GetStat("projectile_pierce")) == "3,5",
            $"Commune sur la Cloueuse : perforation {pierceBefore} → {StatCatalog.Format("projectile_pierce", nailer.GetStat("projectile_pierce"))}");

        // Parcmètre monté à 360° (120° + 200 %) : l'arc au plafond n'est plus tiré, pas de carte « Zone +0 % ».
        WeaponInstance hammer = new(WeaponDataLoader.Get("heavy_hammer"));
        hammer.ApplyUpgrade(new[] { new StatGain("arc_angle", 2f) });
        bool arcOffered = false;
        for (int i = 0; i < 200 && !arcOffered; i++)
            arcOffered = UpgradeRoller.RollWeaponGains(hammer, UpgradeRoller.Get("common"), rng).Exists(g => g.Stat == "arc_angle");
        Check(hammer.IsStatCapped("arc_angle") && !arcOffered,
            $"Parcmètre : l'arc au plafond ({hammer.GetStat("arc_angle"):0}°) n'est plus tiré");

        // Lance-billes à 3,5 projectiles : 3 ou 4 par salve, la moitié du temps chacun, sur 1 000 attaques.
        WeaponInstance sling = new(WeaponDataLoader.Get("sling"));
        sling.ApplyUpgrade(new[] { new StatGain("projectile_count", 0.5f) });
        RandomNumberGenerator attacks = new() { Seed = 9 };
        int total = 0, low = int.MaxValue, high = 0;
        for (int i = 0; i < 1000; i++)
        {
            int count = FractionalCount.Roll(sling.GetStat("projectile_count"), attacks.Randf());
            total += count;
            low = Mathf.Min(low, count);
            high = Mathf.Max(high, count);
        }
        Check(low == 3 && high == 4 && total > 3420 && total < 3580,
            $"Lance-billes à 3,5 projectiles : {low} à {high} par salve, {total / 1000f:0.00} en moyenne sur 1 000 attaques");

        // Boîte à musique à 3,5 puis 4 orbes : 3 orbes, puis 4, sans perte ni doublon.
        WeaponInstance box = FindSlot("music_box") ?? AddAndFind("music_box");
        MethodInfo setup = typeof(Player).GetMethod("SetupOrbitalWeapon", Private);
        List<Node2D> orbs = (List<Node2D>)typeof(Player).GetField("_orbitalProjectiles", Private).GetValue(_player);
        box.ApplyUpgrade(new[] { new StatGain("projectile_count", 0.5f) });
        setup.Invoke(_player, new object[] { box });
        int half = orbs.Count;
        box.ApplyUpgrade(new[] { new StatGain("projectile_count", 0.5f) });
        setup.Invoke(_player, new object[] { box });
        int whole = orbs.Count;
        HashSet<Node2D> distinct = new(orbs);
        Check(half == 3 && whole == 4 && distinct.Count == 4,
            $"Boîte à musique : 3,5 orbes en font {half}, 4 en font {whole}, toutes distinctes");
    }

    /// <summary>Un bonus de zone ouvre l'arc sans allonger la portée (plan 05 §4).</summary>
    private void CheckRangeAndZone()
    {
        WeaponInstance mace = FindSlot("nail_mace");
        float range = _player.GetWeaponStatForDisplay(mace, "range");
        float arc = _player.GetWeaponStatForDisplay(mace, "arc_angle");
        _player.ApplyPerkModifier("aoe_radius", 1.2f, "multiplicative");
        float rangeAfter = _player.GetWeaponStatForDisplay(mace, "range");
        float arcAfter = _player.GetWeaponStatForDisplay(mace, "arc_angle");
        Check(Mathf.IsEqualApprox(range, rangeAfter) && arcAfter > arc * 1.19f,
            $"zone +20 % : portée inchangée ({range:0} → {rangeAfter:0}), arc {arc:0}° → {arcAfter:0}°");
    }

    private WeaponInstance FindSlot(string id)
    {
        foreach (WeaponInstance weapon in _player.WeaponSlots)
            if (weapon.Id == id)
                return weapon;
        return null;
    }

    private WeaponInstance AddAndFind(string id)
    {
        _player.AddWeapon(WeaponDataLoader.Get(id));
        return FindSlot(id);
    }

    /// <summary>
    /// Ascensions (plan 21 §3, lot G3) : deux voies au niveau 50, offertes ensemble, choisies pour de bon ; leurs
    /// leviers (motif, stats, projectiles en plus, effet à l'impact, orbite qui va et vient). Un joueur à part, pour ne pas toucher
    /// aux armes des autres contrôles.
    /// </summary>
    private void CheckAscensions()
    {
        // Toutes les armes ont leurs deux voies (DECISIONS §40), dans un motif que leur famille sait jouer.
        int withPaths = 0, total = 0;
        List<string> badPatterns = new();
        foreach (WeaponData data in WeaponDataLoader.GetAll())
        {
            total++;
            withPaths += data.Ascensions.Count == 2 ? 1 : 0;
            foreach (WeaponAscensionData path in data.Ascensions)
            {
                AttackPatternKind pattern = path.AttackPattern ?? data.AttackPattern;
                bool known = data.Category == WeaponCategory.Melee
                    ? pattern is AttackPatternKind.Arc or AttackPatternKind.Linear or AttackPatternKind.Circular or AttackPatternKind.Chain
                    : pattern is AttackPatternKind.Linear or AttackPatternKind.Burst or AttackPatternKind.Homing or AttackPatternKind.Orbital
                        or AttackPatternKind.Bounce;
                if (!known)
                    badPatterns.Add($"{data.Id}:{path.Id}:{pattern}");
            }
        }
        Check(withPaths == total && badPatterns.Count == 0,
            $"Ascensions : {withPaths}/{total} armes ont leurs deux voies, motifs inconnus [{string.Join(" ", badPatterns)}]");

        WeaponInstance bow = MaxedWeapon("makeshift_bow");
        bool ready = bow.CanAscend && !new WeaponInstance(WeaponDataLoader.Get("heavy_hammer")).CanAscend;
        bool chosen = bow.Ascend("volley");
        Check(ready && chosen && !bow.Ascend("pierce_through") && bow.AttackPattern == AttackPatternKind.Burst
            && bow.GetStat("projectile_count") >= 2f && Mathf.IsEqualApprox(bow.GetStat("spread_angle"), 40f) && Mathf.IsEqualApprox(bow.BonusProjectileMultiplier, 2f),
            "Volée : éventail, deux fois plus de flèches et de projectiles en plus ; la voie est définitive");
        WeaponInstance piercing = MaxedWeapon("makeshift_bow");
        float before = piercing.GetStat("damage");
        piercing.Ascend("pierce_through");
        WeaponInstance thrust = MaxedWeapon("chipped_blade");
        thrust.Ascend("thrust");
        WeaponInstance harvest = MaxedWeapon("chipped_blade");
        harvest.Ascend("harvest");
        Check(!WeaponProperties.Concerns(thrust, "size") && WeaponProperties.Concerns(harvest, "size") && !WeaponTraits.SearchesTarget(harvest)
            && StatCatalog.Format("projectile_pierce", 999f) == "∞",
            "La voie choisie compte partout : Estoc ne grandit plus par la Taille, Moisson ne cherche plus de cible ; perforation « ∞ »");
        Check(Mathf.IsEqualApprox(piercing.GetStat("damage"), before * 1.5f) && piercing.GetStat("projectile_pierce") >= 999f
            && Mathf.IsEqualApprox(piercing.GetStat("projectile_count"), 1f) && piercing.BonusProjectileMultiplier == 0f,
            "Transpercer : une flèche, dégâts × 1,5, perforation illimitée, aucun projectile en plus");

        Player player = GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate<Player>();
        AddChild(player);
        player.InitializeCharacter(CharacterDataLoader.Get("traqueur"));
        player.SetPhysicsProcess(false);
        player.IsAIControlled = true;
        WeaponInstance starting = player.WeaponSlots[0];
        while (starting.CanLevelUp)
            starting.ApplyUpgrade(Array.Empty<StatGain>());
        player.AddWeapon(WeaponDataLoader.Get("music_box"));
        WeaponInstance box = player.WeaponSlots[1];
        while (box.CanLevelUp)
            box.ApplyUpgrade(Array.Empty<StatGain>());

        FragmentManager fragments = new() { Name = "AscensionFragments" };
        AddChild(fragments);
        typeof(FragmentManager).GetField("_player", Private).SetValue(fragments, player);
        typeof(FragmentManager).GetMethod("OfferFragments", Private).Invoke(fragments, new object[] { 60 });
        List<FragmentOption> offer = new(fragments.PendingChoices);
        bool paired = offer.Count == 3 && offer[0].Type == FragmentOption.AscensionType && offer[1].Type == FragmentOption.AscensionType
            && offer[0].Id == offer[1].Id && offer[0].Ascension.Id != offer[1].Ascension.Id && offer[2].Type != FragmentOption.AscensionType;
        fragments.SelectFragment(offer[0]);
        WeaponInstance ascended = player.WeaponSlots[0].Id == offer[0].Id ? player.WeaponSlots[0] : player.WeaponSlots[1];
        Check(paired && ascended.Ascension?.Id == offer[0].Ascension.Id,
            $"Offre : les deux voies de {offer[0].DisplayName.Split(':')[0].Trim()} ensemble et une autre carte ; le choix transforme l'arme");

        box = player.WeaponSlots[1];
        bool lullaby = player.AscendWeapon(box.Id, "lullaby");
        Enemy enemy = EnemyScene.Instantiate<Enemy>();
        AddChild(enemy);
        enemy.Initialize(EnemyDataLoader.Get("rodeur"), 1f, 1f);
        enemy.SetTicking(false);
        enemy.Position = new Vector2(4000f, 4000f);
        player.OnProjectileHit(enemy, 1f, false, box);
        float frozen = (float)typeof(Enemy).GetField("_freezeTimer", Private).GetValue(enemy);
        Check(lullaby && frozen > 0.5f, "Berceuse : une note fige un instant l'ennemi qu'elle touche");

        // Réglages d'effet spécial remplacés par la voie, sans toucher à l'arme de base (G3 étape 2).
        WeaponInstance suture = MaxedWeapon("surgeons_scalpel");
        suture.Ascend("suture");
        WeaponInstance combination = MaxedWeapon("echo_gauntlets");
        combination.Ascend("combination");
        Check(Mathf.IsEqualApprox(suture.SpecialEffect.Get(SpecialEffectParam.HitsPerHeal), 3f) && Mathf.IsEqualApprox(suture.Base.SpecialEffect.Get(SpecialEffectParam.HitsPerHeal), 5f)
            && Mathf.IsEqualApprox(combination.SpecialEffect.Get(SpecialEffectParam.EchoCount), 2f),
            "Suture soigne tous les 3 coups et Enchaînement fait deux échos ; le Scalpel de base reste à 5");

        player.AddWeapon(WeaponDataLoader.Get("clock_hand"));
        WeaponInstance clock = player.WeaponSlots[^1];
        while (clock.CanLevelUp)
            clock.ApplyUpgrade(Array.Empty<StatGain>());
        player.AscendWeapon(clock.Id, "freeze_frame");
        Enemy frozenTarget = EnemyScene.Instantiate<Enemy>();
        AddChild(frozenTarget);
        frozenTarget.Initialize(EnemyDataLoader.Get("rodeur"), 1f, 1f);
        frozenTarget.SetTicking(false);
        frozenTarget.Position = new Vector2(6000f, 6000f);
        player.OnProjectileHit(frozenTarget, 1f, false, clock);
        float stopped = (float)typeof(Enemy).GetField("_freezeTimer", Private).GetValue(frozenTarget);
        Check(stopped > 0.3f, $"Arrêt sur image : le champ du Chronomètre fige ({stopped:0.00} s)");
        frozenTarget.QueueFree();

        WeaponInstance round = MaxedWeapon("music_box");
        round.Ascend("round");
        MethodInfo pulse = typeof(Player).GetMethod("OrbitPulse", Private);
        float low = 10f, high = 0f;
        for (int i = 0; i < 40; i++)
        {
            float factor = (float)pulse.Invoke(player, new object[] { round, 0.05f });
            low = Mathf.Min(low, factor);
            high = Mathf.Max(high, factor);
        }
        Check(low < 0.7f && high > 1.5f, $"Ronde : l'orbite va de {low:0.00} à {high:0.00} fois la portée en 2 s");
        enemy.QueueFree();
        fragments.QueueFree();
        player.QueueFree();
    }

    /// <summary>
    /// Grammaire d'attaque (plan 26 Q5) : une faute de clé écarte l'arme avec un diagnostic qui nomme l'arme, la voie
    /// et le champ, au lieu de la jouer avec le motif ou la famille par défaut.
    /// </summary>
    private void CheckGrammarRejectsUnknownKeys()
    {
        Godot.Collections.Dictionary Weapon(string type, string pattern, string pathPattern)
        {
            Godot.Collections.Dictionary path = new() { ["id"] = "voie_test" };
            if (pathPattern != null)
                path["attack_pattern"] = pathPattern;
            return new Godot.Collections.Dictionary
            {
                ["id"] = "arme_test",
                ["type"] = type,
                ["attack_pattern"] = pattern,
                ["stats"] = new Godot.Collections.Dictionary { ["damage"] = 10, ["attack_speed"] = 1, ["range"] = 70 },
                ["ascensions"] = new Godot.Collections.Array { path, new Godot.Collections.Dictionary { ["id"] = "autre_voie" } },
            };
        }

        bool valid = WeaponDataLoader.TryParseWeapon(Weapon("melee", "circular", "chain"), out WeaponData parsed, out _)
            && parsed.Category == WeaponCategory.Melee && parsed.AttackPattern == AttackPatternKind.Circular
            && parsed.Ascensions[0].AttackPattern == AttackPatternKind.Chain && parsed.Ascensions[1].AttackPattern == null;
        bool badPattern = !WeaponDataLoader.TryParseWeapon(Weapon("ranged", "homming", null), out WeaponData rejected, out string patternError)
            && rejected == null && patternError.Contains("arme_test") && patternError.Contains("attack_pattern") && patternError.Contains("homming");
        bool badCategory = !WeaponDataLoader.TryParseWeapon(Weapon("Melee", "arc", null), out _, out string categoryError)
            && categoryError.Contains("arme_test") && categoryError.Contains("type") && categoryError.Contains("Melee");
        bool badPath = !WeaponDataLoader.TryParseWeapon(Weapon("melee", "arc", "spiral"), out _, out string pathError)
            && pathError.Contains("voie_test") && pathError.Contains("spiral");
        Check(valid && badPattern && badCategory && badPath,
            $"Grammaire : clés connues lues en types, motif/famille/voie inconnus refusés [{patternError} | {categoryError} | {pathError}]");
    }

    /// <summary>
    /// Contrat des armes (plan 26 Q6a) : chaque faute de données écarte l'arme avec un message qui nomme l'arme, la voie
    /// et le champ ; une arme conforme reçoit les secours du contrat pour les réglages qu'elle ne déclare pas.
    /// </summary>
    private void CheckContractRejectsBadData()
    {
        Godot.Collections.Dictionary Valid() => new()
        {
            ["id"] = "arme_test",
            ["type"] = "ranged",
            ["attack_pattern"] = "linear",
            ["stats"] = new Godot.Collections.Dictionary { ["damage"] = 10, ["attack_speed"] = 1, ["range"] = 200 },
            ["growth"] = new Godot.Collections.Dictionary { ["damage"] = 3 },
            ["special_effect"] = new Godot.Collections.Dictionary { ["type"] = "delayed_echo", ["echo_delay"] = 0.3, ["echo_damage_percent"] = 0.6 },
            ["ascensions"] = new Godot.Collections.Array
            {
                new Godot.Collections.Dictionary { ["id"] = "voie_test" },
                new Godot.Collections.Dictionary { ["id"] = "autre_voie" },
            },
        };
        Godot.Collections.Dictionary Path(Godot.Collections.Dictionary weapon) => weapon["ascensions"].AsGodotArray()[0].AsGodotDictionary();
        Godot.Collections.Dictionary Table(Godot.Collections.Dictionary owner, string key) => owner[key].AsGodotDictionary();

        bool valid = WeaponDataLoader.TryParseWeapon(Valid(), out WeaponData parsed, out string validError)
            && Mathf.IsEqualApprox(parsed.SpecialEffect.Get(SpecialEffectParam.EchoRadius), 40f)
            && Mathf.IsEqualApprox(parsed.SpecialEffect.Get(SpecialEffectParam.EchoCount), 1f);
        Check(valid, $"Contrat : arme conforme acceptée, secours du contrat résolus (écho 40 px, 1 répétition) {validError}");

        (string Label, System.Action<Godot.Collections.Dictionary> Mutate, string Expected)[] cases =
        {
            ("stat inconnue", w => Table(w, "stats")["damge"] = 5, "damge"),
            ("stat non numérique", w => Table(w, "stats")["damage"] = "dix", "nombre attendu"),
            ("stat non finie", w => Table(w, "stats")["damage"] = float.PositiveInfinity, "non finie"),
            ("stat nulle (NaN écrit en null)", w => Table(w, "stats")["damage"] = float.NaN, "nombre attendu"),
            ("stat hors bornes", w => Table(w, "stats")["arc_angle"] = 400, "hors de"),
            ("stat obligatoire absente", w => Table(w, "stats").Remove("range"), "range obligatoire"),
            ("croissance inconnue", w => Table(w, "growth")["rang"] = 1, "rang"),
            ("croissance sans réglage d'amélioration", w => Table(w, "growth")["spread_angle"] = 1, "réglage d'amélioration"),
            ("effet à l'impact inconnu", w => w["on_hit_effect"] = new Godot.Collections.Dictionary { ["type"] = "bleed", ["duration"] = 1 }, "bleed"),
            ("réglage d'impact absent", w => w["on_hit_effect"] = new Godot.Collections.Dictionary { ["type"] = "slow", ["duration"] = 1 }, "value obligatoire"),
            ("réglage d'impact inconnu", w => w["on_hit_effect"] = new Godot.Collections.Dictionary { ["type"] = "slow", ["value"] = 0.5, ["duration"] = 1, ["valeur"] = 1 }, "valeur"),
            ("effet spécial inconnu", w => Table(w, "special_effect")["type"] = "delayed_ecco", "delayed_ecco"),
            ("réglage spécial absent", w => Table(w, "special_effect").Remove("echo_delay"), "echo_delay obligatoire"),
            ("réglage spécial non entier", w => Table(w, "special_effect")["echo_count"] = 1.5, "pas entier"),
            ("champ d'arme inconnu", w => w["dammage_type"] = "physical", "dammage_type"),
            ("arme en main retirée (§65)", w => w["held_sprite"] = "assets/weapons/held/weapon_held_sickle.png", "held_sprite"),
            ("champ de voie inconnu", w => Path(w)["stat_multiplier"] = new Godot.Collections.Dictionary(), "stat_multiplier"),
            ("multiplicateur nul", w => Path(w)["stat_multipliers"] = new Godot.Collections.Dictionary { ["damage"] = 0 }, "stat_multipliers.damage"),
            ("remplacement de stat inconnue", w => Path(w)["stat_overrides"] = new Godot.Collections.Dictionary { ["piercing"] = 1 }, "piercing"),
            ("réglage de voie hors de l'effet", w => Path(w)["special_overrides"] = new Godot.Collections.Dictionary { ["echo_radiuss"] = 1 }, "echo_radiuss"),
            ("réglage de voie sans effet spécial", w => { w.Remove("special_effect"); Path(w)["special_overrides"] = new Godot.Collections.Dictionary { ["n"] = 3 }; }, "pas d'effet spécial"),
            ("drapeau inconnu", w => Path(w)["flags"] = new Godot.Collections.Array { "orbit_pluse" }, "orbit_pluse"),
            ("réglage de drapeau absent", w => { Path(w)["flags"] = new Godot.Collections.Array { "orbit_pulse" }; Path(w)["params"] = new Godot.Collections.Dictionary { ["pulse_min"] = 0.6, ["pulse_max"] = 1.6 }; }, "pulse_period obligatoire"),
            ("son introuvable", w => w["attack_audio"] = "sfx_inexistant", "sfx_inexistant"),
            ("son d'impact introuvable", w => w["impact_audio"] = "sfx_impact_inexistant", "sfx_impact_inexistant"),
            ("image introuvable", w => w["sprite"] = "assets/weapons/icons/inexistant.png", "inexistant.png"),
            ("style inconnu", w => w["fx"] = new Godot.Collections.Dictionary { ["style"] = "spin" }, "spin"),
            ("famille inconnue", w => w["fx"] = new Godot.Collections.Dictionary { ["family"] = "gold" }, "gold"),
            ("projectile introuvable", w => w["fx"] = new Godot.Collections.Dictionary { ["projectile"] = "comet" }, "comet"),
        };
        foreach ((string label, System.Action<Godot.Collections.Dictionary> mutate, string expected) in cases)
        {
            Godot.Collections.Dictionary weapon = Valid();
            mutate(weapon);
            bool rejected = !WeaponDataLoader.TryParseWeapon(weapon, out WeaponData result, out string error);
            Check(rejected && result == null && error.Contains("arme_test") && error.Contains(expected)
                && (!label.Contains("voie") || error.Contains("voie_test")),
                $"Contrat : {label} refusé [{error}]");
        }
    }

    /// <summary>
    /// La matière vient du coup résolu, tandis que critiques et objets gardent leurs retours distincts.
    /// </summary>
    private void CheckImpactSounds()
    {
        Enemy enemy = SpawnStill(new Vector2(500f, 0f));
        enemy.ProcessMode = ProcessModeEnum.Disabled;
        List<string> requests = new();
        void OnSound(string key, AudioManager.SoundOutcome outcome) => requests.Add(key);
        AudioManager.Instance.SoundRequested += OnSound;
        try
        {
            HashSet<string> families = new();
            bool followedSource = true;
            foreach (WeaponData data in WeaponDataLoader.GetAll())
            {
                requests.Clear();
                WeaponInstance held = new(data);
                AttackContext source = new(_player.GetInstanceId(), held, 1, DamageKind.DirectWeapon);
                enemy.TakeDamage(1f, source: source);
                followedSource &= !string.IsNullOrEmpty(data.ImpactAudio) && requests.Count == 1
                    && requests[0] == data.ImpactAudio;
                families.Add(data.ImpactAudio);
            }
            Check(followedSource && families.Count == 6, "Impacts : toutes les armes suivent leur provenance dans six familles de matière");
            WeaponInstance blade = new(WeaponDataLoader.Get("chipped_blade"));
            AttackContext bladeSource = new(_player.GetInstanceId(), blade, 2, DamageKind.DirectWeapon);
            requests.Clear();
            enemy.TakeDamage(1f, isCrit: true, source: bladeSource);
            Check(requests.Count == 1 && requests[0] == "sfx_hit_critique", "Impact critique : son distinct, sans superposer la matière");
            requests.Clear();
            enemy.TakeDamage(1f, source: bladeSource.As(DamageKind.SecondaryWeapon));
            Check(requests.Count == 1 && requests[0] == blade.Base.ImpactAudio, "Impact secondaire : garde la matière de l'arme d'origine");
            requests.Clear();
            enemy.TakeDamage(1f, source: bladeSource.As(DamageKind.Passive));
            Check(requests.Count == 1 && requests[0] == "sfx_hit_ennemi", "Objet passif : ne prend pas la matière d'une arme étrangère");
            requests.Clear();
            enemy.TakeDamage(1f);
            Check(requests.Count == 1 && requests[0] == "sfx_hit_ennemi", "Dommage sans arme : impact générique conservé");
            requests.Clear();
            enemy.TakeDamage(1f, showImpact: false, source: bladeSource);
            Check(requests.Count == 0, "Impact continu entre deux retours : reste silencieux");
        }
        finally
        {
            AudioManager.Instance.SoundRequested -= OnSound;
            enemy.QueueFree();
        }
    }

    /// <summary>La Boîte joue au contact avec sa propre clé et cesse de sonner après son retrait.</summary>
    private void CheckOrbitalSound()
    {
        Player player = GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate<Player>();
        AddChild(player);
        player.InitializeCharacter(CharacterDataLoader.Get("traqueur"));
        player.ProcessMode = ProcessModeEnum.Disabled;
        while (player.WeaponSlots.Count > 0)
            player.RemoveWeapon(0);
        WeaponData musicBox = WeaponDataLoader.Get("music_box");
        int requests = 0;
        void OnSound(string key, AudioManager.SoundOutcome outcome)
        {
            if (key == musicBox.AttackAudio)
                requests++;
        }
        AudioManager.Instance.SoundRequested += OnSound;
        Enemy enemy = EnemyScene.Instantiate<Enemy>();
        AddChild(enemy);
        enemy.Initialize(EnemyDataLoader.Get("rodeur"), 100000f, 1f);
        enemy.SetTicking(false);
        enemy.ProcessMode = ProcessModeEnum.Disabled;
        try
        {
            player.AddWeapon(musicBox);
            typeof(Player).GetMethod("OnWeaponAttackTimeout", Private).Invoke(player, new object[] { 0 });
            Check(requests == 0, "Boîte : silencieuse à l'équipement et au minuteur sans contact");
            // Une autre arme devient courante avant le contact de la note.
            player.AddWeapon(WeaponDataLoader.Get("makeshift_bow"));
            // Contact d'une orbe : ce qu'appelle ProcessOrbitalWeapons quand une créature entre dans son disque.
            MethodInfo orbitalHit = typeof(Player).GetMethod("OrbitalHit", Private);
            orbitalHit.Invoke(player, new object[] { enemy });
            Check(requests == 1 && player.GetDamageDealt("music_box") > 0f,
                "Boîte : son de l'arme orbitale au contact malgré une autre arme courante");
            player.RemoveWeapon(0);
            orbitalHit.Invoke(player, new object[] { enemy });
            Check(requests == 1, "Boîte : aucun son d'une ancienne orbe après retrait");
        }
        finally
        {
            AudioManager.Instance.SoundRequested -= OnSound;
            enemy.QueueFree();
            player.QueueFree();
        }
    }

    /// <summary>Le Transistor émet au départ, jamais pendant le maintien ni après retrait.</summary>
    private void CheckSustainedConeSound()
    {
        Player player = GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate<Player>();
        AddChild(player);
        player.InitializeCharacter(CharacterDataLoader.Get("traqueur"));
        player.ProcessMode = ProcessModeEnum.Disabled;
        while (player.WeaponSlots.Count > 0)
            player.RemoveWeapon(0);
        WeaponData radio = WeaponDataLoader.Get("last_broadcast");
        player.AddWeapon(radio);
        int requests = 0;
        void OnSound(string key, AudioManager.SoundOutcome outcome)
        {
            if (key == radio.AttackAudio)
                requests++;
        }
        AudioManager.Instance.SoundRequested += OnSound;
        MethodInfo attack = typeof(Player).GetMethod("OnWeaponAttackTimeout", Private);
        MethodInfo process = typeof(Player).GetMethod("ProcessSustainedCone", Private);
        FieldInfo active = typeof(Player).GetField("_isConeActive", Private);
        try
        {
            attack.Invoke(player, new object[] { 0 });
            Check(!string.IsNullOrEmpty(radio.AttackAudio) && requests == 1 && (bool)active.GetValue(player),
                "Transistor : un son au départ du cône");
            for (int frame = 0; frame < 20; frame++)
            {
                process.Invoke(player, new object[] { 0.01f });
                attack.Invoke(player, new object[] { 0 });
            }
            Check(requests == 1, "Transistor : aucun son supplémentaire pendant le maintien");
            process.Invoke(player, new object[] { 100f });
            attack.Invoke(player, new object[] { 0 });
            Check(requests == 2, "Transistor : un nouveau son à la reprise");
            player.RemoveWeapon(0);
            process.Invoke(player, new object[] { 0.1f });
            Check(requests == 2 && !(bool)active.GetValue(player), "Transistor : retrait sans son ni cône résiduel");
        }
        finally
        {
            AudioManager.Instance.SoundRequested -= OnSound;
            player.QueueFree();
        }
    }

    private static bool AnySfxPlaying()
    {
        foreach (Node child in AudioManager.Instance.GetChildren())
            if (child is AudioStreamPlayer { Playing: true } player && player.Name.ToString().StartsWith("Sfx"))
                return true;
        return false;
    }

    /// <summary>
    /// Tirs à la chaîne (DECISIONS §59 et §123) : trois flèches sur un ennemi seul ne se superposent plus, deux attendent
    /// avant de partir, et toutes touchent leur cible ; deux flèches sur deux ennemis partent aussi l'une après l'autre ;
    /// chaque flèche sonne à son départ.
    /// </summary>
    private async Task CheckVolleyOnSingleTarget()
    {
        CombatPools pools = new() { Name = "CombatPools" };
        AddChild(pools);
        // Un joueur neuf : celui du banc porte les effets des contrôles précédents (échos, déclencheurs). Leurs
        // minuteurs d'arme sont suspendus pour que seuls les tirs commandés comptent.
        Player player = GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate<Player>();
        AddChild(player);
        player.InitializeCharacter(CharacterDataLoader.Get("traqueur"));
        player.SetPhysicsProcess(false);
        player.IsAIControlled = true;
        player.ProcessMode = ProcessModeEnum.Disabled;
        _player.ProcessMode = ProcessModeEnum.Disabled;
        typeof(Player).GetField("_critChance", Private).SetValue(player, 0f);
        FieldInfo equipped = typeof(Player).GetField("_equippedWeapon", Private);
        MethodInfo ranged = typeof(Player).GetMethod("PerformRangedAttack", Private);
        Enemy enemy = EnemyScene.Instantiate<Enemy>();
        AddChild(enemy);
        enemy.Initialize(EnemyDataLoader.Get("rodeur"), 100000f, 1f);
        enemy.SetTicking(false);
        enemy.Position = player.Position + new Vector2(90f, 0f);
        Enemy second = null;
        int hits = 0;
        int attackSounds = 0;
        string soundKey = WeaponDataLoader.Get("makeshift_bow").AttackAudio;
        // Seuls les sons entendus comptent : un départ écrêté par l'intervalle minimal du son resterait muet.
        void OnSound(string key, AudioManager.SoundOutcome outcome)
        {
            if (key == soundKey && outcome is AudioManager.SoundOutcome.Played or AudioManager.SoundOutcome.VoiceStolen)
                attackSounds++;
        }
        AudioManager.Instance.SoundRequested += OnSound;
        EventBus bus = GetNode<EventBus>("/root/EventBus");
        EventBus.EntityDamagedEventHandler onDamaged = (target, _) => hits += target == enemy ? 1 : 0;
        bus.EntityDamaged += onDamaged;
        try
        {
            // Les contrôles précédents, joués en temps accéléré, laissent des voix occupées à l'horloge : attendre qu'elles se libèrent.
            for (int frame = 0; frame < 180 && AnySfxPlaying(); frame++)
            {
                OS.DelayMsec(17);
                await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            }

            async Task<(int Hits, int Flying, int Waiting)> Fire(int count)
            {
                WeaponInstance bow = new(WeaponDataLoader.Get("makeshift_bow"));
                if (count > 1)
                    bow.ApplyUpgrade(new[] { new StatGain("projectile_count", count - 1f) });
                equipped.SetValue(player, bow);
                hits = 0;
                attackSounds = 0;
                ranged.Invoke(player, new object[] { AttackPatternKind.Linear });
                int flying = 0, waiting = 0;
                foreach (Node child in pools.GetChildren())
                {
                    if (child is not Projectile shot || shot.ProcessMode == ProcessModeEnum.Disabled)
                        continue;
                    flying += shot.Visible ? 1 : 0;
                    waiting += shot.Visible ? 0 : 1;
                }
                // Au rythme réel : l'intervalle minimal d'un son se compte à l'horloge, et le banc tourne en temps accéléré.
                for (int frame = 0; frame < 90; frame++)
                {
                    OS.DelayMsec(17);
                    await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
                }
                Check(attackSounds == count, $"Arc à {count} projectiles : un départ sonore par flèche ({attackSounds} demandes)");
                return (hits, flying, waiting);
            }

            (int singleHits, _, _) = await Fire(1);
            (int volleyHits, int flying, int waiting) = await Fire(3);
            Check(singleHits > 0 && flying == 1 && waiting == 2 && volleyHits == 3 * singleHits,
                $"Chaîne : 3 flèches sur un ennemi seul, {flying} part, {waiting} attendent leur tour ; {volleyHits} impacts pour {singleHits} avec une flèche");

            second = EnemyScene.Instantiate<Enemy>();
            AddChild(second);
            second.Initialize(EnemyDataLoader.Get("rodeur"), 100000f, 1f);
            second.SetTicking(false);
            second.Position = player.Position + new Vector2(-90f, 0f);
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            (_, int flyingPair, int waitingPair) = await Fire(2);
            Check(flyingPair == 1 && waitingPair == 1,
                $"Chaîne : 2 flèches sur 2 ennemis, {flyingPair} part, {waitingPair} attend son tour au lieu de partir en même temps");
        }
        finally
        {
            AudioManager.Instance.SoundRequested -= OnSound;
            bus.EntityDamaged -= onDamaged;
            enemy.QueueFree();
            second?.QueueFree();
            player.QueueFree();
            pools.QueueFree();
            _player.ProcessMode = ProcessModeEnum.Inherit;
        }
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
    }

    /// <summary>
    /// Plan 21 G6a : les 24 armes montent leur nombre (tirs, frappes, ondes, orbes, cibles de chaîne) à un poids franc ;
    /// une frappe de mêlée en plus touche à pleins dégâts ; une onde de plus double le cône ; Papier carbone favorisé.
    /// </summary>
    private async Task CheckCountForAllWeapons()
    {
        int missing = 0;
        float lowestShare = 1f;
        foreach (WeaponData data in WeaponDataLoader.GetAll())
        {
            string count = data.AttackPattern switch
            {
                AttackPatternKind.Orbital => "projectile_count",
                AttackPatternKind.Chain or AttackPatternKind.Bounce => "chain_targets",
                _ => "projectile_count",
            };
            float total = 0f;
            foreach (float weight in data.Growth.Values)
                total += weight;
            if (!data.Growth.TryGetValue(count, out float own))
            {
                missing++;
                continue;
            }
            lowestShare = Mathf.Min(lowestShare, own / total);
        }
        Check(missing == 0 && lowestShare >= 0.12f,
            $"Nombre pour toutes les armes : {missing} sans, part la plus faible {lowestShare:P0} des tirages de stat");

        FieldInfo equipped = typeof(Player).GetField("_equippedWeapon", Private);
        FieldInfo crit = typeof(Player).GetField("_critChance", Private);
        float critBefore = (float)crit.GetValue(_player);
        crit.SetValue(_player, 0f);
        WeaponInstance blade = new(WeaponDataLoader.Get("chipped_blade"));
        equipped.SetValue(_player, blade);
        Enemy enemy = EnemyScene.Instantiate<Enemy>();
        AddChild(enemy);
        enemy.Initialize(EnemyDataLoader.Get("rodeur"), 100000f, 1f);
        enemy.SetTicking(false);
        enemy.Position = _player.Position + new Vector2(40f, 0f);
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

        MethodInfo melee = typeof(Player).GetMethod("PerformMeleeAttack", Private);
        melee.Invoke(_player, new object[] { AttackPatternKind.Arc });
        float single = _player.GetDamageDealt("chipped_blade");
        blade.ApplyUpgrade(new[] { new StatGain("projectile_count", 1f) });
        melee.Invoke(_player, new object[] { AttackPatternKind.Arc });
        float twice = _player.GetDamageDealt("chipped_blade") - single;
        Check(single > 0f && twice > single * 1.8f && twice < single * 2.2f
              && StatCatalog.Name("projectile_count", blade.Base) != StatCatalog.Name("projectile_count"),
            $"Lame ébréchée à 2 frappes : {single:0.#} puis {twice:0.#} dégâts, libellé « {StatCatalog.Name("projectile_count", blade.Base)} »");
        enemy.QueueFree();

        WeaponInstance radio = new(WeaponDataLoader.Get("last_broadcast"));
        equipped.SetValue(_player, radio);
        MethodInfo activate = typeof(Player).GetMethod("ActivateSustainedCone", Private);
        MethodInfo deactivate = typeof(Player).GetMethod("DeactivateSustainedCone", Private);
        FieldInfo coneDamage = typeof(Player).GetField("_coneBaseDamage", Private);
        activate.Invoke(_player, new object[] { radio.SpecialEffect });
        float oneWave = (float)coneDamage.GetValue(_player);
        deactivate.Invoke(_player, null);
        radio.ApplyUpgrade(new[] { new StatGain("projectile_count", 1f) });
        equipped.SetValue(_player, radio);
        activate.Invoke(_player, new object[] { radio.SpecialEffect });
        float twoWaves = (float)coneDamage.GetValue(_player);
        deactivate.Invoke(_player, null);
        Check(oneWave > 0f && Mathf.IsEqualApprox(twoWaves, oneWave * 2f),
            $"Transistor à 2 ondes : cône de {oneWave:0.#} à {twoWaves:0.#} dégâts de base");
        crit.SetValue(_player, critBefore);

        Check(Mathf.IsEqualApprox(PassiveSouvenirDataLoader.Get("souffle_du_neant").OfferWeight, 2f)
              && Mathf.IsEqualApprox(PassiveSouvenirDataLoader.Get("ancrage").OfferWeight, 1f),
            "Papier carbone pèse 2 dans les offres, les autres objets 1");

        WeaponVisualConfig visuals = WeaponVisualConfig.Load();
        Check(Mathf.IsEqualApprox(visuals.ScaleFor(1f), 1f) && Mathf.IsEqualApprox(visuals.ScaleFor(1.5f), 1.5f)
              && Mathf.IsEqualApprox(visuals.ScaleFor(3f), visuals.MaxScale) && Mathf.IsEqualApprox(visuals.ScaleFor(0.6f), 1f),
            $"Visuels d'arme (G6e) : taille ×1,5 → échelle {visuals.ScaleFor(1.5f):0.0#}, plafond {visuals.MaxScale:0.0#}, jamais sous 1");
    }

    /// <summary>
    /// Plan 22 C2a : la Trempe ajoute une stat aux 5 améliorations suivantes, décomptée à l'application ; la Retrempe
    /// remplace les gains de la dernière amélioration sans changer le niveau.
    /// </summary>
    private void CheckTemper()
    {
        WeaponInstance blade = FindSlot("chipped_blade") ?? AddAndFind("chipped_blade");
        RandomNumberGenerator rng = new() { Seed = 21 };
        UpgradeRarity common = UpgradeRoller.Get("common");
        FragmentOption Offer() => UpgradeRoller.RollGains(new FragmentOption(blade.Id, "weapon_upgrade", blade.Name), _player, common, rng);

        _player.GrantTemper(5);
        bool tempered = true;
        for (int i = 0; i < 5; i++)
        {
            FragmentOption option = Offer();
            tempered &= option.WeaponGains.Count == 2 && option.ApplyTo(_player);
        }
        FragmentOption plain = Offer();
        Check(tempered && _player.TemperCharges == 0 && plain.WeaponGains.Count == 1,
            $"Trempe : 5 améliorations communes à 2 stats, charges {_player.TemperCharges}, puis {plain.WeaponGains.Count} stat");

        // Amélioration U1, puis Retrempe en U2 : l'arme doit valoir celle qui aurait reçu U2 à la place de U1.
        List<StatGain> first = UpgradeRoller.RollWeaponGains(blade, common, rng);
        List<StatGain> second = UpgradeRoller.RollWeaponGains(blade, UpgradeRoller.Get("rare"), rng);
        WeaponInstance expected = blade.PreviewWith(second);
        _player.UpgradeWeapon(blade.Id, first, "common");
        int level = blade.Level;
        bool retempered = _player.RetemperWeapon(blade.Id, second, "rare");
        bool same = true;
        foreach (string stat in blade.Base.Growth.Keys)
            same &= Mathf.IsEqualApprox(blade.GetStat(stat), expected.GetStat(stat));
        Check(retempered && same && blade.Level == level && blade.LastRarityId == "rare" && blade.LastGains.Count == second.Count,
            $"Retrempe : gains de la dernière amélioration remplacés ({first.Count} → {second.Count} stats, rare), niveau {level} inchangé");
    }

    private static WeaponInstance MaxedWeapon(string id)
    {
        WeaponInstance weapon = new(WeaponDataLoader.Get(id));
        while (weapon.CanLevelUp)
            weapon.ApplyUpgrade(Array.Empty<StatGain>());
        return weapon;
    }

    private void Check(bool passed, string message)
    {
        GD.Print($"[WeaponRegression] {(passed ? "PASS" : "FAIL")} {message}");
        if (!passed) _failures++;
    }
}
