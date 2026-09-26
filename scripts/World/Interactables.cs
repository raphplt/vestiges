using System.Collections.Generic;

namespace Vestiges.World;

/// <summary>Registre des lieux activables présents dans la scène (voir <see cref="IInteractable"/>).</summary>
public static class Interactables
{
    private static readonly List<IInteractable> _all = new();

    public static IReadOnlyList<IInteractable> All => _all;

    public static void Register(IInteractable interactable)
    {
        if (!_all.Contains(interactable))
            _all.Add(interactable);
    }

    public static void Unregister(IInteractable interactable) => _all.Remove(interactable);
}
