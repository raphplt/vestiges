using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Progression;

/// <summary>
/// Signature de départ d'un personnage (perks `is_passive` de perks.json), appliquée une fois au début de la run.
/// Les anciens Dons des coffres et leurs synergies sont retirés (plan 21, lot G2b) ; les affinités des personnages
/// remplaceront ces signatures au lot G4.
/// </summary>
public partial class PerkManager : Node
{
    public override void _Ready()
    {
        PerkDataLoader.Load();
    }

    public void ApplyPassivePerks(string characterId)
    {
        if (GetTree().GetFirstNodeInGroup("player") is not Player player)
            return;
        foreach (PerkData passive in PerkDataLoader.GetAll())
        {
            if (!passive.IsPassive || passive.CharacterId != characterId)
                continue;
            if (passive.Effects != null)
                foreach (PerkEffect effect in passive.Effects)
                    if (effect.Stat != null)
                        player.ApplyPerkModifier(effect.Stat, effect.Modifier, effect.ModifierType);
            if (passive.Stat != null)
                player.ApplyPerkModifier(passive.Stat, passive.Modifier, passive.ModifierType);
            GD.Print($"[PerkManager] Applied passive: {passive.Name}");
        }
    }
}
