using Godot;
using Vestiges.Combat;
using Vestiges.Infrastructure;

namespace Vestiges.Core;

/// <summary>
/// Élan (plan 52) : la Série, combo d'éliminations dont les rangs passent par les modificateurs de stats, et
/// l'esquive parfaite. Allumer l'Élan allume aussi le saut du vide, la percussion et le vide comme arme.
/// </summary>
public partial class Player
{
    public PlayerStreak Streak { get; private set; }

    private static readonly Color PerfectDodgeFlashColor = new(1f, 0.95f, 0.8f);

    private int _appliedStreakRank;
    private int _lastDodgedDash = -1;
    private StreakFeedback _streakFeedback;
    private Color? _baseOutline;

    /// <summary>Esquives parfaites de la run (affichage de réglage du bac à sable).</summary>
    public int PerfectDodges { get; private set; }

    private void InitializeElan()
    {
        Streak = new PlayerStreak(ElanConfig.Load());
        PercussionMinRank = Streak.Config.PercussionMinRank;
    }

    /// <summary>Allume ou éteint l'Élan (bac à sable, réglage) ; éteint, les bonus de la Série sont retirés.</summary>
    public void SetElanEnabled(bool enabled)
    {
        int count = Streak.Count, rank = Streak.Rank;
        Streak.SetEnabled(enabled);
        ApplyStreakRank();
        if (!enabled && count > 0)
            PublishStreak(StreakChangeKind.Reset, count, rank, 0);
    }

    private void StepStreak(float delta)
    {
        if (!Streak.Enabled)
            return;
        int count = Streak.Count, rank = Streak.Rank;
        int essence = Streak.Step(delta);
        // Chaque pas : un rang changé hors de ce fichier (bac à sable, banc) touche aussi les stats ; sans changement,
        // ce n'est qu'une comparaison d'entiers.
        ApplyStreakRank();
        if (essence >= 0)
            PublishStreak(StreakChangeKind.CashOut, count, rank, essence);
        _streakFeedback?.Step(delta);
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
        AddStreakKills(fall ? Streak.Config.StreakFallWeight : 1, fall ? "chute dans le vide" : "élimination");
    }

    /// <summary>Fait monter la Série comme des éliminations, publiée comme elles (bac à sable, captures).</summary>
    public void AddStreakKills(int weight, string source)
    {
        if (!Streak.Enabled)
            return;
        int count = Streak.Count, rank = Streak.Rank;
        Streak.Kill(weight, source);
        ApplyStreakRank();
        PublishStreak(StreakChangeKind.Gain, count, rank, 0);
    }

    /// <summary>Un coup d'arme porté à un gardien ou un mini-boss relance la fenêtre : un long combat ne casse pas la Série.</summary>
    internal void RefreshStreakOnBossHit() => Streak.Refresh("coup au gardien");

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
        if (!Streak.Enabled || Streak.Count == 0)
            return;
        int count = Streak.Count, rank = Streak.Rank;
        Streak.Hit();
        ApplyStreakRank();
        PublishStreak(StreakChangeKind.Hit, count, rank, 0);
    }

    /// <summary>Le joueur montre d'abord le changement autour de lui (sons, aura, onde), puis le publie pour le HUD et l'Essence.</summary>
    private void PublishStreak(StreakChangeKind kind, int previousCount, int previousRank, int essence)
    {
        StreakChange change = new(kind, Streak.Count, Streak.Rank, previousCount, previousRank, essence, GlobalPosition);
        // Les retours n'existent qu'une fois la Série allumée : une run où elle est éteinte n'a aucun nœud de plus.
        if (_streakFeedback == null && Streak.Enabled)
        {
            _streakFeedback = new StreakFeedback { Name = "StreakFeedback" };
            _streakFeedback.Setup(Streak);
            AddChild(_streakFeedback);
        }
        _streakFeedback?.Present(change);
        _eventBus?.PublishStreakChange(change);
    }

    /// <summary>Seul le changement de rang touche les stats, pour ne pas relancer les minuteurs d'armes à chaque élimination.</summary>
    private void ApplyStreakRank()
    {
        if (Streak.Rank == _appliedStreakRank)
            return;
        int previous = _appliedStreakRank;
        _appliedStreakRank = Streak.Rank;
        ApplyPerkModifier("speed", Streak.SpeedAt(_appliedStreakRank) / Streak.SpeedAt(previous), "multiplicative");
        ApplyPerkModifier("damage", Streak.DamageAt(_appliedStreakRank) / Streak.DamageAt(previous), "multiplicative");
        ApplyPerkModifier("luck", Streak.LuckAt(_appliedStreakRank) - Streak.LuckAt(previous), "additive");
        ShowStreakRankOnBody();
    }

    /// <summary>Nouveau matériau du personnage : son contour devient la base, et le rang en cours s'y applique.</summary>
    private void OnSpriteMaterialCreated()
    {
        _baseOutline = null;
        if (Streak != null)
            ShowStreakRankOnBody();
    }

    /// <summary>Contour à la couleur du rang dès FURIE, images rémanentes continues à MONSTRE.</summary>
    private void ShowStreakRankOnBody()
    {
        StreakFeedbackSettings feedback = Streak.Config.Feedback;
        _mobilityFeedback?.SetAfterimages(_appliedStreakRank >= feedback.AfterimagesFromRank);
        if (!_hasSprite || _spriteMaterial == null)
            return;
        _baseOutline ??= _spriteMaterial.GetShaderParameter("outline_color").AsColor();
        _spriteMaterial.SetShaderParameter("outline_color",
            _appliedStreakRank >= feedback.OutlineFromRank ? Streak.ColorAt(_appliedStreakRank) : _baseOutline.Value);
    }
}
