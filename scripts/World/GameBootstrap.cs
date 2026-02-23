using Godot;
using Vestiges.Base;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;
using Vestiges.Score;
using Vestiges.UI;

namespace Vestiges.World;

/// <summary>
/// Wire les systèmes entre eux au démarrage de la scène.
/// Exécuté après tous les _Ready (grâce à l'ordre des enfants dans Main).
/// Lit le personnage sélectionné depuis GameManager (choisi dans le Hub).
/// </summary>
public partial class GameBootstrap : Node
{
    private const string FallbackCharacterId = "vagabond";

    public override void _Ready()
    {
        CharacterDataLoader.Load();
        PerkDataLoader.Load();

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
        Player player = GetNode<Player>("../Player");

        hud.SetProgression(progression);
        hud.SetDayNightCycle(dayNightCycle);
        levelUpScreen.SetPerkManager(perkManager);
        gameOverScreen.SetScoreManager(scoreManager);

        craftManager.SetInventory(inventory);
        craftPanel.SetCraftManager(craftManager);
        craftPanel.SetInventory(inventory);
        craftPanel.SetStructureManager(structureManager);
        structurePlacer.SetStructureManager(structureManager);

        // Read character from GameManager (selected in the Hub)
        GameManager gm = GetNode<GameManager>("/root/GameManager");
        string characterId = gm.SelectedCharacterId;
        if (string.IsNullOrEmpty(characterId))
            characterId = FallbackCharacterId;

        CharacterData data = CharacterDataLoader.Get(characterId);
        if (data == null)
        {
            GD.PushError($"[GameBootstrap] Unknown character: {characterId}, falling back to {FallbackCharacterId}");
            data = CharacterDataLoader.Get(FallbackCharacterId);
        }

        player.InitializeCharacter(data);
        perkManager.ApplyPassivePerks(data.Id);
        scoreManager.SetCharacterMultiplier(data.ScoreMultiplier);

        gm.ChangeState(GameManager.GameState.Run);

        GD.Print($"[GameBootstrap] Run started with {data.Name}");
    }
}
