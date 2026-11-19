using Godot;
using Godot.Collections;

namespace Vestiges.World3D;

/// <summary>
/// Réglages du bac à sable de la verticalité (plan 59), lus dans <c>data/sandbox/relief.json</c>. Unités : une cellule
/// vaut 1, un étage vaut <see cref="LevelHeight"/>, les vitesses sont en cellules par seconde.
/// </summary>
public sealed class ReliefConfig
{
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
        // Agrandissement du monde autour des corps, décors, cellules et étages (plan 60 §28) ; --world-scale en jeu.
        public float WorldScale;
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
        // Erreur tolérée des niveaux de détail, en pixels (Viewport.MeshLodThreshold, plan 60 §19) ; 0 : toujours le plus fin.
        public float LodThreshold;
        // Rayon (cellules) du cylindre de la caméra au joueur où les décors s'effacent en trame.
        public float SeeThroughRadius;
    }

    /// <summary>Effets propres à Forward+ (plan 60 §26, lot 60.0f) : ombres de contact, brume volumétrique, lumières.</summary>
    public sealed class AtmosphereStyle
    {
        public bool StartSsao, StartFog, StartLights;
        public float SsaoRadius, SsaoIntensity, SsaoPower, SsaoDetail;
        public float FogDensity, FogAnisotropy, FogLength, FogAmbientInject;
        public Color FogAlbedo, FogEmission;
        public Color ProjectileColor;
        public float ProjectileEnergy, ProjectileRange;
        public float GlowEnergy, GlowRange, Bloom, BloomThreshold;
        public int MaxGlows;
    }

    public AtmosphereStyle Atmosphere;
    // Agrandissement du monde autour des corps (§28) ; 1 : l'échelle jouée depuis le §237.
    public float WorldScale = 1f;
    public float LevelHeight;
    public Vector2 Spawn;
    public string[] Arena;
    public string Character;
    // La terre dont viennent les décors et les sols de la terre d'essai (data/props, data/biomes) ; vide : le semis du réglage.
    public string Land;

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
    // Taille de la horde dans la run : au départ, en plus chaque minute, au plus (data/world3d/crowd.json).
    public int RunCrowdStart, RunCrowdMax;
    public float RunCrowdPerMinute;
    // Le joueur revenu du vide ou relevé trouve la horde écartée de ce rayon, de cette poussée.
    public float RespawnClearRadius, RespawnClearPush;
    public float SpawnMin, SpawnMax, CrowdRadius, Separation, ClimbSpeed, ContactReach, ContactHeight, ContactDamage;
    public float PushDecay, CrowdFallDamagePerLevel, CrowdSafeDropLevels;
    public Crowd3D.Species[] Creatures;
    public LowPolyStyle LowPoly;

    public float ChasePitch, ChaseMinPitch, ChaseMaxPitch, ChaseDistance;
    public float ChaseFov, ChaseHeight, ChaseMouseDegrees, ChaseClearance;

    public const string RenderPath = "res://data/world3d/render.json";
    public const string TestLandPath = "res://data/world3d/test_land.json";
    public const string CameraPath = "res://data/world3d/camera.json";
    public const string PlayerPath = "res://data/movement/player_3d.json";
    public const string CrowdPath = "res://data/world3d/crowd.json";

    private static Dictionary Read(string path)
    {
        using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read)
            ?? throw new System.IO.FileNotFoundException(path);
        return Json.ParseString(file.GetAsText()).AsGodotDictionary();
    }

    /// <summary>
    /// Les réglages de la 3D (rendu, terre d'essai, caméra, joueur, plan 60 §29) ; avec <paramref name="sandboxPath"/>, la
    /// horde d'essai et l'arme du bac à sable en plus.
    /// </summary>
    public static ReliefConfig Load(string sandboxPath = null)
    {
        Dictionary render = Read(RenderPath);
        Dictionary land = Read(TestLandPath);
        Dictionary chase = Read(CameraPath);
        Dictionary player = Read(PlayerPath);
        Dictionary crowd = Read(CrowdPath);
        Dictionary sandbox = sandboxPath != null ? Read(sandboxPath) : null;
        Array spawn = land["spawn"].AsGodotArray();

        ReliefConfig config = new()
        {
            LevelHeight = F(land, "level_height"),
            Spawn = new Vector2((float)spawn[0].AsDouble(), (float)spawn[1].AsDouble()),
            Arena = land["arena"].AsStringArray(),
            Land = land["land"].AsString(),
            Character = sandbox != null ? sandbox["character"].AsString() : "facteur",
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
            ChasePitch = F(chase, "pitch_degrees"),
            ChaseMinPitch = F(chase, "min_pitch_degrees"),
            ChaseMaxPitch = F(chase, "max_pitch_degrees"),
            ChaseDistance = F(chase, "distance"),
            ChaseClearance = F(chase, "ground_clearance"),
            ChaseFov = F(chase, "fov"),
            ChaseHeight = F(chase, "height"),
            ChaseMouseDegrees = F(chase, "mouse_degrees_per_pixel"),
            Creatures = System.Array.Empty<Crowd3D.Species>(),
        };
        config.LoadCrowd(crowd);
        if (sandbox != null)
            config.LoadSandbox(sandbox);
        // Sommet h atteint à mi-durée T : v = 4h / T, g = 8h / T².
        float jumpHeight = F(player, "jump_height");
        float jumpSeconds = F(player, "jump_seconds");
        config.JumpVelocity = 4f * jumpHeight / jumpSeconds;
        config.Gravity = 8f * jumpHeight / (jumpSeconds * jumpSeconds);
        config.SetCharacter(config.Character);
        config.LowPoly = LoadLowPoly(render, land);
        config.Atmosphere = LoadAtmosphere(render["atmosphere"].AsGodotDictionary());
        return config;
    }

    /// <summary>Les règles de la horde, communes au jeu et au bac à sable, et sa taille dans la run.</summary>
    private void LoadCrowd(Dictionary crowd)
    {
        Dictionary run = crowd["run"].AsGodotDictionary();
        SpawnMin = F(crowd, "spawn_min");
        SpawnMax = F(crowd, "spawn_max");
        CrowdRadius = F(crowd, "radius");
        Separation = F(crowd, "separation");
        ClimbSpeed = F(crowd, "climb_speed");
        ContactReach = F(crowd, "contact_reach");
        ContactHeight = F(crowd, "contact_height");
        PushDecay = F(crowd, "push_decay");
        CrowdFallDamagePerLevel = F(crowd, "fall_damage_per_level");
        CrowdSafeDropLevels = F(crowd, "safe_drop_levels");
        RespawnClearRadius = F(crowd, "respawn_clear_radius");
        RespawnClearPush = F(crowd, "respawn_clear_push");
        RunCrowdStart = (int)F(run, "start");
        RunCrowdPerMinute = F(run, "per_minute");
        RunCrowdMax = (int)F(run, "max");
    }

    /// <summary>La horde d'essai et l'arme du bac à sable.</summary>
    private void LoadSandbox(Dictionary sandbox)
    {
        Dictionary weapon = sandbox["weapon"].AsGodotDictionary();
        Dictionary crowd = sandbox["crowd"].AsGodotDictionary();
        Array creatures = sandbox["creatures"].AsGodotArray();
        WeaponInterval = F(weapon, "interval");
        WeaponRange = F(weapon, "range");
        WeaponDamage = F(weapon, "damage");
        WeaponSpeed = F(weapon, "speed");
        WeaponHitRadius = F(weapon, "hit_radius");
        WeaponPush = F(weapon, "push");
        CrowdCount = (int)F(crowd, "count");
        ContactDamage = F(crowd, "contact_damage");
        Creatures = new Crowd3D.Species[creatures.Count];
        for (int i = 0; i < creatures.Count; i++)
        {
            Dictionary creature = creatures[i].AsGodotDictionary();
            Creatures[i] = new Crowd3D.Species
            {
                Id = creature["id"].AsString(),
                Speed = F(creature, "speed"),
                Hp = F(creature, "hp"),
                ContactDamage = ContactDamage,
                Weight = F(creature, "weight"),
            };
        }
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

    /// <summary>
    /// Agrandit le monde autour des corps (plan 60 §28) : la carte, les étages et les décors gardent leur taille en
    /// cellules, et tout ce qui se mesure au corps rapetisse d'autant (taille, vitesses, saut, portées, caméra). À l'écran,
    /// les corps ne changent pas ; les décors, les cellules et les étages grandissent autour d'eux. À appeler avant
    /// <see cref="ScaleBodies"/>.
    /// </summary>
    public void ScaleWorld(float scale)
    {
        WorldScale = scale;
        float body = 1f / scale;
        LowPoly.BodyScale *= body;
        Pace *= body;
        SetCharacter(Character);
        foreach (Crowd3D.Species creature in Creatures)
            creature.Speed *= body;
        ClimbSpeed *= body;
        JumpVelocity *= body;
        Gravity *= body;
        WeaponRange *= body;
        WeaponSpeed *= body;
        WeaponHitRadius *= body;
        WeaponPush *= body;
        StompRadius *= body;
        StompPush *= body;
        SpawnMin *= body;
        SpawnMax *= body;
        ChaseDistance *= body;
        ChaseHeight *= body;
        ChaseClearance *= body;
        LowPoly.SeeThroughRadius *= body;
    }

    private const string CharactersPath = "res://data/characters/characters.json";
    // Un pas de la grille iso du jeu : la diagonale d'une cellule fait 64 px, son côté 45.
    private const float GamePixelsPerCell = 45.254834f;

    /// <summary>
    /// Le facteur unique des réglages du jeu 2D vers la 3D (plan 60 §29) : une longueur en pixels du jeu, ou une vitesse en
    /// px/s, devient des cellules (par seconde). Il suit le rythme joué depuis le §236 : 45,25 px par cellule 2D, ramenés
    /// par <see cref="Pace"/>, qui porte aussi l'agrandissement du monde (<see cref="ScaleWorld"/>) ; 91 px par cellule
    /// à ×1,35.
    /// </summary>
    public float CellsPerPixel => Pace / GamePixelsPerCell;

    /// <summary>Une cellule en mètres mesurés au corps (§253) : 3,58 m à ×1,35. Pour lire et rapporter, jamais pour simuler.</summary>
    public float MetresPerCellAtBody => LowPoly.MetresPerCell * WorldScale;

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
            PlayerSpeed = F(character["base_stats"].AsGodotDictionary(), "speed") * CellsPerPixel;
            Mobility = Vestiges.Infrastructure.MobilityConfig.Load(character.ContainsKey("mobility") ? character["mobility"].AsString() : null);
            return;
        }
        throw new System.InvalidOperationException($"Personnage inconnu : {id}");
    }

    private static AtmosphereStyle LoadAtmosphere(Dictionary atmosphere)
    {
        Dictionary start = atmosphere["start"].AsGodotDictionary();
        Dictionary ssao = atmosphere["ssao"].AsGodotDictionary();
        Dictionary fog = atmosphere["fog"].AsGodotDictionary();
        Dictionary lights = atmosphere["lights"].AsGodotDictionary();
        return new AtmosphereStyle
        {
            StartSsao = start["ssao"].AsBool(),
            StartFog = start["fog"].AsBool(),
            StartLights = start["lights"].AsBool(),
            SsaoRadius = F(ssao, "radius"),
            SsaoIntensity = F(ssao, "intensity"),
            SsaoPower = F(ssao, "power"),
            SsaoDetail = F(ssao, "detail"),
            FogDensity = F(fog, "density"),
            FogAnisotropy = F(fog, "anisotropy"),
            FogLength = F(fog, "length"),
            FogAmbientInject = F(fog, "ambient_inject"),
            FogAlbedo = new Color(fog["albedo"].AsString()),
            FogEmission = new Color(fog["emission"].AsString()),
            ProjectileColor = new Color(lights["projectile_color"].AsString()),
            ProjectileEnergy = F(lights, "projectile_energy"),
            ProjectileRange = F(lights, "projectile_range"),
            GlowEnergy = F(lights, "glow_energy"),
            GlowRange = F(lights, "glow_range"),
            MaxGlows = (int)F(lights, "max_glows"),
            Bloom = F(lights, "bloom"),
            BloomThreshold = F(lights, "bloom_threshold"),
        };
    }

    private static LowPolyStyle LoadLowPoly(Dictionary lowPoly, Dictionary land)
    {
        Array scatter = land["scatter"].AsGodotArray();
        Array landmarks = land["landmarks"].AsGodotArray();
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
            WorldScale = F(lowPoly, "world_scale"),
            GrassPerCell = F(lowPoly["grass"].AsGodotDictionary(), "per_cell"),
            GrassHeight = F(lowPoly["grass"].AsGodotDictionary(), "height"),
            CarvedRelief = F(lowPoly, "carved_relief"),
            LodThreshold = F(lowPoly, "lod_threshold"),
            SeeThroughRadius = F(lowPoly, "see_through_radius"),
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
