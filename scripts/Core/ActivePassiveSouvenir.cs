using Vestiges.Infrastructure;

namespace Vestiges.Core;

/// <summary>
/// Objet porté (plan 21 §4, plan 23 R3) : son niveau, de 1 à 30, et la somme des gains de chaque effet. Chaque carte
/// donne un niveau et ajoute à chaque effet son pas multiplié par le gain de sa rareté ; l'objet neuf vaut un pas.
/// Ses paliers s'atteignent une fois pour toutes.
/// </summary>
public class ActivePassiveSouvenir
{
	public string Id { get; }
	public PassiveSouvenirData Data { get; }
	public int Level { get; private set; }
	public bool IsMaxLevel => Level >= Data.MaxLevel;

	private readonly float[] _gains;

	public ActivePassiveSouvenir(PassiveSouvenirData data)
	{
		Data = data;
		Id = data.Id;
		Level = 1;
		_gains = new float[data.Effects.Count];
		for (int i = 0; i < _gains.Length; i++)
			_gains[i] = data.Effects[i].Step;
	}

	/// <summary>Valeur d'un effet : son neutre plus la somme de ses gains.</summary>
	public float Value(int effect) => Data.Effects[effect].Neutral + _gains[effect];

	/// <summary>Valeur d'un effet après une carte de gain <paramref name="gain"/> (1 pour une commune).</summary>
	public float ValueAfter(int effect, float gain) => Value(effect) + Data.Effects[effect].Step * gain;

	public bool Reached(ObjectMilestoneData milestone) => Level >= milestone.Level;

	/// <summary>Une carte : un niveau de plus et le pas de chaque effet, multiplié par <paramref name="gain"/>.</summary>
	public bool Upgrade(float gain)
	{
		if (IsMaxLevel)
			return false;
		Level++;
		for (int i = 0; i < _gains.Length; i++)
			_gains[i] += Data.Effects[i].Step * gain;
		return true;
	}
}
