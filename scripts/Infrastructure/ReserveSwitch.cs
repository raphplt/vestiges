using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Réserve du LE-JEU (plan 56 B2) : Résurgences, micro-événements et chasses de Souverains, Ateliers, Puits et stèles
/// sont éteints en run par le champ <c>enabled</c> de leurs données, sans être supprimés. Une run garde ainsi une seule
/// horloge, celle de la terre. Dans les outils, <c>--reserve</c> les rallume pour les bancs et régressions qui les
/// exercent, comme <c>--no-lands</c> rend la grande carte.
/// </summary>
public static class ReserveSwitch
{
    private static bool? _revived;

    /// <summary>Les systèmes en réserve sont rallumés pour un banc (<c>--reserve</c>, outils seulement).</summary>
    public static bool Revived => _revived ??= ReadRevived();

    /// <summary>Un système en réserve tourne-t-il ? Son interrupteur de données, ou <c>--reserve</c>.</summary>
    public static bool IsOn(bool enabledInData) => enabledInData || Revived;

    private static bool ReadRevived()
    {
#if TOOLS
        return System.Array.IndexOf(OS.GetCmdlineUserArgs(), "--reserve") >= 0;
#else
        return false;
#endif
    }
}
