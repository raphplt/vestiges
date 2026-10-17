using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Godot;
using Vestiges.Infrastructure;
using Vestiges.Progression;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// Contrats des catalogues de réglages (plan 26 Q7a) : les fichiers du dépôt sont acceptés ; chaque règle a sa fixture
/// négative (champ absent, mauvais type, borne, clé inconnue, clé en double, créature inconnue), refusée avec un message
/// qui nomme le champ, sans rien publier.
/// </summary>
public partial class CatalogContractRegression : Node
{
    private int _checks;

    public override void _Ready()
    {
        try
        {
            EnemyDataLoader.Load();
            CheckXpCurve();
            CheckScore();
            CheckPeril();
            CheckObjects();
            CheckRarities();
            CheckOffer();
            CheckLandmarks();
            CheckBlessings();
            CheckOublis();
            CheckChests();
            CheckFieldBonuses();
            CheckSmallPlaces();
            CheckWaymarks();
            CheckBiomes();
            CheckWorldGen();
            CheckSites();
            CheckCrises();
            CheckErasure();
            CheckRunEvents();
            CheckSpawnFlow();
            CheckCrowd();
            CheckVariants();
            CheckCharacters();
            CheckAtmosphere();
            CheckWeather();
            CheckFauna();
            CheckExodus();
            CheckAbandonedVoices();
            CheckFalseLights();
            GD.Print($"[CatalogContractRegression] RESULT failures=0 checks={_checks}");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError($"[CatalogContractRegression] FAIL {exception}");
            GetTree().Quit(1);
        }
    }

    private void CheckXpCurve()
    {
        string valid = FileAccess.GetFileAsString("res://data/scaling/progression.json");
        Check(XpCurveConfig.TryParse(valid, out _, out string error), $"courbe d'XP du dépôt acceptée {error}");
        (string, Action<JsonObject>, string)[] cases =
        {
            ("champ absent", root => root.Remove("base_xp"), "base_xp absent"),
            ("coût nul", root => root["base_xp"] = 0, "base_xp : 0 (strictement positif attendu)"),
            ("plafond nul", root => root["max_xp_per_level"] = 0, "max_xp_per_level : 0"),
            ("texte au lieu d'un nombre", root => root["exponent"] = "vite", "exponent : nombre fini attendu"),
            ("niveaux précoces non entiers", root => root["early_levels"] = 2.5, "early_levels : 2.5 (entier de 0 à 100 attendu)"),
            ("clé inconnue", root => root["base_xpp"] = 20, "clé « base_xpp » inconnue"),
        };
        foreach ((string label, Action<JsonObject> mutate, string expected) in cases)
        {
            bool rejected = !XpCurveConfig.TryParse(Mutated(valid, mutate), out XpCurveConfig config, out string message);
            Check(rejected && config == null && message.Contains(expected, StringComparison.Ordinal), $"courbe d'XP refusée ({label}) : {message}");
        }
        bool duplicate = !XpCurveConfig.TryParse("{\"base_xp\": 22, \"base_xp\": 0, \"exponent\": 1.42, \"early_levels\": 5, \"early_multiplier_start\": 1.65, \"early_multiplier_end\": 1.2, \"max_xp_per_level\": 20000}",
            out _, out string duplicateError);
        Check(duplicate && duplicateError.Contains("« base_xp » en double", StringComparison.Ordinal), $"courbe d'XP refusée (clé en double) : {duplicateError}");
    }

    private void CheckScore()
    {
        string valid = FileAccess.GetFileAsString("res://data/scaling/score.json");
        Check(ScoreConfig.TryParse(valid, out ScoreConfig parsed, out string error), $"barème du dépôt accepté {error}");
        Check(parsed != null && parsed.VestigesFor(17) == Mathf.FloorToInt(17 / parsed.KillsPerVestige), "Vestiges = éliminations ÷ taux, arrondis en dessous");
        (string, Action<JsonObject>, string)[] cases =
        {
            ("taux absent", root => root.Remove("kills_per_vestige"), "kills_per_vestige absent"),
            ("taux nul", root => root["kills_per_vestige"] = 0, "kills_per_vestige : 0"),
            ("texte au lieu d'un nombre", root => root["kills_per_vestige"] = "un", "kills_per_vestige : nombre fini attendu"),
            ("ancien barème", root => root["kill_points"] = new JsonObject(), "clé « kill_points » inconnue"),
        };
        foreach ((string label, Action<JsonObject> mutate, string expected) in cases)
        {
            bool rejected = !ScoreConfig.TryParse(Mutated(valid, mutate), out ScoreConfig config, out string message);
            Check(rejected && config == null && message.Contains(expected, StringComparison.Ordinal), $"barème refusé ({label}) : {message}");
        }
    }

    private void CheckPeril()
    {
        string valid = FileAccess.GetFileAsString("res://data/scaling/peril.json");
        Check(PerilDataLoader.Apply(valid) == null, "Péril du dépôt accepté");
        int ceiling = PerilDataLoader.ActiveEnemiesCeiling;
        float count = PerilDataLoader.EnemyCountMultiplier(100);
        (string, Action<JsonObject>, string)[] cases =
        {
            ("bloc absent", root => root.Remove("banish"), "section banish absente"),
            ("diviseur nul", root => root["banish"]!["peril_divisor"] = 0, "peril_divisor : 0 (entier de 1 à 1000 attendu)"),
            ("valeur négative", root => root["per_point"]!["enemy_hp"] = -0.1, "enemy_hp : -0.1 (positif ou nul attendu)"),
            ("valeur absente", root => root["per_point"]!.AsObject().Remove("enemy_hp"), "enemy_hp absent"),
            ("clé inconnue", root => root["per_point"]!["xp"] = 0.1, "clé « xp » inconnue"),
            ("plafond de coût non entier", root => root["active_enemies_ceiling"] = 9.5, "active_enemies_ceiling : 9.5"),
            ("bonus de nombre négatif", root => root["enemy_count_bonus_max"] = -1, "enemy_count_bonus_max : -1"),
        };
        foreach ((string label, Action<JsonObject> mutate, string expected) in cases)
        {
            JsonObject root = JsonNode.Parse(valid)!.AsObject();
            mutate(root);
            // Une valeur valide changée en même temps prouve qu'un refus ne publie rien, même partiellement.
            if (root["active_enemies_ceiling"] is JsonValue value && value.TryGetValue(out int _))
                root["active_enemies_ceiling"] = 7;
            string message = PerilDataLoader.Apply(root.ToJsonString());
            Check(message != null && message.Contains(expected, StringComparison.Ordinal), $"Péril refusé ({label}) : {message}");
        }
        Check(PerilDataLoader.ActiveEnemiesCeiling == ceiling && Mathf.IsEqualApprox(PerilDataLoader.EnemyCountMultiplier(100), count),
            "Péril : aucun refus n'a publié de valeur");
    }

    private void CheckObjects()
    {
        string valid = FileAccess.GetFileAsString("res://data/progression/passive_souvenirs.json");
        Check(ObjectDataValidator.TryParseContract(FileAccess.GetFileAsString(ObjectDataValidator.ContractPath), out ObjectDataValidator.Contract contract, out string contractError),
            $"contrat des objets lu {contractError}");
        Func<string, bool> exists = path => ResourceLoader.Exists(path);
        string accepted = ObjectDataValidator.Validate(valid, contract, exists);
        Check(accepted == null, $"objets du dépôt acceptés, désactivés compris {accepted}");
        (string, Action<JsonArray>, string)[] cases =
        {
            ("statistique inconnue", items => Effect(items, "memoire_vive")["stat"] = "attack_sped", "statistique « attack_sped » inconnue"),
            ("accès initial non booléen", items => Item(items, "dossard_froisse")["initial_unlock"] = "false", "initial_unlock : booléen attendu"),
            ("accès réservé sur Écusson", items => Item(items, "memoire_vive")["initial_unlock"] = false, "initial_unlock : réservé aux objets"),
            ("modificateur non admis", items => Effect(items, "memoire_vive")["modifier_type"] = "additive", "modificateur « additive » non admis"),
            ("pas nul", items => Effect(items, "memoire_vive")["step"] = 0, "step : 0 (pas non nul attendu)"),
            ("réglage exigé absent", items => Item(items, "allumette_humide")["params"]!.AsObject().Remove("burn_seconds"), "burn_seconds absent (exigé par ses statistiques)"),
            ("réglage inconnu", items => Item(items, "allumette_humide")["params"]!["burn_secs"] = 3, "réglage « burn_secs » inconnu de ses statistiques"),
            ("réglage du texte absent", items => Item(items, "memoire_vive")["description"] = "Plus vite pendant {duree} s.", "jeton « duree » absent de params"),
            ("aucun texte", items => { JsonObject item = Item(items, "memoire_vive").AsObject(); item.Remove("description"); item.Remove("flavor"); }, "description ou flavor attendue"),
            ("palier inconnu", items => Milestone(items, "memoire_vive")["effect"] = "repeat_atack", "effet « repeat_atack » inconnu"),
            ("palier trop tôt", items => Milestone(items, "memoire_vive")["level"] = 1, "level : 1 (entier de 2 à 99 attendu)"),
            ("palier au-delà du maximum", items => Milestone(items, "memoire_vive")["level"] = 100, "level : 100"),
            ("étagère inconnue", items => Item(items, "memoire_vive")["kind"] = "tome", "kind : « badge » ou « item » attendu"),
            ("étagère absente", items => Item(items, "glacon").Remove("kind"), "kind : « badge » ou « item » attendu"),
            ("sous-titre vide", items => Item(items, "memoire_vive")["subtitle"] = "", "subtitle : texte non vide attendu"),
            ("limite d’un Écusson", items => Item(items, "memoire_vive")["max_copies"] = 1, "max_copies : réservé aux objets"),
            ("ancienne courbe refusée", items => Item(items, "glacon")["copy_levels"] = new JsonArray(10, 31), "clé « copy_levels » inconnue"),
            ("rareté absente", items => Item(items, "glacon").Remove("rarity"), "rarity : rareté fixe valide attendue"),
            ("rareté inconnue", items => Item(items, "glacon")["rarity"] = "mythic", "rarity : rareté fixe valide attendue"),
            ("limite négative", items => Item(items, "glacon")["max_copies"] = -1, "max_copies"),
            ("objet ordinaire limité", items => Item(items, "glacon")["max_copies"] = 6, "limite réservée aux légendaires ou objets uniques"),
            ("légendaire sans limite", items => Item(items, "ticket_de_consigne")["max_copies"] = 0, "limite positive attendue pour un légendaire"),
            ("unique incohérent", items => Item(items, "trefle_plastifie")["max_copies"] = 2, "unique : un seul exemplaire attendu"),
            ("palier d’objet refusé", items => Item(items, "glacon")["milestones"] = new JsonArray(), "milestones : réservés aux Écussons"),
            ("Écusson sans sous-titre", items => Item(items, "memoire_vive").Remove("subtitle"), "subtitle : attendu pour un Écusson"),
            ("objet avec sous-titre", items => Item(items, "glacon")["subtitle"] = "Glaçon", "subtitle : réservé aux Écussons"),
            ("réglage de palier absent", items => Milestone(items, "memoire_vive")["params"]!.AsObject().Remove("every"), "every absent (exigé par repeat_attack)"),
            ("réglage de palier inconnu", items => Milestone(items, "memoire_vive")["params"]!["toujours"] = 1, "réglage « toujours » inconnu de repeat_attack"),
            ("identifiant en double", items => Item(items, "ancrage")["id"] = "memoire_vive", "identifiant en double"),
            ("champ inconnu", items => Item(items, "memoire_vive")["rarete"] = "rare", "clé « rarete » inconnue"),
            ("icône introuvable", items => Item(items, "memoire_vive")["icon"] = "res://assets/items/icons/item_absent.png", "icon : image"),
            ("couleur invalide", items => Item(items, "memoire_vive")["icon_color"] = new JsonArray(1, 2), "icon_color : trois composantes"),
            ("trop d'effets", items => { JsonArray effects = Item(items, "memoire_vive")["effects"]!.AsArray(); for (int i = 0; i < 8; i++) effects.Add(effects[0]!.DeepClone()); }, "effects : 1 à 8 effets attendus"),
            ("facteur qui s'annule", items => Effect(items, "memoire_vive")["step"] = -0.1, "attack_speed : le facteur s'annule avant le niveau 99"),
            ("réglages qui ne sont pas un objet", items => Item(items, "memoire_vive")["params"] = new JsonArray(1), "params : objet attendu"),
            ("statistique à réglages en second effet", items => Item(items, "oeil_critique")["effects"]!.AsArray().Add(JsonNode.Parse("{\"stat\": \"crit_echo\", \"modifier_type\": \"additive\", \"step\": 0.06}")), "radius absent (exigé par ses statistiques)"),
            ("statistique inactive sur un objet actif", items => Effect(items, "memoire_vive")["stat"] = "cooldown_reduction", "statistique « cooldown_reduction » inconnue"),
        };
        foreach ((string label, Action<JsonArray> mutate, string expected) in cases)
        {
            JsonArray items = JsonNode.Parse(valid)!.AsArray();
            mutate(items);
            string message = ObjectDataValidator.Validate(items.ToJsonString(), contract, exists);
            Check(message != null && message.Contains(expected, StringComparison.Ordinal), $"objets refusés ({label}) : {message}");
        }
    }

    private void CheckRarities()
    {
        string valid = FileAccess.GetFileAsString("res://data/progression/upgrade_rarities.json");
        Check(UpgradeRoller.Apply(valid) == null, "raretés du dépôt acceptées");
        float legendaryWeight = UpgradeRoller.Get("legendary").Weight;
        int count = UpgradeRoller.Rarities.Count;
        (string, Action<JsonObject>, string)[] cases =
        {
            ("poids nul", root => Rarity(root, 1)["weight"] = 0, "rareté uncommon, weight : 0"),
            ("chance de montée au-delà de 1", root => Rarity(root, 0)["bump_chance"] = 1.5, "bump_chance : 1.5"),
            ("statistiques non entières", root => Rarity(root, 2)["weapon_stats"] = 1.5, "weapon_stats : 1.5"),
            ("gain d'objet au-delà de la borne des objets", root => Rarity(root, 4)["passive_gain"] = 4, "passive_gain : 4 (au plus 3"),
            ("identifiant en double", root => Rarity(root, 1)["id"] = "common", "identifiant en double"),
            ("clé inconnue", root => Rarity(root, 0)["poids"] = 1, "clé « poids » inconnue"),
            ("liste vide", root => root["rarities"] = new JsonArray(), "rarities : liste non vide attendue"),
            ("cran de zone absent", root => root["zone_steps"]!.AsObject().Remove("erased"), "erased absent"),
        };
        foreach ((string label, Action<JsonObject> mutate, string expected) in cases)
        {
            JsonObject root = JsonNode.Parse(valid)!.AsObject();
            // Une valeur valide changée en même temps prouve qu'un refus ne publie rien.
            Rarity(root, 4)["weight"] = 99;
            mutate(root);
            string message = UpgradeRoller.Apply(root.ToJsonString());
            Check(message != null && message.Contains(expected, StringComparison.Ordinal), $"raretés refusées ({label}) : {message}");
        }
        Check(UpgradeRoller.Rarities.Count == count && Mathf.IsEqualApprox(UpgradeRoller.Get("legendary").Weight, legendaryWeight),
            "raretés : aucun refus n'a publié de valeur");
    }

    private void CheckOffer()
    {
        string valid = FileAccess.GetFileAsString("res://data/progression/level_up_offer.json");
        Check(LevelUpOfferConfig.TryParse(valid, out _, out string error), $"offre de niveau du dépôt acceptée {error}");
        (string, Action<JsonObject>, string)[] cases =
        {
            ("poids absent", root => root.Remove("upgrade_weight"), "upgrade_weight absent"),
            ("plancher nul", root => root["min_weight"] = 0, "min_weight : 0 (strictement positif attendu)"),
            ("clé inconnue", root => root["weight"] = 1, "clé « weight » inconnue"),
            ("surplus nul", root => root["weapon_surplus_damage"] = 0, "weapon_surplus_damage : 0 (strictement positif attendu)"),
        };
        foreach ((string label, Action<JsonObject> mutate, string expected) in cases)
        {
            bool rejected = !LevelUpOfferConfig.TryParse(Mutated(valid, mutate), out LevelUpOfferConfig config, out string message);
            Check(rejected && config == null && message.Contains(expected, StringComparison.Ordinal), $"offre de niveau refusée ({label}) : {message}");
        }
    }

    private void CheckLandmarks()
    {
        string valid = FileAccess.GetFileAsString("res://data/world/landmarks.json");
        Func<string, bool> exists = path => ResourceLoader.Exists(path);
        string[] rarities = { "common", "uncommon", "rare", "epic", "legendary" };
        Check(LandmarkDataLoader.Apply(valid, exists, rarities) == null, "lieux du dépôt acceptés");
        int shards = LandmarkDataLoader.Memorial.Shards;
        (string, Action<JsonObject>, string)[] cases =
        {
            ("section absente", root => root.Remove("workshop"), "section workshop absente"),
            ("réglage absent", root => root["memorial"]!.AsObject().Remove("hold_time"), "hold_time absent"),
            ("rareté inconnue", root => root["rift"]!["offer_min_rarity"] = "mythique", "offer_min_rarity : « mythique » inconnu"),
            ("image introuvable", root => root["workshop"]!["sprite"] = "assets/landmarks/absent.png", "sprite : image « assets/landmarks/absent.png » introuvable"),
            ("intervalle décroissant", root => root["memorial"]!["shard_distance_px"] = new JsonArray(380, 200), "shard_distance_px : [380, 200]"),
            ("couronne hors de la carte", root => root["rift"]!["placement"]![0]!["band"] = new JsonArray(0.5, 1.5), "band : [0.5, 1.5]"),
            ("placement vide", root => root["workshop"]!["placement"] = new JsonArray(), "placement : liste d'au moins 1 élément(s) attendue"),
            ("prix fractionnaire", root => root["memorial"]!["services"]!["heal_cost"] = 20.5, "heal_cost : 20.5"),
            ("part de soin nulle", root => root["memorial"]!["services"]!["heal_percent"] = 0, "heal_percent : 0"),
            ("chance au-delà de 1", root => root["rift"]!["erased_spawn"]!["chance"] = 1.5, "chance : 1.5 (chance dans [0 ; 1] attendue)"),
            ("Puits absent", root => root.Remove("well"), "section well absente"),
            ("trace nulle", root => root["well"]!["forget"]!["trace_ratio"] = 0, "trace_ratio : 0"),
            ("clé inconnue", root => root["memorial"]!["shard_count"] = 3, "clé « shard_count » inconnue"),
        };
        foreach ((string label, Action<JsonObject> mutate, string expected) in cases)
        {
            JsonObject root = JsonNode.Parse(valid)!.AsObject();
            root["memorial"]!["shards"] = 9;
            mutate(root);
            string message = LandmarkDataLoader.Apply(root.ToJsonString(), exists, rarities);
            Check(message != null && message.Contains(expected, StringComparison.Ordinal), $"lieux refusés ({label}) : {message}");
        }
        Check(LandmarkDataLoader.Memorial.Shards == shards, "lieux : aucun refus n'a publié de valeur");
    }

    private void CheckBlessings()
    {
        string valid = FileAccess.GetFileAsString("res://data/progression/blessings.json");
        Check(BlessingDataLoader.Apply(valid) == null, "bénédictions du dépôt acceptées");
        int count = BlessingDataLoader.All.Count;
        (string, Action<JsonObject>, string)[] cases =
        {
            ("statistique d'objet", root => Entry(root, "blessings", 0)["stat"] = "burn_chance", "« burn_chance » n'est pas une statistique du joueur"),
            ("statistique inconnue", root => Entry(root, "blessings", 0)["stat"] = "max_hpp", "« max_hpp » n'est pas une statistique du joueur"),
            ("modificateur non admis", root => Entry(root, "blessings", 0)["modifier_type"] = "multiplicative", "modifier_type : « multiplicative » non admis pour max_hp"),
            ("valeur nulle", root => Entry(root, "blessings", 1)["amount"] = 0, "amount : 0"),
            ("clé de traduction absente", root => Entry(root, "blessings", 0)["name_key"] = "BLESSING_ABSENTE", "clé « BLESSING_ABSENTE » absente des traductions"),
            ("identifiant en double", root => Entry(root, "blessings", 1)["id"] = "blessing_breath", "identifiant en double"),
            ("champ inconnu", root => Entry(root, "blessings", 0)["rarete"] = "rare", "clé « rarete » inconnue"),
        };
        foreach ((string label, Action<JsonObject> mutate, string expected) in cases)
        {
            string message = BlessingDataLoader.Apply(Mutated(valid, mutate));
            Check(message != null && message.Contains(expected, StringComparison.Ordinal), $"bénédictions refusées ({label}) : {message}");
        }
        Check(BlessingDataLoader.All.Count == count, "bénédictions : aucun refus n'a publié de liste");
    }

    private void CheckOublis()
    {
        string valid = FileAccess.GetFileAsString("res://data/progression/oublis.json");
        Func<string, bool> translated = key => TranslationServer.Translate(key) != key;
        Check(OubliDataLoader.Apply(valid, translated) == null, "Oublis du dépôt acceptés");
        (string, Action<JsonObject>, string)[] cases =
        {
            ("effet inconnu", root => Entry(root, "oublis", 0)["effect"] = "extra_boss", "effect : « extra_boss » inconnu"),
            ("valeur nulle", root => Entry(root, "oublis", 1)["amount"] = 0, "amount : 0 (strictement positif attendu)"),
            ("définitif non booléen", root => Entry(root, "oublis", 5)["permanent"] = "oui", "permanent : booléen attendu"),
            ("description non traduite", root => Entry(root, "oublis", 0)["description_key"] = "OUBLI_ABSENT_DESC", "clé « OUBLI_ABSENT_DESC » absente des traductions"),
            ("identifiant en double", root => Entry(root, "oublis", 1)["id"] = "oubli_fear", "identifiant en double"),
        };
        foreach ((string label, Action<JsonObject> mutate, string expected) in cases)
        {
            string message = OubliDataLoader.Apply(Mutated(valid, mutate), translated);
            Check(message != null && message.Contains(expected, StringComparison.Ordinal), $"Oublis refusés ({label}) : {message}");
        }
    }

    private void CheckChests()
    {
        string chests = FileAccess.GetFileAsString("res://data/chests/chests.json");
        string placement = FileAccess.GetFileAsString("res://data/chests/chest_placement.json");
        ChestDataLoader.References refs = new(path => ResourceLoader.Exists(path),
            DataKeySets.ListIds("res://data/ui/rarities.json", "rarities"),
            DataKeySets.StringList("res://data/enemies/_contract.json", "families"), LootTableLoader.Exists);
        Check(ChestDataLoader.Apply(chests, placement, refs) == null, "coffres du dépôt acceptés (deux fichiers)");
        int groups = ChestDataLoader.LoadPlacement().Groups.Count;
        (string, Action<JsonArray>, string)[] chestCases =
        {
            ("rareté inconnue", list => list[0]!["rarity"] = "mythique", "rarity : « mythique » inconnu"),
            ("famille d'effets inconnue", list => list[0]!["fx_family"] = "soie", "fx_family : « soie » inconnu"),
            ("table de butin inconnue", list => list[1]!["loot_table_id"] = "chest_rarissime", "table « chest_rarissime » inconnue"),
            ("image introuvable", list => list[0]!["sprite_open"] = "assets/chests/absent.png", "sprite_open : image"),
            ("rétrogradation inconnue", list => list[1]!["downgrade_to"] = "chest_bois", "downgrade_to : coffre « chest_bois » inconnu"),
            ("tirages nuls", list => list[2]!["loot_rolls"] = 0, "loot_rolls : 0"),
            ("identifiant en double", list => list[1]!["id"] = "chest_common", "identifiant en double"),
            ("élément qui n'est pas un objet", list => list.Add(1), "chaque coffre doit être un objet"),
        };
        foreach ((string label, Action<JsonArray> mutate, string expected) in chestCases)
        {
            JsonArray list = JsonNode.Parse(chests)!.AsArray();
            mutate(list);
            string message = ChestDataLoader.Apply(list.ToJsonString(), placement, refs);
            Check(message != null && message.Contains(expected, StringComparison.Ordinal), $"coffres refusés ({label}) : {message}");
        }
        (string, Action<JsonObject>, string)[] placementCases =
        {
            ("coffre de groupe inconnu", root => root["groups"]![0]!["chest"] = "chest_bois", "chest : « chest_bois » inconnu"),
            ("dégagement absent", root => root["clearance_px"]!.AsObject().Remove("south"), "south absent"),
            ("couronne hors de la carte", root => root["groups"]![1]!["band"] = new JsonArray(0.4, 1.2), "band : [0.4, 1.2]"),
        };
        foreach ((string label, Action<JsonObject> mutate, string expected) in placementCases)
        {
            string message = ChestDataLoader.Apply(chests, Mutated(placement, mutate), refs);
            Check(message != null && message.Contains(expected, StringComparison.Ordinal), $"placement refusé ({label}) : {message}");
        }
        Check(ChestDataLoader.LoadPlacement().Groups.Count == groups, "coffres : aucun refus n'a publié de valeur");
    }

    private void CheckFieldBonuses()
    {
        string valid = FileAccess.GetFileAsString("res://data/world/field_bonuses.json");
        IReadOnlyCollection<string> sprites = DataKeySets.SectionKeys("res://assets/vfx/pickups/pickups_manifest.json", "pickups");
        IReadOnlyCollection<string> variants = DataKeySets.SectionKeys("res://data/enemies/_variants.json", "variants");
        Func<string, bool> translated = key => TranslationServer.Translate(key) != key;
        Check(FieldBonusDataLoader.Apply(valid, sprites, variants, translated) == null, "bonus lâchés du dépôt acceptés");
        (string, Action<JsonObject>, string)[] cases =
        {
            ("effet inconnu", root => Entry(root, "bonuses", 0)["effect"] = "teleport", "effect : « teleport » inconnu"),
            ("réglage absent", root => Entry(root, "bonuses", 1).Remove("duration_s"), "duration_s absent"),
            ("réglage d'un autre effet", root => Entry(root, "bonuses", 0)["radius_px"] = 100, "clé « radius_px » inconnue"),
            ("image hors du manifeste", root => Entry(root, "bonuses", 0)["sprite"] = "potion", "sprite : « potion » inconnu"),
            ("nom non traduit", root => Entry(root, "bonuses", 0)["name_key"] = "BONUS_ABSENT", "clé « BONUS_ABSENT » absente des traductions"),
            ("variante inconnue", root => root["variant_chance"]!["boss"] = 1.0, "variante « boss » inconnue"),
            ("chance au-delà de 1", root => root["variant_chance"]!["elite"] = 1.5, "elite : 1.5"),
            ("bonus de mort inconnu", root => root["kill_pool"] = new JsonArray("canteen", "potion"), "kill_pool : bonus « potion » inconnu"),
            ("premier bonus de Résurgence inconnu", root => root["crisis_first"] = "potion", "crisis_first : « potion » inconnu"),
            ("couleur invalide", root => Entry(root, "bonuses", 0)["color"] = new JsonArray(0.4, 2, 0.5), "color : trois composantes"),
        };
        foreach ((string label, Action<JsonObject> mutate, string expected) in cases)
        {
            string message = FieldBonusDataLoader.Apply(Mutated(valid, mutate), sprites, variants, translated);
            Check(message != null && message.Contains(expected, StringComparison.Ordinal), $"bonus lâchés refusés ({label}) : {message}");
        }
    }

    private void CheckSmallPlaces()
    {
        string valid = FileAccess.GetFileAsString("res://data/world/small_places.json");
        HashSet<string> props = new();
        foreach (string folder in new[] { "urban_ruins", "forest", "swamp", "wild_fields", "collapsed_quarry" })
            props.UnionWith(DataKeySets.TopLevelKeys($"res://assets/props/{folder}/props_manifest.json"));
        IReadOnlyCollection<string> families = DataKeySets.StringList("res://data/enemies/_contract.json", "families");
        Func<string, bool> translated = key => TranslationServer.Translate(key) != key;
        Check(SmallPlaceDataLoader.Apply(valid, props, families, translated) == null, "petits lieux du dépôt acceptés");
        (string, Action<JsonObject>, string)[] cases =
        {
            ("récompense inconnue", root => Entry(root, "places", 0)["reward"] = "treasure", "reward : « treasure » inconnu"),
            ("réglage exigé absent", root => Entry(root, "places", 0).Remove("amount_max"), "amount_max absent"),
            ("réglage d'une autre récompense", root => Entry(root, "places", 0)["duration_s"] = 5, "clé « duration_s » inconnue"),
            ("fourchette inversée", root => { Entry(root, "places", 0)["amount_min"] = 12; Entry(root, "places", 0)["amount_max"] = 6; }, "amount_max : 6 inférieur à amount_min 12"),
            ("décor inconnu", root => Entry(root, "places", 0)["sprites"] = new JsonArray("prop_puits_magique"), "décor « prop_puits_magique » inconnu des manifestes"),
            ("famille inconnue", root => Entry(root, "places", 0)["family"] = "eau", "family : « eau » inconnu"),
            ("invite non traduite", root => Entry(root, "places", 0)["prompt"] = "PLACE_ABSENT_PROMPT", "clé « PLACE_ABSENT_PROMPT » absente des traductions"),
            ("ligne de lore non traduite", root => Entry(root, "places", 7)["lore"] = new JsonArray("PLACE_PICNIC_LORE_99"), "clé « PLACE_PICNIC_LORE_99 » absente des traductions"),
            ("chance au-delà de 1", root => Entry(root, "places", 3)["chance"] = 1.5, "chance : 1.5"),
            ("identifiant en double", root => Entry(root, "places", 1)["id"] = "crystal_vein", "identifiant en double"),
        };
        foreach ((string label, Action<JsonObject> mutate, string expected) in cases)
        {
            string message = SmallPlaceDataLoader.Apply(Mutated(valid, mutate), props, families, translated);
            Check(message != null && message.Contains(expected, StringComparison.Ordinal), $"petits lieux refusés ({label}) : {message}");
        }
    }

    private void CheckWaymarks()
    {
        string valid = FileAccess.GetFileAsString("res://data/world/waymarks.json");
        ObjectDataValidator.TryParseContract(FileAccess.GetFileAsString(ObjectDataValidator.ContractPath), out ObjectDataValidator.Contract contract, out _);
        List<string> types = new(SmallPlaceDataLoader.PlaceIds()) { "chest", "memorial", "rift", "workshop", "well" };
        Func<string, bool> translated = key => TranslationServer.Translate(key) != key;
        Check(WaymarkDataLoader.Apply(valid, types, contract, translated) == null, "Repères du dépôt acceptés");
        (string, Action<JsonObject>, string)[] cases =
        {
            ("type de lieu inconnu", root => Entry(root, "types", 0)["id"] = "fontaine", "id : « fontaine » inconnu"),
            ("deux gains", root => Entry(root, "types", 10)["banishes"] = 1, "un seul gain attendu"),
            ("aucun gain", root => Entry(root, "types", 11).Remove("banishes"), "un seul gain attendu"),
            ("statistique d'objet", root => Entry(root, "types", 0)["stat"] = "kill_heal", "« kill_heal » n'est pas une statistique du joueur"),
            ("modificateur non admis", root => Entry(root, "types", 0)["modifier_type"] = "multiplicative", "« multiplicative » non admis pour max_hp"),
            ("montant sans statistique", root => Entry(root, "types", 10)["amount"] = 1, "amount et modifier_type ne vont qu'avec stat"),
            ("type en double", root => Entry(root, "types", 1)["id"] = "well", "type en double"),
        };
        foreach ((string label, Action<JsonObject> mutate, string expected) in cases)
        {
            string message = WaymarkDataLoader.Apply(Mutated(valid, mutate), types, contract, translated);
            Check(message != null && message.Contains(expected, StringComparison.Ordinal), $"Repères refusés ({label}) : {message}");
        }
    }

    private void CheckBiomes()
    {
        Func<string, bool> enemyExists = EnemyDataLoader.Exists;
        HashSet<string> pois = new();
        foreach (PoiData poi in PoiDataLoader.GetAll())
            pois.Add(poi.Id);
        Func<string, bool> audioExists = DataKeySets.TopLevelKeys(AudioManager.SoundBankPath).Contains;
        Func<string, bool> resourceExists = path => ResourceLoader.Exists(path);
        Check(BiomeDataLoader.TryLoad(out string loadError), $"biomes du dépôt acceptés {loadError}");
        List<BiomeData> original = BiomeDataLoader.GetAll();
        Check(original.Count == 5, "les cinq biomes sont publiés ensemble");
        BiomeData forest = BiomeDataLoader.Get("forest_reclaimed");
        string valid = FileAccess.GetFileAsString("res://data/biomes/forest_reclaimed.json");
        (string, Action<JsonObject>, string)[] cases =
        {
            ("id absent", root => root.Remove("id"), "id absent"),
            ("id non textuel", root => root["id"] = 12, "id : texte non vide"),
            ("champ inconnu", root => root["danger_levell"] = 1, "danger_levell"),
            ("poids absents", root => root.Remove("terrain_weights"), "terrain_weights absente"),
            ("poids non objet", root => root["terrain_weights"] = 1, "terrain_weights absente"),
            ("terrain inconnu", root => root["terrain_weights"]!["lava"] = 1, "terrain_weights.lava"),
            ("poids négatif", root => root["terrain_weights"]!["grass"] = -1, "grass : -1"),
            ("poids non numérique", root => root["terrain_weights"]!["grass"] = "beaucoup", "grass : nombre fini"),
            ("total nul", root => root["terrain_weights"] = new JsonObject { ["grass"] = 0 }, "total fini strictement positif"),
            ("poids trop grand", root => root["terrain_weights"]!["grass"] = 1e300, "grass : nombre fini"),
            ("somme trop grande", root => root["terrain_weights"] = new JsonObject { ["grass"] = 3e38, ["forest"] = 3e38 }, "total fini strictement positif"),
            ("pool absent", root => root.Remove("exploration_enemy_pool"), "exploration_enemy_pool"),
            ("pool vide", root => root["resurgence_enemy_pool"] = new JsonArray(), "resurgence_enemy_pool"),
            ("pool non liste", root => root["exploration_enemy_pool"] = "rodeur", "exploration_enemy_pool"),
            ("créature inconnue", root => root["exploration_enemy_pool"]![0] = "rodeurr", "exploration_enemy_pool[0]"),
            ("créature non textuelle", root => root["resurgence_enemy_pool"]![0] = 1, "resurgence_enemy_pool[0]"),
            ("danger fractionnaire", root => root["danger_level"] = 1.5, "danger_level"),
            ("compte négatif", root => root["poi_count_min"] = -1, "poi_count_min"),
            ("compte au-delà d'int32", root => root["poi_count_max"] = 2147483648L, "poi_count_max"),
            ("intervalle inversé", root => root["poi_count_max"] = 1, "intervalle croissant"),
            ("lieu inconnu", root => root["poi_pool"]!["fontaine"] = 1, "poi_pool.fontaine"),
            ("poids de lieu négatif", root => root["poi_pool"]!["anomaly"] = -1, "anomaly : -1"),
            ("poids de carte nul", root => root["map_weight"] = 0, "map_weight : 0"),
            ("poids de carte trop grand", root => root["map_weight"] = 1e300, "map_weight : nombre fini"),
            ("son inconnu", root => root["footstep_audio"] = "sfx_inconnu", "footstep_audio"),
            ("sources absentes", root => root.Remove("tile_sources"), "tile_sources"),
            ("sources vides", root => root["tile_sources"] = new JsonObject(), "au moins un groupe"),
            ("groupe inconnu", root => root["tile_sources"]!["gras"] = new JsonArray("foret/tile_foret_sol_w00"), "tile_sources.gras"),
            ("liste de tuiles vide", root => root["tile_sources"]!["grass"] = new JsonArray(), "grass : liste"),
            ("tuile introuvable", root => root["tile_sources"]!["grass"]![0] = "foret/inconnue", "tuile « foret/inconnue »"),
            ("tuile non textuelle", root => root["tile_sources"]!["grass"]![0] = false, "grass[0]"),
            ("chemin hors tuiles", root => root["tile_sources"]!["grass"]![0] = "../../assets/tiles/foret/tile_foret_sol_w00", "grass[0]"),
            ("Wang sans groupe", root => root["wang_tile_groups"]![0] = "water", "wang_tile_groups[0]"),
            ("Wang en double", root => root["wang_tile_groups"]!.AsArray().Add("grass"), "groupe « grass » en double"),
            ("Wang incomplet", root => root["tile_sources"]!["grass"]!.AsArray().RemoveAt(0), "multiple de 16"),
            ("fusion non booléenne", root => root["blend_terrains"] = 1, "blend_terrains"),
            ("style non objet", root => root["path_style"] = 1, "path_style"),
            ("couleur invalide", root => root["path_style"]!["tone"] = "bleu", "path_style.tone"),
            ("ornières hors borne", root => root["path_style"]!["ruts"] = 1.1, "ruts"),
            ("largeur nulle", root => root["path_style"]!["width_px"] = 0, "width_px"),
            ("champ de style inconnu", root => root["path_style"]!["width"] = 28, "clé « width » inconnue"),
            ("atmosphère non objet", root => root["atmosphere"] = 1, "atmosphere"),
            ("teinte incomplète", root => root["atmosphere"]!["tint"] = new JsonArray(1, 1), "tint"),
            ("contraste hors borne", root => root["atmosphere"]!["contrast"] = 2, "atmosphere.contrast"),
            ("exposition hors borne", root => root["atmosphere"]!["exposure"] = 0.3, "atmosphere.exposure"),
            ("brume hors borne", root => root["atmosphere"]!["fog_density"] = 1.5, "fog_density"),
            ("champ d'atmosphère inconnu", root => root["atmosphere"]!["fog"] = 0.2, "clé « fog » inconnue"),
            ("ombre de nuage trop noire", root => root["atmosphere"]!["cloud_opacity"] = 0.9, "cloud_opacity"),
            ("couverture absente", root => root["atmosphere"]!.AsObject().Remove("cloud_cover"), "cloud_cover"),
            ("ombre portée trop noire", root => root["atmosphere"]!["shadow_opacity"] = 0.9, "shadow_opacity"),
            ("salissure hors borne", root => root["atmosphere"]!["grime"] = 1.5, "grime"),
            ("fond d'ambiance inconnu", root => root["ambience"]!["loops"]![0] = "sfx_inconnu", "son « sfx_inconnu »"),
            ("son ponctuel inconnu", root => root["ambience"]!["accents"]![0] = "sfx_inconnu", "son « sfx_inconnu »"),
            ("sons ponctuels sans intervalle", root => root["ambience"]!.AsObject().Remove("accent_interval_s"), "accent_interval_s"),
            ("particules inconnues", root => root["motes"]![0]!["kind"] = "snow", "kind"),
            ("écran couvert de particules", root => root["motes"]![0]!["amount"] = 500, "amount"),
            ("liste de particules vide", root => root["motes"] = new JsonArray(), "motes"),
            ("réverbération noyée", root => root["ambience"]!["reverb"] = new JsonObject { ["wet"] = 0.8, ["room_size"] = 0.5, ["damping"] = 0.5 }, "reverb.wet"),
        };
        foreach ((string label, Action<JsonObject> mutate, string expected) in cases)
        {
            string first = Mutated(valid, root => { root["id"] = "biome_candidat"; root["map_weight"] = 9; });
            Dictionary<string, string> candidates = new()
            {
                ["res://data/biomes/candidat.json"] = first,
                ["res://data/biomes/invalide.json"] = Mutated(valid, mutate)
            };
            string message = BiomeDataLoader.Apply(candidates, enemyExists, pois.Contains, audioExists, resourceExists);
            Check(message != null && message.Contains("invalide.json", StringComparison.Ordinal)
                && message.Contains(expected, StringComparison.Ordinal) && BiomeDataLoader.Get("biome_candidat") == null
                && ReferenceEquals(BiomeDataLoader.Get("forest_reclaimed"), forest) && BiomeDataLoader.GetAll().Count == original.Count,
                $"biome refusé sans publication ({label}) : {message}");
        }
        foreach ((string text, string expected) in new[]
        {
            ("[]", "objet attendu"), ("null", "objet attendu"), ("{", "JSON illisible"),
            (valid.Replace("\"id\":", "\"id\": \"doublon\", \"id\":", StringComparison.Ordinal), "clé « id » en double"),
            (valid.Replace("\"grass\": 0.30", "\"grass\": 0.30, \"grass\": 1", StringComparison.Ordinal), "clé « grass » en double")
        })
        {
            bool refused = !BiomeDataLoader.TryParse(text, enemyExists, pois.Contains, audioExists, resourceExists, out BiomeData biome, out string error);
            Check(refused && biome == null && error.Contains(expected, StringComparison.Ordinal), $"fiche de biome refusée ({expected}) : {error}");
        }
        Dictionary<string, string> duplicate = new() { ["a.json"] = valid, ["b.json"] = valid };
        Check(BiomeDataLoader.Apply(duplicate, enemyExists, pois.Contains, audioExists, resourceExists)?.Contains("id en double", StringComparison.Ordinal) == true
            && ReferenceEquals(BiomeDataLoader.Get("forest_reclaimed"), forest), "identifiant de biome en double refusé sans publication");
        Check(BiomeDataLoader.Apply(new Dictionary<string, string>(), enemyExists, pois.Contains, audioExists, resourceExists) != null,
            "catalogue de biomes vide refusé");
        string optional = Mutated(valid, root =>
        {
            foreach (string key in new[] { "name", "danger_level", "poi_pool", "poi_count_min", "poi_count_max", "path_style", "wang_tile_groups", "blend_terrains", "atmosphere", "ambience", "motes" })
                root.Remove(key);
        });
        Check(BiomeDataLoader.TryParse(optional, enemyExists, pois.Contains, audioExists, resourceExists, out BiomeData defaults, out _)
            && defaults.Name == "" && defaults.DangerLevel == 1 && defaults.MapWeight == 1f && defaults.PoiCountMin == 3
            && defaults.PoiCountMax == 5 && defaults.PoiPool.Count == 0 && defaults.PathStyle == null
            && defaults.WangTileGroups.Count == 0 && !defaults.BlendTerrains && defaults.Atmosphere == BiomeAtmosphereStyle.Neutral,
            "secours des champs facultatifs conservés");
        Check(forest.Atmosphere.FogDensity > 0f && forest.Atmosphere.TintStrength > 0f, "atmosphère de la forêt lue");
        Check(forest.Ambience.LoopKeys.Count == 1 && forest.Ambience.LoopKeys[0] == "sfx_ambiance_foret" && forest.Ambience.AccentKeys.Count == 2
            && Mathf.IsEqualApprox(forest.Ambience.AccentVolumeDb, -4f), "ambiance de la forêt lue (fond, oiseaux, −4 dB)");
        Check(BiomeDataLoader.Get("collapsed_quarry").Ambience.LoopKeys.Count == 2, "carrière : deux fonds, l'un ou l'autre");
        Check(forest.Motes.Count == 1 && forest.Motes[0].Kind == AtmosphereParticleKind.Leaves && defaults.Motes.Count == 0,
            "feuilles en forêt, aucune particule sans liste");
        Check(defaults.Ambience == BiomeAmbience.Silent, "biome sans bloc d'ambiance : silence et son sec");
        Check(BiomeAtmosphereStyle.Neutral.GradeMultiplier().IsEqualApprox(Vector3.One), "atmosphère neutre sans effet sur la couleur");
        Check(BiomeDataLoader.TryParse(Mutated(valid, root => root["path_style"] = new JsonObject()), enemyExists, pois.Contains, audioExists, resourceExists,
            out BiomeData styleDefaults, out _) && styleDefaults.PathStyle?.WidthPx == PathStyle.Default.WidthPx,
            "style de chemin facultatif : secours conservés");
        Check(forest.ExplorationEnemyPool.Count == 6 && forest.ExplorationEnemyPool[1] == forest.ExplorationEnemyPool[2]
            && forest.TileSources["forest"].Count == 32, "répétitions des créatures et matières de Wang conservées");
        original.Clear();
        Check(BiomeDataLoader.GetAll().Count == 5, "modifier la liste reçue ne modifie pas le catalogue");
        Check(((IDictionary<string, float>)forest.TerrainWeights).IsReadOnly && ((IList<string>)forest.ExplorationEnemyPool).IsReadOnly
            && ((IDictionary<string, IReadOnlyList<string>>)forest.TileSources).IsReadOnly
            && ((IList<string>)forest.TileSources["forest"]).IsReadOnly && ((ISet<string>)forest.WangTileGroups).IsReadOnly,
            "définitions et collections imbriquées publiées en lecture seule");
    }

    /// <summary>Réglages du monde (plan 26 Q7c-4b) : fichier du dépôt accepté, chaque règle refusée avec son champ.</summary>
    private void CheckWorldGen()
    {
        Func<string, bool> biomeExists = id => BiomeDataLoader.Get(id) != null;
        Func<string, bool> propExists = path => ResourceLoader.Exists($"res://assets/props/{path}.png");
        Check(WorldGenConfig.TryLoad(out WorldGenConfig loaded, out string loadError), $"réglages du monde du dépôt acceptés {loadError}");
        Check(loaded.Zones.Count == 3 && loaded.AvailableBiomes.Count == 5 && loaded.Lands.CountScales.Count == 6,
            "réglages du monde : trois zones, cinq biomes, six parts de terre");
        Check(!ReferenceEquals(WorldGenConfig.Load(), WorldGenConfig.Load()), "chaque lecture rend une copie neuve, ajustable par la terre");
        string valid = FileAccess.GetFileAsString(WorldGenConfig.FilePath);
        (string, Action<JsonObject>, string)[] cases =
        {
            ("JSON invalide", null, "JSON invalide"),
            ("champ inconnu", root => root["map_radiuss"] = 200, "clé « map_radiuss » inconnue"),
            ("rayon absent", root => root.Remove("map_radius"), "map_radius absent"),
            ("rayon fractionnaire", root => root["map_radius"] = 200.5, "map_radius"),
            ("rayon trop petit", root => root["map_radius"] = 2, "map_radius"),
            ("hauteur non numérique", root => root["map_radius_y"] = "grand", "map_radius_y"),
            ("lissage absent", root => root.Remove("ca_iterations"), "ca_iterations absent"),
            ("dégagement négatif", root => root["spawn_clearance"] = -1, "spawn_clearance"),
            ("brouillard non entier", root => root["fog_reveal_radius"] = 2.5, "fog_reveal_radius"),
            ("lieux V1 non booléens", root => root["pois_enabled"] = 0, "pois_enabled"),
            ("biomes non liste", root => root["available_biomes"] = "swamp", "available_biomes"),
            ("biome inconnu", root => root["available_biomes"]![0] = "volcan", "biome « volcan » inconnu"),
            ("biome en double", root => root["available_biomes"]!.AsArray().Add("swamp"), "biome « swamp » en double"),
            ("trop de biomes par carte", root => root["biome_count"] = 6, "biome_count"),
            ("espacement nul", root => root["biome_layout"]!["region_spacing"] = 0, "region_spacing"),
            ("décalage hors borne", root => root["biome_layout"]!["spawn_offset_factor"] = 1.5, "spawn_offset_factor"),
            ("disposition inconnue", root => root["biome_layout"]!["spacing"] = 28, "clé « spacing » inconnue"),
            ("fondu non booléen", root => root["ground_blend"]!["enabled"] = "oui", "enabled"),
            ("bande de fondu nulle", root => root["ground_blend"]!["band_px"] = 0, "band_px"),
            ("grain hors borne", root => root["ground_blend"]!["grain"] = 2, "grain"),
            ("bruit de bord négatif", root => root["ground_blend"]!["edge_noise"] = -0.1, "edge_noise"),
            ("chance de boucle hors borne", root => root["paths"]!["extra_edge_chance"] = 1.5, "extra_edge_chance"),
            ("coût nul", root => root["paths"]!["costs"]!["road"] = 0, "road"),
            ("coût inconnu", root => root["paths"]!["costs"]!["lava"] = 1, "clé « lava » inconnue"),
            ("liaisons fractionnaires", root => root["paths"]!["spawn_links"] = 1.5, "spawn_links"),
            ("exploration nulle", root => root["paths"]!["max_expansions"] = 0, "max_expansions"),
            ("style de chemin invalide", root => root["paths"]!["default_style"]!["tone"] = "bleu", "paths.default_style.tone"),
            ("style de chemin inconnu", root => root["paths"]!["default_style"]!["width"] = 28, "clé « width » inconnue"),
            ("jonction hors borne", root => root["junction_props"]!["chance"] = 1.2, "chance"),
            ("bande de jonction négative", root => root["junction_props"]!["band_cells"] = -1, "band_cells"),
            ("paire non triée", root => root["junction_props"]!["pairs"]!["wild_fields+forest_reclaimed"] = new JsonArray("forest/prop_bush"), "wild_fields+forest_reclaimed"),
            ("paire de biome inconnu", root => root["junction_props"]!["pairs"]!["forest_reclaimed+volcan"] = new JsonArray("forest/prop_bush"), "forest_reclaimed+volcan"),
            ("paire sans décor", root => root["junction_props"]!["pairs"]!["swamp+wild_fields"] = new JsonArray(), "swamp+wild_fields"),
            ("décor introuvable", root => root["junction_props"]!["pairs"]!["swamp+wild_fields"]![0] = "swamp/prop_inconnu", "décor « swamp/prop_inconnu »"),
            ("décor hors dossier", root => root["junction_props"]!["pairs"]!["swamp+wild_fields"]![0] = "../tiles/foret/tile_foret_sol_w00", "décor « ../tiles"),
            ("terre sans rayon", root => root["lands"]!.AsObject().Remove("map_radius"), "map_radius absent"),
            ("terre non booléenne", root => root["lands"]!["enabled"] = 1, "enabled"),
            ("part de terre inconnue", root => root["lands"]!["count_scales"]!["fountain"] = 0.3, "sorte de lieu « fountain »"),
            ("part de terre négative", root => root["lands"]!["count_scales"]!["chests"] = -0.1, "chests"),
            ("couronne inversée", root => root["lands"]!["memorial_band"] = new JsonArray(0.9, 0.3), "memorial_band"),
            ("couronne hors borne", root => root["lands"]!["memorial_band"] = new JsonArray(0.3, 1.2), "memorial_band"),
            ("champ de terre inconnu", root => root["lands"]!["radius"] = 116, "clé « radius » inconnue"),
            ("décor bloquant négatif", root => root["props"]!["min_blocking_height"] = -1, "min_blocking_height"),
            ("emprise nulle", root => root["props"]!["footprint_scale"] = 0, "footprint_scale"),
            ("préfixe vide", root => root["props"]!["trample_prefixes"]![0] = "", "trample_prefixes"),
            ("règle de décor inconnue", root => root["props"]!["min_height"] = 18, "clé « min_height » inconnue"),
            ("zones absentes", root => root.Remove("zones"), "zones"),
            ("zones vides", root => root["zones"] = new JsonArray(), "zones"),
            ("rayons non croissants", root => root["zones"]![1]!["max_radius"] = 10, "rayons strictement croissants"),
            ("terrain de zone inconnu", root => root["zones"]![0]!["weights"]!["lava"] = 0.1, "clé « lava » inconnue"),
            ("poids de zone absent", root => root["zones"]![0]!["weights"]!.AsObject().Remove("water"), "water absent"),
            ("poids de zone négatif", root => root["zones"]![0]!["weights"]!["grass"] = -0.5, "grass"),
            ("zone sans poids", root => root["zones"]![0]!["weights"] = new JsonObject { ["grass"] = 0, ["concrete"] = 0, ["water"] = 0, ["forest"] = 0 }, "total strictement positif"),
        };
        foreach ((string name, Action<JsonObject> mutate, string expected) in cases)
        {
            string text = mutate == null ? "{ \"map_radius\": " : Mutated(valid, mutate);
            bool refused = !WorldGenConfig.TryParse(text, biomeExists, propExists, out WorldGenConfig config, out string error);
            Check(refused && config == null && error?.Contains(expected, StringComparison.Ordinal) == true,
                $"monde, {name} refusé ({error})");
        }
        string minimal = Mutated(valid, root =>
        {
            foreach (string key in new[] { "map_radius_y", "spawn_clearance", "lands", "props", "ground_blend", "paths", "junction_props",
                         "edge_fade_width", "biome_count", "biome_layout", "poi_min_distance_between", "pois_enabled",
                         "lore_elements_enabled", "fog_reveal_radius", "fog_initial_clear_radius" })
                root.Remove(key);
        });
        Check(WorldGenConfig.TryParse(minimal, biomeExists, propExists, out WorldGenConfig defaults, out string minimalError)
            && defaults.MapRadiusY == defaults.MapRadius && defaults.SpawnClearance == 4 && defaults.BiomeCount == 3
            && !defaults.Lands.Enabled && defaults.JunctionProps == null && defaults.Paths.MaxExpansions == PathNetworkConfig.Default.MaxExpansions
            && defaults.PropRules.FootprintScale == PropRules.Default.FootprintScale && defaults.FogRevealRadius == 6,
            $"monde : champs facultatifs absents, secours d'avant gardés {minimalError}");
        Check(WorldGenConfig.TryParse(Mutated(valid, root => root.Remove("available_biomes")), biomeExists, propExists, out WorldGenConfig all, out _)
            && all.AvailableBiomes.Count == 0, "monde : biomes absents, liste vide (tous les biomes du catalogue)");
        Check(WorldGenConfig.TryParse(Mutated(valid, root => root["available_biomes"] = new JsonArray()), biomeExists, propExists, out _, out _),
            "monde : liste de biomes vide admise (tous les biomes du catalogue)");
        Check(WorldGenConfig.TryParse(Mutated(valid, root =>
            {
                root["lands"]!["count_scales"]!["rift"] = 1.5;
                root["ground_blend"]!["edge_noise"] = 1.5;
                root["junction_props"]!["band_cells"] = 0;
                root["zones"]![0]!.AsObject().Remove("name");
            }), biomeExists, propExists, out WorldGenConfig loose, out string looseError)
            && loose.Lands.CountScales["rift"] == 1.5f && loose.GroundBlend.EdgeNoise == 1.5f && loose.JunctionProps.BandCells == 0,
            $"monde : part de terre au-delà de 1, bruit de bord ample, bande nulle et zone sans nom admis {looseError}");
    }

    /// <summary>Fermes et chantiers de la carrière (plan 26 Q7c-4c) : fichiers du dépôt acceptés, règles refusées.</summary>
    private void CheckSites()
    {
        Func<string, bool> farmSprite = stem => ResourceLoader.Exists($"{WildFieldsComposer.Folder}{stem}.png");
        Func<string, bool> quarrySprite = stem => ResourceLoader.Exists($"{WorldSetup.QuarryFolder}{stem}.png");
        Check(FarmConfig.TryLoad(out FarmConfig farm, out string farmError) && farm.Site.Enabled && farm.Site.Elements.Count > 0
            && farm.OrchardTrees.Count == 2 && farm.BlockingScenes.Length == 1, $"fermes du dépôt acceptées {farmError}");
        Check(SitePlan.TryLoad(WorldSetup.QuarrySitesPath, "collapsed_quarry", WorldSetup.QuarryFolder, "chantiers", out SitePlan quarry,
            out string quarryError) && quarry.Enabled && quarry.Elements.Count == 23, $"chantiers du dépôt acceptés {quarryError}");
        string quarryText = FileAccess.GetFileAsString(WorldSetup.QuarrySitesPath);
        (string, Action<JsonObject>, string)[] planCases =
        {
            ("JSON invalide", null, "JSON invalide"),
            ("champ inconnu", root => root["enable"] = true, "clé « enable » inconnue"),
            ("activation non booléenne", root => root["enabled"] = 1, "enabled"),
            ("recherche nulle", root => root["search_radius_cells"] = 0, "search_radius_cells"),
            ("emprise à une dimension", root => root["half_extent_px"] = new JsonArray(480), "half_extent_px"),
            ("emprise nulle", root => root["half_extent_px"] = new JsonArray(0, 140), "half_extent_px"),
            ("emprise non numérique", root => root["half_extent_px"] = new JsonArray("large", 140), "half_extent_px"),
            ("portée négative", root => root["path_reach_px"] = -1, "path_reach_px"),
            ("largeur nulle", root => root["spur_width_factor"] = 0, "spur_width_factor"),
            ("éléments non liste", root => root["elements"] = 1, "elements"),
            ("élément sans décor", root => root["elements"]![0]!.AsObject().Remove("sprites"), "sprites"),
            ("liste de décors vide", root => root["elements"]![0]!["sprites"] = new JsonArray(), "sprites"),
            ("décor introuvable", root => root["elements"]![0]!["sprites"]![0] = "prop_inconnu", "décor « prop_inconnu »"),
            ("décor hors dossier", root => root["elements"]![0]!["sprites"]![0] = "../forest/prop_bush", "décor « ../forest/prop_bush »"),
            ("élément sans position", root => root["elements"]![0]!.AsObject().Remove("at"), "at"),
            ("champ d'élément inconnu", root => root["elements"]![0]!["blocked"] = true, "clé « blocked » inconnue"),
            ("chance hors borne", root => root["elements"]![3]!["chance"] = 1.5, "chance"),
            ("rangée sans pas", root => root["elements"]![1]!.AsObject().Remove("step"), "to et step vont ensemble"),
            ("pas sans rangée", root => root["elements"]![0]!["step"] = 10, "to et step vont ensemble"),
            ("pas nul", root => root["elements"]![1]!["step"] = 0, "step"),
        };
        foreach ((string name, Action<JsonObject> mutate, string expected) in planCases)
        {
            string text = mutate == null ? "{ \"enabled\": " : Mutated(quarryText, mutate);
            bool refused = !SitePlan.TryParse(text, "collapsed_quarry", WorldSetup.QuarryFolder, "chantiers", quarrySprite,
                out SitePlan plan, out string error);
            Check(refused && plan == null && error?.Contains(expected, StringComparison.Ordinal) == true, $"chantiers, {name} refusé ({error})");
        }
        string farmText = FileAccess.GetFileAsString(FarmConfig.FilePath);
        (string, Action<JsonObject>, string)[] farmCases =
        {
            ("champ inconnu", root => root["scene"] = new JsonArray(), "clé « scene » inconnue"),
            ("bord de parcelle introuvable", root => root["parcel_edges"]![0] = "prop_haie", "décor « prop_haie »"),
            ("verger à un décor", root => root["orchard_trees"]![0] = new JsonArray("prop_orchard_tree_base"), "orchard_trees"),
            ("verger introuvable", root => root["orchard_trees"]![0]![1] = "prop_canopee", "prop_canopee"),
            ("scène introuvable", root => root["scenes"]![0] = "prop_scene_inconnue", "décor « prop_scene_inconnue »"),
            ("scène bloquante hors des scènes", root => root["blocking_scenes"]![0] = "prop_hedge_h", "absente de scenes"),
            ("chance de scène hors borne", root => root["scene_chance_per_region"] = 2, "scene_chance_per_region"),
            ("élément de ferme invalide", root => root["elements"]![0]!["at"] = new JsonArray(1, 2, 3), "at"),
        };
        foreach ((string name, Action<JsonObject> mutate, string expected) in farmCases)
        {
            bool refused = !FarmConfig.TryParse(Mutated(farmText, mutate), farmSprite, out FarmConfig config, out string error);
            Check(refused && config == null && error?.Contains(expected, StringComparison.Ordinal) == true, $"fermes, {name} refusé ({error})");
        }
        string minimal = Mutated(quarryText, root =>
        {
            foreach (string key in new[] { "search_radius_cells", "half_extent_px", "path_reach_px", "spur_start", "spur_width_factor", "elements" })
                root.Remove(key);
        });
        SitePlan defaults = new();
        Check(SitePlan.TryParse(minimal, "collapsed_quarry", WorldSetup.QuarryFolder, "chantiers", quarrySprite, out SitePlan bare, out string bareError)
            && bare.SearchRadiusCells == defaults.SearchRadiusCells && bare.HalfExtentPx == defaults.HalfExtentPx
            && bare.PathReachPx == defaults.PathReachPx && bare.SpurWidthFactor == defaults.SpurWidthFactor && bare.Elements.Count == 0,
            $"chantiers : champs facultatifs absents, secours d'avant gardés {bareError}");
        Check(FarmConfig.TryParse("{ \"enabled\": false }", farmSprite, out FarmConfig emptyFarm, out _) && !emptyFarm.Site.Enabled
            && emptyFarm.Scenes.Length == 0 && emptyFarm.SceneChancePerRegion == 0.5f, "fermes : fichier minimal, secours d'avant gardés");
    }

    /// <summary>Résurgences, late game et accalmie (plan 26 Q7d-1) : fichier du dépôt accepté, règles refusées.</summary>
    private void CheckCrises()
    {
        Func<string, bool> chestExists = id => ChestDataLoader.Get(id) != null;
        Check(Vestiges.Events.CrisisConfig.TryLoad(out Vestiges.Events.CrisisConfig loaded, out string loadError)
            && loaded.FirstCrisisDelaySec == 360f && loaded.BossCrisisThreshold == 5 && loaded.CalmChest == "chest_rare",
            $"crises du dépôt acceptées {loadError}");
        string valid = FileAccess.GetFileAsString(Vestiges.Events.CrisisConfig.FilePath);
        (string, Action<JsonObject>, string)[] cases =
        {
            ("JSON invalide", null, "JSON invalide"),
            ("champ inconnu", root => root["interval"] = 290, "clé « interval » inconnue"),
            ("premier délai nul", root => root["first_crisis_delay_sec"] = 0, "first_crisis_delay_sec"),
            ("intervalle non numérique", root => root["interval_sec"] = "souvent", "interval_sec"),
            ("variance négative", root => root["interval_variance_sec"] = -5, "interval_variance_sec"),
            ("annonce négative", root => root["warning_duration_sec"] = -1, "warning_duration_sec"),
            ("crise nulle", root => root["crisis_duration_sec"] = 0, "crisis_duration_sec"),
            ("seuil d'oubli hors borne", root => root["late_game_erasure_threshold"] = 1.2, "late_game_erasure_threshold"),
            ("seuil de crises nul", root => root["late_game_crisis_threshold"] = 0, "late_game_crisis_threshold"),
            ("seuil de boss fractionnaire", root => root["boss_crisis_threshold"] = 4.5, "boss_crisis_threshold"),
            ("heure du boss nulle", root => root["boss_time_threshold_sec"] = 0, "boss_time_threshold_sec"),
            ("PV du boss nuls", root => root["boss_hp_scale"] = 0, "boss_hp_scale"),
            ("tempo de fin nul", root => root["endgame_interval_multiplier"] = 0, "endgame_interval_multiplier"),
            ("Essence d'accalmie nulle", root => root["calm_essence_multiplier"] = 0, "calm_essence_multiplier"),
            ("accalmie négative", root => root["calm_essence_seconds"] = -1, "calm_essence_seconds"),
            ("coffre inconnu", root => root["calm_chest"] = "chest_mythique", "coffre « chest_mythique » inconnu"),
            ("coffre vide", root => root["calm_chest"] = "", "calm_chest"),
            ("distance inversée", root => root["calm_chest_distance"] = new JsonArray(340, 220), "calm_chest_distance"),
            ("distance négative", root => root["calm_chest_distance"] = new JsonArray(-10, 220), "calm_chest_distance"),
        };
        foreach ((string name, Action<JsonObject> mutate, string expected) in cases)
        {
            string text = mutate == null ? "{ \"interval_sec\": " : Mutated(valid, mutate);
            bool refused = !Vestiges.Events.CrisisConfig.TryParse(text, chestExists, out Vestiges.Events.CrisisConfig config, out string error);
            Check(refused && config == null && error?.Contains(expected, StringComparison.Ordinal) == true, $"crises, {name} refusé ({error})");
        }
        Check(Vestiges.Events.CrisisConfig.TryParse("{}", chestExists, out Vestiges.Events.CrisisConfig defaults, out _)
            && defaults.FirstCrisisDelaySec == 240f && defaults.IntervalVarianceSec == 45f && defaults.LateGameErasureThreshold == 0.68f
            && defaults.LateGameCrisisThreshold == 4 && defaults.CalmChestDistance == new Vector2(220f, 340f),
            "crises : fichier vide, secours d'avant gardés");
    }

    /// <summary>Effacement (plan 26 Q7d-2) : fichier du dépôt accepté, chaque règle refusée avec son champ.</summary>
    private void CheckErasure()
    {
        Func<string, bool> characterExists = id => CharacterDataLoader.Get(id) != null;
        Func<string, bool> inhabitantExists = id => ResourceLoader.Exists($"res://assets/characters/{id}/char_{id}_S_idle_01.png");
        Check(ErasureConfig.TryLoad(out ErasureConfig loaded, out string loadError) && loaded.Motes != null
            && loaded.Echoes.Inhabitants.Count > 0 && loaded.ZoneEffects.Count == 4, $"Effacement du dépôt accepté {loadError}");
        string valid = FileAccess.GetFileAsString(ErasureConfig.FilePath);
        (string, Action<JsonObject>, string)[] cases =
        {
            ("JSON invalide", null, "JSON invalide"),
            ("champ inconnu", root => root["cellsize"] = 128, "clé « cellsize » inconnue"),
            ("cellule fractionnaire", root => root["cell_size"] = 127.5, "cell_size"),
            ("mémoire semée hors borne", root => root["seeded_memory"] = 1.2, "seeded_memory"),
            ("oubli négatif", root => root["base_decay_per_minute"] = -0.01, "base_decay_per_minute"),
            ("présence nulle", root => root["player_presence_falloff_cells"] = 0, "player_presence_falloff_cells"),
            ("seuil de fin hors borne", root => root["late_game_threshold"] = 2, "late_game_threshold"),
            ("cadence nulle", root => root["update_interval_sec"] = 0, "update_interval_sec"),
            ("rayon suivi nul", root => root["tracked_radius_cells"] = 0, "tracked_radius_cells"),
            ("stabilisation non booléenne", root => root["stabilize_on_chest_open"] = "oui", "stabilize_on_chest_open"),
            ("Néant négatif", root => root["void_damage_ratio_per_second"] = -1, "void_damage_ratio_per_second"),
            ("front invalide", root => root["land_front"]!["heart_radius"] = 1.5, "heart_radius"),
            ("éclats inconnus", root => root["motes"]!["speed"] = 50, "clé « speed » inconnue"),
            ("éclats trop nombreux", root => root["motes"]!["count"] = 100, "count"),
            ("vitesses inversées", root => root["motes"]!["speed_min"] = 200, "speed_min ≤ speed_max"),
            ("vie nulle", root => root["motes"]!["life_min"] = 0, "life_min"),
            ("écho inconnu", root => root["echoes"]!["delay"] = 60, "clé « delay » inconnue"),
            ("intervalle d'écho inversé", root => root["echoes"]!["interval_min_sec"] = 120, "intervalles min ≤ max"),
            ("mémoire d'écho hors borne", root => root["echoes"]!["max_memory"] = 1.5, "max_memory"),
            ("fondu nul", root => root["echoes"]!["fade_sec"] = 0, "fade_sec"),
            ("dissolution nulle", root => root["echoes"]!["dissolve_sec"] = 0, "dissolve_sec"),
            ("murmures fractionnaires", root => root["echoes"]!["whisper_count"] = 2.5, "whisper_count"),
            ("personnage inconnu", root => root["echoes"]!["characters"]![0] = "chevalier", "personnage « chevalier » inconnu"),
            ("habitant sans dessin", root => root["echoes"]!["inhabitants"]![0] = "habitant_fantome", "habitant « habitant_fantome » inconnu"),
            ("habitant en double", root => root["echoes"]!["inhabitants"]!.AsArray().Add("habitant_ecoliere"), "en double"),
            ("phase inconnue", root => root["zone_effects"]!["anchored"] = new JsonObject(), "clé « anchored » inconnue"),
            ("vitesse de phase nulle", root => root["zone_effects"]!["void"]!["speed"] = 0, "speed"),
            ("voile hors borne", root => root["zone_effects"]!["erased"]!["veil"] = 1.5, "veil"),
            ("effet de phase inconnu", root => root["zone_effects"]!["frayed"]!["score"] = 1, "clé « score » inconnue"),
        };
        foreach ((string name, Action<JsonObject> mutate, string expected) in cases)
        {
            string text = mutate == null ? "{ \"cell_size\": " : Mutated(valid, mutate);
            bool refused = !ErasureConfig.TryParse(text, characterExists, inhabitantExists, out ErasureConfig config, out string error);
            Check(refused && config == null && error?.Contains(expected, StringComparison.Ordinal) == true, $"Effacement, {name} refusé ({error})");
        }
        Check(ErasureConfig.TryParse("{}", characterExists, inhabitantExists, out ErasureConfig defaults, out _)
            && defaults.CellSize == 128 && defaults.BaseDecayPerMinute == 0.012f && defaults.StabilizeOnChestOpen && defaults.Motes == null
            && defaults.Echoes.Enabled && defaults.Echoes.FirstDelaySec == 60f && defaults.ZoneEffects.Count == 0,
            "Effacement : fichier vide, secours d'avant gardés (sans éclats ni effets de phase)");
    }

    /// <summary>Micro-événements (plan 26 Q7d-3) : fichier du dépôt accepté, règles refusées sans publication.</summary>
    private void CheckRunEvents()
    {
        Func<string, bool> chestExists = id => ChestDataLoader.Get(id) != null;
        Func<string, bool> translated = key => TranslationServer.Translate(key) != key;
        Check(RunEventDataLoader.TryLoad(out string loadError) && RunEventDataLoader.Events.Count > 0
            && RunEventDataLoader.Schedule.FixedEvents.Count > 0, $"micro-événements du dépôt acceptés {loadError}");
        RunEventData hunt = RunEventDataLoader.Find("hunt");
        string valid = FileAccess.GetFileAsString(RunEventDataLoader.DataPath);
        (string, Action<JsonObject>, string)[] cases =
        {
            ("JSON invalide", null, "JSON invalide"),
            ("champ racine inconnu", root => root["event"] = new JsonArray(), "clé « event » inconnue"),
            ("cadence absente", root => root.Remove("schedule"), "schedule"),
            ("cadence inconnue", root => root["schedule"]!["gap"] = 60, "clé « gap » inconnue"),
            ("premier événement inversé", root => root["schedule"]!["first_event_min_sec"] = 200, "first_event_min_sec ≤ first_event_max_sec"),
            ("mémoire fractionnaire", root => root["schedule"]!["recent_memory"] = 1.5, "recent_memory"),
            ("poids récent hors borne", root => root["schedule"]!["recent_weight_factor"] = 2, "recent_weight_factor"),
            ("rendez-vous inconnu", root => root["schedule"]!["fixed_events"]![0]!["id"] = "pluie", "événement « pluie » inconnu"),
            ("rendez-vous à zéro", root => root["schedule"]!["fixed_events"]![0]!["at_sec"] = 0, "at_sec"),
            ("événements vides", root => root["events"] = new JsonArray(), "events"),
            ("sorte inconnue", root => root["events"]![0]!["kind"] = "carnaval", "« carnaval » inconnu"),
            ("id en double", root => root["events"]![1]!["id"] = root["events"]![0]!["id"]!.GetValue<string>(), "en double"),
            ("titre sans traduction", root => root["events"]![0]!["title_key"] = "EVENT_INCONNU", "sans traduction"),
            ("réglage d'une autre sorte", root => root["events"]![0]!["band_width"] = 280, "clé « band_width » inconnue"),
            ("réglage négatif", root => root["events"]![0]!["reward_essence"] = -5, "reward_essence"),
            ("durée nulle", root => root["events"]![0]!["duration_sec"] = 0, "duration_sec"),
            ("poids négatif", root => root["events"]![1]!["weight"] = -1, "weight"),
            ("coffre inconnu", root => root["events"]![1]!["reward_chest"] = "chest_mythique", "coffre « chest_mythique »"),
            ("créature inconnue", root => root["events"]![1]!["preferred_enemies"]![0] = "dragon", "créature « dragon »"),
            ("distances inversées", root => root["events"]![0]!["spawn_distance_min"] = 9999, "spawn_distance_min ≤ spawn_distance_max"),
        };
        foreach ((string name, Action<JsonObject> mutate, string expected) in cases)
        {
            string text = mutate == null ? "{ \"events\": " : Mutated(valid, mutate);
            string error = RunEventDataLoader.Apply(text, chestExists, EnemyDataLoader.Exists, translated);
            Check(error?.Contains(expected, StringComparison.Ordinal) == true && ReferenceEquals(RunEventDataLoader.Find("hunt"), hunt),
                $"micro-événements, {name} refusé sans publication ({error})");
        }
    }

    /// <summary>Flux d'apparition (plan 26 Q7e-1) : fichier du dépôt accepté, chaque règle refusée avec son champ.</summary>
    private void CheckSpawnFlow()
    {
        Check(Vestiges.Spawn.SpawnFlowConfig.TryLoad(out Vestiges.Spawn.SpawnFlowConfig loaded, out string loadError)
            && loaded.Integer("max_enemies_on_screen") == 110 && loaded.HpSegments.Count == 4
            && loaded.FallbackResurgencePool.Count(id => id == "shade") == 2, $"flux d'apparition du dépôt accepté {loadError}");
        string valid = FileAccess.GetFileAsString(Vestiges.Spawn.SpawnFlowConfig.FilePath);
        (string, Action<JsonObject>, string)[] cases =
        {
            ("JSON invalide", null, "JSON invalide"),
            ("champ inconnu", root => root["max_enemies"] = 110, "clé « max_enemies » inconnue"),
            ("intervalle absent", root => root.Remove("base_spawn_interval"), "base_spawn_interval"),
            ("intervalle nul", root => root["base_spawn_interval"] = 0, "base_spawn_interval"),
            ("intervalle minimal au-dessus du normal", root => root["min_spawn_interval"] = 3, "min_spawn_interval ≤ base_spawn_interval"),
            ("plafond fractionnaire", root => root["max_enemies_on_screen"] = 110.5, "max_enemies_on_screen"),
            ("plafond nul", root => root["max_enemies_on_screen"] = 0, "max_enemies_on_screen"),
            ("décroissance négative", root => root["spawn_interval_decay_per_minute"] = -0.01, "spawn_interval_decay_per_minute"),
            ("PV d'emblée nuls", root => root["flat_hp_multiplier"] = 0, "flat_hp_multiplier"),
            ("chance de groupe hors borne", root => root["same_type_cluster_chance"] = 1.5, "same_type_cluster_chance"),
            ("groupe inversé", root => root["same_type_cluster_min"] = 6, "same_type_cluster_min ≤ same_type_cluster_max"),
            ("écart inversé", root => root["same_type_cluster_spacing_min"] = 60, "same_type_cluster_spacing_min ≤ same_type_cluster_spacing_max"),
            ("marge inversée", root => root["spawn_screen_margin_min"] = 200, "spawn_screen_margin_min ≤ spawn_screen_margin_max"),
            ("rafale de crise fractionnaire", root => root["crisis_burst_base"] = 8.5, "crisis_burst_base"),
            ("arc au-delà du tour", root => root["spawn_forward_arc_degrees"] = 400, "spawn_forward_arc_degrees"),
            ("biais hors borne", root => root["spawn_forward_bias"] = -0.1, "spawn_forward_bias"),
            ("Péril négatif", root => root["time_peril_per_minute"] = -0.5, "time_peril_per_minute"),
            ("secours inconnu", root => root["fallback_exploration_pool"]![0] = "dragon", "créature « dragon » inconnue"),
            ("secours vide", root => root["fallback_resurgence_pool"] = new JsonArray(), "fallback_resurgence_pool"),
            ("secours absent", root => root.Remove("fallback_exploration_pool"), "fallback_exploration_pool"),
            ("segment inconnu", root => root["hp_scaling_segments"]![0]!["slope"] = 1.1, "clé « slope » inconnue"),
            ("pente nulle", root => root["damage_scaling_segments"]![0]!["per_minute"] = 0, "per_minute"),
            ("minute négative", root => root["hp_scaling_segments"]![1]!["from_minute"] = -4, "from_minute"),
        };
        foreach ((string name, Action<JsonObject> mutate, string expected) in cases)
        {
            string text = mutate == null ? "{ \"base_spawn_interval\": " : Mutated(valid, mutate);
            bool refused = !Vestiges.Spawn.SpawnFlowConfig.TryParse(text, EnemyDataLoader.Exists, out Vestiges.Spawn.SpawnFlowConfig config, out string error);
            Check(refused && config == null && error?.Contains(expected, StringComparison.Ordinal) == true, $"flux d'apparition, {name} refusé ({error})");
        }
        const string minimal = "{ \"base_spawn_interval\": 2, \"min_spawn_interval\": 0.3, \"spawn_interval_decay_per_minute\": 0.05, "
            + "\"max_enemies_on_screen\": 120, \"fallback_exploration_pool\": [\"shade\"], \"fallback_resurgence_pool\": [\"shade\"], "
            + "\"local_enemy_target_base\": 10 }";
        Check(Vestiges.Spawn.SpawnFlowConfig.TryParse(minimal, EnemyDataLoader.Exists, out Vestiges.Spawn.SpawnFlowConfig defaults, out string minimalError)
            && defaults.Number("opening_local_target_start") == 10f && defaults.Number("local_enemy_radius") == 900f
            && defaults.Integer("local_spawn_burst_max") == 4 && defaults.Number("crisis_spawn_multiplier") == 1.65f
            && defaults.Number("spawn_forward_arc_degrees") == 140f && defaults.HpSegments.Count == 1 && defaults.HpSegments[0] == (0f, 1f),
            $"flux d'apparition : fichier minimal, secours d'avant gardés {minimalError}");
    }

    /// <summary>Foule (plan 26 Q7e-2) : fichier du dépôt accepté, règles refusées, secours d'avant.</summary>
    private void CheckCrowd()
    {
        Check(CrowdDataLoader.TryLoad(out string loadError) && CrowdDataLoader.SeparationRadius == 26f, $"foule du dépôt acceptée {loadError}");
        string valid = FileAccess.GetFileAsString(CrowdDataLoader.DataPath);
        (string, Action<JsonObject>, string)[] cases =
        {
            ("JSON invalide", null, "JSON invalide"),
            ("champ inconnu", root => root["separation"] = 26, "clé « separation » inconnue"),
            ("rayon négatif", root => root["separation_radius_px"] = -1, "separation_radius_px"),
            ("force non numérique", root => root["separation_strength"] = "forte", "separation_strength"),
            ("contact négatif", root => root["contact_distance_px"] = -22, "contact_distance_px"),
        };
        foreach ((string name, Action<JsonObject> mutate, string expected) in cases)
        {
            string text = mutate == null ? "{ \"separation_radius_px\": " : Mutated(valid, mutate);
            bool refused = !CrowdDataLoader.TryParse(text, out _, out string error);
            Check(refused && error?.Contains(expected, StringComparison.Ordinal) == true, $"foule, {name} refusé ({error})");
        }
        Check(CrowdDataLoader.TryParse("{}", out CrowdDataLoader.Settings defaults, out _)
            && defaults == new CrowdDataLoader.Settings(26f, 0.9f, 1.5f, 22f), "foule : fichier vide, secours d'avant gardés");
    }

    /// <summary>Variantes et affixes (plan 26 Q7e-2) : fichier du dépôt accepté, règles refusées sans publication.</summary>
    private void CheckVariants()
    {
        Func<string, bool> chestExists = id => ChestDataLoader.Get(id) != null;
        Check(EnemyVariantDataLoader.TryLoad(out string loadError) && EnemyVariantDataLoader.GetVariant("champion")?.LevelScaling != null
            && EnemyVariantDataLoader.NaturalElites.LateMaxAlive == 8, $"variantes du dépôt acceptées {loadError}");
        EnemyVariantData elite = EnemyVariantDataLoader.GetVariant("elite");
        NaturalEliteConfig natural = EnemyVariantDataLoader.NaturalElites;
        string valid = FileAccess.GetFileAsString(EnemyVariantDataLoader.DataPath);
        (string, Action<JsonObject>, string)[] cases =
        {
            ("JSON invalide", null, "JSON invalide"),
            ("section inconnue", root => root["bosses"] = new JsonObject(), "clé « bosses » inconnue"),
            ("section absente", root => root.Remove("phase_modifiers"), "phase_modifiers"),
            ("variante inconnue du code", root => root["variants"]!["legend"] = new JsonObject(), "variante « legend » inconnue du code"),
            ("variante absente", root => root["variants"]!.AsObject().Remove("aberration"), "variante « aberration » absente"),
            ("variante non objet", root => root["variants"]!["elite"] = 5, "variante elite : objet attendu"),
            ("affixe non objet", root => root["affixes"]!["swift"] = new JsonArray(), "affixe swift : objet attendu"),
            ("champ de variante inconnu", root => root["variants"]!["elite"]!["hp"] = 5, "clé « hp » inconnue"),
            ("PV nuls", root => root["variants"]!["elite"]!["hp_mult"] = 0, "hp_mult"),
            ("bornes de PV inversées", root => root["variants"]!["elite"]!["hp_mult_min"] = 6, "hp_mult_min ≤ hp_mult"),
            ("taille nulle", root => root["variants"]!["aberration"]!["scale"] = 0, "scale"),
            ("affixes fractionnaires", root => root["variants"]!["elite"]!["affix_count"] = 1.5, "affix_count"),
            ("Essence négative", root => root["variants"]!["elite"]!["bonus_essence"] = -3, "bonus_essence"),
            ("coffre inconnu", root => root["variants"]!["elite"]!["reward_chest"] = "chest_mythique", "« chest_mythique » inconnu"),
            ("chance de coffre hors borne", root => root["variants"]!["elite"]!["reward_chest_chance"] = 1.5, "reward_chest_chance"),
            ("couleur invalide", root => root["variants"]!["elite"]!["outline_color"] = "doré", "couleur « doré » invalide"),
            ("secousse inconnue", root => root["variants"]!["champion"]!["death_shake"] = "huge", "« huge » inconnu"),
            ("plaque non booléenne", root => root["variants"]!["elite"]!["nameplate"] = "oui", "nameplate"),
            ("titre non textuel", root => root["variants"]!["champion"]!["title_m"] = 3, "title_m"),
            ("affixe de variante inconnu", root => root["variants"]!["champion"]!["affix_pool"]![0] = "maudit", "affixe « maudit » inconnu"),
            ("affixe de variante en double", root => root["variants"]!["champion"]!["affix_pool"]!.AsArray().Add("swift"), "« swift » en double"),
            ("échelle de niveau inconnue", root => root["variants"]!["champion"]!["level_scaling"]!["slope"] = 1, "clé « slope » inconnue"),
            ("combat nul", root => root["variants"]!["champion"]!["level_scaling"]!["fight_seconds"] = 0, "fight_seconds"),
            ("champ d'affixe inconnu", root => root["affixes"]!["swift"]!["speed"] = 1.45, "clé « speed » inconnue"),
            ("nom d'affixe vide", root => root["affixes"]!["swift"]!["name_m"] = "", "name_m"),
            ("dégâts subis nuls", root => root["affixes"]!["armored"]!["damage_taken_mult"] = 0, "damage_taken_mult"),
            ("régénération hors borne", root => root["affixes"]!["regenerant"]!["regen_ratio_per_sec"] = 1.5, "regen_ratio_per_sec"),
            ("explosion négative", root => root["affixes"]!["explosive"]!["death_explosion_radius"] = -64, "death_explosion_radius"),
            ("élites inversées", root => root["natural_elites"]!["interval_min_sec"] = 90, "interval_min_sec ≤ interval_max_sec"),
            ("élites fractionnaires", root => root["natural_elites"]!["max_alive"] = 2.5, "max_alive"),
            ("élites tardives inconnues", root => root["natural_elites"]!["late"]!["count"] = 8, "clé « count » inconnue"),
            ("élites tardives inversées", root => root["natural_elites"]!["late"]!["interval_min_sec"] = 30, "interval_min_sec ≤ interval_max_sec"),
            ("affixe d'élite inconnu", root => root["natural_elites"]!["affix_pool"]![0] = "maudit", "affixe « maudit » inconnu"),
            ("chance de phase hors borne", root => root["phase_modifiers"]!["affix_chance_endgame"] = 1.2, "affix_chance_endgame"),
            ("affixe de phase inconnu", root => root["phase_modifiers"]!["affix_pool"]![0] = "maudit", "affixe « maudit » inconnu"),
        };
        foreach ((string name, Action<JsonObject> mutate, string expected) in cases)
        {
            string text = mutate == null ? "{ \"variants\": " : Mutated(valid, mutate);
            string error = EnemyVariantDataLoader.Apply(text, chestExists);
            Check(error?.Contains(expected, StringComparison.Ordinal) == true && ReferenceEquals(EnemyVariantDataLoader.GetVariant("elite"), elite)
                && ReferenceEquals(EnemyVariantDataLoader.NaturalElites, natural), $"variantes, {name} refusé sans publication ({error})");
        }
        JsonObject repeated = JsonNode.Parse(valid)!.AsObject();
        repeated["phase_modifiers"]!["affix_pool"]!.AsArray().Add("enraged");
        Check(EnemyVariantDataLoader.Apply(repeated.ToJsonString(), chestExists) == null
            && EnemyVariantDataLoader.PhaseModifiers.AffixPool.Count == 4, "variantes : affixe de phase répété admis (il pèse davantage)");
        Check(EnemyVariantDataLoader.Apply(valid, chestExists) == null, "variantes du dépôt republiées");
    }

    /// <summary>Personnages (plan 26 Q7f-1) : fichier du dépôt accepté, références contrôlées, refus sans publication.</summary>
    private void CheckCharacters()
    {
        Check(CharacterDataLoader.TryLoad(out string loadError) && CharacterDataLoader.GetAll().Count == 6
            && CharacterDataLoader.Get("facteur")?.MobilityProfile == "glide", $"personnages du dépôt acceptés {loadError}");
        CharacterData vagabond = CharacterDataLoader.Get("vagabond");
        CharacterDataLoader.References references = CharacterDataLoader.GameReferences();
        string valid = FileAccess.GetFileAsString(CharacterDataLoader.DataPath);
        (string, Func<JsonArray, JsonNode>, string)[] cases =
        {
            ("JSON invalide", null, "JSON invalide"),
            ("racine non liste", _ => new JsonObject(), "liste de personnages attendue"),
            ("liste vide", _ => new JsonArray(), "liste de personnages attendue"),
            ("champ inconnu", list => { list[0]!["exclusive_perks"] = new JsonArray(); return list; }, "clé « exclusive_perks » inconnue"),
            ("identifiant en double", list => { list.Add(list[0]!.DeepClone()); return list; }, "identifiant déjà déclaré"),
            ("nom vide", list => { list[0]!["name"] = ""; return list; }, "name"),
            ("statistiques absentes", list => { list[0]!.AsObject().Remove("base_stats"); return list; }, "base_stats"),
            ("statistique inconnue", list => { list[0]!["base_stats"]!["armor"] = 5; return list; }, "clé « armor » inconnue"),
            ("vitesse nulle", list => { list[0]!["base_stats"]!["speed"] = 0; return list; }, "speed"),
            ("PV nuls", list => { list[0]!["base_stats"]!["max_hp"] = 0; return list; }, "max_hp"),
            ("régénération négative", list => { list[0]!["base_stats"]!["regen_rate"] = -1; return list; }, "regen_rate"),
            ("arme inconnue", list => { list[0]!["starting_weapon"] = "excalibur"; return list; }, "arme « excalibur » inconnue"),
            ("signature inconnue", list => { list[0]!["passive_perk"] = "traqueur_piercing"; return list; }, "n'est pas une signature de traqueur"),
            ("signature d'un autre", list => { list[0]!["passive_perk"] = "vagabond_harvest"; return list; }, "n'est pas une signature de traqueur"),
            ("mobilité inconnue", list => { list[0]!["mobility"] = "teleport"; return list; }, "Profil de mobilité inconnu : teleport"),
            ("sprites absents", list => { list[0]!["sprite_folder"] = "vagabond"; return list; }, "sans char_traqueur_S_idle_01.png"),
            ("dossier hors des personnages", list => { list[0]!["sprite_folder"] = "../traqueur"; return list; }, "sprite_folder"),
            ("couleur à trois composantes", list => { list[0]!["visual_color"] = new JsonArray(0.2, 0.7, 0.3); return list; }, "visual_color"),
            ("accent hors borne", list => { list[0]!["accent_color"] = new JsonArray(0.2, 0.7, 1.3, 1); return list; }, "accent_color"),
            ("pieds négatifs", list => { list[0]!["sprite_feet_offset"] = -4; return list; }, "sprite_feet_offset"),
        };
        foreach ((string name, Func<JsonArray, JsonNode> mutate, string expected) in cases)
        {
            string text = mutate == null ? "[ { \"id\": " : mutate(JsonNode.Parse(valid)!.AsArray()).ToJsonString();
            string error = CharacterDataLoader.Apply(text, references);
            Check(error?.Contains(expected, StringComparison.Ordinal) == true && ReferenceEquals(CharacterDataLoader.Get("vagabond"), vagabond),
                $"personnages, {name} refusé sans publication ({error})");
        }
    }

    private void CheckAtmosphere()
    {
        string valid = FileAccess.GetFileAsString("res://data/world/atmosphere.json");
        Check(AtmosphereConfig.TryParse(valid, out AtmosphereConfig config, out string error) && config.GradeFadeSeconds > 0f,
            $"atmosphère du dépôt acceptée {error}");
        (string, Action<JsonObject>, string)[] cases =
        {
            ("fondu nul", root => root["grade"]!["fade_seconds"] = 0, "fade_seconds"),
            ("fondu trop long", root => root["grade"]!["fade_seconds"] = 30, "fade_seconds"),
            ("part hors borne", root => root["grade"]!["entity_share"] = 1.5, "entity_share"),
            ("force hors borne", root => root["grade"]!["strength"] = 1.5, "strength"),
            ("nuages trop petits", root => root["clouds"]!["scale_px"] = 10, "scale_px"),
            ("nuages trop rapides", root => root["clouds"]!["speed_px_s"] = 500, "speed_px_s"),
            ("nuages absents", root => root.Remove("clouds"), "clouds"),
            ("vent qui couche les herbes", root => root["wind"]!["sway_px"] = 12, "sway_px"),
            ("vent absent", root => root.Remove("wind"), "wind"),
            ("ombre portée trop longue", root => root["cast_shadows"]!["length"] = 3, "cast_shadows.length"),
            ("hauteur minimale nulle", root => root["cast_shadows"]!["min_height_px"] = 0, "min_height_px"),
            ("couleur d'ombre invalide", root => root["cast_shadows"]!["color"] = new JsonArray(0, 0), "color"),
            ("salissure absente", root => root.Remove("grime"), "grime"),
            ("papier trop sombre", root => root["grime"]!["paper"] = new JsonArray(1, 0.5, 0.5), "grime.paper"),
            ("noir trop relevé", root => root["grime"]!["ink"] = new JsonArray(0.3, 0, 0), "grime.ink"),
            ("grain trop fort", root => root["grime"]!["grain"] = 0.5, "grime.grain"),
            ("saturation hors borne", root => root["grime"]!["saturation"] = 1.5, "saturation"),
            ("taches hors borne", root => root["grime"]!["stains"] = 1.5, "stains"),
            ("taches minuscules", root => root["grime"]!["stain_scale_px"] = 4, "grime.stain_scale_px"),
            ("section absente", root => root.Remove("grade"), "grade"),
            ("champ inconnu", root => root["grade"]!["speed"] = 1, "clé « speed » inconnue"),
        };
        foreach ((string label, Action<JsonObject> mutate, string expected) in cases)
        {
            bool refused = !AtmosphereConfig.TryParse(Mutated(valid, mutate), out AtmosphereConfig refusedConfig, out string message);
            Check(refused && refusedConfig == null && message.Contains(expected, StringComparison.Ordinal),
                $"atmosphère refusée ({label}) : {message}");
        }
    }

    private void CheckWeather()
    {
        string valid = FileAccess.GetFileAsString("res://data/world/weather.json");
        Check(WeatherConfig.TryParse(valid, out WeatherConfig config, out string error) && config.Weathers.Count == 4,
            $"météo du dépôt acceptée (quatre météos) {error}");
        foreach (WeatherData weather in config?.Weathers ?? new List<WeatherData>())
            foreach (string biome in weather.BiomeWeights.Keys)
                Check(BiomeDataLoader.Get(biome) != null, $"météo {weather.Id} : biome « {biome} » connu");
        Check(config?.Find("storm")?.FlashExposure > 0f && config.Find("thick_fog")?.ViewOpacity > 0f,
            "orage avec éclairs, brouillard qui réduit la vue");
        Check(config?.Find("storm")?.Particles == AtmosphereParticleKind.None,
            "orage sec : pas de pluie avant l'Indicible (DECISIONS §150)");
        (string, Action<JsonObject>, string)[] cases =
        {
            ("aucune météo", root => root["weathers"] = new JsonObject(), "au moins une météo"),
            ("brouillard aveuglant", root => root["weathers"]!["thick_fog"]!["view"]!["clear_px"] = 60, "clear_px"),
            ("particules inconnues", root => root["weathers"]!["ash_rain"]!["particles"]!["kind"] = "snow", "kind"),
            ("éclair trop fort", root => root["weathers"]!["storm"]!["flashes"]!["exposure"] = 2, "flashes.exposure"),
            ("nuit en plein jour", root => root["weathers"]!["storm"]!["grade"]!["exposure"] = 0.2, "grade"),
            ("champ inconnu", root => root["weathers"]!["storm"]!["thunder"] = 1, "clé « thunder » inconnue"),
            ("vent démesuré", root => root["weathers"]!["storm"]!["wind"] = 6, "storm.wind"),
        };
        foreach ((string label, Action<JsonObject> mutate, string expected) in cases)
        {
            bool refused = !WeatherConfig.TryParse(Mutated(valid, mutate), out WeatherConfig refusedConfig, out string message);
            Check(refused && refusedConfig == null && message.Contains(expected, StringComparison.Ordinal),
                $"météo refusée ({label}) : {message}");
        }
    }

    private void CheckFauna()
    {
        string valid = FileAccess.GetFileAsString("res://data/world/fauna.json");
        HashSet<string> known = FaunaConfig.KnownSpecies();
        Check(known.Count == 5, $"manifeste de la faune : cinq espèces ({known.Count})");
        Check(FaunaConfig.TryParse(valid, known.Contains, out FaunaConfig config, out string error) && config.AllSpecies.Count == 5,
            $"faune du dépôt acceptée (cinq espèces) {error}");
        foreach (FaunaSpecies species in config?.AllSpecies ?? new List<FaunaSpecies>())
        {
            foreach (string biome in species.BiomeWeights.Keys)
                Check(BiomeDataLoader.Get(biome) != null, $"faune {species.Id} : biome « {biome} » connu");
            foreach (string action in new[] { species.RestAction, species.FleeAction })
                Check(ResourceLoader.Exists($"res://assets/fauna/{species.Id}/fauna_{species.Id}_E_{action}_01.png"),
                    $"faune {species.Id} : poses « {action} » présentes");
        }
        (string, Action<JsonObject>, string)[] cases =
        {
            ("naissance au bord de l'écran", root => root["spawn_margin_px"] = new JsonArray(10, 240), "spawn_margin_px"),
            ("disparition trop proche", root => root["despawn_distance_px"] = 600, "despawn_distance_px"),
            ("espèce sans sprites", root => root["species"]!["heron"] = root["species"]!["rat"]!.DeepClone(), "heron"),
            ("animation trop rapide", root => root["species"]!["rat"]!["fps"] = 40, "fps"),
            ("foule de bêtes", root => root["max_alive"] = 500, "max_alive"),
        };
        foreach ((string label, Action<JsonObject> mutate, string expected) in cases)
        {
            bool refused = !FaunaConfig.TryParse(Mutated(valid, mutate), known.Contains, out FaunaConfig refusedConfig, out string message);
            Check(refused && refusedConfig == null && message.Contains(expected, StringComparison.Ordinal),
                $"faune refusée ({label}) : {message}");
        }
    }

    private void CheckExodus()
    {
        string valid = FileAccess.GetFileAsString("res://data/world/exodus.json");
        Check(ExodusConfig.TryParse(valid, out ExodusConfig config, out string error) && config.ScatterChance > 0f,
            $"exode du dépôt accepté {error}");
        // Sprites que le placeur compose : quatre sens de fuite par peinture, panneau et flèche.
        List<string> sprites = new() { "suitcase_open", "luggage", "stroller", "cart", "boxes", "cones", "barrier_x", "barrier_y" };
        foreach (string direction in new[] { "e", "w", "n", "s" })
        {
            sprites.Add($"sign_{direction}");
            sprites.Add($"arrow_{direction}");
            foreach (string paint in new[] { "red", "teal", "cream" })
            {
                sprites.Add($"car_{paint}_{direction}");
            }
        }
        int missing = sprites.FindAll(id => !ResourceLoader.Exists($"res://assets/props/urban_ruins/prop_exodus_{id}.png")).Count;
        Check(missing == 0, $"exode : {sprites.Count} sprites présents ({missing} absents)");
        (string, Action<JsonObject>, string)[] cases =
        {
            ("scènes dans le même écran", root => root["min_spacing_px"] = 300, "min_spacing_px"),
            ("voitures qui se chevauchent", root => root["car_spacing_px"]!["vertical"] = 20, "car_spacing_px"),
            ("file trop longue", root => root["jam_cars"] = new JsonArray(3, 12), "jam_cars"),
            ("voitures fractionnaires", root => root["jam_cars"] = new JsonArray(2.5, 4), "jam_cars"),
            ("clé inconnue", root => root["bus"] = 1, "bus"),
        };
        foreach ((string label, Action<JsonObject> mutate, string expected) in cases)
        {
            bool refused = !ExodusConfig.TryParse(Mutated(valid, mutate), out ExodusConfig refusedConfig, out string message);
            Check(refused && refusedConfig == null && message.Contains(expected, StringComparison.Ordinal),
                $"exode refusé ({label}) : {message}");
        }
    }

    private void CheckAbandonedVoices()
    {
        string valid = FileAccess.GetFileAsString("res://data/world/abandoned_voices.json");
        Func<string, bool> exists = path => ResourceLoader.Exists(path);
        bool accepted = AbandonedVoicesConfig.TryParse(valid, exists, out AbandonedVoicesConfig config, out string error);
        Check(accepted && config.RingPath.EndsWith("/phone_ring.wav", StringComparison.Ordinal),
            $"téléphones du dépôt acceptés {error}");
        (string, Action<JsonObject>, string)[] cases =
        {
            ("son absent", root => root["phone"]!["ring"] = "sonnerie_inconnue", "sonnerie_inconnue"),
            ("téléphone muet d'avance", root => root["phone"]!["silence_px"] = 900, "silence_px"),
            ("niveau qui sature", root => root["phone"]!["ring_db"] = 3, "0 au plus"),
            ("radio retirée", root => root["radio"] = new JsonObject(), "radio"),
        };
        foreach ((string label, Action<JsonObject> mutate, string expected) in cases)
        {
            bool refused = !AbandonedVoicesConfig.TryParse(Mutated(valid, mutate), exists, out AbandonedVoicesConfig refusedConfig, out string message);
            Check(refused && refusedConfig == null && message.Contains(expected, StringComparison.Ordinal),
                $"téléphones refusés ({label}) : {message}");
        }
    }

    private void CheckFalseLights()
    {
        string valid = FileAccess.GetFileAsString("res://data/world/false_lights.json");
        string[] weathers = { "storm", "thick_fog", "ash_rain", "forgotten_wind" };
        bool accepted = FalseLightsConfig.TryParse(valid, FalseLightsConfig.PropSpriteExists, path => FileAccess.FileExists(path), weathers, out FalseLightsConfig config, out string error);
        Check(accepted && config.Kinds.Count == 5 && config.Kinds.Any(kind => kind.Sprite == "prop_scene_campfire" && kind.Smoke && kind.LitShare == 1f) && config.WeatherDarkness["storm"] == 1f, $"fausses lumières du dépôt acceptées {error}");
        int glows = Array.FindAll(new[] { "core", "halo", "pool", "pixel", "smoke" }, id => ResourceLoader.Exists($"res://assets/vfx/lights/light_{id}.png")).Length;
        Check(glows == 5, $"fausses lumières : 5 lueurs présentes ({glows})");
        // Fenêtres (B4b) : chaque immeuble a son entrée, chaque vitre tient dans son sprite.
        using JsonDocument windows = JsonDocument.Parse(FileAccess.GetFileAsString("res://" + config.WindowsPath));
        int buildings = 0;
        int panes = 0;
        List<string> faults = new();
        foreach (string file in DirAccess.GetFilesAt("res://assets/props/urban_ruins"))
        {
            if (!file.StartsWith(FalseLightsConfig.WindowSpritePrefix, StringComparison.Ordinal) || !file.EndsWith(".png", StringComparison.Ordinal))
                continue;
            buildings++;
            string stem = file.GetBaseName();
            if (!windows.RootElement.TryGetProperty(stem, out JsonElement rects))
            {
                faults.Add($"{stem} absent");
                continue;
            }
            Vector2 size = GD.Load<Texture2D>($"res://assets/props/urban_ruins/{file}").GetSize();
            foreach (JsonElement rect in rects.EnumerateArray())
            {
                panes++;
                if (rect[0].GetInt32() < 0 || rect[1].GetInt32() < 0 || rect[0].GetInt32() + rect[2].GetInt32() > size.X
                    || rect[1].GetInt32() + rect[3].GetInt32() > size.Y)
                    faults.Add($"{stem} : vitre hors du sprite");
            }
        }
        // Chaque style d'immeuble garde au moins une vitre, sauf l'église (vitraux, aucune vitre ordinaire).
        foreach (string style in new[] { "apartment", "house", "shop", "ruin" })
        {
            bool glazed = false;
            foreach (JsonProperty building in windows.RootElement.EnumerateObject())
                glazed |= building.Name.StartsWith($"{FalseLightsConfig.WindowSpritePrefix}{style}_", StringComparison.Ordinal)
                    && building.Value.GetArrayLength() > 0;
            if (!glazed)
                faults.Add($"style {style} sans vitre");
        }
        Check(buildings > 0 && panes > 0 && faults.Count == 0,
            $"fenêtres : {panes} vitres sur {buildings} immeubles {string.Join(", ", faults)}");
        (string, Action<JsonObject>, string)[] cases =
        {
            ("décor inconnu", root => root["kinds"]![0]!["sprite"] = "prop_lampe_inconnue", "prop_lampe_inconnue"),
            ("décor en double", root => root["kinds"]![1]!["sprite"] = "prop_traffic_light", "en double"),
            ("météo inconnue", root => root["weather_darkness"]!["blizzard"] = 1, "blizzard"),
            ("rythme inconnu", root => root["kinds"]![0]!["pattern"] = "strobe", "pattern"),
            ("part allumée hors clignotement", root => root["kinds"]![1]!["on_share"] = 0.5, "on_share"),
            ("ampoule hors du sprite", root => root["kinds"]![0]!["anchor"] = new JsonArray(-2, 10), "anchor"),
            ("mémoire inversée", root => root["memory_dead"] = 0.8, "memory_dead"),
            ("plus fortes de jour", root => root["dark_strength"] = 0.3, "day_strength"),
            ("fenêtres introuvables", root => root["windows"]!["data"] = "assets/props/urban_ruins/absent.json", "absent.json"),
            ("télévision sans rythme", root => root["windows"]!["screen_period_s"] = 0, "screen_period_s"),
            ("fumée sans bouffée", root => root["smoke"]!["max_puffs"] = 0, "max_puffs"),
            ("part propre hors bornes", root => root["kinds"]![0]!["lit_share"] = 2, "lit_share"),
            ("lampe éteinte plus vive", root => root["rhythm"]!["off_level"] = 0.5, "off_level"),
            ("clé inconnue", root => root["glow"] = 1, "glow"),
        };
        foreach ((string label, Action<JsonObject> mutate, string expected) in cases)
        {
            bool refused = !FalseLightsConfig.TryParse(Mutated(valid, mutate), FalseLightsConfig.PropSpriteExists,
                path => FileAccess.FileExists(path), weathers,
                out FalseLightsConfig refusedConfig, out string message);
            Check(refused && refusedConfig == null && message.Contains(expected, StringComparison.Ordinal),
                $"fausses lumières refusées ({label}) : {message}");
        }
    }

    private static JsonObject Entry(JsonObject root, string list, int index) => root[list]![index]!.AsObject();

    private static JsonObject Item(JsonArray items, string id)
    {
        foreach (JsonNode item in items)
        {
            if ((string)item!["id"] == id)
                return item.AsObject();
        }
        throw new InvalidOperationException($"objet {id} absent du catalogue");
    }

    private static JsonObject Effect(JsonArray items, string id) => Item(items, id)["effects"]![0]!.AsObject();
    private static JsonObject Milestone(JsonArray items, string id) => Item(items, id)["milestones"]![0]!.AsObject();
    private static JsonObject Rarity(JsonObject root, int index) => root["rarities"]![index]!.AsObject();

    private static string Mutated(string json, Action<JsonObject> mutate)
    {
        JsonObject root = JsonNode.Parse(json)!.AsObject();
        mutate(root);
        return root.ToJsonString();
    }

    private void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
        _checks++;
        GD.Print($"[CatalogContractRegression] OK {message}");
    }
}
