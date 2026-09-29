using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.UI;
using Vestiges.World;

namespace Vestiges.Combat;

/// <summary>
/// Arme lâchée au sol par une créature, ou par un coffre quand les quatre emplacements sont pris. Ramassée d'elle-même
/// si un emplacement est libre ; sinon c'est un lieu activable comme un coffre : l'invite commune propose de
/// l'échanger contre l'arme équipée. Au ramassage, son icône vole jusqu'au HUD (plan 02 J3).
/// </summary>
public partial class WeaponPickup : Area2D, IInteractable
{
	private const float PickupRadius = 30f;
	private const float BobAmplitude = 3f;
	private const float BobSpeed = 2f;
	private const float SpawnScatterSpeed = 80f;
	private const float DespawnTime = 120f;
	private const float RingRadius = 13f;
	private const float SwapHoldSec = 0.25f;
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
	// L'invite traduit sa clé : un texte déjà composé s'affiche tel quel.
	public string PromptVerbKey => _prompt ??= SwapPrompt();
	public float HoldTime => SwapHoldSec;
	public Color GaugeColor => UITheme.GoldBright;

	/// <summary>
	/// L'échange prévient s'il laisserait un perk sans arme compatible (plan 05, B2). L'invite est lue à chaque pas
	/// physique : le texte est gardé jusqu'au prochain changement d'arme ou de perk.
	/// </summary>
	private string SwapPrompt()
	{
		string prompt = string.Format(Tr("WEAPON_SWAP_PROMPT"), _weaponInstance?.Name ?? "");
		if (_weaponInstance == null || _groupCache?.GetPlayer() is not Player { WeaponSlots.Count: >= Player.MaxWeaponSlots } player)
			return prompt;
		string deactivated = Progression.PerkSpecializationOffers.DeactivatedBySwap(player, _weaponInstance);
		return deactivated.Length == 0 ? prompt : string.Format(Tr("WEAPON_SWAP_PERK_WARNING"), prompt, deactivated);
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
		if (_eventBus == null)
			return;
		_eventBus.WeaponInventoryChanged += InvalidatePrompt;
		_eventBus.SpecializationAcquired += OnSpecializationAcquired;
	}

	public override void _ExitTree()
	{
		Interactables.Unregister(this);
		if (_eventBus == null)
			return;
		_eventBus.WeaponInventoryChanged -= InvalidatePrompt;
		_eventBus.SpecializationAcquired -= OnSpecializationAcquired;
	}

	private void InvalidatePrompt() => _prompt = null;

	private void OnSpecializationAcquired(string specializationId) => _prompt = null;

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

	/// <summary>Activation par l'invite : prendre si un emplacement s'est libéré, sinon échanger avec l'arme équipée.</summary>
	public void Interact(Player player)
	{
		if (_collected)
			return;
		if (HasWeapon(player, _weaponInstance.Id))
		{
			FlashRefused();
			return;
		}
		if (player.WeaponSlots.Count < Player.MaxWeaponSlots)
		{
			TryCollect(player);
			return;
		}
		SwapWithEquippedWeapon(player);
	}

	private void OnBodyEntered(Node2D body)
	{
		if (_collected || body is not Player player)
			return;
		// Ramassage automatique seulement s'il reste un emplacement ; sinon l'invite propose l'échange.
		if (player.WeaponSlots.Count < Player.MaxWeaponSlots && !HasWeapon(player, _weaponInstance.Id))
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

	private void SwapWithEquippedWeapon(Player player)
	{
		WeaponInstance removed = player.RemoveWeapon(0);
		if (removed == null)
			return;

		if (!player.AddWeapon(_weaponInstance))
		{
			player.AddWeapon(removed);
			FlashRefused();
			return;
		}

		WeaponPickup dropped = new();
		dropped.Initialize(removed, GlobalPosition + new Vector2((float)GD.RandRange(-18, 18), (float)GD.RandRange(-10, 10)));
		GetTree().CurrentScene.CallDeferred(Node.MethodName.AddChild, dropped);
		Collected(player);
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
