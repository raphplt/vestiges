using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// --capture-lands (plan 51) : une terre de bout en bout, en images. Le cercle d'un Mémorial en cours, le front qui ronge
/// les bords, le repaire aux lumières allumées, l'effondrement au réveil du gardien, la terre suivante ; les cartons
/// d'arrivée et la piste de run (plan 56 B3). Joueur
/// invincible, apparitions normales.
/// </summary>
public partial class RunObservation
{
    // --lands-bot (plan 51) : le bot joue la boucle des terres au lieu d'errer.
    private bool _landsBot;
    private const float CircleOrbitPx = 110f;
    private const float ReachPx = 60f;
    private const float FightStancePx = 70f;
    private const int FightStanceTries = 12;
    // Un Mémorial visé plus longtemps sans être atteint (eau, immeubles) est laissé pour la terre (plan 56 B1).
    private const double MemorialGiveUpSeconds = 60.0;
    private readonly System.Collections.Generic.HashSet<Memorial> _unreachableMemorials = new();
    private Memorial _memorialTarget;
    private double _memorialTargetSince;
    private int _memorialLand = -1;

    /// <summary>
    /// Cible du bot sur une terre, par priorité : le gardien levé, le repaire prêt, le cercle d'un Mémorial en cours (il
    /// y tourne), le Mémorial endormi le plus proche dont le sol tient encore, sinon le cœur. Rend vrai s'il a une cible.
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
        for (int i = 0; i < Memorial.All.Count; i++)
        {
            if (Memorial.All[i].State != Memorial.MemorialState.Gathering)
                continue;
            waypoint = Memorial.All[i].GlobalPosition + Vector2.FromAngle((float)time * 1.3f) * CircleOrbitPx;
            return true;
        }
        ErasureManager erasure = _world.GetNode<ErasureManager>("ErasureManager");
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
        await Seconds(2.0);
        Save("lands-2-front.png");

        _world.GetNode<MemorialDirector>("MemorialDirector").SetLitForTests(3);
        GuardianLair lair = GuardianLair.Current;
        _player.GlobalPosition = lair.GlobalPosition + new Vector2(-70f, 30f);
        await Seconds(2.0);
        Save("lands-3-lair.png");

        lair.Interact(_player);
        await Seconds(2.0);
        Save("lands-4-guardian.png");

        // Ses coups (§16 G1) : une frappe annoncée au sol, puis la brèche de vide qu'elle laisse.
        ulong strikeDeadline = Time.GetTicksMsec() + 15_000;
        while (!SlamShown() && Time.GetTicksMsec() < strikeDeadline)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await Seconds(0.6);
        Save("lands-4b-strike.png");
        await Seconds(1.2);
        Save("lands-4c-breach.png");

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

    private static bool SlamShown()
    {
        foreach (Combat.Abilities.GroundTelegraph mark in Combat.Abilities.GroundTelegraph.Shown)
        {
            if (mark.Name.ToString().StartsWith("GuardianSlamMarker", System.StringComparison.Ordinal))
                return true;
        }
        return false;
    }
}
