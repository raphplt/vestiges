using Godot;
using Vestiges.Core;

namespace Vestiges.Base;

/// <summary>
/// Classe de base pour toutes les structures placées par le joueur.
/// Gère les HP, les dégâts, la destruction et le feedback visuel.
/// Rendu isométrique : face du dessus (Visual) + faces latérales (LeftFace, RightFace).
/// </summary>
public partial class Structure : StaticBody2D
{
	protected float MaxHp;
	protected float CurrentHp;
	protected string StructureId;
	protected string RecipeId;
	protected Vector2I GridPosition;
	protected Polygon2D Visual;
	protected Polygon2D LeftFace;
	protected Polygon2D RightFace;
	protected Color OriginalColor;

	public bool IsDestroyed => CurrentHp <= 0;
	public float HpRatio => MaxHp > 0 ? CurrentHp / MaxHp : 0;
	public string Recipe => RecipeId;

	public override void _Ready()
	{
		Visual = GetNode<Polygon2D>("Visual");
		LeftFace = GetNodeOrNull<Polygon2D>("LeftFace");
		RightFace = GetNodeOrNull<Polygon2D>("RightFace");
		AddToGroup("structures");
	}

	public virtual void Initialize(string recipeId, string structureId, float maxHp, Vector2I gridPos, Color color)
	{
		RecipeId = recipeId;
		StructureId = structureId;
		MaxHp = maxHp;
		CurrentHp = maxHp;
		GridPosition = gridPos;

		Visual.Color = color;
		OriginalColor = color;

		// Side faces : couleurs plus sombres pour l'effet 3D
		if (LeftFace != null)
			LeftFace.Color = color.Darkened(0.3f);
		if (RightFace != null)
			RightFace.Color = color.Darkened(0.5f);
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
		Visual.Color = Colors.White;
		if (LeftFace != null)
			LeftFace.Color = Colors.White;
		if (RightFace != null)
			RightFace.Color = Colors.White;

		Tween tween = CreateTween();
		tween.TweenProperty(Visual, "color", OriginalColor, 0.15f)
			.SetDelay(0.05f);

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

	private void UpdateVisualDamage()
	{
		float ratio = HpRatio;
		float alpha = Mathf.Lerp(0.4f, 1f, ratio);
		Modulate = new Color(1f, 1f, 1f, alpha);
	}
}
