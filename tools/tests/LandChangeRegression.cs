using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Progression;
using Vestiges.Score;
using Vestiges.UI;
using Vestiges.Infrastructure;
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
    private int _memorials;
    private int _chests;

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
            CheckFrontRules();
            CheckFrontOnLand();
            // Comptes de la terre 0 avant que le cercle n'y fasse tomber le reste : chaque terre doit en avoir autant.
            _memorials = Memorial.All.Count;
            _chests = Chest.Closed.Count;
            await CheckMemorialCircle();
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

    /// <summary>Lot P2a, règles du front seul : ordre et heure des états, cœur intact, présence, stabilisation, ancre.</summary>
    private void CheckFrontRules()
    {
        LandFrontConfig config = LandFrontConfig.Load(out string error);
        Check(config != null, $"front : réglages valides ({error})");
        if (config == null)
            return;
        Check(LandFrontConfig.Parse(FileAccess.GetFileAsString("res://data/scaling/erasure.json")
            .Replace("\"heart_reached_seconds\": 720", "\"heart_reached_seconds\": 30"), out string order) == null && order.Contains("suivre"),
            $"front : cœur atteint avant la première chute refusé ({order})");
        LandFrontConfig quiet = new()
        {
            FirstFallSeconds = config.FirstFallSeconds, HeartReachedSeconds = config.HeartReachedSeconds,
            WarningSeconds = config.WarningSeconds, HeartRadius = config.HeartRadius, NoiseSeconds = 0f,
            NoiseFrequency = config.NoiseFrequency,
        };
        LandFront front = new(quiet, 1);
        Vector2I heart = new(0, 0), middle = new(1, 0), rim = new(2, 0);
        front.AddCell(heart, 0f);
        // À mi-chemin en surface entre le cœur et le bord : sa chute tombe à mi-chemin entre la première et le cœur atteint.
        front.AddCell(middle, Mathf.Sqrt((1f + quiet.HeartRadius * quiet.HeartRadius) / 2f));
        front.AddCell(rim, 1f);
        float rimFall = quiet.FirstFallSeconds;
        float third = quiet.WarningSeconds / 4f;
        front.Advance(rimFall - quiet.WarningSeconds * 0.5f + 0.1f);
        Check(Phase(front.MemoryAt(rim)) == ErasureManager.ErasureZonePhase.Frayed && Phase(front.MemoryAt(middle)) == ErasureManager.ErasureZonePhase.Anchored,
            $"front : le bord s'effiloche avant le milieu ({front.MemoryAt(rim):F2}, {front.MemoryAt(middle):F2})");
        front.Delay(rim, third);
        Check(Phase(front.MemoryAt(rim)) == ErasureManager.ErasureZonePhase.Fragile, $"front : la présence repousse la chute ({front.MemoryAt(rim):F2})");
        front.Advance(quiet.WarningSeconds);
        Check(front.MemoryAt(rim) == 0f && Phase(front.MemoryAt(rim)) == ErasureManager.ErasureZonePhase.Void,
            "front : le bord tombe dans le Néant après son préavis");
        float middleFall = (quiet.FirstFallSeconds + quiet.HeartReachedSeconds) / 2f;
        front.Advance(middleFall - front.Time - 1f);
        front.Restore(middle, 0.1f);
        Check(front.MemoryAt(middle) > 0f, "front : un lieu ravivé n'avance jamais la chute");
        front.Anchor(middle);
        front.Advance(quiet.HeartReachedSeconds * 3f);
        Check(front.MemoryAt(middle) == 1f && front.MemoryAt(heart) == 1f, "front : zone ancrée et cœur ne tombent jamais");
        Check(Mathf.IsEqualApprox(front.StandingShare(), 2f / 3f), $"front : terre restante {front.StandingShare():F2} (cœur et ancre)");
    }

    /// <summary>Lot P2a sur la vraie terre : toutes ses cellules au front, cœur sûr, bords déjà pâles à l'arrivée.</summary>
    private void CheckFrontOnLand()
    {
        ErasureManager erasure = _world.GetNode<ErasureManager>("ErasureManager");
        LandFront front = erasure.Front;
        Check(front != null && front.CellCount > 1000, $"front de la terre 0 : {front?.CellCount ?? 0} cellules");
        if (front == null)
            return;
        int pale = 0;
        foreach (Vector2I cell in front.Cells)
        {
            if (front.MemoryAt(cell) <= 0.75f)
                pale++;
        }
        Check(erasure.GetMemoryAt(Vector2.Zero) == 1f && pale > 0, $"front de la terre 0 : cœur intact, {pale} cellules du bord déjà fragiles");
        Check(Mathf.IsEqualApprox(front.StandingShare(), 1f), "front de la terre 0 : toute la terre debout à l'arrivée");
    }

    /// <summary>
    /// Lot P2b : sur une terre, raviver un Mémorial, c'est tenir son cercle. S'éloigner l'abandonne ; le tenir jusqu'au
    /// bout le ravive, allume une lumière et ancre sa zone, qui ne tombe plus même quand tout le reste est tombé.
    /// </summary>
    private async Task CheckMemorialCircle()
    {
        MemorialConfig config = Vestiges.Infrastructure.LandmarkDataLoader.Memorial;
        MemorialDirector director = _world.GetNode<MemorialDirector>("MemorialDirector");
        Vestiges.Spawn.SpawnManager spawner = _world.GetNode<Vestiges.Spawn.SpawnManager>("SpawnManager");
        EventBus bus = GetNode<EventBus>("/root/EventBus");
        Memorial memorial = Memorial.All[0];
        _player.IsGodMode = true;
        // Sans apparitions ni créatures : pas d'XP, donc pas d'écran de niveau qui figerait le maintien.
        spawner.ProcessMode = ProcessModeEnum.Disabled;
        foreach (Node child in _world.GetNode("EnemyContainer").GetChildren())
        {
            if (child is Enemy enemy)
                enemy.Withdraw();
        }

        _player.GlobalPosition = memorial.GlobalPosition;
        bus.EmitSignal(EventBus.SignalName.MemorialInteracted, memorial);
        Check(memorial.State == Memorial.MemorialState.Gathering && _world.GetNode("PoiContainer").GetNodeOrNull("MemorialCircle") != null
            && Mathf.IsEqualApprox(spawner.EncounterDensityMultiplier, config.CircleCrowdDensity),
            $"cercle : il apparaît, la foule se resserre (×{spawner.EncounterDensityMultiplier})");
        await Frames(60);
        _player.GlobalPosition = memorial.GlobalPosition + new Vector2(config.CircleAbandonPx + 50f, 0f);
        await Frames(2);
        Check(memorial.State == Memorial.MemorialState.Dormant && spawner.EncounterDensityMultiplier == 1f,
            "cercle : trop loin, la tentative s'arrête et la foule se relâche");

        _player.GlobalPosition = memorial.GlobalPosition;
        bus.EmitSignal(EventBus.SignalName.MemorialInteracted, memorial);
        await Frames(Mathf.CeilToInt(config.CircleSeconds * Engine.PhysicsTicksPerSecond) + 10);
        ChoiceScreen choices = _world.GetNode<ChoiceScreen>("ChoiceScreen");
        // Le réveil animé se déroule en temps réel, même quand le banc accélère le jeu : l'attendre à l'horloge.
        ulong deadline = Time.GetTicksMsec() + 15_000;
        while (!choices.IsOpen && Time.GetTicksMsec() < deadline)
            await Frame();
        for (int attempt = 0; attempt < 10 && choices.IsOpen; attempt++)
        {
            choices.Activate(choices.FirstEnabled());
            await Frame();
        }
        Check(memorial.State == Memorial.MemorialState.Awake && director.LitThisLand == 1 && !GetTree().Paused,
            $"cercle tenu {config.CircleSeconds} s : Mémorial ravivé, {director.LitThisLand} lumière, bénédiction choisie");

        LandFront front = _world.GetNode<ErasureManager>("ErasureManager").Front;
        front.Advance(front.Config.HeartReachedSeconds * 2f);
        Vector2I memorialCell = new(Mathf.FloorToInt(memorial.GlobalPosition.X / 128f), Mathf.FloorToInt(memorial.GlobalPosition.Y / 128f));
        Check(front.MemoryAt(memorialCell) == 1f && front.StandingShare() < 0.2f,
            $"cercle : la zone du Mémorial est ancrée quand le reste est tombé (terre restante {front.StandingShare():P0})");
        _player.GlobalPosition = Vector2.Zero;
        _player.IsGodMode = false;
        spawner.ProcessMode = ProcessModeEnum.Inherit;
    }

    private async Task Frames(int count)
    {
        for (int i = 0; i < count; i++)
            await Frame();
    }

    private static ErasureManager.ErasureZonePhase Phase(float memory) => memory <= 0f ? ErasureManager.ErasureZonePhase.Void
        : memory <= 0.25f ? ErasureManager.ErasureZonePhase.Erased : memory <= 0.5f ? ErasureManager.ErasureZonePhase.Frayed
        : memory <= 0.75f ? ErasureManager.ErasureZonePhase.Fragile : ErasureManager.ErasureZonePhase.Anchored;

    private async Task CheckLands()
    {
        LandTransition transition = _world.GetNode<LandTransition>("LandTransition");
        HashSet<string> biomes = new() { _world.Land.BiomeIds[0] };
        Check(_world.Land.BiomeIds.Count == 1 && _world.Generator.MapRadiusX == _world.Land.MapRadius,
            $"terre 0 : un biome ({_world.Land.BiomeIds[0]}), rayon {_world.Generator.MapRadiusX}");
        int memorials = _memorials;
        int chests = _chests;
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
            LandFront front = _world.GetNode<ErasureManager>("ErasureManager").Front;
            Check(front != null && front.Time == 0f && Mathf.IsEqualApprox(front.StandingShare(), 1f),
                $"terre {land} : front neuf ({front?.CellCount ?? 0} cellules)");
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
