using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Godot;
using Vestiges.Combat;

namespace Vestiges.Tests;

/// <summary>
/// Durées d'image d'une vraie run par tranche de temps de jeu (plan 29) : FPS moyen, p99, pire image et créatures en
/// vie. Le banc dense mesure une foule fixe ; ici comptent les vagues, les morts en masse et les événements. N'a de sens
/// qu'en temps réel (capture_run), pas en headless accéléré. Les pauses ne comptent pas.
/// </summary>
public sealed class RunFrameStats
{
    private readonly double _window;
    private readonly List<double> _frames = new();
    private readonly List<string> _rows = new() { "t_start,t_end,alive_max,fps,p99_ms,max_ms,frames,allocated_mb,gc0,gc1,gc2,gc_pause_ms,heap_mb,promoted_mb,pinned,handles_info" };
    private double _windowStart;
    private int _aliveMax;
    private ulong _previous;
    // Images de plus de SpikeMs : instant, foule et passages du ramasse-miettes pendant l'image, pour les attribuer.
    private const double SpikeMs = 50.0;
    private readonly List<string> _spikes = new() { "t,ms,alive,kills,spawns,gc0,gc1,gc2,physics_scripts_ms,physics_step_ms,process_ms,render_ms,render_cpu_ms,render_gpu_ms,clock_s" };
    private int _kills, _spawns;

    /// <summary>Morts et apparitions de l'image en cours, comptées par l'observateur.</summary>
    public void CountKill() => _kills++;

    public void CountSpawn() => _spawns++;
    private readonly int[] _gcCounts = new int[3];
    // Mémoire allouée et passages du ramasse-miettes par tranche : ses pauses font les à-coups restants (plan 29).
    private readonly long[] _lastLogged = new long[4];
    private readonly List<string> _collections = new() { "t,index,kind,generation,pause_ms,compacted,concurrent,promoted_mb,heap_mb,fragmented_mb" };
    private long _windowAllocated = System.GC.GetTotalAllocatedBytes();
    private System.TimeSpan _windowPause = System.GC.GetTotalPauseDuration();
    private readonly int[] _windowGc = { System.GC.CollectionCount(0), System.GC.CollectionCount(1), System.GC.CollectionCount(2) };

    private FrameSplit _split;
    private (double PhysicsScripts, double PhysicsStep, double Process, double Render) _lastTotals;

    public RunFrameStats(double windowSeconds)
    {
        _window = windowSeconds;
    }

    /// <summary>Découpe chaque image lente par poste (marqueurs en tête et en queue des files de traitement).</summary>
    public void Attach(Node root)
    {
        _split = FrameSplit.Attach(root);
        _viewport = root.GetViewport().GetViewportRid();
        RenderingServer.ViewportSetMeasureRenderTime(_viewport, true);
    }

    private Rid _viewport;

    /// <summary>Pause : l'image qui la suit ne compte pas la durée de la pause.</summary>
    public void Pause() => _previous = 0;

    public void Sample(double gameTime)
    {
        ulong now = Time.GetTicksUsec();
        (double PhysicsScripts, double PhysicsStep, double Process, double Render) totals = _split?.Totals() ?? default;
        if (_previous != 0)
        {
            double ms = (now - _previous) / 1000.0;
            _frames.Add(ms);
            if (ms > SpikeMs)
            {
                _spikes.Add(string.Format(CultureInfo.InvariantCulture, "{0:F1},{1:F1},{2},{3},{4},{5},{6},{7},{8:F1},{9:F1},{10:F1},{11:F1},{12:F1},{13:F1},{14:F1}", gameTime, ms, CrowdIndex.Count, _kills, _spawns,
                    System.GC.CollectionCount(0) - _gcCounts[0], System.GC.CollectionCount(1) - _gcCounts[1], System.GC.CollectionCount(2) - _gcCounts[2],
                    totals.PhysicsScripts - _lastTotals.PhysicsScripts, totals.PhysicsStep - _lastTotals.PhysicsStep,
                    totals.Process - _lastTotals.Process, totals.Render - _lastTotals.Render,
                    RenderingServer.ViewportGetMeasuredRenderTimeCpu(_viewport) + RenderingServer.GetFrameSetupTimeCpu(),
                    RenderingServer.ViewportGetMeasuredRenderTimeGpu(_viewport), now / 1e6));
            }
        }
        if (System.GC.CollectionCount(0) != _gcCounts[0])
            LogCollections(gameTime);
        _kills = 0;
        _spawns = 0;
        _lastTotals = totals;
        for (int generation = 0; generation < 3; generation++)
            _gcCounts[generation] = System.GC.CollectionCount(generation);
        _previous = now;
        _aliveMax = Mathf.Max(_aliveMax, CrowdIndex.Count);
        if (gameTime - _windowStart >= _window)
            Flush(gameTime);
    }

    public void Write(string path, double gameTime)
    {
        Flush(gameTime);
        File.WriteAllLines(path, _rows);
        File.WriteAllLines(Path.ChangeExtension(path, null) + "-spikes.csv", _spikes);
        File.WriteAllLines(Path.ChangeExtension(path, null) + "-gc.csv", _collections);
        foreach (string collection in _collections.Skip(1))
            GD.Print($"[RunObservation] GC {collection}");
        foreach (string row in _rows.Skip(1))
            GD.Print($"[RunObservation] FRAMES {row}");
        foreach (string spike in _spikes.Skip(1))
            GD.Print($"[RunObservation] SPIKE {spike}");
    }

    /// <summary>Une ligne par collecte : genre, génération, pause, compactage, octets promus.</summary>
    private void LogCollections(double gameTime)
    {
        foreach (System.GCKind kind in new[] { System.GCKind.Ephemeral, System.GCKind.FullBlocking, System.GCKind.Background })
        {
            System.GCMemoryInfo info = System.GC.GetGCMemoryInfo(kind);
            if (info.Index <= _lastLogged[(int)kind])
                continue;
            _lastLogged[(int)kind] = info.Index;
            double pause = 0;
            foreach (System.TimeSpan span in info.PauseDurations)
                pause += span.TotalMilliseconds;
            _collections.Add(string.Format(CultureInfo.InvariantCulture, "{0:F1},{1},{2},{3},{4:F1},{5},{6},{7:F1},{8:F1},{9:F1}", gameTime, info.Index, kind, info.Generation,
                pause, info.Compacted, info.Concurrent, info.PromotedBytes / 1e6, info.HeapSizeBytes / 1e6, info.FragmentedBytes / 1e6));
        }
    }

    private void Flush(double gameTime)
    {
        if (_frames.Count > 0)
        {
            List<double> sorted = _frames.OrderBy(ms => ms).ToList();
            double p99 = sorted[Mathf.Min(sorted.Count - 1, (int)(sorted.Count * 0.99))];
            double fps = 1000.0 * sorted.Count / sorted.Sum();
            long allocated = System.GC.GetTotalAllocatedBytes();
            System.GCMemoryInfo info = System.GC.GetGCMemoryInfo();
            _rows.Add(string.Format(CultureInfo.InvariantCulture, "{0:F0},{1:F0},{2},{3:F1},{4:F2},{5:F2},{6},{7:F1},{8},{9},{10},{11:F1},{12:F1},{13:F1},{14},{15}",
                _windowStart, gameTime, _aliveMax, fps, p99, sorted[^1], sorted.Count, (allocated - _windowAllocated) / 1e6,
                System.GC.CollectionCount(0) - _windowGc[0], System.GC.CollectionCount(1) - _windowGc[1], System.GC.CollectionCount(2) - _windowGc[2],
                (System.GC.GetTotalPauseDuration() - _windowPause).TotalMilliseconds, info.HeapSizeBytes / 1e6, info.PromotedBytes / 1e6,
                info.PinnedObjectsCount, info.Generation));
        }
        _frames.Clear();
        _aliveMax = 0;
        _windowStart = gameTime;
        _windowAllocated = System.GC.GetTotalAllocatedBytes();
        _windowPause = System.GC.GetTotalPauseDuration();
        for (int generation = 0; generation < 3; generation++)
            _windowGc[generation] = System.GC.CollectionCount(generation);
    }
}
