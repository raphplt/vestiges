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
/// Quêtes de déblocage (plan 06 §9) dans un profil neuf et isolé. Lot Q1 : catalogue des 64 quêtes et fixtures
/// refusées, réserve de départ appliquée par l'offre de niveau et le butin, quête accomplie et avancée retenue
/// identiques après rechargement. Lot Q2 (<c>--tracking</c>) : suivi en run, voir QuestRegression.Tracking.
/// </summary>
public partial class QuestRegression : Node
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    private static readonly string[] StartingWeapons =
    {
        "makeshift_bow", "sling", "sharpened_pipe", "crossbow", "cleaver", "whip", "throwing_axes", "compass_needle",
    };

    private static readonly string[] StartingObjects =
    {
        "memoire_vive", "resonance", "portee_etendue", "persistance", "ancrage", "regeneration", "instinct",
        "siphon_essence", "photo_de_classe", "jeton_de_fete", "allumette_humide", "glacon", "de_a_coudre",
        "semelle_usee", "chewing_gum", "plan_de_metro", "flacon_de_pharmacie", "montre_arretee",
        "dossard_froisse", "pastille_mentholee", "bouillotte", "agrafe_rouillee", "bons_points", "bande_magnetique",
        "petite_cle", "compas", "longue_vue", "verre_fele", "yoyo", "boucle_cabossee", "diapositive_en_double",
        "flechette_mousse", "etiquette_perdue", "sonnette", "tampon_annule", "carnet_dadresses", "cassette_rembobinee",
    };

    /// <summary>Réserve de départ des Réminiscences (plan 37 H4) : une par famille de la première offre.</summary>
    private static readonly string[] StartingReminiscences = { "overheal_reserve", "overflow", "xp_trail" };

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
                CheckEarlierScaphandriereUnlock();
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
        Dictionary<UnlockKind, int> unlocks = new()
        {
            [UnlockKind.Character] = 0, [UnlockKind.Weapon] = 0, [UnlockKind.Object] = 0, [UnlockKind.Badge] = 0,
            [UnlockKind.Reminiscence] = 0,
        };
        int dormant = 0;
        foreach (QuestDefinition quest in quests)
        {
            difficulties[quest.Difficulty]++;
            if (quest.Scope == QuestScope.Cumulative)
                cumulative++;
            foreach (QuestUnlock unlock in quest.Unlocks)
                unlocks[unlock.Kind]++;
            if (QuestBook.IsDormant(quest))
                dormant++;
        }
        Check(quests.Count == 64, $"64 quêtes ({quests.Count})");
        Check(difficulties[1] == 20 && difficulties[2] == 34 && difficulties[3] == 10,
            $"20 ★, 34 ★★, 10 ★★★ ({difficulties[1]}, {difficulties[2]}, {difficulties[3]})");
        Check(cumulative == 18, $"dix-huit quêtes de cumul ({cumulative})");
        Check(unlocks[UnlockKind.Reminiscence] == 11 && dormant == 7,
            $"11 Réminiscences par quête, dont 7 en attente de leur effet ({unlocks[UnlockKind.Reminiscence]}, {dormant})");
        Check(unlocks[UnlockKind.Character] == 5 && unlocks[UnlockKind.Weapon] == 18 && unlocks[UnlockKind.Object] == 29
            && unlocks[UnlockKind.Badge] == 6,
            $"5 personnages, 18 armes (13 + 5 de départ de personnage), 29 objets, 6 Écussons ({unlocks[UnlockKind.Character]}, {unlocks[UnlockKind.Weapon]}, {unlocks[UnlockKind.Object]}, {unlocks[UnlockKind.Badge]})");
        // Traqueur de départ, Vagabond gagné par la chasse (plan 41 R6, DECISIONS §129 ter).
        Check(QuestDataLoader.FindUnlocking(UnlockKind.Weapon, "makeshift_bow") == null,
            "l'arc du Traqueur, personnage de départ, est libre");
        Check(QuestDataLoader.FindUnlocking(UnlockKind.Weapon, "chipped_blade")?.Id == "lead_the_hunt",
            "l'arme de départ du Vagabond vient avec lui");
        Check(QuestDataLoader.FindUnlocking(UnlockKind.Weapon, "letter_satchel")?.Id == "the_round",
            "la Sacoche de lettres vient avec le Facteur");
        CharacterData scaphandriere = CharacterDataLoader.Get("scaphandriere");
        WeaponData harpoon = WeaponDataLoader.Get(scaphandriere?.StartingWeaponId ?? "");
        Check(harpoon?.Id == "harpoon_rifle" && harpoon.DefaultFor == scaphandriere.Id
            && harpoon.Source == "default" && harpoon.Tier == 1
            && QuestDataLoader.FindUnlocking(UnlockKind.Weapon, harpoon.Id)?.Id == "raise_the_bodies"
            && QuestDataLoader.FindUnlocking(UnlockKind.Character, scaphandriere.Id)?.Id == "raise_the_bodies",
            "Scaphandrière et harpon de départ partagent Remonter les corps");
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
            ("personnage inconnu sans nom", root => { Unlock(root, 4)["id"] = "unknown_character"; Unlock(root, 4).Remove("name"); }, "inconnu sans name"),
            ("difficulté hors bornes", root => Quest(root, 0)["difficulty"] = 4, "difficulty"),
            ("clé inconnue", root => Quest(root, 0)["reward"] = "souvenir", "clé « reward » inconnue"),
            ("id en double", root => Quest(root, 1)["id"] = "lead_the_hunt", "id en double"),
            ("Écusson déclaré objet", root => Unlock(root, 20)["type"] = "object", "pièce inconnue"),
            ("objet déclaré Écusson", root => Unlock(root, 18)["type"] = "badge", "pièce inconnue"),
            ("Réminiscence inconnue", root => Unlock(root, 38)["id"] = "seconde_peau", "pièce inconnue"),
        };
        Func<string, bool> weapons = id => WeaponDataLoader.Get(id) != null;
        Func<string, bool> objects = id => PassiveSouvenirDataLoader.GetAll().Exists(item => item.Id == id && !item.IsBadge);
        Func<string, bool> badges = id => PassiveSouvenirDataLoader.GetAll().Exists(item => item.Id == id && item.IsBadge);
        Func<string, bool> characters = id => CharacterDataLoader.Get(id) != null;
        foreach ((string label, Action<JsonObject> mutate, string expected) in cases)
        {
            JsonObject root = JsonNode.Parse(valid)!.AsObject();
            mutate(root);
            string message = QuestDataLoader.Apply(root.ToJsonString(), weapons, objects, badges, characters,
                QuestDataLoader.ReminiscenceExists);
            Check(message != null && message.Contains(expected, StringComparison.Ordinal), $"quêtes refusées ({label}) : {message}");
        }
        Check(QuestDataLoader.GetAll().Count == 64, "un fichier refusé ne remplace pas le catalogue chargé");
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
        Check(objects.SetEquals(StartingObjects), $"profil neuf : 27 objets et 10 Écussons de départ ({objects.Count})");
        int pending = 0, epics = 0, legendaries = 0;
        foreach (PassiveSouvenirData item in PassiveSouvenirDataLoader.GetAll())
        {
            if (item.IsBadge) continue;
            if (!item.InitialUnlock)
            {
                pending++;
                Check(QuestDataLoader.FindUnlocking(UnlockKind.Object, item.Id) != null
                    && !MetaSaveManager.IsObjectUnlocked(item.Id), $"{item.Id} : quête active requise au profil neuf");
            }
            if (objects.Contains(item.Id) && item.Rarity == "epic") epics++;
            if (objects.Contains(item.Id) && item.Rarity == "legendary") legendaries++;
        }
        Check(pending == 15 && epics >= 3 && legendaries >= 2 && objects.Contains("verre_fele"),
            "quinze nouveaux accès réservés ; trois épiques, deux légendaires et le critique accessibles au profil neuf");

        List<string> characters = new();
        foreach (CharacterData character in CharacterDataLoader.GetAll())
            if (MetaSaveManager.IsCharacterUnlocked(character.Id))
                characters.Add(character.Id);
        Check(characters.Count == 1 && characters[0] == "traqueur", $"profil neuf : le Traqueur seul ({string.Join(", ", characters)})");
        Check(!MetaSaveManager.IsCharacterUnlocked("scaphandriere") && !MetaSaveManager.IsWeaponUnlocked("harpoon_rifle"),
            "profil neuf : Scaphandrière et harpon verrouillés ensemble");

        HashSet<string> reminiscences = new();
        foreach (PerkSpecializationData perk in PerkSpecializationDataLoader.GetAll())
            if (MetaSaveManager.IsReminiscenceUnlocked(perk.Id) && PerkSpecializationEffects.IsImplemented(perk.Effect))
                reminiscences.Add(perk.Id);
        Check(reminiscences.SetEquals(StartingReminiscences),
            $"profil neuf : les 3 Réminiscences de départ ({string.Join(", ", reminiscences)})");

        bool lootInPool = true;
        for (int i = 0; i < 300; i++)
            lootInPool &= Array.IndexOf(StartingWeapons, LootRewards.PickRandomWeapon()?.Id) >= 0;
        Check(lootInPool, "butin d'arme : 300 tirages dans la réserve de départ");

        Player player = GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate<Player>();
        AddChild(player);
        player.InitializeCharacter(CharacterDataLoader.Get("traqueur"));
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
        expectedWeapons.Remove("makeshift_bow");
        Check(offeredWeapons.SetEquals(expectedWeapons), $"offre de niveau : les 7 autres armes de départ ({offeredWeapons.Count})");
        HashSet<string> startingBadges = new();
        foreach (string id in StartingObjects)
            if (PassiveSouvenirDataLoader.Get(id).IsBadge)
                startingBadges.Add(id);
        Check(startingBadges.Count == 10 && offeredObjects.SetEquals(startingBadges),
            $"offre de niveau : les 10 Écussons de départ, aucun objet (ils se trouvent dans le monde) ({offeredObjects.Count})");
        RemoveChild(fragments);
        fragments.QueueFree();
        RemoveChild(player);
        player.QueueFree();
    }

    private void CheckClaimAndReload()
    {
        PassiveSouvenirData reserved = PassiveSouvenirDataLoader.Get("thermometre");
        bool initialUnlock = reserved.InitialUnlock;
        reserved.InitialUnlock = false;
        try
        {
            QuestDefinition itemQuest = QuestDataLoader.FindUnlocking(UnlockKind.Object, reserved.Id);
            bool closed = !MetaSaveManager.IsObjectUnlocked(reserved.Id);
            MetaSaveManager.ClaimQuest(itemQuest, out _);
            Check(closed && MetaSaveManager.IsObjectUnlocked(reserved.Id),
                "une quête accomplie ouvre un objet même avec initial_unlock=false");
        }
        finally { reserved.InitialUnlock = initialUnlock; }
        string meta = ProjectSettings.GlobalizePath(DevelopmentMode.GetSavePath("meta_save.json"));
        QuestDefinition hunt = QuestDataLoader.Get("lead_the_hunt");
        Check(MetaSaveManager.ClaimQuest(hunt, out SaveFile.WriteResult saved) && saved.Succeeded, "quête accomplie, écrite aussitôt");
        Check(File.ReadAllText(meta).Contains("\"lead_the_hunt\""), "quête accomplie sur disque");
        Check(MetaSaveManager.IsCharacterUnlocked("vagabond") && MetaSaveManager.IsWeaponUnlocked("chipped_blade"),
            "Vagabond et sa lame débloqués");
        Check(!MetaSaveManager.ClaimQuest(hunt, out _), "une quête ne s'accomplit qu'une fois");

        QuestDefinition voices = QuestDataLoader.Get("hear_the_voices");
        Check(MetaSaveManager.ClaimQuest(voices, out _) && MetaSaveManager.IsCharacterUnlocked("eveillee"),
            "l'Éveillée garde son déblocage");

        QuestDefinition bodies = QuestDataLoader.Get("raise_the_bodies");
        Check(MetaSaveManager.ClaimQuest(bodies, out SaveFile.WriteResult bodiesSaved) && bodiesSaved.Succeeded
            && MetaSaveManager.IsCharacterUnlocked("scaphandriere") && MetaSaveManager.IsWeaponUnlocked("harpoon_rifle"),
            "Remonter les corps écrit les droits Scaphandrière et Fusil-harpon ensemble");

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
        Check(MetaSaveManager.HasCompletedQuest("lead_the_hunt") && MetaSaveManager.IsWeaponUnlocked("chipped_blade")
              && Near(MetaSaveManager.GetQuestProgress("every_door"), 55f), "déblocages et avancée retrouvés après rechargement");
        Check(QuestBook.ProgressText(doors, MetaSaveManager.GetQuestProgress("every_door")) == "55 / 60", "avancée lisible");
        Check(MetaSaveManager.IsCharacterUnlocked("scaphandriere") && MetaSaveManager.IsWeaponUnlocked("harpoon_rifle")
            && !MetaSaveManager.ClaimQuest(bodies, out _), "Scaphandrière et harpon retrouvés sans nouvel accomplissement");
    }

    private void CheckEarlierScaphandriereUnlock()
    {
        string path = ProjectSettings.GlobalizePath(DevelopmentMode.GetSavePath("meta_save.json"));
        string current = File.ReadAllText(path);
        // Profil écrit avant l'entrée au catalogue : seule la quête existe, sans liste de droits de l'arme.
        const string earlier = """
            {"version":2,"vestiges":37,"completed_quests":["raise_the_bodies"],"new_unlocks":["character:scaphandriere"]}
            """;
        try
        {
            File.WriteAllText(path, earlier);
            MetaSaveManager.ReloadProfile();
            MetaSaveManager.Load();
            Check(MetaSaveManager.IsCharacterUnlocked("scaphandriere") && MetaSaveManager.IsWeaponUnlocked("harpoon_rifle")
                && MetaSaveManager.GetVestiges() == 37 && MetaSaveManager.GetCompletedQuests().Count == 1,
                "ancienne sauvegarde : harpon ouvert par la quête déjà accomplie, acquis conservés");
            Check(!MetaSaveManager.ClaimQuest(QuestDataLoader.Get("raise_the_bodies"), out _)
                && File.ReadAllText(path) == earlier, "ancienne sauvegarde : aucun nouvel accomplissement ni réécriture forcée");
            Check(MetaSaveManager.Save().Succeeded, "ancienne sauvegarde : sauvegarde normale possible");
            MetaSaveManager.ReloadProfile();
            Check(MetaSaveManager.IsWeaponUnlocked("harpoon_rifle") && MetaSaveManager.IsCharacterUnlocked("scaphandriere"),
                "ancienne sauvegarde : les deux droits persistent au rechargement suivant");
        }
        finally
        {
            File.WriteAllText(path, current);
            MetaSaveManager.ReloadProfile();
        }
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
