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
            if (passive.Momentum != null)
            {
                RoundMomentum momentum = new() { Name = "RoundMomentum" };
                momentum.Initialize(player, passive.Momentum);
                player.AddChild(momentum);
            }
            if (passive.Echo != null)
            {
                RemanenceEcho echo = new() { Name = "RemanenceEcho" };
                echo.Initialize(player, passive.Echo);
                player.AddChild(echo);
            }
            if (passive.OpeningStrike != null && player.GetNodeOrNull<OpeningStrike>("OpeningStrike") == null)
            {
                OpeningStrike opening = new() { Name = "OpeningStrike" };
                opening.Initialize(player, passive.OpeningStrike);
                player.AddChild(opening);
            }
            if (passive.Pressure != null && player.GetNodeOrNull<ErasurePressure>("ErasurePressure") == null)
            {
                ErasurePressure pressure = new() { Name = "ErasurePressure" };
                pressure.Initialize(player, passive.Pressure);
                player.AddChild(pressure);
            }
            if (passive.RouteEndurance != null && player.RouteEndurance == null)
            {
                RouteEndurance endurance = new() { Name = "RouteEndurance" };
                endurance.Initialize(player, passive.RouteEndurance);
                player.AddChild(endurance);
            }
            if (passive.Shockwave != null && player.GetNodeOrNull<TemperedImpact>("TemperedImpact") == null)
            {
                TemperedImpact impact = new() { Name = "TemperedImpact" };
                impact.Initialize(player, passive.Shockwave);
                player.AddChild(impact);
            }
            GD.Print($"[PerkManager] Applied passive: {passive.Name}");
        }
    }
}
