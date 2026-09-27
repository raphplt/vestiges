using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;

namespace Vestiges.Tests;

/// <summary>
/// --check-orb-sleep : une orbe d'XP laissée loin s'endort, puis se réveille et se laisse ramasser quand le joueur
/// revient ; l'XP gagnée est exactement celle semée. Ligne RESULT avec failures=.
/// </summary>
public partial class RunObservation
{
    private async Task CheckOrbSleep()
    {
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        _player.AIInputOverride = Vector2.Zero;
        EventBus eventBus = GetNode<EventBus>("/root/EventBus");
        float gained = 0f;
        EventBus.XpGainedEventHandler onXp = amount => gained += amount;
        eventBus.XpGained += onXp;
        await Frames(30);
        int before = CombatPools.Instance.XpOrbsOnGround;
        Vector2 far = _player.GlobalPosition + new Vector2(1400f, 0f);
        CombatPools.Instance.SpawnXpOrb(far, 7f);
        await Frames(30);
        int failures = 0;
        XpOrb orb = FindOrbNear(far);
        bool asleep = orb is { IsAsleep: true };
        if (!asleep)
            failures++;
        // Le joueur revient près de l'orbe : la ronde de CombatPools la réveille, l'attraction la ramène.
        _player.GlobalPosition = far + new Vector2(-120f, 0f);
        await Seconds(2.0);
        bool collected = CombatPools.Instance.XpOrbsOnGround == before;
        if (!collected)
            failures++;
        if (!Mathf.IsEqualApprox(gained, 7f))
            failures++;
        eventBus.XpGained -= onXp;
        GD.Print($"[RunObservation] RESULT orb_sleep asleep_far={asleep} collected={collected} xp={gained} failures={failures}");
    }

    private XpOrb FindOrbNear(Vector2 position)
    {
        foreach (Node child in CombatPools.Instance.GetChildren())
        {
            if (child is XpOrb orb && orb.Visible && orb.GlobalPosition.DistanceTo(position) < 40f)
                return orb;
        }
        return null;
    }
}
