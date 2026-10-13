using System;
using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Téléphones abandonnés (plan 39 C1), en son seul : une partie des cabines activables sonne, et se tait pour de bon
/// quand le joueur arrive. Les radios et leurs voix ont été retirées (DECISIONS §150). Un seul gestionnaire et quelques
/// lecteurs spatialisés prêtés aux cabines les plus proches : une cabine hors de portée ne coûte rien.
/// Réglages : <c>data/world/abandoned_voices.json</c>.
/// </summary>
public partial class AbandonedVoicesDirector : Node2D
{
	private const float RefreshIntervalS = 0.25f;
	// Une cabine déjà entendue compte comme plus proche d'autant : deux cabines à distance voisine ne se volent pas le
	// lecteur à chaque rafraîchissement.
	private const float KeepServedFactor = 0.85f;

	private sealed class Phone
	{
		public SmallPlace Booth;
		public Vector2 Position;
		public bool Silenced;
		public float Distance;
		public AudioStreamPlayer2D Player;
	}

	private AbandonedVoicesConfig _config;
	private GameManager _gameManager;
	private Node2D _player;
	private RandomNumberGenerator _rng;
	private readonly List<Phone> _phones = new();
	private readonly List<Phone> _audible = new();
	private readonly List<AudioStreamPlayer2D> _players = new();
	private readonly Dictionary<AudioStreamPlayer2D, Phone> _owners = new();
	private AudioStream _ring;
	private float _refreshTimer;
	private readonly List<Vector2> _ringingBooths = new();

	/// <summary>Cabines qui sonnent et téléphones tus : lus par les captures et les journaux.</summary>
	public IReadOnlyList<Vector2> RingingBooths => _ringingBooths;
	public int PhonesSilenced { get; private set; }
	public int Heard => _owners.Count;
	private static readonly Comparison<Phone> ByDistance = (a, b) => a.Distance.CompareTo(b.Distance);

	/// <summary>Cabines qui sonnent, parmi les petits lieux posés.</summary>
	public void Setup(SmallPlaceDirector places, Node2D player)
	{
		if (!AbandonedVoicesConfig.TryLoad(out _config, out string error))
		{
			GD.PushError($"[AbandonedVoicesDirector] {error} : téléphones muets");
			return;
		}
		_player = player;
		// Flux à part : les sonneries ne décalent aucun tirage du jeu.
		_rng = RunRandom.Create("abandoned_voices");
		// Une cabine sur deux (selon la part réglée), à intervalles réguliers dans l'ordre de pose des petits lieux.
		int booths = 0;
		foreach (SmallPlace place in places.Places)
		{
			if (place.Data.Id != _config.PhoneBoothId)
				continue;
			if (Mathf.FloorToInt((booths + 1) * _config.PhoneShare) > Mathf.FloorToInt(booths * _config.PhoneShare))
			{
				_phones.Add(new Phone { Booth = place, Position = place.GlobalPosition });
				_ringingBooths.Add(place.GlobalPosition);
			}
			booths++;
		}
		GD.Print($"[AbandonedVoicesDirector] {_phones.Count}/{booths} cabines qui sonnent");
	}

	public override void _Ready()
	{
		if (_config == null || _phones.Count == 0)
		{
			SetProcess(false);
			return;
		}
		_gameManager = GetNode<GameManager>("/root/GameManager");
		_ring = AudioManager.CreateLoopableStream(GD.Load<AudioStream>(_config.RingPath));
		for (int i = 0; i < _config.MaxHeard; i++)
		{
			AudioStreamPlayer2D ring = new()
			{
				Name = $"Ring{i}", Bus = AudioManager.BusWorld, Attenuation = _config.Attenuation, Stream = _ring,
				MaxDistance = _config.PhoneHearingPx, VolumeDb = _config.PhoneRingDb,
			};
			AddChild(ring);
			_players.Add(ring);
		}
	}

	public override void _Process(double delta)
	{
		if (_gameManager.CurrentState != GameManager.GameState.Run || !IsInstanceValid(_player))
		{
			foreach (AudioStreamPlayer2D ring in _players)
				Release(ring);
			return;
		}
		_refreshTimer -= (float)delta;
		if (_refreshTimer <= 0f)
		{
			_refreshTimer = RefreshIntervalS;
			Refresh(_player.GlobalPosition);
		}
		// Chaque image : un dash traverserait la zone de silence entre deux rafraîchissements.
		Vector2 playerPosition = _player.GlobalPosition;
		foreach (AudioStreamPlayer2D ring in _players)
		{
			if (_owners.TryGetValue(ring, out Phone phone) && phone.Position.DistanceTo(playerPosition) <= _config.PhoneSilencePx)
			{
				phone.Silenced = true;
				PhonesSilenced++;
				Release(ring);
			}
		}
	}

	/// <summary>Prête les lecteurs aux cabines audibles les plus proches, les reprend aux autres.</summary>
	private void Refresh(Vector2 playerPosition)
	{
		_audible.Clear();
		foreach (Phone phone in _phones)
		{
			phone.Distance = phone.Position.DistanceTo(playerPosition);
			if (IsAudible(phone))
				_audible.Add(phone);
		}
		foreach (Phone phone in _audible)
		{
			if (phone.Player != null)
				phone.Distance *= KeepServedFactor;
		}
		_audible.Sort(ByDistance);
		int heard = Mathf.Min(_audible.Count, _players.Count);
		foreach (AudioStreamPlayer2D ring in _players)
		{
			if (!_owners.TryGetValue(ring, out Phone owner))
				continue;
			int rank = _audible.IndexOf(owner);
			if (rank < 0 || rank >= heard)
				Release(ring);
		}
		for (int i = 0; i < heard; i++)
		{
			Phone phone = _audible[i];
			if (phone.Player != null)
				continue;
			foreach (AudioStreamPlayer2D ring in _players)
			{
				if (_owners.ContainsKey(ring))
					continue;
				Assign(ring, phone);
				break;
			}
		}
	}

	private bool IsAudible(Phone phone)
	{
		if (phone.Silenced || phone.Booth.Used || phone.Booth.Lost)
			return false;
		// Le téléphone se tait quand on arrive, et ne sonne plus jamais (voir aussi _Process, chaque image).
		if (phone.Distance <= _config.PhoneSilencePx)
		{
			phone.Silenced = true;
			PhonesSilenced++;
			return false;
		}
		return phone.Distance < _config.PhoneHearingPx;
	}

	private void Assign(AudioStreamPlayer2D ring, Phone phone)
	{
		_owners[ring] = phone;
		phone.Player = ring;
		ring.GlobalPosition = phone.Position;
		// La sonnerie a déjà commencé avant qu'on l'entende.
		ring.Play(_rng.RandfRange(0f, (float)_ring.GetLength()));
	}

	private void Release(AudioStreamPlayer2D ring)
	{
		if (!_owners.Remove(ring, out Phone phone))
			return;
		phone.Player = null;
		ring.Stop();
	}
}
