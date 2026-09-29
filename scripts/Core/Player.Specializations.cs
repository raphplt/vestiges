using System.Collections.Generic;
using Vestiges.Infrastructure;

namespace Vestiges.Core;

public partial class Player
{
    private readonly List<PerkSpecializationData> _specializations = new();

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
        _eventBus?.EmitSignal(EventBus.SignalName.SpecializationAcquired, perk.Id);
        return true;
    }
}
