using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// --capture-prop-gallery BIOME : un cadrage en jeu par décor distinct du biome (plan 31). Pour chaque texture de base,
/// l'exemplaire le plus dégagé de ses voisins ; le joueur se tient à sa gauche pour l'échelle, créatures et brouillard
/// retirés. Un fichier prop-&lt;nom&gt;.png par décor, recadré autour du joueur.
/// </summary>
public partial class RunObservation
{
    private const float GalleryNeighbourRadius = 96f;
    private static readonly Vector2I GalleryCrop = new(720, 480);

    private async Task CapturePropGallery(string biome)
    {
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
            if (node is Vestiges.Combat.Enemy existing && existing.IsActive)
                _world.GetNode<Vestiges.Spawn.EnemyPool>("EnemyPool").Return(existing);
        Node2D fog = _world.GetNodeOrNull<Node2D>("FogOfWar");
        if (fog != null)
            fog.Visible = false;
        await Frames(90);
        _player.AIInputOverride = Vector2.Zero;

        List<EnvironmentProp> props = new();
        foreach (Node child in _world.GetNode("PropContainer").FindChildren("*", "", true, false))
            if (child is EnvironmentProp prop && prop.BaseTexture != null && (_world.GetBiomeAt(prop.GlobalPosition)?.Id ?? "") == biome)
                props.Add(prop);

        Dictionary<string, (EnvironmentProp Prop, int Neighbours)> chosen = new();
        foreach (EnvironmentProp prop in props)
        {
            int neighbours = 0;
            foreach (EnvironmentProp other in props)
                if (other != prop && prop.GlobalPosition.DistanceSquaredTo(other.GlobalPosition) < GalleryNeighbourRadius * GalleryNeighbourRadius)
                    neighbours++;
            string key = prop.BaseTexture.ResourcePath;
            if (!chosen.TryGetValue(key, out (EnvironmentProp Prop, int Neighbours) best) || neighbours < best.Neighbours)
                chosen[key] = (prop, neighbours);
        }

        foreach ((string path, (EnvironmentProp prop, int _)) in chosen)
        {
            Rect2 visible = prop.VisibleWorldRect();
            _player.GlobalPosition = prop.GlobalPosition + new Vector2(-visible.Size.X * 0.5f - 18f, 6f);
            _camera.ResetSmoothing();
            await Frames(15);
            using Image image = GetViewport().GetTexture().GetImage();
            Vector2I size = image.GetSize();
            Vector2I crop = new(Mathf.Min(GalleryCrop.X, size.X), Mathf.Min(GalleryCrop.Y, size.Y));
            Rect2I region = new((size - crop) / 2 + new Vector2I(crop.X / 6, -crop.Y / 8), crop);
            using Image framed = image.GetRegion(region);
            framed.SavePng($"{_output}/prop-{path.GetFile().GetBaseName()}.png");
        }
        GD.Print($"[RunObservation] RESULT prop_gallery biome={biome} props={props.Count} sprites={chosen.Count} output={_output}");
    }
}
