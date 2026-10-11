using System;
using Godot;
using Vestiges.Core;

namespace Vestiges.UI;

/// <summary>
/// Butin qui vole vers le HUD (plan 02 J3) : l'icône d'une arme ou d'un objet ramassé au sol part de sa place dans le
/// monde et rejoint sa case (barre d'armes, sac), qui s'allume à l'arrivée. La case reste vide pendant le vol.
/// </summary>
public partial class HudLootFlight : Control
{
    private const float FlightSec = 0.45f;
    private const float StartScale = 1.6f;

    private readonly Func<string, Control> _slotIconOf;
    private readonly Func<string, Control> _itemIconOf;
    private EventBus _eventBus;

    public HudLootFlight(Func<string, Control> slotIconOf, Func<string, Control> itemIconOf)
    {
        _slotIconOf = slotIconOf;
        _itemIconOf = itemIconOf;
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public override void _Ready()
    {
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.WeaponPickedUp += OnWeaponPickedUp;
        _eventBus.ItemPickedUp += OnItemPickedUp;
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
        {
            _eventBus.WeaponPickedUp -= OnWeaponPickedUp;
            _eventBus.ItemPickedUp -= OnItemPickedUp;
        }
    }

    private void OnWeaponPickedUp(string weaponId, Vector2 worldPosition)
    {
        // La barre se met à jour dans la même frame : on attend qu'elle ait posé l'icône dans sa case.
        Callable.From(() => Fly(_slotIconOf(weaponId), worldPosition)).CallDeferred();
    }

    private void OnItemPickedUp(string itemId, Vector2 worldPosition) =>
        Callable.From(() => Fly(_itemIconOf(itemId), worldPosition)).CallDeferred();

    private void Fly(Control target, Vector2 worldPosition)
    {
        if (target is not TextureRect slotIcon || slotIcon.Texture == null)
            return;
        // Tout en coordonnées locales : la racine du HUD est mise à l'échelle de l'écran.
        Transform2D toLocal = GetGlobalTransform().AffineInverse();
        Vector2 targetPosition = toLocal * slotIcon.GetGlobalRect().Position;
        Vector2 targetSize = slotIcon.Size;
        Transform2D layer = GetCanvasLayerNode()?.Transform ?? Transform2D.Identity;
        Vector2 start = toLocal * (layer.AffineInverse() * (GetViewport().GetCanvasTransform() * worldPosition));
        Vector2 startSize = targetSize * StartScale;

        TextureRect flying = new()
        {
            Texture = slotIcon.Texture,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = TextureFilterEnum.Nearest,
            MouseFilter = MouseFilterEnum.Ignore,
            Size = startSize,
            Position = start - startSize / 2f,
        };
        AddChild(flying);
        slotIcon.Modulate = Colors.Transparent;

        Tween tween = flying.CreateTween().SetParallel();
        tween.TweenProperty(flying, "position", targetPosition, FlightSec)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        tween.TweenProperty(flying, "size", targetSize, FlightSec)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
        tween.Chain().TweenCallback(Callable.From(() =>
        {
            flying.QueueFree();
            if (!IsInstanceValid(slotIcon))
                return;
            // Arrivée : la case s'allume puis revient.
            slotIcon.Modulate = new Color(2f, 2f, 2f);
            slotIcon.CreateTween().TweenProperty(slotIcon, "modulate", Colors.White, 0.25f);
        }));
    }
}
