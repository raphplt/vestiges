using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.World;

public partial class AltarManager : Node2D
{
	private int _altarCount = 4;
	private float _minDistanceBetween = 44f;
	private float _minDistanceFromSpawn = 18f;
	private float _maxDistanceFromSpawn = 170f;
	private float _earlyAltarMaxDistance = 56f;
	private float _interactionRange = 78f;
	private float _cooldownSec = 18f;
	private int _upgradeCost = 18;
	private int _reforgeCost = 28;
	private int _healCost = 20;
	private float _healPercent = 0.30f;

	private readonly List<Vector2I> _occupiedCells = new();

	public override void _Ready()
	{
		LoadConfig();
		CallDeferred(MethodName.SpawnAltars);
	}

	private void SpawnAltars()
	{
		WorldSetup worldSetup = GetParentOrNull<WorldSetup>();
		if (worldSetup == null || !worldSetup.IsWorldReady)
			return;

		TileMapLayer ground = worldSetup.GetNodeOrNull<TileMapLayer>("Ground");
		EssenceTracker essenceTracker = GetParent().GetNodeOrNull<EssenceTracker>("EssenceTracker");
		if (ground == null || essenceTracker == null)
			return;

		RandomNumberGenerator rng = new();
		rng.Randomize();

		for (int i = 0; i < _altarCount; i++)
		{
			bool early = i == 0;
			Vector2I cell = FindValidCell(worldSetup, early, rng);
			if (cell.X == int.MinValue)
				continue;

			_occupiedCells.Add(cell);
			Altar altar = new()
			{
				Name = $"Altar{i + 1}"
			};
			altar.Initialize(
				essenceTracker,
				_interactionRange,
				_cooldownSec,
				_upgradeCost,
				_reforgeCost,
				_healCost,
				_healPercent);
			altar.GlobalPosition = ground.MapToLocal(cell);
			AddChild(altar);
		}
	}

	private Vector2I FindValidCell(WorldSetup worldSetup, bool early, RandomNumberGenerator rng)
	{
		WorldGenerator generator = worldSetup.Generator;
		float maxDist = early ? _earlyAltarMaxDistance : _maxDistanceFromSpawn;

		for (int attempt = 0; attempt < 120; attempt++)
		{
			float angle = rng.RandfRange(0f, Mathf.Tau);
			float radius = Mathf.Lerp(_minDistanceFromSpawn, maxDist, Mathf.Sqrt(rng.Randf()));
			int x = Mathf.RoundToInt(Mathf.Cos(angle) * radius);
			int y = Mathf.RoundToInt(Mathf.Sin(angle) * radius);
			Vector2I cell = new(x, y);

			if (!generator.IsWithinBounds(cell.X, cell.Y) || generator.IsErased(cell.X, cell.Y))
				continue;
			if (generator.GetTerrain(cell.X, cell.Y) == TerrainType.Water)
				continue;

			bool tooClose = false;
			foreach (Vector2I existing in _occupiedCells)
			{
				if (existing.DistanceTo(cell) < _minDistanceBetween)
				{
					tooClose = true;
					break;
				}
			}

			if (tooClose)
				continue;

			return cell;
		}

		return new Vector2I(int.MinValue, int.MinValue);
	}

	private void LoadConfig()
	{
		FileAccess file = FileAccess.Open("res://data/scaling/altars.json", FileAccess.ModeFlags.Read);
		if (file == null)
			return;

		string jsonText = file.GetAsText();
		file.Close();

		Json json = new();
		if (json.Parse(jsonText) != Error.Ok)
			return;

		Godot.Collections.Dictionary dict = json.Data.AsGodotDictionary();
		_altarCount = dict.ContainsKey("altar_count") ? (int)dict["altar_count"].AsDouble() : _altarCount;
		_minDistanceBetween = dict.ContainsKey("min_distance_between") ? (float)dict["min_distance_between"].AsDouble() : _minDistanceBetween;
		_minDistanceFromSpawn = dict.ContainsKey("min_distance_from_spawn") ? (float)dict["min_distance_from_spawn"].AsDouble() : _minDistanceFromSpawn;
		_maxDistanceFromSpawn = dict.ContainsKey("max_distance_from_spawn") ? (float)dict["max_distance_from_spawn"].AsDouble() : _maxDistanceFromSpawn;
		_earlyAltarMaxDistance = dict.ContainsKey("early_altar_max_distance") ? (float)dict["early_altar_max_distance"].AsDouble() : _earlyAltarMaxDistance;
		_interactionRange = dict.ContainsKey("interaction_range") ? (float)dict["interaction_range"].AsDouble() : _interactionRange;
		_cooldownSec = dict.ContainsKey("cooldown_sec") ? (float)dict["cooldown_sec"].AsDouble() : _cooldownSec;
		_upgradeCost = dict.ContainsKey("upgrade_cost") ? (int)dict["upgrade_cost"].AsDouble() : _upgradeCost;
		_reforgeCost = dict.ContainsKey("reforge_cost") ? (int)dict["reforge_cost"].AsDouble() : _reforgeCost;
		_healCost = dict.ContainsKey("heal_cost") ? (int)dict["heal_cost"].AsDouble() : _healCost;
		_healPercent = dict.ContainsKey("heal_percent") ? (float)dict["heal_percent"].AsDouble() : _healPercent;
	}
}

public partial class Altar : Node2D
{
	private const string IdleText = "[E] Ameliorer  [Shift+E] Reforge  [Ctrl+E] Soin";

	private EssenceTracker _essenceTracker;
	private float _interactionRange;
	private float _cooldownSec;
	private int _upgradeCost;
	private int _reforgeCost;
	private int _healCost;
	private float _healPercent;
	private float _cooldownRemaining;
	private Player _player;
	private Polygon2D _core;
	private Label _label;
	private InteractableAura _aura;

	public void Initialize(
		EssenceTracker essenceTracker,
		float interactionRange,
		float cooldownSec,
		int upgradeCost,
		int reforgeCost,
		int healCost,
		float healPercent)
	{
		_essenceTracker = essenceTracker;
		_interactionRange = interactionRange;
		_cooldownSec = cooldownSec;
		_upgradeCost = upgradeCost;
		_reforgeCost = reforgeCost;
		_healCost = healCost;
		_healPercent = healPercent;
	}

	public override void _Ready()
	{
		AddToGroup("altars");

		_core = new Polygon2D
		{
			Polygon = new[]
			{
				new Vector2(0, -18),
				new Vector2(16, 0),
				new Vector2(0, 20),
				new Vector2(-16, 0)
			},
			Color = new Color(0.24f, 0.32f, 0.48f)
		};
		AddChild(_core);

		Polygon2D glow = new()
		{
			Polygon = new[]
			{
				new Vector2(0, -24),
				new Vector2(22, 0),
				new Vector2(0, 26),
				new Vector2(-22, 0)
			},
			Color = new Color(0.42f, 0.84f, 0.92f, 0.16f),
			ZIndex = -1
		};
		AddChild(glow);

		_aura = new InteractableAura();
		_aura.Configure(
			new Color(0.18f, 0.36f, 0.45f, 0.22f),
			new Color(0.48f, 0.9f, 0.96f, 0.28f),
			_interactionRange * 0.45f,
			24f,
			true,
			0.9f,
			0.10f,
			0.08f,
			0.08f,
			0.05f);
		AddChild(_aura);

		_label = new Label
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			Position = new Vector2(-110, 26),
			Size = new Vector2(220, 54),
			Visible = false
		};
		_label.AddThemeFontSizeOverride("font_size", 11);
		AddChild(_label);
	}

	public override void _Process(double delta)
	{
		CachePlayer();
		if (_player == null || !IsInstanceValid(_player))
			return;

		if (_cooldownRemaining > 0f)
			_cooldownRemaining = Mathf.Max(0f, _cooldownRemaining - (float)delta);

		float distance = GlobalPosition.DistanceTo(_player.GlobalPosition);
		bool inRange = distance <= _interactionRange;
		_label.Visible = inRange;
		if (!inRange)
			return;

		UpdateLabel();
		if (!Input.IsActionJustPressed("interact") || _cooldownRemaining > 0f)
			return;

		bool shift = Input.IsKeyPressed(Key.Shift);
		bool ctrl = Input.IsKeyPressed(Key.Ctrl);
		if (ctrl)
			TryHeal();
		else if (shift)
			TryReforge();
		else
			TryUpgrade();
	}

	private void TryUpgrade()
	{
		if (!_essenceTracker.TrySpend(_upgradeCost))
		{
			SetFeedback("Essence insuffisante");
			return;
		}

		if (!_player.UpgradeEquippedWeaponAtAltar())
		{
			_essenceTracker.AddEssence(_upgradeCost);
			SetFeedback("Arme deja au maximum");
			return;
		}

		StartCooldown();
		SetFeedback("Arme amelioree");
	}

	private void TryReforge()
	{
		WeaponInstance equipped = _player.EquippedWeapon;
		if (equipped == null)
		{
			SetFeedback("Aucune arme equipee");
			return;
		}

		if (!_essenceTracker.TrySpend(_reforgeCost))
		{
			SetFeedback("Essence insuffisante");
			return;
		}

		string rarity = WeaponRarityDataLoader.RollReforgeRarity(equipped.Rarity, equipped.Tier);
		_player.ReforgeEquippedWeapon(rarity);
		StartCooldown();
		SetFeedback($"Reforge: {WeaponRarityDataLoader.Get(rarity)?.DisplayName ?? rarity}");
	}

	private void TryHeal()
	{
		if (_player.CurrentHp >= _player.EffectiveMaxHp - 0.1f)
		{
			SetFeedback("Sante deja pleine");
			return;
		}

		if (!_essenceTracker.TrySpend(_healCost))
		{
			SetFeedback("Essence insuffisante");
			return;
		}

		_player.Heal(_player.EffectiveMaxHp * _healPercent);
		StartCooldown();
		SetFeedback("Le corps se souvient");
	}

	private void StartCooldown()
	{
		_cooldownRemaining = _cooldownSec;
		_core.Color = new Color(0.4f, 0.9f, 1f);
		Tween tween = CreateTween();
		tween.TweenProperty(_core, "color", new Color(0.24f, 0.32f, 0.48f), 0.45f);
	}

	private void UpdateLabel()
	{
		if (_cooldownRemaining > 0f)
		{
			_label.Text = $"Autel assoupi ({Mathf.CeilToInt(_cooldownRemaining)}s)";
			_label.AddThemeColorOverride("font_color", new Color(0.62f, 0.72f, 0.86f));
			return;
		}

		_label.Text = $"{IdleText}\n{_upgradeCost}E / {_reforgeCost}E / {_healCost}E";
		_label.AddThemeColorOverride("font_color", new Color(0.82f, 0.92f, 0.98f));
	}

	private void SetFeedback(string text)
	{
		_label.Text = text;
		_label.Visible = true;
	}

	private void CachePlayer()
	{
		if (_player != null && IsInstanceValid(_player))
			return;

		_player = GetTree().GetFirstNodeInGroup("player") as Player;
	}
}
