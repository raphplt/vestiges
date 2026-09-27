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
/// --capture-abilities [--enemies a,b] [--shots n] [--still] : captures des attaques d'ennemis choisis (Présage et Charognard par défaut) ;
///     --still garde le joueur immobile, pour les attaques qui visent sa position (surgissement).
/// --capture-map : répartition des biomes autour du spawn (plusieurs seeds) et vues dézoomées.
/// --capture-props : zone la plus chargée en décors de chaque biome, collisions affichées (sauf --hide-collisions).
/// --capture-junctions : frontières entre biomes les plus proches du départ, avec et sans décors.
/// --capture-paths : chemins de terre par biome, raccord à une rue, vue dézoomée du départ (RunObservation.Paths.cs).
/// --capture-farms : fermes des Champs Sauvages les plus proches du départ, normal et dézoomé (RunObservation.Farms.cs).
/// --capture-landmarks : églises et pylônes des Ruines Urbaines, normal et dézoomé (RunObservation.UrbanLandmarks.cs).
/// --capture-levelup-fx : effet de montée de niveau au ralenti, puis entrée de l'écran de choix (RunObservation.LevelUpFx.cs).
/// --capture-crowd : foule de 60 créatures, recul de caméra puis compteur de morts en rafale (RunObservation.Crowd.cs).
/// --capture-micro : coffre qui frémit à l'approche, poussière de pas (RunObservation.Micro.cs).
/// --capture-death [--seconds 8] : mort réelle après quelques secondes, bilan de fin de run (RunObservation.Death.cs).
/// --capture-echoes : échos de l'oubli forcés en zone Fragile, apparition, dissolution, murmure (RunObservation.Echoes.cs).
/// --capture-erasure : une capture par phase de l'oubli, puis un dégradé de toutes les phases.
/// --measure-props [--measure-seconds 8] : coût de rendu des décors par biome (RunObservation.PropCost.cs).
/// --capture-weapons [--weapons a,b] [--lethal] : galerie des attaques du joueur, cibles qui meurent au premier coup avec --lethal (RunObservation.Weapons.cs).
/// --capture-held [--weapons a,b] : arme en main dans les huit directions et pendant un coup (RunObservation.HeldWeapon.cs).
/// --capture-chests : chaque coffre cadré, avec et sans décors (RunObservation.Chests.cs).
/// --capture-levelup : l'écran de level-up, une capture par rareté (RunObservation.LevelUp.cs).
/// --capture-oublis : les neuf Oublis de carte pris d'un coup, effets mesurés (RunObservation.Oublis.cs).
/// --capture-rift : offre d'une Faille, Péril et Oubli dans la pause, Oubli levé au Mémorial (RunObservation.Landmarks.cs).
/// --capture-memorial : parcours complet d'un Mémorial, du réveil aux services (RunObservation.Landmarks.cs).
/// --loot-draws N : tirages de butin de chaque coffre, sans les appliquer (RunObservation.Chests.cs).
/// --capture-bestiary : gros plans des créatures du pilote de sprites procéduraux, autour du joueur immobile.
/// --density : mesure de densité en spawn naturel (ennemis visibles, temps sans ennemi, débits, niveaux,
/// coffres entrés dans le cadre, et parmi eux ceux qu'aucun décor ne masquait).
/// --nomad : pendant la mesure, le bot garde un cap (tiré de la seed) au lieu d'errer autour du départ,
/// et en change quand il bute sur le bord du monde.
/// --peril N : pendant la mesure, la run commence avec N points de Péril (plan 17 lot 3A).
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
            else if (Array.IndexOf(args, "--capture-junctions") >= 0)
                await CaptureJunctions();
            else if (Array.IndexOf(args, "--capture-farms") >= 0)
                await CaptureFarms();
            else if (Array.IndexOf(args, "--capture-landmarks") >= 0)
                await CaptureLandmarks();
            else if (Array.IndexOf(args, "--capture-levelup-fx") >= 0)
                await CaptureLevelUpFx();
            else if (Array.IndexOf(args, "--capture-crowd") >= 0)
                await CaptureCrowd();
            else if (Array.IndexOf(args, "--capture-micro") >= 0)
                await CaptureMicro();
            else if (Array.IndexOf(args, "--capture-death") >= 0)
                await CaptureDeath(double.Parse(Argument(args, "--seconds", "8"), CultureInfo.InvariantCulture));
            else if (Array.IndexOf(args, "--capture-echoes") >= 0)
                await CaptureEchoes();
            else if (Array.IndexOf(args, "--capture-paths") >= 0)
                await CapturePaths();
            else if (Array.IndexOf(args, "--capture-erasure") >= 0)
                await CaptureErasure();
            else if (Array.IndexOf(args, "--capture-abilities") >= 0)
                await CaptureAbilities();
            else if (Array.IndexOf(args, "--measure-props") >= 0)
                await MeasurePropCost(double.Parse(Argument(args, "--measure-seconds", "8"), CultureInfo.InvariantCulture));
            else if (Array.IndexOf(args, "--capture-weapons") >= 0)
                await CaptureWeapons(Argument(args, "--weapons", null), Array.IndexOf(args, "--lethal") >= 0);
            else if (Array.IndexOf(args, "--capture-chests") >= 0)
                await CaptureChests();
            else if (Array.IndexOf(args, "--capture-levelup") >= 0)
                await CaptureLevelUp();
            else if (Array.IndexOf(args, "--capture-pause") >= 0)
                await CapturePause();
            else if (Array.IndexOf(args, "--capture-memorial") >= 0)
                await CaptureMemorial();
            else if (Array.IndexOf(args, "--capture-rift") >= 0)
                await CaptureRift();
            else if (Array.IndexOf(args, "--capture-oublis") >= 0)
                await CaptureOublis();
            else if (Array.IndexOf(args, "--loot-draws") >= 0)
                MeasureLootDraws(int.Parse(Argument(args, "--loot-draws", "1000"), CultureInfo.InvariantCulture));
            else if (Array.IndexOf(args, "--capture-bestiary") >= 0)
                await CaptureBestiary();
            else if (Array.IndexOf(args, "--capture-held") >= 0)
                await CaptureHeldWeapons(Argument(args, "--weapons", null));
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
        // L'écran de chargement s'efface après l'initialisation du monde.
        await Frames(90);

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

        bool still = Array.IndexOf(OS.GetCmdlineUserArgs(), "--still") >= 0;
        _player.AIInputOverride = still ? Vector2.Zero : new Vector2(0.5f, 0f);
        int shots = int.Parse(Argument(OS.GetCmdlineUserArgs(), "--shots", "12"), CultureInfo.InvariantCulture);
        for (int shot = 0; shot < shots; shot++)
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
        int peril = int.Parse(Argument(OS.GetCmdlineUserArgs(), "--peril", "0"), CultureInfo.InvariantCulture);
        if (peril > 0)
            _world.GetNode<Vestiges.Progression.PerilManager>("PerilManager").AddPeril(peril);
        List<string> rows = new() { "t,visible,near600,alive,spawned,killed,level,hit_damage" };
        // Indice de pression : dégâts que les ennemis infligent à un joueur qui n'esquive jamais (invincible ici).
        double hitDamage = 0;
        EventBus eventBus = GetNode<EventBus>("/root/EventBus");
        // Souffle du tout début de run (retour du 26 septembre) : premier coup reçu et dégâts cumulés à 10 et 30 s.
        double firstHit = -1, damage10 = 0, damage30 = 0;
        // Temps de jeu, pauses exclues : la mesure vaut la même chose en temps réel et en headless
        // accéléré (--fixed-fps), où une seconde de jeu dure bien moins qu'une seconde d'horloge.
        double gameTime = 0;
        int pausedFrames = 0;
        EventBus.PlayerHitByEventHandler onHit = (_, damage) =>
        {
            hitDamage += damage;
            double at = gameTime;
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
        double nextSample = 1.0;
        double nextCapture = _captureEvery;
        // Coffres entrés dans le cadre (plan 17 lot 0A) : échantillonnés quatre fois par seconde,
        // un coffre traverse l'écran en plus de deux secondes à la vitesse du joueur.
        HashSet<ulong> seenChests = new();
        HashSet<ulong> clearChests = new();
        // Signalé : le coffre, le haut de sa colonne ou sa flèche de bord d'écran est visible.
        HashSet<ulong> signaledChests = new();
        float pointerRange = ChestDataLoader.LoadPlacement().PointerRangePx;
        double firstChestSeen = -1;
        double nextChestSample = 0.0;
        List<(Rect2 Rect, float SortY, bool Canopy)> propRects = CollectPropRects();
        bool nomad = Array.IndexOf(OS.GetCmdlineUserArgs(), "--nomad") >= 0;
        float heading = rng.RandfRange(0f, Mathf.Tau);
        Vestiges.Events.RunEventDirector director = _world.GetNode<Vestiges.Events.RunEventDirector>("RunEventDirector");
        PlayerProgressionAccessor progression = new(_player);
        ProcessMode = ProcessModeEnum.Always;
        Vector2 lastProgressPosition = _player.GlobalPosition;
        double lastProgressTime = 0;

        while (true)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            if (GetTree().Paused)
            {
                // L'écran de niveau fige la run : choisir la première offre ; ce temps ne compte pas.
                AutoPickLevelUp();
                if (++pausedFrames > 3000)
                    throw new InvalidOperationException("Run en pause depuis 3 000 frames : écran bloquant non géré par le banc.");
                continue;
            }
            pausedFrames = 0;
            gameTime += GetProcessDeltaTime();
            double t = gameTime;
            if (t >= seconds)
                break;

            // Déplacement nomade déterministe : nouveau point de passage à 500–900 px dès l'arrivée,
            // ou si le joueur est bloqué (obstacle, Néant) depuis deux secondes.
            if (t - lastProgressTime > 2.0)
            {
                waypoint = _player.GlobalPosition;
                lastProgressTime = t;
                if (nomad)
                    heading += rng.RandfRange(Mathf.Pi * 0.5f, Mathf.Pi);
            }
            if (_player.GlobalPosition.DistanceTo(lastProgressPosition) > 60f)
            {
                lastProgressPosition = _player.GlobalPosition;
                lastProgressTime = t;
            }
            if (_player.GlobalPosition.DistanceTo(waypoint) < 40f && !director.IsEventActive)
            {
                float angle = nomad ? heading + rng.RandfRange(-0.6f, 0.6f) : rng.RandfRange(0f, Mathf.Tau);
                waypoint = _player.GlobalPosition + Vector2.FromAngle(angle) * rng.RandfRange(500f, 900f);
            }
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

            if (t >= nextChestSample)
            {
                nextChestSample += 0.25;
                Rect2 chestView = VisibleWorldRect();
                foreach (Node node in GetTree().GetNodesInGroup("chests"))
                {
                    if (node is not Chest chest)
                        continue;
                    bool columnVisible = chestView.HasPoint(chest.GlobalPosition + new Vector2(0f, -chest.ColumnHeight * 0.6f));
                    if (columnVisible || chest.GlobalPosition.DistanceTo(_camera.GetScreenCenterPosition()) <= pointerRange)
                        signaledChests.Add(chest.GetInstanceId());
                    if (!chestView.HasPoint(chest.GlobalPosition))
                        continue;
                    signaledChests.Add(chest.GetInstanceId());
                    if (seenChests.Add(chest.GetInstanceId()) && firstChestSeen < 0)
                        firstChestSeen = t;
                    if (!IsChestMasked(chest.GlobalPosition, propRects))
                        clearChests.Add(chest.GetInstanceId());
                }
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
        summary.Append(CultureInfo.InvariantCulture, $" view={VisibleWorldRect().Size.X:F0}x{VisibleWorldRect().Size.Y:F0}");
        summary.Append(CultureInfo.InvariantCulture, $" first_hit_s={firstHit:F1} damage_10s={damage10:F0} damage_30s={damage30:F0}");
        int openRifts = 0;
        foreach (Rift rift in Rift.All)
            if (rift.IsOpen)
                openRifts++;
        summary.Append(CultureInfo.InvariantCulture, $" rifts={Rift.All.Count} rifts_open={openRifts}");
        summary.Append(CultureInfo.InvariantCulture, $" peril={peril} kills={tracker.TotalKilled} spawned={tracker.TotalSpawned}");
        summary.Append(CultureInfo.InvariantCulture,
            $" chests_total={GetTree().GetNodesInGroup("chests").Count} chests_seen={seenChests.Count} chests_clear={clearChests.Count} chests_signaled={signaledChests.Count} first_chest_s={firstChestSeen:F0}");
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

    /// <summary>Silhouettes des décors (fixes) : un décor masque un coffre s'il est dessiné devant lui.</summary>
    private List<(Rect2 Rect, float SortY, bool Canopy)> CollectPropRects()
    {
        List<(Rect2, float, bool)> rects = new();
        Stack<Node> pending = new();
        pending.Push(_world.GetNode("PropContainer"));
        while (pending.Count > 0)
        {
            foreach (Node child in pending.Pop().GetChildren())
            {
                if (child is EnvironmentProp prop)
                    rects.Add((prop.VisibleWorldRect(), prop.GlobalPosition.Y, prop.HasCanopy));
                else
                    pending.Push(child);
            }
        }
        return rects;
    }

    /// <summary>Le haut du coffre est couvert par un décor trié devant lui, ou par une canopée.</summary>
    private static bool IsChestMasked(Vector2 chest, List<(Rect2 Rect, float SortY, bool Canopy)> props)
    {
        Vector2 top = chest + new Vector2(0f, -12f);
        foreach ((Rect2 rect, float sortY, bool canopy) in props)
        {
            if (rect.HasPoint(top) && (sortY > chest.Y || canopy))
                return true;
        }
        return false;
    }

    private void AutoPickLevelUp()
    {
        // Écran de choix (Mémorial, Faille) : le bot sort quand c'est permis, sinon prend la première carte.
        if (_world.GetNodeOrNull<Vestiges.UI.ChoiceScreen>("ChoiceScreen") is { IsOpen: true } choices)
        {
            choices.Activate(choices.CanCancel ? int.MaxValue : 0);
            return;
        }
        Node screen = _world.GetNode("LevelUpScreen");
        object manager = screen.GetType().GetField("_fragmentManager", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(screen);
        if (manager is not Vestiges.Progression.FragmentManager fragments || !fragments.IsChoiceActive || fragments.PendingChoices.Count == 0)
            return;
        Vestiges.Progression.FragmentOption choice = fragments.PendingChoices[0];
        screen.GetType().GetMethod("OnCardChosen", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(screen, new object[] { choice });
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
        {
            ability.GetType().GetField("_cooldownTimer", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(ability, 0f);
            // Enfouissement : la phase de surface se compte sur le minuteur de phase.
            if (ability is BurrowAbility)
                ability.GetType().GetField("_timer", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(ability, 0f);
        }
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
