using Godot;

namespace Vestiges.Combat;

public partial class DamageNumber : Node2D
{
	private float _damage;

	public void SetDamage(float damage)
	{
		_damage = damage;
	}

	public override void _Ready()
	{
		Label label = GetNode<Label>("Label");
		label.Text = ((int)_damage).ToString();

		Tween tween = CreateTween();
		tween.SetParallel(true);
		tween.TweenProperty(this, "position", Position + new Vector2(0, -40), 0.6f)
			.SetEase(Tween.EaseType.Out);
		tween.TweenProperty(this, "modulate:a", 0.0f, 0.6f)
			.SetDelay(0.3f);
		tween.SetParallel(false);
		tween.TweenCallback(Callable.From(QueueFree));
	}
}
