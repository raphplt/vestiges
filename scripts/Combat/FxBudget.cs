using Godot;

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

    private static readonly int[] Full = { 320, 32, 12, 24 };
    private static readonly int[] Reduced = { 120, 12, 5, 12 };
    private static readonly int[] Used = new int[KindKeys.Length];
    private static readonly long[] Dropped = new long[KindKeys.Length];
    private static ulong _frame = ulong.MaxValue;
    private static bool _loaded;

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

    private static void EnsureLoaded()
    {
        if (_loaded)
            return;
        _loaded = true;
        using FileAccess file = FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Read);
        if (file == null)
            return;
        Json json = new();
        if (json.Parse(file.GetAsText()) != Error.Ok)
        {
            GD.PushError($"[FxBudget] {ConfigPath} invalide : {json.GetErrorMessage()}");
            return;
        }
        Godot.Collections.Dictionary root = json.Data.AsGodotDictionary();
        Read(root, "full", Full);
        Read(root, "reduced", Reduced);
    }

    private static void Read(Godot.Collections.Dictionary root, string level, int[] limits)
    {
        if (!root.ContainsKey(level))
            return;
        Godot.Collections.Dictionary values = root[level].AsGodotDictionary();
        for (int i = 0; i < KindKeys.Length; i++)
            if (values.ContainsKey(KindKeys[i]))
                limits[i] = Mathf.Max(0, (int)values[KindKeys[i]].AsDouble());
    }
}
