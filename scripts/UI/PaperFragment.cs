using System;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.UI;

/// <summary>
/// Un fragment du level-up au fil (plan 04 §11) : papier vertical teinté par la rareté, photo de l'objet scotchée,
/// type, nom, gain en grand et tampon. Suspendu par son bord haut à une pince ; au repos il penche un peu, au survol il
/// se redresse, monte et prend un liseré. Banni ou relancé, il tombe et s'efface.
/// </summary>
public partial class PaperFragment : Control
{
    public const float Width = 380f;
    private const float FocusLift = -10f;
    private static readonly Color DimModulate = new(0.84f, 0.82f, 0.8f);
    private static readonly Vector2 ShadowOffset = new(10f, 14f);
    private static readonly Color ForgottenModulate = new(0.22f, 0.18f, 0.28f, 0f);

    /// <summary>Clic sur le fragment.</summary>
    public event Action Pressed;
    /// <summary>La souris entre sur le fragment.</summary>
    public event Action Hovered;

    public FragmentOption Option { get; private set; }

    /// <summary>Icône de l'objet, que la ceinture montre en fantôme dans la poche qu'il prendrait.</summary>
    public Texture2D Icon { get; private set; }

    private PanelContainer _paper;
    private StyleBoxTexture _paperStyle;
    private NinePatchRect _glow;
    private NinePatchRect _shadow;
    private Label _kind;
    private RarityIcon _rarityIcon;
    private Vector2 _anchor;
    private float _restAngle;
    private float _lift;
    private bool _focused;
    private Tween _poseTween;

    public void Setup(FragmentOption option, Player player, float restAngle, int seed)
    {
        Option = option;
        _restAngle = restAngle;
        MouseFilter = MouseFilterEnum.Ignore;
        bool isAscension = option.Type == FragmentOption.AscensionType;
        bool isWeapon = isAscension || option.Type is "weapon_new" or "weapon_upgrade" or FragmentOption.SurplusType;
        bool isPerk = option.Type == PerkSpecializationOffers.OptionType;
        bool isNew = isPerk || isAscension || option.Type is "weapon_new" or "passive_new";

        _glow = new NinePatchRect
        {
            Texture = PaperArt.GlowTexture(),
            PatchMarginLeft = PaperArt.PaperLeft + PaperArt.GlowSpread,
            PatchMarginTop = PaperArt.PaperTop + PaperArt.GlowSpread,
            PatchMarginRight = PaperArt.PaperRight + PaperArt.GlowSpread,
            PatchMarginBottom = PaperArt.PaperBottom + PaperArt.GlowSpread,
            AxisStretchHorizontal = NinePatchRect.AxisStretchMode.Tile,
            AxisStretchVertical = NinePatchRect.AxisStretchMode.Tile,
            TextureFilter = TextureFilterEnum.Nearest,
            MouseFilter = MouseFilterEnum.Ignore,
            Visible = false,
        };
        AddChild(_glow);

        // Ombre portée sur le mur invisible derrière le fil : la feuille décolle un peu du fond.
        _shadow = new NinePatchRect
        {
            Texture = PaperArt.GlowTexture(),
            PatchMarginLeft = PaperArt.PaperLeft + PaperArt.GlowSpread,
            PatchMarginTop = PaperArt.PaperTop + PaperArt.GlowSpread,
            PatchMarginRight = PaperArt.PaperRight + PaperArt.GlowSpread,
            PatchMarginBottom = PaperArt.PaperBottom + PaperArt.GlowSpread,
            AxisStretchHorizontal = NinePatchRect.AxisStretchMode.Tile,
            AxisStretchVertical = NinePatchRect.AxisStretchMode.Tile,
            TextureFilter = TextureFilterEnum.Nearest,
            MouseFilter = MouseFilterEnum.Ignore,
            Modulate = new Color(0.02f, 0.01f, 0.03f, 0.42f),
        };
        AddChild(_shadow);
        MoveChild(_shadow, 0);

        _paperStyle = PaperArt.PaperStyle(PaperTint(option.Rarity?.Id, isAscension, isPerk), seed % PaperArt.PaperVariants);
        _paperStyle.ContentMarginLeft = 32;
        _paperStyle.ContentMarginRight = 32;
        _paperStyle.ContentMarginTop = 26;
        _paperStyle.ContentMarginBottom = 34;
        _paper = new PanelContainer { CustomMinimumSize = new Vector2(Width, 0), MouseFilter = MouseFilterEnum.Stop };
        _paper.AddThemeStyleboxOverride("panel", _paperStyle);
        _paper.MouseEntered += () => Hovered?.Invoke();
        _paper.GuiInput += @event =>
        {
            if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
            {
                _paper.AcceptEvent();
                Pressed?.Invoke();
            }
        };
        _paper.MinimumSizeChanged += Fit;
        AddChild(_paper);

        _paper.AddChild(BuildStains(seed));
        VBoxContainer body = new() { MouseFilter = MouseFilterEnum.Ignore };
        body.AddThemeConstantOverride("separation", 4);
        _paper.AddChild(body);

        Icon = LoadIcon(option, isWeapon);
        body.AddChild(BuildPhoto(Icon, PhotoTones(option.Rarity?.Id, isAscension, isPerk), seed));
        body.AddChild(Spacer(10));

        HBoxContainer header = new() { MouseFilter = MouseFilterEnum.Ignore };
        header.AddThemeConstantOverride("separation", 8);
        body.AddChild(header);
        if (option.Rarity != null)
        {
            _rarityIcon = new RarityIcon(option.Rarity.Rank);
            header.AddChild(_rarityIcon);
        }
        _kind = Ink(KindText(option, isWeapon, isAscension, isPerk), TextRole.Small, KindColor(option.Rarity?.Id));
        header.AddChild(_kind);

        Label name = Ink(option.DisplayName, TextRole.Section, PaperArt.Ink);
        name.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        body.AddChild(name);
        body.AddChild(DashedLine());

        UpgradeParts parts = UpgradeText.Parts(option, player);
        if (parts.Gain.Length > 0)
        {
            body.AddChild(Wrapped(parts.Label, TextRole.Body, PaperArt.Ink));
            body.AddChild(Ink(parts.Gain, TextRole.Title, PaperArt.InkGreen));
        }
        else if (parts.Label.Length > 0)
            body.AddChild(Wrapped(parts.Label, TextRole.Body, PaperArt.InkGreen));
        if (parts.Others.Length > 0)
            body.AddChild(Wrapped(parts.Others, TextRole.Small, PaperArt.InkGreen));
        if (parts.Detail.Length > 0)
            body.AddChild(Wrapped(parts.Detail, TextRole.Small, PaperArt.Ink));
        if (parts.Flavor.Length > 0)
            body.AddChild(Wrapped(parts.Flavor, TextRole.Small, PaperArt.InkDim));

        Control stamps = new() { CustomMinimumSize = new Vector2(0, 64), MouseFilter = MouseFilterEnum.Ignore };
        body.AddChild(stamps);
        string stampText = StampText(option, player, isWeapon, isAscension, isPerk, isNew);
        float right = 0f;
        if (stampText.Length > 0)
            right = AddStamp(stamps, stampText, isNew && !isAscension ? PaperArt.InkRed : isAscension ? PaperArt.InkGold : PaperArt.InkBlue, right, -6f);
        if (UpgradeText.ReachesMilestone(option, player))
            AddStamp(stamps, Tr("LEVELUP_MILESTONE_BADGE"), PaperArt.InkGold, right + 30f, 5f);

        Callable.From(Fit).CallDeferred();
    }

    /// <summary>Point d'accroche à l'écran, au milieu du bord haut : la pince tient le fragment là.</summary>
    public void HangAt(Vector2 anchor)
    {
        _anchor = anchor;
        Rotation = Mathf.DegToRad(_restAngle);
        Place();
    }

    public void SetFocused(bool focused, bool banish)
    {
        _focused = focused;
        _glow.Visible = focused;
        _glow.Modulate = banish ? new Color(0.85f, 0.25f, 0.2f) : UITheme.GoldBright;
        Modulate = focused ? Colors.White : DimModulate;
        ZIndex = focused ? 1 : 0;
        _poseTween?.Kill();
        _poseTween = CreateTween();
        _poseTween.SetPauseMode(Tween.TweenPauseMode.Process);
        _poseTween.SetParallel();
        _poseTween.TweenProperty(this, "rotation", focused ? 0f : Mathf.DegToRad(_restAngle), 0.16f)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        _poseTween.TweenMethod(Callable.From<float>(SetLift), _lift, focused ? FocusLift : 0f, 0.16f);
    }

    /// <summary>Le fil vient d'être accroché : le fragment se balance avant de se poser, droit s'il est survolé.</summary>
    public void Swing(float delay, float amplitudeDegrees)
    {
        float rest = _focused ? 0f : _restAngle;
        Rotation = Mathf.DegToRad(rest + amplitudeDegrees);
        SetLift(_focused ? FocusLift : 0f);
        _poseTween?.Kill();
        _poseTween = CreateTween();
        _poseTween.SetPauseMode(Tween.TweenPauseMode.Process);
        _poseTween.TweenInterval(delay);
        _poseTween.TweenProperty(this, "rotation", Mathf.DegToRad(rest), 1.1f)
            .SetTrans(Tween.TransitionType.Elastic).SetEase(Tween.EaseType.Out);
    }

    /// <summary>
    /// Le fragment quitte sa pince, tombe en tournant et disparaît ; oublié (banni), il s'éteint vers le sombre avant
    /// d'avoir fini de tomber. Il se libère à la fin de sa chute.
    /// </summary>
    public void Fall(Node layer, float direction, bool forgotten)
    {
        _poseTween?.Kill();
        _paper.MouseFilter = MouseFilterEnum.Ignore;
        _glow.Visible = false;
        ZIndex = 0;
        Reparent(layer);
        Tween tween = CreateTween();
        tween.SetPauseMode(Tween.TweenPauseMode.Process);
        tween.SetParallel();
        tween.TweenProperty(this, "position:y", Position.Y + (forgotten ? 260f : 520f), 0.4f)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        tween.TweenProperty(this, "rotation", Rotation + 0.5f * direction, 0.4f);
        tween.TweenProperty(this, "modulate", forgotten ? ForgottenModulate : new Color(Modulate, 0f), forgotten ? 0.36f : 0.28f)
            .SetDelay(forgotten ? 0f : 0.08f);
        tween.Chain().TweenCallback(Callable.From(QueueFree));
    }

    /// <summary>Bas du fragment au repos redressé, en coordonnées de son parent : l'étiquette de ce qui change y pend.</summary>
    public Vector2 BottomCenter => _anchor + new Vector2(0f, Size.Y * Scale.Y + _lift);

    /// <summary>
    /// La Chance (ou l'oubli, le Péril) a monté la rareté : le fragment montre d'abord la rareté tirée, puis il prend le
    /// rang gagné avec un trèfle et un son (plan 24 D2) ; le papier change de teinte au même instant.
    /// </summary>
    public void PlayRarityRaise(float delay)
    {
        UpgradeRarity rolled = Option.RolledRarity;
        string finalText = _kind.Text;
        Color finalInk = KindColor(Option.Rarity.Id);
        Color finalTint = PaperTint(Option.Rarity.Id, false, false);
        _kind.Text = RarityPalette.DisplayName(rolled.Id).ToUpper();
        _kind.AddThemeColorOverride("font_color", KindColor(rolled.Id));
        _paperStyle.ModulateColor = PaperTint(rolled.Id, false, false);
        _rarityIcon?.RevealFrom(rolled.Rank, Option.Rarity.Rank, delay);

        Tween tween = _kind.CreateTween();
        tween.SetPauseMode(Tween.TweenPauseMode.Process);
        tween.TweenInterval(delay + (Option.Rarity.Rank - rolled.Rank) * 6f / RarityArt.Fps);
        tween.TweenCallback(Callable.From(() =>
        {
            _kind.Text = finalText;
            _kind.AddThemeColorOverride("font_color", finalInk);
            _paperStyle.ModulateColor = finalTint;
            _kind.PivotOffset = _kind.Size / 2f;
            _kind.Scale = new Vector2(1.5f, 1.5f);
            AudioManager.PlayUI("sfx_rare_fragment", 0.08f, -6f);
        }));
        tween.TweenProperty(_kind, "scale", Vector2.One, 0.25f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
    }

    private void SetLift(float lift)
    {
        _lift = lift;
        Place();
    }

    // Le pivot est au milieu du bord haut : rotation et échelle laissent la pince en place.
    private void Place() => Position = _anchor + new Vector2(-Size.X / 2f, _lift);

    private void Fit()
    {
        Vector2 size = _paper.GetCombinedMinimumSize();
        _paper.Size = size;
        Size = size;
        PivotOffset = new Vector2(size.X / 2f, 0f);
        _glow.Position = -Vector2.One * PaperArt.GlowSpread;
        _glow.Size = size + Vector2.One * PaperArt.GlowSpread * 2f;
        _shadow.Position = _glow.Position + ShadowOffset;
        _shadow.Size = _glow.Size;
        Place();
    }

    private static Color PaperTint(string rarityId, bool ascension, bool perk)
    {
        if (rarityId != null)
            return PaperArt.Paper.Lerp(RarityPalette.Colors(rarityId).Light, 0.5f);
        if (ascension)
            return PaperArt.Paper.Lerp(UITheme.GoldBright, 0.35f);
        return perk ? PaperArt.Paper.Lerp(ChoiceStyle.PerkBorder, 0.3f) : PaperArt.Paper;
    }

    /// <summary>Sur le papier clair, la rareté s'écrit dans sa teinte sombre : la teinte principale ne s'y lirait pas.</summary>
    private static Color KindColor(string rarityId) => rarityId != null ? RarityPalette.Colors(rarityId).Dark : PaperArt.InkDim;

    private string KindText(FragmentOption option, bool isWeapon, bool isAscension, bool isPerk)
    {
        string kind = option.Rarity != null
            ? RarityPalette.DisplayName(option.Rarity.Id).ToUpper()
            : isPerk ? PerkTag(option.Id)
            : isAscension ? Tr("LEVELUP_ASCENSION")
            : Tr(isWeapon ? "LEVELUP_KIND_WEAPON"
                : PassiveSouvenirDataLoader.Get(option.Id)?.IsBadge == true ? "LEVELUP_KIND_BADGE" : "LEVELUP_KIND_OBJECT");
        return option.IsCarried ? $"{kind}  ·  {Tr("LEVELUP_CARRIED")}" : kind;
    }

    private string PerkTag(string id)
    {
        string family = PerkSpecializationDataLoader.Get(id)?.Family ?? "";
        return $"{Tr("LEVELUP_NEW_PERK")}  ·  {Tr($"PERK_FAMILY_{family.ToUpperInvariant()}")}";
    }

    /// <summary>« Niv 3 → 4 » pour une amélioration, « NOUVEAU » pour une arme ou un objet, « DÉFINITIVE » pour une voie.</summary>
    private string StampText(FragmentOption option, Player player, bool isWeapon, bool isAscension, bool isPerk, bool isNew)
    {
        if (isAscension)
            return Tr("LEVELUP_ASCENSION_FINAL");
        if (isPerk)
            return "";
        if (isNew)
            return Tr("LEVELUP_NEW");
        if (option.Type == FragmentOption.SurplusType)
            return Tr("LEVELUP_SURPLUS");
        int level = isWeapon ? player?.GetWeaponFragmentLevel(option.Id) ?? 0 : player?.GetPassiveLevel(option.Id) ?? 0;
        return string.Format(Tr("LEVELUP_LEVEL"), level, level + 1).ToUpper();
    }

    private static Texture2D LoadIcon(FragmentOption option, bool isWeapon)
    {
        string path = option.Type == PerkSpecializationOffers.OptionType
            ? PerkSpecializationDataLoader.Get(option.Id)?.Icon
            : isWeapon
            ? WeaponDataLoader.Get(option.Id)?.Sprite
            : PassiveSouvenirDataLoader.Get(option.Id)?.Icon;
        if (string.IsNullOrEmpty(path))
            return null;
        string resPath = path.StartsWith("res://") ? path : $"res://{path}";
        return ResourceLoader.Exists(resPath) ? GD.Load<Texture2D>(resPath) : null;
    }

    /// <summary>Teintes du tirage : celles de la rareté, l'or d'une voie, le turquoise des Mémoriaux pour une Réminiscence, sépia sinon.</summary>
    private static (string Key, RarityColors Colors) PhotoTones(string rarityId, bool ascension, bool perk)
    {
        if (rarityId != null)
            return (rarityId, RarityPalette.Colors(rarityId));
        if (ascension)
            return ("legendary", RarityPalette.Colors("legendary"));
        if (perk)
            return ("memorial", RarityPalette.Colors("memorial"));
        return ("sepia", new RarityColors(new Color("7a6248"), new Color("d8c4a0"), new Color("2a2018")));
    }

    /// <summary>
    /// Tirage photo scotché : la scène virée à la teinte, l'objet posé devant avec son ombre portée, deux bouts de
    /// scotch jaunis aux coins.
    /// </summary>
    private static Control BuildPhoto(Texture2D icon, (string Key, RarityColors Colors) tones, int seed)
    {
        Control holder = new() { CustomMinimumSize = new Vector2(0, PaperArt.PhotoHeight), MouseFilter = MouseFilterEnum.Ignore };
        Control print = new() { MouseFilter = MouseFilterEnum.Ignore, Size = new Vector2(PaperArt.PhotoWidth, PaperArt.PhotoHeight) };
        print.SetAnchorsPreset(LayoutPreset.CenterTop);
        print.OffsetLeft = -PaperArt.PhotoWidth / 2f;
        print.OffsetRight = PaperArt.PhotoWidth / 2f;
        print.OffsetBottom = PaperArt.PhotoHeight;
        holder.AddChild(print);
        print.AddChild(new TextureRect
        {
            Texture = PaperArt.Photo(tones.Key, tones.Colors.Dark, tones.Colors.Main, tones.Colors.Light, seed & 3),
            TextureFilter = TextureFilterEnum.Nearest,
            MouseFilter = MouseFilterEnum.Ignore,
        });
        Vector2 iconPosition = new((PaperArt.PhotoWidth - IconSize) / 2f, (PaperArt.PhotoHeight - IconSize) / 2f - 6f);
        print.AddChild(IconRect(icon, iconPosition + new Vector2(6f, 8f), new Color(0.02f, 0.01f, 0.04f, 0.55f)));
        print.AddChild(IconRect(icon, iconPosition, Colors.White));

        RandomNumberGenerator rng = new() { Seed = (ulong)(seed * 31 + 7) };
        for (int side = 0; side < 2; side++)
        {
            TextureRect tape = new()
            {
                Texture = PaperArt.Tape(),
                TextureFilter = TextureFilterEnum.Nearest,
                MouseFilter = MouseFilterEnum.Ignore,
                Rotation = Mathf.DegToRad((side == 0 ? -1f : 1f) * rng.RandfRange(14f, 26f)),
                Position = side == 0 ? new Vector2(-14f, -2f) : new Vector2(PaperArt.PhotoWidth - 30f, -14f),
            };
            print.AddChild(tape);
        }
        return holder;
    }

    private const float IconSize = 128f;

    private static TextureRect IconRect(Texture2D icon, Vector2 position, Color modulate) => new()
    {
        Texture = icon,
        Position = position,
        Size = Vector2.One * IconSize,
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        TextureFilter = TextureFilterEnum.Nearest,
        MouseFilter = MouseFilterEnum.Ignore,
        Modulate = modulate,
    };

    /// <summary>Taches d'humidité, parfois un rond de tasse et une pliure en travers : chaque feuille a vécu.</summary>
    private static Control BuildStains(int seed)
    {
        Control layer = new() { MouseFilter = MouseFilterEnum.Ignore };
        RandomNumberGenerator rng = new() { Seed = (ulong)(seed * 977 + 3) };
        int count = rng.RandiRange(3, 5);
        for (int i = 0; i < count; i++)
            AddDecal(layer, PaperArt.Stain(rng.RandiRange(0, 2)), rng.Randf(), rng.RandfRange(0.35f, 1f));
        if (rng.Randf() < 0.4f)
            AddDecal(layer, PaperArt.CoffeeRing(), rng.RandfRange(0.1f, 0.9f), rng.RandfRange(0.55f, 0.95f));
        if (rng.Randf() < 0.6f)
        {
            TextureRect crease = new()
            {
                Texture = PaperArt.Crease(),
                StretchMode = TextureRect.StretchModeEnum.Tile,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                TextureFilter = TextureFilterEnum.Nearest,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            layer.AddChild(crease);
            crease.AnchorRight = 1f;
            crease.AnchorTop = crease.AnchorBottom = rng.RandfRange(0.45f, 0.8f);
            crease.OffsetLeft = -32f;
            crease.OffsetRight = 32f;
            crease.OffsetBottom = 6f;
        }
        return layer;
    }

    private static void AddDecal(Control layer, Texture2D texture, float x, float y)
    {
        TextureRect decal = new() { Texture = texture, TextureFilter = TextureFilterEnum.Nearest, MouseFilter = MouseFilterEnum.Ignore };
        layer.AddChild(decal);
        decal.AnchorLeft = decal.AnchorRight = x;
        decal.AnchorTop = decal.AnchorBottom = y;
        decal.OffsetLeft = -texture.GetWidth() / 2f;
        decal.OffsetTop = -texture.GetHeight() / 2f;
    }

    /// <summary>Tampon encré, un peu de travers ; posé depuis la droite, il rend le bord gauche pour le suivant.</summary>
    private static float AddStamp(Control row, string text, Color ink, float fromRight, float degrees)
    {
        PanelContainer stamp = new() { MouseFilter = MouseFilterEnum.Ignore };
        StyleBoxTexture style = PaperArt.StampStyle(ink);
        style.ContentMarginLeft = style.ContentMarginRight = 14;
        style.ContentMarginTop = 4;
        style.ContentMarginBottom = 6;
        stamp.AddThemeStyleboxOverride("panel", style);
        stamp.AddChild(Ink(text, TextRole.Body, new Color(ink, 0.88f)));
        row.AddChild(stamp);
        Vector2 size = stamp.GetCombinedMinimumSize();
        stamp.AnchorLeft = stamp.AnchorRight = 1f;
        stamp.OffsetLeft = -fromRight - size.X;
        stamp.OffsetRight = -fromRight;
        stamp.OffsetTop = 6f;
        stamp.PivotOffset = size / 2f;
        stamp.Rotation = Mathf.DegToRad(degrees);
        return fromRight + size.X;
    }

    private static TextureRect DashedLine() => new()
    {
        Texture = PaperArt.Dash(),
        StretchMode = TextureRect.StretchModeEnum.Tile,
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        CustomMinimumSize = new Vector2(0, 2),
        TextureFilter = TextureFilterEnum.Nearest,
        MouseFilter = MouseFilterEnum.Ignore,
    };

    private static Control Spacer(float height) => new() { CustomMinimumSize = new Vector2(0, height), MouseFilter = MouseFilterEnum.Ignore };

    /// <summary>Encre sur papier : sans contour, qui baverait sur le fond clair.</summary>
    private static Label Ink(string text, TextRole role, Color color) =>
        UITheme.MakeLabel(text, role, color, TextWeight.Regular, HorizontalAlignment.Left, outline: 0);

    private static Label Wrapped(string text, TextRole role, Color color)
    {
        Label label = Ink(text, role, color);
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        return label;
    }
}
