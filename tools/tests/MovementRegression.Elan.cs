using System;
using System.Threading.Tasks;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Tests;

/// <summary>Élan (plan 52, lot E1) : règles du module seul, puis effet réel sur la course du joueur.</summary>
public partial class MovementRegression
{
    private const float ElanStep = 1f / 60f;

    private async Task RunElanChecks()
    {
        CheckElanConfig();
        CheckElanModule();
        await CheckElanPlayer();
    }

    private void CheckElanConfig()
    {
        ElanConfig config = ElanConfig.Load();
        Check(config.Tiers.Count == 3 && !config.EnabledInRuns, $"élan : trois paliers, éteint en run ({config.Tiers.Count})");
        string valid = FileAccess.GetFileAsString("res://data/movement/elan.json");
        Check(ElanConfig.Parse(valid.Replace("\"from\": 0.67", "\"from\": 0.2"), out string order) == null && order.Contains("croissants"),
            $"élan : seuils non croissants refusés ({order})");
        Check(ElanConfig.Parse(valid.Replace("\"hit_loss\"", "\"hit_lost\""), out string unknown) == null && unknown.Contains("inconnue"),
            $"élan : clé inconnue refusée ({unknown})");
        Check(ElanConfig.Parse(valid.Replace("\"cap\": 0.34", "\"cap\": 0.2"), out string cap) == null && cap.Contains("premier palier"),
            $"élan : plafond de course sous le premier palier refusé ({cap})");
    }

    private void CheckElanModule()
    {
        ElanConfig config = ElanConfig.Load();
        PlayerElan elan = new(config);
        elan.Gain(0.5f, "geste");
        Check(elan.Value == 0f, "élan éteint : un geste ne donne rien");

        elan.SetEnabled(true);
        Advance(elan, 2f, config.SpeedMinRatio - 0.05f);
        Check(elan.Value == 0f, $"élan : une course trop lente ne le nourrit pas ({elan.Value:F3})");
        Advance(elan, 12f, 1f);
        Check(Mathf.IsEqualApprox(elan.Value, config.SpeedCap) && elan.Tier == 1,
            $"élan : la course seule plafonne au premier palier ({elan.Value:F3}, palier {elan.Tier})");

        elan.Gain(0.5f, "geste");
        float boosted = elan.Value;
        Check(elan.Tier == 2 && elan.LastChange == "geste", $"élan : un geste dépasse le plafond de la course ({boosted:F3}, palier {elan.Tier})");
        Advance(elan, 1f, 1f);
        Check(Math.Abs(boosted - config.UpkeepDecayPerSecond - elan.Value) < 0.002f,
            $"élan : au-dessus du plafond, il s'érode en courant ({boosted:F3} → {elan.Value:F3})");

        float beforeStop = elan.Value;
        Advance(elan, config.StillGraceSeconds * 0.5f, 0f);
        Check(elan.Value == beforeStop, "élan : un arrêt bref est toléré");
        Advance(elan, 1f, 0f);
        Check(elan.Value < beforeStop - config.StillDecayPerSecond * 0.5f, $"élan : il fond à l'arrêt ({beforeStop:F3} → {elan.Value:F3})");

        elan.Gain(1f, "geste");
        elan.Hit();
        Check(Mathf.IsEqualApprox(elan.Value, 1f - config.HitLoss) && elan.Tier == 2,
            $"élan : un coup fait perdre un palier ({elan.Value:F3}, palier {elan.Tier})");
        elan.SetEnabled(false);
        Check(elan.Value == 0f && elan.Tier == 0, "élan éteint : remis à zéro");
    }

    /// <summary>Le palier atteint en courant accélère vraiment le joueur ; un coup le retire.</summary>
    private async Task CheckElanPlayer()
    {
        await ReadyMobility();
        SetActions(Vector2.Right);
        await Step(10);
        float baseSpeed = (await Measure(60)).Length();
        _player.SetElanEnabled(true);
        float runSeconds = ElanConfig.Load().SpeedCap / ElanConfig.Load().SpeedGainPerSecond + 1f;
        for (int second = 0; second < Mathf.CeilToInt(runSeconds); second++)
        {
            _player.Position = Vector2.Zero;
            await Step(60);
        }
        Check(_player.Elan.Tier == 1, $"élan du joueur : premier palier après {runSeconds:F1} s de course ({_player.Elan.Value:F3})");
        float elanSpeed = (await Measure(60)).Length();
        float expected = _player.Elan.SpeedAt(1);
        Check(Math.Abs(elanSpeed / baseSpeed - expected) < 0.01f,
            $"élan du joueur : la course va ×{elanSpeed / baseSpeed:F3} (attendu ×{expected:F2})");

        _player.TakeDamage(1f);
        Check(_player.Elan.Tier == 0, $"élan du joueur : un coup retire le palier ({_player.Elan.Value:F3})");
        await Step(30);
        _player.SetElanEnabled(false);
        float restored = (await Measure(60)).Length();
        Check(Math.Abs(restored - baseSpeed) < 0.5f, $"élan éteint : vitesse de base rendue ({restored:F1} / {baseSpeed:F1})");
        SetActions(Vector2.Zero);
    }

    private static void Advance(PlayerElan elan, float seconds, float speedRatio)
    {
        for (float elapsed = 0f; elapsed < seconds - 0.0001f; elapsed += ElanStep)
            elan.Step(ElanStep, speedRatio);
    }
}
