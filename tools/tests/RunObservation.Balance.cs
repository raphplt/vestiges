using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Godot;
using Vestiges.Core;
using Vestiges.Progression;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// Relevés d'équilibrage pendant --density (DECISIONS §50) : à chaque écran de niveau, la Chance du joueur, les crans
/// de montée de rareté (Chance, oubli de la zone, Péril) et la rareté des améliorations offertes, par tranche de 5 min.
/// </summary>
public partial class RunObservation
{
    private const double BalanceBandSeconds = 300.0;

    private readonly Dictionary<int, Dictionary<string, int>> _offeredRarities = new();
    private readonly Dictionary<int, (double HpLost, double Healed, int Hits, int Fatal, int Shielded)> _survival = new();
    private readonly Dictionary<int, Dictionary<string, int>> _loot = new();
    private readonly Dictionary<int, (float MaxHp, float ArmorReduction)> _defenseAtBand = new();
    private bool _mortal;
    private StringBuilder _damageProbe;
    private readonly Dictionary<(int Minute, HealingKind Kind), double> _healingProbe = new();
    private string _damageProbeSource;
    private float _damageProbeAnnounced;
    private double _damageProbeTime;

    /// <summary>
    /// --mortal : le bot n'est plus invincible ; armure, bouclier et invulnérabilité jouent, un coup fatal remet les PV
    /// au maximum et se compte (DECISIONS §54). Le butin reçu (stats et niveaux d'objets des coffres) est compté aussi.
    /// </summary>
    private void StartBalanceProbes(string[] args)
    {
        EventBus bus = GetNode<EventBus>("/root/EventBus");
        bus.LootReceived += (type, _, amount) =>
        {
            int band = (int)(_balanceTime / BalanceBandSeconds);
            if (!_loot.TryGetValue(band, out Dictionary<string, int> counts))
                _loot[band] = counts = new Dictionary<string, int>();
            counts[type] = counts.GetValueOrDefault(type) + amount;
        };
        _mortal = System.Array.IndexOf(args, "--mortal") >= 0;
        if (!_mortal)
            return;
        _player.IsGodMode = false;
        _player.SurviveFatalHitsForTests = true;
        if (System.Array.IndexOf(args, "--damage-probe") >= 0)
        {
            _damageProbe = new StringBuilder("t,kind,source,announced_damage,hp_before,max_hp,armor_reduction,hp_lost,fatal,shielded\n");
            bus.PlayerHitBy += (source, damage) =>
            {
                _damageProbeSource = source;
                _damageProbeAnnounced = damage;
                _damageProbeTime = _balanceTime;
            };
        }
        bus.PlayerDamageResolved += result =>
        {
            if (result.PlayerId != _player.GetInstanceId())
                return;
            int band = (int)(_balanceTime / BalanceBandSeconds);
            (double lost, double healed, int hits, int fatal, int shielded) = _survival.GetValueOrDefault(band);
            _survival[band] = (lost + result.HpLost, healed, hits + (result.HpLost > 0f ? 1 : 0), fatal + (result.Fatal ? 1 : 0),
                shielded + (result.ShieldAbsorbed ? 1 : 0));
            _defenseAtBand[band] = (_player.EffectiveMaxHp, _player.ArmorReduction);
            if (_damageProbe != null)
            {
                // L'annonce précède TakeDamage ; les coups rejetés ne publient aucun résultat. Le Néant est à part.
                bool known = result.Kind == PlayerDamageKind.Combat && _damageProbeSource != null && _damageProbeTime == _balanceTime;
                string source = result.Kind == PlayerDamageKind.Erasure ? "erasure" : known ? _damageProbeSource : "unknown";
                _damageProbe.Append(CultureInfo.InvariantCulture,
                    $"{_balanceTime:F3},{result.Kind},{source},{(known ? _damageProbeAnnounced : 0f):F4},{result.HpBefore:F4},{_player.EffectiveMaxHp:F4},{_player.ArmorReduction:F4},{result.HpLost:F4},{(result.Fatal ? 1 : 0)},{(result.ShieldAbsorbed ? 1 : 0)}\n");
                _damageProbeSource = null;
            }
        };
        bus.PlayerHealingResolved += result =>
        {
            if (result.PlayerId != _player.GetInstanceId())
                return;
            int band = (int)(_balanceTime / BalanceBandSeconds);
            (double lost, double healed, int hits, int fatal, int shielded) = _survival.GetValueOrDefault(band);
            _survival[band] = (lost, healed + result.HpRestored, hits, fatal, shielded);
            if (_damageProbe != null && result.HpRestored > 0f)
            {
                (int, HealingKind) key = ((int)(_balanceTime / 60.0), result.Kind);
                _healingProbe[key] = _healingProbe.GetValueOrDefault(key) + result.HpRestored;
            }
        };
    }
    private readonly Dictionary<int, (double Luck, double Steps, int Count)> _offerContext = new();
    private double _balanceTime;

    private void RecordOffer(FragmentManager fragments)
    {
        int band = (int)(_balanceTime / BalanceBandSeconds);
        if (!_offeredRarities.TryGetValue(band, out Dictionary<string, int> rarities))
            _offeredRarities[band] = rarities = new Dictionary<string, int>();
        foreach (FragmentOption option in fragments.PendingChoices)
            if (option.Rarity != null)
                rarities[option.Rarity.Id] = rarities.GetValueOrDefault(option.Rarity.Id) + 1;

        ErasureManager erasure = _world.GetNodeOrNull<ErasureManager>("ErasureManager");
        ErasureManager.ErasureZonePhase phase = erasure?.GetZonePhaseAt(_player.GlobalPosition) ?? ErasureManager.ErasureZonePhase.Anchored;
        float steps = UpgradeRoller.BumpSteps(_player.LuckBonus, phase);
        (double luck, double total, int count) = _offerContext.GetValueOrDefault(band);
        _offerContext[band] = (luck + _player.LuckBonus, total + steps, count + 1);
    }

    private void AppendBalance(StringBuilder summary)
    {
        if (_damageProbe != null)
        {
            using FileAccess damage = FileAccess.Open($"{_output}/damage.csv", FileAccess.ModeFlags.Write);
            damage.StoreString(_damageProbe.ToString());
            StringBuilder rows = new("minute,kind,hp_restored\n");
            foreach (KeyValuePair<(int Minute, HealingKind Kind), double> entry in _healingProbe)
                rows.Append(CultureInfo.InvariantCulture, $"{entry.Key.Minute},{entry.Key.Kind},{entry.Value:F4}\n");
            using FileAccess healing = FileAccess.Open($"{_output}/healing.csv", FileAccess.ModeFlags.Write);
            healing.StoreString(rows.ToString());
        }
        for (int band = 0; band <= (int)(_balanceTime / BalanceBandSeconds); band++)
        {
            Dictionary<string, int> loot = _loot.GetValueOrDefault(band);
            summary.Append(CultureInfo.InvariantCulture,
                $" | loot_{band * 5}-{band * 5 + 5}min stat={loot?.GetValueOrDefault("stat") ?? 0} object_level={loot?.GetValueOrDefault("object_level") ?? 0}");
            if (!_mortal)
                continue;
            (double lost, double healed, int hits, int fatal, int shielded) = _survival.GetValueOrDefault(band);
            // Tranche sans coup reçu : la défense du moment plutôt qu'un zéro trompeur.
            (float maxHp, float armorReduction) = _defenseAtBand.TryGetValue(band, out (float, float) defense)
                ? defense
                : (_player.EffectiveMaxHp, _player.ArmorReduction);
            // Coup moyen après armure, et nombre de ces coups pour vider les PV max : la menace d'un coup, quelle que
            // soit l'habileté du bot à les éviter.
            double meanHit = hits > 0 ? lost / hits : 0.0;
            summary.Append(CultureInfo.InvariantCulture,
                $" hp_lost_per_min={lost / 5.0:F0} healed_per_min={healed / 5.0:F0} hits={hits} shielded={shielded} deaths={fatal} max_hp={maxHp:F0} armor_reduction={armorReduction:F2} mean_hit={meanHit:F1} hits_to_die={(meanHit > 0 ? maxHp / meanHit : 0):F1}");
        }
        List<int> bands = new(_offeredRarities.Keys);
        bands.Sort();
        foreach (int band in bands)
        {
            Dictionary<string, int> rarities = _offeredRarities[band];
            int total = 0;
            foreach (int count in rarities.Values)
                total += count;
            (double luck, double steps, int offers) = _offerContext.GetValueOrDefault(band);
            summary.Append(CultureInfo.InvariantCulture,
                $" | offers_{band * 5}-{band * 5 + 5}min n={total} luck={(offers > 0 ? luck / offers : 0):F2} steps={(offers > 0 ? steps / offers : 0):F2}");
            foreach (UpgradeRarity rarity in UpgradeRoller.Rarities)
                summary.Append(CultureInfo.InvariantCulture,
                    $" {rarity.Id}={(total > 0 ? 100.0 * rarities.GetValueOrDefault(rarity.Id) / total : 0):F0}%");
        }
    }
}
