using System;
using Godot;

namespace Vestiges.UI;

/// <summary>
/// Overlay de chargement affiché pendant l'initialisation async du monde (plan 24 B5). Démarre noir (raccord avec la
/// VoidTransition du Hub) ; le personnage marche sur une bande de sol qui se dessine au rythme du chargement
/// (<see cref="LoadingWalkStrip"/>), sous une phrase courte, et les poussières natives du fond s'animent. Les étapes
/// techniques ne s'affichent plus : elles vont au journal. Puis fondu vers la partie.
/// </summary>
public partial class GameLoadingOverlay : CanvasLayer
{
	private static readonly Color VoidBlack = new(0.02f, 0.02f, 0.05f);
	private static readonly Color GoldFoyer = new(0.83f, 0.66f, 0.26f);

	// Phrases réécrites, moins directes (plan 19, plan 24 B5) : des choses vues, pas des explications.
	private const int LoreCount = 8;

	// Étapes du chargement (GameBootstrap, WorldSetup) : début de leur part de la progression, et sa fin. Un « N % »
	// dans le texte place la progression à l'intérieur de l'étape.
	private static readonly (string Prefix, float Start, float End)[] Steps =
	{
		("Préparation des shaders", 0f, 0.05f),
		("Création du monde", 0.05f, 0.1f),
		("Terrain", 0.1f, 0.45f),
		("Routes", 0.45f, 0.6f),
		("Brouillard", 0.6f, 0.65f),
		("Points d'intérêt", 0.65f, 0.7f),
		("Décors", 0.7f, 0.85f),
		("Atmosphère", 0.85f, 0.88f),
		("Préparation des créatures", 0.88f, 0.97f),
		("Initialisation", 0.97f, 1f),
	};

	private Control _root;
	private ColorRect _background;
	private Label _loreLabel;
	private LoadingWalkStrip _strip;
	private Control _particleLayer;
	private RandomNumberGenerator _rng = new();
	private bool _isVisible = true;
	private float _loreTimer;
	private int _loreIndex;

	public override void _Ready()
	{
		Layer = 100;
		ProcessMode = ProcessModeEnum.Always;

		_rng.Seed = (ulong)Time.GetTicksMsec();
		_loreIndex = (int)(_rng.Randi() % LoreCount);

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

		// Fond natif partagé avec les écrans de choix.
		_particleLayer = new PixelBackdrop(PixelBackdrop.NeutralTint) { Modulate = new Color(1f, 1f, 1f, 0.25f) };
		_particleLayer.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		_particleLayer.MouseFilter = Control.MouseFilterEnum.Ignore;
		_root.AddChild(_particleLayer);

		// Bande de sol et personnage qui marche.
		_strip = new LoadingWalkStrip();
		_strip.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		_root.AddChild(_strip);

		// Phrase au-dessus de la bande.
		_loreLabel = new Label
		{
			Text = LoreLine(_loreIndex),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Modulate = new Color(1f, 1f, 1f, 0f),
		};
		_loreLabel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		_loreLabel.OffsetBottom = -260f;
		UITheme.SetTextRole(_loreLabel, TextRole.Heading);
		_loreLabel.AddThemeColorOverride("font_color", GoldFoyer);
		_root.AddChild(_loreLabel);

		// Fade-in initial du texte de lore
		FadeInLoreText();
	}

	public override void _Process(double delta)
	{
		if (!_isVisible) return;

		_loreTimer += (float)delta;

		// Changer la phrase toutes les 4 s
		if (_loreTimer > 4f)
		{
			_loreTimer = 0f;
			_loreIndex = (_loreIndex + 1) % LoreCount;
			TransitionLoreText(LoreLine(_loreIndex));
		}

	}

	private static string LoreLine(int index) => TranslationServer.Translate($"LOADING_LINE_{index + 1}");

	/// <summary>Étape du chargement : elle avance la marche ; son texte ne va qu'au journal.</summary>
	public void SetProgress(string text)
	{
		GD.Print($"[Chargement] {text}");
		if (_strip == null || !IsInstanceValid(_strip))
			return;
		foreach ((string prefix, float start, float end) in Steps)
		{
			if (!text.StartsWith(prefix, StringComparison.Ordinal))
				continue;
			int percentAt = text.LastIndexOf('%');
			float inside = 0f;
			if (percentAt > 0)
			{
				int from = percentAt - 1;
				while (from > 0 && char.IsDigit(text[from - 1]))
					from--;
				if (int.TryParse(text.AsSpan(from, percentAt - from).Trim(), out int percent))
					inside = percent / 100f;
			}
			_strip.SetProgress(Mathf.Lerp(start, end, inside));
			return;
		}
	}

	/// <summary>Fade-out de l'overlay vers le gameplay. Appelle onComplete quand fini.</summary>
	public void FadeOut(Action onComplete = null)
	{
		_isVisible = false;

		Tween tween = CreateTween();
		tween.SetProcessMode(Tween.TweenProcessMode.Idle);

		// La marche arrive au bout, puis la phrase et la bande s'effacent.
		_strip.SetProgress(1f);
		tween.TweenInterval(0.35f);
		tween.TweenProperty(_loreLabel, "modulate:a", 0f, 0.3f);
		tween.Parallel().TweenProperty(_strip, "modulate:a", 0f, 0.5f);

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

}
