using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Text.Json.Nodes;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// Quêtes de déblocage (plan 06 §9) dans un profil neuf et isolé. Lot Q1 : catalogue des 36 quêtes et fixtures
/// refusées, réserve de départ appliquée par l'offre de niveau et le butin, quête accomplie et avancée retenue
/// identiques après rechargement. Lot Q2 (<c>--tracking</c>) : suivi en run, voir QuestRegression.Tracking.
/// </summary>
public partial class QuestRegression : Node
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    private static readonly string[] StartingWeapons =
    {
        "chipped_blade", "sling", "sharpened_pipe", "crossbow", "cleaver", "whip", "throwing_axes", "compass_needle",
    };

    private static readonly string[] StartingObjects =
    {
        "memoire_vive", "resonance", "portee_etendue", "persistance", "ancrage", "regeneration", "instinct",
        "siphon_essence", "photo_de_classe", "jeton_de_fete", "allumette_humide", "glacon", "de_a_coudre",
        "semelle_usee", "chewing_gum",
    };

    private int _checks;
    private int _failures;

    public override async void _Ready()
    {
        try
        {
            // Deux passages, chacun sur un profil neuf : déblocages (Q1), puis suivi en run de chaque quête (Q2).
            if (Array.IndexOf(OS.GetCmdlineUserArgs(), "--tracking") >= 0)
                await CheckTracking();
            else
            {
                CheckCatalog();
                CheckRefusals();
                CheckStartingPool();
                CheckClaimAndReload();
            }
            GD.Print($"[QuestRegression] RESULT failures={_failures} checks={_checks}");
            if (_failures == 0)
                GD.Print("[QuestRegression] PASS");
            GetTree().Quit(_failures == 0 ? 0 : 1);
        }
        catch (Exception exception)
        {
            GD.PushError($"[QuestRegression] FAIL {exception}");
            GetTree().Quit(1);
        }
    }

    private void CheckCatalog()
    {
        Check(QuestDataLoader.TryLoad(out string error), $"catalogue du dépôt accepté {error}");
        IReadOnlyList<QuestDefinition> quests = QuestDataLoader.GetAll();
        int[] difficulties = new int[4];
        int cumulative = 0;
        Dictionary<UnlockKind, int> unlocks = new() { [UnlockKind.Character] = 0, [UnlockKind.Weapon] = 0, [UnlockKind.Object] = 0 };
        foreach (QuestDefinition quest in quests)
        {
            difficulties[quest.Difficulty]++;
            if (quest.Scope == QuestScope.Cumulative)
                cumulative++;
            foreach (QuestUnlock unlock in quest.Unlocks)
                unlocks[unlock.Kind]++;
        }
        Check(quests.Count == 36, $"36 quêtes ({quests.Count})");
        Check(difficulties[1] == 14 && difficulties[2] == 17 && difficulties[3] == 5,
            $"14 ★, 17 ★★, 5 ★★★ ({difficulties[1]}, {difficulties[2]}, {difficulties[3]})");
        Check(cumulative == 2, $"deux quêtes de cumul ({cumulative})");
        Check(unlocks[UnlockKind.Character] == 5 && unlocks[UnlockKind.Weapon] == 16 && unlocks[UnlockKind.Object] == 18,
            $"5 personnages, 16 armes (13 + 3 de départ de personnage), 18 objets ({unlocks[UnlockKind.Character]}, {unlocks[UnlockKind.Weapon]}, {unlocks[UnlockKind.Object]})");
        Check(QuestDataLoader.FindUnlocking(UnlockKind.Weapon, "makeshift_bow")?.Id == "lead_the_hunt",
            "l'arme de départ du Traqueur vient avec lui");
    }

    private void CheckRefusals()
    {
        string valid = Godot.FileAccess.GetFileAsString("res://data/quests/quests.json");
        (string, Action<JsonObject>, string)[] cases =
        {
            ("fait inconnu", root => Condition(root, 0)["stat"] = "kills_everything", "stat"),
            ("réglage manquant", root => Condition(root, 7).Remove("param"), "param : attendu pour weapons_at_level"),
            ("réglage sans objet", root => Condition(root, 0)["param"] = 3, "param : sans objet pour sovereign_kills"),
            ("arme inconnue", root => Condition(root, 34)["weapon"] = "bazooka", "weapon : « bazooka » inconnue"),
            ("cumul d'un maximum", root => { Quest(root, 0)["scope"] = "cumulative"; Condition(root, 0)["stat"] = "peril"; }, "peril ne se cumule pas"),
            ("pièce débloquée deux fois", root => Unlock(root, 5)["id"] = "lighthouse_shard", "déjà débloqué par la quête"),
            ("pièce inconnue", root => Unlock(root, 18)["id"] = "parapluie_dore", "pièce inconnue"),
            ("nom d'un personnage connu", root => Unlock(root, 0)["name"] = "Le Traqueur", "déjà au catalogue"),
            ("personnage inconnu sans nom", root => ((JsonObject)Quest(root, 3)["unlocks"]![0]!).Remove("name"), "inconnu sans name"),
            ("difficulté hors bornes", root => Quest(root, 0)["difficulty"] = 4, "difficulty"),
            ("clé inconnue", root => Quest(root, 0)["reward"] = "souvenir", "clé « reward » inconnue"),
            ("id en double", root => Quest(root, 1)["id"] = "lead_the_hunt", "id en double"),
        };
        Func<string, bool> weapons = id => WeaponDataLoader.Get(id) != null;
        Func<string, bool> objects = id => PassiveSouvenirDataLoader.GetAll().Exists(item => item.Id == id);
        Func<string, bool> characters = id => CharacterDataLoader.Get(id) != null;
        foreach ((string label, Action<JsonObject> mutate, string expected) in cases)
        {
            JsonObject root = JsonNode.Parse(valid)!.AsObject();
            mutate(root);
            string message = QuestDataLoader.Apply(root.ToJsonString(), weapons, objects, characters);
            Check(message != null && message.Contains(expected, StringComparison.Ordinal), $"quêtes refusées ({label}) : {message}");
        }
        Check(QuestDataLoader.GetAll().Count == 36, "un fichier refusé ne remplace pas le catalogue chargé");
    }

    private void CheckStartingPool()
    {
        Check(!DevelopmentMode.IsEnabled, "profil normal");
        HashSet<string> weapons = new();
        foreach (WeaponData weapon in WeaponDataLoader.GetAll())
            if (MetaSaveManager.IsWeaponUnlocked(weapon.Id))
                weapons.Add(weapon.Id);
        Check(weapons.SetEquals(StartingWeapons), $"profil neuf : les 8 armes de départ ({string.Join(", ", weapons)})");

        HashSet<string> objects = new();
        foreach (PassiveSouvenirData item in PassiveSouvenirDataLoader.GetAll())
            if (MetaSaveManager.IsObjectUnlocked(item.Id))
                objects.Add(item.Id);
        Check(objects.SetEquals(StartingObjects), $"profil neuf : les 15 objets de départ ({objects.Count})");

        List<string> characters = new();
        foreach (CharacterData character in CharacterDataLoader.GetAll())
            if (MetaSaveManager.IsCharacterUnlocked(character.Id))
                characters.Add(character.Id);
        Check(characters.Count == 1 && characters[0] == "vagabond", $"profil neuf : le Vagabond seul ({string.Join(", ", characters)})");

        bool lootInPool = true;
        for (int i = 0; i < 300; i++)
            lootInPool &= Array.IndexOf(StartingWeapons, LootRewards.PickRandomWeapon()?.Id) >= 0;
        Check(lootInPool, "butin d'arme : 300 tirages dans la réserve de départ");

        Player player = GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate<Player>();
        AddChild(player);
        player.InitializeCharacter(CharacterDataLoader.Get("vagabond"));
        player.IsAIControlled = true;
        player.SetPhysicsProcess(false);
        FragmentManager fragments = new() { Name = "FragmentManager" };
        AddChild(fragments);
        typeof(FragmentManager).GetMethod("CachePlayer", Private)!.Invoke(fragments, null);
        typeof(FragmentManager).GetField("_currentLevel", Private)!.SetValue(fragments, 5);
        List<FragmentOption> pool = (List<FragmentOption>)typeof(FragmentManager).GetMethod("BuildFragmentPool", Private)!.Invoke(fragments, null);
        HashSet<string> offeredWeapons = new();
        HashSet<string> offeredObjects = new();
        foreach (FragmentOption option in pool)
        {
            if (option.Type == "weapon_new")
                offeredWeapons.Add(option.Id);
            else if (option.Type == "passive_new")
                offeredObjects.Add(option.Id);
        }
        HashSet<string> expectedWeapons = new(StartingWeapons);
        expectedWeapons.Remove("chipped_blade");
        Check(offeredWeapons.SetEquals(expectedWeapons), $"offre de niveau : les 7 autres armes de départ ({offeredWeapons.Count})");
        Check(offeredObjects.SetEquals(StartingObjects), $"offre de niveau : les 15 objets de départ ({offeredObjects.Count})");
        RemoveChild(fragments);
        fragments.QueueFree();
        RemoveChild(player);
        player.QueueFree();
    }

    private void CheckClaimAndReload()
    {
        string meta = ProjectSettings.GlobalizePath(DevelopmentMode.GetSavePath("meta_save.json"));
        QuestDefinition hunt = QuestDataLoader.Get("lead_the_hunt");
        Check(MetaSaveManager.ClaimQuest(hunt, out SaveFile.WriteResult saved) && saved.Succeeded, "quête accomplie, écrite aussitôt");
        Check(File.ReadAllText(meta).Contains("\"lead_the_hunt\""), "quête accomplie sur disque");
        Check(MetaSaveManager.IsCharacterUnlocked("traqueur") && MetaSaveManager.IsWeaponUnlocked("makeshift_bow"),
            "Traqueur et Arc du gymnase débloqués");
        Check(!MetaSaveManager.ClaimQuest(hunt, out _), "une quête ne s'accomplit qu'une fois");

        QuestDefinition voices = QuestDataLoader.Get("hear_the_voices");
        Check(MetaSaveManager.ClaimQuest(voices, out _) && MetaSaveManager.IsCharacterUnlocked("eveillee"),
            "un personnage à venir garde son déblocage");

        QuestDefinition doors = QuestDataLoader.Get("every_door");
        MetaSaveManager.RecordQuestProgress(doors, new[] { 30f });
        MetaSaveManager.RecordQuestProgress(doors, new[] { 25f });
        QuestDefinition crowd = QuestDataLoader.Get("the_round");
        MetaSaveManager.RecordQuestProgress(crowd, new[] { 1800f, 2f });
        MetaSaveManager.RecordQuestProgress(crowd, new[] { 900f, 4f });
        MetaSaveManager.RecordQuestProgress(hunt, new[] { 5f });
        Check(MetaSaveManager.Save().Succeeded, "avancée écrite");
        Check(Near(MetaSaveManager.GetQuestProgress("every_door"), 55f), "cumul : 30 + 25 coffres");
        Check(Near(MetaSaveManager.GetQuestProgress("the_round"), 1800f, 4f), "run : meilleure valeur par condition");
        Check(MetaSaveManager.GetQuestProgress("lead_the_hunt").Count == 0, "aucune avancée retenue pour une quête accomplie");

        string before = File.ReadAllText(meta);
        MetaSaveManager.ReloadProfile();
        MetaSaveManager.Load();
        Check(MetaSaveManager.Save().Succeeded && File.ReadAllText(meta) == before, "sauvegarde rechargée puis réécrite à l'identique");
        Check(MetaSaveManager.HasCompletedQuest("lead_the_hunt") && MetaSaveManager.IsWeaponUnlocked("makeshift_bow")
              && Near(MetaSaveManager.GetQuestProgress("every_door"), 55f), "déblocages et avancée retrouvés après rechargement");
        Check(QuestBook.ProgressText(doors, MetaSaveManager.GetQuestProgress("every_door")) == "55 / 100", "avancée lisible");
    }

    private static bool Near(IReadOnlyList<float> values, params float[] expected)
    {
        if (values.Count != expected.Length)
            return false;
        for (int i = 0; i < expected.Length; i++)
            if (!Mathf.IsEqualApprox(values[i], expected[i]))
                return false;
        return true;
    }

    private static JsonObject Quest(JsonObject root, int index) => (JsonObject)root["quests"]![index]!;
    private static JsonObject Condition(JsonObject root, int index) => (JsonObject)Quest(root, index)["conditions"]![0]!;
    private static JsonObject Unlock(JsonObject root, int index) => (JsonObject)Quest(root, index)["unlocks"]![0]!;

    private void Check(bool ok, string label)
    {
        _checks++;
        if (!ok)
            _failures++;
        GD.Print($"[QuestRegression] {(ok ? "OK" : "FAIL")} {label}");
    }
}
