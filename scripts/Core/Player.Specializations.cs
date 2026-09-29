using System;
using System.Collections.Generic;
using Godot;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.Core;

public partial class Player
{
    private readonly List<PerkSpecializationData> _specializations = new();
    private SpecializationRuntime _specializationRuntime;

    /// <summary>État des effets de perks ; absent tant qu'aucun perk n'est acquis.</summary>
    public SpecializationRuntime SpecializationRuntime => _specializationRuntime;

    /// <summary>Perks de spécialisation de la run, dans l'ordre d'acquisition : uniques, sans niveau ni remplacement.</summary>
    public IReadOnlyList<PerkSpecializationData> Specializations => _specializations;

    public bool HasSpecialization(string id)
    {
        foreach (PerkSpecializationData owned in _specializations)
            if (owned.Id == id)
                return true;
        return false;
    }

    /// <summary>Faux si le perk est inconnu, déjà acquis ou si les emplacements sont pleins.</summary>
    public bool AcquireSpecialization(PerkSpecializationData perk)
    {
        int capacity = PerkSpecializationDataLoader.Config?.MaxEquipped ?? 0;
        if (perk == null || _specializations.Count >= capacity || HasSpecialization(perk.Id))
            return false;
        _specializations.Add(perk);
        if (_specializationRuntime == null)
        {
            _specializationRuntime = new SpecializationRuntime { Name = "Specializations" };
            _specializationRuntime.Initialize(this);
            AddChild(_specializationRuntime);
        }
        _specializationRuntime.Add(perk);
        _eventBus?.EmitSignal(EventBus.SignalName.SpecializationAcquired, perk.Id);
        return true;
    }

    /// <summary>Convergence : place en tête d'une recherche triée la cible prioritaire de l'arme qui attaque.</summary>
    private void PromotePriorityTarget<T>(List<T> sortedByDistance, Func<T, Node2D> enemyOf) =>
        _specializationRuntime?.PriorityTargeting?.Promote(_equippedWeapon, sortedByDistance, enemyOf);
}
