using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.Combat;

/// <summary>
/// Retours de la défense et des soins du joueur (plan 27 V3c) : bouclier qui casse en éclats de verre (distinct d'un
/// coup bloqué), bouclier de nouveau plein, armure qui pare une part notable d'un coup, coup ignoré dévié, soin qui
/// monte en étincelles. Information pour le joueur, ni attaque ni menace : soumis au seul réglage « Particules ». Les
/// sons (tintement, soin) restent à produire au plan 15. Classe simple possédée par le joueur, sans allocation.
/// </summary>
public sealed class PlayerDefenseFeedback
{
    private const float TorsoHeight = 10f;
    private ulong _nextHealMsec;

    public void ShieldBroken(Vector2 feet)
    {
        PlayerFeedbackConfig config = PlayerFeedbackConfig.Get();
        if (config == null)
            return;
        Burst(feet + new Vector2(0f, -TorsoHeight), config.ShieldFamily, Vector2.Zero, 12, 50f, 120f, 2, true);
        Ring(feet, config.ShieldFamily, 14f);
    }

    public void ShieldFull(Vector2 feet)
    {
        PlayerFeedbackConfig config = PlayerFeedbackConfig.Get();
        if (config == null)
            return;
        Burst(feet + new Vector2(0f, -TorsoHeight), config.ShieldFamily, Vector2.Up, 6, 15f, 35f, 1, false);
        Star(feet + new Vector2(0f, -TorsoHeight * 2f), config.ShieldFamily, 4f);
    }

    /// <summary>Armure : seulement quand elle retire une part notable d'un coup qui porte.</summary>
    public void ArmorAbsorbed(Vector2 feet, float damage, float afterArmor, Vector2 fromDirection)
    {
        PlayerFeedbackConfig config = PlayerFeedbackConfig.Get();
        float removed = damage - afterArmor;
        if (config == null || damage <= 0f || removed < config.ParryMinHp || removed / damage < config.ParryMinShare)
            return;
        Vector2 side = fromDirection == Vector2.Zero ? Vector2.Up : fromDirection;
        Vector2 point = feet + new Vector2(0f, -TorsoHeight) + side * 6f;
        Star(point, config.ParryFamily, 4f);
        Burst(point, config.ParryFamily, side, 4, 30f, 70f, 1, false);
    }

    public void Ignored(Vector2 feet)
    {
        Burst(feet + new Vector2(0f, -TorsoHeight), FxFamily.Stone, Vector2.Up, 3, 25f, 50f, 1, false);
    }

    /// <summary>Soin qui rend des PV, au plus une gerbe par intervalle (le vol de vie soigne quatre fois par seconde).</summary>
    public void Healed(Vector2 feet, float restored)
    {
        PlayerFeedbackConfig config = PlayerFeedbackConfig.Get();
        ulong now = Time.GetTicksMsec();
        if (config == null || restored <= 0f || now < _nextHealMsec)
            return;
        _nextHealMsec = now + (ulong)(config.HealMinInterval * 1000f);
        Burst(feet + new Vector2(0f, -4f), config.HealFamily, Vector2.Up, 6, 12f, 30f, 1, false);
    }

    private static void Burst(Vector2 point, FxFamily family, Vector2 direction, int count, float speedMin, float speedMax,
        int size, bool ballistic)
    {
        CombatPools.Instance?.Sparks.Emit(point, new SparkBurst
        {
            Family = family,
            Owner = FxOwner.World,
            Count = count,
            Direction = direction,
            Spread = direction == Vector2.Zero ? Mathf.Tau : 1.4f,
            SpeedMin = speedMin,
            SpeedMax = speedMax,
            LifeMin = 0.25f,
            LifeMax = 0.5f,
            Ballistic = ballistic,
            Size = size,
        });
    }

    private static void Star(Vector2 point, FxFamily family, float radius)
    {
        if (CombatPools.Instance == null)
            return;
        PixelFxSpec spec = PixelFxSpec.Of(PixelFxShape.Star, family, radius, 1f, 0.15f);
        spec.Steps = 3;
        spec.FadeTail = 0.4f;
        spec.ZIndex = 3;
        CombatPools.Instance.PlayFx(point, spec, FxOwner.World);
    }

    private static void Ring(Vector2 feet, FxFamily family, float radius)
    {
        if (CombatPools.Instance == null)
            return;
        PixelFxSpec spec = PixelFxSpec.Of(PixelFxShape.Ring, family, radius, 1f, 0.25f);
        spec.Squash = Core.Iso.GroundSquash;
        spec.Steps = 5;
        spec.FadeTail = 0.4f;
        spec.ZIndex = -1;
        CombatPools.Instance.PlayFx(feet, spec, FxOwner.World);
    }
}
