using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Spawn;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>Banc rendu de Main : aucune modification des données ou du gameplay de production.</summary>
public partial class MovementDenseBenchmark : Node
{
    private const int EnemyCount = 120;
    private const ulong Seed = 221092026;
    private readonly Enemy[] _enemies = new Enemy[EnemyCount];
    private readonly double[] _frames = new double[200000];
    private readonly double[] _activationFrames = new double[200000];
    private Player _player;
    private WorldSetup _world;
    private bool _ready;
    private bool _finished;
    private bool _dash;
    private string _output;
    private double _duration;
    private double _warmup;
    private double _physicsTime;
    private ulong _start;
    private ulong _previous;
    private int _samples;
    private int _activationSamples;
    private int _activations;
    private bool _activationPending;
    private bool _previousDash;
    private double _lastDashRequest = -10;
    private int _minLiving = EnemyCount;
    private int _maxLiving;
    private int _minFullAi = EnemyCount;
    private double _processSum;
    private double _physicsSum;
    private long _managedStart;
    private long _allocatedStart;
    // Nœuds ajoutés à l'arbre pendant la mesure : coût des effets créés puis libérés (plan 02 J0).
    private long _nodesAdded;
    private readonly Dictionary<string, int> _nodesAddedByName = new();
    private long _rssStart;
    private double _nativeStart;
    private Label _label;
    private Vector2I _requestedSize;
    private Vector2I _renderSize;
    private Vector2 _previousPosition;
    private double _distance;
    private double _dashDistance;

    public override async void _Ready()
    {
        try
        {
            ProcessMode = ProcessModeEnum.Always;
            ProcessPhysicsPriority = 100;
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            string[] args = OS.GetCmdlineUserArgs();
            _dash = Array.IndexOf(args, "--dash") >= 0;
            _output = Argument(args, "--output", "/tmp/vestiges-dense");
            _duration = double.Parse(Argument(args, "--seconds", "20"), CultureInfo.InvariantCulture);
            _warmup = double.Parse(Argument(args, "--warmup", "5"), CultureInfo.InvariantCulture);
            Vector2I size = new(int.Parse(Argument(args, "--width", "1280")), int.Parse(Argument(args, "--height", "720")));
            _requestedSize = size;
            GetWindow().Mode = Window.ModeEnum.Windowed;
            GetWindow().ContentScaleSize = size;
            GetWindow().ContentScaleFactor = 1;
            GetWindow().Size = size;
            DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
            Engine.MaxFps = 0;
            GD.Seed(Seed);
            GameManager manager = GetNode<GameManager>("/root/GameManager");
            manager.RunSeed = Seed;
            manager.SelectedCharacterId = "traqueur";
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
            _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
            _world.GetNode("ErasureManager").ProcessMode = ProcessModeEnum.Disabled;
            _world.GetNode("CrisisManager").ProcessMode = ProcessModeEnum.Disabled;
            // Main signale prêt avant la fin des 200 lots de brouillard différés.
            // Attendre leur fin exclut ce chargement du coût du combat mesuré.
            FogOfWar fog = _world.GetNode<FogOfWar>("FogOfWar");
            FieldInfo fogInitializing = typeof(FogOfWar).GetField("_initPhase", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Contrat de préparation FogOfWar introuvable.");
            while ((bool)fogInitializing.GetValue(fog)!)
            {
                if (Time.GetTicksMsec() > timeout)
                    throw new InvalidOperationException("Préparation FogOfWar supérieure à 120 s.");
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            // Éliminer les gardes issus du placement procédural pour fixer exactement la population.
            foreach (Node node in GetTree().GetNodesInGroup("enemies"))
                node.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            _player = _world.GetNode<Player>("Player");
            _player.IsGodMode = true;
            _player.IsAIControlled = true;
            _player.GlobalPosition = Vector2.Zero;
            EnemyPool pool = _world.GetNode<EnemyPool>("EnemyPool");
            Node container = _world.GetNode("EnemyContainer");
            GD.Seed(Seed);
            for (int i = 0; i < EnemyCount; i++)
            {
                Enemy enemy = pool.Get();
                float angle = i * 2.3999632f;
                float radius = 40f + 100f * Mathf.Sqrt((i + 1f) / EnemyCount);
                enemy.Position = Vector2.FromAngle(angle) * radius;
                container.AddChild(enemy);
                // Les HP renforcés conservent les 120 IA, attaques, collisions et impacts pendant l'essai.
                enemy.Initialize(EnemyDataLoader.Get(i < 100 ? "shade" : "fading_spitter"), 10000f, 1f);
                _enemies[i] = enemy;
            }
            CanvasLayer layer = new() { Layer = 100 };
            AddChild(layer);
            _label = new Label { Position = new Vector2(16, 88), Text = $"BENCH Main / {_dash} dash / 120 ennemis / {size.X}x{size.Y}" };
            _label.AddThemeFontSizeOverride("font_size", 20);
            _label.AddThemeColorOverride("font_shadow_color", Colors.Black);
            layer.AddChild(_label);
            GetWindow().Mode = Window.ModeEnum.Windowed;
            GetWindow().Size = size;
            GD.Print("[MovementDenseBenchmark] Préparation terminée ; attente de la première image rendue.");
            await WaitForRenderedFrame();
            using (Image initial = GetViewport().GetTexture().GetImage())
                _renderSize = initial.GetSize();
            _start = _previous = Time.GetTicksUsec();
            _ready = true;
            GD.Print($"[MovementDenseBenchmark] Chauffe puis mesure ; image {_renderSize}.");
        }
        catch (Exception ex)
        {
            GD.PushError($"[MovementDenseBenchmark] {ex}");
            GetTree().Quit(1);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_ready || _finished)
            return;
        _physicsTime += delta;
        float distance = _player.GlobalPosition.DistanceTo(_previousPosition);
        _previousPosition = _player.GlobalPosition;
        if ((Time.GetTicksUsec() - _start) / 1e6 >= _warmup)
        {
            _distance += distance;
            if (_player.Mobility.IsDashStep)
                _dashDistance += distance;
        }
        // Même cible orbitale et vitesse de base ; le dash modifie réellement le trajet et les contacts.
        Vector2 target = Vector2.FromAngle((float)(_physicsTime * 0.8)) * 70f;
        _player.AIInputOverride = ((target - _player.GlobalPosition) / 20f).LimitLength();
        if (_player.Mobility.StartedThisStep)
        {
            _activationPending = true;
            if ((Time.GetTicksUsec() - _start) / 1e6 >= _warmup)
                _activations++;
        }
        if (_dash && _physicsTime - _lastDashRequest >= 2.65)
        {
            _player.Mobility.Request();
            _lastDashRequest = _physicsTime;
        }
    }

    public override void _Process(double delta)
    {
        if (!_ready || _finished)
            return;
        ulong now = Time.GetTicksUsec();
        double elapsed = (now - _start) / 1e6;
        double frameMs = (now - _previous) / 1000.0;
        _previous = now;
        if (elapsed < _warmup)
        {
            _activationPending = false;
            return;
        }
        if (_samples == 0)
        {
            _managedStart = GC.GetTotalMemory(false);
            _allocatedStart = GC.GetTotalAllocatedBytes();
            GetTree().NodeAdded += node =>
            {
                _nodesAdded++;
                // Nom de la racine d'effet (script C# ou classe native) : repère les sources à recycler.
                string name = node.GetScript().Obj is Script script ? script.ResourcePath.GetFile() : node.GetClass();
                _nodesAddedByName[name] = _nodesAddedByName.GetValueOrDefault(name) + 1;
            };
            _rssStart = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64;
            _nativeStart = Performance.GetMonitor(Performance.Monitor.MemoryStatic);
        }
        if (_samples >= _frames.Length)
        {
            GD.PushError("[MovementDenseBenchmark] Capacité d'échantillons dépassée.");
            GetTree().Quit(1);
            return;
        }
        _frames[_samples++] = frameMs;
        if (_activationPending || _previousDash || _player.Mobility.IsDashing)
            _activationFrames[_activationSamples++] = frameMs;
        _activationPending = false;
        _previousDash = _player.Mobility.IsDashing;
        _processSum += Performance.GetMonitor(Performance.Monitor.TimeProcess) * 1000;
        _physicsSum += Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess) * 1000;
        int living = 0;
        int fullAi = 0;
        foreach (Enemy enemy in _enemies)
        {
            if (IsInstanceValid(enemy) && enemy.IsActive && !enemy.IsDying && enemy.HpRatio > 0)
            {
                living++;
                if (enemy.GlobalPosition.DistanceSquaredTo(_player.GlobalPosition) <= 600f * 600f)
                    fullAi++;
            }
        }
        _minLiving = Math.Min(_minLiving, living);
        _maxLiving = Math.Max(_maxLiving, living);
        _minFullAi = Math.Min(_minFullAi, fullAi);
        if (elapsed >= _warmup + _duration)
        {
            _finished = true;
            Finish();
        }
    }

    private async void Finish()
    {
        try
        {
            long managedEnd = GC.GetTotalMemory(false);
            long allocated = GC.GetTotalAllocatedBytes() - _allocatedStart;
            using System.Diagnostics.Process process = System.Diagnostics.Process.GetCurrentProcess();
            long rssEnd = process.WorkingSet64;
            long rssPeak = process.PeakWorkingSet64;
            double nativeEnd = Performance.GetMonitor(Performance.Monitor.MemoryStatic);
            bool valid = _minLiving >= 100 && _minFullAi >= 100 && (!_dash || _activations >= 5)
                && !GetTree().Paused && DisplayServer.GetName() != "headless"
                && GetWindow().Size == _requestedSize && _renderSize == _requestedSize
                && _distance > 100 && (!_dash || _dashDistance > 50);
            var result = new
            {
                valid, dash = _dash, seed = Seed, warmup_seconds = _warmup, requested_seconds = _duration,
                measured_seconds = _frames.Take(_samples).Sum() / 1000,
                resolution = new[] { GetWindow().Size.X, GetWindow().Size.Y },
                viewport = new[] { _renderSize.X, _renderSize.Y },
                renderer = RenderingServer.GetCurrentRenderingMethod(), display = DisplayServer.GetName(),
                vsync = DisplayServer.WindowGetVsyncMode().ToString(), max_fps = Engine.MaxFps,
                cpu = OS.GetProcessorName(), cpu_threads = OS.GetProcessorCount(),
                gpu = RenderingServer.GetVideoAdapterName(), vendor = RenderingServer.GetVideoAdapterVendor(),
                engine = Engine.GetVersionInfo()["string"].AsString(),
                living_min = _minLiving, living_max = _maxLiving, full_ai_range_min = _minFullAi,
                activations = _activations, frames = Stats(_frames, _samples), dash_window_frames = Stats(_activationFrames, _activationSamples),
                traveled_pixels = _distance, dash_traveled_pixels = _dashDistance,
                // Noms historiques du premier banc : moniteurs Godot, pas un profil CPU isolé.
                process_cpu_mean_ms = _processSum / _samples, physics_cpu_mean_ms = _physicsSum / _samples,
                managed_start_bytes = _managedStart, managed_end_bytes = managedEnd, allocated_bytes = allocated,
                nodes_added = _nodesAdded,
                nodes_added_by_type = _nodesAddedByName.OrderByDescending(pair => pair.Value).Take(12).ToDictionary(pair => pair.Key, pair => pair.Value), nodes_added_per_second = _nodesAdded / (_frames.Take(_samples).Sum() / 1000),
                native_start_bytes = _nativeStart, native_end_bytes = nativeEnd,
                rss_start_bytes = _rssStart, rss_end_bytes = rssEnd, rss_process_peak_bytes = rssPeak,
                fixture = "Main réelle ; profil dev temporaire sans souvenir équipé, Steam désactivé ; 100 shade + 20 fading_spitter HP x10000 ; traqueur invincible, arme initiale active ; spawn naturel, Effacement et crises figés ; cible orbitale commune, trajectoires réelles différentes avec dash ; code courant dans les deux cas."
            };
            Directory.CreateDirectory(Path.GetDirectoryName(_output)!);
            File.WriteAllText(_output + ".json", JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
            using (StreamWriter csv = new(_output + ".frames.csv"))
            {
                csv.WriteLine("frame,wall_ms");
                for (int i = 0; i < _samples; i++)
                    csv.WriteLine($"{i},{_frames[i].ToString("F4", CultureInfo.InvariantCulture)}");
            }
            _label.Text = $"BENCH Main | {(_dash ? "DASH" : "SANS DASH")} | vivants {_minLiving}–{_maxLiving} | {_activations} dashs | {_samples} frames";
            // Le readback GPU et l'écriture PNG sont exclus de toutes les mesures.
            await WaitForRenderedFrame();
            using Image screenshot = GetViewport().GetTexture().GetImage();
            Error saved = screenshot.SavePng(_output + ".png");
            if (saved != Error.Ok)
                throw new IOException($"Capture : {saved}");
            GD.Print($"[MovementDenseBenchmark] RESULT valid={valid} output={_output}");
            GetTree().Quit(valid ? 0 : 1);
        }
        catch (Exception ex)
        {
            GD.PushError($"[MovementDenseBenchmark] {ex}");
            GetTree().Quit(1);
        }
    }

    private static object Stats(double[] source, int count)
    {
        if (count == 0)
            return null;
        double[] sorted = source.Take(count).Order().ToArray();
        double mean = sorted.Average();
        return new { count, mean_ms = mean, fps = 1000 / mean,
            p95_ms = sorted[Math.Clamp((int)Math.Ceiling(count * 0.95) - 1, 0, count - 1)],
            p99_ms = sorted[Math.Clamp((int)Math.Ceiling(count * 0.99) - 1, 0, count - 1)],
            max_ms = sorted[^1], over_16_67_percent = 100.0 * sorted.Count(v => v > 1000.0 / 60) / count };
    }

    private async Task WaitForRenderedFrame()
    {
        // Séparer l'attente du rendu du readback pour éviter une reprise réentrante ;
        // les blocages observés étaient avant mesure, sans cause moteur établie.
        int previous = Engine.GetFramesDrawn();
        ulong deadline = Time.GetTicksMsec() + 5000;
        do
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            if (Time.GetTicksMsec() > deadline)
                throw new TimeoutException("Aucune nouvelle image rendue depuis 5 s.");
        }
        while (Engine.GetFramesDrawn() <= previous);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private static string Argument(string[] args, string name, string fallback)
    {
        int index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : fallback;
    }
}
