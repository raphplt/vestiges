using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.UI;

namespace Vestiges.World;

/// <summary>
/// Cercle à tenir d'un Mémorial sur une terre (plan 51 P2b, plan 57 C4) : son bord au sol, un anneau qui se remplit comme
/// une horloge depuis le haut, et les secondes restantes au-dessus du Mémorial. Hors du cercle, bord et anneau rougissent
/// et le texte rappelle d'y revenir. Au dernier tiers, le bord se resserre. Au pixel du monde, sous les entités ; les bords
/// de chaque rayon possible sont cuits une fois, et le dessin ne se refait qu'à un changement visible.
/// </summary>
public partial class MemorialCircle : Node2D
{
    // Crans du resserrement : des bords cuits d'avance plutôt qu'une texture par pixel de rayon.
    private const int CloseSteps = 6;
    // Une perle de l'anneau tous les ~6 px de pourtour.
    private const float BeadSpacing = 6f;
    private const float BeadSize = 2f;
    private const float LabelLift = 76f;
    private static readonly Color DangerColor = new(1f, 0.42f, 0.34f);

    private int _radius;
    private int _minRadius;
    private int _shownRadius;
    private Color _color;
    private float _progress;
    private int _beads;
    private int _litBeads = -1;
    private bool _inside = true;
    private int _seconds = -1;
    private Label _label;

    public void Setup(float radiusPx, float closeShare, Color color)
    {
        _radius = Mathf.RoundToInt(radiusPx);
        _minRadius = Mathf.RoundToInt(radiusPx * closeShare);
        _shownRadius = _radius;
        _color = color;
        ZIndex = -1;
        for (int step = 0; step <= CloseSteps; step++)
            PixelEllipseArt.Get(StepRadius(step), false);
        _label = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Position = new Vector2(-90f, -LabelLift),
            Size = new Vector2(180f, 12f),
            TextureFilter = TextureFilterEnum.Nearest,
            // Texte à lire au-dessus des entités (couches dans AGENTS.md), quand le cercle reste au sol.
            ZIndex = 20,
            ZAsRelative = false,
        };
        UITheme.SetCompactText(_label, 8);
        AddChild(_label);
    }

    /// <summary>
    /// Part tenue (0 à 1), rayon du moment, joueur dedans ou non, secondes restantes : le dessin et le texte ne se refont
    /// qu'à un changement visible.
    /// </summary>
    public void SetState(float progress, float radius, bool inside, int seconds)
    {
        _progress = Mathf.Clamp(progress, 0f, 1f);
        int shown = Quantize(radius);
        int beads = Mathf.Max(24, Mathf.RoundToInt(Perimeter(shown) / BeadSpacing));
        int lit = Mathf.FloorToInt(_progress * beads);
        if (shown != _shownRadius || beads != _beads || lit != _litBeads || inside != _inside)
        {
            _shownRadius = shown;
            _beads = beads;
            _litBeads = lit;
            QueueRedraw();
        }
        if (inside == _inside && seconds == _seconds)
            return;
        _inside = inside;
        _seconds = seconds;
        _label.Text = inside ? string.Format(Tr("MEMORIAL_CIRCLE_HOLD"), seconds) : Tr("MEMORIAL_CIRCLE_RETURN");
        _label.AddThemeColorOverride("font_color", inside ? _color : DangerColor);
    }

    public override void _Draw()
    {
        Color edge = _inside ? _color : DangerColor;
        DrawTexture(PixelEllipseArt.Get(_shownRadius, false), PixelEllipseArt.Offset(_shownRadius), edge with { A = 0.7f });
        // L'anneau-horloge, posé juste à l'intérieur du bord : il part du haut et tourne dans le sens des aiguilles.
        float ring = _shownRadius - 5f;
        for (int i = 0; i < _beads; i++)
        {
            float angle = -Mathf.Pi / 2f + Mathf.Tau * i / _beads;
            Vector2 at = Iso.ToScreen(Vector2.FromAngle(angle) * ring).Round() - Vector2.One * (BeadSize / 2f);
            bool lit = i < _litBeads;
            DrawRect(new Rect2(at, Vector2.One * BeadSize), lit ? edge : edge with { A = 0.22f });
        }
    }

    private int Quantize(float radius)
    {
        if (_radius == _minRadius)
            return _radius;
        float share = Mathf.Clamp((_radius - radius) / (_radius - _minRadius), 0f, 1f);
        return StepRadius(Mathf.RoundToInt(share * CloseSteps));
    }

    private int StepRadius(int step) => _radius - (_radius - _minRadius) * step / CloseSteps;

    // Pourtour de l'ellipse écrasée au sol (Ramanujan, assez juste pour espacer des perles).
    private static float Perimeter(float radius)
    {
        float a = radius;
        float b = radius / Iso.GroundSquash;
        return Mathf.Pi * (3f * (a + b) - Mathf.Sqrt((3f * a + b) * (a + 3f * b)));
    }
}
