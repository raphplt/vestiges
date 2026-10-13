using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.UI;

/// <summary>
/// Page Crédits de l'accueil : contributeurs et ressources tierces, puis les licences que le jeu doit livrer
/// (Godot et ses composants, lus dans le moteur, et celles de data/ui/credits.json).
/// Un seul texte défilant : il prend le focus, les flèches et la molette le font défiler, Échap revient au menu.
/// </summary>
public partial class HubCreditsPanel : MarginContainer
{
    private RichTextLabel _text;
    private bool _built;

    public override void _Ready()
    {
        PanelContainer frame = new() { SizeFlagsVertical = SizeFlags.ExpandFill };
        frame.AddThemeStyleboxOverride("panel", MenuSkin.Panel(40));
        AddChild(frame);

        _text = new RichTextLabel
        {
            Name = "CreditsText",
            ScrollActive = true,
            FocusMode = FocusModeEnum.All,
            SelectionEnabled = false,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        _text.AddThemeFontOverride("normal_font", UITheme.BodyFont);
        _text.AddThemeFontOverride("bold_font", UITheme.StrongFont);
        _text.AddThemeFontSizeOverride("normal_font_size", UITheme.FontSize(TextRole.Lead));
        _text.AddThemeFontSizeOverride("bold_font_size", UITheme.FontSize(TextRole.Lead));
        _text.AddThemeColorOverride("default_color", UITheme.TextColor);
        _text.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
        frame.AddChild(_text);
    }

    /// <summary>Compose le texte à la première ouverture ; renvoie le contrôle à focaliser.</summary>
    public Control Refresh()
    {
        if (!_built)
        {
            Compose();
            _built = true;
        }
        _text.ScrollToLine(0);
        return _text;
    }

    private void Compose()
    {
        CreditsDataLoader.Credits credits = CreditsDataLoader.Get();
        _text.Clear();

        Heading(Tr("UI_CREDITS_CREATED_BY"));
        Line(credits.CreatedBy, UITheme.TextLight, bold: true);

        foreach (CreditsDataLoader.Section section in credits.Sections)
        {
            Heading(Tr(section.TitleKey));
            if (section.NoteKey.Length > 0)
                Line(Tr(section.NoteKey), UITheme.TextDim);
            foreach (CreditsDataLoader.Entry entry in section.Entries)
            {
                _text.PushBold();
                _text.PushColor(UITheme.TextLight);
                _text.AddText(entry.Name);
                _text.Pop();
                _text.Pop();
                _text.AddText("  —  " + entry.Detail);
                _text.Newline();
            }
        }

        Heading(Tr("UI_CREDITS_LICENSES"));
        Line(Tr("UI_CREDITS_ENGINE_NOTICE"), UITheme.TextDim);
        Block(Engine.GetLicenseText());

        foreach (CreditsDataLoader.License license in credits.Licenses)
        {
            Line(license.Name, UITheme.TextLight, bold: true);
            Block(license.Text);
        }

        AppendEngineComponents();
    }

    /// <summary>Notices des bibliothèques embarquées par Godot (FreeType, HarfBuzz…), puis le texte de chaque licence.</summary>
    private void AppendEngineComponents()
    {
        Heading(Tr("UI_CREDITS_COMPONENTS"));
        foreach (Godot.Collections.Dictionary component in Engine.GetCopyrightInfo())
        {
            Line(component["name"].AsString(), UITheme.TextLight, bold: true);
            foreach (Variant part in component["parts"].AsGodotArray())
            {
                Godot.Collections.Dictionary fields = part.AsGodotDictionary();
                foreach (string copyright in fields["copyright"].AsStringArray())
                    Line("© " + copyright, UITheme.TextColor);
                Line(Tr("UI_CREDITS_LICENSE") + " : " + fields["license"].AsString(), UITheme.TextDim);
            }
            _text.Newline();
        }

        Godot.Collections.Dictionary licenses = Engine.GetLicenseInfo();
        foreach (Variant name in licenses.Keys)
        {
            Line(name.AsString(), UITheme.TextLight, bold: true);
            Block(licenses[name].AsString());
        }
    }

    private void Heading(string text)
    {
        _text.Newline();
        _text.PushFont(UITheme.TitleFont, UITheme.FontSize(TextRole.Title));
        _text.PushColor(UITheme.GoldBright);
        _text.AddText(text);
        _text.Pop();
        _text.Pop();
        _text.Newline();
        _text.Newline();
    }

    private void Line(string text, Color color, bool bold = false)
    {
        if (bold)
            _text.PushBold();
        _text.PushColor(color);
        _text.AddText(text);
        _text.Pop();
        if (bold)
            _text.Pop();
        _text.Newline();
    }

    /// <summary>Texte de licence en plus petit, pour que la page reste lisible malgré sa longueur.</summary>
    private void Block(string text)
    {
        _text.PushFontSize(UITheme.FontSize(TextRole.Body));
        _text.PushColor(UITheme.TextDim);
        _text.AddText(text.Trim());
        _text.Pop();
        _text.Pop();
        _text.Newline();
        _text.Newline();
    }
}
