using System.Collections.Generic;
using Godot;
using Steamworks;

namespace Vestiges.Infrastructure.Steam;

/// <summary>
/// Gestion des leaderboards Steam : upload de scores, téléchargement d'entrées.
/// Supporte les 5 types de leaderboard du GDD :
///   - Global (tous joueurs, tous personnages)
///   - Par personnage (vagabond, forgeuse, traqueur)
///   - Friends-only
///   - Weekly (seed fixé, reset hebdo)
///   - Nuits survivées (endurance pure)
///
/// L'API Steamworks est callback-based. On utilise CallResult pour les opérations async.
/// </summary>
public partial class SteamLeaderboards : Node
{
	// --- Noms des leaderboards (à créer dans Steamworks App Admin) ---
	public const string BoardGlobal = "Vestiges_Global";
	public const string BoardNightsSurvived = "Vestiges_Nights";
	public const string BoardWeekly = "Vestiges_Weekly";
	private const string BoardCharacterPrefix = "Vestiges_Char_";

	/// <summary>Résultat d'une requête de leaderboard.</summary>
	public record LeaderboardEntry(int Rank, string PlayerName, int Score, CSteamID SteamId);

	// Cache des handles de leaderboards résolus
	private readonly Dictionary<string, SteamLeaderboard_t> _boardCache = new();

	// Callbacks actifs
	private CallResult<LeaderboardFindResult_t> _findCallback;
	private CallResult<LeaderboardScoreUploaded_t> _uploadCallback;
	private CallResult<LeaderboardScoresDownloaded_t> _downloadCallback;

	// Résultats de la dernière requête de download (consommés par l'UI)
	private readonly List<LeaderboardEntry> _lastEntries = new();
	public IReadOnlyList<LeaderboardEntry> LastEntries => _lastEntries;
	public bool IsLoading { get; private set; }

	// Signaux pour l'UI
	[Signal] public delegate void ScoreUploadedEventHandler(bool success);
	[Signal] public delegate void EntriesLoadedEventHandler(int count);

	public override void _Ready()
	{
		if (!SteamManager.IsActive)
			return;

		_findCallback = CallResult<LeaderboardFindResult_t>.Create(OnLeaderboardFound);
		_uploadCallback = CallResult<LeaderboardScoreUploaded_t>.Create(OnScoreUploaded);
		_downloadCallback = CallResult<LeaderboardScoresDownloaded_t>.Create(OnScoresDownloaded);
	}

	/// <summary>
	/// Upload le score de fin de run sur les leaderboards appropriés.
	/// Appelé par ScoreManager.SaveEndOfRun().
	/// </summary>
	public void UploadScore(int score, int nightsSurvived, string characterId)
	{
		if (!SteamManager.IsActive)
			return;

		// Score global
		UploadToBoard(BoardGlobal, score);

		// Score par personnage
		string charBoard = BoardCharacterPrefix + characterId;
		UploadToBoard(charBoard, score);

		// Nuits survivées (leaderboard séparé, trié par nuits)
		UploadToBoard(BoardNightsSurvived, nightsSurvived);

		// Weekly (même board name, le reset est géré côté Steamworks App Admin)
		UploadToBoard(BoardWeekly, score);
	}

	/// <summary>Charge les entrées d'un leaderboard pour affichage.</summary>
	public void LoadEntries(string boardName, LeaderboardRange range, int count = 10)
	{
		if (!SteamManager.IsActive)
			return;

		IsLoading = true;
		_lastEntries.Clear();

		if (_boardCache.TryGetValue(boardName, out SteamLeaderboard_t handle))
		{
			DownloadEntries(handle, range, count);
		}
		else
		{
			// Trouver d'abord le leaderboard, puis télécharger
			_pendingDownloadRange = range;
			_pendingDownloadCount = count;
			_pendingDownloadBoard = boardName;
			SteamAPICall_t call = SteamUserStats.FindOrCreateLeaderboard(
				boardName,
				ELeaderboardSortMethod.k_ELeaderboardSortMethodDescending,
				ELeaderboardDisplayType.k_ELeaderboardDisplayTypeNumeric
			);
			_findCallback.Set(call);
		}
	}

	/// <summary>Charge les entrées du leaderboard global.</summary>
	public void LoadGlobalTop(int count = 10)
	{
		LoadEntries(BoardGlobal, LeaderboardRange.Global, count);
	}

	/// <summary>Charge les entrées des amis.</summary>
	public void LoadFriendsTop(int count = 10)
	{
		LoadEntries(BoardGlobal, LeaderboardRange.Friends, count);
	}

	/// <summary>Charge les entrées autour du joueur.</summary>
	public void LoadAroundPlayer(int count = 10)
	{
		LoadEntries(BoardGlobal, LeaderboardRange.AroundUser, count);
	}

	public enum LeaderboardRange
	{
		Global,
		Friends,
		AroundUser
	}

	// --- Upload interne ---

	private void UploadToBoard(string boardName, int score)
	{
		if (_boardCache.TryGetValue(boardName, out SteamLeaderboard_t handle))
		{
			DoUpload(handle, score);
		}
		else
		{
			_pendingUploadScore = score;
			_pendingUploadBoard = boardName;
			SteamAPICall_t call = SteamUserStats.FindOrCreateLeaderboard(
				boardName,
				ELeaderboardSortMethod.k_ELeaderboardSortMethodDescending,
				ELeaderboardDisplayType.k_ELeaderboardDisplayTypeNumeric
			);
			_findCallback.Set(call);
		}
	}

	private void DoUpload(SteamLeaderboard_t handle, int score)
	{
		SteamAPICall_t call = SteamUserStats.UploadLeaderboardScore(
			handle,
			ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodKeepBest,
			score,
			null,
			0
		);
		_uploadCallback.Set(call);
	}

	// --- Download interne ---

	private void DownloadEntries(SteamLeaderboard_t handle, LeaderboardRange range, int count)
	{
		ELeaderboardDataRequest requestType = range switch
		{
			LeaderboardRange.Friends => ELeaderboardDataRequest.k_ELeaderboardDataRequestFriends,
			LeaderboardRange.AroundUser => ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobalAroundUser,
			_ => ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobal
		};

		int rangeStart = range == LeaderboardRange.AroundUser ? -count / 2 : 1;
		int rangeEnd = range == LeaderboardRange.AroundUser ? count / 2 : count;

		SteamAPICall_t call = SteamUserStats.DownloadLeaderboardEntries(handle, requestType, rangeStart, rangeEnd);
		_downloadCallback.Set(call);
	}

	// --- Callbacks ---

	// État temporaire pour chaîner find → upload/download
	private int _pendingUploadScore;
	private string _pendingUploadBoard;
	private string _pendingDownloadBoard;
	private LeaderboardRange _pendingDownloadRange;
	private int _pendingDownloadCount;

	private void OnLeaderboardFound(LeaderboardFindResult_t result, bool ioFailure)
	{
		if (ioFailure || result.m_bLeaderboardFound == 0)
		{
			GD.PushWarning("[SteamLeaderboards] Failed to find/create leaderboard.");
			IsLoading = false;
			return;
		}

		SteamLeaderboard_t handle = result.m_hSteamLeaderboard;

		// Cache le handle pour les appels suivants
		if (_pendingUploadBoard != null)
		{
			_boardCache[_pendingUploadBoard] = handle;
			DoUpload(handle, _pendingUploadScore);
			_pendingUploadBoard = null;
		}
		else if (_pendingDownloadBoard != null)
		{
			_boardCache[_pendingDownloadBoard] = handle;
			DownloadEntries(handle, _pendingDownloadRange, _pendingDownloadCount);
			_pendingDownloadBoard = null;
		}
	}

	private void OnScoreUploaded(LeaderboardScoreUploaded_t result, bool ioFailure)
	{
		bool success = !ioFailure && result.m_bSuccess != 0;
		if (success && result.m_bScoreChanged != 0)
			GD.Print($"[SteamLeaderboards] Score uploaded. New rank: {result.m_nGlobalRankNew} (was {result.m_nGlobalRankPrevious})");

		EmitSignal(SignalName.ScoreUploaded, success);
	}

	private void OnScoresDownloaded(LeaderboardScoresDownloaded_t result, bool ioFailure)
	{
		IsLoading = false;
		_lastEntries.Clear();

		if (ioFailure)
		{
			GD.PushWarning("[SteamLeaderboards] Failed to download entries.");
			EmitSignal(SignalName.EntriesLoaded, 0);
			return;
		}

		for (int i = 0; i < result.m_cEntryCount; i++)
		{
			SteamUserStats.GetDownloadedLeaderboardEntry(result.m_hSteamLeaderboardEntries, i, out LeaderboardEntry_t entry, null, 0);

			string name = SteamFriends.GetFriendPersonaName(entry.m_steamIDUser);
			_lastEntries.Add(new LeaderboardEntry(
				entry.m_nGlobalRank,
				name,
				entry.m_nScore,
				entry.m_steamIDUser
			));
		}

		GD.Print($"[SteamLeaderboards] Loaded {_lastEntries.Count} entries.");
		EmitSignal(SignalName.EntriesLoaded, _lastEntries.Count);
	}
}
