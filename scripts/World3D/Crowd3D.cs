using Godot;

namespace Vestiges.World3D;

/// <summary>
/// La horde sur le relief 3D (plan 60, 60.2a), sans nœud ni rendu : elle apparaît autour du joueur, fonce sur lui, se
/// sépare par une grille de voisinage chaînée, grimpe lentement les falaises, longe le vide, tombe quand on la pousse et
/// touche au contact, à hauteur égale. Aucune allocation par pas : tableaux de taille fixe.
/// Repère du relief : sol (x, y) en cellules, hauteur z. La run 3D et le bac à sable partagent cette horde.
/// </summary>
public sealed class Crowd3D
{
    public enum Life : byte { Inactive, Alive, Dying }

    public enum Cause : byte { Hit, Stomp, Fall, Void }

    /// <summary>
    /// Une sorte de créature : son modèle, sa vitesse (cellules/s, au corps), ses PV, son coup au contact, son poids au
    /// tirage et la minute de run d'où elle apparaît.
    /// </summary>
    public sealed class Species
    {
        public string Id;
        public float Speed;
        public float Hp;
        public float ContactDamage;
        public float Weight;
        public float FromMinute;
        // Le modèle hors de assets/models (bibliothèque de l'export de masse), chemin sans extension ; null : assets/models/creatures/<Id>.
        public string ModelRoot;
    }

    private const int MaxSpawnsPerStep = 4;
    /// <summary>Durée de la pose de mort.</summary>
    public const float DyingSeconds = 0.45f;
    /// <summary>Durée de la pose d'attaque, lancée par un coup au contact.</summary>
    public const float StrikeSeconds = 0.4f;
    private const float FlashSeconds = 0.12f;
    private const float VoidFloor = -5f;
    private const float SameLevel = 0.5f;
    private const float SpawnClearance = 4f;

    private readonly ReliefConfig _config;
    private readonly ReliefArena _arena;
    private readonly Species[] _species;
    private readonly RandomNumberGenerator _rng = new();
    private readonly int[] _bucketHead;
    private readonly int[] _bucketNext;

    public readonly Vector3[] Position;
    public readonly Vector2[] Facing;
    public readonly Life[] State;
    public readonly int[] Kind;
    public readonly float[] Timer;
    public readonly bool[] Climbing;
    public readonly bool[] Grounded;
    public readonly float[] Flash;
    /// <summary>Temps restant de la pose d'attaque, lancée par un coup au contact.</summary>
    public readonly float[] Strike;
    private readonly float[] _hp;
    private readonly float[] _vz;
    private readonly float[] _takeoff;
    private readonly float[] _climbTarget;
    // La chute en cours vient d'une poussée : elle seule blesse. Une créature qui saute d'elle-même d'un gradin n'a rien.
    private readonly bool[] _pushedFall;
    private readonly Vector2[] _push;

    public int Kills, KillsByFall, KillsByVoid, KillsByStomp, ContactHits;

    public Crowd3D(ReliefConfig config, ReliefArena arena, Species[] species, int capacity, ulong seed)
    {
        _config = config;
        _arena = arena;
        _species = species;
        _rng.Seed = seed;
        Position = new Vector3[capacity];
        Facing = new Vector2[capacity];
        State = new Life[capacity];
        Kind = new int[capacity];
        Timer = new float[capacity];
        Climbing = new bool[capacity];
        Grounded = new bool[capacity];
        Flash = new float[capacity];
        Strike = new float[capacity];
        _hp = new float[capacity];
        _vz = new float[capacity];
        _takeoff = new float[capacity];
        _climbTarget = new float[capacity];
        _pushedFall = new bool[capacity];
        _push = new Vector2[capacity];
        _bucketHead = new int[arena.Width * arena.Depth];
        _bucketNext = new int[capacity];
    }

    public int Capacity => Position.Length;


    /// <summary>Créatures vivantes ou mourantes, la place qu'elles tiennent dans la horde.</summary>
    public int Present
    {
        get
        {
            int count = 0;
            for (int i = 0; i < Capacity; i++)
            {
                if (State[i] != Life.Inactive)
                    count++;
            }
            return count;
        }
    }

    /// <summary>
    /// Remplit la horde jusqu'à <paramref name="target"/>, des sortes déjà venues à <paramref name="minutes"/> de run,
    /// quelques créatures à la fois ; une fois par image, pas à chaque pas.
    /// </summary>
    public void Fill(PlayerMotor3D player, int target, float minutes) => Spawn(player, target, minutes);

    /// <summary>Un pas : chaque créature vers le joueur.</summary>
    public void Step(float dt, PlayerMotor3D player)
    {
        RebuildBuckets();
        float decay = Mathf.Exp(-_config.PushDecay * dt);
        for (int i = 0; i < Capacity; i++)
            StepOne(i, dt, decay, player);
    }

    /// <summary>Retombée du joueur : onde qui blesse et repousse la horde au même niveau, qui peut la jeter du bord.</summary>
    public void Stomp(Vector3 at, Vector2 fallback)
    {
        ReliefConfig c = _config;
        float radiusSq = c.StompRadius * c.StompRadius;
        for (int i = 0; i < Capacity; i++)
        {
            if (State[i] != Life.Alive || Mathf.Abs(Position[i].Z - at.Z) > SameLevel + 0.1f)
                continue;
            Vector2 away = new(Position[i].X - at.X, Position[i].Y - at.Y);
            float distSq = away.LengthSquared();
            if (distSq > radiusSq)
                continue;
            Vector2 dir = distSq > 0.0001f ? away / Mathf.Sqrt(distSq) : fallback;
            Push(i, dir * c.StompPush * (1f - Mathf.Sqrt(distSq) / c.StompRadius * 0.5f));
            Damage(i, c.StompDamage, Cause.Stomp);
        }
    }

    /// <summary>Repousse la horde proche d'un point (le joueur qui revient au dernier sol sûr, qui renaît).</summary>
    public void Repel(Vector2 at, float radius, float strength)
    {
        for (int i = 0; i < Capacity; i++)
        {
            if (State[i] != Life.Alive)
                continue;
            Vector2 away = new(Position[i].X - at.X, Position[i].Y - at.Y);
            if (away.LengthSquared() < radius * radius && away.LengthSquared() > 0.0001f)
                Push(i, away.Normalized() * strength);
        }
    }

    public void Push(int i, Vector2 impulse)
    {
        _push[i] += impulse;
        // Poussée en l'air, la chute devient la sienne : elle blessera.
        if (!Grounded[i])
            _pushedFall[i] = true;
    }

    public void Damage(int i, float damage, Cause cause)
    {
        if (State[i] != Life.Alive)
            return;
        _hp[i] -= damage;
        Flash[i] = FlashSeconds;
        if (_hp[i] <= 0f)
            Kill(i, cause);
    }

    /// <summary>La créature vivante la plus proche de <paramref name="from"/> dans <paramref name="range"/>, ou -1.</summary>
    public int Nearest(Vector3 from, float range)
    {
        float best = range * range;
        int target = -1;
        for (int i = 0; i < Capacity; i++)
        {
            if (State[i] != Life.Alive)
                continue;
            float d = (Position[i] - from).LengthSquared();
            if (d < best)
            {
                best = d;
                target = i;
            }
        }
        return target;
    }

    private void Spawn(PlayerMotor3D player, int target, float minutes)
    {
        float weights = 0f;
        foreach (Species s in _species)
        {
            if (s.FromMinute <= minutes)
                weights += s.Weight;
        }
        if (weights <= 0f)
            return;
        int present = Present;
        int spawned = 0;
        for (int i = 0; i < Capacity && present < target && spawned < MaxSpawnsPerStep; i++)
        {
            if (State[i] != Life.Inactive)
                continue;
            for (int attempt = 0; attempt < 8; attempt++)
            {
                float angle = _rng.Randf() * Mathf.Tau;
                float distance = _rng.RandfRange(_config.SpawnMin, _config.SpawnMax);
                float x = player.Position.X + Mathf.Cos(angle) * distance;
                float y = player.Position.Y + Mathf.Sin(angle) * distance;
                float ground = _arena.GroundAt(x, y);
                // Ni dans le vide, ni sur un toit hors d'atteinte : une créature naît là où elle peut marcher.
                if (ReliefArena.IsVoid(ground) || Mathf.Abs(ground - player.Position.Z) > SpawnClearance * _arena.LevelHeight)
                    continue;
                Activate(i, new Vector3(x, y, ground), PickKind(weights, minutes));
                present++;
                spawned++;
                break;
            }
        }
    }

    private int PickKind(float weights, float minutes)
    {
        float roll = _rng.Randf() * weights;
        int last = 0;
        for (int k = 0; k < _species.Length; k++)
        {
            if (_species[k].FromMinute > minutes)
                continue;
            last = k;
            roll -= _species[k].Weight;
            if (roll < 0f)
                return k;
        }
        return last;
    }

    private void Activate(int i, Vector3 at, int kind)
    {
        State[i] = Life.Alive;
        Kind[i] = kind;
        Position[i] = at;
        Facing[i] = new Vector2(1f, 1f);
        Climbing[i] = false;
        Grounded[i] = true;
        Flash[i] = 0f;
        Strike[i] = 0f;
        Timer[i] = 0f;
        _hp[i] = _species[kind].Hp;
        _vz[i] = 0f;
        _push[i] = Vector2.Zero;
    }

    private void RebuildBuckets()
    {
        System.Array.Fill(_bucketHead, -1);
        for (int i = 0; i < Capacity; i++)
        {
            if (State[i] != Life.Alive)
                continue;
            int cx = Mathf.FloorToInt(Position[i].X);
            int cy = Mathf.FloorToInt(Position[i].Y);
            if (cx < 0 || cy < 0 || cx >= _arena.Width || cy >= _arena.Depth)
            {
                _bucketNext[i] = -1;
                continue;
            }
            int bucket = cy * _arena.Width + cx;
            _bucketNext[i] = _bucketHead[bucket];
            _bucketHead[bucket] = i;
        }
    }

    private Vector2 SeparationOf(int i)
    {
        float diameter = _config.CrowdRadius * 2f;
        Vector3 me = Position[i];
        int cx = Mathf.FloorToInt(me.X);
        int cy = Mathf.FloorToInt(me.Y);
        Vector2 push = Vector2.Zero;
        for (int by = cy - 1; by <= cy + 1; by++)
        {
            for (int bx = cx - 1; bx <= cx + 1; bx++)
            {
                if (bx < 0 || by < 0 || bx >= _arena.Width || by >= _arena.Depth)
                    continue;
                for (int j = _bucketHead[by * _arena.Width + bx]; j >= 0; j = _bucketNext[j])
                {
                    if (j == i || Mathf.Abs(Position[j].Z - me.Z) > SameLevel)
                        continue;
                    Vector2 d = new(me.X - Position[j].X, me.Y - Position[j].Y);
                    float len = d.Length();
                    if (len >= diameter)
                        continue;
                    push += len > 0.0001f ? d / len * (1f - len / diameter) : new Vector2(((i * 7) % 3) - 1f, 0.5f);
                }
            }
        }
        return push;
    }

    private void StepOne(int i, float dt, float decay, PlayerMotor3D player)
    {
        ReliefConfig c = _config;
        Flash[i] = Mathf.Max(0f, Flash[i] - dt);
        Strike[i] = Mathf.Max(0f, Strike[i] - dt);
        if (State[i] == Life.Dying)
        {
            Timer[i] -= dt;
            if (Timer[i] <= 0f)
                State[i] = Life.Inactive;
            return;
        }
        if (State[i] != Life.Alive)
            return;
        Timer[i] += dt;
        Vector3 pos = Position[i];
        Species kind = _species[Kind[i]];
        Vector2 toPlayer = new(player.Position.X - pos.X, player.Position.Y - pos.Y);
        float dist = toPlayer.Length();
        Vector2 desire = dist > 0.05f ? toPlayer / dist * kind.Speed : Vector2.Zero;
        Vector2 velocity = desire + SeparationOf(i) * c.Separation * kind.Speed + _push[i];
        bool pushed = _push[i].LengthSquared() > 0.25f;
        _push[i] *= decay;
        if (velocity.LengthSquared() > 0.01f)
            Facing[i] = velocity;

        if (!Grounded[i])
        {
            StepAir(i, dt, velocity);
            return;
        }
        if (Climbing[i])
        {
            // Grimpe sur place, lentement ; repoussée du mur, elle lâche prise et retombe.
            if (pushed)
            {
                Climbing[i] = false;
                StartFall(i, true);
                return;
            }
            pos.Z += c.ClimbSpeed * dt;
            Facing[i] = desire;
            if (pos.Z >= _climbTarget[i])
            {
                pos.Z = _climbTarget[i];
                Climbing[i] = false;
                Vector2 hop = desire.Normalized() * 0.35f;
                pos.X += hop.X;
                pos.Y += hop.Y;
            }
            Position[i] = pos;
            return;
        }

        Vector2 next = _arena.Obstacles.PushOut(new Vector2(pos.X + velocity.X * dt, pos.Y + velocity.Y * dt), c.CrowdRadius);
        float ground = _arena.GroundAt(next.X, next.Y);
        if (ReliefArena.IsVoid(ground) && !pushed)
        {
            // Le vide ne se prend pas de lui-même : la créature longe le bord. Poussée, elle y tombe.
            next = MoveAvoidingVoid(pos, velocity * dt);
            ground = _arena.GroundAt(next.X, next.Y);
        }
        if (ground > pos.Z + c.Step)
        {
            if (!pushed && desire.LengthSquared() > 0f)
            {
                Climbing[i] = true;
                _climbTarget[i] = ground;
            }
            return;
        }
        pos.X = next.X;
        pos.Y = next.Y;
        Position[i] = pos;
        if (ground >= pos.Z - c.Step)
            Position[i] = pos with { Z = ground };
        else
            StartFall(i, pushed);
        Contact(i, kind, player);
    }

    private Vector2 MoveAvoidingVoid(Vector3 pos, Vector2 delta)
    {
        Vector2 alongX = new(pos.X + delta.X, pos.Y);
        if (!ReliefArena.IsVoid(_arena.GroundAt(alongX.X, alongX.Y)))
            return alongX;
        Vector2 alongY = new(pos.X, pos.Y + delta.Y);
        return ReliefArena.IsVoid(_arena.GroundAt(alongY.X, alongY.Y)) ? new Vector2(pos.X, pos.Y) : alongY;
    }

    private void StartFall(int i, bool pushed)
    {
        Grounded[i] = false;
        _pushedFall[i] = pushed;
        _vz[i] = 0f;
        _takeoff[i] = Position[i].Z;
    }

    private void StepAir(int i, float dt, Vector2 velocity)
    {
        ReliefConfig c = _config;
        Vector3 pos = Position[i];
        Vector2 next = new(pos.X + velocity.X * dt, pos.Y + velocity.Y * dt);
        if (_arena.GroundAt(next.X, next.Y) <= pos.Z)
        {
            pos.X = next.X;
            pos.Y = next.Y;
        }
        _vz[i] -= c.Gravity * dt;
        pos.Z += _vz[i] * dt;
        float floor = _arena.GroundAt(pos.X, pos.Y);
        if (pos.Z <= floor)
        {
            pos.Z = floor;
            Position[i] = pos;
            Grounded[i] = true;
            float dropLevels = (_takeoff[i] - floor) / _arena.LevelHeight;
            if (_pushedFall[i] && dropLevels > c.CrowdSafeDropLevels)
                Damage(i, (dropLevels - c.CrowdSafeDropLevels + 0.5f) * c.CrowdFallDamagePerLevel, Cause.Fall);
            return;
        }
        Position[i] = pos;
        if (pos.Z < VoidFloor)
            Kill(i, Cause.Void);
    }

    private void Contact(int i, Species kind, PlayerMotor3D player)
    {
        ReliefConfig c = _config;
        if (player.Invulnerable > 0f || Mathf.Abs(Position[i].Z - player.Position.Z) > c.ContactHeight)
            return;
        Vector2 d = new(Position[i].X - player.Position.X, Position[i].Y - player.Position.Y);
        if (d.LengthSquared() > c.ContactReach * c.ContactReach)
            return;
        if (!player.Hurt(kind.ContactDamage))
            return;
        player.Invulnerable = c.HurtInvulnerable;
        Strike[i] = StrikeSeconds;
        ContactHits++;
    }

    private void Kill(int i, Cause cause)
    {
        if (State[i] != Life.Alive)
            return;
        State[i] = Life.Dying;
        Timer[i] = DyingSeconds;
        Climbing[i] = false;
        Kills++;
        if (cause == Cause.Fall)
            KillsByFall++;
        else if (cause == Cause.Void)
            KillsByVoid++;
        else if (cause == Cause.Stomp)
            KillsByStomp++;
    }
}
