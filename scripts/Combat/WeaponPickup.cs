using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Combat;

/// <summary>
/// Arme lâchée au sol par un ennemi ou un événement.
/// Le joueur s'approche pour la ramasser automatiquement (pas d'interaction requise).
/// Affiche le nom de l'arme et une lueur pulsante.
/// Si les slots sont pleins, flash rouge (pas de swap — les armes viennent du level-up).
/// </summary>
public partial class WeaponPickup : Area2D
{
	private const float PickupRadius = 30f;
	private const float BobAmplitude = 3f;
	private const float BobSpeed = 2f;
	private const float SpawnScatterSpeed = 80f;
	private const float DespawnTime = 120f;
	private const string ResourcePrefix = "res://";

	private WeaponData _weaponData;
	private Node2D _visualRoot;
	private Sprite2D _visual;
	private Polygon2D _glow;
	private Label _nameLabel;
	private float _bobTimer;
	private Vector2 _basePosition;
	private Vector2 _scatterVelocity;
	private float _scatterTimer;
	private bool _collected;
	private Tween _glowTween;

	public WeaponData Weapon => _weaponData;

	public void Initialize(WeaponData weapon, Vector2 position)
	{
		_weaponData = weapon;
		GlobalPosition = position;
		_basePosition = position;

		float angle = (float)GD.RandRange(0, Mathf.Tau);
		_scatterVelocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * SpawnScatterSpeed;
		_scatterTimer = 0.3f;
	}

	public override void _Ready()
	{
		CollisionLayer = 0;
		CollisionMask = 1;

		CollisionShape2D shape = new();
		CircleShape2D circle = new() { Radius = PickupRadius };
		shape.Shape = circle;
		AddChild(shape);

		CreateVisual();
		CreateNameLabel();

		BodyEntered += OnBodyEntered;

		GetTree().CreateTimer(DespawnTime).Timeout += () =>
		{
			if (!_collected && IsInstanceValid(this))
				Despawn();
		};

		Scale = Vector2.Zero;
		Tween spawnTween = CreateTween();
		spawnTween.TweenProperty(this, "scale", Vector2.One, 0.25f)
			.SetTrans(Tween.TransitionType.Back)
			.SetEase(Tween.EaseType.Out);

		AddToGroup("weapon_pickups");
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;

		if (_scatterTimer > 0f)
		{
			_scatterTimer -= dt;
			GlobalPosition += _scatterVelocity * dt;
			_scatterVelocity *= 0.9f;
			_basePosition = GlobalPosition;
		}

		_bobTimer += dt * BobSpeed;
		float bobOffset = Mathf.Sin(_bobTimer) * BobAmplitude;
		if (_visualRoot != null)
			_visualRoot.Position = new Vector2(0, bobOffset);
	}

	private void OnBodyEntered(Node2D body)
	{
		if (_collected)
			return;

		if (body is not Player player)
			return;

		// Defer to avoid "Can't change state while flushing queries"
		CallDeferred(MethodName.ProcessPickup, player.GetPath());
	}

	private void ProcessPickup(NodePath playerPath)
	{
		if (_collected)
			return;

		Player player = GetNodeOrNull<Player>(playerPath);
		if (player == null)
			return;

		if (player.AddWeapon(_weaponData))
		{
			_collected = true;
			PlayPickupEffect(player);

			EventBus eventBus = GetNodeOrNull<EventBus>("/root/EventBus");
			eventBus?.EmitSignal(EventBus.SignalName.LootReceived, "weapon", _weaponData.Id, 1);

			GD.Print($"[WeaponPickup] {_weaponData.Name} ramassée !");
		}
		else
		{
			FlashFull();
		}
	}

	private void PlayPickupEffect(Player player)
	{
		_glowTween?.Kill();
		Tween tween = CreateTween();
		tween.SetParallel();
		tween.TweenProperty(this, "scale", Vector2.One * 1.5f, 0.1f);
		tween.TweenProperty(this, "modulate", Colors.White, 0.05f);
		tween.Chain().SetParallel();
		tween.TweenProperty(this, "scale", Vector2.Zero, 0.15f);
		tween.TweenProperty(this, "modulate:a", 0f, 0.15f);
		tween.Chain().TweenCallback(Callable.From(QueueFree));

		SpawnFloatingText(player);
	}

	private void SpawnFloatingText(Player player)
	{
		Label floatLabel = new()
		{
			Text = $"+ {_weaponData.Name}",
			HorizontalAlignment = HorizontalAlignment.Center,
			GlobalPosition = GlobalPosition + new Vector2(0, -20)
		};
		floatLabel.AddThemeColorOverride("font_color", GetTierColor());
		floatLabel.AddThemeFontSizeOverride("font_size", 11);

		GetTree().Root.CallDeferred("add_child", floatLabel);

		Vector2 startPos = floatLabel.GlobalPosition;
		Callable cleanup = Callable.From(() =>
		{
			if (IsInstanceValid(floatLabel))
				floatLabel.QueueFree();
		});

		SceneTreeTimer timer = GetTree().CreateTimer(0f);
		timer.Timeout += () =>
		{
			if (!IsInstanceValid(floatLabel))
				return;
			Tween tween = floatLabel.CreateTween();
			tween.SetParallel();
			tween.TweenProperty(floatLabel, "global_position", startPos + new Vector2(0, -40), 1.2f);
			tween.TweenProperty(floatLabel, "modulate:a", 0f, 1.2f).SetDelay(0.5f);
			tween.Chain().TweenCallback(cleanup);
		};
	}

	private void FlashFull()
	{
		if (_visual == null)
			return;

		Color original = _visual.Modulate;
		_visual.Modulate = new Color(1f, 0.3f, 0.2f);
		Tween tween = CreateTween();
		tween.TweenProperty(_visual, "modulate", original, 0.3f).SetDelay(0.1f);
	}

	private void Despawn()
	{
		_glowTween?.Kill();
		Tween tween = CreateTween();
		tween.SetParallel();
		tween.TweenProperty(this, "scale", Vector2.Zero, 0.4f);
		tween.TweenProperty(this, "modulate:a", 0f, 0.4f);
		tween.Chain().TweenCallback(Callable.From(QueueFree));
	}

	private void CreateVisual()
	{
		_visualRoot = new Node2D();
		AddChild(_visualRoot);

		_glow = new Polygon2D();
		float gr = 14f;
		_glow.Polygon = new Vector2[]
		{
			new(-gr, 0), new(0, -gr * 0.5f),
			new(gr, 0), new(0, gr * 0.5f)
		};
		_glow.Color = new Color(GetTierColor(), 0.35f);
		_glow.ZIndex = -1;
		_visualRoot.AddChild(_glow);

		_glowTween = CreateTween();
		_glowTween.SetLoops();
		_glowTween.TweenProperty(_glow, "modulate:a", 0.4f, 0.7f)
			.SetTrans(Tween.TransitionType.Sine);
		_glowTween.TweenProperty(_glow, "modulate:a", 1f, 0.7f)
			.SetTrans(Tween.TransitionType.Sine);

		Texture2D weaponTexture = LoadWeaponTexture();
		if (weaponTexture != null)
		{
				_visual = new Sprite2D
				{
					Texture = weaponTexture,
					Centered = true,
					TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
					Scale = new Vector2(0.5f, 0.5f)
				};
			_visualRoot.AddChild(_visual);
		}
		else
		{
			// Fallback défensif si un sprite d'arme est manquant.
				_visual = new Sprite2D
				{
					Texture = CreateFallbackTexture(),
					Centered = true,
					TextureFilter = CanvasItem.TextureFilterEnum.Nearest
				};
			_visualRoot.AddChild(_visual);
		}
	}

	private void CreateNameLabel()
	{
		_nameLabel = new Label
		{
			Text = _weaponData?.Name ?? "???",
			HorizontalAlignment = HorizontalAlignment.Center,
			Position = new Vector2(-40, -24)
		};
		_nameLabel.AddThemeColorOverride("font_color", GetTierColor());
		_nameLabel.AddThemeFontSizeOverride("font_size", 8);
		_nameLabel.Size = new Vector2(80, 16);
		AddChild(_nameLabel);
	}

	private Color GetTierColor()
	{
		int tier = _weaponData?.Tier ?? 1;
		return tier switch
		{
			1 => new Color(0.7f, 0.7f, 0.7f),
			2 => new Color(0.4f, 0.7f, 1f),
			3 => new Color(0.9f, 0.6f, 0.15f),
			4 => new Color(0.7f, 0.3f, 1f),
			5 => new Color(1f, 0.85f, 0.2f),
			_ => Colors.White
		};
	}

	private Texture2D LoadWeaponTexture()
	{
		string spritePath = _weaponData?.Sprite;
		if (string.IsNullOrWhiteSpace(spritePath))
			return null;

		string resourcePath = spritePath.StartsWith(ResourcePrefix) ? spritePath : ResourcePrefix + spritePath;
		return GD.Load<Texture2D>(resourcePath);
	}

	private ImageTexture CreateFallbackTexture()
	{
		Color color = GetTierColor();
		Image image = Image.Create(16, 16, false, Image.Format.Rgba8);
		image.Fill(Colors.Transparent);

		for (int x = 3; x < 13; x++)
		{
			for (int y = 6; y < 10; y++)
				image.SetPixel(x, y, color);
		}

		return ImageTexture.CreateFromImage(image);
	}
}
