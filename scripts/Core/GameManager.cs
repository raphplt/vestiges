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

    public enum RunPhase
    {
        Exploration,
        Crisis,
        LateGame,
        Endgame,
        Death
    }

    private GameState _currentState = GameState.Hub;
    private RunPhase _currentRunPhase = RunPhase.Exploration;
    private EventBus _eventBus;

    /// <summary>Personnage sélectionné dans le Hub. Persiste entre scènes.</summary>
    public string SelectedCharacterId { get; set; }

    /// <summary>Données de la dernière run terminée (pour affichage dans le Hub).</summary>
    public RunRecord LastRunData { get; set; }

    /// <summary>Seed de la run. 0 = aléatoire au lancement.</summary>
    public ulong RunSeed { get; set; }

    /// <summary>Vestiges gagnés lors de la dernière run.</summary>
    public int LastVestigesEarned { get; set; }

    /// <summary>Personnages débloqués lors de la dernière run.</summary>
    public List<string> LastUnlocks { get; set; }

    /// <summary>Quêtes de progression validées lors de la dernière run.</summary>
    public List<string> LastQuestCompletions { get; set; }

    /// <summary>Mutateurs actifs pour la prochaine run (sélectionnés dans le Hub).</summary>
    public List<string> ActiveMutators { get; set; } = new();

    public GameState CurrentState => _currentState;
    public RunPhase CurrentRunPhase => _currentRunPhase;

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

        if (newState == GameState.Run)
            SetRunPhase(RunPhase.Exploration);
        else if (newState == GameState.Death)
            SetRunPhase(RunPhase.Death);
    }

    public void SetRunPhase(RunPhase newPhase)
    {
        if (_currentRunPhase == newPhase)
            return;

        RunPhase oldPhase = _currentRunPhase;
        _currentRunPhase = newPhase;

        GD.Print($"[GameManager] RunPhase {oldPhase} → {newPhase}");
        _eventBus.EmitSignal(EventBus.SignalName.RunPhaseChanged, oldPhase.ToString(), newPhase.ToString());
    }
}
