using Godot;

namespace Vestiges.Core;

/// <summary>Changement de terre (plan 51) : le joueur garde tout ce qu'il porte et repart du départ de la terre suivante.</summary>
public partial class Player
{
    /// <summary>Pose le joueur sur la terre neuve : mouvement suspendu, caméra recalée sans glisser, phase du sol relue.</summary>
    public void ArriveOnLand(Vector2 position)
    {
        SuspendMovement();
        GlobalPosition = position;
        GetNode<Camera2D>("Camera").ResetSmoothing();
        CacheWorldSetup();
    }
}
