using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Infrastructure;

namespace Vestiges.Tests;

/// <summary>
/// Plan 26 Q6c : contrat des fiches de créatures. Le catalogue du dépôt passe en entier ; chaque diagnostic refuse une
/// fiche fautive avec un message qui nomme le champ ; les renforts absents et les doublons écartent leur fiche ; la
/// meute compte les voisins de la même famille, pas d'un même identifiant.
/// </summary>
public partial class EnemyAbilityRegression
{
    private void RunContractChecks()
    {
        Check(EnemyContract.IsUsable, "contrat des créatures lisible et complet");
        foreach (EnemyAbilityKind kind in Enum.GetValues<EnemyAbilityKind>())
            Check(EnemyContract.Rules(kind) != null, $"contrat : règles de {EnemyGrammar.Key(kind)}");
        List<string> ids = EnemyDataLoader.GetAllIds();
        int files = 0;
        foreach (string file in DirAccess.GetFilesAt("res://data/enemies"))
            files += file.EndsWith(".json") && !file.StartsWith('_') ? 1 : 0;
        Check(ids.Count == files, $"catalogue : toutes les fiches chargées ({ids.Count} sur {files})");
        foreach (EnemyAbilityKind kind in Enum.GetValues<EnemyAbilityKind>())
            Check(EnemyGrammar.TryParseAbility(EnemyGrammar.Key(kind), out EnemyAbilityKind back) && back == kind,
                $"grammaire : {kind} a sa clé ({EnemyGrammar.Key(kind)})");
        HashSet<string> drawn = new();
        foreach (FxFamily family in Enum.GetValues<FxFamily>())
            drawn.Add(family.ToString().ToLowerInvariant());
        Check(drawn.SetEquals(EnemyContract.Families), "contrat : mêmes familles que le rendu (FxFamily)");
        foreach (string family in EnemyContract.Families)
            Check(PixelPalette.ParseFamily(family, FxFamily.Physical) == PixelPalette.ParseFamily(family, FxFamily.Verdigris),
                $"famille {family} reconnue par le rendu, sans secours");
        Check(string.Join(",", EnemyDataLoader.Get("hurleur").Abilities.Keys) == "AimedShot,Cry",
            "Hurleur : capacités dans l'ordre de la fiche (tir puis cri)");
        Check(EnemyDataLoader.Get("charognard").PackFamily == "charognard"
            && EnemyDataLoader.Get("charognard").Behavior == EnemyBehavior.Pack, "Charognard : meute de la famille charognard");
        EnemyAbilityData cry = EnemyDataLoader.Get("hurleur").Abilities[EnemyAbilityKind.Cry];
        Check(cry.Text("reinforcement_id") == "shade" && !cry.TryGetNumber("first_delay", out _),
            "Hurleur : renfort déclaré, premier délai laissé au code");
        EnemyData weaver = EnemyDataLoader.Get("tisseuse");
        Check(weaver.GetStat("web_slow_multiplier") == 0.4f && weaver.GetStat("web_slow_seconds") == 2f,
            "Tisseuse : ralentissement 0,4 pendant 2 s (constantes d'avant Q6c, désormais au contrat)");
        EnemyAbilityData shot = EnemyDataLoader.Get("fading_spitter").Abilities[EnemyAbilityKind.AimedShot];
        Check(shot.Number("cooldown_multiplier") == 1f && shot.Number("show_range") == 0f && shot.Text("fx_family") == "rust",
            "Cracheur : secours du contrat résolus (cooldown_multiplier 1, show_range 0), famille déclarée");

        RunNegativeFixtures();
        RunContractFixtures();
        RunPoolFixtures();
        RunCatalogFixtures();
    }

    private void RunNegativeFixtures()
    {
        (string Label, string File, Action<JsonObject> Mutate, string Expected)[] cases =
        {
            ("champ inconnu", "rodeur", e => e["speciality"] = 1, "champ « speciality » inconnu"),
            ("id absent", "rodeur", e => e.Remove("id"), "id obligatoire"),
            ("name absent", "rodeur", e => e.Remove("name"), "name obligatoire"),
            ("type inconnu", "rodeur", e => e["type"] = "flying", "type : « flying » inconnu"),
            ("type absent", "rodeur", e => e.Remove("type"), "type obligatoire"),
            ("comportement inconnu", "rodeur", e => e["behavior"] = "swarm", "behavior : « swarm » inconnu"),
            ("rang inconnu", "rodeur", e => e["tier"] = "legend", "tier : « legend » inconnu"),
            ("genre inconnu", "rodeur", e => e["grammatical_gender"] = "n", "grammatical_gender : « n » inconnu"),
            ("comportement texte attendu", "rodeur", e => e["behavior"] = 3, "behavior : texte attendu"),
            ("son d'attaque absent", "rodeur", e => e["attack_audio"] = "sfx_inexistant", "attack_audio : son « sfx_inexistant » absent"),
            ("meute sans famille", "charognard", e => e.Remove("pack_family"), "comportement pack sans pack_family"),
            ("famille de meute vide", "charognard", e => e["pack_family"] = "", "pack_family : famille de meute vide"),
            ("stats absentes", "rodeur", e => e.Remove("stats"), "objet stats obligatoire"),
            ("stat obligatoire absente", "rodeur", e => e["stats"].AsObject().Remove("hp"), "stat hp obligatoire"),
            ("stat inconnue", "rodeur", e => e["stats"]["armor"] = 3, "stat « armor » inconnue"),
            ("stat non numérique", "rodeur", e => e["stats"]["speed"] = "vite", "stats.speed : nombre attendu"),
            ("stat hors bornes", "rodeur", e => e["stats"]["hp"] = 0, "stats.hp : 0 inférieur au minimum 1"),
            ("ralentissement hors bornes", "tisseuse", e => e["stats"]["web_slow_multiplier"] = 1.5, "stats.web_slow_multiplier : 1.5 hors de [0 ; 1]"),
            ("visuel absent", "rodeur", e => e.Remove("visual"), "objet visual obligatoire"),
            ("visuel inconnu", "rodeur", e => e["visual"]["glow"] = 1, "visual : champ « glow » inconnu"),
            ("couleur invalide", "rodeur", e => e["visual"]["color"] = "pas une couleur", "visual.color : couleur « pas une couleur » invalide"),
            ("forme inconnue", "rodeur", e => e["visual"]["shape"] = "hexagon", "visual.shape : forme « hexagon » inconnue"),
            ("taille nulle", "rodeur", e => e["visual"]["size"] = 0, "visual.size : 0 : strictement positif attendu"),
            ("projectile absent du manifeste", "tisseuse", e => e["visual"]["projectile"]["sprite"] = "boulet", "projectile.sprite : projectile « boulet » absent"),
            ("famille de projectile inconnue", "tisseuse", e => e["visual"]["projectile"]["family"] = "plasma", "projectile.family : famille « plasma » inconnue"),
            ("capacité inconnue", "rodeur", e => e["abilities"] = new JsonObject { ["teleport"] = new JsonObject() }, "capacité « teleport » inconnue"),
            ("capacité non objet", "rodeur", e => e["abilities"] = new JsonObject { ["burrow"] = 3 }, "abilities.burrow : objet attendu"),
            ("réglage inconnu", "presage", e => e["abilities"]["omen_strike"]["speed"] = 2, "abilities.omen_strike : réglage « speed » inconnu"),
            ("réglage hors bornes", "presage", e => e["abilities"]["omen_strike"]["delay_seconds"] = 0.05, "omen_strike.delay_seconds : 0.05 inférieur au minimum 0.1"),
            ("réglage non entier", "presage", e => e["abilities"]["omen_strike"]["max_simultaneous"] = 2.5, "omen_strike.max_simultaneous : 2.5 n'est pas entier"),
            ("réglage non numérique", "presage", e => e["abilities"]["omen_strike"]["radius"] = "large", "omen_strike.radius : nombre attendu"),
            ("premier délai négatif", "void_brute", e => e["abilities"]["charge"]["first_delay"] = -1, "charge.first_delay : -1 inférieur au minimum 0"),
            ("renfort obligatoire absent", "hurleur", e => e["abilities"]["cry"].AsObject().Remove("reinforcement_id"), "abilities.cry : réglage reinforcement_id obligatoire"),
            ("renfort vide", "hurleur", e => e["abilities"]["cry"]["reinforcement_id"] = "", "cry.reinforcement_id : créature vide"),
            ("famille d'effet inconnue", "presage", e => e["abilities"]["omen_strike"]["fx_family"] = "plasma", "omen_strike.fx_family : famille « plasma » inconnue"),
            ("son de capacité absent", "presage", e => e["abilities"]["omen_strike"]["cast_audio"] = "sfx_inexistant", "omen_strike.cast_audio : son « sfx_inexistant » absent"),
            ("secousse inconnue", "void_brute", e => e["abilities"]["charge"]["impact_shake"] = "huge", "charge.impact_shake : secousse « huge » inconnue"),
            ("texte attendu", "presage", e => e["abilities"]["omen_strike"]["fx_family"] = 3, "omen_strike.fx_family : texte attendu"),
            ("capacités non objet", "presage", e => e["abilities"] = 3, "abilities : objet attendu"),
            ("taille absente", "rodeur", e => e["visual"].AsObject().Remove("size"), "visual.size : obligatoire"),
            ("pieds négatifs", "rodeur", e => e["visual"]["sprite_feet_offset"] = -2, "visual.sprite_feet_offset : -2 inférieur au minimum 0"),
            ("dossier non texte", "rodeur", e => e["visual"]["sprite_folder"] = 3, "visual, sprite_folder : texte attendu"),
            ("famille de meute non texte", "charognard", e => e["pack_family"] = 3, "pack_family : texte attendu"),
            ("projectile non objet", "tisseuse", e => e["visual"]["projectile"] = "web", "visual.projectile : objet attendu"),
            ("projectile inconnu", "tisseuse", e => e["visual"]["projectile"]["speed"] = 2, "visual.projectile : champ « speed » inconnu"),
        };

        foreach ((string label, string file, Action<JsonObject> mutate, string expected) in cases)
        {
            JsonObject enemy = JsonNode.Parse(ReadEnemyFile(file)).AsObject();
            mutate(enemy);
            string error = EnemyDataLoader.Parse(enemy.ToJsonString(), out EnemyData data, out _);
            Check(error != null && error.Contains(expected) && data == null,
                $"refusé, {label} : {error ?? "accepté"}");
        }

        string notObject = EnemyDataLoader.Parse("[]", out _, out _);
        Check(notObject != null && notObject.Contains("objet attendu"), $"refusé, racine non objet : {notObject}");

        string malformed = EnemyDataLoader.Parse("{ \"id\": ", out _, out _);
        Check(malformed != null && malformed.StartsWith("JSON illisible"), $"refusé, JSON illisible : {malformed}");

        // Un secours vide (« aucun son ») peut s'écrire explicitement.
        JsonObject silent = JsonNode.Parse(ReadEnemyFile("presage")).AsObject();
        silent["abilities"]["omen_strike"]["cast_audio"] = "";
        Check(EnemyDataLoader.Parse(silent.ToJsonString(), out _, out _) == null, "accepté : son vide explicite");
    }

    private void RunContractFixtures()
    {
        string contract;
        using (FileAccess file = FileAccess.Open("res://data/enemies/_contract.json", FileAccess.ModeFlags.Read))
            contract = file.GetAsText();
        Check(EnemyContract.Check(contract) == null, "contrat du dépôt accepté par le contrôle à part");

        (string Label, Action<JsonObject> Mutate, string Expected)[] cases =
        {
            ("section absente", c => c.Remove("shapes"), "section « shapes » absente"),
            ("capacité du code absente", c => c["abilities"].AsObject().Remove("cry"), "capacité « cry » du code absente du contrat"),
            ("capacité inconnue du code", c => c["abilities"]["teleport"] = new JsonObject(), "capacité « teleport » inconnue du code"),
            ("same_as sans règles", c => c["abilities"]["charge"]["same_as"] = "fly", "capacité « charge » : same_as « fly » sans règles propres"),
            ("sorte de texte inconnue", c => c["abilities"]["cry"]["texts"]["cry_audio"]["kind"] = "color", "texte « cry_audio » : sorte « color » inconnue"),
        };
        foreach ((string label, Action<JsonObject> mutate, string expected) in cases)
        {
            JsonObject candidate = JsonNode.Parse(contract).AsObject();
            mutate(candidate);
            string error = EnemyContract.Check(candidate.ToJsonString());
            Check(error != null && error.Contains(expected), $"contrat refusé, {label} : {error ?? "accepté"}");
        }
        string notObject = EnemyContract.Check("[]");
        Check(notObject == "objet attendu", $"contrat refusé, racine non objet : {notObject}");
        string malformed = EnemyContract.Check("{");
        Check(malformed != null && malformed.StartsWith("illisible"), $"contrat refusé, JSON illisible : {malformed}");
        Check(EnemyContract.IsUsable && EnemyContract.Rules(EnemyAbilityKind.Cry) != null, "contrat chargé intact après les contrôles à part");
    }

    private void RunPoolFixtures()
    {
        List<string> pool = new() { "rodeur", "inexistant", "shade", "shade" };
        List<string> errors = EnemyPools.KeepKnown(pool, "biome test, exploration_enemy_pool");
        Check(string.Join(",", pool) == "rodeur,shade,shade" && errors.Count == 1
            && errors[0] == "biome test, exploration_enemy_pool : créature « inexistant » introuvable, retirée du groupe",
            $"groupe de biome : créature absente retirée, répétitions gardées ({string.Join(" | ", errors)})");

        Godot.Collections.Dictionary flow = new() { ["fallback_exploration_pool"] = new Godot.Collections.Array { "shade", "fantome" } };
        List<string> fallback = new();
        errors = Spawn.SpawnManager.ReadFallbackPool(flow, "fallback_exploration_pool", fallback);
        Check(string.Join(",", fallback) == "shade" && errors.Count == 1 && errors[0].Contains("« fantome » introuvable"),
            $"secours d'apparition : créature absente retirée ({string.Join(" | ", errors)})");
        errors = Spawn.SpawnManager.ReadFallbackPool(flow, "fallback_resurgence_pool", fallback);
        Check(fallback.Count == 0 && errors.Count == 1 && errors[0].Contains("fallback_resurgence_pool absent"),
            $"secours d'apparition : clé absente, groupe vide ({string.Join(" | ", errors)})");
    }

    private void RunCatalogFixtures()
    {
        string rodeur = ReadEnemyFile("rodeur");
        JsonObject hurleur = JsonNode.Parse(ReadEnemyFile("hurleur")).AsObject();
        hurleur["abilities"]["cry"]["reinforcement_id"] = "rodeur";
        JsonObject caller = JsonNode.Parse(ReadEnemyFile("hurleur")).AsObject();
        caller["id"] = "hurleur_appelant";
        caller["abilities"]["cry"]["reinforcement_id"] = "hurleur";

        // Le Hurleur appelle le Rôdeur, absent : il est écarté, puis celui qui l'appelait à son tour.
        Dictionary<string, EnemyData> catalog = new();
        List<string> errors = EnemyDataLoader.BuildCatalog(new[]
        {
            ("hurleur.json", hurleur.ToJsonString()), ("appelant.json", caller.ToJsonString()),
        }, catalog);
        Check(catalog.Count == 0 && errors.Count == 2
            && errors[0].Contains("créature hurleur : créature citée « rodeur » introuvable")
            && errors[1].Contains("créature hurleur_appelant : créature citée « hurleur » introuvable"),
            $"renforts absents : fiches écartées en chaîne ({string.Join(" | ", errors)})");

        catalog.Clear();
        errors = EnemyDataLoader.BuildCatalog(new[]
        {
            ("rodeur.json", rodeur), ("hurleur.json", hurleur.ToJsonString()), ("copie.json", rodeur), ("vide.json", (string)null),
        }, catalog);
        Check(catalog.Count == 2 && catalog.ContainsKey("hurleur") && errors.Count == 2
            && errors[0].Contains("copie.json : créature rodeur : identifiant déjà déclaré")
            && errors[1].Contains("vide.json : fichier illisible"),
            $"doublon et fichier illisible écartés, le reste publié ({string.Join(" | ", errors)})");

        // Un Hurleur qui s'appelle lui-même, puis deux qui s'appellent l'un l'autre : renforts sans fin, écartés.
        JsonObject self = JsonNode.Parse(ReadEnemyFile("hurleur")).AsObject();
        self["abilities"]["cry"]["reinforcement_id"] = "hurleur";
        JsonObject first = JsonNode.Parse(ReadEnemyFile("hurleur")).AsObject();
        first["id"] = "hurleur_a";
        first["abilities"]["cry"]["reinforcement_id"] = "hurleur_b";
        JsonObject second = JsonNode.Parse(ReadEnemyFile("hurleur")).AsObject();
        second["id"] = "hurleur_b";
        second["abilities"]["cry"]["reinforcement_id"] = "hurleur_a";
        catalog.Clear();
        errors = EnemyDataLoader.BuildCatalog(new[]
        {
            ("rodeur.json", rodeur), ("soi.json", self.ToJsonString()), ("a.json", first.ToJsonString()), ("b.json", second.ToJsonString()),
        }, catalog);
        Check(catalog.Count == 1 && catalog.ContainsKey("rodeur") && errors.Count == 3
            && errors[0] == "créature hurleur : renforts en boucle (hurleur → hurleur)"
            && errors[1] == "créature hurleur_a : renforts en boucle (hurleur_a → hurleur_b → hurleur_a)",
            $"renforts en boucle écartés ({string.Join(" | ", errors)})");
    }

    /// <summary>La meute compte les voisins de la même famille : une créature d'une autre fiche mais de la même famille compte.</summary>
    private async Task RunPackChecks()
    {
        JsonObject kin = JsonNode.Parse(ReadEnemyFile("rodeur")).AsObject();
        kin["id"] = "rodeur_meute";
        kin["pack_family"] = "charognard";
        Check(EnemyDataLoader.Parse(kin.ToJsonString(), out EnemyData kinData, out _) == null, "fiche de test : Rôdeur de la famille charognard");

        Enemy leader = await SpawnReady("charognard", new Vector2(-400f, 0f));
        float alone = leader.Speed;
        Enemy stranger = await SpawnReady("rodeur", new Vector2(-380f, 0f));
        await RefreshPack(leader);
        Check(Mathf.IsEqualApprox(leader.Speed, alone), $"meute : un Rôdeur d'une autre famille ne compte pas ({alone} → {leader.Speed})");

        Enemy sibling = await SpawnReady("charognard", new Vector2(-420f, 0f));
        Enemy kinEnemy = await SpawnReady("rodeur", new Vector2(-400f, 20f));
        kinEnemy.Initialize(kinData, 1000f, 1f);
        await RefreshPack(leader);
        float expected = alone * (1f + EnemyDataLoader.Get("charognard").GetStat("pack_bonus_speed") * 2f);
        Check(Mathf.IsEqualApprox(leader.Speed, expected),
            $"meute : un Charognard et un membre d'une autre fiche de la même famille comptent ({leader.Speed} ≈ {expected})");

        Despawn(stranger);
        Despawn(sibling);
        Despawn(kinEnemy);
        Despawn(leader);
    }

    private async Task RefreshPack(Enemy enemy)
    {
        typeof(Enemy).GetField("_packBonusTimer", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(enemy, 0f);
        await Step(1);
    }

    private static string ReadEnemyFile(string id)
    {
        using FileAccess file = FileAccess.Open($"res://data/enemies/{id}.json", FileAccess.ModeFlags.Read);
        return file.GetAsText();
    }
}
