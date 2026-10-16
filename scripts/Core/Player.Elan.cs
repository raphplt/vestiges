using Vestiges.Infrastructure;

namespace Vestiges.Core;

/// <summary>Élan du joueur (plan 52) : avancé à chaque pas physique, ses paliers passent par les modificateurs de stats.</summary>
public partial class Player
{
    public PlayerElan Elan { get; private set; }

    private int _appliedElanTier;

    private void InitializeElan()
    {
        Elan = new PlayerElan(ElanConfig.Load());
    }

    /// <summary>Allume ou éteint l'Élan (bac à sable, réglage) ; éteint, ses bonus sont retirés.</summary>
    public void SetElanEnabled(bool enabled)
    {
        Elan.SetEnabled(enabled);
        ApplyElanTier();
        _mobilityFeedback?.SetElan(Elan.Value, Elan.Tier, Elan.Config.Tiers);
    }

    /// <param name="movementSpeed">Vitesse réellement parcourue à ce pas.</param>
    private void StepElan(float delta, float movementSpeed)
    {
        if (!Elan.Enabled)
            return;
        float runSpeed = Speed * _speedMultiplier * _erasurePenalty.Speed;
        Elan.Step(delta, runSpeed > 0f ? movementSpeed / runSpeed : 0f);
        ApplyElanTier();
        _mobilityFeedback.SetElan(Elan.Value, Elan.Tier, Elan.Config.Tiers);
    }

    private void LoseElanOnHit()
    {
        Elan.Hit();
        ApplyElanTier();
    }

    /// <summary>Seul le changement de palier touche les stats, pour ne pas relancer les minuteurs d'armes à chaque pas.</summary>
    private void ApplyElanTier()
    {
        if (Elan.Tier == _appliedElanTier)
            return;
        int previous = _appliedElanTier;
        _appliedElanTier = Elan.Tier;
        ApplyPerkModifier("speed", Elan.SpeedAt(_appliedElanTier) / Elan.SpeedAt(previous), "multiplicative");
        ApplyPerkModifier("damage", Elan.DamageAt(_appliedElanTier) / Elan.DamageAt(previous), "multiplicative");
    }
}
