using System;
using System.IO;
using System.Text;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Écriture et lecture sûres des fichiers d'acquis (plan 26 Q2a). L'écriture passe par un temporaire vérifié puis
/// un renommage, la version précédente reste en <c>.bak</c>. Un fichier illisible est mis de côté, jamais écrasé.
/// </summary>
public static class SaveFile
{
    /// <summary>Issue d'une écriture : un échec laisse toujours la destination précédente intacte.</summary>
    public readonly record struct WriteResult(bool Succeeded, string Error)
    {
        public static WriteResult Ok => new(true, "");
        public static WriteResult Failed(string error) => new(false, error);
    }

    public enum ReadStatus
    {
        /// <summary>Aucun fichier : profil neuf.</summary>
        Missing,
        Loaded,
        /// <summary>Destination illisible ou absente, reprise sur la copie de secours.</summary>
        Recovered,
        /// <summary>Destination et copie illisibles : la destination est mise de côté, on repart à vide.</summary>
        Unreadable,
        /// <summary>Écrit par une version plus récente du jeu : lu au mieux, à ne jamais réécrire.</summary>
        FutureVersion,
        /// <summary>Lecture refusée par le système : rien ne doit être réécrit par-dessus.</summary>
        Inaccessible
    }

    public enum ContentCheck { Valid, Invalid, FutureVersion }

    public readonly record struct Parse<T>(ContentCheck Check, T Value, string Detail)
    {
        public static Parse<T> Valid(T value) => new(ContentCheck.Valid, value, "");
        public static Parse<T> Invalid(string detail) => new(ContentCheck.Invalid, default, detail);
        public static Parse<T> Future(T value, string detail) => new(ContentCheck.FutureVersion, value, detail);
    }

    public readonly record struct ReadResult<T>(ReadStatus Status, T Value, string Detail)
    {
        /// <summary>Vrai si le fichier ne doit pas être réécrit pendant cette session.</summary>
        public bool BlocksWrites => Status is ReadStatus.FutureVersion or ReadStatus.Inaccessible;
    }

    private const string TempSuffix = ".tmp";
    private const string BackupSuffix = ".bak";
    private static readonly UTF8Encoding Utf8 = new(false);

    public static WriteResult Write(string path, string content)
    {
        string target = ProjectSettings.GlobalizePath(path);
        string temp = target + TempSuffix;
        try
        {
            using (FileStream stream = new(temp, FileMode.Create, System.IO.FileAccess.Write, FileShare.None))
            {
                byte[] bytes = Utf8.GetBytes(content);
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }

            if (File.ReadAllText(temp, Utf8) != content)
            {
                TryDelete(temp);
                return WriteResult.Failed($"relecture différente du contenu écrit ({path})");
            }

            if (File.Exists(target))
                File.Replace(temp, target, target + BackupSuffix, true);
            else
                File.Move(temp, target);
            return WriteResult.Ok;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            TryDelete(temp);
            return WriteResult.Failed($"{path} : {ex.Message}");
        }
    }

    public static ReadResult<T> Read<T>(string path, Func<string, Parse<T>> parse)
    {
        string target = ProjectSettings.GlobalizePath(path);
        string backup = target + BackupSuffix;

        // Un temporaire restant vient d'une écriture interrompue. Si la destination existe, elle fait foi ; sinon le
        // remplacement s'est arrêté entre ses deux renommages et le temporaire, s'il est sain, est la dernière version.
        string temp = target + TempSuffix;
        if (File.Exists(temp))
        {
            if (!File.Exists(target) && TryReadText(temp, out string pending, out _)
                && parse(pending).Check == ContentCheck.Valid && TryPromote(temp, target))
                GD.Print($"[SaveFile] {path} repris depuis une écriture interrompue");
            else
            {
                GD.PushWarning($"[SaveFile] Écriture interrompue ignorée : {path}{TempSuffix}");
                TryDelete(temp);
            }
        }

        string primaryDetail = "";
        if (File.Exists(target))
        {
            if (!TryReadText(target, out string text, out string error))
                return new ReadResult<T>(ReadStatus.Inaccessible, default, error);

            Parse<T> primary = parse(text);
            if (primary.Check == ContentCheck.Valid)
                return new ReadResult<T>(ReadStatus.Loaded, primary.Value, "");
            if (primary.Check == ContentCheck.FutureVersion)
                return new ReadResult<T>(ReadStatus.FutureVersion, primary.Value, primary.Detail);

            primaryDetail = primary.Detail;
            if (!TryQuarantine(target, out string quarantine))
                return new ReadResult<T>(ReadStatus.Inaccessible, default, $"{primary.Detail} ; mise de côté impossible : {quarantine}");
            GD.PushWarning($"[SaveFile] {path} illisible ({primary.Detail}), conservé dans {quarantine}");
        }

        if (!File.Exists(backup))
        {
            return string.IsNullOrEmpty(primaryDetail)
                ? new ReadResult<T>(ReadStatus.Missing, default, "")
                : new ReadResult<T>(ReadStatus.Unreadable, default, primaryDetail);
        }

        if (!TryReadText(backup, out string backupText, out string backupError))
            return new ReadResult<T>(ReadStatus.Inaccessible, default, backupError);

        Parse<T> fallback = parse(backupText);
        if (fallback.Check == ContentCheck.FutureVersion)
            return new ReadResult<T>(ReadStatus.FutureVersion, fallback.Value, fallback.Detail);
        if (fallback.Check == ContentCheck.Invalid)
        {
            string detail = string.IsNullOrEmpty(primaryDetail) ? fallback.Detail : primaryDetail;
            return new ReadResult<T>(ReadStatus.Unreadable, default, detail);
        }

        // La destination est absente à ce stade : la restaurer laisse la copie de secours en place.
        WriteResult restored = Write(path, backupText);
        if (!restored.Succeeded)
            GD.PushWarning($"[SaveFile] Restauration de {path} impossible : {restored.Error}");
        GD.Print($"[SaveFile] {path} repris depuis sa copie de secours");
        return new ReadResult<T>(ReadStatus.Recovered, fallback.Value, primaryDetail);
    }

    private static bool TryReadText(string file, out string text, out string error)
    {
        try
        {
            text = File.ReadAllText(file, Utf8);
            error = "";
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            text = "";
            error = ex.Message;
            return false;
        }
    }

    /// <summary>Déplace un fichier illisible à côté de lui, pour diagnostic ; en cas d'échec, renvoie l'erreur.</summary>
    private static bool TryQuarantine(string target, out string destinationOrError)
    {
        string directory = Path.GetDirectoryName(target) ?? "";
        string name = Path.GetFileNameWithoutExtension(target);
        string extension = Path.GetExtension(target);
        string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        string destination = Path.Combine(directory, $"{name}.corrupt-{stamp}{extension}");
        for (int index = 2; File.Exists(destination); index++)
            destination = Path.Combine(directory, $"{name}.corrupt-{stamp}-{index}{extension}");
        try
        {
            File.Move(target, destination);
            destinationOrError = destination;
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            destinationOrError = ex.Message;
            return false;
        }
    }

    private static bool TryPromote(string temp, string target)
    {
        try
        {
            File.Move(temp, target);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static void TryDelete(string file)
    {
        try
        {
            if (File.Exists(file))
                File.Delete(file);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            GD.PushWarning($"[SaveFile] Suppression impossible de {file} : {ex.Message}");
        }
    }
}
