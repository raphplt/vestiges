using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Vestiges.Core;
using Vestiges.UI;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// Photographie d'un vrai biome pour le bac à sable de la verticalité (plan 59) : la vraie scène de run est générée sur
/// la terre demandée, puis on rend son sol tel que le jeu le dessine (tuiles et transitions, chemins, routes, décalques,
/// ombres portées des décors, shader d'ambiance) dans une image qui couvre l'arène, et on relève chaque sprite debout
/// (décors, lieux) avec son point au sol et son emprise. La scène de run est libérée ensuite.
/// </summary>
public sealed class ReliefBake
{
    /// <summary>Un sprite debout du biome, relevé à l'écran : son point au sol et le centre de son image, en pixels du monde.</summary>
    public sealed class Upright
    {
        public Texture2D Texture;
        public Vector2 Base;
        public Vector2 CenterFromBase;
        public Vector2 Scale;
        public bool FlipH;
        public bool RegionEnabled;
        public Rect2 Region;
        public Color Modulate;
        /// <summary>Rayon de l'emprise au sol en pixels, 0 si le décor ne bloque pas.</summary>
        public float GroundRadius;
        /// <summary>Rang du sprite dans son lieu (tronc 0, feuillage 1…) : en 3D, deux plans confondus se disputent la profondeur.</summary>
        public int Layer;
    }

    public Image Ground;
    /// <summary>Position monde (px) de la pointe haute de la cellule (0, 0) de l'arène.</summary>
    public Vector2 Origin;
    public Color CanvasTint = Colors.White;
    public readonly List<Upright> Uprights = new();

    /// <summary>
    /// Lance la vraie run sur <paramref name="biome"/> (lu par <c>--land-order</c>, que le lanceur passe), attend le monde,
    /// photographie une zone de <paramref name="width"/> × <paramref name="depth"/> cellules, puis libère la run.
    /// </summary>
    public static async Task<ReliefBake> Capture(Node host, int width, int depth, ulong seed, string character)
    {
        SceneTree tree = host.GetTree();
        GameManager manager = host.GetNode<GameManager>("/root/GameManager");
        manager.RunSeed = seed;
        manager.SelectedCharacterId = character;
        // La racine finit de poser ses enfants pendant _Ready : la run n'y entre qu'à l'image suivante.
        await host.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        Node sandbox = tree.CurrentScene;
        WorldSetup world = GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<WorldSetup>();
        tree.Root.AddChild(world);
        tree.CurrentScene = world;
        while (!world.IsWorldReady || manager.CurrentState != GameManager.GameState.Run)
            await host.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        world.ProcessMode = Node.ProcessModeEnum.Disabled;

        ReliefBake bake = new();
        TileMapLayer ground = world.GetNode<TileMapLayer>("Ground");
        Vector2 spawn = world.GetNode<Node2D>("Player").GlobalPosition;
        Rect2I used = ground.GetUsedRect();
        Vector2 mapCenter = ground.ToGlobal(ground.MapToLocal(used.Position + used.Size / 2));
        // Centre de l'arène : un peu vers l'intérieur de la terre depuis l'arrivée, qui borde son côté ouest.
        Vector2 center = spawn.Lerp(mapCenter, 0.35f);
        Vector2 tileCenter = ground.ToGlobal(ground.MapToLocal(ground.LocalToMap(ground.ToLocal(center))));
        // La pointe haute de la cellule centrale tombe sur celle d'une vraie tuile : les falaises suivent les losanges du sol.
        Vector2 size = new((width + depth) * 32f, (width + depth) * 16f);
        bake.Origin = new Vector2(tileCenter.X - (width / 2 - depth / 2) * 32f, tileCenter.Y - 16f - (width / 2 + depth / 2) * 16f);
        if (world.GetNodeOrNull<CanvasModulate>("CanvasModulate") is { Visible: true } modulate)
            bake.CanvasTint = modulate.Color;

        bake.CollectUprights(world, bake.Origin, size, depth);
        ShowGroundOnly(world);
        bake.Ground = await RenderGround(host, world, bake.Origin + new Vector2(-depth * 32f, 0f), size);
        GD.Print($"[ReliefBake] Sol {size.X}×{size.Y} px autour de {center}, {bake.Uprights.Count} sprites debout.");

        tree.CurrentScene = sandbox;
        world.QueueFree();
        foreach (Node node in tree.Root.GetChildren())
        {
            if (node is GameLoadingOverlay)
                node.QueueFree();
        }
        await host.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        return bake;
    }

    /// <summary>Sprites debout des décors et des lieux dans la zone ; les ombres au sol (ZIndex −1) restent pour le rendu du sol.</summary>
    private void CollectUprights(WorldSetup world, Vector2 origin, Vector2 size, int depth)
    {
        Rect2 zone = new(origin + new Vector2(-depth * 32f, 0f), size);
        foreach (string containerName in new[] { "PropContainer", "PoiContainer" })
        {
            Node container = world.GetNodeOrNull(containerName);
            if (container == null)
                continue;
            List<Node2D> places = new();
            CollectPlaces(container, places);
            foreach (Node2D place in places)
            {
                if (!zone.HasPoint(place.GlobalPosition))
                    continue;
                float radius = place is EnvironmentProp prop && HasCollision(prop) ? prop.GroundRadius : 0f;
                int layer = 0;
                CollectSprites(place, place, radius, ref layer);
            }
        }
    }

    /// <summary>Les décors sont rangés en tronçons : on descend jusqu'aux décors eux-mêmes, et aux lieux posés à part.</summary>
    private static void CollectPlaces(Node node, List<Node2D> places)
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is EnvironmentProp prop)
                places.Add(prop);
            else if (child.Name.ToString().StartsWith("Chunk_"))
                CollectPlaces(child, places);
            else if (child is Node2D place)
                places.Add(place);
        }
    }

    private static bool HasCollision(EnvironmentProp prop)
    {
        foreach (Node child in prop.GetChildren())
        {
            if (child is CollisionShape2D { Disabled: false })
                return true;
        }
        return false;
    }

    private void CollectSprites(Node node, Node2D place, float radius, ref int layer)
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is Sprite2D { Texture: not null } sprite && sprite.ZIndex >= 0 && sprite.IsVisibleInTree())
            {
                Vector2 scale = sprite.GlobalScale;
                Vector2 frame = sprite.RegionEnabled ? sprite.RegionRect.Size : sprite.Texture.GetSize();
                Vector2 center = sprite.GlobalPosition + sprite.Offset * scale + (sprite.Centered ? Vector2.Zero : frame * scale * 0.5f);
                Uprights.Add(new Upright
                {
                    Texture = sprite.Texture,
                    Base = place.GlobalPosition,
                    CenterFromBase = center - place.GlobalPosition,
                    Scale = scale,
                    FlipH = sprite.FlipH,
                    RegionEnabled = sprite.RegionEnabled,
                    Region = sprite.RegionRect,
                    Modulate = sprite.Modulate * sprite.SelfModulate,
                    GroundRadius = radius,
                    Layer = layer++,
                });
                radius = 0f;
            }
            CollectSprites(child, place, radius, ref layer);
        }
    }

    /// <summary>
    /// Ne laisse voir que ce qui est au sol : couches de tuiles, chemins et routes, décalques, ombres portées des décors.
    /// Les tronçons de décors sont tous montrés, et leur gestionnaire arrêté pour qu'il ne les remasque pas.
    /// </summary>
    private static void ShowGroundOnly(WorldSetup world)
    {
        foreach (Node child in world.GetChildren())
        {
            if (child is not CanvasItem item || child is CanvasLayer)
                continue;
            string name = child.Name.ToString();
            bool keep = child is TileMapLayer || name == "GroundDecals" || name == "PropContainer"
                || (item is Node2D node && node.ZIndex is <= -6 and > -100);
            item.Visible = keep;
        }
        if (world.GetNodeOrNull("PropChunks") is Node chunks)
            chunks.ProcessMode = Node.ProcessModeEnum.Disabled;
        foreach (string containerName in new[] { "PropContainer", "GroundDecals" })
        {
            if (world.GetNodeOrNull(containerName) is not Node container)
                continue;
            foreach (Node chunk in container.GetChildren())
            {
                if (chunk is CanvasItem chunkItem)
                    chunkItem.Visible = true;
            }
        }
        // Dans les décors debout, seules les ombres portées (ZIndex −1) restent : elles tombent sur le sol photographié.
        if (world.GetNodeOrNull("PropContainer") is Node props)
            HideUprightSprites(props);
    }

    private static void HideUprightSprites(Node node)
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is Sprite2D sprite && sprite.ZIndex >= 0)
                sprite.Visible = false;
            HideUprightSprites(child);
        }
    }

    /// <summary>Rend la zone d'un coup dans une vue qui partage le monde 2D de la run, à l'échelle 1.</summary>
    private static async Task<Image> RenderGround(Node host, WorldSetup world, Vector2 topLeft, Vector2 size)
    {
        SubViewport view = new()
        {
            Size = new Vector2I(Mathf.CeilToInt(size.X), Mathf.CeilToInt(size.Y)),
            World2D = world.GetViewport().World2D,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
            CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest,
        };
        Camera2D camera = new() { Position = topLeft + size / 2f, Zoom = Vector2.One };
        view.AddChild(camera);
        host.AddChild(view);
        camera.MakeCurrent();
        for (int i = 0; i < 3; i++)
            await host.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Image image = view.GetTexture().GetImage();
        view.QueueFree();
        return image;
    }
}
