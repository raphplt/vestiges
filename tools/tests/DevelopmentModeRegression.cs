using System;
using System.Threading.Tasks;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Infrastructure.Analytics;
using Vestiges.Infrastructure.Steam;
using Vestiges.Score;
using Vestiges.UI;

namespace Vestiges.Tests;

/// <summary>Trois processus sur le même répertoire : profil normal, dev, retour normal.</summary>
public partial class DevelopmentModeRegression : Node
{
    public override async void _Ready()
    {
        try
        {
            if (Array.IndexOf(OS.GetCmdlineUserArgs(), "--toggle-profile") >= 0)
            {
                await RunToggleChecks();
                GD.Print("[DevelopmentModeRegression] PASS");
                GetTree().Quit();
                return;
            }
            if (Array.IndexOf(OS.GetCmdlineUserArgs(), "--verify-toggle-enabled") >= 0)
            {
                Check(DevelopmentMode.IsEnabled, "préférence dev retrouvée au lancement sans argument --dev");
                Check(MetaSaveManager.IsCharacterUnlocked("vagabond"), "profil débloqué retrouvé après relancement");
                await LoadHub();
                await ToggleHub(false);
                Check(!MetaSaveManager.IsCharacterUnlocked("vagabond"), "retour normal depuis préférence mémorisée");
                GD.Print("[DevelopmentModeRegression] PASS");
                GetTree().Quit();
                return;
            }
            MetaSaveManager.Load();
            bool seed = Array.IndexOf(OS.GetCmdlineUserArgs(), "--seed-profile") >= 0;
            bool dev = DevelopmentMode.IsEnabled;
            if (seed)
            {
                MetaSaveManager.AddVestiges(37);
                MetaSaveManager.UnlockCharacter("forgeuse");
                MetaSaveManager.DiscoverSouvenir("billet_de_train");
                MetaSaveManager.CompleteQuest("profile_fixture");
            }

            if (dev)
            {
                foreach (CharacterData character in CharacterDataLoader.GetAll())
                    Check(MetaSaveManager.IsCharacterUnlocked(character.Id), $"personnage {character.Id}");
                foreach (SouvenirData souvenir in SouvenirDataLoader.GetAll())
                    Check(MetaSaveManager.HasSouvenir(souvenir.Id), $"souvenir {souvenir.Id}");
                foreach (WeaponData weapon in WeaponDataLoader.GetAll())
                    Check(string.IsNullOrEmpty(weapon.RequiresSouvenir) || MetaSaveManager.HasSouvenir(weapon.RequiresSouvenir), $"accès arme {weapon.Id}");
                Check(!SteamManager.IsActive, "Steam inactif");
                Check(MetaSaveManager.GetCompletedQuests().Count == 0, "quêtes non falsifiées");
                Check(MetaSaveManager.GetVestiges() == 0, "monnaie normale non importée");
                Check(RunHistoryManager.GetHistory().Count == 0, "historique normal non importé");
                DevelopmentBadge.AttachTo(this);
                Check(GetNodeOrNull<CanvasLayer>("DevelopmentBadge") != null, "repère dev présent");
            }
            else
            {
                Check(MetaSaveManager.IsCharacterUnlocked("forgeuse"), "acquis normal conservé");
                Check(!MetaSaveManager.IsCharacterUnlocked("vagabond"), "verrou normal conservé");
                Check(MetaSaveManager.GetDiscoveredSouvenirs().Count == 1, "lore normal conservé");
                Check(MetaSaveManager.HasCompletedQuest("profile_fixture"), "quête normale conservée");
                Check(MetaSaveManager.GetVestiges() == (seed ? 37 : 47), "monnaie normale conservée");
            }

            ScoreManager score = new();
            AddChild(score);
            Check(score.BestScore == (seed || dev ? 0 : 100), "record du bon profil chargé");
            if (seed || dev)
            {
                GetNode<GameManager>("/root/GameManager").SelectedCharacterId = "traqueur";
                // La fin de run réelle exerce ensemble score, historique, méta et analytics.
                EventBus bus = GetNode<EventBus>("/root/EventBus");
                for (int i = 0; i < (dev ? 20 : 10); i++)
                    bus.EmitSignal(EventBus.SignalName.EnemyKilled, "melee", Vector2.Zero);
                score.SaveEndOfRun();
                Check(RunHistoryManager.GetHistory().Count == 1, "fin de run sauvegardée");
                Check(FileAccess.FileExists(DevelopmentMode.GetSavePath("highscore.save")), "record sauvegardé");
                Check(FileAccess.FileExists(DevelopmentMode.GetSavePath("analytics/aggregate.json")), "analytics sauvegardées");
            }
            else
                Check(RunHistoryManager.GetHistory().Count == 1 && RunHistoryManager.GetBestScore() == 100, "historique normal intact après dev");

            GD.Print("[DevelopmentModeRegression] PASS");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    private async Task LoadHub()
    {
        // Le pilote reste à la racine pendant que la vraie scène Hub est rechargée.
        GetTree().CurrentScene = null;
        Check(GetTree().ChangeSceneToFile("res://scenes/Hub.tscn") == Error.Ok, "chargement du Hub");
        for (int frame = 0; frame < 30 && GetTree().CurrentScene is not HubScreen; frame++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Check(GetTree().CurrentScene is HubScreen, "vrai Hub prêt");
    }

    private async Task ToggleHub(bool enabled)
    {
        Node previous = GetTree().CurrentScene;
        CheckButton toggle = previous.FindChild("DevelopmentToggle", true, false) as CheckButton;
        Check(toggle != null && toggle.ButtonPressed == DevelopmentMode.IsEnabled, "toggle visible et synchronisé");
        toggle.ButtonPressed = enabled;
        for (int frame = 0; frame < 30 && (GetTree().CurrentScene == null || GetTree().CurrentScene == previous); frame++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Check(GetTree().CurrentScene is HubScreen && GetTree().CurrentScene != previous, "bascule recharge le Hub");
        Check(DevelopmentMode.IsEnabled == enabled, "profil demandé actif");
        Check((GetTree().CurrentScene.GetNodeOrNull<CanvasLayer>("DevelopmentBadge") != null) == enabled,
            "bandeau synchronisé avec le profil");
    }

    private async Task RunToggleChecks()
    {
        Check(DevelopmentMode.IsAvailable && !DevelopmentMode.IsEnabled, "F5 debug : toggle disponible, profil normal initial");
        await LoadHub();
        for (int iteration = 0; iteration < 2; iteration++)
        {
            await ToggleHub(true);
            Check(MetaSaveManager.IsCharacterUnlocked("vagabond"), "accès dev actualisés sans relancer Godot");
            Check(RunHistoryManager.GetBestScore() == 200, "historique dev rechargé");
            Check(!SteamManager.IsActive, "Steam désactivé par la bascule");
            MetaSaveManager.AddVestiges(1);
            AnalyticsManager.Instance.RecordRunEnd(new RunRecord { CharacterId = "traqueur", Score = 300 });
            GetNode<GameManager>("/root/GameManager").SelectedCharacterId = "vagabond";
            await ToggleHub(false);
            Check(!MetaSaveManager.IsCharacterUnlocked("vagabond") && MetaSaveManager.GetVestiges() == 47, "acquis et monnaie normaux conservés");
            Check(RunHistoryManager.GetBestScore() == 100, "historique normal rechargé");
            Check(AnalyticsManager.Instance.GetReport().TotalRuns == 1, "analytics normales sans runs dev");
            Check(GetNode<GameManager>("/root/GameManager").SelectedCharacterId != "vagabond", "sélection dev invalidée au retour normal");
            ScoreManager score = new();
            GetTree().CurrentScene.AddChild(score);
            Check(score.BestScore == 100, "record normal chargé après la bascule");
            score.QueueFree();
        }
        await ToggleHub(true);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
        GD.Print($"[DevelopmentModeRegression] OK {message}");
    }
}
