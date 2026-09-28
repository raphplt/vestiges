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
/// --skip-reveal : un appui pendant la révélation du bilan, pour vérifier que l'état final ne change pas (M4).
/// --play-bot : le bot de mesure joue pendant ces secondes (avec --nomad, il garde un cap) ; relevés de M2 réalistes.
/// --history-fixture &lt;fichier&gt; : historique posé dans le profil isolé avant la mort, pour vérifier qu'une run
/// s'y ajoute sans perdre les anciennes (plan 02 M2). Le relevé enregistré est imprimé (RECORD).
/// </summary>
public partial class RunObservation
{
    private static readonly string[] PassiveIds = { "flamme_interieure", "memoire_vive", "instinct" };

    private async Task CaptureDeath(double playSeconds)
    {
        // --play-bot : le bot de mesure joue (déplacements, combats, choix de niveau) au lieu d'attendre sur place.
        if (System.Array.IndexOf(OS.GetCmdlineUserArgs(), "--play-bot") >= 0)
            await MeasureDensity(playSeconds, GetNode<GameManager>("/root/GameManager").RunSeed);
        else
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
        LoadHistoryFixture(Argument(OS.GetCmdlineUserArgs(), "--history-fixture", ""));
        _player.IsGodMode = false;
        // Le Néant passe esquive, bouclier et invulnérabilité ; un second souffle éventuel demande un deuxième coup.
        for (int attempt = 0; attempt < 4 && !_player.IsDead; attempt++)
            _player.TakeErasureDamage(100000f);
        // Séquence de mort : impact, le joueur se défait, effacement naissant, à mi-course, écran couvert ; puis le bilan.
        // Puis les cartes de gain qui se retournent, le compteur de Vestiges, l'état final.
        double[] moments = { 0.08, 0.6, 1.2, 1.8, 2.4, 3.3, 5.2, 5.8, 8.0 };
        // --skip-reveal : un appui à 3,5 s termine la révélation ; l'état final doit être le même, gains compris.
        bool skip = System.Array.IndexOf(OS.GetCmdlineUserArgs(), "--skip-reveal") >= 0;
        const double SkipAt = 3.5;
        // Instants comptés depuis la mort : l'écriture d'une capture 4K prend elle-même plusieurs centaines de ms.
        ulong deathMsec = Time.GetTicksMsec();
        for (int shot = 0; shot < moments.Length; shot++)
        {
            double wait = moments[shot] - (Time.GetTicksMsec() - deathMsec) / 1000.0;
            // Au moins une image rendue entre deux captures : l'écriture de la précédente a pu consommer l'attente.
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            if (skip && moments[shot] > SkipAt)
            {
                double untilSkip = SkipAt - (Time.GetTicksMsec() - deathMsec) / 1000.0;
                if (untilSkip > 0.0)
                    await Seconds(untilSkip);
                Input.ParseInputEvent(new InputEventKey { Keycode = Key.Space, Pressed = true });
                Input.ParseInputEvent(new InputEventKey { Keycode = Key.Space, Pressed = false });
                skip = false;
                wait = moments[shot] - (Time.GetTicksMsec() - deathMsec) / 1000.0;
            }
            if (wait > 0.0)
                await Seconds(wait);
            Save($"death-{shot}.png");
            GD.Print($"[RunObservation] death-{shot} at {(Time.GetTicksMsec() - deathMsec) / 1000.0:0.00} s");
        }
        List<RunRecord> history = RunHistoryManager.GetHistory();
        Dictionary<int, int> versions = new();
        foreach (RunRecord run in history)
            versions[run.Version] = versions.GetValueOrDefault(run.Version) + 1;
        GD.Print($"[RunObservation] HISTORY runs={history.Count} versions={System.Text.Json.JsonSerializer.Serialize(versions)}");
        if (history.Count > 0)
            GD.Print($"[RunObservation] RECORD {System.Text.Json.JsonSerializer.Serialize(history[0])}");
        RunJourney journey = _world.GetNode<RunTracker>("RunTracker").Journey;
        Dictionary<RunMarkerKind, int> markers = new();
        foreach (RunMarker marker in journey.Markers)
            markers[marker.Kind] = markers.GetValueOrDefault(marker.Kind) + 1;
        RunSample last = journey.Samples.Count > 0 ? journey.Samples[^1] : default;
        GD.Print($"[RunObservation] JOURNEY samples={journey.Samples.Count} last=({last.Time:0.0} s, niv. {last.Level}, {last.Kills} élim., {last.DistanceMeters:0} m) markers={System.Text.Json.JsonSerializer.Serialize(markers)}");
        GD.Print($"[RunObservation] RESULT death captured, killer={killer?.EnemyId ?? "none"}, dead={_player.IsDead}");
    }

    private static void LoadHistoryFixture(string path)
    {
        if (string.IsNullOrEmpty(path))
            return;
        using FileAccess source = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (source == null)
        {
            GD.PushError($"[RunObservation] Historique introuvable : {path}");
            return;
        }
        using FileAccess target = FileAccess.Open(DevelopmentMode.GetSavePath("run_history.json"), FileAccess.ModeFlags.Write);
        target.StoreString(source.GetAsText());
        RunHistoryManager.ForceReload();
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
