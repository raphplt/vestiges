using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.Core;

/// <summary>Objets du joueur (plan 21 §4, plan 23 R3) : six emplacements, trente niveaux, des gains selon la rareté et des paliers.</summary>
public partial class Player
{
    public const int MaxPassiveSlots = 6;
    private static readonly Color MilestoneColor = new(1f, 0.82f, 0.35f);
    private static readonly Color IgnoredHitColor = new(0.72f, 0.72f, 0.7f);
    private const ulong IgnoredHitFlashIntervalMsec = 250;
    private ulong _lastIgnoredHitFlashMsec;

    private readonly List<ActivePassiveSouvenir> _passiveSlots = new();
    // Papier carbone : projectiles en plus, pleins, en fraction (2,5 : deux, et une chance sur deux d'un troisième).
    private float _bonusProjectiles;
    private float _statusDurationMultiplier = 1f;
    // Valeurs des effets d'un objet avant une carte ; le chargeur refuse un objet à plus d'effets que de cases.
    private readonly float[] _passiveValuesBefore = new float[PassiveSouvenirDataLoader.MaxEffects];
    private ObjectMilestones _objectMilestones;
    private ObjectTriggers _objectTriggers;
    private ObjectStances _objectStances;

    public IReadOnlyList<ActivePassiveSouvenir> PassiveSlots => _passiveSlots;
    /// <summary>Projectiles, ou frappes de mêlée, en plus à chaque attaque (Papier carbone), en fraction.</summary>
    public float BonusProjectiles => _bonusProjectiles;
    /// <summary>Durée des statuts infligés et des zones au sol (Pince à linge).</summary>
    public float StatusDurationMultiplier => _statusDurationMultiplier;
    /// <summary>Paliers d'objets atteints ; absent tant qu'aucun ne l'est.</summary>
    public ObjectMilestones ObjectMilestones => _objectMilestones;
    /// <summary>Objets de déclencheur portés ; absent tant qu'aucun ne l'est.</summary>
    public ObjectTriggers ObjectTriggers => _objectTriggers;
    /// <summary>Objets d'état portés (Tabouret, Gilet, Thermos, Médaille, Porte-monnaie) ; absent tant qu'aucun ne l'est.</summary>
    public ObjectStances ObjectStances => _objectStances;
    /// <summary>Repères : types de lieux déjà utilisés dans la run, et la Chance qu'ils ont donnée.</summary>
    public Waymarks Waymarks { get; private set; }

    /// <summary>Texte flottant au-dessus du joueur (paliers, Repères).</summary>
    internal void ShowPopup(string text, Color color) => SpawnLootPopup(text, color, GlobalPosition, 0);

    /// <summary>
    /// Ajoute un objet au niveau 1, ou le monte de <paramref name="cards"/> cartes de gain <paramref name="gain"/>
    /// chacune (1 pour une commune, 3 pour une légendaire ; plan 23 R3). Faux si rien n'a changé.
    /// </summary>
    public bool AddOrUpgradePassive(string passiveId, int cards = 1, float gain = 1f)
    {
        PassiveSouvenirData data = PassiveSouvenirDataLoader.Get(passiveId);
        if (data == null)
            return false;

        foreach (ActivePassiveSouvenir existing in _passiveSlots)
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

        if (_passiveSlots.Count >= MaxPassiveSlots)
            return false;

        ActivePassiveSouvenir passive = new(data);
        _passiveSlots.Add(passive);
        if (HasTriggerEffect(data))
            EnsureObjectTriggers().Configure(data);
        if (HasStanceEffect(data))
            EnsureObjectStances().Configure(data);

        for (int i = 0; i < data.Effects.Count; i++)
            _passiveValuesBefore[i] = data.Effects[i].Neutral;
        ApplyPassiveEffects(passive, _passiveValuesBefore);

        int slotIndex = _passiveSlots.Count - 1;
        _eventBus?.EmitSignal(EventBus.SignalName.PassiveSouvenirAdded, passiveId, slotIndex);
        _eventBus?.EmitSignal(EventBus.SignalName.PassiveSouvenirSlotsChanged);

        GD.Print($"[Player] Passive added [{slotIndex}]: {data.Name} (level 1/{data.MaxLevel})");
        if (cards > 1)
            AddOrUpgradePassive(passiveId, cards - 1, gain);
        return true;
    }

    /// <summary>Vérifie si un passif donné est au max.</summary>
    public bool IsPassiveMaxLevel(string passiveId)
    {
        foreach (ActivePassiveSouvenir p in _passiveSlots)
        {
            if (p.Id == passiveId)
                return p.IsMaxLevel;
        }
        return false;
    }

    /// <summary>Retourne le niveau actuel d'un passif (0 si pas équipé).</summary>
    public int GetPassiveLevel(string passiveId)
    {
        foreach (ActivePassiveSouvenir p in _passiveSlots)
        {
            if (p.Id == passiveId)
                return p.Level;
        }
        return 0;
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
            if (ObjectTriggers.IsTriggerStat(effect.Stat))
                EnsureObjectTriggers().Add(effect.Stat, change);
            else if (ObjectStances.IsStanceStat(effect.Stat))
                EnsureObjectStances().Add(effect.Stat, change);
            else
                ApplyPerkModifier(effect.Stat, change, effect.ModifierType);
        }
    }

    /// <summary>Active les paliers codés que l'amélioration vient de franchir ; une cascade peut en franchir plusieurs.</summary>
    private void ReachMilestones(ActivePassiveSouvenir passive, int fromLevel)
    {
        int reached = 0;
        foreach (ObjectMilestoneData milestone in passive.Data.Milestones)
        {
            if (milestone.Level <= fromLevel || !passive.Reached(milestone) || !ObjectMilestoneEffects.IsImplemented(milestone.Effect))
                continue;
            if (ObjectTriggers.IsTriggerMilestone(milestone.Effect))
                EnsureObjectTriggers().Activate(milestone);
            else if (ObjectStances.IsStanceMilestone(milestone.Effect))
                EnsureObjectStances().Activate(milestone);
            else
            {
                if (_objectMilestones == null)
                {
                    _objectMilestones = new ObjectMilestones { Name = "ObjectMilestones" };
                    _objectMilestones.Initialize(this);
                    AddChild(_objectMilestones);
                }
                _objectMilestones.Activate(milestone);
            }
            SpawnLootPopup(string.Format(Tr("OBJECT_MILESTONE_REACHED"), passive.Data.Name, milestone.Level), MilestoneColor, GlobalPosition, reached++);
            GD.Print($"[Player] Milestone reached: {passive.Data.Name} level {milestone.Level} ({milestone.Effect})");
        }
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

    private ObjectStances EnsureObjectStances()
    {
        if (_objectStances != null)
            return _objectStances;
        _objectStances = new ObjectStances { Name = "ObjectStances" };
        _objectStances.Initialize(this);
        AddChild(_objectStances);
        return _objectStances;
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
        PerformDiscreteAttack(weapon.Type?.ToLower() ?? "ranged", weapon.AttackPattern?.ToLower() ?? "linear");
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
    private void QueueMeleeEchoes(Vector2 direction, float range, float arcAngle, int strikeCount, float startOffset,
        float step, float strikeDamage, AttackContext context)
    {
        if (arcAngle >= 359f)
        {
            _objectMilestones.QueueArcEcho(direction, range, 360f, strikeDamage * strikeCount, _equippedWeapon, context);
            return;
        }
        for (int strike = 0; strike < strikeCount; strike++)
            _objectMilestones.QueueArcEcho(direction.Rotated(Mathf.DegToRad(startOffset + step * strike)), range, arcAngle,
                strikeDamage, _equippedWeapon, context);
    }

    /// <summary>
    /// Projectiles ou frappes en plus du Papier carbone pour cette attaque, fraction tirée : la voie d'ascension de
    /// l'arme peut les doubler (Volée) ou les refuser (Transpercer).
    /// </summary>
    private int RollBonusProjectiles(WeaponInstance weapon) =>
        FractionalCount.Roll(_bonusProjectiles * (weapon?.BonusProjectileMultiplier ?? 1f), GD.Randf());

    /// <summary>Papier carbone, palier 15 : les projectiles en plus visent chacun leur propre cible.</summary>
    private bool ExtraProjectilesSpread => _objectMilestones?.SpreadsExtraProjectiles == true;

    /// <summary>Reflet brisé, palier 15 : gain de dégâts d'un projectile par ennemi traversé (0 sans le palier).</summary>
    private float PierceDamageRamp => _objectMilestones?.PierceDamageRamp ?? 0f;

    /// <summary>Un projectile d'arme arrive en bout de course sans avoir été arrêté (Mètre pliant, palier 25).</summary>
    internal void OnProjectileSpent(Vector2 position, float damage, AttackContext context)
    {
        if (!_isDead && _objectMilestones?.HasRangeEndBurst == true)
            _objectMilestones.BurstAtRangeEnd(position, damage, context);
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
    }
}
