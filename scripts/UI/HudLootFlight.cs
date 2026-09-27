using System;
using Godot;
using Vestiges.Core;

namespace Vestiges.UI;

/// <summary>
/// Butin qui vole vers le HUD (plan 02 J3) : l'icône d'une arme ramassée au sol part de sa place dans le monde et
/// rejoint sa case de la barre d'armes, qui s'allume à l'arrivée. La case reste vide pendant le vol.
/// </summary>
public partial class HudLootFlight : Control
{
    private const float FlightSec = 0.45f;
    private const float StartScale = 1.6f;

    private readonly Func<string, Control> _slotIconOf;
    private EventBus _eventBus;

    public HudLootFlight(Func<string, Control> slotIconOf)
    {
        _slotIconOf = slotIconOf;
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public override void _Ready()
    {
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.WeaponPickedUp += OnWeaponPickedUp;
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
            _eventBus.WeaponPickedUp -= OnWeaponPickedUp;
    }

    private void OnWeaponPickedUp(string weaponId, Vector2 worldPosition)
    {
        // La barre se met à jour dans la même frame : on attend qu'elle ait posé l'icône dans sa case.
        Callable.From(() => Fly(weaponId, worldPosition)).CallDeferred();
    }

    private void Fly(string weaponId, Vector2 worldPosition)
    {
        if (_slotIconOf(weaponId) is not TextureRect slotIcon || slotIcon.Texture == null)
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
