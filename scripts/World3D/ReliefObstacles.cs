using System.Collections.Generic;
using Godot;

namespace Vestiges.World3D;

/// <summary>
/// Emprises au sol des décors qui bloquent, en disques (cellules), rangées par cellule de la carte : un corps qui y entre
/// en est sorti en glissant le long de leur bord. Remplies à la pose des décors, puis figées par <see cref="Finish"/>.
/// </summary>
public sealed class ReliefObstacles
{
    private readonly int _width;
    private readonly int _depth;
    private readonly List<Vector3> _discs = new();
    private int[] _start;
    private int[] _list;

    public ReliefObstacles(int width, int depth)
    {
        _width = width;
        _depth = depth;
    }

    public int Count => _discs.Count;

    /// <summary>Ajoute l'emprise d'un décor (avant <see cref="Finish"/>).</summary>
    public void Add(Vector2 at, float radius) => _discs.Add(new Vector3(at.X, at.Y, radius));

    /// <summary>Range les emprises par cellule touchée, marge d'un corps comprise.</summary>
    public void Finish()
    {
        int cells = _width * _depth;
        List<int>[] perCell = new List<int>[cells];
        for (int o = 0; o < _discs.Count; o++)
        {
            Vector3 disc = _discs[o];
            float reach = disc.Z + 0.5f;
            for (int y = Mathf.FloorToInt(disc.Y - reach); y <= Mathf.FloorToInt(disc.Y + reach); y++)
            {
                for (int x = Mathf.FloorToInt(disc.X - reach); x <= Mathf.FloorToInt(disc.X + reach); x++)
                {
                    if (x < 0 || y < 0 || x >= _width || y >= _depth)
                        continue;
                    (perCell[y * _width + x] ??= new()).Add(o);
                }
            }
        }
        _start = new int[cells + 1];
        List<int> flat = new();
        for (int c = 0; c < cells; c++)
        {
            _start[c] = flat.Count;
            if (perCell[c] != null)
                flat.AddRange(perCell[c]);
        }
        _start[cells] = flat.Count;
        _list = flat.ToArray();
    }

    /// <summary>Sort un corps des emprises de sa cellule, en glissant le long de leur bord.</summary>
    public Vector2 PushOut(Vector2 at, float radius)
    {
        if (_start == null)
            return at;
        int cx = Mathf.FloorToInt(at.X);
        int cy = Mathf.FloorToInt(at.Y);
        if (cx < 0 || cy < 0 || cx >= _width || cy >= _depth)
            return at;
        int cell = cy * _width + cx;
        for (int k = _start[cell]; k < _start[cell + 1]; k++)
        {
            Vector3 disc = _discs[_list[k]];
            Vector2 d = new(at.X - disc.X, at.Y - disc.Y);
            float min = disc.Z + radius;
            float lenSq = d.LengthSquared();
            if (lenSq >= min * min)
                continue;
            float len = Mathf.Sqrt(lenSq);
            at = new Vector2(disc.X, disc.Y) + (len > 0.0001f ? d / len : new Vector2(1f, 0f)) * min;
        }
        return at;
    }
}
