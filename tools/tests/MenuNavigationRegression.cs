using System;
using System.Reflection;
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
            lockedRow.GrabFocus();
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
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner);
    private async Task Frames(int count) { for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
    private async Task Press(string action)
    {
        Input.ParseInputEvent(new InputEventAction { Action = action, Pressed = true });
        await Frames(1);
        Input.ParseInputEvent(new InputEventAction { Action = action, Pressed = false });
        await Frames(2);
    }
}
