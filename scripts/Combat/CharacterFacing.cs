using Godot;

namespace Vestiges.Combat;

/// <summary>
/// Orientation visuelle d'un personnage déduite de son déplacement réel.
/// Huit directions si le jeu de sprites les fournit, sinon les quatre diagonales historiques.
/// Des bandes d'hystérésis évitent l'oscillation de pose au stick près d'une frontière.
/// </summary>
public sealed class CharacterFacing
{
    public enum Direction { E, SE, S, SW, W, NW, N, NE }

    public static readonly string[] DirectionNames = { "E", "SE", "S", "SW", "W", "NW", "N", "NE" };

    private const float SectorAngle = Mathf.Pi / 4f;
    // Marge au-delà de la demi-largeur d'un secteur avant de changer de direction (≈ 7°).
    private const float SectorHysteresis = 0.12f;
    // Bande de stabilité autour des axes pour les quatre diagonales.
    private const float AxisHysteresis = 0.1f;

    private bool _eightWay;

    public Direction Current { get; private set; } = Direction.SE;

    public void Reset(bool eightWay)
    {
        _eightWay = eightWay;
        Current = Direction.SE;
    }

    /// <summary>Met à jour l'orientation à partir d'une direction de déplacement non nulle, repère écran.</summary>
    public Direction Update(Vector2 direction)
    {
        Current = _eightWay ? UpdateEightWay(direction) : UpdateDiagonals(direction);
        return Current;
    }

    private Direction UpdateEightWay(Vector2 direction)
    {
        float angle = direction.Angle();
        float currentCenter = (int)Current * SectorAngle;
        if (Mathf.Abs(Mathf.AngleDifference(currentCenter, angle)) <= SectorAngle * 0.5f + SectorHysteresis)
            return Current;

        int sector = Mathf.PosMod(Mathf.RoundToInt(angle / SectorAngle), 8);
        return (Direction)sector;
    }

    private Direction UpdateDiagonals(Vector2 direction)
    {
        // Un axe pur conserve le côté orthogonal de la dernière pose : pas d'inversion à l'arrêt ni au stick.
        bool facesWest = Current is Direction.SW or Direction.NW;
        bool facesNorth = Current is Direction.NE or Direction.NW;
        if (direction.X > AxisHysteresis) facesWest = false;
        else if (direction.X < -AxisHysteresis) facesWest = true;
        if (direction.Y > AxisHysteresis) facesNorth = false;
        else if (direction.Y < -AxisHysteresis) facesNorth = true;

        return (facesWest, facesNorth) switch
        {
            (false, false) => Direction.SE,
            (true, false) => Direction.SW,
            (false, true) => Direction.NE,
            (true, true) => Direction.NW
        };
    }
}
