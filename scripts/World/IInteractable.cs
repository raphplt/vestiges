using Godot;
using Vestiges.Core;

namespace Vestiges.World;

/// <summary>
/// Lieu du monde qu'on active en restant sur la touche d'interaction (coffre, Mémorial, Faille) : invite au-dessus,
/// jauge pendant <see cref="HoldTime"/>, puis <see cref="Interact"/>. Les lieux actifs s'inscrivent dans
/// <see cref="Interactables"/>, pour que l'interaction les trouve sans recherche par groupe.
/// </summary>
public interface IInteractable
{
    bool CanInteract { get; }
    Vector2 InteractPosition { get; }
    /// <summary>Point au-dessus du sprite, pour l'invite et la jauge.</summary>
    Vector2 PromptPosition { get; }
    /// <summary>Clé de traduction du verbe de l'invite (« Ouvrir », « Raviver »…).</summary>
    string PromptVerbKey { get; }
    float HoldTime { get; }
    Color GaugeColor { get; }
    void Interact(Player player);
}
