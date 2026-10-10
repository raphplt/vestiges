using System.Collections.Generic;
using Godot;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.UI;

/// <summary>Sélection avant départ : le portrait prime, la fiche chiffrée est facultative.</summary>
public partial class HubCharactersPanel : MarginContainer
{
    [Signal] public delegate void DepartRequestedEventHandler(string characterId);
    [Signal] public delegate void QuestRequestedEventHandler(string questId);
    [Signal] public delegate void WeaponRequestedEventHandler(string weaponId);
    private VBoxContainer _list;
    private TextureRect _portrait;
    private Label _name;
    private Label _description;
    private Label _signature;
    private Label _condition;
    private Label _details;
    private Button _detailsButton;
    private Button _depart;
    private Button _quest;
    private Button _weapon;
    private CharacterData _examined;
    private readonly Dictionary<string, Button> _rows = new();
    private readonly Texture2D[] _idle = new Texture2D[CharacterPortrait.IdleFrames];
    private int _idleCount;
    private float _idleTime;
    public string ExaminedCharacterId => _examined?.Id;
    public bool CanDepart => _examined != null && MetaSaveManager.IsCharacterUnlocked(_examined.Id);

    public override void _Ready()
    {
        HBoxContainer body = new() { SizeFlagsVertical = SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("separation", 32);
        AddChild(body);
        ScrollContainer roster = new()
        {
            Name = "CharacterRoster", CustomMinimumSize = new Vector2(300, 0),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, FollowFocus = true,
        };
        body.AddChild(roster);
        _list = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _list.AddThemeConstantOverride("separation", 16);
        roster.AddChild(_list);
        _portrait = new TextureRect
        {
            CustomMinimumSize = new Vector2(660, 0), SizeFlagsHorizontal = SizeFlags.ExpandFill,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _portrait.Material = new ShaderMaterial { Shader = GD.Load<Shader>("res://assets/shaders/menu_portrait.gdshader") };
        body.AddChild(_portrait);
        PanelContainer panel = new() { CustomMinimumSize = new Vector2(400, 0), SizeFlagsHorizontal = SizeFlags.ExpandFill };
        panel.AddThemeStyleboxOverride("panel", MenuSkin.Panel());
        body.AddChild(panel);
        ScrollContainer scroll = new() { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, FollowFocus = true };
        panel.AddChild(scroll);
        VBoxContainer sheet = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        sheet.AddThemeConstantOverride("separation", 22);
        scroll.AddChild(sheet);
        _name = Label(sheet, "", TextRole.Title, UITheme.TextLight);
        _description = Label(sheet, "", TextRole.Heading, UITheme.TextColor);
        _signature = Label(sheet, "", TextRole.Subhead, UITheme.GoldBright);
        _weapon = MenuSkin.Button("");
        _weapon.Alignment = HorizontalAlignment.Left;
        _weapon.Pressed += () => EmitSignal(SignalName.WeaponRequested, _examined.StartingWeaponId);
        MenuSkin.StyleLink(_weapon);
        sheet.AddChild(_weapon);
        _detailsButton = MenuSkin.Button("Détails du personnage");
        _detailsButton.Pressed += () =>
        {
            _details.Visible = !_details.Visible;
            _detailsButton.Text = _details.Visible ? "Masquer les détails" : "Détails du personnage";
        };
        MenuSkin.StyleLink(_detailsButton);
        sheet.AddChild(_detailsButton);
        _details = Label(sheet, "", TextRole.Body, UITheme.TextColor);
        _details.Visible = false;
        sheet.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 24) });
        _condition = Label(sheet, "", TextRole.Body, UITheme.TextColor);
        _quest = MenuSkin.Button("Voir la quête");
        _quest.Pressed += () =>
        {
            QuestDefinition quest = QuestDataLoader.FindUnlocking(UnlockKind.Character, _examined.Id);
            if (quest != null) EmitSignal(SignalName.QuestRequested, quest.Id);
        };
        sheet.AddChild(_quest);
        _depart = MenuSkin.Button("Partir", true);
        _depart.Name = "DepartButton";
        _depart.CustomMinimumSize = new Vector2(0, 68);
        _depart.Pressed += () => { if (CanDepart) EmitSignal(SignalName.DepartRequested, _examined.Id); };
        sheet.AddChild(_depart);
    }

    private static Label Label(Container parent, string text, TextRole role, Color color)
    {
        Label label = UITheme.MakeLabel(text, role, color);
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        parent.AddChild(label);
        return label;
    }

    public Control Refresh(string selectedCharacterId = null)
    {
        if (_rows.Count == 0)
        {
            foreach (CharacterData character in CharacterDataLoader.GetAll())
            {
                bool unlocked = MetaSaveManager.IsCharacterUnlocked(character.Id);
                Button row = MenuSkin.Button("");
                MenuSkin.StyleLink(row);
                row.Name = character.Id;
                row.AccessibilityName = character.Name;
                row.CustomMinimumSize = new Vector2(284, 118);
                TextureRect sprite = new()
                {
                    Texture = CharacterPortrait.Idle(character.Id), Position = new Vector2(12, 10), Size = new Vector2(64, 96),
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    TextureFilter = TextureFilterEnum.Nearest, MouseFilter = MouseFilterEnum.Ignore,
                    Modulate = unlocked ? Colors.White : new Color(0.55f, 0.53f, 0.58f),
                };
                row.AddChild(sprite);
                Label name = UITheme.MakeLabel(character.Name, TextRole.Subhead, unlocked ? UITheme.TextLight : UITheme.TextDim);
                name.Position = new Vector2(88, 28);
                row.AddChild(name);
                Label state = UITheme.MakeLabel(unlocked ? "Jouable" : "À débloquer", TextRole.Small, UITheme.TextDim);
                state.Position = new Vector2(88, 66);
                row.AddChild(state);
                row.FocusEntered += () => ShowSheet(character);
                row.MouseEntered += () => ShowSheet(character);
                row.Pressed += () => { ShowSheet(character); if (unlocked) _depart.GrabFocus(); else if (_quest.Visible) _quest.GrabFocus(); };
                _list.AddChild(row);
                _rows[character.Id] = row;
            }
        }
        string id = selectedCharacterId ?? _examined?.Id;
        CharacterData selected = (id != null ? CharacterDataLoader.Get(id) : null) ?? CharacterDataLoader.GetAll()[0];
        ShowSheet(selected);
        return _rows.GetValueOrDefault(selected.Id);
    }

    private void ShowSheet(CharacterData character)
    {
        if (_examined == character) return;
        _examined = character;
        _idleTime = 0;
        _idleCount = 0;
        Texture2D art = CharacterPortrait.Artwork(character.Id);
        if (art == null)
            for (int i = 0; i < _idle.Length; i++)
            {
                Texture2D frame = CharacterPortrait.Idle(character.Id, i + 1);
                if (frame == null) break;
                _idle[_idleCount++] = frame;
            }
        _portrait.Texture = art ?? (_idleCount > 0 ? _idle[0] : null);
        _portrait.TextureFilter = art != null ? TextureFilterEnum.Linear : TextureFilterEnum.Nearest;
        _portrait.Modulate = CanDepart ? Colors.White : new Color(0.65f, 0.63f, 0.68f);
        _name.Text = character.Name;
        _description.Text = character.Description;
        PerkData passive = PerkDataLoader.GetAll().Find(perk => perk.Id == character.PassivePerk);
        _signature.Text = passive?.Name ?? "";
        WeaponData weapon = WeaponDataLoader.Get(character.StartingWeaponId);
        _weapon.Text = weapon?.Name ?? "";
        _weapon.Icon = weapon != null ? GD.Load<Texture2D>(weapon.Sprite) : null;
        _weapon.ExpandIcon = true;
        _weapon.AddThemeConstantOverride("icon_max_width", 48);
        _details.Visible = false;
        _detailsButton.Text = "Détails du personnage";
        MobilityConfig mobility = MobilityConfig.Load(character.MobilityProfile);
        CharacterStats stats = character.BaseStats;
        _details.Text = $"{passive?.Description}\n\n{weapon?.Summary}\n\n{mobility.Name} — {mobility.Description}\n\nPV {stats.MaxHp:0} · vitesse {stats.Speed:0}";
        QuestDefinition quest = QuestDataLoader.FindUnlocking(UnlockKind.Character, character.Id);
        _condition.Text = CanDepart ? "" : QuestBook.LockText(UnlockKind.Character, character.Id);
        _condition.Visible = !CanDepart;
        _quest.Visible = !CanDepart && quest != null;
        _depart.Disabled = !CanDepart;
        _depart.Text = CanDepart ? "Partir" : "Personnage verrouillé";
        foreach ((string id, Button row) in _rows)
            row.SelfModulate = id == character.Id ? Colors.White : new Color(0.78f, 0.78f, 0.82f);
    }

    public override void _Process(double delta)
    {
        if (_idleCount < 2 || !IsVisibleInTree()) return;
        _idleTime += (float)delta;
        _portrait.Texture = _idle[(int)(_idleTime * 5) % _idleCount];
    }
}
