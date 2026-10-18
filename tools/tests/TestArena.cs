using System;
using Godot;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>Terrain dégagé pour les bancs qui posent une foule sur la vraie carte (banc dense, bac à sable de l'Élan).</summary>
public static class TestArena
{
    private const uint PropLayer = 4;

    /// <summary>
    /// Premier point, en spirale depuis l'origine, sans décor bloquant dans ce rayon et sur le sol de la carte. Depuis la
    /// carte agrandie du 30 septembre, l'origine tombe dans un immeuble : les créatures y forçaient contre un mur (plan 29
    /// §1.1). Depuis que les terres sont la règle (plan 51), le premier cercle sans décor tombait hors de la terre, dans
    /// le vide où le joueur ne peut pas marcher : il faut aussi du sol sous l'arène.
    /// </summary>
    public static Vector2 FindOpen(Node2D probe, WorldSetup world, float radius = 420f)
    {
        // Une terre dense (forêt, ville) n'a pas toujours un cercle de ce rayon sans décor : on se contente d'un plus petit.
        for (float scale = 1f; scale >= 0.49f; scale -= 0.25f)
        {
            if (TryFindOpen(probe, world, radius * scale, out Vector2 found))
                return found;
        }
        throw new InvalidOperationException("Aucun terrain dégagé près de l'origine.");
    }

    private static bool TryFindOpen(Node2D probe, WorldSetup world, float radius, out Vector2 found)
    {
        PhysicsDirectSpaceState2D space = probe.GetWorld2D().DirectSpaceState;
        TileMapLayer ground = world.GetNode<TileMapLayer>("Ground");
        using CircleShape2D circle = new() { Radius = radius };
        using PhysicsShapeQueryParameters2D query = new() { Shape = circle, CollisionMask = PropLayer, CollideWithAreas = false };
        for (int ring = 0; ring <= 40; ring++)
        {
            int steps = Math.Max(1, ring * 6);
            for (int step = 0; step < steps; step++)
            {
                Vector2 candidate = Vector2.FromAngle(Mathf.Tau * step / steps) * ring * 256f;
                if (!IsGroundAround(world.Generator, ground, candidate, radius))
                    continue;
                query.Transform = new Transform2D(0f, candidate);
                if (space.IntersectShape(query, 1).Count == 0)
                {
                    found = candidate;
                    return true;
                }
            }
        }
        found = Vector2.Zero;
        return false;
    }

    /// <summary>Le centre et huit points du cercle sont sur le sol de la carte (dans ses bornes, non dissous).</summary>
    private static bool IsGroundAround(WorldGenerator generator, TileMapLayer ground, Vector2 center, float radius)
    {
        if (!IsGround(generator, ground, center))
            return false;
        for (int point = 0; point < 8; point++)
        {
            if (!IsGround(generator, ground, center + Vector2.FromAngle(Mathf.Tau * point / 8f) * radius))
                return false;
        }
        return true;
    }

    private static bool IsGround(WorldGenerator generator, TileMapLayer ground, Vector2 position)
    {
        Vector2I cell = ground.LocalToMap(ground.ToLocal(position));
        return generator.IsWithinBounds(cell.X, cell.Y) && !generator.IsErased(cell.X, cell.Y);
    }
}
