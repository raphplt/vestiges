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

    // --- Cycle Jour/Nuit ---
    [Signal] public delegate void DayPhaseChangedEventHandler(string phase);

    // --- Score ---
    [Signal] public delegate void ScoreChangedEventHandler(int newScore);
}
