using System;
using Godot;

namespace Vestiges.Combat;

/// <summary>Chiffre de dégâts recyclé par CombatPools : monte, s'efface, retourne au pool.</summary>
public partial class DamageNumber : Node2D
{
	private static readonly RandomNumberGenerator Rng = new();

	private Label _label;
	private Tween _tween;
	private Action<DamageNumber> _release;

	public void SetRelease(Action<DamageNumber> release)
	{
		_release = release;
	}

	public override void _Ready()
	{
		// Toujours lisible au-dessus des entités triées en Y et du brouillard.
		ZIndex = 30;
		_label = GetNode<Label>("Label");
	}

	public void Play(Vector2 position, float damage, bool isCrit)
	{
		// Décalage latéral aléatoire pour éviter les empilements.
		GlobalPosition = position + new Vector2(Rng.RandfRange(-12f, 12f), 0f);
		Visible = true;
		Modulate = Colors.White;
		_label.Text = ((int)damage).ToString();

		if (isCrit)
		{
			_label.AddThemeColorOverride("font_color", new Color(1f, 0.75f, 0.1f));
			_label.AddThemeFontSizeOverride("font_size", 22);
			_label.Text += "!";
			Scale = new Vector2(1.6f, 0.6f);
		}
		else
		{
			// Taille proportionnelle aux dégâts (petits coups = plus discrets).
			_label.AddThemeColorOverride("font_color", new Color(1f, 1f, 0.3f));
			_label.AddThemeFontSizeOverride("font_size", damage > 30 ? 18 : (damage > 15 ? 16 : 14));
			Scale = Vector2.One;
		}

		_tween?.Kill();
		_tween = CreateTween();
		_tween.SetParallel(true);
		float rise = isCrit ? -55f : -35f;
		_tween.TweenProperty(this, "position", Position + new Vector2(0, rise), 0.7f)
			.SetEase(Tween.EaseType.Out)
			.SetTrans(Tween.TransitionType.Quad);
		if (isCrit)
		{
			_tween.TweenProperty(this, "scale", Vector2.One * 1.1f, 0.1f)
				.SetTrans(Tween.TransitionType.Back)
				.SetEase(Tween.EaseType.Out);
		}
		_tween.TweenProperty(this, "modulate:a", 0.0f, 0.5f).SetDelay(0.35f);
		_tween.SetParallel(false);
		_tween.TweenCallback(Callable.From(Finish));
	}

	private void Finish()
	{
		Visible = false;
		_release(this);
	}
}
