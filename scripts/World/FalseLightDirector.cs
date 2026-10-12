using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Fausses lumières (plan 39 B4a) : une lampe sur quatre des décors qui en portent une (feux tricolores, lampadaires,
/// lampes de carrière, lanternes du marais) brille encore. Lueurs additives sans script (cœur, halo, flaque au sol),
/// animées par ce seul gestionnaire et seulement près de la caméra, grâce à une grille. Discrètes en plein jour, elles
/// ressortent quand le ciel s'assombrit (météo, nuit de l'Indicible). Dans une zone qui s'oublie, la lampe vacille de
/// plus en plus, puis meurt pour de bon. Réglages : <c>data/world/false_lights.json</c>.
/// </summary>
public partial class FalseLightDirector : Node2D
{
	private const float GridCellPx = 256f;
	// Marge autour de la vue : une lueur de bord d'écran s'anime avant d'entrer.
	private const float ViewMarginPx = 96f;
	private const float MemoryCheckIntervalS = 0.5f;
	// La teinte la plus sombre que la scène puisse prendre ne divise pas les lueurs au-delà (nuit de l'Indicible).
	private const float MinSceneLuminance = 0.25f;
	// Pas d'écriture vers le moteur pour un changement de niveau plus petit que ce pas.
	private const float LevelStep = 1f / 48f;
	private const int CoreZIndex = 1;

	private sealed class Light
	{
		public FalseLightKind Kind;
		public Vector2 Position;
		public Sprite2D Core;
		public Sprite2D Halo;
		public Sprite2D Pool;
		// Fenêtre (B4b) : l'immeuble qui la porte, dont la transparence (joueur derrière) éteint d'autant la lueur.
		public EnvironmentProp Building;
		public float Phase;
		public uint Seed;
		public float Failing;
		public float MemoryTimer;
		public bool Dead;
		public float ShownLevel = -1f;
		public int UpdatedFrame = -2;
	}

	private FalseLightsConfig _config;
	private ErasureManager _erasure;
	private CanvasModulate _scene;
	private float _sceneDayLuminance = 1f;
	private EventBus _eventBus;
	private readonly List<Light> _lights = new();
	private readonly Dictionary<Vector2I, List<Light>> _grid = new();
	private float _darkness;
	private float _weatherDarkness;
	private float _time;
	private int _frame;
	private int _diedCount;

	/// <summary>Lampes allumées et lampes mortes avec l'oubli : lus par les captures et les journaux.</summary>
	public int LitCount => _lights.Count - _diedCount;
	public int DiedCount => _diedCount;
	public int WindowCount { get; private set; }
	public float Darkness => _darkness;

	/// <summary>Lampes encore vivantes : décor porteur et pied du décor, pour les captures.</summary>
	public List<(string Sprite, Vector2 Position)> LivingLights()
	{
		List<(string, Vector2)> living = new();
		foreach (Light light in _lights)
		{
			if (!light.Dead)
				living.Add((light.Kind.Sprite, light.Building?.GlobalPosition ?? light.Position));
		}
		return living;
	}

	/// <summary>Repère les lampes parmi les décors posés (déjà rangés en tronçons) et allume une part de chaque sorte.</summary>
	public void Setup(Node propContainer, ErasureManager erasure, CanvasModulate scene)
	{
		if (!FalseLightsConfig.TryLoad(out _config, out string error))
		{
			GD.PushError($"[FalseLightDirector] {error} : aucune lampe allumée");
			return;
		}
		_erasure = erasure;
		_scene = scene;
		if (_scene != null)
			_sceneDayLuminance = Mathf.Max(MinSceneLuminance, _scene.Color.Luminance);

		Dictionary<string, FalseLightKind> byStem = new();
		foreach (FalseLightKind kind in _config.Kinds)
			byStem[kind.Sprite] = kind;
		Dictionary<FalseLightKind, int> seen = new();
		Dictionary<FalseLightKind, int> lit = new();
		Texture2D core = GD.Load<Texture2D>("res://assets/vfx/lights/light_core.png");
		Texture2D halo = GD.Load<Texture2D>("res://assets/vfx/lights/light_halo.png");
		Texture2D pool = GD.Load<Texture2D>("res://assets/vfx/lights/light_pool.png");
		Texture2D pixel = GD.Load<Texture2D>("res://assets/vfx/lights/light_pixel.png");
		Dictionary<string, Rect2I[]> windows = LoadWindows(_config.WindowsPath);
		int buildings = 0;
		int screens = 0;
		CanvasItemMaterial additive = new() { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
		RandomNumberGenerator rng = RunRandom.Create("false_lights");

		// Les décors sont rangés en tronçons (PropChunks) : un niveau de plus sous le conteneur.
		foreach (Node chunk in propContainer.GetChildren())
		foreach (Node child in chunk.GetChildren())
		{
			if (child is not EnvironmentProp prop || prop.BaseTexture == null)
				continue;
			string stem = prop.BaseTexture.ResourcePath.GetFile().GetBaseName();
			if (windows.TryGetValue(stem, out Rect2I[] panes) && panes.Length > 0)
			{
				bool lights = Mathf.FloorToInt((buildings + 1) * _config.BuildingShare) > Mathf.FloorToInt(buildings * _config.BuildingShare);
				buildings++;
				if (lights)
				{
					bool screen = rng.Randf() < _config.ScreenShare;
					screens += screen ? 1 : 0;
					AddWindow(prop, panes[rng.RandiRange(0, panes.Length - 1)], screen ? _config.ScreenKind : _config.WindowKind,
						pixel, halo, additive, rng);
				}
				continue;
			}
			if (!byStem.TryGetValue(stem, out FalseLightKind kind))
				continue;
			int index = seen.GetValueOrDefault(kind);
			seen[kind] = index + 1;
			// Une part de chaque sorte, à intervalles réguliers dans l'ordre de pose.
			if (Mathf.FloorToInt((index + 1) * _config.LitShare) <= Mathf.FloorToInt(index * _config.LitShare))
				continue;
			lit[kind] = lit.GetValueOrDefault(kind) + 1;
			Light light = new()
			{
				Kind = kind,
				Position = prop.GlobalPosition,
				Phase = rng.Randf(),
				Seed = rng.Randi(),
				MemoryTimer = rng.Randf() * MemoryCheckIntervalS,
			};
			Vector2 bulb = prop.TextureOrigin + kind.Anchor;
			light.Core = AddGlow(core, bulb, CoreZIndex, additive);
			if (kind.Halo > 0f)
				light.Halo = AddGlow(halo, bulb, CoreZIndex, additive);
			if (kind.Pool > 0f)
				light.Pool = AddGlow(pool, new Vector2(bulb.X, light.Position.Y), -1, additive);
			Register(light);
		}
		foreach (FalseLightKind kind in _config.Kinds)
			GD.Print($"[FalseLightDirector] {kind.Sprite} : {lit.GetValueOrDefault(kind)}/{seen.GetValueOrDefault(kind)} allumées");
		GD.Print($"[FalseLightDirector] fenêtres : {WindowCount} immeubles éclairés sur {buildings} à vitres, dont {screens} télévisions");
	}

	private void Register(Light light)
	{
		_lights.Add(light);
		Vector2I cell = CellOf(light.Position);
		if (!_grid.TryGetValue(cell, out List<Light> bucket))
			_grid[cell] = bucket = new List<Light>();
		bucket.Add(light);
	}

	/// <summary>
	/// Fenêtre éclairée (B4b) : un aplat additif sur la vitre et un halo, enfants de l'immeuble. Ils se trient avec lui :
	/// un passant devant la façade la cache, comme il cache la fenêtre.
	/// </summary>
	private void AddWindow(EnvironmentProp building, Rect2I pane, FalseLightKind kind, Texture2D pixel, Texture2D halo,
		Material additive, RandomNumberGenerator rng)
	{
		Vector2 origin = building.TextureOrigin - building.GlobalPosition;
		Sprite2D fill = new()
		{
			Texture = pixel, Centered = false, Position = origin + pane.Position, Scale = pane.Size, Material = additive,
			TextureFilter = TextureFilterEnum.Nearest, Modulate = Colors.Black,
		};
		building.AddChild(fill);
		Vector2 center = origin + pane.Position + (Vector2)pane.Size * 0.5f;
		Sprite2D glow = new()
		{
			Texture = halo, Centered = false, Position = (center - halo.GetSize() * 0.5f).Round(), Material = additive,
			TextureFilter = TextureFilterEnum.Nearest, Modulate = Colors.Black,
		};
		building.AddChild(glow);
		Register(new Light
		{
			Kind = kind,
			Position = building.GlobalPosition + center,
			Core = fill,
			Halo = glow,
			Building = building,
			Phase = rng.Randf(),
			Seed = rng.Randi(),
			MemoryTimer = rng.Randf() * MemoryCheckIntervalS,
		});
		WindowCount++;
	}

	/// <summary>Vitres de chaque immeuble, en px du sprite (tools/extract_building_windows.py).</summary>
	private static Dictionary<string, Rect2I[]> LoadWindows(string path)
	{
		Dictionary<string, Rect2I[]> windows = new();
		using FileAccess file = FileAccess.Open("res://" + path, FileAccess.ModeFlags.Read);
		if (file == null)
			return windows;
		// Fichier généré, mais un fichier abîmé ne doit coûter que les fenêtres, pas la run.
		try
		{
			using JsonDocument document = JsonDocument.Parse(file.GetAsText());
			foreach (JsonProperty building in document.RootElement.EnumerateObject())
			{
				Rect2I[] panes = new Rect2I[building.Value.GetArrayLength()];
				int i = 0;
				foreach (JsonElement rect in building.Value.EnumerateArray())
					panes[i++] = new Rect2I(rect[0].GetInt32(), rect[1].GetInt32(), rect[2].GetInt32(), rect[3].GetInt32());
				windows[building.Name] = panes;
			}
		}
		catch (Exception exception) when (exception is JsonException or InvalidOperationException or IndexOutOfRangeException or FormatException)
		{
			GD.PushError($"[FalseLightDirector] {path} illisible ({exception.Message}) : aucune fenêtre éclairée");
			windows.Clear();
		}
		return windows;
	}

	private Sprite2D AddGlow(Texture2D texture, Vector2 position, int zIndex, Material material)
	{
		// Coin haut-gauche arrondi au pixel : une lueur de taille impaire centrée sur un point tomberait entre deux pixels.
		Sprite2D sprite = new()
		{
			Texture = texture, Centered = false, Position = (position - texture.GetSize() * 0.5f).Round(),
			Material = material, ZIndex = zIndex, ZAsRelative = false,
			TextureFilter = TextureFilterEnum.Nearest, Modulate = Colors.Black,
		};
		AddChild(sprite);
		return sprite;
	}

	public override void _Ready()
	{
		if (_config == null || _lights.Count == 0)
		{
			SetProcess(false);
			return;
		}
		_eventBus = GetNode<EventBus>("/root/EventBus");
		_eventBus.WeatherChanged += OnWeatherChanged;
		// Une météo déjà installée (mode dev, captures) compte dès le départ.
		WeatherDirector weather = GetParent().GetNodeOrNull<WeatherDirector>("WeatherDirector");
		if (weather != null)
			OnWeatherChanged(weather.ActiveId);
	}

	public override void _ExitTree()
	{
		if (_eventBus != null)
			_eventBus.WeatherChanged -= OnWeatherChanged;
	}

	private void OnWeatherChanged(string weatherId) =>
		_weatherDarkness = _config.WeatherDarkness.GetValueOrDefault(weatherId);

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		_time += dt;
		_frame++;
		// Nuit de l'Indicible : la teinte de la scène s'assombrit. Les lueurs additives y sont multipliées aussi, d'où
		// la compensation, sans quoi elles s'éteindraient avec le reste au lieu de ressortir.
		float luminance = _scene == null ? _sceneDayLuminance : Mathf.Max(MinSceneLuminance, _scene.Color.Luminance);
		float sceneDarkness = Mathf.Clamp(1f - luminance / _sceneDayLuminance, 0f, 1f) / (1f - MinSceneLuminance / _sceneDayLuminance);
		float targetDarkness = Mathf.Max(_weatherDarkness, Mathf.Min(1f, sceneDarkness));
		_darkness = Mathf.MoveToward(_darkness, targetDarkness, dt / _config.DarknessFadeS);
		float strength = Mathf.Lerp(_config.DayStrength, _config.DarkStrength, _darkness) * _sceneDayLuminance / luminance;

		Camera2D camera = GetViewport().GetCamera2D();
		if (camera == null)
			return;
		Vector2 half = GetViewport().GetVisibleRect().Size * 0.5f / camera.Zoom + new Vector2(ViewMarginPx, ViewMarginPx);
		Vector2 center = camera.GetScreenCenterPosition();
		Vector2I from = CellOf(center - half);
		Vector2I to = CellOf(center + half);
		for (int x = from.X; x <= to.X; x++)
		{
			for (int y = from.Y; y <= to.Y; y++)
			{
				if (!_grid.TryGetValue(new Vector2I(x, y), out List<Light> bucket))
					continue;
				foreach (Light light in bucket)
				{
					if (!light.Dead)
						UpdateLight(light, strength, dt);
				}
			}
		}
	}

	private void UpdateLight(Light light, float strength, float dt)
	{
		// Revenue dans la vue : sa zone a pu s'oublier entre-temps, la mémoire se relit tout de suite.
		if (light.UpdatedFrame < _frame - 1)
			light.MemoryTimer = 0f;
		light.UpdatedFrame = _frame;
		light.MemoryTimer -= dt;
		if (light.MemoryTimer <= 0f)
		{
			light.MemoryTimer = MemoryCheckIntervalS;
			if (_erasure != null)
			{
				float memory = _erasure.GetMemoryAt(light.Position);
				if (memory <= _config.MemoryDead || _erasure.GetZonePhaseAt(light.Position) == ErasureManager.ErasureZonePhase.Void)
				{
					Kill(light);
					return;
				}
				light.Failing = Mathf.Clamp((_config.MemorySteady - memory) / (_config.MemorySteady - _config.MemoryDead), 0f, 1f);
			}
		}

		FalseLightKind kind = light.Kind;
		float cycle = _time / kind.PeriodS + light.Phase;
		float level = kind.Pattern switch
		{
			FalseLightPattern.Blink => Mathf.PosMod(cycle, 1f) < kind.OnShare ? 1f : _config.OffLevel,
			FalseLightPattern.Buzz => Buzz(Noise(light.Seed, Mathf.FloorToInt(cycle))),
			FalseLightPattern.Breathe => Mathf.Lerp(_config.BreatheFloor, 1f, 0.5f + 0.5f * Mathf.Sin(Mathf.Tau * cycle)),
			// Fenêtre qui tremble à peine, ou télévision : une valeur tirée par période, au-dessus de son creux.
			_ => Mathf.Lerp(kind.Floor, 1f, Noise(light.Seed, Mathf.FloorToInt(cycle))),
		};
		// Oubli : la lampe se coupe d'autant plus souvent que sa zone s'efface, par à-coups d'un dixième de seconde.
		if (light.Failing > 0f
			&& Noise(light.Seed ^ 0x5A17u, Mathf.FloorToInt(_time / _config.FailStepS)) < light.Failing * _config.FailCutMax)
			level = _config.OffLevel;
		level *= strength;
		// Joueur derrière l'immeuble : la façade s'efface (PropOcclusion), sa fenêtre aussi.
		if (light.Building != null)
			level *= light.Building.BaseAlpha;
		if (Mathf.Abs(level - light.ShownLevel) < LevelStep)
			return;
		light.ShownLevel = level;
		Color color = kind.Color * level;
		color.A = 1f;
		light.Core.Modulate = color;
		if (light.Halo != null)
			light.Halo.Modulate = Scaled(kind.Color, level * kind.Halo);
		if (light.Pool != null)
			light.Pool.Modulate = Scaled(kind.Color, level * kind.Pool);
	}

	private void Kill(Light light)
	{
		light.Dead = true;
		_diedCount++;
		light.Core.Visible = false;
		if (light.Halo != null)
			light.Halo.Visible = false;
		if (light.Pool != null)
			light.Pool.Visible = false;
	}

	/// <summary>Grésillement : un tirage par période, le plus souvent presque plein, parfois une chute.</summary>
	private float Buzz(float draw) => draw < _config.BuzzDropChance
		? _config.BuzzDropLevel
		: Mathf.Lerp(_config.BuzzFloor, 1f, draw);

	private static Color Scaled(Color color, float level) => new(color.R * level, color.G * level, color.B * level, 1f);

	private static Vector2I CellOf(Vector2 position) =>
		new(Mathf.FloorToInt(position.X / GridCellPx), Mathf.FloorToInt(position.Y / GridCellPx));

	/// <summary>Valeur pseudo-aléatoire de [0 ; 1[ stable pour une lampe et un pas de temps.</summary>
	private static float Noise(uint seed, int step)
	{
		uint h = seed ^ (uint)step * 0x9E3779B1u;
		h ^= h >> 15;
		h *= 0x85EBCA77u;
		h ^= h >> 13;
		return (h & 0xFFFFFFu) / 16777216f;
	}
}
