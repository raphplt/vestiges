using Godot;
using Vestiges.Core;

namespace Vestiges.UI;

/// <summary>
/// Compteur de morts en rafale (plan 02 J5) : « ×24 » sous la plaque de vie, à partir de cinq créatures abattues à moins
/// de 1,5 s d'intervalle. Chaque mort le fait pulser ; il grossit et se dore avec la rafale, puis s'efface quand elle
/// retombe. Écoute EventBus.EnemyKilled ; le texte ne change qu'à une mort, jamais par frame.
/// </summary>
public partial class KillStreakDisplay : Control
{
    private const float StreakWindowSec = 1.5f;
    private const int ShowFrom = 5;
    private const float FadeSec = 0.6f;
    private const float PopSec = 0.12f;
    private static readonly Color Warm = new(1f, 0.93f, 0.78f);
    private static readonly Color Gold = new(0xD4 / 255f, 0xA8 / 255f, 0x43 / 255f);

    private EventBus _eventBus;
    private Label _count;
    private Label _caption;
    private int _streak;
    private float _sinceKill = float.MaxValue;
    private float _sincePop = float.MaxValue;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        _count = new Label { MouseFilter = MouseFilterEnum.Ignore, Size = new Vector2(120f, 30f) };
        _count.AddThemeFontSizeOverride("font_size", 22);
        _count.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.9f));
        _count.AddThemeConstantOverride("outline_size", 5);
        _count.PivotOffset = new Vector2(0f, 15f);
        AddChild(_count);
        _caption = new Label { MouseFilter = MouseFilterEnum.Ignore, Position = new Vector2(2f, 26f), Text = Tr("UI_HUD_KILL_STREAK") };
        _caption.AddThemeFontSizeOverride("font_size", 8);
        _caption.AddThemeColorOverride("font_color", new Color(0.8f, 0.78f, 0.72f));
        _caption.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 0.9f));
        _caption.AddThemeConstantOverride("outline_size", 3);
        AddChild(_caption);
        Modulate = new Color(1f, 1f, 1f, 0f);

        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.EnemyKilled += OnEnemyKilled;
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
            _eventBus.EnemyKilled -= OnEnemyKilled;
    }

    private void OnEnemyKilled(string enemyId, Vector2 position)
    {
        _streak = _sinceKill <= StreakWindowSec ? _streak + 1 : 1;
        _sinceKill = 0f;
        if (_streak < ShowFrom)
        {
            // Nouvelle rafale pendant que l'ancienne s'efface : on la cache aussitôt.
            if (_streak == 1)
                Modulate = new Color(1f, 1f, 1f, 0f);
            return;
        }
        _count.Text = $"×{_streak}";
        // Plus la rafale est longue, plus le chiffre est gros et doré (plein à 50).
        float heat = Mathf.Clamp((_streak - ShowFrom) / 45f, 0f, 1f);
        _count.AddThemeColorOverride("font_color", Warm.Lerp(Gold, heat));
        _count.AddThemeFontSizeOverride("font_size", 22 + (int)(heat * 10f));
        _sincePop = 0f;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _sinceKill += dt;
        _sincePop += dt;
        if (_streak < ShowFrom)
            return;

        float pop = Mathf.Clamp(_sincePop / PopSec, 0f, 1f);
        _count.Scale = Vector2.One * Mathf.Lerp(1.35f, 1f, 1f - (1f - pop) * (1f - pop));
        float fade = Mathf.Clamp((_sinceKill - StreakWindowSec) / FadeSec, 0f, 1f);
        Modulate = new Color(1f, 1f, 1f, 1f - fade);
        if (fade >= 1f)
            _streak = 0;
    }
}
