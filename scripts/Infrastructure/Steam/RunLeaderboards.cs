using System;
using System.Collections.Generic;
using System.Globalization;

namespace Vestiges.Infrastructure.Steam;

/// <summary>
/// Classements Steam alimentés par une fin de run (plan 26 Q3). Les noms sont des clés du service Steam :
/// le jeu crée chaque tableau à son premier envoi. Sans type Steamworks, pour rester chargeable hors x86/x64.
/// </summary>
public static class RunLeaderboards
{
	public const string BoardGlobal = "Vestiges_Global";
	public const string BoardCrisesSurvived = "Vestiges_Crises";
	public const string BoardCharacterPrefix = "Vestiges_Char_";
	public const string BoardWeeklyPrefix = "Vestiges_Weekly_";

	/// <summary>Un score à envoyer sur un classement.</summary>
	public readonly record struct Submission(string Board, int Score);

	/// <summary>
	/// Toutes les runs, un tableau par semaine ISO (DECISIONS §64) : la remise à zéro ne dépend d'aucun réglage côté Steam.
	/// </summary>
	public static string WeeklyBoard(DateTime utcDate)
	{
		int year = ISOWeek.GetYear(utcDate);
		int week = ISOWeek.GetWeekOfYear(utcDate);
		return string.Create(CultureInfo.InvariantCulture, $"{BoardWeeklyPrefix}{year}-W{week:00}");
	}

	public static List<Submission> ForRun(int score, int crisesSurvived, string characterId, DateTime utcNow)
	{
		List<Submission> submissions = new()
		{
			new Submission(BoardGlobal, score),
		};
		if (!string.IsNullOrEmpty(characterId))
			submissions.Add(new Submission(BoardCharacterPrefix + characterId, score));
		submissions.Add(new Submission(BoardCrisesSurvived, crisesSurvived));
		submissions.Add(new Submission(WeeklyBoard(utcNow), score));
		return submissions;
	}
}
