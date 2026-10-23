using Godot;

namespace Vestiges.Core;

/// <summary>
/// Horloge unique de la run (plan 51, lot P0) : temps de jeu depuis le début de la run, pauses exclues, qui n'avance
/// que dans l'état Run et s'arrête donc à la mort. Apparitions, Résurgences, micro-événements, fin de partie,
/// Effacement, bilan et quêtes la lisent au lieu de compter chacun leur temps ; les terres successives s'y appuieront.
/// Traitée avant eux à chaque image, pour qu'ils lisent tous la même date.
/// Elle porte deux dates : le temps joué (chrono, bilan, quêtes, Effacement) et le temps de difficulté (foule, Péril
/// du temps, Résurgences, micro-événements), qui s'arrête pendant le combat d'un gardien (plan 56 B1, l'arène fermée).
/// </summary>
public partial class RunClock : Node
{
    // Sommes en double : sur une run d'une heure à 60 Hz, une somme en float dériverait au centième de seconde.
    private double _seconds;
    private double _difficultySeconds;
    private GameManager _gameManager;
    private EventBus _eventBus;

    public float Seconds => (float)_seconds;

    /// <summary>Temps qui fait monter la difficulté : le temps joué, combats de gardien exclus.</summary>
    public float DifficultySeconds => (float)_difficultySeconds;

    /// <summary>Arène d'un gardien fermée : le temps de difficulté est arrêté.</summary>
    public bool DifficultyHeld { get; private set; }

    public override void _EnterTree()
    {
        ProcessPriority = -100;
    }

    public override void _Ready()
    {
        _gameManager = GetNodeOrNull<GameManager>("/root/GameManager");
        _eventBus = GetNodeOrNull<EventBus>("/root/EventBus");
        if (_eventBus != null)
            _eventBus.GuardianArenaChanged += OnGuardianArenaChanged;
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
            _eventBus.GuardianArenaChanged -= OnGuardianArenaChanged;
    }

    public override void _Process(double delta)
    {
        if (_gameManager?.CurrentState == GameManager.GameState.Run)
            Advance(delta);
    }

    /// <summary>Avance l'horloge ; appelé par <see cref="_Process"/>, et par les bancs qui pilotent un système seul.</summary>
    internal void Advance(double delta)
    {
        if (delta <= 0.0)
            return;
        _seconds += delta;
        if (!DifficultyHeld)
            _difficultySeconds += delta;
    }

    private void OnGuardianArenaChanged(bool closed) => DifficultyHeld = closed;
}
