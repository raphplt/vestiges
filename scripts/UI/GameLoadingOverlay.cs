using System;
using Godot;

namespace Vestiges.UI;

/// <summary>
/// Overlay de chargement affiché pendant l'initialisation async du monde.
/// Démarre noir (seamless avec VoidTransition), affiche des particules
/// dorées flottantes et des fragments de lore, puis fade-out vers le gameplay.
/// </summary>
public partial class GameLoadingOverlay : CanvasLayer
{
	private static readonly Color VoidBlack = new(0.02f, 0.02f, 0.05f);
	private static readonly Color GoldFoyer = new(0.83f, 0.66f, 0.26f);
	private static readonly Color GoldDim = new(0.63f, 0.47f, 0.16f);
	private static readonly Color VioletBrume = new(0.29f, 0.19f, 0.4f);
	private static readonly Color CyanEssence = new(0.37f, 0.77f, 0.77f);

	private static readonly string[] LoreFragments = new[]
	{
		"Le monde oublie ce qu'il était...",
		"Les Constellations veillent encore...",
		"Le Foyer résiste à l'Effacement...",
		"Des vestiges murmurent dans l'ombre...",
		"La mémoire est la dernière forteresse...",
		"Le Vide grignote les bords du réel...",
		"Quelque chose persiste, malgré tout...",
		"Les étoiles se souviennent de vos noms...",
	};

	private Control _root;
	private ColorRect _background;
	private Label _loreLabel;
	private Label _progressLabel;
	private Control _particleLayer;
	private RandomNumberGenerator _rng = new();
	private bool _isVisible = true;
	private float _loreTimer;
	private int _loreIndex;
	private float _particleTimer;

	public override void _Ready()
	{
		Layer = 100;
		ProcessMode = ProcessModeEnum.Always;

		_rng.Seed = (ulong)Time.GetTicksMsec();
		_loreIndex = (int)(_rng.Randi() % (uint)LoreFragments.Length);

		_root = new Control();
		_root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		_root.MouseFilter = Control.MouseFilterEnum.Stop;
		AddChild(_root);

		// Fond noir du vide
		_background = new ColorRect();
		_background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		_background.Color = VoidBlack;
		_background.MouseFilter = Control.MouseFilterEnum.Ignore;
		_root.AddChild(_background);

		// Couche particules
		_particleLayer = new Control();
		_particleLayer.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		_particleLayer.MouseFilter = Control.MouseFilterEnum.Ignore;
		_root.AddChild(_particleLayer);

		// Texte de lore (centre bas)
		_loreLabel = new Label
		{
			Text = LoreFragments[_loreIndex],
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Modulate = new Color(1f, 1f, 1f, 0f),
		};
		_loreLabel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		_loreLabel.OffsetTop = 200f;
		_loreLabel.AddThemeFontSizeOverride("font_size", 22);
		_loreLabel.AddThemeColorOverride("font_color", GoldFoyer);
		_root.AddChild(_loreLabel);

		// Indicateur de progression (bas)
		_progressLabel = new Label
		{
			Text = "...",
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Bottom,
			Modulate = new Color(1f, 1f, 1f, 0.5f),
		};
		_progressLabel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		_progressLabel.OffsetBottom = -40f;
		_progressLabel.AddThemeFontSizeOverride("font_size", 14);
		_progressLabel.AddThemeColorOverride("font_color", GoldDim);
		_root.AddChild(_progressLabel);

		// Fade-in initial du texte de lore
		FadeInLoreText();
		SpawnAmbientParticles(12);
	}

	public override void _Process(double delta)
	{
		if (!_isVisible) return;

		_loreTimer += (float)delta;
		_particleTimer += (float)delta;

		// Changer le texte de lore toutes les 3s
		if (_loreTimer > 3f)
		{
			_loreTimer = 0f;
			_loreIndex = (_loreIndex + 1) % LoreFragments.Length;
			TransitionLoreText(LoreFragments[_loreIndex]);
		}

		// Spawner des particules ambiantes régulièrement
		if (_particleTimer > 0.4f)
		{
			_particleTimer = 0f;
			SpawnAmbientParticles(2);
		}
	}

	/// <summary>Met à jour le texte de progression affiché en bas.</summary>
	public void SetProgress(string text)
	{
		if (_progressLabel != null && IsInstanceValid(_progressLabel))
			_progressLabel.Text = text;
	}

	/// <summary>Fade-out de l'overlay vers le gameplay. Appelle onComplete quand fini.</summary>
	public void FadeOut(Action onComplete = null)
	{
		_isVisible = false;

		Tween tween = CreateTween();
		tween.SetProcessMode(Tween.TweenProcessMode.Idle);

		// Fade-out du texte de lore
		tween.TweenProperty(_loreLabel, "modulate:a", 0f, 0.3f);
		tween.Parallel().TweenProperty(_progressLabel, "modulate:a", 0f, 0.3f);

		// Petite pause dans le noir
		tween.TweenInterval(0.2f);

		// Fade-out du fond noir
		tween.TweenProperty(_background, "color:a", 0f, 0.8f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
		tween.Parallel().TweenProperty(_particleLayer, "modulate:a", 0f, 0.6f);

		// Libérer les inputs et cleanup
		tween.TweenCallback(Callable.From(() =>
		{
			_root.MouseFilter = Control.MouseFilterEnum.Ignore;
			onComplete?.Invoke();
			QueueFree();
		}));
	}

	private void FadeInLoreText()
	{
		Tween tween = CreateTween();
		tween.TweenProperty(_loreLabel, "modulate:a", 0.8f, 1.0f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.Out);
	}

	private void TransitionLoreText(string newText)
	{
		Tween tween = CreateTween();
		tween.TweenProperty(_loreLabel, "modulate:a", 0f, 0.4f)
			.SetTrans(Tween.TransitionType.Sine);
		tween.TweenCallback(Callable.From(() => _loreLabel.Text = newText));
		tween.TweenProperty(_loreLabel, "modulate:a", 0.8f, 0.6f)
			.SetTrans(Tween.TransitionType.Sine);
	}

	private void SpawnAmbientParticles(int count)
	{
		Vector2 viewSize = GetViewport().GetVisibleRect().Size;

		for (int i = 0; i < count; i++)
		{
			float x = _rng.RandfRange(0f, viewSize.X);
			float y = _rng.RandfRange(0f, viewSize.Y);
			float size = _rng.RandfRange(1.5f, 4f);
			float duration = _rng.RandfRange(2f, 4f);

			// Mélange or / violet / cyan
			float roll = _rng.Randf();
			Color color;
			if (roll < 0.6f) color = GoldDim;
			else if (roll < 0.85f) color = VioletBrume;
			else color = CyanEssence;

			ColorRect particle = new()
			{
				Size = new Vector2(size, size),
				Position = new Vector2(x, y),
				Color = new Color(color, 0f),
				MouseFilter = Control.MouseFilterEnum.Ignore,
			};
			_particleLayer.AddChild(particle);

			// Animation : apparition, flottement vers le haut, disparition
			float drift = _rng.RandfRange(-30f, 30f);
			float rise = _rng.RandfRange(20f, 60f);
			Vector2 endPos = new(x + drift, y - rise);

			Tween pt = CreateTween();
			pt.TweenProperty(particle, "color:a", _rng.RandfRange(0.3f, 0.7f), duration * 0.3f)
				.SetTrans(Tween.TransitionType.Sine);
			pt.Parallel().TweenProperty(particle, "position", endPos, duration)
				.SetTrans(Tween.TransitionType.Sine)
				.SetEase(Tween.EaseType.Out);
			pt.TweenProperty(particle, "color:a", 0f, duration * 0.4f)
				.SetTrans(Tween.TransitionType.Sine);
			pt.TweenCallback(Callable.From(() => particle.QueueFree()));
		}
	}
}
