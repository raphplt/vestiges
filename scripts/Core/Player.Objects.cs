using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.Core;

/// <summary>Objets du joueur (plan 21 §4) : six emplacements, cinquante niveaux, des effets par formule et des paliers.</summary>
public partial class Player
{
    public const int MaxPassiveSlots = 6;
    private static readonly Color MilestoneColor = new(1f, 0.82f, 0.35f);
    private static readonly Color IgnoredHitColor = new(0.72f, 0.72f, 0.7f);
    private const ulong IgnoredHitFlashIntervalMsec = 250;
    private ulong _lastIgnoredHitFlashMsec;

    private readonly List<ActivePassiveSouvenir> _passiveSlots = new();
    // Papier carbone : copies d'attaque à dégâts réduits, à part des projectiles pleins des anciens Dons.
    private int _attackCopies;
    private float _copyDamageFactor;
    private float _statusDurationMultiplier = 1f;
    private ObjectMilestones _objectMilestones;
    private ObjectTriggers _objectTriggers;

    public IReadOnlyList<ActivePassiveSouvenir> PassiveSlots => _passiveSlots;
    public int AttackCopies => _attackCopies;
    /// <summary>Part des dégâts d'une attaque que porte chacune de ses copies.</summary>
    public float CopyDamageFactor => _copyDamageFactor;
    /// <summary>Durée des statuts infligés et des zones au sol (Pince à linge).</summary>
    public float StatusDurationMultiplier => _statusDurationMultiplier;
    /// <summary>Paliers d'objets atteints ; absent tant qu'aucun ne l'est.</summary>
    public ObjectMilestones ObjectMilestones => _objectMilestones;
    /// <summary>Objets de déclencheur portés ; absent tant qu'aucun ne l'est.</summary>
    public ObjectTriggers ObjectTriggers => _objectTriggers;

    /// <summary>Ajoute un objet au niveau 1, ou monte un objet possédé de <paramref name="levels"/> niveaux (plan 21 §4).</summary>
    public bool AddOrUpgradePassive(string passiveId, int levels = 1)
    {
        PassiveSouvenirData data = PassiveSouvenirDataLoader.Get(passiveId);
        if (data == null)
            return false;

        foreach (ActivePassiveSouvenir existing in _passiveSlots)
        {
            if (existing.Id == passiveId)
            {
                int previousLevel = existing.Level;
                if (!existing.Upgrade(levels))
                    return false;

                ApplyPassiveEffects(data, previousLevel, existing.Level);
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

        ApplyPassiveEffects(data, 0, passive.Level);

        int slotIndex = _passiveSlots.Count - 1;
        _eventBus?.EmitSignal(EventBus.SignalName.PassiveSouvenirAdded, passiveId, slotIndex);
        _eventBus?.EmitSignal(EventBus.SignalName.PassiveSouvenirSlotsChanged);

        GD.Print($"[Player] Passive added [{slotIndex}]: {data.Name} (level 1/{data.MaxLevel})");
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

    /// <summary>
    /// Fait passer chaque effet de l'objet de sa valeur au niveau <paramref name="fromLevel"/> à celle du niveau
    /// <paramref name="toLevel"/> ; niveau 0 : l'objet n'était pas encore possédé.
    /// </summary>
    private void ApplyPassiveEffects(PassiveSouvenirData data, int fromLevel, int toLevel)
    {
        foreach (PassiveEffectData effect in data.Effects)
        {
            float before = effect.ValueAt(fromLevel);
            float after = effect.ValueAt(toLevel);
            // Multiplicatif : on retire l'ancien facteur en appliquant le rapport ; additif : seulement l'écart.
            float change = effect.Multiplicative ? after / before : after - before;
            if (ObjectTriggers.IsTriggerStat(effect.Stat))
                EnsureObjectTriggers().Add(effect.Stat, change);
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
    /// Dégâts d'un coup d'arme au moment de toucher sa cible : critique sur PV pleins (Lunettes de lecture, palier 25),
    /// cible brûlée ou ralentie (Thermomètre, Épingle à nourrice).
    /// </summary>
    internal float ResolveHitDamage(Enemy enemy, float damage, bool isCrit)
    {
        float resolved = _objectMilestones?.CritAgainst(enemy, damage, isCrit) ?? damage;
        return _objectTriggers?.AgainstTarget(enemy, resolved) ?? resolved;
    }

    /// <summary>
    /// Rondelle de cuivre : chaque frappe de mêlée refrappera dans sa propre direction, avec sa part des dégâts
    /// (copies comprises) ; un cercle complet refrappe d'un seul tenant.
    /// </summary>
    private void QueueMeleeEchoes(Vector2 direction, float range, float arcAngle, int strikeCount, float startOffset,
        float step, float strikeDamage, AttackContext context)
    {
        if (arcAngle >= 359f)
        {
            _objectMilestones.QueueArcEcho(direction, range, 360f, strikeDamage * StrikeMultiplierSum(0, strikeCount - 1), _equippedWeapon, context);
            return;
        }
        for (int strike = 0; strike < strikeCount; strike++)
            _objectMilestones.QueueArcEcho(direction.Rotated(Mathf.DegToRad(startOffset + step * strike)), range, arcAngle,
                strikeDamage * StrikeMultiplierSum(strike, strike), _equippedWeapon, context);
    }

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
