using Godot;
using Godot.Collections;
using Vestiges.Combat;
using Vestiges.Core;

namespace Vestiges.World;

/// <summary>
/// Transmet aux shaders des petits décors la position du joueur (<c>trample_origin</c>) et celle des huit créatures
/// les plus proches de lui (<c>trample_creatures_0</c> à <c>_3</c>, deux par vecteur) : herbes et fleurs plient à leur
/// passage (plan 10 lot E). Un parcours des créatures toutes les 50 ms, aucun parcours des décors.
/// </summary>
public partial class GrassTrample : Node
{
    private const int CreatureCount = 8;
    // Au-delà, une créature est hors de l'écran : ses herbes ne se voient pas.
    private const float CreatureRangeSq = 600f * 600f;
    // Les créatures sont relues à 20 Hz : entre deux lectures, elles bougent de quelques pixels, invisibles sur une
    // flexion de 5 px, et le parcours coûtait 0,2 ms par image en combat dense.
    private const float CreatureRefreshSec = 0.05f;
    private static readonly StringName OriginParam = "trample_origin";
    private static readonly StringName[] CreatureParams =
        { "trample_creatures_0", "trample_creatures_1", "trample_creatures_2", "trample_creatures_3" };
    private static readonly Vector2 FarPoint = new(1e6f, 1e6f);
    private static readonly Vector4 Far = new(1e6f, 1e6f, 1e6f, 1e6f);

    private readonly Vector2[] _nearest = new Vector2[CreatureCount];
    private readonly float[] _nearestDistSq = new float[CreatureCount];
    private GroupCache _groups;
    private float _creatureTimer;

    public override void _Ready()
    {
        _groups = GetNode<GroupCache>("/root/GroupCache");
    }

    public override void _Process(double delta)
    {
        if (_groups.GetPlayer() is not Node2D player)
            return;
        Vector2 center = player.GlobalPosition;
        RenderingServer.GlobalShaderParameterSet(OriginParam, new Vector4(center.X, center.Y, 0f, 0f));

        _creatureTimer -= (float)delta;
        if (_creatureTimer > 0f)
            return;
        _creatureTimer = CreatureRefreshSec;
        for (int i = 0; i < CreatureCount; i++)
        {
            _nearest[i] = FarPoint;
            _nearestDistSq[i] = CreatureRangeSq;
        }
        Array<Node> enemies = _groups.GetEnemies();
        foreach (Node node in enemies)
        {
            if (node is not Enemy enemy || !enemy.IsActive || enemy.IsDying)
                continue;
            Vector2 position = enemy.GlobalPosition;
            float distSq = position.DistanceSquaredTo(center);
            if (distSq >= _nearestDistSq[CreatureCount - 1])
                continue;
            // Insertion dans les huit plus proches, triés.
            int slot = CreatureCount - 1;
            while (slot > 0 && _nearestDistSq[slot - 1] > distSq)
            {
                _nearest[slot] = _nearest[slot - 1];
                _nearestDistSq[slot] = _nearestDistSq[slot - 1];
                slot--;
            }
            _nearest[slot] = position;
            _nearestDistSq[slot] = distSq;
        }
        for (int i = 0; i < CreatureParams.Length; i++)
        {
            Vector2 a = _nearest[i * 2];
            Vector2 b = _nearest[i * 2 + 1];
            RenderingServer.GlobalShaderParameterSet(CreatureParams[i], new Vector4(a.X, a.Y, b.X, b.Y));
        }
    }

    public override void _ExitTree()
    {
        RenderingServer.GlobalShaderParameterSet(OriginParam, Far);
        foreach (StringName param in CreatureParams)
            RenderingServer.GlobalShaderParameterSet(param, Far);
    }
}
