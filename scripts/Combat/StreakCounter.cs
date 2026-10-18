using Godot;
using Vestiges.UI;

namespace Vestiges.Combat;

/// <summary>
/// La Série au-dessus de la tête du joueur (plan 52 §13) : « ×N » à la couleur de son palier, un rebond à chaque
/// dizaine et plus fort au palier gagné, un éclair rouge quand elle retombe. Le texte n'est refait qu'au changement du
/// compteur ; le nœud ne travaille que pendant un rebond ou un éclair.
/// </summary>
public partial class StreakCounter : Node2D
{
    private const int FontSize = 16;
    // Au-dessus de la jauge d'interaction (−44) : le compteur ne passe pas dessous pendant une fouille.
    private const float HeadOffset = -58f;
    private const float PopSeconds = 0.15f;
    private const float PopScale = 0.35f;
    private const float DropSeconds = 0.35f;
    // Un rebond toutes les dix éliminations : en foule, le compteur ne tremble pas en continu.
    private const int PopEvery = 10;
    // Textes des compteurs courants, faits une fois : aucune chaîne neuve à chaque élimination.
    private const int CachedTexts = 1000;
    private static readonly string[] Texts = new string[CachedTexts];
    private static readonly Color DropColor = new(0.95f, 0.3f, 0.25f, 1f);
    private static readonly Color Outline = new(0.06f, 0.05f, 0.05f, 0.9f);

    private Font _font;
    private int _count;
    private int _tier;
    private string _text = "";
    private float _width;
    private float _pop;
    private float _drop;

    public override void _Ready()
    {
        _font = UITheme.SmallStrongFont;
        ZIndex = 20;
        Position = new Vector2(0f, HeadOffset);
        SetProcess(false);
    }

    public void SetStreak(int count, int tier)
    {
        if (count == _count && tier == _tier)
            return;
        if (count > _count)
        {
            _drop = 0f;
            if (tier > _tier)
                _pop = PopSeconds * 2f;
            else if (count / PopEvery > _count / PopEvery)
                _pop = PopSeconds;
        }
        else
            _drop = DropSeconds;
        _count = count;
        _tier = tier;
        // Une Série qui retombe à zéro garde son dernier nombre le temps de l'éclair.
        if (count > 0)
        {
            _text = TextFor(count);
            _width = _font?.GetStringSize(_text, HorizontalAlignment.Left, -1, FontSize).X ?? 0f;
        }
        SetProcess(_pop > 0f || _drop > 0f);
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        _pop = Mathf.Max(0f, _pop - (float)delta);
        _drop = Mathf.Max(0f, _drop - (float)delta);
        Scale = Vector2.One * (1f + PopScale * _pop / PopSeconds);
        if (_pop <= 0f && _drop <= 0f)
            SetProcess(false);
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_font == null || _text.Length == 0 || (_count == 0 && _drop <= 0f))
            return;
        Color color = _drop > 0f ? DropColor : MobilityFeedback.StreakColor(_tier);
        Vector2 position = new(-_width / 2f, 0f);
        DrawStringOutline(_font, position, _text, HorizontalAlignment.Left, -1, FontSize, 3, Outline);
        DrawString(_font, position, _text, HorizontalAlignment.Left, -1, FontSize, color);
    }

    private static string TextFor(int count)
    {
        if (count >= CachedTexts)
            return "×" + count;
        return Texts[count] ??= "×" + count;
    }
}
