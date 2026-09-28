using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.UI;

namespace Vestiges.Tests;

/// <summary>
/// --capture-death : mort et bilan de fin de run (plan 02 lot D, M1). Le bot joue --seconds secondes pour se constituer
/// un build, puis meurt pour de bon sous le coup de la créature la plus proche ; captures pendant l'impact, l'effacement,
/// les trois temps de la révélation et l'écran final.
/// </summary>
public partial class RunObservation
{
    private static readonly string[] PassiveIds = { "flamme_interieure", "memoire_vive", "instinct" };

    private async Task CaptureDeath(double playSeconds)
    {
        await Seconds(playSeconds);
        // Build fourni pour juger la mise en page d'une run riche : quatre armes, niveaux variés, souvenirs.
        _player.AddWeapon(WeaponDataLoader.Get("heavy_hammer"));
        _player.AddWeapon(WeaponDataLoader.Get("makeshift_bow"));
        _player.AddWeapon(WeaponDataLoader.Get("chain_of_names"));
        for (int i = 0; i < 3; i++)
            _player.UpgradeWeapon("heavy_hammer", System.Array.Empty<StatGain>());
        foreach (string passive in PassiveIds)
            _player.AddOrUpgradePassive(passive, 1f, 1);
        _player.AddOrUpgradePassive(PassiveIds[0], 1f, 1);
        // Profil dev, tout est débloqué : on fait comme si la Chaîne des noms avait été retrouvée pendant la run,
        // pour montrer la carte « Arme retrouvée » et l'entrée de la Collection.
        GameOverScreen gameOver = _world.GetNode<GameOverScreen>("GameOverScreen");
        ((HashSet<string>)typeof(GameOverScreen).GetField("_weaponsAtStart", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(gameOver)).Remove("chain_of_names");
        // Le coup fatal vient de la créature la plus proche, pour que la séquence ait un tueur à montrer.
        Enemy killer = NearestEnemy();
        if (killer != null)
            GetNode<EventBus>("/root/EventBus").EmitSignal(EventBus.SignalName.PlayerHitBy, killer.EnemyId, 1f);
        _player.IsGodMode = false;
        // Esquive (Instinct) ou dash du bot peuvent annuler un coup : on frappe jusqu'à la mort.
        for (int attempt = 0; attempt < 120 && !_player.IsDead; attempt++)
        {
            _player.TakeDamage(100000f);
            if (!_player.IsDead)
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        // Séquence de mort : impact, le joueur se défait, effacement naissant, à mi-course, écran couvert ; puis le bilan.
        double[] moments = { 0.08, 0.6, 1.2, 1.8, 2.4, 3.3, 6.0 };
        // Instants comptés depuis la mort : l'écriture d'une capture 4K prend elle-même plusieurs centaines de ms.
        ulong deathMsec = Time.GetTicksMsec();
        for (int shot = 0; shot < moments.Length; shot++)
        {
            double wait = moments[shot] - (Time.GetTicksMsec() - deathMsec) / 1000.0;
            // Au moins une image rendue entre deux captures : l'écriture de la précédente a pu consommer l'attente.
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            if (wait > 0.0)
                await Seconds(wait);
            Save($"death-{shot}.png");
            GD.Print($"[RunObservation] death-{shot} at {(Time.GetTicksMsec() - deathMsec) / 1000.0:0.00} s");
        }
        GD.Print($"[RunObservation] RESULT death captured, killer={killer?.EnemyId ?? "none"}, dead={_player.IsDead}");
    }

    private Enemy NearestEnemy()
    {
        Enemy nearest = null;
        float best = float.MaxValue;
        foreach (Node node in GetNode<GroupCache>("/root/GroupCache").GetEnemies())
        {
            if (node is not Enemy enemy || !enemy.IsActive || enemy.IsDying)
                continue;
            float distance = enemy.GlobalPosition.DistanceSquaredTo(_player.GlobalPosition);
            if (distance < best)
            {
                best = distance;
                nearest = enemy;
            }
        }
        return nearest;
    }
}
