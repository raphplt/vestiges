using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Combat.Abilities;
using Vestiges.Infrastructure;
using Vestiges.Spawn;

namespace Vestiges.Tests;

/// <summary>
/// --capture-bile : trois Cracheurs autour du joueur immobile crachent ensemble ; gros plans du geste, de la goutte en
/// cloche, de l'impact et de la flaque (plan 33).
/// </summary>
public partial class RunObservation
{
    private static readonly Vector2 BileFrame = new(230f, 130f);
    private static readonly Vector2[] BileSpots = { new(-190f, -30f), new(170f, -70f), new(20f, -150f) };
    private static readonly int[] BileCaptureFrames = { 8, 24, 40, 62, 75, 110, 170, 230 };

    private async Task CaptureBileLob()
    {
        await PrepareCloseUpScene();
        await MoveToOpenGround(BileFrame, Argument(OS.GetCmdlineUserArgs(), "--status-biome", null));
        SpawnManager spawner = _world.GetNode<SpawnManager>("SpawnManager");
        Vector2 center = _player.GlobalPosition + new Vector2(0f, -50f);
        foreach (Vector2 spot in BileSpots)
            spawner.ForceSpawnEnemy("fading_spitter", _player.GlobalPosition + spot);
        await Frames(2);

        FieldInfo hp = typeof(Enemy).GetField("_currentHp", BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo cache = typeof(Enemy).GetField("_abilityCache", BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo cooldown = typeof(BileLobAbility).GetField("_cooldownTimer", BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (Vector2 spot in BileSpots)
        {
            Enemy spitter = NearestEnemy(_player.GlobalPosition + spot);
            hp.SetValue(spitter, 100000f);
            var abilities = (Dictionary<EnemyAbilityKind, IEnemyAbility>)cache.GetValue(spitter);
            cooldown.SetValue(abilities[EnemyAbilityKind.BileLob], 0f);
        }

        int elapsed = 0;
        for (int shot = 0; shot < BileCaptureFrames.Length; shot++)
        {
            await Frames(BileCaptureFrames[shot] - elapsed);
            elapsed = BileCaptureFrames[shot];
            SaveCloseUp($"{_output}/bile-{shot}.png", center, BileFrame);
        }
        GD.Print($"[RunObservation] RESULT bile cracheurs={BileSpots.Length} images={BileCaptureFrames.Length} dossier={_output}");
    }
}
