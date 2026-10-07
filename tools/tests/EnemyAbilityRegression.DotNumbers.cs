using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;

namespace Vestiges.Tests;

/// <summary>
/// Plan 27 V2a : brûlure et saignement sortent en un chiffre par statut toutes les 0,5 s, jamais fondu avec les chiffres
/// des coups ; la part fractionnaire attend, le reliquat sort à la mort.
/// </summary>
public partial class EnemyAbilityRegression
{
    private async Task RunDotNumberChecks()
    {
        CombatPools pools = new() { Name = "CombatPools" };
        AddChild(pools);
        Enemy enemy = await SpawnReady("rodeur", new Vector2(90f, 0f));
        await Step(40);

        enemy.ApplyIgnite(12f, 3f);
        await Step(28);
        Check(VisibleNumbers(pools, out string texts) == 0, $"Brûlure : rien avant l'intervalle ({texts})");
        await Step(3);
        Check(VisibleNumbers(pools, out texts) == 1 && texts == "6", $"Brûlure : un chiffre de 6 après 0,5 s à 12/s ({texts})");
        enemy.TakeDamage(3f);
        Check(VisibleNumbers(pools, out texts) == 2 && texts.Contains('3'), $"Coup pendant la brûlure : chiffre à part ({texts})");
        await Step(120);

        Enemy slow = await SpawnReady("rodeur", new Vector2(-90f, 0f));
        await Step(40);
        slow.ApplyBleed(1f, 3f);
        await Step(31);
        Check(VisibleNumbers(pools, out texts) == 0, $"Saignement à 1/s : la demi-unité attend ({texts})");
        await Step(30);
        Check(VisibleNumbers(pools, out texts) == 1 && texts == "1", $"Saignement à 1/s : un chiffre de 1 après 1 s ({texts})");
        await Step(120);

        slow.ApplyBleed(1f, 3f);
        await Step(20);
        slow.TakeDamage(1000000f);
        Check(VisibleNumbers(pools, out texts) >= 1, $"Mort : chiffres affichés, reliquat compris ({texts})");
        Despawn(enemy);
        Despawn(slow);
        await Step(60);
        pools.QueueFree();
    }
}
