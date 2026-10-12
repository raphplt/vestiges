using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Ce que la météo montre en plus des couches d'atmosphère (plan 39 A5) : le brouillard qui referme la vue, à l'écran,
/// et les particules autour de la caméra (cendres, débris au vent, pluie). Enfant de BiomeAtmosphere, qui suit le joueur.
/// Les particules sont émises dans le monde : elles restent en place quand le joueur avance.
/// </summary>
public partial class WeatherVisuals : Node2D
{
	// Vue en px monde autour du joueur (caméra de run) : boîte d'émission un peu plus large.
	private static readonly Vector2 EmissionHalfExtents = new(560f, 340f);

	private static readonly StringName ViewWorldParameter = "view_world";
	private static readonly StringName ViewOriginParameter = "view_origin";
	private static readonly StringName CenterUvParameter = "center_uv";
	private static readonly StringName OpacityParameter = "opacity";

	private CanvasLayer _viewLayer;
	private ColorRect _view;
	private ShaderMaterial _viewMaterial;
	private GpuParticles2D _particles;
	// Un tween pour le voile, un par émetteur de particules : deux changements rapprochés ne s'annulent pas.
	private Tween _viewTween;
	private float _viewOpacity;
	private Vector2 _wind = Vector2.Right;

	public override void _Ready()
	{
		_viewMaterial = new ShaderMaterial { Shader = GD.Load<Shader>("res://assets/shaders/weather_view.gdshader") };
		_viewLayer = new CanvasLayer { Name = "WeatherViewLayer", Layer = 4, Visible = false };
		_view = new ColorRect
		{
			Name = "WeatherView",
			Material = _viewMaterial,
			MouseFilter = Control.MouseFilterEnum.Ignore,
		};
		// La propriété AnchorsPreset posée à la création laisse le rectangle à taille nulle : ancres et marges ensemble.
		_view.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		_viewLayer.AddChild(_view);
		AddChild(_viewLayer);
		SetProcess(false);
	}

	/// <summary>Sens du vent à l'écran (unitaire) : pluie inclinée et débris qui filent.</summary>
	public void SetWind(Vector2 direction) => _wind = direction.Normalized();

	/// <summary>Installe la météo en <paramref name="fadeSec"/> ; null la retire.</summary>
	public void Apply(WeatherData weather, float fadeSec)
	{
		float viewTarget = weather?.ViewOpacity ?? 0f;
		if (weather != null && weather.ViewOpacity > 0f)
		{
			_viewMaterial.SetShaderParameter("fog_color", weather.ViewColor);
			_viewMaterial.SetShaderParameter("clear_px", weather.ViewClearPx);
			_viewMaterial.SetShaderParameter("full_px", weather.ViewFullPx);
		}
		if (viewTarget > 0f || _viewOpacity > 0f)
		{
			_viewLayer.Visible = true;
			SetProcess(true);
			_viewTween?.Kill();
			_viewTween = CreateTween();
			_viewTween.TweenMethod(Callable.From<float>(SetViewOpacity), _viewOpacity, viewTarget, fadeSec);
		}

		if (_particles != null)
		{
			// Le tween appartient à l'émetteur : il meurt avec lui, et rien ne l'interrompt avant sa libération.
			GpuParticles2D old = _particles;
			_particles = null;
			Tween fadeOut = old.CreateTween();
			fadeOut.TweenProperty(old, "modulate:a", 0f, fadeSec);
			fadeOut.TweenCallback(Callable.From(old.QueueFree));
		}
		if (weather != null && weather.Particles != WeatherParticleKind.None)
		{
			_particles = CreateParticles(weather);
			_particles.Modulate = new Color(1f, 1f, 1f, 0f);
			AddChild(_particles);
			_particles.CreateTween().TweenProperty(_particles, "modulate:a", 1f, fadeSec);
		}
	}

	public override void _Process(double delta)
	{
		Viewport viewport = GetViewport();
		Transform2D canvas = viewport.GetCanvasTransform();
		Vector2 screen = viewport.GetVisibleRect().Size;
		Vector2 world = screen / canvas.Scale;
		Vector2 origin = -canvas.Origin / canvas.Scale;
		_viewMaterial.SetShaderParameter(ViewWorldParameter, world);
		_viewMaterial.SetShaderParameter(ViewOriginParameter, origin);
		_viewMaterial.SetShaderParameter(CenterUvParameter, (GlobalPosition - origin) / world);
		if (_viewOpacity <= 0f && (_viewTween == null || !_viewTween.IsRunning()))
		{
			_viewLayer.Visible = false;
			SetProcess(false);
		}
	}

	private void SetViewOpacity(float value)
	{
		_viewOpacity = value;
		_viewMaterial.SetShaderParameter(OpacityParameter, value);
	}

	private GpuParticles2D CreateParticles(WeatherData weather)
	{
		(Vector2I size, Vector3 direction, float spread, float speedMin, float speedMax, float lifetime, float gravity) = weather.Particles switch
		{
			// Cendres : flocons de 2 px qui tombent lentement et dérivent.
			WeatherParticleKind.Ash => (new Vector2I(2, 2), new Vector3(_wind.X * 0.4f, 1f, 0f), 35f, 12f, 28f, 9f, 2f),
			// Débris au vent : traits pâles qui filent dans le sens du vent.
			WeatherParticleKind.Wind => (new Vector2I(7, 1), new Vector3(_wind.X, _wind.Y * 0.5f, 0f), 12f, 160f, 260f, 3.5f, 0f),
			// Pluie : traits de 6 px, rapides, inclinés par le vent.
			_ => (new Vector2I(1, 6), new Vector3(_wind.X * 0.25f, 1f, 0f), 4f, 420f, 520f, 1.1f, 0f),
		};
		Image image = Image.CreateEmpty(size.X, size.Y, false, Image.Format.Rgba8);
		image.Fill(Colors.White);
		ParticleProcessMaterial material = new()
		{
			EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Box,
			EmissionBoxExtents = new Vector3(EmissionHalfExtents.X, EmissionHalfExtents.Y, 0f),
			Direction = direction,
			Spread = spread,
			InitialVelocityMin = speedMin,
			InitialVelocityMax = speedMax,
			Gravity = new Vector3(0f, gravity, 0f),
			Color = weather.ParticleColor,
			// Les traits suivent leur trajectoire (pluie inclinée, débris qui filent).
			ParticleFlagAlignY = weather.Particles == WeatherParticleKind.Rain,
		};
		Curve alpha = new();
		alpha.AddPoint(new Vector2(0f, 0f));
		alpha.AddPoint(new Vector2(0.1f, 1f));
		alpha.AddPoint(new Vector2(0.85f, 1f));
		alpha.AddPoint(new Vector2(1f, 0f));
		material.AlphaCurve = new CurveTexture { Curve = alpha };
		return new GpuParticles2D
		{
			Name = $"Weather{weather.Particles}",
			Amount = weather.ParticleAmount,
			Lifetime = lifetime,
			Preprocess = lifetime,
			Randomness = 1f,
			Texture = ImageTexture.CreateFromImage(image),
			ProcessMaterial = material,
			LocalCoords = false,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			// Devant les entités (0), derrière les plaques d'ennemis (20) et les chiffres de dégâts (30) qui restent lisibles.
			ZIndex = 15,
			ZAsRelative = false,
		};
	}
}
