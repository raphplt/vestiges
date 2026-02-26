using System.Collections.Generic;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Résout les loot tables en récompenses concrètes.
/// Utilitaire statique sans dépendance Node, testable en isolation.
/// </summary>
public static class LootResolver
{
    public struct LootResult
    {
        public string Type;
        public string ItemId;
        public int Amount;
    }

    /// <summary>
    /// Lance N rolls sur une loot table et retourne les résultats.
    /// Chaque roll pioche une entrée pondérée et génère un montant aléatoire.
    /// </summary>
    public static List<LootResult> Roll(string lootTableId, int rolls)
    {
        List<LootResult> results = new();

        if (string.IsNullOrEmpty(lootTableId))
            return results;

        LootTableData table = LootTableLoader.Get(lootTableId);
        if (table == null || table.Entries.Count == 0)
            return results;

        for (int i = 0; i < rolls; i++)
        {
            LootEntry entry = PickWeighted(table.Entries);
            if (entry == null)
                continue;

            int amount = (int)GD.RandRange(entry.MinAmount, entry.MaxAmount + 1);
            results.Add(new LootResult
            {
                Type = entry.Type,
                ItemId = entry.Item,
                Amount = amount
            });
        }

        return results;
    }

    private static LootEntry PickWeighted(List<LootEntry> entries)
    {
        float totalWeight = 0f;
        foreach (LootEntry entry in entries)
            totalWeight += entry.Weight;

        if (totalWeight <= 0f)
            return null;

        float roll = (float)GD.Randf() * totalWeight;
        float cumulative = 0f;

        foreach (LootEntry entry in entries)
        {
            cumulative += entry.Weight;
            if (roll < cumulative)
                return entry;
        }

        return entries[entries.Count - 1];
    }
}
