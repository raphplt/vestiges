using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.UI;

namespace Vestiges.World;

/// <summary>
/// Objet posé au sol (plan 35 I3) : lâché par une élite ou un Souverain. Ramassé de lui-même s'il reste une place dans
/// le sac ; sinon c'est un lieu activable : l'invite maintenue ouvre l'écran d'échange (<see cref="ItemExchange"/>), en
/// pause. Il s'efface avec le temps, comme une arme laissée au sol.
/// </summary>
public partial class ItemPickup : Area2D, IInteractable
{
    private const float PickupRadius = 26f;
    private const float BobAmplitude = 3f;
    private const float BobSpeed = 2.2f;
    private const float DespawnTime = 120f;
    private const float RingRadius = 12f;
    private const float ExchangeHoldSec = 0.35f;

    private PassiveSouvenirData _item;
    private Node2D _visualRoot;
    private Sprite2D _visual;
    private float _bobTimer;
    private bool _collected;
    private ItemExchange _exchange;
    private string _prompt;

    public string ItemId => _item?.Id;
    public bool CanInteract => !_collected;
    public Vector2 InteractPosition => GlobalPosition;
    public Vector2 PromptPosition => GlobalPosition + new Vector2(0f, -24f);
    // Lu à chaque pas physique tant que le joueur est à portée : composé une fois, l'objet ne change pas.
    public string PromptVerbKey => _prompt ??= string.Format(Tr("ITEM_EXCHANGE_PROMPT"), _item?.Name ?? "");
    public float HoldTime => ExchangeHoldSec;
    public Color GaugeColor => UITheme.CyanEssence;

    public void Initialize(PassiveSouvenirData item, Vector2 position, ItemExchange exchange)
    {
        _item = item;
        _exchange = exchange;
        GlobalPosition = position;
    }

    public override void _EnterTree() => Interactables.Register(this);

    public override void _ExitTree() => Interactables.Unregister(this);

    public override void _Ready()
    {
        CollisionLayer = 0;
        CollisionMask = 1;
        CollisionShape2D shape = new() { Shape = new CircleShape2D { Radius = PickupRadius } };
        AddChild(shape);
        PixelGroundRing ring = new() { Name = "Ring" };
        AddChild(ring);
        ring.Show(UITheme.CyanEssence, RingRadius);
        _visualRoot = new Node2D();
        AddChild(_visualRoot);
        _visual = new Sprite2D
        {
            Texture = !string.IsNullOrEmpty(_item?.Icon) && ResourceLoader.Exists(_item.Icon) ? GD.Load<Texture2D>(_item.Icon) : null,
            TextureFilter = TextureFilterEnum.Nearest,
            Position = new Vector2(0f, -10f),
        };
        _visualRoot.AddChild(_visual);
        BodyEntered += OnBodyEntered;
        GetTree().CreateTimer(DespawnTime, false).Timeout += () =>
        {
            if (!_collected && IsInstanceValid(this))
                Despawn();
        };
        AddToGroup("item_pickups");
    }

    public override void _Process(double delta)
    {
        _bobTimer += (float)delta * BobSpeed;
        _visualRoot.Position = new Vector2(0, Mathf.Round(Mathf.Sin(_bobTimer) * BobAmplitude));
    }

    /// <summary>
    /// Invite tenue : prendre s'il y a eu de la place entre-temps, sinon proposer l'échange. Un objet déjà à son maximum
    /// d'exemplaires est refusé : l'objet rougit et reste au sol.
    /// </summary>
    public void Interact(Player player)
    {
        if (_collected || _item == null)
            return;
        if (player.CanTakeItem(_item.Id))
        {
            Take(player);
            return;
        }
        if (_exchange == null || !_exchange.Open(player, _item, () => Take(player)))
            FlashRefused();
    }

    private void OnBodyEntered(Node2D body)
    {
        if (!_collected && body is Player player && player.CanTakeItem(_item.Id))
            CallDeferred(MethodName.Take, player);
    }

    private bool Take(Player player)
    {
        if (_collected || !IsInstanceValid(player) || !player.AddItemCopy(_item.Id))
            return false;
        _collected = true;
        Interactables.Unregister(this);
        GetNodeOrNull<EventBus>("/root/EventBus")?.EmitSignal(EventBus.SignalName.LootReceived, "object", _item.Id, 1);
        QueueFree();
        return true;
    }

    private void FlashRefused()
    {
        _visual.Modulate = new Color(1f, 0.3f, 0.2f);
        CreateTween().TweenProperty(_visual, "modulate", Colors.White, 0.3f).SetDelay(0.1f);
    }

    private void Despawn()
    {
        _collected = true;
        Interactables.Unregister(this);
        Tween tween = CreateTween();
        tween.SetParallel();
        tween.TweenProperty(this, "scale", Vector2.Zero, 0.4f);
        tween.TweenProperty(this, "modulate:a", 0f, 0.4f);
        tween.Chain().TweenCallback(Callable.From(QueueFree));
    }
}
