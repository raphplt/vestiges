using Godot;
using Vestiges.Core;
using Vestiges.World;
using Vestiges.Spawn;

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
    private DayNightCycle _dayNightCycle;
    private SpawnManager _spawnManager;
    
    private bool _teleportActive;
    
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        Layer = 101;
        
        _eventBus = GetNode<EventBus>("/root/EventBus");
        
        BuildUI();
        _panel.Visible = false;
        _visible = false;
    }
    
    public override void _Process(double delta)
    {
        if (_player == null || !IsInstanceValid(_player))
            _player = GetTree().GetFirstNodeInGroup("player") as Player;
            
        if (_dayNightCycle == null || !IsInstanceValid(_dayNightCycle))
            _dayNightCycle = GetTree().CurrentScene?.GetNodeOrNull<DayNightCycle>("DayNightCycle");
            
        if (_spawnManager == null || !IsInstanceValid(_spawnManager))
        {
            _spawnManager = GetTree().CurrentScene?.GetNodeOrNull<SpawnManager>("SpawnManager");
        }
            
        if (_visible && _player != null)
        {
            _godModeButton.Text = $"God Mode: {(_player.IsGodMode ? "ON" : "OFF")}";
        }
    }
    
    public override void _UnhandledInput(InputEvent @event)
    {
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
        
        Label title = new Label { Text = "DEBUG ACTIONS (F4)", HorizontalAlignment = HorizontalAlignment.Center };
        title.AddThemeColorOverride("font_color", new Color(1f, 0.3f, 0.3f));
        _vbox.AddChild(title);
        
        Button xpBtn = new Button { Text = "+ 1000 XP" };
        xpBtn.Pressed += () => _eventBus.EmitSignal(EventBus.SignalName.XpGained, 1000f);
        _vbox.AddChild(xpBtn);
        
        Button timeBtn = new Button { Text = "Advance Time Phase" };
        timeBtn.Pressed += () => _dayNightCycle?.AdvancePhase();
        _vbox.AddChild(timeBtn);
        
        _godModeButton = new Button { Text = "God Mode: OFF" };
        _godModeButton.Pressed += () => {
            if (_player != null) _player.IsGodMode = !_player.IsGodMode;
        };
        _vbox.AddChild(_godModeButton);
        
        _teleportButton = new Button { Text = "Teleport (Right Click): OFF" };
        _teleportButton.Pressed += () => {
            _teleportActive = !_teleportActive;
            _teleportButton.Text = $"Teleport (Right Click): {(_teleportActive ? "ON" : "OFF")}";
        };
        _vbox.AddChild(_teleportButton);
        
        Button healBtn = new Button { Text = "Full Heal" };
        healBtn.Pressed += () => _player?.Heal(99999f);
        _vbox.AddChild(healBtn);
        
        Button resourceBtn = new Button { Text = "+100 Wood & Stone" };
        resourceBtn.Pressed += () => {
            if (_player?.Inventory != null)
            {
                _player.Inventory.Add("wood", 100);
                _player.Inventory.Add("stone", 100);
                GD.Print("[Debug] Added 100 wood and stone");
            }
        };
        _vbox.AddChild(resourceBtn);
        
        Button spawnEnemyBtn = new Button { Text = "Spawn Test Enemy (Mouse)" };
        spawnEnemyBtn.Pressed += () => {
            if (_spawnManager != null)
            {
                Vector2 spawnPos = _panel.GetGlobalMousePosition();
                _spawnManager.ForceSpawnEnemy("shadow_crawler", spawnPos);
            }
        };
        _vbox.AddChild(spawnEnemyBtn);
        
        Button upgradeWeaponBtn = new Button { Text = "Upgrade Equipped Weapon" };
        upgradeWeaponBtn.Pressed += () => {
            if (_player != null && _player.EquippedWeapon != null)
            {
                _player.UpgradeWeaponFragmentLevel(_player.EquippedWeapon.Id);
                GD.Print($"[Debug] Upgraded weapon {_player.EquippedWeapon.Id}");
            }
        };
        _vbox.AddChild(upgradeWeaponBtn);
        
        AddChild(_panel);
    }
}
