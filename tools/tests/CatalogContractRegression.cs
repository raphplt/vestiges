using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Godot;
using Vestiges.Infrastructure;
using Vestiges.Progression;

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
        Check(ScoreConfig.TryParse(valid, EnemyDataLoader.Exists, out _, out string error), $"barème du dépôt accepté {error}");
        (string, Action<JsonObject>, string)[] cases =
        {
            ("table absente", root => root.Remove("kill_points"), "section kill_points absente"),
            ("défaut absent", root => root["kill_points"]!.AsObject().Remove("default"), "kill_points : default absent"),
            ("créature inconnue", root => root["kill_points"]!["rodeurr"] = 15, "créature « rodeurr » inconnue"),
            ("points négatifs", root => root["kill_points"]!["rodeur"] = -5, "rodeur : -5"),
            ("points fractionnaires", root => root["kill_points"]!["rodeur"] = 15.5, "rodeur : 15.5"),
            ("texte au lieu d'un nombre", root => root["kill_points"]!["rodeur"] = "quinze", "rodeur : nombre fini attendu"),
            ("clé inconnue", root => root["points"] = 1, "clé « points » inconnue"),
        };
        foreach ((string label, Action<JsonObject> mutate, string expected) in cases)
        {
            bool rejected = !ScoreConfig.TryParse(Mutated(valid, mutate), EnemyDataLoader.Exists, out ScoreConfig config, out string message);
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
            ("valeur absente", root => root["per_point"]!.AsObject().Remove("score"), "score absent"),
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
            ("réglage du texte absent", items => Item(items, "memoire_vive")["description"] = "Plus vite pendant {duree} s.", "réglage « duree » absent de params"),
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
