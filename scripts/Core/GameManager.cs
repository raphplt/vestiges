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
    // Faits de la run d'où se déduit la phase (plan 26 Q8d) ; le late game et l'endgame ne reviennent jamais en arrière.
    private bool _crisisActive;
    private bool _lateGameReached;
    private bool _endgameReached;
    private EventBus _eventBus;

    /// <summary>Personnage sélectionné dans le Hub. Persiste entre scènes.</summary>
    public string SelectedCharacterId { get; set; }

    /// <summary>Données de la dernière run terminée (pour affichage dans le Hub).</summary>
    public RunRecord LastRunData { get; set; }

    /// <summary>Seed de la run. 0 = aléatoire au lancement.</summary>
    public ulong RunSeed { get; set; }

    /// <summary>Seed réellement utilisée par la run en cours : celle du Hub, ou celle tirée au lancement (plan 26 Q8c).</summary>
    public ulong EffectiveSeed { get; set; }

    /// <summary>Vestiges gagnés lors de la dernière run.</summary>
    public int LastVestigesEarned { get; set; }

    /// <summary>Quêtes de déblocage accomplies pendant la dernière run (ids), montrées au bilan.</summary>
    public List<string> LastQuestCompletions { get; set; }

    /// <summary>Vide si la dernière fin de run est enregistrée ; sinon la raison, montrée au bilan.</summary>
    public string LastRunSaveError { get; set; } = "";

    /// <summary>Acquis enregistrés mais relevé pas encore inscrit dans l'historique (complété au camp).</summary>
    public bool LastRunHistoryPending { get; set; }

    /// <summary>Arme à montrer dans la Collection à l'arrivée au camp (bilan : « Voir dans la Collection »), ou nul.</summary>
    public string CollectionFocusWeaponId { get; set; }

    /// <summary>Mutateurs actifs pour la prochaine run (sélectionnés dans le Hub).</summary>
    public List<string> ActiveMutators { get; set; } = new();

    public GameState CurrentState => _currentState;
    public RunPhase CurrentRunPhase => _currentRunPhase;

    public override void _Ready()
    {
        _eventBus = GetNode<EventBus>("/root/EventBus");
        // La fermeture de la fenêtre passe par la sortie propre, comme le bouton Quitter.
        GetTree().AutoAcceptQuit = false;
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
            _ = GameExit.QuitAsync(GetTree());
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
        {
            _crisisActive = false;
            _lateGameReached = false;
            _endgameReached = false;
            ApplyRunPhase();
        }
        else if (newState == GameState.Death)
        {
            ApplyRunPhase();
        }
    }

    /// <summary>Le late game a-t-il été atteint pendant cette run (seuil d'Effacement, Résurgences ou boss) ?</summary>
    public bool LateGameReached => _lateGameReached;

    /// <summary>Une Résurgence commence ou finit : la phase change avant le signal de début, comme avant.</summary>
    public void ReportCrisis(bool active)
    {
        _crisisActive = active;
        ApplyRunPhase();
    }

    /// <summary>Le late game est atteint : il le reste jusqu'à la fin de la run, crises comprises.</summary>
    public void ReportLateGame()
    {
        _lateGameReached = true;
        ApplyRunPhase();
    }

    /// <summary>Le boss est vaincu : l'endgame commence et ne s'arrête plus.</summary>
    public void ReportEndgame()
    {
        _endgameReached = true;
        _lateGameReached = true;
        ApplyRunPhase();
    }

    /// <summary>Seul endroit qui décide de la phase : mort, endgame, Résurgence, late game, puis exploration.</summary>
    private void ApplyRunPhase()
    {
        RunPhase phase = _currentState == GameState.Death ? RunPhase.Death
            : _endgameReached ? RunPhase.Endgame
            : _crisisActive ? RunPhase.Crisis
            : _lateGameReached ? RunPhase.LateGame
            : RunPhase.Exploration;
        SetRunPhase(phase);
    }

    private void SetRunPhase(RunPhase newPhase)
    {
        if (_currentRunPhase == newPhase)
            return;

        RunPhase oldPhase = _currentRunPhase;
        _currentRunPhase = newPhase;

        GD.Print($"[GameManager] RunPhase {oldPhase} → {newPhase}");
        _eventBus.EmitSignal(EventBus.SignalName.RunPhaseChanged, oldPhase.ToString(), newPhase.ToString());
    }
}
