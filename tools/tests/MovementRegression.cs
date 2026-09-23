using System;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Tests;

/// <summary>Banc isolé : vraies entrées Godot, Player et collisions, sans génération de monde.</summary>
public partial class MovementRegression : Node2D
{
    private Player _player;
    private AnimatedSprite2D _sprite;
    private GameManager _manager;
    private int _failures;
    private const int Frames = 120;
    private const int TestDevice = 31;

    public override async void _Ready()
    {
        try
        {
            Input.UseAccumulatedInput = false;
            IsolateInputs();
            _manager = GetNode<GameManager>("/root/GameManager");
            if (Array.IndexOf(OS.GetCmdlineUserArgs(), "--run-integration") >= 0)
                await RunWorldIntegration();
            else
            {
                _manager.ChangeState(GameManager.GameState.Run);
                _player = GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate<Player>();
                AddChild(_player);
                _player.InitializeCharacter(CharacterDataLoader.Get("traqueur"));
                // Un seul appel du vrai contrôleur par tick physique, piloté par le banc.
                _player.SetPhysicsProcess(false);
                _sprite = _player.GetNode<AnimatedSprite2D>("Sprite");
                await RunChecks();
            }
            GD.Print($"[MovementRegression] RESULT failures={_failures}");
            GetTree().Quit(_failures == 0 ? 0 : 1);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(2);
        }
    }

    private async Task RunChecks()
    {
        Vector2[] directions = { Vector2.Right, Vector2.Down, Vector2.Left, Vector2.Up,
            new(1, 1), new(-1, 1), new(-1, -1), new(1, -1) };
        float fullDistance = _player.Speed * Frames / Engine.PhysicsTicksPerSecond;
        foreach (Vector2 direction in directions)
        {
            SetActions(direction);
            Vector2 displacement = await Measure();
            Vector2 expected = direction.Normalized() * fullDistance;
            Check(displacement.DistanceTo(expected) < fullDistance * 0.02f,
                $"axes {direction}: displacement={displacement}, expected={expected}, animation={_sprite.Animation}");
        }

        SetActions(Vector2.Zero);
        float deadzone = InputMap.ActionGetDeadzone("move_right");
        foreach (float amplitude in new[] { 0.25f, 0.5f, 1f })
        {
            // Amplitude utile demandée APRÈS la zone morte circulaire de GetVector.
            float rawStrength = deadzone + amplitude * (1f - deadzone);
            Input.ParseInputEvent(new InputEventJoypadMotion { Device = TestDevice, Axis = JoyAxis.LeftX, AxisValue = rawStrength });
            Vector2 displacement = await Measure();
            Check(displacement.DistanceTo(Vector2.Right * fullDistance * amplitude) < fullDistance * 0.02f,
                $"stick useful={amplitude:F2}, raw={rawStrength:F2}: displacement={displacement}, cadence={_sprite.SpeedScale:F3}");
            Check(Math.Abs(_sprite.SpeedScale - amplitude) < 0.01f, $"cadence de marche suit stick {amplitude:F2}");
        }
        Input.ParseInputEvent(new InputEventJoypadMotion { Device = TestDevice, Axis = JoyAxis.LeftX, AxisValue = deadzone * 0.5f });
        Check((await Measure(2)).Length() < 0.01f, "stick dans la zone morte : immobile");
        Input.ParseInputEvent(new InputEventJoypadMotion { Device = TestDevice, Axis = JoyAxis.LeftX, AxisValue = 0f });

        InputRemapManager.Instance.RemapKey("move_right", Key.L);
        IsolateInputs();
        Input.ParseInputEvent(new InputEventKey { Device = TestDevice, PhysicalKeycode = Key.L, Pressed = true });
        Check((await Measure()).DistanceTo(Vector2.Right * fullDistance) < 0.1f, "touche remappée L → droite");
        Input.ParseInputEvent(new InputEventKey { Device = TestDevice, PhysicalKeycode = Key.L, Pressed = false });
        Check((await Measure(2)).Length() < 0.01f, "relâchement immédiat");
        Input.ActionPress("move_left");
        Input.ActionPress("move_right");
        Check((await Measure(2)).Length() < 0.01f, "touches opposées : immobile");

        SetActions(Vector2.Right);
        _player.ApplySlow(0.5f, 10f);
        Check((await Measure()).DistanceTo(Vector2.Right * fullDistance * 0.5f) < 0.1f, "ralentissement ×0,5 préservé");
        _player.ApplySlow(1f, 0f);
        _player.ApplySpeedMultiplier(1.25f);
        Check((await Measure()).DistanceTo(Vector2.Right * fullDistance * 1.25f) < 0.1f, "bonus vitesse ×1,25 préservé");
        _player.ApplySpeedMultiplier(0.8f);

        SetActions(Vector2.Zero);
        _player.IsAIControlled = true;
        _player.AIInputOverride = new Vector2(3f, 4f);
        Check((await Measure()).DistanceTo(new Vector2(0.6f, 0.8f) * fullDistance) < 0.1f, "IA : repère écran et amplitude bornée");
        _manager.ChangeState(GameManager.GameState.Hub);
        Check((await Measure(2)).Length() < 0.01f, "IA bloquée hors run");
        _player.IsAIControlled = false;
        SetActions(Vector2.Right);
        Check((await Measure(2)).Length() < 0.01f, "clavier bloqué hors run");
        _manager.ChangeState(GameManager.GameState.Run);

        // Ici le moteur pilote réellement Player : une pause doit suspendre ses ticks.
        _player.ProcessMode = ProcessModeEnum.Pausable;
        _player.SetPhysicsProcess(true);
        GetTree().Paused = true;
        Vector2 pausedPosition = _player.Position;
        for (int frame = 0; frame < 3; frame++)
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        Check(_player.Position == pausedPosition, "pause de l'arbre : aucun déplacement");
        GetTree().Paused = false;
        _player.SetPhysicsProcess(false);

        SetActions(Vector2.Left);
        await Measure(2);
        StringName facing = _sprite.Animation;
        // L'attaque automatique peut viser ailleurs ; elle ne pilote pas la pose de marche.
        typeof(Player).GetMethod("PlayAttackFeedback", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(_player, new object[] { true, Vector2.Right });
        await Measure(2);
        Check(_sprite.Animation == facing, "attaque à droite pendant fuite à gauche : pose conservée");

        _player.IsAIControlled = true;
        _player.AIInputOverride = new Vector2(1f, -1f);
        await Measure(2);
        facing = _sprite.Animation;
        foreach (float drift in new[] { 0.01f, -0.01f, 0f, 0.015f, -0.015f })
        {
            _player.AIInputOverride = new Vector2(1f, drift);
            await Measure(2);
            Check(_sprite.Animation == facing, $"orientation stable autour axe horizontal ({drift})");
        }
        _player.IsAIControlled = false;

        StaticBody2D wall = new() { Position = new Vector2(60, 0), CollisionLayer = 4 };
        wall.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(20, 4000) } });
        AddChild(wall);
        _player.Position = Vector2.Zero;
        SetActions(Vector2.Right);
        await Step(Frames);
        Check(_player.Position.X < 39f && Math.Abs(_player.Position.Y) < 0.1f, $"mur respecté : {_player.Position}");
        Check(_sprite.Animation.ToString().EndsWith("_idle"), $"mur : animation au repos ({_sprite.Animation})");
        Check((float)typeof(Player).GetField("_footstepTimer", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(_player) == 0f, "mur : cadence de pas arrêtée");
        SetActions(new Vector2(1, 1));
        Vector2 slideStart = _player.Position;
        await Step(30);
        Check(_player.Position.Y > slideStart.Y + 20f && _player.Position.X < 39f, "glissement contre mur préservé");
        wall.QueueFree();

        await Step(1);
        await RunMobilityChecks();
        _player.TakeDamage(10000f);
        Check(_player.Mobility.State == MobilityState.Death && _player.Mobility.BufferRemaining == 0f,
            "mort en dash : état Death et buffer vide");
        Check((await Measure(2)).Length() < 0.01f && _player.Velocity == Vector2.Zero, "mort : mouvement arrêté");
        Check(_sprite.Animation.ToString().EndsWith("_death") && _sprite.SpeedScale == 1f, "mort : animation à cadence normale");
    }

    private static void SetActions(Vector2 direction)
    {
        foreach (string action in new[] { "move_left", "move_right", "move_up", "move_down" })
            Input.ActionRelease(action);
        if (direction.X < 0) Input.ActionPress("move_left", -direction.X);
        if (direction.X > 0) Input.ActionPress("move_right", direction.X);
        if (direction.Y < 0) Input.ActionPress("move_up", -direction.Y);
        if (direction.Y > 0) Input.ActionPress("move_down", direction.Y);
    }

    private static void IsolateInputs()
    {
        // Ignorer les périphériques branchés sur la machine qui exécute ce banc.
        foreach (string action in new[] { "move_left", "move_right", "move_up", "move_down", "mobility" })
        {
            Godot.Collections.Array<InputEvent> bindings = InputMap.ActionGetEvents(action);
            InputMap.ActionEraseEvents(action);
            foreach (InputEvent binding in bindings)
            {
                binding.Device = TestDevice;
                InputMap.ActionAddEvent(action, binding);
            }
            Input.ActionRelease(action);
        }
    }

    private async Task<Vector2> Measure(int frames = Frames)
    {
        _player.Position = Vector2.Zero;
        await Step(frames);
        return _player.Position;
    }

    private async Task Step(int frames)
    {
        for (int frame = 0; frame < frames; frame++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            _player._PhysicsProcess(1.0 / Engine.PhysicsTicksPerSecond);
        }
    }

    private void Check(bool passed, string message)
    {
        GD.Print($"[MovementRegression] {(passed ? "PASS" : "FAIL")} {message}");
        if (!passed) _failures++;
    }
}
