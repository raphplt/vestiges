using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Spawn;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// Bac à sable de l'Élan (plan 52), jouable à la main : la vraie scène Main sans apparitions, Effacement ni événements,
/// une foule entretenue autour du joueur, l'Élan allumé et un affichage de réglage. Un coup fatal remet les PV au
/// maximum : on y essaie les gestes sans fin de partie. Lancement : tools/elan_sandbox.sh.
/// </summary>
public partial class ElanSandbox : Node
{
    private const int Seed = 52;
    private const float SpawnRingMin = 420f;
    private const float SpawnRingMax = 560f;
    private const float RefillInterval = 0.5f;
    private const float RespawnDistance = 1400f;

    private readonly List<Enemy> _crowd = new();
    private WorldSetup _world;
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
            GameManager manager = GetNode<GameManager>("/root/GameManager");
            manager.RunSeed = Seed;
            manager.SelectedCharacterId = Argument(args, "--character", "traqueur");
            GetTree().CurrentScene = null;
            _world = GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<WorldSetup>();
            GetTree().Root.AddChild(_world);
            GetTree().CurrentScene = _world;
            while (!_world.IsWorldReady || GetTree().Paused || manager.CurrentState != GameManager.GameState.Run)
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            foreach (string director in new[] { "SpawnManager", "ErasureManager", "CrisisManager", "RunEventDirector",
                "EndgameManager", "BarrierDirector" })
            {
                Node node = _world.GetNodeOrNull(director);
                if (node != null)
                    node.ProcessMode = ProcessModeEnum.Disabled;
            }
            foreach (Node node in GetTree().GetNodesInGroup("enemies"))
                node.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            _player = _world.GetNode<Player>("Player");
            _player.SurviveFatalHitsForTests = true;
            _player.SetElanEnabled(true);
            if (_captureDir != null)
                _player.IsAIControlled = true;
            _player.GlobalPosition = TestArena.FindOpen(_player);
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
            _ready = true;
            GD.Print($"[ElanSandbox] Prêt : {_crowdSize} créatures entretenues, personnage {manager.SelectedCharacterId}.");
        }
        catch (Exception exception)
        {
            GD.PushError($"[ElanSandbox] {exception}");
            GetTree().Quit(1);
        }
    }

    public override void _Process(double delta)
    {
        if (!_ready || !IsInstanceValid(_player))
            return;
        _elapsed += (float)delta;
        if (_captureDir != null)
            StepCapture();
        _refillClock -= (float)delta;
        if (_refillClock <= 0f)
        {
            _refillClock = RefillInterval;
            RefillCrowd();
        }
        PlayerElan elan = _player.Elan;
        _label.Text = string.Format(CultureInfo.InvariantCulture,
            "ÉLAN {0:0.00}  palier {1}  ({2})\nvitesse ×{3:0.00}  dégâts ×{4:0.00}\nesquives parfaites {7}\ncréatures {5}  apparues {6}",
            elan.Value, elan.Tier, elan.LastChange, elan.SpeedAt(elan.Tier), elan.DamageAt(elan.Tier), _crowd.Count, _spawned,
            _player.PerfectDodges);
    }

    /// <summary>
    /// Fonce vers la créature la plus proche et dashe au contact ; passé le délai, l'image est gardée juste après une esquive
    /// parfaite (onde visible), ou au bout de dix secondes de plus.
    /// </summary>
    private void StepCapture()
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
        DirAccess.MakeDirRecursiveAbsolute(_captureDir);
        string path = $"{_captureDir}/elan_{_captureSeconds:0}s.png";
        GetViewport().GetTexture().GetImage().SavePng(path);
        GD.Print($"[ElanSandbox] Capture : {path}");
        GetTree().Quit(0);
        _captureDir = null;
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

    private static string Argument(string[] args, string name, string fallback)
    {
        int index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : fallback;
    }
}
