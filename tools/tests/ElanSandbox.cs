using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Spawn;
using Vestiges.UI;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// Bac à sable de l'Élan (plan 52), jouable à la main : la vraie scène Main sans apparitions ni événements, l'Effacement
/// figé, une foule entretenue autour du joueur, des bandes de vide de 1, 2 et 3 cellules à l'est du départ (saut du
/// vide, E3), l'Élan allumé et un affichage de réglage. Un coup fatal remet les PV au maximum : on y essaie les gestes
/// sans fin de partie. La Série (combo d'éliminations) s'affiche au-dessus de la tête et dans l'affichage de réglage.
/// Lancement : tools/elan_sandbox.sh.
/// </summary>
public partial class ElanSandbox : Node
{
    private const int Seed = 52;
    private const float SpawnRingMin = 420f;
    private const float SpawnRingMax = 560f;
    private const float RefillInterval = 0.5f;
    private const float RespawnDistance = 1400f;
    // Bandes de vide (E3) : largeurs en cellules, de l'ouest à l'est, séparées par du sol ; hauteur en cellules.
    private static readonly int[] StripWidths = { 1, 2, 3 };
    private const int StripGapCells = 3;
    private const int StripHalfHeightCells = 2;
    // Distance entre le joueur et le premier bord de vide, au moins.
    private const float FirstStripDistance = 96f;

    private readonly List<Enemy> _crowd = new();
    private WorldSetup _world;
    private ErasureManager _erasure;
    private Player _player;
    private EnemyPool _pool;
    private Node _container;
    private Label _label;
    private int _crowdSize;
    private float _refillClock;
    private int _spawned;
    private bool _ready;
    private string _captureDir;
    private float _captureSeconds;
    private float _elapsed;
    private int _dodgesBeforeCapture;
    private string _scenario;
    private int _leapFramesInAir;
    private float _firstStripEdge;
    private int _falls;
    private bool _columnSpawned;
    private float _columnSpawnedAt;
    private int _framesSinceFall = -1;
    private EventBus _eventBus;
    private GameLoadingOverlay _overlay;

    public override async void _Ready()
    {
        try
        {
            ProcessMode = ProcessModeEnum.Always;
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            string[] args = OS.GetCmdlineUserArgs();
            _crowdSize = int.Parse(Argument(args, "--enemies", "40"), CultureInfo.InvariantCulture);
            // --capture <dossier> [--seconds S] : le joueur court seul en cercle, une image est gardée puis le banc se ferme.
            _captureDir = Argument(args, "--capture", null);
            _captureSeconds = float.Parse(Argument(args, "--seconds", "12"), CultureInfo.InvariantCulture);
            // --scenario leap : le joueur, au rang FURIE, court vers la première bande de vide et la saute ; l'image est
            // gardée en l'air. --scenario percussion : il longe le bord de la première bande et dashe dans une colonne de
            // créatures ; l'image est gardée pendant leur chute. Sans scénario : il fonce sur la foule et dashe au contact
            // (esquive parfaite).
            _scenario = Argument(args, "--scenario", "dodge");
            GameManager manager = GetNode<GameManager>("/root/GameManager");
            manager.RunSeed = Seed;
            manager.SelectedCharacterId = Argument(args, "--character", "traqueur");
            GetTree().CurrentScene = null;
            _world = GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<WorldSetup>();
            GetTree().Root.AddChild(_world);
            GetTree().CurrentScene = _world;
            while (!_world.IsWorldReady || GetTree().Paused || manager.CurrentState != GameManager.GameState.Run)
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            foreach (string director in new[] { "SpawnManager", "CrisisManager", "RunEventDirector", "EndgameManager",
                "BarrierDirector" })
            {
                Node node = _world.GetNodeOrNull(director);
                if (node != null)
                    node.ProcessMode = ProcessModeEnum.Disabled;
            }
            // Sur une terre, l'Effacement reste actif mais figé : rien ne tombe, le Néant draine toujours et le sol suit
            // le joueur à l'écran. Sur la grande carte (--no-lands), il est coupé comme avant.
            _erasure = _world.GetNode<ErasureManager>("ErasureManager");
            if (_erasure.Front != null)
                _erasure.FrontFrozen = true;
            else
                _erasure.ProcessMode = ProcessModeEnum.Disabled;
            foreach (Node node in GetTree().GetNodesInGroup("enemies"))
                node.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            _player = _world.GetNode<Player>("Player");
            _player.SurviveFatalHitsForTests = true;
            _player.SetElanEnabled(true);
            // --percussion-tier N : rang à partir duquel le dash projette (0 par défaut).
            string percussionTier = Argument(args, "--percussion-tier", null);
            if (percussionTier != null)
                _player.PercussionMinRank = int.Parse(percussionTier, CultureInfo.InvariantCulture);
            _eventBus = GetNode<EventBus>("/root/EventBus");
            _eventBus.CreatureFellIntoVoid += OnCreatureFell;
            if (_captureDir != null)
                _player.IsAIControlled = true;
            _player.GlobalPosition = TestArena.FindOpen(_player, _world);
            CarveVoidStrips();
            _erasure.SettleStitchForTests();
            _pool = _world.GetNode<EnemyPool>("EnemyPool");
            _container = _world.GetNode("EnemyContainer");
            CanvasLayer layer = new() { Layer = 100 };
            AddChild(layer);
            _label = new Label { Position = new Vector2(16f, 120f) };
            _label.AddThemeFontSizeOverride("font_size", 18);
            _label.AddThemeColorOverride("font_shadow_color", Colors.Black);
            _label.AddThemeConstantOverride("shadow_offset_x", 2);
            _label.AddThemeConstantOverride("shadow_offset_y", 2);
            layer.AddChild(_label);
            _overlay = FindOverlay(GetTree().Root);
            _ready = true;
            GD.Print($"[ElanSandbox] Prêt : {_crowdSize} créatures entretenues, personnage {manager.SelectedCharacterId}.");
        }
        catch (Exception exception)
        {
            GD.PushError($"[ElanSandbox] {exception}");
            GetTree().Quit(1);
        }
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
            _eventBus.CreatureFellIntoVoid -= OnCreatureFell;
    }

    private void OnCreatureFell(Vector2 edge) => _falls++;

    public override void _Process(double delta)
    {
        if (!_ready || !IsInstanceValid(_player))
            return;
        // Le temps des captures ne part qu'une fois le voile de chargement retiré (il dure plus longtemps sous charge).
        if (_overlay == null || !IsInstanceValid(_overlay) || _overlay.IsQueuedForDeletion())
            _elapsed += (float)delta;
        if (_captureDir != null)
            StepCapture();
        _refillClock -= (float)delta;
        if (_refillClock <= 0f)
        {
            _refillClock = RefillInterval;
            RefillCrowd();
        }
        PlayerStreak streak = _player.Streak;
        _label.Text = string.Format(CultureInfo.InvariantCulture,
            "SÉRIE ×{0}  rang {1}  ({2})  fenêtre {12:0.0} s  meilleure ×{13}\n"
            + "vitesse ×{3:0.00}  dégâts ×{4:0.00}  Chance +{14:0}%\nesquives parfaites {7}  sauts du vide {8}\n"
            + "percussion dès le rang {9} : {10} créatures projetées, {11} tombées\n"
            + "créatures {5}  apparues {6}\nvide à l'est : bandes de 1, 2 et 3 cellules",
            streak.Count, streak.Rank, streak.LastChange, streak.SpeedAt(streak.Rank), streak.DamageAt(streak.Rank), _crowd.Count,
            _spawned, _player.PerfectDodges, _player.VoidLeaps, _player.PercussionMinRank, _player.Percussions, _falls,
            streak.WindowRemaining, streak.Best, streak.LuckAt(streak.Rank) * 100f);
    }

    /// <summary>
    /// Bandes de Néant à l'est du départ, de la largeur de <see cref="StripWidths"/> : la mémoire de leurs cellules est
    /// mise à zéro, l'Effacement figé ne les relève pas.
    /// </summary>
    private void CarveVoidStrips()
    {
        int cell = _erasure.CellSize;
        Vector2 start = _player.GlobalPosition;
        int x = Mathf.CeilToInt((start.X + FirstStripDistance) / cell);
        int row = Mathf.FloorToInt(start.Y / cell);
        _firstStripEdge = x * cell;
        foreach (int width in StripWidths)
        {
            for (int dx = 0; dx < width; dx++)
            {
                for (int dy = -StripHalfHeightCells; dy <= StripHalfHeightCells; dy++)
                    _erasure.OverrideMemory(new Vector2I(x + dx, row + dy), 0f);
            }
            x += width + StripGapCells;
        }
    }

    private bool IsVoidAt(Vector2 position) => _erasure.GetZonePhaseAt(position) == ErasureManager.ErasureZonePhase.Void;

    private void StepCapture()
    {
        PickLevelUp();
        if (_scenario == "leap")
            StepLeapCapture();
        else if (_scenario == "percussion")
            StepPercussionCapture();
        else
            StepDodgeCapture();
    }

    /// <summary>
    /// Percussion (E4) : après le délai, le joueur, au rang FURIE, est posé au nord d'une colonne de six Ombres alignée sur
    /// le bord ouest de la première bande ; il descend le long du bord et dashe dans la colonne. Une créature sur deux
    /// est poussée vers le vide. L'image est gardée cinq pas après la première chute (milieu de la chute), ou six secondes après la colonne.
    /// </summary>
    private void StepPercussionCapture()
    {
        if (_elapsed < _captureSeconds)
        {
            _player.AIInputOverride = Vector2.Zero;
            return;
        }
        if (_player.Streak.Rank < 3)
            _player.Streak.Kill(_player.Streak.Config.Ranks[2].From, "bac à sable");
        if (!_columnSpawned)
            SpawnEdgeColumn();
        _player.AIInputOverride = Vector2.Down;
        Enemy nearest = NearestCrowd(out float distance);
        if (nearest != null && distance < 40f && _player.Mobility.CooldownRemaining <= 0f)
            _player.Mobility.Request();
        if (_falls > 0)
            _framesSinceFall++;
        if (_framesSinceFall < 5 && _elapsed < _columnSpawnedAt + 6f)
            return;
        SaveCapture("percussion");
    }

    private void SpawnEdgeColumn()
    {
        _columnSpawned = true;
        _columnSpawnedAt = _elapsed;
        float x = _firstStripEdge - 12f;
        float top = _player.GlobalPosition.Y - 60f;
        _player.GlobalPosition = new Vector2(x, top - 100f);
        for (int i = 0; i < 6; i++)
        {
            Enemy enemy = _pool.Get();
            enemy.Position = new Vector2(x, top + i * 24f);
            _container.AddChild(enemy);
            enemy.Initialize(EnemyDataLoader.Get("shade"), 1f, 1f);
            _crowd.Add(enemy);
            _spawned++;
        }
    }

    /// <summary>
    /// Saut du vide (E3) : rang FURIE donné d'office, course vers l'est, dash juste avant le bord de la première bande ;
    /// l'image est gardée au huitième pas en l'air (milieu d'une bande d'une cellule) au-dessus du vide, ou au bout du délai.
    /// </summary>
    private void StepLeapCapture()
    {
        // Le voile de chargement s'efface pendant le délai : le joueur attend sur place.
        if (_elapsed < _captureSeconds)
        {
            _player.AIInputOverride = Vector2.Zero;
            return;
        }
        if (_player.Streak.Rank < 3)
            _player.Streak.Kill(_player.Streak.Config.Ranks[2].From, "bac à sable");
        _player.AIInputOverride = Vector2.Right;
        if (!_player.Mobility.IsDashing && _player.Mobility.CooldownRemaining <= 0f
            && IsVoidAt(_player.GlobalPosition + new Vector2(14f, 0f)) && !IsVoidAt(_player.GlobalPosition))
            _player.Mobility.Request();
        _leapFramesInAir = _player.IsLeaping && IsVoidAt(_player.GlobalPosition) ? _leapFramesInAir + 1 : 0;
        if (_leapFramesInAir < 8 && _elapsed < _captureSeconds + 10f)
            return;
        SaveCapture("leap");
    }

    /// <summary>
    /// Fonce vers la créature la plus proche et dashe au contact ; passé le délai, l'image est gardée juste après une esquive
    /// parfaite (onde visible), ou au bout de dix secondes de plus.
    /// </summary>
    private void StepDodgeCapture()
    {
        Enemy nearest = NearestCrowd(out float distance);
        _player.AIInputOverride = nearest != null
            ? (nearest.GlobalPosition - _player.GlobalPosition).Normalized()
            : Vector2.FromAngle(_elapsed * 0.35f).Orthogonal();
        if (nearest != null && distance < 70f && _player.Mobility.CooldownRemaining <= 0f)
            _player.Mobility.Request();
        if (_elapsed < _captureSeconds)
        {
            _dodgesBeforeCapture = _player.PerfectDodges;
            return;
        }
        if (_player.PerfectDodges == _dodgesBeforeCapture && _elapsed < _captureSeconds + 10f)
            return;
        SaveCapture($"elan_{_captureSeconds:0}s");
    }

    private void SaveCapture(string name)
    {
        DirAccess.MakeDirRecursiveAbsolute(_captureDir);
        string path = $"{_captureDir}/{name}.png";
        GetViewport().GetTexture().GetImage().SavePng(path);
        GD.Print($"[ElanSandbox] Capture : {path} (sauts du vide {_player.VoidLeaps}, rang {_player.Streak.Rank}, "
            + $"percussions {_player.Percussions}, chutes {_falls})");
        GetTree().Quit(0);
        _captureDir = null;
    }

    /// <summary>En capture, une montée de niveau prend la première carte aussitôt : la scène ne reste pas en pause.</summary>
    private void PickLevelUp()
    {
        if (!GetTree().Paused)
            return;
        Node screen = _world.GetNode("LevelUpScreen");
        if (screen.GetType().GetField("_fragmentManager", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(screen)
                is not Vestiges.Progression.FragmentManager fragments || !fragments.IsChoiceActive || fragments.PendingChoices.Count == 0)
            return;
        screen.GetType().GetMethod("OnCardChosen", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(screen, new object[] { fragments.PendingChoices[0] });
    }

    private Enemy NearestCrowd(out float distance)
    {
        Enemy nearest = null;
        distance = float.MaxValue;
        foreach (Enemy enemy in _crowd)
        {
            if (!IsInstanceValid(enemy) || !enemy.IsActive)
                continue;
            float candidate = enemy.GlobalPosition.DistanceTo(_player.GlobalPosition);
            if (candidate < distance)
            {
                distance = candidate;
                nearest = enemy;
            }
        }
        return nearest;
    }

    /// <summary>Retire les créatures mortes ou semées, puis complète la foule sur un anneau autour du joueur.</summary>
    private void RefillCrowd()
    {
        Vector2 center = _player.GlobalPosition;
        for (int i = _crowd.Count - 1; i >= 0; i--)
        {
            Enemy enemy = _crowd[i];
            if (!IsInstanceValid(enemy) || !enemy.IsActive || enemy.IsQueuedForDeletion())
                _crowd.RemoveAt(i);
            else if (enemy.GlobalPosition.DistanceSquaredTo(center) > RespawnDistance * RespawnDistance)
            {
                enemy.Vanish();
                _crowd.RemoveAt(i);
            }
        }
        while (_crowd.Count < _crowdSize)
        {
            Enemy enemy = _pool.Get();
            float angle = _spawned * 2.3999632f;
            float radius = Mathf.Lerp(SpawnRingMin, SpawnRingMax, (_spawned * 0.618034f) % 1f);
            enemy.Position = center + Vector2.FromAngle(angle) * radius;
            _container.AddChild(enemy);
            // Cinq Ombres pour un Cracheur : la proportion du banc dense, au contact et à distance.
            enemy.Initialize(EnemyDataLoader.Get(_spawned % 6 == 5 ? "fading_spitter" : "shade"), 1f, 1f);
            _crowd.Add(enemy);
            _spawned++;
        }
    }

    private static GameLoadingOverlay FindOverlay(Node node)
    {
        if (node is GameLoadingOverlay overlay)
            return overlay;
        foreach (Node child in node.GetChildren())
        {
            GameLoadingOverlay found = FindOverlay(child);
            if (found != null)
                return found;
        }
        return null;
    }

    private static string Argument(string[] args, string name, string fallback)
    {
        int index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : fallback;
    }
}
