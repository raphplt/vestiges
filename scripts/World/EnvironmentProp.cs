using Godot;

namespace Vestiges.World;

/// <summary>
/// Décor d'environnement non-interactif. Peut être multi-couches :
/// - Base (tronc, rocher, immeuble) : collision optionnelle calée sur sa base visible
/// - Canopée (feuillage, toit) : overlay au-dessus du joueur, pas de collision
///
/// Le nœud est placé au centre de son emprise au sol : le tri en Y de la scène
/// le dessine devant ou derrière le joueur comme en isométrie. Les décors plats
/// (débris, flaques, fleurs) sont des décalques dessinés sous les entités.
/// Quand le joueur passe derrière un décor haut, PropOcclusion le rend semi-transparent.
/// </summary>
public partial class EnvironmentProp : StaticBody2D
{
	private Sprite2D _baseSprite;
	private Sprite2D _canopySprite;
	private PropFootprint _footprint;
	private static ShaderMaterial _swayMaterial;

	/// <summary>
	/// Initialise le prop. La position courante est le centre de la cellule. Avec un manifeste, le pivot au sol
	/// du sprite s'y pose ; sinon le nœud remonte au centre de l'emprise estimée et le sprite redescend d'autant.
	/// </summary>
	public void Initialize(
		Texture2D baseTexture,
		Texture2D canopyTexture,
		float canopyOffsetY,
		bool blocking,
		float footprintScale = 1f)
	{
		PropRules rules = PropRules.Current;
		_footprint = PropFootprint.Of(baseTexture);
		float scale = rules.FootprintScale * footprintScale;
		Vector2 textureSize = baseTexture.GetSize();

		// Décor procédural : le pivot au sol du manifeste tombe sur le centre de la cellule et l'emprise est exacte.
		// Décor dessiné : pieds du sprite 4 px sous le centre de la cellule, emprise déduite des pixels.
		bool hasManifest = PropManifest.TryGet(baseTexture, out PropManifest.Entry manifest) && manifest.Footprint.Length >= 3;
		float sortShift = 0f;
		Vector2[] ground;
		if (hasManifest)
		{
			ground = new Vector2[manifest.Footprint.Length];
			for (int i = 0; i < ground.Length; i++)
				ground[i] = manifest.Footprint[i] * scale;
		}
		else
		{
			float visibleBottomY = 4f + _footprint.VisibleBottom;
			float halfHeight = _footprint.DiamondHalfHeight(scale);
			sortShift = visibleBottomY - halfHeight;
			float halfWidth = Mathf.Max(4f, _footprint.BaseWidth * scale * 0.5f);
			float cx = _footprint.BaseCenterX;
			ground = new[]
			{
				new Vector2(cx, -halfHeight),
				new Vector2(cx + halfWidth, 0f),
				new Vector2(cx, halfHeight),
				new Vector2(cx - halfWidth, 0f),
			};
		}
		Position += new Vector2(0f, sortShift);

		if (_swayMaterial == null && ResourceLoader.Exists("res://assets/shaders/sway.gdshader"))
		{
			Shader swayShader = GD.Load<Shader>("res://assets/shaders/sway.gdshader");
			_swayMaterial = new ShaderMaterial { Shader = swayShader };
			_swayMaterial.SetShaderParameter("speed", 1.0f);
			_swayMaterial.SetShaderParameter("max_strength", 0.05f);
		}

		_baseSprite = new Sprite2D
		{
			Texture = baseTexture,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			Offset = hasManifest
				? textureSize * 0.5f - manifest.Pivot
				: new Vector2(0, -textureSize.Y * 0.5f + 4f - sortShift),
		};
		AddChild(_baseSprite);

		bool blocks = blocking
			&& _footprint.VisibleHeight >= rules.MinBlockingHeight
			&& _footprint.OpaquePixels >= rules.MinBlockingPixels;
		if (blocks)
		{
			AddChild(new CollisionShape2D { Shape = new ConvexPolygonShape2D { Points = ground } });
			CollisionLayer = 4;
		}
		else
		{
			CollisionLayer = 0;
		}
		CollisionMask = 0;

		if (!blocks && canopyTexture == null && _footprint.VisibleHeight <= rules.GroundDecalMaxHeight)
			ZIndex = -1;
		else if (_footprint.OpaquePixels > 0)
			AddChild(PropShadow.Create(ground));

		if (canopyTexture != null)
		{
			_canopySprite = new Sprite2D
			{
				Texture = canopyTexture,
				TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
				Position = new Vector2(0, canopyOffsetY - sortShift),
				ZIndex = 100,
				ZAsRelative = false,
				SelfModulate = new Color(1f, 1f, 1f, 0.85f),
			};
			if (_swayMaterial != null)
				_canopySprite.Material = _swayMaterial;
			AddChild(_canopySprite);
		}
	}

	/// <summary>Rend le prop entier (base + canopée) semi-transparent : 1 opaque, ~0,35 très transparent.</summary>
	public void SetOverallTransparency(float alpha)
	{
		if (_baseSprite != null)
			_baseSprite.SelfModulate = new Color(1f, 1f, 1f, alpha);
		if (_canopySprite != null)
			_canopySprite.SelfModulate = new Color(1f, 1f, 1f, alpha);
	}

	public bool HasCanopy => _canopySprite != null;

	/// <summary>Silhouette visible en coordonnées monde (pour l'occlusion), canopée comprise.</summary>
	public Rect2 VisibleWorldRect()
	{
		Rect2 rect = _baseSprite.GetRect();
		rect.Position += _baseSprite.GlobalPosition;
		if (_canopySprite != null)
		{
			Rect2 canopy = _canopySprite.GetRect();
			canopy.Position += _canopySprite.GlobalPosition;
			rect = rect.Merge(canopy);
		}
		return rect;
	}

	public PropFootprint Footprint => _footprint;
}
