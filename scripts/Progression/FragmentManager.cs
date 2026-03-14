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
	private const int DefaultRerolls = 3;
	private const int DefaultBanishes = 3;

	private EventBus _eventBus;
	private Player _player;
	private int _currentLevel = 1;

	// Level-up queue (multi-level-up support)
	private readonly Queue<int> _levelUpQueue = new();
	private bool _choosingActive;

	// Reroll & Banish
	private int _rerollsRemaining = DefaultRerolls;
	private int _banishesRemaining = DefaultBanishes;
	private readonly HashSet<string> _banishedIds = new();

	public int RerollsRemaining => _rerollsRemaining;
	public int BanishesRemaining => _banishesRemaining;

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

		if (_choosingActive)
		{
			_levelUpQueue.Enqueue(newLevel);
			GD.Print($"[FragmentManager] Queued level {newLevel} ({_levelUpQueue.Count} in queue)");
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
			ProcessNextInQueue();
			return;
		}

		_pendingChoices.Clear();
		_pendingChoices.AddRange(PickRandom(options, FragmentsPerChoice));
		_choosingActive = true;

		GD.Print($"[FragmentManager] Level {level} (maxTier={GetMaxFragmentTier(level)}): offering {_pendingChoices.Count} fragments (pool had {options.Count})");

		_eventBus.EmitSignal(EventBus.SignalName.FragmentChoicesReady, _pendingChoices.Count);
	}

	/// <summary>Traite le prochain level-up en attente, ou signale la fin des choix.</summary>
	private void ProcessNextInQueue()
	{
		if (_levelUpQueue.Count > 0)
		{
			int nextLevel = _levelUpQueue.Dequeue();
			GD.Print($"[FragmentManager] Processing queued level-up: {nextLevel} ({_levelUpQueue.Count} remaining)");
			OfferFragments(nextLevel);
		}
		else
		{
			_choosingActive = false;
		}
	}

	/// <summary>Indique si un choix de fragment est actuellement actif (inclut le choix en cours + queue).</summary>
	public bool IsChoiceActive => _choosingActive;

	/// <summary>
	/// Tier max autorisé dans le pool de fragments selon le niveau du joueur.
	/// Progression graduelle : Tier 1 tôt, Tier 3 mid-game, Tier 4-5 très tard.
	/// Petite chance de voir un tier au-dessus du max normal (3% base + luck × 30%).
	/// </summary>
	private int GetMaxFragmentTier(int playerLevel)
	{
		int baseTier;
		if (playerLevel < 5) baseTier = 1;
		else if (playerLevel < 10) baseTier = 2;
		else if (playerLevel < 15) baseTier = 3;
		else if (playerLevel < 20) baseTier = 4;
		else baseTier = 5;

		if (baseTier < 5)
		{
			CachePlayer();
			float luck = _player?.LuckBonus ?? 0f;
			float tierBumpChance = 0.03f + luck * 0.30f;
			if (GD.Randf() < tierBumpChance)
				baseTier++;
		}

		return baseTier;
	}

	private static string TierToRarity(int tier)
	{
		if (tier >= 4) return "rare";
		if (tier >= 3) return "uncommon";
		return "common";
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
				if (_banishedIds.Contains(weapon.Id))
					continue;
				if (weapon.Tier > maxTier)
					continue;
				if (!string.IsNullOrEmpty(weapon.RequiresSouvenir) && !MetaSaveManager.HasSouvenir(weapon.RequiresSouvenir))
					continue;

				pool.Add(new FragmentOption(weapon.Id, "weapon_new", weapon.Name, weapon.Tier, TierToRarity(weapon.Tier)));
			}
		}

		// Upgrades d'armes existantes
		foreach (WeaponInstance w in _player.WeaponSlots)
		{
			if (!_player.IsWeaponFragmentMaxed(w.Id))
			{
				int level = _player.GetWeaponFragmentLevel(w.Id);
				string rarity = TierToRarity(w.Tier);
				pool.Add(new FragmentOption(w.Id, "weapon_upgrade", w.Name, level + 1, rarity));
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
				if (_banishedIds.Contains(passive.Id))
					continue;
				pool.Add(new FragmentOption(passive.Id, "passive_new", passive.Name, 1));
			}
		}

		// Upgrades de passifs existants
		foreach (ActivePassiveSouvenir p in _player.PassiveSlots)
		{
			if (!p.IsMaxLevel)
			{
				string rarity = p.Level + 1 >= p.Data.MaxLevel ? "uncommon" : "common";
				pool.Add(new FragmentOption(p.Id, "passive_upgrade", p.Data.Name, p.Level + 1, rarity));
			}
		}

		return pool;
	}

	/// <summary>Relance les choix de fragments (consomme un reroll).</summary>
	public void Reroll()
	{
		if (_rerollsRemaining <= 0)
			return;

		_rerollsRemaining--;
		GD.Print($"[FragmentManager] Reroll used ({_rerollsRemaining} remaining)");
		OfferFragments(_currentLevel);
	}

	/// <summary>Bannit un fragment du pool de la run entière et re-propose des choix.</summary>
	public void BanishFragment(string id)
	{
		if (_banishesRemaining <= 0)
			return;

		_banishedIds.Add(id);
		_banishesRemaining--;
		GD.Print($"[FragmentManager] Banished '{id}' ({_banishesRemaining} remaining, total banished: {_banishedIds.Count})");
		OfferFragments(_currentLevel);
	}

	public void AddRerolls(int count) => _rerollsRemaining += count;
	public void AddBanishes(int count) => _banishesRemaining += count;

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
			ProcessNextInQueue();
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

	private List<FragmentOption> PickRandom(List<FragmentOption> pool, int count)
	{
		CachePlayer();
		float luck = _player?.LuckBonus ?? 0f;

		// Séparer new vs upgrade pour garantir un mélange
		List<FragmentOption> newItems = new();
		List<FragmentOption> upgrades = new();
		foreach (FragmentOption o in pool)
		{
			if (o.Type is "weapon_new" or "passive_new")
				newItems.Add(o);
			else
				upgrades.Add(o);
		}

		List<FragmentOption> result = new();
		List<FragmentOption> remaining = new(pool);

		// Garantir au moins 1 de chaque catégorie si possible
		if (newItems.Count > 0 && upgrades.Count > 0 && count >= 2)
		{
			FragmentOption picked = WeightedPick(newItems, luck);
			result.Add(picked);
			remaining.Remove(picked);

			picked = WeightedPick(upgrades, luck);
			result.Add(picked);
			remaining.Remove(picked);
		}

		// Remplir le reste avec sélection pondérée
		while (result.Count < count && remaining.Count > 0)
		{
			FragmentOption picked = WeightedPick(remaining, luck);
			result.Add(picked);
			remaining.Remove(picked);
		}

		return result;
	}

	private static FragmentOption WeightedPick(List<FragmentOption> options, float luck)
	{
		if (options.Count == 1)
			return options[0];

		float totalWeight = 0f;
		foreach (FragmentOption opt in options)
			totalWeight += GetFragmentWeight(opt, luck);

		float roll = GD.Randf() * totalWeight;
		float cumulative = 0f;
		foreach (FragmentOption opt in options)
		{
			cumulative += GetFragmentWeight(opt, luck);
			if (roll <= cumulative)
				return opt;
		}
		return options[options.Count - 1];
	}

	private static float GetFragmentWeight(FragmentOption opt, float luck)
	{
		float weight = 1f;
		// Tiers élevés sont plus rares, mais la luck les booste
		if (opt.SortWeight >= 4) weight = 0.3f + luck * 1.5f;
		else if (opt.SortWeight >= 3) weight = 0.5f + luck * 1.0f;
		// Upgrades légèrement favorisées (aide à compléter les builds)
		if (opt.Type.Contains("upgrade")) weight *= 1.15f;
		return Mathf.Max(weight, 0.1f);
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
	/// <summary>"common", "uncommon", ou "rare" — déterminé par le tier.</summary>
	public string Rarity { get; }

	public FragmentOption(string id, string type, string displayName, int sortWeight, string rarity = "common")
	{
		Id = id;
		Type = type;
		DisplayName = displayName;
		SortWeight = sortWeight;
		Rarity = rarity;
	}
}
