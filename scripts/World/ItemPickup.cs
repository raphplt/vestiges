using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.UI;

namespace Vestiges.World;

/// <summary>
/// Objet posé au sol (plan 35 I3) : lâché par une élite ou un Souverain. Ramassé de lui-même s'il reste une place dans
/// le sac ; sinon il attend au sol et l'invite renvoie au Puits, seul endroit où l'on oublie un objet (plan 44). Il
/// s'efface avec le temps, comme une arme laissée au sol.
/// </summary>
public partial class ItemPickup : Area2D, IInteractable
{
    private const float PickupRadius = 26f;
    private const float BobAmplitude = 3f;
    private const float BobSpeed = 2.2f;
    private const float DespawnTime = 120f;
    private const float RingRadius = 16f;
    private const float PickupHoldSec = 0.35f;

    private PassiveSouvenirData _item;
    private Node2D _visualRoot;
    private Sprite2D _visual;
    private float _bobTimer;
    private bool _collected;
    private EventBus _eventBus;
    private string _prompt;
    private GroupCache _groups;
    private ErasureManager _erasure;
    private float _age;
    private float _lifetimeCheck;
    private bool _waiting;

    public string ItemId => _item?.Id;
    public bool CanInteract => !_collected;
    public Vector2 InteractPosition => GlobalPosition;
    public Vector2 PromptPosition => GlobalPosition + new Vector2(0f, -24f);
    // Lu à chaque pas physique tant que le joueur est à portée : gardé jusqu'au prochain changement du sac.
    public string PromptVerbKey => _prompt ??= string.Format(Tr(PromptKey()), _item?.Name ?? "");
    public float HoldTime => PickupHoldSec;
    public Color GaugeColor => RarityPalette.Main(_item?.Rarity ?? "common");

    public void Initialize(PassiveSouvenirData item, Vector2 position)
    {
        _item = item;
        GlobalPosition = position;
    }

    public override void _EnterTree()
    {
        Interactables.Register(this);
        _eventBus = GetNodeOrNull<EventBus>("/root/EventBus");
        if (_eventBus != null)
            _eventBus.PassiveSouvenirSlotsChanged += InvalidatePrompt;
    }

    public override void _ExitTree()
    {
        Interactables.Unregister(this);
        if (_eventBus != null)
            _eventBus.PassiveSouvenirSlotsChanged -= InvalidatePrompt;
    }

    private void InvalidatePrompt() => _prompt = null;

    private string PromptKey()
    {
        if (_groups?.GetPlayer() is not Player player || _item == null || player.CanTakeItem(_item.Id))
            return "ITEM_PICKUP_PROMPT";
        return player.IsItemAtMax(_item.Id) ? "ITEM_AT_MAX_PROMPT" : "ITEM_BAG_FULL_PROMPT";
    }

    public override void _Ready()
    {
        CollisionLayer = 0;
        CollisionMask = 1;
        CollisionShape2D shape = new() { Shape = new CircleShape2D { Radius = PickupRadius } };
        AddChild(shape);
        PixelGroundRing ring = new() { Name = "Ring", RimAlpha = 0.95f, FillAlpha = 0.36f };
        AddChild(ring);
        ring.Show(GaugeColor, RingRadius);
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
        _groups = GetNodeOrNull<GroupCache>("/root/GroupCache");
        _erasure = GetTree().CurrentScene?.GetNodeOrNull<ErasureManager>("ErasureManager");
        AddToGroup("item_pickups");
    }

    public override void _Process(double delta)
    {
        if (_collected) return;
        // Tant qu'il ne peut pas entrer dans le sac, l'objet attend : on n'a que le Puits pour lui faire une place.
        if (!_waiting)
            _age += (float)delta;
        _lifetimeCheck -= (float)delta;
        if (_lifetimeCheck <= 0f)
        {
            _lifetimeCheck = .25f;
            _waiting = _groups?.GetPlayer() is Player player && !player.CanTakeItem(_item?.Id ?? "");
            bool erased = _erasure != null && GodotObject.IsInstanceValid(_erasure)
                && _erasure.GetZonePhaseAt(GlobalPosition) == ErasureManager.ErasureZonePhase.Void;
            if (erased || _age >= DespawnTime + WorldItemEffects.ExtraItemSeconds(_groups?.GetPlayer() as Player))
            { Despawn(); return; }
        }
        _bobTimer += (float)delta * BobSpeed;
        _visualRoot.Position = new Vector2(0, Mathf.Round(Mathf.Sin(_bobTimer) * BobAmplitude));
    }

    /// <summary>
    /// Invite tenue : prendre s'il y a eu de la place entre-temps. Sinon (sac plein, ou objet à son maximum
    /// d'exemplaires) l'objet rougit et reste au sol.
    /// </summary>
    public void Interact(Player player)
    {
        if (_collected || _item == null)
            return;
        if (player.CanTakeItem(_item.Id))
            Take(player);
        else
            FlashRefused();
    }

    private void OnBodyEntered(Node2D body)
    {
        if (!_collected && body is Player player && player.CanTakeItem(_item.Id))
            CallDeferred(MethodName.Take, player);
    }

    private void Take(Player player)
    {
        if (_collected || !IsInstanceValid(player) || !player.AddItemCopy(_item.Id))
            return;
        _collected = true;
        Interactables.Unregister(this);
        _eventBus?.EmitSignal(EventBus.SignalName.LootReceived, "object", _item.Id, 1);
        _eventBus?.EmitSignal(EventBus.SignalName.ItemPickedUp, _item.Id, _visual.GlobalPosition);
        AudioManager.PlayUI("sfx_souvenir_trouve");
        QueueFree();
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
