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
    [Signal] public delegate void EnemySpawnedEventHandler(string enemyId, float hpScale, float dmgScale);
    [Signal] public delegate void EnemyKilledEventHandler(string enemyId, Vector2 position);
    [Signal] public delegate void PlayerDamagedEventHandler(float currentHp, float maxHp);
    [Signal] public delegate void PlayerHitByEventHandler(string enemyId, float damage);

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
    [Signal] public delegate void WeaponUpgradedEventHandler(string weaponId, int slotIndex, string stat, int newLevel);
    [Signal] public delegate void WeaponDroppedEventHandler(string weaponId);

    // --- Passive Souvenirs (level-up) ---
    [Signal] public delegate void PassiveSouvenirAddedEventHandler(string passiveId, int slotIndex);
    [Signal] public delegate void PassiveSouvenirUpgradedEventHandler(string passiveId, int newLevel);
    [Signal] public delegate void PassiveSouvenirSlotsChangedEventHandler();

    // --- Fragments de Mémoire (level-up choices) ---
    [Signal] public delegate void FragmentChoicesReadyEventHandler(int count);
    [Signal] public delegate void FragmentChosenEventHandler(string fragmentId, string fragmentType);

    // --- Fusions (Vestiges) ---
    [Signal] public delegate void FusionAvailableEventHandler(string fusionId, string weaponId, string passiveId);
    [Signal] public delegate void FusionCompletedEventHandler(string fusionId);

    // --- Fog of War ---
    [Signal] public delegate void ZoneDiscoveredEventHandler(int cellX, int cellY, int cellCount);

    // --- Souvenirs ---
    [Signal] public delegate void SouvenirDiscoveredEventHandler(string souvenirId, string souvenirName, string constellationId);

    // --- Événements aléatoires ---
    [Signal] public delegate void RandomEventTriggeredEventHandler(string eventId, string eventName);
    [Signal] public delegate void RandomEventEndedEventHandler(string eventId);
}
