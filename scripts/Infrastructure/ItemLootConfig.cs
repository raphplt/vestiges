using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>Rangs fixes, poids des sources et prix. Publication atomique après validation complète.</summary>
public static class ItemLootConfig
{
    public const string Path = "res://data/progression/item_loot.json";
    public static readonly string[] Rarities = { "common", "uncommon", "rare", "epic", "legendary" };
    public static readonly string[] Sources = { "generic", "chest_common", "chest_rare", "chest_epic", "chest_lore", "elite", "sovereign", "workshop" };
    private static Dictionary<string, double[]> _weights = new();
    private static Dictionary<string, int> _chestCosts = new();
    private static int[] _workshopCosts = new int[5];
    private static bool _loaded;
    public static float ChestGrowth { get; private set; }
    public static float LuckExponent { get; private set; }

    public static int Rank(string rarity) => Array.IndexOf(Rarities, rarity);
    public static bool TryLoad(out string error)
    {
        error = null;
        if (_loaded) return true;
        error = FileAccess.FileExists(Path) ? Apply(FileAccess.GetFileAsString(Path)) : "Fichier item_loot absent";
        _loaded = error == null;
        return _loaded;
    }
    public static void Load()
    {
        if (!TryLoad(out string error)) throw new InvalidOperationException($"[ItemLootConfig] {error}");
    }
    public static double[] Weights(string source)
    {
        Load();
        return _weights.TryGetValue(source, out double[] weights) ? weights : throw new ArgumentException($"Source inconnue : {source}");
    }
    public static int ChestCost(string rarity, float elapsedSeconds)
    {
        Load();
        double amount = _chestCosts[rarity] * (1d + ChestGrowth * Math.Floor(Math.Max(0f, elapsedSeconds) / 60d));
        return (int)Math.Min(int.MaxValue, Math.Ceiling(amount));
    }
    public static int WorkshopCost(string rarity) { Load(); return _workshopCosts[Rank(rarity)]; }

    public static string Apply(string json)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonConfigReader reader = new(document.RootElement);
            JsonElement root = reader.Root;
            reader.AllowOnly(root, "butin", "sources", "chest_costs", "chest_cost_growth_per_minute", "chest_luck_exponent_per_rank", "workshop_costs");
            JsonElement sources = reader.Section("sources");
            reader.AllowOnly(sources, "sources", Sources);
            Dictionary<string, double[]> weights = new();
            foreach (string source in Sources)
            {
                JsonElement row = reader.Section(sources, source);
                reader.AllowOnly(row, source, Rarities);
                double[] values = new double[5];
                double sum = 0d;
                for (int rank = 0; rank < values.Length; rank++) sum += values[rank] = reader.NonNegative(row, Rarities[rank]);
                if (Math.Abs(sum - 100d) > .001d) reader.Fail($"{source} : poids total 100 attendu");
                weights[source] = values;
            }
            JsonElement costs = reader.Section("chest_costs");
            string[] chestRarities = { "common", "rare", "epic", "lore" };
            reader.AllowOnly(costs, "chest_costs", chestRarities);
            Dictionary<string, int> chestCosts = new();
            foreach (string rarity in chestRarities) chestCosts[rarity] = reader.Count(costs, rarity, 1000000);
            JsonElement workshop = reader.Section("workshop_costs");
            reader.AllowOnly(workshop, "workshop_costs", Rarities);
            int[] workshopCosts = new int[5];
            for (int rank = 0; rank < 5; rank++) workshopCosts[rank] = reader.Count(workshop, Rarities[rank], 1000000);
            float growth = reader.NonNegative(root, "chest_cost_growth_per_minute");
            float exponent = reader.NonNegative(root, "chest_luck_exponent_per_rank");
            if (reader.Error != null) return reader.Error;
            _weights = weights;
            _chestCosts = chestCosts;
            _workshopCosts = workshopCosts;
            ChestGrowth = growth;
            LuckExponent = exponent;
            return null;
        }
        catch (JsonException exception) { return exception.Message; }
    }
}
