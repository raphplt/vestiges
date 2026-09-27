using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Faille (plan 17 lot 3C) : trace de l'Effacement où l'on peut arracher une grande amélioration contre un Oubli.
/// Ouverte, elle palpite et appelle ; refermée (offre acceptée), elle n'est plus qu'une cicatrice au sol.
/// Le lieu ne porte que son état et son apparence ; <see cref="RiftDirector"/> mène les offres.
/// </summary>
public partial class Rift : Node2D, IInteractable
{
    private static readonly List<Rift> _all = new();
    private static readonly RarityColors ColumnColors = RarityPalette.Colors("rift");

    private RiftConfig _config;
    private Sprite2D _sprite;
    private LightColumn _column;
    private EventBus _eventBus;
    private float _time;

    public static IReadOnlyList<Rift> All => _all;

    public bool IsOpen { get; private set; } = true;

    public bool CanInteract => IsOpen;
    public Vector2 InteractPosition => GlobalPosition;
    public Vector2 PromptPosition => GlobalPosition + new Vector2(0f, _sprite.Offset.Y - 6f);
    public string PromptVerbKey => "RIFT_PROMPT";
    public float HoldTime => _config.HoldTime;
    public Color GaugeColor => ColumnColors.Main;

    public void Initialize(RiftConfig config)
    {
        _config = config;
        // Au ras du sol : le joueur marche dessus, les créatures passent devant.
        ZIndex = -1;

        _column = new LightColumn { Name = "LightColumn" };
        AddChild(_column);
        _column.Configure(ColumnColors, 110f, 1f, 14f);

        _sprite = new Sprite2D { Name = "Sprite", TextureFilter = CanvasItem.TextureFilterEnum.Nearest, Centered = false };
        AddChild(_sprite);
        ShowTexture(GD.Load<Texture2D>($"res://{config.SpriteOpen}"));
        _time = GD.Randf() * Mathf.Tau;
    }

    public override void _EnterTree()
    {
        _all.Add(this);
        if (IsOpen)
            Interactables.Register(this);
    }

    public override void _ExitTree()
    {
        _all.Remove(this);
        Interactables.Unregister(this);
    }

    public override void _Ready()
    {
        _eventBus = GetNode<EventBus>("/root/EventBus");
    }

    public override void _Process(double delta)
    {
        if (!IsOpen)
            return;
        _time += (float)delta;
        float pulse = 0.85f + 0.15f * Mathf.Sin(_time * 3f);
        _sprite.Modulate = new Color(pulse, pulse, pulse);
    }

    public void Interact(Player player)
    {
        _eventBus.EmitSignal(EventBus.SignalName.RiftInteracted, this);
    }

    /// <summary>Offre acceptée : la Faille se referme.</summary>
    public void Close()
    {
        IsOpen = false;
        Interactables.Unregister(this);
        _column.Visible = false;
        _sprite.Modulate = Colors.White;
        ShowTexture(GD.Load<Texture2D>($"res://{_config.SpriteClosed}"));
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
