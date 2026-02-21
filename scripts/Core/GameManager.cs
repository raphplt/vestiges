using Godot;

namespace Vestiges.Core;

/// <summary>
/// Gestionnaire global de l'état du jeu.
/// Autoload — pilote les transitions entre états (Hub, Run, Death).
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
