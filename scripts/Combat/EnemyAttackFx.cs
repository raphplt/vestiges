using Godot;

namespace Vestiges.Combat;

/// <summary>
/// Mise en scène des attaques ennemies (plan 08, effets d'attaque) : griffe de mêlée, slam des Colosses,
/// annonce de charge et de cri. Toujours affichée (information de danger), opacité réglable.
/// </summary>
public static class EnemyAttackFx
{
    private const float TorsoHeight = 10f;

    private static CombatPools Pools => CombatPools.Instance;

    /// <summary>Coup de mêlée qui porte : griffe vert-acide vers le joueur, éclats de sang sur lui.</summary>
    public static void PlayMeleeHit(Vector2 attacker, Vector2 target)
    {
        if (Pools == null)
            return;
        Vector2 direction = (target - attacker).Normalized();
        Vector2 torso = attacker + new Vector2(0f, -TorsoHeight);
        PixelFxSpec spec = PixelFxSpec.Of(PixelFxShape.Arc, FxFamily.Hostile, 16f, 7f, 0.16f);
        spec.Angle = new Vector2(direction.X, direction.Y * 1.35f).Angle();
        spec.ArcHalf = 0.9f;
        spec.Squash = 1.35f;
        spec.Steps = 5;
        spec.FadeTail = 0.25f;
        spec.ZIndex = 2;
        Pools.PlayFx(torso + direction * 6f, spec, FxOwner.Enemy);
        Pools.EmitSparks(target + new Vector2(0f, -TorsoHeight), new SparkBurst
        {
            Family = FxFamily.Blood,
            Owner = FxOwner.Enemy,
            Count = 5,
            Direction = direction,
            Spread = 1.4f,
            SpeedMin = 50f,
            SpeedMax = 110f,
            LifeMin = 0.25f,
            LifeMax = 0.4f,
            Ballistic = true,
            Size = 1,
        });
    }

    /// <summary>Slam d'un Colosse : onde au sol et gravats projetés.</summary>
    public static void PlaySlam(Vector2 ground, float radius)
    {
        if (Pools == null)
            return;
        PixelFxSpec ring = PixelFxSpec.Of(PixelFxShape.Ring, FxFamily.Hostile, radius, 5f, 0.35f);
        ring.Squash = 2f;
        ring.Steps = 6;
        ring.FadeTail = 0.35f;
        ring.ZIndex = -1;
        Pools.PlayFx(ground, ring, FxOwner.Enemy);
        Pools.EmitSparks(ground, new SparkBurst
        {
            Family = FxFamily.Stone,
            Owner = FxOwner.Enemy,
            Count = 14,
            Spread = Mathf.Tau,
            SpeedMin = 70f,
            SpeedMax = 150f,
            LifeMin = 0.4f,
            LifeMax = 0.65f,
            Ballistic = true,
            Size = 2,
        });
    }

    /// <summary>
    /// Annonce d'une charge ou d'un cri sur le sprite lui-même (l'ancien flash colorait un polygone caché).
    /// Teinte brève et éclaircie vers une couleur de la palette, puis retour au blanc.
    /// </summary>
    public static void FlashWarning(CanvasItem sprite, Color paletteColor, float duration)
    {
        if (sprite == null)
            return;
        sprite.SelfModulate = Colors.White.Lerp(paletteColor, 0.55f) * 1.4f;
        Tween tween = sprite.CreateTween();
        tween.TweenProperty(sprite, "self_modulate", Colors.White, duration).SetDelay(0.1f);
    }
}
