using Godot;

namespace Vestiges.Combat;

/// <summary>
/// Nom, affixes et PV au-dessus d'une variante renforcée : l'élite se repère et se jauge d'un coup d'œil.
/// Compense l'échelle de son porteur pour garder un texte net et de taille constante.
/// Ne se redessine que lorsque les PV affichés changent.
/// </summary>
public partial class EnemyNameplate : Node2D
{
    private const int NameFontSize = 8;
    private const int AffixFontSize = 6;
    private const float BarWidth = 34f;
    private const float BarHeight = 3f;
    private const float Gap = 3f;

    private static Font _font;
    private static readonly Color TrackColor = new(0.03f, 0.03f, 0.06f, 0.9f);
    private static readonly Color HpColor = new(0.77f, 0.26f, 0.17f);
    private static readonly Color TextOutline = new(0f, 0f, 0f, 0.95f);

    private Enemy _owner;
    private string _name;
    private string _affixLine;
    private Color _accent;
    private float _shownRatio = -1f;

    public void Setup(Enemy owner, string displayName, string affixLine, Color accent, float ownerScale, float visualTop)
    {
        _font ??= GD.Load<Font>("res://assets/fonts/saira/SairaSemiCondensed-SemiBold.ttf");
        _owner = owner;
        _name = displayName;
        _affixLine = affixLine;
        _accent = accent;
        ZIndex = 20;
        Scale = Vector2.One / Mathf.Max(0.1f, ownerScale);
        // Position exprimée dans le repère agrandi du porteur : juste au-dessus de sa tête.
        Position = new Vector2(0f, visualTop - Gap / ownerScale);
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (_owner == null)
            return;
        float ratio = Mathf.Snapped(_owner.HpRatio, 0.01f);
        if (!Mathf.IsEqualApprox(ratio, _shownRatio))
        {
            _shownRatio = ratio;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        if (_font == null)
            return;

        float ratio = Mathf.Clamp(_shownRatio, 0f, 1f);
        DrawRect(new Rect2(-BarWidth / 2f - 1f, -BarHeight - 1f, BarWidth + 2f, BarHeight + 2f), TrackColor);
        DrawRect(new Rect2(-BarWidth / 2f, -BarHeight, BarWidth * ratio, BarHeight), HpColor);
        DrawRect(new Rect2(-BarWidth / 2f, -BarHeight, BarWidth * ratio, 1f), _accent with { A = 0.6f });

        float y = -BarHeight - 3f;
        if (!string.IsNullOrEmpty(_affixLine))
        {
            DrawCentered(_affixLine, AffixFontSize, y, _accent.Lightened(0.25f));
            y -= AffixFontSize + 1f;
        }
        DrawCentered(_name, NameFontSize, y, _accent.Lightened(0.45f));
    }

    private void DrawCentered(string text, int size, float baseline, Color color)
    {
        float width = _font.GetStringSize(text, HorizontalAlignment.Left, -1, size).X;
        Vector2 position = new(-width / 2f, baseline);
        DrawStringOutline(_font, position, text, HorizontalAlignment.Left, -1, size, 3, TextOutline);
        DrawString(_font, position, text, HorizontalAlignment.Left, -1, size, color);
    }
}
