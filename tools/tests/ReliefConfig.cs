using Godot;
using Godot.Collections;

namespace Vestiges.Tests;

/// <summary>
/// Réglages du bac à sable de la verticalité (plan 59), lus dans <c>data/sandbox/relief.json</c>. Unités : une cellule
/// vaut 1, un étage vaut <see cref="LevelHeight"/>, les vitesses sont en cellules par seconde.
/// </summary>
public sealed class ReliefConfig
{
    public sealed class Creature
    {
        public string Id;
        public float Speed;
        public float Hp;
        public int Weight;
    }

    public float LevelHeight;
    public Vector2 Spawn;
    public string[] Arena;
    public string Character;

    public float PlayerSpeed, PlayerRadius, Step, PlayerHp, HurtInvulnerable;
    public float BondVertical, BondHorizontal, BondCooldown, BondInvulnerable, AirAccel, Gravity;
    public float SafeDropLevels, DropDamagePerLevel, StompMinDropLevels, StompRadius, StompPush, StompDamage, VoidDamage;

    public float WeaponInterval, WeaponRange, WeaponDamage, WeaponSpeed, WeaponHitRadius, WeaponPush;

    public int CrowdCount;
    public float SpawnMin, SpawnMax, CrowdRadius, Separation, ClimbSpeed, ContactReach, ContactHeight, ContactDamage;
    public float PushDecay, CrowdFallDamagePerLevel, CrowdSafeDropLevels;
    public Creature[] Creatures;

    public string[] GroundTiles, StepTiles, TerraceTiles, ButteTiles;

    public float CameraPitch, CameraDistance, CameraFov, CameraMinDistance, CameraMaxDistance;
    public float ChasePitch, ChaseDistance, ChaseFov, ChaseHeight, ChaseMouseDegrees;

    public static ReliefConfig Load(string path)
    {
        using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        Dictionary root = Json.ParseString(file.GetAsText()).AsGodotDictionary();
        Dictionary player = root["player"].AsGodotDictionary();
        Dictionary weapon = root["weapon"].AsGodotDictionary();
        Dictionary crowd = root["crowd"].AsGodotDictionary();
        Dictionary tiles = root["tiles"].AsGodotDictionary();
        Dictionary camera = root["camera_3d"].AsGodotDictionary();
        Dictionary chase = root["camera_chase"].AsGodotDictionary();
        Array spawn = root["spawn"].AsGodotArray();
        Array creatures = root["creatures"].AsGodotArray();

        ReliefConfig config = new()
        {
            LevelHeight = F(root, "level_height"),
            Spawn = new Vector2((float)spawn[0].AsDouble(), (float)spawn[1].AsDouble()),
            Arena = root["arena"].AsStringArray(),
            Character = root["character"].AsString(),
            PlayerSpeed = F(player, "speed"),
            PlayerRadius = F(player, "radius"),
            Step = F(player, "step"),
            PlayerHp = F(player, "hp"),
            HurtInvulnerable = F(player, "hurt_invulnerable_seconds"),
            BondVertical = F(player, "bond_vertical"),
            BondHorizontal = F(player, "bond_horizontal"),
            BondCooldown = F(player, "bond_cooldown"),
            BondInvulnerable = F(player, "bond_invulnerable_seconds"),
            AirAccel = F(player, "air_accel"),
            Gravity = F(player, "gravity"),
            SafeDropLevels = F(player, "safe_drop_levels"),
            DropDamagePerLevel = F(player, "drop_damage_per_level"),
            StompMinDropLevels = F(player, "stomp_min_drop_levels"),
            StompRadius = F(player, "stomp_radius"),
            StompPush = F(player, "stomp_push"),
            StompDamage = F(player, "stomp_damage"),
            VoidDamage = F(player, "void_damage"),
            WeaponInterval = F(weapon, "interval"),
            WeaponRange = F(weapon, "range"),
            WeaponDamage = F(weapon, "damage"),
            WeaponSpeed = F(weapon, "speed"),
            WeaponHitRadius = F(weapon, "hit_radius"),
            WeaponPush = F(weapon, "push"),
            CrowdCount = (int)F(crowd, "count"),
            SpawnMin = F(crowd, "spawn_min"),
            SpawnMax = F(crowd, "spawn_max"),
            CrowdRadius = F(crowd, "radius"),
            Separation = F(crowd, "separation"),
            ClimbSpeed = F(crowd, "climb_speed"),
            ContactReach = F(crowd, "contact_reach"),
            ContactHeight = F(crowd, "contact_height"),
            ContactDamage = F(crowd, "contact_damage"),
            PushDecay = F(crowd, "push_decay"),
            CrowdFallDamagePerLevel = F(crowd, "fall_damage_per_level"),
            CrowdSafeDropLevels = F(crowd, "safe_drop_levels"),
            GroundTiles = tiles["ground"].AsStringArray(),
            StepTiles = tiles["steps"].AsStringArray(),
            TerraceTiles = tiles["terrace"].AsStringArray(),
            ButteTiles = tiles["butte"].AsStringArray(),
            CameraPitch = F(camera, "pitch_degrees"),
            CameraDistance = F(camera, "distance"),
            CameraFov = F(camera, "fov"),
            CameraMinDistance = F(camera, "min_distance"),
            CameraMaxDistance = F(camera, "max_distance"),
            ChasePitch = F(chase, "pitch_degrees"),
            ChaseDistance = F(chase, "distance"),
            ChaseFov = F(chase, "fov"),
            ChaseHeight = F(chase, "height"),
            ChaseMouseDegrees = F(chase, "mouse_degrees_per_pixel"),
            Creatures = new Creature[creatures.Count],
        };
        for (int i = 0; i < creatures.Count; i++)
        {
            Dictionary creature = creatures[i].AsGodotDictionary();
            config.Creatures[i] = new Creature
            {
                Id = creature["id"].AsString(),
                Speed = F(creature, "speed"),
                Hp = F(creature, "hp"),
                Weight = (int)F(creature, "weight"),
            };
        }
        return config;
    }

    private static float F(Dictionary dictionary, string key) => (float)dictionary[key].AsDouble();
}
