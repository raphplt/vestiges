using System.Collections.Generic;
using Godot;
using Vestiges.Infrastructure;
using Vestiges.Simulation;
using Vestiges.Simulation.MathSim;

namespace Vestiges.Core;

/// <summary>
/// Gestionnaire global de l'état du jeu.
/// Autoload — pilote les transitions entre états (Hub, Run, Death).
/// Porte l'état qui survit aux changements de scène.
/// F2 = lancer la simulation AI depuis n'importe quelle scène.
/// F3 = lancer la simulation mathématique (instantanée, pas de scène).
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

    /// <summary>Seed de la run. 0 = aléatoire au lancement.</summary>
    public ulong RunSeed { get; set; }

    /// <summary>Vestiges gagnés lors de la dernière run.</summary>
    public int LastVestigesEarned { get; set; }

    /// <summary>Personnages débloqués lors de la dernière run.</summary>
    public List<string> LastUnlocks { get; set; }

    /// <summary>Mutateurs actifs pour la prochaine run (sélectionnés dans le Hub).</summary>
    public List<string> ActiveMutators { get; set; } = new();

    public GameState CurrentState => _currentState;

    public override void _Ready()
    {
        _eventBus = GetNode<EventBus>("/root/EventBus");
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey keyEvent || !keyEvent.Pressed || keyEvent.Echo)
            return;

        if (keyEvent.Keycode == Key.F2)
        {
            GD.Print("[GameManager] F2 — Launching AI simulation batch...");
            BatchRunner.StartBatch("res://data/simulation/default_batch.json");
            GetViewport().SetInputAsHandled();
        }
        else if (keyEvent.Keycode == Key.F3)
        {
            GD.Print("[GameManager] F3 — Launching math simulation batch...");
            MathSimBatchRunner.RunBatch("res://data/simulation/default_batch.json");
            GetViewport().SetInputAsHandled();
        }
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
