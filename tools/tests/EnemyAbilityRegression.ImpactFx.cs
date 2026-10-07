using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;

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

        // Plan 27 V3a : un coup situé donne sa direction au résultat, et son chiffre s'affiche au-dessus du joueur.
        await WaitHurtRecovery();
        int numbers = VisibleNumbers(pools, out _);
        PlayerDamageResult hit = _player.TakeDamage(5f, _player.GlobalPosition + new Vector2(50f, 0f));
        Check(hit.Applied && hit.FromDirection.IsEqualApprox(Vector2.Right), $"Blessure : direction du coup ({hit.FromDirection})");
        Check(VisibleNumbers(pools, out string texts) == numbers + 1 && texts.Contains("−5"), $"Blessure : chiffre des dégâts reçus ({texts})");
        await WaitHurtRecovery();
        pools.QueueFree();
    }
}
