using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.Core;

/// <summary>Élan du joueur (plan 52) : avancé à chaque pas physique, ses paliers passent par les modificateurs de stats.</summary>
public partial class Player
{
    public PlayerElan Elan { get; private set; }

    private static readonly Color PerfectDodgeFlashColor = new(1f, 0.95f, 0.8f);

    private int _appliedElanTier;
    private int _lastDodgedDash = -1;

    /// <summary>Esquives parfaites de la run (affichage de réglage du bac à sable).</summary>
    public int PerfectDodges { get; private set; }

    private void InitializeElan()
    {
        Elan = new PlayerElan(ElanConfig.Load());
        PercussionMinTier = Elan.Config.PercussionMinTier;
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

    /// <summary>
    /// Vrai si le dash fait passer ce coup au travers. Le premier coup évité d'un dash est une esquive parfaite
    /// (plan 52 E2) : de l'Élan et une part de la recharge rendue. Tout ce qui frappe le joueur passe par ici, pour que
    /// l'esquive compte quelle que soit la source.
    /// </summary>
    public bool DodgesHit()
    {
        if (!Mobility.IsInvulnerable)
            return false;
        if (!Elan.Enabled || _lastDodgedDash == Mobility.DashesStarted)
            return true;
        _lastDodgedDash = Mobility.DashesStarted;
        PerfectDodges++;
        Elan.Gain(Elan.Config.PerfectDodgeGain, "esquive parfaite");
        Mobility.RefundCooldown(Elan.Config.PerfectDodgeCooldownRefund);
        ApplyElanTier();
        _mobilityFeedback?.PerfectDodge(Elan.Config.PerfectDodgeAudio);
        Flash(PerfectDodgeFlashColor, false, 0.12f);
        return true;
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
