using Godot;

namespace Vestiges.World;

/// <summary>Un lieu composé retenu au chargement (ferme, chantier) : son centre et son sens (retourné d'est en ouest ou non).</summary>
public readonly record struct CompositionSite(Vector2 Anchor, bool Mirrored);
