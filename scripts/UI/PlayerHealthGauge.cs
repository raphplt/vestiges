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
    private static readonly Color ShieldColor = new(0.72f, 0.86f, 1f);
    /// <summary>Icône de toile (plan 27 V3b) : rayons et anneau d'une toile de 5 px, à droite de la jauge.</summary>
    private static readonly Vector2I[] WebIcon =
    {
        new(0, 0), new(4, 0), new(2, 0), new(1, 1), new(3, 1), new(0, 2), new(1, 2), new(2, 2), new(3, 2), new(4, 2),
        new(1, 3), new(3, 3), new(0, 4), new(2, 4), new(4, 4),
    };

    private EventBus _eventBus;
    private float _ratio = 1f;
    private float _chipRatio = 1f;
    private float _emphasis;
    private float _pulse;
    private float _shieldRatio;
    private bool _webbed;
    /// <summary>Surbrillance de la barre après un soin (plan 27 V3c), en secondes restantes.</summary>
    private float _healGlow;
    private const float HealGlowSeconds = 0.35f;
    private static readonly Color HealGlowColor = new(0.72f, 0.95f, 0.62f);
    private Player _player;

    public override void _Ready()
    {
        ZIndex = 5;
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.PlayerDamaged += OnPlayerDamaged;
        _eventBus.PlayerShieldChanged += OnShieldChanged;
        _eventBus.PlayerHealingResolved += OnHealed;
        if (GetParent() is Player player)
        {
            OnShieldChanged(player.Shield, player.MaxShield);
            _player = player;
            _player.WebbedChanged += SetWebbed;
        }
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
        {
            _eventBus.PlayerDamaged -= OnPlayerDamaged;
            _eventBus.PlayerShieldChanged -= OnShieldChanged;
            _eventBus.PlayerHealingResolved -= OnHealed;
        }
        if (_player != null)
            _player.WebbedChanged -= SetWebbed;
    }

    private void OnPlayerDamaged(float currentHp, float maxHp)
    {
        // Sans vie, la jauge s'efface avec le héros (séquence de mort) ; un second souffle la ramène.
        Visible = currentHp > 0f;
        float ratio = Mathf.Clamp(currentHp / Mathf.Max(1f, maxHp), 0f, 1f);
        if (ratio > _ratio)
            _chipRatio = ratio;
        _ratio = ratio;
        _emphasis = EmphasisDuration;
        QueueRedraw();
    }

    private void OnHealed(HealingResult result)
    {
        // Un soin net (ni la régénération continue) : la barre s'éclaire brièvement.
        if (result.HpRestored <= 0f || result.Kind == HealingKind.Regeneration)
            return;
        _healGlow = HealGlowSeconds;
        QueueRedraw();
    }

    /// <summary>La toile ralentit le joueur : petite icône à droite de la jauge, toujours visible.</summary>
    public void SetWebbed(bool webbed)
    {
        if (webbed == _webbed)
            return;
        _webbed = webbed;
        QueueRedraw();
    }

    private void OnShieldChanged(float shield, float maxShield)
    {
        float ratio = maxShield > 0f ? Mathf.Clamp(shield / maxShield, 0f, 1f) : 0f;
        if (Mathf.IsEqualApprox(ratio, _shieldRatio))
            return;
        if (ratio < _shieldRatio)
            _emphasis = EmphasisDuration;
        _shieldRatio = ratio;
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
        if (_healGlow > 0f)
        {
            _healGlow -= dt;
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
        if (_healGlow > 0f)
            DrawRect(outer.Grow(1f), HealGlowColor with { A = 0.8f }, false);

        float inner = Width;
        if (_chipRatio > _ratio)
            DrawRect(new Rect2(-Width / 2f, OffsetY, inner * _chipRatio, Height), ChipColor with { A = 0.8f * alpha });

        Color fill = _ratio < LowHpRatio ? CriticalColor : (_ratio < 0.55f ? WoundedColor : HealthyColor);
        if (critical)
            fill = fill.Lerp(Colors.White, 0.35f * (0.5f + 0.5f * Mathf.Sin(_pulse)));
        DrawRect(new Rect2(-Width / 2f, OffsetY, inner * _ratio, Height), fill with { A = alpha });
        // Reflet d'un pixel : la barre se lit comme un objet, pas comme un aplat.
        DrawRect(new Rect2(-Width / 2f, OffsetY, inner * _ratio, 1f), Colors.White with { A = 0.25f * alpha });
        // Bouclier : un trait bleu pâle au-dessus de la vie, qui se vide au coup encaissé.
        if (_shieldRatio > 0f)
            DrawRect(new Rect2(-Width / 2f, OffsetY - 2f, inner * _shieldRatio, 1f), ShieldColor with { A = alpha });
        if (_webbed)
        {
            Vector2 corner = new(Width / 2f + 3f, OffsetY - 1f);
            DrawRect(new Rect2(corner - Vector2.One, new Vector2(7f, 7f)), TrackColor);
            foreach (Vector2I pixel in WebIcon)
                DrawRect(new Rect2(corner + pixel, Vector2.One), ChipColor);
        }
    }
}
