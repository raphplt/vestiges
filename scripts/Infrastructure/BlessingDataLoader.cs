using System.Collections.Generic;

namespace Vestiges.Infrastructure;

/// <summary>Bénédictions des Mémoriaux (data/progression/blessings.json).</summary>
public static class BlessingDataLoader
{
    private static readonly List<StatEffectData> _blessings = new();
    private static bool _loaded;

    public static IReadOnlyList<StatEffectData> All
    {
        get
        {
            if (!_loaded)
            {
                _loaded = true;
                StatEffectReader.Read("res://data/progression/blessings.json", "blessings", _blessings);
            }
            return _blessings;
        }
    }
}
