using Godot;
using Vestiges.Core;
using Vestiges.Progression;

namespace Vestiges.UI;

/// <summary>
/// Perks de survie sur la barre de PV (plan 05, B2) : la réserve de Prévoyance en liseré doré au bas de la barre,
/// pleine quand elle est chargée ; la part que Reprise peut encore rendre en prolongement clair des PV, qui s'éteint
/// avec sa fenêtre et bat dans sa dernière seconde et demie.
/// </summary>
public partial class VitalsPerkOverlay : Control
{
    private const float ReserveThickness = 3f;
    private const float UrgentSeconds = 1.5f;
    private static readonly Color ReserveColor = new(0xD4 / 255f, 0xA8 / 255f, 0x43 / 255f);
    private static readonly Color RecoverableColor = new(0.72f, 0.95f, 0.62f);

    private EventBus _eventBus;
    private float _hpRatio = 1f;
    private float _reserveRatio;
    private float _recoverableRatio;
    private float _windowRatio;
    private float _remaining;
    private float _pulse;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.SpecializationGaugeChanged += OnGauge;
        _eventBus.PlayerDamaged += OnVitals;
        SetProcess(false);
    }

    public override void _ExitTree()
    {
        if (_eventBus == null)
            return;
        _eventBus.SpecializationGaugeChanged -= OnGauge;
        _eventBus.PlayerDamaged -= OnVitals;
    }

    private void OnVitals(float currentHp, float maxHp)
    {
        _hpRatio = maxHp > 0f ? Mathf.Clamp(currentHp / maxHp, 0f, 1f) : 0f;
        QueueRedraw();
    }

    private void OnGauge(SpecializationGauge gauge)
    {
        switch (gauge.Effect)
        {
            case SpecializationRuntime.OverhealReserveEffect:
                _reserveRatio = gauge.Max > 0f ? Mathf.Clamp(gauge.Value / gauge.Max, 0f, 1f) : 0f;
                break;
            case SpecializationRuntime.RallyEffect:
                _recoverableRatio = gauge.Max > 0f ? Mathf.Clamp(gauge.Value / gauge.Max, 0f, 1f) : 0f;
                _windowRatio = gauge.Duration > 0f ? Mathf.Clamp(gauge.Remaining / gauge.Duration, 0f, 1f) : 0f;
                _remaining = gauge.Remaining;
                SetProcess(_recoverableRatio > 0f && _remaining < UrgentSeconds);
                break;
            default:
                return;
        }
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        _pulse += (float)delta * 14f;
        QueueRedraw();
    }

    public override void _Draw()
    {
        // Intérieur de la piste : 1 px de bord, comme le remplissage des PV.
        Rect2 inner = new(1f, 1f, Size.X - 2f, Size.Y - 2f);
        if (_recoverableRatio > 0f && _windowRatio > 0f)
        {
            float start = inner.Position.X + inner.Size.X * _hpRatio;
            float width = Mathf.Min(inner.Size.X * _recoverableRatio, inner.End.X - start);
            float alpha = 0.25f + 0.45f * _windowRatio;
            if (_remaining < UrgentSeconds)
                alpha *= 0.55f + 0.45f * Mathf.Sin(_pulse);
            DrawRect(new Rect2(start, inner.Position.Y, width, inner.Size.Y), RecoverableColor with { A = alpha });
        }
        if (_reserveRatio > 0f)
        {
            DrawRect(new Rect2(inner.Position.X, inner.End.Y - ReserveThickness, inner.Size.X * _reserveRatio, ReserveThickness),
                ReserveColor);
        }
    }
}
