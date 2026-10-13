#if TOOLS
using System;
using Godot;
using Vestiges.Core;
using Vestiges.Progression;

namespace Vestiges.Infrastructure;

/// <summary>
/// Trace de survie des parties jouées dans un build d'édition (plan 42 M0e), mode dev ou non, jamais dans les bancs :
/// à la fin de la run, <c>traces/&lt;date&gt;-&lt;personnage&gt;.csv</c> et son résumé <c>.txt</c> dans le profil. Sert à vérifier
/// que le profil « fort » du bot de mesure ressemble à un vrai joueur (<c>tools/compare_play_traces.py</c>). Absent de
/// l'export.
/// </summary>
public partial class PlayTraceRecorder : Node
{
    private PlayTrace _trace;
    private Player _player;
    private PlayerProgression _progression;
    private GameManager _gameManager;

    public void Setup(Player player) => _player = player;

    public override void _Ready()
    {
        _gameManager = GetNode<GameManager>("/root/GameManager");
        _progression = _player.GetNode<PlayerProgression>("PlayerProgression");
        _trace = new PlayTrace(GetNode<EventBus>("/root/EventBus"), _player);
    }

    public override void _Process(double delta)
    {
        if (_gameManager.CurrentState == GameManager.GameState.Run)
            _trace.Advance(delta);
    }

    public override void _ExitTree()
    {
        if (_trace == null)
            return;
        _trace.Dispose();
        // Une run abandonnée dans les premières secondes n'apprend rien. Le joueur, ajouté avant, sort de l'arbre après.
        if (_trace.Seconds >= 30.0 && IsInstanceValid(_player))
        {
            string stem = DevelopmentMode.GetSavePath(
                $"traces/{DateTime.Now:yyyyMMdd-HHmmss}-{_gameManager.SelectedCharacterId}");
            int level = _progression.CurrentLevel;
            Write($"{stem}.csv", _trace.ToCsv());
            Write($"{stem}.txt", _trace.Summary(_gameManager.SelectedCharacterId, _gameManager.EffectiveSeed, level) + "\n");
            GD.Print($"[PlayTraceRecorder] Trace écrite : {ProjectSettings.GlobalizePath(stem)}.csv");
        }
        _trace = null;
    }

    private static void Write(string path, string text)
    {
        using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        if (file == null)
            GD.PushWarning($"[PlayTraceRecorder] Trace non écrite : {path} ({FileAccess.GetOpenError()})");
        else
            file.StoreString(text);
    }
}
#endif
