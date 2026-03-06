using Godot;

namespace Vestiges.World;

/// <summary>
/// Décor d'environnement non-interactif. Peut être multi-couches :
/// - Base (tronc, rocher, etc.) : avec collision optionnelle
/// - Canopée (feuillage, toit) : overlay au-dessus du joueur, pas de collision
/// Les petits props (fleurs, champignons) n'ont ni canopée ni collision.
///
/// Quand le joueur se retrouve derrière le prop (position Y plus haute),
/// le prop entier devient semi-transparent pour garder le joueur visible.
/// C'est la technique standard en jeu isométrique 2D.
/// </summary>
public partial class EnvironmentProp : StaticBody2D
{
	private Sprite2D _baseSprite;
	private Sprite2D _canopySprite;
	private float _baseHeight;

	/// <summary>
	/// Initialise le prop avec ses textures et paramètres.
	/// </summary>
	public void Initialize(
		Texture2D baseTexture,
		Texture2D canopyTexture,
		float canopyOffsetY,
		float collisionRadius,
		float collisionOffsetY)
	{
		_baseHeight = baseTexture.GetHeight();

		// --- Sprite de base (tronc, rocher, structure) ---
		_baseSprite = new Sprite2D
		{
			Texture = baseTexture,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			// Ancrer le sprite en bas pour que le pied soit au GlobalPosition
			Offset = new Vector2(0, -_baseHeight * 0.5f + 4),
		};
		AddChild(_baseSprite);

		// --- Collision (optionnelle, radius > 0) ---
		if (collisionRadius > 0)
		{
			CircleShape2D shape = new()
			{
				Radius = collisionRadius,
			};
			CollisionShape2D collider = new()
			{
				Shape = shape,
				Position = new Vector2(0, collisionOffsetY),
			};
			AddChild(collider);

			CollisionLayer = 4;
			CollisionMask = 0;
		}
		else
		{
			CollisionLayer = 0;
			CollisionMask = 0;
		}

		// --- Canopée (overlay au-dessus du joueur) ---
		if (canopyTexture != null)
		{
			_canopySprite = new Sprite2D
			{
				Texture = canopyTexture,
				TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
				Position = new Vector2(0, canopyOffsetY),
				ZIndex = 100,
				ZAsRelative = false,
				SelfModulate = new Color(1f, 1f, 1f, 0.85f),
			};
			AddChild(_canopySprite);
		}
	}

	/// <summary>
	/// Rend le prop entier (base + canopée) semi-transparent.
	/// Appelé par PropSpawner quand le joueur est derrière le prop.
	/// alpha=1 → opaque, alpha~0.35 → très transparent.
	/// </summary>
	public void SetOverallTransparency(float alpha)
	{
		if (_baseSprite != null)
			_baseSprite.SelfModulate = new Color(1f, 1f, 1f, alpha);
		if (_canopySprite != null)
			_canopySprite.SelfModulate = new Color(1f, 1f, 1f, alpha);
	}

	public bool HasCanopy => _canopySprite != null;

	/// <summary>Hauteur du sprite de base en pixels (pour la détection "derrière").</summary>
	public float BaseHeight => _baseHeight;

	public Vector2 CanopyWorldPosition => _canopySprite != null
		? GlobalPosition + _canopySprite.Position
		: GlobalPosition;
}
