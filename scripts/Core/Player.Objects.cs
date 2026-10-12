using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Combat;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.Core;

/// <summary>
/// Objets du joueur (plan 21 §4, plan 23 R3, plan 35). Les Écussons, quatre au plus, montent de niveau carte après carte,
/// avec des gains selon la rareté et des paliers. Les objets vont dans un sac, un type par place : chaque
/// exemplaire ajoute son effet de base, sans palier ; l’oubli retire exactement cet apport.
/// </summary>
public partial class Player
{
    private static readonly Color MilestoneColor = new(1f, 0.82f, 0.35f);
    private static readonly Color IgnoredHitColor = new(0.72f, 0.72f, 0.7f);
    private const ulong IgnoredHitFlashIntervalMsec = 250;
    private ulong _lastIgnoredHitFlashMsec;

    private readonly List<ActivePassiveSouvenir> _passiveSlots = new();
    private readonly List<ActivePassiveSouvenir> _badgeSlots = new();
    // Papier carbone : projectiles en plus, pleins, en fraction (2,5 : deux, et une chance sur deux d'un troisième).
    private float _bonusProjectiles;
    private float _statusDurationMultiplier = 1f;
    // Valeurs des effets d'un objet avant une carte ; le chargeur refuse un objet à plus d'effets que de cases.
    private readonly float[] _passiveValuesBefore = new float[PassiveSouvenirDataLoader.MaxEffects];
    private ObjectMilestones _objectMilestones;
    private ObjectTriggers _objectTriggers;
    private ObjectCombatEffects _objectCombatEffects;
    private ObjectStances _objectStances;
    private ObjectProcs _objectProcs;
    private ObjectUtilities _objectUtilities;
    private BadgeUsage _badgeUsage;
    private float _objectPerilFraction;

    /// <summary>Objets à effet portés, un par fiche ; leurs exemplaires sont dans <see cref="ActivePassiveSouvenir.Copies"/>.</summary>
    public IReadOnlyList<ActivePassiveSouvenir> PassiveSlots => _passiveSlots;

    /// <summary>Places du sac (plan 35) : inventory.json, puis les Poches du Campement.</summary>
    public int BagCapacity { get; private set; } = InventoryConfig.BagSize;

    /// <summary>Places occupées : une par type distinct, indépendamment de ses exemplaires.</summary>
    public int BagCount => _passiveSlots.Count;

    /// <summary>Nombre total d'exemplaires portés, distinct de la capacité du sac.</summary>
    public int BagCopyCount
    {
        get
        {
            int count = 0;
            foreach (ActivePassiveSouvenir item in _passiveSlots)
                count += item.Copies;
            return count;
        }
    }

    public bool HasBagRoom => BagCount < BagCapacity;

    /// <summary>Fixe les places du sac de la run, jamais sous les types déjà portés.</summary>
    public void SetBagCapacity(int capacity)
    {
        BagCapacity = Mathf.Max(Mathf.Max(1, capacity), BagCount);
        _eventBus?.EmitSignal(EventBus.SignalName.PassiveSouvenirSlotsChanged);
    }

    /// <summary>Écussons portés (plan 35), au plus <see cref="InventoryConfig.BadgeSlots"/>.</summary>
    public IReadOnlyList<ActivePassiveSouvenir> BadgeSlots => _badgeSlots;
    /// <summary>Projectiles, ou frappes de mêlée, en plus à chaque attaque (Papier carbone), en fraction.</summary>
    public float BonusProjectiles => _bonusProjectiles;
    /// <summary>Durée des statuts infligés et des zones au sol (Pince à linge).</summary>
    public float StatusDurationMultiplier => _statusDurationMultiplier;
    /// <summary>Paliers d'objets atteints ; absent tant qu'aucun ne l'est.</summary>
    public ObjectMilestones ObjectMilestones => _objectMilestones;
    /// <summary>Objets de déclencheur portés ; absent tant qu'aucun ne l'est.</summary>
    public ObjectTriggers ObjectTriggers => _objectTriggers;
    public ObjectCombatEffects ObjectCombatEffects => _objectCombatEffects;
    /// <summary>Objets d'état portés (Tabouret, Gilet, Thermos, Médaille, Porte-monnaie) ; absent tant qu'aucun ne l'est.</summary>
    public ObjectStances ObjectStances => _objectStances;
    /// <summary>Icônes des objets qui agissent (plan 21 F4) ; créé avec le premier objet.</summary>
    public ObjectProcs ObjectProcs => _objectProcs;

    /// <summary>Fils d'usage des Écussons portés (plan 35 I7) ; absent tant qu'aucun Écusson n'est cousu.</summary>
    public BadgeUsage BadgeUsage => _badgeUsage;

    /// <summary>Cartes de plus à chaque level-up (Trèfle plastifié).</summary>
    public int ExtraLevelUpCards => _objectUtilities?.ExtraCards ?? 0;

    /// <summary>Repères : types de lieux déjà utilisés dans la run, et la Chance qu'ils ont donnée.</summary>
    public Waymarks Waymarks { get; private set; }

    /// <summary>Texte flottant au-dessus du joueur, sans « + » (Repères).</summary>
    internal void ShowPopup(string text, Color color) => SpawnLootPopup(text, color, GlobalPosition, 0, plusSign: false);

    /// <summary>
    /// Ajoute un Écusson au niveau 1, ou le monte de <paramref name="cards"/> cartes de gain <paramref name="gain"/>
    /// chacune (1 pour une commune, 3 pour une légendaire ; plan 23 R3). Pour un objet, ajoute des copies entières,
    /// sans gain de rareté ni niveau caché. Faux si rien n’a changé.
    /// </summary>
    public bool AddOrUpgradePassive(string passiveId, int cards = 1, float gain = 1f)
    {
        PassiveSouvenirData data = PassiveSouvenirDataLoader.Get(passiveId);
        if (data == null || cards < 1)
            return false;
        if (!data.IsBadge)
        {
            bool changed = false;
            for (int copy = 0; copy < cards && AddItemCopy(passiveId); copy++)
                changed = true;
            return changed;
        }
        if (_objectProcs == null)
        {
            _objectProcs = new ObjectProcs { Name = "ObjectProcs" };
            AddChild(_objectProcs);
        }

        List<ActivePassiveSouvenir> shelf = data.IsBadge ? _badgeSlots : _passiveSlots;
        foreach (ActivePassiveSouvenir existing in shelf)
        {
            if (existing.Id == passiveId)
            {
                int previousLevel = existing.Level;
                for (int card = 0; card < cards && !existing.IsMaxLevel; card++)
                {
                    for (int i = 0; i < data.Effects.Count; i++)
                        _passiveValuesBefore[i] = existing.Value(i);
                    existing.Upgrade(gain);
                    ApplyPassiveEffects(existing, _passiveValuesBefore);
                }
                if (existing.Level == previousLevel)
                    return false;
                ReachMilestones(existing, previousLevel);

                _eventBus?.EmitSignal(EventBus.SignalName.PassiveSouvenirUpgraded, passiveId, existing.Level);
                _eventBus?.EmitSignal(EventBus.SignalName.PassiveSouvenirSlotsChanged);

                GD.Print($"[Player] Passive upgraded: {data.Name} → level {existing.Level}/{data.MaxLevel}");
                return true;
            }
        }

        if (data.IsBadge ? shelf.Count >= InventoryConfig.BadgeSlots : !HasBagRoom)
            return false;

        ActivePassiveSouvenir passive = new(data) { Copies = data.IsBadge ? 0 : 1 };
        shelf.Add(passive);
        if (HasCombatEffect(data))
            EnsureObjectCombatEffects().Configure(data);
        if (HasTriggerEffect(data))
            EnsureObjectTriggers().Configure(data);
        if (HasStanceEffect(data))
            EnsureObjectStances().Configure(data);
        if (HasUtilityEffect(data))
            EnsureObjectUtilities().Configure(data);
        if (data.IsBadge)
            EnsureBadgeUsage().Track(data);

        for (int i = 0; i < data.Effects.Count; i++)
            _passiveValuesBefore[i] = data.Effects[i].Neutral;
        ApplyPassiveEffects(passive, _passiveValuesBefore);

        int slotIndex = shelf.Count - 1;
        _eventBus?.EmitSignal(EventBus.SignalName.PassiveSouvenirAdded, passiveId, slotIndex);
        _eventBus?.EmitSignal(EventBus.SignalName.PassiveSouvenirSlotsChanged);

        GD.Print($"[Player] Passive added [{slotIndex}]: {data.Name} (level 1/{data.MaxLevel})");
        if (cards > 1)
            AddOrUpgradePassive(passiveId, cards - 1, gain);
        return true;
    }

    /// <summary>Une pile existante reçoit ses doublons même à sac plein ; un nouveau type exige une place.</summary>
    public bool CanTakeItem(string id)
    {
        PassiveSouvenirData data = PassiveSouvenirDataLoader.Get(id);
        if (data == null || data.IsBadge)
            return false;
        ActivePassiveSouvenir owned = FindObject(id);
        return (owned != null || HasBagRoom) && !data.IsCopyLimitReached(owned?.Copies ?? 0);
    }

    /// <summary>Objet déjà à son maximum d'exemplaires : aucun échange ne peut le faire entrer.</summary>
    public bool IsItemAtMax(string id)
    {
        ActivePassiveSouvenir owned = FindObject(id);
        return owned != null && !owned.Data.IsBadge && owned.Data.IsCopyLimitReached(owned.Copies);
    }

    /// <summary>Ajoute une copie et son effet linéaire, sans niveau intermédiaire ni palier.</summary>
    public bool AddItemCopy(string id)
    {
        if (!CanTakeItem(id))
            return false;
        PassiveSouvenirData data = PassiveSouvenirDataLoader.Get(id);
        ActivePassiveSouvenir owned = FindObject(id);
        bool added = owned == null;
        if (added)
        {
            owned = new ActivePassiveSouvenir(data);
            owned.SetItemCopies(0);
            _passiveSlots.Add(owned);
            if (_objectProcs == null)
            {
                _objectProcs = new ObjectProcs { Name = "ObjectProcs" };
                AddChild(_objectProcs);
            }
            if (HasCombatEffect(data))
                EnsureObjectCombatEffects().Configure(data);
            if (HasTriggerEffect(data))
                EnsureObjectTriggers().Configure(data);
            if (HasStanceEffect(data))
                EnsureObjectStances().Configure(data);
            if (HasUtilityEffect(data))
                EnsureObjectUtilities().Configure(data);
        }
        for (int i = 0; i < data.Effects.Count; i++)
            _passiveValuesBefore[i] = owned.Value(i);
        owned.SetItemCopies(owned.Copies + 1);
        ApplyPassiveEffects(owned, _passiveValuesBefore);
        if (added)
            _eventBus?.EmitSignal(EventBus.SignalName.PassiveSouvenirAdded, id, _passiveSlots.Count - 1);
        else
            _eventBus?.EmitSignal(EventBus.SignalName.PassiveSouvenirUpgraded, id, owned.Copies);
        _eventBus?.EmitSignal(EventBus.SignalName.PassiveSouvenirSlotsChanged);
        return true;
    }

    /// <summary>
    /// Oublie un exemplaire et son apport exact ; la dernière copie retire l’objet du sac.
    /// </summary>
    public bool ForgetItemCopy(string id) => RemoveItemCopies(id, 1);

    /// <summary>Oublie une pile entière pour libérer son emplacement, avec un seul recalcul des effets.</summary>
    public bool ForgetItemStack(string id)
    {
        ActivePassiveSouvenir owned = FindObject(id);
        return owned != null && !owned.Data.IsBadge && RemoveItemCopies(id, owned.Copies);
    }

    private bool RemoveItemCopies(string id, int copies)
    {
        ActivePassiveSouvenir owned = FindObject(id);
        if (owned == null || owned.Data.IsBadge)
            return false;
        for (int i = 0; i < owned.Data.Effects.Count; i++)
            _passiveValuesBefore[i] = owned.Value(i);
        owned.SetItemCopies(Mathf.Max(0, owned.Copies - copies));
        ApplyPassiveEffects(owned, _passiveValuesBefore);
        if (owned.Copies <= 0)
            _passiveSlots.Remove(owned);
        _eventBus?.EmitSignal(EventBus.SignalName.PassiveSouvenirSlotsChanged);
        GD.Print($"[Player] Item forgotten: {owned.Data.Name} ({owned.Copies} left)");
        return true;
    }

    /// <summary>Vérifie si un objet ou un Écusson donné est au max.</summary>
    public bool IsPassiveMaxLevel(string passiveId) => FindObject(passiveId)?.IsMaxLevel ?? false;

    /// <summary>Retourne le niveau actuel d'un objet ou d'un Écusson (0 s'il n'est pas porté).</summary>
    public int GetPassiveLevel(string passiveId) => FindObject(passiveId)?.Level ?? 0;

    /// <summary>Objet ou Écusson porté, ou null.</summary>
    public ActivePassiveSouvenir FindObject(string id)
    {
        foreach (ActivePassiveSouvenir badge in _badgeSlots)
            if (badge.Id == id)
                return badge;
        foreach (ActivePassiveSouvenir item in _passiveSlots)
            if (item.Id == id)
                return item;
        return null;
    }

    /// <summary>Fait passer chaque effet de l'objet de sa valeur d'avant la carte (<paramref name="before"/>) à sa valeur actuelle.</summary>
    private void ApplyPassiveEffects(ActivePassiveSouvenir passive, float[] valuesBefore)
    {
        for (int i = 0; i < passive.Data.Effects.Count; i++)
        {
            PassiveEffectData effect = passive.Data.Effects[i];
            float before = valuesBefore[i];
            float after = passive.Value(i);
            // Multiplicatif : on retire l'ancien facteur en appliquant le rapport ; additif : seulement l'écart.
            float change = effect.Multiplicative ? after / before : after - before;
            if (ObjectCombatEffects.IsCombatStat(effect.Stat))
                EnsureObjectCombatEffects().Add(effect.Stat, change);
            else if (ObjectTriggers.IsTriggerStat(effect.Stat))
                EnsureObjectTriggers().Add(effect.Stat, change);
            else if (ObjectStances.IsStanceStat(effect.Stat))
                EnsureObjectStances().Add(effect.Stat, change);
            else if (ObjectUtilities.IsUtilityStat(effect.Stat))
                EnsureObjectUtilities().Add(effect.Stat, change);
            else if (passive.Data.IsBadge)
                ApplyPerkModifier(effect.Stat, change, effect.ModifierType);
            else
                ApplyItemModifier(effect.Stat, change, effect.ModifierType);
        }
    }

    /// <summary>Active les paliers codés que l'amélioration vient de franchir ; une cascade peut en franchir plusieurs.</summary>
    private void ReachMilestones(ActivePassiveSouvenir passive, int fromLevel)
    {
        if (!passive.Data.IsBadge)
            return;
        int reached = 0;
        foreach (ObjectMilestoneData milestone in passive.Data.Milestones)
        {
            if (milestone.Level <= fromLevel || !passive.Reached(milestone) || !ObjectMilestoneEffects.IsImplemented(milestone.Effect))
                continue;
            if (_objectMilestones == null)
            {
                _objectMilestones = new ObjectMilestones { Name = "ObjectMilestones" };
                _objectMilestones.Initialize(this);
                AddChild(_objectMilestones);
            }
            _objectMilestones.Activate(milestone);
            SpawnLootPopup(string.Format(Tr("OBJECT_MILESTONE_REACHED"), passive.Data.Name, milestone.Level), MilestoneColor, GlobalPosition, reached++);
            GD.Print($"[Player] Milestone reached: {passive.Data.Name} level {milestone.Level} ({milestone.Effect})");
        }
    }

    /// <summary>
    /// Sifflet d'arbitre (plan 28 P3) : chaque niveau monte le Péril de la run. Une carte rare en donne une fraction de
    /// plus ; les fractions se cumulent et seuls les points entiers passent au Péril.
    /// </summary>
    private void AddObjectPeril(float points)
    {
        // Le Péril pris ne se rend pas : oublier un Sifflet ne retire rien, et ne doit pas non plus manger les suivants.
        if (points <= 0f)
            return;
        _objectPerilFraction += points;
        int whole = Mathf.FloorToInt(_objectPerilFraction + 0.0001f);
        if (whole <= 0)
            return;
        _objectPerilFraction -= whole;
        PerilManager.Current?.AddPeril(whole);
    }

    /// <summary>Durée d'un statut ou d'une zone au sol posés par le joueur, Pince à linge comprise.</summary>
    private float StatusDuration(float seconds) => seconds * _statusDurationMultiplier;

    internal PlayerAttackFx AttackFx => _attackFx;

    /// <summary>Invulnérabilité brève accordée par un objet (Boîte de pansements, palier 25).</summary>
    internal void GrantInvulnerability(float seconds) => _defense.GrantInvulnerability(seconds);

    private static bool HasTriggerEffect(PassiveSouvenirData data)
    {
        foreach (PassiveEffectData effect in data.Effects)
            if (ObjectTriggers.IsTriggerStat(effect.Stat))
                return true;
        return false;
    }

    private static bool HasStanceEffect(PassiveSouvenirData data)
    {
        foreach (PassiveEffectData effect in data.Effects)
            if (ObjectStances.IsStanceStat(effect.Stat))
                return true;
        return false;
    }

    private static bool HasUtilityEffect(PassiveSouvenirData data)
    {
        foreach (PassiveEffectData effect in data.Effects)
            if (ObjectUtilities.IsUtilityStat(effect.Stat))
                return true;
        return false;
    }

#if TOOLS
    /// <summary>Mesure au bot (plan 35 I7) : le module des fils existe dès le départ et compte tous les déclencheurs.</summary>
    internal BadgeUsage MeasureBadgeUsage()
    {
        BadgeUsage usage = EnsureBadgeUsage();
        usage.MeasureAll();
        return usage;
    }
#endif

    private BadgeUsage EnsureBadgeUsage()
    {
        if (_badgeUsage != null)
            return _badgeUsage;
        _badgeUsage = new BadgeUsage { Name = "BadgeUsage" };
        _badgeUsage.Initialize(this);
        AddChild(_badgeUsage);
        return _badgeUsage;
    }

    private ObjectUtilities EnsureObjectUtilities() => _objectUtilities ??= new ObjectUtilities(this);

    /// <summary>
    /// Ticket de consigne (plan 35 I6) : un coup mortel consume un exemplaire, et le joueur se relève avec une part de
    /// ses PV et un instant d'invulnérabilité. Faux sans ticket. Le Néant ne s'arrête pas au ticket : il n'est pas appelé
    /// pour l'Effacement.
    /// </summary>
    private bool TryConsumeRevive()
    {
        if (_objectUtilities is not { CanRevive: true } utilities)
            return false;
        float ratio = utilities.ReviveHpRatio;
        float invulnerability = utilities.ReviveInvulnerabilitySeconds;
        string ticket = utilities.ReviveItemId;
        if (!ForgetItemCopy(ticket))
            return false;
        _currentHp = Mathf.Max(1f, EffectiveMaxHp * ratio);
        _defense.GrantInvulnerability(invulnerability);
        Flash(MilestoneColor, true, 0.35f);
        SpawnLootPopup(string.Format(Tr("ITEM_REVIVE_POPUP"), PassiveSouvenirDataLoader.Get(ticket)?.Name), MilestoneColor,
            GlobalPosition, 0, plusSign: false);
        AudioManager.Play("sfx_artefact_trouve", 0f, -2f);
        GD.Print($"[Player] Revived by {ticket}");
        return true;
    }

    private ObjectStances EnsureObjectStances()
    {
        if (_objectStances != null)
            return _objectStances;
        _objectStances = new ObjectStances { Name = "ObjectStances" };
        _objectStances.Initialize(this);
        AddChild(_objectStances);
        return _objectStances;
    }

    private static bool HasCombatEffect(PassiveSouvenirData data)
    {
        foreach (PassiveEffectData effect in data.Effects)
            if (ObjectCombatEffects.IsCombatStat(effect.Stat)) return true;
        return false;
    }

    private ObjectCombatEffects EnsureObjectCombatEffects()
    {
        if (_objectCombatEffects != null) return _objectCombatEffects;
        _objectCombatEffects = new ObjectCombatEffects { Name = "ObjectCombatEffects" };
        _objectCombatEffects.Initialize(this);
        AddChild(_objectCombatEffects);
        return _objectCombatEffects;
    }

    private ObjectTriggers EnsureObjectTriggers()
    {
        if (_objectTriggers != null)
            return _objectTriggers;
        _objectTriggers = new ObjectTriggers { Name = "ObjectTriggers" };
        _objectTriggers.Initialize(this);
        AddChild(_objectTriggers);
        return _objectTriggers;
    }

    /// <summary>Ressort de sommier : l'arme repart, si elle est toujours portée ; cette attaque ne se compte pas.</summary>
    internal void RepeatAttack(WeaponInstance weapon)
    {
        if (_isDead || !_weaponSlots.Contains(weapon))
            return;
        _equippedWeapon = weapon;
        PerformDiscreteAttack(weapon.Category, weapon.AttackPattern);
    }

    /// <summary>
    /// Dégâts d'un coup d'arme au moment de toucher sa cible : critique sur PV pleins (Lunettes de lecture, palier 15),
    /// cible brûlée ou ralentie (Thermomètre, Épingle à nourrice), états du joueur (Gilet, Thermos, Médaille, Porte-monnaie).
    /// </summary>
    internal float ResolveHitDamage(Enemy enemy, float damage, bool isCrit)
    {
        float resolved = _objectMilestones?.CritAgainst(enemy, damage, isCrit) ?? damage;
        resolved = _objectTriggers?.AgainstTarget(enemy, resolved) ?? resolved;
        return resolved * (_objectStances?.DamageMultiplier ?? 1f);
    }

    /// <summary>
    /// Rondelle de cuivre : chaque frappe de mêlée refrappera dans sa propre direction ; un cercle complet refrappe
    /// d'un seul tenant, avec toutes ses frappes.
    /// </summary>
    private void QueueMeleeEchoes(Vector2 direction, float range, float arcAngle, float squash, int strikeCount, float startOffset,
        float step, float strikeDamage, AttackContext context)
    {
        if (arcAngle >= 359f)
        {
            _objectMilestones.QueueArcEcho(direction, range, 360f, strikeDamage * strikeCount, _equippedWeapon, context);
            return;
        }
        for (int strike = 0; strike < strikeCount; strike++)
            _objectMilestones.QueueArcEcho(MeleeStrikeShape.RotateOnSlash(direction, startOffset + step * strike, squash), range, arcAngle,
                strikeDamage, _equippedWeapon, context);
    }

    /// <summary>
    /// Projectiles ou frappes en plus du Papier carbone pour cette attaque, fraction tirée : la voie d'ascension de
    /// l'arme peut les doubler (Volée) ou les refuser (Transpercer).
    /// </summary>
    private int RollBonusProjectiles(WeaponInstance weapon) =>
        FractionalCount.Roll(_bonusProjectiles * (weapon?.BonusProjectileMultiplier ?? 1f), RunRandom.Combat.Randf());

    /// <summary>Papier carbone, palier 15 : les projectiles en plus visent chacun leur propre cible.</summary>
    private bool ExtraProjectilesSpread => _objectMilestones?.SpreadsExtraProjectiles == true;

    /// <summary>Reflet brisé, palier 15 : gain de dégâts d'un projectile par ennemi traversé (0 sans le palier).</summary>
    private float PierceDamageRamp => _objectMilestones?.PierceDamageRamp ?? 0f;

    /// <summary>
    /// Un projectile d'arme arrive en bout de course sans avoir été arrêté : éclat du Mètre pliant (palier 25), sinon
    /// un petit éclat à sa couleur (plan 27 V2c), pour qu'il ne disparaisse pas d'un coup.
    /// </summary>
    internal void OnProjectileSpent(Vector2 position, float damage, AttackContext context, FxFamily family, Vector2 direction)
    {
        if (!_isDead && _objectMilestones?.HasRangeEndBurst == true)
            _objectMilestones.BurstAtRangeEnd(position, damage, context);
        else
            _attackFx.PlayFizzle(position, family, direction);
    }

    /// <summary>Orbe d'XP ramassée par ce joueur (Aimant de frigo, palier 25).</summary>
    internal void OnXpOrbCollected() => _objectMilestones?.OnOrbCollected();

    /// <summary>Coup ignoré par le Bouton de manteau : éclair gris bref, au plus quatre fois par seconde dans une foule.</summary>
    private void ShowIgnoredHit()
    {
        ulong now = Time.GetTicksMsec();
        if (now - _lastIgnoredHitFlashMsec < IgnoredHitFlashIntervalMsec)
            return;
        _lastIgnoredHitFlashMsec = now;
        Flash(IgnoredHitColor, false);
        _defenseFeedback.Ignored(GlobalPosition);
    }
}
