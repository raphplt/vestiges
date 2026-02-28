using Godot;

namespace Vestiges.Core;

/// <summary>
/// Bus d'événements global pour la communication inter-systèmes découplée.
/// Autoload — jamais instancié manuellement.
/// </summary>
public partial class EventBus : Node
{
    // --- Game State ---
    [Signal] public delegate void GameStateChangedEventHandler(string oldState, string newState);

    // --- Combat ---
    [Signal] public delegate void EntityDamagedEventHandler(Node entity, float amount);
    [Signal] public delegate void EntityDiedEventHandler(Node entity);
    [Signal] public delegate void EnemyKilledEventHandler(string enemyId, Vector2 position);
    [Signal] public delegate void PlayerDamagedEventHandler(float currentHp, float maxHp);

    // --- Progression ---
    [Signal] public delegate void XpGainedEventHandler(float amount);
    [Signal] public delegate void LevelUpEventHandler(int newLevel);
    [Signal] public delegate void PerkChosenEventHandler(string perkId);
    [Signal] public delegate void SynergyActivatedEventHandler(string synergyId, string notification);

    // --- Cycle Jour/Nuit ---
    [Signal] public delegate void DayPhaseChangedEventHandler(string phase);

    // --- Score ---
    [Signal] public delegate void ScoreChangedEventHandler(int newScore);

    // --- Résumé de nuit ---
    [Signal] public delegate void NightSummaryEventHandler(int nightNumber, int kills, int score);

    // --- Ressources & Inventaire ---
    [Signal] public delegate void ResourceCollectedEventHandler(string resourceId, int amount);
    [Signal] public delegate void InventoryChangedEventHandler(string resourceId, int newAmount);

    // --- Craft ---
    [Signal] public delegate void CraftStartedEventHandler(string recipeId);
    [Signal] public delegate void CraftCompletedEventHandler(string recipeId);

    // --- Structures ---
    [Signal] public delegate void StructurePlacedEventHandler(string structureId, Vector2 position);
    [Signal] public delegate void StructureDestroyedEventHandler(string structureId, Vector2 position);

    // --- Mémorial ---
    [Signal] public delegate void MemorialActivatedEventHandler();

    // --- Points d'Intérêt ---
    [Signal] public delegate void PoiDiscoveredEventHandler(string poiId, string poiType, Vector2 position);
    [Signal] public delegate void PoiExploredEventHandler(string poiId, string poiType);

    // --- Coffres & Loot ---
    [Signal] public delegate void ChestOpenedEventHandler(string chestId, string rarity, Vector2 position);
    [Signal] public delegate void LootReceivedEventHandler(string itemType, string itemId, int amount);

    // --- Armes ---
    [Signal] public delegate void WeaponEquippedEventHandler(string weaponId, int slotIndex);
    [Signal] public delegate void WeaponInventoryChangedEventHandler();

    // --- Fog of War ---
    [Signal] public delegate void ZoneDiscoveredEventHandler(int cellX, int cellY, int cellCount);

    // --- Souvenirs ---
    [Signal] public delegate void SouvenirDiscoveredEventHandler(string souvenirId, string souvenirName, string constellationId);
}
