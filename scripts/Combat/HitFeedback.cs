using Godot;

namespace Vestiges.Combat;

/// <summary>
/// Retour visuel d'un coup reçu par une créature (plan 02 J1) : flash blanc, écrasement puis rebond élastique,
/// recul du visuel dans le sens du coup, étincelles projetées du même côté. Animé par l'ennemi à chaque tick
/// physique, sans tween ni allocation par coup ; le recul déplace le visuel, jamais le corps physique.
/// </summary>
public sealed class HitFeedback
{
    private const float FlashDelay = 0.06f;
    private const float FlashSec = 0.15f;
    private const float SquashSec = 0.15f;
    private const float RecoilSec = 0.1f;
    private const float RecoilPx = 3f;
    private static readonly Vector2 SquashScale = new(1.25f, 0.75f);
    private static readonly Color FlashModulate = new(3f, 3f, 3f, 1f);
    private static readonly StringName FlashParam = "flash_amount";

    private Node2D _target;
    private AnimatedSprite2D _sprite;
    private ShaderMaterial _material;
    private Polygon2D _polygon;
    private Color _polygonColor;
    private Vector2 _recoil;
    private float _elapsed = -1f;

    public bool IsActive => _elapsed >= 0f;

    /// <summary>
    /// Échelle de repos du visuel, que l'écrasement multiplie au lieu de l'écraser : une posture d'annonce
    /// (Enemy.SetWindupPose) survit ainsi à un coup encaissé.
    /// </summary>
    public Vector2 RestScale { get; set; } = Vector2.One;

    /// <summary>
    /// Déclenche le retour d'un coup. <paramref name="direction"/> : sens du coup (de l'attaquant vers la créature),
    /// normalisé, ou zéro. Sans sprite, le polygone de repli flashe par sa couleur.
    /// </summary>
    public void Trigger(AnimatedSprite2D sprite, ShaderMaterial material, Polygon2D polygon, Color polygonColor, Vector2 direction)
    {
        bool hasSprite = sprite != null && material != null;
        _sprite = hasSprite ? sprite : null;
        _material = hasSprite ? material : null;
        _polygon = hasSprite ? null : polygon;
        _polygonColor = polygonColor;
        _target = hasSprite ? sprite : polygon;
        _recoil = direction * RecoilPx;
        _elapsed = 0f;
        Apply();
    }

    /// <summary>Avance l'animation ; à appeler à chaque tick tant que <see cref="IsActive"/>.</summary>
    public void Tick(float delta)
    {
        if (_elapsed < 0f)
            return;
        _elapsed += delta;
        Apply();
        if (_elapsed >= FlashDelay + FlashSec)
            Stop();
    }

    /// <summary>Interrompt et remet le visuel au repos (retour au pool, mort).</summary>
    public void Stop()
    {
        if (_elapsed < 0f)
            return;
        _elapsed = -1f;
        if (_target == null)
            return;
        _target.Scale = RestScale;
        _target.Position = Vector2.Zero;
        if (_sprite != null)
        {
            _material.SetShaderParameter(FlashParam, 0f);
            _sprite.SelfModulate = Colors.White;
        }
        else if (_polygon != null)
        {
            _polygon.Color = _polygonColor;
        }
    }

    private void Apply()
    {
        if (_target == null)
            return;

        // Flash : plein pendant FlashDelay, puis s'éteint linéairement.
        float flash = 1f - Mathf.Clamp((_elapsed - FlashDelay) / FlashSec, 0f, 1f);
        if (_sprite != null)
        {
            _material.SetShaderParameter(FlashParam, flash);
            _sprite.SelfModulate = Colors.White.Lerp(FlashModulate, flash);
        }
        else if (_polygon != null)
        {
            _polygon.Color = _polygonColor.Lerp(Colors.White, flash);
        }

        // Écrasement puis rebond élastique vers l'échelle de repos (courbe Elastic Out de Godot).
        float squash = Mathf.Clamp(_elapsed / SquashSec, 0f, 1f);
        float elastic = squash >= 1f ? 1f
            : Mathf.Pow(2f, -10f * squash) * Mathf.Sin((squash * 10f - 0.75f) * (Mathf.Tau / 3f)) + 1f;
        _target.Scale = RestScale * SquashScale.Lerp(Vector2.One, elastic);

        // Recul bref dans le sens du coup, qui revient en douceur.
        float recoil = Mathf.Clamp(_elapsed / RecoilSec, 0f, 1f);
        _target.Position = _recoil * (1f - recoil) * (1f - recoil);
    }
}
