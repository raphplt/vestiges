using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Infrastructure;

namespace Vestiges.Tests;

/// <summary>
/// --capture-weapon-pickup : arme au sol ramassée d'elle-même (vol de l'icône vers le HUD, trois vues), puis, les
/// quatre emplacements pris, invite d'échange et échange. Ligne RESULT avec l'état des emplacements.
/// </summary>
public partial class RunObservation
{
    private async Task CaptureWeaponPickup()
    {
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        _player.AIInputOverride = Vector2.Zero;
        await Frames(90);
        int slotsBefore = _player.WeaponSlots.Count;

        WeaponPickup first = new();
        first.Initialize(WeaponDataLoader.Get("crossbow"), _player.GlobalPosition + new Vector2(70f, 30f));
        _world.AddChild(first);
        await Frames(30);
        SaveFrame("pickup-0-ground");
        _player.GlobalPosition = first.GlobalPosition;
        await Frames(6);
        SaveFrame("pickup-1-flight");
        await Frames(12);
        SaveFrame("pickup-2-flight");
        await Frames(30);
        SaveFrame("pickup-3-arrived");
        int slotsAfterAuto = _player.WeaponSlots.Count;

        while (_player.WeaponSlots.Count < Vestiges.Core.Player.MaxWeaponSlots)
            _player.AddWeapon(WeaponDataLoader.Get(_player.WeaponSlots.Count % 2 == 0 ? "sling" : "throwing_axes"));
        WeaponPickup second = new();
        second.Initialize(WeaponDataLoader.Get("music_box"), _player.GlobalPosition + new Vector2(20f, 10f));
        _world.AddChild(second);
        await Frames(40);
        SaveFrame("pickup-4-swap-prompt");
        bool stillOnGround = IsInstanceValid(second) && !second.IsQueuedForDeletion();
        second.Interact(_player);
        await Frames(40);
        SaveFrame("pickup-5-swapped");
        bool swapped = System.Linq.Enumerable.Any(_player.WeaponSlots, weapon => weapon.Id == "music_box");
        GD.Print($"[RunObservation] RESULT weapon_pickup slots={slotsBefore}->{slotsAfterAuto} full_stays_on_ground={stillOnGround} swapped={swapped}");
    }
}
