using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;
using Vestiges.Spawn;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// Planches du plan 27 (tout se voit).
/// --capture-statuses [--status-enemy id] : une rangée de créatures, chacune sous un état posé directement, en gros
/// plan à plusieurs instants. Le joueur n'a plus d'arme, pour que rien d'autre ne touche la rangée.
/// --capture-status-crowd : soixante créatures autour du joueur armé de la Cloche et de la Berceuse (statuts en foule).
/// --status-biome id : ces planches se tiennent sur un terrain dégagé de ce biome (sols différents).
/// --capture-player-hit : le joueur blessé, bouclier qui encaisse puis casse, coup ignoré, toile, soin.
/// </summary>
public partial class RunObservation
{
    private const float StatusSeconds = 30f;
    private const float StatusSpacing = 56f;
    private const int StatusPerLine = 5;
    // Plus haut qu'une créature : un tel décor peut la cacher. Herbes et gravats restent dans le cadre.
    private const float TallPropHeight = 40f;
    private static readonly Vector2 StatusFrame = new(150f, 85f);

    /// <summary>Ordre de la rangée, de gauche à droite ; le Rampant porte l'état terré, qui lui est propre.</summary>
    private static readonly string[] StatusRow =
        { "sain", "coup", "annonce", "brulure", "saignement", "ralenti", "fige", "desoriente", "fragile", "terre", "instable" };

    private static readonly int[] StatusCaptureFrames = { 1, 4, 8, 20, 45, 90 };

    private async Task CaptureStatuses()
    {
        await PrepareCloseUpScene();
        await MoveToOpenGround(StatusFrame, Argument(OS.GetCmdlineUserArgs(), "--status-biome", null));
        SpawnManager spawner = _world.GetNode<SpawnManager>("SpawnManager");
        string enemyId = Argument(OS.GetCmdlineUserArgs(), "--status-enemy", "rodeur");
        // Deux lignes au-dessus du joueur : les créatures avancent vers lui, la planche reste lisible le temps des captures.
        Vector2 rowCenter = _player.GlobalPosition + new Vector2(0f, -45f);
        Vector2[] spots = new Vector2[StatusRow.Length];
        for (int index = 0; index < StatusRow.Length; index++)
        {
            int column = index % StatusPerLine;
            int line = index / StatusPerLine;
            spots[index] = rowCenter + new Vector2((column - (StatusPerLine - 1) / 2f) * StatusSpacing, (line - 0.5f) * 60f);
            spawner.ForceSpawnEnemy(StatusRow[index] == "terre" ? "rampant" : enemyId, spots[index]);
            AddStatusLabel(StatusRow[index], spots[index]);
        }
        await Frames(2);
        Enemy[] row = new Enemy[StatusRow.Length];
        for (int index = 0; index < spots.Length; index++)
            row[index] = NearestEnemy(spots[index]);

        FieldInfo hp = typeof(Enemy).GetField("_currentHp", BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (Enemy enemy in row)
            hp.SetValue(enemy, 100000f);
        for (int index = 0; index < row.Length; index++)
            ApplyCaptureStatus(row[index], StatusRow[index]);

        int elapsed = 0;
        for (int shot = 0; shot < StatusCaptureFrames.Length; shot++)
        {
            await Frames(StatusCaptureFrames[shot] - elapsed);
            elapsed = StatusCaptureFrames[shot];
            // Le coup se répète : chaque capture montre le flash à un instant différent de sa courbe.
            if (shot % 2 == 1)
                row[1].TakeDamage(1f, false);
            SaveCloseUp($"{_output}/statuses-{shot}.png", rowCenter, StatusFrame);
        }
        GD.Print($"[RunObservation] RESULT statuses rangée={string.Join(',', StatusRow)} créature={enemyId} dossier={_output}");
    }

    private static void ApplyCaptureStatus(Enemy enemy, string status)
    {
        switch (status)
        {
            case "coup":
                enemy.TakeDamage(1f, false);
                break;
            case "annonce":
                enemy.FlashWarning(PixelPalette.Ramp(FxFamily.Hostile).Light, StatusSeconds);
                break;
            case "brulure":
                enemy.ApplyIgnite(1f, StatusSeconds);
                break;
            case "saignement":
                enemy.ApplyBleed(1f, StatusSeconds);
                break;
            case "ralenti":
                enemy.ApplySlow(0.4f, StatusSeconds);
                break;
            case "fige":
                enemy.Freeze(StatusSeconds);
                break;
            case "desoriente":
                enemy.ApplyDisorient(StatusSeconds);
                break;
            case "fragile":
                enemy.ApplyFragile(0.2f, StatusSeconds);
                break;
            case "terre":
                enemy.SetBurrowed(true);
                break;
            case "instable":
                // Blessé sous le seuil : le rayon de sa détonation s'affiche au sol (plan 27 V4).
                enemy.ApplyAffix(EnemyVariantDataLoader.GetAffix("explosive"));
                typeof(Enemy).GetField("_currentHp", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(enemy, enemy.MaxHp * 0.25f);
                break;
        }
    }

    private Enemy NearestEnemy(Vector2 spot)
    {
        Enemy nearest = null;
        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
            if (node is Enemy { IsActive: true } enemy
                && (nearest == null || enemy.GlobalPosition.DistanceSquaredTo(spot) < nearest.GlobalPosition.DistanceSquaredTo(spot)))
                nearest = enemy;
        return nearest;
    }

    private void AddStatusLabel(string text, Vector2 spot)
    {
        Label label = new()
        {
            Text = text,
            ZIndex = 30,
            HorizontalAlignment = HorizontalAlignment.Center,
            Size = new Vector2(StatusSpacing, 10f),
            Position = spot + new Vector2(-StatusSpacing / 2f, 14f),
        };
        label.AddThemeFontSizeOverride("font_size", 6);
        _world.AddChild(label);
    }

    private static readonly int[] CrowdCaptureFrames = { 30, 60, 120, 180, 300 };

    private async Task CaptureStatusCrowd()
    {
        Vector2 half = new(160f, 95f);
        await PrepareCloseUpScene();
        await MoveToOpenGround(half, Argument(OS.GetCmdlineUserArgs(), "--status-biome", null));
        SpawnManager spawner = _world.GetNode<SpawnManager>("SpawnManager");
        Vector2 origin = _player.GlobalPosition;
        for (int index = 0; index < 60; index++)
            spawner.ForceSpawnEnemy("rodeur", origin + Vector2.FromAngle(index * 2.4f) * (45f + index * 1.8f));
        await Frames(2);
        FieldInfo hp = typeof(Enemy).GetField("_currentHp", BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
            if (node is Enemy { IsActive: true } enemy)
                hp.SetValue(enemy, 100000f);
        _player.AddWeapon(WeaponDataLoader.Get("teachers_bell"));
        _player.AddWeapon(WeaponDataLoader.Get("music_box"));
        foreach (WeaponInstance held in _player.WeaponSlots)
        {
            if (held.Id != "music_box")
                continue;
            while (held.CanLevelUp)
                held.ApplyUpgrade(System.Array.Empty<StatGain>());
            _player.AscendWeapon(held.Id, "lullaby");
        }
        int elapsed = 0;
        for (int shot = 0; shot < CrowdCaptureFrames.Length; shot++)
        {
            await Frames(CrowdCaptureFrames[shot] - elapsed);
            elapsed = CrowdCaptureFrames[shot];
            SaveCloseUp($"{_output}/crowd-statuses-{shot}.png", _player.GlobalPosition, half);
        }
        GD.Print($"[RunObservation] RESULT status-crowd créatures=60 dossier={_output}");
    }

    private async Task CapturePlayerHit()
    {
        await PrepareCloseUpScene();
        _player.IsGodMode = false;
        _player.SurviveFatalHitsForTests = true;
        object defense = typeof(Player).GetField("_defense", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_player);
        Vector2 half = new(90f, 55f);
        await MoveToOpenGround(half);
        SaveCloseUp($"{_output}/player-0-repos.png", _player.GlobalPosition, half);

        // Blessure : éclair, animation de blessure, puis clignotement d'invulnérabilité. Le coup vient de la droite :
        // la vignette s'épaissit de ce côté (plein écran).
        _player.TakeDamage(12f, _player.GlobalPosition + new Vector2(60f, 0f));
        await Frames(2);
        Save("player-1-blessure-ecran.png");
        await SavePlayerSequence("1-blessure", half, new[] { 1, 3, 6, 10, 16 });
        await Seconds(1.0);

        // Bouclier : il encaisse un coup, puis casse sur le suivant.
        defense.GetType().GetMethod("AddShield").Invoke(defense, new object[] { 20f });
        await Frames(2);
        _player.TakeDamage(8f);
        await SavePlayerSequence("2-bouclier-encaisse", half, new[] { 1, 4, 10 });
        await Seconds(1.0);
        _player.TakeDamage(30f);
        await SavePlayerSequence("3-bouclier-casse", half, new[] { 1, 4, 10 });
        await Seconds(1.0);

        // Toile de la Tisseuse : le joueur marche, ralenti.
        _player.ApplySlow(0.4f, 2f);
        _player.AIInputOverride = Vector2.Right;
        await SavePlayerSequence("4-toile", half, new[] { 2, 15, 30 });
        _player.AIInputOverride = Vector2.Zero;
        await Seconds(2.0);

        // Effacement qui ralentit : poussière pâle laissée en marchant (plan 27 V3b).
        FieldInfo penalty = typeof(Player).GetField("_erasurePenalty", BindingFlags.NonPublic | BindingFlags.Instance);
        object before = penalty.GetValue(_player);
        penalty.SetValue(_player, new Vestiges.World.ErasureEffects.Effect(0.6f, 1f, 0f, 0f, 0f));
        _player.AIInputOverride = Vector2.Left;
        await SavePlayerSequence("6-effacement", half, new[] { 20, 40, 60 });
        _player.AIInputOverride = Vector2.Zero;
        penalty.SetValue(_player, before);
        await Seconds(1.0);

        // Néant : trois tranches, vignette pâle qui pulse, sans le paquet d'une blessure (plan 27 V3d).
        for (int tick = 0; tick < 3; tick++)
        {
            _player.TakeErasureDamage(2f);
            await Frames(3);
            Save($"player-7-neant-{tick}.png");
            await Seconds(0.5);
        }

        // Soin.
        _player.TakeDamage(40f);
        await Seconds(1.0);
        _player.Heal(30f);
        await SavePlayerSequence("5-soin", half, new[] { 1, 6, 15 });
        Save("player-5-soin-ecran.png");
        GD.Print($"[RunObservation] RESULT player-hit dossier={_output}");
    }

    private async Task SavePlayerSequence(string name, Vector2 half, int[] frames)
    {
        int elapsed = 0;
        for (int shot = 0; shot < frames.Length; shot++)
        {
            await Frames(frames[shot] - elapsed);
            elapsed = frames[shot];
            SaveCloseUp($"{_output}/player-{name}-{shot}.png", _player.GlobalPosition, half);
        }
    }

    /// <summary>Monde vide autour du joueur immobile, désarmé, écran de chargement effacé.</summary>
    private async Task PrepareCloseUpScene()
    {
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        EnemyPool pool = _world.GetNode<EnemyPool>("EnemyPool");
        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
            if (node is Enemy existing && existing.IsActive)
                pool.Return(existing);
        while (_player.WeaponSlots.Count > 0)
            _player.RemoveWeapon(0);
        _player.AIInputOverride = Vector2.Zero;
        for (int frame = 0; frame < 600 && _world.FindChild("GameLoadingOverlay", true, false) != null; frame++)
            await Frames(1);
        await Frames(30);
    }

    /// <summary>
    /// Place le joueur sur le terrain dégagé le plus proche : aucun décor haut dessiné dans le cadre <paramref name="half"/>,
    /// étendu vers le haut où se tiennent les créatures, pour qu'aucune ne passe derrière un immeuble.
    /// </summary>
    private async Task MoveToOpenGround(Vector2 half, string biomeId = null)
    {
        List<Rect2> props = new();
        // Les décors sont rangés par tronçons (PropChunks), à l'origine : leur rectangle visible est en coordonnées monde.
        foreach (Node chunk in _world.GetNode("PropContainer").GetChildren())
            foreach (Node node in chunk.GetChildren())
                if (node is EnvironmentProp prop && prop.VisibleWorldRect() is { Size.Y: > TallPropHeight } rect)
                    props.Add(rect);
        Vector2 origin = _player.GlobalPosition;
        Rect2 frame = new(-half.X - 30f, -half.Y * 2f - 30f, half.X * 2f + 60f, half.Y * 3f + 60f);
        if (props.Count == 0)
            throw new System.InvalidOperationException("Aucun décor haut trouvé sous PropContainer.");
        // Un biome précis peut se trouver loin du départ : la carte fait 12 800 px de haut.
        float reach = biomeId == null ? 3000f : 12000f;
        for (float radius = 0f; radius < reach; radius += 64f)
        {
            int steps = radius <= 0f ? 1 : Mathf.CeilToInt(Mathf.Tau * radius / 64f);
            for (int step = 0; step < steps; step++)
            {
                Vector2 candidate = origin + Vector2.FromAngle(step * Mathf.Tau / steps) * radius;
                if (biomeId != null && _world.GetBiomeAt(candidate)?.Id != biomeId)
                    continue;
                Rect2 area = new(candidate + frame.Position, frame.Size);
                if (props.Exists(prop => area.Intersects(prop)))
                    continue;
                GD.Print($"[RunObservation] terrain dégagé à {candidate}, biome {_world.GetBiomeAt(candidate)?.Id} ({props.Count} décors hauts vérifiés)");
                _player.GlobalPosition = candidate;
                _camera.ResetSmoothing();
                await Frames(20);
                return;
            }
        }
        throw new System.InvalidOperationException($"Aucun terrain dégagé {(biomeId == null ? "" : $"du biome {biomeId} ")}à moins de {reach} px du départ.");
    }

    /// <summary>Gros plan centré sur un point du monde, en pixels physiques de la capture.</summary>
    private void SaveCloseUp(string path, Vector2 worldCenter, Vector2 half)
    {
        using Image image = GetViewport().GetTexture().GetImage();
        float pixelRatio = image.GetWidth() / GetViewport().GetVisibleRect().Size.X;
        Vector2 center = GetViewport().GetCanvasTransform() * worldCenter;
        Vector2 zoom = _camera.Zoom;
        Vector2 size = half * 2f * zoom * pixelRatio;
        Vector2 corner = (center - half * zoom) * pixelRatio;
        Rect2I region = new Rect2I((Vector2I)corner, (Vector2I)size).Intersection(new Rect2I(0, 0, image.GetWidth(), image.GetHeight()));
        using Image crop = image.GetRegion(region);
        crop.SavePng(path);
    }
}
