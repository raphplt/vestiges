using Godot;
using Vestiges.Combat;
using Vestiges.Infrastructure;

namespace Vestiges.Core;

/// <summary>
/// Élan (plan 52) : la Série, combo d'éliminations dont les paliers passent par les modificateurs de stats, et
/// l'esquive parfaite. Allumer l'Élan allume aussi le saut du vide, la percussion et le vide comme arme.
/// </summary>
public partial class Player
{
    public PlayerStreak Streak { get; private set; }

    private static readonly Color PerfectDodgeFlashColor = new(1f, 0.95f, 0.8f);

    private int _appliedStreakTier;
    private int _lastDodgedDash = -1;
    private StreakCounter _streakCounter;

    /// <summary>Esquives parfaites de la run (affichage de réglage du bac à sable).</summary>
    public int PerfectDodges { get; private set; }

    private void InitializeElan()
    {
        Streak = new PlayerStreak(ElanConfig.Load());
        PercussionMinTier = Streak.Config.PercussionMinTier;
    }

    /// <summary>Allume ou éteint l'Élan (bac à sable, réglage) ; éteint, les bonus de la Série sont retirés.</summary>
    public void SetElanEnabled(bool enabled)
    {
        Streak.SetEnabled(enabled);
        ApplyStreakTier();
        ShowStreak();
    }

    private void StepStreak(float delta)
    {
        if (!Streak.Enabled)
            return;
        Streak.Step(delta);
        ApplyStreakTier();
        ShowStreak();
    }

    /// <summary>
    /// Les éliminations du joueur nourrissent la Série, une chute dans le Néant en vaut plusieurs. Ne comptent pas : une
    /// explosion de créature ou un dégât d'événement (sans propriétaire), ni une partie de boss.
    /// </summary>
    private void OnStreakKill(EnemyKillResult kill)
    {
        if (!Streak.Enabled || kill.Damage.Source.OwnerId != GetInstanceId() || kill.EnemyId == EnemyGrammar.BossPartId)
            return;
        bool fall = kill.Damage.Source.Kind == DamageKind.Void;
        Streak.Kill(fall ? Streak.Config.StreakFallWeight : 1, fall ? "chute dans le vide" : "élimination");
        ApplyStreakTier();
        ShowStreak();
    }

    /// <summary>
    /// Vrai si le dash fait passer ce coup au travers. Le premier coup évité d'un dash est une esquive parfaite
    /// (plan 52 E2) : la fenêtre de la Série repart et une part de la recharge est rendue. Tout ce qui frappe le joueur
    /// passe par ici, pour que l'esquive compte quelle que soit la source.
    /// </summary>
    public bool DodgesHit()
    {
        if (!Mobility.IsInvulnerable)
            return false;
        if (!Streak.Enabled || _lastDodgedDash == Mobility.DashesStarted)
            return true;
        _lastDodgedDash = Mobility.DashesStarted;
        PerfectDodges++;
        Streak.Refresh("esquive parfaite");
        Mobility.RefundCooldown(Streak.Config.PerfectDodgeCooldownRefund);
        _mobilityFeedback?.PerfectDodge(Streak.Config.PerfectDodgeAudio);
        Flash(PerfectDodgeFlashColor, false, 0.12f);
        return true;
    }

    private void LoseStreakOnHit()
    {
        Streak.Hit();
        ApplyStreakTier();
        ShowStreak();
    }

    private void ShowStreak()
    {
        float window = Streak.Count > 0 ? Streak.WindowRemaining / Streak.Config.StreakWindowSeconds : 0f;
        _mobilityFeedback?.SetStreak(window, Streak.Tier);
        // Le compteur n'existe qu'une fois la Série allumée : une run normale n'a aucun nœud de plus.
        if (_streakCounter == null && Streak.Enabled)
        {
            _streakCounter = new StreakCounter { Name = "StreakCounter" };
            AddChild(_streakCounter);
        }
        _streakCounter?.SetStreak(Streak.Count, Streak.Tier);
    }

    /// <summary>Seul le changement de palier touche les stats, pour ne pas relancer les minuteurs d'armes à chaque élimination.</summary>
    private void ApplyStreakTier()
    {
        if (Streak.Tier == _appliedStreakTier)
            return;
        int previous = _appliedStreakTier;
        _appliedStreakTier = Streak.Tier;
        ApplyPerkModifier("speed", Streak.SpeedAt(_appliedStreakTier) / Streak.SpeedAt(previous), "multiplicative");
        ApplyPerkModifier("damage", Streak.DamageAt(_appliedStreakTier) / Streak.DamageAt(previous), "multiplicative");
        ApplyPerkModifier("luck", Streak.LuckAt(_appliedStreakTier) - Streak.LuckAt(previous), "additive");
    }
}
