using Godot;
using Vestiges.Base;
using Vestiges.Progression;
using Vestiges.Score;
using Vestiges.UI;

namespace Vestiges.World;

/// <summary>
/// Wire les systèmes entre eux au démarrage de la scène.
/// Exécuté après tous les _Ready (grâce à l'ordre des enfants dans Main).
/// </summary>
public partial class GameBootstrap : Node
{
    public override void _Ready()
    {
        PlayerProgression progression = GetNode<PlayerProgression>("../Player/PlayerProgression");
        Inventory inventory = GetNode<Inventory>("../Player/Inventory");
        PerkManager perkManager = GetNode<PerkManager>("../PerkManager");
        ScoreManager scoreManager = GetNode<ScoreManager>("../ScoreManager");
        CraftManager craftManager = GetNode<CraftManager>("../CraftManager");
        StructureManager structureManager = GetNode<StructureManager>("../StructureManager");
        StructurePlacer structurePlacer = GetNode<StructurePlacer>("../StructurePlacer");
        DayNightCycle dayNightCycle = GetNode<DayNightCycle>("../DayNightCycle");
        HUD hud = GetNode<HUD>("../HUD");
        LevelUpScreen levelUpScreen = GetNode<LevelUpScreen>("../LevelUpScreen");
        GameOverScreen gameOverScreen = GetNode<GameOverScreen>("../GameOverScreen");
        CraftPanel craftPanel = GetNode<CraftPanel>("../CraftPanel");

        hud.SetProgression(progression);
        hud.SetDayNightCycle(dayNightCycle);
        levelUpScreen.SetPerkManager(perkManager);
        gameOverScreen.SetScoreManager(scoreManager);

        craftManager.SetInventory(inventory);
        craftPanel.SetCraftManager(craftManager);
        craftPanel.SetInventory(inventory);
        craftPanel.SetStructureManager(structureManager);
        structurePlacer.SetStructureManager(structureManager);

        GD.Print("[GameBootstrap] Systems wired");
    }
}
