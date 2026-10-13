using System;
using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// Petits lieux (plan 22, lots C1 et C4) sur de vrais décors : placement (types reconnus, quota, écart, graine), signe à
/// portée, usage unique et récompenses (Essence, XP, arme, révélation, vitesse, lore). Le puits de décor n'est plus un
/// petit lieu depuis le plan 44 : l'oubli a son propre Puits.
/// </summary>
public partial class SmallPlacesRegression : Node2D
{
	private int _failures;

	public override async void _Ready()
	{
		try
		{
			GetNode<GameManager>("/root/GameManager").ChangeState(GameManager.GameState.Run);
			Node2D props = new() { Name = "PropContainer" };
			AddChild(props);
			// Quatre épouvantails de plus que le quota, en ligne tous les 700 px ; deux veines trop proches l'une de l'autre et
			// une troisième à l'écart ; un puits de décor, qui ne doit plus porter de lieu.
			int scarecrowQuota = SmallPlaceDataLoader.Load().Places.Find(data => data.Id == "scarecrow").MaxPerMap;
			for (int i = 0; i < scarecrowQuota + 4; i++)
				AddProp(props, "wild_fields/prop_scarecrow_broken", new Vector2(i * 700f, 0f));
			AddProp(props, "collapsed_quarry/prop_crystal_vein", new Vector2(0f, 2000f));
			AddProp(props, "collapsed_quarry/prop_crystal_vein", new Vector2(100f, 2000f));
			AddProp(props, "collapsed_quarry/prop_crystal_vein", new Vector2(3000f, 2000f));
			Vector2 decorWell = new(5000f, 5000f);
			AddProp(props, "wild_fields/prop_abandoned_well", decorWell);
			// Les six lieux du lot C4, un de chaque, en ligne tous les 700 px.
			string[] c4 = { "urban_ruins/prop_mailbox", "collapsed_quarry/prop_mine_cart", "urban_ruins/prop_urban_car_red_x",
				"urban_ruins/prop_phone_booth", "urban_ruins/prop_bus_shelter", "wild_fields/prop_scene_picnic" };
			for (int i = 0; i < c4.Length; i++)
				AddProp(props, c4[i], new Vector2(i * 700f, 4000f));

			List<Vector2> first = Place(props, out SmallPlaceDirector director, 7);
			List<Vector2> again = Place(props, out _, 7);
			int veins = 0, scarecrows = 0;
			bool spaced = true, wellFree = true;
			foreach (SmallPlace place in director.Places)
			{
				wellFree &= place.GlobalPosition.DistanceTo(decorWell) > 100f;
				veins += place.Data.Id == "crystal_vein" ? 1 : 0;
				scarecrows += place.Data.Id == "scarecrow" ? 1 : 0;
				foreach (SmallPlace other in director.Places)
					spaced &= other == place || other.GlobalPosition.DistanceTo(place.GlobalPosition) >= 600f;
			}
			Check(scarecrows == scarecrowQuota && veins == 2 && spaced && wellFree,
				$"Placement : {scarecrows} épouvantails (quota {scarecrowQuota}), {veins} veines (écart 600 px), aucun lieu sur le puits de décor");
			Check(first.Count == again.Count && first.TrueForAll(again.Contains), "Placement : même graine, mêmes lieux");
			bool allKinds = true;
			foreach (string kind in new[] { "mailbox", "mine_cart", "abandoned_car", "phone_booth", "bus_shelter", "picnic" })
				allKinds &= new List<SmallPlace>(director.Places).Exists(place => place.Data.Id == kind);
			Check(allKinds, "Placement : les six lieux du lot C4 reconnus sur leurs décors");

			Player player = GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate<Player>();
			AddChild(player);
			player.InitializeCharacter(CharacterDataLoader.Get("traqueur"));
			player.SetPhysicsProcess(false);
			player.DisableDefenseForTests();
			float luckBefore = player.LuckBonus;
			EssenceTracker essence = new() { Name = "EssenceTracker" };
			AddChild(essence);

			// Le son de confirmation part à l'usage, pas à une seconde tentative sur un lieu épuisé.
			AudioManager audio = GetNode<AudioManager>("/root/AudioManager");
			int confirmations = 0;
			void OnSound(string key, AudioManager.SoundOutcome outcome)
			{
				if (key == "sfx_poi_activate")
					confirmations++;
			}
			audio.SoundRequested += OnSound;
			SmallPlace vein = new List<SmallPlace>(director.Places).Find(place => place.Data.Id == "crystal_vein");
			int before = essence.CurrentEssence;
			vein.Interact(player);
			int gained = essence.CurrentEssence - before;
			vein.Interact(player);
			audio.SoundRequested -= OnSound;
			Check(gained is >= 6 and <= 10 && essence.CurrentEssence - before == gained && !vein.CanInteract,
				$"Veine de cristal : {gained} Essence (6 à 10), une seule fois");
			Check(confirmations == 1, $"Veine de cristal : un son de confirmation à l'usage, aucun sur le lieu épuisé ({confirmations})");

			await CheckC4(director, player, essence, luckBefore);

			GD.Print($"[SmallPlacesRegression] RESULT failures={_failures}");
			GetTree().Quit(_failures == 0 ? 0 : 1);
		}
		catch (Exception exception)
		{
			GD.PushError(exception.ToString());
			GetTree().Quit(2);
		}
	}

	private async System.Threading.Tasks.Task CheckC4(SmallPlaceDirector director, Player player, EssenceTracker essence, float luckBefore)
	{
		List<SmallPlace> places = new(director.Places);
		SmallPlace Find(string id) => places.Find(place => place.Data.Id == id);
		EventBus events = GetNode<EventBus>("/root/EventBus");

		PlayerProgression progression = new() { Name = "PlayerProgression" };
		player.AddChild(progression);
		typeof(SmallPlaceDirector).GetField("_progression", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
			.SetValue(director, progression);
		float xp = 0f;
		EventBus.XpGainedEventHandler onXp = amount => xp += amount;
		events.XpGained += onXp;
		Find("mailbox").Interact(player);
		events.XpGained -= onXp;
		Check(Mathf.IsEqualApprox(xp, progression.XpToNextLevel * 0.5f), $"Boîte aux lettres : la moitié de l'XP du niveau suivant ({xp:0})");

		int essenceBefore = essence.CurrentEssence;
		SmallPlace cart = Find("mine_cart");
		cart.Interact(player);
		// L'arme est posée en différé, à côté du wagonnet : elle apparaît à la frame suivante.
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		string weapon = null;
		foreach (Node child in cart.GetParent().GetChildren())
			if (child is WeaponPickup pickup)
				weapon = pickup.WeaponInstance.Id;
		Check(weapon != null || essence.CurrentEssence - essenceBefore is >= 8 and <= 14,
			$"Wagonnet : une arme au sol ({weapon ?? "non"}) ou 8 à 14 Essence ({essence.CurrentEssence - essenceBefore})");

		// Emplacements d'armes pleins : même au tirage de l'arme, le Wagonnet ne pose rien et donne de l'Essence
		// (DECISIONS §40). La carte de test n'a qu'un wagonnet, déjà servi : on rejoue directement la pose.
		foreach (WeaponData data in WeaponDataLoader.GetAll())
			if (player.HasFreeWeaponSlot)
				player.AddWeapon(data);
		int pickupsBefore = CountPickups(cart.GetParent());
		bool dropped = (bool)typeof(SmallPlaceDirector).GetMethod("DropWeapon", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
			.Invoke(director, new object[] { cart, player });
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		Check(!player.HasFreeWeaponSlot && !dropped && CountPickups(cart.GetParent()) == pickupsBefore,
			"Wagonnet, emplacements pleins : aucune arme posée, la récompense retombe sur l'Essence");

		player.TakeDamage(30f);
		float hp = player.CurrentHp;
		essenceBefore = essence.CurrentEssence;
		Find("abandoned_car").Interact(player);
		Check(player.CurrentHp > hp || essence.CurrentEssence - essenceBefore is >= 6 and <= 11, "Voiture abandonnée : un soin ou de l'Essence");

		SmallPlace booth = Find("phone_booth");
		booth.Interact(player);
		bool revealed = Revealed(Find("bus_shelter")) && Revealed(Find("picnic"))
			&& !Revealed(booth) && !Revealed(Find("mailbox"));
		director._Process(46.0);
		Check(revealed && !Revealed(Find("bus_shelter")),
			"Cabine téléphonique : les lieux encore utiles à portée reçoivent une flèche, 45 s");

		// Le Repère de l'abribus ajoute une vitesse permanente, qui reste après le bonus de 20 s (plan 24 D3).
		float speed = player.SpeedMultiplier;
		Find("bus_shelter").Interact(player);
		float waymarkSpeed = 1f + WaymarkDataLoader.Load().Rewards["bus_shelter"].Amount;
		bool faster = Mathf.IsEqualApprox(player.SpeedMultiplier / speed, 1.2f * waymarkSpeed);
		director._Process(21.0);
		Check(faster && Mathf.IsEqualApprox(player.SpeedMultiplier, speed * waymarkSpeed), "Abribus : vitesse +20 % pendant 20 s, puis retirée");

		// PV pleins d'abord : les coups des contrôles précédents laissent moins de 20 PV au Traqueur.
		player.Heal(player.EffectiveMaxHp);
		// Le bouclier du Repère de la voiture encaisse un coup entier : un second coup entame les PV.
		player.TakeDamage(20f);
		player.TakeDamage(20f);
		hp = player.CurrentHp;
		SmallPlace picnic = Find("picnic");
		picnic.Interact(player);
		Label line = null;
		foreach (Node child in picnic.GetChildren())
			line ??= child as Label;
		Check(player.CurrentHp > hp && line is { Visible: true } && line.Text.Length > 0 && !line.Text.StartsWith("PLACE_"),
			$"[{hp:0.0} → {player.CurrentHp:0.0} / {player.EffectiveMaxHp:0}] " +
			$"Table de pique-nique : soin léger et une ligne de lore (« {line?.Text} »)");

		// Repères (plan 23 R9, plan 24 D3) : un gain par type de lieu utilisé, propre au lieu, jamais deux fois le même.
		HashSet<string> usedTypes = new();
		foreach (SmallPlace place in places)
			if (place.Used)
				usedTypes.Add(place.Data.Id);
		WaymarkConfig waymarks = WaymarkDataLoader.Load();
		float expectedLuck = 0f;
		foreach (string type in usedTypes)
			if (waymarks.Rewards.TryGetValue(type, out WaymarkReward reward) && reward.Stat == "luck")
				expectedLuck += reward.Amount;
		float luck = player.LuckBonus;
		float maxHp = player.EffectiveMaxHp;
		SmallPlace secondVein = places.Find(place => place.Data.Id == "crystal_vein" && !place.Used);
		secondVein?.Interact(player);
		Check(secondVein != null && usedTypes.Contains("crystal_vein") && player.Waymarks.Found == usedTypes.Count
				&& Mathf.IsEqualApprox(luck - luckBefore, expectedLuck) && Mathf.IsEqualApprox(player.LuckBonus, luck)
				&& Mathf.IsEqualApprox(player.EffectiveMaxHp, maxHp),
			$"Repères : {player.Waymarks.Found} types utilisés, Chance +{(luck - luckBefore) * 100f:0} %, une deuxième veine n'ajoute rien");
		Check(waymarks.Rewards.Count == 13 && waymarks.Rewards["well"].Stat == "max_hp" && waymarks.Rewards["memorial"].Rerolls == 1
				&& waymarks.Rewards["workshop"].Stat == "attack_speed"
				&& waymarks.Rewards["rift"].Banishes == 1,
			"Repères : treize gains, PV max au puits, une relance au Mémorial, un bannissement à la Faille, cadence à l'Atelier");
	}

	private static bool Revealed(SmallPlace place)
	{
		foreach (SmallPlace revealed in SmallPlace.Revealed)
			if (revealed == place)
				return true;
		return false;
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

	private static int CountPickups(Node parent)
	{
		int count = 0;
		foreach (Node child in parent.GetChildren())
			if (child is WeaponPickup)
				count++;
		return count;
	}
}
