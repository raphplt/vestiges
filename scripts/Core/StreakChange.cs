using Godot;

namespace Vestiges.Core;

/// <summary>
/// Un changement de la Série, publié par le joueur sur l'EventBus pour le HUD, les retours et l'Essence. Le rang gagné
/// se lit à <see cref="Rank"/> &gt; <see cref="PreviousRank"/> ; <see cref="Essence"/> n'est non nul qu'à l'encaissement.
/// </summary>
public readonly record struct StreakChange(
    StreakChangeKind Kind, int Count, int Rank, int PreviousCount, int PreviousRank, int Essence, Vector2 Position);
