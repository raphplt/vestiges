namespace Vestiges.Infrastructure;

/// <summary>Un niveau de difficulté (data/scaling/difficulty.json) : multiplicateurs par rapport à Normal.</summary>
public sealed class DifficultyLevel
{
    public string Id { get; init; }
    public string NameKey { get; init; }
    public string DescKey { get; init; }
    /// <summary>Ouvert d'office ; sinon, il s'ouvre en battant l'Indicible au niveau d'avant.</summary>
    public bool Open { get; init; }
    public float EnemyCount { get; init; } = 1f;
    public float EnemyHp { get; init; } = 1f;
    public float EnemyDamage { get; init; } = 1f;
    /// <summary>Divise l'intervalle entre deux élites naturelles.</summary>
    public float EliteFrequency { get; init; } = 1f;
    public float ErasureSpeed { get; init; } = 1f;
    public int StartPeril { get; init; }
    public float Vestiges { get; init; } = 1f;
}
