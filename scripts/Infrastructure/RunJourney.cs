using Godot;
using Vestiges.Core;

namespace Vestiges.Infrastructure;

/// <summary>
/// Trajet de la run pour le bilan (plan 02 lot D, M2) : jusqu'où le joueur est allé (plus grande distance au sol
/// depuis son départ) et le chemin parcouru. La frise du bilan et ses échantillons ont été retirés (DECISIONS §150).
/// Relevé à pas fixe par <see cref="RunTracker"/> ; rien ici n'est sauvegardé en dehors du résumé de fin de run.
/// </summary>
public sealed class RunJourney
{
    private readonly RunSummaryConfig _config;
    private bool _started;
    private Vector2 _start;
    private Vector2 _last;

    public RunJourney(RunSummaryConfig config) => _config = config;

    public float MaxDistanceMeters { get; private set; }
    public float TravelledMeters { get; private set; }

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
        float fromStart = Iso.ToGround(playerPosition - _start).Length() / _config.PixelsPerMeter;
        MaxDistanceMeters = Mathf.Max(MaxDistanceMeters, fromStart);
    }
}
