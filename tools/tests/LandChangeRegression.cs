using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Progression;
using Vestiges.Score;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// Changement de terre (plan 51, P1) sur la vraie scène Main, terres allumées (--lands) : trois terres enchaînées par le
/// vrai passage, foule et orbe d'XP posées avant chacun. Rien de la terre quittée ne reste (lieux, coffres, obstacles,
/// créatures), la terre suivante est peuplée et d'un autre biome, l'état du joueur et l'horloge ne bougent pas.
/// </summary>
public partial class LandChangeRegression : Node
{
    private const ulong Seed = 51;
    private const int Lands = 3;
    private const float OrbXp = 7f;
    private WorldSetup _world;
    private Player _player;
    private int _failures;

    public override async void _Ready()
    {
        try
        {
            ProcessMode = ProcessModeEnum.Always;
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GameManager manager = GetNode<GameManager>("/root/GameManager");
            manager.RunSeed = Seed;
            GetTree().CurrentScene = null;
            _world = GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<WorldSetup>();
            GetTree().Root.AddChild(_world);
            GetTree().CurrentScene = _world;
            while (!_world.IsWorldReady || GetTree().Paused || manager.CurrentState != GameManager.GameState.Run)
                await Frame();
            _player = _world.GetNode<Player>("Player");
            _player.SurviveFatalHitsForTests = true;
            await CheckLands();
            GD.Print($"[LandChangeRegression] RESULT failures={_failures}");
            GetTree().Quit(_failures == 0 ? 0 : 1);
        }
        catch (Exception exception)
        {
            GD.PushError($"[LandChangeRegression] {exception}");
            GetTree().Quit(2);
        }
    }

    private async Task CheckLands()
    {
        LandTransition transition = _world.GetNode<LandTransition>("LandTransition");
        HashSet<string> biomes = new() { _world.Land.BiomeIds[0] };
        Check(_world.Land.BiomeIds.Count == 1 && _world.Generator.MapRadiusX == _world.Land.MapRadius,
            $"terre 0 : un biome ({_world.Land.BiomeIds[0]}), rayon {_world.Generator.MapRadiusX}");
        int memorials = Memorial.All.Count;
        int chests = Chest.Closed.Count;
        Check(GroundHoles() == 0, $"terre 0 : sol sans trou ({GroundHoles()})");
        Check(memorials > 0 && chests > 0 && ObstacleField.Count > 0,
            $"terre 0 peuplée : {memorials} Mémoriaux, {chests} coffres, {ObstacleField.Count} obstacles");

        for (int land = 1; land < Lands; land++)
        {
            Node enemies = _world.GetNode("EnemyContainer");
            PlaceCrowd(enemies);
            CombatPools.Instance.SpawnXpOrb(_player.GlobalPosition + new Vector2(1500f, 0f), OrbXp);
            await Frame();
            await Frame();
            Check(CountActive(enemies) >= 6, $"terre {land - 1} : foule posée avant le passage ({CountActive(enemies)})");

            Snapshot before = Snapshot.Of(_world, _player);
            float gained = 0f;
            void OnXp(float amount) => gained += amount;
            EventBus bus = GetNode<EventBus>("/root/EventBus");
            bus.XpGained += OnXp;
            await transition.GoToLandAsync(land);
            bus.XpGained -= OnXp;
            Snapshot after = Snapshot.Of(_world, _player);

            string biome = _world.Land.BiomeIds[0];
            Check(transition.LandIndex == land && _world.Land.Index == land && biomes.Add(biome),
                $"terre {land} : nouveau biome ({biome}), graine {_world.LandSeed}");
            Check(_world.IsWorldReady && _world.Generator.MapRadiusX == _world.Land.MapRadius && !GetTree().Paused,
                $"terre {land} : construite (rayon {_world.Generator.MapRadiusX}), jeu repris");
            Check(GroundHoles() == 0, $"terre {land} : sol sans trou ({GroundHoles()})");
            Check(Memorial.All.Count == memorials && Chest.Closed.Count == chests && ObstacleField.Count > 0,
                $"terre {land} : seuls ses lieux comptent ({Memorial.All.Count} Mémoriaux, {Chest.Closed.Count} coffres, {ObstacleField.Count} obstacles)");
            Check(CountActive(enemies) == 0, $"terre {land} : aucune créature de la terre quittée ({CountActive(enemies)})");
            Check(Mathf.IsEqualApprox(gained, OrbXp), $"terre {land} : l'orbe restée au sol est revenue ({gained} XP)");
            Check(_player.GlobalPosition == Vector2.Zero, $"terre {land} : joueur au départ ({_player.GlobalPosition})");
            Check(after.SameRunAs(before), $"terre {land} : run intacte ({before} → {after})");

            // Quelques secondes de jeu sur la terre neuve : apparitions, minimap, Effacement tournent sans erreur.
            _player.IsAIControlled = true;
            for (int frame = 0; frame < 240; frame++)
            {
                _player.AIInputOverride = Vector2.FromAngle(frame * 0.02f);
                await Frame();
            }
            _player.IsAIControlled = false;
            Check(_world.GetNode<RunClock>("RunClock").Seconds > after.Clock, $"terre {land} : l'horloge repart après le passage");
        }

        // Assez d'XP au sol pour monter de niveau : l'écran de choix garde la pause, la fin du passage ne la lève pas.
        CombatPools.Instance.SpawnXpOrb(_player.GlobalPosition + new Vector2(1500f, 0f), 5000f);
        await Frame();
        await Frame();
        // Par la commande de dev, comme depuis l'atelier de test (F4).
        Vestiges.Development.DevelopmentCommandResult command =
            _world.GetNode<Vestiges.Development.DevelopmentRunTools>("DevelopmentRunTools").Execute("land next");
        Check(command.Success && transition.IsChanging, $"commande « land next » : {command.Message}");
        while (transition.IsChanging)
            await Frame();
        Check(transition.LandIndex == Lands, $"commande « land next » : terre {transition.LandIndex}");
        bool levelUpShown = false;
        for (int frame = 0; frame < 30 && !levelUpShown; frame++)
        {
            await Frame();
            levelUpShown = GetTree().Paused && _world.GetNode<CanvasLayer>("LevelUpScreen").Visible;
        }
        Check(levelUpShown, $"terre {Lands} : la montée de niveau des orbes ramassées garde la pause sous son écran");
    }

    /// <summary>Ce que le passage ne doit jamais toucher : niveau, PV, armes, Essence, Péril, éliminations, horloge.</summary>
    private readonly record struct Snapshot(int Level, float Hp, int Weapons, int Essence, int Peril, int Kills, float Clock)
    {
        public static Snapshot Of(WorldSetup world, Player player) => new(
            player.GetNode<PlayerProgression>("PlayerProgression").CurrentLevel, player.CurrentHp, player.WeaponSlots.Count,
            world.GetNode<EssenceTracker>("EssenceTracker").CurrentEssence, world.GetNode<PerilManager>("PerilManager").Peril,
            world.GetNode<ScoreManager>("ScoreManager").TotalKills, world.GetNode<RunClock>("RunClock").Seconds);

        public bool SameRunAs(Snapshot before) => Level == before.Level && Hp == before.Hp && Weapons == before.Weapons
            && Essence == before.Essence && Peril == before.Peril && Kills == before.Kills && Clock == before.Clock;
    }

    /// <summary>Cellules de sol de la carte sans tuile : un terrain que le biome ne sait pas dessiner laisse un trou.</summary>
    private int GroundHoles()
    {
        WorldGenerator generator = _world.Generator;
        TileMapLayer ground = _world.GetNode<TileMapLayer>("Ground");
        int holes = 0;
        for (int x = -generator.MapRadiusX; x <= generator.MapRadiusX; x++)
        {
            for (int y = -generator.MapRadiusY; y <= generator.MapRadiusY; y++)
            {
                if (generator.IsWithinBounds(x, y) && !generator.IsErased(x, y) && ground.GetCellSourceId(new Vector2I(x, y)) < 0)
                    holes++;
            }
        }
        return holes;
    }

    private void PlaceCrowd(Node container)
    {
        Vestiges.Spawn.EnemyPool pool = _world.GetNode<Vestiges.Spawn.EnemyPool>("EnemyPool");
        for (int i = 0; i < 6; i++)
        {
            Enemy enemy = pool.Get();
            enemy.Position = _player.GlobalPosition + Vector2.FromAngle(i) * 300f;
            container.AddChild(enemy);
            enemy.Initialize(Vestiges.Infrastructure.EnemyDataLoader.Get("shade"), 1f, 1f);
        }
    }

    private static int CountActive(Node container)
    {
        int active = 0;
        foreach (Node child in container.GetChildren())
        {
            if (child is Enemy { IsActive: true })
                active++;
        }
        return active;
    }

    private SignalAwaiter Frame() => ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

    private void Check(bool passed, string message)
    {
        GD.Print($"[LandChangeRegression] {(passed ? "PASS" : "FAIL")} {message}");
        if (!passed)
            _failures++;
    }
}
