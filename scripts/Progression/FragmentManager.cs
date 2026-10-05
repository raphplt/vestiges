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
/// ce qu'elle donne. Aux paliers de perks, le niveau propose des perks à la place (plan 05, B1).
/// </summary>
public partial class FragmentManager : Node
{
    private const int FragmentsPerChoice = 3;
    private const int DefaultRerolls = 3;
    private const string CarriedChoiceEffect = SpecializationRuntime.CarriedChoiceEffect;

    private EventBus _eventBus;
    private Player _player;
    private int _currentLevel = 1;
    // Palier tiré pour la dernière offre, gardé pour le journal : le retirer referait un tirage.
    private int _peril;

    // Level-up queue (multi-level-up support)
    private readonly Queue<int> _levelUpQueue = new();
    private bool _choosingActive;


    // Reroll & Banish
    private int _rerollsRemaining = DefaultRerolls;
    // Bannir, c'est oublier (plan 21 §16) : les premiers sont gratuits, les suivants coûtent du Péril, de plus en plus.
    private int _banishesRemaining;
    private int _paidBanishes;
    // Dette en fractions de Péril (1 / BanishPerilDivisor), réglée par points entiers : aucune dérive d'arrondi.
    private int _banishPerilDebt;
    private readonly HashSet<string> _banishedIds = new();

    private PerkSpecializationOffers _specializations;
    private readonly CarriedChoice _carriedChoice = new();
    private bool _specializationChoice;

    public int RerollsRemaining => _rerollsRemaining;
    /// <summary>Bannissements gratuits restants.</summary>
    public int BanishesRemaining => _banishesRemaining;

    /// <summary>Fractions de Péril que coûtera le prochain bannissement (0 tant qu'il en reste de gratuits).</summary>
    public int NextBanishPerilFractions => _banishesRemaining > 0 ? 0 : _paidBanishes + 1;

    /// <summary>Même coût en points de Péril, pour les mesures.</summary>
    public float NextBanishPerilCost => NextBanishPerilFractions / (float)PerilDataLoader.BanishPerilDivisor;

    private readonly RandomNumberGenerator _rng = RunRandom.Create("level_up_offers");

    // Choix en attente — l'UI lit PendingChoices après le signal
    private readonly List<FragmentOption> _pendingChoices = new();

    /// <summary>Les choix de fragments en attente de sélection par le joueur.</summary>
    public IReadOnlyList<FragmentOption> PendingChoices => _pendingChoices;

    /// <summary>Niveaux gagnés qui attendent encore leur choix, après celui affiché.</summary>
    public int QueuedLevels => _levelUpQueue.Count;

    /// <summary>Le choix affiché sert un droit de fragment (après une Résurgence) plutôt qu'un choix de niveau.</summary>
    public bool IsSpecializationChoice => _specializationChoice;

    /// <summary>Emplacements de perks de la run.</summary>
    public int SpecializationCapacity => _specializations?.Capacity ?? 0;

    // Plus de signaux locaux — tout passe par l'EventBus (Autoload fiable)
    // Les signaux sur nodes dynamiques (new + AddChild) ne sont pas fiables en Godot C#.

    public override void _Ready()
    {
        PassiveSouvenirDataLoader.Load();
        PerkSpecializationDataLoader.Load();
        _specializations = new PerkSpecializationOffers(PerkSpecializationDataLoader.Config);
        _banishesRemaining = PerilDataLoader.BanishFree;

        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.LevelUp += OnLevelUp;
        _eventBus.GameStateChanged += OnGameStateChanged;
        _eventBus.PerilChanged += OnPerilChanged;
        _eventBus.CrisisEnded += OnCrisisEnded;
        _eventBus.ChoiceTokensGranted += OnChoiceTokensGranted;
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
        {
            _eventBus.LevelUp -= OnLevelUp;
            _eventBus.GameStateChanged -= OnGameStateChanged;
            _eventBus.PerilChanged -= OnPerilChanged;
            _eventBus.CrisisEnded -= OnCrisisEnded;
            _eventBus.ChoiceTokensGranted -= OnChoiceTokensGranted;
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

        // Jeton de fête foraine, palier 25 : la relance arrive avant l'écran de ce niveau.
        if (_player.ObjectMilestones?.GrantsRerollAt(newLevel) == true)
            AddRerolls(1);
        // Un fragment reporté retrouve une occasion à chaque niveau gagné.
        _specializations.Resume();
        // Un niveau gagné pendant un choix attend son tour dans le même écran ; sinon l'écran s'ouvre aussitôt.
        if (_choosingActive)
        {
            _levelUpQueue.Enqueue(newLevel);
            GD.Print($"[FragmentManager] Queued level {newLevel} ({_levelUpQueue.Count} in queue)");
            return;
        }

        // Un fragment en attente passe avant ce niveau, qui suit dans la même file.
        if (TryOfferSpecializations())
        {
            _levelUpQueue.Enqueue(newLevel);
            return;
        }
        OfferFragments(newLevel);
    }

    /// <summary>
    /// Une Résurgence survécue cristallise un fragment (plan 21 §16) : offert aussitôt, ou juste après le choix en
    /// cours, avant les niveaux en attente.
    /// </summary>
    private void OnCrisisEnded(int crisisNumber)
    {
        CachePlayer();
        if (_player == null || !_specializations.GrantMoment(_player))
            return;
        GD.Print($"[FragmentManager] Résurgence {crisisNumber} survécue : droit de fragment ({_specializations.PendingMoments} en attente)");
        if (!_choosingActive)
            TryOfferSpecializations();
    }

    /// <summary>Une run finie ne propose plus de choix : un niveau en attente ne doit pas s'ouvrir sur le bilan.</summary>
    private void OnGameStateChanged(string oldState, string newState)
    {
        if (newState == nameof(GameManager.GameState.Death))
            _levelUpQueue.Clear();
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
        _specializationChoice = false;

        // Seconde lecture : la carte reportée prend une place, les autres ne réaméliorent pas la même arme.
        FragmentOption carried = _carriedChoice.Validate(_player, _banishedIds);
        List<FragmentOption> options = BuildFragmentPool();
        List<FragmentOption> ascension = AscensionChoices();
        if (carried != null)
            options.RemoveAll(option => option.Type == CarriedChoice.UpgradeType && option.Id == carried.Id);
        if (options.Count == 0 && carried == null && ascension.Count == 0)
        {
            GD.PushWarning($"[FragmentManager] OfferFragments: pool is empty (level {level})");
            ProcessNextInQueue();
            return;
        }

        _pendingChoices.Clear();
        List<FragmentOption> picked = PickRandom(options, Mathf.Max(0, FragmentsPerChoice - (carried != null ? 1 : 0) - ascension.Count));
        picked.InsertRange(0, ascension);
        if (carried != null)
            picked.Insert(0, carried);
        EnsureSurvivalChoice(picked, options);
        foreach (FragmentOption option in picked)
            _pendingChoices.Add(option.IsCarried ? option : RollUpgrade(option));
        _choosingActive = true;

        GD.Print($"[FragmentManager] Level {level}: offering {_pendingChoices.Count} fragments (pool had {options.Count})");

        _eventBus.EmitSignal(EventBus.SignalName.FragmentChoicesReady, _pendingChoices.Count);
    }

    /// <summary>Offre de fragments si un droit est prêt ; faux sinon, ou sans candidat (le droit est alors reporté).</summary>
    private bool TryOfferSpecializations()
    {
        if (!_specializations.IsReady(_player))
            return false;
        List<FragmentOption> candidates = _specializations.Candidates(_player, _banishedIds);
        if (candidates.Count == 0)
        {
            _specializations.Defer();
            GD.Print("[FragmentManager] Aucun fragment proposable : droit reporté");
            return false;
        }

        _pendingChoices.Clear();
        _pendingChoices.AddRange(_specializations.Pick(candidates, _player.Specializations.Count == 0, _rng));
        _specializationChoice = true;
        _choosingActive = true;
        GD.Print($"[FragmentManager] Offering {_pendingChoices.Count} fragments (slot {_player.Specializations.Count + 1}/{_specializations.Capacity})");
        _eventBus.EmitSignal(EventBus.SignalName.FragmentChoicesReady, _pendingChoices.Count);
        return true;
    }

    /// <summary>Traite le prochain level-up en attente, ou signale la fin des choix.</summary>
    private void ProcessNextInQueue()
    {
        if (TryOfferSpecializations())
            return;
        if (_levelUpQueue.Count > 0)
        {
            int nextLevel = _levelUpQueue.Dequeue();
            // Un fragment passé retrouve une occasion après ce niveau déjà gagné.
            _specializations.Resume();
            GD.Print($"[FragmentManager] Processing queued level-up: {nextLevel} ({_levelUpQueue.Count} remaining)");
            OfferFragments(nextLevel);
        }
        else
        {
            _choosingActive = false;
            _specializationChoice = false;
            _pendingChoices.Clear();
        }
    }

    /// <summary>Indique si un choix de fragment est actuellement actif (inclut le choix en cours + queue).</summary>
    public bool IsChoiceActive => _choosingActive;

    /// <summary>
    /// Ascension (plan 21 §3) : la première arme au niveau maximal qui n'a pas choisi montre ses deux voies, à chaque
    /// offre, jusqu'à ce qu'une soit prise. Une arme bannie ne les propose plus.
    /// </summary>
    private List<FragmentOption> AscensionChoices()
    {
        List<FragmentOption> choices = new();
        foreach (WeaponInstance weapon in _player.WeaponSlots)
        {
            if (!weapon.CanAscend || _banishedIds.Contains(weapon.Id))
                continue;
            foreach (WeaponAscensionData ascension in weapon.Base.Ascensions)
                choices.Add(FragmentOption.ForAscension(weapon, ascension));
            break;
        }
        return choices;
    }

    private List<FragmentOption> BuildFragmentPool()
    {
        List<FragmentOption> pool = new();
        int weaponCount = _player.WeaponSlots.Count;
        int passiveCount = _player.PassiveSlots.Count;
        bool weaponSlotsFull = weaponCount >= Player.MaxWeaponSlots;
        bool passiveSlotsFull = passiveCount >= Player.MaxPassiveSlots;

        // Armes nouvelles (si slots dispo) : toutes celles qui sont débloquées, au même poids, dès le premier niveau.
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
                if (!MetaSaveManager.IsWeaponUnlocked(weapon))
                    continue;

                pool.Add(new FragmentOption(weapon.Id, "weapon_new", weapon.Name));
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
                pool.Add(new FragmentOption(w.Id, "weapon_upgrade", w.Name));
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
                pool.Add(new FragmentOption(passive.Id, "passive_new", passive.Name));
            }
        }

        // Upgrades de passifs existants
        foreach (ActivePassiveSouvenir p in _player.PassiveSlots)
        {
            if (_banishedIds.Contains(p.Id))
                continue;
            if (!p.IsMaxLevel)
            {
                pool.Add(new FragmentOption(p.Id, "passive_upgrade", p.Data.Name));
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
        RenewOffer();
    }

    /// <summary>
    /// Bannit une carte pour la run et renouvelle l'offre. Les premiers bannissements sont gratuits, les suivants
    /// coûtent du Péril, de plus en plus. Un bannissement qui viderait l'offre (ou, pour un fragment, empêcherait de
    /// remplir les emplacements restants) est refusé et ne coûte rien.
    /// </summary>
    public void BanishFragment(string id)
    {
        if (_specializationChoice)
        {
            if (!_specializations.TryBanish(id, _player, _banishedIds))
            {
                GD.Print($"[FragmentManager] Banish of fragment '{id}' refused: slots or offer would be left empty");
                return;
            }
        }
        else
        {
            _banishedIds.Add(id);
            if (BuildFragmentPool().Count == 0)
            {
                _banishedIds.Remove(id);
                GD.Print($"[FragmentManager] Banish of '{id}' refused: nothing left to offer");
                return;
            }
        }
        PayBanish();
        GD.Print($"[FragmentManager] Banished '{id}' ({_banishesRemaining} free left, {_paidBanishes} paid)");
        RenewOffer();
    }

    /// <summary>Bannir, c'est oublier : au-delà des gratuits, la dette de Péril croît et se règle par points entiers.</summary>
    private void PayBanish()
    {
        if (_banishesRemaining > 0)
        {
            _banishesRemaining--;
            return;
        }
        _paidBanishes++;
        _banishPerilDebt += _paidBanishes;
        int points = _banishPerilDebt / PerilDataLoader.BanishPerilDivisor;
        // Sans gestionnaire de Péril, la dette attend ; au Péril maximal, rien de plus ne peut être payé.
        if (points <= 0 || PerilManager.Current == null)
            return;
        _banishPerilDebt -= points * PerilDataLoader.BanishPerilDivisor;
        PerilManager.Current.AddPeril(points);
    }

    /// <summary>Nouvelle offre du même moment : fragments après une Résurgence, sinon le niveau en cours.</summary>
    private void RenewOffer()
    {
        if (_specializationChoice)
        {
            if (!TryOfferSpecializations())
                ProcessNextInQueue();
            return;
        }
        OfferFragments(_currentLevel);
    }

    public void AddRerolls(int count) => _rerollsRemaining += count;

    private void OnChoiceTokensGranted(int rerolls, int banishes)
    {
        AddRerolls(rerolls);
        AddBanishes(banishes);
    }
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
            RenewOffer();
            return;
        }

        if (_specializationChoice)
            _specializations.Consume();
        else
            _carriedChoice.Resolve(_pendingChoices, option, _player.HasSpecializationEffect(CarriedChoiceEffect));
        _pendingChoices.Clear();
        _specializationChoice = false;
        _eventBus.EmitSignal(EventBus.SignalName.FragmentChosen, fragmentId, fragmentType);
        GD.Print($"[FragmentManager] Fragment selected: {fragmentId} ({fragmentType}, {option.Rarity?.Id ?? "nouveau"})");
        ProcessNextInQueue();
    }

    /// <summary>Passer le choix : un niveau est acquis sans carte ; un fragment passé attend le prochain niveau.</summary>
    public void SkipChoice()
    {
        _pendingChoices.Clear();
        if (_specializationChoice)
            _specializations.Defer();
        else
            _carriedChoice.Clear();
        _specializationChoice = false;
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
        UpgradeRarity rarity = UpgradeRoller.RollRarity(UpgradeRoller.BumpSteps(_player.LuckBonus, phase, _peril), _rng, out UpgradeRarity rolled);

        return UpgradeRoller.RollGains(option, _player, rarity, _rng).WithRolledRarity(rolled);
    }

    /// <summary>
    /// Tant que le joueur n'a aucun passif de survie, une des cartes en propose un (plan 03, lot 8B) :
    /// sans cela, trois passifs de survie sur quatorze passaient souvent toute une run sans être vus.
    /// La dernière carte est remplacée, les deux premières gardant le mélange nouveauté / amélioration.
    /// </summary>
    private void EnsureSurvivalChoice(List<FragmentOption> picked, List<FragmentOption> pool)
    {
        // Les voies d'une ascension vont ensemble : la carte de survie ne prend jamais la place de l'une d'elles.
        if (picked.Count < 3 || picked[^1].Type == FragmentOption.AscensionType)
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
        picked[picked.Count - 1] = WeightedPick(survival);
    }

    private static bool IsSurvival(FragmentOption option) =>
        option.Type == "passive_new" && (PassiveSouvenirDataLoader.Get(option.Id)?.Survival ?? false);

    private List<FragmentOption> PickRandom(List<FragmentOption> pool, int count)
    {
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
            FragmentOption picked = WeightedPick(newItems);
            result.Add(picked);
            remaining.Remove(picked);

            picked = WeightedPick(upgrades);
            result.Add(picked);
            remaining.Remove(picked);
        }

        // Remplir le reste avec sélection pondérée
        while (result.Count < count && remaining.Count > 0)
        {
            FragmentOption picked = WeightedPick(remaining);
            result.Add(picked);
            remaining.Remove(picked);
        }

        return result;
    }

    private FragmentOption WeightedPick(List<FragmentOption> options)
    {
        if (options.Count == 1)
            return options[0];

        float totalWeight = 0f;
        foreach (FragmentOption opt in options)
            totalWeight += GetFragmentWeight(opt);

        float roll = _rng.Randf() * totalWeight;
        float cumulative = 0f;
        foreach (FragmentOption opt in options)
        {
            cumulative += GetFragmentWeight(opt);
            if (roll <= cumulative)
                return opt;
        }
        return options[options.Count - 1];
    }

    private static float GetFragmentWeight(FragmentOption opt)
    {
        LevelUpOfferConfig offer = LevelUpOfferConfig.Load();
        float weight = 1f;
        if (opt.Type.Contains("upgrade"))
            weight *= offer.UpgradeWeight;
        if (opt.Type is "passive_new" or "passive_upgrade")
            weight *= PassiveSouvenirDataLoader.Get(opt.Id)?.OfferWeight ?? 1f;
        return Mathf.Max(weight, offer.MinWeight);
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
    public const string AscensionType = "weapon_ascension";

    public string Id { get; }
    public string Type { get; }
    public string DisplayName { get; }
    /// <summary>Rareté de l'amélioration ; null pour une arme ou un passif nouveaux.</summary>
    public UpgradeRarity Rarity { get; private init; }
    /// <summary>Gains d'une amélioration d'arme, tirés à l'offre.</summary>
    public IReadOnlyList<StatGain> WeaponGains { get; private init; } = System.Array.Empty<StatGain>();
    /// <summary>Amélioration d'objet : un niveau, dont le gain multiplie le pas de l'objet selon la rareté (plan 23 R3).</summary>
    public float PassiveGain { get; private init; } = 1f;
    /// <summary>Rareté tirée avant que la Chance, l'oubli de la zone ou le Péril ne la montent ; null hors amélioration.</summary>
    public UpgradeRarity RolledRarity { get; private init; }
    /// <summary>La Chance (ou l'oubli, le Péril) a monté la rareté de cette carte : l'écran le montre (plan 24 D2).</summary>
    public bool RarityRaised => RolledRarity != null && Rarity != null && RolledRarity.Rank < Rarity.Rank;
    /// <summary>Voie d'ascension proposée pour l'arme <see cref="Id"/>.</summary>
    public WeaponAscensionData Ascension { get; private init; }

    public static FragmentOption ForAscension(WeaponInstance weapon, WeaponAscensionData ascension) =>
        new(weapon.Id, AscensionType, $"{weapon.Name} : {ascension.Name}") { Ascension = ascension };

    public FragmentOption(string id, string type, string displayName)
    {
        Id = id;
        Type = type;
        DisplayName = displayName;
    }

    public FragmentOption WithWeaponUpgrade(UpgradeRarity rarity, IReadOnlyList<StatGain> gains) =>
        new(Id, Type, DisplayName) { Rarity = rarity, WeaponGains = gains };

    /// <summary>Carte reportée par Seconde lecture : mêmes gains et même rareté, marquée pour l'écran.</summary>
    public bool IsCarried { get; private init; }

    public FragmentOption AsCarried() =>
        new(Id, Type, DisplayName) { Rarity = Rarity, WeaponGains = WeaponGains, PassiveGain = PassiveGain, IsCarried = true };

    public FragmentOption WithRolledRarity(UpgradeRarity rolled) =>
        new(Id, Type, DisplayName)
        {
            Rarity = Rarity, WeaponGains = WeaponGains, PassiveGain = PassiveGain, Ascension = Ascension, RolledRarity = rolled,
        };

    public FragmentOption WithPassiveUpgrade(UpgradeRarity rarity) =>
        new(Id, Type, DisplayName) { Rarity = rarity, PassiveGain = rarity.PassiveGain };

    /// <summary>Donne le fragment au joueur ; faux si l'offre a vieilli (arme déjà là, emplacements pleins, maximum).</summary>
    public bool ApplyTo(Player player)
    {
        switch (Type)
        {
            case "weapon_new":
                WeaponData weaponData = WeaponDataLoader.Get(Id);
                return weaponData != null && player.AddWeapon(weaponData);
            case "weapon_upgrade":
                return player.UpgradeWeapon(Id, WeaponGains, Rarity?.Id);
            case AscensionType:
                return player.AscendWeapon(Id, Ascension.Id);
            case "passive_new":
                return player.AddOrUpgradePassive(Id);
            case "passive_upgrade":
                return player.AddOrUpgradePassive(Id, 1, PassiveGain);
            case PerkSpecializationOffers.OptionType:
                return player.AcquireSpecialization(PerkSpecializationDataLoader.Get(Id));
            default:
                return false;
        }
    }
}
