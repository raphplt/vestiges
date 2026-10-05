using System;
using System.Collections.Generic;
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
        int max = PerilDataLoader.Max;
        float xp = PerilDataLoader.XpMultiplier(10);
        (string, Action<JsonObject>, string)[] cases =
        {
            ("bloc absent", root => root.Remove("banish"), "section banish absente"),
            ("diviseur nul", root => root["banish"]!["peril_divisor"] = 0, "peril_divisor : 0 (entier de 1 à 1000 attendu)"),
            ("valeur négative", root => root["per_point"]!["xp"] = -0.1, "xp : -0.1 (positif ou nul attendu)"),
            ("valeur absente", root => root["per_point"]!.AsObject().Remove("rarity_steps"), "rarity_steps absent"),
            ("clé inconnue", root => root["per_point"]!["luck"] = 0.1, "clé « luck » inconnue"),
            ("maximum non entier", root => root["max"] = 9.5, "max : 9.5"),
        };
        foreach ((string label, Action<JsonObject> mutate, string expected) in cases)
        {
            JsonObject root = JsonNode.Parse(valid)!.AsObject();
            mutate(root);
            // Une valeur valide changée en même temps prouve qu'un refus ne publie rien, même partiellement.
            if (root["max"] is JsonValue value && value.TryGetValue(out int _))
                root["max"] = 7;
            string message = PerilDataLoader.Apply(root.ToJsonString());
            Check(message != null && message.Contains(expected, StringComparison.Ordinal), $"Péril refusé ({label}) : {message}");
        }
        Check(PerilDataLoader.Max == max && Mathf.IsEqualApprox(PerilDataLoader.XpMultiplier(10), xp),
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
            ("modificateur non admis", items => Effect(items, "memoire_vive")["modifier_type"] = "additive", "modificateur « additive » non admis"),
            ("pas nul", items => Effect(items, "memoire_vive")["step"] = 0, "step : 0 (pas non nul attendu)"),
            ("réglage exigé absent", items => Item(items, "allumette_humide")["params"]!.AsObject().Remove("burn_seconds"), "burn_seconds absent (exigé par ses statistiques)"),
            ("réglage inconnu", items => Item(items, "allumette_humide")["params"]!["burn_secs"] = 3, "réglage « burn_secs » inconnu de ses statistiques"),
            ("réglage du texte absent", items => Item(items, "memoire_vive")["description"] = "Plus vite pendant {duree} s.", "réglage « duree » absent de params"),
            ("palier inconnu", items => Milestone(items, "memoire_vive")["effect"] = "repeat_atack", "effet « repeat_atack » inconnu"),
            ("palier trop tôt", items => Milestone(items, "memoire_vive")["level"] = 1, "level : 1 (entier de 2 à 30 attendu)"),
            ("palier au-delà du maximum", items => Milestone(items, "memoire_vive")["level"] = 31, "level : 31"),
            ("réglage de palier absent", items => Milestone(items, "memoire_vive")["params"]!.AsObject().Remove("every"), "every absent (exigé par repeat_attack)"),
            ("réglage de palier inconnu", items => Milestone(items, "memoire_vive")["params"]!["toujours"] = 1, "réglage « toujours » inconnu de repeat_attack"),
            ("identifiant en double", items => Item(items, "ancrage")["id"] = "memoire_vive", "identifiant en double"),
            ("champ inconnu", items => Item(items, "memoire_vive")["rarete"] = "rare", "clé « rarete » inconnue"),
            ("icône introuvable", items => Item(items, "memoire_vive")["icon"] = "res://assets/items/icons/item_absent.png", "icon : image"),
            ("couleur invalide", items => Item(items, "memoire_vive")["icon_color"] = new JsonArray(1, 2), "icon_color : trois composantes"),
            ("trop d'effets", items => { JsonArray effects = Item(items, "memoire_vive")["effects"]!.AsArray(); for (int i = 0; i < 8; i++) effects.Add(effects[0]!.DeepClone()); }, "effects : 1 à 8 effets attendus"),
            ("facteur qui s'annule", items => Effect(items, "memoire_vive")["step"] = -0.1, "attack_speed : le facteur s'annule avant le niveau 30"),
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
        string statBonus = FileAccess.GetFileAsString("res://data/chests/chest_stat_bonus.json");
        ObjectDataValidator.TryParseContract(FileAccess.GetFileAsString(ObjectDataValidator.ContractPath), out ObjectDataValidator.Contract contract, out _);
        ChestDataLoader.References refs = new(path => ResourceLoader.Exists(path),
            DataKeySets.ListIds("res://data/ui/rarities.json", "rarities"),
            DataKeySets.StringList("res://data/enemies/_contract.json", "families"), LootTableLoader.Exists, contract);
        Check(ChestDataLoader.Apply(chests, placement, statBonus, refs) == null, "coffres du dépôt acceptés (trois fichiers)");
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
            string message = ChestDataLoader.Apply(list.ToJsonString(), placement, statBonus, refs);
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
            string message = ChestDataLoader.Apply(chests, Mutated(placement, mutate), statBonus, refs);
            Check(message != null && message.Contains(expected, StringComparison.Ordinal), $"placement refusé ({label}) : {message}");
        }
        (string, Action<JsonObject>, string)[] bonusCases =
        {
            ("rareté de coffre sans multiplicateur", root => root["rarity_multiplier"]!.AsObject().Remove("lore"), "rareté « lore » du coffre chest_lore absente"),
            ("statistique d'objet", root => root["stats"]![0]!["stat"] = "burn_chance", "« burn_chance » n'est pas une statistique du joueur"),
            ("modificateur non admis", root => root["stats"]![2]!["modifier_type"] = "multiplicative", "modificateur « multiplicative » non admis pour max_hp"),
            ("valeur nulle", root => root["stats"]![0]!["amount"] = 0, "amount : 0"),
        };
        foreach ((string label, Action<JsonObject> mutate, string expected) in bonusCases)
        {
            JsonObject root = JsonNode.Parse(placement)!.AsObject();
            root["groups"]!.AsArray().RemoveAt(0);
            string message = ChestDataLoader.Apply(chests, root.ToJsonString(), Mutated(statBonus, mutate), refs);
            Check(message != null && message.Contains(expected, StringComparison.Ordinal), $"bonus de coffre refusé ({label}) : {message}");
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
