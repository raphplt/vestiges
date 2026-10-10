using System.Collections.Generic;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Règlement d'une fin de run (plan 26 Q2b). La méta s'engage d'abord, en une écriture qui garde le relevé en
/// attente ; l'historique suit, puis le relevé est retiré. Une interruption entre les deux laisse des acquis déjà
/// comptés et un historique que <see cref="RepairPendingHistory"/> complète sans rien réattribuer.
/// </summary>
public static class RunSettlement
{
    /// <summary><c>Saved</c> : acquis engagés. <c>HistoryPending</c> : acquis sûrs, relevé à inscrire au prochain passage au camp.</summary>
    public readonly record struct Outcome(bool Saved, bool HistoryPending, bool AlreadySettled, string Error);

    public static Outcome Settle(RunRecord record, int vestiges)
    {
        RepairPendingHistory();
        MetaSaveManager.Settlement meta = MetaSaveManager.SettleRun(record, vestiges);
        if (meta.AlreadySettled)
            return new Outcome(true, false, true, "");
        if (!meta.Write.Succeeded)
        {
            GD.PushError($"[RunSettlement] Acquis de la run non enregistrés : {meta.Write.Error}");
            return new Outcome(false, false, false, meta.Write.Error);
        }

        string historyError = CommitHistory(record);
        return new Outcome(true, historyError.Length > 0, false, historyError);
    }

    /// <summary>Inscrit dans l'historique les runs réglées qui n'y sont pas encore ; renvoie la première erreur, vide sinon.</summary>
    public static string RepairPendingHistory()
    {
        foreach (RunRecord pending in MetaSaveManager.GetPendingHistory())
        {
            bool missing = !RunHistoryManager.Contains(pending.RunId);
            string error = CommitHistory(pending);
            if (error.Length > 0)
                return error;
            if (missing)
                GD.Print($"[RunSettlement] Historique complété : run {pending.RunId}");
        }
        return "";
    }

    private static string CommitHistory(RunRecord record)
    {
        if (!RunHistoryManager.Contains(record.RunId))
        {
            SaveFile.WriteResult history = RunHistoryManager.SaveRun(record);
            if (!history.Succeeded)
            {
                GD.PushError($"[RunSettlement] Historique non écrit, relevé gardé en attente : {history.Error}");
                return history.Error;
            }
        }

        // L'historique est sûr : un échec ici laisse seulement le relevé en attente, retiré au prochain règlement.
        SaveFile.WriteResult cleared = MetaSaveManager.ClearPendingHistory(record.RunId);
        if (!cleared.Succeeded)
            GD.PushWarning($"[RunSettlement] Relevé en attente non retiré : {cleared.Error}");
        return "";
    }
}
