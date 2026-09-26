using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Progression;
using Vestiges.Spawn;
using Vestiges.World;

namespace Vestiges.Events.RunEvents;

/// <summary>
/// Accès du micro-événement au monde : position du joueur, apparitions, récompenses.
/// Construit une fois par run par le <see cref="RunEventDirector"/>.
/// </summary>
public sealed class RunEventContext
{
    private static PackedScene _xpOrbScene;
    private static PackedScene _chestScene;

    public Player Player { get; }
    public SpawnManager Spawner { get; }
    public Node2D WorldRoot { get; }
    public EventBus EventBus { get; }
    public GroupCache Groups { get; }
    public PlayerProgression Progression { get; }
    public RandomNumberGenerator Rng { get; } = new();

    public RunEventContext(Player player, SpawnManager spawner, Node2D worldRoot, EventBus eventBus, GroupCache groups)
    {
        Player = player;
        Spawner = spawner;
        WorldRoot = worldRoot;
        EventBus = eventBus;
        Groups = groups;
        Progression = player.GetNodeOrNull<PlayerProgression>("PlayerProgression");
        Rng.Randomize();
        _xpOrbScene ??= GD.Load<PackedScene>("res://scenes/combat/XpOrb.tscn");
        _chestScene ??= GD.Load<PackedScene>("res://scenes/world/Chest.tscn");
    }

    public float ElapsedSeconds => Spawner.ElapsedSeconds;
    public float ElapsedMinutes => Spawner.ElapsedSeconds / 60f;

    /// <summary>
    /// Point praticable devant le joueur (±70° autour de sa marche), sinon dans une direction quelconque.
    /// L'événement se place là où le joueur va, sans le forcer à revenir sur ses pas.
    /// </summary>
    public Vector2 PickPointAhead(float minDistance, float maxDistance)
    {
        Vector2 origin = Player.GlobalPosition;
        Vector2 heading = Player.Velocity.LengthSquared() > 1f ? Player.Velocity.Normalized() : Vector2.FromAngle(Rng.RandfRange(0f, Mathf.Tau));
        for (int attempt = 0; attempt < 16; attempt++)
        {
            float spread = attempt < 10 ? Mathf.DegToRad(70f) : Mathf.Pi;
            Vector2 direction = heading.Rotated(Rng.RandfRange(-spread, spread));
            Vector2 candidate = origin + direction * Rng.RandfRange(minDistance, maxDistance);
            if (Spawner.IsSpawnablePosition(candidate))
                return candidate;
        }
        return origin + heading * minDistance;
    }

    public Vector2 PointAround(Vector2 center, float minRadius, float maxRadius) =>
        center + Vector2.FromAngle(Rng.RandfRange(0f, Mathf.Tau)) * Rng.RandfRange(minRadius, maxRadius);

    public void SpawnChest(string chestId, Vector2 position)
    {
        Infrastructure.ChestData data = Infrastructure.ChestDataLoader.Get(chestId);
        if (data == null)
            return;
        Chest chest = _chestScene.Instantiate<Chest>();
        chest.GlobalPosition = position;
        WorldRoot.CallDeferred(Node.MethodName.AddChild, chest);
        Callable.From(() => chest.Initialize(data)).CallDeferred();
    }

    /// <summary>Gerbe d'orbes d'XP : la récompense se voit et se ramasse.</summary>
    public void SpawnXpBurst(float totalXp, Vector2 position, int orbCount)
    {
        if (totalXp <= 0f || orbCount <= 0)
            return;
        float perOrb = totalXp / orbCount;
        for (int i = 0; i < orbCount; i++)
        {
            XpOrb orb = _xpOrbScene.Instantiate<XpOrb>();
            orb.GlobalPosition = PointAround(position, 4f, 28f);
            orb.Initialize(perOrb);
            WorldRoot.CallDeferred(Node.MethodName.AddChild, orb);
        }
    }

    /// <summary>XP équivalente à une fraction du niveau en cours : une récompense qui garde sa valeur toute la run.</summary>
    public float XpForLevelRatio(float ratio) => (Progression?.XpToNextLevel ?? 50f) * ratio;

    public void GrantEssence(int amount)
    {
        if (amount > 0)
            EventBus.EmitSignal(EventBus.SignalName.LootReceived, "essence", "run_event", amount);
    }

    public void HealRatio(float ratio)
    {
        if (ratio > 0f)
            Player.Heal(Player.EffectiveMaxHp * ratio);
    }

    /// <summary>Dégâts de zone aux créatures ; renvoie le nombre de créatures abattues.</summary>
    /// <summary>Zone posée au sol : le rayon est mesuré au sol, comme l'ellipse annoncée.</summary>
    public int DamageEnemiesInRadius(Vector2 center, float radius, float damage)
    {
        float radiusSq = radius * radius;
        int kills = 0;
        foreach (Node node in Groups.GetEnemies())
        {
            if (node is not Enemy enemy || !enemy.IsActive || enemy.IsDying)
                continue;
            if (Iso.GroundDistanceSquared(enemy.GlobalPosition, center) > radiusSq)
                continue;
            enemy.TakeDamage(damage);
            if (enemy.IsDying)
                kills++;
        }
        return kills;
    }

    /// <summary>Zone posée au sol : le rayon est mesuré au sol, comme l'ellipse annoncée.</summary>
    public void DamagePlayerIfInside(Vector2 center, float radius, float maxHpRatio)
    {
        if (Iso.GroundDistanceSquared(Player.GlobalPosition, center) <= radius * radius)
            Player.TakeDamage(Player.EffectiveMaxHp * maxHpRatio);
    }

    public static string Tr(string key) => TranslationServer.Translate(key);
}
