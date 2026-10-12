using Godot;

namespace Vestiges.Core;

/// <summary>Déplacement réel après collisions ; départ/fin encadrent aussi un saut instantané dans la même image.</summary>
public readonly record struct PlayerDashEvent(ulong PlayerId, Vector2 From, Vector2 To, bool Blink);
