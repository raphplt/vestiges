using Godot;

namespace Vestiges.Combat;

public partial class DamageNumber : Node2D
{
	private float _damage;
	private bool _isCrit;

	public void SetDamage(float damage, bool isCrit = false)
	{
		_damage = damage;
		_isCrit = isCrit;
	}

	public override void _Ready()
	{
		Label label = GetNode<Label>("Label");
		label.Text = ((int)_damage).ToString();

		if (_isCrit)
		{
			label.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.2f));
			label.AddThemeFontSizeOverride("font_size", 18);
			label.Text += "!";
		}

		Tween tween = CreateTween();
		tween.SetParallel(true);
		float rise = _isCrit ? -55f : -40f;
		tween.TweenProperty(this, "position", Position + new Vector2(0, rise), 0.6f)
			.SetEase(Tween.EaseType.Out);
		tween.TweenProperty(this, "modulate:a", 0.0f, 0.6f)
			.SetDelay(0.3f);
		tween.SetParallel(false);
		tween.TweenCallback(Callable.From(QueueFree));
	}
}
