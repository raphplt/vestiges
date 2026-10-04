using System.Collections.Generic;

namespace Vestiges.Infrastructure;

/// <summary>
/// Groupes d'apparition (biomes, secours de spawn_flow.json) : relation de contenu vers le catalogue des créatures,
/// contrôlée une fois au chargement (plan 26 Q6c) plutôt qu'à chaque tirage.
/// </summary>
public static class EnemyPools
{
	/// <summary>Retire du groupe les créatures absentes du catalogue ; renvoie un message par créature retirée.</summary>
	public static List<string> KeepKnown(List<string> pool, string where)
	{
		List<string> errors = new();
		for (int i = pool.Count - 1; i >= 0; i--)
		{
			if (EnemyDataLoader.Exists(pool[i]))
				continue;
			errors.Insert(0, $"{where} : créature « {pool[i]} » introuvable, retirée du groupe");
			pool.RemoveAt(i);
		}
		return errors;
	}
}
