using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Tests;

/// <summary>
/// --capture-held [--weapons id1,id2] : arme en main (plan 17 lot 2C). Pour chaque arme équipée seule, le joueur
/// marche dans les huit directions ; un gros plan par direction, puis un gros plan pendant un coup.
/// </summary>
public partial class RunObservation
{
    private static readonly Vector2[] HeldDirections =
    {
        Vector2.Right, new(1, 1), Vector2.Down, new(-1, 1), Vector2.Left, new(-1, -1), Vector2.Up, new(1, -1),
    };

    private async Task CaptureHeldWeapons(string weaponList)
    {
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        CombatFxSettings.HeldWeapon = true;
        await Frames(90);
        MethodInfo attack = typeof(Player).GetMethod("OnWeaponAttackTimeout", BindingFlags.NonPublic | BindingFlags.Instance);
        string[] ids = (weaponList ?? "chipped_blade,heavy_hammer,makeshift_bow,crossbow,music_box,memory_lantern").Split(',');
        foreach (string id in ids)
        {
            WeaponData weapon = WeaponDataLoader.Get(id);
            if (weapon == null)
            {
                GD.PushError($"[RunObservation] Arme inconnue : {id}");
                continue;
            }
            while (_player.WeaponSlots.Count > 0)
                _player.RemoveWeapon(0);
            _player.AddWeapon(weapon);
            for (int index = 0; index < HeldDirections.Length; index++)
            {
                _player.AIInputOverride = HeldDirections[index].Normalized();
                await Frames(14);
                SavePlayerCloseUp($"{_output}/held-{id}-{index}-{CharacterFacing.DirectionNames[index]}.png", new Vector2(40f, 36f));
            }
            _player.AIInputOverride = Vector2.Zero;
            await Frames(10);
            attack.Invoke(_player, new object[] { 0 });
            await Frames(2);
            SavePlayerCloseUp($"{_output}/held-{id}-coup.png", new Vector2(40f, 36f));
        }
        GD.Print($"[RunObservation] RESULT held weapons={ids.Length}");
    }
}
