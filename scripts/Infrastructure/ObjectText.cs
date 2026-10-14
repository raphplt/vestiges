using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Textes d'un objet ou d'un Écusson : <c>{jeton}</c> affiche une valeur, <c>{jeton%}</c> la même en pourcentage.
/// Un jeton est un réglage de <c>params</c>, <c>n</c> (copies), <c>amount</c> (pas × copies) ou une valeur dérivée de la
/// stat (<see cref="DerivedTokens"/>) : le texte du JSON reste la seule source et suit les copies portées.
/// </summary>
public static class ObjectText
{
	public static readonly Regex Token = new(@"\{(\w+)(%?)\}");
	private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

	/// <summary>Jetons calculés selon les copies, par stat ; les autres jetons viennent des réglages.</summary>
	public static readonly IReadOnlyDictionary<string, string[]> DerivedTokens = new Dictionary<string, string[]>
	{
		["chill_chance"] = new[] { "slow" },
		["elite_slow"] = new[] { "slow" },
		["dash_trail"] = new[] { "slow", "trail_seconds" },
		["dash_blink"] = new[] { "distance" },
		["chest_pointers"] = new[] { "pointers" },
		["item_lifetime"] = new[] { "extra_seconds" },
		["free_chest"] = new[] { "chance" },
		["chest_xp_pull"] = new[] { "radius" },
		["chest_choices"] = new[] { "choices" },
		["damage_link"] = new[] { "ratio" },
		["dash_decoy"] = new[] { "decoy_seconds" },
	};

	/// <summary>Durées de statut allongées par la stat du joueur quand la fiche la connaît.</summary>
	private static readonly string[] StatusSeconds = { "burn_seconds", "slow_seconds", "seconds", "fragile_seconds" };

	public static bool IsKnownToken(string stat, string token, ICollection<string> parameters) =>
		token is "n" or "amount" || parameters.Contains(token)
		|| (DerivedTokens.TryGetValue(stat, out string[] derived) && System.Array.IndexOf(derived, token) >= 0);

	/// <summary>Valeurs des jetons pour <paramref name="copies"/> copies ; la durée des statuts du joueur vaut 1 hors run.</summary>
	public static Dictionary<string, float> Values(PassiveSouvenirData data, int copies, float statusDuration = 1f)
	{
		Dictionary<string, float> values = new(data.Parameters);
		float P(string key) => data.Parameters.GetValueOrDefault(key);
		float amount = data.Effects.Count > 0 ? data.Effects[0].Step * copies : 0f;
		values["n"] = copies;
		values["amount"] = amount;
		foreach (string key in StatusSeconds)
			if (data.Parameters.ContainsKey(key))
				values[key] = P(key) * statusDuration;
		switch (data.Stat)
		{
			case "chill_chance":
				values["slow"] = 1f - P("slow_factor");
				break;
			case "elite_slow":
				values["slow"] = 1f - P("slow_factor");
				values["seconds"] = Mathf.Min(P("max_seconds"), P("seconds") * statusDuration);
				break;
			case "dash_trail":
				values["slow"] = 1f - P("slow_factor");
				values["trail_seconds"] = Mathf.Min(P("max_seconds"), P("base_seconds") * copies * statusDuration);
				break;
			case "dash_blink":
				values["distance"] = P("base_distance") + P("distance_per_copy") * (copies - 1);
				break;
			case "chest_pointers":
				values["pointers"] = Mathf.Min(P("pointers_per_copy") * copies, P("max_pointers"));
				break;
			case "item_lifetime":
				values["extra_seconds"] = P("extra_seconds_per_copy") * copies;
				break;
			case "free_chest":
				values["chance"] = Mathf.Min(P("chance_per_copy") * copies, P("max_chance"));
				break;
			case "chest_xp_pull":
				values["radius"] = P("radius_per_copy") * copies;
				break;
			case "chest_choices":
				values["choices"] = 1f + P("extra_choices_per_copy") * copies;
				break;
			case "damage_link":
				values["ratio"] = Mathf.Min(amount, P("max_ratio"));
				break;
			case "dash_decoy":
				values["decoy_seconds"] = P("seconds_per_copy") * copies;
				break;
		}
		return values;
	}

	public static string Format(string text, IReadOnlyDictionary<string, float> values, string owner) => Token.Replace(text, match =>
	{
		if (!values.TryGetValue(match.Groups[1].Value, out float value))
		{
			GD.PushError($"[ObjectText] Jeton {match.Groups[1].Value} inconnu dans {owner}.");
			return match.Value;
		}
		return match.Groups[2].Value == "%"
			? (value * 100f).ToString("0.#", French) + " %"
			: value.ToString("0.##", French);
	});
}
