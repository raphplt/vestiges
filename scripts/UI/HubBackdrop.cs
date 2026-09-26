using Godot;

namespace Vestiges.UI;

/// <summary>
/// Fond vivant de l'accueil : la peinture du camp (480×270 affichée ×4), la lumière du feu qui vacille,
/// les braises, l'Effacement qui arrache des pixels sur le bord droit et des yeux qui s'ouvrent dans le noir.
/// Les positions sont exprimées dans l'espace 1920×1080 de la peinture.
/// </summary>
public partial class HubBackdrop : Control
{
	public static readonly Vector2 FirePosition = new(964f, 876f);
	public const int PixelScale = 4;

	private static readonly Color FireCore = new(1f, 0.72f, 0.36f);
	private static readonly Color FireOuter = new(0.88f, 0.48f, 0.22f);
	private static readonly Color Ember = new(0.98f, 0.78f, 0.36f);
	private static readonly Color EmberDeep = new(0.88f, 0.48f, 0.22f);
	private static readonly Color Erasure = new(0.961f, 0.941f, 0.922f);
	private static readonly Color CreatureEyes = new(0.5f, 1f, 0f);
	private static readonly Color Night = new(0.04f, 0.04f, 0.08f);

	// Coins sombres de la peinture où des yeux peuvent s'ouvrir (lisière de forêt, ruelles, marais).
	private static readonly Vector2[] WatcherSpots =
	{
		new(292f, 612f), new(456f, 468f), new(612f, 704f), new(1236f, 496f),
		new(1408f, 752f), new(1560f, 660f), new(172f, 820f), new(1320f, 612f)
	};

	private readonly RandomNumberGenerator _rng = new();
	private Control _scene;
	private TextureRect _fireGlow;
	private float _flickerTime;
	private Timer _watcherTimer;

	/// <summary>Calque de la peinture où placer les éléments diégétiques (personnages du camp).</summary>
	public Control Scene => _scene;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		_rng.Randomize();

		ColorRect night = new() { Color = Night, MouseFilter = MouseFilterEnum.Ignore };
		night.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		AddChild(night);

		_scene = new Control { MouseFilter = MouseFilterEnum.Ignore, Size = new Vector2(1920f, 1080f) };
		AddChild(_scene);

		Texture2D painting = UITheme.LoadTex("res://assets/bg_menu_1920x1080.png");
		if (painting != null)
			_scene.AddChild(new TextureRect { Texture = painting, MouseFilter = MouseFilterEnum.Ignore, Size = new Vector2(1920f, 1080f) });

		_fireGlow = CreateFireGlow();
		_scene.AddChild(_fireGlow);
		_scene.AddChild(CreateEmbers());
		_scene.AddChild(CreateErasureDrift());

		AddChild(CreateShade(new Vector2(0f, 0f), new Vector2(0.46f, 0f), 0.86f));
		AddChild(CreateShade(new Vector2(0f, 1f), new Vector2(0f, 0.8f), 0.8f));

		_watcherTimer = new Timer { OneShot = true };
		_watcherTimer.Timeout += OpenWatcherEyes;
		AddChild(_watcherTimer);
		_watcherTimer.Start(_rng.RandfRange(1.5f, 3f));

		Resized += CenterScene;
		CenterScene();
	}

	public override void _Process(double delta)
	{
		// Somme de sinus incommensurables : un vacillement sans motif répété, sans bruit à allouer.
		_flickerTime += (float)delta;
		float flicker = 0.78f
			+ 0.1f * Mathf.Sin(_flickerTime * 7.3f)
			+ 0.07f * Mathf.Sin(_flickerTime * 12.9f + 1.7f)
			+ 0.05f * Mathf.Sin(_flickerTime * 23.1f + 0.4f);
		_fireGlow.Modulate = new Color(1f, 1f, 1f, flicker);
	}

	private void CenterScene()
	{
		float scale = Mathf.Max(Size.X / 1920f, Size.Y / 1080f);
		_scene.Scale = new Vector2(scale, scale);
		_scene.Position = (Size - new Vector2(1920f, 1080f) * scale) * 0.5f;
	}

	/// <summary>Halo du feu en anneaux francs de 4 px, fondu additif : la lumière reste au grain de la peinture.</summary>
	private static TextureRect CreateFireGlow()
	{
		Gradient gradient = new()
		{
			InterpolationMode = Gradient.InterpolationModeEnum.Constant,
			Offsets = new[] { 0f, 0.22f, 0.45f, 0.7f, 1f },
			Colors = new[]
			{
				new Color(FireCore, 0.34f), new Color(FireCore, 0.22f), new Color(FireOuter, 0.13f),
				new Color(FireOuter, 0.06f), new Color(FireOuter, 0f)
			}
		};
		GradientTexture2D texture = new()
		{
			Gradient = gradient,
			Fill = GradientTexture2D.FillEnum.Radial,
			FillFrom = new Vector2(0.5f, 0.5f),
			FillTo = new Vector2(0.5f, 0f),
			Width = 110,
			Height = 60
		};
		Vector2 size = new Vector2(texture.Width, texture.Height) * PixelScale;
		return new TextureRect
		{
			Texture = texture,
			Size = size,
			Position = FirePosition - size * 0.5f,
			MouseFilter = MouseFilterEnum.Ignore,
			Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add },
			TextureFilter = TextureFilterEnum.Nearest
		};
	}

	private static Node2D CreateEmbers()
	{
		CpuParticles2D embers = new()
		{
			Position = FirePosition + new Vector2(0f, -8f),
			Amount = 36,
			Lifetime = 3.6,
			Preprocess = 4.0,
			EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle,
			EmissionRectExtents = new Vector2(34f, 6f),
			Direction = Vector2.Up,
			Spread = 22f,
			Gravity = new Vector2(6f, -14f),
			InitialVelocityMin = 28f,
			InitialVelocityMax = 74f,
			ScaleAmountMin = PixelScale,
			ScaleAmountMax = PixelScale,
			ColorRamp = FadingRamp(Ember, EmberDeep),
			TextureFilter = TextureFilterEnum.Nearest
		};
		embers.Texture = PixelTexture();
		return embers;
	}

	private static Node2D CreateErasureDrift()
	{
		CpuParticles2D drift = new()
		{
			Position = new Vector2(1900f, 560f),
			Amount = 70,
			Lifetime = 7.0,
			Preprocess = 8.0,
			EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle,
			EmissionRectExtents = new Vector2(70f, 560f),
			Direction = new Vector2(-1f, -0.25f),
			Spread = 18f,
			Gravity = Vector2.Zero,
			InitialVelocityMin = 12f,
			InitialVelocityMax = 46f,
			ScaleAmountMin = PixelScale,
			ScaleAmountMax = PixelScale * 2,
			ColorRamp = FadingRamp(Erasure, new Color(0.82f, 0.8f, 0.86f)),
			TextureFilter = TextureFilterEnum.Nearest
		};
		drift.Texture = PixelTexture();
		return drift;
	}

	private static Gradient FadingRamp(Color start, Color end)
	{
		return new Gradient
		{
			Offsets = new[] { 0f, 0.12f, 0.7f, 1f },
			Colors = new[] { new Color(start, 0f), start, new Color(end, 0.7f), new Color(end, 0f) }
		};
	}

	private static ImageTexture PixelTexture()
	{
		Image pixel = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8);
		pixel.Fill(Colors.White);
		return ImageTexture.CreateFromImage(pixel);
	}

	/// <summary>Assombrissement en dégradé vers un bord de l'écran, pour lire le menu et la plaque du personnage.</summary>
	private static TextureRect CreateShade(Vector2 from, Vector2 to, float strength)
	{
		TextureRect shade = new()
		{
			Texture = new GradientTexture2D
			{
				Gradient = new Gradient
				{
					Offsets = new[] { 0f, 1f },
					Colors = new[] { new Color(Night, strength), new Color(Night, 0f) }
				},
				FillFrom = from,
				FillTo = to,
				Width = 64,
				Height = 64
			},
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.Scale,
			MouseFilter = MouseFilterEnum.Ignore,
			TextureFilter = TextureFilterEnum.Linear
		};
		shade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		return shade;
	}

	/// <summary>Une paire d'yeux s'ouvre dans un recoin sombre, cligne, puis se referme.</summary>
	private void OpenWatcherEyes()
	{
		Vector2 spot = WatcherSpots[_rng.RandiRange(0, WatcherSpots.Length - 1)];
		Node2D eyes = new() { Position = spot.Snapped(new Vector2(PixelScale, PixelScale)), Modulate = new Color(1f, 1f, 1f, 0f) };
		float gap = PixelScale * _rng.RandiRange(2, 3);
		eyes.AddChild(new ColorRect { Color = CreatureEyes, Size = new Vector2(PixelScale, PixelScale), MouseFilter = MouseFilterEnum.Ignore });
		eyes.AddChild(new ColorRect { Color = CreatureEyes, Size = new Vector2(PixelScale, PixelScale), Position = new Vector2(gap, 0f), MouseFilter = MouseFilterEnum.Ignore });
		_scene.AddChild(eyes);

		float stare = _rng.RandfRange(1.4f, 3.2f);
		Tween tween = eyes.CreateTween();
		tween.TweenProperty(eyes, "modulate:a", 0.9f, 0.5f);
		tween.TweenInterval(stare * 0.5f);
		tween.TweenProperty(eyes, "scale:y", 0f, 0.06f);
		tween.TweenProperty(eyes, "scale:y", 1f, 0.06f);
		tween.TweenInterval(stare * 0.5f);
		tween.TweenProperty(eyes, "modulate:a", 0f, 0.35f);
		tween.TweenCallback(Callable.From(eyes.QueueFree));

		_watcherTimer.Start(_rng.RandfRange(2.5f, 6f));
	}
}
