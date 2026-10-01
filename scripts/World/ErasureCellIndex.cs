using System.Collections.Generic;
using System.Numerics;
using Godot;

namespace Vestiges.World;

/// <summary>
/// Cellules encore susceptibles de perdre de la mémoire, dans l'ordre de leur première apparition.
/// Un bit par cellule permet de sauter les blocs éteints sans déplacer les indices ni réordonner les signaux.
/// Les états de mémoire et de phase restent dans ErasureManager, même quand leur bit est éteint.
/// </summary>
internal sealed class ErasureCellIndex
{
    private readonly Dictionary<Vector2I, int> _indices = new();
    private readonly List<Vector2I> _cells = new();
    private readonly List<ulong> _activeWords = new();

    public int ActiveCount { get; private set; }
    public int WordCount => _activeWords.Count;

    public void SetActive(Vector2I cell, bool active)
    {
        if (!_indices.TryGetValue(cell, out int index))
        {
            index = _cells.Count;
            _indices.Add(cell, index);
            _cells.Add(cell);
            if (index % 64 == 0)
                _activeWords.Add(0);
        }
        int word = index / 64;
        ulong bit = 1UL << (index % 64);
        bool wasActive = (_activeWords[word] & bit) != 0;
        if (active == wasActive)
            return;
        _activeWords[word] = active ? _activeWords[word] | bit : _activeWords[word] & ~bit;
        ActiveCount += active ? 1 : -1;
    }

    public Enumerator GetEnumerator() => new(this);

    public struct Enumerator
    {
        private readonly ErasureCellIndex _index;
        private readonly int _limit;
        private int _next;
        public Vector2I Current { get; private set; }

        public Enumerator(ErasureCellIndex index)
        {
            _index = index;
            // Même frontière que l'ancien instantané : les nouvelles cellules attendent le prochain pas.
            _limit = index._cells.Count;
            _next = 0;
            Current = default;
        }

        public bool MoveNext()
        {
            while (_next < _limit)
            {
                int word = _next / 64;
                // Relire les bits permet à un signal de raviver une ancienne cellule encore à venir.
                // Les indices déjà traités restent exclus, même s'ils ont été ravivés entre-temps.
                ulong pending = _index._activeWords[word] & (ulong.MaxValue << (_next % 64));
                if (pending == 0)
                {
                    _next = (word + 1) * 64;
                    continue;
                }
                int cellIndex = word * 64 + BitOperations.TrailingZeroCount(pending);
                if (cellIndex >= _limit)
                    return false;
                Current = _index._cells[cellIndex];
                _next = cellIndex + 1;
                return true;
            }
            return false;
        }
    }
}
