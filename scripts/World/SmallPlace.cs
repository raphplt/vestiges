using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Petit lieu (plan 22, lot C1) : un décor déjà posé qui se souvient. Il n'a pas de sprite propre ; il porte
/// l'interaction, un signe discret au sol, et son état. <see cref="SmallPlaceDirector"/> le place, montre son signe
/// et donne sa récompense.
/// </summary>
public partial class SmallPlace : Node2D, IInteractable
{
	// Plus large que le pied du décor, pour que la lueur déborde de dessous le sprite.
	private const float SignRadius = 42f;
	private const float SignHeight = 34f;

	private SmallPlaceDirector _director;
	private InteractableAura _sign;

	public SmallPlaceData Data { get; private set; }
	/// <summary>Famille d'effets des étincelles du lieu, résolue une fois.</summary>
	public Combat.FxFamily Family { get; private set; }
	public bool Used { get; private set; }
	public bool Lost { get; private set; }

	public bool CanInteract => !Used && !Lost;
	public Vector2 InteractPosition => GlobalPosition;
	public Vector2 PromptPosition => GlobalPosition + new Vector2(0f, -SignHeight - 8f);
	public string PromptVerbKey => Data.PromptKey;
	public float HoldTime => Data.HoldSeconds;
	public Color GaugeColor => Data.Color;

	public void Initialize(SmallPlaceData data, SmallPlaceDirector director)
	{
		Data = data;
		Family = Combat.PixelPalette.ParseFamily(data.Family, Combat.FxFamily.Essence);
		_director = director;
		_sign = new InteractableAura { Name = "Sign" };
		AddChild(_sign);
		_sign.Configure(data.Color, data.Color.Lightened(0.25f), SignRadius, SignHeight, withMote: true, pulseSpeed: 1.6f,
			baseAlpha: 0.3f, pulseAlpha: 0.12f, crownAlpha: 0.14f, crownPulseAlpha: 0.08f);
		_sign.SetActive(false);
	}

	public override void _EnterTree() => Interactables.Register(this);

	public override void _ExitTree() => Interactables.Unregister(this);

	/// <summary>Le signe ne se montre qu'à portée du joueur, tant que le lieu peut encore servir.</summary>
	public void ShowSign(bool visible) => _sign.SetActive(visible && CanInteract);

	public bool SignVisible => _sign.Visible;

	public void Interact(Player player)
	{
		if (!CanInteract)
			return;
		Used = true;
		Interactables.Unregister(this);
		_sign.SetActive(false);
		_director.Reward(this, player);
	}

	/// <summary>La zone du lieu est passée au Néant : il s'éteint pour de bon.</summary>
	public void Lose()
	{
		Lost = true;
		Interactables.Unregister(this);
		_sign.SetActive(false);
	}
}
