using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Atelier (plan 22 C2) : établi abandonné où l'Essence devient de la puissance d'arme. Toujours ouvert tant que le
/// Néant ne l'a pas englouti. Le lieu ne porte que son apparence, ses usages et sa Trempe offerte ;
/// <see cref="WorkshopDirector"/> mène les services.
/// </summary>
public partial class Workshop : StaticBody2D, IInteractable
{
    private static readonly List<Workshop> _all = new();
    private static readonly RarityColors ColumnColors = RarityPalette.Colors("workshop");

    private Sprite2D _sprite;
    private LightColumn _column;
    private EventBus _eventBus;
    private readonly Dictionary<string, int> _serviceUses = new();

    public static IReadOnlyList<Workshop> All => _all;

    /// <summary>La Trempe gratuite a été donnée : seule la première visite la reçoit.</summary>
    public bool Visited { get; private set; }
    public bool IsLost { get; private set; }

    public bool CanInteract => !IsLost;
    public Vector2 InteractPosition => GlobalPosition;
    public Vector2 PromptPosition => GlobalPosition + new Vector2(0f, _sprite.Offset.Y - 3f);
    public string PromptVerbKey => "WORKSHOP_PROMPT";
    public float HoldTime => 0f;
    public Color GaugeColor => ColumnColors.Main;

    public void Initialize(WorkshopConfig config)
    {
        CollisionLayer = 4;
        CollisionMask = 0;

        _column = new LightColumn { Name = "LightColumn" };
        AddChild(_column);
        _column.Configure(ColumnColors, 130f, 1.5f);

        _sprite = new Sprite2D { Name = "Sprite", TextureFilter = CanvasItem.TextureFilterEnum.Nearest, Centered = false };
        AddChild(_sprite);
        Texture2D texture = GD.Load<Texture2D>($"res://{config.Sprite}");
        _sprite.Texture = texture;
        bool known = PropManifest.TryGet(texture, out PropManifest.Entry entry);
        Vector2 pivot = known ? entry.Pivot : new Vector2(texture.GetWidth() * 0.5f, texture.GetHeight());
        _sprite.Offset = -pivot.Round();
        if (known && entry.Footprint.Length >= 3)
        {
            AddChild(new CollisionShape2D { Shape = new ConvexPolygonShape2D { Points = entry.Footprint } });
            ObstacleField.Add(this, entry.Footprint);
            Sprite2D shadow = PropShadow.Create(entry.Footprint);
            AddChild(shadow);
            MoveChild(shadow, 0);
        }
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
        _eventBus.EmitSignal(EventBus.SignalName.WorkshopInteracted, this);
    }

    /// <summary>Première visite : vrai une seule fois, le directeur donne alors la Trempe.</summary>
    public bool MarkVisited()
    {
        if (Visited)
            return false;
        Visited = true;
        return true;
    }

    public int ServiceUses(string service) => _serviceUses.GetValueOrDefault(service);

    public void RecordServiceUse(string service) => _serviceUses[service] = ServiceUses(service) + 1;

    /// <summary>Englouti par le Néant : l'établi s'éteint et ne sert plus.</summary>
    public void MarkLost()
    {
        if (IsLost)
            return;
        IsLost = true;
        Interactables.Unregister(this);
        _column.Visible = false;
        _sprite.Modulate = new Color(0.45f, 0.42f, 0.5f, 0.7f);
    }
}
