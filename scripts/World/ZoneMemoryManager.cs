using System.Collections.Generic;
using Godot;
using Vestiges.Core;

namespace Vestiges.World;

/// <summary>
/// Systeme de memoire de zone : les zones non-visitees "s'oublient" (plus d'ennemis,
/// visuellement corrompues). La presence du joueur et la decouverte de lore stabilisent les zones.
/// Le Foyer agit comme ancre permanente de memoire.
/// </summary>
public partial class ZoneMemoryManager : Node
{
	private int _cellSize = 128;
	private float _initialMemory = 0.5f;
	private float _decayPerMinute = 0.03f;
	private float _playerPresenceRate = 0.15f;
	private int _playerRadiusCells = 3;
	private int _foyerRadiusCells = 6;
	private float _foyerMemory = 1.0f;
	private int _loreStabilizeRadiusCells = 5;
	private float _loreStabilizeDurationMin = 20f;
	private float _fadedSpawnDensityMult = 1.8f;
	private float _fadedEnemySpeedBonus = 0.2f;
	private float _updateIntervalSec = 1.0f;

	private readonly Dictionary<Vector2I, float> _zoneMemory = new();
	private readonly Dictionary<Vector2I, float> _stabilizedUntil = new();
	private float _updateTimer;
	private float _totalElapsed;

	private Player _player;
	private Vector2 _foyerPosition;
	private EventBus _eventBus;

	// Visual overlay
	private ZoneMemoryOverlay _overlay;

	public override void _Ready()
	{
		LoadConfig();

		_eventBus = GetNode<EventBus>("/root/EventBus");
		_eventBus.SouvenirDiscovered += OnSouvenirDiscovered;
		_eventBus.PoiDiscovered += OnPoiDiscovered;

		Node2D foyer = GetNodeOrNull<Node2D>("../Foyer");
		if (foyer != null)
			_foyerPosition = foyer.GlobalPosition;

		// Create visual overlay as sibling Node2D (needs to be in scene tree for drawing)
		_overlay = new ZoneMemoryOverlay(this, _cellSize);
		_overlay.Name = "ZoneMemoryOverlay";
		_overlay.ZIndex = -1;
		CallDeferred("add_child", _overlay);
	}

	public override void _ExitTree()
	{
		if (_eventBus != null)
		{
			_eventBus.SouvenirDiscovered -= OnSouvenirDiscovered;
			_eventBus.PoiDiscovered -= OnPoiDiscovered;
		}
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		_totalElapsed += dt;
		_updateTimer += dt;

		if (_updateTimer < _updateIntervalSec)
			return;

		_updateTimer = 0f;
		CachePlayer();
		if (_player == null) return;

		float decayAmount = _decayPerMinute * (_updateIntervalSec / 60f);
		float presenceAmount = _playerPresenceRate * _updateIntervalSec;

		// Decay all known cells
		List<Vector2I> toRemove = null;
		foreach (Vector2I cell in new List<Vector2I>(_zoneMemory.Keys))
		{
			// Skip foyer zone
			if (IsFoyerCell(cell))
			{
				_zoneMemory[cell] = _foyerMemory;
				continue;
			}

			// Skip stabilized cells
			if (_stabilizedUntil.TryGetValue(cell, out float until) && _totalElapsed < until)
			{
				_zoneMemory[cell] = Mathf.Max(_zoneMemory[cell], 0.8f);
				continue;
			}

			float newVal = _zoneMemory[cell] - decayAmount;
			if (newVal <= 0.01f)
			{
				toRemove ??= new List<Vector2I>();
				toRemove.Add(cell);
			}
			else
			{
				_zoneMemory[cell] = newVal;
			}
		}

		if (toRemove != null)
		{
			foreach (Vector2I cell in toRemove)
				_zoneMemory[cell] = 0f;
		}

		// Player presence increases memory nearby
		Vector2I playerCell = WorldToCell(_player.GlobalPosition);
		for (int dx = -_playerRadiusCells; dx <= _playerRadiusCells; dx++)
		{
			for (int dy = -_playerRadiusCells; dy <= _playerRadiusCells; dy++)
			{
				Vector2I cell = new(playerCell.X + dx, playerCell.Y + dy);
				float dist = Mathf.Sqrt(dx * dx + dy * dy);
				if (dist > _playerRadiusCells) continue;

				float falloff = 1f - (dist / (_playerRadiusCells + 1));
				float current = GetMemoryAtCell(cell);
				float newVal = Mathf.Min(1f, current + presenceAmount * falloff);
				_zoneMemory[cell] = newVal;
			}
		}

		// Ensure foyer zone is always at max
		Vector2I foyerCell = WorldToCell(_foyerPosition);
		for (int dx = -_foyerRadiusCells; dx <= _foyerRadiusCells; dx++)
		{
			for (int dy = -_foyerRadiusCells; dy <= _foyerRadiusCells; dy++)
			{
				float dist = Mathf.Sqrt(dx * dx + dy * dy);
				if (dist > _foyerRadiusCells) continue;
				Vector2I cell = new(foyerCell.X + dx, foyerCell.Y + dy);
				_zoneMemory[cell] = _foyerMemory;
			}
		}

		// Refresh visual overlay
		_overlay?.QueueRedraw();
	}

	public float InitialMemory => _initialMemory;

	/// <summary>
	/// Retourne les cellules visibles dans un rectangle monde avec leur memoire.
	/// Utilise par l'overlay pour savoir quoi dessiner.
	/// </summary>
	public void GetCellsInRect(Rect2 worldRect, List<(Vector2I cell, float memory)> output)
	{
		output.Clear();
		Vector2I min = WorldToCell(worldRect.Position);
		Vector2I max = WorldToCell(worldRect.Position + worldRect.Size);

		for (int x = min.X - 1; x <= max.X + 1; x++)
		{
			for (int y = min.Y - 1; y <= max.Y + 1; y++)
			{
				Vector2I cell = new(x, y);
				float memory = GetMemoryAtCell(cell);
				if (memory < 0.95f)
					output.Add((cell, memory));
			}
		}
	}

	/// <summary>Convertit une cellule en position monde (coin haut-gauche).</summary>
	public Vector2 CellToWorld(Vector2I cell)
	{
		return new Vector2(cell.X * _cellSize, cell.Y * _cellSize);
	}

	/// <summary>Retourne la memoire (0.0-1.0) a une position monde.</summary>
	public float GetMemoryAt(Vector2 worldPos)
	{
		Vector2I cell = WorldToCell(worldPos);
		return GetMemoryAtCell(cell);
	}

	/// <summary>
	/// Multiplicateur de densite d'ennemis pour une position (1.0 = normal, >1.0 = plus d'ennemis).
	/// </summary>
	public float GetSpawnDensityMultiplier(Vector2 worldPos)
	{
		float memory = GetMemoryAt(worldPos);
		return Mathf.Lerp(_fadedSpawnDensityMult, 1f, memory);
	}

	/// <summary>Bonus de vitesse ennemi dans les zones oubliees (0.0 = pas de bonus).</summary>
	public float GetEnemySpeedBonus(Vector2 worldPos)
	{
		float memory = GetMemoryAt(worldPos);
		return Mathf.Lerp(_fadedEnemySpeedBonus, 0f, memory);
	}

	/// <summary>Stabilise une zone autour d'une position (decouverte de lore).</summary>
	public void StabilizeZone(Vector2 worldPos)
	{
		Vector2I center = WorldToCell(worldPos);
		float stabilizeUntil = _totalElapsed + _loreStabilizeDurationMin * 60f;

		for (int dx = -_loreStabilizeRadiusCells; dx <= _loreStabilizeRadiusCells; dx++)
		{
			for (int dy = -_loreStabilizeRadiusCells; dy <= _loreStabilizeRadiusCells; dy++)
			{
				float dist = Mathf.Sqrt(dx * dx + dy * dy);
				if (dist > _loreStabilizeRadiusCells) continue;

				Vector2I cell = new(center.X + dx, center.Y + dy);
				_zoneMemory[cell] = Mathf.Max(GetMemoryAtCell(cell), 0.9f);
				_stabilizedUntil[cell] = stabilizeUntil;
			}
		}

		GD.Print($"[ZoneMemoryManager] Zone stabilized at ({worldPos.X:F0}, {worldPos.Y:F0}) for {_loreStabilizeDurationMin} min");
	}

	private void OnSouvenirDiscovered(string souvenirId, string souvenirName, string constellationId)
	{
		CachePlayer();
		if (_player != null)
			StabilizeZone(_player.GlobalPosition);
	}

	private void OnPoiDiscovered(string poiId, string poiType, Vector2 position)
	{
		StabilizeZone(position);
	}

	private float GetMemoryAtCell(Vector2I cell)
	{
		if (_zoneMemory.TryGetValue(cell, out float val))
			return val;

		return _initialMemory;
	}

	private bool IsFoyerCell(Vector2I cell)
	{
		Vector2I foyerCell = WorldToCell(_foyerPosition);
		int dx = cell.X - foyerCell.X;
		int dy = cell.Y - foyerCell.Y;
		return Mathf.Sqrt(dx * dx + dy * dy) <= _foyerRadiusCells;
	}

	private Vector2I WorldToCell(Vector2 worldPos)
	{
		return new Vector2I(
			Mathf.FloorToInt(worldPos.X / _cellSize),
			Mathf.FloorToInt(worldPos.Y / _cellSize));
	}

	private void CachePlayer()
	{
		if (_player != null && IsInstanceValid(_player))
			return;

		Node playerNode = GetTree().GetFirstNodeInGroup("player");
		if (playerNode is Player p)
			_player = p;
	}

	private void LoadConfig()
	{
		FileAccess file = FileAccess.Open("res://data/scaling/zone_memory.json", FileAccess.ModeFlags.Read);
		if (file == null) return;

		string jsonText = file.GetAsText();
		file.Close();

		Json json = new();
		if (json.Parse(jsonText) != Error.Ok) return;

		Godot.Collections.Dictionary dict = json.Data.AsGodotDictionary();
		_cellSize = dict.ContainsKey("cell_size") ? (int)dict["cell_size"].AsDouble() : 128;
		_initialMemory = dict.ContainsKey("initial_memory") ? (float)dict["initial_memory"].AsDouble() : 0.5f;
		_decayPerMinute = dict.ContainsKey("decay_per_minute") ? (float)dict["decay_per_minute"].AsDouble() : 0.03f;
		_playerPresenceRate = dict.ContainsKey("player_presence_rate") ? (float)dict["player_presence_rate"].AsDouble() : 0.15f;
		_playerRadiusCells = dict.ContainsKey("player_radius_cells") ? (int)dict["player_radius_cells"].AsDouble() : 3;
		_foyerRadiusCells = dict.ContainsKey("foyer_radius_cells") ? (int)dict["foyer_radius_cells"].AsDouble() : 6;
		_foyerMemory = dict.ContainsKey("foyer_memory") ? (float)dict["foyer_memory"].AsDouble() : 1f;
		_loreStabilizeRadiusCells = dict.ContainsKey("lore_stabilize_radius_cells") ? (int)dict["lore_stabilize_radius_cells"].AsDouble() : 5;
		_loreStabilizeDurationMin = dict.ContainsKey("lore_stabilize_duration_min") ? (float)dict["lore_stabilize_duration_min"].AsDouble() : 20f;
		_fadedSpawnDensityMult = dict.ContainsKey("faded_spawn_density_mult") ? (float)dict["faded_spawn_density_mult"].AsDouble() : 1.8f;
		_fadedEnemySpeedBonus = dict.ContainsKey("faded_enemy_speed_bonus") ? (float)dict["faded_enemy_speed_bonus"].AsDouble() : 0.2f;
		_updateIntervalSec = dict.ContainsKey("update_interval_sec") ? (float)dict["update_interval_sec"].AsDouble() : 1f;

		GD.Print($"[ZoneMemoryManager] Config loaded — cell: {_cellSize}px, decay: {_decayPerMinute}/min, fadedMult: x{_fadedSpawnDensityMult}");
	}
}
