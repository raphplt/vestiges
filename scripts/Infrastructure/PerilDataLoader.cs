using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Règles du Péril (data/scaling/peril.json) : ce que vaut chaque point. Un multiplicateur vaut
/// 1 + valeur par point × Péril.
/// </summary>
public static class PerilDataLoader
{
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
        _loaded = true;

        using FileAccess file = FileAccess.Open("res://data/scaling/peril.json", FileAccess.ModeFlags.Read);
        Json json = new();
        if (file == null || json.Parse(file.GetAsText()) != Error.Ok)
        {
            GD.PushError("[PerilDataLoader] Cannot read data/scaling/peril.json");
            return;
        }

        Godot.Collections.Dictionary root = json.Data.AsGodotDictionary();
        _max = (int)root["max"].AsDouble();
        Godot.Collections.Dictionary perPoint = root["per_point"].AsGodotDictionary();
        _enemyCount = (float)perPoint["enemy_count"].AsDouble();
        _enemyHp = (float)perPoint["enemy_hp"].AsDouble();
        _enemyDamage = (float)perPoint["enemy_damage"].AsDouble();
        _xp = (float)perPoint["xp"].AsDouble();
        _score = (float)perPoint["score"].AsDouble();
        _raritySteps = (float)perPoint["rarity_steps"].AsDouble();
        if (!root.ContainsKey("banish") || root["banish"].VariantType != Variant.Type.Dictionary)
        {
            GD.PushError("[PerilDataLoader] Bloc banish absent : 3 bannissements gratuits, puis un tiers de Péril de plus à chacun");
            return;
        }
        Godot.Collections.Dictionary banish = root["banish"].AsGodotDictionary();
        if (!banish.ContainsKey("free") || !banish.ContainsKey("peril_divisor"))
        {
            GD.PushError("[PerilDataLoader] banish doit définir free et peril_divisor");
            return;
        }
        _banishFree = Mathf.Max(0, (int)banish["free"].AsDouble());
        _banishPerilDivisor = Mathf.Max(1, (int)banish["peril_divisor"].AsDouble());
    }
}
