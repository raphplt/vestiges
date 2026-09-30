using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;
using Vestiges.Spawn;

namespace Vestiges.World;

/// <summary>
/// Petits lieux de la carte (plan 22, lots C1 et C4). Après la génération, parcourt une fois les décors posés et en retient
/// une partie, selon data/world/small_places.json ; ensuite, quatre fois par seconde, montre les signes proches,
/// éteint les lieux passés au Néant et suit les embuscades. Aucune boucle par frame sur les décors. Une embuscade ne
/// paie que gagnée : le joueur qui secoue l'épouvantail et s'enfuit n'a rien.
/// </summary>
public partial class SmallPlaceDirector : Node
{
	private const float UpdateInterval = 0.25f;
	private const float SignSparkChance = 0.6f;
	private const int AmbushAttempts = 4;

	private readonly List<SmallPlace> _places = new();
	private readonly List<Ambush> _ambushes = new();
	// Abribus : vitesse accordée, et le temps qu'il lui reste ; retirée par écart à l'expiration.
	private readonly List<(float Bonus, float Remaining)> _speedBoosts = new();
	private Player _boostedPlayer;
	private PlayerProgression _progression;
	private SmallPlaceConfig _config;
	private SpawnManager _spawner;
	private ErasureManager _erasure;
	private EventBus _eventBus;
	private GroupCache _groups;
	// Placement tiré de la graine de la carte ; récompenses et étincelles sur un tirage à part, pour que le placement
	// ne dépende jamais de ce qui s'est passé en jeu.
	private RandomNumberGenerator _placementRng;
	private readonly RandomNumberGenerator _rng = new();
	private float _timer;

	/// <summary>Épouvantail secoué : ses créatures, la récompense promise, le temps qui reste.</summary>
	private sealed class Ambush
	{
		public SmallPlace Place;
		public readonly HashSet<EnemyLife> Alive = new();
		public float Remaining;
	}

	public IReadOnlyList<SmallPlace> Places => _places;

	public void Setup(SpawnManager spawner, ErasureManager erasure, ulong seed)
	{
		_spawner = spawner;
		_erasure = erasure;
		_placementRng = new RandomNumberGenerator { Seed = seed ^ 0x5A11_91ACEUL };
		_rng.Randomize();
	}

	public override void _Ready()
	{
		_eventBus = GetNode<EventBus>("/root/EventBus");
		_groups = GetNode<GroupCache>("/root/GroupCache");
		_eventBus.EnemyKillResolved += OnEnemyKill;
		_progression = GetNodeOrNull<PlayerProgression>("/root/Main/Player/PlayerProgression");
	}

	public override void _ExitTree()
	{
		if (_eventBus != null)
			_eventBus.EnemyKillResolved -= OnEnemyKill;
	}

	/// <summary>Retient les décors qui deviennent des lieux et pose un lieu sur chacun, dans <paramref name="container"/>.</summary>
	public void PlacePlaces(Node propContainer, Node2D container)
	{
		_config = SmallPlaceDataLoader.Load();
		if (!_config.Enabled)
			return;
		Dictionary<string, SmallPlaceData> bySprite = new();
		Dictionary<SmallPlaceData, List<Vector2>> candidates = new();
		foreach (SmallPlaceData data in _config.Places)
		{
			candidates[data] = new List<Vector2>();
			foreach (string sprite in data.Sprites)
				bySprite[sprite] = data;
		}
		// Quelques dizaines de textures distinctes pour ~10 000 décors : le nom se lit une fois par texture.
		Dictionary<Texture2D, SmallPlaceData> byTexture = new();
		Stack<Node> pending = new();
		pending.Push(propContainer);
		while (pending.Count > 0)
		{
			foreach (Node child in pending.Pop().GetChildren())
			{
				if (child is EnvironmentProp { BaseTexture: { } texture } prop)
				{
					if (!byTexture.TryGetValue(texture, out SmallPlaceData data))
					{
						data = bySprite.GetValueOrDefault(texture.ResourcePath.GetFile().GetBaseName());
						byTexture[texture] = data;
					}
					if (data != null)
						candidates[data].Add(prop.GlobalPosition);
				}
				else if (child is not EnvironmentProp)
					pending.Push(child);
			}
		}

		// Chaque type pose un lieu à son tour : un type rare (épouvantail) n'est pas évincé par un type fréquent.
		float spacingSq = _config.MinSpacingPx * _config.MinSpacingPx;
		List<Vector2> taken = new();
		Dictionary<SmallPlaceData, int> placed = new();
		Dictionary<SmallPlaceData, int> cursor = new();
		foreach (SmallPlaceData data in _config.Places)
		{
			Shuffle(candidates[data], _placementRng);
			placed[data] = 0;
			cursor[data] = 0;
		}
		bool progress = true;
		while (progress)
		{
			progress = false;
			foreach (SmallPlaceData data in _config.Places)
			{
				List<Vector2> pool = candidates[data];
				while (placed[data] < data.MaxPerMap && cursor[data] < pool.Count)
				{
					Vector2 position = pool[cursor[data]++];
					if (TooClose(position, taken, spacingSq))
						continue;
					SmallPlace place = new() { Name = $"SmallPlace_{data.Id}_{placed[data]}", Position = position };
					place.Initialize(data, this);
					container.AddChild(place);
					_places.Add(place);
					taken.Add(position);
					placed[data]++;
					progress = true;
					break;
				}
			}
		}
		foreach (SmallPlaceData data in _config.Places)
			GD.Print($"[SmallPlaceDirector] {data.Id} : {placed[data]} lieux sur {candidates[data].Count} décors");
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		AdvanceAmbushes(dt);
		AdvanceBoosts(dt);
		AdvanceReveals(dt);
		_timer -= dt;
		if (_timer > 0f || _places.Count == 0)
			return;
		_timer = UpdateInterval;
		if (_groups.GetPlayer() is not Node2D player)
			return;
		float rangeSq = _config.SignRangePx * _config.SignRangePx;
		foreach (SmallPlace place in _places)
		{
			if (!place.CanInteract)
				continue;
			if (_erasure != null && _erasure.GetZonePhaseAt(place.GlobalPosition) == ErasureManager.ErasureZonePhase.Void)
			{
				place.Lose();
				continue;
			}
			place.ShowSign(place.GlobalPosition.DistanceSquaredTo(player.GlobalPosition) <= rangeSq);
			// Quelques étincelles montent du lieu : on le repère du coin de l'œil, sans marqueur d'interface.
			if (place.SignVisible && _rng.Randf() < SignSparkChance)
				CombatPools.Instance?.EmitSparks(place.GlobalPosition + new Vector2(_rng.RandfRange(-14f, 14f), -8f), new SparkBurst
				{
					Family = place.Family,
					Owner = FxOwner.Player,
					Count = 2,
					Direction = Vector2.Up,
					Spread = 0.5f,
					SpeedMin = 12f,
					SpeedMax = 28f,
					LifeMin = 0.6f,
					LifeMax = 1.0f,
					Size = 1,
				});
		}
	}

	/// <summary>Récompense d'un lieu qu'on vient d'utiliser.</summary>
	public void Reward(SmallPlace place, Player player)
	{
		SmallPlaceData data = place.Data;
		switch (data.Reward)
		{
			case "heal":
				player.Heal(player.EffectiveMaxHp * data.Amount);
				Burst(place);
				break;
			case "essence":
				GiveEssence(place, _rng.RandiRange(data.AmountMin, data.AmountMax));
				Burst(place);
				break;
			case "ambush":
				StartAmbush(place);
				break;
			case "xp":
				// Une lettre jamais lue : une part de l'XP du niveau suivant, qui garde son poids à tout moment de la run.
				if (_progression != null)
					_eventBus.EmitSignal(EventBus.SignalName.XpGained, _progression.XpToNextLevel * data.Amount);
				Burst(place);
				break;
			case "essence_or_weapon":
				if (_rng.Randf() >= data.Chance || !DropWeapon(place))
					GiveEssence(place, _rng.RandiRange(data.AmountMin, data.AmountMax));
				Burst(place);
				break;
			case "search":
				if (_rng.Randf() < data.Chance)
					player.Heal(player.EffectiveMaxHp * data.Amount);
				else
					GiveEssence(place, _rng.RandiRange(data.AmountMin, data.AmountMax));
				Burst(place);
				break;
			case "reveal":
				RevealAround(place, data);
				Burst(place);
				break;
			case "speed":
				player.ApplyPerkModifier("speed", 1f + data.Amount, "multiplicative");
				_boostedPlayer = player;
				_speedBoosts.Add((data.Amount, data.DurationSeconds));
				Burst(place);
				break;
			case "lore_heal":
				player.Heal(player.EffectiveMaxHp * data.Amount);
				if (data.Lore.Count > 0)
					place.ShowLine(Tr(data.Lore[_rng.RandiRange(0, data.Lore.Count - 1)]));
				Burst(place);
				break;
		}
	}

	/// <summary>Wagonnet : une arme au hasard, posée à côté, à ramasser comme celles des coffres. Faux s'il n'y en a pas.</summary>
	private bool DropWeapon(SmallPlace place)
	{
		WeaponData data = LootRewards.PickRandomWeapon();
		if (data == null)
			return false;
		WeaponPickup pickup = new();
		pickup.Initialize(new WeaponInstance(data), place.GlobalPosition + Iso.ToScreen(new Vector2(40f, 0f)));
		place.GetParent().CallDeferred(Node.MethodName.AddChild, pickup);
		_eventBus.EmitSignal(EventBus.SignalName.LootReceived, "weapon", data.Id, 1);
		return true;
	}

	/// <summary>Cabine téléphonique : les lieux encore utiles à portée reçoivent une flèche de bord d'écran un moment.</summary>
	private void RevealAround(SmallPlace booth, SmallPlaceData data)
	{
		float radiusSq = data.RadiusPx * data.RadiusPx;
		foreach (SmallPlace place in _places)
			if (place != booth && place.CanInteract && place.GlobalPosition.DistanceSquaredTo(booth.GlobalPosition) <= radiusSq)
				place.Reveal(data.DurationSeconds);
	}

	private void AdvanceReveals(float delta)
	{
		IReadOnlyList<SmallPlace> revealed = SmallPlace.Revealed;
		for (int i = revealed.Count - 1; i >= 0; i--)
		{
			SmallPlace place = revealed[i];
			place.RevealRemaining -= delta;
			if (place.RevealRemaining <= 0f)
				place.Unreveal();
		}
	}

	/// <summary>Abribus : chaque vitesse accordée se retire d'elle-même, par écart, sans toucher aux autres bonus.</summary>
	private void AdvanceBoosts(float delta)
	{
		for (int i = _speedBoosts.Count - 1; i >= 0; i--)
		{
			(float bonus, float remaining) = _speedBoosts[i];
			remaining -= delta;
			if (remaining > 0f)
			{
				_speedBoosts[i] = (bonus, remaining);
				continue;
			}
			_speedBoosts.RemoveAt(i);
			if (IsInstanceValid(_boostedPlayer))
				_boostedPlayer.ApplyPerkModifier("speed", 1f / (1f + bonus), "multiplicative");
		}
	}

	private void StartAmbush(SmallPlace place)
	{
		Ambush ambush = new() { Place = place, Remaining = place.Data.AmbushTimeoutSeconds };
		for (int i = 0; i < place.Data.Enemies; i++)
		{
			// Autour du lieu, jamais dans l'eau : une créature qui ne peut pas être tuée bloquerait la récompense.
			for (int attempt = 0; attempt < AmbushAttempts; attempt++)
			{
				Vector2 offset = Vector2.FromAngle(Mathf.Tau * (i + attempt * 0.37f) / place.Data.Enemies + _rng.RandfRange(-0.3f, 0.3f))
					* place.Data.AmbushRadiusPx;
				Vector2 position = place.GlobalPosition + Iso.ToScreen(offset);
				if (_spawner == null || !_spawner.IsSpawnablePosition(position))
					continue;
				Enemy enemy = _spawner.SpawnEventEnemy(_spawner.PickLocalEnemyId(position), position);
				if (enemy != null)
					ambush.Alive.Add(enemy.Life);
				break;
			}
		}
		Burst(place);
		if (ambush.Alive.Count == 0)
			GiveEssence(place, (int)place.Data.Amount);
		else
			_ambushes.Add(ambush);
	}

	private void OnEnemyKill(EnemyKillResult kill)
	{
		for (int i = _ambushes.Count - 1; i >= 0; i--)
		{
			Ambush ambush = _ambushes[i];
			if (!ambush.Alive.Remove(kill.Target) || ambush.Alive.Count > 0)
				continue;
			GiveEssence(ambush.Place, (int)ambush.Place.Data.Amount);
			_ambushes.RemoveAt(i);
		}
	}

	/// <summary>Une embuscade qu'on laisse derrière soi s'oublie : passé le délai, plus rien à gagner.</summary>
	private void AdvanceAmbushes(float delta)
	{
		for (int i = _ambushes.Count - 1; i >= 0; i--)
		{
			_ambushes[i].Remaining -= delta;
			if (_ambushes[i].Remaining <= 0f)
				_ambushes.RemoveAt(i);
		}
	}

	private void GiveEssence(SmallPlace place, int amount)
	{
		if (amount <= 0)
			return;
		_eventBus.EmitSignal(EventBus.SignalName.LootReceived, "essence", place.Data.Id, amount);
		_eventBus.EmitSignal(EventBus.SignalName.EssenceGained, amount, place.GlobalPosition);
	}

	private static void Burst(SmallPlace place)
	{
		CombatPools.Instance?.EmitSparks(place.GlobalPosition + new Vector2(0f, -18f), new SparkBurst
		{
			Family = place.Family,
			Owner = FxOwner.Player,
			Count = 10,
			Direction = Vector2.Up,
			Spread = 1.6f,
			SpeedMin = 30f,
			SpeedMax = 80f,
			LifeMin = 0.3f,
			LifeMax = 0.5f,
			Size = 1,
		});
	}

	private static void Shuffle(List<Vector2> list, RandomNumberGenerator rng)
	{
		for (int i = list.Count - 1; i > 0; i--)
		{
			int j = rng.RandiRange(0, i);
			(list[i], list[j]) = (list[j], list[i]);
		}
	}

	private static bool TooClose(Vector2 position, List<Vector2> taken, float spacingSq)
	{
		foreach (Vector2 other in taken)
			if (position.DistanceSquaredTo(other) < spacingSq)
				return true;
		return false;
	}
}
