using System.Collections.Generic;
using Godot;
using Vestiges.Combat.Abilities;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Spawn;

namespace Vestiges.Combat;

/// <summary>
/// Seconde vague de l'Indicible (plan 07 B3c) : une vague traverse l'écran d'un bord à l'autre, annoncée, avec une ou
/// deux brèches où se tenir. Elle blesse ce qu'elle franchit hors des brèches : elle punit l'immobilité loin d'une
/// brèche comme la fuite au hasard. Après chaque vague, l'Indicible se découvre quelques secondes près du joueur : une
/// grande cible fragile, qui entame la réserve commune plus vite que les mains.
/// </summary>
public partial class IndicibleWave : Node2D
{
	private const float CrossExtent = 1600f;
	private const float FoamThickness = 40f;
	// La vague porte une masse d'eau derrière son écume : on la voit venir de loin.
	private const float TrailLength = 160f;
	private const float WarningPulseHz = 3f;
	private const float WindowScale = 2.2f;

	private enum WaveState { Idle, Warning, Rolling, Window }

	private static readonly Vector2[] Directions = { Vector2.Up, Vector2.Right, Vector2.Down, Vector2.Left };

	private IndicibleConfig _config;
	private BossHealth _health;
	private SpawnManager _spawner;
	private Texture2D _windowTexture;
	private Node2D _front;
	private readonly List<Polygon2D> _foam = new();
	private readonly List<Polygon2D> _trail = new();
	private readonly List<Polygon2D> _breachStrips = new();
	private readonly List<float> _breaches = new();
	private GroundTelegraph _windowRing;
	private Sprite2D _windowSprite;
	private Enemy _windowPart;
	private WaveState _state = WaveState.Idle;
	private Vector2 _anchor;
	private Vector2 _direction;
	private float _position;
	private float _timer;
	private bool _hitThisWave;

	public int Waves { get; private set; }
	public int WaveHits { get; private set; }
	/// <summary>PV pris à la réserve pendant les fenêtres découvertes (mesure).</summary>
	public float WindowDamage { get; private set; }
	public bool IsActive => _state != WaveState.Idle;
	/// <summary>La partie découverte, tant que la fenêtre est ouverte.</summary>
	public Enemy WindowPart => _windowPart;

	public void Setup(IndicibleConfig config, BossHealth health, SpawnManager spawner, Texture2D windowTexture)
	{
		_config = config;
		_health = health;
		_spawner = spawner;
		_windowTexture = windowTexture;
		_front = new Node2D { Name = "Front", Visible = false, ZAsRelative = false, ZIndex = -4 };
		AddChild(_front);
		_windowRing = new GroundTelegraph { Name = "WindowRing" };
		AddChild(_windowRing);
		_windowSprite = new Sprite2D { Centered = false, Visible = false, Texture = windowTexture, Scale = Vector2.One * WindowScale };
		if (World.PropManifest.TryGet(windowTexture, out World.PropManifest.Entry entry))
			_windowSprite.Offset = -entry.Pivot;
		AddChild(_windowSprite);
		_health.PartHit += OnPartHit;
	}

	public void Begin()
	{
		_state = WaveState.Window;
		_timer = _config.WaveInterval * 0.4f;
	}

	public void Stop()
	{
		_state = WaveState.Idle;
		_front.Visible = false;
		foreach (Polygon2D strip in _breachStrips)
			strip.Visible = false;
		CloseWindow();
	}

	/// <summary>Un tick ; vrai si la vague vient de franchir le joueur hors d'une brèche.</summary>
	public bool Tick(Player player, float delta)
	{
		if (_state == WaveState.Idle)
			return false;
		_timer -= delta;
		switch (_state)
		{
			case WaveState.Window:
				if (_timer <= 0f)
				{
					CloseWindow();
					Announce(player.GlobalPosition);
				}
				return false;
			case WaveState.Warning:
				float pulse = 0.45f + 0.35f * Mathf.Sin(_timer * Mathf.Tau * WarningPulseHz);
				foreach (Polygon2D strip in _breachStrips)
					strip.Modulate = Colors.White with { A = pulse + 0.2f };
				_front.Modulate = Colors.White with { A = pulse };
				if (_timer <= 0f)
				{
					_state = WaveState.Rolling;
					_front.Modulate = Colors.White;
					AudioManager.Play(_config.WaveCrashAudio, 0.04f, -2f);
				}
				return false;
			default:
				return Roll(player, delta);
		}
	}

	private void Announce(Vector2 playerPosition)
	{
		_state = WaveState.Warning;
		_timer = _config.WaveWarningSec;
		_hitThisWave = false;
		_anchor = playerPosition;
		_direction = Directions[RunRandom.Behavior.RandiRange(0, Directions.Length - 1)];
		_position = -_config.WaveHalfLength;
		PickBreaches();
		BuildShapes();
		PlaceFront();
		_front.Visible = true;
		Waves++;
		AudioManager.Play(_config.WaveWarningAudio, 0.04f, -3f);
	}

	private bool Roll(Player player, float delta)
	{
		float before = _position;
		_position += _config.WaveSpeed * delta;
		PlaceFront();
		Vector2 local = player.GlobalPosition - _anchor;
		float along = local.Dot(_direction);
		bool hit = false;
		if (!_hitThisWave && before < along && along <= _position && !InBreach(local.Dot(_direction.Orthogonal())))
		{
			_hitThisWave = true;
			hit = true;
		}
		if (_position >= _config.WaveHalfLength)
		{
			_front.Visible = false;
			foreach (Polygon2D strip in _breachStrips)
				strip.Visible = false;
			OpenWindow(player.GlobalPosition);
		}
		return hit;
	}

	/// <summary>
	/// Point de la brèche la plus proche, à la hauteur du joueur dans le sens de la vague, tant qu'une vague est annoncée
	/// ou roule ; null sinon. Sert aux bancs (un bot qui joue la brèche).
	/// </summary>
	public Vector2? NearestBreach(Vector2 point)
	{
		if (_state is not (WaveState.Warning or WaveState.Rolling) || _breaches.Count == 0)
			return null;
		Vector2 across = _direction.Orthogonal();
		Vector2 local = point - _anchor;
		float current = local.Dot(across);
		float best = _breaches[0];
		foreach (float breach in _breaches)
			if (Mathf.Abs(breach - current) < Mathf.Abs(best - current))
				best = breach;
		return _anchor + across * best + _direction * local.Dot(_direction);
	}

	/// <summary>Compté par l'appelant quand le coup a vraiment porté.</summary>
	public void CountHit() => WaveHits++;

	private bool InBreach(float across)
	{
		float half = _config.WaveBreachWidth * 0.5f;
		foreach (float breach in _breaches)
		{
			if (Mathf.Abs(across - breach) <= half)
				return true;
		}
		return false;
	}

	private void PickBreaches()
	{
		_breaches.Clear();
		int count = RunRandom.Behavior.RandiRange(_config.WaveBreachMin, _config.WaveBreachMax);
		for (int i = 0; i < count; i++)
		{
			// Deux brèches ne se recouvrent pas : une seconde tirée sur la première est écartée d'une largeur.
			float breach = RunRandom.Behavior.RandfRange(-_config.WaveBreachSpread, _config.WaveBreachSpread);
			foreach (float other in _breaches)
				if (Mathf.Abs(breach - other) < _config.WaveBreachWidth)
					breach = other + _config.WaveBreachWidth * (breach >= other ? 1.5f : -1.5f);
			_breaches.Add(breach);
		}
		_breaches.Sort();
	}

	/// <summary>Écume du front entre les brèches, et couloirs des brèches sur toute la traversée.</summary>
	private void BuildShapes()
	{
		Vector2 across = _direction.Orthogonal();
		float half = _config.WaveBreachWidth * 0.5f;
		int segment = 0;
		float start = -CrossExtent;
		for (int i = 0; i <= _breaches.Count; i++)
		{
			float end = i < _breaches.Count ? _breaches[i] - half : CrossExtent;
			if (end > start)
			{
				Vector2 back = -_direction * FoamThickness * 0.5f;
				SetQuad(Trail(segment), across * start + back, across * end + back, -_direction * TrailLength);
				SetQuad(Foam(segment++), across * start + back, across * end + back, _direction * FoamThickness);
			}
			if (i < _breaches.Count)
				start = _breaches[i] + half;
		}
		for (int i = segment; i < _foam.Count; i++)
			_foam[i].Visible = false;
		for (int i = segment; i < _trail.Count; i++)
			_trail[i].Visible = false;
		for (int i = 0; i < _breaches.Count; i++)
		{
			Vector2 from = _anchor + across * (_breaches[i] - half) - _direction * _config.WaveHalfLength;
			SetQuad(Strip(i), from, from + across * _config.WaveBreachWidth, _direction * _config.WaveHalfLength * 2f);
		}
		for (int i = _breaches.Count; i < _breachStrips.Count; i++)
			_breachStrips[i].Visible = false;
	}

	private void PlaceFront() => _front.Position = _anchor + _direction * _position;

	/// <summary>Quadrilatère bordé par le segment [<paramref name="a"/>, <paramref name="b"/>] et épais de <paramref name="thickness"/>.</summary>
	private static void SetQuad(Polygon2D quad, Vector2 a, Vector2 b, Vector2 thickness)
	{
		quad.Polygon = new[] { a, b, b + thickness, a + thickness };
		quad.Visible = true;
	}

	private Polygon2D Foam(int index)
	{
		while (_foam.Count <= index)
		{
			Polygon2D foam = new() { Color = _config.WaveFoamColor with { A = 0.9f } };
			_front.AddChild(foam);
			_foam.Add(foam);
		}
		return _foam[index];
	}

	private Polygon2D Trail(int index)
	{
		while (_trail.Count <= index)
		{
			// Sous l'écume : ajoutée avant elle dans le front.
			Polygon2D trail = new() { Color = _config.TideDeepColor with { A = 0.6f } };
			_front.AddChild(trail);
			_front.MoveChild(trail, 0);
			_trail.Add(trail);
		}
		return _trail[index];
	}

	private Polygon2D Strip(int index)
	{
		while (_breachStrips.Count <= index)
		{
			Polygon2D strip = new() { Color = _config.WaveBreachColor with { A = 0.6f }, ZAsRelative = false, ZIndex = -4 };
			AddChild(strip);
			_breachStrips.Add(strip);
		}
		return _breachStrips[index];
	}

	// --- Fenêtre découverte ---

	private void OpenWindow(Vector2 playerPosition)
	{
		_state = WaveState.Window;
		_timer = _config.WindowSec;
		float angle = RunRandom.Behavior.RandfRange(0f, Mathf.Tau);
		Vector2 position = playerPosition + Iso.ToScreen(Vector2.FromAngle(angle) * _config.WindowDistance);
		Enemy part = _spawner.SpawnEventEnemy(EnemyGrammar.BossPartId, position);
		if (part == null)
			return;
		_health.AddPart(part, _config.WindowRadius);
		// Découvert, l'Indicible est fragile : la fenêtre entame la réserve plus vite que les mains.
		part.ApplyFragile(_config.WindowBonus, _config.WindowSec);
		_windowPart = part;
		_windowSprite.Position = position;
		_windowSprite.Visible = true;
		_windowSprite.Modulate = new Color(0.85f, 0.75f, 1.2f);
		_windowRing.ShowRing(position, _config.WindowRadius * 1.6f, FxFamily.Void);
		AudioManager.Play(_config.WindowOpenAudio, 0.02f, -2f);
	}

	private void CloseWindow()
	{
		_windowSprite.Visible = false;
		_windowRing.HideMarker();
		if (_windowPart != null && IsInstanceValid(_windowPart) && _windowPart.IsActive && !_windowPart.IsDying
			&& _windowPart.Boss == _health)
			_windowPart.Vanish();
		_windowPart = null;
	}

	private void OnPartHit(Enemy part, float lost)
	{
		if (part == _windowPart)
			WindowDamage += lost;
	}

	public override void _ExitTree()
	{
		if (_health != null)
			_health.PartHit -= OnPartHit;
		CloseWindow();
	}
}
