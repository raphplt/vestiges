using Godot;
using Vestiges.Core;

namespace Vestiges.World;

/// <summary>
/// Transmet la position du joueur aux shaders des petits décors (uniforme global <c>trample_origin</c>) : herbes et
/// fleurs plient à son passage (plan 10 lot E). Un appel par frame, aucun parcours des décors.
/// </summary>
public partial class GrassTrample : Node
{
    private static readonly StringName OriginParam = "trample_origin";
    private static readonly Vector4 Far = new(1e6f, 1e6f, 0f, 0f);
    private GroupCache _groups;

    public override void _Ready()
    {
        _groups = GetNode<GroupCache>("/root/GroupCache");
    }

    public override void _Process(double delta)
    {
        if (_groups.GetPlayer() is Node2D player)
            RenderingServer.GlobalShaderParameterSet(OriginParam, new Vector4(player.GlobalPosition.X, player.GlobalPosition.Y, 0f, 0f));
    }

    public override void _ExitTree()
    {
        RenderingServer.GlobalShaderParameterSet(OriginParam, Far);
    }
}
