using System;
using System.Collections.Generic;
using Godot;
using Vestiges.Infrastructure.Steam;

namespace Vestiges.Tests;

/// <summary>
/// Plan 26 Q3 : file des classements Steam avec un faux service piloté à la main. Chaque score atteint son tableau à
/// cache froid et chaud, une lecture ne se mêle pas aux envois, échecs et délais sont relancés puis abandonnés, une
/// réponse tardive n'envoie rien deux fois. Ne prouve pas le service Steam réel.
/// </summary>
public partial class LeaderboardQueueRegression : Node
{
	private int _failures;

	public override void _Ready()
	{
		try
		{
			CheckWeeklyBoards();
			CheckRunSubmissions();
			CheckColdThenWarmCache();
			CheckDownloadDuringUploads();
			CheckFailuresRetried();
			CheckAbandonAfterMaxAttempts();
			CheckTimeoutIgnoresLateReply();
			CheckSynchronousReplies();
			CheckClearDropsPending();
			CheckCapacity();
			CheckNextStartsOnTick();
			CheckSubscriberEnqueues();
			CheckDownloadFailure();
			CheckSynchronousFailure();
			CheckLateFindIgnored();
			CheckBackendException();
			CheckClearWhileWaiting();
		}
		catch (Exception ex)
		{
			Check(false, $"exception {ex.GetType().Name} : {ex.Message}");
		}
		GD.Print($"[LeaderboardQueueRegression] RESULT failures={_failures}");
		GetTree().Quit(_failures == 0 ? 0 : 1);
	}

	private void CheckWeeklyBoards()
	{
		(DateTime Date, string Expected)[] cases =
		{
			(new DateTime(2026, 10, 4, 23, 59, 0, DateTimeKind.Utc), "Vestiges_Weekly_2026-W40"),
			(new DateTime(2025, 12, 29, 0, 0, 0, DateTimeKind.Utc), "Vestiges_Weekly_2026-W01"),
			(new DateTime(2026, 12, 31, 12, 0, 0, DateTimeKind.Utc), "Vestiges_Weekly_2026-W53"),
			(new DateTime(2027, 1, 1, 12, 0, 0, DateTimeKind.Utc), "Vestiges_Weekly_2026-W53"),
			(new DateTime(2027, 1, 4, 0, 0, 0, DateTimeKind.Utc), "Vestiges_Weekly_2027-W01"),
		};
		foreach ((DateTime date, string expected) in cases)
		{
			string actual = RunLeaderboards.WeeklyBoard(date);
			Check(actual == expected, $"semaine du {date:yyyy-MM-dd} : {actual} (attendu {expected})");
		}
	}

	private void CheckRunSubmissions()
	{
		DateTime date = new(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc);
		List<RunLeaderboards.Submission> full = RunLeaderboards.ForRun(12_345, 4, "vagabond", date);
		string actual = Describe(full);
		const string expected = "Vestiges_Global=12345 Vestiges_Char_vagabond=12345 Vestiges_Crises=4 Vestiges_Weekly_2026-W40=12345";
		Check(actual == expected, $"envois d'une run : {actual}");

		List<RunLeaderboards.Submission> noCharacter = RunLeaderboards.ForRun(10, 1, "", date);
		Check(noCharacter.Count == 3 && !Describe(noCharacter).Contains("Char_"), $"personnage absent : {Describe(noCharacter)}");
	}

	private void CheckColdThenWarmCache()
	{
		FakeBackend backend = new();
		LeaderboardQueue queue = new(backend);
		List<LeaderboardQueue.UploadResult> results = new();
		queue.UploadFinished += results.Add;

		SubmitRun(queue, 900, 3);
		Check(backend.InFlight == 1, $"cache froid : un seul appel en vol ({backend.InFlight})");
		Drain(queue, backend);
		Check(results.Count == 4 && results.TrueForAll(r => r.Succeeded), $"cache froid : 4 envois réussis ({results.Count})");
		Check(backend.Log("find") == "Vestiges_Global Vestiges_Char_traqueur Vestiges_Crises Vestiges_Weekly_2026-W40",
			$"cache froid : une recherche par tableau ({backend.Log("find")})");
		Check(backend.Log("upload") == "Vestiges_Global=900 Vestiges_Char_traqueur=900 Vestiges_Crises=3 Vestiges_Weekly_2026-W40=900",
			$"cache froid : chaque score sur son tableau ({backend.Log("upload")})");

		backend.Calls.Clear();
		results.Clear();
		SubmitRun(queue, 1_200, 5);
		Drain(queue, backend);
		Check(backend.Log("find") == "", $"cache chaud : aucune recherche ({backend.Log("find")})");
		Check(backend.Log("upload") == "Vestiges_Global=1200 Vestiges_Char_traqueur=1200 Vestiges_Crises=5 Vestiges_Weekly_2026-W40=1200",
			$"cache chaud : quatre envois ({backend.Log("upload")})");
		Check(results.Count == 4 && queue.PendingCount == 0, $"cache chaud : 4 résultats, file vide ({results.Count}, {queue.PendingCount})");
	}

	private void CheckDownloadDuringUploads()
	{
		FakeBackend backend = new();
		LeaderboardQueue queue = new(backend);
		List<LeaderboardQueue.UploadResult> uploads = new();
		List<LeaderboardQueue.DownloadResult> downloads = new();
		queue.UploadFinished += uploads.Add;
		queue.DownloadFinished += downloads.Add;

		queue.SubmitScore("A", 10);
		queue.RequestEntries("B", LeaderboardQueue.EntryRange.Friends, 5);
		queue.SubmitScore("C", 30);
		backend.ReplyOk(); // recherche A
		Check(backend.InFlight == 1 && backend.Pending.Kind == "upload", "lecture en attente : A s'envoie d'abord");
		Drain(queue, backend);
		Check(backend.Log("upload") == "A=10 C=30", $"lecture mêlée : envois intacts ({backend.Log("upload")})");
		Check(downloads.Count == 1 && downloads[0].Board == "B" && downloads[0].Succeeded && downloads[0].Entries.Count == 5,
			"lecture mêlée : B lu une fois, sur son tableau");
		Check(backend.Log("download") == "B:Friends:5", $"lecture mêlée : demande transmise ({backend.Log("download")})");
		Check(uploads.Count == 2, $"lecture mêlée : deux envois réglés ({uploads.Count})");
	}

	private void CheckFailuresRetried()
	{
		FakeBackend backend = new();
		LeaderboardQueue queue = new(backend);
		List<LeaderboardQueue.UploadResult> results = new();
		queue.UploadFinished += results.Add;

		queue.SubmitScore("A", 50);
		backend.ReplyFailure(); // recherche en échec
		Check(backend.InFlight == 0 && results.Count == 0, "échec de recherche : rien n'est réglé, attente");
		queue.Tick(LeaderboardQueue.RetryDelaySec - 0.1);
		Check(backend.InFlight == 0, "échec de recherche : pas de relance avant le délai");
		queue.Tick(0.2);
		Check(backend.InFlight == 1 && backend.Pending.Kind == "find", "échec de recherche : relancée après le délai");
		backend.ReplyOk();
		backend.ReplyFailure(); // envoi en échec
		queue.Tick(LeaderboardQueue.RetryDelaySec);
		Check(backend.InFlight == 1 && backend.Pending.Kind == "upload", "échec d'envoi : relance directe, tableau en cache");
		backend.ReplyOk();
		Check(results.Count == 1 && results[0].Succeeded && results[0].Attempts == 3, $"réussite à la 3e tentative ({results.Count}, {(results.Count > 0 ? results[0].Attempts : 0)})");
		Check(backend.Log("upload") == "A=50 A=50", $"un envoi refusé puis un réussi ({backend.Log("upload")})");
	}

	private void CheckAbandonAfterMaxAttempts()
	{
		FakeBackend backend = new();
		LeaderboardQueue queue = new(backend);
		List<LeaderboardQueue.UploadResult> results = new();
		queue.UploadFinished += results.Add;

		queue.SubmitScore("A", 1);
		queue.SubmitScore("B", 2);
		// La dernière image démarre B : après l'abandon de A, la file est libre.
		for (int attempt = 0; attempt < LeaderboardQueue.MaxAttempts; attempt++)
		{
			backend.ReplyFailure();
			queue.Tick(LeaderboardQueue.RetryDelaySec);
		}
		Check(results.Count == 1 && !results[0].Succeeded && results[0].Board == "A" && results[0].Attempts == LeaderboardQueue.MaxAttempts
			&& results[0].Error == "tableau introuvable", "A abandonné après 3 tentatives, avec sa cause");
		Check(backend.InFlight == 1 && backend.Pending.Board == "B", "B passe ensuite");
		backend.ReplyOk();
		backend.ReplyOk();
		Check(results.Count == 2 && results[1].Succeeded && results[1].Board == "B", "B envoyé");
	}

	private void CheckTimeoutIgnoresLateReply()
	{
		FakeBackend backend = new();
		LeaderboardQueue queue = new(backend);
		List<LeaderboardQueue.UploadResult> results = new();
		queue.UploadFinished += results.Add;

		queue.SubmitScore("A", 7);
		backend.ReplyOk(); // recherche
		FakeBackend.Call stale = backend.Pending;
		queue.Tick(LeaderboardQueue.ReplyTimeoutSec - 0.5);
		Check(backend.Cancels == 0, "délai : pas d'abandon avant 15 s");
		queue.Tick(1.0);
		Check(backend.Cancels == 1 && backend.InFlight == 0, "délai dépassé : appel abandonné");
		stale.Reply(true); // la réponse arrive malgré tout, en retard
		Check(results.Count == 0, "réponse tardive ignorée pendant l'attente");
		queue.Tick(LeaderboardQueue.RetryDelaySec);
		Check(backend.InFlight == 1 && backend.Pending.Kind == "upload", "relance : nouvel envoi en vol");
		stale.Reply(false); // une réponse de l'ancien envoi arrive pendant la relance
		Check(results.Count == 0 && backend.InFlight == 1 && backend.Log("upload") == "A=7 A=7",
			"réponse tardive ignorée pendant la relance : ni échec, ni envoi de plus");
		backend.ReplyOk();
		Check(results.Count == 1 && results[0].Succeeded && results[0].Attempts == 2, "relance après délai réglée une fois");
		Check(backend.Log("upload") == "A=7 A=7", $"deux appels d'envoi, un seul résultat ({backend.Log("upload")})");
	}

	private void CheckSynchronousReplies()
	{
		FakeBackend backend = new() { AnswerImmediately = true };
		LeaderboardQueue queue = new(backend);
		List<LeaderboardQueue.UploadResult> results = new();
		queue.UploadFinished += results.Add;

		SubmitRun(queue, 40, 2);
		Drain(queue, backend);
		Check(results.Count == 4 && queue.PendingCount == 0, $"réponses immédiates : 4 envois, file vide ({results.Count})");
		Check(backend.MaxInFlight == 1, $"réponses immédiates : jamais deux appels en vol ({backend.MaxInFlight})");
	}

	private void CheckClearDropsPending()
	{
		FakeBackend backend = new();
		LeaderboardQueue queue = new(backend);
		List<LeaderboardQueue.UploadResult> results = new();
		queue.UploadFinished += results.Add;

		SubmitRun(queue, 70, 1);
		FakeBackend.Call stale = backend.Pending;
		queue.Clear();
		Check(queue.PendingCount == 0 && backend.Cancels == 1, "arrêt : file vidée, appel abandonné");
		stale.Reply(true);
		Check(results.Count == 0 && backend.InFlight == 0, "arrêt : rappel tardif sans effet");
	}

	private void CheckCapacity()
	{
		FakeBackend backend = new();
		LeaderboardQueue queue = new(backend);
		int accepted = 0;
		for (int i = 0; i < LeaderboardQueue.Capacity + 5; i++)
		{
			if (queue.SubmitScore($"B{i}", i))
				accepted++;
		}
		Check(accepted == LeaderboardQueue.Capacity && queue.PendingCount == LeaderboardQueue.Capacity,
			$"file pleine : {accepted} acceptés sur {LeaderboardQueue.Capacity + 5}");
		Check(!queue.SubmitScore("", 1), "classement sans nom refusé");
		queue.Clear();
	}

	private void CheckNextStartsOnTick()
	{
		FakeBackend backend = new();
		LeaderboardQueue queue = new(backend);
		queue.SubmitScore("A", 1);
		queue.SubmitScore("B", 2);
		backend.ReplyOk();
		backend.ReplyOk();
		Check(backend.InFlight == 0 && queue.PendingCount == 1, "suite : rien ne démarre pendant le rappel de A");
		queue.Tick(0);
		Check(backend.InFlight == 1 && backend.Pending.Board == "B", "suite : B démarre à l'image suivante");
		queue.Clear();
	}

	private void CheckSubscriberEnqueues()
	{
		FakeBackend backend = new();
		LeaderboardQueue queue = new(backend);
		List<LeaderboardQueue.UploadResult> results = new();
		queue.UploadFinished += result =>
		{
			results.Add(result);
			if (result.Board == "A")
				queue.SubmitScore("C", 3);
		};
		queue.SubmitScore("A", 1);
		backend.ReplyOk();
		backend.ReplyOk();
		Check(backend.InFlight == 0 && queue.PendingCount == 1, "abonné qui ajoute : aucun appel lancé depuis le rappel");
		Drain(queue, backend);
		Check(results.Count == 2 && results[1].Board == "C" && results[1].Succeeded, "abonné qui ajoute : C envoyé ensuite");
	}

	private void CheckDownloadFailure()
	{
		FakeBackend backend = new();
		LeaderboardQueue queue = new(backend);
		List<LeaderboardQueue.DownloadResult> downloads = new();
		queue.DownloadFinished += downloads.Add;
		queue.RequestEntries("B", LeaderboardQueue.EntryRange.Global, 10);
		backend.ReplyOk(); // recherche
		for (int attempt = 0; attempt < LeaderboardQueue.MaxAttempts; attempt++)
		{
			backend.ReplyFailure();
			queue.Tick(LeaderboardQueue.RetryDelaySec);
		}
		Check(downloads.Count == 1 && !downloads[0].Succeeded && downloads[0].Entries.Count == 0 && downloads[0].Error == "lecture refusée"
			&& downloads[0].Attempts == LeaderboardQueue.MaxAttempts, "lecture en échec : abandon signalé, sans entrées");
		Check(backend.Log("find") == "B", $"lecture en échec : tableau gardé en cache ({backend.Log("find")})");
	}

	private void CheckSynchronousFailure()
	{
		FakeBackend backend = new() { AnswerImmediately = true, ImmediateAnswer = false };
		LeaderboardQueue queue = new(backend);
		List<LeaderboardQueue.UploadResult> results = new();
		queue.UploadFinished += results.Add;
		queue.SubmitScore("A", 1);
		Check(results.Count == 0 && backend.Calls.Count == 1, "refus immédiat : une tentative, puis attente");
		for (int attempt = 1; attempt < LeaderboardQueue.MaxAttempts; attempt++)
			queue.Tick(LeaderboardQueue.RetryDelaySec);
		Check(results.Count == 1 && !results[0].Succeeded && backend.Calls.Count == LeaderboardQueue.MaxAttempts,
			$"refus immédiat : abandon après {backend.Calls.Count} tentatives, sans boucle");
	}

	private void CheckLateFindIgnored()
	{
		FakeBackend backend = new();
		LeaderboardQueue queue = new(backend);
		queue.SubmitScore("A", 5);
		FakeBackend.Call stale = backend.Pending;
		queue.Tick(LeaderboardQueue.ReplyTimeoutSec);
		queue.Tick(LeaderboardQueue.RetryDelaySec);
		Check(backend.InFlight == 1 && backend.Pending.Kind == "find" && backend.Pending != stale, "recherche relancée après délai");
		stale.Reply(true);
		Check(backend.Pending.Kind == "find" && backend.Log("upload") == "", "ancienne recherche tardive ignorée : aucun envoi");
		backend.ReplyOk();
		backend.ReplyOk();
		Check(backend.Log("upload") == "A=5" && queue.PendingCount == 0, $"recherche relancée : un seul envoi ({backend.Log("upload")})");
	}

	private void CheckBackendException()
	{
		FakeBackend backend = new() { ThrowOnNextCall = true };
		LeaderboardQueue queue = new(backend);
		List<LeaderboardQueue.UploadResult> results = new();
		queue.UploadFinished += results.Add;
		Check(queue.SubmitScore("A", 9), "exception du service : l'ajout ne remonte pas d'exception");
		Check(backend.InFlight == 0 && queue.PendingCount == 1, "exception du service : tentative en échec, attente");
		queue.Tick(LeaderboardQueue.RetryDelaySec);
		Drain(queue, backend);
		Check(results.Count == 1 && results[0].Succeeded && results[0].Attempts == 2, "exception du service : réussite à la relance");
	}

	private void CheckClearWhileWaiting()
	{
		FakeBackend backend = new();
		LeaderboardQueue queue = new(backend);
		queue.SubmitScore("A", 1);
		backend.ReplyFailure();
		queue.Clear();
		queue.Tick(LeaderboardQueue.RetryDelaySec);
		queue.Tick(LeaderboardQueue.ReplyTimeoutSec);
		Check(backend.Calls.Count == 1 && backend.InFlight == 0 && queue.PendingCount == 0, "arrêt pendant l'attente : aucune relance");
	}

	// Répond à chaque appel et avance d'une image quand la file attend la suivante.
	private static void Drain(LeaderboardQueue queue, FakeBackend backend)
	{
		for (int guard = 0; guard < 100 && queue.PendingCount > 0; guard++)
		{
			if (backend.InFlight > 0)
				backend.ReplyOk();
			else
				queue.Tick(0);
		}
	}

	private static void SubmitRun(LeaderboardQueue queue, int score, int crises)
	{
		DateTime date = new(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc);
		foreach (RunLeaderboards.Submission submission in RunLeaderboards.ForRun(score, crises, "traqueur", date))
			queue.SubmitScore(submission.Board, submission.Score);
	}

	private static string Describe(List<RunLeaderboards.Submission> submissions)
	{
		List<string> parts = new();
		foreach (RunLeaderboards.Submission s in submissions)
			parts.Add($"{s.Board}={s.Score}");
		return string.Join(' ', parts);
	}

	private void Check(bool condition, string label)
	{
		if (!condition)
			_failures++;
		GD.Print($"[LeaderboardQueueRegression] {(condition ? "OK" : "ÉCHEC")} {label}");
	}

	/// <summary>Faux service : garde chaque appel jusqu'à ce que le test y réponde, ou répond aussitôt.</summary>
	private sealed class FakeBackend : ILeaderboardBackend
	{
		public sealed class Call
		{
			public string Kind;
			public string Board;
			public int Score;
			public Action<bool> Reply;
		}

		private readonly Dictionary<ulong, string> _boards = new();
		private ulong _nextHandle = 100;

		public readonly List<Call> Calls = new();
		public Call Pending { get; private set; }
		public int InFlight => Pending != null ? 1 : 0;
		public int MaxInFlight { get; private set; }
		public int Cancels { get; private set; }
		public bool AnswerImmediately { get; init; }
		public bool ImmediateAnswer { get; init; } = true;
		public bool ThrowOnNextCall { get; set; }

		public void Find(string board, Action<bool, ulong> done)
		{
			ulong handle = _nextHandle++;
			_boards[handle] = board;
			Start(new Call { Kind = "find", Board = board, Reply = ok => done(ok, ok ? handle : 0) });
		}

		public void Upload(ulong handle, int score, Action<bool, bool, int> done)
		{
			Start(new Call { Kind = "upload", Board = _boards[handle], Score = score, Reply = ok => done(ok, ok, ok ? 1 : 0) });
		}

		public void Download(ulong handle, LeaderboardQueue.EntryRange range, int count, Action<bool, IReadOnlyList<LeaderboardQueue.Entry>> done)
		{
			string board = _boards[handle];
			Start(new Call
			{
				Kind = "download",
				Board = $"{board}:{range}:{count}",
				Reply = ok =>
				{
					List<LeaderboardQueue.Entry> entries = new();
					for (int i = 0; ok && i < count; i++)
						entries.Add(new LeaderboardQueue.Entry(i + 1, $"joueur{i}", 100 - i, (ulong)i));
					done(ok, entries);
				},
			});
		}

		public void Cancel()
		{
			Cancels++;
			Pending = null;
		}

		public void ReplyOk() => Answer(true);

		public void ReplyFailure() => Answer(false);

		public string Log(string kind)
		{
			List<string> parts = new();
			foreach (Call call in Calls)
			{
				if (call.Kind == kind)
					parts.Add(kind == "upload" ? $"{call.Board}={call.Score}" : call.Board);
			}
			return string.Join(' ', parts);
		}

		private void Start(Call call)
		{
			if (ThrowOnNextCall)
			{
				ThrowOnNextCall = false;
				throw new InvalidOperationException("service injoignable (faux service)");
			}
			if (Pending != null)
				MaxInFlight = 2;
			Calls.Add(call);
			Pending = call;
			MaxInFlight = Math.Max(MaxInFlight, 1);
			if (AnswerImmediately)
				Answer(ImmediateAnswer);
		}

		private void Answer(bool ok)
		{
			Call call = Pending;
			if (call == null)
				return;
			Pending = null;
			call.Reply(ok);
		}
	}
}
