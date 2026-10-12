using System;
using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>Instantané des objets accessibles ; la place dans le sac n’empêche jamais de tirer un objet échangeable.</summary>
public sealed class ItemLootPool
{
    private readonly List<PassiveSouvenirData>[] _ranks = { new(), new(), new(), new(), new() };
    private readonly Dictionary<string, int> _owned = new();

    public ItemLootPool(IEnumerable<PassiveSouvenirData> catalog, Func<PassiveSouvenirData, bool> unlocked, Func<string, int> owned)
    {
        foreach (PassiveSouvenirData item in catalog)
        {
            if (item.IsBadge || !unlocked(item)) continue;
            int copies = owned(item.Id);
            if (item.IsCopyLimitReached(copies)) continue;
            int rank = ItemLootConfig.Rank(item.Rarity);
            if (rank < 0) continue;
            _ranks[rank].Add(item);
            _owned[item.Id] = copies;
        }
    }
    public static ItemLootPool For(Player player) => new(PassiveSouvenirDataLoader.GetAll(),
        item => MetaSaveManager.IsObjectUnlocked(item.Id), id => player?.FindObject(id)?.Copies ?? 0);

    private bool Available(PassiveSouvenirData item, IReadOnlyDictionary<string, int> promised) =>
        !item.IsCopyLimitReached(_owned[item.Id] + (promised?.GetValueOrDefault(item.Id) ?? 0));

    public bool CanSupply(int count)
    {
        long available = 0;
        foreach (List<PassiveSouvenirData> rank in _ranks)
            foreach (PassiveSouvenirData item in rank)
            {
                if (!item.HasCopyLimit) return true;
                available += item.MaxCopies - _owned[item.Id];
                if (available >= count) return true;
            }
        return false;
    }

    /// <summary>Normalisation stable même avec une Chance énorme ; les rangs vides ne conservent aucun poids.</summary>
    public double[] Probabilities(string source, float luck = 0f, IReadOnlyDictionary<string, int> promised = null)
    {
        double[] weights = ItemLootConfig.Weights(source);
        double[] result = new double[5];
        double logLuck = source.StartsWith("chest_", StringComparison.Ordinal)
            ? Math.Log(1d + Math.Max(0d, luck)) * ItemLootConfig.LuckExponent : 0d;
        double highest = double.NegativeInfinity;
        int bestAvailable = -1;
        for (int rank = 0; rank < 5; rank++)
        {
            bool available = false;
            foreach (PassiveSouvenirData item in _ranks[rank])
                if (Available(item, promised)) { available = true; break; }
            if (available) bestAvailable = rank;
            result[rank] = available && weights[rank] > 0d ? Math.Log(weights[rank]) + rank * logLuck : double.NegativeInfinity;
            highest = Math.Max(highest, result[rank]);
        }
        if (double.IsNegativeInfinity(highest))
        {
            Array.Clear(result);
            if (bestAvailable >= 0) result[bestAvailable] = 1d;
            return result;
        }
        double sum = 0d;
        for (int rank = 0; rank < 5; rank++) sum += result[rank] = Math.Exp(result[rank] - highest);
        for (int rank = 0; rank < 5; rank++) result[rank] /= sum;
        return result;
    }
    public int RollRank(string source, RandomNumberGenerator rng, float luck = 0f, IReadOnlyDictionary<string, int> promised = null)
    {
        double[] probabilities = Probabilities(source, luck, promised);
        double roll = rng.Randf();
        int last = -1;
        for (int rank = 0; rank < 5; rank++)
        {
            if (probabilities[rank] <= 0d) continue;
            last = rank;
            roll -= probabilities[rank];
            if (roll < 0d) return rank;
        }
        return last;
    }
    public List<PassiveSouvenirData> Choices(int rank, int count, RandomNumberGenerator rng, IReadOnlyDictionary<string, int> promised = null)
    {
        List<PassiveSouvenirData> candidates = new();
        if (rank < 0) return candidates;
        foreach (PassiveSouvenirData item in _ranks[rank]) if (Available(item, promised)) candidates.Add(item);
        for (int i = 0; i < Math.Min(count, candidates.Count); i++)
        {
            int picked = rng.RandiRange(i, candidates.Count - 1);
            (candidates[i], candidates[picked]) = (candidates[picked], candidates[i]);
        }
        if (candidates.Count > count) candidates.RemoveRange(count, candidates.Count - count);
        return candidates;
    }
    public PassiveSouvenirData Pick(string source, RandomNumberGenerator rng, float luck = 0f, IReadOnlyDictionary<string, int> promised = null)
    {
        List<PassiveSouvenirData> choices = Choices(RollRank(source, rng, luck, promised), 1, rng, promised);
        return choices.Count > 0 ? choices[0] : null;
    }
    public List<PassiveSouvenirData> Eligible(string source, float luck = 0f)
    {
        double[] weights = Probabilities(source, luck);
        List<PassiveSouvenirData> result = new();
        for (int rank = 0; rank < 5; rank++) if (weights[rank] > 0d) result.AddRange(_ranks[rank]);
        return result;
    }
}
