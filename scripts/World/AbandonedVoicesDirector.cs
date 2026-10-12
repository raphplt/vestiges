using System;
using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Radios et téléphones abandonnés (plan 39 C1), en son seul. Les radios oubliées sur une voiture d'embouteillage
/// diffusent encore le message d'évacuation, mot par mot sur un grésillement ; plus leur zone s'oublie, plus il manque
/// de mots, et au Néant elles se taisent. Une partie des cabines activables sonne, et se tait pour de bon quand le
/// joueur arrive. Un seul gestionnaire et quelques lecteurs spatialisés prêtés aux sources les plus proches : une
/// source hors de portée ne coûte rien. Réglages : <c>data/world/abandoned_voices.json</c>.
/// </summary>
public partial class AbandonedVoicesDirector : Node2D
{
	private const float RefreshIntervalS = 0.25f;
	// Une source déjà entendue compte comme plus proche d'autant : deux sources à distance voisine ne se volent pas le
	// lecteur à chaque rafraîchissement (le message repartirait à chaque fois).
	private const float KeepServedFactor = 0.85f;

	private sealed class Source
	{
		public Vector2 Position;
		public SmallPlace Booth;
		public bool Silenced;
		public float Pitch;
		public float Distance;
		public Voice Voice;
		public bool IsPhone => Booth != null;
	}

	/// <summary>Lecteurs prêtés à une source : la voix ou la sonnerie, et le grésillement d'une radio.</summary>
	private sealed class Voice
	{
		public AudioStreamPlayer2D Main;
		public AudioStreamPlayer2D Static;
		public Source Owner;
		public int Step;
		public float Wait;
		public float DropoutRemaining;
	}

	private AbandonedVoicesConfig _config;
	private GameManager _gameManager;
	private ErasureManager _erasure;
	private Node2D _player;
	private RandomNumberGenerator _rng;
	private readonly List<Source> _sources = new();
	private readonly List<Source> _audible = new();
	private readonly List<Voice> _voices = new();
	private AudioStream[] _loopStreams;
	private AudioStream _static;
	private AudioStream _ring;
	private float _refreshTimer;
	private readonly List<Vector2> _ringingBooths = new();

	/// <summary>Cabines qui sonnent, mots joués ou perdus, téléphones tus : lus par les captures et les journaux.</summary>
	public IReadOnlyList<Vector2> RingingBooths => _ringingBooths;
	public int WordsPlayed { get; private set; }
	public int WordsLost { get; private set; }
	public int PhonesSilenced { get; private set; }
	public int Heard
	{
		get
		{
			int heard = 0;
			foreach (Voice voice in _voices)
				heard += voice.Owner != null ? 1 : 0;
			return heard;
		}
	}
	private static readonly Comparison<Source> ByDistance = (a, b) => a.Distance.CompareTo(b.Distance);

	/// <summary>Sources de la carte : radios des embouteillages, cabines qui sonnent parmi les petits lieux posés.</summary>
	public void Setup(WorldSetup world, SmallPlaceDirector places, ErasureManager erasure, Node2D player)
	{
		if (!AbandonedVoicesConfig.TryLoad(out _config, out string error))
		{
			GD.PushError($"[AbandonedVoicesDirector] {error} : radios et téléphones muets");
			return;
		}
		_erasure = erasure;
		_player = player;
		// Flux à part : les voix ne décalent aucun tirage du jeu.
		_rng = RunRandom.Create("abandoned_voices");

		int radios = 0;
		foreach (ExodusScenePlacer.Scene scene in world.ExodusScenes)
		{
			if (!scene.Radio)
				continue;
			_sources.Add(new Source { Position = scene.Position, Pitch = 1f + _rng.RandfRange(-_config.RadioPitchJitter, _config.RadioPitchJitter) });
			radios++;
		}
		// Une cabine sur deux (selon la part réglée), à intervalles réguliers dans l'ordre de pose des petits lieux.
		int booths = 0;
		int phones = 0;
		foreach (SmallPlace place in places.Places)
		{
			if (place.Data.Id != _config.PhoneBoothId)
				continue;
			if (Mathf.FloorToInt((booths + 1) * _config.PhoneShare) > Mathf.FloorToInt(booths * _config.PhoneShare))
			{
				_sources.Add(new Source { Position = place.GlobalPosition, Booth = place, Pitch = 1f });
				_ringingBooths.Add(place.GlobalPosition);
				phones++;
			}
			booths++;
		}
		GD.Print($"[AbandonedVoicesDirector] {radios} radios, {phones}/{booths} cabines qui sonnent");
	}

	public override void _Ready()
	{
		if (_config == null || _sources.Count == 0)
		{
			SetProcess(false);
			return;
		}
		_gameManager = GetNode<GameManager>("/root/GameManager");
		_loopStreams = new AudioStream[_config.RadioLoop.Count];
		for (int i = 0; i < _loopStreams.Length; i++)
			_loopStreams[i] = GD.Load<AudioStream>(_config.RadioLoop[i].Path);
		_static = AudioManager.CreateLoopableStream(GD.Load<AudioStream>(_config.StaticPath));
		_ring = AudioManager.CreateLoopableStream(GD.Load<AudioStream>(_config.RingPath));
		for (int i = 0; i < _config.MaxHeard; i++)
		{
			Voice voice = new()
			{
				Main = new AudioStreamPlayer2D { Name = $"Voice{i}", Bus = AudioManager.BusWorld, Attenuation = _config.Attenuation },
				Static = new AudioStreamPlayer2D
				{
					Name = $"Static{i}", Bus = AudioManager.BusWorld, Attenuation = _config.Attenuation, Stream = _static,
				},
			};
			AddChild(voice.Main);
			AddChild(voice.Static);
			_voices.Add(voice);
		}
	}

	public override void _Process(double delta)
	{
		if (_gameManager.CurrentState != GameManager.GameState.Run || !IsInstanceValid(_player))
		{
			foreach (Voice voice in _voices)
				Release(voice);
			return;
		}
		float dt = (float)delta;
		_refreshTimer -= dt;
		if (_refreshTimer <= 0f)
		{
			_refreshTimer = RefreshIntervalS;
			Refresh(_player.GlobalPosition);
		}
		Vector2 playerPosition = _player.GlobalPosition;
		foreach (Voice voice in _voices)
		{
			if (voice.Owner == null)
				continue;
			// Chaque image pour les cabines qui sonnent : un dash traverserait la zone de silence entre deux rafraîchissements.
			if (voice.Owner.IsPhone && voice.Owner.Position.DistanceTo(playerPosition) <= _config.PhoneSilencePx)
				Silence(voice);
			else if (!voice.Owner.IsPhone)
				AdvanceRadio(voice, dt);
		}
	}

	/// <summary>Prête les lecteurs aux sources audibles les plus proches, les reprend aux autres.</summary>
	private void Refresh(Vector2 playerPosition)
	{
		_audible.Clear();
		foreach (Source source in _sources)
		{
			source.Distance = source.Position.DistanceTo(playerPosition);
			if (IsAudible(source))
				_audible.Add(source);
		}
		foreach (Source source in _audible)
		{
			if (source.Voice != null)
				source.Distance *= KeepServedFactor;
		}
		_audible.Sort(ByDistance);
		int heard = Mathf.Min(_audible.Count, _voices.Count);
		foreach (Voice voice in _voices)
		{
			if (voice.Owner == null)
				continue;
			int rank = _audible.IndexOf(voice.Owner);
			if (rank < 0 || rank >= heard)
				Release(voice);
		}
		for (int i = 0; i < heard; i++)
		{
			Source source = _audible[i];
			if (source.Voice != null)
				continue;
			foreach (Voice voice in _voices)
			{
				if (voice.Owner != null)
					continue;
				Assign(voice, source);
				break;
			}
		}
	}

	private bool IsAudible(Source source)
	{
		if (source.IsPhone)
		{
			if (source.Silenced || source.Booth.Used || source.Booth.Lost)
				return false;
			// Le téléphone se tait quand on arrive, et ne sonne plus jamais (voir aussi Silence, chaque image).
			if (source.Distance <= _config.PhoneSilencePx)
			{
				source.Silenced = true;
				PhonesSilenced++;
				return false;
			}
			return source.Distance < _config.PhoneHearingPx;
		}
		return source.Distance < _config.RadioHearingPx
			&& _erasure?.GetZonePhaseAt(source.Position) != ErasureManager.ErasureZonePhase.Void;
	}

	private void Assign(Voice voice, Source source)
	{
		voice.Owner = source;
		source.Voice = voice;
		voice.Main.GlobalPosition = source.Position;
		voice.Static.GlobalPosition = source.Position;
		if (source.IsPhone)
		{
			voice.Main.MaxDistance = _config.PhoneHearingPx;
			voice.Main.VolumeDb = _config.PhoneRingDb;
			voice.Main.PitchScale = 1f;
			voice.Main.Stream = _ring;
			// La sonnerie a déjà commencé avant qu'on l'entende.
			voice.Main.Play(_rng.RandfRange(0f, (float)_ring.GetLength()));
			return;
		}
		voice.Main.MaxDistance = _config.RadioHearingPx;
		voice.Main.VolumeDb = _config.RadioVoiceDb;
		voice.Main.PitchScale = source.Pitch;
		voice.Static.MaxDistance = _config.RadioHearingPx;
		voice.Static.VolumeDb = _config.RadioStaticDb;
		voice.Static.Play(_rng.RandfRange(0f, (float)_static.GetLength()));
		// On arrive au milieu du message, pas à son début.
		voice.Step = _rng.RandiRange(0, _loopStreams.Length - 1);
		voice.Wait = _rng.RandfRange(0.1f, 0.6f);
		voice.DropoutRemaining = 0f;
	}

	private void Silence(Voice voice)
	{
		voice.Owner.Silenced = true;
		PhonesSilenced++;
		Release(voice);
	}

	private static void Release(Voice voice)
	{
		if (voice.Owner == null)
			return;
		voice.Owner.Voice = null;
		voice.Owner = null;
		voice.Main.Stop();
		voice.Static.Stop();
	}

	/// <summary>Joue le son suivant de la boucle, ou le perd dans le grésillement selon la mémoire de la zone.</summary>
	private void AdvanceRadio(Voice voice, float dt)
	{
		if (voice.DropoutRemaining > 0f)
		{
			voice.DropoutRemaining -= dt;
			if (voice.DropoutRemaining <= 0f)
				voice.Static.VolumeDb = _config.RadioStaticDb;
		}
		voice.Wait -= dt;
		if (voice.Wait > 0f)
			return;
		AbandonedVoicesConfig.RadioStep step = _config.RadioLoop[voice.Step];
		AudioStream stream = _loopStreams[voice.Step];
		voice.Step = (voice.Step + 1) % _loopStreams.Length;
		float length = (float)stream.GetLength() / voice.Owner.Pitch;
		voice.Wait = length + step.GapAfterS;
		if (step.Word && _rng.Randf() < DropoutChance(voice.Owner.Position))
		{
			voice.Static.VolumeDb = _config.RadioDropoutStaticDb;
			voice.DropoutRemaining = length;
			WordsLost++;
			return;
		}
		voice.Main.Stream = stream;
		voice.Main.Play();
		if (step.Word)
			WordsPlayed++;
	}

	private float DropoutChance(Vector2 position)
	{
		if (_erasure == null)
			return 0f;
		float memory = _erasure.GetMemoryAt(position);
		return Mathf.Clamp((_config.MemoryWhole - memory) / (_config.MemoryWhole - _config.MemorySilent), 0f, 1f);
	}
}
