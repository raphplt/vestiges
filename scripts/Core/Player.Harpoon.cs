using Godot;
using Vestiges.Combat;

namespace Vestiges.Core;

public partial class Player
{
    public HarpoonTether Harpoon { get; private set; }
    internal bool CanUseHarpoon => !_isDead && _gameManager.CurrentState == GameManager.GameState.Run
        && !GetTree().Paused && !Mobility.IsDashing && !Mobility.IsDashStep && Mobility.State != MobilityState.Hurt;
    internal float HarpoonTravelFactor => _slowFactor * _erasurePenalty.Speed;

    /// <summary>La traction ne traverse ni eau de carte ni Néant ; la marée conserve ses effets indépendants.</summary>
    internal Vector2 ClampHarpoonTravel(Vector2 origin, Vector2 displacement) => ClampMobilityTravel(origin, displacement, true);
}
