using System.Collections.Generic;
using Godot;
using Vestiges.Base;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Génère le sol isométrique et place les nœuds de ressource au lancement.
/// Gère le respawn périodique des ressources épuisées.
/// Prototype — sera remplacé par la génération procédurale.
/// </summary>
public partial class WorldSetup : Node2D
{
    [Export] public int MapRadius = 30;
    [Export] public int ResourceCount = 40;
    [Export] public float RespawnIntervalSeconds = 30f;
    [Export] public int RespawnThreshold = 30;

    private static readonly (string id, float weight)[] ResourceDistribution =
    {
        ("wood", 0.50f),
        ("stone", 0.35f),
        ("metal", 0.15f)
    };

    private TileMapLayer _ground;
    private Node2D _resourceContainer;
    private PackedScene _resourceScene;
    private HashSet<Vector2I> _usedCells = new();
    private Timer _respawnTimer;

    public override void _Ready()
    {
        EnemyDataLoader.Load();
        ResourceDataLoader.Load();

        _ground = GetNode<TileMapLayer>("Ground");
        _resourceContainer = GetNode<Node2D>("ResourceContainer");
        _resourceScene = GD.Load<PackedScene>("res://scenes/base/ResourceNode.tscn");

        GenerateFloor(_ground);
        SpawnResources();

        _respawnTimer = new Timer();
        _respawnTimer.WaitTime = RespawnIntervalSeconds;
        _respawnTimer.Autostart = true;
        _respawnTimer.Timeout += OnRespawnTimer;
        AddChild(_respawnTimer);
    }

    private void GenerateFloor(TileMapLayer tileMap)
    {
        for (int x = -MapRadius; x <= MapRadius; x++)
        {
            for (int y = -MapRadius; y <= MapRadius; y++)
            {
                tileMap.SetCell(new Vector2I(x, y), 0, Vector2I.Zero);
            }
        }
    }

    private void SpawnResources()
    {
        for (int i = 0; i < ResourceCount; i++)
        {
            SpawnSingleResource();
        }

        GD.Print($"[WorldSetup] Spawned {_usedCells.Count} resource nodes");
    }

    private void SpawnSingleResource(float minDistFromPlayer = 0f)
    {
        string resourceId = PickResourceType();
        ResourceData data = ResourceDataLoader.Get(resourceId);
        if (data == null)
            return;

        Vector2I cell = PickResourceCell(_usedCells, minDistFromPlayer);
        if (cell == new Vector2I(int.MinValue, int.MinValue))
            return;

        _usedCells.Add(cell);

        Vector2 worldPos = _ground.MapToLocal(cell);

        ResourceNode node = _resourceScene.Instantiate<ResourceNode>();
        node.GlobalPosition = worldPos;
        _resourceContainer.AddChild(node);
        node.Initialize(data);
    }

    private void OnRespawnTimer()
    {
        RefreshUsedCells();

        int activeCount = _resourceContainer.GetChildCount();
        if (activeCount >= RespawnThreshold)
            return;

        int toSpawn = Mathf.Min(ResourceCount - activeCount, 5);
        for (int i = 0; i < toSpawn; i++)
        {
            SpawnSingleResource(300f);
        }

        if (toSpawn > 0)
            GD.Print($"[WorldSetup] Respawned {toSpawn} resources (active: {activeCount + toSpawn})");
    }

    private void RefreshUsedCells()
    {
        _usedCells.Clear();
        foreach (Node child in _resourceContainer.GetChildren())
        {
            if (child is ResourceNode res && !res.IsExhausted)
            {
                Vector2I cell = _ground.LocalToMap(_ground.ToLocal(res.GlobalPosition));
                _usedCells.Add(cell);
            }
        }
    }

    private string PickResourceType()
    {
        float roll = (float)GD.RandRange(0, 1);
        float cumulative = 0f;
        foreach ((string id, float weight) in ResourceDistribution)
        {
            cumulative += weight;
            if (roll <= cumulative)
                return id;
        }
        return ResourceDistribution[0].id;
    }

    private Vector2I PickResourceCell(HashSet<Vector2I> used, float minDistFromPlayer = 0f)
    {
        int foyerExclusionRadius = 5;

        Node playerNode = GetTree().GetFirstNodeInGroup("player");
        Vector2 playerPos = playerNode is Node2D player ? player.GlobalPosition : Vector2.Zero;

        for (int attempt = 0; attempt < 30; attempt++)
        {
            int x = (int)GD.RandRange(-MapRadius + 1, MapRadius);
            int y = (int)GD.RandRange(-MapRadius + 1, MapRadius);
            Vector2I cell = new(x, y);

            if (used.Contains(cell))
                continue;

            if (Mathf.Abs(x) <= foyerExclusionRadius && Mathf.Abs(y) <= foyerExclusionRadius)
                continue;

            if (minDistFromPlayer > 0f)
            {
                Vector2 worldPos = _ground.MapToLocal(cell);
                if (worldPos.DistanceTo(playerPos) < minDistFromPlayer)
                    continue;
            }

            return cell;
        }

        return new Vector2I(int.MinValue, int.MinValue);
    }
}
