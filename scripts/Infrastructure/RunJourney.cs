using System.Collections.Generic;
using Godot;
using Vestiges.Core;

namespace Vestiges.Infrastructure;

/// <summary>Moment marquant de la run, placé sur la frise du bilan.</summary>
public enum RunMarkerKind
{
    LevelUp,
    Crisis,
    Chest,
    Elite,
    Sovereign,
    Boss,
}

public readonly record struct RunMarker(float Time, RunMarkerKind Kind);

/// <summary>État de la run à intervalle régulier : la frise du bilan en trace les courbes.</summary>
public readonly record struct RunSample(float Time, int Level, int Kills, float DistanceMeters);

/// <summary>
/// Trajet de la run pour le bilan (plan 02 lot D, M2) : jusqu'où le joueur est allé (plus grande distance au sol
/// depuis son départ), le chemin parcouru, des échantillons réguliers et les moments marquants.
/// Relevé à pas fixe par <see cref="RunTracker"/> ; rien ici n'est sauvegardé en dehors du résumé de fin de run.
/// </summary>
public sealed class RunJourney
{
    private readonly RunSummaryConfig _config;
    private readonly List<RunSample> _samples = new();
    private readonly List<RunMarker> _markers = new();
    private bool _started;
    private Vector2 _start;
    private Vector2 _last;
    private float _nextSample;

    public RunJourney(RunSummaryConfig config) => _config = config;

    public float MaxDistanceMeters { get; private set; }
    public float TravelledMeters { get; private set; }
    public float CurrentDistanceMeters { get; private set; }
    public IReadOnlyList<RunSample> Samples => _samples;
    public IReadOnlyList<RunMarker> Markers => _markers;

    public void Track(Vector2 playerPosition)
    {
        if (!_started)
        {
            _started = true;
            _start = playerPosition;
            _last = playerPosition;
            return;
        }
        TravelledMeters += Iso.ToGround(playerPosition - _last).Length() / _config.PixelsPerMeter;
        _last = playerPosition;
        CurrentDistanceMeters = Iso.ToGround(playerPosition - _start).Length() / _config.PixelsPerMeter;
        MaxDistanceMeters = Mathf.Max(MaxDistanceMeters, CurrentDistanceMeters);
    }

    /// <summary>Ajoute un échantillon quand son heure est venue ; le premier à zéro.</summary>
    public void Sample(float runTime, int level, int kills)
    {
        if (runTime < _nextSample)
            return;
        _nextSample += _config.TimelineSampleSeconds;
        _samples.Add(new RunSample(runTime, level, kills, CurrentDistanceMeters));
    }

    /// <summary>Dernier échantillon, pris à la mort : la frise va jusqu'au bout.</summary>
    public void Close(float runTime, int level, int kills) =>
        _samples.Add(new RunSample(runTime, level, kills, CurrentDistanceMeters));

    public void Mark(float runTime, RunMarkerKind kind) => _markers.Add(new RunMarker(runTime, kind));
}
