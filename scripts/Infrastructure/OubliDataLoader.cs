using System.Collections.Generic;

namespace Vestiges.Infrastructure;

/// <summary>Oublis des Failles (data/progression/oublis.json).</summary>
public static class OubliDataLoader
{
    private static readonly List<StatEffectData> _oublis = new();
    private static bool _loaded;

    public static IReadOnlyList<StatEffectData> All
    {
        get
        {
            if (!_loaded)
            {
                _loaded = true;
                StatEffectReader.Read("res://data/progression/oublis.json", "oublis", _oublis);
            }
            return _oublis;
        }
    }
}
