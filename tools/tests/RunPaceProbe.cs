using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// Rythme d'une run mesurée (plan 42 M0a), toujours relevé avec --density. Chaque seconde : chemin parcouru,
/// éloignement du départ, phase d'oubli sous le joueur, PV, morts comptées et temps depuis la dernière décision.
/// En continu : dégâts infligés par arme et par nature, temps pour tuer chaque créature (du premier coup du joueur à
/// sa mort) et journal des décisions. La synthèse par minute est faite par <c>tools/summarize_pace.py</c>.
/// </summary>
internal sealed class RunPaceProbe : IDisposable
{
    // Portée d'un élite ou d'un Souverain qui compte comme une décision : celle des créatures « proches » du relevé.
    private const float EliteRangePx = 600f;

    private readonly string _output;
    private readonly ulong _seed;
    private readonly SceneTree _tree;
    private readonly Player _player;
    private readonly ErasureManager _erasure;
    private readonly EventBus _bus;
    private readonly float _pixelsPerMeter;
    private readonly Vector2 _start;
    private Vector2 _last;
    private double _time;
    private double _nextSample = 1.0;
    private double _lastDecision;
    private double _longestGap;
    private bool _eliteNear;
    private int _deaths;

    public double TravelledMeters { get; private set; }
    public double MaxDistanceMeters { get; private set; }

    private readonly StringBuilder _rows = new("t,travelled_m,from_start_m,phase,hp,max_hp,deaths,since_decision_s,elite_near\n");
    private readonly StringBuilder _kills = new("t,enemy,elite,ttk_s\n");
    private readonly StringBuilder _decisions = new("t,kind\n");
    private readonly Dictionary<(int Minute, string Weapon, DamageKind Kind), double> _damage = new();
    private readonly Dictionary<EnemyLife, double> _firstHit = new();
    // Même compte que les parties de Raphaël, pour comparer un profil à un vrai joueur (plan 42 M0e).
    private readonly PlayTrace _trace;

    public RunPaceProbe(string output, ulong seed, SceneTree tree, Player player, ErasureManager erasure, EventBus bus)
    {
        _output = output;
        _seed = seed;
        _tree = tree;
        _player = player;
        _erasure = erasure;
        _bus = bus;
        _pixelsPerMeter = RunSummaryConfig.Load().PixelsPerMeter;
        _start = player.GlobalPosition;
        _last = _start;
        _trace = new PlayTrace(bus, player);
        _bus.EnemyDamageResolved += OnDamage;
        _bus.EnemyKillResolved += OnKill;
        _bus.PlayerDamageResolved += OnPlayerDamage;
        _bus.FragmentChoicesReady += OnChoices;
        _bus.ChestOpened += OnChest;
        _bus.SmallPlaceUsed += OnPlace;
        _bus.MemorialInteracted += OnLandmark;
        _bus.WorkshopInteracted += OnLandmark;
        _bus.RiftInteracted += OnLandmark;
        _bus.RunEventStarted += OnEvent;
        _bus.CrisisStarted += OnCrisis;
        _bus.BossEncounterStarted += OnBoss;
    }

    /// <summary>À chaque image de jeu, pauses exclues : <paramref name="time"/> est le temps de jeu.</summary>
    public void Sample(double time)
    {
        _trace.Advance(time - _time);
        _time = time;
        Vector2 position = _player.GlobalPosition;
        TravelledMeters += Iso.ToGround(position - _last).Length() / _pixelsPerMeter;
        _last = position;
        double fromStart = Iso.ToGround(position - _start).Length() / _pixelsPerMeter;
        MaxDistanceMeters = Math.Max(MaxDistanceMeters, fromStart);
        if (time < _nextSample)
            return;
        _nextSample += 1.0;
        bool eliteNear = AnyEliteNear(position);
        if (eliteNear && !_eliteNear)
            Decide("elite");
        _eliteNear = eliteNear;
        // Un élite en vue occupe le joueur tant qu'il reste là : le creux ne court pas.
        if (eliteNear)
            _lastDecision = time;
        double since = time - _lastDecision;
        _longestGap = Math.Max(_longestGap, since);
        _rows.Append(CultureInfo.InvariantCulture,
            $"{time:F0},{TravelledMeters:F1},{fromStart:F1},{(int)_erasure.GetZonePhaseAt(position)},{_player.CurrentHp:F1},{_player.EffectiveMaxHp:F1},{_deaths},{since:F1},{(eliteNear ? 1 : 0)}\n");
    }

    public void AppendSummary(StringBuilder summary) => summary.Append(CultureInfo.InvariantCulture,
        $" travelled_m={TravelledMeters:F0} max_distance_m={MaxDistanceMeters:F0} deaths={_deaths} longest_gap_s={_longestGap:F0}");

    private bool AnyEliteNear(Vector2 position)
    {
        foreach (Node node in _tree.GetNodesInGroup("enemies"))
        {
            if (node is Enemy { IsActive: true, IsDying: false } enemy && IsElite(enemy)
                && enemy.GlobalPosition.DistanceTo(position) <= EliteRangePx)
                return true;
        }
        return false;
    }

    // Même règle que la mort d'une créature (EnemyKillResult.Elite) : variante élite ou champion, ou rang d'élite et au-delà.
    private static bool IsElite(Enemy enemy) =>
        enemy.IsPriorityTarget || enemy.Tier is EnemyTier.Elite or EnemyTier.Miniboss or EnemyTier.Boss;

    private void Decide(string kind)
    {
        _lastDecision = _time;
        _decisions.Append(CultureInfo.InvariantCulture, $"{_time:F1},{kind}\n");
    }

    private void OnDamage(DamageResult result)
    {
        if (!result.Applied || !result.Source.IsPlayerOwned || result.HpLost <= 0f)
            return;
        (int, string, DamageKind) key = ((int)(_time / 60.0), result.Source.Weapon?.Id ?? "-", result.Source.Kind);
        _damage[key] = _damage.GetValueOrDefault(key) + result.HpLost;
        _firstHit.TryAdd(result.Target, _time);
    }

    private void OnKill(EnemyKillResult kill)
    {
        if (!_firstHit.Remove(kill.Target, out double firstHit))
            return;
        _kills.Append(CultureInfo.InvariantCulture, $"{_time:F2},{kill.EnemyId},{(kill.Elite || kill.Sovereign ? 1 : 0)},{_time - firstHit:F2}\n");
    }

    private void OnPlayerDamage(PlayerDamageResult result)
    {
        if (result.PlayerId == _player.GetInstanceId() && result.Fatal)
            _deaths++;
    }

    private void OnChoices(int count) => Decide("choix");
    private void OnChest(string chestId, string rarity, Vector2 position) => Decide("coffre");
    private void OnPlace(string placeId, Vector2 position) => Decide("lieu");
    private void OnLandmark(Node2D landmark) => Decide("monument");
    private void OnEvent(string eventId, string title, string objective, float duration) => Decide("evenement");
    private void OnCrisis(int number, int intensity) => Decide("resurgence");
    private void OnBoss(int encounterId, string bossName, float maxHp, int notches) => Decide("souverain");

    public void Dispose()
    {
        _trace.Dispose();
        _bus.EnemyDamageResolved -= OnDamage;
        _bus.EnemyKillResolved -= OnKill;
        _bus.PlayerDamageResolved -= OnPlayerDamage;
        _bus.FragmentChoicesReady -= OnChoices;
        _bus.ChestOpened -= OnChest;
        _bus.SmallPlaceUsed -= OnPlace;
        _bus.MemorialInteracted -= OnLandmark;
        _bus.WorkshopInteracted -= OnLandmark;
        _bus.RiftInteracted -= OnLandmark;
        _bus.RunEventStarted -= OnEvent;
        _bus.CrisisStarted -= OnCrisis;
        _bus.BossEncounterStarted -= OnBoss;
        StringBuilder damage = new("minute,weapon,kind,damage\n");
        foreach (KeyValuePair<(int Minute, string Weapon, DamageKind Kind), double> entry in _damage)
            damage.Append(CultureInfo.InvariantCulture, $"{entry.Key.Minute},{entry.Key.Weapon},{entry.Key.Kind},{entry.Value:F1}\n");
        string root = ProjectSettings.GlobalizePath(_output);
        File.WriteAllText(Path.Combine(root, $"pace-{_seed}.csv"), _rows.ToString());
        File.WriteAllText(Path.Combine(root, $"damage-sources-{_seed}.csv"), damage.ToString());
        File.WriteAllText(Path.Combine(root, $"kills-{_seed}.csv"), _kills.ToString());
        File.WriteAllText(Path.Combine(root, $"decisions-{_seed}.csv"), _decisions.ToString());
        File.WriteAllText(Path.Combine(root, $"trace-{_seed}.csv"), _trace.ToCsv());
        File.WriteAllText(Path.Combine(root, $"trace-{_seed}.txt"),
            _trace.Summary(_player.CharacterId, _seed, _player.GetNodeOrNull<PlayerProgression>("PlayerProgression")?.CurrentLevel ?? 0) + "\n");
    }
}
