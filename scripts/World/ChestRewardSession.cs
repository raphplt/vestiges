using System;
using System.Collections.Generic;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>Une ouverture : Chance et catalogue figés, un rang par récompense, seules les décisions prises consomment les disponibilités.</summary>
public sealed class ChestRewardSession
{
    private readonly ItemLootPool _pool;
    private readonly string _source;
    private readonly float _luck;
    private readonly int _count, _choices;
    private readonly RandomNumberGenerator _rng;
    private readonly Dictionary<string, int> _promised = new();
    private readonly List<ResolvedLoot> _rewards = new();
    private List<PassiveSouvenirData> _current;
    public IReadOnlyList<ResolvedLoot> Rewards => _rewards;
    public bool Complete => _rewards.Count >= _count;
    public bool Applied { get; private set; }
    public IReadOnlyList<PassiveSouvenirData> Eligible { get; }

    public ChestRewardSession(ItemLootPool pool, string source, float luck, int count, int extraChoices, RandomNumberGenerator rng)
    {
        _pool = pool; _source = source; _luck = luck; _count = count; _choices = 1 + Math.Max(0, extraChoices);
        // Flux privé : aucun délai de lecture ou autre système ne peut modifier les récompenses suivantes.
        _rng = new RandomNumberGenerator { Seed = rng.Randi() };
        Eligible = pool.Eligible(source, luck);
    }
    public IReadOnlyList<PassiveSouvenirData> Propose()
    {
        if (Complete) return Array.Empty<PassiveSouvenirData>();
        return _current ??= _pool.Choices(_pool.RollRank(_source, _rng, _luck, _promised), _choices, _rng, _promised);
    }
    public bool Choose(int index)
    {
        if (_current == null || index < 0 || index >= _current.Count || Complete) return false;
        PassiveSouvenirData item = _current[index];
        _promised[item.Id] = _promised.GetValueOrDefault(item.Id) + 1;
        _rewards.Add(LootRewards.Object(item));
        _current = null;
        return true;
    }
    public bool MarkApplied()
    {
        if (Applied || !Complete) return false;
        Applied = true;
        return true;
    }
}
