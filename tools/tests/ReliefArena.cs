using Godot;

namespace Vestiges.Tests;

/// <summary>
/// Carte de hauteurs de l'arène du bac à sable de la verticalité (plan 59) : une hauteur par cellule, en unités du monde,
/// ou le vide. Le sol (x, y) est un plan carré : la cellule (cx, cy) couvre [cx, cx+1[ × [cy, cy+1[.
/// </summary>
public sealed class ReliefArena
{
    /// <summary>Hauteur rendue aux cellules de vide et hors de l'arène : tout ce qui y tombe y reste.</summary>
    public const float VoidHeight = -1000f;

    private readonly float[] _heights;

    public int Width { get; }
    public int Depth { get; }
    public float LevelHeight { get; }

    public ReliefArena(string[] rows, float levelHeight)
    {
        LevelHeight = levelHeight;
        Depth = rows.Length;
        Width = rows[0].Length;
        _heights = new float[Width * Depth];
        for (int y = 0; y < Depth; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                char c = rows[y][x];
                _heights[y * Width + x] = c == '.' ? VoidHeight : Quarters(c) * levelHeight / 4f;
            }
        }
    }

    /// <summary>Hauteur en quarts d'étage : '0' à '9', puis 'a' = 10 jusqu'à 'z' = 35.</summary>
    private static int Quarters(char c) => c >= 'a' ? 10 + c - 'a' : c - '0';

    /// <summary>Hauteur du sol de la cellule, <see cref="VoidHeight"/> pour le vide ou hors de l'arène.</summary>
    public float CellHeight(int x, int y) =>
        x < 0 || y < 0 || x >= Width || y >= Depth ? VoidHeight : _heights[y * Width + x];

    public float GroundAt(float x, float y) => CellHeight(Mathf.FloorToInt(x), Mathf.FloorToInt(y));

    public static bool IsVoid(float height) => height <= VoidHeight + 1f;

    /// <summary>Plus haut sol sous un disque (quatre points de son bord et son centre), pour buter contre une falaise.</summary>
    public float HighestUnder(float x, float y, float radius)
    {
        float r = radius * 0.7f;
        float h = GroundAt(x, y);
        h = Mathf.Max(h, GroundAt(x + r, y + r));
        h = Mathf.Max(h, GroundAt(x - r, y + r));
        h = Mathf.Max(h, GroundAt(x + r, y - r));
        return Mathf.Max(h, GroundAt(x - r, y - r));
    }
}
