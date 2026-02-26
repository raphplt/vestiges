using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Meta;

/// <summary>
/// Gère la découverte des Souvenirs (fragments de lore) pendant la run.
/// Persiste les découvertes via MetaSaveManager.
/// Lore : se souvenir = rendre possible. Chaque fragment renforce la réalité.
/// </summary>
public partial class SouvenirManager : Node
{
    private EventBus _eventBus;
    private HashSet<string> _discoveredThisRun = new();

    public override void _Ready()
    {
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.LootReceived += OnLootReceived;
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
            _eventBus.LootReceived -= OnLootReceived;
    }

    private void OnLootReceived(string itemType, string itemId, int amount)
    {
        if (itemType != "souvenir")
            return;

        // "random_souvenir" means pick a random undiscovered one
        string souvenirId = itemId == "random_souvenir" ? PickRandomUndiscovered() : itemId;
        if (!string.IsNullOrEmpty(souvenirId))
            DiscoverSouvenir(souvenirId);
    }

    /// <summary>
    /// Découvre un souvenir spécifique. Retourne true si c'est une nouvelle découverte.
    /// </summary>
    public bool DiscoverSouvenir(string souvenirId)
    {
        SouvenirData data = SouvenirDataLoader.Get(souvenirId);
        if (data == null)
        {
            GD.PushWarning($"[SouvenirManager] Unknown souvenir: {souvenirId}");
            return false;
        }

        if (MetaSaveManager.IsSouvenirDiscovered(souvenirId))
        {
            GD.Print($"[SouvenirManager] Already discovered: {data.Name}");
            return false;
        }

        MetaSaveManager.DiscoverSouvenir(souvenirId);
        _discoveredThisRun.Add(souvenirId);

        _eventBus.EmitSignal(EventBus.SignalName.SouvenirDiscovered, souvenirId, data.Name, data.ConstellationId);

        ApplyUnlock(data);

        GD.Print($"[SouvenirManager] Discovered: {data.Name} ({data.ConstellationId})");
        return true;
    }

    /// <summary>
    /// Pioche un souvenir aléatoire parmi les non-découverts.
    /// Utilisé par le loot system quand type = "souvenir".
    /// Retourne null si tout est découvert.
    /// </summary>
    public string PickRandomUndiscovered()
    {
        List<SouvenirData> all = SouvenirDataLoader.GetAll();
        List<string> candidates = new();

        foreach (SouvenirData s in all)
        {
            if (!MetaSaveManager.IsSouvenirDiscovered(s.Id))
                candidates.Add(s.Id);
        }

        if (candidates.Count == 0)
            return null;

        int index = (int)(GD.Randi() % candidates.Count);
        return candidates[index];
    }

    /// <summary>Nombre de souvenirs découverts cette run.</summary>
    public int DiscoveredThisRunCount => _discoveredThisRun.Count;

    private void ApplyUnlock(SouvenirData data)
    {
        if (string.IsNullOrEmpty(data.UnlockType))
            return;

        switch (data.UnlockType)
        {
            case "character":
                MetaSaveManager.UnlockCharacter(data.UnlockId);
                GD.Print($"[SouvenirManager] Unlocked character: {data.UnlockId}");
                break;
            case "recipe":
                GD.Print($"[SouvenirManager] Recipe unlock: {data.UnlockId} (placeholder)");
                break;
            case "perk":
                GD.Print($"[SouvenirManager] Perk unlock: {data.UnlockId} (placeholder)");
                break;
        }
    }
}
