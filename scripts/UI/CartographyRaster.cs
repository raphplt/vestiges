using System;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.UI;

/// <summary>
/// Un fond fin par cellule connue ; le terrain est échantillonné une seule fois, l'Effacement recolore son cache.
/// Les deux buffers RGBA sont réutilisés. Aucun parcours du monde entier à chaque mise à jour.
/// </summary>
public sealed class CartographyRaster : IDisposable
{
    public const int Detail = 4;
    private const byte Unknown = 255;
    private readonly int _width;
    private readonly int _height;
    private readonly int _cellSize;
    private readonly Vector2I _origin;
    private readonly byte[] _base;
    private readonly byte[] _pixels;
    private readonly byte[] _phases;
    private readonly Func<Vector2, Color> _sample;
    private readonly MapPalette _palette;
    private static readonly Vector2I[] Sides = { Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right };
    private bool _dirty;
    public Image Image { get; }
    public int BufferBytes => _base.Length + _pixels.Length + _phases.Length;

    public CartographyRaster(int width, int height, int cellSize, Vector2I origin, Func<Vector2, Color> sample)
    {
        _width = width;
        _height = height;
        _cellSize = cellSize;
        _origin = origin;
        _sample = sample;
        _palette = MapPalette.Load();
        _base = new byte[checked(width * height * Detail * Detail * 4)];
        _pixels = new byte[_base.Length];
        _phases = new byte[width * height];
        Array.Fill(_phases, Unknown);
        Image = Image.CreateFromData(width * Detail, height * Detail, false, Image.Format.Rgba8, _pixels);
    }

    public bool IsKnown(Vector2I cell)
    {
        int index = Index(cell);
        return index >= 0 && _phases[index] != Unknown;
    }

    public bool Paint(Vector2I cell, int phase, bool reveal = false)
    {
        int index = Index(cell);
        if (index < 0 || (!reveal && _phases[index] == Unknown))
            return false;
        phase = Mathf.Clamp(phase, 0, 4);
        if (_phases[index] == phase)
            return false;
        bool discovered = _phases[index] == Unknown;
        if (discovered)
            SampleTerrain(cell);
        _phases[index] = (byte)phase;
        Render(cell);
        // Une cellule découverte referme le bord effiloché de ses voisines déjà connues.
        if (discovered)
            foreach (Vector2I side in Sides)
                if (IsKnown(cell + side))
                    Render(cell + side);
        _dirty = true;
        return true;
    }

    private void SampleTerrain(Vector2I cell)
    {
        int startX = (cell.X - _origin.X) * Detail;
        int startY = (cell.Y - _origin.Y) * Detail;
        float step = _cellSize / (float)Detail;
        for (int y = 0; y < Detail; y++)
            for (int x = 0; x < Detail; x++)
            {
                Color color = _sample(new Vector2(cell.X * _cellSize + (x + 0.5f) * step, cell.Y * _cellSize + (y + 0.5f) * step));
                Store(_base, ((startY + y) * _width * Detail + startX + x) * 4, color);
            }
    }

    /// <summary>
    /// Couleur de phase de chaque pixel ; contre une voisine inconnue, le bord s'effiloche en trame (plan 37 R4) : un
    /// pixel sur deux au bord. L'intérieur de la cellule garde tout son détail.
    /// </summary>
    private void Render(Vector2I cell)
    {
        int phase = _phases[Index(cell)];
        bool up = Settled(cell + Vector2I.Up);
        bool down = Settled(cell + Vector2I.Down);
        bool left = Settled(cell + Vector2I.Left);
        bool right = Settled(cell + Vector2I.Right);
        int startX = (cell.X - _origin.X) * Detail;
        int startY = (cell.Y - _origin.Y) * Detail;
        for (int y = 0; y < Detail; y++)
        {
            for (int x = 0; x < Detail; x++)
            {
                int offset = ((startY + y) * _width * Detail + startX + x) * 4;
                int gx = startX + x;
                int gy = startY + y;
                int depth = Mathf.Min(Mathf.Min(up ? Detail : y, down ? Detail : Detail - 1 - y),
                    Mathf.Min(left ? Detail : x, right ? Detail : Detail - 1 - x));
                if (depth == 0 && ((gx + gy) & 1) == 0)
                {
                    Store(_pixels, offset, Colors.Transparent);
                    continue;
                }
                Color terrain = new(_base[offset] / 255f, _base[offset + 1] / 255f, _base[offset + 2] / 255f, _base[offset + 3] / 255f);
                Store(_pixels, offset, _palette.ApplyPhase(terrain, phase, gx, gy));
            }
        }
    }

    /// <summary>Voisine qui ne fait pas effilocher le bord : connue, ou hors de la carte (le bord du monde reste net).</summary>
    private bool Settled(Vector2I cell)
    {
        int index = Index(cell);
        return index < 0 || _phases[index] != Unknown;
    }

    public bool Flush()
    {
        if (!_dirty)
            return false;
        Image.SetData(_width * Detail, _height * Detail, false, Image.Format.Rgba8, _pixels);
        _dirty = false;
        return true;
    }

    private int Index(Vector2I cell)
    {
        int x = cell.X - _origin.X;
        int y = cell.Y - _origin.Y;
        return x < 0 || y < 0 || x >= _width || y >= _height ? -1 : y * _width + x;
    }

    private static void Store(byte[] pixels, int offset, Color color)
    {
        pixels[offset] = (byte)Mathf.RoundToInt(color.R * 255f);
        pixels[offset + 1] = (byte)Mathf.RoundToInt(color.G * 255f);
        pixels[offset + 2] = (byte)Mathf.RoundToInt(color.B * 255f);
        pixels[offset + 3] = (byte)Mathf.RoundToInt(color.A * 255f);
    }

    public void Dispose() => Image.Dispose();
}
