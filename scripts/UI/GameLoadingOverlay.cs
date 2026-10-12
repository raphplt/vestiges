using System;
using Godot;

namespace Vestiges.UI;

/// <summary>
/// Overlay de chargement affiché pendant l'initialisation async du monde (plan 24 B5). Démarre noir (raccord avec la
/// VoidTransition du Hub) ; le personnage marche sur une bande de sol qui se dessine au rythme du chargement
/// (<see cref="LoadingWalkStrip"/>), au-dessus de fragments et conseils mêlés (plan 24 B5b). Les étapes
/// techniques ne s'affichent plus : elles vont au journal. Puis fondu vers la partie. En cas d'échec (plan 26 Q4),
/// l'écran s'arrête sur l'erreur et un bouton de retour au camp, utilisable pendant la pause.
/// </summary>
public partial class GameLoadingOverlay : CanvasLayer
{
	private static readonly Color VoidBlack = new(0.02f, 0.02f, 0.05f);
	private static readonly Color GoldFoyer = new(0.83f, 0.66f, 0.26f);

	// Un seul catalogue localisé : aucune présentation différente pour les conseils.
	private const int MessageCount = 40;
	private const float MessageSeconds = 7f;
	// Conservé entre les départs en run : un chargement court consomme aussi un nouveau texte.
	private static readonly int[] MessageOrder = new int[MessageCount];
	private static int _messageCursor = MessageCount;
	private static int _lastMessage = -1;

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
	private Label _messageLabel;
	private LoadingWalkStrip _strip;
	private Control _particleLayer;
	private RandomNumberGenerator _rng = new();
	private bool _isVisible = true;
	private float _messageTimer;
	private Tween _messageTween;
	private (string Step, string Message, Action OnReturn)? _pendingFailure;

	public override void _Ready()
	{
		Layer = 100;
		ProcessMode = ProcessModeEnum.Always;

		_rng.Seed = (ulong)Time.GetTicksMsec();

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

		// Fond dédié en 480×270 : le grain de l'accueil, avec des masses de pierre simplifiées (plan 24 B5c).
		_particleLayer = new PixelBackdrop(PixelBackdrop.NeutralTint, overGame: false,
			background: GD.Load<Texture2D>("res://assets/ui/menus/stone/loading.png")) { Modulate = new Color(1f, 1f, 1f, 0.25f) };
		_particleLayer.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		_particleLayer.MouseFilter = Control.MouseFilterEnum.Ignore;
		_root.AddChild(_particleLayer);

		// Bande de sol et personnage qui marche.
		_strip = new LoadingWalkStrip();
		_strip.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		_root.AddChild(_strip);

		// Zone de lecture sous la bande, séparée de la marche et du sol même sur plusieurs lignes.
		_messageLabel = new Label
		{
			Name = "LoadingMessage",
			Text = NextMessage(),
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Modulate = new Color(1f, 1f, 1f, 0f),
		};
		_messageLabel.AnchorLeft = 0.25f;
		_messageLabel.AnchorRight = 0.75f;
		_messageLabel.AnchorTop = 0.69f;
		_messageLabel.AnchorBottom = 0.89f;
		_messageLabel.AddThemeConstantOverride("line_spacing", 4);
		UITheme.SetTextRole(_messageLabel, TextRole.Heading);
		_messageLabel.AddThemeColorOverride("font_color", GoldFoyer);
		_root.AddChild(_messageLabel);

		// Apparition du premier message
		FadeInMessage();

		if (_pendingFailure is { } failure)
			ShowFailure(failure.Step, failure.Message, failure.OnReturn);
	}

	/// <summary>
	/// Arrête l'écran sur l'échec du chargement : une phrase, l'étape et le message, un bouton focalisé qui rend la main.
	/// Appelé avant l'entrée dans l'arbre (échec à la lecture des catalogues), il s'affiche dès que l'écran est prêt.
	/// </summary>
	public void ShowFailure(string step, string message, Action onReturn)
	{
		if (!IsNodeReady())
		{
			_pendingFailure = (step, message, onReturn);
			return;
		}
		_pendingFailure = null;
		_isVisible = false;
		_messageTween?.Kill();
		_strip.Visible = false;
		_messageLabel.Visible = false;

		CenterContainer center = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
		center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		_root.AddChild(center);
		VBoxContainer box = new() { Name = "LoadingFailure" };
		box.AddThemeConstantOverride("separation", 24);
		center.AddChild(box);

		box.AddChild(UITheme.MakeLabel(Tr("LOADING_FAILED_TITLE"), TextRole.Title, UITheme.GoldColor, TextWeight.Strong,
			HorizontalAlignment.Center));
		Label detail = UITheme.MakeLabel(string.Format(Tr("LOADING_FAILED_DETAIL"), step, message), TextRole.Body,
			UITheme.TextDim, align: HorizontalAlignment.Center);
		detail.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		detail.CustomMinimumSize = new Vector2(720f, 0f);
		box.AddChild(detail);

		Button back = new() { Name = "ReturnToCamp", Text = Tr("UI_END_HUB"), CustomMinimumSize = new Vector2(320f, 56f),
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter };
		UITheme.SetTextRole(back, TextRole.Lead);
		UITheme.ApplyButtonStyle(back, UITheme.LoadTex(UITheme.MenusPath + "ui_button_normal.png"),
			UITheme.LoadTex(UITheme.MenusPath + "ui_button_hover.png"), UITheme.LoadTex(UITheme.MenusPath + "ui_button_pressed.png"),
			null);
		back.Pressed += () => onReturn?.Invoke();
		box.AddChild(back);
		back.CallDeferred(Control.MethodName.GrabFocus);
	}

	public override void _Process(double delta)
	{
		if (!_isVisible) return;

		_messageTimer += (float)delta;

		if (_messageTimer >= MessageSeconds)
		{
			_messageTimer = 0f;
			TransitionMessage(NextMessage());
		}

	}

	private string NextMessage()
	{
		if (_messageCursor >= MessageCount)
		{
			for (int i = 0; i < MessageCount; i++)
				MessageOrder[i] = i;
			for (int i = MessageCount - 1; i > 0; i--)
			{
				int other = _rng.RandiRange(0, i);
				(MessageOrder[i], MessageOrder[other]) = (MessageOrder[other], MessageOrder[i]);
			}
			// La jonction entre deux tirages ne répète pas le dernier texte lu.
			if (MessageOrder[0] == _lastMessage)
				(MessageOrder[0], MessageOrder[1]) = (MessageOrder[1], MessageOrder[0]);
			_messageCursor = 0;
		}
		int index = MessageOrder[_messageCursor++];
		_lastMessage = index;
		// Le CSV conserve une ligne par clé ; chaque document garde ses retours à la ligne.
		return Tr($"LOADING_LINE_{index + 1}").ToString().Replace("\\n", "\n", StringComparison.Ordinal);
	}

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
		_messageTween?.Kill();

		Tween tween = CreateTween();
		tween.SetProcessMode(Tween.TweenProcessMode.Idle);

		// La marche arrive au bout, puis la phrase et la bande s'effacent.
		_strip.SetProgress(1f);
		tween.TweenInterval(0.35f);
		tween.TweenProperty(_messageLabel, "modulate:a", 0f, 0.3f);
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

	private void FadeInMessage()
	{
		Tween tween = _messageTween = CreateTween();
		tween.TweenProperty(_messageLabel, "modulate:a", 0.8f, 1.0f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.Out);
	}

	private void TransitionMessage(string newText)
	{
		_messageTween?.Kill();
		Tween tween = _messageTween = CreateTween();
		tween.TweenProperty(_messageLabel, "modulate:a", 0f, 0.4f)
			.SetTrans(Tween.TransitionType.Sine);
		tween.TweenCallback(Callable.From(() => _messageLabel.Text = newText));
		tween.TweenProperty(_messageLabel, "modulate:a", 0.8f, 0.6f)
			.SetTrans(Tween.TransitionType.Sine);
	}

}
