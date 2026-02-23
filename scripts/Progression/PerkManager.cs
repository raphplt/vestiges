using System.Collections.Generic;
using System.Linq;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Progression;

/// <summary>
/// Gère les perks actifs, propose des choix au level up, applique les modifiers au joueur.
/// Supporte les perks passifs, les perks exclusifs par personnage, la sélection pondérée,
/// la détection de synergies, et les effets complexes (vampirism, ignite, etc.).
/// </summary>
public partial class PerkManager : Node
{
    private const int PerksPerChoice = 3;

    /// <summary>
    /// Perks whose mechanics depend on systems not yet implemented.
    /// They are loaded as data but never offered in level-up or memorial choices.
    /// Remove IDs from this set as their systems come online.
    /// </summary>
    private static readonly HashSet<string> _disabledPerks = new()
    {
        // Essence system not implemented
        "channeling", "siphon", "instability", "essence_regen",
        // Light / vision / fog system not implemented
        "torch_bearer", "night_vision", "awakened_sight",
        // Day cycle modifier not implemented
        "time_master",
        // Foyer aura / structure-count mechanics not implemented
        "memory_anchor", "last_stand",
        // Salvage system not implemented
        "salvager",
        // AoE system not implemented (stat applies but unused in combat)
        "aoe_up",
        // Turret stat not implemented
        "forgeuse_overcharge",
        // Complex character perks requiring unbuilt systems
        "vagabond_jack_of_all", "vagabond_nomad", "vagabond_scrounger",
        "forgeuse_recycler", "forgeuse_last_wall",
        "traqueur_ambush", "traqueur_marked",
    };

    private readonly Dictionary<string, int> _activeStacks = new();
    private readonly HashSet<string> _activeSynergies = new();
    private EventBus _eventBus;
    private Player _player;
    private string _characterId;

    [Signal] public delegate void PerkChoicesReadyEventHandler(string[] perkIds);
    [Signal] public delegate void PerkAppliedEventHandler(string perkId, int stacks);
    [Signal] public delegate void SynergyActivatedEventHandler(string synergyId, string notification);

    public override void _Ready()
    {
        PerkDataLoader.Load();
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.LevelUp += OnLevelUp;
        _eventBus.MemorialActivated += OnMemorialActivated;
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
        {
            _eventBus.LevelUp -= OnLevelUp;
            _eventBus.MemorialActivated -= OnMemorialActivated;
        }
    }

    public void ApplyPassivePerks(string characterId)
    {
        _characterId = characterId;

        List<PerkData> passives = PerkDataLoader.GetAll()
            .Where(p => p.IsPassive && p.CharacterId == characterId)
            .ToList();

        foreach (PerkData passive in passives)
        {
            _activeStacks[passive.Id] = 1;
            ApplyPerkToPlayer(passive);
            GD.Print($"[PerkManager] Applied passive: {passive.Name}");
        }
    }

    private void OnLevelUp(int newLevel)
    {
        string[] choices = PickRandomPerks(PerksPerChoice);
        EmitSignal(SignalName.PerkChoicesReady, choices);
    }

    private void OnMemorialActivated()
    {
        string[] choices = PickRandomPerks(PerksPerChoice);
        if (choices.Length > 0)
            EmitSignal(SignalName.PerkChoicesReady, choices);
    }

    public void SelectPerk(string perkId)
    {
        PerkData data = PerkDataLoader.Get(perkId);
        if (data == null)
            return;

        int currentStacks = _activeStacks.GetValueOrDefault(perkId, 0);
        if (currentStacks >= data.MaxStacks)
            return;

        _activeStacks[perkId] = currentStacks + 1;
        ApplyPerkToPlayer(data);

        _eventBus.EmitSignal(EventBus.SignalName.PerkChosen, perkId);
        EmitSignal(SignalName.PerkApplied, perkId, _activeStacks[perkId]);

        GD.Print($"[PerkManager] Applied {data.Name} (stack {_activeStacks[perkId]}/{data.MaxStacks})");

        CheckSynergies();
    }

    private void ApplyPerkToPlayer(PerkData data)
    {
        CachePlayer();
        if (_player == null)
            return;

        // Multi-effect perks (effects array)
        if (data.Effects != null && data.Effects.Count > 0)
        {
            foreach (PerkEffect effect in data.Effects)
            {
                if (effect.Stat != null)
                    _player.ApplyPerkModifier(effect.Stat, effect.Modifier, effect.ModifierType);
            }
        }
        // Simple stat modifier
        else if (data.Stat != null)
        {
            _player.ApplyPerkModifier(data.Stat, data.Modifier, data.ModifierType);
        }

        // Complex effect (singular effect dict)
        if (data.Effect != null)
            ApplyComplexEffect(data);
    }

    private void ApplyComplexEffect(PerkData data)
    {
        ComplexEffect fx = data.Effect;

        // Stat modifiers in effect format (trigger=passive, action=modify_stat)
        if (fx.Action == "modify_stat" && fx.Stat != null && fx.Trigger == "passive")
        {
            _player.ApplyPerkModifier(fx.Stat, fx.Modifier, fx.ModifierType);
            return;
        }

        // Conditional stat modifier (berserker-type: bonus when HP below threshold)
        if (fx.Trigger == "passive_conditional" && fx.Condition == "hp_percent_below" && fx.Action == "modify_stat")
        {
            _player.AddBerserker(fx.ConditionValue, fx.Modifier);
            return;
        }

        switch (fx.Action)
        {
            case "heal_percent_of_damage":
                _player.AddVampirism(fx.Value);
                break;

            case "apply_dot":
                _player.AddIgnite(fx.Chance, fx.DotDamage, fx.DotDuration);
                break;

            case "bounce_to_nearby":
                _player.AddRicochet(fx.Chance, fx.BounceRange);
                break;

            case "reflect_damage_percent":
                _player.AddThorns(fx.Value);
                break;

            case "execute_below_percent":
                _player.AddExecution(fx.Value);
                break;

            case "revive":
                _player.SetSecondWind(fx.HealPercent);
                break;

            case "temporary_buff":
                _player.AddKillSpeed(fx.Modifier, fx.Duration, fx.MaxBuffStacks);
                break;

            case "bonus_resource":
                _player.AddHarvestBonus((int)fx.Value);
                break;

            case "dodge":
                _player.AddDodge(fx.Chance);
                break;

            default:
                // Effects not yet implemented (torch_bearer, night_vision, essence perks, etc.)
                GD.Print($"[PerkManager] Complex effect '{fx.Action}' for {data.Id} not yet implemented");
                break;
        }
    }

    private void CheckSynergies()
    {
        List<PerkData> synergies = PerkDataLoader.GetSynergies();

        foreach (PerkData synergy in synergies)
        {
            if (_activeSynergies.Contains(synergy.Id))
                continue;

            if (synergy.RequiredPerks == null || synergy.RequiredPerks.Count == 0)
                continue;

            bool allPresent = synergy.RequiredPerks.All(req => _activeStacks.ContainsKey(req));
            if (!allPresent)
                continue;

            _activeSynergies.Add(synergy.Id);

            string notification = synergy.Notification ?? synergy.Name;
            EmitSignal(SignalName.SynergyActivated, synergy.Id, notification);
            _eventBus.EmitSignal(EventBus.SignalName.SynergyActivated, synergy.Id, notification);

            GD.Print($"[PerkManager] Synergy activated: {synergy.Name}");
        }
    }

    /// <summary>
    /// Weighted random selection. Perks with lower Weight (e.g. 0.25 for rares) appear less often.
    /// </summary>
    private string[] PickRandomPerks(int count)
    {
        List<PerkData> available = PerkDataLoader.GetAll()
            .Where(p =>
                !p.IsPassive
                && !_disabledPerks.Contains(p.Id)
                && _activeStacks.GetValueOrDefault(p.Id, 0) < p.MaxStacks
                && (p.CharacterId == null || p.CharacterId == _characterId))
            .ToList();

        if (available.Count == 0)
            return System.Array.Empty<string>();

        List<string> picked = new();
        for (int i = 0; i < count && available.Count > 0; i++)
        {
            PerkData selected = WeightedRandom(available);
            picked.Add(selected.Id);
            available.Remove(selected);
        }

        return picked.ToArray();
    }

    private static PerkData WeightedRandom(List<PerkData> perks)
    {
        float totalWeight = 0f;
        foreach (PerkData perk in perks)
            totalWeight += perk.Weight;

        float roll = (float)(GD.Randf() * totalWeight);
        float cumulative = 0f;

        foreach (PerkData perk in perks)
        {
            cumulative += perk.Weight;
            if (roll <= cumulative)
                return perk;
        }

        return perks[perks.Count - 1];
    }

    public int GetStacks(string perkId)
    {
        return _activeStacks.GetValueOrDefault(perkId, 0);
    }

    public bool IsSynergyActive(string synergyId)
    {
        return _activeSynergies.Contains(synergyId);
    }

    private void CachePlayer()
    {
        if (_player != null && IsInstanceValid(_player))
            return;

        Node playerNode = GetTree().GetFirstNodeInGroup("player");
        if (playerNode is Player p)
            _player = p;
    }
}
