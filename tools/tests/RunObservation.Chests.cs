using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Godot;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// --capture-chests : chaque coffre du monde cadré (joueur à portée, invite visible), avec puis sans décors,
/// un plein écran au départ (premier coffre, repères de bord) et un plein écran à distance d'un coffre.
/// Une ligne RESULT donne le nombre de coffres par type et leur distance au départ en fraction du rayon.
/// --loot-draws N : N coffres de chaque type tirés sans être appliqués ; échec si un perk exclu du level-up
/// (V1, passif, autre personnage) ou une malédiction sort.
/// </summary>
public partial class RunObservation
{
    private async Task CaptureChests()
    {
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
            if (node is Vestiges.Combat.Enemy existing && existing.IsActive)
                _world.GetNode<Vestiges.Spawn.EnemyPool>("EnemyPool").Return(existing);
        _player.AIInputOverride = Vector2.Zero;
        await Frames(90);
        SaveFrame("chests-start");

        List<Chest> chests = new(Chest.Closed);
        Vector2 spawn = _player.GlobalPosition;
        chests.Sort((a, b) => a.GlobalPosition.DistanceSquaredTo(spawn).CompareTo(b.GlobalPosition.DistanceSquaredTo(spawn)));
        TileMapLayer ground = _world.GetNode<TileMapLayer>("Ground");
                Dictionary<string, int> counts = new();
        List<string> bands = new();
        foreach (Chest chest in chests)
        {
            counts[chest.ChestId] = counts.GetValueOrDefault(chest.ChestId) + 1;
            Vector2I cell = ground.LocalToMap(ground.ToLocal(chest.GlobalPosition));
            bands.Add(string.Create(CultureInfo.InvariantCulture, $"{chest.ChestId[6..]}:{_world.Generator.EllipseDistance(cell.X, cell.Y) / _world.Generator.MapRadiusX:F2}"));
        }

        Node2D props = _world.GetNode<Node2D>("PropContainer");
        for (int index = 0; index < chests.Count; index++)
        {
            Chest chest = chests[index];
            // Joueur à gauche, à portée : l'invite s'affiche, le coffre reste dégagé.
            _player.GlobalPosition = chest.GlobalPosition + new Vector2(-40f, 6f);
            _camera.ResetSmoothing();
            await Frames(25);
            SaveCrop($"chest-{index:00}-{chest.ChestId[6..]}", chest.GlobalPosition + new Vector2(0f, -40f), new Vector2(240f, 135f));
            props.Visible = false;
            await Frames(2);
            SaveCrop($"chest-{index:00}-{chest.ChestId[6..]}-bare", chest.GlobalPosition + new Vector2(0f, -40f), new Vector2(240f, 135f));
            props.Visible = true;
        }

        if (chests.Count > 0)
        {
            // Hors du cadre, en dessous : la colonne dépasse du bord bas, la flèche pointe le coffre.
            _player.GlobalPosition = chests[0].GlobalPosition + new Vector2(0f, -330f);
            _camera.ResetSmoothing();
            await Frames(25);
            SaveFrame("chests-offscreen");
            // Loin sur le côté : seule la flèche de bord signale le coffre.
            _player.GlobalPosition = chests[0].GlobalPosition + new Vector2(-760f, 0f);
            _camera.ResetSmoothing();
            await Frames(25);
            SaveFrame("chests-pointer");

            // Ouverture réelle : jauge, puis écran de butin (le jeu est en pause, la capture continue).
            ProcessMode = ProcessModeEnum.Always;
            Chest target = chests[chests.Count - 1];
            _player.GlobalPosition = target.GlobalPosition + new Vector2(-30f, 4f);
            _camera.ResetSmoothing();
            await Frames(10);
            _player.AITriggerInteract();
            await Frames(20);
            SaveCrop("chest-opening", target.GlobalPosition + new Vector2(0f, -30f), new Vector2(160f, 90f));
            for (int shot = 0; shot < 3; shot++)
            {
                await Frames(90);
                SaveFrame($"chest-loot-{shot}");
            }
        }

        List<string> summary = new();
        foreach (KeyValuePair<string, int> entry in counts)
            summary.Add($"{entry.Key}={entry.Value}");
        GD.Print($"[RunObservation] RESULT chests total={chests.Count} {string.Join(" ", summary)} bands={string.Join(",", bands)}");
    }

    /// <summary>
    /// --loot-draws : répartition du butin de chaque coffre. Le joueur porte un objet à monter et un objet au niveau
    /// maximal : un niveau d'objet ne doit viser que le premier ; plus aucun Don ne sort.
    /// </summary>
    private void MeasureLootDraws(int draws)
    {
        _player.AddOrUpgradePassive("resonance");
        _player.AddOrUpgradePassive("ancrage");
        _player.AddOrUpgradePassive("ancrage", 29);
        int failures = 0;
        foreach (Vestiges.Infrastructure.ChestData chest in Vestiges.Infrastructure.ChestDataLoader.GetAll())
        {
            Dictionary<string, int> types = new();
            for (int i = 0; i < draws; i++)
            {
                foreach (ResolvedLoot loot in LootRewards.Resolve(LootResolver.Roll(chest.LootTableId, chest.LootRolls), _player))
                {
                    types[loot.Type] = types.GetValueOrDefault(loot.Type) + 1;
                    if (loot.Type == "perk" || (loot.Type == "object_level" && loot.ItemId != "resonance"))
                    {
                        failures++;
                        GD.PushError($"[RunObservation] butin inattendu : {loot.Type} {loot.ItemId} ({chest.Id})");
                    }
                }
            }
            if (types.ContainsKey("cursed_item"))
                failures++;
            List<string> parts = new();
            foreach (KeyValuePair<string, int> entry in types)
                parts.Add($"{entry.Key}={entry.Value}");
            GD.Print($"[RunObservation] RESULT loot {chest.Id} draws={draws} {string.Join(" ", parts)}");
        }
        GD.Print($"[RunObservation] RESULT loot failures={failures}");
    }

    private void SaveFrame(string name)
    {
        using Image image = GetViewport().GetTexture().GetImage();
        image.SavePng($"{_output}/{name}.png");
    }

    /// <summary>Recadrage autour d'un point du monde, en pixels physiques de la capture.</summary>
    private void SaveCrop(string name, Vector2 worldCenter, Vector2 half)
    {
        using Image image = GetViewport().GetTexture().GetImage();
        float pixelRatio = image.GetWidth() / GetViewport().GetVisibleRect().Size.X;
        Vector2 center = GetViewport().GetCanvasTransform() * worldCenter;
        Vector2 zoom = _camera.Zoom;
        Vector2 corner = (center - half * zoom) * pixelRatio;
        Rect2I region = new Rect2I((Vector2I)corner, (Vector2I)(half * 2f * zoom * pixelRatio))
            .Intersection(new Rect2I(0, 0, image.GetWidth(), image.GetHeight()));
        using Image crop = image.GetRegion(region);
        crop.SavePng($"{_output}/{name}.png");
    }
}
