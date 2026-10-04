using System;
using System.Collections.Generic;
using Steamworks;

namespace Vestiges.Infrastructure.Steam;

/// <summary>
/// Appels Steamworks des classements. <see cref="LeaderboardQueue"/> n'en lance qu'un à la fois : un <c>CallResult</c>
/// par sorte d'appel suffit, et la file ne réarme jamais un <c>CallResult</c> dont la réponse est encore attendue.
/// À ne construire que si Steam est actif (types Steamworks absents hors x86/x64).
/// </summary>
internal sealed class SteamLeaderboardBackend : ILeaderboardBackend
{
	private readonly CallResult<LeaderboardFindResult_t> _findCall;
	private readonly CallResult<LeaderboardScoreUploaded_t> _uploadCall;
	private readonly CallResult<LeaderboardScoresDownloaded_t> _downloadCall;
	private Action<bool, ulong> _onFound;
	private Action<bool, bool, int> _onUploaded;
	private Action<bool, IReadOnlyList<LeaderboardQueue.Entry>> _onDownloaded;

	public SteamLeaderboardBackend()
	{
		_findCall = CallResult<LeaderboardFindResult_t>.Create(OnFound);
		_uploadCall = CallResult<LeaderboardScoreUploaded_t>.Create(OnUploaded);
		_downloadCall = CallResult<LeaderboardScoresDownloaded_t>.Create(OnDownloaded);
	}

	public void Find(string board, Action<bool, ulong> done)
	{
		SteamAPICall_t call = SteamUserStats.FindOrCreateLeaderboard(
			board,
			ELeaderboardSortMethod.k_ELeaderboardSortMethodDescending,
			ELeaderboardDisplayType.k_ELeaderboardDisplayTypeNumeric);
		if (call == SteamAPICall_t.Invalid)
		{
			done(false, 0);
			return;
		}
		_onFound = done;
		_findCall.Set(call);
	}

	public void Upload(ulong handle, int score, Action<bool, bool, int> done)
	{
		SteamAPICall_t call = SteamUserStats.UploadLeaderboardScore(
			new SteamLeaderboard_t(handle),
			ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodKeepBest,
			score,
			null,
			0);
		if (call == SteamAPICall_t.Invalid)
		{
			done(false, false, 0);
			return;
		}
		_onUploaded = done;
		_uploadCall.Set(call);
	}

	public void Download(ulong handle, LeaderboardQueue.EntryRange range, int count, Action<bool, IReadOnlyList<LeaderboardQueue.Entry>> done)
	{
		ELeaderboardDataRequest request = range switch
		{
			LeaderboardQueue.EntryRange.Friends => ELeaderboardDataRequest.k_ELeaderboardDataRequestFriends,
			LeaderboardQueue.EntryRange.AroundUser => ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobalAroundUser,
			_ => ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobal,
		};
		int rangeStart = range == LeaderboardQueue.EntryRange.AroundUser ? -count / 2 : 1;
		int rangeEnd = range == LeaderboardQueue.EntryRange.AroundUser ? count / 2 : count;

		SteamAPICall_t call = SteamUserStats.DownloadLeaderboardEntries(new SteamLeaderboard_t(handle), request, rangeStart, rangeEnd);
		if (call == SteamAPICall_t.Invalid)
		{
			done(false, null);
			return;
		}
		_onDownloaded = done;
		_downloadCall.Set(call);
	}

	public void Cancel()
	{
		_findCall.Cancel();
		_uploadCall.Cancel();
		_downloadCall.Cancel();
		_onFound = null;
		_onUploaded = null;
		_onDownloaded = null;
	}

	private void OnFound(LeaderboardFindResult_t result, bool ioFailure)
	{
		Action<bool, ulong> done = _onFound;
		_onFound = null;
		done?.Invoke(!ioFailure && result.m_bLeaderboardFound != 0, result.m_hSteamLeaderboard.m_SteamLeaderboard);
	}

	private void OnUploaded(LeaderboardScoreUploaded_t result, bool ioFailure)
	{
		Action<bool, bool, int> done = _onUploaded;
		_onUploaded = null;
		done?.Invoke(!ioFailure && result.m_bSuccess != 0, result.m_bScoreChanged != 0, result.m_nGlobalRankNew);
	}

	private void OnDownloaded(LeaderboardScoresDownloaded_t result, bool ioFailure)
	{
		Action<bool, IReadOnlyList<LeaderboardQueue.Entry>> done = _onDownloaded;
		_onDownloaded = null;
		if (done == null)
			return;
		if (ioFailure)
		{
			done(false, null);
			return;
		}

		List<LeaderboardQueue.Entry> entries = new(result.m_cEntryCount);
		for (int i = 0; i < result.m_cEntryCount; i++)
		{
			SteamUserStats.GetDownloadedLeaderboardEntry(result.m_hSteamLeaderboardEntries, i, out LeaderboardEntry_t entry, null, 0);
			entries.Add(new LeaderboardQueue.Entry(
				entry.m_nGlobalRank,
				SteamFriends.GetFriendPersonaName(entry.m_steamIDUser),
				entry.m_nScore,
				entry.m_steamIDUser.m_SteamID));
		}
		done(true, entries);
	}
}
