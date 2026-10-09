using System.Diagnostics;
using Godot;

namespace Vestiges.Tests;

/// <summary>
/// Découpe le temps mural d'une image du banc en quatre postes, par deux marqueurs en tête et en queue des files de
/// traitement : scripts physiques, pas du moteur physique (synchronisation et requêtes comprises), scripts
/// <c>_Process</c>, puis rendu et le reste de la boucle. Un tick physique peut se répéter dans une image (plan 29).
/// </summary>
public sealed partial class FrameSplit
{
    private long _physicsScripts, _physicsStep, _process, _render, _frames, _ticks;
    private long _mark;
    private bool _afterPhysics;

    public static FrameSplit Attach(Node root)
    {
        FrameSplit split = new();
        root.AddChild(new Marker(split, true));
        root.AddChild(new Marker(split, false));
        return split;
    }

    public void Reset()
    {
        _physicsScripts = _physicsStep = _process = _render = _frames = _ticks = 0;
    }

    public object Summary()
    {
        double perFrame = Stopwatch.Frequency / 1000.0 * System.Math.Max(1, _frames);
        return new
        {
            physics_scripts = _physicsScripts / perFrame,
            physics_step = _physicsStep / perFrame,
            process_scripts = _process / perFrame,
            render_and_rest = _render / perFrame,
            physics_ticks_per_frame = (double)_ticks / System.Math.Max(1, _frames),
        };
    }

    private void Begin(long now, bool physics)
    {
        // Avant le premier marqueur : soit le pas physique du tick précédent, soit le rendu de l'image précédente.
        if (_mark != 0)
        {
            if (_afterPhysics)
                _physicsStep += now - _mark;
            else
                _render += now - _mark;
        }
        _afterPhysics = false;
        _mark = now;
        if (physics)
            _ticks++;
        else
            _frames++;
    }

    private void End(long now, bool physics)
    {
        if (physics)
            _physicsScripts += now - _mark;
        else
            _process += now - _mark;
        _afterPhysics = physics;
        _mark = now;
    }

    private sealed partial class Marker : Node
    {
        private readonly FrameSplit _split;
        private readonly bool _first;

        public Marker() { }

        public Marker(FrameSplit split, bool first)
        {
            _split = split;
            _first = first;
            ProcessMode = ProcessModeEnum.Always;
            ProcessPriority = ProcessPhysicsPriority = first ? int.MinValue : int.MaxValue;
        }

        public override void _PhysicsProcess(double delta)
        {
            long now = Stopwatch.GetTimestamp();
            if (_first) _split.Begin(now, true); else _split.End(now, true);
        }

        public override void _Process(double delta)
        {
            long now = Stopwatch.GetTimestamp();
            if (_first) _split.Begin(now, false); else _split.End(now, false);
        }
    }
}
