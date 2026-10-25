using Godot;
using Vestiges.Infrastructure;
using Vestiges.World;

namespace Vestiges.Combat;

public partial class Enemy
{
    internal ulong HarpoonOwner { get; set; }
    public bool IsHarpoonHeavy { get; private set; }
    public bool IsHarpoonAnchored => EnemyGrammar.IsHeavy(Tier) || Boss != null || VariantId == "champion";

    /// <summary>La traction balaie le trajet par petits pas ; la marche normale conserve sa résolution actuelle.</summary>
    internal Vector2 MoveByHarpoon(Vector2 displacement)
    {
        Vector2 before = GlobalPosition;
        Vector2 bounded = displacement.LimitLength(512f);
        int steps = Mathf.Max(1, Mathf.CeilToInt(bounded.Length() / 2f));
        Vector2 step = bounded / steps;
        for (int i = 0; i < steps; i++)
        {
            Vector2 next = GlobalPosition + step;
            // Arrêt au contact : ne pas glisser vers un sol non contrôlé par le propriétaire de la traction.
            if (ObstacleField.Resolve(ref next, BodyRadius)) break;
            GlobalPosition = next;
        }
        CrowdIndex.MarkMoved();
        return GlobalPosition - before;
    }
}
