using Godot;

namespace Vestiges.Spawn;

/// <summary>
/// Choisit une position d'apparition juste au-delà du cadre visible de la caméra.
/// Un anneau calé sur le rectangle de l'écran fait entrer les ennemis rapidement dans le champ,
/// quelle que soit la direction ; le biais avant nourrit la route du joueur plutôt que ce qu'il laisse derrière lui.
/// </summary>
public sealed class SpawnPositionPicker
{
    public float MarginMin { get; set; } = 40f;
    public float MarginMax { get; set; } = 140f;
    /// <summary>Probabilité (0–1) de viser le demi-plan vers lequel le joueur se déplace.</summary>
    public float ForwardBias { get; set; } = 0.5f;
    public float ForwardArcDegrees { get; set; } = 140f;

    /// <param name="marginScale">Rapproche l'anneau pendant les phases intenses (crise, fin de run).</param>
    public Vector2 Pick(Vector2 center, Vector2 viewHalfExtents, Vector2 moveDirection, float marginScale)
    {
        float angle;
        if (moveDirection.LengthSquared() > 0.01f && GD.Randf() < ForwardBias)
        {
            float halfArc = Mathf.DegToRad(ForwardArcDegrees) * 0.5f;
            angle = moveDirection.Angle() + (float)GD.RandRange(-halfArc, halfArc);
        }
        else
        {
            angle = (float)GD.RandRange(0, Mathf.Tau);
        }

        Vector2 direction = Vector2.FromAngle(angle);
        float edge = DistanceToRectEdge(direction, viewHalfExtents);
        float margin = (float)GD.RandRange(MarginMin, MarginMax) * marginScale;
        return center + direction * (edge + margin);
    }

    private static float DistanceToRectEdge(Vector2 direction, Vector2 halfExtents)
    {
        float tx = Mathf.Abs(direction.X) > 0.0001f ? halfExtents.X / Mathf.Abs(direction.X) : float.MaxValue;
        float ty = Mathf.Abs(direction.Y) > 0.0001f ? halfExtents.Y / Mathf.Abs(direction.Y) : float.MaxValue;
        return Mathf.Min(tx, ty);
    }
}
