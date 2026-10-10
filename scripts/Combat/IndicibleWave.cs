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
/// Rendu (B5d) : la vague et ses brèches sont dessinées par <c>indicible_wave.gdshader</c> (crête d'écume qui roule,
/// embruns, masse d'eau qui traîne, couloirs de lumière chaude). Ce qui se découvre est l'œil (planche B5a, DECISIONS
/// §95) : il sort de l'eau en s'ouvrant, respire, se referme et replonge ; il porte sa propre lumière contre la nuit.
/// </summary>
public partial class IndicibleWave : Node2D
{
	private const float CrossExtent = 1600f;
	// Devant la crête, la place des embruns ; derrière, la masse d'eau qui traîne.
	private const float AheadLength = 40f;
	private const float TrailLength = 170f;
	private const float WarningPulseHz = 3f;
	private const float SprayInterval = 0.06f;
	private const float SpraySpread = 320f;
	private const float WaveGlow = 1.35f;
	private const float BreachGlow = 2f;
	// L'œil : s'ouvrir, se refermer, sa respiration, sa lumière propre (au-delà de 1 : la nuit l'assombrit ensuite).
	private const float EyeOpenSec = 0.45f;
	private const float EyeCloseSec = 0.45f;
	private const float EyeOvershoot = 0.12f;
	private const float EyeBreathHz = 0.6f;
	private const float EyeBreath = 0.015f;
	private const float EyeLight = 1.7f;
	private const float EyeHitFlashSec = 0.12f;
	private static readonly NodePath SelfModulateProperty = "self_modulate";
	private static readonly StringName OriginParam = "origin";
	private static readonly StringName FadeParam = "fade";

	private enum WaveState { Idle, Warning, Rolling, Window }

	private static readonly Vector2[] Directions = { Vector2.Up, Vector2.Right, Vector2.Down, Vector2.Left };

	private IndicibleConfig _config;
	private BossHealth _health;
	private SpawnManager _spawner;
	private Texture2D[] _eyeFrames;
	private Vector2[] _eyePivots;
	private Polygon2D _front;
	private Polygon2D _corridors;
	private ShaderMaterial _frontMaterial;
	private ShaderMaterial _corridorMaterial;
	private readonly Vector2[] _frontQuad = new Vector2[4];
	private readonly Vector2[] _corridorQuad = new Vector2[4];
	private readonly List<float> _breaches = new();
	private GroundTelegraph _windowRing;
	private Sprite2D _eye;
	private Tween _eyeFlash;
	private Enemy _windowPart;
	private RandomNumberGenerator _visualRng;
	private WaveState _state = WaveState.Idle;
	private Vector2 _anchor;
	private Vector2 _direction;
	private float _position;
	private float _timer;
	private float _sprayTimer;
	private float _eyeAge;
	private float _eyeClosing = -1f;
	// L'œil est là (en train de sortir, ouvert ou de replonger), même quand son sprite est encore sous l'eau.
	private bool _eyeShown;
	private float _eyeSunk = 1f;
	private int _eyeFrame = -1;
	private bool _hitThisWave;

	public int Waves { get; private set; }
	public int WaveHits { get; private set; }
	/// <summary>PV pris à la réserve pendant les fenêtres découvertes (mesure).</summary>
	public float WindowDamage { get; private set; }
	public bool IsActive => _state != WaveState.Idle;
	/// <summary>Où l'œil est ouvert, s'il l'est.</summary>
	public Vector2? EyePosition => _eyeShown ? _eye.Position : null;
	/// <summary>La partie découverte, tant que la fenêtre est ouverte.</summary>
	public Enemy WindowPart => _windowPart;

	/// <summary><paramref name="eyeFrames"/> : l'œil fermé, mi-ouvert, ouvert.</summary>
	public void Setup(IndicibleConfig config, BossHealth health, SpawnManager spawner, Texture2D[] eyeFrames)
	{
		_config = config;
		_health = health;
		_spawner = spawner;
		_eyeFrames = eyeFrames;
		_eyePivots = new Vector2[eyeFrames.Length];
		for (int i = 0; i < eyeFrames.Length; i++)
			_eyePivots[i] = World.PropManifest.TryGet(eyeFrames[i], out World.PropManifest.Entry entry)
				? entry.Pivot : eyeFrames[i].GetSize() * new Vector2(0.5f, 1f);
		// Flux à part : les embruns ne décalent aucun tirage du jeu.
		_visualRng = RunRandom.Create("indicible_wave");
		Shader shader = GD.Load<Shader>("res://assets/shaders/indicible_wave.gdshader");
		_frontMaterial = MakeMaterial(shader, false, WaveGlow);
		_corridorMaterial = MakeMaterial(shader, true, BreachGlow);
		// Couloirs puis front, posés au sol au-dessus de la marée (−5) : ajoutés dans cet ordre, le front les recouvre.
		_corridors = new Polygon2D { Name = "Breaches", Visible = false, ZAsRelative = false, ZIndex = -4, Material = _corridorMaterial };
		AddChild(_corridors);
		_front = new Polygon2D { Name = "Front", Visible = false, ZAsRelative = false, ZIndex = -4, Material = _frontMaterial };
		AddChild(_front);
		_windowRing = new GroundTelegraph { Name = "WindowRing" };
		AddChild(_windowRing);
		_eye = new Sprite2D { Name = "Eye", Centered = false, Visible = false };
		AddChild(_eye);
		ApplyColors();
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
		_corridors.Visible = false;
		CloseWindow();
	}

	/// <summary>
	/// Le boss est vaincu : la vague retombe et l'œil se referme puis replonge, au lieu de disparaître d'un coup.
	/// Vrai tant que ce retrait se joue (l'appelant continue de l'animer).
	/// </summary>
	public bool Recede(float delta)
	{
		if (_state is WaveState.Warning or WaveState.Rolling)
		{
			_front.Visible = false;
			_corridors.Visible = false;
		}
		_state = WaveState.Idle;
		ReleaseWindowPart();
		if (!_eyeShown)
			return false;
		BeginClosing();
		return AnimateEye(delta);
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
				// La fenêtre se referme avant la fin : l'œil se ferme et replonge pendant ses dernières secondes.
				if (_eyeShown && _timer <= EyeCloseSec)
					BeginClosing();
				AnimateEye(delta);
				if (_timer <= 0f)
				{
					CloseWindow();
					Announce(player.GlobalPosition);
				}
				return false;
			case WaveState.Warning:
				float pulse = 0.45f + 0.35f * Mathf.Sin(_timer * Mathf.Tau * WarningPulseHz);
				_corridorMaterial.SetShaderParameter(FadeParam, pulse + 0.2f);
				_frontMaterial.SetShaderParameter(FadeParam, pulse);
				if (_timer <= 0f)
				{
					_state = WaveState.Rolling;
					_frontMaterial.SetShaderParameter(FadeParam, 1f);
					_corridorMaterial.SetShaderParameter(FadeParam, 1f);
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
		_corridors.Visible = true;
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
		Spray(local.Dot(_direction.Orthogonal()), delta);
		if (_position >= _config.WaveHalfLength)
		{
			_front.Visible = false;
			_corridors.Visible = false;
			OpenWindow(player.GlobalPosition);
		}
		return hit;
	}

	/// <summary>Embruns à la crête, autour du joueur seulement : le reste du front est hors de l'écran.</summary>
	private void Spray(float playerAcross, float delta)
	{
		_sprayTimer -= delta;
		if (_sprayTimer > 0f)
			return;
		_sprayTimer = SprayInterval;
		float across = playerAcross + _visualRng.RandfRange(-SpraySpread, SpraySpread);
		if (InBreach(across))
			return;
		Vector2 at = _anchor + _direction * _position + _direction.Orthogonal() * across;
		CombatPools.Instance?.EmitSparks(at, new SparkBurst
		{
			Family = FxFamily.Glass, Owner = FxOwner.World, Count = 4, Direction = Vector2.Up,
			Spread = 1.6f, SpeedMin = 60f, SpeedMax = 150f, LifeMin = 0.25f, LifeMax = 0.5f, Ballistic = true, Size = 1,
			Decorative = true,
		});
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

	/// <summary>
	/// Surfaces de la vague : le front (crête, embruns devant, masse d'eau derrière), qui suit sa position, et les
	/// couloirs des brèches sur toute la traversée. Le shader y dessine les brèches qu'on lui donne.
	/// </summary>
	private void BuildShapes()
	{
		Vector2 across = _direction.Orthogonal();
		Vector4 breaches = Vector4.Zero;
		for (int i = 0; i < _breaches.Count && i < 4; i++)
			breaches[i] = _breaches[i];
		foreach (ShaderMaterial material in new[] { _frontMaterial, _corridorMaterial })
		{
			material.SetShaderParameter("direction", _direction);
			material.SetShaderParameter("breaches", breaches);
			material.SetShaderParameter("breach_count", Mathf.Min(_breaches.Count, 4));
		}
		_corridorMaterial.SetShaderParameter(OriginParam, _anchor);
		// Le front est posé dans le repère du monde : son quadrilatère suit sa position à chaque pas.
		SetQuad(_frontQuad, across * -CrossExtent, across * CrossExtent, -_direction * TrailLength, _direction * AheadLength);
		_front.Polygon = _frontQuad;
		float reach = _config.WaveBreachSpread + _config.WaveBreachWidth;
		SetQuad(_corridorQuad, _anchor + across * -reach, _anchor + across * reach,
			-_direction * _config.WaveHalfLength, _direction * _config.WaveHalfLength);
		_corridors.Polygon = _corridorQuad;
	}

	private void PlaceFront()
	{
		Vector2 origin = _anchor + _direction * _position;
		_front.Position = origin;
		_frontMaterial.SetShaderParameter(OriginParam, origin);
	}

	/// <summary>Quadrilatère du segment [<paramref name="a"/>, <paramref name="b"/>], de <paramref name="back"/> à <paramref name="ahead"/>.</summary>
	private static void SetQuad(Vector2[] quad, Vector2 a, Vector2 b, Vector2 back, Vector2 ahead)
	{
		quad[0] = a + back;
		quad[1] = b + back;
		quad[2] = b + ahead;
		quad[3] = a + ahead;
	}

	private static ShaderMaterial MakeMaterial(Shader shader, bool breachLight, float glow)
	{
		ShaderMaterial material = new() { Shader = shader };
		material.SetShaderParameter("breach_light", breachLight ? 1f : 0f);
		material.SetShaderParameter("glow", glow);
		return material;
	}

	private void ApplyColors()
	{
		foreach (ShaderMaterial material in new[] { _frontMaterial, _corridorMaterial })
		{
			material.SetShaderParameter("breach_width", _config.WaveBreachWidth);
			material.SetShaderParameter("foam_color", _config.WaveFoamColor);
			material.SetShaderParameter("water_color", _config.TideDeepColor);
			material.SetShaderParameter("light_color", _config.WaveBreachColor);
			material.SetShaderParameter("trail_length", TrailLength);
		}
	}

	// --- Fenêtre découverte : l'œil ---

	private void OpenWindow(Vector2 playerPosition)
	{
		_state = WaveState.Window;
		_timer = _config.WindowSec;
		// Devant le joueur, plutôt vers le haut de l'écran : il voit l'œil s'ouvrir face à lui.
		float angle = RunRandom.Behavior.RandfRange(-Mathf.Pi + 0.6f, -0.6f);
		Vector2 position = playerPosition + Iso.ToScreen(Vector2.FromAngle(angle) * _config.WindowDistance);
		Enemy part = _spawner.SpawnEventEnemy(EnemyGrammar.BossPartId, position);
		if (part == null)
			return;
		_health.AddPart(part, _config.WindowRadius);
		// Découvert, l'Indicible est fragile : la fenêtre entame la réserve plus vite que les mains.
		part.ApplyFragile(_config.WindowBonus, _config.WindowSec);
		_windowPart = part;
		_eye.Position = position;
		_eye.Modulate = new Color(EyeLight, EyeLight, EyeLight);
		_eyeFlash?.Kill();
		_eye.SelfModulate = Colors.White;
		_eyeShown = true;
		_eyeFrame = -1;
		_eyeAge = 0f;
		_eyeClosing = -1f;
		AnimateEye(0f);
		_windowRing.ShowRing(position, _config.WindowRadius * 1.6f, FxFamily.Void);
		AudioManager.Play(_config.WindowOpenAudio, 0.02f, -2f);
		ScreenShake.Instance?.ShakeMedium();
		CombatPools.Instance?.EmitSparks(position, new SparkBurst
		{
			Family = FxFamily.Glass, Owner = FxOwner.World, Count = 24, Direction = Vector2.Up, Spread = 2.8f,
			SpeedMin = 50f, SpeedMax = 170f, LifeMin = 0.35f, LifeMax = 0.7f, Ballistic = true, Size = 2, Decorative = true,
		});
	}

	/// <summary>
	/// L'œil sort de l'eau en s'ouvrant (fermé, mi-ouvert, ouvert), respire, puis se referme et replonge. Vrai tant
	/// qu'il est visible.
	/// </summary>
	private bool AnimateEye(float delta)
	{
		if (!_eyeShown)
			return false;
		_eyeAge += delta;
		float sunk;
		int frame;
		if (_eyeClosing >= 0f)
		{
			_eyeClosing += delta;
			float progress = Mathf.Clamp(_eyeClosing / EyeCloseSec, 0f, 1f);
			frame = progress < 0.3f ? 2 : progress < 0.55f ? 1 : 0;
			sunk = Mathf.Clamp((progress - 0.35f) / 0.65f, 0f, 1f);
			sunk *= sunk;
			if (progress >= 1f)
			{
				_eyeShown = false;
				_eye.Visible = false;
				_windowRing.HideMarker();
				return false;
			}
		}
		else
		{
			float progress = Mathf.Clamp(_eyeAge / EyeOpenSec, 0f, 1f);
			frame = progress < 0.45f ? 0 : progress < 0.75f ? 1 : 2;
			sunk = 1f - GroundReveal.EaseOutBack(Mathf.Clamp(progress / 0.6f, 0f, 1f), EyeOvershoot);
		}
		if (frame != _eyeFrame)
		{
			_eyeFrame = frame;
			_eye.Texture = _eyeFrames[frame];
		}
		_eyeSunk = sunk;
		GroundReveal.Show(_eye, _eyeFrames[frame].GetSize(), _eyePivots[frame], sunk);
		_eye.Scale = new Vector2(1f, 1f + EyeBreath * Mathf.Sin(_eyeAge * EyeBreathHz * Mathf.Tau));
		return true;
	}

	/// <summary>
	/// L'œil commence à se refermer, une seule fois : s'il sortait encore de l'eau, la fermeture reprend à son
	/// enfoncement présent, sans le faire sauter.
	/// </summary>
	private void BeginClosing()
	{
		if (!_eyeShown || _eyeClosing >= 0f)
			return;
		// Inverse de l'enfoncement de la fermeture : sunk = ((p − 0,35) / 0,65)².
		float progress = 0.35f + 0.65f * Mathf.Sqrt(Mathf.Clamp(_eyeSunk, 0f, 1f));
		_eyeClosing = _eyeSunk > 0f ? progress * EyeCloseSec : 0f;
	}

	private void CloseWindow()
	{
		_eyeShown = false;
		_eye.Visible = false;
		_windowRing.HideMarker();
		ReleaseWindowPart();
	}

	private void ReleaseWindowPart()
	{
		if (_windowPart != null && IsInstanceValid(_windowPart) && _windowPart.IsActive && !_windowPart.IsDying
			&& _windowPart.Boss == _health)
			_windowPart.Vanish();
		_windowPart = null;
	}

	private void OnPartHit(Enemy part, float lost)
	{
		if (part != _windowPart)
			return;
		WindowDamage += lost;
		_eyeFlash?.Kill();
		_eye.SelfModulate = new Color(1.6f, 1.6f, 1.6f);
		_eyeFlash = CreateTween();
		_eyeFlash.TweenProperty(_eye, SelfModulateProperty, Colors.White, EyeHitFlashSec);
	}

	public override void _ExitTree()
	{
		if (_health != null)
			_health.PartHit -= OnPartHit;
		CloseWindow();
	}
}
