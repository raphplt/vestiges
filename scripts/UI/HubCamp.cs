using System;
using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Infrastructure;

namespace Vestiges.UI;

/// <summary>
/// Le Miroir diégétique : les personnages veillent autour du feu du Hub, au grain de la peinture.
/// Le personnage choisi se tourne vers le joueur dans un halo doré ; les autres regardent le feu ;
/// ceux qui restent à débloquer ne sont que des silhouettes que l'Effacement ronge.
/// </summary>
public partial class HubCamp : Control
{
	[Signal] public delegate void SelectionChangedEventHandler(string characterId);

	// Places autour du feu, dans l'ordre d'arrivée des personnages (angle en degrés, écran y vers le bas).
	private static readonly float[] SpotAngles = { 58f, 122f, 12f, 168f, 32f, 148f };
	private static readonly Vector2 SpotRadii = new(232f, 76f);
	private static readonly Vector2 FrameFeet = new(16f, 36f);
	private static readonly Color Unselected = new(0.5f, 0.45f, 0.5f);
	private static readonly Color Hovered = new(0.86f, 0.8f, 0.78f);
	private static readonly Color Gold = new(0.83f, 0.66f, 0.26f);

	private sealed class Figure
	{
		public string Id;
		public bool Unlocked;
		public Vector2 Feet;
		public Node2D Root;
		public AnimatedSprite2D Sprite;
		public bool IsHovered;
		public Tween SpriteTween;
		public Tween RootTween;
	}

	private readonly List<Figure> _figures = new();
	private ShaderMaterial _chosenOutline;
	private readonly RandomNumberGenerator _rng = new();
	private Node2D _halo;
	private Tween _haloTween;
	private Figure _selected;

	public string SelectedId => _selected?.Id;

	public bool IsSelectedUnlocked => _selected?.Unlocked ?? false;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		Size = new Vector2(1920f, 1080f);
		_rng.Randomize();
		_halo = CreateHalo();
		AddChild(_halo);
	}

	/// <summary>Installe les personnages (tous, verrouillés compris) et sélectionne celui demandé sans animation.</summary>
	public void Populate(IReadOnlyList<CharacterData> characters, Func<string, bool> isUnlocked, string selectedId)
	{
		ShaderMaterial forgotten = new() { Shader = GD.Load<Shader>("res://assets/shaders/hub_forgotten.gdshader") };
		_chosenOutline = new ShaderMaterial { Shader = GD.Load<Shader>("res://assets/shaders/outline.gdshader") };
		_chosenOutline.SetShaderParameter("outline_color", new Color(0.96f, 0.8f, 0.4f, 0.95f));
		List<Figure> ordered = new();
		if (characters.Count > SpotAngles.Length)
			GD.PushWarning($"[HubCamp] {characters.Count} personnages pour {SpotAngles.Length} places autour du feu : les suivants sont absents.");
		for (int index = 0; index < characters.Count && index < SpotAngles.Length; index++)
		{
			CharacterData data = characters[index];
			SpriteFrames frames = CharacterSpriteLoader.LoadOrGet(data.Id, data.SpriteFolder);
			if (frames == null)
			{
				GD.PushWarning($"[HubCamp] Sprites introuvables pour '{data.Id}' : absent du camp.");
				continue;
			}

			float angle = Mathf.DegToRad(SpotAngles[index]);
			Vector2 feet = (HubBackdrop.FirePosition + new Vector2(Mathf.Cos(angle) * SpotRadii.X, Mathf.Sin(angle) * SpotRadii.Y))
				.Snapped(new Vector2(HubBackdrop.PixelScale, HubBackdrop.PixelScale));
			Figure figure = new() { Id = data.Id, Unlocked = isUnlocked(data.Id), Feet = feet };
			figure.Root = new Node2D { Position = feet };
			figure.Root.AddChild(CreateShadow());
			figure.Sprite = new AnimatedSprite2D
			{
				SpriteFrames = frames,
				Centered = false,
				Offset = -FrameFeet,
				Scale = new Vector2(HubBackdrop.PixelScale, HubBackdrop.PixelScale),
				TextureFilter = TextureFilterEnum.Nearest,
				Material = figure.Unlocked ? null : forgotten
			};
			figure.Root.AddChild(figure.Sprite);
			figure.Root.AddChild(CreateHitArea(figure));
			ordered.Add(figure);
		}

		// Les plus proches du spectateur passent devant.
		ordered.Sort((a, b) => a.Feet.Y.CompareTo(b.Feet.Y));
		foreach (Figure figure in ordered)
		{
			AddChild(figure.Root);
			_figures.Add(figure);
		}
		_figures.Sort((a, b) => IndexOf(characters, a.Id).CompareTo(IndexOf(characters, b.Id)));

		foreach (Figure figure in _figures)
		{
			FaceFire(figure);
			figure.Sprite.Frame = _rng.RandiRange(0, 5);
			Fade(figure, Unselected, 0f);
		}

		Figure initial = Find(selectedId) ?? (_figures.Count > 0 ? _figures[0] : null);
		if (initial != null)
			ApplySelection(initial, false);
	}

	public void Select(string characterId)
	{
		Figure figure = Find(characterId);
		if (figure != null && figure != _selected)
			ApplySelection(figure, true);
	}

	/// <summary>Passe au personnage suivant ou précédent dans l'ordre du Miroir.</summary>
	public void Cycle(int step)
	{
		if (_figures.Count < 2 || _selected == null)
			return;
		int index = _figures.IndexOf(_selected);
		ApplySelection(_figures[Mathf.PosMod(index + step, _figures.Count)], true);
	}

	/// <summary>Le personnage choisi quitte le camp en s'éloignant dans la nuit.</summary>
	public void Depart(float duration)
	{
		if (_selected == null)
			return;
		Figure figure = _selected;
		PlayAnimation(figure, "NE", "walk");
		Tween tween = Restart(ref figure.RootTween, figure.Root).SetParallel(true);
		tween.TweenProperty(figure.Root, "position", figure.Feet + new Vector2(96f, -64f), duration);
		tween.TweenProperty(figure.Root, "modulate:a", 0f, duration).SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
		Restart(ref _haloTween, _halo).TweenProperty(_halo, "modulate:a", 0f, duration * 0.6f);
	}

	private void ApplySelection(Figure figure, bool animate)
	{
		Figure previous = _selected;
		_selected = figure;

		if (previous != null)
		{
			if (previous.Unlocked)
				previous.Sprite.Material = null;
			FaceFire(previous);
			Fade(previous, previous.IsHovered ? Hovered : Unselected, animate ? 0.25f : 0f);
		}

		PlayAnimation(figure, "S", "idle");
		_halo.Visible = figure.Unlocked;
		if (figure.Unlocked)
			figure.Sprite.Material = _chosenOutline;
		if (animate)
		{
			// Un éclat quand le personnage « se souvient » de lui-même.
			figure.Sprite.Modulate = new Color(1.8f, 1.6f, 1.2f);
			Fade(figure, Colors.White, 0.35f);
			Restart(ref _haloTween, _halo).TweenProperty(_halo, "position", figure.Feet, 0.22f).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
			figure.Root.Scale = new Vector2(1f, 0.92f);
			Restart(ref figure.RootTween, figure.Root).TweenProperty(figure.Root, "scale", Vector2.One, 0.25f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
		}
		else
		{
			Fade(figure, Colors.White, 0f);
			_halo.Position = figure.Feet;
		}

		EmitSignal(SignalName.SelectionChanged, figure.Id);
	}

	private void FaceFire(Figure figure)
	{
		Vector2 toFire = HubBackdrop.FirePosition - figure.Feet;
		int sector = Mathf.PosMod(Mathf.RoundToInt(toFire.Angle() / (Mathf.Pi / 4f)), 8);
		PlayAnimation(figure, CharacterFacing.DirectionNames[sector], "idle");
	}

	private static void PlayAnimation(Figure figure, string direction, string action)
	{
		string animation = $"{direction}_{action}";
		if (!figure.Sprite.SpriteFrames.HasAnimation(animation))
			animation = $"SE_{action}";
		int frame = figure.Sprite.Frame;
		figure.Sprite.Play(animation);
		figure.Sprite.Frame = frame % figure.Sprite.SpriteFrames.GetFrameCount(animation);
	}

	private static void Fade(Figure figure, Color target, float duration)
	{
		if (duration <= 0f)
		{
			figure.SpriteTween?.Kill();
			figure.Sprite.Modulate = target;
			return;
		}
		Restart(ref figure.SpriteTween, figure.Sprite).TweenProperty(figure.Sprite, "modulate", target, duration);
	}

	/// <summary>Remplace le tween en cours sur une cible : des changements rapides ne se disputent pas les mêmes propriétés.</summary>
	private static Tween Restart(ref Tween tween, Node target)
	{
		tween?.Kill();
		tween = target.CreateTween();
		return tween;
	}

	private Button CreateHitArea(Figure figure)
	{
		Button hit = new()
		{
			Flat = true,
			FocusMode = FocusModeEnum.None,
			MouseDefaultCursorShape = CursorShape.PointingHand,
			Size = new Vector2(24f, 34f) * HubBackdrop.PixelScale,
			Position = new Vector2(-12f, -34f) * HubBackdrop.PixelScale
		};
		StyleBoxEmpty empty = new();
		foreach (string state in new[] { "normal", "hover", "pressed", "focus", "hover_pressed" })
			hit.AddThemeStyleboxOverride(state, empty);

		hit.MouseEntered += () =>
		{
			figure.IsHovered = true;
			if (figure != _selected)
			{
				Fade(figure, Hovered, 0.12f);
				AudioManager.PlayUI("sfx_menu_survol");
			}
		};
		hit.MouseExited += () =>
		{
			figure.IsHovered = false;
			if (figure != _selected)
				Fade(figure, Unselected, 0.2f);
		};
		hit.Pressed += () =>
		{
			if (figure == _selected)
				return;
			AudioManager.PlayUI("sfx_menu_clic");
			ApplySelection(figure, true);
		};
		return hit;
	}

	private static Sprite2D CreateShadow()
	{
		Image image = Image.CreateEmpty(14, 4, false, Image.Format.Rgba8);
		image.Fill(new Color(0f, 0f, 0f, 0f));
		for (int x = 1; x < 13; x++)
		{
			for (int y = 0; y < 4; y++)
			{
				bool corner = (x < 3 || x > 10) && (y == 0 || y == 3);
				if (!corner)
					image.SetPixel(x, y, new Color(0.04f, 0.03f, 0.06f, 0.55f));
			}
		}
		return new Sprite2D
		{
			Texture = ImageTexture.CreateFromImage(image),
			Scale = new Vector2(HubBackdrop.PixelScale, HubBackdrop.PixelScale),
			Position = new Vector2(0f, -2f),
			TextureFilter = TextureFilterEnum.Nearest
		};
	}

	/// <summary>Halo doré au sol, en anneaux francs, qui respire sous le personnage choisi.</summary>
	private static Node2D CreateHalo()
	{
		Gradient gradient = new()
		{
			InterpolationMode = Gradient.InterpolationModeEnum.Constant,
			Offsets = new[] { 0f, 0.4f, 0.75f, 1f },
			Colors = new[] { new Color(1f, 0.86f, 0.5f, 0.75f), new Color(Gold, 0.5f), new Color(Gold, 0.24f), new Color(Gold, 0f) }
		};
		Sprite2D ring = new()
		{
			Texture = new GradientTexture2D
			{
				Gradient = gradient,
				Fill = GradientTexture2D.FillEnum.Radial,
				FillFrom = new Vector2(0.5f, 0.5f),
				FillTo = new Vector2(0.5f, 0f),
				Width = 30,
				Height = 10
			},
			Scale = new Vector2(HubBackdrop.PixelScale, HubBackdrop.PixelScale),
			Position = new Vector2(0f, -2f),
			TextureFilter = TextureFilterEnum.Nearest,
			Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add }
		};
		Node2D halo = new();
		halo.AddChild(ring);

		CpuParticles2D motes = new()
		{
			Amount = 10,
			Lifetime = 1.8,
			EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle,
			EmissionRectExtents = new Vector2(40f, 4f),
			Direction = Vector2.Up,
			Spread = 8f,
			Gravity = Vector2.Zero,
			InitialVelocityMin = 22f,
			InitialVelocityMax = 48f,
			ScaleAmountMin = HubBackdrop.PixelScale,
			ScaleAmountMax = HubBackdrop.PixelScale,
			TextureFilter = TextureFilterEnum.Nearest,
			ColorRamp = new Gradient
			{
				Offsets = new[] { 0f, 0.2f, 1f },
				Colors = new[] { new Color(Gold, 0f), new Color(1f, 0.86f, 0.5f, 0.85f), new Color(Gold, 0f) }
			}
		};
		Image pixel = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8);
		pixel.Fill(Colors.White);
		motes.Texture = ImageTexture.CreateFromImage(pixel);
		halo.AddChild(motes);

		Tween breathe = ring.CreateTween().SetLoops();
		breathe.TweenProperty(ring, "modulate:a", 0.65f, 1.1f).SetTrans(Tween.TransitionType.Sine);
		breathe.TweenProperty(ring, "modulate:a", 1f, 1.1f).SetTrans(Tween.TransitionType.Sine);
		return halo;
	}

	private Figure Find(string characterId)
	{
		foreach (Figure figure in _figures)
		{
			if (figure.Id == characterId)
				return figure;
		}
		return null;
	}

	private static int IndexOf(IReadOnlyList<CharacterData> characters, string id)
	{
		for (int index = 0; index < characters.Count; index++)
		{
			if (characters[index].Id == id)
				return index;
		}
		return -1;
	}
}
