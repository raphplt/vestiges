using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.UI;

/// <summary>
/// Page Personnages de l'accueil (plan 06 §9.10, lot Q3) : les personnages du jeu, jouables ou à débloquer ; ceux qui
/// ne sont pas encore en jeu n'y figurent pas. À gauche la liste ; à droite le portrait agrandi (illustration si elle
/// existe, sinon le sprite de jeu au repos, animé) et la fiche : description, passif, arme de départ, statistiques et
/// la quête qui le débloque.
/// </summary>
public partial class HubCharactersPanel : MarginContainer
{
    private const float RowWidth = 420f;
    private const float RowHeight = 96f;
    /// <summary>Côté du portrait : l'illustration carrée y tient entière, le sprite 48×64 y est agrandi ×6.</summary>
    private const float PortraitSize = 432f;
    private const float IdleFps = 5f;
    private static readonly Color LockedTint = new(0.45f, 0.44f, 0.5f, 1f);

    private Font _bodyFont;
    private Font _strongFont;
    private VBoxContainer _list;
    private TextureRect _portrait;
    private Label _name;
    private Label _state;
    private Label _description;
    private Label _passive;
    private Label _weapon;
    private Label _stats;
    private Label _quest;
    private readonly Texture2D[] _idle = new Texture2D[CharacterPortrait.IdleFrames];
    private int _idleCount;
    private float _idleTime;

    public override void _Ready()
    {
        _bodyFont = UITheme.BodyFont;
        _strongFont = UITheme.StrongFont;

        HBoxContainer body = new() { SizeFlagsVertical = SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("separation", 48);
        AddChild(body);

        ScrollContainer scroll = new()
        {
            CustomMinimumSize = new Vector2(RowWidth + 24f, 0f),
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            FollowFocus = true,
        };
        body.AddChild(scroll);
        _list = new VBoxContainer();
        _list.AddThemeConstantOverride("separation", 10);
        scroll.AddChild(_list);

        // Cadre sombre filet d'or : le portrait ne se perd pas dans le décor du camp derrière le voile.
        PanelContainer frame = new() { SizeFlagsVertical = SizeFlags.ShrinkBegin };
        frame.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(UITheme.BgDark, 0.92f),
            BorderColor = UITheme.GoldDim,
            BorderWidthTop = 2,
            BorderWidthBottom = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            ContentMarginLeft = 24,
            ContentMarginRight = 24,
            ContentMarginTop = 24,
            ContentMarginBottom = 24,
        });
        body.AddChild(frame);
        _portrait = new TextureRect
        {
            CustomMinimumSize = new Vector2(PortraitSize, PortraitSize),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = TextureFilterEnum.Nearest,
        };
        frame.AddChild(_portrait);

        VBoxContainer sheet = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        sheet.AddThemeConstantOverride("separation", 12);
        body.AddChild(sheet);
        _name = AddLabel(sheet, _strongFont, TextRole.Banner, UITheme.GoldBright);
        _state = AddLabel(sheet, _bodyFont, TextRole.Heading, UITheme.TextDim);
        _description = AddLabel(sheet, _bodyFont, TextRole.Heading, UITheme.TextLight);
        _passive = AddLabel(sheet, _bodyFont, TextRole.Heading, UITheme.TextColor);
        _weapon = AddLabel(sheet, _bodyFont, TextRole.Heading, UITheme.TextColor);
        _stats = AddLabel(sheet, _bodyFont, TextRole.Heading, UITheme.TextDim);
        _quest = AddLabel(sheet, _strongFont, TextRole.Heading, UITheme.CyanEssence);
    }

    /// <summary>Le sprite de repos tourne tant que la page est ouverte ; rien ne tourne sinon.</summary>
    public override void _Process(double delta)
    {
        if (_idleCount < 2 || !IsVisibleInTree())
            return;
        _idleTime += (float)delta;
        _portrait.Texture = _idle[(int)(_idleTime * IdleFps) % _idleCount];
    }

    private static Label AddLabel(Container parent, Font font, TextRole role, Color color)
    {
        Label label = new() { AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(420f, 0f) };
        label.AddThemeFontOverride("font", font);
        UITheme.SetTextRole(label, role);
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeConstantOverride("outline_size", 5);
        label.AddThemeColorOverride("font_outline_color", new Color(0.02f, 0.02f, 0.05f, 0.85f));
        parent.AddChild(label);
        return label;
    }

    /// <summary>Reconstruit la liste ; renvoie la ligne du personnage choisi à l'accueil, qui prend le focus.</summary>
    public Control Refresh(string selectedCharacterId)
    {
        foreach (Node child in _list.GetChildren())
            child.QueueFree();
        Control focused = null;
        CharacterData focusedCharacter = null;
        foreach (CharacterData character in CharacterDataLoader.GetAll())
        {
            Button row = MakeRow(character);
            _list.AddChild(row);
            if (focused == null || character.Id == selectedCharacterId)
            {
                focused = row;
                focusedCharacter = character;
            }
        }
        if (focusedCharacter != null)
            ShowSheet(focusedCharacter);
        return focused;
    }

    private Button MakeRow(CharacterData character)
    {
        Button row = new() { CustomMinimumSize = new Vector2(RowWidth, RowHeight), FocusMode = FocusModeEnum.All };
        row.TextureFilter = TextureFilterEnum.Nearest;
        bool unlocked = MetaSaveManager.IsCharacterUnlocked(character.Id);
        string skin = unlocked ? "ui_card_normal.png" : "ui_card_locked.png";
        StyleBoxTexture normal = UITheme.CreateNinePatch(UITheme.LoadTex(UITheme.MenusPath + skin), 4, 4, 4, 4);
        StyleBoxTexture lit = UITheme.CreateNinePatch(UITheme.LoadTex(UITheme.MenusPath + "ui_card_selected.png"), 4, 4, 4, 4);
        row.AddThemeStyleboxOverride("normal", normal);
        row.AddThemeStyleboxOverride("pressed", normal);
        row.AddThemeStyleboxOverride("hover", lit);
        row.AddThemeStyleboxOverride("focus", lit);
        row.AddThemeStyleboxOverride("hover_pressed", lit);

        row.AddChild(new TextureRect
        {
            Texture = CharacterPortrait.Idle(character.Id),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = TextureFilterEnum.Nearest,
            MouseFilter = MouseFilterEnum.Ignore,
            Position = new Vector2(10f, 6f),
            Size = new Vector2(63f, 84f),
            SelfModulate = unlocked ? Colors.White : LockedTint,
        });
        Label name = new() { Text = character.Name, MouseFilter = MouseFilterEnum.Ignore, Position = new Vector2(92f, 18f) };
        name.AddThemeFontOverride("font", _strongFont);
        UITheme.SetTextRole(name, TextRole.Heading);
        name.AddThemeColorOverride("font_color", unlocked ? UITheme.TextLight : UITheme.TextDim);
        row.AddChild(name);
        Label state = new() { Text = StateText(unlocked), MouseFilter = MouseFilterEnum.Ignore, Position = new Vector2(92f, 52f) };
        state.AddThemeFontOverride("font", _bodyFont);
        UITheme.SetTextRole(state, TextRole.Body);
        state.AddThemeColorOverride("font_color", UITheme.TextDim);
        row.AddChild(state);

        row.FocusEntered += () => ShowSheet(character);
        row.MouseEntered += () => ShowSheet(character);
        UITheme.WireButtonAudio(row);
        return row;
    }

    private static string StateText(bool unlocked) => unlocked ? "Jouable" : "À débloquer";

    private void ShowSheet(CharacterData character)
    {
        bool unlocked = MetaSaveManager.IsCharacterUnlocked(character.Id);
        LoadPortrait(character.Id, unlocked);
        _name.Text = character.Name;
        _state.Text = StateText(unlocked);
        QuestDefinition quest = QuestDataLoader.FindUnlocking(UnlockKind.Character, character.Id);
        bool questDone = quest != null && MetaSaveManager.HasCompletedQuest(quest.Id);
        _quest.Text = quest == null ? "" : questDone
            ? $"Débloqué par la quête « {quest.Name} »."
            : $"Quête « {quest.Name} » : {quest.Description}";
        _description.Text = character.Description;
        PerkData passive = PerkDataLoader.GetAll().Find(perk => perk.Id == character.PassivePerk);
        _passive.Text = passive == null ? "" : $"Passif — {passive.Name} : {passive.Description}";
        WeaponData weapon = WeaponDataLoader.Get(character.StartingWeaponId);
        _weapon.Text = weapon == null ? "" : $"Arme de départ — {weapon.Name} : {weapon.Summary}";
        CharacterStats stats = character.BaseStats;
        _stats.Text = $"PV {stats.MaxHp:0} · vitesse {stats.Speed:0} · dégâts {stats.AttackDamage:0} · cadence ×{stats.AttackSpeed:0.0} · "
            + $"portée {stats.AttackRange:0} · régénération {stats.RegenRate:0.0}/s";
    }

    /// <summary>
    /// L'illustration se lisse à la réduction ; le sprite de jeu, pixel art, s'agrandit sans flou et s'anime au repos.
    /// </summary>
    private void LoadPortrait(string characterId, bool unlocked)
    {
        _idleTime = 0f;
        _idleCount = 0;
        Texture2D artwork = CharacterPortrait.Artwork(characterId);
        if (artwork == null)
        {
            for (int i = 0; i < _idle.Length; i++)
            {
                Texture2D frame = CharacterPortrait.Idle(characterId, i + 1);
                if (frame == null)
                    break;
                _idle[_idleCount++] = frame;
            }
        }
        _portrait.TextureFilter = artwork != null ? TextureFilterEnum.Linear : TextureFilterEnum.Nearest;
        _portrait.Texture = artwork ?? (_idleCount > 0 ? _idle[0] : null);
        _portrait.SelfModulate = unlocked ? Colors.White : LockedTint;
    }
}
