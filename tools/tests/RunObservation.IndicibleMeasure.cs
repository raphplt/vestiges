using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Events;
using Vestiges.Infrastructure;

namespace Vestiges.Tests;

/// <summary>
/// --measure-indicible (DECISIONS §64) : mesure, sans image, pourquoi l'Indicible ne se combat pas. Le boss est forcé
/// sur le joueur, qui garde le build de la capture de fin de partie. Trois postures de bot, chacune --seconds secondes de
/// jeu (60 images par seconde) : il tourne en rond, il reste au centre, il se poste hors de l'anneau des bords.
/// Par posture : PV perdus par le boss, tirs du joueur, part visée vers le centre, part qui atteint un bord, attaques de
/// tentacules et coups reçus, distance au centre.
/// </summary>
public partial class RunObservation
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly FieldInfo ProjectileDirection = typeof(Projectile).GetField("_direction", Private);
    private static readonly FieldInfo ProjectileAge = typeof(Projectile).GetField("_age", Private);
    private static readonly FieldInfo ProjectileOwner = typeof(Projectile).GetField("_owner", Private);
    private static readonly FieldInfo BossHp = typeof(Indicible).GetField("_currentHp", Private);
    private static readonly FieldInfo BossTentacleTimer = typeof(Indicible).GetField("_tentacleTimer", Private);

    private sealed class ShotTrace
    {
        public float LastAge;
        public bool TowardCenter;
        public bool ReachedEdge;
        public float ClosestToCenter = float.MaxValue;
    }

    private async Task MeasureIndicible(double phaseSeconds)
    {
        _world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
        await Frames(60);
        foreach (string weapon in new[] { "crossbow", "throwing_axes", "sling" })
            _player.AddWeapon(WeaponDataLoader.Get(weapon));
        EndgameManager endgame = _world.GetNode<EndgameManager>("EndgameManager");
        typeof(EndgameManager).GetMethod("SpawnIndicible", Private).Invoke(endgame, null);
        Indicible boss = _world.GetNode<Indicible>("IndicibleBoss");
        IndicibleConfig.TryLoad(out IndicibleConfig config, out _);
        Vector2 center = boss.GlobalPosition;
        GD.Print(string.Create(CultureInfo.InvariantCulture,
            $"[IndicibleMeasure] arène centre={center} bords à {config.EdgeSpread} (demi-longueur {config.EdgeHalfLength}, demi-épaisseur {config.EdgeHalfThickness}) ; tentacule largeur {config.TentacleWidth}, longueur {config.TentacleLength}, annonce {config.WarningDuration} s, décalage ±{config.TargetJitter}"));

        int hits = 0;
        EventBus eventBus = GetNode<EventBus>("/root/EventBus");
        EventBus.PlayerHitByEventHandler onHit = (source, _) => { if (source == EnemyGrammar.FinalBossId) hits++; };
        eventBus.PlayerHitBy += onHit;

        (string Name, System.Func<int, Vector2?> Move, Vector2? Place)[] postures =
        {
            ("cercle", frame => Vector2.FromAngle(frame / 60f * 0.9f), null),
            ("centre", _ => Vector2.Zero, center),
            ("dehors", _ => Vector2.Zero, center + new Vector2(config.EdgeSpread + config.EdgeHalfThickness + 150f, 0f)),
        };
        foreach ((string name, System.Func<int, Vector2?> move, Vector2? place) in postures)
        {
            if (!endgame.IsBossSpawned || !IsInstanceValid(boss))
                break;
            if (place is Vector2 spot)
                _player.GlobalPosition = spot;
            await MeasurePosture(name, boss, config, center, move, (int)(phaseSeconds * 60), () => hits, value => hits = value);
        }
        eventBus.PlayerHitBy -= onHit;
        GD.Print("[RunObservation] RESULT indicible-measure done");
    }

    private async Task MeasurePosture(string name, Indicible boss, IndicibleConfig config, Vector2 center,
        System.Func<int, Vector2?> move, int frames, System.Func<int> readHits, System.Action<int> setHits)
    {
        setHits(0);
        float startHp = (float)BossHp.GetValue(boss);
        Dictionary<ulong, ShotTrace> shots = new();
        int launched = 0, attacks = 0;
        float lastTimer = (float)BossTentacleTimer.GetValue(boss);
        double distanceSum = 0;
        float distanceMin = float.MaxValue, distanceMax = 0f;
        int insideRing = 0;

        for (int frame = 0; frame < frames && IsInstanceValid(boss); frame++)
        {
            _player.AIInputOverride = move(frame) ?? Vector2.Zero;
            await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
            if (GetTree().Paused)
                AutoPickLevelUp();
            if (!IsInstanceValid(boss))
                break;

            float timer = (float)BossTentacleTimer.GetValue(boss);
            if (timer > lastTimer)
                attacks++;
            lastTimer = timer;

            Vector2 local = _player.GlobalPosition - center;
            float distance = local.Length();
            distanceSum += distance;
            distanceMin = Mathf.Min(distanceMin, distance);
            distanceMax = Mathf.Max(distanceMax, distance);
            float ring = config.EdgeSpread - config.EdgeHalfThickness;
            if (Mathf.Abs(local.X) < ring && Mathf.Abs(local.Y) < ring)
                insideRing++;

            foreach (Projectile projectile in FindPlayerProjectiles(_world))
            {
                float age = (float)ProjectileAge.GetValue(projectile);
                ulong id = projectile.GetInstanceId();
                if (!shots.TryGetValue(id, out ShotTrace trace) || age < trace.LastAge)
                {
                    trace = new ShotTrace();
                    shots[id] = trace;
                    launched++;
                    Vector2 direction = (Vector2)ProjectileDirection.GetValue(projectile);
                    Vector2 toCenter = center - projectile.GlobalPosition;
                    trace.TowardCenter = toCenter.LengthSquared() < 1f || Mathf.Abs(direction.AngleTo(toCenter)) < Mathf.DegToRad(15f);
                }
                trace.LastAge = age;
                Vector2 at = projectile.GlobalPosition - center;
                trace.ClosestToCenter = Mathf.Min(trace.ClosestToCenter, at.Length());
                trace.ReachedEdge |= InEdge(at, config);
            }
        }

        int toward = 0, edge = 0, nearCenter = 0;
        foreach (ShotTrace trace in shots.Values)
        {
            toward += trace.TowardCenter ? 1 : 0;
            edge += trace.ReachedEdge ? 1 : 0;
            nearCenter += trace.ClosestToCenter < 40f ? 1 : 0;
        }
        float endHp = IsInstanceValid(boss) ? (float)BossHp.GetValue(boss) : 0f;
        GD.Print(string.Create(CultureInfo.InvariantCulture,
            $"[IndicibleMeasure] posture={name} secondes={frames / 60f:F0} pv_boss={startHp:F0}->{endHp:F0} tirs={launched} vers_centre={toward} passent_a_40px_du_centre={nearCenter} atteignent_un_bord={edge} attaques={attacks} coups_recus={readHits()} distance_centre_moy={distanceSum / frames:F0} min={distanceMin:F0} max={distanceMax:F0} dans_l_anneau={100.0 * insideRing / frames:F0}%"));
    }

    private static bool InEdge(Vector2 at, IndicibleConfig config)
    {
        float spread = config.EdgeSpread, length = config.EdgeHalfLength, thickness = config.EdgeHalfThickness;
        bool horizontal = Mathf.Abs(at.X) <= length && (Mathf.Abs(at.Y - spread) <= thickness || Mathf.Abs(at.Y + spread) <= thickness);
        bool vertical = Mathf.Abs(at.Y) <= length && (Mathf.Abs(at.X - spread) <= thickness || Mathf.Abs(at.X + spread) <= thickness);
        return horizontal || vertical;
    }

    private readonly HashSet<Node> _projectileParents = new();
    private int _projectileScanFrame = -1;

    // Le monde compte des milliers de décors : l'arbre entier n'est parcouru qu'une fois par seconde, pour trouver les
    // conteneurs de projectiles ; entre deux, seuls ces conteneurs sont lus.
    private List<Projectile> FindPlayerProjectiles(Node root)
    {
        int frame = (int)Engine.GetPhysicsFrames();
        if (_projectileScanFrame < 0 || frame - _projectileScanFrame >= 60)
        {
            _projectileScanFrame = frame;
            CollectParents(root);
        }
        List<Projectile> found = new();
        foreach (Node parent in _projectileParents)
        {
            if (!IsInstanceValid(parent))
                continue;
            foreach (Node child in parent.GetChildren())
            {
                if (child is Projectile projectile && projectile.Visible && ProjectileOwner.GetValue(projectile) == _player)
                    found.Add(projectile);
            }
        }
        return found;
    }

    private void CollectParents(Node node)
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is Projectile)
                _projectileParents.Add(node);
            else
                CollectParents(child);
        }
    }
}
