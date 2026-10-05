using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Règles du Péril (data/scaling/peril.json) : ce que vaut chaque point. Un multiplicateur vaut
/// 1 + valeur par point × Péril.
/// </summary>
public static class PerilDataLoader
{
    private const string ConfigPath = "res://data/scaling/peril.json";
    private static int _max = 10;
    private static float _enemyCount;
    private static float _enemyHp;
    private static float _enemyDamage;
    private static float _xp;
    private static float _score;
    private static float _raritySteps;
    private static int _banishFree = 3;
    private static int _banishPerilDivisor = 3;
    private static bool _loaded;

    public static int Max
    {
        get
        {
            Load();
            return _max;
        }
    }

    public static float EnemyCountMultiplier(int peril)
    {
        Load();
        return 1f + _enemyCount * peril;
    }

    public static float EnemyHpMultiplier(int peril)
    {
        Load();
        return 1f + _enemyHp * peril;
    }

    public static float EnemyDamageMultiplier(int peril)
    {
        Load();
        return 1f + _enemyDamage * peril;
    }

    public static float XpMultiplier(int peril)
    {
        Load();
        return 1f + _xp * peril;
    }

    public static float ScoreMultiplier(int peril)
    {
        Load();
        return 1f + _score * peril;
    }

    /// <summary>Crans de montée de rareté dus au Péril (voir <c>UpgradeRoller.BumpSteps</c>).</summary>
    public static float RaritySteps(int peril)
    {
        Load();
        return _raritySteps * peril;
    }

    /// <summary>Bannissements gratuits par run avant qu'ils ne coûtent du Péril.</summary>
    public static int BanishFree
    {
        get
        {
            Load();
            return _banishFree;
        }
    }

    /// <summary>Le n-ième bannissement payant coûte n fractions de Péril ; il en faut autant pour un point.</summary>
    public static int BanishPerilDivisor
    {
        get
        {
            Load();
            return _banishPerilDivisor;
        }
    }

    private static void Load()
    {
        if (_loaded)
            return;
        if (!TryLoad(out string error))
            GD.PushError($"[PerilDataLoader] {error}");
    }

    /// <summary>
    /// Lit et contrôle tout le fichier (plan 26 Q7a) ; rien n'est publié s'il est invalide. Un fichier invalide arrête
    /// le chargement de la run (GameBootstrap).
    /// </summary>
    public static bool TryLoad(out string error)
    {
        _loaded = true;
        using FileAccess file = FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            error = $"{ConfigPath} absent";
            return false;
        }
        error = Apply(file.GetAsText());
        if (error != null)
            error = $"{ConfigPath} : {error}";
        return error == null;
    }

    /// <summary>Contrôle un texte de réglages ; ne publie les valeurs que s'il est entièrement valide. Rend l'erreur, ou null.</summary>
    public static string Apply(string json)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonConfigReader reader = new(document.RootElement);
            reader.AllowOnly(reader.Root, "Péril", "max", "banish", "per_point");
            int max = reader.Integer(reader.Root, "max", 0, 1000);
            JsonElement banish = reader.Section("banish");
            reader.AllowOnly(banish, "banish", "free", "peril_divisor");
            int free = reader.Integer(banish, "free", 0, 1000);
            int divisor = reader.Integer(banish, "peril_divisor", 1, 1000);
            JsonElement perPoint = reader.Section("per_point");
            reader.AllowOnly(perPoint, "per_point", "enemy_count", "enemy_hp", "enemy_damage", "xp", "score", "rarity_steps");
            float enemyCount = reader.NonNegative(perPoint, "enemy_count");
            float enemyHp = reader.NonNegative(perPoint, "enemy_hp");
            float enemyDamage = reader.NonNegative(perPoint, "enemy_damage");
            float xp = reader.NonNegative(perPoint, "xp");
            float score = reader.NonNegative(perPoint, "score");
            float raritySteps = reader.NonNegative(perPoint, "rarity_steps");
            if (reader.Error != null)
                return reader.Error;
            _max = max;
            _banishFree = free;
            _banishPerilDivisor = divisor;
            _enemyCount = enemyCount;
            _enemyHp = enemyHp;
            _enemyDamage = enemyDamage;
            _xp = xp;
            _score = score;
            _raritySteps = raritySteps;
            return null;
        }
        catch (JsonException ex)
        {
            return $"JSON illisible : {ex.Message}";
        }
    }
}
