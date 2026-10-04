using System;
using System.Collections.Generic;

namespace Vestiges.Infrastructure.Steam;

/// <summary>
/// Les trois appels asynchrones du service de classements. <see cref="LeaderboardQueue"/> n'en lance qu'un à la fois,
/// et un appel ne rappelle qu'une fois. Séparé de Steamworks pour être remplacé par un faux service dans les tests.
/// </summary>
public interface ILeaderboardBackend
{
	/// <summary>Trouve le tableau, ou le crée ; rappelle avec son identifiant.</summary>
	void Find(string board, Action<bool, ulong> done);

	/// <summary>Envoie un score en gardant le meilleur ; rappelle avec la réussite, le changement de score et le rang.</summary>
	void Upload(ulong handle, int score, Action<bool, bool, int> done);

	void Download(ulong handle, LeaderboardQueue.EntryRange range, int count, Action<bool, IReadOnlyList<LeaderboardQueue.Entry>> done);

	/// <summary>Abandonne l'appel en cours : son rappel ne viendra pas.</summary>
	void Cancel();
}
