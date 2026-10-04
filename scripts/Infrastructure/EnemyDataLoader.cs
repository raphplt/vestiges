using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

public class EnemyStats
{
    public float Hp { get; set; }
    public float Speed { get; set; }
    public float Damage { get; set; }
    public float AttackRange { get; set; }
    public float XpReward { get; set; }
}

public class EnemyVisual
{
    public Color Color { get; set; }
    public string Shape { get; set; }
    public float Size { get; set; }
    public string SpriteFolder { get; set; }
    /// <summary>Distance du centre du cadre aux pieds, en pixels. Non nul : sprite à la densité du monde, sans mise à l'échelle.</summary>
    public float SpriteFeetOffset { get; set; }
    /// <summary>Projectile tiré (planche de assets/vfx/projectiles) et famille de couleurs de sa traînée et de son impact.</summary>
    public string ProjectileSprite { get; set; } = "spit";
    public string ProjectileFamily { get; set; } = "hostile";
}

/// <summary>
/// Réglages d'une capacité composée (bloc « abilities » de la fiche), résolus au chargement : chaque réglage du
/// contrat y figure, secours compris, sauf un réglage facultatif absent (le code le calcule alors).
/// </summary>
public class EnemyAbilityData
{
    public EnemyAbilityKind Kind { get; init; }
    public Dictionary<string, float> Numbers { get; } = new();
    public Dictionary<string, string> Texts { get; } = new();

    public float Number(string key)
    {
        if (Numbers.TryGetValue(key, out float value))
            return value;
        GD.PushError($"[EnemyAbilityData] {EnemyGrammar.Key(Kind)}.{key} absent du contrat des créatures");
        return 0f;
    }

    public bool TryGetNumber(string key, out float value) => Numbers.TryGetValue(key, out value);

    public string Text(string key)
    {
        if (Texts.TryGetValue(key, out string value))
            return value;
        GD.PushError($"[EnemyAbilityData] {EnemyGrammar.Key(Kind)}.{key} absent du contrat des créatures");
        return "";
    }
}

public class EnemyData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public EnemyCombatType CombatType { get; set; }
    public EnemyBehavior Behavior { get; set; } = EnemyBehavior.Default;
    public EnemyTier Tier { get; set; } = EnemyTier.Normal;
    /// <summary>Famille de meute : les créatures d'une même famille se renforcent (comportement pack). Null : aucune.</summary>
    public string PackFamily { get; set; }
    /// <summary>Accord des titres de variante (« Tisseuse Enragée »).</summary>
    public bool IsFeminine { get; set; }
    public EnemyStats Stats { get; set; }
    public EnemyVisual Visual { get; set; }
    /// <summary>Stats propres à un comportement déclarées par la fiche (`stats` hors stats de base).</summary>
    public Dictionary<string, float> ExtraStats { get; set; } = new();
    /// <summary>Stat déclarée par la fiche, sinon secours du contrat.</summary>
    public float GetStat(string key) => ExtraStats.TryGetValue(key, out float value) ? value : EnemyContract.StatDefault(key);
    /// <summary>Capacités dans l'ordre de la fiche.</summary>
    public Dictionary<EnemyAbilityKind, EnemyAbilityData> Abilities { get; set; } = new();
    /// <summary>Clé AudioManager jouée quand un coup porte ou qu'un projectile part (null : muet).</summary>
    public string AttackAudio { get; set; }
}

/// <summary>
/// Catalogue des créatures (<c>data/enemies/*.json</c>, hors fichiers préfixés par <c>_</c>). Chaque fiche est contrôlée
/// par <see cref="EnemyDataValidator"/> puis convertie en <c>System.Text.Json</c> ; une fiche fautive, en double ou qui
/// cite une créature absente est écartée avec un message précis (plan 26 Q6c).
/// </summary>
public static class EnemyDataLoader
{
    private const string Directory = "res://data/enemies/";
    private static readonly HashSet<string> CoreStats = new() { "hp", "speed", "damage", "attack_range", "xp_reward" };

    private static readonly Dictionary<string, EnemyData> _cache = new();
    private static bool _loaded;

    public static void Load()
    {
        if (_loaded)
            return;
        _loaded = true;

        List<(string Path, string Text)> files = new();
        foreach (string path in GetEnemyFiles())
        {
            using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            files.Add((path, file?.GetAsText()));
        }
        foreach (string error in BuildCatalog(files, _cache))
            GD.PushError($"[EnemyDataLoader] {error} ; fiche écartée");

        GD.Print($"[EnemyDataLoader] Loaded {_cache.Count} enemy definitions");
    }

    /// <summary>
    /// Remplit <paramref name="catalog"/> avec les fiches valides et renvoie un message par fiche écartée : fiche
    /// fautive, identifiant en double, ou créature citée absente.
    /// </summary>
    public static List<string> BuildCatalog(IEnumerable<(string Path, string Text)> files, Dictionary<string, EnemyData> catalog)
    {
        List<string> errors = new();
        Dictionary<string, List<string>> references = new();
        foreach ((string path, string text) in files)
        {
            if (text == null)
            {
                errors.Add($"{path} : fichier illisible");
                continue;
            }
            string error = Parse(text, out EnemyData data, out List<string> cited);
            if (error == null && catalog.ContainsKey(data.Id))
                error = $"créature {data.Id} : identifiant déjà déclaré par une autre fiche";
            if (error != null)
            {
                errors.Add($"{path} : {error}");
                continue;
            }
            catalog[data.Id] = data;
            references[data.Id] = cited;
        }
        errors.AddRange(RemoveMissingReferences(catalog, references));
        return errors;
    }

    public static EnemyData Get(string id)
    {
        if (!_loaded)
            Load();
        if (string.IsNullOrEmpty(id))
            return null;

        if (_cache.TryGetValue(id, out EnemyData data))
            return data;

        GD.PushError($"[EnemyDataLoader] Unknown enemy id: {id}");
        return null;
    }

    public static bool Exists(string id)
    {
        if (!_loaded)
            Load();
        return !string.IsNullOrEmpty(id) && _cache.ContainsKey(id);
    }

    public static List<string> GetAllIds()
    {
        if (!_loaded)
            Load();

        return new List<string>(_cache.Keys);
    }

    /// <summary>Contrôle puis convertit une fiche ; renvoie la raison du refus, ou null.</summary>
    public static string Parse(string jsonText, out EnemyData data, out List<string> cited)
    {
        data = null;
        cited = new List<string>();
        try
        {
            using JsonDocument document = JsonDocument.Parse(jsonText);
            JsonElement root = document.RootElement;
            string error = EnemyDataValidator.Validate(root, cited);
            if (error != null)
                return error;
            data = Convert(root);
            return null;
        }
        catch (JsonException ex)
        {
            return $"JSON illisible : {ex.Message}";
        }
    }

    /// <summary>
    /// Écarte les créatures qui citent une créature absente (renforts), jusqu'à stabilité : retirer l'une peut en priver
    /// une autre. Écarte aussi celles qui s'appellent elles-mêmes, directement ou en boucle (renforts sans fin).
    /// Renvoie un message par fiche écartée.
    /// </summary>
    private static List<string> RemoveMissingReferences(Dictionary<string, EnemyData> catalog, Dictionary<string, List<string>> references)
    {
        List<string> errors = new();
        foreach (string id in new List<string>(references.Keys))
        {
            List<string> loop = FindLoop(id, references);
            if (loop != null && catalog.Remove(id))
                errors.Add($"créature {id} : renforts en boucle ({string.Join(" → ", loop)})");
        }

        bool removed = true;
        while (removed)
        {
            removed = false;
            foreach ((string id, List<string> cited) in references)
            {
                if (!catalog.ContainsKey(id))
                    continue;
                foreach (string target in cited)
                {
                    if (catalog.ContainsKey(target))
                        continue;
                    catalog.Remove(id);
                    errors.Add($"créature {id} : créature citée « {target} » introuvable");
                    removed = true;
                    break;
                }
            }
        }
        return errors;
    }

    /// <summary>Chemin qui ramène à <paramref name="start"/> en suivant les renforts, ou null.</summary>
    private static List<string> FindLoop(string start, Dictionary<string, List<string>> references)
    {
        List<string> path = new() { start };
        HashSet<string> visited = new();
        return Walk(start, start, references, path, visited) ? path : null;
    }

    private static bool Walk(string current, string start, Dictionary<string, List<string>> references, List<string> path, HashSet<string> visited)
    {
        if (!references.TryGetValue(current, out List<string> cited))
            return false;
        foreach (string target in cited)
        {
            if (target == start)
            {
                path.Add(target);
                return true;
            }
            if (!visited.Add(target))
                continue;
            path.Add(target);
            if (Walk(target, start, references, path, visited))
                return true;
            path.RemoveAt(path.Count - 1);
        }
        return false;
    }

    private static string[] GetEnemyFiles()
    {
        List<string> files = new();
        DirAccess dir = DirAccess.Open(Directory);
        if (dir == null)
        {
            GD.PushError("[EnemyDataLoader] Cannot open data/enemies/ directory");
            return files.ToArray();
        }

        dir.ListDirBegin();
        string fileName = dir.GetNext();
        while (fileName != "")
        {
            if (!dir.CurrentIsDir() && fileName.EndsWith(".json") && !fileName.StartsWith("_"))
                files.Add(Directory + fileName);
            fileName = dir.GetNext();
        }
        dir.ListDirEnd();
        files.Sort(System.StringComparer.Ordinal);
        return files.ToArray();
    }

    // La fiche est déjà validée : les clés obligatoires existent et les valeurs sont dans leurs bornes.
    private static EnemyData Convert(JsonElement root)
    {
        JsonElement stats = root.GetProperty("stats");
        JsonElement visual = root.GetProperty("visual");
        EnemyGrammar.TryParseCombatType(root.GetProperty("type").GetString(), out EnemyCombatType combatType);
        EnemyBehavior behavior = EnemyBehavior.Default;
        if (root.TryGetProperty("behavior", out JsonElement behaviorValue))
            EnemyGrammar.TryParseBehavior(behaviorValue.GetString(), out behavior);
        EnemyTier tier = EnemyTier.Normal;
        if (root.TryGetProperty("tier", out JsonElement tierValue))
            EnemyGrammar.TryParseTier(tierValue.GetString(), out tier);

        EnemyData data = new()
        {
            Id = root.GetProperty("id").GetString(),
            Name = root.GetProperty("name").GetString(),
            CombatType = combatType,
            Behavior = behavior,
            Tier = tier,
            PackFamily = OptionalText(root, "pack_family"),
            IsFeminine = OptionalText(root, "grammatical_gender") == "f",
            AttackAudio = OptionalText(root, "attack_audio"),
            Stats = new EnemyStats
            {
                Hp = stats.GetProperty("hp").GetSingle(),
                Speed = stats.GetProperty("speed").GetSingle(),
                Damage = stats.GetProperty("damage").GetSingle(),
                AttackRange = stats.TryGetProperty("attack_range", out JsonElement range) ? range.GetSingle() : EnemyContract.StatDefault("attack_range"),
                XpReward = stats.GetProperty("xp_reward").GetSingle(),
            },
            Visual = new EnemyVisual
            {
                Color = Color.FromHtml(visual.GetProperty("color").GetString()),
                Shape = visual.GetProperty("shape").GetString(),
                Size = visual.GetProperty("size").GetSingle(),
                SpriteFolder = OptionalText(visual, "sprite_folder"),
                SpriteFeetOffset = visual.TryGetProperty("sprite_feet_offset", out JsonElement feet) ? feet.GetSingle() : 0f,
            },
        };
        if (visual.TryGetProperty("projectile", out JsonElement projectile))
        {
            data.Visual.ProjectileSprite = OptionalText(projectile, "sprite") ?? data.Visual.ProjectileSprite;
            data.Visual.ProjectileFamily = OptionalText(projectile, "family") ?? data.Visual.ProjectileFamily;
        }

        foreach (JsonProperty stat in stats.EnumerateObject())
        {
            if (!CoreStats.Contains(stat.Name))
                data.ExtraStats[stat.Name] = stat.Value.GetSingle();
        }

        if (root.TryGetProperty("abilities", out JsonElement abilities))
        {
            foreach (JsonProperty entry in abilities.EnumerateObject())
            {
                EnemyGrammar.TryParseAbility(entry.Name, out EnemyAbilityKind kind);
                data.Abilities[kind] = ResolveAbility(kind, entry.Value);
            }
        }
        return data;
    }

    private static EnemyAbilityData ResolveAbility(EnemyAbilityKind kind, JsonElement values)
    {
        EnemyContract.AbilityRules rules = EnemyContract.Rules(kind);
        EnemyAbilityData ability = new() { Kind = kind };
        foreach ((string name, DataValueRule rule) in rules.Numbers)
        {
            if (values.TryGetProperty(name, out JsonElement value))
                ability.Numbers[name] = value.GetSingle();
            else if (rule.Default is float fallback)
                ability.Numbers[name] = fallback;
        }
        foreach ((string name, EnemyContract.TextRule rule) in rules.Texts)
        {
            if (values.TryGetProperty(name, out JsonElement value))
                ability.Texts[name] = value.GetString();
            else if (rule.Default != null)
                ability.Texts[name] = rule.Default;
        }
        return ability;
    }

    private static string OptionalText(JsonElement owner, string key) =>
        owner.TryGetProperty(key, out JsonElement value) ? value.GetString() : null;
}
