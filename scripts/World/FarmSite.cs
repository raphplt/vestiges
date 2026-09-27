using Godot;

namespace Vestiges.World;

/// <summary>Une ferme retenue au chargement : centre de sa cour et sens (retournée d'est en ouest ou non).</summary>
public readonly record struct FarmSite(Vector2 Anchor, bool Mirrored);
