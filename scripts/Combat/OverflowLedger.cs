using System.Collections.Generic;
using Godot;

namespace Vestiges.Combat;

/// <summary>
/// Réserves de Débordement (plan 05 §3.2), une au plus par joueur et par arme : un montant de dégâts figé, que le
/// premier impact direct d'un lancement ultérieur de la même arme emporte en entier. Le lancement qui charge ne
/// consomme jamais sa propre réserve. Consulté par chaque impact direct : sans réserve, la lecture est immédiate.
/// </summary>
public static class OverflowLedger
{
    private struct Reserve
    {
        public ulong OwnerId;
        public WeaponInstance Weapon;
        public ulong LaunchId;
        public float Amount;
        public float Cap;
        public float Remaining;
    }

    private static readonly List<Reserve> Reserves = new();

    /// <summary>Report à ajouter à cet impact ; la réserve est retirée.</summary>
    public static float Take(in AttackContext source)
    {
        if (Reserves.Count == 0 || !source.IsDirectWeapon)
            return 0f;
        for (int i = 0; i < Reserves.Count; i++)
        {
            Reserve reserve = Reserves[i];
            if (reserve.OwnerId != source.OwnerId || reserve.Weapon != source.Weapon || reserve.LaunchId == source.LaunchId)
                continue;
            Reserves.RemoveAt(i);
            return reserve.Amount;
        }
        return 0f;
    }

    /// <summary>
    /// Les impacts d'un même lancement s'agrègent sous un plafond unique, celui de l'attaque qui charge ; un nouvel
    /// excédent réel rafraîchit la durée.
    /// </summary>
    public static void Charge(ulong ownerId, WeaponInstance weapon, ulong launchId, float amount, float cap, float duration)
    {
        if (amount <= 0f || cap <= 0f)
            return;
        int index = Find(ownerId, weapon);
        Reserve reserve = index >= 0 && Reserves[index].LaunchId == launchId
            ? Reserves[index]
            : new Reserve { OwnerId = ownerId, Weapon = weapon, LaunchId = launchId };
        reserve.Cap = Mathf.Max(reserve.Cap, cap);
        reserve.Amount = Mathf.Min(reserve.Amount + amount, reserve.Cap);
        reserve.Remaining = duration;
        if (index >= 0)
            Reserves[index] = reserve;
        else
            Reserves.Add(reserve);
    }

    public static float Amount(ulong ownerId, WeaponInstance weapon)
    {
        int index = Find(ownerId, weapon);
        return index >= 0 ? Reserves[index].Amount : 0f;
    }

    /// <summary>Fait vieillir les réserves du joueur ; vrai s'il lui en reste.</summary>
    public static bool Advance(ulong ownerId, float delta)
    {
        bool any = false;
        for (int i = Reserves.Count - 1; i >= 0; i--)
        {
            Reserve reserve = Reserves[i];
            if (reserve.OwnerId != ownerId)
                continue;
            reserve.Remaining -= delta;
            if (reserve.Remaining <= 0f)
            {
                Reserves.RemoveAt(i);
                continue;
            }
            Reserves[i] = reserve;
            any = true;
        }
        return any;
    }

    /// <summary>Une arme qui quitte l'inventaire perd sa réserve.</summary>
    public static void DropMissing(ulong ownerId, IReadOnlyList<WeaponInstance> equipped)
    {
        for (int i = Reserves.Count - 1; i >= 0; i--)
        {
            if (Reserves[i].OwnerId != ownerId)
                continue;
            bool kept = false;
            foreach (WeaponInstance weapon in equipped)
                kept |= weapon == Reserves[i].Weapon;
            if (!kept)
                Reserves.RemoveAt(i);
        }
    }

    /// <summary>Fin de run ou joueur retiré : aucune réserve ne survit à son propriétaire.</summary>
    public static void Clear(ulong ownerId)
    {
        for (int i = Reserves.Count - 1; i >= 0; i--)
            if (Reserves[i].OwnerId == ownerId)
                Reserves.RemoveAt(i);
    }

    private static int Find(ulong ownerId, WeaponInstance weapon)
    {
        for (int i = 0; i < Reserves.Count; i++)
            if (Reserves[i].OwnerId == ownerId && Reserves[i].Weapon == weapon)
                return i;
        return -1;
    }
}
