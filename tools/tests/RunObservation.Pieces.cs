using System.Globalization;
using System.Threading.Tasks;
using Godot;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// --capture-pieces (plan 58) : chaque pièce maîtresse de la terre cadrée en entier, son Mémorial s'il en a un, les deux
/// passerelles de chenal les plus proches du départ s'il y en a (T2a), puis une vue de haut de toute la terre. Sans
/// créatures, pour juger la composition.
/// </summary>
public partial class RunObservation
{
    // Marge autour de l'emprise d'une pièce dans son cadre, en pixels du monde.
    private const float PieceFrameMarginPx = 140f;
    private const float LandOverviewZoom = 0.25f;

    private async Task CapturePieces()
    {
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
            if (node is Vestiges.Combat.Enemy existing && existing.IsActive)
                _world.GetNode<Vestiges.Spawn.EnemyPool>("EnemyPool").Return(existing);
        _player.AIInputOverride = Vector2.Zero;
        // Le carton d'arrivée passe d'abord : il couvrirait la première pièce.
        await Seconds(7.0);

        Vector2 initialZoom = _camera.Zoom;
        Vector2 visible = GetViewport().GetVisibleRect().Size;
        int index = 0;
        foreach (PlacedPiece piece in _world.Pieces)
        {
            Vector2 half = piece.Piece.Plan.HalfExtentPx + Vector2.One * PieceFrameMarginPx;
            float zoom = Mathf.Min(visible.X / (half.X * 2f), visible.Y / (half.Y * 2f));
            _camera.Zoom = new Vector2(zoom, zoom);
            // Le joueur au sud de la pièce, hors de son cœur : la caméra le suit, il reste dans le cadre.
            _player.GlobalPosition = piece.Anchor + new Vector2(0f, piece.Piece.Plan.HalfExtentPx.Y * 0.6f);
            _camera.ResetSmoothing();
            await Frames(40);
            string memorial = piece.MemorialPosition is Vector2 slot ? $" Mémorial ({slot.X:F0}, {slot.Y:F0})" : "";
            GD.Print(string.Create(CultureInfo.InvariantCulture,
                $"[RunObservation] pièce {index} {piece.Piece.Id}{(piece.IsHeart ? " (cœur)" : "")} en ({piece.Anchor.X:F0}, {piece.Anchor.Y:F0}){memorial}"));
            SaveFrame($"piece-{index:00}-{piece.Piece.Id}");
            index++;
        }

        _camera.Zoom = initialZoom;
        int bridge = 0;
        foreach (Vector2I cell in NearestBridges(2))
        {
            _player.GlobalPosition = PathNetworkGenerator.CellCenter(cell) + new Vector2(0f, 90f);
            _camera.ResetSmoothing();
            await Frames(40);
            SaveFrame($"bridge-{bridge++:00}");
        }

        _camera.Zoom = new Vector2(LandOverviewZoom, LandOverviewZoom);
        _player.GlobalPosition = Vector2.Zero;
        _camera.ResetSmoothing();
        await Frames(90);
        SaveFrame("pieces-land");
        _camera.Zoom = initialZoom;
        GD.Print($"[RunObservation] RESULT pieces={_world.Pieces.Count}");
    }

    /// <summary>Cellules de chemin entre deux cellules d'eau (en x ou en y à l'écran), les plus proches du départ, écartées.</summary>
    private System.Collections.Generic.List<Vector2I> NearestBridges(int count)
    {
        WorldGenerator generator = _world.Generator;
        System.Collections.Generic.List<Vector2I> found = new();
        if (_world.PathCells == null)
            return found;
        System.Collections.Generic.List<Vector2I> candidates = new();
        foreach (Vector2I cell in _world.PathCells)
        {
            bool water(int dx, int dy) => generator.GetTerrain(cell.X + dx, cell.Y + dy) == TerrainType.Water;
            if (generator.GetTerrain(cell.X, cell.Y) != TerrainType.Water && ((water(-1, 0) && water(1, 0)) || (water(0, -2) && water(0, 2))))
                candidates.Add(cell);
        }
        candidates.Sort((a, b) => PathNetworkGenerator.CellCenter(a).LengthSquared().CompareTo(PathNetworkGenerator.CellCenter(b).LengthSquared()));
        foreach (Vector2I cell in candidates)
        {
            if (found.Count >= count)
                break;
            if (found.TrueForAll(other => PathNetworkGenerator.CellCenter(other).DistanceTo(PathNetworkGenerator.CellCenter(cell)) > 1200f))
                found.Add(cell);
        }
        return found;
    }
}
