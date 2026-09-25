using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Godot;

namespace Vestiges.Tests;

/// <summary>
/// --measure-props [--measure-seconds 8] : coût de rendu des décors. Pour chaque biome, le joueur tourne en rond
/// autour du point le plus chargé (sans ennemis) ; une ligne RESULT donne FPS moyen et p99 par biome.
/// Le banc de combat dense se déroule loin de la forêt : c'est ici qu'on juge le coût des canopées.
/// </summary>
public partial class RunObservation
{
    private async Task MeasurePropCost(double seconds)
    {
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        foreach (Node node in GetTree().GetNodesInGroup("enemies"))
            if (node is Vestiges.Combat.Enemy existing && existing.IsActive)
                _world.GetNode<Vestiges.Spawn.EnemyPool>("EnemyPool").Return(existing);
        await Frames(90);

        List<(string Biome, Vector2 Point, int Neighbours, int Total)> hotspots = FindPropHotspots();
        hotspots.Sort((a, b) => string.CompareOrdinal(a.Biome, b.Biome));
        foreach ((string biome, Vector2 point, int neighbours, int total) in hotspots)
        {
            _player.GlobalPosition = point + new Vector2(0f, 24f);
            _camera.ResetSmoothing();
            await Frames(30);

            List<double> frames = new();
            ulong start = Time.GetTicksUsec();
            ulong last = start;
            while ((Time.GetTicksUsec() - start) / 1e6 < seconds)
            {
                double t = (Time.GetTicksUsec() - start) / 1e6;
                // Cercle d'environ 200 px : la vue balaie la zone et passe sous les canopées.
                _player.AIInputOverride = Vector2.FromAngle((float)(t * 0.9));
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                ulong now = Time.GetTicksUsec();
                frames.Add((now - last) / 1000.0);
                last = now;
            }
            _player.AIInputOverride = Vector2.Zero;
            frames.Sort();
            double mean = 0;
            foreach (double frame in frames)
                mean += frame;
            mean /= frames.Count;
            double p99 = frames[Mathf.Min(frames.Count - 1, (int)(frames.Count * 0.99))];
            GD.Print(string.Format(CultureInfo.InvariantCulture,
                "[RunObservation] RESULT props_cost biome={0} props={1} neighbours={2} fps={3:F1} p99_ms={4:F2}",
                biome, total, neighbours, 1000.0 / mean, p99));
        }
    }
}
