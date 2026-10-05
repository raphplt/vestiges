using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.World;

namespace Vestiges.Events;

/// <summary>
/// Accalmie après une Résurgence (V2 §8, plan 03 lot C) : pendant quelques dizaines de secondes, l'Essence rapporte
/// davantage, et un coffre rare est posé à portée, devant le joueur, jamais dans l'eau ni dans le Néant. Une nouvelle
/// crise referme l'accalmie. Réglages : clés calm_* de data/scaling/crises.json.
/// </summary>
public partial class CrisisAftermath : Node
{
    private const string ChestScenePath = "res://scenes/world/Chest.tscn";
    private const int ChestAttempts = 16;

    private EventBus _eventBus;
    private GroupCache _groups;
    private PackedScene _chestScene;
    private readonly RandomNumberGenerator _rng = new();
    private float _essenceMultiplier = 2f;
    private float _essenceSeconds = 30f;
    private string _chestId = "chest_rare";
    private Vector2 _chestDistance = new(220f, 340f);
    private float _calmRemaining;

    public override void _Ready()
    {
        LoadConfig();
        _rng.Seed = RunRandom.SeedFor("crisis_aftermath");
        _chestScene = GD.Load<PackedScene>(ChestScenePath);
        _groups = GetNode<GroupCache>("/root/GroupCache");
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.CrisisStarted += OnCrisisStarted;
        _eventBus.CrisisEnded += OnCrisisEnded;
        SetProcess(false);
    }

    public override void _ExitTree()
    {
        if (_eventBus == null)
            return;
        _eventBus.CrisisStarted -= OnCrisisStarted;
        _eventBus.CrisisEnded -= OnCrisisEnded;
    }

    public override void _Process(double delta)
    {
        _calmRemaining -= (float)delta;
        if (_calmRemaining <= 0f)
            EndCalm();
    }

    private void OnCrisisStarted(int crisisNumber, int intensity)
    {
        if (_calmRemaining > 0f)
            EndCalm();
    }

    private void OnCrisisEnded(int crisisNumber)
    {
        _calmRemaining = _essenceSeconds;
        SetProcess(true);
        _eventBus.EmitSignal(EventBus.SignalName.EssenceMultiplierChanged, _essenceMultiplier, _essenceSeconds);
        _eventBus.EmitSignal(EventBus.SignalName.CrisisCalmChanged, true);
        PlaceChest();
    }

    private void EndCalm()
    {
        _calmRemaining = 0f;
        SetProcess(false);
        _eventBus.EmitSignal(EventBus.SignalName.EssenceMultiplierChanged, 1f, 0f);
        _eventBus.EmitSignal(EventBus.SignalName.CrisisCalmChanged, false);
    }

    /// <summary>Devant le joueur (sa direction de marche, sinon au hasard), à distance de marche, sur un sol qui tient.</summary>
    private void PlaceChest()
    {
        if (_chestScene == null || _groups.GetPlayer() is not CharacterBody2D player)
            return;
        ChestData data = ChestDataLoader.Get(_chestId);
        if (data == null)
            return;
        Node root = GetParent();
        WorldSetup world = root as WorldSetup;
        ErasureManager erasure = root.GetNodeOrNull<ErasureManager>("ErasureManager");

        Vector2 forward = player.Velocity.LengthSquared() > 1f ? player.Velocity.Normalized() : Vector2.FromAngle(_rng.RandfRange(0f, Mathf.Tau));
        for (int attempt = 0; attempt < ChestAttempts; attempt++)
        {
            // S'écarte de plus en plus de la direction de marche si le sol devant ne convient pas.
            float spread = (attempt / 2 + 1) * 0.35f * (attempt % 2 == 0 ? 1f : -1f);
            Vector2 position = player.GlobalPosition + forward.Rotated(attempt == 0 ? 0f : spread)
                * _rng.RandfRange(_chestDistance.X, _chestDistance.Y);
            if (world != null && world.IsWaterAt(position))
                continue;
            if (erasure != null && erasure.GetZonePhaseAt(position) == ErasureManager.ErasureZonePhase.Void)
                continue;
            Chest chest = _chestScene.Instantiate<Chest>();
            chest.GlobalPosition = position;
            root.AddChild(chest);
            chest.Initialize(data);
            GD.Print($"[CrisisAftermath] Coffre {_chestId} posé à {position.DistanceTo(player.GlobalPosition):0} px");
            return;
        }
        GD.PushWarning("[CrisisAftermath] Aucun sol praticable devant le joueur : pas de coffre d'accalmie");
    }

    private void LoadConfig()
    {
        using FileAccess file = FileAccess.Open("res://data/scaling/crises.json", FileAccess.ModeFlags.Read);
        if (file == null)
            return;
        Json json = new();
        if (json.Parse(file.GetAsText()) != Error.Ok)
            return;
        Godot.Collections.Dictionary dict = json.Data.AsGodotDictionary();
        _essenceMultiplier = (float)dict.GetValueOrDefault("calm_essence_multiplier", _essenceMultiplier).AsDouble();
        _essenceSeconds = (float)dict.GetValueOrDefault("calm_essence_seconds", _essenceSeconds).AsDouble();
        _chestId = dict.GetValueOrDefault("calm_chest", _chestId).AsString();
        if (dict.ContainsKey("calm_chest_distance"))
        {
            Godot.Collections.Array range = dict["calm_chest_distance"].AsGodotArray();
            _chestDistance = new Vector2((float)range[0].AsDouble(), (float)range[1].AsDouble());
        }
    }
}
