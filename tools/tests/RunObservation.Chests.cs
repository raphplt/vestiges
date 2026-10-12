using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Vestiges.Infrastructure;
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
    /// <summary>
    /// --capture-light-columns [secondes] : arrêt devant le plus proche coffre de chaque rareté, la durée donnée, pour
    /// juger l'animation des colonnes de lumière (plan 41 R3) ; rafale de quatre gros plans à 0,15 s en fin d'arrêt.
    /// À filmer avec --write-movie.
    /// </summary>
    private async Task CaptureLightColumns(double seconds)
    {
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
            if (node is Vestiges.Combat.Enemy existing && existing.IsActive)
                _world.GetNode<Vestiges.Spawn.EnemyPool>("EnemyPool").Return(existing);
        _player.AIInputOverride = Vector2.Zero;
        await Frames(180);
        List<Chest> chests = new(Chest.Closed);
        Vector2 spawn = _player.GlobalPosition;
        chests.Sort((a, b) => a.GlobalPosition.DistanceSquaredTo(spawn).CompareTo(b.GlobalPosition.DistanceSquaredTo(spawn)));
        HashSet<string> seen = new();
        foreach (Chest chest in chests)
        {
            if (!seen.Add(chest.ChestId))
                continue;
            _player.GlobalPosition = chest.GlobalPosition + new Vector2(-60f, 30f);
            _camera.ResetSmoothing();
            await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
            for (int shot = 0; shot < 4; shot++)
            {
                SaveCrop($"column-{chest.ChestId[6..]}-{shot}", chest.GlobalPosition + new Vector2(0f, -60f), new Vector2(200f, 160f));
                await ToSignal(GetTree().CreateTimer(0.15), SceneTreeTimer.SignalName.Timeout);
            }
        }
        GD.Print($"[RunObservation] RESULT light-columns kinds={seen.Count}");
    }

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
            _world.GetNode<Vestiges.Progression.EssenceTracker>("EssenceTracker").AddEssence(target.Quote(0));
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
    /// --loot-draws : tous les tirages de coffre doivent donner exclusivement des objets du sac.
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
                    if (loot.Type != "object")
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

    /// <summary>
    /// --capture-loot : le joueur ouvre le coffre fermé le plus proche par le vrai chemin d'ouverture (plan 23 R8) ;
    /// l'écran de butin est capturé pendant que ses lignes apparaissent.
    /// </summary>
    private async Task CaptureLootScreen()
    {
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        ProcessMode = ProcessModeEnum.Always;
        await ToSignal(GetTree().CreateTimer(3.5, processAlways: true), SceneTreeTimer.SignalName.Timeout);
        string[] options = OS.GetCmdlineUserArgs();
        bool epicOnly = System.Array.IndexOf(options, "--capture-loot-epic") >= 0;
        bool captureFree = System.Array.IndexOf(options, "--capture-loot-free") >= 0;
        Chest nearest = null;
        foreach (Chest chest in Chest.Closed)
            if ((!epicOnly || chest.Rarity == "epic") && (nearest == null
                || chest.GlobalPosition.DistanceSquaredTo(_player.GlobalPosition) < nearest.GlobalPosition.DistanceSquaredTo(_player.GlobalPosition)))
                nearest = chest;
        if (nearest == null)
        {
            GD.PushError("[RunObservation] Aucun coffre fermé");
            return;
        }
        _player.SetBagCapacity(20);
        if (System.Array.IndexOf(options, "--capture-loot-pointers") >= 0)
        {
            _player.AIInputOverride = Vector2.Zero;
            _player.GlobalPosition = nearest.GlobalPosition + new Vector2(-760f, 0f);
            _camera.ResetSmoothing();
            await Frames(30);
            SaveFrame("loot-pointer-without-label");
            if (!_player.AddItemCopy("etiquette_perdue"))
                throw new System.InvalidOperationException("Étiquette indisponible pour la capture.");
            await Frames(30);
            SaveFrame("loot-pointer-with-label");
        }
        _player.GlobalPosition = nearest.GlobalPosition + new Vector2(-30f, 4f);
        _player.AIInputOverride = Vector2.Zero;
        _camera.ResetSmoothing();
        Vestiges.Progression.EssenceTracker essence = _world.GetNode<Vestiges.Progression.EssenceTracker>("EssenceTracker");
        essence.TrySpend(essence.CurrentEssence);
        await Frames(30);
        SaveFrame("loot-price-insufficient");
        if (!captureFree) essence.AddEssence(nearest.Quote(0));
        await Frames(10);
        SaveFrame("loot-price-affordable");
        bool captureChoices = System.Array.IndexOf(OS.GetCmdlineUserArgs(), "--capture-loot-choices") >= 0;
        if (captureChoices && !_player.AddItemCopy("carnet_dadresses"))
            throw new System.InvalidOperationException("La capture de choix exige le Carnet d’adresses disponible.");
        Node interaction = _player.GetNode("WorldInteraction");
        if (captureFree)
        {
            for (int copy = 0; copy < 4; copy++)
                if (!_player.AddItemCopy("pied_de_biche"))
                    throw new System.InvalidOperationException("Pied-de-biche indisponible pour la capture.");
            RandomNumberGenerator stream = (RandomNumberGenerator)interaction.GetType().GetField("_chestRng", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(interaction);
            RandomNumberGenerator probe = new();
            for (ulong seed = 1; seed < 10000; seed++)
            {
                probe.Seed = seed;
                if (probe.Randf() >= WorldItemEffects.FreeChestChance(_player)) continue;
                stream.Seed = seed; break;
            }
        }
        interaction.GetType().GetMethod("OpenChest", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(interaction, new object[] { nearest });
        Vestiges.UI.ChoiceScreen choices = _world.GetNode<Vestiges.UI.ChoiceScreen>("ChoiceScreen");
        int decision = 0;
        while (choices.IsOpen)
        {
            await Frames(40);
            SaveFrame($"loot-choices-{decision++}");
            choices.Activate(choices.FirstEnabled());
        }
        if (captureChoices && decision == 0)
            throw new System.InvalidOperationException("Le tirage ne proposait aucune alternative pour capturer le Carnet.");
        if (System.Array.IndexOf(OS.GetCmdlineUserArgs(), "--capture-loot-motion") >= 0)
        {
            // Après le fondu : vérifier le déplacement continu des rayons, horodaté au rendu.
            await ToSignal(GetTree().CreateTimer(0.7, processAlways: true), SceneTreeTimer.SignalName.Timeout);
            for (int sample = 0; sample < 32; sample++)
            {
                await ToSignal(GetTree().CreateTimer(1.0 / 30, processAlways: true), SceneTreeTimer.SignalName.Timeout);
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                GD.Print($"[RunObservation] loot-motion frame={sample} t_ms={Time.GetTicksMsec()}");
                using Image motion = GetViewport().GetTexture().GetImage();
                motion.Resize(960, 540, Image.Interpolation.Nearest);
                motion.SavePng($"{_output}/loot-motion-{sample:00}.png");
            }
            SaveFrame("loot-motion-end");
            GD.Print($"[RunObservation] RESULT loot-motion frames=32 dossier={_output}");
            return;
        }
        int elapsed = 0;
        foreach (int frame in new[] { 20, 28, 36, 44 })
        {
            await Frames(frame - elapsed);
            elapsed = frame;
            SaveFrame($"loot-{frame}");
        }
        await ToSignal(GetTree().CreateTimer(4, processAlways: true), SceneTreeTimer.SignalName.Timeout);
        SaveFrame("loot-revealed");
        _world.GetNode<Vestiges.UI.ChestLootScreen>("ChestLootScreen")._Input(new InputEventAction { Action = "ui_accept", Pressed = true });
        if (System.Array.IndexOf(OS.GetCmdlineUserArgs(), "--capture-loot-gallery") >= 0)
        {
            while (GetTree().Paused)
                await Frames(1);
            await CaptureLootGallery();
        }
        GD.Print($"[RunObservation] RESULT loot-screen chest={nearest.ChestId} dossier={_output}");
    }

    /// <summary>Galerie de présentation : récompenses résolues, sans appliquer un butin supplémentaire au joueur.</summary>
    private async Task CaptureLootGallery()
    {
        List<ResolvedLoot> rewards = new();
        foreach (PassiveSouvenirData item in PassiveSouvenirDataLoader.GetAll())
            if (!item.IsBadge && MetaSaveManager.IsObjectUnlocked(item.Id))
                rewards.Add(new ResolvedLoot("object", item.Id, 1, item.Name, new Color("6ACA5A")));
        Vestiges.UI.ChestLootScreen screen = _world.GetNode<Vestiges.UI.ChestLootScreen>("ChestLootScreen");
        for (int start = 0; start < rewards.Count; start += 5)
        {
            bool complete = false;
            List<ResolvedLoot> page = rewards.GetRange(start, System.Math.Min(5, rewards.Count - start));
            screen.ShowLoot(page, "rare", () => complete = true);
            await ToSignal(GetTree().CreateTimer(1, processAlways: true), SceneTreeTimer.SignalName.Timeout);
            SaveFrame($"loot-gallery-{start / 5}-roulette");
            await ToSignal(GetTree().CreateTimer(3.5, processAlways: true), SceneTreeTimer.SignalName.Timeout);
            SaveFrame($"loot-gallery-{start / 5}-revealed");
            screen._Input(new InputEventAction { Action = "ui_accept", Pressed = true });
            if (!complete) throw new System.InvalidOperationException("Le butin ne se ferme pas après confirmation.");
        }
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
