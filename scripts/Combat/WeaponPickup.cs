using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.UI;
using Vestiges.World;

namespace Vestiges.Combat;

/// <summary>
/// Arme posée au sol par le Wagonnet. Ramassée d'elle-même si un emplacement est libre ; sinon elle attend au sol et
/// l'invite renvoie au Puits, seul endroit où l'on oublie une arme (plan 44). Au ramassage, son icône vole jusqu'au
/// HUD (plan 02 J3).
/// </summary>
public partial class WeaponPickup : Area2D, IInteractable
{
	private const float PickupRadius = 30f;
	private const float BobAmplitude = 3f;
	private const float BobSpeed = 2f;
	private const float SpawnScatterSpeed = 80f;
	private const float DespawnTime = 120f;
	private const float RingRadius = 13f;
	private const float PickupHoldSec = 0.25f;
	private const string ResourcePrefix = "res://";

	private WeaponInstance _weaponInstance;
	private Node2D _visualRoot;
	private Sprite2D _visual;
	private PixelGroundRing _ring;
	private float _bobTimer;
	private Vector2 _scatterVelocity;
	private float _scatterTimer;
	private bool _collected;
	private GroupCache _groupCache;
	private EventBus _eventBus;
	private string _prompt;

	public WeaponData Weapon => _weaponInstance?.Base;
	public WeaponInstance WeaponInstance => _weaponInstance;

	public bool CanInteract => !_collected;
	public Vector2 InteractPosition => GlobalPosition;
	public Vector2 PromptPosition => GlobalPosition + new Vector2(0f, -24f);
	// L'invite traduit sa clé : un texte déjà composé s'affiche tel quel. Lue à chaque pas physique, elle est gardée
	// jusqu'au prochain changement d'arme.
	public string PromptVerbKey => _prompt ??= PickupPrompt();
	public float HoldTime => PickupHoldSec;
	public Color GaugeColor => UITheme.GoldBright;

	private string PickupPrompt()
	{
		string key = _groupCache?.GetPlayer() is not Player player || _weaponInstance == null ? "WEAPON_PICKUP_PROMPT"
			: HasWeapon(player, _weaponInstance.Id) ? "WEAPON_OWNED_PROMPT"
			: player.HasFreeWeaponSlot ? "WEAPON_PICKUP_PROMPT" : "WEAPON_SLOTS_FULL_PROMPT";
		return string.Format(Tr(key), _weaponInstance?.Name ?? "");
	}

	public void Initialize(WeaponData weapon, Vector2 position)
	{
		Initialize(new WeaponInstance(weapon), position);
	}

	public void Initialize(WeaponInstance weapon, Vector2 position)
	{
		_weaponInstance = weapon;
		GlobalPosition = position;

		float angle = (float)GD.RandRange(0, Mathf.Tau);
		_scatterVelocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * SpawnScatterSpeed;
		_scatterTimer = 0.3f;
	}

	public override void _EnterTree()
	{
		Interactables.Register(this);
		_eventBus = GetNodeOrNull<EventBus>("/root/EventBus");
		if (_eventBus != null)
			_eventBus.WeaponInventoryChanged += InvalidatePrompt;
	}

	public override void _ExitTree()
	{
		Interactables.Unregister(this);
		if (_eventBus != null)
			_eventBus.WeaponInventoryChanged -= InvalidatePrompt;
	}

	private void InvalidatePrompt() => _prompt = null;

	public override void _Ready()
	{
		_groupCache = GetNodeOrNull<GroupCache>("/root/GroupCache");
		CollisionLayer = 0;
		CollisionMask = 1;

		CollisionShape2D shape = new();
		CircleShape2D circle = new() { Radius = PickupRadius };
		shape.Shape = circle;
		AddChild(shape);

		CreateVisual();

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
		}

		// Flottement par pixels entiers : l'icône ne glisse pas entre deux lignes de la grille.
		_bobTimer += dt * BobSpeed;
		_visualRoot.Position = new Vector2(0, Mathf.Round(Mathf.Sin(_bobTimer) * BobAmplitude));
	}

	/// <summary>Activation par l'invite : prendre si un emplacement s'est libéré, sinon refuser (oublier se fait au Puits).</summary>
	public void Interact(Player player)
	{
		if (_collected)
			return;
		if (player.HasFreeWeaponSlot && !HasWeapon(player, _weaponInstance.Id))
			TryCollect(player);
		else
			FlashRefused();
	}

	private void OnBodyEntered(Node2D body)
	{
		if (_collected || body is not Player player)
			return;
		// Ramassage automatique seulement s'il reste un emplacement ; sinon l'invite renvoie au Puits.
		if (player.HasFreeWeaponSlot && !HasWeapon(player, _weaponInstance.Id))
			CallDeferred(MethodName.TryCollect, player);
	}

	private void TryCollect(Player player)
	{
		if (_collected || !IsInstanceValid(player) || !player.AddWeapon(_weaponInstance))
			return;
		Collected(player);
		GetNodeOrNull<EventBus>("/root/EventBus")?.EmitSignal(EventBus.SignalName.LootReceived, "weapon", _weaponInstance.Id, 1);
		GD.Print($"[WeaponPickup] {_weaponInstance.Name} ramassée");
	}

	/// <summary>L'arme quitte le sol : son icône part vers le HUD, le repère s'efface.</summary>
	private void Collected(Player player)
	{
		_collected = true;
		Interactables.Unregister(this);
		GetNodeOrNull<EventBus>("/root/EventBus")?.EmitSignal(EventBus.SignalName.WeaponPickedUp,
			_weaponInstance.Id, _visual.GlobalPosition);
		QueueFree();
	}

	private void FlashRefused()
	{
		Color original = _visual.Modulate;
		_visual.Modulate = new Color(1f, 0.3f, 0.2f);
		Tween tween = CreateTween();
		tween.TweenProperty(_visual, "modulate", original, 0.3f).SetDelay(0.1f);
	}

	private void Despawn()
	{
		_collected = true;
		Interactables.Unregister(this);
		Tween tween = CreateTween();
		tween.SetParallel();
		tween.TweenProperty(this, "scale", Vector2.Zero, 0.4f);
		tween.TweenProperty(this, "modulate:a", 0f, 0.4f);
		tween.Chain().TweenCallback(Callable.From(QueueFree));
	}

	private void CreateVisual()
	{
		_ring = new PixelGroundRing { Name = "Ring" };
		AddChild(_ring);
		_ring.Show(UITheme.GoldColor, RingRadius);

		_visualRoot = new Node2D();
		AddChild(_visualRoot);
		// Icône 32×32 à l'échelle 1 : même densité de pixels que les décors et les personnages.
		_visual = new Sprite2D
		{
			Texture = LoadWeaponTexture() ?? CreateFallbackTexture(),
			Centered = true,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			Position = new Vector2(0f, -10f),
		};
		_visualRoot.AddChild(_visual);
	}

	private Texture2D LoadWeaponTexture()
	{
		string spritePath = _weaponInstance?.Sprite;
		if (string.IsNullOrWhiteSpace(spritePath))
			return null;

		string resourcePath = spritePath.StartsWith(ResourcePrefix) ? spritePath : ResourcePrefix + spritePath;
		return ResourceLoader.Exists(resourcePath) ? GD.Load<Texture2D>(resourcePath) : null;
	}

	private static ImageTexture CreateFallbackTexture()
	{
		Image image = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8);
		image.Fill(Colors.Transparent);
		for (int x = 3; x < 13; x++)
		{
			for (int y = 6; y < 10; y++)
				image.SetPixel(x, y, UITheme.GoldColor);
		}
		return ImageTexture.CreateFromImage(image);
	}

	private static bool HasWeapon(Player player, string weaponId)
	{
		foreach (WeaponInstance weapon in player.WeaponSlots)
		{
			if (weapon.Id == weaponId)
				return true;
		}
		return false;
	}
}
