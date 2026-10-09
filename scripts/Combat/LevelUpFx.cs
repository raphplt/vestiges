using Godot;
using Vestiges.Core;

namespace Vestiges.Combat;

/// <summary>
/// Temps fort de la montée de niveau (plan 02 J4) : onde dorée au sol, colonne de lumière sur le héros, gerbe d'éclats,
/// et créatures proches repoussées en apparence seulement (aucun effet de jeu). Tout passe par CombatPools, sans nœud
/// créé. L'écran de choix met le jeu en pause aussitôt : l'effet se fige derrière lui et se joue au retour dans la run,
/// sans rien retarder.
/// </summary>
public static class LevelUpFx
{
    private const float ShoveRadius = 150f;
    private const float ShovePx = 10f;

    public static void Play(Player player)
    {
        CombatPools pools = CombatPools.Instance;
        if (pools == null || player == null)
            return;
        Vector2 feet = player.GlobalPosition;

        PixelFxSpec ring = PixelFxSpec.Of(PixelFxShape.Ring, FxFamily.Crit, 90f, 3f, 0.55f);
        ring.Squash = 2f;
        ring.Steps = 8;
        ring.FadeTail = 0.4f;
        ring.ZIndex = -1;
        pools.PlayFx(feet, ring, FxOwner.World);

        // Colonne : un trait droit qui jaillit des pieds vers le ciel (le rayon Beam, brisé, se lirait comme un éclair reçu).
        PixelFxSpec column = PixelFxSpec.Of(PixelFxShape.Thrust, FxFamily.Crit, 80f, 12f, 0.5f);
        column.Angle = -Mathf.Pi / 2f;
        column.Steps = 7;
        column.FadeTail = 0.4f;
        column.ZIndex = 2;
        pools.PlayFx(feet, column, FxOwner.World);

        pools.EmitSparks(feet + new Vector2(0f, -10f), new SparkBurst
        {
            Family = FxFamily.Crit,
            Owner = FxOwner.World,
            Count = 18,
            Direction = Vector2.Up,
            Spread = 2.4f,
            SpeedMin = 50f,
            SpeedMax = 120f,
            LifeMin = 0.4f,
            LifeMax = 0.7f,
            Size = 1,
            Decorative = true,
        });

        float radiusSq = ShoveRadius * ShoveRadius;
        using CrowdQuery crowd = CrowdIndex.Near(feet, ShoveRadius);
        foreach (Node node in crowd.Targets)
        {
            if (node is not Enemy enemy || !enemy.IsActive || enemy.IsDying)
                continue;
            Vector2 offset = enemy.GlobalPosition - feet;
            float distSq = offset.LengthSquared();
            if (distSq > radiusSq || distSq < 0.01f)
                continue;
            // Plus fort au contact, nul au bord de l'onde.
            enemy.Shove(offset.Normalized(), ShovePx * (1f - Mathf.Sqrt(distSq) / ShoveRadius));
        }
    }
}
