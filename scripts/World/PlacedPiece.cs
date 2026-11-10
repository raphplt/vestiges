using Godot;

namespace Vestiges.World;

/// <summary>Une pièce maîtresse retenue sur une terre (plan 58) : son plan, son centre, son sens, et si c'est la pièce du cœur.</summary>
public readonly record struct PlacedPiece(LandPieceConfig.Piece Piece, Vector2 Anchor, bool Mirrored, bool IsHeart)
{
    public CompositionSite Site => new(Anchor, Mirrored);

    /// <summary>Place du Mémorial de la pièce, en coordonnées du monde ; nulle si elle n'en a pas.</summary>
    public Vector2? MemorialPosition => Piece.Memorial is Vector2 slot ? Anchor + SiteComposer.Mirror(slot, Mirrored) : null;

    /// <summary>Centre du pan de vide de la pièce, en coordonnées du monde ; nul si elle n'en a pas.</summary>
    public Vector2? VoidCenter => Piece.VoidCenter is Vector2 center ? Anchor + SiteComposer.Mirror(center, Mirrored) : null;
}
