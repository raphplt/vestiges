using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Tests;

/// <summary>
/// Pilotage de la musique (plan 15, A1) : l'annonce garde la main jusqu'au début de la crise, quelle que soit la
/// cadence d'images et la présence ennemie ; un écran en pause ne consomme ni annonce ni combat ; fin de crise vers
/// l'accalmie puis l'exploration ; crises tardives et endgame ; mort, Hub et seconde run ; combat par présence
/// réelle (un retrait au pool compte). Première run d'une session : exploration et ambiance sans changement de phase.
/// </summary>
public partial class MusicRegression : Node2D
{
    private static readonly PackedScene EnemyScene = GD.Load<PackedScene>("res://scenes/enemies/Enemy.tscn");
    private readonly List<Enemy> _enemies = new();
    private GameManager _manager;
    private EventBus _bus;
    private AudioManager _audio;
    private MusicDirector _music;
    private int _failures;

    public override async void _Ready()
    {
        try
        {
            _manager = GetNode<GameManager>("/root/GameManager");
            _bus = GetNode<EventBus>("/root/EventBus");
            _audio = AudioManager.Instance;
            _music = _audio.GetNode<MusicDirector>("Music");
            MusicConfig config = MusicConfig.Load();
            Node2D player = new() { Name = "Player" };
            player.AddToGroup("player");
            AddChild(player);

            _audio.RefreshMusic();
            await Frames(2);
            Check(_music.CurrentIntent == MusicIntent.Hub && _music.CurrentKey == "mus_hub", "Hub au démarrage : mus_hub");

            // Première run : la phase vaut déjà Exploration, aucun RunPhaseChanged n'est émis.
            _manager.ChangeState(GameManager.GameState.Run);
            await Frames(2);
            Check(_music.CurrentIntent == MusicIntent.Exploration && _music.CurrentKey == "mus_jour_exploration",
                $"Première run : exploration sans changement de phase ({_music.CurrentKey})");
            Check(AmbiancePhase() == "Exploration", $"Première run : ambiance d'exploration ({AmbiancePhase()})");

            // Combat par présence réelle, entrée tenue puis sortie après un retrait au pool.
            SpawnEnemies(config.CombatEnterEnemies + 2, 150f);
            await Simulate(config.CombatEnterHoldSeconds + 2f * config.CombatSampleSeconds, 1f / 60f);
            Check(_music.CurrentIntent == MusicIntent.Combat, $"Combat : {_enemies.Count} créatures proches ({_music.CurrentIntent})");
            foreach (Enemy enemy in _enemies)
                enemy.Reset();
            await Simulate(config.CombatExitHoldSeconds - 2f * config.CombatSampleSeconds, 1f / 60f);
            Check(_music.CurrentIntent == MusicIntent.Combat, "Combat tenu avant la fin du délai de sortie");
            await Simulate(4f * config.CombatSampleSeconds, 1f / 60f);
            Check(_music.CurrentIntent == MusicIntent.Exploration, "Créatures retirées au pool : retour à l'exploration");

            // Annonce : rien ne la remplace avant le début réel, à 30, 60 ou 144 images/s, même en foule.
            foreach (Enemy enemy in _enemies)
                enemy.Initialize(EnemyDataLoader.Get("rodeur"), 1f, 1f);
            _bus.EmitSignal(EventBus.SignalName.CrisisWarning, 1, 20f);
            await Frames(2);
            foreach (float fps in new[] { 30f, 60f, 144f })
                await Simulate(6f, 1f / fps);
            Check(_music.CurrentIntent == MusicIntent.Warning && _music.CurrentKey == "mus_crepuscule",
                $"Annonce tenue 18 s à 30/60/144 i/s, créatures proches ({_music.CurrentKey})");

            // Pause pendant l'annonce : les images passent, l'annonce reste.
            GetTree().Paused = true;
            await Frames(240);
            Check(_music.CurrentIntent == MusicIntent.Warning, "Pause pendant l'annonce : l'annonce reste");
            GetTree().Paused = false;

            // Début : phase et signal dans la même image, une seule intention.
            _manager.ReportCrisis(true);
            _bus.EmitSignal(EventBus.SignalName.CrisisStarted, 1, 1);
            await Frames(2);
            Check(_music.CurrentIntent == MusicIntent.Resurgence && _music.CurrentKey == "mus_nuit_vagues", "Début de crise : mus_nuit_vagues");
            GetTree().Paused = true;
            await Frames(120);
            GetTree().Paused = false;
            Check(_music.CurrentIntent == MusicIntent.Resurgence, "Pause pendant la crise : la crise reste");

            // Fin : CrisisEnded (et l'accalmie qu'il ouvre) précède le retour de phase, dans la même image.
            EndCrisis(1);
            await Frames(2);
            await Simulate(3f, 1f / 60f);
            Check(_music.CurrentIntent == MusicIntent.Calm && _music.CurrentKey == "mus_jour_exploration",
                $"Accalmie : exploration malgré la foule ({_music.CurrentIntent})");
            _bus.EmitSignal(EventBus.SignalName.CrisisCalmChanged, false);
            await Frames(2);
            await Simulate(config.CombatEnterHoldSeconds + 2f * config.CombatSampleSeconds, 1f / 60f);
            Check(_music.CurrentIntent == MusicIntent.Combat, "Fin de l'accalmie : le combat revient");
            foreach (Enemy enemy in _enemies)
                enemy.Reset();
            await Simulate(config.CombatExitHoldSeconds + 2f * config.CombatSampleSeconds, 1f / 60f);

            // Seconde crise.
            _bus.EmitSignal(EventBus.SignalName.CrisisWarning, 2, 20f);
            await Frames(2);
            Check(_music.CurrentIntent == MusicIntent.Warning, "Seconde annonce");
            _manager.ReportCrisis(true);
            _bus.EmitSignal(EventBus.SignalName.CrisisStarted, 2, 1);
            await Frames(2);
            EndCrisis(2);
            await Frames(2);
            Check(_music.CurrentIntent == MusicIntent.Calm, "Seconde crise terminée : accalmie");
            _bus.EmitSignal(EventBus.SignalName.CrisisCalmChanged, false);

            // Fin de partie avancée : l'annonce et la crise gardent leur identité, puis retour au fond tardif.
            _manager.ReportLateGame();
            await Frames(2);
            Check(_music.CurrentIntent == MusicIntent.LateGame && _music.CurrentKey == "mus_nuit_chaos", "LateGame : mus_nuit_chaos");
            _bus.EmitSignal(EventBus.SignalName.CrisisWarning, 3, 20f);
            await Frames(2);
            Check(_music.CurrentIntent == MusicIntent.Warning, "Annonce en LateGame");
            _manager.ReportCrisis(true);
            _bus.EmitSignal(EventBus.SignalName.CrisisStarted, 3, 2);
            await Frames(2);
            EndCrisis(3);
            await Frames(2);
            Check(_music.CurrentIntent == MusicIntent.LateGame, "Crise tardive terminée : retour à mus_nuit_chaos, pas d'accalmie d'exploration");
            _bus.EmitSignal(EventBus.SignalName.CrisisCalmChanged, false);

            // Endgame : la phase ne change pas pendant la crise, la musique en sort quand même.
            _manager.ReportEndgame();
            _manager.ReportCrisis(true);
            _bus.EmitSignal(EventBus.SignalName.CrisisStarted, 4, 2);
            await Frames(2);
            Check(_music.CurrentIntent == MusicIntent.Resurgence, "Crise en endgame");
            EndCrisis(4);
            await Frames(2);
            Check(_music.CurrentIntent == MusicIntent.Endgame && _music.CurrentKey == "mus_nuit_chaos",
                $"Crise d'endgame terminée : la musique en sort ({_music.CurrentKey})");
            _bus.EmitSignal(EventBus.SignalName.CrisisCalmChanged, false);

            // Mort pendant une annonce, Hub, seconde run.
            _bus.EmitSignal(EventBus.SignalName.CrisisWarning, 5, 20f);
            await Frames(2);
            _manager.ChangeState(GameManager.GameState.Death);
            await Frames(2);
            Check(_music.CurrentIntent == MusicIntent.Death && _music.CurrentKey == "mus_mort", "Mort pendant l'annonce : mus_mort");
            _manager.ChangeState(GameManager.GameState.Hub);
            await Frames(2);
            Check(_music.CurrentIntent == MusicIntent.Hub, "Retour au Hub");
            _manager.ChangeState(GameManager.GameState.Run);
            await Frames(2);
            Check(_music.CurrentIntent == MusicIntent.Exploration && AmbiancePhase() == "Exploration",
                $"Seconde run : exploration, annonce oubliée ({_music.CurrentIntent}, ambiance {AmbiancePhase()})");

            GD.Print($"[MusicRegression] RESULT failures={_failures}");
            GetTree().Quit(_failures == 0 ? 0 : 1);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(2);
        }
    }

    /// <summary>Ordre de CrisisManager.EndCrisis : fin, accalmie ouverte par CrisisAftermath, puis fin de crise signalée,
    /// d'où GameManager déduit la nouvelle phase (plan 26 Q8d).</summary>
    private void EndCrisis(int number)
    {
        _bus.EmitSignal(EventBus.SignalName.CrisisEnded, number);
        _bus.EmitSignal(EventBus.SignalName.CrisisCalmChanged, true);
        _manager.ReportCrisis(false);
    }

    private string AmbiancePhase() =>
        (string)typeof(AudioManager).GetField("_currentPhase", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_audio);

    private void SpawnEnemies(int count, float distance)
    {
        for (int i = 0; i < count; i++)
        {
            Enemy enemy = EnemyScene.Instantiate<Enemy>();
            AddChild(enemy);
            enemy.Initialize(EnemyDataLoader.Get("rodeur"), 1f, 1f);
            enemy.SetPhysicsProcess(false);
            enemy.GlobalPosition = Vector2.FromAngle(Mathf.Tau * i / count) * distance;
            _enemies.Add(enemy);
        }
    }

    /// <summary>Fait avancer le pilotage à une cadence donnée, en résolvant les intentions demandées à chaque pas.</summary>
    private async Task Simulate(float seconds, float step)
    {
        for (float t = 0f; t < seconds; t += step)
        {
            _music._Process(step);
            if ((int)(t / step) % 30 == 0)
                await Frames(1);
        }
        await Frames(2);
    }

    private async Task Frames(int count)
    {
        for (int i = 0; i < count; i++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private void Check(bool ok, string label)
    {
        if (!ok)
            _failures++;
        GD.Print($"[MusicRegression] {(ok ? "PASS" : "FAIL")} {label}");
    }
}
