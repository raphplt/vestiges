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
        // Agrandissement du joueur et des créatures, décors à part (§237) : des corps qui comptent à l'écran, comme Megabonk.
        public float BodyScale;
        // Teintes de l'herbe du sol et des terrasses, que les touffes reprennent.
        public Color Ground, Terrace;
        // Saillie des facettes des falaises taillées (cellules, §238).
        public float CarvedRelief;
        public string StartGround;
        public bool StartMid, StartOutlines, StartToon;
        public Color OutlineColor;
        public float OutlineSensitivity, OutlineOrthoSensitivity;
        // Touffes d'herbe en relief du sol texturé : combien par cellule d'herbe, et leur hauteur moyenne (cellules).
        public float GrassPerCell, GrassHeight;
        public LowPolyScatter[] Scatter;
        public LowPolyLandmark[] Landmarks;
    }

    public float LevelHeight;
    public Vector2 Spawn;
    public string[] Arena;
    public string Character;

    public float PlayerSpeed, PlayerRadius, Step, PlayerHp, HurtInvulnerable;
    // Rythme 3D : les vitesses du jeu (px/s, 45 px par cellule) ramenées à des corps à l'échelle (plan 60, §236).
    public float Pace, AccelerationSeconds, BrakingSeconds;
    // Saut commun : hauteur (cellules) et durée en l'air sur le plat, d'où sa vitesse de départ et la gravité.
    public float JumpVelocity, JumpCooldown, JumpInvulnerable, AirControl, Gravity;
    // La capacité de déplacement propre au personnage (data/movement/mobility.json, son profil dans characters.json).
    public Vestiges.Infrastructure.MobilityConfig Mobility;
    public float SafeDropLevels, DropDamagePerLevel, StompMinDropLevels, StompRadius, StompPush, StompDamage, VoidDamage;

    public float WeaponInterval, WeaponRange, WeaponDamage, WeaponSpeed, WeaponHitRadius, WeaponPush;

    public int CrowdCount;
    public float SpawnMin, SpawnMax, CrowdRadius, Separation, ClimbSpeed, ContactReach, ContactHeight, ContactDamage;
    public float PushDecay, CrowdFallDamagePerLevel, CrowdSafeDropLevels;
    public Creature[] Creatures;
    public LowPolyStyle LowPoly;

    public string[] GroundTiles, StepTiles, TerraceTiles, ButteTiles;

    public float ChasePitch, ChaseMinPitch, ChaseMaxPitch, ChaseDistance;
    public float ChaseFov, ChaseHeight, ChaseMouseDegrees, ChaseClearance;

    public static ReliefConfig Load(string path)
    {
        using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        Dictionary root = Json.ParseString(file.GetAsText()).AsGodotDictionary();
        Dictionary player = root["player"].AsGodotDictionary();
        Dictionary weapon = root["weapon"].AsGodotDictionary();
        Dictionary crowd = root["crowd"].AsGodotDictionary();
        Dictionary tiles = root["tiles"].AsGodotDictionary();
        Dictionary chase = root["camera_chase"].AsGodotDictionary();
        Array spawn = root["spawn"].AsGodotArray();
        Array creatures = root["creatures"].AsGodotArray();

        ReliefConfig config = new()
        {
            LevelHeight = F(root, "level_height"),
            Spawn = new Vector2((float)spawn[0].AsDouble(), (float)spawn[1].AsDouble()),
            Arena = root["arena"].AsStringArray(),
            Character = root["character"].AsString(),
            Pace = F(player, "pace"),
            AccelerationSeconds = F(player, "acceleration_seconds"),
            BrakingSeconds = F(player, "braking_seconds"),
            PlayerRadius = F(player, "radius"),
            Step = F(player, "step"),
            PlayerHp = F(player, "hp"),
            HurtInvulnerable = F(player, "hurt_invulnerable_seconds"),
            JumpCooldown = F(player, "jump_cooldown"),
            JumpInvulnerable = F(player, "jump_invulnerable_seconds"),
            AirControl = F(player, "air_control"),
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
            ChasePitch = F(chase, "pitch_degrees"),
            ChaseMinPitch = F(chase, "min_pitch_degrees"),
            ChaseMaxPitch = F(chase, "max_pitch_degrees"),
            ChaseDistance = F(chase, "distance"),
            ChaseClearance = F(chase, "ground_clearance"),
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
        // Sommet h atteint à mi-durée T : v = 4h / T, g = 8h / T².
        float jumpHeight = F(player, "jump_height");
        float jumpSeconds = F(player, "jump_seconds");
        config.JumpVelocity = 4f * jumpHeight / jumpSeconds;
        config.Gravity = 8f * jumpHeight / (jumpSeconds * jumpSeconds);
        config.SetCharacter(config.Character);
        config.LowPoly = LoadLowPoly(root["lowpoly"].AsGodotDictionary());
        return config;
    }

    /// <summary>
    /// Des corps plus grands occupent plus de place : le joueur et la horde grossissent de leur emprise au sol et de leur
    /// portée de contact, pour que les corps ne se traversent pas.
    /// </summary>
    public void ScaleBodies(float scale)
    {
        PlayerRadius *= scale;
        CrowdRadius *= scale;
        ContactReach *= scale;
        ContactHeight *= scale;
    }

    private const string CharactersPath = "res://data/characters/characters.json";
    // Un pas de la grille iso du jeu : la diagonale d'une cellule fait 64 px, son côté 45.
    private const float GamePixelsPerCell = 45.254834f;

    /// <summary>
    /// Le personnage joué : sa vitesse du jeu ramenée au rythme 3D, et sa capacité de déplacement (dash commun si son
    /// entrée n'en nomme pas).
    /// </summary>
    public void SetCharacter(string id)
    {
        using FileAccess file = FileAccess.Open(CharactersPath, FileAccess.ModeFlags.Read);
        Array characters = Json.ParseString(file.GetAsText()).AsGodotArray();
        foreach (Variant entry in characters)
        {
            Dictionary character = entry.AsGodotDictionary();
            if (character["id"].AsString() != id)
                continue;
            Character = id;
            PlayerSpeed = F(character["base_stats"].AsGodotDictionary(), "speed") / GamePixelsPerCell * Pace;
            Mobility = Vestiges.Infrastructure.MobilityConfig.Load(character.ContainsKey("mobility") ? character["mobility"].AsString() : null);
            return;
        }
        throw new System.InvalidOperationException($"Personnage inconnu : {id}");
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
            StartToon = start["toon"].AsBool(),
            OutlineColor = new Color(outline["color"].AsString()),
            OutlineSensitivity = F(outline, "sensitivity"),
            OutlineOrthoSensitivity = F(outline, "ortho_sensitivity"),
            MetresPerCell = F(lowPoly, "metres_per_cell"),
            BodyScale = F(lowPoly, "body_scale"),
            GrassPerCell = F(lowPoly["grass"].AsGodotDictionary(), "per_cell"),
            GrassHeight = F(lowPoly["grass"].AsGodotDictionary(), "height"),
            Ground = new Color(colors["ground"].AsString()),
            Terrace = new Color(colors["terrace"].AsString()),
            CarvedRelief = F(lowPoly, "carved_relief"),
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
