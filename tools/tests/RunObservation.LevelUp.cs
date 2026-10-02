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
/// d'arme, une amélioration d'objet qui franchit son palier 15 et un objet nouveau, puis une fois avec
/// le focus sur les actions, puis sur la carte d'objet (armes concernées allumées dans l'inventaire).
/// --capture-pause : le HUD sans bouclier, puis la pause après 20 s de combat, quatre armes et six objets portés
/// (dont l'Écusson de pompier, seule source de bouclier), dont un au-delà de son palier.
/// </summary>
public partial class RunObservation
{
    private async Task CaptureLevelUp()
    {
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        ProcessMode = ProcessModeEnum.Always;
        await ToSignal(GetTree().CreateTimer(3.5, processAlways: true), SceneTreeTimer.SignalName.Timeout);

        FragmentManager fragments = _world.GetNode<FragmentManager>("FragmentManager");
        _player.AddOrUpgradePassive("souffle_du_neant");
        // Niveau 14 : l'amélioration d'objet fait atteindre le palier 15 (badge doré).
        _player.AddOrUpgradePassive("souffle_du_neant", 13);
        // --levelup-weapon et --levelup-new : arme améliorée et objet neuf montrés (plan 21 G6 : frappes, Paille tordue).
        string[] args = OS.GetCmdlineUserArgs();
        string weaponId = Argument(args, "--levelup-weapon", "crossbow");
        string newItem = Argument(args, "--levelup-new", "persistance");
        _player.AddWeapon(WeaponDataLoader.Get(weaponId));
        List<FragmentOption> pending = (List<FragmentOption>)typeof(FragmentManager)
            .GetField("_pendingChoices", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(fragments);
        FieldInfo active = typeof(FragmentManager).GetField("_choosingActive", BindingFlags.NonPublic | BindingFlags.Instance);
        EventBus eventBus = GetNode<EventBus>("/root/EventBus");
        RandomNumberGenerator rng = new() { Seed = 3 };
        Node screen = _world.GetNode("LevelUpScreen");

        foreach (UpgradeRarity rarity in UpgradeRoller.Rarities)
        {
            WeaponInstance weapon = _player.WeaponSlots[_player.WeaponSlots.Count - 1];
            pending.Clear();
            pending.Add(new FragmentOption(weapon.Id, "weapon_upgrade", weapon.Name, 1)
                .WithWeaponUpgrade(rarity, UpgradeRoller.RollWeaponGains(weapon, rarity, rng)));
            pending.Add(new FragmentOption("souffle_du_neant", "passive_upgrade", PassiveSouvenirDataLoader.Get("souffle_du_neant").Name, 1)
                .WithPassiveUpgrade(rarity));
            pending.Add(new FragmentOption(newItem, "passive_new", PassiveSouvenirDataLoader.Get(newItem).Name, 1));
            active.SetValue(fragments, true);
            eventBus.EmitSignal(EventBus.SignalName.FragmentChoicesReady, pending.Count);
            await Frames(20);
            SaveFrame($"levelup-{rarity.Rank}-{rarity.Id}");
            if (rarity.Rank == UpgradeRoller.Rarities.Count - 1)
            {
                screen.GetType().GetMethod("SetFocus", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(screen, new object[] { 4 });
                await Frames(4);
                SaveFrame("levelup-focus-actions");
                // Carte d'objet focalisée : l'inventaire allume les armes qu'il renforce (plan 23 R2).
                screen.GetType().GetMethod("SetFocus", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(screen, new object[] { 1 });
                await Frames(4);
                SaveFrame("levelup-focus-object");
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
        _player.AcquireSpecialization(PerkSpecializationDataLoader.Get("overheal_reserve"));
        pending.Clear();
        foreach (string id in new[] { "priority_targeting", "overflow", "rally" })
        {
            PerkSpecializationData perk = PerkSpecializationDataLoader.Get(id);
            pending.Add(new FragmentOption(id, PerkSpecializationOffers.OptionType, perk.Name, 1));
        }
        active.SetValue(fragments, true);
        eventBus.EmitSignal(EventBus.SignalName.FragmentChoicesReady, pending.Count);
        await Frames(30);
        SaveFrame("levelup-reminiscences");
        screen.GetType().GetMethod("Skip", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(screen, null);
        await Frames(4);
        // Trois rangs gagnés : état initial, éclatement intermédiaire puis icône finale sans débordement.
        WeaponInstance raisedWeapon = _player.WeaponSlots[_player.WeaponSlots.Count - 1];
        UpgradeRarity finalRarity = UpgradeRoller.Get("epic");
        pending.Clear();
        pending.Add(new FragmentOption(raisedWeapon.Id, "weapon_upgrade", raisedWeapon.Name, 1)
            .WithWeaponUpgrade(finalRarity, UpgradeRoller.RollWeaponGains(raisedWeapon, finalRarity, rng))
            .WithRolledRarity(UpgradeRoller.Get("common")));
        active.SetValue(fragments, true);
        eventBus.EmitSignal(EventBus.SignalName.FragmentChoicesReady, pending.Count);
        await Frames(2);
        SaveFrame("levelup-luck-start");
        await ToSignal(GetTree().CreateTimer(0.75, processAlways: true), SceneTreeTimer.SignalName.Timeout);
        SaveFrame("levelup-luck-break");
        await ToSignal(GetTree().CreateTimer(1.5, processAlways: true), SceneTreeTimer.SignalName.Timeout);
        SaveFrame("levelup-luck-end");
        screen.GetType().GetMethod("Skip", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(screen, null);
        GD.Print($"[RunObservation] RESULT levelup_captured=True output={_output}");
    }

    private async Task CapturePause()
    {
        ProcessMode = ProcessModeEnum.Always;
        await Frames(90);
        // Sans objet de bouclier, ni liseré sur la barre de PV ni ligne dans la fiche (plan 23 R1).
        SaveFrame("hud-no-shield");
        foreach (string weapon in new[] { "crossbow", "music_box", "nail_mace" })
            _player.AddWeapon(WeaponDataLoader.Get(weapon));
        foreach (string passive in new[] { "oeil_critique", "memoire_vive", "resonance", "souffle_du_neant", "persistance", "carapace" })
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
        SaveFrame("hud-shield");
        Node pause = _world.GetNode("PauseMenu");
        pause.GetType().GetMethod("Pause", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public).Invoke(pause, null);
        await Frames(10);
        SaveFrame("pause");
        // Les vrais événements traversent le routage de Godot, y compris pendant la pause.
        GetViewport().PushInput(new InputEventKey { Keycode = Key.Down, Pressed = true });
        await Frames(2);
        GetViewport().PushInput(new InputEventKey { Keycode = Key.Down, Pressed = false });
        await Frames(5);
        SaveFrame("pause-focus-settings");
        if (GetViewport().GuiGetFocusOwner() is not Button { Text: "Paramètres" })
            throw new System.InvalidOperationException("La touche bas doit sélectionner Paramètres dans la pause.");
        GetViewport().PushInput(new InputEventJoypadButton { ButtonIndex = JoyButton.A, Pressed = true });
        await Frames(2);
        GetViewport().PushInput(new InputEventJoypadButton { ButtonIndex = JoyButton.A, Pressed = false });
        await Frames(10);
        SaveFrame("pause-settings");
        if (!pause.GetNode<Vestiges.UI.SettingsScreen>("SettingsScreen").IsOpen)
            throw new System.InvalidOperationException("Le bouton A doit ouvrir les paramètres.");
        GetViewport().PushInput(new InputEventJoypadButton { ButtonIndex = JoyButton.B, Pressed = true });
        await Frames(2);
        GetViewport().PushInput(new InputEventJoypadButton { ButtonIndex = JoyButton.B, Pressed = false });
        await Frames(10);
        SaveFrame("pause-return-settings");
        if (GetViewport().GuiGetFocusOwner() is not Button { Text: "Paramètres" })
            throw new System.InvalidOperationException("Le bouton B doit rendre le focus à Paramètres.");
        Control focus = GetViewport().GuiGetFocusOwner();
        if (focus != null)
        {
            Vector2 point = focus.GetGlobalRect().GetCenter();
            GetViewport().NotifyMouseEntered();
            GetViewport().PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
            await Frames(5);
            SaveFrame("pause-hover-focus");
        }
        // Stick droit ou Page bas : les colonnes défilent sans souris.
        Input.ActionPress("scroll_down");
        await Frames(30);
        Input.ActionRelease("scroll_down");
        await Frames(5);
        SaveFrame("pause-scrolled");
        GD.Print($"[RunObservation] RESULT pause_captured=True output={_output}");
    }
}
