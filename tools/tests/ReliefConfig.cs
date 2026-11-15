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

    /// <summary>Décor semé : une chance par cellule plate, et son emprise d'obstacle (cellules).</summary>
    public sealed class LowPolyScatter
    {
        public string Model;
        public float Density;
        public float Radius;
    }

    /// <summary>Décor posé à la main : son pied (cellules) et son orientation.</summary>
    public sealed class LowPolyLandmark
    {
        public string Model;
        public Vector2 At;
        public float YawDegrees;
    }

    /// <summary>Rendu low poly (plan 60, lot 60.0b) : échelle des modèles, couleurs du relief, décors.</summary>
    public sealed class LowPolyStyle
    {
        public float MetresPerCell;
        public Color Ground, Terrace, Butte, Steps, Cliff, CliffDark;
        public float ShadeJitter, HeightJitter, CliffJitter;
        public string StartGround;
        public bool StartMid, StartOutlines, StartPixels, StartToon;
        public int PixelScale;
        public Color OutlineColor;
        public float OutlineSensitivity, OutlineOrthoSensitivity;
        public LowPolyScatter[] Scatter;
        public LowPolyLandmark[] Landmarks;
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
    public LowPolyStyle LowPoly;

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
        config.LowPoly = LoadLowPoly(root["lowpoly"].AsGodotDictionary());
        return config;
    }

    private static LowPolyStyle LoadLowPoly(Dictionary lowPoly)
    {
        Dictionary colors = lowPoly["colors"].AsGodotDictionary();
        Array scatter = lowPoly["scatter"].AsGodotArray();
        Array landmarks = lowPoly["landmarks"].AsGodotArray();
        Dictionary start = lowPoly["start"].AsGodotDictionary();
        Dictionary outline = lowPoly["outline"].AsGodotDictionary();
        LowPolyStyle style = new()
        {
            StartGround = start["ground"].AsString(),
            StartMid = start["models"].AsString() == "mid",
            StartOutlines = start["outlines"].AsBool(),
            StartPixels = start["pixels"].AsBool(),
            StartToon = start["toon"].AsBool(),
            PixelScale = (int)F(lowPoly, "pixel_scale"),
            OutlineColor = new Color(outline["color"].AsString()),
            OutlineSensitivity = F(outline, "sensitivity"),
            OutlineOrthoSensitivity = F(outline, "ortho_sensitivity"),
            MetresPerCell = F(lowPoly, "metres_per_cell"),
            Ground = new Color(colors["ground"].AsString()),
            Terrace = new Color(colors["terrace"].AsString()),
            Butte = new Color(colors["butte"].AsString()),
            Steps = new Color(colors["steps"].AsString()),
            Cliff = new Color(colors["cliff"].AsString()),
            CliffDark = new Color(colors["cliff_dark"].AsString()),
            ShadeJitter = F(lowPoly, "shade_jitter"),
            HeightJitter = F(lowPoly, "height_jitter"),
            CliffJitter = F(lowPoly, "cliff_jitter"),
            Scatter = new LowPolyScatter[scatter.Count],
            Landmarks = new LowPolyLandmark[landmarks.Count],
        };
        for (int i = 0; i < scatter.Count; i++)
        {
            Dictionary entry = scatter[i].AsGodotDictionary();
            style.Scatter[i] = new LowPolyScatter { Model = entry["model"].AsString(), Density = F(entry, "density"), Radius = F(entry, "radius") };
        }
        for (int i = 0; i < landmarks.Count; i++)
        {
            Dictionary entry = landmarks[i].AsGodotDictionary();
            Array at = entry["at"].AsGodotArray();
            style.Landmarks[i] = new LowPolyLandmark
            {
                Model = entry["model"].AsString(),
                At = new Vector2((float)at[0].AsDouble(), (float)at[1].AsDouble()),
                YawDegrees = F(entry, "yaw_degrees"),
            };
        }
        return style;
    }

    private static float F(Dictionary dictionary, string key) => (float)dictionary[key].AsDouble();
}
