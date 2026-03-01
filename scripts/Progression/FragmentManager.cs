using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Progression;

/// <summary>
/// Gère le système de Fragments de Mémoire au level-up.
/// Remplace PerkManager pour les choix de level-up.
/// Propose un mélange d'armes et de Souvenirs Passifs (3 choix).
/// Gère la détection de fusions (arme max + passif max → Vestige).
/// </summary>
public partial class FragmentManager : Node
{
	private const int FragmentsPerChoice = 3;

	private EventBus _eventBus;
	private Player _player;
	private int _currentLevel = 1;

	// Pending fusion (offered at next chest)
	private FusionData _pendingFusion;

	// Choix en attente — l'UI lit PendingChoices après le signal
	private readonly List<FragmentOption> _pendingChoices = new();

	/// <summary>Les choix de fragments en attente de sélection par le joueur.</summary>
	public IReadOnlyList<FragmentOption> PendingChoices => _pendingChoices;

	// Plus de signaux locaux — tout passe par l'EventBus (Autoload fiable)
	// Les signaux sur nodes dynamiques (new + AddChild) ne sont pas fiables en Godot C#.

	public override void _Ready()
	{
		PassiveSouvenirDataLoader.Load();
		FusionDataLoader.Load();

		_eventBus = GetNode<EventBus>("/root/EventBus");
		_eventBus.LevelUp += OnLevelUp;
		_eventBus.ChestOpened += OnChestOpened;
	}

	public override void _ExitTree()
	{
		if (_eventBus != null)
		{
			_eventBus.LevelUp -= OnLevelUp;
			_eventBus.ChestOpened -= OnChestOpened;
		}
	}

	private void OnLevelUp(int newLevel)
	{
		GD.Print($"[FragmentManager] OnLevelUp called: level {newLevel}");
		CachePlayer();
		if (_player == null)
		{
			GD.PushWarning("[FragmentManager] OnLevelUp: player is null — level-up sera rattrapé par GameBootstrap");
			return;
		}

		OfferFragments(newLevel);
	}

	/// <summary>
	/// Appelé par GameBootstrap pour rattraper des level-ups manqués
	/// (XP gagnée avant que le setup soit complet).
	/// </summary>
	public void TriggerLevelUp(int currentLevel)
	{
		GD.Print($"[FragmentManager] TriggerLevelUp (rattrapage): level {currentLevel}");
		CachePlayer();
		if (_player == null)
		{
			GD.PushWarning("[FragmentManager] TriggerLevelUp: player is null");
			return;
		}

		OfferFragments(currentLevel);
	}

	private void OfferFragments(int level)
	{
		_currentLevel = level;
		List<FragmentOption> options = BuildFragmentPool();
		if (options.Count == 0)
		{
			GD.PushWarning($"[FragmentManager] OfferFragments: pool is empty (level {level})");
			return;
		}

		_pendingChoices.Clear();
		_pendingChoices.AddRange(PickRandom(options, FragmentsPerChoice));

		GD.Print($"[FragmentManager] Level {level} (maxTier={GetMaxFragmentTier(level)}): offering {_pendingChoices.Count} fragments (pool had {options.Count})");

		_eventBus.EmitSignal(EventBus.SignalName.FragmentChoicesReady, _pendingChoices.Count);
	}

	/// <summary>
	/// Tier max autorisé dans le pool de fragments selon le niveau du joueur.
	/// Progression graduelle : Tier 1 tôt, Tier 3 mid-game, Tier 4-5 très tard.
	/// </summary>
	private static int GetMaxFragmentTier(int playerLevel)
	{
		if (playerLevel < 5)
			return 1;
		if (playerLevel < 10)
			return 2;
		if (playerLevel < 15)
			return 3;
		if (playerLevel < 20)
			return 4;
		return 5;
	}

	private List<FragmentOption> BuildFragmentPool()
	{
		List<FragmentOption> pool = new();
		int weaponCount = _player.WeaponSlots.Count;
		int passiveCount = _player.PassiveSlots.Count;
		bool weaponSlotsFull = weaponCount >= Player.MaxWeaponSlots;
		bool passiveSlotsFull = passiveCount >= Player.MaxPassiveSlots;

		// Armes nouvelles (si slots dispo)
		int maxTier = GetMaxFragmentTier(_currentLevel);
		if (!weaponSlotsFull)
		{
			HashSet<string> equippedIds = new();
			foreach (WeaponInstance w in _player.WeaponSlots)
				equippedIds.Add(w.Id);

			foreach (WeaponData weapon in WeaponDataLoader.GetAll())
			{
				if (equippedIds.Contains(weapon.Id))
					continue;
				if (weapon.Source == "craft")
					continue;
				if (weapon.Tier > maxTier)
					continue;
				if (!string.IsNullOrEmpty(weapon.RequiresSouvenir) && !MetaSaveManager.HasSouvenir(weapon.RequiresSouvenir))
					continue;

				pool.Add(new FragmentOption(weapon.Id, "weapon_new", weapon.Name, weapon.Tier));
			}
		}

		// Upgrades d'armes existantes
		foreach (WeaponInstance w in _player.WeaponSlots)
		{
			if (!_player.IsWeaponFragmentMaxed(w.Id))
			{
				int level = _player.GetWeaponFragmentLevel(w.Id);
				pool.Add(new FragmentOption(w.Id, "weapon_upgrade", w.Name, level + 1));
			}
		}

		// Souvenirs Passifs nouveaux (si slots dispo)
		if (!passiveSlotsFull)
		{
			HashSet<string> equippedPassiveIds = new();
			foreach (ActivePassiveSouvenir p in _player.PassiveSlots)
				equippedPassiveIds.Add(p.Id);

			foreach (PassiveSouvenirData passive in PassiveSouvenirDataLoader.GetAll())
			{
				if (equippedPassiveIds.Contains(passive.Id))
					continue;
				pool.Add(new FragmentOption(passive.Id, "passive_new", passive.Name, 1));
			}
		}

		// Upgrades de passifs existants
		foreach (ActivePassiveSouvenir p in _player.PassiveSlots)
		{
			if (!p.IsMaxLevel)
				pool.Add(new FragmentOption(p.Id, "passive_upgrade", p.Data.Name, p.Level + 1));
		}

		return pool;
	}

	public void SelectFragment(string fragmentId, string fragmentType)
	{
		CachePlayer();
		if (_player == null)
			return;

		bool success = false;

		switch (fragmentType)
		{
			case "weapon_new":
				WeaponData weaponData = WeaponDataLoader.Get(fragmentId);
				if (weaponData != null)
					success = _player.AddWeapon(weaponData);
				break;
			case "weapon_upgrade":
				success = _player.UpgradeWeaponFragmentLevel(fragmentId);
				break;
			case "passive_new":
			case "passive_upgrade":
				success = _player.AddOrUpgradePassive(fragmentId);
				break;
		}

		if (success)
		{
			_pendingChoices.Clear();
			_eventBus.EmitSignal(EventBus.SignalName.FragmentChosen, fragmentId, fragmentType);
			GD.Print($"[FragmentManager] Fragment selected: {fragmentId} ({fragmentType})");
			CheckFusions();
		}
	}

	private void CheckFusions()
	{
		if (_pendingFusion != null)
			return;

		List<string> maxedWeapons = _player.GetMaxedWeaponIds();
		List<string> maxedPassives = _player.GetMaxedPassiveIds();

		if (maxedWeapons.Count == 0 || maxedPassives.Count == 0)
			return;

		List<FusionData> available = FusionDataLoader.FindAvailableFusions(maxedWeapons, maxedPassives);
		if (available.Count == 0)
			return;

		_pendingFusion = available[0];
		_eventBus.EmitSignal(EventBus.SignalName.FusionAvailable, _pendingFusion.Id, _pendingFusion.WeaponId, _pendingFusion.PassiveId);
		GD.Print($"[FragmentManager] Fusion available: {_pendingFusion.Name} ({_pendingFusion.WeaponId} + {_pendingFusion.PassiveId})");
	}

	private void OnChestOpened(string chestId, string rarity, Vector2 position)
	{
		if (_pendingFusion == null)
			return;
		GD.Print($"[FragmentManager] Offering fusion at chest: {_pendingFusion.Name}");
	}

	public bool ApplyFusion(string fusionId)
	{
		CachePlayer();
		FusionData fusion = FusionDataLoader.Get(fusionId);
		if (fusion == null || _player == null)
			return false;

		int weaponSlotIndex = -1;
		for (int i = 0; i < _player.WeaponSlots.Count; i++)
		{
			if (_player.WeaponSlots[i].Id == fusion.WeaponId)
			{
				weaponSlotIndex = i;
				break;
			}
		}

		if (weaponSlotIndex < 0)
			return false;

		_player.RemoveWeapon(weaponSlotIndex);
		_player.RemovePassive(fusion.PassiveId);
		WeaponData vestige = CreateVestigeWeapon(fusion);
		_player.AddWeapon(vestige);
		_pendingFusion = null;

		_eventBus.EmitSignal(EventBus.SignalName.FusionCompleted, fusionId);
		GD.Print($"[FragmentManager] Fusion completed: {fusion.Name}");
		return true;
	}

	public void DeclineFusion()
	{
		if (_pendingFusion != null)
		{
			GD.Print($"[FragmentManager] Fusion declined: {_pendingFusion.Name}");
			_pendingFusion = null;
		}
	}

	public bool HasPendingFusion => _pendingFusion != null;
	public FusionData PendingFusion => _pendingFusion;

	private static WeaponData CreateVestigeWeapon(FusionData fusion)
	{
		WeaponData vestige = new()
		{
			Id = fusion.Id,
			Name = fusion.Name,
			Description = fusion.Description,
			Tier = 5,
			Type = fusion.Type ?? "melee",
			DamageType = fusion.DamageType ?? "physical",
			AttackPattern = fusion.AttackPattern ?? "arc",
			Stats = new Dictionary<string, float>(fusion.Stats)
		};

		if (!string.IsNullOrEmpty(fusion.SpecialEffectType))
		{
			vestige.SpecialEffect = new WeaponSpecialEffect
			{
				Type = fusion.SpecialEffectType,
				Params = new Dictionary<string, float>(fusion.SpecialEffectParams)
			};
		}

		return vestige;
	}

	private static List<FragmentOption> PickRandom(List<FragmentOption> pool, int count)
	{
		List<FragmentOption> result = new();
		List<FragmentOption> available = new(pool);

		for (int i = 0; i < count && available.Count > 0; i++)
		{
			int index = (int)(GD.Randi() % available.Count);
			result.Add(available[index]);
			available.RemoveAt(index);
		}

		return result;
	}

	private void CachePlayer()
	{
		if (_player != null && IsInstanceValid(_player))
			return;

		Node playerNode = GetTree().GetFirstNodeInGroup("player");
		if (playerNode is Player p)
			_player = p;
	}
}

public class FragmentOption
{
	public string Id { get; }
	public string Type { get; }
	public string DisplayName { get; }
	public int SortWeight { get; }

	public FragmentOption(string id, string type, string displayName, int sortWeight)
	{
		Id = id;
		Type = type;
		DisplayName = displayName;
		SortWeight = sortWeight;
	}
}
