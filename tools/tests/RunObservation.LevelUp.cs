using System.Collections.Generic;
using System.Linq;
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
/// le focus sur les actions, puis sur la carte d'objet (armes concernées allumées dans l'inventaire) ; ascension, puis
/// quatre cartes avec le Trèfle plastifié.
/// --capture-pause : le HUD sans bouclier, puis la pause après 20 s de combat, quatre armes, quatre Écussons et un sac garni
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
            pending.Add(new FragmentOption(weapon.Id, "weapon_upgrade", weapon.Name)
                .WithWeaponUpgrade(rarity, UpgradeRoller.RollWeaponGains(weapon, rarity, rng)));
            pending.Add(new FragmentOption("souffle_du_neant", "passive_upgrade", PassiveSouvenirDataLoader.Get("souffle_du_neant").Name)
                .WithPassiveUpgrade(rarity));
            pending.Add(new FragmentOption(newItem, "passive_new", PassiveSouvenirDataLoader.Get(newItem).Name));
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
        // Trèfle plastifié (plan 35 I6) : une quatrième carte dans la même offre.
        _player.AddItemCopy("trefle_plastifie");
        typeof(FragmentManager).GetMethod("OfferFragments", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(fragments, new object[] { 61 });
        await Frames(20);
        SaveFrame("levelup-clover");
        screen.GetType().GetMethod("Skip", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(screen, null);
        await Frames(4);
        _player.AcquireSpecialization(PerkSpecializationDataLoader.Get("overheal_reserve"));
        pending.Clear();
        foreach (string id in new[] { "priority_targeting", "overflow", "rally" })
        {
            PerkSpecializationData perk = PerkSpecializationDataLoader.Get(id);
            pending.Add(new FragmentOption(id, PerkSpecializationOffers.OptionType, perk.Name));
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
        pending.Add(new FragmentOption(raisedWeapon.Id, "weapon_upgrade", raisedWeapon.Name)
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
        // Sans objet de bouclier, pas de liseré sur les PV ; la fiche affiche néanmoins sa valeur nulle.
        SaveFrame("hud-no-shield");
        foreach (string weapon in new[] { "crossbow", "music_box", "nail_mace" })
            _player.AddWeapon(WeaponDataLoader.Get(weapon));
        foreach (string badge in new[] { "oeil_critique", "memoire_vive", "souffle_du_neant", "carapace" })
            _player.AddOrUpgradePassive(badge);
        // Niveaux à deux chiffres dans les cases du HUD, un palier atteint et un palier à venir dans la pause.
        _player.AddOrUpgradePassive("souffle_du_neant", 29);
        _player.AddOrUpgradePassive("memoire_vive", 11);
        // Le sac : plusieurs quantités pour lire l’effet par copie et la comparaison sans palier de doublon.
        bool newItems = System.Array.IndexOf(OS.GetCmdlineUserArgs(), "--capture-new-items") >= 0;
        string[] bag = newItems
            ? new[] { "pastille_mentholee", "pastille_mentholee", "verre_fele", "carnet_dadresses", "boucle_cabossee", "diapositive_en_double", "tampon_annule" }
            : new[] { "de_a_coudre", "de_a_coudre", "glacon", "boite_de_pansements", "boite_de_pansements", "boite_de_pansements", "thermos" };
        foreach (string item in bag)
            _player.AddItemCopy(item);
        WeaponInstance equipped = _player.EquippedWeapon;
        // Surplus posé à la main : l'arme n'a pas le temps d'atteindre le niveau 50 dans la capture.
        equipped.AddSurplus(0.08f);
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
        AssertMenuFits((Control)pause.GetType().GetField("_content", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(pause), "pause");
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
        // Le détail d'une stat (base, bonus du build) n'est plus écrit sous la ligne : il s'ouvre au survol.
        VBoxContainer sheet = (VBoxContainer)pause.GetType().GetField("_sheetContainer", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(pause);
        Control statRow = sheet.GetChildren().OfType<Control>().FirstOrDefault(row => !string.IsNullOrEmpty(row.TooltipText))
            ?? throw new System.InvalidOperationException("Les lignes de stats de la pause doivent porter leur détail en infobulle.");
        Vector2 statPoint = statRow.GetGlobalRect().GetCenter();
        GetViewport().PushInput(new InputEventMouseMotion { Position = statPoint, GlobalPosition = statPoint }, true);
        await Frames(60);
        SaveFrame("pause-stat-tooltip");
        if (focus != null)
        {
            Vector2 back = focus.GetGlobalRect().GetCenter();
            GetViewport().PushInput(new InputEventMouseMotion { Position = back, GlobalPosition = back }, true);
            await Frames(5);
        }
        // Journal depuis la pause : ouverture à la manette, retour sans reprise et focus restitué.
        GetViewport().PushInput(new InputEventAction { Action = "ui_down", Pressed = true });
        await Frames(2);
        if (GetViewport().GuiGetFocusOwner() is not Button { Text: "Journal" } journalButton)
            throw new System.InvalidOperationException("Bas après Paramètres doit atteindre le Journal.");
        journalButton.EmitSignal(BaseButton.SignalName.Pressed);
        await Frames(3);
        if (!_world.GetNode<Vestiges.UI.JournalScreen>("JournalScreen").IsOpen)
            throw new System.InvalidOperationException("Le journal doit s'ouvrir depuis la pause.");
        SaveFrame("pause-journal");
        GetViewport().PushInput(new InputEventJoypadButton { ButtonIndex = JoyButton.B, Pressed = true });
        await Frames(3);
        if (!GetTree().Paused || GetViewport().GuiGetFocusOwner() != journalButton)
            throw new System.InvalidOperationException("Fermer le journal doit retrouver la pause et son focus.");
        // L'abandon est confirmé dans une modale qui garde la run en pause.
        GetViewport().PushInput(new InputEventAction { Action = "ui_down", Pressed = true });
        await Frames(1);
        GetViewport().PushInput(new InputEventAction { Action = "ui_down", Pressed = false });
        await Frames(2);
        if (GetViewport().GuiGetFocusOwner() is not Button { Text: "Abandonner la run" } abandon)
            throw new System.InvalidOperationException("Bas après Paramètres doit atteindre Abandonner.");
        abandon.EmitSignal(BaseButton.SignalName.Pressed);
        await Frames(3);
        SaveFrame("pause-confirm-abandon");
        if (GetViewport().GuiGetFocusOwner() is not Button { Text: "Continuer la partie" } || !GetTree().Paused)
            throw new System.InvalidOperationException("La confirmation doit proposer de conserver la partie.");
        GetViewport().PushInput(new InputEventAction { Action = "ui_cancel", Pressed = true });
        await Frames(1);
        GetViewport().PushInput(new InputEventAction { Action = "ui_cancel", Pressed = false });
        await Frames(3);
        if (GetViewport().GuiGetFocusOwner() != abandon || !GetTree().Paused)
            throw new System.InvalidOperationException("Annuler doit retrouver Abandonner sans reprendre la run.");
        System.Collections.Generic.List<ScrollContainer> columns = (System.Collections.Generic.List<ScrollContainer>)pause.GetType()
            .GetField("_columnScrolls", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(pause);
        // Dix-neuf statistiques exhaustives, puis les Oublis : les deux cas doivent rester accessibles au défilement.
        PerilManager peril = _world.GetNode<PerilManager>("PerilManager");
        foreach (OubliData oubli in OubliDataLoader.All) peril.AddOubli(oubli);
        pause.GetType().GetMethod("UpdateStats", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(pause, null);
        await Frames(4);
        VBoxContainer inventory = (VBoxContainer)pause.GetType()
            .GetField("_inventoryContainer", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(pause);
        inventory.GetChild<GridContainer>(5).GetChild<Button>(newItems ? 0 : 2).GrabFocus();
        await Frames(3);
        SaveFrame("pause-object");
        inventory.GetChild<GridContainer>(5).GetChild<Button>(newItems ? 2 : 1).GrabFocus();
        await Frames(3);
        SaveFrame("pause-object-status");
        columns[1].GrabFocus();
        // Stick droit ou Page bas : seule la colonne examinée défile.
        int previousScroll = columns[1].ScrollVertical;
        Input.ActionPress("scroll_down");
        await Frames(30);
        Input.ActionRelease("scroll_down");
        await Frames(5);
        SaveFrame("pause-scrolled");
        if (columns[1].ScrollVertical <= previousScroll || columns[0].ScrollVertical != 0)
            throw new System.InvalidOperationException("Le défilement doit affecter seulement la fiche sélectionnée.");
        GD.Print($"[RunObservation] RESULT pause_captured=True output={_output}");
    }
}
