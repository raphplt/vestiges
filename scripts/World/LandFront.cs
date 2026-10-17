using System.Collections.Generic;
using Godot;

namespace Vestiges.World;

/// <summary>
/// Front de chute d'une terre (plan 51 P2a) : chaque cellule d'Effacement reçoit à la construction sa date de chute,
/// tirée de sa distance au cœur et d'un bruit qui dessine langues et baies. Sa mémoire est le temps qui lui reste,
/// rapporté au préavis : les états visibles (fragile, effiloché, effacé) arrivent donc dans l'ordre et à l'heure. Le
/// cœur ne tombe jamais. État pur : l'Effacement fait avancer son horloge et lit les mémoires.
/// </summary>
public sealed class LandFront
{
    private readonly LandFrontConfig _config;
    private readonly FastNoiseLite _noise;
    private readonly Dictionary<Vector2I, float> _fallAt = new();

    public LandFront(LandFrontConfig config, ulong seed)
    {
        _config = config;
        _noise = new FastNoiseLite
        {
            Seed = (int)(seed & 0x7FFFFFFF),
            NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex,
            Frequency = config.NoiseFrequency,
        };
    }

    public LandFrontConfig Config => _config;

    /// <summary>Horloge du front, en secondes de terre : le temps de jeu, accéléré par les Oublis, la difficulté, les crises.</summary>
    public float Time { get; private set; }

    public int CellCount => _fallAt.Count;

    /// <summary>Part du cœur dans la terre : ce qui reste debout quand tout le reste est tombé.</summary>
    public float HeartShare => _fallAt.Count == 0 ? 1f : (float)_heartCells / _fallAt.Count;
    private int _heartCells;

    public IEnumerable<Vector2I> Cells => _fallAt.Keys;

    /// <summary>
    /// Inscrit une cellule de sol ; <paramref name="distance"/> est sa distance au cœur en part du rayon de la terre
    /// (0 au cœur, 1 au bord).
    /// </summary>
    public void AddCell(Vector2I cell, float distance)
    {
        if (distance <= _config.HeartRadius)
        {
            _fallAt[cell] = float.PositiveInfinity;
            _heartCells++;
            return;
        }
        // Au carré de la distance : la surface d'une couronne croît avec elle, la terre perd donc une part égale chaque
        // minute au lieu de fondre vite au début puis de moins en moins.
        float heart = _config.HeartRadius * _config.HeartRadius;
        float outward = Mathf.Clamp((distance * distance - heart) / (1f - heart), 0f, 1f);
        float fall = Mathf.Lerp(_config.HeartReachedSeconds, _config.FirstFallSeconds, outward)
            + _noise.GetNoise2D(cell.X, cell.Y) * _config.NoiseSeconds;
        // Le bruit ne fait tomber aucun sol avant le premier, ni après le cœur atteint.
        _fallAt[cell] = Mathf.Clamp(fall, _config.FirstFallSeconds, _config.HeartReachedSeconds);
    }

    public bool Contains(Vector2I cell) => _fallAt.ContainsKey(cell);

    public void Advance(float seconds)
    {
        if (seconds > 0f)
            Time += seconds;
    }

    /// <summary>Mémoire de 1 (loin de la chute) à 0 (tombée) ; une cellule hors de la terre ou au cœur vaut 1.</summary>
    public float MemoryAt(Vector2I cell)
    {
        if (!_fallAt.TryGetValue(cell, out float fall) || float.IsPositiveInfinity(fall))
            return 1f;
        return Mathf.Clamp((fall - Time) / _config.WarningSeconds, 0f, 1f);
    }

    /// <summary>Retarde la chute d'une cellule encore debout (présence du joueur).</summary>
    public void Delay(Vector2I cell, float seconds)
    {
        if (_fallAt.TryGetValue(cell, out float fall) && float.IsFinite(fall) && fall > Time)
            _fallAt[cell] = fall + seconds;
    }

    /// <summary>Impose une mémoire (stabilisation d'un lieu, essais) : la chute est replacée en conséquence.</summary>
    public void SetMemory(Vector2I cell, float memory)
    {
        if (_fallAt.TryGetValue(cell, out float fall) && float.IsFinite(fall))
            _fallAt[cell] = Time + Mathf.Clamp(memory, 0f, 1f) * _config.WarningSeconds;
    }

    /// <summary>Remonte la mémoire d'une cellule sans jamais avancer sa chute (lieu ravivé, coffre ouvert).</summary>
    public void Restore(Vector2I cell, float memory)
    {
        if (_fallAt.TryGetValue(cell, out float fall) && float.IsFinite(fall))
            _fallAt[cell] = Mathf.Max(fall, Time + Mathf.Clamp(memory, 0f, 1f) * _config.WarningSeconds);
    }

    /// <summary>Ancre une cellule encore debout (Mémorial ravivé, P2b) : elle ne tombera plus. Le sol déjà tombé le reste.</summary>
    public void Anchor(Vector2I cell)
    {
        if (_fallAt.TryGetValue(cell, out float fall) && fall > Time)
            _fallAt[cell] = float.PositiveInfinity;
    }

    /// <summary>Réveil du gardien (P3) : tout le sol qui n'est ni le cœur ni ancré tombe d'un coup ; rend les cellules tombées.</summary>
    public List<Vector2I> CollapseUnanchored()
    {
        List<Vector2I> falling = new();
        foreach (KeyValuePair<Vector2I, float> entry in _fallAt)
        {
            if (float.IsFinite(entry.Value) && entry.Value > Time)
                falling.Add(entry.Key);
        }
        foreach (Vector2I cell in falling)
            _fallAt[cell] = Time;
        return falling;
    }

    /// <summary>Part du sol de la terre encore debout, de 1 à l'arrivée à la part du cœur quand tout le reste est tombé.</summary>
    public float StandingShare()
    {
        if (_fallAt.Count == 0)
            return 1f;
        int standing = 0;
        foreach (float fall in _fallAt.Values)
        {
            if (fall > Time)
                standing++;
        }
        return (float)standing / _fallAt.Count;
    }
}
