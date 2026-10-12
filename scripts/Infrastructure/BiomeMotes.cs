using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Un émetteur de particules propre à un biome (plan 39 B1), entrée de la liste « motes » de <c>data/biomes/*.json</c> :
/// feuilles en forêt, poussière qui scintille et papiers en ville, pollen aux champs, poussière à la carrière. Autour de la
/// caméra, dans le sens du vent commun.
/// </summary>
public sealed record BiomeMotes(AtmosphereParticleKind Kind, int Amount, Color Color);
