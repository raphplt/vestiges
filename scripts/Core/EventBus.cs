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
    [Signal] public delegate void RunPhaseChangedEventHandler(string oldPhase, string newPhase);

    // Contrats C# typés : aucune conversion Variant ni allocation par impact.
    public event System.Action<Combat.DamageResult> EnemyDamageResolved;
    public event System.Action<Combat.EnemyKillResult> EnemyKillResolved;
    public event System.Action<HealingResult> PlayerHealingResolved;
    public event System.Action<PlayerDamageResult> PlayerDamageResolved;

    public void PublishEnemyDamage(Combat.DamageResult result) => EnemyDamageResolved?.Invoke(result);
    public void PublishEnemyKill(Combat.EnemyKillResult result) => EnemyKillResolved?.Invoke(result);
    public void PublishPlayerHealing(HealingResult result) => PlayerHealingResolved?.Invoke(result);
    public void PublishPlayerDamage(PlayerDamageResult result) => PlayerDamageResolved?.Invoke(result);
    /// <summary>Expiration d'un statut infligé par un joueur, pour les effets qui le prolongent (Pince à linge, plan 21 §4).</summary>
    public event System.Action<Combat.StatusExpiry> EnemyStatusExpired;
    public void PublishStatusExpiry(Combat.StatusExpiry expiry) => EnemyStatusExpired?.Invoke(expiry);

    /// <summary>Jauges des perks (réserves, fenêtres) pour leurs retours visuels, publiées à chaque changement.</summary>
    public event System.Action<SpecializationGauge> SpecializationGaugeChanged;
    public void PublishSpecializationGauge(SpecializationGauge gauge) => SpecializationGaugeChanged?.Invoke(gauge);

    // --- Combat ---
    [Signal] public delegate void EntityDamagedEventHandler(Node entity, float amount);
    [Signal] public delegate void EntityDiedEventHandler(Node entity);
    [Signal] public delegate void EnemySpawnedEventHandler(string enemyId, float hpScale, float dmgScale);
    [Signal] public delegate void EnemyKilledEventHandler(string enemyId, Vector2 position);
    [Signal] public delegate void PlayerDamagedEventHandler(float currentHp, float maxHp);
    [Signal] public delegate void PlayerHitByEventHandler(string enemyId, float damage);
    [Signal] public delegate void PlayerShieldChangedEventHandler(float shield, float maxShield);

    // --- Progression ---
    [Signal] public delegate void XpGainedEventHandler(float amount);
    [Signal] public delegate void LevelUpEventHandler(int newLevel);

    // --- Score ---
    [Signal] public delegate void ScoreChangedEventHandler(int newScore);

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
    /// <summary>Arme ramassée au sol : le HUD fait voler son icône de ce point jusqu'à sa case.</summary>
    [Signal] public delegate void WeaponPickedUpEventHandler(string weaponId, Vector2 worldPosition);

    // --- Passive Souvenirs (level-up) ---
    [Signal] public delegate void PassiveSouvenirAddedEventHandler(string passiveId, int slotIndex);
    [Signal] public delegate void PassiveSouvenirUpgradedEventHandler(string passiveId, int newLevel);
    [Signal] public delegate void PassiveSouvenirSlotsChangedEventHandler();

    // --- Fragments de Mémoire (level-up choices) ---
    [Signal] public delegate void FragmentChoicesReadyEventHandler(int count);
    [Signal] public delegate void FragmentChosenEventHandler(string fragmentId, string fragmentType);

    // --- Perks de spécialisation (plan 05, catalogue B) ---
    [Signal] public delegate void SpecializationAcquiredEventHandler(string specializationId);

    // --- Fusions (Vestiges) ---

    // --- Fog of War ---
    [Signal] public delegate void ZoneDiscoveredEventHandler(int cellX, int cellY, int cellCount);

    // --- Souvenirs ---
    [Signal] public delegate void SouvenirDiscoveredEventHandler(string souvenirId, string souvenirName, string constellationId);

    // --- Événements aléatoires ---
    [Signal] public delegate void RandomEventTriggeredEventHandler(string eventId, string eventName);
    [Signal] public delegate void RandomEventEndedEventHandler(string eventId);

    // --- Micro-événements de run ---
    [Signal] public delegate void RunEventStartedEventHandler(string eventId, string title, string objective, float duration);
    [Signal] public delegate void RunEventProgressEventHandler(string objective, float progress, float timeRemaining, Vector2 target, bool hasTarget);
    [Signal] public delegate void RunEventEndedEventHandler(string eventId, bool success, string summary);
    [Signal] public delegate void EventEnemyKilledEventHandler(int eventToken, Vector2 position);

    // --- Variantes d'ennemis ---
    [Signal] public delegate void VariantEnemyKilledEventHandler(string displayName, string variantId, Vector2 position);

    // --- Péril (plan 17 lot 3A) ---
    [Signal] public delegate void PerilChangedEventHandler(int peril);
    /// <summary>Total des Oublis portés pour un effet de carte (data/progression/oublis.json), après prise ou levée.</summary>
    [Signal] public delegate void OubliEffectChangedEventHandler(string effect, float total);

    // --- Effacement (V2) ---
    [Signal] public delegate void ErasureUpdatedEventHandler(float globalErasurePercent);
    [Signal] public delegate void ZonePhaseChangedEventHandler(int cellX, int cellY, int phase);
    /// <summary>Phase de la zone où se tient le joueur (ErasureManager.ErasureZonePhase), émise quand elle change.</summary>
    [Signal] public delegate void PlayerErasurePhaseChangedEventHandler(int phase);

    // --- Résurgences (V2) ---
    [Signal] public delegate void CrisisWarningEventHandler(int crisisNumber, float countdown);
    [Signal] public delegate void CrisisStartedEventHandler(int crisisNumber, int intensity);
    [Signal] public delegate void CrisisEndedEventHandler(int crisisNumber);
    /// <summary>Accalmie qui suit une Résurgence (CrisisAftermath) : ouverte à la fin de la crise, close au terme de sa durée ou par une nouvelle crise.</summary>
    [Signal] public delegate void CrisisCalmChangedEventHandler(bool active);

    // --- Mémoriaux (plan 17 lot 3B) ---
    [Signal] public delegate void MemorialInteractedEventHandler(Node2D memorial);
    /// <summary>Un Mémorial vient d'être ravivé : la zone autour se souvient.</summary>
    [Signal] public delegate void MemorialAwakenedEventHandler(Vector2 position);

    // --- Ateliers (plan 22 C2) ---
    [Signal] public delegate void WorkshopInteractedEventHandler(Node2D workshop);
    /// <summary>Première visite d'un Atelier : la zone se souvient et le Repère se gagne.</summary>
    [Signal] public delegate void WorkshopVisitedEventHandler(Vector2 position);

    // --- Failles (plan 17 lot 3C) ---
    [Signal] public delegate void RiftInteractedEventHandler(Node2D rift);
    /// <summary>Une offre de Faille vient d'être acceptée : la Faille se referme (Repères, plan 23 R9).</summary>
    [Signal] public delegate void RiftUsedEventHandler(Vector2 position);

    // --- Petits lieux (plan 22 C1 et C4) ---
    /// <summary>Un petit lieu vient de servir (Repères, plan 23 R9).</summary>
    [Signal] public delegate void SmallPlaceUsedEventHandler(string placeId, Vector2 position);
    /// <summary>Une quête de run a changé : rang, nom, part accomplie (0 à 1), remplie (plan 24 A2).</summary>
    [Signal] public delegate void RunQuestUpdatedEventHandler(int index, string name, float progress, bool completed);
    /// <summary>Relances et bannissements gratuits gagnés hors du level-up (Repère d'un Mémorial ou d'une Faille).</summary>
    [Signal] public delegate void ChoiceTokensGrantedEventHandler(int rerolls, int banishes);

    // --- Essence (V2) ---
    [Signal] public delegate void EssenceChangedEventHandler(int newAmount);
    /// <summary>Essence gagnée à un endroit du monde (Vector2.Zero : sans lieu), pour son trajet vers le HUD.</summary>
    [Signal] public delegate void EssenceGainedEventHandler(int amount, Vector2 worldPosition);
    /// <summary>Multiplicateur d'Essence en cours (accalmie après une crise) et sa durée ; 1 et 0 à la fin.</summary>
    [Signal] public delegate void EssenceMultiplierChangedEventHandler(float multiplier, float seconds);
}
