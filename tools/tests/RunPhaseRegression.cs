using System;
using System.Reflection;
using Godot;
using Vestiges.Core;
using Vestiges.Events;

namespace Vestiges.Tests;

/// <summary>
/// Transitions globales (plan 26 Q8d) avec les vrais CrisisManager et EndgameManager, pilotés image par image :
/// quatrième Résurgence sous le seuil d'Effacement puis fin de crise (le late game reste, aussi aux images suivantes),
/// crise pendant le late game, endgame qui survit aux crises, mort, nouvelle run.
/// </summary>
public partial class RunPhaseRegression : Node
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    private int _checks;
    private GameManager _manager;
    private CrisisManager _crises;
    private EndgameManager _endgame;

    public override void _Ready()
    {
        try
        {
            _manager = GetNode<GameManager>("/root/GameManager");
            _manager.ChangeState(GameManager.GameState.Run);
            _crises = new CrisisManager { Name = "CrisisManager" };
            AddChild(_crises);
            _endgame = new EndgameManager { Name = "EndgameManager" };
            AddChild(_endgame);
            // Pilotés à la main : aucune Résurgence ni aucun boss ne part tout seul pendant le banc.
            _crises.SetProcess(false);
            _endgame.SetProcess(false);
            int lateGameCrises = (int)typeof(EndgameManager).GetField("_lateGameCrisisThreshold", Private).GetValue(_endgame);

            Phase(GameManager.RunPhase.Exploration, "début de run : exploration");
            for (int number = 1; number < lateGameCrises; number++)
            {
                Call(_crises, "StartCrisis");
                Phase(GameManager.RunPhase.Crisis, $"Résurgence {number} : crise");
                Call(_crises, "EndCrisis");
                Phase(GameManager.RunPhase.Exploration, $"fin de la Résurgence {number} : exploration");
            }

            Call(_crises, "StartCrisis");
            Check(_manager.LateGameReached, $"Résurgence {lateGameCrises} : late game atteint, Effacement sous le seuil");
            Phase(GameManager.RunPhase.Crisis, $"Résurgence {lateGameCrises} : crise");
            Call(_crises, "EndCrisis");
            Phase(GameManager.RunPhase.LateGame, $"fin de la Résurgence {lateGameCrises} : late game gardé (constat F08)");
            for (int frame = 0; frame < 30; frame++)
                _crises._Process(1.0 / 60.0);
            Phase(GameManager.RunPhase.LateGame, "30 images plus tard : toujours late game");

            Call(_crises, "StartCrisis");
            Phase(GameManager.RunPhase.Crisis, "crise pendant le late game");
            Call(_crises, "EndCrisis");
            Phase(GameManager.RunPhase.LateGame, "fin de cette crise : late game");

            GetNode<EventBus>("/root/EventBus").EmitSignal(EventBus.SignalName.EnemyKilled, "indicible", Vector2.Zero);
            Phase(GameManager.RunPhase.Endgame, "boss vaincu : endgame");
            Call(_crises, "StartCrisis");
            Phase(GameManager.RunPhase.Endgame, "crise d'endgame : la phase reste endgame");
            Call(_crises, "EndCrisis");
            Phase(GameManager.RunPhase.Endgame, "fin de crise d'endgame : endgame");

            _manager.ChangeState(GameManager.GameState.Death);
            _manager.ReportCrisis(true);
            Phase(GameManager.RunPhase.Death, "mort : la phase reste la mort, même si une crise est signalée");

            _manager.ChangeState(GameManager.GameState.Hub);
            _manager.ChangeState(GameManager.GameState.Run);
            Check(!_manager.LateGameReached, "nouvelle run : late game remis à zéro");
            Phase(GameManager.RunPhase.Exploration, "nouvelle run : exploration");

            GD.Print($"[RunPhaseRegression] RESULT failures=0 checks={_checks}");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError($"[RunPhaseRegression] FAIL {exception}");
            GetTree().Quit(1);
        }
    }

    private static void Call(object target, string method) =>
        target.GetType().GetMethod(method, Private).Invoke(target, null);

    private void Phase(GameManager.RunPhase expected, string message) =>
        Check(_manager.CurrentRunPhase == expected, $"{message} ({_manager.CurrentRunPhase})");

    private void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
        _checks++;
        GD.Print($"[RunPhaseRegression] OK {message}");
    }
}
