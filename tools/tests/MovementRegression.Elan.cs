using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// Élan (plan 52) : la Série seule, son effet réel sur le joueur (paliers, coup reçu, élimination), puis les gestes
/// (esquive parfaite, saut du vide).
/// </summary>
public partial class MovementRegression
{
    private const float ElanStep = 1f / 60f;

    private async Task RunElanChecks()
    {
        CheckElanConfig();
        CheckStreakModule();
        await CheckStreakPlayer();
        await CheckPerfectDodge();
        CheckLeapExtensionInPixels();
        await CheckVoidLeap();
    }

    private void CheckElanConfig()
    {
        ElanConfig config = ElanConfig.Load();
        Check(config.Tiers.Count == 3 && !config.EnabledInRuns && config.Tiers[0].From == 20 && config.StreakFallWeight > 1,
            $"série : trois paliers dès 20 éliminations, éteinte en run, une chute vaut {config.StreakFallWeight}");
        string valid = FileAccess.GetFileAsString("res://data/movement/elan.json");
        Check(ElanConfig.Parse(valid.Replace("\"from\": 60", "\"from\": 10"), out string order) == null && order.Contains("croissants"),
            $"série : seuils non croissants refusés ({order})");
        Check(ElanConfig.Parse(valid.Replace("\"window_seconds\"", "\"window_second\""), out string unknown) == null && unknown.Contains("inconnue"),
            $"série : clé inconnue refusée ({unknown})");
        Check(ElanConfig.Parse(valid.Replace("\"window_seconds\": 2.5", "\"window_seconds\": 0"), out string window) == null && window.Contains("window_seconds"),
            $"série : fenêtre nulle refusée ({window})");
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

    private void CheckStreakModule()
    {
        ElanConfig config = ElanConfig.Load();
        PlayerStreak streak = new(config);
        streak.Kill(5, "essai");
        Check(streak.Count == 0, "série éteinte : une élimination ne donne rien");

        streak.SetEnabled(true);
        streak.Kill(config.Tiers[0].From, "élimination");
        Check(streak.Tier == 1 && Mathf.IsEqualApprox(streak.WindowRemaining, config.StreakWindowSeconds),
            $"série : {config.Tiers[0].From} éliminations donnent le premier palier, fenêtre pleine ({streak.Count}, palier {streak.Tier})");
        Advance(streak, config.StreakWindowSeconds * 0.5f);
        streak.Refresh("geste");
        Check(streak.Count == config.Tiers[0].From && Mathf.IsEqualApprox(streak.WindowRemaining, config.StreakWindowSeconds),
            "série : un geste relance la fenêtre sans ajouter d'élimination");
        Advance(streak, config.StreakWindowSeconds + ElanStep);
        Check(streak.Count == 0 && streak.Tier == 0, $"série : fenêtre écoulée au premier palier, elle retombe à zéro ({streak.Count})");

        streak.Kill(config.Tiers[1].From + 5, "élimination");
        Check(streak.Tier == 2, $"série : {config.Tiers[1].From} éliminations donnent le deuxième palier ({streak.Count})");
        Advance(streak, config.StreakWindowSeconds + ElanStep);
        Check(streak.Count == config.Tiers[0].From && streak.Tier == 1 && streak.WindowRemaining > 0f,
            $"série : fenêtre écoulée au deuxième palier, retour au début du premier et nouvelle fenêtre ({streak.Count})");
        streak.Kill(config.Tiers[2].From, "élimination");
        streak.Hit();
        Check(streak.Count == config.Tiers[1].From && streak.Tier == 2,
            $"série : un coup reçu au troisième palier ramène au début du deuxième ({streak.Count})");
        Check(streak.Best >= config.Tiers[2].From, $"série : la meilleure est gardée ({streak.Best})");
        streak.SetEnabled(false);
        Check(streak.Count == 0 && streak.Tier == 0, "série éteinte : remise à zéro");
    }

    /// <summary>Le palier gagné par les éliminations accélère vraiment le joueur ; la course seule ne donne rien ; un coup le retire.</summary>
    private async Task CheckStreakPlayer()
    {
        ElanConfig config = ElanConfig.Load();
        await ReadyMobility();
        SetActions(Vector2.Right);
        await Step(10);
        float baseSpeed = (await Measure(60)).Length();
        _player.SetElanEnabled(true);
        for (int second = 0; second < 4; second++)
        {
            _player.Position = Vector2.Zero;
            await Step(60);
        }
        Check(_player.Streak.Count == 0, "série du joueur : la course seule ne la nourrit pas");
        _player.Streak.Kill(config.Tiers[0].From, "essai");
        await Step(1);
        float streakSpeed = (await Measure(60)).Length();
        float expected = _player.Streak.SpeedAt(1);
        Check(_player.Streak.Tier == 1 && Math.Abs(streakSpeed / baseSpeed - expected) < 0.01f,
            $"série du joueur : au premier palier, la course va ×{streakSpeed / baseSpeed:F3} (attendu ×{expected:F2})");

        _player.TakeDamage(1f);
        Check(_player.Streak.Tier == 0, $"série du joueur : un coup retire le palier ({_player.Streak.Count})");
        await Step(30);

        float baseDamage = _player.DamageMultiplier / _player.Streak.DamageAt(_player.Streak.Tier);
        float baseLuck = _player.LuckBonus - _player.Streak.LuckAt(_player.Streak.Tier);
        _player.Streak.Kill(config.Tiers[2].From, "essai");
        await Step(1);
        Check(_player.Streak.Tier == 3 && Mathf.IsEqualApprox(_player.DamageMultiplier, baseDamage * _player.Streak.DamageAt(3))
            && Mathf.IsEqualApprox(_player.LuckBonus, baseLuck + _player.Streak.LuckAt(3)),
            $"série du joueur : au troisième palier, dégâts ×{_player.DamageMultiplier / baseDamage:F2} et Chance +{_player.LuckBonus - baseLuck:F2}");
        _player.SetElanEnabled(false);
        float restored = (await Measure(60)).Length();
        Check(Math.Abs(restored - baseSpeed) < 0.5f && Math.Abs(_player.DamageMultiplier - baseDamage) < 0.0001f
            && Math.Abs(_player.LuckBonus - baseLuck) < 0.0001f,
            $"élan éteint : vitesse ({restored:F1} / {baseSpeed:F1}), dégâts et Chance de base rendus");
        SetActions(Vector2.Zero);
    }

    /// <summary>Lot E2 : un coup évité pendant le dash relance la Série et rend de la recharge, une fois par dash.</summary>
    private async Task CheckPerfectDodge()
    {
        ElanConfig config = ElanConfig.Load();
        await ReadyMobility();
        _player.SetElanEnabled(true);
        _player.Streak.Kill(5, "essai");
        await Step(30);
        float windowBefore = _player.Streak.WindowRemaining;
        SetActions(Vector2.Right);
        PressMobility();
        await Step(1);
        ReleaseMobility();
        float cooldownBefore = _player.Mobility.CooldownRemaining;
        PlayerDamageResult dodged = _player.TakeDamage(5f);
        Check(!dodged.Applied && _player.PerfectDodges == 1 && windowBefore < config.StreakWindowSeconds - 0.4f
            && Mathf.IsEqualApprox(_player.Streak.WindowRemaining, config.StreakWindowSeconds),
            $"esquive parfaite : coup évité, fenêtre de la Série relancée ({windowBefore:F2} → {_player.Streak.WindowRemaining:F2} s)");
        float refunded = cooldownBefore - _player.Mobility.CooldownRemaining;
        Check(Math.Abs(refunded - _player.Mobility.CooldownSeconds * config.PerfectDodgeCooldownRefund) < 0.001f,
            $"esquive parfaite : {refunded:F2} s de recharge rendues");
        _player.TakeDamage(5f);
        Check(_player.PerfectDodges == 1, "esquive parfaite : une seule par dash, même sous plusieurs coups");
        await Step(12);
        SetActions(Vector2.Zero);
        Check(_player.TakeDamage(1f).Applied, "esquive parfaite : hors du dash, le coup porte");

        _player.SetElanEnabled(false);
        await ReadyMobility();
        PressMobility();
        await Step(1);
        ReleaseMobility();
        Check(!_player.TakeDamage(5f).Applied && _player.Streak.Count == 0 && _player.PerfectDodges == 1,
            "élan éteint : le dash protège toujours, sans esquive parfaite");
        await Step(12);
    }

    /// <summary>
    /// Lot E3 : une bande de Néant d'une cellule (x de 128 à 256) dans un gestionnaire synthétique. Au palier 0, le dash
    /// s'arrête au bord ; au palier II, il la franchit en l'air et relance la Série ; au palier I, parti trop loin, il
    /// retombe dans le vide sans rien relancer.
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
        _player.Streak.Kill(ElanConfig.Load().Tiers[0].From, "essai");
        _player.Mobility.BlinkDistance = 0.8f;
        (leaped, _) = await DashFrom(new Vector2(edge - 25f, 20f));
        _player.Mobility.BlinkDistance = 0f;
        Check(_player.Position.X < edge && !leaped && _player.VoidLeaps == 0,
            $"saut du vide : le saut instantané (Plan de métro) n'est pas porté, il s'arrête au bord (x={_player.Position.X:0})");

        await ReadyMobility();
        managerField.SetValue(_player, erasure);
        _player.Streak.Kill(ElanConfig.Load().Tiers[1].From, "essai");
        await Step(40);
        float windowBefore = _player.Streak.WindowRemaining;
        (leaped, raised) = await DashFrom(new Vector2(edge - 10f, 20f));
        Check(_player.Position.X >= 2f * edge && leaped && raised,
            $"saut du vide : au palier II, le dash franchit une cellule de vide en l'air (x={_player.Position.X:0})");
        Check(_player.VoidLeaps == 1 && _player.Streak.WindowRemaining > windowBefore,
            $"saut du vide : la retombée sur le sol relance la Série ({windowBefore:F2} → {_player.Streak.WindowRemaining:F2} s)");

        await ReadyMobility();
        managerField.SetValue(_player, erasure);
        _player.Streak.Kill(ElanConfig.Load().Tiers[1].From, "essai");
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
        _player.Streak.Kill(ElanConfig.Load().Tiers[0].From, "essai");
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

    private static void Advance(PlayerStreak streak, float seconds)
    {
        for (float elapsed = 0f; elapsed < seconds - 0.0001f; elapsed += ElanStep)
            streak.Step(ElanStep);
    }
}
