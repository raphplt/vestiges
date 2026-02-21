using Godot;

namespace Vestiges.World;

/// <summary>
/// Génère le sol isométrique et les ennemis de test au lancement.
/// Prototype — sera remplacé par la génération procédurale et le SpawnManager.
/// </summary>
public partial class WorldSetup : Node2D
{
    [Export] public int MapRadius = 25;
    [Export] public int TestEnemyCount = 8;

    private PackedScene _enemyScene;

    public override void _Ready()
    {
        _enemyScene = GD.Load<PackedScene>("res://scenes/enemies/Enemy.tscn");

        TileMapLayer ground = GetNode<TileMapLayer>("Ground");
        GenerateFloor(ground);
        SpawnTestEnemies();
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

    private void SpawnTestEnemies()
    {
        TileMapLayer ground = GetNode<TileMapLayer>("Ground");

        // Positions en coordonnées de tuile — on les ajoute comme enfants
        // du TileMapLayer pour garantir l'alignement parfait sur la grille
        Vector2I[] tilePositions =
        {
            new(5, 3), new(-4, 2), new(3, -4),
            new(-5, -1), new(1, 6), new(7, -2),
            new(-3, 5), new(0, -6)
        };

        int count = Mathf.Min(TestEnemyCount, tilePositions.Length);
        for (int i = 0; i < count; i++)
        {
            Node2D enemy = _enemyScene.Instantiate<Node2D>();
            enemy.Position = ground.MapToLocal(tilePositions[i]);
            ground.AddChild(enemy);
        }
    }
}
