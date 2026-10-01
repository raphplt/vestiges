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
        bool sample = _phases[index] == Unknown;
        int startX = (cell.X - _origin.X) * Detail;
        int startY = (cell.Y - _origin.Y) * Detail;
        float step = _cellSize / (float)Detail;
        for (int y = 0; y < Detail; y++)
        {
            for (int x = 0; x < Detail; x++)
            {
                int offset = ((startY + y) * _width * Detail + startX + x) * 4;
                if (sample)
                {
                    Color color = _sample(new Vector2(cell.X * _cellSize + (x + 0.5f) * step, cell.Y * _cellSize + (y + 0.5f) * step));
                    Store(_base, offset, color);
                }
                Color terrain = new(_base[offset] / 255f, _base[offset + 1] / 255f, _base[offset + 2] / 255f, _base[offset + 3] / 255f);
                Store(_pixels, offset, _palette.ApplyPhase(terrain, phase, startX + x, startY + y));
            }
        }
        _phases[index] = (byte)phase;
        _dirty = true;
        return true;
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
