using System.Collections.Generic;
using Godot;
using Vestiges.Base;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Génère le sol isométrique et place les nœuds de ressource au lancement.
/// Prototype — sera remplacé par la génération procédurale.
/// </summary>
public partial class WorldSetup : Node2D
{
    [Export] public int MapRadius = 30;
    [Export] public int ResourceCount = 40;

    private static readonly (string id, float weight)[] ResourceDistribution =
    {
        ("wood", 0.50f),
        ("stone", 0.35f),
        ("metal", 0.15f)
    };

    public override void _Ready()
    {
        EnemyDataLoader.Load();
        ResourceDataLoader.Load();

        TileMapLayer ground = GetNode<TileMapLayer>("Ground");
        GenerateFloor(ground);
        SpawnResources(ground);
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

    private void SpawnResources(TileMapLayer tileMap)
    {
        Node2D container = GetNode<Node2D>("../ResourceContainer");
        PackedScene resourceScene = GD.Load<PackedScene>("res://scenes/base/ResourceNode.tscn");
        HashSet<Vector2I> usedCells = new();

        for (int i = 0; i < ResourceCount; i++)
        {
            string resourceId = PickResourceType();
            ResourceData data = ResourceDataLoader.Get(resourceId);
            if (data == null)
                continue;

            Vector2I cell = PickResourceCell(usedCells);
            if (cell == new Vector2I(int.MinValue, int.MinValue))
                continue;

            usedCells.Add(cell);

            Vector2 worldPos = tileMap.MapToLocal(cell);

            ResourceNode node = resourceScene.Instantiate<ResourceNode>();
            node.GlobalPosition = worldPos;
            container.AddChild(node);
            node.Initialize(data);
        }

        GD.Print($"[WorldSetup] Spawned {usedCells.Count} resource nodes");
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

    private Vector2I PickResourceCell(HashSet<Vector2I> used)
    {
        int foyerExclusionRadius = 5;

        for (int attempt = 0; attempt < 30; attempt++)
        {
            int x = (int)GD.RandRange(-MapRadius + 1, MapRadius);
            int y = (int)GD.RandRange(-MapRadius + 1, MapRadius);
            Vector2I cell = new(x, y);

            if (used.Contains(cell))
                continue;

            if (Mathf.Abs(x) <= foyerExclusionRadius && Mathf.Abs(y) <= foyerExclusionRadius)
                continue;

            return cell;
        }

        return new Vector2I(int.MinValue, int.MinValue);
    }
}
