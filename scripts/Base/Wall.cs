using Godot;

namespace Vestiges.Base;

/// <summary>
/// Mur défensif. Bloque le passage des ennemis (collision layer 4).
/// Les ennemis l'attaquent s'il bloque leur chemin vers le Foyer.
/// </summary>
public partial class Wall : Structure
{
    public override void _Ready()
    {
        base._Ready();
        CollisionLayer = 16; // Layer 5 (bit 4) pour structures
        CollisionMask = 0;
    }

    public override void Initialize(string recipeId, string structureId, float maxHp, Vector2I gridPos, Color color)
    {
        base.Initialize(recipeId, structureId, maxHp, gridPos, color);

        float s = 14f;
        float h = 7f;

        // Face du dessus (losange iso)
        Visual.Polygon = new Vector2[] { new(-s, 0), new(0, -s * 0.5f), new(s, 0), new(0, s * 0.5f) };

        // Face gauche (extrusion vers le bas)
        if (LeftFace != null)
            LeftFace.Polygon = new Vector2[] { new(-s, 0), new(0, s * 0.5f), new(0, s * 0.5f + h), new(-s, h) };

        // Face droite (extrusion vers le bas)
        if (RightFace != null)
            RightFace.Polygon = new Vector2[] { new(0, s * 0.5f), new(s, 0), new(s, h), new(0, s * 0.5f + h) };
    }
}
