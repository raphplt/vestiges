using System.Collections.Generic;
using Godot;
using Vestiges.Combat.Abilities;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Spawn;

namespace Vestiges.Combat;

/// <summary>
/// L'Indicible (plan 07 B3, fiche du 4 octobre validée au §70) : la nuit du 14, trop grande pour l'écran, sans corps à
/// viser. Il se combat à ses mains, des parties de boss en réserve commune qui sortent du sol près du joueur puis
/// agrippent sa position. La réserve franchit deux seuils : la tempête (nuit, vent, éclairs), puis la marée
/// (<see cref="IndicibleTide"/>), puis la seconde vague et la fenêtre où il se découvre (<see cref="IndicibleWave"/>).
/// </summary>
public partial class Indicible : Node2D
{
	public const string AssetFolder = "res://assets/bosses/indicible";
	private const float HitFlashSec = 0.12f;
	private const float SinkSec = 0.35f;
	private const float StrikeFlashSec = 0.2f;
	private const float FreeDelaySec = 1.2f;
	private static readonly NodePath SelfModulateProperty = "self_modulate";

	private enum HandState { Rising, Waiting, Grabbing, Sinking }

	private sealed class Hand
	{
		public Enemy Part;
		public Sprite2D Sprite;
		public GroundTelegraph Marker;
		public HandState State;
		public float Timer;
		public Vector2 Target;
		public Tween Flash;
	}

	private readonly List<Hand> _hands = new();
	private readonly Dictionary<Enemy, Hand> _handByPart = new();
	private readonly Stack<Hand> _spareHands = new();
	private IndicibleConfig _config;
	private EventBus _eventBus;
	private Player _player;
	private SpawnManager _spawner;
	private CanvasModulate _night;
	private Color _dayColor;
	private BossHealth _health;
	private FxFamily _family;
	private float _damage;
	private float _handTimer;
	private float _windTimer;
	private Vector2 _wind;
	private float _lightningTimer;
	private float _lightningWarning;
	private float _lightningFlash;
	private Vector2 _lightningCenter;
	private GroundTelegraph _lightningMarker;
	private Texture2D _openTexture;
	private Texture2D _grabTexture;
	private bool _defeated;
	private GameManager _gameManager;
	private IndicibleTide _tide;
	private IndicibleWave _wave;
	private Tween _nightTween;

	public BossHealth Health => _health;
	public bool IsDefeated => _defeated;
	/// <summary>Coups portés au joueur et attaques lancées (mesure).</summary>
	public int LightningStrikes { get; private set; }
	public int LightningHits { get; private set; }
	public int Grabs { get; private set; }
	public int GrabHits { get; private set; }
	public int HandsRaised { get; private set; }
	public int TideHits { get; private set; }
	public IndicibleTide Tide => _tide;
	public IndicibleWave Wave => _wave;

	/// <summary>Faux si la configuration ou un sprite manquent : le boss se retire aussitôt, sans entrer en jeu.</summary>
	public bool Initialize(float hpScale, float dmgScale, Player player, SpawnManager spawner)
	{
		_eventBus = GetNode<EventBus>("/root/EventBus");
		EnemyData data = EnemyDataLoader.Get(EnemyGrammar.FinalBossId);
		if (!IndicibleConfig.TryLoad(out _config, out string error) || data == null)
		{
			GD.PushError($"[Indicible] Le boss n'apparaît pas : {error ?? "fiche data/enemies/indicible.json absente"}.");
			return false;
		}
		_openTexture = LoadTexture("indicible_hand_open");
		_grabTexture = LoadTexture("indicible_hand_grab");
		if (_openTexture == null || _grabTexture == null)
			return false;
		_player = player;
		_spawner = spawner;
		_gameManager = GetNodeOrNull<GameManager>("/root/GameManager");
		_family = PixelPalette.ParseFamily(_config.FxFamily, FxFamily.Void);
		_damage = data.Stats.Damage * dmgScale;
		YSortEnabled = true;
		_health = new BossHealth(Tr(_config.NameKey), data.Stats.Hp * hpScale, _config.PhaseThresholds);
		_health.PartHit += OnHandHit;
		_health.PhaseReached += OnPhaseReached;
		_health.Depleted += OnDepleted;
		_health.ShowBar(_eventBus);
		_handTimer = _config.FirstAttackSec;
		_lightningTimer = _config.FirstAttackSec + _config.LightningInterval * 0.5f;
		_lightningMarker = new GroundTelegraph { Name = "LightningMarker" };
		AddChild(_lightningMarker);
		_tide = new IndicibleTide { Name = "Tide" };
		AddChild(_tide);
		_tide.Setup(_config);
		_wave = new IndicibleWave { Name = "Wave" };
		AddChild(_wave);
		_wave.Setup(_config, _health, spawner, _openTexture);
		ChangeWind();
		FallNight();
		AudioManager.Play(_config.RiseAudio, 0f);
		ScreenShake.Instance?.ShakeHeavy();
		_eventBus.EmitSignal(EventBus.SignalName.EnemySpawned, EnemyGrammar.FinalBossId, hpScale, dmgScale);
		GD.Print($"[Indicible] La nuit tombe (réserve {_health.Max:F0} PV)");
		return true;
	}

	/// <summary>Mains autorisées à la fois : une de plus à chaque seuil franchi, et celles de la marée.</summary>
	private int MaxHands => _config.HandMaxAlive + _config.HandExtraPerPhase * _health.Phase
		+ (_tide.IsActive ? _config.TideExtraHands : 0);

	public override void _PhysicsProcess(double delta)
	{
		if (_defeated || _player == null || !IsInstanceValid(_player))
			return;
		// Écran de choix, mort : le boss attend, comme le joueur.
		if (_gameManager != null && _gameManager.CurrentState != GameManager.GameState.Run)
			return;
		float dt = (float)delta;
		// Première phase : la tempête. Ensuite, la marée remplace le vent et les éclairs.
		if (_health.Phase == 0)
		{
			TickWind(dt);
			TickLightning(dt);
		}
		if (_tide.Tick(_player, dt) && HitPlayer(_damage * _config.TideDeepDamageMultiplier, _player.GlobalPosition))
			TideHits++;
		if (_wave.Tick(_player, dt) && HitPlayer(_damage * _config.WaveDamageMultiplier, _player.GlobalPosition))
			_wave.CountHit();
		TickHands(dt);
		_handTimer -= dt;
		if (_handTimer <= 0f)
		{
			_handTimer = _config.HandInterval;
			if (CountLivingHands() < MaxHands)
				RaiseHand();
		}
	}

	// --- Tempête : nuit, vent, éclairs ---

	private void FallNight()
	{
		_night = GetTree().CurrentScene?.GetNodeOrNull<CanvasModulate>("CanvasModulate");
		if (_night == null)
			return;
		_dayColor = _night.Color;
		_nightTween = _night.CreateTween();
		_nightTween.TweenProperty(_night, "color", _dayColor * _config.DarkColor, _config.DarkFadeSec);
	}

	private void LiftNight()
	{
		if (_night == null || !IsInstanceValid(_night))
			return;
		_nightTween?.Kill();
		_nightTween = _night.CreateTween();
		_nightTween.TweenProperty(_night, "color", _dayColor, _config.DarkFadeSec);
	}

	private void TickWind(float delta)
	{
		_windTimer -= delta;
		if (_windTimer <= 0f)
			ChangeWind();
		_player.ExternalDrift = _wind;
	}

	private void ChangeWind()
	{
		_windTimer = RunRandom.Behavior.RandfRange(_config.WindChangeMinSec, _config.WindChangeMaxSec);
		_wind = Vector2.FromAngle(RunRandom.Behavior.RandfRange(0f, Mathf.Tau)) * _config.WindSpeed;
	}

	private void TickLightning(float delta)
	{
		if (_lightningFlash > 0f)
		{
			_lightningFlash -= delta;
			if (_lightningFlash <= 0f)
				_lightningMarker.HideMarker();
			else
				_lightningMarker.SetFlash(_lightningFlash / StrikeFlashSec);
		}
		if (_lightningWarning > 0f)
		{
			_lightningWarning -= delta;
			_lightningMarker.SetProgress(1f - _lightningWarning / _config.LightningWarning);
			if (_lightningWarning <= 0f)
				StrikeLightning();
			return;
		}
		_lightningTimer -= delta;
		if (_lightningTimer > 0f)
			return;
		_lightningTimer = _config.LightningInterval;
		_lightningCenter = DriftedPosition(_config.LightningWarning);
		_lightningWarning = _config.LightningWarning;
		_lightningMarker.ShowCircle(_lightningCenter, _config.LightningRadius, _family);
		AudioManager.Play(_config.LightningWarningAudio, 0.08f, -6f);
		LightningStrikes++;
	}

	private void StrikeLightning()
	{
		_lightningFlash = StrikeFlashSec;
		_lightningMarker.SetFlash(1f);
		AudioManager.Play(_config.LightningStrikeAudio, 0.06f, -2f);
		ScreenShake.Instance?.ShakeLight();
		CombatPools.Instance?.ShowHitFlash(_lightningCenter);
		float reach = _config.LightningRadius + _config.LightningHitMargin;
		if (Iso.GroundDistanceSquared(_player.GlobalPosition, _lightningCenter) <= reach * reach
			&& HitPlayer(_damage * _config.LightningDamageMultiplier, _lightningCenter))
			LightningHits++;
	}

	// --- Mains ---

	private int CountLivingHands()
	{
		int count = 0;
		foreach (Hand hand in _hands)
			count += hand.State is HandState.Rising or HandState.Waiting or HandState.Grabbing ? 1 : 0;
		return count;
	}

	private void RaiseHand()
	{
		// Pendant la marée, les mains sortent de l'eau quand elle est à portée.
		if (!_tide.TryWaterPoint(_player.GlobalPosition, _config.HandDistanceMin, _config.HandDistanceMax, out Vector2 position))
		{
			float angle = RunRandom.Behavior.RandfRange(0f, Mathf.Tau);
			float distance = RunRandom.Behavior.RandfRange(_config.HandDistanceMin, _config.HandDistanceMax);
			// Distance au sol : un écart vertical à l'écran compte double.
			position = _player.GlobalPosition + Iso.ToScreen(Vector2.FromAngle(angle) * distance);
		}
		if (!_spawner.IsSpawnablePosition(position))
			return;
		Enemy part = _spawner.SpawnEventEnemy(EnemyGrammar.BossPartId, position);
		if (part == null)
			return;
		_health.AddPart(part, _config.HandBodyRadius);
		Hand hand = _spareHands.Count > 0 ? _spareHands.Pop() : CreateHand();
		hand.Part = part;
		hand.State = HandState.Rising;
		hand.Timer = _config.HandRiseSec;
		hand.Sprite.Texture = _openTexture;
		hand.Sprite.Offset = Pivot(_openTexture);
		hand.Sprite.Position = position;
		hand.Sprite.Scale = new Vector2(1f, 0f);
		hand.Sprite.SelfModulate = Colors.White;
		hand.Sprite.Visible = true;
		_hands.Add(hand);
		_handByPart[part] = hand;
		HandsRaised++;
		AudioManager.Play(_config.HandRiseAudio, 0.1f, -8f);
		CombatPools.Instance?.EmitKnockbackDust(position, Vector2.Up);
	}

	private Hand CreateHand()
	{
		Hand hand = new()
		{
			Sprite = new Sprite2D { Centered = false, Visible = false },
			Marker = new GroundTelegraph { Name = "GrabMarker" },
		};
		AddChild(hand.Sprite);
		AddChild(hand.Marker);
		return hand;
	}

	private void TickHands(float delta)
	{
		for (int i = _hands.Count - 1; i >= 0; i--)
		{
			Hand hand = _hands[i];
			hand.Timer -= delta;
			switch (hand.State)
			{
				case HandState.Rising:
					hand.Sprite.Scale = new Vector2(1f, Mathf.Clamp(1f - hand.Timer / _config.HandRiseSec, 0f, 1f));
					if (hand.Timer <= 0f)
					{
						hand.State = HandState.Waiting;
						hand.Timer = _config.HandLifeSec;
					}
					break;
				case HandState.Waiting:
					if (hand.Timer <= 0f)
						BeginGrab(hand);
					break;
				case HandState.Grabbing:
					hand.Marker.SetProgress(1f - hand.Timer / _config.GrabWarningSec);
					if (hand.Timer <= 0f)
						ResolveGrab(hand);
					break;
				case HandState.Sinking:
					hand.Sprite.Scale = new Vector2(1f, Mathf.Clamp(hand.Timer / SinkSec, 0f, 1f));
					if (hand.Timer <= StrikeFlashSec * 0.5f)
						hand.Marker.HideMarker();
					if (hand.Timer <= 0f)
						Retire(hand, i);
					break;
			}
		}
	}

	private void BeginGrab(Hand hand)
	{
		hand.State = HandState.Grabbing;
		hand.Timer = _config.GrabWarningSec;
		// La prise anticipe le pas du joueur (vent compris) : elle punit qui file droit, l'éclair punit qui reste.
		hand.Target = _player.GlobalPosition + (_player.Velocity + _player.ExternalDrift) * _config.GrabWarningSec;
		hand.Marker.ShowCircle(hand.Target, _config.GrabRadius, _family);
		AudioManager.Play(_config.GrabWarningAudio, 0.08f, -6f);
		Grabs++;
	}

	/// <summary>La main replonge et ressort sur la position annoncée, poing fermé.</summary>
	private void ResolveGrab(Hand hand)
	{
		hand.State = HandState.Sinking;
		hand.Timer = SinkSec + StrikeFlashSec;
		hand.Marker.SetFlash(1f);
		hand.Sprite.Texture = _grabTexture;
		hand.Sprite.Offset = Pivot(_grabTexture);
		hand.Sprite.Position = hand.Target;
		if (IsOwnHand(hand.Part))
			hand.Part.GlobalPosition = hand.Target;
		AudioManager.Play(_config.GrabImpactAudio, 0.08f, -3f);
		ScreenShake.Instance?.ShakeLight();
		CombatPools.Instance?.EmitKnockbackDust(hand.Target, Vector2.Up);
		float reach = _config.GrabRadius + _config.GrabHitMargin;
		if (Iso.GroundDistanceSquared(_player.GlobalPosition, hand.Target) <= reach * reach
			&& HitPlayer(_damage * _config.GrabDamageMultiplier, hand.Target))
			GrabHits++;
	}

	private void Retire(Hand hand, int index)
	{
		hand.Flash?.Kill();
		hand.Sprite.Visible = false;
		hand.Marker.HideMarker();
		_handByPart.Remove(hand.Part);
		if (IsOwnHand(hand.Part))
			hand.Part.Vanish();
		hand.Part = null;
		_hands.RemoveAt(index);
		_spareHands.Push(hand);
	}

	private void OnHandHit(Enemy part, float lost)
	{
		if (!_handByPart.TryGetValue(part, out Hand hand))
			return;
		hand.Flash?.Kill();
		hand.Sprite.SelfModulate = new Color(1.9f, 1.9f, 1.9f);
		hand.Flash = CreateTween();
		hand.Flash.TweenProperty(hand.Sprite, SelfModulateProperty, Colors.White, HitFlashSec);
	}

	private void OnPhaseReached(int phase)
	{
		if (phase == 1)
		{
			// La tempête retombe, la mer monte.
			_player.ExternalDrift = Vector2.Zero;
			_lightningWarning = 0f;
			_lightningMarker.HideMarker();
			_tide.Begin(_player.GlobalPosition);
		}
		else if (phase == 2)
		{
			// La mer se retire avant la seconde vague.
			_tide.Stop();
			_wave.Begin();
		}
		AudioManager.Play(_config.RiseAudio, 0.04f, -3f);
		ScreenShake.Instance?.ShakeMedium();
		GD.Print($"[Indicible] Phase {phase + 1} ({_health.Ratio:P0} de la réserve)");
	}

	private void OnDepleted()
	{
		_defeated = true;
		_player.ExternalDrift = Vector2.Zero;
		_tide.Stop();
		_wave.Stop();
		_lightningMarker.HideMarker();
		for (int i = _hands.Count - 1; i >= 0; i--)
			Retire(_hands[i], i);
		LiftNight();
		AudioManager.Play(_config.DefeatedAudio, 0.02f);
		ScreenShake.Instance?.ShakeHeavy();
		ScreenShake.Instance?.Hitstop(0.08f);
		// Mort unique : score, succès et entrée en endgame (EndgameManager) lisent cette élimination.
		_eventBus.EmitSignal(EventBus.SignalName.EnemyKilled, EnemyGrammar.FinalBossId, _player.GlobalPosition);
		GD.Print("[Indicible] Vaincu : le jour revient");
		// Un tween meurt avec le nœud : une sortie de run dans l'intervalle ne libère rien deux fois.
		Tween free = CreateTween();
		free.TweenInterval(FreeDelaySec);
		free.TweenCallback(Callable.From(QueueFree));
	}

	/// <summary>Retiré sans être vaincu (fin de run, mesure) : ses mains rentrent, le vent tombe, le jour revient.</summary>
	public override void _ExitTree()
	{
		if (_player != null && IsInstanceValid(_player))
			_player.ExternalDrift = Vector2.Zero;
		for (int i = _hands.Count - 1; i >= 0; i--)
			Retire(_hands[i], i);
		if (!_defeated && _night != null && IsInstanceValid(_night))
		{
			_nightTween?.Kill();
			_night.Color = _dayColor;
		}
		_health?.EndEncounter();
	}

	/// <summary>
	/// Où le vent aura porté un joueur qui ne bouge pas, à la fin de l'annonce de l'éclair : l'immobilité reste punie
	/// malgré la dérive ; celui qui bouge de lui-même s'en écarte.
	/// </summary>
	private Vector2 DriftedPosition(float seconds) => _player.GlobalPosition + _wind * seconds;

	/// <summary>La partie est encore une main de ce boss : ni libérée, ni rendue au pool pour une autre créature.</summary>
	private bool IsOwnHand(Enemy part) => IsInstanceValid(part) && part.IsActive && !part.IsDying && part.Boss == _health;

	private bool HitPlayer(float damage, Vector2 from)
	{
		if (_player.Mobility.IsInvulnerable)
			return false;
		_eventBus.EmitSignal(EventBus.SignalName.PlayerHitBy, EnemyGrammar.FinalBossId, damage);
		_player.TakeDamage(damage, from);
		return true;
	}

	private static Vector2 Pivot(Texture2D texture) =>
		World.PropManifest.TryGet(texture, out World.PropManifest.Entry entry) ? -entry.Pivot : -texture.GetSize() * new Vector2(0.5f, 1f);

	private static Texture2D LoadTexture(string stem)
	{
		string path = $"{AssetFolder}/{stem}.png";
		if (ResourceLoader.Exists(path))
			return GD.Load<Texture2D>(path);
		GD.PushError($"[Indicible] Sprite absent : {path}");
		return null;
	}
}
