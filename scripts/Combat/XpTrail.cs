using System.Collections.Generic;
using Godot;
using Vestiges.Core;

namespace Vestiges.Combat;

/// <summary>
/// Sillage (plan 05 §3.6) : couloir des dernières secondes de déplacement réel d'un joueur, où une orbe d'XP peut le
/// rejoindre comme s'il était encore là. Échantillonné au temps de jeu ; un saut plus long qu'un dash coupe le couloir
/// (téléportation). Les orbes le consultent par intervalles, jamais toutes à chaque frame.
/// </summary>
public sealed class XpTrail
{
    private struct Sample
    {
        public Vector2 Point;
        public float Time;
        public bool Connected;
    }

    private static readonly List<XpTrail> Active = new();

    private readonly Player _owner;
    private readonly float _duration;
    private readonly float _sampleSeconds;
    private readonly float _breakDistanceSq;
    private readonly float _radiusMultiplier;
    private readonly List<Sample> _samples = new();
    private Rect2 _bounds;
    private float _clock;
    private float _sampleTimer;
    private Vector2 _lastPoint;
    private bool _hasLastPoint;

    public XpTrail(Player owner, float duration, float sampleSeconds, float breakDistance, float radiusMultiplier)
    {
        _owner = owner;
        _duration = duration;
        _sampleSeconds = sampleSeconds;
        _breakDistanceSq = breakDistance * breakDistance;
        _radiusMultiplier = radiusMultiplier;
    }

    public static bool Any => Active.Count > 0;

    public int SampleCount => _samples.Count;

    public void Register()
    {
        if (!Active.Contains(this))
            Active.Add(this);
    }

    public void Unregister() => Active.Remove(this);

    /// <summary>Vrai si un couloir actif contient ce point.</summary>
    public static bool Covers(Vector2 position)
    {
        foreach (XpTrail trail in Active)
            if (trail.Contains(position))
                return true;
        return false;
    }

    /// <summary>Temps de jeu écoulé : nouvel échantillon si le joueur a bougé, oubli de ce qui a vieilli.</summary>
    public void Advance(float delta)
    {
        _clock += delta;
        _sampleTimer -= delta;
        bool changed = false;
        if (_sampleTimer <= 0f)
        {
            _sampleTimer = _sampleSeconds;
            changed = TrySample();
        }
        while (_samples.Count > 0 && _clock - _samples[0].Time > _duration)
        {
            _samples.RemoveAt(0);
            changed = true;
        }
        if (_samples.Count > 0 && _samples[0].Connected)
            _samples[0] = _samples[0] with { Connected = false };
        if (changed)
            RecomputeBounds();
    }

    private bool TrySample()
    {
        Vector2 position = _owner.GlobalPosition;
        if (!_hasLastPoint)
        {
            _lastPoint = position;
            _hasLastPoint = true;
            return false;
        }
        float movedSq = _lastPoint.DistanceSquaredTo(position);
        // Immobile : pas de nouveau point, le couloir vieillit quand même.
        if (movedSq < 4f)
            return false;
        bool connected = movedSq <= _breakDistanceSq;
        // Le départ d'un déplacement après une pause (points précédents expirés) rouvre le couloir depuis ce point.
        if (connected && (_samples.Count == 0 || _samples[^1].Point != _lastPoint))
            _samples.Add(new Sample { Point = _lastPoint, Time = _clock, Connected = false });
        _samples.Add(new Sample { Point = position, Time = _clock, Connected = connected && _samples.Count > 0 });
        _lastPoint = position;
        return true;
    }

    private void RecomputeBounds()
    {
        if (_samples.Count == 0)
            return;
        Rect2 bounds = new(_samples[0].Point, Vector2.Zero);
        foreach (Sample sample in _samples)
            bounds = bounds.Expand(sample.Point);
        _bounds = bounds;
    }

    private bool Contains(Vector2 position)
    {
        if (_samples.Count == 0 || !IsInstanceValid())
            return false;
        float radius = XpOrb.AttractionRadius(_owner) * _radiusMultiplier;
        if (!_bounds.Grow(radius).HasPoint(position))
            return false;
        float radiusSq = radius * radius;
        for (int i = 0; i < _samples.Count; i++)
        {
            Sample sample = _samples[i];
            float distanceSq = sample.Connected
                ? DistanceSquaredToSegment(position, _samples[i - 1].Point, sample.Point)
                : position.DistanceSquaredTo(sample.Point);
            if (distanceSq <= radiusSq)
                return true;
        }
        return false;
    }

    private bool IsInstanceValid() => GodotObject.IsInstanceValid(_owner) && !_owner.IsDead;

    private static float DistanceSquaredToSegment(Vector2 point, Vector2 from, Vector2 to)
    {
        Vector2 segment = to - from;
        float lengthSq = segment.LengthSquared();
        float t = lengthSq > 0f ? Mathf.Clamp((point - from).Dot(segment) / lengthSq, 0f, 1f) : 0f;
        return point.DistanceSquaredTo(from + segment * t);
    }
}
