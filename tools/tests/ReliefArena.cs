using Godot;

namespace Vestiges.Tests;

/// <summary>
/// Carte de hauteurs de l'arène du bac à sable de la verticalité (plan 59) : une hauteur par cellule, en unités du monde,
/// ou le vide. Le sol (x, y) est un plan carré : la cellule (cx, cy) couvre [cx, cx+1[ × [cy, cy+1[.
/// Les cases de montée (§239) sont des rampes : leur sol monte en pente douce d'une cellule voisine à l'autre. Une rampe
/// de plusieurs cases se lit comme une seule pente, de la case pleine d'où elle part à celle où elle arrive.
/// </summary>
public sealed class ReliefArena
{
    /// <summary>Hauteur rendue aux cellules de vide et hors de l'arène : tout ce qui y tombe y reste.</summary>
    public const float VoidHeight = -1000f;

    // Sens de la montée de chaque caractère de rampe : '>' vers l'est (+x), '<' vers l'ouest, 'v' vers le sud (+y), '^' vers le nord.
    private const string RampChars = "><v^";
    private static readonly Vector2I[] RampAscent = { new(1, 0), new(-1, 0), new(0, 1), new(0, -1) };

    private readonly float[] _heights;
    // Pour une rampe : sens de montée (1 à 4, 0 hors rampe), hauteur à son bord bas, montée sur la cellule.
    private readonly byte[] _ramp;
    private readonly float[] _rampLow;
    private readonly float[] _rampRise;

    public int Width { get; }
    public int Depth { get; }
    public float LevelHeight { get; }

    public ReliefArena(string[] rows, float levelHeight)
    {
        LevelHeight = levelHeight;
        Depth = rows.Length;
        Width = rows[0].Length;
        _heights = new float[Width * Depth];
        _ramp = new byte[Width * Depth];
        _rampLow = new float[Width * Depth];
        _rampRise = new float[Width * Depth];
        for (int y = 0; y < Depth; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                char c = rows[y][x];
                int ramp = RampChars.IndexOf(c);
                _ramp[y * Width + x] = (byte)(ramp + 1);
                _heights[y * Width + x] = c == '.' || ramp >= 0 ? VoidHeight : Quarters(c) * levelHeight / 4f;
            }
        }
        for (int y = 0; y < Depth; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (_ramp[y * Width + x] != 0)
                    SetRamp(x, y);
            }
        }
    }

    /// <summary>Hauteur en quarts d'étage : '0' à '9', puis 'a' = 10 jusqu'à 'z' = 35.</summary>
    private static int Quarters(char c) => c >= 'a' ? 10 + c - 'a' : c - '0';

    /// <summary>
    /// Pente d'une case de rampe : la chaîne des cases de même sens qui la contient va de la case pleine qui la précède à
    /// celle qui la suit ; la case en prend sa part.
    /// </summary>
    private void SetRamp(int x, int y)
    {
        int index = y * Width + x;
        byte kind = _ramp[index];
        Vector2I d = RampAscent[kind - 1];
        int before = 0, after = 0;
        while (RampKind(x - d.X * (before + 1), y - d.Y * (before + 1)) == kind)
            before++;
        while (RampKind(x + d.X * (after + 1), y + d.Y * (after + 1)) == kind)
            after++;
        float low = CellHeight(x - d.X * (before + 1), y - d.Y * (before + 1));
        float high = CellHeight(x + d.X * (after + 1), y + d.Y * (after + 1));
        if (IsVoid(low) || IsVoid(high) || RampKind(x - d.X * (before + 1), y - d.Y * (before + 1)) != 0
            || RampKind(x + d.X * (after + 1), y + d.Y * (after + 1)) != 0)
            throw new System.InvalidOperationException($"Rampe ({x},{y}) : elle doit partir d'une case pleine et arriver sur une autre.");
        int length = before + after + 1;
        _rampRise[index] = (high - low) / length;
        _rampLow[index] = low + _rampRise[index] * before;
        _heights[index] = _rampLow[index] + _rampRise[index] * 0.5f;
    }

    private byte RampKind(int x, int y) => x < 0 || y < 0 || x >= Width || y >= Depth ? (byte)0 : _ramp[y * Width + x];

    /// <summary>Hauteur du sol de la cellule (au milieu d'une rampe), <see cref="VoidHeight"/> pour le vide ou hors de l'arène.</summary>
    public float CellHeight(int x, int y) =>
        x < 0 || y < 0 || x >= Width || y >= Depth ? VoidHeight : _heights[y * Width + x];

    public bool IsRamp(int x, int y) => RampKind(x, y) != 0;

    /// <summary>
    /// Sol du plan de la cellule (cx, cy) au point (x, y), pris sur son bord s'il en sort : la hauteur d'une arête vue de
    /// chacune des deux cellules qui la partagent.
    /// </summary>
    public float CellSurface(int cx, int cy, float x, float y)
    {
        byte kind = RampKind(cx, cy);
        if (kind == 0)
            return CellHeight(cx, cy);
        Vector2I d = RampAscent[kind - 1];
        float fx = Mathf.Clamp(x - cx, 0f, 1f), fy = Mathf.Clamp(y - cy, 0f, 1f);
        float t = d.X == 1 ? fx : d.X == -1 ? 1f - fx : d.Y == 1 ? fy : 1f - fy;
        int index = cy * Width + cx;
        return _rampLow[index] + _rampRise[index] * t;
    }

    public float GroundAt(float x, float y) => CellSurface(Mathf.FloorToInt(x), Mathf.FloorToInt(y), x, y);

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
