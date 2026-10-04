using System;
using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure.Steam;

/// <summary>
/// Opérations de classement traitées une à une (plan 26 Q3) : recherche du tableau s'il n'est pas en cache, puis envoi
/// ou lecture. Chaque tentative porte un jeton ; un rappel tardif ou d'une autre opération est ignoré. Une réponse qui
/// tarde, un échec ou une exception du service relance l'opération, puis l'abandonne en le disant.
/// L'opération suivante ne démarre jamais depuis un rappel du service : à l'image suivante (<see cref="Tick"/>),
/// pour ne pas réarmer un appel Steam pendant que son dispatcher le termine. Sans type Steamworks : chargeable hors x86/x64.
/// </summary>
public sealed class LeaderboardQueue
{
	public const int MaxAttempts = 3;
	public const double ReplyTimeoutSec = 15.0;
	public const double RetryDelaySec = 2.0;
	public const int Capacity = 64;

	public enum EntryRange
	{
		Global,
		Friends,
		AroundUser,
	}

	public readonly record struct Entry(int Rank, string PlayerName, int Score, ulong SteamId);

	public readonly record struct UploadResult(string Board, int Score, bool Succeeded, bool ScoreChanged, int GlobalRank, int Attempts, string Error);

	public readonly record struct DownloadResult(string Board, bool Succeeded, IReadOnlyList<Entry> Entries, int Attempts, string Error);

	private enum OperationKind
	{
		Upload,
		Download,
	}

	private enum Step
	{
		Idle,
		Finding,
		Sending,
		WaitingRetry,
	}

	private sealed class Operation
	{
		public OperationKind Kind;
		public string Board;
		public int Score;
		public EntryRange Range;
		public int Count;
		public int Attempts;
	}

	private static readonly IReadOnlyList<Entry> NoEntries = Array.Empty<Entry>();

	private readonly ILeaderboardBackend _backend;
	private readonly Queue<Operation> _pending = new();
	private readonly Dictionary<string, ulong> _handles = new();
	private Operation _current;
	private Step _step;
	private int _token;
	private double _elapsed;
	private bool _pumping;
	private bool _inReply;

	public event Action<UploadResult> UploadFinished;
	public event Action<DownloadResult> DownloadFinished;

	public LeaderboardQueue(ILeaderboardBackend backend)
	{
		_backend = backend;
	}

	/// <summary>Opérations en cours ou en attente.</summary>
	public int PendingCount => _pending.Count + (_current != null ? 1 : 0);

	public bool SubmitScore(string board, int score)
	{
		return Enqueue(new Operation { Kind = OperationKind.Upload, Board = board, Score = score });
	}

	public bool RequestEntries(string board, EntryRange range, int count)
	{
		return Enqueue(new Operation { Kind = OperationKind.Download, Board = board, Range = range, Count = count });
	}

	/// <summary>
	/// Démarre l'opération suivante et compte le temps d'attente d'une réponse ou d'une nouvelle tentative ;
	/// appelé à chaque image, pause comprise, avec un temps réel (pas celui ralenti par <c>Engine.TimeScale</c>).
	/// </summary>
	public void Tick(double delta)
	{
		if (_current == null)
		{
			Pump();
			return;
		}

		_elapsed += delta;
		if (_step == Step.WaitingRetry)
		{
			if (_elapsed >= RetryDelaySec)
				BeginAttempt();
		}
		else if (_elapsed >= ReplyTimeoutSec)
		{
			_backend.Cancel();
			Fail($"pas de réponse en {ReplyTimeoutSec:0} s");
		}
	}

	/// <summary>Abandonne l'opération en cours et celles en attente (arrêt de Steam, session d'essai).</summary>
	public void Clear()
	{
		int dropped = PendingCount;
		if (_current != null)
			_backend.Cancel();
		_token++;
		_pending.Clear();
		_current = null;
		_step = Step.Idle;
		if (dropped > 0)
			GD.PushWarning($"[SteamLeaderboards] {dropped} opération(s) de classement abandonnée(s) à l'arrêt.");
	}

	private bool Enqueue(Operation operation)
	{
		if (string.IsNullOrEmpty(operation.Board))
		{
			GD.PushWarning("[SteamLeaderboards] Classement sans nom refusé.");
			return false;
		}
		if (PendingCount >= Capacity)
		{
			GD.PushWarning($"[SteamLeaderboards] File pleine ({Capacity}) : {operation.Board} refusé.");
			return false;
		}

		_pending.Enqueue(operation);
		if (!_inReply)
			Pump();
		return true;
	}

	// Un faux service peut répondre pendant l'appel : la boucle reprend la suite sans récursion.
	private void Pump()
	{
		if (_pumping)
			return;

		_pumping = true;
		try
		{
			while (_current == null && _pending.Count > 0)
			{
				_current = _pending.Dequeue();
				BeginAttempt();
			}
		}
		finally
		{
			_pumping = false;
		}
	}

	private void BeginAttempt()
	{
		_current.Attempts++;
		_elapsed = 0;
		int token = ++_token;
		if (_handles.TryGetValue(_current.Board, out ulong handle))
		{
			Send(token, handle);
			return;
		}

		_step = Step.Finding;
		try
		{
			_backend.Find(_current.Board, (found, foundHandle) => OnFound(token, found, foundHandle));
		}
		catch (Exception ex)
		{
			FailOnException(token, Step.Finding, ex);
		}
	}

	private void OnFound(int token, bool found, ulong handle)
	{
		if (!IsAwaited(token, Step.Finding))
			return;

		bool outerReply = _inReply;
		_inReply = true;
		try
		{
			if (!found)
			{
				Fail("tableau introuvable");
				return;
			}

			_handles[_current.Board] = handle;
			Send(token, handle);
		}
		finally
		{
			_inReply = outerReply;
		}
	}

	private void Send(int token, ulong handle)
	{
		_step = Step.Sending;
		_elapsed = 0;
		try
		{
			if (_current.Kind == OperationKind.Upload)
				_backend.Upload(handle, _current.Score, (succeeded, changed, rank) => OnUploaded(token, succeeded, changed, rank));
			else
				_backend.Download(handle, _current.Range, _current.Count, (succeeded, entries) => OnDownloaded(token, succeeded, entries));
		}
		catch (Exception ex)
		{
			FailOnException(token, Step.Sending, ex);
		}
	}

	private void OnUploaded(int token, bool succeeded, bool scoreChanged, int globalRank)
	{
		if (!IsAwaited(token, Step.Sending))
			return;

		bool outerReply = _inReply;
		_inReply = true;
		try
		{
			if (!succeeded)
			{
				Fail("envoi refusé");
				return;
			}

			Operation operation = Finish();
			GD.Print($"[SteamLeaderboards] {operation.Board} : {operation.Score} envoyé"
				+ (scoreChanged ? $", rang {globalRank}." : ", meilleur score inchangé."));
			Notify(new UploadResult(operation.Board, operation.Score, true, scoreChanged, globalRank, operation.Attempts, null));
		}
		finally
		{
			_inReply = outerReply;
		}
	}

	private void OnDownloaded(int token, bool succeeded, IReadOnlyList<Entry> entries)
	{
		if (!IsAwaited(token, Step.Sending))
			return;

		bool outerReply = _inReply;
		_inReply = true;
		try
		{
			if (!succeeded)
			{
				Fail("lecture refusée");
				return;
			}

			Operation operation = Finish();
			Notify(new DownloadResult(operation.Board, true, entries ?? NoEntries, operation.Attempts, null));
		}
		finally
		{
			_inReply = outerReply;
		}
	}

	private void FailOnException(int token, Step step, Exception ex)
	{
		GD.PushWarning($"[SteamLeaderboards] Exception du service : {ex.GetType().Name} : {ex.Message}");
		if (IsAwaited(token, step))
			Fail($"exception du service ({ex.GetType().Name})");
	}

	// Une faute d'un abonné ne doit ni bloquer la file, ni remonter dans le dispatcher Steam.
	private void Notify(UploadResult result)
	{
		try
		{
			UploadFinished?.Invoke(result);
		}
		catch (Exception ex)
		{
			GD.PushError($"[SteamLeaderboards] Abonné en échec sur {result.Board} : {ex.Message}");
		}
	}

	private void Notify(DownloadResult result)
	{
		try
		{
			DownloadFinished?.Invoke(result);
		}
		catch (Exception ex)
		{
			GD.PushError($"[SteamLeaderboards] Abonné en échec sur {result.Board} : {ex.Message}");
		}
	}

	private bool IsAwaited(int token, Step step) => _current != null && token == _token && _step == step;

	private void Fail(string error)
	{
		_token++;
		if (_current.Attempts < MaxAttempts)
		{
			_step = Step.WaitingRetry;
			_elapsed = 0;
			return;
		}

		Operation operation = Finish();
		GD.PushWarning($"[SteamLeaderboards] {operation.Board} : abandon après {operation.Attempts} tentatives ({error}).");
		if (operation.Kind == OperationKind.Upload)
			Notify(new UploadResult(operation.Board, operation.Score, false, false, 0, operation.Attempts, error));
		else
			Notify(new DownloadResult(operation.Board, false, NoEntries, operation.Attempts, error));
	}

	private Operation Finish()
	{
		Operation operation = _current;
		_current = null;
		_step = Step.Idle;
		_token++;
		return operation;
	}
}
