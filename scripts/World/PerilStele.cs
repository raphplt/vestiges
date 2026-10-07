using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;
using Vestiges.UI;

namespace Vestiges.World;

/// <summary>
/// Stèle du Péril (plan 28 P2) : le joueur qui la réveille monte le Péril de sa run, pour de bon, et rien d'autre
/// (DECISIONS §78 : le Péril ne paie que par plus d'ennemis). Allumée, sa fente rouge palpite ; éteinte, elle reste
/// comme un repère de ce qu'on a choisi.
/// </summary>
public partial class PerilStele : Node2D, IInteractable
{
    private const float LineSeconds = 2.5f;
    private const float LineRise = 22f;
    private const int LineZIndex = 30;

    private static readonly List<PerilStele> _all = new();

    private PerilSteleConfig _config;
    private Sprite2D _sprite;
    private Label _line;
    private float _time;
    private float _lineTime = -1f;

    public static IReadOnlyList<PerilStele> All => _all;

    public bool IsLit { get; private set; } = true;

    public bool CanInteract => IsLit;
    public Vector2 InteractPosition => GlobalPosition;
    public Vector2 PromptPosition => GlobalPosition + new Vector2(0f, _sprite.Offset.Y - 6f);
    public string PromptVerbKey => "PERIL_STELE_PROMPT";
    public float HoldTime => _config.HoldTime;
    public Color GaugeColor => PlayerSheet.PerilColor;

    public void Initialize(PerilSteleConfig config)
    {
        _config = config;
        _sprite = new Sprite2D { Name = "Sprite", TextureFilter = CanvasItem.TextureFilterEnum.Nearest, Centered = false };
        AddChild(_sprite);
        ShowTexture(GD.Load<Texture2D>($"res://{config.SpriteLit}"));
        _time = GD.Randf() * Mathf.Tau;
    }

    public override void _EnterTree()
    {
        _all.Add(this);
        if (IsLit)
            Interactables.Register(this);
    }

    public override void _ExitTree()
    {
        _all.Remove(this);
        Interactables.Unregister(this);
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        if (IsLit)
        {
            _time += dt;
            float pulse = 0.9f + 0.18f * Mathf.Sin(_time * 2.2f);
            _sprite.Modulate = new Color(pulse, pulse, pulse);
            return;
        }
        _lineTime += dt;
        float progress = _lineTime / LineSeconds;
        if (progress >= 1f)
        {
            _line.Visible = false;
            SetProcess(false);
            return;
        }
        _line.Position = new Vector2(-_line.Size.X * _line.Scale.X * 0.5f, _sprite.Offset.Y - 20f - LineRise * progress);
        _line.Modulate = new Color(1f, 1f, 1f, progress < 0.7f ? 1f : (1f - progress) / 0.3f);
    }

    public void Interact(Player player)
    {
        if (!IsLit || PerilManager.Current == null)
            return;
        IsLit = false;
        Interactables.Unregister(this);
        _sprite.Modulate = Colors.White;
        ShowTexture(GD.Load<Texture2D>($"res://{_config.SpriteSpent}"));
        PerilManager.Current.AddPeril(_config.Peril);
        AudioManager.Play("sfx_clock_strike", 0f, -4f, 0.6f);
        ShowLine(string.Format(Tr("PERIL_LINE"), _config.Peril));
    }

    private void ShowLine(string text)
    {
        _line = new Label { ZIndex = LineZIndex, HorizontalAlignment = HorizontalAlignment.Center, Scale = Vector2.One * 0.5f, Text = text };
        _line.AddThemeFontOverride("font", GD.Load<Font>("res://assets/fonts/saira/SairaSemiCondensed-SemiBold.ttf"));
        _line.AddThemeFontSizeOverride("font_size", 22);
        _line.AddThemeColorOverride("font_color", PlayerSheet.PerilColor);
        _line.AddThemeColorOverride("font_outline_color", new Color(0.12f, 0.1f, 0.1f, 0.85f));
        _line.AddThemeConstantOverride("outline_size", 6);
        AddChild(_line);
        _line.ResetSize();
        _lineTime = 0f;
    }

    private void ShowTexture(Texture2D texture)
    {
        _sprite.Texture = texture;
        Vector2 pivot = PropManifest.TryGet(texture, out PropManifest.Entry entry)
            ? entry.Pivot
            : new Vector2(texture.GetWidth() * 0.5f, texture.GetHeight());
        _sprite.Offset = -pivot.Round();
    }
}
