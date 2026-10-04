using System;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Une valeur admise par un contrat de données (armes, créatures) : bornes, entière ou non, secours (sans secours,
/// elle est obligatoire).
/// </summary>
public readonly record struct DataValueRule(float? Default, float Min, float Max, bool Integer)
{
	/// <summary>Un facteur ou un poids : toute valeur strictement positive.</summary>
	public static readonly DataValueRule Positive = new(null, float.Epsilon, float.MaxValue, false);
	public static readonly DataValueRule NonNegative = new(null, 0f, float.MaxValue, false);

	public bool Required => Default == null;

	/// <summary>Raison du refus, ou null si la valeur convient. Nombres écrits comme dans le JSON, quelle que soit la langue.</summary>
	public string Reject(float value)
	{
		if (!float.IsFinite(value))
			return "valeur non finie";
		if (value < Min || value > Max)
		{
			if (Min == float.Epsilon)
				return Invariant($"{value} : strictement positif attendu");
			if (Max == float.MaxValue)
				return Invariant($"{value} inférieur au minimum {Min}");
			return Invariant($"{value} hors de [{Min} ; {Max}]");
		}
		if (Integer && value != Mathf.Floor(value))
			return Invariant($"{value} n'est pas entier");
		return null;
	}

	private static string Invariant(FormattableString text) => FormattableString.Invariant(text);
}
