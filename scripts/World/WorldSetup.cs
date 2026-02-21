using Godot;

namespace Vestiges.World;

/// <summary>
/// Génère le sol isométrique au lancement de la scène.
/// Prototype — sera remplacé par la génération procédurale.
/// </summary>
public partial class WorldSetup : Node2D
{
    [Export] public int MapRadius = 25;

    public override void _Ready()
    {
        TileMapLayer ground = GetNode<TileMapLayer>("Ground");
        GenerateFloor(ground);
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
}
