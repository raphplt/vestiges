using System.Collections.Generic;
using Godot;

namespace Vestiges.Combat;

/// <summary>
/// Pistage d'une créature (plan 07 §7) : une créature qui a déjà approché le joueur puis a été distancée assez
/// longtemps perd sa trace et erre, au lieu de le suivre en file jusqu'à son retrait. Les créatures qui arrivent
/// du flux ne sont pas concernées tant qu'elles ne l'ont pas atteint. Réglages : data/scaling/enemy_tracking.json.
/// </summary>
public sealed class EnemyTracking
{
    private static bool _configLoaded;
    private static float _engageDistanceSq = 450f * 450f;
    private static float _loseTrackDistanceSq = 800f * 800f;
    private static float _loseTrackSeconds = 4f;
    private static float _wanderSpeedFactor = 0.35f;
    private static float _wanderTurnSeconds = 2.5f;

    private bool _engaged;
    private float _outOfRangeTime;
    private float _wanderTimer;
    private Vector2 _wanderDirection;

    public bool IsLost { get; private set; }
    public float WanderSpeedFactor => _wanderSpeedFactor;
    public Vector2 WanderDirection => _wanderDirection;

    public EnemyTracking()
    {
        LoadConfig();
    }

    public void Reset()
    {
        _engaged = false;
        _outOfRangeTime = 0f;
        _wanderTimer = 0f;
        IsLost = false;
    }

    /// <summary>Met à jour le pistage ; renvoie vrai si la créature a perdu la trace du joueur.</summary>
    public bool Tick(float distanceToPlayerSq, float delta)
    {
        if (distanceToPlayerSq < _engageDistanceSq)
        {
            _engaged = true;
            _outOfRangeTime = 0f;
            IsLost = false;
            return false;
        }

        if (!_engaged)
            return false;

        if (distanceToPlayerSq > _loseTrackDistanceSq)
            _outOfRangeTime += delta;
        else if (!IsLost)
            _outOfRangeTime = 0f;

        if (!IsLost && _outOfRangeTime >= _loseTrackSeconds)
        {
            IsLost = true;
            _wanderTimer = 0f;
        }

        if (IsLost)
        {
            _wanderTimer -= delta;
            if (_wanderTimer <= 0f)
            {
                _wanderTimer = _wanderTurnSeconds;
                float angle = GD.Randf() * Mathf.Tau;
                _wanderDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * 0.5f).Normalized();
            }
        }
        return IsLost;
    }

    private static void LoadConfig()
    {
        if (_configLoaded)
            return;
        _configLoaded = true;

        using FileAccess file = FileAccess.Open("res://data/scaling/enemy_tracking.json", FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PushWarning("[EnemyTracking] enemy_tracking.json introuvable : réglages par défaut");
            return;
        }
        Json json = new();
        if (json.Parse(file.GetAsText()) != Error.Ok)
        {
            GD.PushError($"[EnemyTracking] enemy_tracking.json invalide : {json.GetErrorMessage()}");
            return;
        }
        Godot.Collections.Dictionary dict = json.Data.AsGodotDictionary();
        float engage = (float)dict.GetValueOrDefault("engage_distance", 450f).AsDouble();
        float lose = (float)dict.GetValueOrDefault("lose_track_distance", 800f).AsDouble();
        _engageDistanceSq = engage * engage;
        _loseTrackDistanceSq = lose * lose;
        _loseTrackSeconds = (float)dict.GetValueOrDefault("lose_track_seconds", 4f).AsDouble();
        _wanderSpeedFactor = (float)dict.GetValueOrDefault("wander_speed_factor", 0.35f).AsDouble();
        _wanderTurnSeconds = (float)dict.GetValueOrDefault("wander_turn_seconds", 2.5f).AsDouble();
    }
}
