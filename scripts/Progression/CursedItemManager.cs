using System.Collections.Generic;
using Godot;
using Vestiges.Core;

namespace Vestiges.Progression;

public class CursedItemData
{
	public string Id { get; set; }
	public string Name { get; set; }
	public string Description { get; set; }
	public string Icon { get; set; }
	public float EnemyHpMultiplier { get; set; } = 1f;
	public float EnemyDmgMultiplier { get; set; } = 1f;
	public float EnemyCountMultiplier { get; set; } = 1f;
	public float EnemySpeedMultiplier { get; set; } = 1f;
	public float XpMultiplier { get; set; } = 1f;
	public float LootMultiplier { get; set; } = 1f;
	public float RareDropBonus { get; set; }
}

/// <summary>
/// Gere les objets maudits : modificateurs de difficulte irreversibles
/// trouves en exploration. Stack avec Appel du Vide via PerkManager.
/// </summary>
public partial class CursedItemManager : Node
{
	private static readonly List<CursedItemData> _allItems = new();
	private static bool _dataLoaded;

	private readonly List<CursedItemData> _activeCurses = new();
	private EventBus _eventBus;
	private PerkManager _perkManager;

	// Aggregated multipliers
	public float EnemyHpMult { get; private set; } = 1f;
	public float EnemyDmgMult { get; private set; } = 1f;
	public float EnemyCountMult { get; private set; } = 1f;
	public float XpMult { get; private set; } = 1f;
	public float LootMult { get; private set; } = 1f;
	public float RareDropBonus { get; private set; }

	public IReadOnlyList<CursedItemData> ActiveCurses => _activeCurses;
	public int ActiveCurseCount => _activeCurses.Count;

	public override void _Ready()
	{
		LoadData();
		_eventBus = GetNode<EventBus>("/root/EventBus");
	}

	public void SetPerkManager(PerkManager perkManager)
	{
		_perkManager = perkManager;
	}

	/// <summary>Ajoute une malediction (irreversible pour la run).</summary>
	public void AddCurse(string curseId)
	{
		CursedItemData data = GetCurseData(curseId);
		if (data == null)
		{
			GD.PushWarning($"[CursedItemManager] Unknown curse: {curseId}");
			return;
		}

		_activeCurses.Add(data);
		Recalculate();

		// Propager via PerkManager pour que SpawnManager recoive les modifiers
		_perkManager?.ApplyExternalDifficultyModifiers(
			data.EnemyCountMultiplier,
			data.EnemyHpMultiplier,
			data.EnemyDmgMultiplier,
			data.XpMultiplier);

		GD.Print($"[CursedItemManager] Curse added: {data.Name} (total: {_activeCurses.Count})");
	}

	public static CursedItemData GetCurseData(string id)
	{
		if (!_dataLoaded) LoadData();
		foreach (CursedItemData item in _allItems)
		{
			if (item.Id == id) return item;
		}
		return null;
	}

	public static List<CursedItemData> GetAllCurseData()
	{
		if (!_dataLoaded) LoadData();
		return _allItems;
	}

	private void Recalculate()
	{
		EnemyHpMult = 1f;
		EnemyDmgMult = 1f;
		EnemyCountMult = 1f;
		XpMult = 1f;
		LootMult = 1f;
		RareDropBonus = 0f;

		foreach (CursedItemData curse in _activeCurses)
		{
			EnemyHpMult *= curse.EnemyHpMultiplier;
			EnemyDmgMult *= curse.EnemyDmgMultiplier;
			EnemyCountMult *= curse.EnemyCountMultiplier;
			XpMult *= curse.XpMultiplier;
			LootMult *= curse.LootMultiplier;
			RareDropBonus += curse.RareDropBonus;
		}
	}

	private static void LoadData()
	{
		if (_dataLoaded) return;

		FileAccess file = FileAccess.Open("res://data/cursed_items/cursed_items.json", FileAccess.ModeFlags.Read);
		if (file == null)
		{
			GD.PushWarning("[CursedItemManager] Cannot open cursed_items.json");
			_dataLoaded = true;
			return;
		}

		string jsonText = file.GetAsText();
		file.Close();

		Json json = new();
		if (json.Parse(jsonText) != Error.Ok)
		{
			GD.PushError($"[CursedItemManager] Parse error: {json.GetErrorMessage()}");
			_dataLoaded = true;
			return;
		}

		Godot.Collections.Array array = json.Data.AsGodotArray();
		foreach (Variant item in array)
		{
			if (item.VariantType != Variant.Type.Dictionary) continue;

			Godot.Collections.Dictionary dict = item.AsGodotDictionary();
			CursedItemData curse = new()
			{
				Id = dict["id"].AsString(),
				Name = dict["name"].AsString(),
				Description = dict.ContainsKey("description") ? dict["description"].AsString() : "",
				Icon = dict.ContainsKey("icon") ? dict["icon"].AsString() : null,
				EnemyHpMultiplier = dict.ContainsKey("enemy_hp_multiplier") ? (float)dict["enemy_hp_multiplier"].AsDouble() : 1f,
				EnemyDmgMultiplier = dict.ContainsKey("enemy_dmg_multiplier") ? (float)dict["enemy_dmg_multiplier"].AsDouble() : 1f,
				EnemyCountMultiplier = dict.ContainsKey("enemy_count_multiplier") ? (float)dict["enemy_count_multiplier"].AsDouble() : 1f,
				EnemySpeedMultiplier = dict.ContainsKey("enemy_speed_multiplier") ? (float)dict["enemy_speed_multiplier"].AsDouble() : 1f,
				XpMultiplier = dict.ContainsKey("xp_multiplier") ? (float)dict["xp_multiplier"].AsDouble() : 1f,
				LootMultiplier = dict.ContainsKey("loot_multiplier") ? (float)dict["loot_multiplier"].AsDouble() : 1f,
				RareDropBonus = dict.ContainsKey("rare_drop_bonus") ? (float)dict["rare_drop_bonus"].AsDouble() : 0f
			};
			_allItems.Add(curse);
		}

		_dataLoaded = true;
		GD.Print($"[CursedItemManager] Loaded {_allItems.Count} cursed items");
	}
}
