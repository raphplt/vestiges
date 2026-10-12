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

		GpuParticles2D next = weather == null ? null
			: AtmosphereParticles.Create(weather.Particles, weather.ParticleAmount, weather.ParticleColor, _wind, $"Weather{weather.Particles}");
		_particles = AtmosphereParticles.Swap(this, _particles, next, fadeSec);
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
}
