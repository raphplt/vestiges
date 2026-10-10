using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;
using Vestiges.UI;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// Lot Q2 : chaque quête rejouée par ses vrais signaux, avec un suivi neuf par quête. Sous le seuil rien ne part ;
/// au seuil la quête s'enregistre aussitôt et le signal part une fois ; après, rien de plus. Puis la file du bandeau.
/// </summary>
public partial class QuestRegression
{
    private Player _player;
    private Node _run;
    private QuestTracker _tracker;
    private EventBus _bus;
    private readonly List<string> _signalled = new();
    private ulong _life;

    private async Task CheckTracking()
    {
        _bus = GetNode<EventBus>("/root/EventBus");
        _bus.QuestCompleted += OnQuestCompleted;
        _player = GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate<Player>();
        AddChild(_player);
        _player.InitializeCharacter(CharacterDataLoader.Get("vagabond"));
        _player.IsAIControlled = true;
        _player.SetPhysicsProcess(false);
        _player.GlobalPosition = Vector2.Zero;
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        CheckCumulativeAcrossRuns();
        CheckDeadline();
        int tested = 0;
        foreach (QuestDefinition quest in QuestDataLoader.GetAll())
        {
            if (MetaSaveManager.HasCompletedQuest(quest.Id))
                continue;
            StartRun();
            for (int i = 0; i < quest.Conditions.Count; i++)
            {
                bool last = i == quest.Conditions.Count - 1;
                Drive(quest.Conditions[i], true);
                if (!last)
                    Check(!MetaSaveManager.HasCompletedQuest(quest.Id), $"{quest.Id} : une condition seule ne suffit pas");
            }
            Check(MetaSaveManager.HasCompletedQuest(quest.Id) && Count(quest.Id) == 1, $"{quest.Id} : accomplie au seuil, un signal");
            Drive(quest.Conditions[0], false, false);
            Check(Count(quest.Id) == 1, $"{quest.Id} : rien de plus après le seuil");
            EndRun();
            tested++;
        }
        // Accomplies avant leur tour, et c'est voulu : Toutes les portes et Grandir (contrôles du cumul et du délai),
        // Bien au chaud (12 minutes à PV pleins dans le contrôle du délai), Retenir (150 ralentis, en chemin vers les
        // 300 de Sonner la récré). Les 32 autres l'ont été chacune par son propre jeu.
        Check(tested == 32, $"32 quêtes contrôlées une à une, aucune autre accomplie en chemin ({tested})");
        CheckToastQueue();
        _bus.QuestCompleted -= OnQuestCompleted;
    }

    private void OnQuestCompleted(string questId) => _signalled.Add(questId);
    private int Count(string questId) => _signalled.FindAll(id => id == questId).Count;

    private void StartRun()
    {
        _run = new Node { Name = "Run" };
        AddChild(_run);
        ErasureManager erasure = new() { Name = "ErasureManager", ProcessMode = ProcessModeEnum.Disabled };
        _run.AddChild(erasure);
        RunTracker runTracker = new() { Name = "RunTracker", ProcessMode = ProcessModeEnum.Disabled };
        _run.AddChild(runTracker);
        _tracker = new QuestTracker { Name = "QuestTracker" };
        _run.AddChild(_tracker);
        // Tout le test tient dans une image : le cache de groupe ne verrait pas ce joueur, on le donne au suivi.
        typeof(QuestTracker).GetField("_player", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_tracker, _player);
        SetHp(1f);
        _player.Velocity = new Vector2(120f, 0f);
    }

    private void EndRun()
    {
        RemoveChild(_run);
        _run.QueueFree();
    }

    /// <summary>
    /// Amène une condition juste sous sa cible en contrôlant que la quête n'est pas encore accomplie (sauf si une
    /// autre condition manque encore), puis jusqu'à la cible si <paramref name="finish"/>.
    /// </summary>
    private void Drive(QuestCondition condition, bool finish, bool expectOpen = true)
    {
        string quest = QuestOf(condition);
        QuestDefinition definition = QuestDataLoader.Get(quest);
        int index = definition.Conditions.IndexOf(condition);
        IReadOnlyList<float> stored = MetaSaveManager.GetQuestProgress(quest);
        // Un cumul repart de ce que les runs précédentes du test ont déjà retenu.
        float already = definition.Scope == QuestScope.Cumulative && index < stored.Count ? stored[index] : 0f;
        int target = Mathf.CeilToInt(condition.Target - already);
        switch (condition.Stat)
        {
            case QuestStat.HealthyTime:
                _tracker._Process(condition.Target - 1f);
                Check(!expectOpen || !MetaSaveManager.HasCompletedQuest(quest), $"{quest} : {condition.Target - 1f} s à PV pleins, pas encore");
                if (finish)
                    _tracker._Process(1f);
                return;
            case QuestStat.DistanceMeters:
                RunTracker runTracker = _run.GetNode<RunTracker>("RunTracker");
                Vector2 position = Vector2.Zero;
                runTracker.Journey.Track(position);
                while (runTracker.Journey.TravelledMeters < condition.Target - 1f)
                {
                    position += new Vector2(0f, 64f);
                    runTracker.Journey.Track(position);
                }
                _tracker._Process(0.5f);
                Check(!expectOpen || !MetaSaveManager.HasCompletedQuest(quest), $"{quest} : {runTracker.Journey.TravelledMeters:F0} m, pas encore");
                if (!finish)
                    return;
                while (runTracker.Journey.TravelledMeters < condition.Target)
                {
                    position += new Vector2(0f, 64f);
                    runTracker.Journey.Track(position);
                }
                _tracker._Process(0.5f);
                return;
        }
        if (IsMaximum(condition.Stat))
        {
            Step(condition, target - 1);
            Check(!expectOpen || !MetaSaveManager.HasCompletedQuest(quest), $"{quest} : {condition.Stat} à {target - 1}, pas encore");
            if (finish)
                Step(condition, target);
            return;
        }
        for (int i = 0; i < target - 1; i++)
            Step(condition, i + 1);
        Check(!expectOpen || !MetaSaveManager.HasCompletedQuest(quest), $"{quest} : {condition.Stat} à {target - 1}, pas encore");
        if (finish)
            Step(condition, target);
    }

    private static bool IsMaximum(string stat) => stat is QuestStat.Level or QuestStat.EssenceHeld or QuestStat.Peril
        or QuestStat.WeaponsAtLevel or QuestStat.KillBurst or QuestStat.OublisThroughCrisis;

    /// <summary>Un pas de la condition : une unité pour un compte, la valeur <paramref name="value"/> pour un maximum.</summary>
    private void Step(QuestCondition condition, int value)
    {
        switch (condition.Stat)
        {
            case QuestStat.SovereignKills: Kill(sovereign: true); break;
            case QuestStat.SovereignCritKills: Kill(sovereign: true, critical: true); break;
            case QuestStat.ErasedZoneKills: Kill(position: ErasedSpot()); break;
            case QuestStat.MeleeKills: Kill(weapon: "chipped_blade"); break;
            case QuestStat.RangedKills: Kill(weapon: "sling"); break;
            case QuestStat.WeaponKills: Kill(weapon: condition.Weapon); break;
            case QuestStat.SlowedKills: Kill(slowed: true); break;
            case QuestStat.BurningKills: Kill(burning: true); break;
            case QuestStat.CritKills: Kill(critical: true); break;
            case QuestStat.EliteCritKills: Kill(elite: true, critical: true); break;
            case QuestStat.CloseKills:
                // Distance au sol : à la verticale de l'écran, le même écart compte double.
                Kill(position: new Vector2(0f, condition.Param - 1f));
                Kill(position: new Vector2(condition.Param - 1f, 0f));
                break;
            case QuestStat.StillKills:
                _player.Velocity = Vector2.Zero;
                Kill();
                _player.Velocity = new Vector2(120f, 0f);
                break;
            case QuestStat.LowHpKills:
                SetHp(condition.Param - 0.05f);
                Kill();
                SetHp(1f);
                break;
            case QuestStat.KillBurst:
                // Toutes dans le même instant : la cible moins une, puis la dernière.
                for (int i = 0; i < (value == Mathf.CeilToInt(condition.Target) ? 1 : value); i++)
                    Kill(advance: false);
                break;
            case QuestStat.BarrierDefeated: _bus.EmitSignal(EventBus.SignalName.EnemyKilled, EnemyGrammar.BossPartId, Vector2.Zero); break;
            case QuestStat.IndicibleDefeated: _bus.EmitSignal(EventBus.SignalName.EnemyKilled, EnemyGrammar.FinalBossId, Vector2.Zero); break;
            case QuestStat.CrisesAfterIndicible:
                // Une Résurgence commencée avant la chute de l'Indicible ne compte pas, même finie après.
                _bus.EmitSignal(EventBus.SignalName.CrisisStarted, 1, 1);
                _bus.EmitSignal(EventBus.SignalName.EnemyKilled, EnemyGrammar.FinalBossId, Vector2.Zero);
                _bus.PublishPlayerDamage(new PlayerDamageResult(1, PlayerDamageKind.Combat, 100f, CostlyCrisis, false, false, true));
                _bus.EmitSignal(EventBus.SignalName.CrisisEnded, 1);
                Check(!MetaSaveManager.HasCompletedQuest(QuestOf(condition)), "Résurgence commencée avant la chute : non comptée");
                Crisis(CostlyCrisis);
                break;
            case QuestStat.CrisesSurvived: Crisis(CostlyCrisis); break;
            case QuestStat.CleanCrises:
                Crisis(_player.EffectiveMaxHp * condition.Param + 1f);
                Check(!MetaSaveManager.HasCompletedQuest(QuestOf(condition)), "Résurgence trop coûteuse : non comptée");
                Crisis(_player.EffectiveMaxHp * condition.Param - 1f);
                break;
            case QuestStat.OublisThroughCrisis: OublisCrisis(value); break;
            case QuestStat.MemorialsRevived: _bus.EmitSignal(EventBus.SignalName.MemorialAwakened, Vector2.Zero); break;
            case QuestStat.WaymarksFound: _bus.EmitSignal(EventBus.SignalName.WaymarkFound, "well"); break;
            case QuestStat.EventsSucceeded:
                _bus.EmitSignal(EventBus.SignalName.RunEventEnded, "hunt", false, "");
                _bus.EmitSignal(EventBus.SignalName.RunEventEnded, "hunt", true, "");
                break;
            case QuestStat.ZonesDiscovered: _bus.EmitSignal(EventBus.SignalName.ZoneDiscovered, 0, 0, 1); break;
            case QuestStat.ChestsOpened: _bus.EmitSignal(EventBus.SignalName.ChestOpened, "chest", "common", Vector2.Zero); break;
            case QuestStat.Level: _bus.EmitSignal(EventBus.SignalName.LevelUp, value); break;
            case QuestStat.EssenceHeld: _bus.EmitSignal(EventBus.SignalName.EssenceChanged, value); break;
            case QuestStat.Peril: _bus.EmitSignal(EventBus.SignalName.PerilChanged, value); break;
            case QuestStat.DamageTaken: _bus.PublishPlayerDamage(new PlayerDamageResult(1, PlayerDamageKind.Combat, 100f, 1f, false, false, true)); break;
            case QuestStat.HpHealed: _bus.PublishPlayerHealing(new HealingResult(1, HealingKind.Normal, 1f, 1f, 0f, true)); break;
            case QuestStat.Ascensions: _bus.EmitSignal(EventBus.SignalName.WeaponUpgraded, "sling", 0, "ascension", 30); break;
            case QuestStat.WeaponsAtLevel: WeaponsAtLevel(value, (int)condition.Param); break;
            default: throw new InvalidOperationException($"fait sans pilote de test : {condition.Stat}");
        }
    }

    /// <summary>PV perdus pendant une Résurgence ordinaire du test : trop pour « Tenir le feu ».</summary>
    private float CostlyCrisis => _player.EffectiveMaxHp * 0.3f;

    /// <summary>
    /// Une élimination publiée comme le fait <c>Enemy.Die</c>. L'horloge avance d'un dixième de seconde avant, sauf
    /// <paramref name="advance"/> faux : une longue série ne fait pas un « Bouquet final » par accident.
    /// </summary>
    private void Kill(bool sovereign = false, bool critical = false, bool elite = false, bool slowed = false,
        bool burning = false, string weapon = null, Vector2? position = null, bool advance = true)
    {
        if (advance)
            _tracker._Process(0.1f);
        WeaponInstance instance = weapon != null ? new WeaponInstance(WeaponDataLoader.Get(weapon)) : null;
        AttackContext source = instance != null ? new AttackContext(_player.GetInstanceId(), instance, 1, DamageKind.DirectWeapon) : default;
        DamageResult damage = new DamageResult(new EnemyLife(++_life, 1), source, 10f, 10f, 0f, 10f, true, true) { Critical = critical };
        ControlState slow = slowed ? new ControlState(0.5f, 1f, source, ControlOrigin.NativeWeapon) : default;
        ControlState burn = burning ? new ControlState(5f, 1f, source, ControlOrigin.Unknown) : default;
        _bus.PublishEnemyKill(new EnemyKillResult(new EnemyLife(_life, 1), "crawler", position ?? new Vector2(400f, 0f), damage,
            slow, default, burn, elite || sovereign, sovereign));
    }

    private Vector2 ErasedSpot()
    {
        ErasureManager erasure = _run.GetNode<ErasureManager>("ErasureManager");
        Vector2 spot = new(5000f, 5000f);
        int cellSize = (int)typeof(ErasureManager).GetField("_cellSize", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(erasure)!;
        Dictionary<Vector2I, ErasureManager.ErasureZonePhase> phases = (Dictionary<Vector2I, ErasureManager.ErasureZonePhase>)typeof(ErasureManager)
            .GetField("_zonePhases", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(erasure)!;
        phases[new Vector2I(Mathf.FloorToInt(spot.X / cellSize), Mathf.FloorToInt(spot.Y / cellSize))] = ErasureManager.ErasureZonePhase.Frayed;
        return spot;
    }

    private void Crisis(float hpLost)
    {
        _bus.EmitSignal(EventBus.SignalName.CrisisStarted, 1, 1);
        if (hpLost > 0f)
            _bus.PublishPlayerDamage(new PlayerDamageResult(1, PlayerDamageKind.Combat, 100f, hpLost, false, false, true));
        _bus.EmitSignal(EventBus.SignalName.CrisisEnded, 1);
    }

    /// <summary>
    /// Traverse une Résurgence en portant <paramref name="count"/> Oublis ; un de plus pris pendant la crise ne compte
    /// pas, la Résurgence n'a pas été traversée entière avec lui.
    /// </summary>
    private void OublisCrisis(int count)
    {
        PerilManager peril = new() { Name = "PerilManager" };
        _run.AddChild(peril);
        OubliData oubli = OubliDataLoader.All[0];
        for (int i = 0; i < count; i++)
            peril.AddOubli(oubli);
        _bus.EmitSignal(EventBus.SignalName.CrisisStarted, 1, 1);
        peril.AddOubli(oubli);
        _bus.PublishPlayerDamage(new PlayerDamageResult(1, PlayerDamageKind.Combat, 100f, CostlyCrisis, false, false, true));
        _bus.EmitSignal(EventBus.SignalName.CrisisEnded, 1);
        _run.RemoveChild(peril);
        peril.QueueFree();
    }

    /// <summary>Quatre armes dont <paramref name="count"/> au niveau <paramref name="level"/>, les autres au niveau 1.</summary>
    private void WeaponsAtLevel(int count, int level)
    {
        while (_player.WeaponSlots.Count > 0)
            _player.RemoveWeapon(0);
        string[] ids = { "chipped_blade", "sling", "sharpened_pipe", "crossbow" };
        FieldInfo levelField = typeof(WeaponInstance).GetField("_level", BindingFlags.Instance | BindingFlags.NonPublic)!;
        for (int i = 0; i < ids.Length; i++)
        {
            WeaponInstance weapon = new(WeaponDataLoader.Get(ids[i]));
            levelField.SetValue(weapon, i < count ? level : 1);
            _player.AddWeapon(weapon);
        }
        _bus.EmitSignal(EventBus.SignalName.WeaponInventoryChanged);
    }

    private void SetHp(float ratio) =>
        typeof(Player).GetField("_currentHp", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_player, _player.EffectiveMaxHp * ratio);

    private static string QuestOf(QuestCondition condition)
    {
        foreach (QuestDefinition quest in QuestDataLoader.GetAll())
            if (quest.Conditions.Contains(condition))
                return quest.Id;
        return "?";
    }

    /// <summary>Un cumul se poursuit d'une run à l'autre : avancée retenue à la mort, reprise par le suivi suivant.</summary>
    private void CheckCumulativeAcrossRuns()
    {
        QuestDefinition doors = QuestDataLoader.Get("every_door");
        StartRun();
        for (int i = 0; i < 60; i++)
            _bus.EmitSignal(EventBus.SignalName.ChestOpened, "chest", "common", Vector2.Zero);
        _bus.EmitSignal(EventBus.SignalName.GameStateChanged, "Run", nameof(GameManager.GameState.Death));
        EndRun();
        Check(MetaSaveManager.GetQuestProgress(doors.Id) is { Count: 1 } stored && Mathf.IsEqualApprox(stored[0], 60f), "cumul retenu à la mort : 60 coffres");
        StartRun();
        for (int i = 0; i < 39; i++)
            _bus.EmitSignal(EventBus.SignalName.ChestOpened, "chest", "common", Vector2.Zero);
        Check(!MetaSaveManager.HasCompletedQuest(doors.Id), "99 coffres en deux runs : pas encore");
        _bus.EmitSignal(EventBus.SignalName.ChestOpened, "chest", "common", Vector2.Zero);
        Check(MetaSaveManager.HasCompletedQuest(doors.Id), "100ᵉ coffre de la seconde run : Trousseau débloqué en pleine run");
        EndRun();

        QuestDefinition round = QuestDataLoader.Get("the_round");
        StartRun();
        for (int i = 0; i < 4; i++)
            _bus.EmitSignal(EventBus.SignalName.WaymarkFound, "well");
        EndRun();
        Check(MetaSaveManager.GetQuestProgress(round.Id) is { Count: 2 } best && Mathf.IsEqualApprox(best[1], 4f),
            "quête d'une run : meilleure valeur retenue à la sortie de la run");
    }

    private void CheckDeadline()
    {
        StartRun();
        _tracker._Process(721f);
        _bus.EmitSignal(EventBus.SignalName.LevelUp, 30);
        Check(!MetaSaveManager.HasCompletedQuest("against_the_clock") && MetaSaveManager.HasCompletedQuest("grow"),
            "niveau 30 après la 12ᵉ minute : Grandir oui, Contre la montre non");
        EndRun();
    }

    private void CheckToastQueue()
    {
        QuestToast toast = new() { Name = "QuestToast" };
        AddChild(toast);
        _bus.EmitSignal(EventBus.SignalName.QuestCompleted, "rake");
        _bus.EmitSignal(EventBus.SignalName.QuestCompleted, "fever");
        Check(toast.ShownText.Contains("Ratisser") && toast.ShownText.Contains("Râteau") && toast.PendingCount == 1,
            $"bandeau : la première quête affichée, la seconde en file ({toast.ShownText})");
        RemoveChild(toast);
        toast.QueueFree();
    }
}
