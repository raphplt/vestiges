using System.Collections.Generic;
using Godot;
using Vestiges.Combat.Abilities;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Combat;

/// <summary>
/// Attaques de la Barrière (plan 07 B2c), chacune annoncée au sol :
/// - les poings frappent la position du joueur près de la grille (punit l'immobilité) ;
/// - la chaîne balaie un arc devant le battant le plus proche (punit le va-et-vient mécanique) : on sort de l'arc vers
///   l'arrière, ou on franchit la chaîne au dash ;
/// - le verrou : dès qu'un battant est tombé, chaque poing est suivi d'un second sur la position anticipée.
/// </summary>
public sealed class BarrierAttacks
{
	private const float FistShowSec = 0.35f;
	private const float FlashSec = 0.18f;

	private sealed class Strike
	{
		public GroundTelegraph Marker;
		public Sprite2D Fist;
		public Vector2 Center;
		public float Delay;
		public float Timer;
		public float Flash;
		public float Shown;
		public bool Armed;
		public bool Pending;
	}

	private readonly string _causeId;
	private readonly Barrier _barrier;
	private readonly BarrierConfig _config;
	private readonly FxFamily _family;
	private readonly List<Strike> _strikes = new();
	private readonly GroundTelegraph _chainArea;
	private readonly GroundTelegraph _chainLine;
	private readonly Texture2D _fistTexture;
	private EventBus _eventBus;
	private float _fistTimer;
	private float _chainTimer;
	private float _chainWarning;
	private float _chainSweep;
	private float _chainStart;
	private float _chainEnd;
	private Vector2 _chainCenter;
	private bool _chainHit;

	/// <summary>Coups portés au joueur depuis la levée (mesure).</summary>
	public int FistHits { get; private set; }
	public int ChainHits { get; private set; }
	public int FistsLaunched { get; private set; }
	public int ChainsLaunched { get; private set; }

	public BarrierAttacks(Barrier barrier, BarrierConfig config, string suffix)
	{
		_barrier = barrier;
		_config = config;
		_causeId = EnemyGrammar.BossCausePrefix + config.NameKey;
		_family = PixelPalette.ParseFamily(config.AttackFamily, FxFamily.Rust);
		_fistTimer = config.FirstAttackDelay;
		_chainTimer = config.FirstAttackDelay + config.ChainInterval * 0.5f;
		_chainArea = new GroundTelegraph { Name = "ChainArea" };
		_chainLine = new GroundTelegraph { Name = "ChainLine" };
		barrier.AddChild(_chainArea);
		barrier.AddChild(_chainLine);
		string fistPath = $"{Barrier.AssetFolder}/barrier_fist_gantelet_{(suffix == "h" ? "s" : "e")}.png";
		_fistTexture = ResourceLoader.Exists(fistPath) ? GD.Load<Texture2D>(fistPath) : null;
	}

	/// <summary>Un tick de combat ; <paramref name="locked"/> dès qu'un battant est tombé.</summary>
	public void Tick(Player player, float delta, bool locked)
	{
		_eventBus ??= _barrier.GetNode<EventBus>("/root/EventBus");
		TickStrikes(player, delta);
		TickChain(player, delta);
		if (player.IsDead)
			return;

		_fistTimer -= delta;
		if (_fistTimer <= 0f)
		{
			_fistTimer = _config.FistInterval;
			if (_barrier.DistanceToCore(player.GlobalPosition) <= _config.FistRange)
			{
				Launch(player.GlobalPosition, 0f);
				if (locked)
				{
					Vector2 lead = player.Velocity * _config.LockLead;
					Launch(player.GlobalPosition + lead, _config.LockDelay);
				}
			}
		}

		_chainTimer -= delta;
		if (_chainTimer <= 0f && _chainWarning <= 0f && _chainSweep <= 0f)
		{
			_chainTimer = _config.ChainInterval;
			if (_barrier.NearestStandingLeaf(player.GlobalPosition) is Vector2 leaf
				&& Iso.GroundDistanceSquared(player.GlobalPosition, leaf) <= _config.ChainRadius * _config.ChainRadius)
				BeginChain(player, leaf);
		}
	}

	/// <summary>Fin du combat : les annonces en cours s'effacent sans frapper.</summary>
	public void Stop()
	{
		foreach (Strike strike in _strikes)
		{
			strike.Pending = strike.Armed = false;
			strike.Marker.HideMarker();
			strike.Fist.Visible = false;
		}
		_chainWarning = _chainSweep = 0f;
		_chainArea.HideMarker();
		_chainLine.HideMarker();
	}

	private void Launch(Vector2 center, float delay)
	{
		Strike strike = null;
		foreach (Strike candidate in _strikes)
		{
			if (!candidate.Pending && !candidate.Armed && candidate.Flash <= 0f && candidate.Shown <= 0f)
			{
				strike = candidate;
				break;
			}
		}
		if (strike == null)
		{
			strike = new Strike
			{
				Marker = new GroundTelegraph { Name = "FistMarker" },
				Fist = new Sprite2D { Centered = false, Visible = false, Texture = _fistTexture },
			};
			_barrier.AddChild(strike.Marker);
			_barrier.AddChild(strike.Fist);
			if (_fistTexture != null)
				strike.Fist.Offset = PropPivot(_fistTexture);
			_strikes.Add(strike);
		}
		strike.Center = center;
		strike.Delay = delay;
		strike.Timer = _config.FistWarning;
		strike.Pending = delay > 0f;
		strike.Armed = delay <= 0f;
		if (strike.Armed)
			ShowStrike(strike);
	}

	private void ShowStrike(Strike strike)
	{
		strike.Marker.ShowCircle(strike.Center, _config.FistRadius, _family);
		AudioManager.Play(_config.FistWarningAudio, 0.08f);
		FistsLaunched++;
	}

	private void TickStrikes(Player player, float delta)
	{
		foreach (Strike strike in _strikes)
		{
			if (strike.Pending)
			{
				strike.Delay -= delta;
				if (strike.Delay <= 0f)
				{
					strike.Pending = false;
					strike.Armed = true;
					ShowStrike(strike);
				}
				continue;
			}
			if (strike.Armed)
			{
				strike.Timer -= delta;
				strike.Marker.SetProgress(1f - strike.Timer / _config.FistWarning);
				if (strike.Timer <= 0f)
					Resolve(strike, player);
				continue;
			}
			if (strike.Flash > 0f)
			{
				strike.Flash -= delta;
				if (strike.Flash <= 0f)
					strike.Marker.HideMarker();
				else
					strike.Marker.SetFlash(strike.Flash / FlashSec);
			}
			if (strike.Shown > 0f)
			{
				strike.Shown -= delta;
				strike.Fist.Modulate = Colors.White with { A = Mathf.Clamp(strike.Shown / FistShowSec * 2f, 0f, 1f) };
				if (strike.Shown <= 0f)
					strike.Fist.Visible = false;
			}
		}
	}

	private void Resolve(Strike strike, Player player)
	{
		strike.Armed = false;
		strike.Flash = FlashSec;
		strike.Marker.SetFlash(1f);
		strike.Fist.Position = strike.Center;
		strike.Fist.Modulate = Colors.White;
		strike.Fist.Visible = _fistTexture != null;
		strike.Shown = FistShowSec;
		AudioManager.Play(_config.FistImpactAudio, 0.08f);
		ScreenShake.Instance?.ShakeLight();
		CombatPools.Instance?.EmitKnockbackDust(strike.Center, Vector2.Up);
		float reach = _config.FistRadius + _config.FistHitMargin;
		if (GodotObject.IsInstanceValid(player) && Iso.GroundDistanceSquared(player.GlobalPosition, strike.Center) <= reach * reach
			&& HitPlayer(player, _config.FistDamage, strike.Center))
			FistHits++;
	}

	private void BeginChain(Player player, Vector2 leaf)
	{
		_chainCenter = leaf;
		_chainWarning = _config.ChainWarning;
		_chainHit = false;
		// L'arc est centré sur la normale de la grille côté joueur ; il part d'un bord, au hasard, vers l'autre.
		Vector2 normal = _barrier.NormalToward(player.GlobalPosition);
		float half = Mathf.DegToRad(_config.ChainArcDeg * 0.5f);
		float facing = Iso.ToGround(normal).Angle();
		bool clockwise = RunRandom.Behavior.Randf() < 0.5f;
		_chainStart = facing + (clockwise ? -half : half);
		_chainEnd = facing + (clockwise ? half : -half);
		_chainArea.ShowCircle(leaf, _config.ChainRadius, _family);
		AudioManager.Play(_config.ChainWarningAudio, 0.06f);
		ChainsLaunched++;
	}

	private void TickChain(Player player, float delta)
	{
		if (_chainWarning > 0f)
		{
			_chainWarning -= delta;
			_chainArea.SetProgress(1f - _chainWarning / _config.ChainWarning);
			if (_chainWarning <= 0f)
			{
				_chainArea.HideMarker();
				_chainSweep = _config.ChainSweep;
				AudioManager.Play(_config.ChainSweepAudio, 0.06f);
			}
			return;
		}
		if (_chainSweep <= 0f)
			return;
		_chainSweep -= delta;
		float progress = 1f - Mathf.Max(0f, _chainSweep) / _config.ChainSweep;
		float angle = Mathf.LerpAngle(_chainStart, _chainEnd, progress);
		// Angle au sol → direction écran : la chaîne dessinée suit la même ellipse que la zone annoncée.
		Vector2 tip = _chainCenter + Iso.ToScreen(Vector2.FromAngle(angle) * _config.ChainRadius);
		_chainLine.ShowLine(_chainCenter, tip, 10f, _family);
		_chainLine.SetFlash(1f);
		if (!_chainHit && GodotObject.IsInstanceValid(player) && !player.IsDead)
		{
			Vector2 toPlayer = Iso.ToGround(player.GlobalPosition - _chainCenter);
			float width = Mathf.DegToRad(_config.ChainWidthDeg * 0.5f);
			if (toPlayer.Length() <= _config.ChainRadius && Mathf.Abs(Mathf.AngleDifference(angle, toPlayer.Angle())) <= width
				&& HitPlayer(player, _config.ChainDamage, _chainCenter))
			{
				_chainHit = true;
				ChainHits++;
			}
		}
		if (_chainSweep <= 0f)
			_chainLine.HideMarker();
	}

	/// <summary>
	/// Vrai si le coup a porté. Le dash passe au travers (la chaîne franchie au dash ne touche pas) ; un coup ignoré
	/// (invulnérabilité, palier d'objet) ne compte pas. La cause part avant le coup, comme pour les créatures : la mort
	/// la lit.
	/// </summary>
	private bool HitPlayer(Player player, float damage, Vector2 from)
	{
		if (player.Mobility.IsInvulnerable)
			return false;
		_eventBus.EmitSignal(EventBus.SignalName.PlayerHitBy, _causeId, damage);
		return player.TakeDamage(damage, from).Applied;
	}

	private static Vector2 PropPivot(Texture2D texture) =>
		World.PropManifest.TryGet(texture, out World.PropManifest.Entry entry) ? -entry.Pivot : -texture.GetSize() * new Vector2(0.5f, 1f);
}
