using System;
using System.Collections.Generic;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.Combat;

/// <summary>
/// Levée de la Barrière (plan 07 B2) en trois temps.
/// 1. Présage : le sol se fend le long de la future grille, du centre vers les bords ; la lumière des cadenas filtre.
/// 2. Surgissement : chaque pièce sort de la fissure par son bas, en vague du centre vers les ailes, dépasse un peu sa
///    hauteur et retombe, en tremblant ; terre et pierre jaillissent à sa base.
/// 3. Verrouillage : quand le cœur est debout, les cadenas s'allument d'un coup et les chaînes des ailes se tendent ;
///    la grille arrête alors le joueur et attaque.
/// Une pièce n'est dessinée que pour sa part sortie de terre (région du sprite), sans shader ni nœud de découpe.
/// </summary>
public partial class BarrierRise : Node
{
	private const float JitterHz = 30f;
	private const float LockGlowSec = 0.45f;
	private const float TautSec = 0.35f;
	private const float TautStretch = 0.12f;
	private const float DustInterval = 0.05f;
	// Au-delà, le front de la fissure est hors de l'écran : pas de poussière à dépenser.
	private const float DustReach = 700f;

	public enum PieceKind { Span, Leaf, Pillar, Wing }

	private sealed class Piece
	{
		public Sprite2D Sprite;
		public PieceKind Kind;
		public float Start;
		public bool Emerged;
		public bool Settled;
		public bool Core;
		// Taille et pivot de la dernière texture vue : relus seulement si la Barrière en change (battant entamé).
		public Texture2D Texture;
		public Vector2 Size;
		public Vector2 Pivot;
	}

	private readonly List<Piece> _pieces = new();
	private BarrierConfig _config;
	private BarrierCrack _crack;
	private Vector2 _center;
	private Vector2 _axis;
	private float _dustTimer;
	private float _crackLength;
	private float _coreDone;
	private float _allDone;
	private float _time;
	private bool _locked;
	private Action _onLocked;

	public bool IsLocked => _locked;

	public void Setup(BarrierConfig config, BarrierCrack crack, Vector2 center, Vector2 axis, float crackLength, Action onLocked)
	{
		_config = config;
		_crack = crack;
		_center = center;
		_axis = axis;
		_crackLength = crackLength;
		_onLocked = onLocked;
		_crack.SetState(0f, 1f, 0f);
	}

	/// <summary>
	/// Une pièce à faire surgir, cachée sous terre d'ici là. <paramref name="core"/> : battant, travée fixe ou pilier, dont
	/// la levée conditionne le verrouillage ; les ailes, longues, finissent de sortir hors de l'écran.
	/// </summary>
	public void Add(Sprite2D sprite, PieceKind kind, bool core)
	{
		Piece piece = new()
		{
			Sprite = sprite, Kind = kind, Core = core,
			Start = _config.RiseOmenSec + sprite.Position.DistanceTo(_center) / _config.RiseWaveSpeed,
		};
		Bury(piece, 1f);
		_pieces.Add(piece);
		float end = piece.Start + _config.RisePieceSec;
		if (core)
			_coreDone = Mathf.Max(_coreDone, end);
		_allDone = Mathf.Max(_allDone, end + TautSec);
	}

	public override void _Process(double delta)
	{
		_time += (float)delta;
		float glow = _locked ? Mathf.Max(0.25f, 1f - (_time - _coreDone) / LockGlowSec) : 0.75f + 0.25f * Mathf.Sin(_time * 18f);
		float reach = Mathf.Min(_crackLength, _time * _config.RiseCrackSpeed);
		// L'entaille s'ouvre pendant le présage, puis reste ouverte sous la grille.
		float open = Mathf.Clamp(_time / Mathf.Max(0.01f, _config.RiseOmenSec), 0f, 1f);
		_crack.SetState(reach, glow, open);
		SpitDust(reach, (float)delta);
		foreach (Piece piece in _pieces)
			Animate(piece);
		if (!_locked && _time >= _coreDone)
			Lock();
		if (_locked && _time >= _allDone)
			SetProcess(false);
	}

	private void Animate(Piece piece)
	{
		if (piece.Settled)
			return;
		float progress = (_time - piece.Start) / _config.RisePieceSec;
		if (progress <= 0f)
			return;
		if (!piece.Emerged)
		{
			piece.Emerged = true;
			Erupt(piece);
		}
		if (progress >= 1f)
		{
			float since = _time - piece.Start - _config.RisePieceSec;
			if (piece.Kind != PieceKind.Wing || since >= TautSec)
			{
				// Posée : le sprite redevient entier, et la Barrière peut en changer (battant entamé, brisé).
				piece.Settled = true;
				Bury(piece, 0f);
				piece.Sprite.RegionEnabled = false;
			}
			else if (_locked)
				Taut(piece, since / TautSec);
			else
				Bury(piece, 0f);
			return;
		}
		// Sortie avec un dépassement : la pièce jaillit un peu trop haut puis se pose.
		Bury(piece, 1f - GroundReveal.EaseOutBack(progress, _config.RiseOvershoot));
		// Tremblement au pixel pendant la sortie : la pièce force le sol.
		piece.Sprite.Offset += new Vector2(Mathf.Sin((_time + piece.Start) * JitterHz) >= 0f ? 1f : -1f, 0f);
	}

	/// <summary>Enfonce la pièce (<see cref="GroundReveal.Show"/>) : 1 = sous terre, 0 = debout, négatif = dépassement.</summary>
	private static void Bury(Piece piece, float sunk)
	{
		// Un battant peut être entamé pendant sa levée : taille et pivot suivent sa texture, relus à son changement seulement.
		Texture2D texture = piece.Sprite.Texture;
		if (texture != piece.Texture)
		{
			piece.Texture = texture;
			piece.Size = texture.GetSize();
			piece.Pivot = World.PropManifest.TryGet(texture, out World.PropManifest.Entry entry) ? entry.Pivot : piece.Size * new Vector2(0.5f, 1f);
		}
		GroundReveal.Show(piece.Sprite, piece.Size, piece.Pivot, sunk);
		piece.Sprite.Scale = Vector2.One;
	}

	/// <summary>La chaîne d'une aile se tend : un rebond vertical bref, une fois la grille verrouillée.</summary>
	private static void Taut(Piece piece, float progress)
	{
		Bury(piece, 0f);
		float stretch = TautStretch * Mathf.Sin(Mathf.Clamp(progress, 0f, 1f) * Mathf.Pi) * (1f - progress);
		piece.Sprite.Scale = new Vector2(1f, 1f + stretch);
	}

	/// <summary>
	/// Pendant que la fissure court, la terre gicle à son front, des deux côtés, tant qu'il est dans le champ.
	/// </summary>
	private void SpitDust(float reach, float delta)
	{
		if (reach >= _crackLength || reach > DustReach)
			return;
		_dustTimer -= delta;
		if (_dustTimer > 0f)
			return;
		_dustTimer = DustInterval;
		for (int side = -1; side <= 1; side += 2)
		{
			Vector2 front = _center + _axis * reach * side;
			CombatPools.Instance?.EmitSparks(front, new SparkBurst
			{
				Family = FxFamily.Stone, Owner = FxOwner.World, Count = 3, Direction = Vector2.Up, Spread = 1.6f,
				SpeedMin = 20f, SpeedMax = 55f, LifeMin = 0.25f, LifeMax = 0.45f, Ballistic = true, Size = 1, Decorative = true,
			});
		}
	}

	/// <summary>Terre et pierre jaillissent au pied de la pièce qui sort ; un battant ou un pilier secoue l'écran.</summary>
	private void Erupt(Piece piece)
	{
		Vector2 foot = piece.Sprite.Position;
		// Les ailes lointaines sortent hors de l'écran : pas de débris à dépenser pour elles.
		if (foot.DistanceTo(_center) > DustReach)
			return;
		bool heavy = piece.Kind is PieceKind.Leaf or PieceKind.Pillar;
		CombatPools.Instance?.EmitSparks(foot, new SparkBurst
		{
			Family = FxFamily.Stone,
			Owner = FxOwner.World,
			Count = heavy ? 14 : 5,
			Direction = Vector2.Up,
			Spread = 1.5f,
			SpeedMin = 40f,
			SpeedMax = heavy ? 140f : 75f,
			LifeMin = 0.35f,
			LifeMax = 0.7f,
			Ballistic = true,
			Size = heavy ? 2 : 1,
			Decorative = true,
		});
		CombatPools.Instance?.EmitKnockbackDust(foot, Vector2.Down);
		if (piece.Kind == PieceKind.Pillar)
			ScreenShake.Instance?.ShakeLight();
	}

	/// <summary>
	/// Le cœur est debout : le verrou claque et la grille devient un mur ; la Barrière allume ses cadenas (leur flash est
	/// le sien, pour qu'un coup reçu l'interrompe).
	/// </summary>
	private void Lock()
	{
		_locked = true;
		AudioManager.Play(_config.LockAudio, 0.02f);
		ScreenShake.Instance?.ShakeMedium();
		_onLocked?.Invoke();
	}

	/// <summary>Fin du combat pendant la levée : plus rien ne surgit ni ne se verrouille.</summary>
	public void Cancel()
	{
		_locked = true;
		_onLocked = null;
		SetProcess(false);
	}
}
