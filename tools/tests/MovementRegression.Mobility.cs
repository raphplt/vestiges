using System;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.UI;
using Vestiges.World;

namespace Vestiges.Tests;

public partial class MovementRegression
{
    private async Task RunMobilityChecks()
    {
        CheckMobilityModuleBoundaries();
        await ReadyMobility();
        Check(InputMap.HasAction("mobility"), "mobilité : action déclarée");
        float dashDistance = _player.Speed * 2.4f * 0.15f;
        Vector2[] directions = { Vector2.Right, Vector2.Down, Vector2.Left, Vector2.Up,
            new(1, 1), new(-1, 1), new(-1, -1), new(1, -1) };
        foreach (Vector2 direction in directions)
        {
            await ReadyMobility();
            SetActions(direction);
            _player.Position = Vector2.Zero;
            PressMobility();
            await Step(1);
            ReleaseMobility();
            await Step(8);
            Check(_player.Position.DistanceTo(direction.Normalized() * dashDistance) < 0.1f,
                $"dash clavier {direction} : déplacement={_player.Position}, distance={_player.Position.Length():F3}");
        }

        float deadzone = InputMap.ActionGetDeadzone("move_right");
        Vector2 stickDirection = new Vector2(0.8f, -0.6f);
        foreach (float amplitude in new[] { 0.25f, 0.5f, 1f })
        {
            await ReadyMobility();
            Vector2 raw = stickDirection * (deadzone + amplitude * (1f - deadzone));
            SetStick(raw);
            _player.Position = Vector2.Zero;
            PressMobility(true);
            await Step(1);
            ReleaseMobility(true);
            await Step(8);
            Check(_player.Position.DistanceTo(stickDirection * dashDistance) < 0.1f,
                $"dash manette angle intermédiaire amplitude={amplitude:F2} : {_player.Position}, distance={_player.Position.Length():F3}");
        }

        await ReadyMobility();
        SetActions(Vector2.Up);
        await Step(1);
        SetActions(Vector2.Zero);
        _player.Position = Vector2.Zero;
        PressMobility();
        await Step(1);
        ReleaseMobility();
        await Step(8);
        Check(_player.Position.DistanceTo(Vector2.Up * dashDistance) < 0.1f, "dash au neutre : dernière direction de déplacement");
        Check(_sprite.Animation.ToString().EndsWith("_dash"), $"pose de dash disponible : {_sprite.Animation}");

        await ReadyMobility();
        _player.ApplySlow(0.5f, 10f);
        SetActions(Vector2.Right);
        _player.Position = Vector2.Zero;
        PressMobility();
        await Step(1);
        ReleaseMobility();
        await Step(8);
        Check(Math.Abs(_player.Position.X - dashDistance * 0.5f) < 0.1f,
            $"dash et ralentissement ×0,5 : distance={_player.Position.Length():F3}");
        _player.ApplySlow(1f, 0f);

        await CheckMobilityBindings();
        await CheckMobilityTiming();
        await CheckMobilityPause();
        await CheckMobilityInteractions();
        await CheckMobilityCollisions();
        await CheckMobilityErasure();
        await CheckMobilityDamageAndResponse();
        await ReadyMobility();
        PressMobility();
        await Step(1);
        ReleaseMobility();
        Check(_player.Mobility.IsDashing, "dash actif avant test de mort");
    }

    private async Task CheckMobilityBindings()
    {
        await ReadyMobility();
        InputRemapManager.Instance.RemapKey("mobility", Key.K);
        IsolateInputs();
        Input.ParseInputEvent(new InputEventKey { Device = TestDevice, PhysicalKeycode = Key.K, Pressed = true });
        await Step(1);
        Check(_player.Mobility.IsDashing, "mobilité remappée clavier K");
        Input.ParseInputEvent(new InputEventKey { Device = TestDevice, PhysicalKeycode = Key.K, Pressed = false });
        InputRemapManager.Instance.ResetToDefaults();
        IsolateInputs();
        await ReadyMobility();
        InputRemapManager.Instance.RemapJoyButton("mobility", JoyButton.RightShoulder);
        IsolateInputs();
        Input.ParseInputEvent(new InputEventJoypadButton { Device = TestDevice, ButtonIndex = JoyButton.RightShoulder, Pressed = true });
        await Step(1);
        Check(_player.Mobility.IsDashing, "mobilité remappée manette épaule droite");
        Input.ParseInputEvent(new InputEventJoypadButton { Device = TestDevice, ButtonIndex = JoyButton.RightShoulder, Pressed = false });
        InputRemapManager.Instance.ResetToDefaults();
        IsolateInputs();
        using ConfigFile oldBindings = new();
        oldBindings.SetValue("move_right", "key", (long)Key.L);
        Check(oldBindings.Save("user://input_bindings.cfg") == Error.Ok,
            "ancienne configuration sans mobilité créée dans les données temporaires");
        typeof(InputRemapManager).GetMethod("LoadBindings", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(InputRemapManager.Instance, null);
        IsolateInputs();
        await ReadyMobility();
        Input.ParseInputEvent(new InputEventKey { Device = TestDevice, PhysicalKeycode = Key.L, Pressed = true });
        await Step(1);
        Check(_player.Velocity.X > 0f, "ancienne configuration : touche de déplacement conservée");
        Input.ParseInputEvent(new InputEventKey { Device = TestDevice, PhysicalKeycode = Key.L, Pressed = false });
        PressMobility();
        await Step(1);
        ReleaseMobility();
        Check(_player.Mobility.IsDashing, "ancienne configuration sans mobilité : binding Espace disponible");
        InputRemapManager.Instance.ResetToDefaults();
        IsolateInputs();
    }

    private async Task CheckMobilityTiming()
    {
        await ReadyMobility();
        PressMobility();
        await Step(1);
        SetActions(Vector2.Zero);
        await Step(170);
        Input.ParseInputEvent(new InputEventKey { Device = TestDevice, PhysicalKeycode = Key.Space, Pressed = true, Echo = true });
        await Step(1);
        Check(!_player.Mobility.IsDashing && _player.Mobility.CooldownRemaining == 0f,
            "bouton maintenu au-delà recharge : aucun dash répété");
        ReleaseMobility();
        await Step(1);
        PressMobility();
        await Step(1);
        ReleaseMobility();
        int ticks = 1;
        while (_player.Mobility.CooldownRemaining > 0f && ticks < 155)
        {
            await Step(1);
            ticks++;
        }
        Check(ticks >= 150 && ticks <= 151, $"recharge 2,5 s : {ticks} ticks depuis activation à 60 Hz");

        await ReadyMobility();
        PressMobility();
        await Step(1);
        ReleaseMobility();
        while (_player.Mobility.CooldownRemaining > 0.067f)
            await Step(1);
        PressMobility();
        await Step(1);
        ReleaseMobility();
        bool bufferedStarted = _player.Mobility.StartedThisStep;
        for (int tick = 0; tick < 5; tick++)
        {
            await Step(1);
            bufferedStarted |= _player.Mobility.StartedThisStep;
        }
        Check(bufferedStarted, "buffer : appui dans les 80 ms précédant recharge déclenche une fois");

        await ReadyMobility();
        PressMobility();
        await Step(1);
        ReleaseMobility();
        while (_player.Mobility.CooldownRemaining > 0.117f)
            await Step(1);
        PressMobility();
        await Step(1);
        ReleaseMobility();
        bufferedStarted = false;
        for (int tick = 0; tick < 10; tick++)
        {
            await Step(1);
            bufferedStarted |= _player.Mobility.StartedThisStep;
        }
        Check(!bufferedStarted && _player.Mobility.BufferRemaining == 0f,
            "buffer : appui trop tôt expire avant recharge");
    }

    private async Task CheckMobilityPause()
    {
        await ReadyMobility();
        PressMobility();
        await Step(1);
        ReleaseMobility();
        await Step(10);
        PressMobility();
        await Step(1);
        ReleaseMobility();
        Check(_player.Mobility.BufferRemaining > 0f, "entrée tamponnée avant ouverture menu");
        PauseMenu menu = new();
        AddChild(menu);
        InputEventAction cancel = new() { Action = "ui_cancel", Pressed = true };
        menu._UnhandledInput(cancel);
        float cooldown = _player.Mobility.CooldownRemaining;
        Vector2 position = _player.Position;
        Check(GetTree().Paused && menu.IsOpen, "vrai menu pause ouvert");
        Check(_player.Mobility.BufferRemaining == 0f, "ouverture menu : buffer vidé");
        _player.SetPhysicsProcess(true);
        PressMobility();
        for (int tick = 0; tick < 10; tick++)
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        Check(_player.Mobility.CooldownRemaining == cooldown && _player.Position == position,
            "pause moteur : recharge et déplacement figés");
        menu._UnhandledInput(cancel);
        _player.SetPhysicsProcess(false);
        await Step(165);
        Check(!_player.Mobility.IsDashing && _player.Mobility.CooldownRemaining == 0f,
            "retour menu bouton tenu : aucun dash différé");
        ReleaseMobility();
        await Step(1);
        PressMobility();
        await Step(1);
        ReleaseMobility();
        Check(_player.Mobility.IsDashing, "retour menu : nouvel appui après relâchement accepté");
        menu._UnhandledInput(cancel);
        Check(!_player.Mobility.IsDashing && _player.Mobility.BufferRemaining == 0f,
            "menu ouvert en plein dash : action annulée immédiatement");
        menu._UnhandledInput(cancel);
        menu.QueueFree();
        await ReadyMobility();
        _manager.ChangeState(GameManager.GameState.Hub);
        PressMobility();
        await Step(1);
        Check(!_player.Mobility.IsDashing, "Hub : mobilité refusée");
        _manager.ChangeState(GameManager.GameState.Run);
        await Step(1);
        Check(!_player.Mobility.IsDashing, "retour Run avec bouton tenu : aucune mobilité fantôme");
        ReleaseMobility();
    }

    private async Task CheckMobilityInteractions()
    {
        await ReadyMobility();
        Chest chest = GD.Load<PackedScene>("res://scenes/world/Chest.tscn").Instantiate<Chest>();
        chest.Position = new Vector2(0, 35);
        AddChild(chest);
        chest.Initialize(ChestDataLoader.Get("chest_common"));
        _player.Position = Vector2.Zero;
        await Step(1);
        _player._UnhandledInput(new InputEventAction { Action = "interact", Pressed = true });
        Check(ReadPlayerField<bool>("_isOpeningChest"), "vrai coffre : ouverture commencée");
        PressMobility();
        await Step(1);
        ReleaseMobility();
        Check(!ReadPlayerField<bool>("_isOpeningChest") && !chest.IsOpened,
            "dash au neutre : ouverture coffre annulée sans loot");
        chest.QueueFree();

        await ReadyMobility();
        PointOfInterest poi = GD.Load<PackedScene>("res://scenes/world/PointOfInterest.tscn").Instantiate<PointOfInterest>();
        poi.Position = new Vector2(0, 35);
        AddChild(poi);
        poi.Initialize(PoiDataLoader.Get("searchable_building"));
        _player.Position = Vector2.Zero;
        await Step(2);
        _player._UnhandledInput(new InputEventAction { Action = "interact", Pressed = true });
        Check(ReadPlayerField<bool>("_isExploringPoi"), "vrai POI : fouille commencée");
        PressMobility();
        await Step(1);
        ReleaseMobility();
        Check(!ReadPlayerField<bool>("_isExploringPoi") && !poi.IsExplored,
            "dash au neutre : fouille POI annulée sans loot");
        poi.QueueFree();
    }

    private async Task CheckMobilityCollisions()
    {
        await ReadyMobility();
        StaticBody2D wall = new() { Position = new Vector2(60, 0), CollisionLayer = 4 };
        wall.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(20, 4000) } });
        AddChild(wall);
        SetActions(Vector2.Right);
        _player.Position = Vector2.Zero;
        PressMobility();
        await Step(1);
        ReleaseMobility();
        await Step(12);
        Check(_player.Position.X < 39f && Math.Abs(_player.Position.Y) < 0.1f,
            $"dash respecte mur : {_player.Position}");
        Check(_sprite.Animation.ToString().EndsWith("_idle"), "dash bloqué : retour animation idle");
        StaticBody2D ceiling = new() { Position = new Vector2(0, -60), CollisionLayer = 4 };
        ceiling.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(4000, 20) } });
        AddChild(ceiling);
        await ReadyMobility();
        _player.Position = Vector2.Zero;
        SetActions(new Vector2(1, -1));
        PressMobility();
        await Step(1);
        ReleaseMobility();
        await Step(12);
        Check(_player.Position.X < 39f && _player.Position.Y > -39f, $"dash respecte coin : {_player.Position}");
        Vector2 corner = _player.Position;
        SetActions(new Vector2(-1, 1));
        await Step(15);
        Check(_player.Position.DistanceTo(corner) > 40f, "sortie du coin contrôlable après dash");
        wall.QueueFree();
        ceiling.QueueFree();
    }

    private async Task CheckMobilityErasure()
    {
        await ReadyMobility();
        ErasureManager erasure = new();
        AddChild(erasure);
        erasure.SetProcess(false);
        // Région déterministe dans le vrai gestionnaire ; aucun monde procédural n'est généré.
        System.Collections.Generic.Dictionary<Vector2I, ErasureManager.ErasureZonePhase> phases =
            (System.Collections.Generic.Dictionary<Vector2I, ErasureManager.ErasureZonePhase>)typeof(ErasureManager)
                .GetField("_zonePhases", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(erasure);
        phases[new Vector2I(1, 0)] = ErasureManager.ErasureZonePhase.Void;
        FieldInfo managerField = typeof(Player).GetField("_erasureManager", BindingFlags.Instance | BindingFlags.NonPublic);
        _player.Position = new Vector2(erasure.CellSize - 25f, 20f);
        SetActions(Vector2.Right);
        PressMobility();
        for (int tick = 0; tick < 9; tick++)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            managerField.SetValue(_player, erasure);
            _player._PhysicsProcess(1.0 / Engine.PhysicsTicksPerSecond);
            if (tick == 0)
                ReleaseMobility();
            if (!_player.Mobility.IsDashing)
                break;
        }
        Check(_player.Position.X < erasure.CellSize && !_player.Mobility.IsDashing,
            $"région Néant synthétique : dash arrêté avant frontière x={erasure.CellSize}, position={_player.Position}");
        SetActions(Vector2.Left);
        Vector2 edge = _player.Position;
        await Step(2);
        Check(_player.Position.X < edge.X, "bord du Néant : retour vers terrain sûr contrôlable");
        managerField.SetValue(_player, null);
        erasure.QueueFree();
    }

    private async Task CheckMobilityDamageAndResponse()
    {
        await ReadyMobility();
        SetActions(Vector2.Right);
        _player.Position = Vector2.Zero;
        await Step(1);
        Check(Math.Abs(_player.Velocity.X - _player.Speed) < 0.1f, "réponse directe : vitesse cible dès premier tick");
        SetActions(Vector2.Zero);
        await Step(1);
        Check(_player.Velocity == Vector2.Zero, "réponse directe : arrêt au premier tick");
        _player.Mobility.Response = MovementResponse.Brief;
        SetActions(Vector2.Right);
        await Step(1);
        float initialSpeed = _player.Velocity.X;
        await Step(3);
        Check(initialSpeed > 0f && initialSpeed < _player.Speed && Math.Abs(_player.Velocity.X - _player.Speed) < 0.1f,
            $"réponse brève : premier tick={initialSpeed:F3} px/s, cible atteinte en 4 ticks (66,7 ms)");
        SetActions(Vector2.Left);
        await Step(1);
        Check(_player.Velocity.X < 0f, "réponse brève : demi-tour volontaire dès premier tick");
        SetActions(Vector2.Zero);
        await Step(3);
        Check(_player.Velocity == Vector2.Zero, "réponse brève : arrêt en 3 ticks (50 ms), aucun drift");
        float deadzone = InputMap.ActionGetDeadzone("move_right");
        foreach (float amplitude in new[] { 0.25f, 0.5f, 1f })
        {
            SetActions(Vector2.Zero);
            SetStick(Vector2.Right * (deadzone + amplitude * (1f - deadzone)));
            await Step(4);
            Check(Math.Abs(_player.Velocity.X - _player.Speed * amplitude) < 0.1f,
                $"réponse brève et stick utile {amplitude:F2} : vitesse={_player.Velocity.X:F3}");
            SetStick(Vector2.Zero);
            await Step(3);
            Check(_player.Velocity == Vector2.Zero, "réponse brève : stick relâché sans drift");
        }
        _player.Mobility.Response = MovementResponse.Direct;

        await ReadyMobility();
        float hp = _player.CurrentHp;
        PressMobility();
        await Step(1);
        ReleaseMobility();
        _player.TakeDamage(5f);
        Check(_player.CurrentHp < hp && !_player.Mobility.IsDashing,
            "dash défaut sans iframe : dégâts reçus et dash interrompu");
        PressMobility();
        await Step(1);
        ReleaseMobility();
        Check(!_player.Mobility.IsDashing, "hurt : mobilité refusée pendant réaction aux dégâts");

        await ReadyMobility();
        _player.Mobility.UseInvulnerabilityTrial = true;
        hp = _player.CurrentHp;
        PressMobility();
        await Step(1);
        ReleaseMobility();
        hp = _player.CurrentHp;
        _player.TakeDamage(5f);
        Check(_player.CurrentHp == hp && _player.Mobility.IsDashing,
            $"variante iframe courte : début du dash protégé, HP={hp:F3}→{_player.CurrentHp:F3}, état={_player.Mobility.State}");
        await Step(4);
        _player.TakeDamage(5f);
        Check(_player.CurrentHp < hp && !_player.Mobility.IsDashing,
            "variante iframe courte : dégâts après 83,3 ms, fenêtre expirée");
        _player.Mobility.UseInvulnerabilityTrial = false;
    }

    private void CheckMobilityModuleBoundaries()
    {
        MobilityConfig config = MobilityConfig.Load();
        foreach (int rate in new[] { 30, 60, 120 })
        {
            PlayerMobility mobility = new(config);
            mobility.Request();
            Vector2 displacement = Vector2.Zero;
            int dashSteps = 0;
            for (int tick = 0; tick < rate; tick++)
            {
                Vector2 velocity = mobility.Step(1f / rate, Vector2.Zero, 240f, 1f, true);
                if (mobility.IsDashStep)
                {
                    displacement += velocity / rate;
                    dashSteps++;
                }
            }
            Check(Math.Abs(displacement.Length() - 86.4f) < 0.01f,
                $"module dash à {rate} Hz : distance={displacement.Length():F3}, ticks actifs={dashSteps}");
        }
        foreach (float remaining in new[] { 0.079f, 0.08f, 0.081f, 0.09f })
        {
            PlayerMobility mobility = new(config);
            mobility.Request();
            mobility.Step(0.01f, Vector2.Zero, 240f, 1f, true);
            mobility.Step(0.15f, Vector2.Zero, 240f, 1f, true);
            mobility.Step(mobility.CooldownRemaining - remaining, Vector2.Zero, 240f, 1f, true);
            mobility.Request();
            mobility.Step(remaining, Vector2.Zero, 240f, 1f, true);
            Check(mobility.StartedThisStep == (remaining <= 0.08f),
                $"frontière buffer : recharge dans {remaining * 1000f:F0} ms, démarrage={mobility.StartedThisStep}");
        }
        PlayerMobility brief = new(config) { Response = MovementResponse.Brief };
        brief.Step(0.1f, Vector2.Right, 240f, 1f, true);
        Check(brief.Step(1f / 60f, Vector2.Right, 240f, 0f, true) == Vector2.Zero,
            "réponse brève : terrain bloquant ×0 arrête la vitesse acquise");
        PlayerMobility invulnerable = new(config) { UseInvulnerabilityTrial = true };
        invulnerable.Request();
        invulnerable.Step(0.001f, Vector2.Zero, 240f, 1f, true);
        invulnerable.Step(0.058f, Vector2.Zero, 240f, 1f, true);
        Check(invulnerable.IsInvulnerable, "iframe courte : protection encore présente à 59 ms");
        invulnerable.Step(0.002f, Vector2.Zero, 240f, 1f, true);
        Check(!invulnerable.IsInvulnerable, "iframe courte : protection terminée à 61 ms");
    }

    private async Task ReadyMobility()
    {
        ReleaseMobility();
        ReleaseMobility(true);
        SetActions(Vector2.Zero);
        SetStick(Vector2.Zero);
        await Step(165);
        _player.Position = Vector2.Zero;
        Check(_player.Mobility.CooldownRemaining == 0f && !_player.Mobility.IsDashing,
            "mobilité disponible avant scénario");
    }

    private static void SetStick(Vector2 raw)
    {
        Input.ParseInputEvent(new InputEventJoypadMotion { Device = TestDevice, Axis = JoyAxis.LeftX, AxisValue = raw.X });
        Input.ParseInputEvent(new InputEventJoypadMotion { Device = TestDevice, Axis = JoyAxis.LeftY, AxisValue = raw.Y });
    }

    private static void PressMobility(bool joypad = false) => SendMobility(true, joypad);
    private static void ReleaseMobility(bool joypad = false) => SendMobility(false, joypad);

    private static void SendMobility(bool pressed, bool joypad)
    {
        if (joypad)
            Input.ParseInputEvent(new InputEventJoypadButton { Device = TestDevice, ButtonIndex = JoyButton.X, Pressed = pressed });
        else
            Input.ParseInputEvent(new InputEventKey { Device = TestDevice, PhysicalKeycode = Key.Space, Pressed = pressed });
    }

    private T ReadPlayerField<T>(string name) => (T)typeof(Player)
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_player);
}
