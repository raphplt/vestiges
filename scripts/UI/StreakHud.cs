using Godot;
using Vestiges.Core;

namespace Vestiges.UI;

/// <summary>
/// La Série au HUD, en haut au centre sous le temps (plan 52 §15) : un gros « ×N » et le nom du rang à sa couleur, puis
/// la jauge de la fenêtre. Visible dès ×2. Une élimination fait rebondir le chiffre ; un rang gagné fait claquer son
/// nom ; la jauge presque vide clignote en rouge ; un coup reçu fait virer le chiffre au rouge, affiche « ÷2 » et fait
/// tomber le nom du rang perdu ; l'encaissement fige le chiffre en or et affiche l'Essence gagnée.
/// Écoute <see cref="EventBus.StreakChanged"/> ; lit la fenêtre de la Série à chaque image tant qu'elle est affichée,
/// et se met au repos sinon. Textes faits une fois (compteurs, noms traduits). Coordonnées en unités de référence 960×540.
/// </summary>
public partial class StreakHud : Control
{
    public const float Top = 30f;
    /// <summary>Bas du bloc : ce qui se range en haut au centre (nom du biome, aide, barre de boss) passe dessous.</summary>
    public const float Bottom = Top + GaugeTop + GaugeHeight + 3f;

    private const float Width = 220f;
    private const int ShowFrom = 2;
    private const int CountSize = 32;
    private const int NameSize = 16;
    private const int SmallSize = 16;
    private const float CountBaseline = 26f;
    private const float NameBaseline = 45f;
    private const float GaugeTop = 50f;
    private const float GaugeWidth = 128f;
    private const float GaugeHeight = 3f;
    private const int CountOutline = 6;
    private const int NameOutline = 4;

    private const float PopSeconds = 0.12f;
    private const float PopScale = 0.28f;
    // Au plus une dizaine de rebonds par seconde : en foule, le chiffre bat sans trembler.
    private const float PopMinInterval = 0.1f;
    private const float SlamSeconds = 0.28f;
    private const float SlamScale = 1.6f;
    private const float HitSeconds = 0.55f;
    private const float HitShakePixels = 3f;
    private const float FallSeconds = 0.7f;
    private const float FallPixels = 26f;
    private const float CashHoldSeconds = 1.1f;
    private const float CashFadeSeconds = 0.4f;
    private const float FadeOutSeconds = 0.25f;
    // Sous ce reste de fenêtre, la jauge clignote en rouge, de plus en plus vite.
    private const float LowWindow = 0.3f;

    private const int CachedTexts = 1000;
    private static readonly string[] CountTexts = new string[CachedTexts];
    private const string HalfText = "÷2";

    private static readonly Color Outline = new(0.02f, 0.02f, 0.04f, 0.92f);
    private static readonly Color GaugeBack = new(0.04f, 0.05f, 0.09f, 0.82f);
    private static readonly Color HitRed = new(0.95f, 0.26f, 0.22f);
    private static readonly Color CashGold = new(1f, 0.84f, 0.38f);

    private EventBus _eventBus;
    private PlayerStreak _streak;
    private Font _font;
    private string[] _rankNames = System.Array.Empty<string>();
    private float[] _rankNameWidths = System.Array.Empty<float>();

    private int _count;
    private int _rank;
    private string _countText = "";
    private float _countWidth;
    private float _alpha;

    private float _pop;
    private float _sincePop = float.MaxValue;
    private float _slam;
    private float _hit;
    private float _halfWidth;
    private int _fallingRank;
    private float _fall;
    private float _cash;
    private string _cashText = "";
    private float _cashWidth;
    private string _cashCountText = "";
    private float _cashCountWidth;
    private float _blinkClock;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        AnchorLeft = 0.5f;
        AnchorRight = 0.5f;
        OffsetLeft = -Width / 2f;
        OffsetRight = Width / 2f;
        OffsetTop = Top;
        OffsetBottom = Bottom;
        _font ??= UITheme.TitleFont;
        _halfWidth = TextWidth(HalfText, SmallSize);
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.StreakChanged += OnStreakChanged;
        SetProcess(false);
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
            _eventBus.StreakChanged -= OnStreakChanged;
    }

    /// <summary>Langue changée en pleine run (paramètres) : les noms de rang suivent.</summary>
    public override void _Notification(int what)
    {
        if (what == NotificationTranslationChanged && _streak != null)
            SetStreak(_streak);
    }

    /// <summary>La Série du joueur suivi : sa fenêtre se lit à chaque image, ses rangs donnent noms et couleurs.</summary>
    public void SetStreak(PlayerStreak streak)
    {
        _streak = streak;
        _font ??= UITheme.TitleFont;
        int ranks = streak?.RankCount ?? 0;
        _rankNames = new string[ranks + 1];
        _rankNameWidths = new float[ranks + 1];
        _rankNames[0] = "";
        for (int rank = 1; rank <= ranks; rank++)
        {
            _rankNames[rank] = Tr(streak.NameAt(rank));
            _rankNameWidths[rank] = TextWidth(_rankNames[rank], NameSize);
        }
    }

    private void OnStreakChanged(StreakChange change)
    {
        if (_streak == null)
            return;
        switch (change.Kind)
        {
            case StreakChangeKind.Gain:
                _cash = 0f;
                if (change.Rank > change.PreviousRank)
                    _slam = SlamSeconds;
                if (_sincePop >= PopMinInterval || change.Rank > change.PreviousRank)
                {
                    _pop = PopSeconds;
                    _sincePop = 0f;
                }
                break;
            case StreakChangeKind.Hit:
                if (change.PreviousCount < ShowFrom)
                    break;
                _hit = HitSeconds;
                if (change.Rank < change.PreviousRank)
                {
                    _fallingRank = change.PreviousRank;
                    _fall = FallSeconds;
                }
                break;
            case StreakChangeKind.CashOut:
                if (change.PreviousCount < ShowFrom)
                    break;
                _cash = CashHoldSeconds + CashFadeSeconds;
                _cashCountText = CountText(change.PreviousCount);
                _cashCountWidth = TextWidth(_cashCountText, CountSize);
                _cashText = change.Essence > 0 ? string.Format(Tr("UI_STREAK_CASHOUT"), change.Essence) : "";
                _cashWidth = TextWidth(_cashText, SmallSize);
                _rank = change.PreviousRank;
                _hit = 0f;
                _fall = 0f;
                break;
            case StreakChangeKind.Reset:
                _cash = 0f;
                _hit = 0f;
                _fall = 0f;
                _alpha = 0f;
                break;
        }
        SetCount(change.Count, change.Kind == StreakChangeKind.CashOut ? _rank : change.Rank);
        SetProcess(true);
        QueueRedraw();
    }

    private void SetCount(int count, int rank)
    {
        _rank = rank;
        if (count == _count)
            return;
        _count = count;
        if (count > 0)
        {
            _countText = CountText(count);
            _countWidth = TextWidth(_countText, CountSize);
        }
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _sincePop += dt;
        _pop = Mathf.Max(0f, _pop - dt);
        _slam = Mathf.Max(0f, _slam - dt);
        _hit = Mathf.Max(0f, _hit - dt);
        _fall = Mathf.Max(0f, _fall - dt);
        _cash = Mathf.Max(0f, _cash - dt);
        _blinkClock += dt;

        bool shown = _count >= ShowFrom || _cash > 0f || _fall > 0f || _hit > 0f;
        _alpha = shown ? 1f : Mathf.Max(0f, _alpha - dt / FadeOutSeconds);
        if (_cash > 0f && _cash < CashFadeSeconds && _count < ShowFrom)
            _alpha = _cash / CashFadeSeconds;
        if (_alpha <= 0f && _pop <= 0f && _slam <= 0f && _hit <= 0f)
        {
            SetProcess(false);
            _alpha = 0f;
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_font == null || _alpha <= 0f || _streak == null)
            return;
        float center = Width / 2f;
        bool cashing = _cash > 0f && _count < ShowFrom;

        if (cashing)
        {
            DrawCentered(_cashCountText, _cashCountWidth, CountSize, CountOutline, new Vector2(center, CountBaseline), 1f, CashGold);
            if (_rank > 0)
                DrawCentered(_rankNames[_rank], _rankNameWidths[_rank], NameSize, NameOutline, new Vector2(center, NameBaseline), 1f,
                    CashGold.Lerp(_streak.ColorAt(_rank), 0.4f));
            if (_cashText.Length > 0)
            {
                float rise = (1f - _cash / (CashHoldSeconds + CashFadeSeconds)) * 6f;
                DrawCentered(_cashText, _cashWidth, SmallSize, NameOutline, new Vector2(center, GaugeTop + 14f - rise), 1f, UITheme.CyanEssence);
            }
            return;
        }

        Color rankColor = _streak.ColorAt(_rank);
        float hit = _hit / HitSeconds;
        Color countColor = rankColor.Lerp(HitRed, hit);
        float pop = _pop / PopSeconds;
        float scale = 1f + PopScale * pop * pop;
        float shake = _hit > 0f ? Mathf.Sin(_hit * 90f) * HitShakePixels * hit : 0f;
        if (_count >= ShowFrom || _hit > 0f)
        {
            DrawCentered(_countText, _countWidth, CountSize, CountOutline, new Vector2(center + shake, CountBaseline), scale, countColor);
            if (_hit > 0f)
                DrawString(_font, new Vector2(center + _countWidth / 2f + 6f, CountBaseline - 8f - (1f - hit) * 6f), HalfText,
                    HorizontalAlignment.Left, -1, SmallSize, HitRed with { A = Mathf.Min(1f, hit * 2f) });
        }

        if (_rank > 0)
        {
            float slam = _slam / SlamSeconds;
            float nameScale = 1f + (SlamScale - 1f) * slam * slam;
            Color nameColor = rankColor.Lerp(Colors.White, slam * 0.6f);
            // Après un coup, le nouveau nom n'arrive qu'une fois l'ancien tombé : les deux ne se mêlent pas.
            float arrival = _fall > 0f ? Mathf.Clamp((1f - _fall / FallSeconds - 0.35f) / 0.3f, 0f, 1f) : 1f;
            DrawCentered(_rankNames[_rank], _rankNameWidths[_rank], NameSize, NameOutline, new Vector2(center, NameBaseline), nameScale,
                nameColor with { A = arrival });
        }
        if (_fall > 0f && _fallingRank > 0 && _fallingRank < _rankNames.Length)
        {
            float fall = 1f - _fall / FallSeconds;
            DrawCentered(_rankNames[_fallingRank], _rankNameWidths[_fallingRank], NameSize, NameOutline,
                new Vector2(center + fall * 10f, NameBaseline + (fall * 0.4f + fall * fall * 0.6f) * FallPixels), 1f, HitRed with { A = 1f - fall });
        }

        if (_count >= ShowFrom)
            DrawGauge(center, rankColor);
    }

    private void DrawGauge(float center, Color rankColor)
    {
        float fraction = _streak.WindowFraction;
        Rect2 back = new(center - GaugeWidth / 2f - 1f, GaugeTop - 1f, GaugeWidth + 2f, GaugeHeight + 2f);
        DrawRect(back, GaugeBack);
        Color fill = rankColor;
        if (fraction < LowWindow)
        {
            // Plus elle se vide, plus le clignotement presse : de 4 à 12 battements par seconde.
            float urgency = 1f - fraction / LowWindow;
            float beat = Mathf.Sin(_blinkClock * Mathf.Tau * Mathf.Lerp(4f, 12f, urgency));
            fill = beat > 0f ? HitRed : rankColor;
        }
        DrawRect(new Rect2(center - GaugeWidth / 2f, GaugeTop, Mathf.Round(GaugeWidth * fraction), GaugeHeight), fill);
    }

    /// <summary>Texte centré sur <paramref name="anchor"/> (ligne de base), mis à l'échelle autour de son centre.</summary>
    private void DrawCentered(string text, float width, int size, int outline, Vector2 anchor, float scale, Color color)
    {
        if (text.Length == 0)
            return;
        color.A *= _alpha;
        Color outlineColor = Outline with { A = Outline.A * color.A };
        Vector2 pivot = anchor - new Vector2(0f, size * 0.35f);
        DrawSetTransform(pivot, 0f, new Vector2(scale, scale));
        Vector2 position = anchor - pivot - new Vector2(width / 2f, 0f);
        DrawStringOutline(_font, position, text, HorizontalAlignment.Left, -1, size, outline, outlineColor);
        DrawString(_font, position, text, HorizontalAlignment.Left, -1, size, color);
        DrawSetTransform(Vector2.Zero, 0f, Vector2.One);
    }

    private float TextWidth(string text, int size) =>
        _font == null || text.Length == 0 ? 0f : _font.GetStringSize(text, HorizontalAlignment.Left, -1, size).X;

    private static string CountText(int count)
    {
        if (count >= CachedTexts)
            return "×" + count;
        return CountTexts[count] ??= "×" + count;
    }
}
