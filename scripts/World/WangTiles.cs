namespace Vestiges.World;

/// <summary>
/// Tuiles de Wang sur la grille isométrique « stacked » : chaque arête porte une couleur (0 ou 1) tirée par hachage de
/// l'arête, donc identique vue des deux cellules qu'elle sépare. Une matière existe en 16 tuiles, une par combinaison
/// d'arêtes (tools/generate_ground.py) : les voisines se raccordent et aucun motif ne se répète à l'échelle de la grille.
/// </summary>
public static class WangTiles
{
    public const int TileCount = 16;

    // Mêmes sels que tools/generate_ground.py.
    private const ulong SaltNorthEast = 0x51ED;
    private const ulong SaltNorthWest = 0xA7E3;

    /// <summary>Index de tuile = NE·1 + NW·2 + SE·4 + SW·8. L'arête SE est l'arête NW de la voisine SE, etc.</summary>
    public static int Index(int x, int y)
    {
        int odd = y & 1;
        uint ne = CellHash.Of(x, y, SaltNorthEast) & 1;
        uint nw = CellHash.Of(x, y, SaltNorthWest) & 1;
        uint se = CellHash.Of(x + odd, y + 1, SaltNorthWest) & 1;
        uint sw = CellHash.Of(x - 1 + odd, y + 1, SaltNorthEast) & 1;
        return (int)(ne | nw << 1 | se << 2 | sw << 3);
    }
}
