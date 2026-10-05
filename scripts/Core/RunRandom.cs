using Godot;

namespace Vestiges.Core;

/// <summary>
/// Tirages de gameplay d'une run (plan 26 Q8c-2) : chaque système tire dans son propre flux, dérivé de la seed
/// effective. Deux lancements à même seed, mêmes entrées et même pas de temps font les mêmes tirages ; un système qui
/// tire plus ou moins ne décale pas les autres. Les tirages cosmétiques restent sur le générateur global de Godot.
/// Initialisé par <c>WorldSetup</c> avant le premier nœud de la run, pour le lancement normal comme pour les bancs.
/// </summary>
public static class RunRandom
{
    private static bool _begun;
    private static ulong _seed;
    private static RandomNumberGenerator _spawn;
    private static RandomNumberGenerator _loot;
    private static RandomNumberGenerator _combat;
    private static RandomNumberGenerator _behavior;

    /// <summary>Apparitions : créature, groupe, élite, affixe, position.</summary>
    public static RandomNumberGenerator Spawn => _spawn ??= Fallback();

    /// <summary>Butin et bonus lâchés.</summary>
    public static RandomNumberGenerator Loot => _loot ??= Fallback();

    /// <summary>Coups du joueur : critiques, nombres fractionnaires de projectiles, de sauts, de perforations.</summary>
    public static RandomNumberGenerator Combat => _combat ??= Fallback();

    /// <summary>Comportement des créatures : errance des désorientés, décalage des minuteurs de meute.</summary>
    public static RandomNumberGenerator Behavior => _behavior ??= Fallback();

    public static void Begin(ulong runSeed)
    {
        _begun = true;
        _seed = runSeed;
        _spawn = Create("spawn");
        _loot = Create("loot");
        _combat = Create("combat");
        _behavior = Create("behavior");
    }

    /// <summary>Fin de la run : hors run, les flux reviennent au générateur global.</summary>
    public static void End()
    {
        _begun = false;
        _spawn = _loot = _combat = _behavior = null;
    }

    /// <summary>
    /// Seed d'un flux nommé, pour un système qui garde son propre générateur. Hors run (bancs sur un nœud isolé), elle
    /// vient du générateur global : un banc qui fixe <c>GD.Seed</c> reste reproductible.
    /// </summary>
    public static ulong SeedFor(string stream) => _begun ? Mix(_seed ^ Fnv1a(stream)) : GD.Randi() | ((ulong)GD.Randi() << 32);

    public static RandomNumberGenerator Create(string stream) => new() { Seed = SeedFor(stream) };

    private static RandomNumberGenerator Fallback() => new() { Seed = GD.Randi() | ((ulong)GD.Randi() << 32) };

    /// <summary>Empreinte stable d'un nom (string.GetHashCode change à chaque lancement).</summary>
    private static ulong Fnv1a(string text)
    {
        ulong hash = 14695981039346656037UL;
        foreach (char c in text)
        {
            hash ^= c;
            hash *= 1099511628211UL;
        }
        return hash;
    }

    /// <summary>SplitMix64 : des seeds voisines donnent des flux sans rapport.</summary>
    private static ulong Mix(ulong value)
    {
        value += 0x9E3779B97F4A7C15UL;
        value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
        value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
        return value ^ (value >> 31);
    }
}
