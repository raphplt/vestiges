using System;
using Godot;

namespace Vestiges.UI;

/// <summary>
/// Transition cinematique "Entrer dans le Vide" — multi-phase :
/// 1. Le texte du bouton pulse et l'ecran tremble legerement
/// 2. Des fissures de lumiere doree convergent vers le centre
/// 3. Flash blanc chaud puis fondu vers le noir du Vide
/// 4. Le titre "VESTIGES" apparait brievement en dissolution
/// 5. Scene change
/// </summary>
public partial class VoidTransition : CanvasLayer
{
	// Couleurs narratives
	private static readonly Color VoidBlack = new(0.02f, 0.02f, 0.05f);
	private static readonly Color GoldFoyer = new(0.83f, 0.66f, 0.26f);
	private static readonly Color GoldBright = new(0.95f, 0.85f, 0.45f);
	private static readonly Color FlameWhite = new(1f, 0.97f, 0.9f);
	private static readonly Color VioletBrume = new(0.29f, 0.19f, 0.4f);

	private Control _root;
	private ColorRect _overlay;
	private ColorRect _flash;
	private Label _voidText;
	private Control _particleLayer;
	private Action _onComplete;
	private RandomNumberGenerator _rng = new();
	private bool _isPlaying;

	public override void _Ready()
	{
		Layer = 100;
		ProcessMode = ProcessModeEnum.Always;

		_root = new Control();
		_root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		_root.MouseFilter = Control.MouseFilterEnum.Ignore;
		AddChild(_root);

		// Overlay noir (fondu progressif)
		_overlay = new ColorRect();
		_overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		_overlay.Color = new Color(VoidBlack, 0f);
		_overlay.MouseFilter = Control.MouseFilterEnum.Ignore;
		_root.AddChild(_overlay);

		// Couche particules
		_particleLayer = new Control();
		_particleLayer.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		_particleLayer.MouseFilter = Control.MouseFilterEnum.Ignore;
		_root.AddChild(_particleLayer);

		// Flash blanc
		_flash = new ColorRect();
		_flash.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		_flash.Color = new Color(FlameWhite, 0f);
		_flash.MouseFilter = Control.MouseFilterEnum.Ignore;
		_root.AddChild(_flash);

		// Texte apparaissant dans le vide
		_voidText = new Label
		{
			Text = "Le Vide vous attend...",
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Modulate = new Color(1f, 1f, 1f, 0f)
		};
		_voidText.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		_voidText.AddThemeFontSizeOverride("font_size", 28);
		_voidText.AddThemeColorOverride("font_color", GoldFoyer);
		_root.AddChild(_voidText);

		_root.Visible = false;
	}

	/// <summary>Lance la transition. Appelle onComplete quand c'est fini.</summary>
	public void Play(Action onComplete)
	{
		if (_isPlaying) return;
		_isPlaying = true;
		_onComplete = onComplete;
		_root.Visible = true;
		_rng.Seed = (ulong)Time.GetTicksMsec();

		RunTransition();
	}

	private void RunTransition()
	{
		Tween tween = CreateTween();
		tween.SetProcessMode(Tween.TweenProcessMode.Physics);

		// === Phase 1 (0.0s - 0.6s) : Assombrissement + vignette convergente ===
		// Le monde s'assombrit, les bords noircissent en premier
		tween.TweenProperty(_overlay, "color:a", 0.4f, 0.6f)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.In);

		// Spawner les particules de lumiere (lancees en parallele via callback)
		tween.Parallel().TweenCallback(Callable.From(SpawnConvergingParticles));

		// === Phase 2 (0.6s - 1.0s) : Fissures de lumiere + tremblement ===
		tween.TweenProperty(_overlay, "color:a", 0.7f, 0.4f)
			.SetTrans(Tween.TransitionType.Expo)
			.SetEase(Tween.EaseType.In);
		tween.Parallel().TweenCallback(Callable.From(SpawnLightCracks));
		tween.Parallel().TweenCallback(Callable.From(StartScreenShake));

		// === Phase 3 (1.0s - 1.15s) : Flash blanc intense ===
		tween.TweenProperty(_flash, "color:a", 0.85f, 0.15f)
			.SetTrans(Tween.TransitionType.Expo)
			.SetEase(Tween.EaseType.In);

		// === Phase 4 (1.15s - 1.8s) : Flash se dissipe → noir total ===
		tween.TweenProperty(_flash, "color:a", 0f, 0.5f)
			.SetTrans(Tween.TransitionType.Expo)
			.SetEase(Tween.EaseType.Out);
		tween.Parallel().TweenProperty(_overlay, "color:a", 1f, 0.4f)
			.SetTrans(Tween.TransitionType.Sine);
		tween.Parallel().TweenCallback(Callable.From(StopScreenShake));

		// === Phase 5 (1.8s - 2.8s) : Texte dans le noir ===
		tween.TweenProperty(_voidText, "modulate:a", 0.8f, 0.5f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.Out);
		tween.TweenInterval(0.5f);
		tween.TweenProperty(_voidText, "modulate:a", 0f, 0.5f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.In);

		// === Phase 6 : Callback final ===
		tween.TweenCallback(Callable.From(OnTransitionComplete));
	}

	/// <summary>Particules dorees qui convergent vers le centre de l'ecran.</summary>
	private void SpawnConvergingParticles()
	{
		Vector2 viewSize = GetViewport().GetVisibleRect().Size;
		Vector2 center = viewSize / 2f;

		for (int i = 0; i < 24; i++)
		{
			float delay = i * 0.03f;
			float angle = _rng.RandfRange(0f, Mathf.Tau);
			float dist = _rng.RandfRange(300f, 700f);
			Vector2 start = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
			float size = _rng.RandfRange(2f, 6f);

			bool isGold = _rng.Randf() > 0.3f;
			Color color = isGold ? GoldBright : VioletBrume;

			ColorRect particle = new()
			{
				Size = new Vector2(size, size),
				Position = start - new Vector2(size / 2f, size / 2f),
				Color = new Color(color, 0.8f),
				MouseFilter = Control.MouseFilterEnum.Ignore
			};
			_particleLayer.AddChild(particle);

			float duration = _rng.RandfRange(0.6f, 1.0f);

			Tween pt = CreateTween();
			pt.SetProcessMode(Tween.TweenProcessMode.Physics);
			pt.TweenInterval(delay);
			pt.TweenProperty(particle, "position", center - new Vector2(size / 2f, size / 2f), duration)
				.SetTrans(Tween.TransitionType.Quad)
				.SetEase(Tween.EaseType.In);
			pt.Parallel().TweenProperty(particle, "color:a", 0f, duration * 0.8f)
				.SetDelay(duration * 0.5f)
				.SetTrans(Tween.TransitionType.Expo);
			pt.Parallel().TweenProperty(particle, "size", Vector2.One * 1f, duration)
				.SetTrans(Tween.TransitionType.Quad);
			pt.TweenCallback(Callable.From(() => particle.QueueFree()));
		}
	}

	/// <summary>Lignes de lumiere doree qui craquellent depuis les bords.</summary>
	private void SpawnLightCracks()
	{
		Vector2 viewSize = GetViewport().GetVisibleRect().Size;
		Vector2 center = viewSize / 2f;

		for (int i = 0; i < 8; i++)
		{
			float angle = _rng.RandfRange(0f, Mathf.Tau);
			float length = _rng.RandfRange(100f, 350f);
			Vector2 start = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _rng.RandfRange(20f, 80f);
			Vector2 end = start + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * length;

			// Simuler une ligne via une serie de petits rectangles
			int segments = (int)(length / 8f);
			for (int s = 0; s < segments; s++)
			{
				float t = s / (float)segments;
				Vector2 pos = start.Lerp(end, t);
				// Wobble lateral
				float wobble = Mathf.Sin(t * 12f + i) * 3f;
				pos += new Vector2(-Mathf.Sin(angle), Mathf.Cos(angle)) * wobble;

				float segSize = _rng.RandfRange(2f, 4f);
				float alpha = (1f - t) * 0.7f;

				ColorRect seg = new()
				{
					Size = new Vector2(segSize, segSize),
					Position = pos - new Vector2(segSize / 2f, segSize / 2f),
					Color = new Color(GoldFoyer, 0f),
					MouseFilter = Control.MouseFilterEnum.Ignore
				};
				_particleLayer.AddChild(seg);

				float delay = t * 0.15f + i * 0.04f;

				Tween ct = CreateTween();
				ct.SetProcessMode(Tween.TweenProcessMode.Physics);
				ct.TweenInterval(delay);
				ct.TweenProperty(seg, "color:a", alpha, 0.08f)
					.SetTrans(Tween.TransitionType.Expo);
				ct.TweenProperty(seg, "color:a", 0f, 0.3f)
					.SetTrans(Tween.TransitionType.Expo);
				ct.TweenCallback(Callable.From(() => seg.QueueFree()));
			}
		}
	}

	private Tween _shakeTween;

	private void StartScreenShake()
	{
		_shakeTween = CreateTween();
		_shakeTween.SetProcessMode(Tween.TweenProcessMode.Physics);
		_shakeTween.SetLoops(8);

		_shakeTween.TweenCallback(Callable.From(() =>
		{
			float x = _rng.RandfRange(-4f, 4f);
			float y = _rng.RandfRange(-4f, 4f);
			_root.Position = new Vector2(x, y);
		}));
		_shakeTween.TweenInterval(0.04f);
	}

	private void StopScreenShake()
	{
		_shakeTween?.Kill();
		_shakeTween = null;
		_root.Position = Vector2.Zero;
	}

	private void OnTransitionComplete()
	{
		_isPlaying = false;
		_onComplete?.Invoke();
	}
}
