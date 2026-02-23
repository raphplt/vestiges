using System.Collections.Generic;
using System.Linq;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Progression;

/// <summary>
/// Gère les perks actifs, propose des choix au level up, applique les modifiers au joueur.
/// Supporte les perks passifs (appliqués au start) et les perks exclusifs par personnage.
/// </summary>
public partial class PerkManager : Node
{
    private const int PerksPerChoice = 3;

    private readonly Dictionary<string, int> _activeStacks = new();
    private EventBus _eventBus;
    private Player _player;
    private string _characterId;

    [Signal] public delegate void PerkChoicesReadyEventHandler(string[] perkIds);
    [Signal] public delegate void PerkAppliedEventHandler(string perkId, int stacks);

    public override void _Ready()
    {
        PerkDataLoader.Load();
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.LevelUp += OnLevelUp;
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
            _eventBus.LevelUp -= OnLevelUp;
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
    }

    private void ApplyPerkToPlayer(PerkData data)
    {
        CachePlayer();
        if (_player == null)
            return;

        if (data.Effects != null && data.Effects.Count > 0)
        {
            foreach (PerkEffect effect in data.Effects)
                _player.ApplyPerkModifier(effect.Stat, effect.Modifier, effect.ModifierType);
        }
        else if (data.Stat != null)
        {
            _player.ApplyPerkModifier(data.Stat, data.Modifier, data.ModifierType);
        }
    }

    private string[] PickRandomPerks(int count)
    {
        List<PerkData> available = PerkDataLoader.GetAll()
            .Where(p =>
                !p.IsPassive
                && _activeStacks.GetValueOrDefault(p.Id, 0) < p.MaxStacks
                && (p.CharacterId == null || p.CharacterId == _characterId))
            .ToList();

        if (available.Count == 0)
            return System.Array.Empty<string>();

        List<string> picked = new();
        for (int i = 0; i < count && available.Count > 0; i++)
        {
            int index = (int)(GD.Randi() % available.Count);
            picked.Add(available[index].Id);
            available.RemoveAt(index);
        }

        return picked.ToArray();
    }

    public int GetStacks(string perkId)
    {
        return _activeStacks.GetValueOrDefault(perkId, 0);
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
