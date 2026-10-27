using System;
using System.Globalization;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Development;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.UI;

/// <summary>Atelier F4 : les contrôles et la console appellent tous les mêmes commandes de run.</summary>
public partial class DebugActionPanel : CanvasLayer
{
    private PanelContainer _panel;
    private VBoxContainer _content;
    private DevelopmentRunTools _tools;
    private Label _status;
    private Label _summary;
    private CheckButton _godMode;
    private CheckButton _pause;
    private CheckButton _teleport;
    private SpinBox _level;
    private SpinBox _weaponLevel;
    private SpinBox _time;
    private OptionButton _weapon;
    private OptionButton _slot;
    private OptionButton _rarity;
    private OptionButton _enemy;
    private OptionButton _biome;
    private OptionButton _speed;
    private LineEdit _console;
    private bool _previousPause;
    private bool _pauseChanged;
    private double _refreshRemaining;

    public override void _Ready()
    {
        if (!DevelopmentMode.IsEnabled)
        {
            ProcessMode = ProcessModeEnum.Disabled;
            QueueFree();
            return;
        }
        ProcessMode = ProcessModeEnum.Always;
        Layer = 2010;
        _tools = GetParent().GetNode<DevelopmentRunTools>("DevelopmentRunTools");
        BuildUI();
        _panel.Hide();
        SetProcess(false);
    }

    public override void _ExitTree()
    {
        if (_panel is { Visible: true } && !_pauseChanged && _tools?.IsRunActive == true)
            GetTree().Paused = _previousPause;
    }

    public override void _Process(double delta)
    {
        _refreshRemaining -= delta;
        if (_refreshRemaining > 0)
            return;
        _refreshRemaining = 0.25;
        RefreshState();
    }

    // Les raccourcis doivent également fonctionner lorsqu'un champ ou une liste a le focus.
    public override void _Input(InputEvent @event)
    {
        if (!DevelopmentMode.IsEnabled || _panel == null)
            return;
        if (@event is InputEventKey { Pressed: true, Echo: false } key &&
            (key.Keycode == Key.F4 || (key.Keycode == Key.Escape && _panel.Visible)))
        {
            TogglePanel();
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (DevelopmentMode.IsEnabled && _tools?.TeleportEnabled == true &&
            @event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Right })
        {
            Vector2 position = _tools.Player.GetGlobalMousePosition();
            Run(FormattableString.Invariant($"move {position.X} {position.Y}"));
            GetViewport().SetInputAsHandled();
        }
    }

    public void TogglePanel()
    {
        if (!DevelopmentMode.IsEnabled || _tools == null)
            return;
        if (_panel.Visible)
        {
            _weapon.GetPopup().Hide(); _slot.GetPopup().Hide(); _rarity.GetPopup().Hide(); _enemy.GetPopup().Hide();
            _panel.Hide();
            SetProcess(false);
            if (!_pauseChanged)
                GetTree().Paused = _previousPause || _tools.HasPendingChoices;
            return;
        }
        _previousPause = GetTree().Paused;
        _pauseChanged = false;
        GetTree().Paused = true;
        _panel.Show();
        _level.Value = _tools.Progression.CurrentLevel;
        _time.Value = Math.Round(_tools.DifficultySeconds);
        RefreshSlots();
        SelectEquipped();
        RefreshState();
        SetProcess(true);
    }

    private void BuildUI()
    {
        _panel = new PanelContainer { Name = "Workshop" };
        AddChild(_panel);
        _panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _panel.AnchorLeft = 0.18f; _panel.AnchorRight = 0.82f;
        _panel.AnchorTop = 0.07f; _panel.AnchorBottom = 0.9f;
        StyleBoxFlat style = new()
        {
            BgColor = new Color("14151d"), BorderColor = new Color("a98750"),
            BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
            ContentMarginLeft = 24, ContentMarginRight = 24, ContentMarginTop = 16, ContentMarginBottom = 16
        };
        _panel.AddThemeStyleboxOverride("panel", style);
        VBoxContainer layout = new(); _panel.AddChild(layout);
        HBoxContainer header = Row(layout);
        Label title = new() { Text = "Atelier de test · F4", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        UITheme.SetTextRole(title, TextRole.Heading); header.AddChild(title);
        Button close = Button(header, "Fermer", TogglePanel); close.Name = "CloseWorkshop";
        _summary = Label(layout, "");
        ScrollContainer scroll = new()
        {
            Name = "WorkshopScroll",
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };
        layout.AddChild(scroll);
        _content = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _content.AddThemeConstantOverride("separation", 10); scroll.AddChild(_content);

        Section("Joueur");
        HBoxContainer toggles = Row(_content);
        _godMode = Check(toggles, "Invincibilité", enabled => Run(enabled ? "god on" : "god off"));
        Button(toggles, "Soigner", () => Run("heal"));
        HBoxContainer level = Row(_content); Label(level, "Niveau exact");
        _level = Number(level, "PlayerLevel", 1, 9999, 1);
        Button(level, "Fixer le niveau", () => Run($"level {(int)_level.Value}"));
        Label(_content, "Le niveau seul change ; les armes et le build restent à composer.");
        Quantity("XP (avec choix)", "XpAmount", "xp", 1000);
        Quantity("Essence", "EssenceAmount", "essence", 100);

        Section("Armes");
        _slot = Options(_content, "WeaponSlot");
        _slot.ItemSelected += _ => SelectEquipped();
        _weapon = Options(_content, "WeaponCatalog");
        foreach (WeaponData weapon in WeaponDataLoader.GetAll())
        {
            _weapon.AddItem(weapon.Name);
            _weapon.SetItemMetadata(_weapon.ItemCount - 1, weapon.Id);
        }
        _rarity = Options(_content, "UpgradeRarity");
        foreach (UpgradeRarity rarity in UpgradeRoller.Rarities)
        {
            _rarity.AddItem(Tr("RARITY_" + rarity.Id.ToUpperInvariant()));
            _rarity.SetItemMetadata(_rarity.ItemCount - 1, rarity.Id);
        }
        _rarity.Select(0);
        HBoxContainer weaponRow = Row(_content); Label(weaponRow, "Niveau de l'arme");
        _weaponLevel = Number(weaponRow, "WeaponLevel", 1, WeaponUpgradeDataLoader.GetWeaponMaxLevel(), 1);
        Button(weaponRow, "Équiper / reconstruire", () => Run($"weapon {Selected(_weapon)} {_slot.Selected + 1} {(int)_weaponLevel.Value} {Selected(_rarity)}"));
        Button(weaponRow, "Retirer", () => Run($"remove-weapon {_slot.Selected + 1}"));
        Label(_content, "La rareté règle chaque amélioration ; reconstruire remplace les gains.");

        Section("Temps");
        HBoxContainer clock = Row(_content);
        _pause = Check(clock, "Pause", enabled => Run(enabled ? "pause on" : "pause off"));
        _speed = Options(clock, "SimulationSpeed");
        foreach (string value in new[] { "0.25", "0.5", "1", "2", "4", "8" })
        {
            _speed.AddItem("×" + value); _speed.SetItemMetadata(_speed.ItemCount - 1, value);
        }
        _speed.Select(2); _speed.ItemSelected += _ => Run("speed " + Selected(_speed));
        HBoxContainer time = Row(_content); Label(time, "Difficulté à (secondes)");
        _time = Number(time, "DifficultyTime", 0, 86400, 0);
        Button(time, "Fixer l'horloge", () => Run($"time {(int)_time.Value}"));
        Label(_content, "Change les futurs spawns. L'Effacement et les événements ne sont pas rejoués.");

        Section("Monde");
        _teleport = Check(_content, "Téléportation au clic droit", enabled => Run(enabled ? "teleport on" : "teleport off"));
        HBoxContainer enemies = Row(_content); _enemy = Options(enemies, "EnemyCatalog");
        foreach (string id in EnemyDataLoader.GetAllIds())
        {
            _enemy.AddItem(EnemyDataLoader.Get(id).Name + " · " + id);
            _enemy.SetItemMetadata(_enemy.ItemCount - 1, id);
            if (id == "shadow_crawler") _enemy.Select(_enemy.ItemCount - 1);
        }
        Button(enemies, "Créer près du joueur", () =>
        {
            Vector2 position = _tools.Player.GlobalPosition + new Vector2(100, 0);
            Run(FormattableString.Invariant($"spawn {Selected(_enemy)} {position.X} {position.Y}"));
        });

        Section("Terres (plan 51)");
        Button(_content, "Terre suivante", () => Run("land next"));
        HBoxContainer biomes = Row(_content); _biome = Options(biomes, "BiomeCatalog");
        foreach (BiomeData biome in BiomeDataLoader.GetAll())
        {
            _biome.AddItem((string.IsNullOrEmpty(biome.PlaceKey) ? biome.Name : Tr(biome.PlaceKey)) + " · " + biome.Id);
            _biome.SetItemMetadata(_biome.ItemCount - 1, biome.Id);
        }
        Button(biomes, "Refaire la terre ici", () => Run("land " + Selected(_biome)));
        Label(_content, "Vide la carte et construit la suivante, ou refait celle-ci dans le biome choisi, au même rang ; joueur et build intacts.");

        Section("Console · commandes communes aux agents");
        HBoxContainer console = Row(_content);
        _console = new LineEdit { Name = "Command", PlaceholderText = "weapon makeshift_bow 1 10 rare", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        Compact(_console); console.AddChild(_console);
        _console.TextSubmitted += Run;
        Button(console, "Exécuter", () => Run(_console.Text));
        _status = Label(layout, "Ouverture : simulation suspendue. Fermer reprend son état précédent.");
        _status.Name = "CommandResult";
        Button opener = new() { Name = "OpenWorkshop", Text = "Outils dev · F4" };
        Compact(opener); AddChild(opener);
        opener.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomLeft);
        opener.OffsetLeft = 16; opener.OffsetTop = -58; opener.OffsetRight = 250; opener.OffsetBottom = -12;
        opener.Pressed += TogglePanel;
    }

    private void Run(string command)
    {
        if (!DevelopmentMode.IsEnabled)
            return;
        DevelopmentCommandResult result = _tools.Execute(command);
        if (result.Success && command.StartsWith("pause ", StringComparison.Ordinal))
            _pauseChanged = true;
        _status.Text = result.Message;
        _status.Modulate = result.Success ? new Color("d8dfca") : new Color("f19483");
        RefreshSlots(); RefreshState();
    }

    private void RefreshState()
    {
        _godMode.SetPressedNoSignal(_tools.Player.IsGodMode);
        _pause.SetPressedNoSignal(GetTree().Paused);
        _teleport.SetPressedNoSignal(_tools.TeleportEnabled);
        bool foundSpeed = false;
        for (int i = 0; i < _speed.ItemCount; i++)
        {
            double speed = double.Parse(_speed.GetItemMetadata(i).AsString(), CultureInfo.InvariantCulture);
            if (Math.Abs(speed - Engine.TimeScale) < 0.0001)
            {
                _speed.Select(i);
                foundSpeed = true;
                break;
            }
        }
        if (!foundSpeed)
        {
            _speed.Select(-1);
            _speed.Text = FormattableString.Invariant($"×{Engine.TimeScale}");
        }
        _summary.Text = FormattableString.Invariant($"PV {_tools.Player.CurrentHp:0}/{_tools.Player.EffectiveMaxHp:0} · Niveau {_tools.Progression.CurrentLevel} · Difficulté {_tools.DifficultySeconds:0}s · Simulation ×{Engine.TimeScale}");
    }

    private void RefreshSlots()
    {
        int selected = Mathf.Max(0, _slot.Selected);
        _slot.Clear();
        for (int i = 0; i < Player.MaxWeaponSlots; i++)
        {
            WeaponInstance weapon = _tools.Player.GetWeaponInstance(i);
            _slot.AddItem($"{i + 1} · {(weapon == null ? "Libre" : weapon.Name + " · niv. " + weapon.Level)}");
        }
        _slot.Select(selected);
        for (int i = _tools.Player.WeaponSlots.Count + 1; i < _slot.ItemCount; i++)
            _slot.SetItemDisabled(i, true);
    }

    private void SelectEquipped()
    {
        WeaponInstance weapon = _tools.Player.GetWeaponInstance(_slot.Selected);
        if (weapon == null) return;
        for (int i = 0; i < _weapon.ItemCount; i++)
            if (_weapon.GetItemMetadata(i).AsString() == weapon.Id) _weapon.Select(i);
        _weaponLevel.Value = weapon.Level;
    }

    private void Quantity(string caption, string name, string command, int initial)
    {
        HBoxContainer row = Row(_content); Label(row, caption);
        SpinBox amount = Number(row, name, 0, 1000000, initial);
        Button(row, "Ajouter", () => Run($"{command} {(int)amount.Value}"));
    }
    private void Section(string text)
    {
        Label title = Label(_content, text); title.AddThemeColorOverride("font_color", new Color("d4a843"));
    }
    private static HBoxContainer Row(Node parent)
    {
        HBoxContainer row = new(); row.AddThemeConstantOverride("separation", 12); parent.AddChild(row); return row;
    }
    private static Label Label(Node parent, string text)
    {
        Label label = new() { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        Compact(label); parent.AddChild(label); return label;
    }
    private static Button Button(Node parent, string text, Action action)
    {
        Button button = new() { Text = text }; Compact(button); parent.AddChild(button); button.Pressed += action; return button;
    }
    private static CheckButton Check(Node parent, string text, Action<bool> action)
    {
        CheckButton check = new() { Text = text }; Compact(check); parent.AddChild(check); check.Toggled += enabled => action(enabled); return check;
    }
    private static OptionButton Options(Node parent, string name)
    {
        OptionButton options = new() { Name = name, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, FitToLongestItem = false };
        Compact(options); parent.AddChild(options); return options;
    }
    private static SpinBox Number(Node parent, string name, double min, double max, double value)
    {
        SpinBox number = new() { Name = name, MinValue = min, MaxValue = max, Step = 1, Value = value, CustomMinimumSize = new Vector2(130, 0) };
        Compact(number); Compact(number.GetLineEdit()); parent.AddChild(number); return number;
    }
    private static void Compact(Control control) => control.AddThemeFontSizeOverride("font_size", 24);
    private static string Selected(OptionButton options) => options.GetItemMetadata(options.Selected).AsString();
}
