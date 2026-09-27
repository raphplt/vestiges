using Godot;
using Vestiges.World;

namespace Vestiges.Combat;

/// <summary>
/// Trace de chaque pas selon le sol (plan 02 J6) : ronds et gouttes dans l'eau, poussière grise sur le béton,
/// un grain de terre dans l'herbe. Par CombatPools, sans nœud créé, soumis au budget d'effets.
/// </summary>
public static class FootstepFx
{
    public static void Emit(Vector2 feet, TerrainType terrain, Vector2 moveDirection)
    {
        CombatPools pools = CombatPools.Instance;
        if (pools == null || CombatFxSettings.ParticleLevel == ParticleLevel.Off)
            return;

        if (terrain == TerrainType.Water)
        {
            PixelFxSpec ripple = PixelFxSpec.Of(PixelFxShape.Ring, FxFamily.Essence, 9f, 1f, 0.45f);
            ripple.Squash = 2f;
            ripple.Steps = 5;
            ripple.FadeTail = 0.5f;
            ripple.ZIndex = -1;
            pools.PlayFx(feet, ripple, FxOwner.World);
            pools.EmitSparks(feet, new SparkBurst
            {
                Family = FxFamily.Essence,
                Owner = FxOwner.World,
                Count = 4,
                Direction = Vector2.Up,
                Spread = 2.2f,
                SpeedMin = 25f,
                SpeedMax = 55f,
                LifeMin = 0.25f,
                LifeMax = 0.4f,
                Ballistic = true,
                Size = 1,
                Decorative = true,
            });
            return;
        }

        // Poussière soulevée derrière le pied : plus nette sur le béton que dans l'herbe.
        bool hard = terrain == TerrainType.Concrete;
        pools.EmitSparks(feet, new SparkBurst
        {
            Family = FxFamily.Stone,
            Owner = FxOwner.World,
            Count = hard ? 3 : 1,
            Direction = moveDirection == Vector2.Zero ? Vector2.Up : -moveDirection,
            Spread = 1.8f,
            SpeedMin = 8f,
            SpeedMax = hard ? 26f : 16f,
            LifeMin = 0.25f,
            LifeMax = 0.45f,
            Size = 1,
            Decorative = true,
        });
    }
}
