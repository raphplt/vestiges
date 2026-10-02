using Godot;
using System;
using Vestiges.Core;
using Vestiges.Combat;
using Vestiges.Progression;
using Vestiges.Spawn;
using Vestiges.Infrastructure;

namespace Vestiges.UI;

public partial class DebugActionPanel : CanvasLayer
{
    private PanelContainer _panel;
    private VBoxContainer _vbox;
    private bool _visible;
    private Button _godModeButton;
    private Button _teleportButton;

    private EventBus _eventBus;
    private Player _player;
    private SpawnManager _spawnManager;

    private bool _teleportActive;

    public override void _Ready()
    {
        if (!DevelopmentMode.IsEnabled)
        {
            ProcessMode = ProcessModeEnum.Disabled;
            QueueFree();
            return;
        }
        ProcessMode = ProcessModeEnum.Always;
        Layer = 101;

        _eventBus = GetNode<EventBus>("/root/EventBus");

        BuildUI();
        _panel.Visible = false;
        _visible = false;
    }

    public override void _Process(double delta)
    {
        if (!DevelopmentMode.IsEnabled)
            return;
        if (_player == null || !IsInstanceValid(_player))
            _player = GetTree().GetFirstNodeInGroup("player") as Player;

        if (_spawnManager == null || !IsInstanceValid(_spawnManager))
            _spawnManager = GetTree().CurrentScene?.GetNodeOrNull<SpawnManager>("SpawnManager");

        if (_visible && _player != null)
            _godModeButton.Text = $"God Mode: {(_player.IsGodMode ? "ON" : "OFF")}";
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!DevelopmentMode.IsEnabled)
            return;
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo && keyEvent.Keycode == Key.F4)
        {
            _visible = !_visible;
            _panel.Visible = _visible;
            GetViewport().SetInputAsHandled();
            return;
        }

        if (_teleportActive && @event is InputEventMouseButton mouseBtn && mouseBtn.Pressed && mouseBtn.ButtonIndex == MouseButton.Right)
        {
            if (_player != null && IsInstanceValid(_player))
            {
                _player.GlobalPosition = _panel.GetGlobalMousePosition();
                GetViewport().SetInputAsHandled();
            }
        }
    }

    private void BuildUI()
    {
        _panel = new PanelContainer();
        _panel.AnchorLeft = 0.5f;
        _panel.AnchorTop = 0.5f;
        _panel.AnchorRight = 0.5f;
        _panel.AnchorBottom = 0.5f;
        _panel.GrowHorizontal = Control.GrowDirection.Both;
        _panel.GrowVertical = Control.GrowDirection.Both;

        StyleBoxFlat style = new();
        style.BgColor = new Color(0.1f, 0.1f, 0.2f, 0.85f);
        style.ContentMarginLeft = 16;
        style.ContentMarginRight = 16;
        style.ContentMarginTop = 16;
        style.ContentMarginBottom = 16;
        _panel.AddThemeStyleboxOverride("panel", style);

        _vbox = new VBoxContainer();
        _vbox.AddThemeConstantOverride("separation", 8);
        _panel.AddChild(_vbox);

        Label title = new() { Text = "DEBUG ACTIONS (F4)", HorizontalAlignment = HorizontalAlignment.Center };
        title.AddThemeColorOverride("font_color", new Color(1f, 0.3f, 0.3f));
        _vbox.AddChild(title);

        Button xpBtn = new() { Text = "+ 1000 XP" };
        BindAction(xpBtn, () => _eventBus.EmitSignal(EventBus.SignalName.XpGained, 1000f));
        _vbox.AddChild(xpBtn);

        Button essenceBtn = new() { Text = "+ 100 Essence" };
        BindAction(essenceBtn, () => _eventBus.EmitSignal(EventBus.SignalName.LootReceived, "essence", "debug_essence", 100));
        _vbox.AddChild(essenceBtn);

        _godModeButton = new Button { Text = "God Mode: OFF" };
        BindAction(_godModeButton, () =>
        {
            if (_player != null)
                _player.IsGodMode = !_player.IsGodMode;
        });
        _vbox.AddChild(_godModeButton);

        _teleportButton = new Button { Text = "Teleport (Right Click): OFF" };
        BindAction(_teleportButton, () =>
        {
            _teleportActive = !_teleportActive;
            _teleportButton.Text = $"Teleport (Right Click): {(_teleportActive ? "ON" : "OFF")}";
        });
        _vbox.AddChild(_teleportButton);

        Button healBtn = new() { Text = "Full Heal" };
        BindAction(healBtn, () => _player?.Heal(99999f));
        _vbox.AddChild(healBtn);

        Button spawnEnemyBtn = new() { Text = "Spawn Test Enemy (Mouse)" };
        BindAction(spawnEnemyBtn, () =>
        {
            if (_spawnManager != null)
            {
                Vector2 spawnPos = _panel.GetGlobalMousePosition();
                _spawnManager.ForceSpawnEnemy("shadow_crawler", spawnPos);
            }
        });
        _vbox.AddChild(spawnEnemyBtn);

        Button upgradeWeaponBtn = new() { Text = "Upgrade Equipped Weapon" };
        BindAction(upgradeWeaponBtn, () =>
        {
            if (_player != null && _player.EquippedWeapon != null)
            {
                WeaponInstance weapon = _player.EquippedWeapon;
                _player.UpgradeWeapon(weapon.Id, UpgradeRoller.RollWeaponGains(weapon, UpgradeRoller.Get("rare"), new RandomNumberGenerator()), "rare");
                GD.Print($"[Debug] Upgraded weapon {_player.EquippedWeapon.Id}");
            }
        });
        _vbox.AddChild(upgradeWeaponBtn);

        AddChild(_panel);
    }

    private static void BindAction(Button button, Action action)
    {
        button.Pressed += () =>
        {
            if (DevelopmentMode.IsEnabled)
                action();
        };
    }
}
