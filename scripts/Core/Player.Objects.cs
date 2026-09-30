using System.Collections.Generic;
using Godot;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.Core;

/// <summary>Objets du joueur (plan 21 §4) : six emplacements, cinquante niveaux, des effets par formule et des paliers.</summary>
public partial class Player
{
    public const int MaxPassiveSlots = 6;
    private static readonly Color MilestoneColor = new(1f, 0.82f, 0.35f);

    private readonly List<ActivePassiveSouvenir> _passiveSlots = new();
    // Papier carbone : copies d'attaque à dégâts réduits, à part des projectiles pleins des anciens Dons.
    private int _attackCopies;
    private float _copyDamageFactor;
    private float _statusDurationMultiplier = 1f;
    private ObjectMilestones _objectMilestones;

    public IReadOnlyList<ActivePassiveSouvenir> PassiveSlots => _passiveSlots;
    public int AttackCopies => _attackCopies;
    /// <summary>Part des dégâts d'une attaque que porte chacune de ses copies.</summary>
    public float CopyDamageFactor => _copyDamageFactor;
    /// <summary>Durée des statuts infligés et des zones au sol (Pince à linge).</summary>
    public float StatusDurationMultiplier => _statusDurationMultiplier;
    /// <summary>Paliers d'objets atteints ; absent tant qu'aucun ne l'est.</summary>
    public ObjectMilestones ObjectMilestones => _objectMilestones;

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

    /// <summary>Durée d'un statut ou d'une zone au sol posés par le joueur, Pince à linge comprise.</summary>
    private float StatusDuration(float seconds) => seconds * _statusDurationMultiplier;
}
