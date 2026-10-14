using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.UI;

/// <summary>
/// Poche de la ceinture : creux cousu teinté par la rareté de l'objet (ronde pour une Réminiscence), icône, niveau ou
/// nombre de copies ; un liseré clair la désigne quand le fragment survolé la touche.
/// </summary>
public partial class BeltPocket : PanelContainer
{
    private static readonly Color EmptyTint = new("5a4636");
    private static readonly Color RarityBase = new("3a2a20");
    private static readonly Color Hollow = new("24180f");
    private static readonly Color Seam = new("8a6e4a");
    private static readonly Color HotBorder = new("f5f0eb");
    private const float GhostAlpha = 0.45f;

    private readonly StyleBox _style;
    private readonly Panel _hot;
    private readonly TextureRect _icon;
    private readonly Label _count;

    // Godot réinstancie les scripts par le constructeur sans paramètre (rechargement de l'assembly).
    public BeltPocket() : this(80f, false)
    {
    }

    public BeltPocket(float size, bool round)
    {
        CustomMinimumSize = Vector2.One * size;
        MouseFilter = MouseFilterEnum.Ignore;
        if (round)
        {
            StyleBoxFlat flat = new() { BgColor = Hollow, BorderColor = Seam, CornerDetail = 4 };
            flat.SetBorderWidthAll(2);
            flat.SetCornerRadiusAll((int)(size / 2f));
            _style = flat;
        }
        else
            _style = PaperArt.PocketStyle(EmptyTint);
        _style.ContentMarginLeft = _style.ContentMarginRight = _style.ContentMarginTop = _style.ContentMarginBottom = 0;
        AddThemeStyleboxOverride("panel", _style);
        Control inner = new() { MouseFilter = MouseFilterEnum.Ignore };
        AddChild(inner);
        float iconSize = size >= 64f ? 64f : 32f;
        _icon = new TextureRect
        {
            CustomMinimumSize = Vector2.One * iconSize,
            Size = Vector2.One * iconSize,
            Position = new Vector2((size - iconSize) / 2f, (size - iconSize) / 2f + 2f),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = TextureFilterEnum.Nearest,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        inner.AddChild(_icon);
        _count = UITheme.MakeLabel("", TextRole.Small, UITheme.TextLight, TextWeight.Regular, HorizontalAlignment.Right);
        inner.AddChild(_count);
        _count.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomRight);
        _count.GrowHorizontal = GrowDirection.Begin;
        _count.GrowVertical = GrowDirection.Begin;
        _count.OffsetRight = -4f;
        _count.OffsetBottom = 2f;

        StyleBoxFlat hot = new() { DrawCenter = false, BorderColor = HotBorder, CornerDetail = 4 };
        hot.SetBorderWidthAll(4);
        if (round)
            hot.SetCornerRadiusAll((int)(size / 2f));
        _hot = new Panel { MouseFilter = MouseFilterEnum.Ignore, Visible = false };
        _hot.AddThemeStyleboxOverride("panel", hot);
        inner.AddChild(_hot);
        _hot.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
    }

    /// <summary>Identifiant de ce qu'elle contient ; <c>null</c> pour une poche vide.</summary>
    public string Id { get; private set; }

    public void Fill(string id, Texture2D icon, string rarityId, string count)
    {
        Id = id;
        _icon.Texture = icon;
        _icon.Modulate = Colors.White;
        _count.Text = count;
        if (rarityId == null)
            return;
        Color accent = RarityPalette.Main(rarityId);
        if (_style is StyleBoxTexture pocket)
            pocket.ModulateColor = RarityBase.Lerp(accent, 0.55f);
        else if (_style is StyleBoxFlat flat)
        {
            flat.BgColor = Hollow.Lerp(accent, 0.38f);
            flat.BorderColor = accent;
        }
    }

    /// <summary>
    /// Poche touchée par le fragment survolé : liseré clair et épais, que le bord or d'un objet légendaire ne peut pas
    /// imiter ; vide, elle montre l'objet en fantôme.
    /// </summary>
    public void SetHot(bool hot, Texture2D ghost)
    {
        _hot.Visible = hot;
        _count.AddThemeColorOverride("font_color", hot ? UITheme.GoldBright : UITheme.TextLight);
        if (Id != null)
            return;
        _icon.Texture = hot ? ghost : null;
        _icon.Modulate = new Color(1f, 1f, 1f, GhostAlpha);
    }
}
