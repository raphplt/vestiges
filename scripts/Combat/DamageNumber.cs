using Godot;

namespace Vestiges.Combat;

public partial class DamageNumber : Node2D
{
	private static readonly RandomNumberGenerator Rng = new();

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

		// Offset latéral aléatoire pour éviter les empilements
		float lateralOffset = Rng.RandfRange(-12f, 12f);
		Position += new Vector2(lateralOffset, 0);

		if (_isCrit)
		{
			label.AddThemeColorOverride("font_color", new Color(1f, 0.75f, 0.1f)); // Or vif
			label.AddThemeFontSizeOverride("font_size", 22);
			label.Text += "!";

			// Scale punch pour les crits
			Scale = new Vector2(1.6f, 0.6f);
		}
		else
		{
			// Taille proportionnelle aux dégâts (petits coups = plus discrets)
			int fontSize = _damage > 30 ? 18 : (_damage > 15 ? 16 : 14);
			label.AddThemeFontSizeOverride("font_size", fontSize);
		}

		Tween tween = CreateTween();
		tween.SetParallel(true);

		float rise = _isCrit ? -55f : -35f;
		tween.TweenProperty(this, "position", Position + new Vector2(0, rise), 0.7f)
			.SetEase(Tween.EaseType.Out)
			.SetTrans(Tween.TransitionType.Quad);

		// Scale punch → retour normal
		if (_isCrit)
		{
			tween.TweenProperty(this, "scale", Vector2.One * 1.1f, 0.1f)
				.SetTrans(Tween.TransitionType.Back)
				.SetEase(Tween.EaseType.Out);
		}

		tween.TweenProperty(this, "modulate:a", 0.0f, 0.5f)
			.SetDelay(0.35f);

		tween.SetParallel(false);
		tween.TweenCallback(Callable.From(QueueFree));
	}
}
