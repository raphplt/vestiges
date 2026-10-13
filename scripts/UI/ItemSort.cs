using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;

namespace Vestiges.UI;

public enum ItemSortMode { RarityAscending, RarityDescending, Name, Original }

/// <summary>Préférence de présentation commune aux objets, conservée en session sans modifier les inventaires.</summary>
public static class ItemSort
{
    public static ItemSortMode Mode { get; private set; } = ItemSortMode.RarityAscending;
    public static event Action Changed;

    public static void Select(ItemSortMode mode)
    {
        if (mode == Mode)
            return;
        Mode = mode;
        Changed?.Invoke();
    }

    public static List<T> Order<T>(IEnumerable<T> source, Func<T, string> rarity, Func<T, string> name,
        Func<T, bool> available = null) => Order(Mode, source, rarity, name, available);

    /// <summary>Ordre imposé, indépendant de la préférence de session (sac de la pause).</summary>
    public static List<T> Order<T>(ItemSortMode mode, IEnumerable<T> source, Func<T, string> rarity, Func<T, string> name,
        Func<T, bool> available = null)
    {
        if (mode == ItemSortMode.Original)
            return source.ToList();
        StringComparer names = StringComparer.Create(CultureInfo.GetCultureInfo(TranslationServer.GetLocale().Replace('_', '-')), true);
        // Les objets du monde sans rareté restent après les cinq rangs, dans les deux sens de tri.
        IOrderedEnumerable<T> ordered = source.OrderBy(item => RarityArt.Rank(rarity(item)) < 0);
        if (mode == ItemSortMode.Name)
            return ordered.ThenBy(name, names).ToList();
        ordered = mode == ItemSortMode.RarityDescending
            ? ordered.ThenByDescending(item => RarityArt.Rank(rarity(item)))
            : ordered.ThenBy(item => RarityArt.Rank(rarity(item)));
        if (available != null)
            ordered = ordered.ThenByDescending(available);
        return ordered.ThenBy(name, names).ToList();
    }
}
