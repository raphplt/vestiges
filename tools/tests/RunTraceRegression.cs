using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Godot;
using Vestiges.Core;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// Trace des tirages d'une run (plan 26 Q8c-2) : la vraie Main, une seed (--seed), le joueur immobile et invincible,
/// un pas de temps fixe (--fixed-fps du lanceur). Écrit la suite des apparitions (image, créature, position) et une
/// empreinte. Le lanceur compare deux lancements à même seed (empreintes égales) et un à une autre seed (différente).
/// </summary>
public partial class RunTraceRegression : Node
{
    private const int TracedSpawns = 10;
    private const int MaxFrames = 60 * 90;

    private readonly List<string> _trace = new();
    private Node _enemies;

    public override async void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        try
        {
            string[] args = OS.GetCmdlineUserArgs();
            int index = Array.IndexOf(args, "--seed");
            ulong seed = ulong.Parse(index >= 0 ? args[index + 1] : "221092026", CultureInfo.InvariantCulture);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GameManager manager = GetNode<GameManager>("/root/GameManager");
            manager.SelectedCharacterId = "traqueur";
            manager.RunSeed = seed;
            WorldSetup world = GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<WorldSetup>();
            GetTree().Root.AddChild(world);
            GetTree().CurrentScene = world;
            ulong deadline = Time.GetTicksMsec() + 120000;
            while (!world.IsWorldReady || GetTree().Paused || manager.CurrentState != GameManager.GameState.Run)
            {
                if (Time.GetTicksMsec() > deadline)
                    throw new InvalidOperationException("Main n'a pas terminé son chargement.");
                // Sans rendu, les images s'enchaînent plus vite que le thread de génération : lui laisser du CPU.
                OS.DelayMsec(1);
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }

            Player player = world.GetNode<Player>("Player");
            player.IsGodMode = true;
            player.SetPhysicsProcess(false);
            _enemies = world.GetNode("EnemyContainer");
            ulong start = Engine.GetPhysicsFrames();
            EventBus bus = GetNode<EventBus>("/root/EventBus");
            bus.EnemySpawned += (id, hpScale, _) => Record(start, id, hpScale);
            for (int frame = 0; frame < MaxFrames && _trace.Count < TracedSpawns; frame++)
                await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

            if (_trace.Count < TracedSpawns)
                throw new InvalidOperationException($"{_trace.Count} apparitions en {MaxFrames} images, {TracedSpawns} attendues.");
            string joined = string.Join("\n", _trace);
            for (int i = 0; i < 5; i++)
                GD.Print($"[RunTraceRegression] {_trace[i]}");
            GD.Print($"[RunTraceRegression] RESULT seed={seed} spawns={_trace.Count} hash={Hash(joined)}");
            await GameExit.QuitAsync(GetTree(), 0);
        }
        catch (Exception exception)
        {
            GD.PushError($"[RunTraceRegression] FAIL {exception}");
            GetTree().Quit(1);
        }
    }

    private void Record(ulong start, string id, float hpScale)
    {
        if (_trace.Count >= TracedSpawns || _enemies.GetChildCount() == 0)
            return;
        // L'apparition vient d'entrer dans le conteneur : c'est son dernier enfant.
        Vector2 position = _enemies.GetChild<Node2D>(_enemies.GetChildCount() - 1).GlobalPosition;
        _trace.Add(string.Create(CultureInfo.InvariantCulture,
            $"{Engine.GetPhysicsFrames() - start} {id} {hpScale:0.000} {Mathf.RoundToInt(position.X)},{Mathf.RoundToInt(position.Y)}"));
    }

    private static string Hash(string text)
    {
        byte[] digest = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(digest, 0, 8);
    }
}
