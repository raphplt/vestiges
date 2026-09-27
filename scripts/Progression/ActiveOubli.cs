using Vestiges.Infrastructure;

namespace Vestiges.Progression;

/// <summary>Oubli porté par le joueur : son effet de données et le modificateur appliqué, à retirer pour le lever.</summary>
public sealed record ActiveOubli(StatEffectData Data, StatModifier Modifier);
