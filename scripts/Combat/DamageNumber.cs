using System;
using Godot;

namespace Vestiges.Combat;

/// <summary>
/// Chiffre de dégâts recyclé par CombatPools (plan 02 J1). En Saira cerné de sombre pour rester lisible sur tous les sols.
/// Les coups normaux rapprochés sur une même cible s'additionnent dans un seul chiffre qui reste en place et
/// pulse à chaque ajout, puis s'envole ; le critique ne se fond jamais : plus gros, doré, suffixé « ! », il jaillit.
/// Un coup renforcé par Débordement jaillit de même, en bleu pâle et préfixé « » », sans se confondre avec un critique.
/// Animé dans _Process, sans tween.
/// </summary>
public partial class DamageNumber : Node2D
{
	private int _shown = -1;
	private LabelSettings _currentSettings;

	private const float PopSec = 0.08f;
	private const float MergeWindowSec = 0.25f;
	private const float MaxHoldSec = 1.0f;
	private const float FloatSec = 0.5f;
	private const float NormalRisePx = 30f;
	private const float CritRisePx = 50f;
	private const float NormalPopScale = 1.35f;
	private static readonly Vector2 CritPopScale = new(1.8f, 0.7f);
	private static readonly Vector2 CritRestScale = new(1.1f, 1.1f);
	private static readonly RandomNumberGenerator Rng = new();

	private static LabelSettings _small;
	private static LabelSettings _medium;
	private static LabelSettings _large;
	private static LabelSettings _crit;
	private static LabelSettings _carried;

	private Label _label;
	private Action<DamageNumber> _release;
	private Vector2 _origin;
	private float _total;
	private float _elapsed;
	private float _sinceHit;
	private bool _isCrit;
	private bool _isCarried;
	private bool Pops => _isCrit || _isCarried;

	/// <summary>Numéro de lancement : un détenteur vérifie que le chiffre n'a pas été recyclé pour une autre cible.</summary>
	public int Serial { get; private set; }

	public void SetRelease(Action<DamageNumber> release)
	{
		_release = release;
	}

	public override void _Ready()
	{
		// Toujours lisible au-dessus des entités triées en Y et du brouillard.
		ZIndex = 30;
		_label = GetNode<Label>("Label");
		if (_small == null)
		{
			Font semiBold = GD.Load<Font>("res://assets/fonts/saira/SairaSemiCondensed-SemiBold.ttf");
			Font bold = GD.Load<Font>("res://assets/fonts/saira/SairaSemiCondensed-Bold.ttf");
			Color normal = new(1f, 0.96f, 0.82f);
			Color outline = new(0.08f, 0.05f, 0.1f);
			_small = Settings(semiBold, 14, normal, outline);
			_medium = Settings(semiBold, 16, normal, outline);
			_large = Settings(bold, 19, new Color(1f, 0.9f, 0.45f), outline);
			_crit = Settings(bold, 24, new Color(1f, 0.74f, 0.12f), new Color(0.35f, 0.05f, 0.02f));
			_carried = Settings(bold, 22, new Color(0.66f, 0.9f, 1f), new Color(0.04f, 0.1f, 0.24f));
		}
		SetProcess(false);
	}

	public void Play(Vector2 position, float damage, bool isCrit, bool isCarried = false)
	{
		Serial++;
		// Décalage latéral aléatoire pour éviter les empilements entre cibles voisines.
		_origin = position + new Vector2(Rng.RandfRange(-12f, 12f), 0f);
		GlobalPosition = _origin;
		_isCrit = isCrit;
		_isCarried = isCarried && !isCrit;
		_total = 0f;
		_shown = -1;
		_elapsed = 0f;
		Modulate = Colors.White;
		Visible = true;
		SetProcess(true);
		Add(damage);
	}

	/// <summary>
	/// Ajoute un coup normal au chiffre s'il appartient encore à cette cible (<paramref name="serial"/>)
	/// et qu'il n'a pas commencé à s'envoler. Faux : le détenteur en lance un nouveau.
	/// </summary>
	public bool TryMerge(int serial, float damage)
	{
		if (serial != Serial || !Visible || Pops || _sinceHit > MergeWindowSec || _elapsed > MaxHoldSec)
			return false;
		Add(damage);
		return true;
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		_elapsed += dt;
		_sinceHit += dt;

		float pop = Mathf.Clamp(_sinceHit / PopSec, 0f, 1f);
		pop = 1f - (1f - pop) * (1f - pop);
		Scale = Pops
			? CritPopScale.Lerp(CritRestScale, pop)
			: Vector2.One * Mathf.Lerp(NormalPopScale, 1f, pop);

		// Tenu en place tant que les coups s'enchaînent, puis s'envole et s'efface.
		float hold = Pops ? PopSec : MergeWindowSec;
		if (_sinceHit < hold)
			return;
		float flight = Mathf.Clamp((_sinceHit - hold) / FloatSec, 0f, 1f);
		float rise = 1f - (1f - flight) * (1f - flight);
		GlobalPosition = _origin + new Vector2(0f, -(Pops ? CritRisePx : NormalRisePx) * rise);
		Modulate = new Color(1f, 1f, 1f, 1f - Mathf.Clamp((flight - 0.3f) / 0.7f, 0f, 1f));
		if (flight >= 1f)
			Finish();
	}

	private void Add(float damage)
	{
		_total += damage;
		_sinceHit = 0f;
		int shown = Mathf.Max(1, (int)_total);
		// Un flux de dégâts fractionnaires conserve sa somme sans reformater le même entier 60 fois/s.
		if (shown != _shown)
		{
			_shown = shown;
			_label.Text = _isCrit ? $"{shown}!" : _isCarried ? $"»{shown}" : shown.ToString();
		}
		LabelSettings settings = _isCrit ? _crit : _isCarried ? _carried : _total > 30f ? _large : _total > 15f ? _medium : _small;
		if (settings != _currentSettings)
		{
			_currentSettings = settings;
			_label.LabelSettings = settings;
		}
	}

	private void Finish()
	{
		Visible = false;
		SetProcess(false);
		_release(this);
	}

	private static LabelSettings Settings(Font font, int size, Color color, Color outline) => new()
	{
		Font = font,
		FontSize = size,
		FontColor = color,
		OutlineSize = 4,
		OutlineColor = outline,
	};
}
