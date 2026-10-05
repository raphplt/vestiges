using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.Tests;

/// <summary>
/// Relevé des valeurs effectives de la courbe d'XP, du barème du score et du Péril (plan 26 Q7a), à comparer avant et
/// après un changement de lecteur au même commit de base. N'utilise que des accès présents des deux côtés.
/// </summary>
public partial class CatalogValuesProbe : Node
{
    public override void _Ready()
    {
        List<string> lines = new();
        XpCurveConfig curve = XpCurveConfig.Load();
        for (int level = 1; level <= 250; level++)
            lines.Add(Invariant($"xp {level} {curve.CostOf(level):R}"));
        EnemyDataLoader.Load();
        ScoreConfig score = ScoreConfig.Load();
        List<string> ids = new(EnemyDataLoader.GetAllIds()) { "inconnu" };
        ids.Sort(StringComparer.Ordinal);
        foreach (string id in ids)
            lines.Add($"score {id} {score.KillPoints(id)}");
        for (int peril = 0; peril <= 10; peril++)
            lines.Add(Invariant($"peril {peril} {PerilDataLoader.EnemyCountMultiplier(peril):R} {PerilDataLoader.EnemyHpMultiplier(peril):R} {PerilDataLoader.EnemyDamageMultiplier(peril):R} {PerilDataLoader.XpMultiplier(peril):R} {PerilDataLoader.ScoreMultiplier(peril):R} {PerilDataLoader.RaritySteps(peril):R}"));
        lines.Add($"peril max {PerilDataLoader.Max} banish {PerilDataLoader.BanishFree} {PerilDataLoader.BanishPerilDivisor}");
        foreach (string line in lines)
            GD.Print($"[CatalogValuesProbe] {line}");
        GD.Print($"[CatalogValuesProbe] RESULT lines={lines.Count}");
        GetTree().Quit(0);
    }

    private static string Invariant(FormattableString text) => text.ToString(CultureInfo.InvariantCulture);
}
