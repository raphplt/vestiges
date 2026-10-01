using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Godot;
using Vestiges.Core;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>Observation facultative d'une vraie run : activité des zones et événements qui les ravivent.</summary>
internal sealed class ErasureRunProbe : IDisposable
{
    private readonly ErasureManager _erasure;
    private readonly Player _player;
    private readonly EventBus _bus;
    private readonly string _output;
    private readonly List<string> _rows = new() { "seconds,x,y,tracked,active,index_words,advanced_last_step,global_memory_loss" };
    private readonly List<string> _events = new() { "seconds,event,x,y" };
    private double _nextSample;
    private double _time;

    public ErasureRunProbe(string output, ErasureManager erasure, Player player, EventBus bus)
    {
        _output = output;
        _erasure = erasure;
        _player = player;
        _bus = bus;
        _bus.MemorialAwakened += Memorial;
        _bus.ChestOpened += Chest;
        _bus.CrisisStarted += CrisisStarted;
        _bus.CrisisEnded += CrisisEnded;
    }

    public void Sample(double time)
    {
        _time = time;
        if (time < _nextSample) return;
        _nextSample += 1;
        Vector2 position = _player.GlobalPosition;
        _rows.Add(string.Create(CultureInfo.InvariantCulture,
            $"{time:F3},{position.X:F3},{position.Y:F3},{_erasure.TrackedCellCount},{_erasure.ActiveCellCount},{_erasure.ActiveWordCount},{_erasure.LastAdvancedCellCount},{_erasure.GlobalErasurePercent:R}"));
    }

    private void Event(string name, Vector2 position) => _events.Add(string.Create(CultureInfo.InvariantCulture,
        $"{_time:F3},{name},{position.X:F3},{position.Y:F3}"));
    private void Memorial(Vector2 position) => Event("memorial", position);
    private void Chest(string id, string rarity, Vector2 position) => Event("chest", position);
    private void CrisisStarted(int number, int intensity) => Event("crisis_started", _player.GlobalPosition);
    private void CrisisEnded(int number) => Event("crisis_ended", _player.GlobalPosition);

    public void Dispose()
    {
        _bus.MemorialAwakened -= Memorial;
        _bus.ChestOpened -= Chest;
        _bus.CrisisStarted -= CrisisStarted;
        _bus.CrisisEnded -= CrisisEnded;
        File.WriteAllLines(Path.Combine(_output, "erasure-work.csv"), _rows);
        File.WriteAllLines(Path.Combine(_output, "erasure-events.csv"), _events);
    }
}
