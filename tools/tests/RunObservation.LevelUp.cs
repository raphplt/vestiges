using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.Tests;

/// <summary>
/// --capture-levelup : l'écran de level-up montré une fois par rareté (Commun à Légendaire), avec une amélioration
/// d'arme, une amélioration d'objet qui approche puis franchit son palier 25 et un objet nouveau, puis une fois avec
/// le focus sur les actions.
/// --capture-pause : la pause après 20 s de combat, quatre armes et cinq objets portés, dont un au-delà de son palier.
/// </summary>
public partial class RunObservation
{
    private async Task CaptureLevelUp()
    {
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        ProcessMode = ProcessModeEnum.Always;
        await Frames(90);

        FragmentManager fragments = _world.GetNode<FragmentManager>("FragmentManager");
        _player.AddOrUpgradePassive("souffle_du_neant");
        _player.AddOrUpgradePassive("souffle_du_neant", 21);
        _player.AddWeapon(WeaponDataLoader.Get("crossbow"));
        List<FragmentOption> pending = (List<FragmentOption>)typeof(FragmentManager)
            .GetField("_pendingChoices", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(fragments);
        FieldInfo active = typeof(FragmentManager).GetField("_choosingActive", BindingFlags.NonPublic | BindingFlags.Instance);
        EventBus eventBus = GetNode<EventBus>("/root/EventBus");
        RandomNumberGenerator rng = new() { Seed = 3 };
        Node screen = _world.GetNode("LevelUpScreen");

        foreach (UpgradeRarity rarity in UpgradeRoller.Rarities)
        {
            WeaponInstance crossbow = _player.WeaponSlots[_player.WeaponSlots.Count - 1];
            pending.Clear();
            pending.Add(new FragmentOption(crossbow.Id, "weapon_upgrade", crossbow.Name, 1)
                .WithWeaponUpgrade(rarity, UpgradeRoller.RollWeaponGains(crossbow, rarity, rng)));
            pending.Add(new FragmentOption("souffle_du_neant", "passive_upgrade", PassiveSouvenirDataLoader.Get("souffle_du_neant").Name, 1)
                .WithPassiveUpgrade(rarity, rarity.ObjectLevels));
            pending.Add(new FragmentOption("persistance", "passive_new", PassiveSouvenirDataLoader.Get("persistance").Name, 1));
            active.SetValue(fragments, true);
            eventBus.EmitSignal(EventBus.SignalName.FragmentChoicesReady, pending.Count);
            await Frames(20);
            SaveFrame($"levelup-{rarity.Rank}-{rarity.Id}");
            if (rarity.Rank == UpgradeRoller.Rarities.Count - 1)
            {
                screen.GetType().GetMethod("SetFocus", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(screen, new object[] { 4 });
                await Frames(4);
                SaveFrame("levelup-focus-actions");
            }
            screen.GetType().GetMethod("Skip", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(screen, null);
            await Frames(4);
        }
        // Ascension : l'arme de départ au niveau 50 montre ses deux voies ensemble, et une troisième carte.
        WeaponInstance starting = _player.WeaponSlots[0];
        while (starting.CanLevelUp)
            starting.ApplyUpgrade(System.Array.Empty<StatGain>());
        typeof(FragmentManager).GetMethod("CachePlayer", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(fragments, null);
        typeof(FragmentManager).GetMethod("OfferFragments", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(fragments, new object[] { 60 });
        await Frames(20);
        SaveFrame("levelup-ascension");
        screen.GetType().GetMethod("Skip", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(screen, null);
        await Frames(4);
        GD.Print($"[RunObservation] Captures du level-up écrites dans {_output}");
    }

    private async Task CapturePause()
    {
        ProcessMode = ProcessModeEnum.Always;
        await Frames(90);
        foreach (string weapon in new[] { "crossbow", "music_box", "nail_mace" })
            _player.AddWeapon(WeaponDataLoader.Get(weapon));
        foreach (string passive in new[] { "oeil_critique", "memoire_vive", "resonance", "souffle_du_neant", "persistance" })
            _player.AddOrUpgradePassive(passive);
        // Niveaux à deux chiffres dans les cases du HUD, un palier atteint et un palier à venir dans la pause.
        _player.AddOrUpgradePassive("souffle_du_neant", 29);
        _player.AddOrUpgradePassive("persistance", 11);
        WeaponInstance equipped = _player.EquippedWeapon;
        _player.UpgradeWeapon(equipped.Id, UpgradeRoller.RollWeaponGains(equipped,
            UpgradeRoller.Get("rare"), new RandomNumberGenerator()));
        _player.AIInputOverride = new Vector2(0.6f, 0.2f);
        double until = Time.GetTicksMsec() / 1000.0 + 20.0;
        while (Time.GetTicksMsec() / 1000.0 < until)
        {
            await Frames(1);
            if (GetTree().Paused)
                AutoPickLevelUp();
        }
        _player.AIInputOverride = Vector2.Zero;
        Node pause = _world.GetNode("PauseMenu");
        pause.GetType().GetMethod("Pause", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public).Invoke(pause, null);
        await Frames(10);
        SaveFrame("pause");
        // Stick droit ou Page bas : les colonnes défilent sans souris.
        Input.ActionPress("scroll_down");
        await Frames(30);
        Input.ActionRelease("scroll_down");
        await Frames(5);
        SaveFrame("pause-scrolled");
        GD.Print($"[RunObservation] Capture de la pause écrite dans {_output}");
    }
}
