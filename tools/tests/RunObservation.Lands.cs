using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// --capture-lands (plan 51) : une terre de bout en bout, en images. Le cercle d'un Mémorial en cours, le front qui ronge
/// les bords, le repaire aux lumières allumées, l'effondrement au réveil du gardien, la terre suivante. Joueur
/// invincible, apparitions normales.
/// </summary>
public partial class RunObservation
{
    // --lands-bot (plan 51) : le bot joue la boucle des terres au lieu d'errer.
    private bool _landsBot;
    private const float CircleOrbitPx = 110f;
    private const float ReachPx = 60f;

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
            waypoint = boss.GlobalPosition + (position - boss.GlobalPosition).Normalized() * 70f;
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
        Memorial target = null;
        float nearest = float.MaxValue;
        for (int i = 0; i < Memorial.All.Count; i++)
        {
            Memorial memorial = Memorial.All[i];
            float distance = position.DistanceTo(memorial.GlobalPosition);
            if (memorial.State == Memorial.MemorialState.Dormant && erasure.GetMemoryAt(memorial.GlobalPosition) > 0.3f && distance < nearest)
            {
                nearest = distance;
                target = memorial;
            }
        }
        if (target != null)
        {
            waypoint = target.GlobalPosition;
            if (nearest < ReachPx)
                GetNode<Core.EventBus>("/root/EventBus").EmitSignal(Core.EventBus.SignalName.MemorialInteracted, target);
            return true;
        }
        if (lair == null)
            return false;
        waypoint = lair.GlobalPosition;
        return true;
    }

    private async Task CaptureLands()
    {
        _player.IsGodMode = true;
        await Seconds(3.0);
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

        guardian.Guardian?.TakeDamage(guardian.Guardian.MaxHp * 10f);
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
        await Seconds(3.0);
        Save("lands-5-next.png");
        GD.Print($"[RunObservation] RESULT lands terre {land} → {transition.LandIndex}");
    }
}
