using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.Core;

/// <summary>
/// Objet porté (plan 21 §4) : son niveau, de 1 à 50. Ses effets se lisent au niveau courant ; ses paliers
/// s'atteignent une fois pour toutes.
/// </summary>
public class ActivePassiveSouvenir
{
	public string Id { get; }
	public PassiveSouvenirData Data { get; }
	public int Level { get; private set; }
	public bool IsMaxLevel => Level >= Data.MaxLevel;

	public ActivePassiveSouvenir(PassiveSouvenirData data)
	{
		Data = data;
		Id = data.Id;
		Level = 1;
	}

	/// <summary>Valeur d'un effet au niveau courant de l'objet.</summary>
	public float Value(int effect) => Data.Effects[effect].ValueAt(Level);

	/// <summary>Niveau atteint après une amélioration de <paramref name="levels"/> niveaux, borné au maximum.</summary>
	public int LevelAfter(int levels) => Mathf.Min(Data.MaxLevel, Level + levels);

	public bool Reached(ObjectMilestoneData milestone) => Level >= milestone.Level;

	public bool Upgrade(int levels)
	{
		if (IsMaxLevel)
			return false;
		Level = LevelAfter(levels);
		return true;
	}
}
