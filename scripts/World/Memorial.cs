using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Mémorial (plan 17 lot 3B) : stèle du monde d'avant, à raviver. Endormi, il appelle ; on l'active en rassemblant
/// ses éclats ; ravivé, il offre ses services contre de l'Essence. Englouti par le Néant, il est perdu.
/// Le lieu ne porte que son état et son apparence ; <see cref="MemorialDirector"/> mène le reste.
/// </summary>
public partial class Memorial : StaticBody2D, IInteractable
{
    public enum MemorialState { Dormant, Gathering, Awake, Lost }

    // Réveil (plan 24 B2) : les éclats tournent en spirale autour de la stèle, montent et se fondent dans sa colonne.
    private const float OrbitStart = 0.1f;
    private const float OrbitEnd = 0.62f;
    private const float OrbitRadius = 46f;
    private const float OrbitTurns = 1.6f;
    private const float OrbitHeight = 22f;
    private const float OrbitRise = 30f;

    private static readonly List<Memorial> _all = new();
    private static readonly RarityColors ColumnColors = RarityPalette.Colors("memorial");

    private MemorialConfig _config;
    private Sprite2D _sprite;
    private LightColumn _column;
    private Label _status;
    private LightColumn _revivalColumn;
    private readonly List<Sprite2D> _revivalShards = new();
    private EventBus _eventBus;
    private readonly Dictionary<string, int> _serviceUses = new();

    public static IReadOnlyList<Memorial> All => _all;

    public MemorialState State { get; private set; } = MemorialState.Dormant;

    public bool CanInteract => State is MemorialState.Dormant or MemorialState.Awake;
    public Vector2 InteractPosition => GlobalPosition;
    public Vector2 PromptPosition => GlobalPosition + new Vector2(0f, _sprite.Offset.Y - 3f);
    public string PromptVerbKey => State == MemorialState.Dormant ? "MEMORIAL_WAKE_PROMPT" : "MEMORIAL_SERVICES_PROMPT";
    public float HoldTime => State == MemorialState.Dormant ? _config.HoldTime : 0f;
    public Color GaugeColor => ColumnColors.Main;

    public void Initialize(MemorialConfig config)
    {
        _config = config;
        CollisionLayer = 4;
        CollisionMask = 0;

        _column = new LightColumn { Name = "LightColumn" };
        AddChild(_column);
        _column.Configure(ColumnColors, 150f, 2f);

        _sprite = new Sprite2D { Name = "Sprite", TextureFilter = CanvasItem.TextureFilterEnum.Nearest, Centered = false };
        AddChild(_sprite);
        Texture2D dormant = GD.Load<Texture2D>($"res://{config.SpriteDormant}");
        ShowTexture(dormant);
        if (PropManifest.TryGet(dormant, out PropManifest.Entry entry) && entry.Footprint.Length >= 3)
        {
            AddChild(new CollisionShape2D { Shape = new ConvexPolygonShape2D { Points = entry.Footprint } });
            Sprite2D shadow = PropShadow.Create(entry.Footprint);
            AddChild(shadow);
            MoveChild(shadow, 0);
        }

        // Texte à lire au-dessus des entités : compte des éclats et temps restant pendant le rassemblement.
        _status = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Position = new Vector2(-60f, _sprite.Offset.Y - 18f),
            Size = new Vector2(120f, 12f),
            ZIndex = 20,
            Visible = false,
            TextureFilter = TextureFilterEnum.Nearest,
        };
        _status.AddThemeFontOverride("font", GD.Load<Font>("res://assets/fonts/pixel-operator/PixelOperator8.ttf"));
        _status.AddThemeFontSizeOverride("font_size", 8);
        _status.AddThemeColorOverride("font_color", ColumnColors.Light);
        AddChild(_status);
    }

    public override void _EnterTree()
    {
        _all.Add(this);
        if (CanInteract)
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

    public void Interact(Player player)
    {
        _eventBus.EmitSignal(EventBus.SignalName.MemorialInteracted, this);
    }

    public void SetState(MemorialState state)
    {
        State = state;
        _status.Visible = state == MemorialState.Gathering;
        // Pendant le rassemblement, les éclats portent la lumière ; celle du Mémorial masquerait son compteur.
        _column.Visible = state == MemorialState.Dormant;
        if (CanInteract)
            Interactables.Register(this);
        else
            Interactables.Unregister(this);

        switch (state)
        {
            case MemorialState.Awake:
                ShowTexture(GD.Load<Texture2D>($"res://{_config.SpriteAwake}"));
                _sprite.Modulate = new Color(2.5f, 2.5f, 2.5f);
                // L'éclair se voit pendant le réveil, monde figé, et sous l'écran des bénédictions.
                CreateTween().SetPauseMode(Tween.TweenPauseMode.Process).TweenProperty(_sprite, "modulate", Colors.White, 0.6f);
                break;
            case MemorialState.Lost:
                CreateTween().TweenProperty(_sprite, "modulate", new Color(0.35f, 0.3f, 0.45f, 0.5f), 1.2f);
                break;
        }
    }

    public void ShowStatus(string text) => _status.Text = text;

    /// <summary>
    /// Part du Mémorial dans son réveil, à l'avancement <paramref name="progress"/> (0 à 1) : une colonne élargie
    /// monte, les <paramref name="shards"/> éclats ramassés tournent autour de la stèle et s'y fondent.
    /// Rend vrai une fois les éclats fondus.
    /// </summary>
    public bool ShowRevival(float progress, int shards, Texture2D shardTexture)
    {
        if (_revivalColumn == null)
            BeginRevival(shards, shardTexture);

        _revivalColumn.Modulate = new Color(1f, 1f, 1f, Mathf.SmoothStep(0.05f, 0.35f, progress));
        float orbit = Mathf.Clamp((progress - OrbitStart) / (OrbitEnd - OrbitStart), 0f, 1f);
        // Lent au départ, de plus en plus serré : la spirale se referme sur la colonne.
        float pull = orbit * orbit;
        for (int i = 0; i < _revivalShards.Count; i++)
        {
            Sprite2D shard = _revivalShards[i];
            shard.Visible = progress >= OrbitStart && orbit < 1f;
            float angle = Mathf.Tau * (i / (float)_revivalShards.Count + OrbitTurns * pull);
            float radius = OrbitRadius * (1f - pull);
            // Orbite aplatie de moitié : elle est posée au sol, en vue iso.
            shard.Position = new Vector2(Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius * 0.5f - OrbitHeight - OrbitRise * pull).Round();
            float appear = Mathf.Clamp((progress - OrbitStart) / 0.06f, 0f, 1f);
            shard.Modulate = new Color(1f, 1f, 1f, appear * (1f - Mathf.SmoothStep(0.75f, 1f, orbit)));
        }
        return orbit >= 1f;
    }

    /// <summary>Fin du réveil : les éclats et la colonne élargie disparaissent sous l'écran des bénédictions.</summary>
    public void EndRevival()
    {
        _revivalColumn?.QueueFree();
        _revivalColumn = null;
        foreach (Sprite2D shard in _revivalShards)
            shard.QueueFree();
        _revivalShards.Clear();
    }

    private void BeginRevival(int shards, Texture2D shardTexture)
    {
        _status.Visible = false;
        _revivalColumn = new LightColumn { Name = "RevivalColumn" };
        AddChild(_revivalColumn);
        _revivalColumn.Configure(ColumnColors, 240f, 3f, 20f);
        for (int i = 0; i < shards; i++)
        {
            Sprite2D shard = new()
            {
                Name = $"RevivalShard{i + 1}",
                Texture = shardTexture,
                TextureFilter = TextureFilterEnum.Nearest,
                // Au-dessus de la stèle et des entités, quel que soit leur tri.
                ZIndex = 1,
                Visible = false,
            };
            AddChild(shard);
            _revivalShards.Add(shard);
        }
    }

    /// <summary>Nombre d'usages d'un service à ce Mémorial : chaque usage en augmente le prix.</summary>
    public int ServiceUses(string service) => _serviceUses.GetValueOrDefault(service);

    public void RecordServiceUse(string service) => _serviceUses[service] = ServiceUses(service) + 1;

    private void ShowTexture(Texture2D texture)
    {
        _sprite.Texture = texture;
        Vector2 pivot = PropManifest.TryGet(texture, out PropManifest.Entry entry)
            ? entry.Pivot
            : new Vector2(texture.GetWidth() * 0.5f, texture.GetHeight());
        _sprite.Offset = -pivot.Round();
    }
}
