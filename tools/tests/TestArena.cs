using System;
using Godot;

namespace Vestiges.Tests;

/// <summary>Terrain dégagé pour les bancs qui posent une foule sur la vraie carte (banc dense, bac à sable de l'Élan).</summary>
public static class TestArena
{
    private const uint PropLayer = 4;

    /// <summary>
    /// Premier point, en spirale depuis l'origine, sans décor bloquant dans ce rayon. Depuis la carte agrandie du
    /// 30 septembre, l'origine tombe dans un immeuble : les créatures y forçaient contre un mur (plan 29 §1.1).
    /// </summary>
    public static Vector2 FindOpen(Node2D probe, float radius = 420f)
    {
        PhysicsDirectSpaceState2D space = probe.GetWorld2D().DirectSpaceState;
        using CircleShape2D circle = new() { Radius = radius };
        using PhysicsShapeQueryParameters2D query = new() { Shape = circle, CollisionMask = PropLayer, CollideWithAreas = false };
        for (int ring = 0; ring <= 40; ring++)
        {
            int steps = Math.Max(1, ring * 6);
            for (int step = 0; step < steps; step++)
            {
                Vector2 candidate = Vector2.FromAngle(Mathf.Tau * step / steps) * ring * 256f;
                query.Transform = new Transform2D(0f, candidate);
                if (space.IntersectShape(query, 1).Count == 0)
                    return candidate;
            }
        }
        throw new InvalidOperationException("Aucun terrain dégagé près de l'origine.");
    }
}
