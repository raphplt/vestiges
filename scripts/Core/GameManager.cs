using System.Collections.Generic;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.Core;

/// <summary>
/// Gestionnaire global de l'état du jeu.
/// Autoload — pilote les transitions entre états (Hub, Run, Death).
/// Porte l'état qui survit aux changements de scène.
/// </summary>
public partial class GameManager : Node
{
    public enum GameState
    {
        Hub,
        Run,
        Death
    }

    private GameState _currentState = GameState.Hub;
    private EventBus _eventBus;

    /// <summary>Personnage sélectionné dans le Hub. Persiste entre scènes.</summary>
    public string SelectedCharacterId { get; set; }

    /// <summary>Données de la dernière run terminée (pour affichage dans le Hub).</summary>
    public RunRecord LastRunData { get; set; }

    /// <summary>Vestiges gagnés lors de la dernière run.</summary>
    public int LastVestigesEarned { get; set; }

    /// <summary>Personnages débloqués lors de la dernière run.</summary>
    public List<string> LastUnlocks { get; set; }

    public GameState CurrentState => _currentState;

    public override void _Ready()
    {
        _eventBus = GetNode<EventBus>("/root/EventBus");
    }

    public void ChangeState(GameState newState)
    {
        if (_currentState == newState)
            return;

        GameState oldState = _currentState;
        _currentState = newState;

        GD.Print($"[GameManager] {oldState} → {newState}");
        _eventBus.EmitSignal(EventBus.SignalName.GameStateChanged, oldState.ToString(), newState.ToString());
    }
}
