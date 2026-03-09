using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Crée des effets atmosphériques par biome :
/// - Particules de brume flottante (marécage, forêt profonde)
/// - Shader post-process marécage (brume laiteuse, reflets spectraux, réalité fragile)
/// Se place comme enfant de Main et suit le joueur.
/// </summary>
public partial class BiomeAtmosphere : Node2D
{
	private Node2D _player;
	private WorldSetup _worldSetup;
	private GpuParticles2D _fogParticles;
	private GpuParticles2D _swampSporeParticles;
	private GpuParticles2D _swampGnatParticles;
	private GpuParticles2D _swampMistParticles;
	private GpuParticles2D _swampBubbleParticles;
	private CanvasLayer _swampLayer;
	private ColorRect _swampOverlay;
	private ShaderMaterial _swampMaterial;

	private string _currentBiomeId = "";
	private float _swampIntensityTarget;
	private float _swampIntensityCurrent;
	private float _swampMoistureCurrent;
	private const float SwampFadeSpeed = 0.8f;

	// Viewport interne = 480×270
	private const float ViewportWidth = 480f;
	private const float ViewportHeight = 270f;

	public override void _Ready()
	{
		_worldSetup = GetParent<WorldSetup>();
		CreateFogParticles();
		CreateSwampAmbientParticles();
		CreateSwampOverlay();
	}

	public override void _Process(double delta)
	{
		if (_player == null || !IsInstanceValid(_player))
		{
			Node playerNode = GetTree().GetFirstNodeInGroup("player");
			if (playerNode is Node2D p)
				_player = p;
			else
				return;
		}

		// Suivre le joueur
		GlobalPosition = _player.GlobalPosition;

		// Détecter le biome actuel
		BiomeData biome = _worldSetup.GetBiomeAt(_player.GlobalPosition);
		string biomeId = biome?.Id ?? "";

		if (biomeId != _currentBiomeId)
		{
			_currentBiomeId = biomeId;
			UpdateAtmosphere(biome);
		}

		float targetMoisture = biomeId == "swamp" ? GetLocalSwampMoisture() : 0f;
		_swampMoistureCurrent = Mathf.MoveToward(_swampMoistureCurrent, targetMoisture, (float)delta * 0.9f);
		UpdateSwampAmbientParticles(_swampMoistureCurrent, biomeId == "swamp");
		UpdateSwampShader((float)delta, _swampMoistureCurrent);
	}

	private void UpdateAtmosphere(BiomeData biome)
	{
		if (biome == null)
		{
			_fogParticles.Emitting = false;
			_swampIntensityTarget = 0f;
			return;
		}

		float fogDensity = GetFogDensity(biome.Id);
		Color fogColor = GetFogColor(biome.Id);

		if (fogDensity > 0f)
		{
			_fogParticles.Emitting = true;
			int fogAmount = biome.Id == "swamp"
				? Mathf.Max(10, (int)(18 * fogDensity))
				: Mathf.Max(8, (int)(30 * fogDensity));
			_fogParticles.Amount = fogAmount;

			if (_fogParticles.ProcessMaterial is ParticleProcessMaterial mat)
			{
				float alpha = biome.Id == "swamp" ? fogDensity * 0.32f : fogDensity * 0.5f;
				mat.Color = new Color(fogColor.R, fogColor.G, fogColor.B, alpha);
			}
		}
		else
		{
			_fogParticles.Emitting = false;
		}

		_swampIntensityTarget = biome.Id == "swamp" ? 1f : 0f;

		GD.Print($"[BiomeAtmosphere] Biome '{biome.Id}' → fog density={fogDensity:F2}, swamp={_swampIntensityTarget:F1}");
	}

	private static float GetFogDensity(string biomeId)
	{
		return biomeId switch
		{
			"swamp" => 0.9f,
			"forest_reclaimed" => 0.2f,
			"collapsed_quarry" => 0.3f,
			"urban_ruins" => 0.1f,
			"wild_fields" => 0.05f,
			_ => 0f
		};
	}

	private static Color GetFogColor(string biomeId)
	{
		return biomeId switch
		{
			"swamp" => new Color(0.48f, 0.54f, 0.60f, 1f),
			"forest_reclaimed" => new Color(0.30f, 0.40f, 0.28f, 1f),
			"collapsed_quarry" => new Color(0.45f, 0.40f, 0.35f, 1f),
			"urban_ruins" => new Color(0.35f, 0.35f, 0.40f, 1f),
			"wild_fields" => new Color(0.55f, 0.50f, 0.35f, 1f),
			_ => new Color(0.5f, 0.5f, 0.5f, 1f)
		};
	}

	private void CreateFogParticles()
	{
		_fogParticles = new GpuParticles2D
		{
			Name = "FogParticles",
			Amount = 20,
			Lifetime = 6f,
			Preprocess = 3f,
			Explosiveness = 0f,
			Randomness = 1f,
			Emitting = false,
			ZIndex = 50,
			ZAsRelative = false,
		};

		GradientTexture2D fogTex = new()
		{
			Width = 32,
			Height = 32,
			Fill = GradientTexture2D.FillEnum.Radial,
			FillFrom = new Vector2(0.5f, 0.5f),
			FillTo = new Vector2(0.5f, 0f),
			Gradient = new Gradient()
		};
		fogTex.Gradient.SetColor(0, new Color(1f, 1f, 1f, 0.4f));
		fogTex.Gradient.AddPoint(0.5f, new Color(1f, 1f, 1f, 0.15f));
		fogTex.Gradient.SetColor(fogTex.Gradient.GetPointCount() - 1, new Color(1f, 1f, 1f, 0f));

		_fogParticles.Texture = fogTex;

		ParticleProcessMaterial mat = new()
		{
			EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Box,
			EmissionBoxExtents = new Vector3(ViewportWidth * 0.6f, ViewportHeight * 0.6f, 0f),
			Direction = new Vector3(1f, -0.3f, 0f),
			Spread = 30f,
			InitialVelocityMin = 3f,
			InitialVelocityMax = 8f,
			ScaleMin = 2f,
			ScaleMax = 5f,
			Color = new Color(0.48f, 0.54f, 0.60f, 0.5f),
			Gravity = new Vector3(0f, 0f, 0f),
		};

		Curve alphaCurve = new();
		alphaCurve.AddPoint(new Vector2(0f, 0f));
		alphaCurve.AddPoint(new Vector2(0.2f, 1f));
		alphaCurve.AddPoint(new Vector2(0.8f, 1f));
		alphaCurve.AddPoint(new Vector2(1f, 0f));
		CurveTexture alphaCurveTexture = new() { Curve = alphaCurve };
		mat.AlphaCurve = alphaCurveTexture;

		_fogParticles.ProcessMaterial = mat;
		AddChild(_fogParticles);
	}

	private void CreateSwampAmbientParticles()
	{
		_swampSporeParticles = CreateAmbientParticles(
			"SwampSpores",
			12,
			7f,
			new Vector2(ViewportWidth * 0.42f, ViewportHeight * 0.26f),
			new Vector3(0.2f, -1f, 0f),
			40f,
			1.5f,
			4.2f,
			0.5f,
			1.4f,
			new Color(0.72f, 0.80f, 0.70f, 0.22f));
		AddChild(_swampSporeParticles);

		_swampGnatParticles = CreateAmbientParticles(
			"SwampGnats",
			8,
			3.6f,
			new Vector2(ViewportWidth * 0.28f, ViewportHeight * 0.18f),
			new Vector3(0.1f, -0.2f, 0f),
			140f,
			5f,
			13f,
			0.18f,
			0.42f,
			new Color(0.12f, 0.16f, 0.12f, 0.32f));
		AddChild(_swampGnatParticles);

		_swampMistParticles = CreateAmbientParticles(
			"SwampMist",
			6,
			8f,
			new Vector2(ViewportWidth * 0.44f, ViewportHeight * 0.12f),
			new Vector3(0.5f, -0.12f, 0f),
			20f,
			1f,
			3f,
			2.2f,
			4.6f,
			new Color(0.70f, 0.76f, 0.74f, 0.14f));
		_swampMistParticles.Position = new Vector2(0f, 34f);
		AddChild(_swampMistParticles);

		_swampBubbleParticles = CreateAmbientParticles(
			"SwampBubbles",
			4,
			2.2f,
			new Vector2(ViewportWidth * 0.30f, ViewportHeight * 0.08f),
			new Vector3(0f, -1f, 0f),
			12f,
			2.5f,
			5f,
			0.25f,
			0.55f,
			new Color(0.68f, 0.78f, 0.74f, 0.26f));
		_swampBubbleParticles.Position = new Vector2(0f, 40f);
		AddChild(_swampBubbleParticles);
	}

	private static GpuParticles2D CreateAmbientParticles(
		string name,
		int amount,
		float lifetime,
		Vector2 boxExtents,
		Vector3 direction,
		float spread,
		float velocityMin,
		float velocityMax,
		float scaleMin,
		float scaleMax,
		Color color)
	{
		GpuParticles2D particles = new()
		{
			Name = name,
			Amount = amount,
			Lifetime = lifetime,
			Preprocess = lifetime * 0.35f,
			Explosiveness = 0f,
			Randomness = 1f,
			Emitting = false,
			Visible = false,
			ZIndex = 48,
			ZAsRelative = false,
		};

		GradientTexture2D texture = new()
		{
			Width = 16,
			Height = 16,
			Fill = GradientTexture2D.FillEnum.Radial,
			FillFrom = new Vector2(0.5f, 0.5f),
			FillTo = new Vector2(0.5f, 0f),
			Gradient = new Gradient()
		};
		texture.Gradient.SetColor(0, new Color(1f, 1f, 1f, 0.6f));
		texture.Gradient.AddPoint(0.6f, new Color(1f, 1f, 1f, 0.18f));
		texture.Gradient.SetColor(texture.Gradient.GetPointCount() - 1, new Color(1f, 1f, 1f, 0f));
		particles.Texture = texture;

		ParticleProcessMaterial material = new()
		{
			EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Box,
			EmissionBoxExtents = new Vector3(boxExtents.X, boxExtents.Y, 0f),
			Direction = direction,
			Spread = spread,
			InitialVelocityMin = velocityMin,
			InitialVelocityMax = velocityMax,
			ScaleMin = scaleMin,
			ScaleMax = scaleMax,
			Color = color,
			Gravity = Vector3.Zero,
		};

		Curve alphaCurve = new();
		alphaCurve.AddPoint(new Vector2(0f, 0f));
		alphaCurve.AddPoint(new Vector2(0.15f, 0.8f));
		alphaCurve.AddPoint(new Vector2(0.8f, 0.8f));
		alphaCurve.AddPoint(new Vector2(1f, 0f));
		material.AlphaCurve = new CurveTexture { Curve = alphaCurve };

		particles.ProcessMaterial = material;
		return particles;
	}

	private void CreateSwampOverlay()
	{
		Shader swampShader = GD.Load<Shader>("res://assets/shaders/swamp_atmosphere.gdshader");
		_swampMaterial = new ShaderMaterial { Shader = swampShader };
		_swampMaterial.SetShaderParameter("intensity", 0f);
		_swampMaterial.SetShaderParameter("pixel_size", new Vector2(ViewportWidth, ViewportHeight));

		// CanvasLayer pour rester en screen-space indépendamment de la caméra
		_swampLayer = new CanvasLayer
		{
			Name = "SwampLayer",
			Layer = 5,
			FollowViewportEnabled = false,
		};

		_swampOverlay = new ColorRect
		{
			Name = "SwampOverlay",
			AnchorsPreset = (int)Control.LayoutPreset.FullRect,
			Material = _swampMaterial,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};

		_swampLayer.AddChild(_swampOverlay);
		AddChild(_swampLayer);
	}

	private float GetLocalSwampMoisture()
	{
		if (_player == null || _worldSetup == null)
			return 0f;

		Vector2[] sampleOffsets =
		{
			Vector2.Zero,
			new Vector2(48f, 0f),
			new Vector2(-48f, 0f),
			new Vector2(0f, 32f),
			new Vector2(0f, -32f),
			new Vector2(36f, 18f),
			new Vector2(-36f, 18f),
			new Vector2(36f, -18f),
			new Vector2(-36f, -18f)
		};

		float moisture = 0f;
		foreach (Vector2 offset in sampleOffsets)
		{
			if (_worldSetup.IsWaterAt(_player.GlobalPosition + offset))
				moisture += 1f;
		}

		return moisture / sampleOffsets.Length;
	}

	private void UpdateSwampAmbientParticles(float moisture, bool active)
	{
		UpdateParticleSystem(_swampSporeParticles, active, 6, 14, moisture, 0.08f, 0.25f);
		UpdateParticleSystem(_swampGnatParticles, active, 4, 10, moisture, 0.12f, 0.34f);
		UpdateParticleSystem(_swampMistParticles, active, 3, 8, moisture, 0.06f, 0.18f);
		UpdateParticleSystem(_swampBubbleParticles, active, 0, 5, moisture * 0.8f, 0.04f, 0.24f);
	}

	private static void UpdateParticleSystem(
		GpuParticles2D particles,
		bool active,
		int minAmount,
		int maxAmount,
		float moisture,
		float minAlpha,
		float maxAlpha)
	{
		if (particles == null)
			return;

		if (!active)
		{
			particles.Emitting = false;
			particles.Visible = false;
			return;
		}

		particles.Visible = true;
		particles.Emitting = true;
		particles.Amount = Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(minAmount, maxAmount, moisture)));

		if (particles.ProcessMaterial is ParticleProcessMaterial material)
		{
			Color color = material.Color;
			color.A = Mathf.Lerp(minAlpha, maxAlpha, moisture);
			material.Color = color;
		}
	}

	private void UpdateSwampShader(float delta, float localMoisture)
	{
		_swampIntensityCurrent = Mathf.MoveToward(_swampIntensityCurrent, _swampIntensityTarget, delta * SwampFadeSpeed);
		_swampMaterial.SetShaderParameter("intensity", _swampIntensityCurrent);
		_swampMaterial.SetShaderParameter("local_moisture", localMoisture);
		_swampLayer.Visible = _swampIntensityCurrent > 0.01f;
	}
}
