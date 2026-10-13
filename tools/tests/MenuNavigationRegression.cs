using System;
using System.Reflection;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.UI;

namespace Vestiges.Tests;

/// <summary>Parcours réels du Hub : séparation du camp, verrouillage, liens ciblés et retour de relance.</summary>
public partial class MenuNavigationRegression : Node
{
    private int _checks;
    public override async void _Ready()
    {
        try
        {
            await Frames(1);
            HubScreen hub = GD.Load<PackedScene>("res://scenes/Hub.tscn").Instantiate<HubScreen>();
            GetTree().Root.AddChild(hub);
            await Frames(5);
            GameManager manager = GetNode<GameManager>("/root/GameManager");
            await CheckCampMenu(hub);
            string selected = manager.SelectedCharacterId;
            await Press("ui_right");
            Check(manager.SelectedCharacterId == selected, "Le camp ne sélectionne plus de héros");
            Field<HubMenuButton>(hub, "_enterVoidButton").GrabFocus();
            await Press("ui_accept");
            HubCharactersPanel characters = Field<HubCharactersPanel>(hub, "_charactersPanel");
            Check(characters.IsVisibleInTree() && manager.CurrentState == GameManager.GameState.Hub, "Partir ouvre le choix sans lancer la run");
            Check(characters.CanDepart, "Le héros du profil neuf est jouable");
            Check(!Field<Label>(characters, "_details").Visible, "Les statistiques sont secondaires");
            CharacterData locked = CharacterDataLoader.GetAll().Find(character => !MetaSaveManager.IsCharacterUnlocked(character.Id));
            Check(locked != null, "Le profil neuf contient un héros verrouillé");
            Button lockedRow = (Button)characters.FindChild(locked.Id, true, false);
            string examined = characters.ExaminedCharacterId;
            lockedRow.GrabFocus();
            await Frames(2);
            Check(characters.ExaminedCharacterId == examined, "Le focus seul ne change pas la fiche (DECISIONS §154)");
            lockedRow.EmitSignal(BaseButton.SignalName.Pressed);
            await Frames(2);
            Check(!characters.CanDepart && Field<Button>(characters, "_depart").Disabled, "Le héros verrouillé ne peut pas partir");
            Check(manager.SelectedCharacterId == selected, "Examiner un héros ne change pas le choix confirmé");
            QuestDefinition quest = QuestDataLoader.FindUnlocking(UnlockKind.Character, locked.Id);
            Field<Button>(characters, "_quest").EmitSignal(BaseButton.SignalName.Pressed);
            await Frames(2);
            HubQuestsPanel quests = Field<HubQuestsPanel>(hub, "_questsPanel");
            Check(quests.IsVisibleInTree() && quests.ExaminedQuestId == quest.Id, "Le lien atteint la bonne quête");
            await Press("ui_cancel");
            Check(characters.IsVisibleInTree() && characters.ExaminedCharacterId == locked.Id, "Retour restaure la fiche verrouillée");
            Field<Button>(characters, "_quest").EmitSignal(BaseButton.SignalName.Pressed);
            await Frames(2);
            QuestUnlock weapon = quest.Unlocks.Find(unlock => unlock.Kind == UnlockKind.Weapon);
            Check(weapon != null, "La quête de référence débloque aussi une arme");
            quests.EmitSignal(HubQuestsPanel.SignalName.RewardRequested, (int)weapon.Kind, weapon.Id);
            await Frames(2);
            HubCollectionPanel collection = Field<HubCollectionPanel>(hub, "_collectionPanel");
            Check(collection.IsVisibleInTree() && collection.ExaminedEntryId == weapon.Id, "La récompense ouvre son entrée de Collection");
            await Press("ui_cancel");
            Check(quests.IsVisibleInTree() && quests.ExaminedQuestId == quest.Id, "Retour restaure la quête d'origine");
            await Press("ui_cancel");
            await Press("ui_cancel");
            SettingsScreen settings = Field<SettingsScreen>(hub, "_settingsScreen");
            Control returnFocus = GetViewport().GuiGetFocusOwner();
            settings.Open(); await Frames(2); settings.Close();
            Check(GetViewport().GuiGetFocusOwner() == returnFocus, "Paramètres restitue le focus");
            await CheckItemSorting(hub);
            hub.QueueFree(); await Frames(2);
            manager.ChangeState(GameManager.GameState.Death);
            manager.OpenCharacterSelection = true;
            HubScreen retryHub = GD.Load<PackedScene>("res://scenes/Hub.tscn").Instantiate<HubScreen>();
            GetTree().Root.AddChild(retryHub); await Frames(3);
            Check(Field<HubCharactersPanel>(retryHub, "_charactersPanel").IsVisibleInTree() && !manager.OpenCharacterSelection,
                "La relance ouvre la sélection et consomme sa destination");
            Check(manager.CurrentState == GameManager.GameState.Hub, "Le retour du bilan rétablit l’état du Hub");
            HubCharactersPanel retryCharacters = Field<HubCharactersPanel>(retryHub, "_charactersPanel");
            Button depart = Field<Button>(retryCharacters, "_depart");
            depart.GrabFocus();
            await Press("ui_accept");
            await Press("ui_up");
            Check(GetViewport().GuiGetFocusOwner() == depart, "La transition bloque la navigation après le départ confirmé");
            GD.Print($"[MenuNavigationRegression] RESULT checks={_checks} failures=0");
            await GameExit.QuitAsync(GetTree());
        }
        catch (Exception exception)
        {
            GD.PushError($"[MenuNavigationRegression] {exception}");
            GetTree().Quit(1);
        }
    }

    private void Check(bool condition, string description)
    {
        _checks++;
        if (!condition) throw new InvalidOperationException(description);
        GD.Print($"[MenuNavigationRegression] OK {description}");
    }
    private async Task CheckCampMenu(HubScreen hub)
    {
        Type stateType = typeof(HubScreen).GetNestedType("HubState", BindingFlags.NonPublic)!;
        typeof(HubScreen).GetMethod("OpenPage", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(hub, new[] { Enum.Parse(stateType, "Camp") });
        await Frames(2);
        HubCampUpgradesPanel camp = Field<HubCampUpgradesPanel>(hub, "_campPanel");
        Button rack = (Button)camp.FindChild("camp_weapon_slots", true, false);
        Button pockets = (Button)camp.FindChild("camp_pockets", true, false);
        HBoxContainer header = camp.GetParentControl().GetNode<HBoxContainer>("PageHeader");
        Button back = header.GetNode<Button>("PageBack");
        Check(header.GetGlobalRect().End.Y <= camp.GetGlobalRect().Position.Y
            && Mathf.IsEqualApprox(back.GetGlobalRect().End.X, camp.GetGlobalRect().End.X),
            "Retour est dans l'en-tête, aligné au bord droit du contenu");
        MetaSaveManager.AddVestiges(1000);
        rack.GrabFocus();
        await Frames(2);
        Check(MetaSaveManager.GetCampRank("camp_weapon_slots") == 0, "Examiner le Râtelier n'achète aucun rang");
        Button buy = Field<Button>(camp, "_buySelected");
        await Press("ui_right");
        Check(GetViewport().GuiGetFocusOwner() == buy, "Droite atteint l'achat dans la fiche du Campement");
        await Press("ui_accept");
        Check(MetaSaveManager.GetCampRank("camp_weapon_slots") == 1 && MetaSaveManager.GetVestiges() == 600,
            "Acheter dans la fiche donne un rang et prélève son prix");
        CampRankPips rackPips = rack.FindChild("RankPips", true, false) as CampRankPips;
        Check(rackPips is { Rank: 1, MaxRank: 2 }, "Le Râtelier remplit un point parmi ses deux niveaux");
        Check(Field<Label>(camp, "_detailEffect").Text == TranslationServer.Translate("CAMP_WEAPON_SLOTS_DESC").ToString().Replace("{0}", "1")
            && Field<Label>(camp, "_detailEffect").GetThemeColor("font_color") == UITheme.TextLight,
            "La fiche affiche le gain d'un achat en texte clair, sans comparaison");
        Check(Field<Label>(camp, "_detailRank").Text == "1/2", "Le compteur de fiche se met à jour sans « Rang »");
        pockets.GrabFocus(); await Frames(2);
        Check(Field<Label>(camp, "_detailName").Text == TranslationServer.Translate("CAMP_POCKETS"),
            "Le focus sur les Poches actualise la fiche");
        await Press("ui_right"); await Press("ui_accept");
        Button refund = Field<Button>(camp, "_refundSelected");
        refund.GrabFocus(); await Press("ui_accept");
        Check(MetaSaveManager.GetCampRank("camp_pockets") == 0 && MetaSaveManager.GetVestiges() == 600,
            "Rendre dans la fiche rembourse uniquement l'amélioration choisie");
        Check(GetViewport().GuiGetFocusOwner() == pockets, "Dernier rang rendu : focus restitué à la ligne examinée");
        Button all = Field<Button>(camp, "_refundAll");
        Check(all.GetGlobalRect().Position.Y > camp.GetGlobalRect().GetCenter().Y
            && all.GetGlobalRect().End.X < back.GetGlobalRect().Position.X,
            "Tout rendre est au bas de la liste, éloigné du retour");
        all.GrabFocus(); await Press("ui_accept");
        Check(Field<Control>(camp, "_confirmation")?.IsVisibleInTree() == true
            && MetaSaveManager.GetCampRefundAmount() == 400 && MetaSaveManager.GetVestiges() == 600
            && MetaSaveManager.GetCampRank("camp_weapon_slots") == 1,
            "Ouvrir la confirmation annonce le remboursement sans modifier le Campement");
        Check(GetViewport().GuiGetFocusOwner() == Field<Button>(camp, "_confirmCancel"),
            "Annuler est focalisé par défaut, un second appui ne rembourse pas");
        await Press("ui_accept");
        Check(Field<Control>(camp, "_confirmation") == null && MetaSaveManager.GetVestiges() == 600
            && MetaSaveManager.GetCampRank("camp_weapon_slots") == 1 && GetViewport().GuiGetFocusOwner() == all,
            "Annuler garde les rangs et rend le focus à Tout rendre");
        await Press("ui_accept"); await Press("ui_cancel");
        Check(Field<Control>(camp, "_confirmation") == null && camp.IsVisibleInTree()
            && MetaSaveManager.GetCampRank("camp_weapon_slots") == 1,
            "Échap/B ferme la confirmation sans quitter le Campement ni rembourser");
        await Press("ui_accept");
        await Tab();
        Check(GetViewport().GuiGetFocusOwner() == Field<Button>(camp, "_confirmRefund"),
            "Tab atteint la confirmation du remboursement");
        await Tab();
        Check(GetViewport().GuiGetFocusOwner() == Field<Button>(camp, "_confirmCancel"),
            "Tab reste dans le dialogue");
        await Press("ui_right"); await Press("ui_accept");
        Check(MetaSaveManager.GetVestiges() == 1000 && MetaSaveManager.GetCampRank("camp_weapon_slots") == 0,
            "Confirmer rembourse les améliorations une seule fois");
        Check(rackPips.Rank == 0 && MetaSaveManager.GetCampRefundAmount() == 0,
            "Après remboursement, les points et le montant remboursable reviennent à zéro");
        Check(GetViewport().GuiGetFocusOwner() == pockets, "Tout rendre conserve la sélection et un focus utilisable");
        back.EmitSignal(BaseButton.SignalName.Pressed);
        await Frames(2);
        Check(!camp.IsVisibleInTree(), "Le retour de l'en-tête ferme la page du Campement");
    }

    private async Task CheckItemSorting(HubScreen hub)
    {
        Type stateType = typeof(HubScreen).GetNestedType("HubState", BindingFlags.NonPublic)!;
        typeof(HubScreen).GetMethod("OpenPage", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(hub, new[] { Enum.Parse(stateType, "Collection") });
        HubCollectionPanel collection = Field<HubCollectionPanel>(hub, "_collectionPanel");
        collection.Refresh("allumette_humide", "passives");
        await Frames(2);
        GridContainer grid = Field<GridContainer>(collection, "_grid");
        ItemSortSelector sort = Field<ItemSortSelector>(collection, "_itemSort");
        Check(ItemSort.Mode == ItemSortMode.RarityAscending && sort.ItemCount == 4,
            "Objets : rareté croissante par défaut et quatre options de tri");
        ((Control)grid.GetChild(0)).GrabFocus();
        await Press("ui_up");
        Check(GetViewport().GuiGetFocusOwner() == sort, "Haut depuis la première rangée atteint le tri");
        string examined = collection.ExaminedEntryId;
        foreach (ItemSortMode mode in Enum.GetValues<ItemSortMode>())
        {
            sort.EmitSignal(OptionButton.SignalName.ItemSelected, (long)mode);
            await Frames(2);
            PassiveSouvenirData[] items = grid.GetChildren().Select(tile => PassiveSouvenirDataLoader.Get(tile.Name))
                .Where(item => item != null).ToArray();
            if (mode is ItemSortMode.RarityAscending or ItemSortMode.RarityDescending)
            {
                int[] ranks = items.Select(item => RarityArt.Rank(item.Rarity)).ToArray();
                Check(ranks.Zip(ranks.Skip(1), (a, b) => mode == ItemSortMode.RarityAscending ? a <= b : a >= b).All(ordered => ordered),
                    $"{mode} : tous les rangs restent groupés, verrouillés compris");
            }
            else if (mode == ItemSortMode.Name)
            {
                StringComparer names = StringComparer.Create(CultureInfo.GetCultureInfo("fr"), true);
                Check(items.Select(item => item.Name).SequenceEqual(items.Select(item => item.Name).OrderBy(name => name, names)),
                    "Le tri par nom respecte l'alphabet français");
            }
            else
                Check(items.Select(item => item.Id).SequenceEqual(PassiveSouvenirDataLoader.GetAll().Where(item => !item.IsBadge).Select(item => item.Id)),
                    "Ordre initial : ordre du catalogue restauré");
            Check(collection.ExaminedEntryId == examined && GetViewport().GuiGetFocusOwner() == sort,
                "Changer de tri conserve la fiche examinée et le focus du sélecteur");
        }
        collection.Refresh("arc", "weapons");
        Check(!sort.IsVisibleInTree(), "Le sélecteur d'objets est absent des armes sans rareté");
        collection.Refresh(examined, "passives");
        Check(sort.Selected == (int)ItemSortMode.Original && collection.ExaminedEntryId == examined,
            "Revenir aux objets conserve le tri et la fiche");
        ItemSort.Select(ItemSortMode.RarityAscending);
    }

    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner);
    private async Task Frames(int count) { for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private async Task Tab()
    {
        GetViewport().PushInput(new InputEventKey { Keycode = Key.Tab, Pressed = true });
        await Frames(1);
        GetViewport().PushInput(new InputEventKey { Keycode = Key.Tab, Pressed = false });
        await Frames(2);
    }
    private async Task Press(string action)
    {
        Input.ParseInputEvent(new InputEventAction { Action = action, Pressed = true });
        await Frames(1);
        Input.ParseInputEvent(new InputEventAction { Action = action, Pressed = false });
        await Frames(2);
    }
}
