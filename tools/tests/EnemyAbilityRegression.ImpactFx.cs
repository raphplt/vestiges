using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;

namespace Vestiges.Tests;

/// <summary>Plan 27 V2c : un projectile du joueur arrivé en bout de course éclate au lieu de disparaître d'un coup.</summary>
public partial class EnemyAbilityRegression
{
    private async Task RunImpactFxChecks()
    {
        CombatPools pools = new() { Name = "CombatPools" };
        AddChild(pools);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        int before = VisibleFx(pools);
        _player.OnProjectileSpent(_player.Position + new Vector2(120f, 0f), 5f, default, FxFamily.Essence, Vector2.Right);
        Check(VisibleFx(pools) == before + 1, $"Fin de course : éclat joué ({VisibleFx(pools) - before} effet)");

        bool attackFx = CombatFxSettings.PlayerAttackFx;
        CombatFxSettings.PlayerAttackFx = false;
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        before = VisibleFx(pools);
        _player.OnProjectileSpent(_player.Position + new Vector2(120f, 0f), 5f, default, FxFamily.Essence, Vector2.Right);
        Check(VisibleFx(pools) == before, "Fin de course : rien avec les effets d'attaque coupés");
        CombatFxSettings.PlayerAttackFx = attackFx;
        pools.QueueFree();
    }
}
