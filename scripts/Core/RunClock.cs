using Godot;

namespace Vestiges.Core;

/// <summary>
/// Horloge unique de la run (plan 51, lot P0) : temps de jeu depuis le début de la run, pauses exclues, qui n'avance
/// que dans l'état Run et s'arrête donc à la mort. Apparitions, Résurgences, micro-événements, fin de partie,
/// Effacement, bilan et quêtes la lisent au lieu de compter chacun leur temps ; les terres successives s'y appuieront.
/// Traitée avant eux à chaque image, pour qu'ils lisent tous la même date.
/// </summary>
public partial class RunClock : Node
{
    // Somme en double : sur une run d'une heure à 60 Hz, une somme en float dériverait au centième de seconde.
    private double _seconds;
    private GameManager _gameManager;

    public float Seconds => (float)_seconds;

    public override void _EnterTree()
    {
        ProcessPriority = -100;
    }

    public override void _Ready()
    {
        _gameManager = GetNodeOrNull<GameManager>("/root/GameManager");
    }

    public override void _Process(double delta)
    {
        if (_gameManager?.CurrentState == GameManager.GameState.Run)
            Advance(delta);
    }

    /// <summary>Avance l'horloge ; appelé par <see cref="_Process"/>, et par les bancs qui pilotent un système seul.</summary>
    internal void Advance(double delta)
    {
        if (delta > 0.0)
            _seconds += delta;
    }
}
