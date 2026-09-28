using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.World;

namespace Vestiges.Progression;

/// <summary>
/// Fragments de mémoire du level-up : trois choix entre armes et Souvenirs passifs, nouveaux ou améliorés.
/// Chaque amélioration tire sa rareté et ses gains à l'offre, pour que la carte montre exactement
/// ce qu'elle donne.
/// </summary>
public partial class FragmentManager : Node
{
    private const int FragmentsPerChoice = 3;
    private const int DefaultRerolls = 3;
    private const int DefaultBanishes = 3;

    private EventBus _eventBus;
    private Player _player;
    private int _currentLevel = 1;
    private int _peril;

    // Level-up queue (multi-level-up support)
    private readonly Queue<int> _levelUpQueue = new();
    private bool _choosingActive;

    // Réserve de niveaux : quand les niveaux affluent ou que la foule est dense, l'écran attend
    // quelques secondes et enchaîne ensuite tous les choix, au lieu de s'ouvrir et se refermer en boucle.
    private LevelReserveConfig _reserve;
    private Timer _holdTimer;
    private ulong _lastLevelUpMsec;
    private ulong _lastCloseMsec;
    private GroupCache _groupCache;

    // Reroll & Banish
    private int _rerollsRemaining = DefaultRerolls;
    private int _banishesRemaining = DefaultBanishes;
    private readonly HashSet<string> _banishedIds = new();

    public int RerollsRemaining => _rerollsRemaining;
    public int BanishesRemaining => _banishesRemaining;

    private readonly RandomNumberGenerator _rng = new();

    // Choix en attente — l'UI lit PendingChoices après le signal
    private readonly List<FragmentOption> _pendingChoices = new();

    /// <summary>Les choix de fragments en attente de sélection par le joueur.</summary>
    public IReadOnlyList<FragmentOption> PendingChoices => _pendingChoices;

    /// <summary>Niveaux gagnés qui attendent encore leur choix, après celui affiché.</summary>
    public int QueuedLevels => _levelUpQueue.Count;

    // Plus de signaux locaux — tout passe par l'EventBus (Autoload fiable)
    // Les signaux sur nodes dynamiques (new + AddChild) ne sont pas fiables en Godot C#.

    public override void _Ready()
    {
        PassiveSouvenirDataLoader.Load();

        _eventBus = GetNode<EventBus>("/root/EventBus");
        _groupCache = GetNode<GroupCache>("/root/GroupCache");
        _reserve = LevelReserveConfig.Load();
        _holdTimer = new Timer { OneShot = true, WaitTime = _reserve.HoldSeconds };
        _holdTimer.Timeout += ProcessNextInQueue;
        AddChild(_holdTimer);
        _eventBus.LevelUp += OnLevelUp;
        _eventBus.GameStateChanged += OnGameStateChanged;
        _eventBus.PerilChanged += OnPerilChanged;
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
        {
            _eventBus.LevelUp -= OnLevelUp;
            _eventBus.GameStateChanged -= OnGameStateChanged;
            _eventBus.PerilChanged -= OnPerilChanged;
        }
    }

    private void OnPerilChanged(int peril)
    {
        _peril = peril;
    }

    private void OnLevelUp(int newLevel)
    {
        GD.Print($"[FragmentManager] OnLevelUp called: level {newLevel}");
        CachePlayer();
        if (_player == null)
        {
            GD.PushWarning("[FragmentManager] OnLevelUp: player is null — level-up sera rattrapé par GameBootstrap");
            return;
        }

        bool hold = !_choosingActive && _holdTimer.IsStopped() && ShouldHold();
        _lastLevelUpMsec = Time.GetTicksMsec();
        if (_choosingActive || !_holdTimer.IsStopped() || hold)
        {
            _levelUpQueue.Enqueue(newLevel);
            if (hold)
                _holdTimer.Start();
            GD.Print($"[FragmentManager] Queued level {newLevel} ({_levelUpQueue.Count} in queue{(hold ? ", réserve" : "")})");
            return;
        }

        OfferFragments(newLevel);
    }

    /// <summary>Une run finie ne propose plus de choix : un niveau retenu ne doit pas s'ouvrir sur le bilan.</summary>
    private void OnGameStateChanged(string oldState, string newState)
    {
        if (newState != nameof(GameManager.GameState.Death))
            return;
        _holdTimer.Stop();
        _levelUpQueue.Clear();
    }

    private bool ShouldHold()
    {
        ulong windowMsec = (ulong)(_reserve.WindowSeconds * 1000f);
        ulong now = Time.GetTicksMsec();
        if (now - _lastLevelUpMsec < windowMsec || now - _lastCloseMsec < windowMsec)
            return true;

        float radiusSq = _reserve.CrowdRadius * _reserve.CrowdRadius;
        int near = 0;
        foreach (Node node in _groupCache.GetEnemies())
        {
            if (node is Enemy { IsActive: true } enemy && enemy.GlobalPosition.DistanceSquaredTo(_player.GlobalPosition) <= radiusSq
                && ++near > _reserve.Crowd)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Appelé par GameBootstrap pour rattraper des level-ups manqués
    /// (XP gagnée avant que le setup soit complet).
    /// </summary>
    public void TriggerLevelUp(int currentLevel)
    {
        GD.Print($"[FragmentManager] TriggerLevelUp (rattrapage): level {currentLevel}");
        CachePlayer();
        if (_player == null)
        {
            GD.PushWarning("[FragmentManager] TriggerLevelUp: player is null");
            return;
        }

        OfferFragments(currentLevel);
    }

    private void OfferFragments(int level)
    {
        _currentLevel = level;
        List<FragmentOption> options = BuildFragmentPool();
        if (options.Count == 0)
        {
            GD.PushWarning($"[FragmentManager] OfferFragments: pool is empty (level {level})");
            ProcessNextInQueue();
            return;
        }

        _pendingChoices.Clear();
        List<FragmentOption> picked = PickRandom(options, FragmentsPerChoice);
        EnsureSurvivalChoice(picked, options);
        foreach (FragmentOption option in picked)
            _pendingChoices.Add(RollUpgrade(option));
        _choosingActive = true;

        GD.Print($"[FragmentManager] Level {level} (maxTier={GetMaxFragmentTier(level)}): offering {_pendingChoices.Count} fragments (pool had {options.Count})");

        _eventBus.EmitSignal(EventBus.SignalName.FragmentChoicesReady, _pendingChoices.Count);
    }

    /// <summary>Traite le prochain level-up en attente, ou signale la fin des choix.</summary>
    private void ProcessNextInQueue()
    {
        if (_levelUpQueue.Count > 0)
        {
            int nextLevel = _levelUpQueue.Dequeue();
            GD.Print($"[FragmentManager] Processing queued level-up: {nextLevel} ({_levelUpQueue.Count} remaining)");
            OfferFragments(nextLevel);
        }
        else
        {
            if (_choosingActive)
                _lastCloseMsec = Time.GetTicksMsec();
            _choosingActive = false;
        }
    }

    /// <summary>Indique si un choix de fragment est actuellement actif (inclut le choix en cours + queue).</summary>
    public bool IsChoiceActive => _choosingActive;

    /// <summary>
    /// Tier max autorisé dans le pool de fragments selon le niveau du joueur.
    /// Progression graduelle : Tier 1 tôt, Tier 3 mid-game, Tier 4-5 très tard.
    /// Petite chance de voir un tier au-dessus du max normal (3% base + luck × 30%).
    /// </summary>
    private int GetMaxFragmentTier(int playerLevel)
    {
        int baseTier;
        if (playerLevel < 5) baseTier = 1;
        else if (playerLevel < 10) baseTier = 2;
        else if (playerLevel < 15) baseTier = 3;
        else if (playerLevel < 20) baseTier = 4;
        else baseTier = 5;

        if (baseTier < 5)
        {
            CachePlayer();
            float luck = _player?.LuckBonus ?? 0f;
            float tierBumpChance = 0.03f + luck * 0.30f;
            if (GD.Randf() < tierBumpChance)
                baseTier++;
        }

        return baseTier;
    }

    private List<FragmentOption> BuildFragmentPool()
    {
        List<FragmentOption> pool = new();
        int weaponCount = _player.WeaponSlots.Count;
        int passiveCount = _player.PassiveSlots.Count;
        bool weaponSlotsFull = weaponCount >= Player.MaxWeaponSlots;
        bool passiveSlotsFull = passiveCount >= Player.MaxPassiveSlots;

        // Armes nouvelles (si slots dispo)
        int maxTier = GetMaxFragmentTier(_currentLevel);
        if (!weaponSlotsFull)
        {
            HashSet<string> equippedIds = new();
            foreach (WeaponInstance w in _player.WeaponSlots)
                equippedIds.Add(w.Id);

            foreach (WeaponData weapon in WeaponDataLoader.GetAll())
            {
                if (equippedIds.Contains(weapon.Id))
                    continue;
                if (_banishedIds.Contains(weapon.Id))
                    continue;
                if (weapon.Tier > maxTier)
                    continue;
                if (!MetaSaveManager.IsWeaponUnlocked(weapon))
                    continue;

                pool.Add(new FragmentOption(weapon.Id, "weapon_new", weapon.Name, weapon.Tier));
            }
        }

        // Upgrades d'armes existantes
        foreach (WeaponInstance w in _player.WeaponSlots)
        {
            // Bannir une arme ou un passif l'écarte de la run entière, améliorations comprises.
            if (_banishedIds.Contains(w.Id))
                continue;
            if (!_player.IsWeaponFragmentMaxed(w.Id))
            {
                pool.Add(new FragmentOption(w.Id, "weapon_upgrade", w.Name, 1));
            }
        }

        // Souvenirs Passifs nouveaux (si slots dispo)
        if (!passiveSlotsFull)
        {
            HashSet<string> equippedPassiveIds = new();
            foreach (ActivePassiveSouvenir p in _player.PassiveSlots)
                equippedPassiveIds.Add(p.Id);

            foreach (PassiveSouvenirData passive in PassiveSouvenirDataLoader.GetAll())
            {
                if (equippedPassiveIds.Contains(passive.Id))
                    continue;
                if (_banishedIds.Contains(passive.Id))
                    continue;
                pool.Add(new FragmentOption(passive.Id, "passive_new", passive.Name, 1));
            }
        }

        // Upgrades de passifs existants
        foreach (ActivePassiveSouvenir p in _player.PassiveSlots)
        {
            if (_banishedIds.Contains(p.Id))
                continue;
            if (!p.IsMaxLevel)
            {
                pool.Add(new FragmentOption(p.Id, "passive_upgrade", p.Data.Name, 1));
            }
        }

        return pool;
    }

    /// <summary>Relance les choix de fragments (consomme un reroll).</summary>
    public void Reroll()
    {
        if (_rerollsRemaining <= 0)
            return;

        _rerollsRemaining--;
        GD.Print($"[FragmentManager] Reroll used ({_rerollsRemaining} remaining)");
        OfferFragments(_currentLevel);
    }

    /// <summary>Bannit un fragment du pool de la run entière et re-propose des choix.</summary>
    public void BanishFragment(string id)
    {
        if (_banishesRemaining <= 0)
            return;

        _banishedIds.Add(id);
        // Un bannissement qui viderait l'offre laisserait l'écran ouvert sans carte : il est refusé et non consommé.
        if (BuildFragmentPool().Count == 0)
        {
            _banishedIds.Remove(id);
            GD.Print($"[FragmentManager] Banish of '{id}' refused: nothing left to offer");
            return;
        }
        _banishesRemaining--;
        GD.Print($"[FragmentManager] Banished '{id}' ({_banishesRemaining} remaining, total banished: {_banishedIds.Count})");
        OfferFragments(_currentLevel);
    }

    public void AddRerolls(int count) => _rerollsRemaining += count;
    public void AddBanishes(int count) => _banishesRemaining += count;

    public void SelectFragment(FragmentOption option)
    {
        CachePlayer();
        if (_player == null)
            return;

        string fragmentId = option.Id;
        string fragmentType = option.Type;
        if (!option.ApplyTo(_player))
        {
            // L'offre a vieilli pendant l'écran (arme ramassée, emplacements pleins) : sans nouvelle offre,
            // l'écran fermé laissait le jeu en pause pour de bon.
            GD.PushWarning($"[FragmentManager] Choix devenu impossible ({fragmentId}, {fragmentType}) : nouvelle offre");
            OfferFragments(_currentLevel);
            return;
        }

        _pendingChoices.Clear();
        _eventBus.EmitSignal(EventBus.SignalName.FragmentChosen, fragmentId, fragmentType);
        GD.Print($"[FragmentManager] Fragment selected: {fragmentId} ({fragmentType}, {option.Rarity?.Id ?? "nouveau"})");
        ProcessNextInQueue();
    }

    /// <summary>Passer le choix : le niveau est acquis, aucune carte n'est prise.</summary>
    public void SkipChoice()
    {
        _pendingChoices.Clear();
        ProcessNextInQueue();
    }

    /// <summary>
    /// Rareté et gains d'une amélioration, tirés à l'offre : la Chance du joueur, l'oubli de la zone où il se
    /// tient et le Péril font monter la rareté (plan 17 §4.4). Les nouveautés n'ont pas de rareté.
    /// </summary>
    private FragmentOption RollUpgrade(FragmentOption option)
    {
        if (option.Type is not ("weapon_upgrade" or "passive_upgrade"))
            return option;

        ErasureManager.ErasureZonePhase phase = GetTree().CurrentScene?.GetNodeOrNull<ErasureManager>("ErasureManager")
            ?.GetZonePhaseAt(_player.GlobalPosition) ?? ErasureManager.ErasureZonePhase.Anchored;
        UpgradeRarity rarity = UpgradeRoller.RollRarity(UpgradeRoller.BumpSteps(_player.LuckBonus, phase, _peril), _rng);

        return UpgradeRoller.RollGains(option, _player, rarity, _rng);
    }

    /// <summary>
    /// Tant que le joueur n'a aucun passif de survie, une des cartes en propose un (plan 03, lot 8B) :
    /// sans cela, trois passifs de survie sur quatorze passaient souvent toute une run sans être vus.
    /// La dernière carte est remplacée, les deux premières gardant le mélange nouveauté / amélioration.
    /// </summary>
    private void EnsureSurvivalChoice(List<FragmentOption> picked, List<FragmentOption> pool)
    {
        if (picked.Count < 3)
            return;
        foreach (ActivePassiveSouvenir owned in _player.PassiveSlots)
            if (owned.Data.Survival)
                return;
        foreach (FragmentOption option in picked)
            if (IsSurvival(option))
                return;

        List<FragmentOption> survival = new();
        foreach (FragmentOption option in pool)
            if (IsSurvival(option))
                survival.Add(option);
        if (survival.Count == 0)
            return;
        picked[picked.Count - 1] = WeightedPick(survival, _player.LuckBonus);
    }

    private static bool IsSurvival(FragmentOption option) =>
        option.Type == "passive_new" && (PassiveSouvenirDataLoader.Get(option.Id)?.Survival ?? false);

    private List<FragmentOption> PickRandom(List<FragmentOption> pool, int count)
    {
        CachePlayer();
        float luck = _player?.LuckBonus ?? 0f;

        // Séparer new vs upgrade pour garantir un mélange
        List<FragmentOption> newItems = new();
        List<FragmentOption> upgrades = new();
        foreach (FragmentOption o in pool)
        {
            if (o.Type is "weapon_new" or "passive_new")
                newItems.Add(o);
            else
                upgrades.Add(o);
        }

        List<FragmentOption> result = new();
        List<FragmentOption> remaining = new(pool);

        // Garantir au moins 1 de chaque catégorie si possible
        if (newItems.Count > 0 && upgrades.Count > 0 && count >= 2)
        {
            FragmentOption picked = WeightedPick(newItems, luck);
            result.Add(picked);
            remaining.Remove(picked);

            picked = WeightedPick(upgrades, luck);
            result.Add(picked);
            remaining.Remove(picked);
        }

        // Remplir le reste avec sélection pondérée
        while (result.Count < count && remaining.Count > 0)
        {
            FragmentOption picked = WeightedPick(remaining, luck);
            result.Add(picked);
            remaining.Remove(picked);
        }

        return result;
    }

    private static FragmentOption WeightedPick(List<FragmentOption> options, float luck)
    {
        if (options.Count == 1)
            return options[0];

        float totalWeight = 0f;
        foreach (FragmentOption opt in options)
            totalWeight += GetFragmentWeight(opt, luck);

        float roll = GD.Randf() * totalWeight;
        float cumulative = 0f;
        foreach (FragmentOption opt in options)
        {
            cumulative += GetFragmentWeight(opt, luck);
            if (roll <= cumulative)
                return opt;
        }
        return options[options.Count - 1];
    }

    private static float GetFragmentWeight(FragmentOption opt, float luck)
    {
        float weight = 1f;
        // Tiers élevés sont plus rares, mais la luck les booste
        if (opt.SortWeight >= 4) weight = 0.3f + luck * 1.5f;
        else if (opt.SortWeight >= 3) weight = 0.5f + luck * 1.0f;
        // Upgrades légèrement favorisées (aide à compléter les builds)
        if (opt.Type.Contains("upgrade")) weight *= 1.15f;
        return Mathf.Max(weight, 0.1f);
    }

    private void CachePlayer()
    {
        if (_player != null && IsInstanceValid(_player))
            return;

        Node playerNode = GetTree().GetFirstNodeInGroup("player");
        if (playerNode is Player p)
            _player = p;
    }
}

public class FragmentOption
{
    public string Id { get; }
    public string Type { get; }
    public string DisplayName { get; }
    /// <summary>Tier de l'arme nouvelle (pondère l'offre), 1 sinon.</summary>
    public int SortWeight { get; }
    /// <summary>Rareté de l'amélioration ; null pour une arme ou un passif nouveaux.</summary>
    public UpgradeRarity Rarity { get; private init; }
    /// <summary>Gains d'une amélioration d'arme, tirés à l'offre.</summary>
    public IReadOnlyList<StatGain> WeaponGains { get; private init; } = System.Array.Empty<StatGain>();
    /// <summary>Amélioration de passif : multiple de l'effet d'un niveau, et niveaux gagnés.</summary>
    public float PassiveGain { get; private init; } = 1f;
    public int PassiveLevels { get; private init; } = 1;

    public FragmentOption(string id, string type, string displayName, int sortWeight)
    {
        Id = id;
        Type = type;
        DisplayName = displayName;
        SortWeight = sortWeight;
    }

    public FragmentOption WithWeaponUpgrade(UpgradeRarity rarity, IReadOnlyList<StatGain> gains) =>
        new(Id, Type, DisplayName, SortWeight) { Rarity = rarity, WeaponGains = gains };

    public FragmentOption WithPassiveUpgrade(UpgradeRarity rarity, float gain, int levels) =>
        new(Id, Type, DisplayName, SortWeight) { Rarity = rarity, PassiveGain = gain, PassiveLevels = levels };

    /// <summary>Donne le fragment au joueur ; faux si l'offre a vieilli (arme déjà là, emplacements pleins, maximum).</summary>
    public bool ApplyTo(Player player)
    {
        switch (Type)
        {
            case "weapon_new":
                WeaponData weaponData = WeaponDataLoader.Get(Id);
                return weaponData != null && player.AddWeapon(weaponData);
            case "weapon_upgrade":
                return player.UpgradeWeapon(Id, WeaponGains);
            case "passive_new":
                return player.AddOrUpgradePassive(Id, 1f, 1);
            case "passive_upgrade":
                return player.AddOrUpgradePassive(Id, PassiveGain, PassiveLevels);
            default:
                return false;
        }
    }
}
