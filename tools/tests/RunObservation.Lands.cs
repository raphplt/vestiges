using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Infrastructure;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// --capture-lands (plan 51) : une terre de bout en bout, en images. Le cercle d'un Mémorial en cours, le front qui ronge
/// les bords, le repaire aux lumières allumées, l'effondrement au réveil du gardien, la terre suivante ; les cartons
/// d'arrivée et la piste de run (plan 56 B3), l'alerte avant que le gardien se lève seul (plan 57 C2). Joueur
/// invincible, apparitions normales.
/// </summary>
public partial class RunObservation
{
    // --lands-bot (plan 51) : le bot joue la boucle des terres au lieu d'errer.
    private bool _landsBot;
    private const float CircleOrbitPx = 110f;
    // Orbite au sol autour d'un Mémorial tenu : dans le cercle même resserré (plan 57 C4, 55 % de 220 px).
    private const float MemorialOrbitPx = 90f;
    private const float ReachPx = 60f;
    private const float FightStancePx = 70f;
    private const float FacingReachPx = 24f;
    private const float FightStuckPx = 8f;
    private const double FightStuckSeconds = 3.0;
    private const float FightDetourPx = 160f;
    private const double FightDetourSeconds = 2.5;
    private const int FightStanceTries = 12;
    // Un Mémorial visé plus longtemps sans être atteint (eau, immeubles) est laissé pour la terre (plan 56 B1).
    private const double MemorialGiveUpSeconds = 60.0;
    private readonly System.Collections.Generic.HashSet<Memorial> _unreachableMemorials = new();
    private Memorial _memorialTarget;
    private double _memorialTargetSince;
    private int _memorialLand = -1;

    private GuardianConfig _guardianConfig;
    // Relevé du combat du gardien toutes les 10 s (plan 57 C2) : distance, PV, sol sous le bot, foule, dernier coup reçu.
    private double _nextFightLog;
    private bool _facingGuardian;
    private Vector2 _fightAnchor;
    private double _fightStillSince;
    private Vector2 _fightDetour;
    private double _fightDetourUntil;

    /// <summary>
    /// Cible du bot sur une terre, par priorité : le gardien levé, le repaire prêt, le cœur quand l'alerte annonce que le
    /// gardien va se lever seul (plan 57 C2 : sinon la terre s'effondre sous lui et il reste échoué hors du cœur), le
    /// cercle d'un Mémorial en cours (il y tourne), le Mémorial endormi le plus proche dont le sol tient encore, sinon le
    /// cœur. Rend vrai s'il a une cible.
    /// </summary>
    private bool TrySteerLand(double time, ref Vector2 waypoint)
    {
        GuardianDirector guardian = _world.GetNodeOrNull<GuardianDirector>("GuardianDirector");
        if (!_landsBot || guardian == null || guardian.IsDefeated)
            return false;
        Vector2 position = _player.GlobalPosition;
        if (guardian.IsFighting && guardian.Guardian is { IsActive: true } boss)
        {
            waypoint = FightStance(boss.GlobalPosition, position);
            // Arrivé à sa place, le bot marche vers le gardien jusqu'à son contact, puis y retourne : les armes qui tirent
            // droit devant (arc, lame) visent le sens de la marche, et immobile il garderait le dos tourné (plan 57 C2).
            float toBoss = position.DistanceTo(boss.GlobalPosition);
            if (position.DistanceTo(waypoint) < FacingReachPx)
                _facingGuardian = true;
            else if (toBoss < boss.BodyRadius + FacingReachPx)
                _facingGuardian = false;
            if (_facingGuardian)
                waypoint = boss.GlobalPosition;
            // Immobile au contact, sans toucher (mur, décor entre eux), il passe de l'autre côté du gardien.
            if (position.DistanceSquaredTo(_fightAnchor) > FightStuckPx * FightStuckPx)
            {
                _fightAnchor = position;
                _fightStillSince = time;
            }
            else if (time - _fightStillSince > FightStuckSeconds)
            {
                _fightDetour = boss.GlobalPosition + (boss.GlobalPosition - position).Normalized().Rotated(
                    (float)(time % 1.0 - 0.5)) * FightDetourPx;
                _fightDetourUntil = time + FightDetourSeconds;
                _fightStillSince = time;
            }
            if (time < _fightDetourUntil)
                waypoint = _fightDetour;
            if (time >= _nextFightLog)
            {
                _nextFightLog = time + 10.0;
                using CrowdQuery near = CrowdIndex.Near(position, 400f);
                GD.Print(string.Create(System.Globalization.CultureInfo.InvariantCulture,
                    $"[GuardianFight] t={time:F0} distance={position.DistanceTo(boss.GlobalPosition):F0} pv={boss.CurrentHp:F0}/{boss.MaxHp:F0} bot={position.X:F0},{position.Y:F0} gardien={boss.GlobalPosition.X:F0},{boss.GlobalPosition.Y:F0} vide_bot={_player.IsVoidAt(position)} place={waypoint.X:F0},{waypoint.Y:F0} foule400={near.Targets.Count} dernier_coup={boss.LastImpactDamage:F1}"));
            }
            return true;
        }
        GuardianLair lair = GuardianLair.Current;
        if (lair != null && lair.CanInteract)
        {
            waypoint = lair.GlobalPosition;
            if (position.DistanceTo(lair.GlobalPosition) < ReachPx)
                lair.Interact(_player);
            return true;
        }
        ErasureManager erasure = _world.GetNode<ErasureManager>("ErasureManager");
        if (lair != null && RageImminent(erasure))
        {
            waypoint = lair.GlobalPosition;
            return true;
        }
        for (int i = 0; i < Memorial.All.Count; i++)
        {
            if (Memorial.All[i].State != Memorial.MemorialState.Gathering)
                continue;
            waypoint = Memorial.All[i].GlobalPosition + Core.Iso.ToScreen(Vector2.FromAngle((float)time * 1.3f) * MemorialOrbitPx);
            return true;
        }
        int land = _world.GetNode<LandTransition>("LandTransition").LandIndex;
        if (land != _memorialLand)
        {
            _memorialLand = land;
            _unreachableMemorials.Clear();
            _memorialTarget = null;
        }
        Memorial target = null;
        float nearest = float.MaxValue;
        for (int i = 0; i < Memorial.All.Count; i++)
        {
            Memorial memorial = Memorial.All[i];
            float distance = position.DistanceTo(memorial.GlobalPosition);
            if (memorial.State == Memorial.MemorialState.Dormant && !_unreachableMemorials.Contains(memorial)
                && erasure.GetMemoryAt(memorial.GlobalPosition) > 0.3f && distance < nearest)
            {
                nearest = distance;
                target = memorial;
            }
        }
        if (target != _memorialTarget)
        {
            _memorialTarget = target;
            _memorialTargetSince = time;
        }
        else if (target != null && time - _memorialTargetSince > MemorialGiveUpSeconds)
        {
            GD.Print($"[RunObservation] Mémorial {target.GlobalPosition} hors d'atteinte du bot : laissé pour la terre {land}");
            _unreachableMemorials.Add(target);
            _memorialTarget = null;
        }
        if (target != null)
        {
            waypoint = target.GlobalPosition;
            if (nearest < ReachPx)
                GetNode<Core.EventBus>("/root/EventBus").EmitSignal(Core.EventBus.SignalName.MemorialInteracted, target);
            return true;
        }
        if (lair == null)
        {
            // Terre du late game : rien à raviver, le bot tient le cœur, qui ne tombe jamais, en y tournant.
            if (!guardian.IsLateGame)
                return false;
            waypoint = Vector2.FromAngle((float)time * 0.4f) * CircleOrbitPx;
            return true;
        }
        waypoint = lair.GlobalPosition;
        return true;
    }

    /// <summary>Le gardien se lèvera seul avant la fin de l'alerte, au rythme de l'oubli du moment (même calcul que l'objectif).</summary>
    private bool RageImminent(ErasureManager erasure)
    {
        _guardianConfig ??= GuardianConfig.Load(out _);
        LandFront front = erasure.Front;
        float rate = erasure.FrontRate;
        return front != null && rate > 0f && _guardianConfig != null
            && (front.Config.HeartReachedSeconds - front.Time) / rate <= _guardianConfig.RageWarningSeconds;
    }

    /// <summary>
    /// Place de combat : à portée du gardien, du côté du bot, sur le sol. Ses frappes ouvrent le vide sous le joueur ;
    /// les créatures n'y entrent pas et le bot, invincible, n'y toucherait plus rien (plan 56 B1). À défaut, le cœur.
    /// </summary>
    private Vector2 FightStance(Vector2 boss, Vector2 position)
    {
        float start = (position - boss).Angle();
        for (int i = 0; i < FightStanceTries; i++)
        {
            float offset = (i + 1) / 2 * (Mathf.Tau / FightStanceTries) * (i % 2 == 0 ? 1f : -1f);
            Vector2 candidate = boss + Vector2.FromAngle(start + offset) * FightStancePx;
            if (!_player.IsVoidAt(candidate))
                return candidate;
        }
        return Vector2.Zero;
    }

    /// <summary>Une image du cercle tenu : les vagues donnent de l'XP, l'écran de niveau est réglé au passage.</summary>
    private async Task CircleFrame(Vector2 held)
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (GetTree().Paused)
            AutoPickLevelUp();
        _player.GlobalPosition = held;
    }

    /// <summary>Attendre en jeu : la bénédiction du Mémorial ravivé et les niveaux sont choisis au passage.</summary>
    private async Task Settled(double seconds)
    {
        ulong until = Time.GetTicksMsec() + (ulong)(seconds * 1000.0);
        do
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            if (GetTree().Paused)
                AutoPickLevelUp();
        }
        while (Time.GetTicksMsec() < until);
    }

    private async Task CaptureLands()
    {
        _player.IsGodMode = true;
        // Le carton d'arrivée de la première terre et la piste de run (plan 56 B3).
        await Seconds(2.2);
        Save("lands-0-arrival.png");
        await Seconds(0.8);
        ErasureManager erasure = _world.GetNode<ErasureManager>("ErasureManager");
        GuardianDirector guardian = _world.GetNodeOrNull<GuardianDirector>("GuardianDirector");
        if (erasure.Front == null || guardian == null || Memorial.All.Count == 0)
        {
            GD.PushError("[RunObservation] --capture-lands : la run n'est pas en terres");
            return;
        }

        Memorial memorial = Memorial.All[0];
        _player.GlobalPosition = memorial.GlobalPosition + new Vector2(40f, 20f);
        GetNode<Core.EventBus>("/root/EventBus").EmitSignal(Core.EventBus.SignalName.MemorialInteracted, memorial);
        await Seconds(8.0);
        Save("lands-1-circle.png");
        // Plan 57 C4 : hors du cercle, l'anneau rougit ; au dernier tiers, il se resserre et la 2e vague s'annonce ; au bout,
        // l'onde du Mémorial ravivé.
        Vector2 held = _player.GlobalPosition;
        _player.GlobalPosition = memorial.GlobalPosition + new Vector2(LandmarkDataLoader.Memorial.CircleRadiusPx + 40f, 0f);
        await Seconds(0.6);
        Save("lands-1b-circle-outside.png");
        _player.GlobalPosition = held;
        // La 2e vague s'annonce au moment où le cercle commence à se resserrer.
        const System.Reflection.BindingFlags Private = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        MemorialDirector memorials = _world.GetNode<MemorialDirector>("MemorialDirector");
        System.Reflection.FieldInfo nextWave = typeof(MemorialDirector).GetField("_nextWave", Private);
        System.Reflection.FieldInfo waveIn = typeof(MemorialDirector).GetField("_waveIn", Private);
        while (_world.GetNodeOrNull("PoiContainer/MemorialCircle") is not null
               && ((int)nextWave.GetValue(memorials) < 2 || (float)waveIn.GetValue(memorials) > 0.6f))
            await CircleFrame(held);
        Save("lands-1c-circle-wave.png");
        // Le cercle disparaît à l'instant de l'onde.
        while (_world.GetNodeOrNull("PoiContainer/MemorialCircle") is not null)
            await CircleFrame(held);
        await Seconds(0.1);
        Save("lands-1d-burst.png");

        // Le front à mi-terre : le joueur posé sur une cellule qui s'effiloche.
        LandFront front = erasure.Front;
        front.Advance(front.Config.HeartReachedSeconds * 0.45f - front.Time);
        Vector2I fraying = default;
        foreach (Vector2I cell in front.Cells)
        {
            float memory = front.MemoryAt(cell);
            if (memory > 0.3f && memory < 0.45f)
            {
                fraying = cell;
                break;
            }
        }
        _player.GlobalPosition = erasure.CellCenterToWorld(fraying);
        await Settled(2.0);
        Save("lands-2-front.png");

        // Plan 57 C2 : la dernière minute avant que le gardien se lève seul, l'objectif rouge qui compte à rebours.
        front.Advance(front.Config.HeartReachedSeconds - 45f * erasure.FrontRate - front.Time);
        await Settled(1.5);
        Save("lands-2b-rage-soon.png");

        _world.GetNode<MemorialDirector>("MemorialDirector").SetLitForTests(3);
        GuardianLair lair = GuardianLair.Current;
        _player.GlobalPosition = lair.GlobalPosition + new Vector2(-70f, 30f);
        await Settled(2.0);
        Save("lands-3-lair.png");

        lair.Interact(_player);
        await Settled(2.0);
        Save("lands-4-guardian.png");

        // Ses coups (§16 G1) : une frappe annoncée au sol, puis la brèche de vide qu'elle laisse.
        ulong strikeDeadline = Time.GetTicksMsec() + 15_000;
        while (!MarkShown("GuardianSlamMarker") && Time.GetTicksMsec() < strikeDeadline)
            await Settled(0.0);
        await Settled(0.6);
        Save("lands-4b-strike.png");
        await Settled(1.2);
        Save("lands-4c-breach.png");
        // Le coup propre au gardien de la terre, s'il en a un : la faux de l'Épouvantail et sa brèche (plan 57 G2).
        ulong sweepDeadline = Time.GetTicksMsec() + 20_000;
        while (!MarkShown("GuardianSweepMarker") && Time.GetTicksMsec() < sweepDeadline)
            await Settled(0.0);
        if (MarkShown("GuardianSweepMarker"))
        {
            await Settled(0.5);
            Save("lands-4d-sweep.png");
            ulong releaseDeadline = Time.GetTicksMsec() + 5_000;
            while (MarkShown("GuardianSweepMarker") && Time.GetTicksMsec() < releaseDeadline)
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await Settled(0.1);
            Save("lands-4e-sweep-strike.png");
        }

        // Un Rampant enfoui ne prend aucun dégât : le coup est rejoué jusqu'à ce qu'il tombe.
        for (int frame = 0; frame < 600 && guardian.IsFighting; frame++)
        {
            guardian.Guardian?.TakeDamage(guardian.Guardian.MaxHp * 10f);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        LandTransition transition = _world.GetNode<LandTransition>("LandTransition");
        int land = transition.LandIndex;
        ulong deadline = Time.GetTicksMsec() + 30_000;
        while ((transition.LandIndex == land || transition.IsChanging) && Time.GetTicksMsec() < deadline)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            // L'XP du gardien fait monter de niveau : le bot choisit, le passage suit.
            if (GetTree().Paused && !transition.IsChanging)
                AutoPickLevelUp();
        }
        // La recousure (§16 R1) : la terre neuve monte du Néant autour du joueur.
        // Le voile de chargement s'efface en deux secondes environ, puis la terre monte en stitch_seconds.
        await Seconds(1.5);
        Save("lands-5a-void.png");
        await Seconds(1.0);
        Save("lands-5b-stitch.png");
        await Seconds(3.5);
        Save("lands-6-next.png");
        int next = transition.LandIndex;

        // La terre du late game (§16 L2) et son premier écho de gardien.
        await transition.GoToLandAsync(GuardianConfig.Load(out _).LandsToWin);
        await Seconds(2.5);
        Save("lands-6b-last-arrival.png");
        await Seconds(0.5);
        typeof(GuardianDirector).GetField("_echoIn", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(guardian, 0f);
        await Seconds(2.5);
        Save("lands-7-echo.png");
        GD.Print($"[RunObservation] RESULT lands terre {land} → {next} → {transition.LandIndex}, écho {guardian.Guardian?.DisplayName}");
    }

    private static bool MarkShown(string prefix)
    {
        foreach (Combat.Abilities.GroundTelegraph mark in Combat.Abilities.GroundTelegraph.Shown)
        {
            if (mark.Name.ToString().StartsWith(prefix, System.StringComparison.Ordinal))
                return true;
        }
        return false;
    }
}
