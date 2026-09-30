using System;
using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// Petits lieux (plan 22, lot C1) sur de vrais décors : placement (types reconnus, quota, écart, graine), signe à
/// portée, usage unique et récompenses (soin, Essence).
/// </summary>
public partial class SmallPlacesRegression : Node2D
{
	private int _failures;

	public override void _Ready()
	{
		try
		{
			GetNode<GameManager>("/root/GameManager").ChangeState(GameManager.GameState.Run);
			Node2D props = new() { Name = "PropContainer" };
			AddChild(props);
			// Dix puits en ligne tous les 700 px (le quota en garde six), deux veines trop proches l'une de l'autre, un épouvantail.
			for (int i = 0; i < 10; i++)
				AddProp(props, "wild_fields/prop_abandoned_well", new Vector2(i * 700f, 0f));
			AddProp(props, "collapsed_quarry/prop_crystal_vein", new Vector2(0f, 2000f));
			AddProp(props, "collapsed_quarry/prop_crystal_vein", new Vector2(100f, 2000f));
			AddProp(props, "wild_fields/prop_scarecrow_broken", new Vector2(5000f, 5000f));

			List<Vector2> first = Place(props, out SmallPlaceDirector director, 7);
			List<Vector2> again = Place(props, out _, 7);
			int wells = 0, veins = 0, scarecrows = 0;
			bool spaced = true;
			foreach (SmallPlace place in director.Places)
			{
				wells += place.Data.Id == "well" ? 1 : 0;
				veins += place.Data.Id == "crystal_vein" ? 1 : 0;
				scarecrows += place.Data.Id == "scarecrow" ? 1 : 0;
				foreach (SmallPlace other in director.Places)
					spaced &= other == place || other.GlobalPosition.DistanceTo(place.GlobalPosition) >= 600f;
			}
			Check(wells == 6 && veins == 1 && scarecrows == 1 && spaced, $"Placement : {wells} puits (quota 6), {veins} veine (écart 600 px), {scarecrows} épouvantail");
			Check(first.Count == again.Count && first.TrueForAll(again.Contains), "Placement : même graine, mêmes lieux");

			Player player = GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate<Player>();
			AddChild(player);
			player.InitializeCharacter(CharacterDataLoader.Get("traqueur"));
			player.SetPhysicsProcess(false);
			player.DisableDefenseForTests();
			EssenceTracker essence = new() { Name = "EssenceTracker" };
			AddChild(essence);

			SmallPlace well = new List<SmallPlace>(director.Places).Find(place => place.Data.Id == "well");
			player.TakeDamage(40f);
			float hp = player.CurrentHp;
			well.Interact(player);
			bool healed = Mathf.IsEqualApprox(player.CurrentHp - hp, Mathf.Min(40f, player.EffectiveMaxHp * 0.15f));
			float after = player.CurrentHp;
			well.Interact(player);
			Check(healed && Mathf.IsEqualApprox(player.CurrentHp, after) && !well.CanInteract,
				"Puits : soigne 15 % des PV max, une seule fois");

			SmallPlace vein = new List<SmallPlace>(director.Places).Find(place => place.Data.Id == "crystal_vein");
			int before = essence.CurrentEssence;
			vein.Interact(player);
			int gained = essence.CurrentEssence - before;
			Check(gained is >= 6 and <= 10, $"Veine de cristal : {gained} Essence (6 à 10)");

						SmallPlace farWell = new List<SmallPlace>(director.Places).Find(place => place.Data.Id == "well" && place.CanInteract);
			farWell.ShowSign(true);
			bool shown = farWell.SignVisible;
			well.ShowSign(true);
			Check(shown && !well.SignVisible, "Signe : visible sur un lieu encore utile, jamais sur un lieu déjà utilisé");

			GD.Print($"[SmallPlacesRegression] RESULT failures={_failures}");
			GetTree().Quit(_failures == 0 ? 0 : 1);
		}
		catch (Exception exception)
		{
			GD.PushError(exception.ToString());
			GetTree().Quit(2);
		}
	}

	private List<Vector2> Place(Node2D props, out SmallPlaceDirector director, ulong seed)
	{
		director = new SmallPlaceDirector { Name = $"Director{seed}{GetChildCount()}" };
		director.Setup(null, null, seed);
		AddChild(director);
		Node2D container = new() { Name = $"Places{GetChildCount()}" };
		AddChild(container);
		director.PlacePlaces(props, container);
		List<Vector2> positions = new();
		foreach (SmallPlace place in director.Places)
			positions.Add(place.GlobalPosition);
		return positions;
	}

	private static void AddProp(Node2D container, string path, Vector2 position)
	{
		EnvironmentProp prop = new() { Position = position };
		container.AddChild(prop);
		prop.Initialize(GD.Load<Texture2D>($"res://assets/props/{path}.png"), null, 0f, true);
	}

	private void Check(bool ok, string label)
	{
		if (!ok)
			_failures++;
		GD.Print($"[SmallPlacesRegression] {(ok ? "PASS" : "FAIL")} {label}");
	}
}
