using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Combat;

/// <summary>
/// Pistage d'une créature (plan 07 §7) : une créature qui a déjà approché le joueur puis a été distancée assez
/// longtemps perd sa trace et erre, au lieu de le suivre en file jusqu'à son retrait. Les créatures qui arrivent
/// du flux ne sont pas concernées tant qu'elles ne l'ont pas atteint. Réglages : data/scaling/enemy_tracking.json,
/// et par créature <c>perception</c> et <c>leash</c> dans les stats de sa fiche.
/// </summary>
public sealed class EnemyTracking
{
    private const string ConfigPath = "res://data/scaling/enemy_tracking.json";
    private static bool _configLoaded;
    private static string _loadError;
    private static float _engageDistanceSq = 450f * 450f;
    private static float _loseTrackDistanceSq = 800f * 800f;
    private static float _loseTrackSeconds = 4f;
    private static float _wanderSpeedFactor = 0.35f;
    private static float _wanderTurnSeconds = 2.5f;

    private float _engageSq = _engageDistanceSq;
    private float _loseSq = _loseTrackDistanceSq;
    private bool _engaged;
    private float _outOfRangeTime;
    private float _wanderTimer;
    private Vector2 _wanderDirection;

    public bool IsLost { get; private set; }
    public float WanderSpeedFactor => _wanderSpeedFactor;
    public Vector2 WanderDirection => _wanderDirection;

    // Oubli des traces (plan 17 lot 3D) : les créatures repèrent le joueur de plus loin.
    private static float _detectionScaleSq = 1f;

    /// <summary>Distance de repérage multipliée par <paramref name="scale"/> pour toutes les créatures.</summary>
    public static void SetDetectionScale(float scale) => _detectionScaleSq = scale * scale;

    public EnemyTracking()
    {
        LoadConfig();
    }

    /// <summary>
    /// Perception (distance d'engagement) et laisse (distance de perte de trace) propres à la créature,
    /// champs <c>perception</c> et <c>leash</c> de ses stats ; zéro ou absent : réglage commun.
    /// </summary>
    public void Configure(float perception, float leash)
    {
        _engageSq = perception > 0f ? perception * perception : _engageDistanceSq;
        _loseSq = leash > 0f ? leash * leash : _loseTrackDistanceSq;
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
        if (distanceToPlayerSq < _engageSq * _detectionScaleSq)
        {
            _engaged = true;
            _outOfRangeTime = 0f;
            IsLost = false;
            return false;
        }

        if (!_engaged)
            return false;

        if (distanceToPlayerSq > _loseSq)
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
                float angle = RunRandom.Behavior.Randf() * Mathf.Tau;
                _wanderDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * 0.5f).Normalized();
            }
        }
        return IsLost;
    }

    /// <summary>
    /// Réglages lus strictement une fois (plan 26 Q7g-3) ; refusés, les valeurs d'avant restent et la raison est rendue
    /// (le chargement de la run s'arrête, GameBootstrap).
    /// </summary>
    public static bool TryLoad(out string error)
    {
        LoadConfig();
        error = _loadError;
        return error == null;
    }

    private static void LoadConfig()
    {
        if (_configLoaded)
            return;
        _configLoaded = true;
        _loadError = $"{ConfigPath} : lecture interrompue";
        using FileAccess file = FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Read);
        if (file == null)
            _loadError = $"{ConfigPath} : fichier illisible";
        else if (TryParse(file.GetAsText(), out Settings settings, out _loadError))
        {
            _engageDistanceSq = settings.EngageDistance * settings.EngageDistance;
            _loseTrackDistanceSq = settings.LoseTrackDistance * settings.LoseTrackDistance;
            _loseTrackSeconds = settings.LoseTrackSeconds;
            _wanderSpeedFactor = settings.WanderSpeedFactor;
            _wanderTurnSeconds = settings.WanderTurnSeconds;
        }
        else
            _loadError = $"{ConfigPath} : {_loadError}";
        if (_loadError != null)
            GD.PushError($"[EnemyTracking] {_loadError}");
    }

    public readonly record struct Settings(float EngageDistance, float LoseTrackDistance, float LoseTrackSeconds,
        float WanderSpeedFactor, float WanderTurnSeconds);

    /// <summary>Un champ absent garde le réglage d'avant ; la trace se perd plus loin qu'elle ne s'engage.</summary>
    public static bool TryParse(string text, out Settings settings, out string error)
    {
        settings = default;
        try
        {
            using JsonDocument document = JsonDocument.Parse(text);
            JsonConfigReader reader = new(document.RootElement);
            JsonElement root = reader.Root;
            reader.AllowOnly(root, "pistage", "engage_distance", "lose_track_distance", "lose_track_seconds", "wander_speed_factor",
                "wander_turn_seconds");
            Settings parsed = new(
                JsonConfigReader.Has(root, "engage_distance") ? reader.Positive(root, "engage_distance") : 450f,
                JsonConfigReader.Has(root, "lose_track_distance") ? reader.Positive(root, "lose_track_distance") : 800f,
                JsonConfigReader.Has(root, "lose_track_seconds") ? reader.NonNegative(root, "lose_track_seconds") : 4f,
                JsonConfigReader.Has(root, "wander_speed_factor") ? reader.NonNegative(root, "wander_speed_factor") : 0.35f,
                JsonConfigReader.Has(root, "wander_turn_seconds") ? reader.Positive(root, "wander_turn_seconds") : 2.5f);
            if (reader.Error == null && parsed.EngageDistance > parsed.LoseTrackDistance)
                reader.Fail(FormattableString.Invariant(
                    $"engage_distance ≤ lose_track_distance attendu ({parsed.EngageDistance} > {parsed.LoseTrackDistance})"));
            error = reader.Error;
            if (error != null)
                return false;
            settings = parsed;
            return true;
        }
        catch (JsonException exception)
        {
            error = $"JSON invalide : {exception.Message}";
            return false;
        }
    }
}
