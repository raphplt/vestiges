using Godot;
using Vestiges.Combat;
using Vestiges.Infrastructure;

namespace Vestiges.Core;

/// <summary>Ascensions d'armes (plan 21 §3) : le choix d'une voie au niveau maximal et les comportements qu'elle ouvre.</summary>
public partial class Player
{
    private float _orbitPulseTime;

    /// <summary>Choisit pour de bon une voie d'ascension d'une arme au niveau maximal (plan 21 §3).</summary>
    public bool AscendWeapon(string weaponId, string ascensionId)
    {
        WeaponInstance weapon = FindWeaponSlot(weaponId, out int slot);
        if (weapon == null || !weapon.Ascend(ascensionId))
            return false;
        RefreshAttackSpeed();
        if (weapon == _orbitalWeapon)
            SetupOrbitalWeapon(weapon);
        _eventBus?.EmitSignal(EventBus.SignalName.WeaponUpgraded, weaponId, slot, "ascension", weapon.Level);
        _eventBus?.EmitSignal(EventBus.SignalName.WeaponInventoryChanged);
        GD.Print($"[Player] Weapon ascended: {weaponId} → {weapon.Ascension.Name}");
        return true;
    }

    /// <summary>
    /// Ronde (Boîte à musique) : le rayon d'orbite va et vient entre deux fractions de la portée ; 1 sans cette voie.
    /// </summary>
    private float OrbitPulse(WeaponInstance weapon, float delta)
    {
        if (!weapon.HasFlag(WeaponAscensionData.OrbitPulseFlag))
            return 1f;
        WeaponAscensionData ascension = weapon.Ascension;
        float period = Mathf.Max(0.1f, ascension.Parameter("pulse_period"));
        _orbitPulseTime = Mathf.PosMod(_orbitPulseTime + delta, period);
        float wave = 0.5f - 0.5f * Mathf.Cos(Mathf.Tau * _orbitPulseTime / period);
        return Mathf.Lerp(ascension.Parameter("pulse_min"), ascension.Parameter("pulse_max"), wave);
    }
}
