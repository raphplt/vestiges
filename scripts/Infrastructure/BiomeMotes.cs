using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Particules propres à un biome (plan 39 B1), bloc « motes » de <c>data/biomes/*.json</c> : feuilles en forêt, papiers
/// en ville, pollen aux champs, poussière à la carrière. Autour de la caméra, dans le sens du vent commun.
/// </summary>
public sealed record BiomeMotes(AtmosphereParticleKind Kind, int Amount, Color Color)
{
    public static readonly BiomeMotes None = new(AtmosphereParticleKind.None, 0, Colors.White);
}
