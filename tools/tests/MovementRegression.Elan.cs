using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>Élan (plan 52) : règles du module seul, effet réel sur la course du joueur, puis les gestes (lots E2 et suivants).</summary>
public partial class MovementRegression
{
    private const float ElanStep = 1f / 60f;

    private async Task RunElanChecks()
    {
        CheckElanConfig();
        CheckElanModule();
        await CheckElanPlayer();
        await CheckPerfectDodge();
        CheckLeapExtensionInPixels();
        await CheckVoidLeap();
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
        Check(config.VoidLeapExtensionAt(0) == 0f && config.VoidLeapExtensionAt(3) > config.VoidLeapExtensionAt(1),
            $"saut du vide : pas d'allonge au palier 0, plus longue au palier III ({config.VoidLeapExtensionAt(3)} px)");
        Check(ElanConfig.Parse(valid.Replace("[0, 64, 128, 192]", "[0, 64, 128]"), out string count) == null && count.Contains("palier 0 compris"),
            $"saut du vide : une allonge par palier exigée ({count})");
        Check(ElanConfig.Parse(valid.Replace("[0, 64, 128, 192]", "[0, 128, 64, 192]"), out string order2) == null && order2.Contains("croissantes"),
            $"saut du vide : allonges décroissantes refusées ({order2})");
        Check(config.PercussionMinTier == 0 && config.PercussionDistanceAt(3) > config.PercussionDistanceAt(0),
            $"percussion : dès le palier 0 par défaut, plus forte au palier III ({config.PercussionDistanceAt(3)} px)");
        Check(ElanConfig.Parse(valid.Replace("\"min_tier\": 0", "\"min_tier\": 4"), out string tier) == null && tier.Contains("min_tier"),
            $"percussion : palier minimum hors des paliers refusé ({tier})");
        Check(ElanConfig.Parse(valid.Replace("[70, 100, 130, 160]", "[70, 100]"), out string distances) == null && distances.Contains("palier 0 compris"),
            $"percussion : une distance par palier exigée ({distances})");
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

    /// <summary>Lot E2 : un coup évité pendant le dash donne de l'Élan et rend de la recharge, une fois par dash.</summary>
    private async Task CheckPerfectDodge()
    {
        ElanConfig config = ElanConfig.Load();
        await ReadyMobility();
        _player.SetElanEnabled(true);
        SetActions(Vector2.Right);
        PressMobility();
        await Step(1);
        ReleaseMobility();
        float cooldownBefore = _player.Mobility.CooldownRemaining;
        PlayerDamageResult dodged = _player.TakeDamage(5f);
        // La course de l'image du dash ajoute une miette d'Élan : tolérance de 0,01.
        float afterDodge = _player.Elan.Value;
        Check(!dodged.Applied && Math.Abs(afterDodge - config.PerfectDodgeGain) < 0.01f && _player.PerfectDodges == 1,
            $"esquive parfaite : coup évité, Élan {_player.Elan.Value:F2} (attendu {config.PerfectDodgeGain:F2})");
        float refunded = cooldownBefore - _player.Mobility.CooldownRemaining;
        Check(Math.Abs(refunded - _player.Mobility.CooldownSeconds * config.PerfectDodgeCooldownRefund) < 0.001f,
            $"esquive parfaite : {refunded:F2} s de recharge rendues");
        _player.TakeDamage(5f);
        Check(Math.Abs(_player.Elan.Value - afterDodge) < 0.01f && _player.PerfectDodges == 1,
            "esquive parfaite : une seule par dash, même sous plusieurs coups");
        await Step(12);
        SetActions(Vector2.Zero);
        Check(_player.TakeDamage(1f).Applied, "esquive parfaite : hors du dash, le coup porte");

        _player.SetElanEnabled(false);
        await ReadyMobility();
        PressMobility();
        await Step(1);
        ReleaseMobility();
        Check(!_player.TakeDamage(5f).Applied && _player.Elan.Value == 0f && _player.PerfectDodges == 1,
            "élan éteint : le dash protège toujours, sans esquive parfaite");
        await Step(12);
    }

    /// <summary>
    /// Lot E3 : une bande de Néant d'une cellule (x de 128 à 256) dans un gestionnaire synthétique. Au palier 0, le dash
    /// s'arrête au bord ; au palier II, il la franchit en l'air et nourrit l'Élan ; au palier I, parti trop loin, il
    /// retombe dans le vide sans rien gagner.
    /// </summary>
    private async Task CheckVoidLeap()
    {
        ElanConfig config = ElanConfig.Load();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        ErasureManager erasure = new();
        AddChild(erasure);
        erasure.SetProcess(false);
        Dictionary<Vector2I, ErasureManager.ErasureZonePhase> phases =
            (Dictionary<Vector2I, ErasureManager.ErasureZonePhase>)typeof(ErasureManager).GetField("_zonePhases", flags).GetValue(erasure);
        phases[new Vector2I(1, 0)] = ErasureManager.ErasureZonePhase.Void;
        FieldInfo managerField = typeof(Player).GetField("_erasureManager", flags);
        float edge = erasure.CellSize;

        await ReadyMobility();
        managerField.SetValue(_player, erasure);
        (bool leaped, bool raised) = await DashFrom(new Vector2(edge - 25f, 20f));
        Check(_player.Position.X < edge && !leaped && _player.VoidLeaps == 0,
            $"saut du vide : Élan éteint (run normale), le dash s'arrête au bord (x={_player.Position.X:0})");

        await ReadyMobility();
        managerField.SetValue(_player, erasure);
        _player.SetElanEnabled(true);
        (leaped, raised) = await DashFrom(new Vector2(edge - 25f, 20f));
        Check(_player.Position.X < edge && !leaped && _player.VoidLeaps == 0,
            $"saut du vide : au palier 0, le dash s'arrête au bord (x={_player.Position.X:0})");

        await ReadyMobility();
        managerField.SetValue(_player, erasure);
        _player.Elan.Gain(0.4f, "essai");
        _player.Mobility.BlinkDistance = 0.8f;
        (leaped, _) = await DashFrom(new Vector2(edge - 25f, 20f));
        _player.Mobility.BlinkDistance = 0f;
        Check(_player.Position.X < edge && !leaped && _player.VoidLeaps == 0,
            $"saut du vide : le saut instantané (Plan de métro) n'est pas porté, il s'arrête au bord (x={_player.Position.X:0})");

        await ReadyMobility();
        managerField.SetValue(_player, erasure);
        _player.Elan.Gain(0.7f, "essai");
        float before = _player.Elan.Value;
        (leaped, raised) = await DashFrom(new Vector2(edge - 10f, 20f));
        Check(_player.Position.X >= 2f * edge && leaped && raised,
            $"saut du vide : au palier II, le dash franchit une cellule de vide en l'air (x={_player.Position.X:0})");
        Check(_player.VoidLeaps == 1 && _player.Elan.Value > before + config.VoidLeapGain - 0.03f,
            $"saut du vide : la retombée sur le sol donne de l'Élan ({before:F2} → {_player.Elan.Value:F2})");

        await ReadyMobility();
        managerField.SetValue(_player, erasure);
        _player.Elan.Gain(0.7f, "essai");
        _player.Position = new Vector2(edge - 10f, 20f);
        await Step(1);
        SetActions(Vector2.Right);
        PressMobility();
        bool paused = false, keptInAir = false;
        for (int tick = 0; tick < 60; tick++)
        {
            await Step(1);
            if (tick == 0)
                ReleaseMobility();
            if (!paused && _player.IsLeaping && erasure.GetZonePhaseAt(_player.GlobalPosition) == ErasureManager.ErasureZonePhase.Void)
            {
                paused = true;
                GetTree().Paused = true;
                keptInAir = _player.Mobility.IsDashing && _player.IsLeaping;
                GetTree().Paused = false;
            }
            if (tick > 0 && !_player.Mobility.IsDashing)
                break;
        }
        SetActions(Vector2.Zero);
        Check(paused && keptInAir && _player.Position.X >= 2f * edge && _player.VoidLeaps == 2,
            $"saut du vide : une pause en plein saut le garde en l'air, il reprend et retombe sur le sol (x={_player.Position.X:0})");

        await ReadyMobility();
        managerField.SetValue(_player, erasure);
        _player.SetElanEnabled(false);
        _player.SetElanEnabled(true);
        _player.Elan.Gain(0.4f, "essai");
        (leaped, _) = await DashFrom(new Vector2(edge - 40f, 20f));
        Check(_player.Position.X > edge && _player.Position.X < 2f * edge && leaped && _player.VoidLeaps == 2,
            $"saut du vide : au palier I, parti trop loin, il retombe dans le vide sans gain (x={_player.Position.X:0})");

        SetActions(Vector2.Zero);
        _player.SetElanEnabled(false);
        managerField.SetValue(_player, null);
        erasure.QueueFree();
        _player.Position = Vector2.Zero;
        await Step(2);
    }

    /// <summary>
    /// L'allonge du saut se compte en pixels : si la vitesse baisse de moitié en l'air (eau, ralentissement), le saut
    /// garde sa longueur au lieu de doubler.
    /// </summary>
    private void CheckLeapExtensionInPixels()
    {
        PlayerMobility mobility = new(MobilityConfig.Load()) { LeapExtensionPixels = 64f };
        const float speed = 200f;
        mobility.Request();
        float baseTravel = 0f;
        for (int tick = 0; tick < 60 && (tick == 0 || mobility.IsDashing); tick++)
        {
            mobility.OverVoid = false;
            float step = mobility.Step(ElanStep, Vector2.Right, speed, 1f, true).X * ElanStep;
            baseTravel += mobility.IsDashStep ? step : 0f;
        }
        mobility = new PlayerMobility(MobilityConfig.Load()) { LeapExtensionPixels = 64f };
        mobility.Request();
        float leapTravel = 0f;
        for (int tick = 0; tick < 120 && (tick == 0 || mobility.IsDashing); tick++)
        {
            mobility.OverVoid = true;
            // Après la durée de base, la vitesse tombe de moitié, comme en entrant dans l'eau.
            float tickSpeed = leapTravel >= baseTravel - 0.01f ? speed * 0.5f : speed;
            float step = mobility.Step(ElanStep, Vector2.Right, tickSpeed, 1f, true).X * ElanStep;
            leapTravel += mobility.IsDashStep ? step : 0f;
        }
        Check(Math.Abs(leapTravel - baseTravel - 64f) < 1.5f,
            $"saut du vide : l'allonge reste de 64 px même ralenti en l'air ({leapTravel - baseTravel:F1} px)");
    }

    /// <summary>Dash vers la droite depuis <paramref name="start"/> jusqu'à son arrêt ; dit s'il a été en l'air et vu en hauteur.</summary>
    private async Task<(bool Leaped, bool Raised)> DashFrom(Vector2 start)
    {
        _player.Position = start;
        await Step(1);
        float restY = _sprite.Position.Y;
        bool leaped = false, raised = false;
        int dashes = _player.Mobility.DashesStarted;
        SetActions(Vector2.Right);
        PressMobility();
        for (int tick = 0; tick < 60; tick++)
        {
            await Step(1);
            if (tick == 0)
                ReleaseMobility();
            leaped |= _player.IsLeaping;
            raised |= _sprite.Position.Y < restY - 1f;
            // Un saut instantané finit dans l'image où il part : on s'arrête là, sans pas de marche en plus.
            if (_player.Mobility.DashesStarted > dashes && !_player.Mobility.IsDashing)
                break;
        }
        SetActions(Vector2.Zero);
        await Step(1);
        return (leaped, raised);
    }

    private static void Advance(PlayerElan elan, float seconds, float speedRatio)
    {
        for (float elapsed = 0f; elapsed < seconds - 0.0001f; elapsed += ElanStep)
            elan.Step(ElanStep, speedRatio);
    }
}
