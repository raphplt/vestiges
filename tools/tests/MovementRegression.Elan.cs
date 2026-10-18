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
/// Élan (plan 52) : la Série seule, son effet réel sur le joueur (rangs, coup reçu, encaissement), puis les gestes
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
        Check(config.Ranks.Count == 6 && config.Ranks[0].From == 10 && config.Ranks[5].From == 320 && config.StreakFallWeight > 1,
            $"série : six rangs de ×10 à ×320, une chute vaut {config.StreakFallWeight}");
        bool shorter = true, richer = true;
        for (int index = 1; index < config.Ranks.Count; index++)
        {
            shorter &= config.Ranks[index].WindowSeconds < config.Ranks[index - 1].WindowSeconds;
            richer &= config.Ranks[index].CashOut > config.Ranks[index - 1].CashOut;
        }
        Check(shorter && config.Ranks[0].WindowSeconds < config.StreakWindowSeconds && richer,
            "série : la fenêtre raccourcit et le taux d'encaissement monte à chaque rang");
        string valid = FileAccess.GetFileAsString("res://data/movement/elan.json");
        Check(ElanConfig.Parse(valid.Replace("\"from\": 40", "\"from\": 15"), out string order) == null && order.Contains("croissants"),
            $"série : seuils non croissants refusés ({order})");
        Check(ElanConfig.Parse(valid.Replace("\"cashout\"", "\"cash_out\""), out string unknown) == null && unknown.Contains("inconnue"),
            $"série : clé inconnue refusée ({unknown})");
        Check(ElanConfig.Parse(valid.Replace("\"window_seconds\": 4.0", "\"window_seconds\": 0"), out string window) == null && window.Contains("window_seconds"),
            $"série : fenêtre nulle refusée ({window})");
        Check(ElanConfig.Parse(valid.Replace("\"color\": [0.62, 0.56, 0.46]", "\"color\": [0.62, 0.56]"), out string color) == null && color.Contains("color"),
            $"série : couleur incomplète refusée ({color})");
        Check(config.VoidLeapExtensionAt(0) == 0f && config.VoidLeapExtensionAt(6) > config.VoidLeapExtensionAt(1),
            $"saut du vide : pas d'allonge au rang 0, plus longue à MONSTRE ({config.VoidLeapExtensionAt(6)} px)");
        Check(ElanConfig.Parse(valid.Replace("[0, 64, 96, 128, 160, 192, 224]", "[0, 64, 128]"), out string count) == null && count.Contains("rang 0 compris"),
            $"saut du vide : une allonge par rang exigée ({count})");
        Check(ElanConfig.Parse(valid.Replace("[0, 64, 96, 128, 160, 192, 224]", "[0, 96, 64, 128, 160, 192, 224]"), out string order2) == null
            && order2.Contains("croissantes"), $"saut du vide : allonges décroissantes refusées ({order2})");
        Check(config.PercussionMinRank == 0 && config.PercussionDistanceAt(6) > config.PercussionDistanceAt(0),
            $"percussion : dès le rang 0 par défaut, plus forte à MONSTRE ({config.PercussionDistanceAt(6)} px)");
        Check(ElanConfig.Parse(valid.Replace("\"min_rank\": 0", "\"min_rank\": 7"), out string rank) == null && rank.Contains("min_rank"),
            $"percussion : rang minimum hors des rangs refusé ({rank})");
        Check(ElanConfig.Parse(valid.Replace("[70, 85, 100, 115, 130, 145, 160]", "[70, 100]"), out string distances) == null
            && distances.Contains("rang 0 compris"), $"percussion : une distance par rang exigée ({distances})");
    }

    /// <summary>Les trois règles de la Série (plan 52 §15) : monter, être coupée en deux, s'encaisser.</summary>
    private void CheckStreakModule()
    {
        ElanConfig config = ElanConfig.Load();
        PlayerStreak streak = new(config);
        streak.Kill(5, "essai");
        Check(streak.Count == 0, "série éteinte : une élimination ne donne rien");

        streak.SetEnabled(true);
        streak.Kill(config.Ranks[0].From - 1, "élimination");
        Check(streak.Rank == 0 && Mathf.IsEqualApprox(streak.WindowRemaining, config.StreakWindowSeconds),
            $"série : sous ÉVEIL, rang 0 et fenêtre de {config.StreakWindowSeconds} s");
        Advance(streak, config.StreakWindowSeconds * 0.5f);
        streak.Refresh("geste");
        Check(Mathf.IsEqualApprox(streak.WindowRemaining, config.StreakWindowSeconds), "série : un geste relance la fenêtre sans rien ajouter");
        int cashed = AdvanceUntilCashOut(streak, config.StreakWindowSeconds + ElanStep);
        Check(streak.Count == 0 && cashed == 0, $"série : fenêtre écoulée sous ÉVEIL, elle s'arrête sans rien encaisser ({cashed})");

        streak.Kill(config.Ranks[0].From, "élimination");
        Check(streak.Rank == 1 && Mathf.IsEqualApprox(streak.WindowRemaining, config.Ranks[0].WindowSeconds),
            $"série : ×{config.Ranks[0].From} donne ÉVEIL, fenêtre plus courte ({streak.WindowRemaining:F1} s)");
        streak.Kill(100 - streak.Count, "élimination");
        Check(streak.Rank == 4, $"série : ×100 est au rang CARNAGE ({streak.Rank})");
        streak.Hit();
        Check(streak.Count == 50 && streak.Rank == 3, $"série : un coup la coupe en deux, un rang de moins (×{streak.Count}, rang {streak.Rank})");
        streak.Hit();
        streak.Hit();
        streak.Hit();
        streak.Hit();
        streak.Hit();
        Check(streak.Count == 1, $"série : coupée encore et encore, il reste ×{streak.Count}");
        streak.Hit();
        Check(streak.Count == 0 && streak.WindowRemaining == 0f, "série : ×1 coupée en deux tombe à zéro, sans encaissement");

        streak.Kill(100, "élimination");
        int expected = streak.CashOutFor(100, 4);
        cashed = AdvanceUntilCashOut(streak, config.Ranks[3].WindowSeconds + ElanStep);
        Check(streak.Count == 0 && cashed == expected && expected == Mathf.FloorToInt(100 * config.Ranks[3].CashOut),
            $"série : ×100 à CARNAGE s'encaisse en {cashed} Essence");
        Check(streak.CashOutFor(100, 4) > 10 * streak.CashOutFor(10, 1),
            $"série : une Série de ×100 rapporte plus que dix de ×10 ({streak.CashOutFor(100, 4)} contre {10 * streak.CashOutFor(10, 1)})");
        Check(streak.Best == 100 && streak.BestRank == 4 && streak.EssenceCashed == expected,
            $"série : la meilleure (×{streak.Best}, rang {streak.BestRank}) et l'Essence encaissée sont gardées");
        streak.SetEnabled(false);
        Check(streak.Count == 0 && streak.Rank == 0 && streak.Best == 0, "série éteinte : remise à zéro");
    }

    /// <summary>Le rang gagné par les éliminations accélère vraiment le joueur ; la course seule ne donne rien ; un coup le retire.</summary>
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
        _player.Streak.Kill(config.Ranks[0].From, "essai");
        await Step(1);
        float streakSpeed = (await Measure(60)).Length();
        float expected = _player.Streak.SpeedAt(1);
        Check(_player.Streak.Rank == 1 && Math.Abs(streakSpeed / baseSpeed - expected) < 0.01f,
            $"série du joueur : à ÉVEIL, la course va ×{streakSpeed / baseSpeed:F3} (attendu ×{expected:F2})");

        _player.TakeDamage(1f);
        Check(_player.Streak.Rank == 0, $"série du joueur : un coup la coupe en deux et retire le rang ({_player.Streak.Count})");
        await Step(30);

        float baseDamage = _player.DamageMultiplier / _player.Streak.DamageAt(_player.Streak.Rank);
        float baseLuck = _player.LuckBonus - _player.Streak.LuckAt(_player.Streak.Rank);
        _player.Streak.Kill(config.Ranks[2].From, "essai");
        await Step(1);
        Check(_player.Streak.Rank == 3 && Mathf.IsEqualApprox(_player.DamageMultiplier, baseDamage * _player.Streak.DamageAt(3))
            && Mathf.IsEqualApprox(_player.LuckBonus, baseLuck + _player.Streak.LuckAt(3)),
            $"série du joueur : à FURIE, dégâts ×{_player.DamageMultiplier / baseDamage:F2} et Chance +{_player.LuckBonus - baseLuck:F2}");
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
    /// Lot E3 : une bande de Néant d'une cellule (x de 128 à 256) dans un gestionnaire synthétique. Au rang 0, le dash
    /// s'arrête au bord ; à FURIE, il la franchit en l'air et relance la Série ; à ÉVEIL, parti trop loin, il
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
            $"saut du vide : au rang 0, le dash s'arrête au bord (x={_player.Position.X:0})");

        await ReadyMobility();
        managerField.SetValue(_player, erasure);
        _player.Streak.Kill(ElanConfig.Load().Ranks[0].From, "essai");
        _player.Mobility.BlinkDistance = 0.8f;
        (leaped, _) = await DashFrom(new Vector2(edge - 25f, 20f));
        _player.Mobility.BlinkDistance = 0f;
        Check(_player.Position.X < edge && !leaped && _player.VoidLeaps == 0,
            $"saut du vide : le saut instantané (Plan de métro) n'est pas porté, il s'arrête au bord (x={_player.Position.X:0})");

        await ReadyMobility();
        managerField.SetValue(_player, erasure);
        _player.Streak.Kill(ElanConfig.Load().Ranks[2].From, "essai");
        await Step(40);
        float windowBefore = _player.Streak.WindowRemaining;
        (leaped, raised) = await DashFrom(new Vector2(edge - 10f, 20f));
        Check(_player.Position.X >= 2f * edge && leaped && raised,
            $"saut du vide : à FURIE, le dash franchit une cellule de vide en l'air (x={_player.Position.X:0})");
        Check(_player.VoidLeaps == 1 && _player.Streak.WindowRemaining > windowBefore,
            $"saut du vide : la retombée sur le sol relance la Série ({windowBefore:F2} → {_player.Streak.WindowRemaining:F2} s)");

        await ReadyMobility();
        managerField.SetValue(_player, erasure);
        _player.Streak.Kill(ElanConfig.Load().Ranks[2].From, "essai");
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
        _player.Streak.Kill(ElanConfig.Load().Ranks[0].From, "essai");
        (leaped, _) = await DashFrom(new Vector2(edge - 40f, 20f));
        Check(_player.Position.X > edge && _player.Position.X < 2f * edge && leaped && _player.VoidLeaps == 2,
            $"saut du vide : à ÉVEIL, parti trop loin, il retombe dans le vide sans gain (x={_player.Position.X:0})");

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

    /// <summary>Fait couler la fenêtre ; rend l'Essence encaissée si la Série s'arrête, -1 sinon.</summary>
    private static int AdvanceUntilCashOut(PlayerStreak streak, float seconds)
    {
        for (float elapsed = 0f; elapsed < seconds - 0.0001f; elapsed += ElanStep)
        {
            int cashed = streak.Step(ElanStep);
            if (cashed >= 0)
                return cashed;
        }
        return -1;
    }
}
