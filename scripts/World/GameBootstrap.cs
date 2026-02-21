using Godot;
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
        PerkManager perkManager = GetNode<PerkManager>("../PerkManager");
        ScoreManager scoreManager = GetNode<ScoreManager>("../ScoreManager");
        HUD hud = GetNode<HUD>("../HUD");
        LevelUpScreen levelUpScreen = GetNode<LevelUpScreen>("../LevelUpScreen");
        GameOverScreen gameOverScreen = GetNode<GameOverScreen>("../GameOverScreen");

        hud.SetProgression(progression);
        levelUpScreen.SetPerkManager(perkManager);
        gameOverScreen.SetScoreManager(scoreManager);

        GD.Print("[GameBootstrap] Systems wired");
    }
}
