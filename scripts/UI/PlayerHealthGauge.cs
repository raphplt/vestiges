using Godot;
using Vestiges.Core;

namespace Vestiges.UI;

/// <summary>
/// Jauge de PV sous les pieds du héros : l'information vitale reste là où le regard se trouve en combat.
/// Discrète à pleine vie, pleinement opaque après un changement, battante quand la vie est basse.
/// Ne se redessine que lorsque son état visible change.
/// </summary>
public partial class PlayerHealthGauge : Node2D
{
    private const float Width = 26f;
    private const float Height = 4f;
    private const float OffsetY = 14f;
    private const float LowHpRatio = 0.3f;
    private const float EmphasisDuration = 2.5f;
    private const float IdleAlpha = 0.55f;
    private const float ChipCatchUpPerSecond = 0.6f;

    private static readonly Color TrackColor = new(0.03f, 0.03f, 0.06f, 0.9f);
    private static readonly Color HealthyColor = new(0.42f, 0.74f, 0.36f);
    private static readonly Color WoundedColor = new(0.88f, 0.48f, 0.22f);
    private static readonly Color CriticalColor = new(0.77f, 0.26f, 0.17f);
    private static readonly Color ChipColor = new(0.91f, 0.88f, 0.83f);

    private EventBus _eventBus;
    private float _ratio = 1f;
    private float _chipRatio = 1f;
    private float _emphasis;
    private float _pulse;

    public override void _Ready()
    {
        ZIndex = 5;
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.PlayerDamaged += OnPlayerDamaged;
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
            _eventBus.PlayerDamaged -= OnPlayerDamaged;
    }

    private void OnPlayerDamaged(float currentHp, float maxHp)
    {
        float ratio = Mathf.Clamp(currentHp / Mathf.Max(1f, maxHp), 0f, 1f);
        if (ratio > _ratio)
            _chipRatio = ratio;
        _ratio = ratio;
        _emphasis = EmphasisDuration;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        bool changed = false;
        if (_chipRatio > _ratio)
        {
            _chipRatio = Mathf.Max(_ratio, _chipRatio - ChipCatchUpPerSecond * dt);
            changed = true;
        }
        if (_emphasis > 0f)
        {
            _emphasis -= dt;
            changed = true;
        }
        if (_ratio < LowHpRatio && _ratio > 0f)
        {
            _pulse += dt * 6f;
            changed = true;
        }
        if (changed)
            QueueRedraw();
    }

    public override void _Draw()
    {
        bool critical = _ratio < LowHpRatio && _ratio > 0f;
        float alpha = critical || _emphasis > 0f || _ratio < 1f ? 1f : IdleAlpha;
        Rect2 outer = new(-Width / 2f - 1f, OffsetY - 1f, Width + 2f, Height + 2f);
        DrawRect(outer, TrackColor with { A = TrackColor.A * alpha });

        float inner = Width;
        if (_chipRatio > _ratio)
            DrawRect(new Rect2(-Width / 2f, OffsetY, inner * _chipRatio, Height), ChipColor with { A = 0.8f * alpha });

        Color fill = _ratio < LowHpRatio ? CriticalColor : (_ratio < 0.55f ? WoundedColor : HealthyColor);
        if (critical)
            fill = fill.Lerp(Colors.White, 0.35f * (0.5f + 0.5f * Mathf.Sin(_pulse)));
        DrawRect(new Rect2(-Width / 2f, OffsetY, inner * _ratio, Height), fill with { A = alpha });
        // Reflet d'un pixel : la barre se lit comme un objet, pas comme un aplat.
        DrawRect(new Rect2(-Width / 2f, OffsetY, inner * _ratio, 1f), Colors.White with { A = 0.25f * alpha });
    }
}
