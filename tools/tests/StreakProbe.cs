using System;
using System.Globalization;
using System.Text;
using Godot;
using Vestiges.Core;

namespace Vestiges.Tests;

/// <summary>
/// La Série d'une run mesurée (plan 52 S5) : premier passage à chaque rang (temps de jeu), meilleure Série et son rang,
/// encaissements et Essence encaissée, coups reçus qui l'ont coupée. Vide si la Série est éteinte en run.
/// </summary>
internal sealed class StreakProbe : IDisposable
{
    private readonly EventBus _bus;
    private readonly SceneTree _tree;
    private readonly double[] _firstAtRank;
    private double _time;
    private int _best;
    private int _bestRank;
    private int _cashOuts;
    private int _essence;
    private int _cuts;
    private int _cashedAboveRank1;

    public StreakProbe(EventBus bus, SceneTree tree, int rankCount)
    {
        _bus = bus;
        _tree = tree;
        _firstAtRank = new double[rankCount + 1];
        Array.Fill(_firstAtRank, -1.0);
        _bus.StreakChanged += OnStreak;
        _tree.PhysicsFrame += OnPhysics;
    }

    public void Dispose()
    {
        _bus.StreakChanged -= OnStreak;
        _tree.PhysicsFrame -= OnPhysics;
    }

    // Temps de jeu : le relevé tourne en --fixed-fps, une image physique vaut 1/60 s même accélérée.
    private void OnPhysics()
    {
        if (!_tree.Paused)
            _time += 1.0 / Engine.PhysicsTicksPerSecond;
    }

    private void OnStreak(StreakChange change)
    {
        if (change.Count > _best)
        {
            _best = change.Count;
            _bestRank = change.Rank;
        }
        if (change.Rank > 0 && change.Rank < _firstAtRank.Length && _firstAtRank[change.Rank] < 0)
            _firstAtRank[change.Rank] = _time;
        if (change.Kind == StreakChangeKind.CashOut)
        {
            _cashOuts++;
            _essence += change.Essence;
            if (change.PreviousRank >= 2)
                _cashedAboveRank1++;
        }
        else if (change.Kind == StreakChangeKind.Hit)
            _cuts++;
    }

    public void Append(StringBuilder summary)
    {
        summary.Append(CultureInfo.InvariantCulture,
            $" streak_best={_best} streak_best_rank={_bestRank} streak_cashouts={_cashOuts} streak_cashouts_rank2={_cashedAboveRank1} streak_essence={_essence} streak_cuts={_cuts}");
        for (int rank = 1; rank < _firstAtRank.Length; rank++)
            summary.Append(CultureInfo.InvariantCulture, $" streak_rank{rank}_s={(_firstAtRank[rank] < 0 ? "-" : _firstAtRank[rank].ToString("F0", CultureInfo.InvariantCulture))}");
    }
}
