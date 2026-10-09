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
    private readonly List<string> _rows = new() { "t_start,t_end,alive_max,fps,p99_ms,max_ms,frames" };
    private double _windowStart;
    private int _aliveMax;
    private ulong _previous;

    public RunFrameStats(double windowSeconds)
    {
        _window = windowSeconds;
    }

    /// <summary>Pause : l'image qui la suit ne compte pas la durée de la pause.</summary>
    public void Pause() => _previous = 0;

    public void Sample(double gameTime)
    {
        ulong now = Time.GetTicksUsec();
        if (_previous != 0)
            _frames.Add((now - _previous) / 1000.0);
        _previous = now;
        _aliveMax = Mathf.Max(_aliveMax, CrowdIndex.Count);
        if (gameTime - _windowStart >= _window)
            Flush(gameTime);
    }

    public void Write(string path, double gameTime)
    {
        Flush(gameTime);
        File.WriteAllLines(path, _rows);
        foreach (string row in _rows.Skip(1))
            GD.Print($"[RunObservation] FRAMES {row}");
    }

    private void Flush(double gameTime)
    {
        if (_frames.Count > 0)
        {
            List<double> sorted = _frames.OrderBy(ms => ms).ToList();
            double p99 = sorted[Mathf.Min(sorted.Count - 1, (int)(sorted.Count * 0.99))];
            double fps = 1000.0 * sorted.Count / sorted.Sum();
            _rows.Add(string.Format(CultureInfo.InvariantCulture, "{0:F0},{1:F0},{2},{3:F1},{4:F2},{5:F2},{6}",
                _windowStart, gameTime, _aliveMax, fps, p99, sorted[^1], sorted.Count));
        }
        _frames.Clear();
        _aliveMax = 0;
        _windowStart = gameTime;
    }
}
