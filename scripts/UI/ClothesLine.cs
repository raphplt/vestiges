using System.Collections.Generic;
using Godot;

namespace Vestiges.UI;

/// <summary>
/// Fil du level-up (plan 04 §11) : tendu entre deux clous, il ploie au milieu. Dessine aussi les ficelles des
/// étiquettes. Rien ne bouge d'une image à l'autre : le dessin n'est refait qu'à un changement de mise en page.
/// </summary>
public partial class ClothesLine : Control
{
    private const int Step = 4;
    private readonly List<Vector2> _stringEnds = new();
    private float _left;
    private float _right;
    private float _top;
    private float _sag;

    public ClothesLine()
    {
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public float Left => _left;
    public float Right => _right;
    public float Top => _top;

    public void Stretch(float left, float right, float top, float sag)
    {
        _left = left;
        _right = right;
        _top = top;
        _sag = sag;
        QueueRedraw();
    }

    /// <summary>Hauteur du fil à l'abscisse donnée : une demi-sinusoïde entre les clous.</summary>
    public float ThreadY(float x)
    {
        float t = Mathf.Clamp((x - _left) / Mathf.Max(1f, _right - _left), 0f, 1f);
        return _top + _sag * Mathf.Sin(Mathf.Pi * t);
    }

    /// <summary>Ficelles verticales ou obliques, par paires de points (haut, bas).</summary>
    public void SetStrings(List<Vector2> ends)
    {
        _stringEnds.Clear();
        _stringEnds.AddRange(ends);
        QueueRedraw();
    }

    public override void _Draw()
    {
        for (float x = _left; x <= _right; x += Step)
        {
            float y = Mathf.Floor(ThreadY(x) / 2f) * 2f;
            DrawRect(new Rect2(x, y + 4f, Step, Step), new Color(PaperArt.ThreadShadow, 0.55f));
            // Ficelle torsadée : un toron sur deux plus sombre.
            DrawRect(new Rect2(x, y, Step, Step), (int)(x / Step) % 2 == 0 ? PaperArt.Thread : PaperArt.ThreadTwist);
        }
        Texture2D nail = PaperArt.Nail();
        Vector2 half = nail.GetSize() / 2f;
        DrawTexture(nail, new Vector2(_left, _top) - half);
        DrawTexture(nail, new Vector2(_right, _top) - half);

        for (int i = 0; i + 1 < _stringEnds.Count; i += 2)
        {
            Vector2 from = _stringEnds[i];
            Vector2 to = _stringEnds[i + 1];
            float length = from.DistanceTo(to);
            for (float d = 0f; d < length; d += Step)
            {
                Vector2 p = from.Lerp(to, d / length).Floor();
                DrawRect(new Rect2(p.X - 1f, p.Y, 3f, 3f), PaperArt.Thread);
            }
        }
    }
}
