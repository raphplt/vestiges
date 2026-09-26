using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Combat.Abilities;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Spawn;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// Observation de la vraie scène de run, rendue.
/// --capture-abilities [--enemies a,b] : captures des attaques d'ennemis choisis (Présage et Charognard par défaut).
/// --capture-map : répartition des biomes autour du spawn (plusieurs seeds) et vues dézoomées.
/// --capture-props : zone la plus chargée en décors de chaque biome, collisions affichées (sauf --hide-collisions).
/// --measure-props [--measure-seconds 8] : coût de rendu des décors par biome (RunObservation.PropCost.cs).
/// --capture-weapons [--weapons a,b] : galerie des attaques du joueur (RunObservation.Weapons.cs).
/// --capture-bestiary : gros plans des créatures du pilote de sprites procéduraux, autour du joueur immobile.
/// --density : mesure de densité en spawn naturel (ennemis visibles, temps sans ennemi, débits, niveaux).
/// --capture-every N : pendant la mesure, capture plein écran toutes les N secondes (HUD, événements).
/// --event ID : force le micro-événement ID à 3 s de run (bancs de capture).
/// </summary>
public partial class RunObservation : Node
{
    private const ulong Seed = 221092026;

    private WorldSetup _world;
    private Player _player;
    private Camera2D _camera;
    private string _output;
    private double _captureEvery;
    private string _forcedEvent;

    public override async void _Ready()
    {
        try
        {
            // La racine termine ses _Ready avant de recevoir la scène de run.
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            string[] args = OS.GetCmdlineUserArgs();
            _output = Argument(args, "--output", "/tmp/vestiges-observation");
            DirAccess.MakeDirRecursiveAbsolute(_output);
            ulong seed = ulong.Parse(Argument(args, "--seed", Seed.ToString(CultureInfo.InvariantCulture)), CultureInfo.InvariantCulture);
            bool captureMap = Array.IndexOf(args, "--capture-map") >= 0;
            bool captureProps = Array.IndexOf(args, "--capture-props") >= 0;
            if (captureProps && Array.IndexOf(args, "--hide-collisions") < 0)
                GetTree().DebugCollisionsHint = true;
            if (captureMap)
                MeasureBiomeLayout(seed, int.Parse(Argument(args, "--map-seeds", "40"), CultureInfo.InvariantCulture), 4);
            await LoadRun(seed, Argument(args, "--character", "traqueur"));

            if (captureMap)
                await CaptureWorldOverview();
            else if (captureProps)
                await CapturePropHotspots();
            else if (Array.IndexOf(args, "--capture-abilities") >= 0)
                await CaptureAbilities();
            else if (Array.IndexOf(args, "--measure-props") >= 0)
                await MeasurePropCost(double.Parse(Argument(args, "--measure-seconds", "8"), CultureInfo.InvariantCulture));
            else if (Array.IndexOf(args, "--capture-weapons") >= 0)
                await CaptureWeapons(Argument(args, "--weapons", null));
            else if (Array.IndexOf(args, "--capture-bestiary") >= 0)
                await CaptureBestiary();
            else if (Array.IndexOf(args, "--capture-character") >= 0)
                await CaptureCharacter(Argument(args, "--character", "traqueur"));
            else
            {
                _forcedEvent = Argument(args, "--event", null);
                _captureEvery = double.Parse(Argument(args, "--capture-every", "0"), CultureInfo.InvariantCulture);
                await MeasureDensity(double.Parse(Argument(args, "--seconds", "180"), CultureInfo.InvariantCulture), seed);
            }

            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError($"[RunObservation] {exception}");
            GetTree().Quit(1);
        }
    }

    private async Task LoadRun(ulong seed, string characterId)
    {
        GD.Seed(seed);
        GameManager manager = GetNode<GameManager>("/root/GameManager");
        manager.RunSeed = seed;
        manager.SelectedCharacterId = characterId;
        GetTree().CurrentScene = null;
        _world = GD.Load<PackedScene>("res://scenes/Main.tscn").Instantiate<WorldSetup>();
        GetTree().Root.AddChild(_world);
        GetTree().CurrentScene = _world;
        ulong timeout = Time.GetTicksMsec() + 120000;
        while (!_world.IsWorldReady || GetTree().Paused || manager.CurrentState != GameManager.GameState.Run)
        {
            if (Time.GetTicksMsec() > timeout)
                throw new InvalidOperationException("Initialisation Main supérieure à 120 s.");
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        _player = _world.GetNode<Player>("Player");
        _camera = _player.GetNode<Camera2D>("Camera");
        _player.IsGodMode = true;
        _player.IsAIControlled = true;
    }

    private async Task CaptureAbilities()
    {
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
            if (node is Enemy existing && existing.IsActive)
                _world.GetNode<EnemyPool>("EnemyPool").Return(existing);
        await Frames(2);

        SpawnManager spawner = _world.GetNode<SpawnManager>("SpawnManager");
        Vector2 origin = _player.GlobalPosition;
        string[] casters = Argument(OS.GetCmdlineUserArgs(), "--enemies", "presage,charognard").Split(',');
        for (int index = 0; index < casters.Length; index++)
            spawner.ForceSpawnEnemy(casters[index], origin + Vector2.FromAngle(Mathf.Pi * (0.9f + 0.45f * index)) * (130f + 40f * (index % 2)));
        await Frames(2);

        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
        {
            if (node is not Enemy enemy || !enemy.IsActive)
                continue;
            // PV renforcés pour observer l'annonce complète malgré l'arme automatique.
            typeof(Enemy).GetField("_currentHp", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(enemy, 100000f);
            ResetAbilityCooldowns(enemy);
        }

        _player.AIInputOverride = new Vector2(0.5f, 0f);
        for (int shot = 0; shot < 12; shot++)
        {
            await Frames(15);
            using Image image = GetViewport().GetTexture().GetImage();
            string path = $"{_output}/abilities-{shot:00}.png";
            image.SavePng(path);
        }
        GD.Print($"[RunObservation] Captures écrites dans {_output}");
    }

    private async Task CaptureBestiary()
    {
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
            if (node is Enemy existing && existing.IsActive)
                _world.GetNode<EnemyPool>("EnemyPool").Return(existing);
        // L'écran de chargement s'efface après l'initialisation du monde.
        await Frames(90);

        SpawnManager spawner = _world.GetNode<SpawnManager>("SpawnManager");
        Vector2 origin = _player.GlobalPosition;
        spawner.ForceSpawnEnemy("presage", origin + new Vector2(-110f, -30f));
        spawner.ForceSpawnEnemy("rodeur", origin + new Vector2(100f, -40f));
        spawner.ForceSpawnEnemy("charognard", origin + new Vector2(60f, 80f));
        await Frames(2);
        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
        {
            if (node is Enemy enemy && enemy.IsActive)
                typeof(Enemy).GetField("_currentHp", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(enemy, 100000f);
        }

        _player.AIInputOverride = Vector2.Zero;
        Vector2 half = new(200f, 120f);
        for (int shot = 0; shot < 16; shot++)
        {
            await Frames(12);
            using Image image = GetViewport().GetTexture().GetImage();
            // Gros plan sur le joueur, en pixels physiques de la capture (écrans à haute densité compris).
            float pixelRatio = image.GetWidth() / GetViewport().GetVisibleRect().Size.X;
            Vector2 center = GetViewport().GetCanvasTransform() * _player.GlobalPosition;
            Vector2 zoom = _camera.Zoom;
            Vector2 size = half * 2f * zoom * pixelRatio;
            Vector2 corner = (center - half * zoom) * pixelRatio;
            Rect2I region = new Rect2I((Vector2I)corner, (Vector2I)size).Intersection(new Rect2I(0, 0, image.GetWidth(), image.GetHeight()));
            using Image crop = image.GetRegion(region);
            crop.SavePng($"{_output}/bestiary-{shot:00}.png");
        }
        GD.Print($"[RunObservation] Captures du bestiaire écrites dans {_output}");
    }

    private async Task CaptureCharacter(string characterId)
    {
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        SpawnManager spawner = _world.GetNode<SpawnManager>("SpawnManager");
        // Repères d'échelle : un petit et un grand ennemi à distance du joueur.
        spawner.ForceSpawnEnemy("shade", _player.GlobalPosition + new Vector2(-150f, 60f));
        spawner.ForceSpawnEnemy("rodeur", _player.GlobalPosition + new Vector2(160f, -50f));
        Vector2[] directions = { Vector2.Zero, Vector2.Down, new(1, 1), Vector2.Right, new(1, -1), Vector2.Up, Vector2.Left };
        for (int index = 0; index < directions.Length; index++)
        {
            _player.AIInputOverride = directions[index].Normalized();
            await Frames(20);
            using Image image = GetViewport().GetTexture().GetImage();
            image.SavePng($"{_output}/{characterId}-{index:00}.png");
        }
        GD.Print($"[RunObservation] Captures {characterId} écrites dans {_output}");
    }

    private async Task MeasureDensity(double seconds, ulong seed)
    {
        RunTracker tracker = _world.GetNode<RunTracker>("RunTracker");
        List<string> rows = new() { "t,visible,near600,alive,spawned,killed,level,hit_damage" };
        // Indice de pression : dégâts que les ennemis infligent à un joueur qui n'esquive jamais (invincible ici).
        double hitDamage = 0;
        EventBus eventBus = GetNode<EventBus>("/root/EventBus");
        // Souffle du tout début de run (retour du 26 septembre) : premier coup reçu et dégâts cumulés à 10 et 30 s.
        double firstHit = -1, damage10 = 0, damage30 = 0;
        double startedAt = Time.GetTicksMsec() / 1000.0;
        EventBus.PlayerHitByEventHandler onHit = (_, damage) =>
        {
            hitDamage += damage;
            double at = Time.GetTicksMsec() / 1000.0 - startedAt;
            if (firstHit < 0)
                firstHit = at;
            if (at <= 10)
                damage10 += damage;
            if (at <= 30)
                damage30 += damage;
        };
        eventBus.PlayerHitBy += onHit;
        List<int> visibleSamples = new();
        Dictionary<int, double> levelTimes = new();
        int lastLevel = 1;
        double firstVisible = -1;
        RandomNumberGenerator rng = new() { Seed = seed };
        Vector2 waypoint = _player.GlobalPosition;
        double start = Time.GetTicksMsec() / 1000.0;
        double nextSample = 1.0;
        double nextCapture = _captureEvery;
        Vestiges.Events.RunEventDirector director = _world.GetNode<Vestiges.Events.RunEventDirector>("RunEventDirector");
        PlayerProgressionAccessor progression = new(_player);
        ProcessMode = ProcessModeEnum.Always;
        double pausedSeconds = 0;
        Vector2 lastProgressPosition = _player.GlobalPosition;
        double lastProgressTime = 0;

        while (true)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            if (GetTree().Paused)
            {
                // L'écran de niveau fige la run : choisir la première offre et exclure ce temps de la mesure.
                double pauseStart = Time.GetTicksMsec() / 1000.0;
                AutoPickLevelUp();
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                pausedSeconds += Time.GetTicksMsec() / 1000.0 - pauseStart;
                continue;
            }
            double t = Time.GetTicksMsec() / 1000.0 - start - pausedSeconds;
            if (t >= seconds)
                break;

            // Déplacement nomade déterministe : nouveau point de passage à 500–900 px dès l'arrivée,
            // ou si le joueur est bloqué (obstacle, Néant) depuis deux secondes.
            if (t - lastProgressTime > 2.0)
            {
                waypoint = _player.GlobalPosition;
                lastProgressTime = t;
            }
            if (_player.GlobalPosition.DistanceTo(lastProgressPosition) > 60f)
            {
                lastProgressPosition = _player.GlobalPosition;
                lastProgressTime = t;
            }
            if (_player.GlobalPosition.DistanceTo(waypoint) < 40f && !director.IsEventActive)
                waypoint = _player.GlobalPosition + Vector2.FromAngle(rng.RandfRange(0f, Mathf.Tau)) * rng.RandfRange(500f, 900f);
            // Comme un joueur, le bot suit la cible d'un micro-événement en cours (vestige, veille, Souverain).
            if (director.TryGetActiveTarget(out Vector2 eventTarget))
                waypoint = eventTarget;
            _player.AIInputOverride = _player.GlobalPosition.DistanceTo(waypoint) > 12f
                ? (waypoint - _player.GlobalPosition).Normalized()
                : Vector2.Zero;

            int level = progression.Level;
            if (level > lastLevel)
            {
                for (int l = lastLevel + 1; l <= level; l++)
                    levelTimes[l] = t;
                lastLevel = level;
            }

            if (_forcedEvent != null && t >= 3.0)
            {
                _world.GetNode<Vestiges.Events.RunEventDirector>("RunEventDirector").ForceStart(_forcedEvent);
                _forcedEvent = null;
            }

            if (_captureEvery > 0 && t >= nextCapture)
            {
                nextCapture += _captureEvery;
                using Image frame = GetViewport().GetTexture().GetImage();
                frame.SavePng(string.Create(CultureInfo.InvariantCulture, $"{_output}/screen-{t:000}s.png"));
            }

            if (t < nextSample)
                continue;
            nextSample += 1.0;

            Rect2 view = VisibleWorldRect();
            int visible = 0, near = 0, alive = 0;
            foreach (Node node in GetTree().GetNodesInGroup("enemies"))
            {
                if (node is not Enemy enemy || !enemy.IsActive || enemy.IsDying)
                    continue;
                alive++;
                if (view.HasPoint(enemy.GlobalPosition))
                    visible++;
                if (enemy.GlobalPosition.DistanceTo(_player.GlobalPosition) <= 600f)
                    near++;
            }
            if (visible > 0 && firstVisible < 0)
                firstVisible = t;
            visibleSamples.Add(visible);
            rows.Add(string.Create(CultureInfo.InvariantCulture,
                $"{t:F0},{visible},{near},{alive},{tracker.TotalSpawned},{tracker.TotalKilled},{level},{hitDamage:F0}"));
        }

        eventBus.PlayerHitBy -= onHit;
        using (FileAccess csv = FileAccess.Open($"{_output}/density-{seed}.csv", FileAccess.ModeFlags.Write))
            csv.StoreString(string.Join("\n", rows) + "\n");

        StringBuilder summary = new();
        summary.Append(CultureInfo.InvariantCulture, $"seed={seed} seconds={seconds:F0} first_visible_s={firstVisible:F0}");
        summary.Append(CultureInfo.InvariantCulture, $" first_hit_s={firstHit:F1} damage_10s={damage10:F0} damage_30s={damage30:F0}");
        summary.Append(CultureInfo.InvariantCulture, $" kills={tracker.TotalKilled} spawned={tracker.TotalSpawned}");
        foreach ((int from, int to) in new[] { (0, 60), (60, 120), (120, 180), (180, 300) })
        {
            if (from >= visibleSamples.Count)
                break;
            int end = Math.Min(to, visibleSamples.Count);
            double sum = 0;
            int empty = 0;
            for (int i = from; i < end; i++)
            {
                sum += visibleSamples[i];
                if (visibleSamples[i] == 0)
                    empty++;
            }
            summary.Append(CultureInfo.InvariantCulture,
                $" | {from}-{end}s visible_mean={sum / (end - from):F1} empty={100.0 * empty / (end - from):F0}%");
        }
        foreach (KeyValuePair<int, double> entry in levelTimes)
            summary.Append(CultureInfo.InvariantCulture, $" L{entry.Key}={entry.Value:F0}s");
        GD.Print($"[RunObservation] RESULT {summary}");
    }

    private void AutoPickLevelUp()
    {
        Node screen = _world.GetNode("LevelUpScreen");
        object manager = screen.GetType().GetField("_fragmentManager", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(screen);
        if (manager is not Vestiges.Progression.FragmentManager fragments || !fragments.IsChoiceActive || fragments.PendingChoices.Count == 0)
            return;
        Vestiges.Progression.FragmentOption choice = fragments.PendingChoices[0];
        screen.GetType().GetMethod("OnFragmentSelected", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(screen, new object[] { choice.Id, choice.Type });
    }

    private Rect2 VisibleWorldRect()
    {
        Vector2 size = GetViewport().GetVisibleRect().Size / _camera.Zoom;
        return new Rect2(_camera.GetScreenCenterPosition() - size / 2f, size);
    }

    private static void ResetAbilityCooldowns(Enemy enemy)
    {
        var cache = (Dictionary<string, IEnemyAbility>)typeof(Enemy)
            .GetField("_abilityCache", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(enemy);
        foreach (IEnemyAbility ability in cache.Values)
            ability.GetType().GetField("_cooldownTimer", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(ability, 0f);
    }

    private async Task Frames(int count)
    {
        for (int i = 0; i < count; i++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private static string Argument(string[] args, string name, string fallback)
    {
        int index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : fallback;
    }

    /// <summary>Lecture du niveau courant sans dépendre du chemin de nœud de la progression.</summary>
    private readonly struct PlayerProgressionAccessor
    {
        private readonly Vestiges.Progression.PlayerProgression _progression;

        public PlayerProgressionAccessor(Player player)
        {
            _progression = player.GetNode<Vestiges.Progression.PlayerProgression>("PlayerProgression");
        }

        public int Level => _progression.CurrentLevel;
    }
}
