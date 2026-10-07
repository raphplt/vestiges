using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Infrastructure;

namespace Vestiges.UI;

/// <summary>
/// Blocs du bilan dense (plan 02 lot D, M3), construits depuis le relevé de fin de run : tableau des armes, case de
/// build, carte de la créature qui a porté le dernier coup, faits de la run. Rien ici ne lit l'état vivant de la run.
/// </summary>
public static class RunSummaryPanels
{
    public const float IconSize = 48f;
    private const float PortraitScale = 4f;
    private const float RowHeight = 62f;
    private const float NameWidth = 250f;
    private const float BarWidth = 210f;
    private const float NumberWidth = 96f;
    private const int Separation = 10;
    // Du bord de la ligne à la colonne des éliminations : case, nom, part, dégâts, dégâts par seconde et leurs écarts.
    private const float KillsColumnX = IconSize + 14f + NameWidth + BarWidth + NumberWidth * 2f + Separation * 5;
    private static readonly Color SlotColor = new(0.08f, 0.07f, 0.12f, 0.9f);
    private static readonly Color BarTrack = new(1f, 1f, 1f, 0.06f);

    /// <summary>Une ligne par arme du build, puis les éliminations sans arme (brûlure, feu, explosions) s'il y en a.</summary>
    public static Control WeaponTable(IReadOnlyList<RunWeaponRecord> weapons, int totalKills, float runSeconds)
    {
        VBoxContainer table = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
        table.AddThemeConstantOverride("separation", 6);
        table.AddChild(HeaderRow());

        float totalDamage = 0f;
        float topDamage = 0f;
        int creditedKills = 0;
        foreach (RunWeaponRecord weapon in weapons)
        {
            totalDamage += weapon.Damage;
            topDamage = Mathf.Max(topDamage, weapon.Damage);
            creditedKills += weapon.Kills;
        }
        foreach (RunWeaponRecord weapon in weapons)
        {
            bool top = weapon.Damage > 0f && weapon.Damage >= topDamage;
            float share = totalDamage > 0f ? weapon.Damage / totalDamage : 0f;
            float held = weapon.HeldSeconds is > 1f ? weapon.HeldSeconds.Value : Mathf.Max(1f, runSeconds);
            table.AddChild(WeaponRow(weapon, share, weapon.Damage / held, top));
        }

        int otherKills = totalKills - creditedKills;
        if (otherKills > 0)
        {
            HBoxContainer row = Row();
            row.AddChild(Fixed(Text(TranslationServer.Translate("UI_END_OTHER_EFFECTS"), TextRole.Body, UITheme.TextDim), KillsColumnX - Separation));
            row.AddChild(Fixed(Text(otherKills.ToString("N0"), TextRole.Body, UITheme.TextDim, HorizontalAlignment.Right), NumberWidth));
            table.AddChild(row);
        }
        return table;
    }

    private static Control HeaderRow()
    {
        HBoxContainer row = Row();
        row.AddChild(Fixed(Text(TranslationServer.Translate("UI_END_WEAPONS"), TextRole.Subhead, UITheme.TextDim, weight: TextWeight.Strong), IconSize + 14f + NameWidth + BarWidth + Separation * 2));
        foreach (string key in new[] { "UI_END_COL_DAMAGE", "UI_END_COL_DPS", "UI_END_COL_KILLS" })
            row.AddChild(Fixed(Text(TranslationServer.Translate(key), TextRole.Small, UITheme.TextDim, HorizontalAlignment.Right), NumberWidth));
        return row;
    }

    private static Control WeaponRow(RunWeaponRecord weapon, float share, float dps, bool top)
    {
        WeaponData data = WeaponDataLoader.Get(weapon.Id);
        Color accent = top ? UITheme.GoldBright : UITheme.TextLight;
        HBoxContainer row = Row();
        row.CustomMinimumSize = new Vector2(0f, RowHeight);
        row.AddChild(Slot(data?.Sprite, weapon.Level, top ? UITheme.GoldBright : UITheme.GoldDim));

        VBoxContainer name = new() { MouseFilter = Control.MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center };
        name.AddChild(Text(data?.Name ?? weapon.Id, TextRole.Lead, accent, weight: TextWeight.Strong));
        name.AddChild(Text(string.Format(TranslationServer.Translate("UI_END_WEAPON_LEVEL"), weapon.Level), TextRole.Small, UITheme.TextDim));
        row.AddChild(Fixed(name, NameWidth));

        // Part des dégâts du build : barre, puis pourcentage.
        VBoxContainer shareBox = new() { MouseFilter = Control.MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center };
        Control bar = new() { CustomMinimumSize = new Vector2(BarWidth - 20f, 8f), MouseFilter = Control.MouseFilterEnum.Ignore };
        bar.AddChild(new ColorRect { Color = BarTrack, Size = new Vector2(BarWidth - 20f, 8f), MouseFilter = Control.MouseFilterEnum.Ignore });
        bar.AddChild(new ColorRect { Color = top ? UITheme.GoldColor : UITheme.CyanEssence, Size = new Vector2((BarWidth - 20f) * share, 8f), MouseFilter = Control.MouseFilterEnum.Ignore });
        shareBox.AddChild(bar);
        shareBox.AddChild(Text($"{Mathf.RoundToInt(share * 100f)} %", TextRole.Small, UITheme.TextDim));
        row.AddChild(Fixed(shareBox, BarWidth));

        row.AddChild(Fixed(Text(Mathf.RoundToInt(weapon.Damage).ToString("N0"), TextRole.Lead, accent, HorizontalAlignment.Right, TextWeight.Strong), NumberWidth));
        row.AddChild(Fixed(Text(dps.ToString(dps < 10f ? "N1" : "N0"), TextRole.Lead, UITheme.TextColor, HorizontalAlignment.Right), NumberWidth));
        row.AddChild(Fixed(Text(weapon.Kills.ToString("N0"), TextRole.Lead, UITheme.TextColor, HorizontalAlignment.Right), NumberWidth));
        return row;
    }

    /// <summary>Case du build : icône, cadre coloré selon la famille, niveau en coin. Vide : cadre éteint.</summary>
    public static Control Slot(string iconPath, int level, Color frame, float iconSize = IconSize)
    {
        PanelContainer slot = new()
        {
            CustomMinimumSize = new Vector2(iconSize + 14f, iconSize + 14f),
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        StyleBoxFlat style = new()
        {
            BgColor = SlotColor,
            BorderColor = frame,
            BorderWidthLeft = 3,
            BorderWidthRight = 3,
            BorderWidthTop = 3,
            BorderWidthBottom = 3,
        };
        slot.AddThemeStyleboxOverride("panel", style);
        if (!string.IsNullOrEmpty(iconPath) && ResourceLoader.Exists(iconPath))
        {
            slot.AddChild(new TextureRect
            {
                Texture = GD.Load<Texture2D>(iconPath),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                CustomMinimumSize = new Vector2(iconSize, iconSize),
                MouseFilter = Control.MouseFilterEnum.Ignore,
            });
        }
        if (level > 0)
        {
            Label badge = Text(level.ToString(), TextRole.Small, UITheme.GoldBright, HorizontalAlignment.Right, TextWeight.Bold);
            badge.VerticalAlignment = VerticalAlignment.Bottom;
            slot.AddChild(badge);
        }
        return slot;
    }

    /// <summary>
    /// Ce qui a porté le dernier coup : la créature en grand et son nom, ou l'Effacement lui-même. Null si la cause
    /// n'est pas connue.
    /// </summary>
    public static Control KillerCard(string enemyId, float atSeconds)
    {
        if (string.IsNullOrEmpty(enemyId) || enemyId == "unknown")
            return null;
        bool erasure = enemyId == "void";
        // Un événement (pluie d'éclats, relique qui tombe) porte son nom traduit, sans portrait (plan 27 V4).
        RunEventData runEvent = enemyId.StartsWith(Events.RunEvents.RunEventContext.DeathCausePrefix)
            ? RunEventOf(enemyId[Events.RunEvents.RunEventContext.DeathCausePrefix.Length..])
            : null;
        EnemyData data = erasure || runEvent != null ? null : EnemyDataLoader.Get(enemyId);

        PanelContainer card = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
        card.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = SlotColor,
            BorderColor = new Color(UITheme.TextLight, 0.35f),
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            BorderWidthTop = 2,
            BorderWidthBottom = 2,
            ContentMarginLeft = 18,
            ContentMarginRight = 18,
            ContentMarginTop = 12,
            ContentMarginBottom = 12,
        });
        HBoxContainer row = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
        row.AddThemeConstantOverride("separation", 18);
        card.AddChild(row);

        Texture2D portrait = data != null ? PortraitOf(data) : null;
        if (portrait != null)
        {
            row.AddChild(new TextureRect
            {
                Texture = portrait,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                CustomMinimumSize = new Vector2(portrait.GetWidth() * PortraitScale, portrait.GetHeight() * PortraitScale),
                MouseFilter = Control.MouseFilterEnum.Ignore,
            });
        }
        VBoxContainer text = new() { MouseFilter = Control.MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center };
        text.AddChild(Text(TranslationServer.Translate("UI_END_FELL_TO"), TextRole.Small, UITheme.TextDim));
        string name = erasure ? TranslationServer.Translate("UI_END_KILLER_ERASURE")
            : runEvent != null ? TranslationServer.Translate(runEvent.TitleKey)
            : data?.Name ?? enemyId;
        text.AddChild(Text(name, TextRole.Heading, UITheme.TextLight, weight: TextWeight.Strong));
        text.AddChild(Text(string.Format(TranslationServer.Translate("UI_END_AT_TIME"), FormatDuration(atSeconds)), TextRole.Small, UITheme.TextDim));
        row.AddChild(text);
        return card;
    }

    private static RunEventData RunEventOf(string id)
    {
        foreach (RunEventData runEvent in RunEventDataLoader.Events)
            if (runEvent.Id == id)
                return runEvent;
        return null;
    }

    /// <summary>Première image de face, rognée à ses pixels visibles : la créature remplit la carte.</summary>
    private static Texture2D PortraitOf(EnemyData data)
    {
        SpriteFrames frames = EnemySpriteLoader.LoadOrGet(data.Id, data.Visual?.SpriteFolder);
        if (frames == null)
            return null;
        foreach (string animation in new[] { "S_idle", "SE_idle", "SW_idle" })
        {
            if (!frames.HasAnimation(animation) || frames.GetFrameCount(animation) == 0)
                continue;
            Texture2D frame = frames.GetFrameTexture(animation, 0);
            Image image = frame.GetImage();
            if (image == null)
                return frame;
            if (image.IsCompressed())
                image.Decompress();
            Rect2I used = image.GetUsedRect();
            return used.Size.X > 0 ? ImageTexture.CreateFromImage(image.GetRegion(used)) : frame;
        }
        return null;
    }

    /// <summary>Faits de la run en deux colonnes : libellé discret, valeur lisible.</summary>
    public static Control Facts(IReadOnlyList<(string Caption, string Value)> facts)
    {
        GridContainer grid = new() { Columns = 2, MouseFilter = Control.MouseFilterEnum.Ignore };
        grid.AddThemeConstantOverride("h_separation", 24);
        grid.AddThemeConstantOverride("v_separation", 10);
        foreach ((string caption, string value) in facts)
        {
            grid.AddChild(Fixed(Text(caption, TextRole.Lead, UITheme.TextDim), 230f));
            grid.AddChild(Text(value, TextRole.Heading, UITheme.TextLight, weight: TextWeight.Strong));
        }
        return grid;
    }

    public static string FormatDuration(float seconds)
    {
        int total = Mathf.Max(0, Mathf.RoundToInt(seconds));
        return $"{total / 60:00}:{total % 60:00}";
    }

    private static HBoxContainer Row()
    {
        HBoxContainer row = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
        row.AddThemeConstantOverride("separation", Separation);
        return row;
    }

    private static Control Fixed(Control control, float width)
    {
        control.CustomMinimumSize = new Vector2(width, control.CustomMinimumSize.Y);
        control.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        return control;
    }

    private static Label Text(string text, TextRole role, Color color, HorizontalAlignment align = HorizontalAlignment.Left, TextWeight weight = TextWeight.Regular) =>
        UITheme.MakeLabel(text, role, color, weight, align, outline: 4);
}
