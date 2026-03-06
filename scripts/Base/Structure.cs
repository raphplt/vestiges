using Godot;
using Vestiges.Core;

namespace Vestiges.Base;

/// <summary>
/// Classe de base pour toutes les structures placées par le joueur.
/// Gère les HP, les dégâts, la destruction et le feedback visuel.
/// Utilise un sprite pixel art quand disponible, sinon rendu isométrique procédural.
/// </summary>
public partial class Structure : StaticBody2D
{
	protected float BaseMaxHp;
	protected float MaxHp;
	protected float CurrentHp;
	protected string StructureId;
	protected string RecipeId;
	protected Vector2I GridPosition;
	protected Polygon2D Visual;
	protected Polygon2D LeftFace;
	protected Polygon2D RightFace;
	protected Sprite2D SpriteVisual;
	protected Color OriginalColor;
	protected bool UsesSprite;

	public bool IsDestroyed => CurrentHp <= 0;
	public float HpRatio => MaxHp > 0 ? CurrentHp / MaxHp : 0;
	public string Recipe => RecipeId;

	public override void _Ready()
	{
		Visual = GetNodeOrNull<Polygon2D>("Visual");
		LeftFace = GetNodeOrNull<Polygon2D>("LeftFace");
		RightFace = GetNodeOrNull<Polygon2D>("RightFace");
		SpriteVisual = GetNodeOrNull<Sprite2D>("SpriteVisual");
		AddToGroup("structures");
	}

	public virtual void Initialize(string recipeId, string structureId, float maxHp, Vector2I gridPos, Color color)
	{
		RecipeId = recipeId;
		StructureId = structureId;
		BaseMaxHp = maxHp;
		MaxHp = maxHp;
		CurrentHp = maxHp;
		GridPosition = gridPos;
		OriginalColor = color;

		if (UsesSprite && SpriteVisual != null)
		{
			// Cacher les polygones quand on utilise un sprite
			if (Visual != null) Visual.Visible = false;
			if (LeftFace != null) LeftFace.Visible = false;
			if (RightFace != null) RightFace.Visible = false;
		}
		else
		{
			if (Visual != null)
				Visual.Color = color;
			if (LeftFace != null)
				LeftFace.Color = color.Darkened(0.3f);
			if (RightFace != null)
				RightFace.Color = color.Darkened(0.5f);
		}
	}

	/// <summary>Charge un sprite pour la structure. Appelé par StructurePlacer avant Initialize.</summary>
	public bool TrySetSprite(string spritePath)
	{
		if (string.IsNullOrEmpty(spritePath))
			return false;

		string resPath = spritePath.StartsWith("res://") ? spritePath : $"res://{spritePath}";
		if (!ResourceLoader.Exists(resPath))
		{
			GD.PushWarning($"[Structure] Sprite not found: {resPath}, using polygon fallback");
			return false;
		}

		Texture2D texture = GD.Load<Texture2D>(resPath);
		if (texture == null)
			return false;

		SpriteVisual = new Sprite2D
		{
			Name = "SpriteVisual",
			Texture = texture,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			Offset = new Vector2(0, -texture.GetHeight() * 0.5f + 4)
		};
		AddChild(SpriteVisual);
		UsesSprite = true;
		return true;
	}

	public virtual void TakeDamage(float damage)
	{
		if (IsDestroyed)
			return;

		CurrentHp -= damage;
		HitFlash();
		UpdateVisualDamage();

		if (CurrentHp <= 0)
		{
			CurrentHp = 0;
			OnDestroyed();
		}
	}

	public void Repair(float amount)
	{
		if (IsDestroyed)
			return;

		CurrentHp = Mathf.Min(CurrentHp + amount, MaxHp);
		UpdateVisualDamage();
	}

	/// <summary>Augmente les HP max pour la nuit (basé sur BaseMaxHp, non cumulatif).</summary>
	public void FortifyForNight(float nightScale)
	{
		if (IsDestroyed)
			return;

		float ratio = HpRatio;
		MaxHp = BaseMaxHp * nightScale;
		CurrentHp = MaxHp * ratio;
		UpdateVisualDamage();
	}

	/// <summary>Repare un pourcentage des HP max.</summary>
	public void RepairPercent(float percent)
	{
		if (IsDestroyed)
			return;

		CurrentHp = Mathf.Min(CurrentHp + MaxHp * percent, MaxHp);
		UpdateVisualDamage();
	}

	protected virtual void OnDestroyed()
	{
		RemoveFromGroup("structures");

		EventBus eventBus = GetNode<EventBus>("/root/EventBus");
		eventBus.EmitSignal(EventBus.SignalName.StructureDestroyed, StructureId, GlobalPosition);

		Tween tween = CreateTween();
		tween.SetParallel();
		tween.TweenProperty(this, "scale", Vector2.One * 0.3f, 0.3f);
		tween.TweenProperty(this, "modulate:a", 0f, 0.3f);
		tween.Chain().TweenCallback(Callable.From(() => QueueFree()));
	}

	private void HitFlash()
	{
		if (UsesSprite && SpriteVisual != null)
		{
			SpriteVisual.Modulate = new Color(10f, 10f, 10f, 1f);
			Tween spriteTween = CreateTween();
			spriteTween.TweenProperty(SpriteVisual, "modulate", Colors.White, 0.15f)
				.SetDelay(0.05f);
		}
		else
		{
			if (Visual != null) Visual.Color = Colors.White;
			if (LeftFace != null) LeftFace.Color = Colors.White;
			if (RightFace != null) RightFace.Color = Colors.White;

			Tween tween = CreateTween();
			if (Visual != null)
			{
				tween.TweenProperty(Visual, "color", OriginalColor, 0.15f)
					.SetDelay(0.05f);
			}

			if (LeftFace != null)
			{
				Tween leftTween = CreateTween();
				leftTween.TweenProperty(LeftFace, "color", OriginalColor.Darkened(0.3f), 0.15f)
					.SetDelay(0.05f);
			}
			if (RightFace != null)
			{
				Tween rightTween = CreateTween();
				rightTween.TweenProperty(RightFace, "color", OriginalColor.Darkened(0.5f), 0.15f)
					.SetDelay(0.05f);
			}
		}
	}

	private void UpdateVisualDamage()
	{
		float ratio = HpRatio;
		float alpha = Mathf.Lerp(0.4f, 1f, ratio);
		Modulate = new Color(1f, 1f, 1f, alpha);
	}
}
