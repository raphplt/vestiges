using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Règles du Péril (data/scaling/peril.json) : ce que vaut chaque point. Un multiplicateur vaut
/// 1 + valeur par point × Péril ; le Péril n'a pas de plafond, seul le nombre de créatures en a un (coût du rendu).
/// </summary>
public static class PerilDataLoader
{
    private const string ConfigPath = "res://data/scaling/peril.json";
    private static float _enemyCount;
    private static float _enemyCountBonusMax;
    private static int _activeEnemiesCeiling;
    private static float _enemyHp;
    private static float _enemyDamage;
    private static float _score;
    private static int _banishFree = 3;
    private static int _banishPerilDivisor = 3;
    private static bool _loaded;

    /// <summary>Densité et plafond de créatures ; bonus borné à <c>enemy_count_bonus_max</c>, au-delà les points ne pèsent plus que sur PV et dégâts.</summary>
    public static float EnemyCountMultiplier(float peril)
    {
        Load();
        return 1f + Mathf.Min(_enemyCount * peril, _enemyCountBonusMax);
    }

    /// <summary>Créatures actives que le Péril ne fait jamais dépasser : il ne relève le plafond de la run que jusque-là.</summary>
    public static int ActiveEnemiesCeiling
    {
        get
        {
            Load();
            return _activeEnemiesCeiling;
        }
    }

    public static float EnemyHpMultiplier(float peril)
    {
        Load();
        return 1f + _enemyHp * peril;
    }

    public static float EnemyDamageMultiplier(float peril)
    {
        Load();
        return 1f + _enemyDamage * peril;
    }

    public static float ScoreMultiplier(int peril)
    {
        Load();
        return 1f + _score * peril;
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
            reader.AllowOnly(reader.Root, "Péril", "enemy_count_bonus_max", "active_enemies_ceiling", "banish", "per_point");
            float enemyCountBonusMax = reader.NonNegative(reader.Root, "enemy_count_bonus_max");
            int ceiling = reader.Integer(reader.Root, "active_enemies_ceiling", 1, 2000);
            JsonElement banish = reader.Section("banish");
            reader.AllowOnly(banish, "banish", "free", "peril_divisor");
            int free = reader.Integer(banish, "free", 0, 1000);
            int divisor = reader.Integer(banish, "peril_divisor", 1, 1000);
            JsonElement perPoint = reader.Section("per_point");
            reader.AllowOnly(perPoint, "per_point", "enemy_count", "enemy_hp", "enemy_damage", "score");
            float enemyCount = reader.NonNegative(perPoint, "enemy_count");
            float enemyHp = reader.NonNegative(perPoint, "enemy_hp");
            float enemyDamage = reader.NonNegative(perPoint, "enemy_damage");
            float score = reader.NonNegative(perPoint, "score");
            if (reader.Error != null)
                return reader.Error;
            _enemyCountBonusMax = enemyCountBonusMax;
            _activeEnemiesCeiling = ceiling;
            _banishFree = free;
            _banishPerilDivisor = divisor;
            _enemyCount = enemyCount;
            _enemyHp = enemyHp;
            _enemyDamage = enemyDamage;
            _score = score;
            return null;
        }
        catch (JsonException ex)
        {
            return $"JSON illisible : {ex.Message}";
        }
    }
}
