using Vestiges.Infrastructure;

namespace Vestiges.Core;

/// <summary>
/// Écusson porté : niveaux et gains des cartes, avec rendement et paliers. Objet du sac : copies entières,
/// effet linéaire par copie, sans palier ni gain de rareté. Les deux partagent le calcul des valeurs d’effet.
/// </summary>
public class ActivePassiveSouvenir
{
	public string Id { get; }
	public PassiveSouvenirData Data { get; }
	public int Level { get; private set; }
	/// <summary>Exemplaires d'un objet dans le sac (plan 35) ; 0 pour un Écusson.</summary>
	public int Copies { get; internal set; }
	public bool IsMaxLevel => Data.IsBadge ? Level >= Data.MaxLevel : Data.IsCopyLimitReached(Copies);

	private readonly float[] _gains;

	public ActivePassiveSouvenir(PassiveSouvenirData data)
	{
		Data = data;
		Id = data.Id;
		Level = 1;
		Copies = data.IsBadge ? 0 : 1;
		_gains = new float[data.Effects.Count];
		for (int i = 0; i < _gains.Length; i++)
			_gains[i] = data.Effects[i].Step;
	}

	/// <summary>Valeur d'un effet : son neutre plus la somme de ses gains.</summary>
	public float Value(int effect) => Data.Effects[effect].Neutral + _gains[effect];

	/// <summary>Valeur d'un effet après une carte de gain <paramref name="gain"/> (1 pour une commune).</summary>
	public float ValueAfter(int effect, float gain) => Value(effect) + Data.Effects[effect].Step * gain * StepFactor(Level + 1);

	public bool Reached(ObjectMilestoneData milestone) => Data.IsBadge && Level >= milestone.Level;

	/// <summary>Une carte : un niveau de plus et le pas de chaque effet, multiplié par <paramref name="gain"/>.</summary>
	public bool Upgrade(float gain)
	{
		if (!Data.IsBadge || IsMaxLevel)
			return false;
		Level++;
		float factor = StepFactor(Level);
		for (int i = 0; i < _gains.Length; i++)
			_gains[i] += Data.Effects[i].Step * gain * factor;
		return true;
	}

	/// <summary>
	/// Fixe le nombre réel de copies ; 0 ramène chaque effet au neutre.
	/// L'appelant applique l'écart de valeur au joueur.
	/// </summary>
	public void SetItemCopies(int copies)
	{
		if (Data.IsBadge)
			return;
		Copies = System.Math.Max(0, copies);
		Level = Copies;
		for (int i = 0; i < _gains.Length; i++)
			_gains[i] = Data.Effects[i].Step * Copies;
	}

	/// <summary>Rendement d'un niveau : un pas entier, sauf pour un Écusson au-delà du seuil d'inventory.json.</summary>
	private float StepFactor(int level) => Data.IsBadge ? InventoryConfig.BadgeStepFactor(level) : 1f;
}
