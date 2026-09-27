using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Spawn;

namespace Vestiges.Tests;

/// <summary>
/// --capture-crowd : « rouler sur la game » (plan 02 J5). Soixante créatures autour du joueur : capture avant et après le
/// recul de caméra ; puis, cibles à 1 PV et XP symbolique, le marteau frappe en boucle pour faire monter le compteur de
/// morts en rafale.
/// </summary>
public partial class RunObservation
{
    private async Task CaptureCrowd()
    {
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        SpawnManager spawner = _world.GetNode<SpawnManager>("SpawnManager");
        await Frames(90);
        _player.AIInputOverride = Vector2.Zero;
        Save("crowd-0-empty.png");

        Vector2 origin = _player.GlobalPosition;
        for (int index = 0; index < 60; index++)
            spawner.ForceSpawnEnemy("rodeur", origin + Vector2.FromAngle(index * 2.4f) * (70f + index * 3.5f));
        await Frames(2);
        Save("crowd-1-spawned.png");
        Vector2 zoomBefore = _camera.Zoom;
        await Seconds(3.0);
        Save("crowd-2-pulled-back.png");
        GD.Print($"[RunObservation] RESULT crowd zoom {zoomBefore.X:F3} -> {_camera.Zoom.X:F3}");

        FieldInfo hp = typeof(Enemy).GetField("_currentHp", BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo xp = typeof(Enemy).GetField("_xpReward", BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
        {
            if (node is not Enemy enemy || !enemy.IsActive)
                continue;
            hp.SetValue(enemy, 1f);
            xp.SetValue(enemy, 0.01f);
        }
        while (_player.WeaponSlots.Count > 0)
            _player.RemoveWeapon(0);
        _player.AddWeapon(WeaponDataLoader.Get("heavy_hammer"));
        MethodInfo attack = typeof(Player).GetMethod("OnWeaponAttackTimeout", BindingFlags.NonPublic | BindingFlags.Instance);
        for (int swing = 0; swing < 8; swing++)
        {
            // Le joueur avance dans la foule entre deux coups pour atteindre de nouvelles cibles.
            _player.GlobalPosition = origin + Vector2.FromAngle(swing * 0.8f) * (40f + swing * 18f);
            attack.Invoke(_player, new object[] { 0 });
            await Frames(3);
            if (swing % 2 == 1)
                Save($"crowd-3-streak-{swing / 2}.png");
        }
        GD.Print("[RunObservation] RESULT crowd captured");
    }
}
