using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Faune qui fuit (plan 39 B2) : corbeaux, pigeons, rats, papillons et grenouilles, non hostiles et sans collision. Un seul
/// gestionnaire et un pool de sprites sans script (règle des rappels C# par nœud, plan 29) : les bêtes naissent hors de la
/// vue, selon le biome, jamais dans une zone qui s'efface, puis s'envolent ou filent quand on approche, quand une créature
/// meurt près d'elles ou quand l'Effacement arrive, à l'opposé du joueur et vers la mémoire la plus forte. Réglages :
/// <c>data/world/fauna.json</c> ; poses et pieds : <c>assets/fauna</c>.
/// </summary>
public partial class FaunaDirector : Node2D
{
    private static readonly string[] Directions = { "E", "SE", "S", "SW", "W", "NW", "N", "NE" };
    // Au-dessus de cette hauteur, une bête en vol passe devant les canopées (z 100, couches d'AGENTS.md).
    private const float CanopyHeightPx = 26f;
    private const int CanopyZIndex = 101;
    // Écart des sondes du gradient de mémoire : une zone de l'Effacement fait 128 px.
    private const float MemoryProbePx = 96f;
    private const float GroundRadiusPx = 6f;

    /// <summary>Espèce prête à jouer : poses par direction pour le repos et la fuite, pied des poses.</summary>
    private sealed class Kind
    {
        public FaunaSpecies Species;
        public Texture2D[][] Rest;
        public Texture2D[][] Flee;
        public Vector2 Pivot;
        public bool OnGround;
    }

    private sealed class Critter
    {
        public Sprite2D Sprite;
        public Kind Kind;
        public Vector2 Ground;
        public Vector2 Home;
        public Vector2 Velocity;
        public int Direction;
        public float Height;
        public float AnimTime;
        public float Travelled;
        public float Reaction;
        public float MemoryTimer;
        public float Fade;
        public bool Fleeing;
        public bool Active;
        // Dernières valeurs écrites dans le sprite : le pont natif n'est appelé que si l'une change.
        public Texture2D ShownTexture;
        public Vector2 ShownPosition;
        public int ShownHeight;
        public int ShownZ;
    }

    private FaunaConfig _config;
    private WorldSetup _world;
    private Node2D _player;
    private ErasureManager _erasure;
    private bool _erasureSearched;
    private EventBus _eventBus;
    private GameManager _gameManager;
    private RandomNumberGenerator _rng;
    private readonly List<Critter> _critters = new();
    private readonly List<Kind> _kinds = new();
    private readonly List<Vector2> _noises = new();
    private float _spawnTimer;
    private int _totalSpawned;
    private int _totalFled;

    public override void _Ready()
    {
        if (!FaunaConfig.TryLoad(out _config, out string error))
        {
            GD.PushError($"[FaunaDirector] {error} : run sans faune");
            SetProcess(false);
            return;
        }
        if (!LoadKinds())
        {
            GD.PushError("[FaunaDirector] aucune espèce jouable : run sans faune");
            SetProcess(false);
            return;
        }
        YSortEnabled = true;
        _world = GetParent<WorldSetup>();
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _gameManager = GetNode<GameManager>("/root/GameManager");
        // Flux à part : la faune ne décale aucun tirage du jeu.
        _rng = RunRandom.Create("fauna");
        for (int i = 0; i < _config.MaxAlive; i++)
        {
            Sprite2D sprite = new() { Centered = false, TextureFilter = TextureFilterEnum.Nearest, Visible = false };
            AddChild(sprite);
            _critters.Add(new Critter { Sprite = sprite });
        }
        _eventBus.EnemyKilled += OnEnemyKilled;
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
            _eventBus.EnemyKilled -= OnEnemyKilled;
    }

    public override void _Process(double delta)
    {
        if (_gameManager.CurrentState != GameManager.GameState.Run)
            return;
        if (_player == null || !IsInstanceValid(_player))
        {
            _player = _world.GetNodeOrNull<Node2D>("Player");
            if (_player == null)
                return;
        }
        if (!_erasureSearched)
        {
            _erasure = _world.GetNodeOrNull<ErasureManager>("ErasureManager");
            _erasureSearched = true;
        }
        float dt = (float)delta;
        _spawnTimer -= dt;
        if (_spawnTimer <= 0f)
        {
            _spawnTimer = _config.SpawnIntervalS;
            TrySpawnGroup();
        }
        Vector2 playerPosition = _player.GlobalPosition;
        foreach (Critter critter in _critters)
        {
            if (critter.Active)
                UpdateCritter(critter, playerPosition, dt);
        }
        _noises.Clear();
    }

    private void UpdateCritter(Critter critter, Vector2 playerPosition, float dt)
    {
        Kind kind = critter.Kind;
        FaunaSpecies species = kind.Species;
        Vector2 fromPlayer = critter.Ground - playerPosition;
        if (fromPlayer.LengthSquared() > _config.DespawnDistancePx * _config.DespawnDistancePx)
        {
            Deactivate(critter);
            return;
        }
        if (!critter.Fleeing && ShouldFlee(critter, fromPlayer, dt))
            StartFleeing(critter, fromPlayer);

        if (critter.Fleeing)
        {
            critter.Reaction -= dt;
            if (critter.Reaction <= 0f)
            {
                Vector2 before = critter.Ground;
                critter.Ground += critter.Velocity * dt;
                // Au sol, la bête longe les murs au lieu de les traverser.
                if (kind.OnGround)
                    ObstacleField.Resolve(ref critter.Ground, GroundRadiusPx);
                critter.Travelled += before.DistanceTo(critter.Ground);
                critter.Height += species.ClimbPxPerSecond * dt;
                if (critter.Travelled > species.VanishAfterPx)
                {
                    critter.Fade -= dt / _config.FadeS;
                    if (critter.Fade <= 0f)
                    {
                        Deactivate(critter);
                        return;
                    }
                    critter.Sprite.Modulate = new Color(1f, 1f, 1f, critter.Fade);
                }
            }
        }
        else if (species.WanderPx > 0f)
        {
            // Errance au repos : une boucle lente autour du point de naissance.
            float t = critter.AnimTime * 0.35f;
            Vector2 next = critter.Home + new Vector2(Mathf.Sin(t * 1.3f), Mathf.Sin(t * 0.9f + 1.1f) * 0.5f) * species.WanderPx;
            critter.Direction = DirectionOf(next - critter.Ground, critter.Direction);
            critter.Ground = next;
        }

        critter.AnimTime += dt;
        Texture2D[] poses = (critter.Fleeing && critter.Reaction <= 0f ? kind.Flee : kind.Rest)[critter.Direction];
        Texture2D texture = poses[(int)(critter.AnimTime * species.Fps) % poses.Length];
        if (texture != critter.ShownTexture)
        {
            critter.ShownTexture = texture;
            critter.Sprite.Texture = texture;
        }
        // Pied sur un pixel entier : la bête ne tremble pas sur la grille.
        Vector2 position = critter.Ground.Round();
        if (position != critter.ShownPosition)
        {
            critter.ShownPosition = position;
            critter.Sprite.Position = position;
        }
        int height = Mathf.RoundToInt(critter.Height);
        if (height != critter.ShownHeight)
        {
            critter.ShownHeight = height;
            critter.Sprite.Offset = -kind.Pivot - new Vector2(0f, height);
        }
        int z = critter.Height > CanopyHeightPx ? CanopyZIndex : 0;
        if (z != critter.ShownZ)
        {
            critter.ShownZ = z;
            critter.Sprite.ZIndex = z;
        }
    }

    private bool ShouldFlee(Critter critter, Vector2 fromPlayer, float dt)
    {
        float radius = critter.Kind.Species.FleeRadiusPx;
        // Distance au sol : un rang vaut une demi-colonne en 2:1.
        if (new Vector2(fromPlayer.X, fromPlayer.Y * 2f).LengthSquared() < radius * radius)
            return true;
        float noiseRadius = _config.NoiseRadiusPx * _config.NoiseRadiusPx;
        foreach (Vector2 noise in _noises)
            if (critter.Ground.DistanceSquaredTo(noise) < noiseRadius)
                return true;
        // La mémoire change par zones de 128 px : une lecture toutes les memory_check_s suffit, décalée d'une bête à l'autre.
        critter.MemoryTimer -= dt;
        if (critter.MemoryTimer > 0f || _erasure == null)
            return false;
        critter.MemoryTimer = _config.MemoryCheckS;
        return _erasure.GetMemoryAt(critter.Ground) < _config.MinMemory;
    }

    /// <summary>Loin du joueur et vers la mémoire la plus forte : les bêtes fuient l'Effacement.</summary>
    private void StartFleeing(Critter critter, Vector2 fromPlayer)
    {
        Vector2 away = fromPlayer.LengthSquared() > 1f ? fromPlayer.Normalized() : Vector2.Right;
        Vector2 direction = (away + MemoryGradient(critter.Ground) * _config.ErasurePull).Normalized();
        // Un peu d'écart par bête : le groupe s'égaille au lieu de partir en rang.
        direction = direction.Rotated(_rng.RandfRange(-_config.ScatterRad, _config.ScatterRad));
        critter.Fleeing = true;
        _totalFled++;
        critter.Velocity = direction * critter.Kind.Species.FleeSpeedPxPerSecond * _rng.RandfRange(0.85f, 1.2f);
        critter.Direction = DirectionOf(direction, critter.Direction);
        // Temps de réaction propre à chaque bête : un groupe ne décolle pas comme un seul oiseau.
        critter.Reaction = _rng.RandfRange(0f, _config.ReactionMaxS);
        critter.AnimTime = 0f;
    }

    private Vector2 MemoryGradient(Vector2 at)
    {
        if (_erasure == null)
            return Vector2.Zero;
        Vector2 gradient = new(
            _erasure.GetMemoryAt(at + new Vector2(MemoryProbePx, 0f)) - _erasure.GetMemoryAt(at - new Vector2(MemoryProbePx, 0f)),
            _erasure.GetMemoryAt(at + new Vector2(0f, MemoryProbePx)) - _erasure.GetMemoryAt(at - new Vector2(0f, MemoryProbePx)));
        return gradient.LengthSquared() > 0.0001f ? gradient.Normalized() : Vector2.Zero;
    }

    /// <summary>
    /// Un groupe naît au-delà d'un bord de la vue réelle (zoom compris), tiré au hasard : jamais sous les yeux du
    /// joueur, pas même dans les coins.
    /// </summary>
    private void TrySpawnGroup()
    {
        int free = 0;
        foreach (Critter critter in _critters)
            if (!critter.Active)
                free++;
        if (free == 0)
            return;
        Transform2D canvas = GetViewport().GetCanvasTransform();
        Vector2 halfView = GetViewport().GetVisibleRect().Size / canvas.Scale * 0.5f;
        Vector2 viewCenter = -canvas.Origin / canvas.Scale + halfView;
        float margin = _rng.RandfRange(_config.SpawnMarginMinPx, _config.SpawnMarginMaxPx);
        float along = _rng.RandfRange(-1f, 1f);
        Vector2 offset = _rng.RandiRange(0, 3) switch
        {
            0 => new Vector2(halfView.X + margin, along * (halfView.Y + margin)),
            1 => new Vector2(-halfView.X - margin, along * (halfView.Y + margin)),
            2 => new Vector2(along * (halfView.X + margin), halfView.Y + margin),
            _ => new Vector2(along * (halfView.X + margin), -halfView.Y - margin),
        };
        Vector2 center = viewCenter + offset;
        BiomeData biome = _world.GetBiomeAt(center);
        if (biome == null || (_erasure != null && _erasure.GetMemoryAt(center) < _config.MinMemory))
            return;
        Kind kind = PickKind(biome.Id);
        if (kind == null)
            return;
        int size = Mathf.Min(free, _rng.RandiRange(kind.Species.GroupMin, kind.Species.GroupMax));
        float spread = _config.GroupSpreadPx;
        for (int i = 0; i < size; i++)
        {
            Vector2 spot = center + new Vector2(_rng.RandfRange(-spread, spread), _rng.RandfRange(-spread, spread) * 0.5f);
            if (IsFree(spot, kind))
                Spawn(kind, spot);
        }
    }

    /// <summary>Hors des décors bloquants ; au sec pour tous, sauf la grenouille qui aime le bord de l'eau.</summary>
    private bool IsFree(Vector2 spot, Kind kind)
    {
        Vector2 probe = spot;
        if (ObstacleField.Resolve(ref probe, GroundRadiusPx))
            return false;
        return kind.Species.Id == "grenouille" || !_world.IsWaterAt(spot);
    }

    private Kind PickKind(string biomeId)
    {
        float total = 0f;
        foreach (Kind kind in _kinds)
            total += kind.Species.BiomeWeights.GetValueOrDefault(biomeId);
        if (total <= 0f)
            return null;
        float roll = _rng.Randf() * total;
        foreach (Kind kind in _kinds)
        {
            roll -= kind.Species.BiomeWeights.GetValueOrDefault(biomeId);
            if (roll < 0f)
                return kind;
        }
        return null;
    }

    private void Spawn(Kind kind, Vector2 spot)
    {
        foreach (Critter critter in _critters)
        {
            if (critter.Active)
                continue;
            critter.Kind = kind;
            critter.Ground = spot;
            critter.Home = spot;
            critter.Height = 0f;
            critter.Velocity = Vector2.Zero;
            critter.Travelled = 0f;
            critter.Reaction = 0f;
            critter.MemoryTimer = _rng.Randf() * _config.MemoryCheckS;
            critter.Fade = 1f;
            critter.Fleeing = false;
            critter.AnimTime = _rng.Randf() * 4f;
            // Au repos, de profil ou de trois quarts : la silhouette se lit mieux que de face.
            critter.Direction = _rng.RandiRange(0, 3) switch { 0 => 0, 1 => 1, 2 => 4, _ => 3 };
            critter.ShownTexture = null;
            critter.ShownPosition = new Vector2(float.NaN, float.NaN);
            critter.ShownHeight = int.MinValue;
            critter.ShownZ = int.MinValue;
            critter.Sprite.Modulate = Colors.White;
            critter.Sprite.Visible = true;
            critter.Active = true;
            _totalSpawned++;
            return;
        }
    }

    private static void Deactivate(Critter critter)
    {
        critter.Active = false;
        critter.Sprite.Visible = false;
    }

    private void OnEnemyKilled(string enemyId, Vector2 position) => _noises.Add(position);

    private static int DirectionOf(Vector2 vector, int fallback)
    {
        if (vector.LengthSquared() < 0.0001f)
            return fallback;
        return Mathf.PosMod(Mathf.RoundToInt(Mathf.Atan2(vector.Y, vector.X) / (Mathf.Pi / 4f)), 8);
    }

    /// <summary>
    /// Poses et pied de chaque espèce, résolus une fois. Une pose ou un pied manquant est signalé et l'espèce retirée,
    /// plutôt qu'une bête invisible ou décalée du sol. Faux si aucune espèce n'est jouable.
    /// </summary>
    private bool LoadKinds()
    {
        Dictionary<string, Vector2> pivots = ReadPivots();
        foreach (FaunaSpecies species in _config.AllSpecies)
        {
            if (!pivots.TryGetValue(species.Id, out Vector2 pivot))
            {
                GD.PushError($"[FaunaDirector] {species.Id} : pied absent de {FaunaConfig.ManifestPath}, espèce retirée");
                continue;
            }
            Texture2D[][] rest = LoadPoses(species.Id, species.RestAction);
            Texture2D[][] flee = LoadPoses(species.Id, species.FleeAction);
            if (rest == null || flee == null)
                continue;
            _kinds.Add(new Kind { Species = species, Rest = rest, Flee = flee, Pivot = pivot, OnGround = species.ClimbPxPerSecond <= 0f });
        }
        return _kinds.Count > 0;
    }

    private static Texture2D[][] LoadPoses(string id, string action)
    {
        Texture2D[][] byDirection = new Texture2D[Directions.Length][];
        for (int direction = 0; direction < Directions.Length; direction++)
        {
            List<Texture2D> frames = new();
            for (int index = 1; ; index++)
            {
                string path = $"res://assets/fauna/{id}/fauna_{id}_{Directions[direction]}_{action}_{index:00}.png";
                if (!ResourceLoader.Exists(path))
                    break;
                frames.Add(GD.Load<Texture2D>(path));
            }
            if (frames.Count == 0)
            {
                GD.PushError($"[FaunaDirector] {id} : aucune pose « {action} » {Directions[direction]}, espèce retirée");
                return null;
            }
            byDirection[direction] = frames.ToArray();
        }
        return byDirection;
    }

    /// <summary>Pied des poses par espèce, écrit par tools/generate_fauna.py : il tombe sur le point au sol.</summary>
    private static Dictionary<string, Vector2> ReadPivots()
    {
        Dictionary<string, Vector2> pivots = new();
        using FileAccess file = FileAccess.Open(FaunaConfig.ManifestPath, FileAccess.ModeFlags.Read);
        if (file == null || Json.ParseString(file.GetAsText()).Obj is not Godot.Collections.Dictionary manifest)
        {
            GD.PushError($"[FaunaDirector] {FaunaConfig.ManifestPath} illisible");
            return pivots;
        }
        foreach ((Variant key, Variant value) in manifest)
        {
            if (value.Obj is Godot.Collections.Dictionary entry && entry.TryGetValue("pivot", out Variant pivotValue)
                && pivotValue.Obj is Godot.Collections.Array pivot && pivot.Count == 2)
                pivots[key.AsString()] = new Vector2((float)pivot[0].AsDouble(), (float)pivot[1].AsDouble());
        }
        return pivots;
    }

#if TOOLS
    /// <summary>Bêtes nées depuis le début de la run (bancs, mesures).</summary>
    internal int TotalSpawned
    {
        get { DevelopmentMode.RequireTestAccess(); return _totalSpawned; }
    }

    /// <summary>Fuites déclenchées depuis le début de la run (bancs, mesures).</summary>
    internal int TotalFled
    {
        get { DevelopmentMode.RequireTestAccess(); return _totalFled; }
    }

    /// <summary>Bêtes en vie (bancs, mesures).</summary>
    internal int AliveCount
    {
        get
        {
            DevelopmentMode.RequireTestAccess();
            int count = 0;
            foreach (Critter critter in _critters)
                if (critter.Active)
                    count++;
            return count;
        }
    }

    /// <summary>Bêtes en vie dont le pied est dans <paramref name="view"/> (mesures).</summary>
    internal int CountVisible(Rect2 view)
    {
        DevelopmentMode.RequireTestAccess();
        int count = 0;
        foreach (Critter critter in _critters)
            if (critter.Active && view.HasPoint(critter.Ground))
                count++;
        return count;
    }

    /// <summary>Corps de la bête en vie la plus proche de <paramref name="from"/> dans <paramref name="view"/> (captures).</summary>
    internal bool TryGetNearestVisible(Rect2 view, Vector2 from, out Vector2 position)
    {
        DevelopmentMode.RequireTestAccess();
        position = Vector2.Zero;
        float best = float.MaxValue;
        foreach (Critter critter in _critters)
        {
            if (!critter.Active || !view.HasPoint(critter.Ground))
                continue;
            float distance = critter.Ground.DistanceSquaredTo(from);
            if (distance < best)
            {
                best = distance;
                position = critter.Ground - new Vector2(0f, critter.Height);
            }
        }
        return best < float.MaxValue;
    }
#endif
}
