using System.Text.Json;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.Combat;

public enum FxBudgetKind
{
    Sparks,
    Shapes,
    Deaths,
    Numbers,
}

/// <summary>
/// Budget d'effets par frame (plan 02 J0) : quand un combat dense déclenche plus d'effets qu'on n'en peut lire,
/// les suivants sont écartés au lieu de s'empiler, et le plafond baisse avec le réglage « Particules ».
/// Ne passent ici que les effets décoratifs : les attaques ennemies (information de danger) et les critiques
/// ne sont jamais écartés. Plafonds : data/scaling/fx_budget.json.
/// </summary>
public static class FxBudget
{
    private const string ConfigPath = "res://data/scaling/fx_budget.json";
    private static readonly string[] KindKeys = { "sparks_per_frame", "shapes_per_frame", "deaths_per_frame", "numbers_per_frame" };

    /// <summary>Plafonds d'avant le fichier, gardés pour une catégorie absente.</summary>
    private static readonly int[] DefaultFull = { 320, 32, 12, 24 };
    private static readonly int[] DefaultReduced = { 120, 12, 5, 12 };
    private static readonly int[] Full = (int[])DefaultFull.Clone();
    private static readonly int[] Reduced = (int[])DefaultReduced.Clone();
    private static readonly int[] Used = new int[KindKeys.Length];
    private static readonly long[] Dropped = new long[KindKeys.Length];
    private static ulong _frame = ulong.MaxValue;
    private static bool _loaded;
    private static string _loadError;

    /// <summary>
    /// Accorde jusqu'à <paramref name="wanted"/> unités de la catégorie pour la frame en cours ;
    /// renvoie le nombre accordé (0 quand le plafond est atteint, ou les particules coupées hors chiffres de dégâts).
    /// </summary>
    public static int Take(FxBudgetKind kind, int wanted)
    {
        if (wanted <= 0)
            return 0;
        ParticleLevel level = CombatFxSettings.ParticleLevel;
        // Les chiffres de dégâts informent : particules coupées, ils gardent le plafond réduit.
        if (level == ParticleLevel.Off && kind != FxBudgetKind.Numbers)
            return 0;
        EnsureLoaded();
        ulong frame = Engine.GetProcessFrames();
        if (frame != _frame)
        {
            _frame = frame;
            System.Array.Clear(Used);
        }

        int index = (int)kind;
        int limit = level == ParticleLevel.Full ? Full[index] : Reduced[index];
        int granted = Mathf.Min(wanted, Mathf.Max(0, limit - Used[index]));
        Used[index] += granted;
        Dropped[index] += wanted - granted;
        return granted;
    }

    public static bool TryTake(FxBudgetKind kind) => Take(kind, 1) == 1;

    /// <summary>Unités écartées depuis le lancement, pour les bancs.</summary>
    public static long DroppedCount(FxBudgetKind kind) => Dropped[(int)kind];

    /// <summary>
    /// Plafonds lus strictement une fois (plan 26 Q7g-3) ; refusés, ceux d'avant restent et la raison est rendue (le
    /// chargement de la run s'arrête, GameBootstrap).
    /// </summary>
    public static bool TryLoad(out string error)
    {
        EnsureLoaded();
        error = _loadError;
        return error == null;
    }

    private static void EnsureLoaded()
    {
        if (_loaded)
            return;
        _loaded = true;
        _loadError = $"{ConfigPath} : lecture interrompue";
        using FileAccess file = FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Read);
        if (file == null)
            _loadError = $"{ConfigPath} : fichier illisible";
        else if (TryParse(file.GetAsText(), out int[] full, out int[] reduced, out _loadError))
        {
            full.CopyTo(Full, 0);
            reduced.CopyTo(Reduced, 0);
        }
        else
            _loadError = $"{ConfigPath} : {_loadError}";
        if (_loadError != null)
            GD.PushError($"[FxBudget] {_loadError}");
    }

    /// <summary>Plafonds par niveau ; un niveau ou une catégorie absents gardent les valeurs d'avant, le réduit ne dépasse pas le plein.</summary>
    public static bool TryParse(string text, out int[] full, out int[] reduced, out string error)
    {
        full = (int[])DefaultFull.Clone();
        reduced = (int[])DefaultReduced.Clone();
        try
        {
            using JsonDocument document = JsonDocument.Parse(text);
            JsonConfigReader reader = new(document.RootElement);
            reader.AllowOnly(reader.Root, "budget d'effets", "full", "reduced");
            Read(reader, "full", full);
            Read(reader, "reduced", reduced);
            for (int i = 0; i < KindKeys.Length && reader.Error == null; i++)
            {
                if (reduced[i] > full[i])
                    reader.Fail($"reduced.{KindKeys[i]} ≤ full.{KindKeys[i]} attendu ({reduced[i]} > {full[i]})");
            }
            error = reader.Error;
            return error == null;
        }
        catch (JsonException exception)
        {
            error = $"JSON invalide : {exception.Message}";
            return false;
        }
    }

    private static void Read(JsonConfigReader reader, string level, int[] limits)
    {
        if (!JsonConfigReader.Has(reader.Root, level))
            return;
        JsonElement values = reader.Section(level);
        reader.AllowOnly(values, level, KindKeys);
        for (int i = 0; i < KindKeys.Length; i++)
        {
            if (JsonConfigReader.Has(values, KindKeys[i]))
                limits[i] = reader.Integer(values, KindKeys[i], 0, 10000);
        }
    }
}
