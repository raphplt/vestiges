using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Petit lieu (plan 22, lots C1 et C4) : un décor déjà posé qui se souvient. Il n'a pas de sprite propre ; il porte
/// l'interaction, un signe discret au sol, et son état. <see cref="SmallPlaceDirector"/> le place, montre son signe
/// et donne sa récompense.
/// </summary>
public partial class SmallPlace : Node2D, IInteractable
{
	// Plus large que le pied du décor, pour que la lueur déborde de dessous le sprite.
	private const float SignRadius = 42f;
	private const float SignHeight = 34f;
	private const float LineSeconds = 5f;
	private const float LineRise = 18f;
	private const int LineZIndex = 30;

	private static readonly List<SmallPlace> _revealed = new();

	private SmallPlaceDirector _director;
	private InteractableAura _sign;
	private Label _line;
	private float _lineTime = -1f;

	/// <summary>Lieux révélés par une cabine téléphonique : les flèches de bord d'écran les montrent (plan 22 C4).</summary>
	public static IReadOnlyList<SmallPlace> Revealed => _revealed;
	/// <summary>Temps de jeu restant avant que la révélation s'éteigne ; tenu par le directeur.</summary>
	public float RevealRemaining { get; set; }

	public SmallPlaceData Data { get; private set; }
	/// <summary>Famille d'effets des étincelles du lieu, résolue une fois.</summary>
	public Combat.FxFamily Family { get; private set; }
	public bool Used { get; private set; }
	public bool Lost { get; private set; }

	public bool CanInteract => !Used && !Lost;
	public Vector2 InteractPosition => GlobalPosition;
	public Vector2 PromptPosition => GlobalPosition + new Vector2(0f, -SignHeight - 8f);
	public string PromptVerbKey => Data.PromptKey;
	public float HoldTime => Data.HoldSeconds;
	public Color GaugeColor => Data.Color;

	public void Initialize(SmallPlaceData data, SmallPlaceDirector director)
	{
		Data = data;
		Family = Combat.PixelPalette.ParseFamily(data.Family, Combat.FxFamily.Essence);
		_director = director;
		_sign = new InteractableAura { Name = "Sign" };
		AddChild(_sign);
		_sign.Configure(data.Color, data.Color.Lightened(0.25f), SignRadius, SignHeight, withMote: true, pulseSpeed: 1.6f,
			baseAlpha: 0.3f, pulseAlpha: 0.12f, crownAlpha: 0.14f, crownPulseAlpha: 0.08f);
		_sign.SetActive(false);
	}

	public override void _Ready() => SetProcess(false);

	public override void _EnterTree() => Interactables.Register(this);

	public override void _ExitTree()
	{
		Interactables.Unregister(this);
		_revealed.Remove(this);
	}

	/// <summary>Montre le lieu aux flèches de bord d'écran pendant <paramref name="seconds"/>.</summary>
	public void Reveal(float seconds)
	{
		if (!CanInteract)
			return;
		RevealRemaining = Mathf.Max(RevealRemaining, seconds);
		if (!_revealed.Contains(this))
			_revealed.Add(this);
	}

	public void Unreveal()
	{
		RevealRemaining = 0f;
		_revealed.Remove(this);
	}

	/// <summary>Une ligne de lore qui monte au-dessus du décor et s'efface (Table de pique-nique).</summary>
	public void ShowLine(string text)
	{
		if (_line == null)
		{
			_line = new Label { ZIndex = LineZIndex, HorizontalAlignment = HorizontalAlignment.Center, Scale = Vector2.One * 0.5f };
			_line.AddThemeFontOverride("font", GD.Load<Font>("res://assets/fonts/saira/SairaSemiCondensed-SemiBold.ttf"));
			_line.AddThemeFontSizeOverride("font_size", 18);
			_line.AddThemeColorOverride("font_color", new Color(0.93f, 0.9f, 0.84f));
			_line.AddThemeColorOverride("font_outline_color", new Color(0.12f, 0.1f, 0.1f, 0.85f));
			_line.AddThemeConstantOverride("outline_size", 6);
			AddChild(_line);
		}
		_line.Text = text;
		_line.ResetSize();
		_lineTime = 0f;
		_line.Visible = true;
		SetProcess(true);
	}

	public override void _Process(double delta)
	{
		_lineTime += (float)delta;
		float progress = _lineTime / LineSeconds;
		if (progress >= 1f)
		{
			_line.Visible = false;
			SetProcess(false);
			return;
		}
		_line.Position = new Vector2(-_line.Size.X * _line.Scale.X * 0.5f, -SignHeight - 24f - LineRise * progress);
		_line.Modulate = new Color(1f, 1f, 1f, progress < 0.8f ? 1f : (1f - progress) / 0.2f);
	}

	/// <summary>Le signe ne se montre qu'à portée du joueur, tant que le lieu peut encore servir.</summary>
	public void ShowSign(bool visible) => _sign.SetActive(visible && CanInteract);

	public bool SignVisible => _sign.Visible;

	public void Interact(Player player)
	{
		if (!CanInteract)
			return;
		Used = true;
		Interactables.Unregister(this);
		Unreveal();
		_sign.SetActive(false);
		_director.Reward(this, player);
	}

	/// <summary>La zone du lieu est passée au Néant : il s'éteint pour de bon.</summary>
	public void Lose()
	{
		Lost = true;
		Interactables.Unregister(this);
		Unreveal();
		_sign.SetActive(false);
	}
}
