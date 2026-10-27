using System.Globalization;
using System.Threading.Tasks;
using Godot;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// --capture-pieces (plan 58) : chaque pièce maîtresse de la terre cadrée en entier, son Mémorial s'il en a un, puis une
/// vue de haut de toute la terre. Sans créatures, pour juger la composition.
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

        _camera.Zoom = new Vector2(LandOverviewZoom, LandOverviewZoom);
        _player.GlobalPosition = Vector2.Zero;
        _camera.ResetSmoothing();
        await Frames(90);
        SaveFrame("pieces-land");
        _camera.Zoom = initialZoom;
        GD.Print($"[RunObservation] RESULT pieces={_world.Pieces.Count}");
    }
}
