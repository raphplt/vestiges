using System;
using System.Collections.Generic;

namespace Vestiges.Infrastructure;

/// <summary>
/// Ambiance sonore d'un biome (plan 39 A4), bloc « ambience » de <c>data/biomes/*.json</c> : fonds en boucle (un seul
/// joue, tiré à chaque entrée dans le biome), sons ponctuels tirés au hasard pendant l'exploration, réverbération du bus
/// des sons du monde. Jouée par AudioManager selon le biome où se trouve le joueur.
/// </summary>
public sealed record BiomeAmbience(
    IReadOnlyList<string> LoopKeys,
    IReadOnlyList<string> AccentKeys,
    float AccentFirstMinS, float AccentFirstMaxS,
    float AccentIntervalMinS, float AccentIntervalMaxS,
    float AccentVolumeDb,
    float ReverbWet, float ReverbRoomSize, float ReverbDamping)
{
    /// <summary>Biome sans bloc : ni fond, ni sons ponctuels, ni réverbération.</summary>
    public static readonly BiomeAmbience Silent = new(Array.Empty<string>(), Array.Empty<string>(), 0f, 0f, 0f, 0f, 0f, 0f, 0.5f, 0.5f);

    public bool HasAccents => AccentKeys.Count > 0;
}
