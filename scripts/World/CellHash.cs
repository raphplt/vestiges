namespace Vestiges.World;

/// <summary>
/// Hash déterministe d'une cellule de la grille (et d'un sel) pour les tirages de génération : variantes de tuiles,
/// densité et choix des décors, parcelles. Les bits sont entièrement brassés (finaliseur SplitMix64) : le produit XOR
/// utilisé auparavant gardait des bits de poids faible périodiques en x et y, et `hash % n` dessinait des diagonales
/// et des trames régulières sur la carte.
/// </summary>
public static class CellHash
{
    public static uint Of(int x, int y, ulong salt = 0)
    {
        ulong h = unchecked((ulong)(uint)x * 0x9E3779B97F4A7C15UL ^ (ulong)(uint)y * 0xC2B2AE3D27D4EB4FUL ^ salt * 0x165667B19E3779F9UL);
        h ^= h >> 30;
        h = unchecked(h * 0xBF58476D1CE4E5B9UL);
        h ^= h >> 27;
        h = unchecked(h * 0x94D049BB133111EBUL);
        h ^= h >> 31;
        return (uint)(h & 0x7FFFFFFF);
    }

    /// <summary>Tirage uniforme dans [0, 1).</summary>
    public static float Unit(int x, int y, ulong salt = 0) => Of(x, y, salt) / 2147483648f;
}
