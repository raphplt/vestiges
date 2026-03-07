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
	private CanvasLayer _swampLayer;
	private ColorRect _swampOverlay;
	private ShaderMaterial _swampMaterial;

	private string _currentBiomeId = "";
	private float _swampIntensityTarget;
	private float _swampIntensityCurrent;
	private const float SwampFadeSpeed = 0.8f;

	// Viewport interne = 480×270
	private const float ViewportWidth = 480f;
	private const float ViewportHeight = 270f;

	public override void _Ready()
	{
		_worldSetup = GetParent<WorldSetup>();
		CreateFogParticles();
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

		// Fade progressif du shader marécage
		UpdateSwampShader((float)delta);
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
			_fogParticles.Amount = Mathf.Max(8, (int)(30 * fogDensity));

			if (_fogParticles.ProcessMaterial is ParticleProcessMaterial mat)
			{
				mat.Color = new Color(fogColor.R, fogColor.G, fogColor.B, fogDensity * 0.5f);
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
			"quarry" => 0.3f,
			"urban_ruins" => 0.1f,
			_ => 0f
		};
	}

	private static Color GetFogColor(string biomeId)
	{
		return biomeId switch
		{
			"swamp" => new Color(0.48f, 0.54f, 0.60f, 1f),
			"forest_reclaimed" => new Color(0.30f, 0.40f, 0.28f, 1f),
			"quarry" => new Color(0.45f, 0.40f, 0.35f, 1f),
			"urban_ruins" => new Color(0.35f, 0.35f, 0.40f, 1f),
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

	private void UpdateSwampShader(float delta)
	{
		_swampIntensityCurrent = Mathf.MoveToward(_swampIntensityCurrent, _swampIntensityTarget, delta * SwampFadeSpeed);
		_swampMaterial.SetShaderParameter("intensity", _swampIntensityCurrent);
		_swampLayer.Visible = _swampIntensityCurrent > 0.01f;
	}
}
