using Godot;

namespace Vestiges.Tests;

/// <summary>
/// Simulation 2,5D du bac à sable de la verticalité (plan 59), commune aux deux vues : sol carré (x, y), hauteur z.
/// Le joueur marche, bondit d'un étage, tombe ; la horde fonce sur lui, grimpe lentement les falaises, saute en bas,
/// tombe des gradins quand on la pousse. Le contact ne touche qu'à hauteur égale ; les tirs ignorent les étages.
/// Aucune allocation par image : tableaux de taille fixe et grille de voisinage chaînée.
/// </summary>
public sealed class ReliefSim
{
    public enum EnemyState : byte { Inactive, Alive, Dying }

    public enum Cause : byte { Shot, Stomp, Fall, Void }

    private const int MaxProjectiles = 64;
    private const int MaxSpawnsPerTick = 4;
    private const float ChestHeight = 0.35f;
    private const float DyingSeconds = 0.45f;
    private const float FlashSeconds = 0.12f;
    private const float VoidFloor = -5f;
    private const float RespawnInvulnerable = 1.2f;

    private readonly ReliefConfig _config;
    private readonly ReliefArena _arena;
    private readonly RandomNumberGenerator _rng = new();
    private readonly int[] _bucketHead;
    private readonly int[] _bucketNext;
    private readonly int _weightTotal;
    // Emprises au sol des vrais décors : (x, y, rayon), indexées par cellule.
    private readonly System.Collections.Generic.List<Vector3> _obstacles = new();
    private int[] _obstacleStart;
    private int[] _obstacleList;
    private float _fireTimer;
    private bool _bondQueued;
    private bool _skillQueued;
    private Vector2 _skillDir;
    // Saut flottant (scaphandrière) : sa poussée, en part du saut commun, et sa gravité tant qu'il dure.
    private const float FloatJumpLift = 0.8f;
    private const float FloatGravity = 0.45f;

    // Joueur.
    public Vector3 PlayerPos;
    public Vector2 PlayerFacing = new(1f, 1f);
    public Vector2 PlayerVelocity;
    public bool PlayerGrounded = true;
    public float PlayerHp;
    public float PlayerInvulnerable;
    public float PlayerHurtFlash;
    public bool PlayerAirborneFromBond;
    public float SkillTime;
    public float SkillCooldown;
    private float _playerVz;
    private float _takeoffZ;
    private float _bondCooldown;
    private Vector2 _lastSafe;

    // Horde.
    public readonly Vector3[] EnemyPos;
    public readonly Vector2[] EnemyFacing;
    public readonly EnemyState[] State;
    public readonly int[] EnemyType;
    public readonly float[] EnemyTimer;
    public readonly bool[] EnemyClimbing;
    public readonly bool[] EnemyGrounded;
    public readonly float[] EnemyFlash;
    private readonly float[] _enemyHp;
    private readonly float[] _enemyVz;
    private readonly float[] _enemyTakeoff;
    private readonly float[] _climbTarget;
    // La chute en cours vient d'une poussée : elle seule blesse. Une créature qui saute d'elle-même d'un gradin n'a rien.
    private readonly bool[] _pushedFall;
    private readonly Vector2[] _push;

    // Tirs.
    public readonly Vector3[] ProjectilePos = new Vector3[MaxProjectiles];
    public readonly bool[] ProjectileActive = new bool[MaxProjectiles];
    private readonly Vector3[] _projectileVel = new Vector3[MaxProjectiles];
    private readonly float[] _projectileLife = new float[MaxProjectiles];

    // Événements de l'image, lus par la vue (retours visuels).
    public int StompsThisTick;
    public Vector3 LastStompAt;
    public int LandingsThisTick;
    public bool HurtThisTick;
    public bool VoidFallThisTick;

    // Compteurs affichés.
    public int Bonds, Skills, Stomps, Deaths, VoidFalls;
    public int Kills, KillsByFall, KillsByVoid, KillsByStomp;
    public float Elapsed;

    public ReliefArena Arena => _arena;
    public ReliefConfig Config => _config;
    public int Capacity => EnemyPos.Length;

    public ReliefSim(ReliefConfig config, ReliefArena arena, int capacity, ulong seed)
    {
        _config = config;
        _arena = arena;
        _rng.Seed = seed;
        EnemyPos = new Vector3[capacity];
        EnemyFacing = new Vector2[capacity];
        State = new EnemyState[capacity];
        EnemyType = new int[capacity];
        EnemyTimer = new float[capacity];
        EnemyClimbing = new bool[capacity];
        EnemyGrounded = new bool[capacity];
        EnemyFlash = new float[capacity];
        _enemyHp = new float[capacity];
        _enemyVz = new float[capacity];
        _enemyTakeoff = new float[capacity];
        _climbTarget = new float[capacity];
        _pushedFall = new bool[capacity];
        _push = new Vector2[capacity];
        _bucketHead = new int[arena.Width * arena.Depth];
        _bucketNext = new int[capacity];
        foreach (ReliefConfig.Creature creature in config.Creatures)
            _weightTotal += creature.Weight;
        PlayerHp = config.PlayerHp;
        PlayerPos = new Vector3(config.Spawn.X, config.Spawn.Y, arena.GroundAt(config.Spawn.X, config.Spawn.Y));
        _lastSafe = config.Spawn;
    }

    /// <summary>Ajoute l'emprise au sol d'un décor (avant <see cref="FinishObstacles"/>).</summary>
    public void AddObstacle(Vector2 at, float radius) => _obstacles.Add(new Vector3(at.X, at.Y, radius));

    /// <summary>Range les emprises par cellule touchée, marge d'un corps comprise.</summary>
    public void FinishObstacles()
    {
        int cells = _arena.Width * _arena.Depth;
        System.Collections.Generic.List<int>[] perCell = new System.Collections.Generic.List<int>[cells];
        for (int o = 0; o < _obstacles.Count; o++)
        {
            Vector3 ob = _obstacles[o];
            float reach = ob.Z + 0.5f;
            for (int y = Mathf.FloorToInt(ob.Y - reach); y <= Mathf.FloorToInt(ob.Y + reach); y++)
            {
                for (int x = Mathf.FloorToInt(ob.X - reach); x <= Mathf.FloorToInt(ob.X + reach); x++)
                {
                    if (x < 0 || y < 0 || x >= _arena.Width || y >= _arena.Depth)
                        continue;
                    (perCell[y * _arena.Width + x] ??= new()).Add(o);
                }
            }
        }
        _obstacleStart = new int[cells + 1];
        System.Collections.Generic.List<int> flat = new();
        for (int c = 0; c < cells; c++)
        {
            _obstacleStart[c] = flat.Count;
            if (perCell[c] != null)
                flat.AddRange(perCell[c]);
        }
        _obstacleStart[cells] = flat.Count;
        _obstacleList = flat.ToArray();
    }

    /// <summary>Sort un corps des emprises de décors de sa cellule, en glissant le long de leur bord.</summary>
    private Vector2 PushOutOfObstacles(Vector2 at, float radius)
    {
        if (_obstacleStart == null)
            return at;
        int cell = BucketOf(at.X, at.Y);
        if (cell < 0)
            return at;
        for (int k = _obstacleStart[cell]; k < _obstacleStart[cell + 1]; k++)
        {
            Vector3 ob = _obstacles[_obstacleList[k]];
            Vector2 d = new(at.X - ob.X, at.Y - ob.Y);
            float min = ob.Z + radius;
            float lenSq = d.LengthSquared();
            if (lenSq >= min * min)
                continue;
            float len = Mathf.Sqrt(lenSq);
            at = new Vector2(ob.X, ob.Y) + (len > 0.0001f ? d / len : new Vector2(1f, 0f)) * min;
        }
        return at;
    }

    /// <summary>Le bond est demandé : il part au prochain pas, s'il est au sol et prêt.</summary>
    public void QueueBond() => _bondQueued = true;

    /// <summary>La capacité du personnage est demandée : elle part au prochain pas, s'il est au sol et qu'elle est prête.</summary>
    public void QueueSkill() => _skillQueued = true;

    /// <summary>Place le joueur au sol à cet endroit (captures).</summary>
    public void Teleport(Vector2 at)
    {
        PlayerPos = new Vector3(at.X, at.Y, _arena.HighestUnder(at.X, at.Y, _config.PlayerRadius));
        PlayerGrounded = true;
        PlayerVelocity = Vector2.Zero;
        SkillTime = 0f;
        _playerVz = 0f;
        _lastSafe = at;
    }

    public float PlayerLevel => PlayerPos.Z / _arena.LevelHeight;

    public void Step(float dt, Vector2 wish, int crowdTarget)
    {
        Elapsed += dt;
        StompsThisTick = 0;
        LandingsThisTick = 0;
        HurtThisTick = false;
        VoidFallThisTick = false;
        StepPlayer(dt, wish);
        Spawn(crowdTarget);
        RebuildBuckets();
        StepCrowd(dt);
        StepWeapon(dt);
        StepProjectiles(dt);
    }

    private void StepPlayer(float dt, Vector2 wish)
    {
        ReliefConfig c = _config;
        PlayerInvulnerable = Mathf.Max(0f, PlayerInvulnerable - dt);
        PlayerHurtFlash = Mathf.Max(0f, PlayerHurtFlash - dt);
        _bondCooldown = Mathf.Max(0f, _bondCooldown - dt);
        SkillCooldown = Mathf.Max(0f, SkillCooldown - dt);
        if (wish.LengthSquared() > 0.01f)
            PlayerFacing = wish.Normalized();

        bool bond = _bondQueued;
        _bondQueued = false;
        bool skill = _skillQueued;
        _skillQueued = false;
        if (skill && SkillCooldown <= 0f && PlayerGrounded)
            StartSkill(wish);
        if (SkillTime > 0f)
            SteerSkill(dt, wish);

        if (PlayerGrounded)
        {
            if (bond && _bondCooldown <= 0f && SkillTime <= 0f)
            {
                // Le saut garde l'élan du sol, sans poussée : la portée vient de la course, pas du saut.
                TakeOff(c.JumpVelocity);
                PlayerInvulnerable = Mathf.Max(PlayerInvulnerable, c.JumpInvulnerable);
                _bondCooldown = c.JumpCooldown;
                Bonds++;
                return;
            }
            if (SkillTime <= 0f)
            {
                // Inertie : la vitesse se prend et se perd en une fraction de seconde, au lieu de sauter d'un coup.
                Vector2 target = wish * c.PlayerSpeed;
                float seconds = target.LengthSquared() > 0.0001f ? c.AccelerationSeconds : c.BrakingSeconds;
                PlayerVelocity = PlayerVelocity.MoveToward(target, c.PlayerSpeed / seconds * dt);
            }
            Vector2 at = new(PlayerPos.X, PlayerPos.Y);
            at = MoveBlocked(at, PlayerVelocity * dt, PlayerPos.Z + c.Step, c.PlayerRadius);
            at = PushOutOfObstacles(at, c.PlayerRadius);
            // Le joueur tient sur le plus haut sol sous son corps : il ne tombe d'un rebord qu'une fois sorti tout entier,
            // sinon il retomberait à cheval sur la paroi et y resterait collé.
            float ground = _arena.HighestUnder(at.X, at.Y, c.PlayerRadius);
            PlayerPos = new Vector3(at.X, at.Y, PlayerPos.Z);
            if (ground >= PlayerPos.Z - c.Step)
            {
                PlayerPos.Z = ground;
                _lastSafe = at;
            }
            else
            {
                PlayerGrounded = false;
                PlayerAirborneFromBond = false;
                _playerVz = 0f;
                _takeoffZ = PlayerPos.Z;
            }
            return;
        }

        if (SkillTime <= 0f)
            PlayerVelocity = PlayerVelocity.MoveToward(wish * c.PlayerSpeed, c.AirControl * dt);
        // Le saut flottant pèse moins tant qu'il dure.
        float gravity = SkillTime > 0f && c.Mobility.VisualHeightPixels > 0f ? c.Gravity * FloatGravity : c.Gravity;
        _playerVz -= gravity * dt;
        Vector2 air = MoveBlocked(new Vector2(PlayerPos.X, PlayerPos.Y), PlayerVelocity * dt, PlayerPos.Z, c.PlayerRadius);
        float z = PlayerPos.Z + _playerVz * dt;
        float floor = _arena.HighestUnder(air.X, air.Y, c.PlayerRadius);
        PlayerPos = new Vector3(air.X, air.Y, z);
        if (z <= floor)
        {
            Land(floor);
            return;
        }
        if (z < VoidFloor)
            FallIntoVoid();
    }

    private void TakeOff(float verticalSpeed)
    {
        PlayerGrounded = false;
        PlayerAirborneFromBond = true;
        _playerVz = verticalSpeed;
        _takeoffZ = PlayerPos.Z;
    }

    /// <summary>
    /// La capacité du personnage, d'après son profil du jeu : une poussée dans la direction voulue (dash, déphasage), une
    /// glissade qu'on oriente en route, ou un saut flottant qui quitte le sol.
    /// </summary>
    private void StartSkill(Vector2 wish)
    {
        Vestiges.Infrastructure.MobilityConfig m = _config.Mobility;
        _skillDir = wish.LengthSquared() > 0.01f ? wish.Normalized() : PlayerFacing.Normalized();
        SkillTime = m.DurationSeconds;
        SkillCooldown = m.CooldownSeconds;
        PlayerInvulnerable = Mathf.Max(PlayerInvulnerable, m.InvulnerabilitySeconds);
        Skills++;
        if (m.VisualHeightPixels > 0f)
            TakeOff(_config.JumpVelocity * FloatJumpLift);
    }

    private void SteerSkill(float dt, Vector2 wish)
    {
        Vestiges.Infrastructure.MobilityConfig m = _config.Mobility;
        SkillTime = Mathf.Max(0f, SkillTime - dt);
        if (m.TurnRateDegrees > 0f && wish.LengthSquared() > 0.01f)
        {
            float turn = Mathf.DegToRad(m.TurnRateDegrees) * dt;
            float delta = Mathf.Clamp(_skillDir.AngleTo(wish), -turn, turn);
            _skillDir = _skillDir.Rotated(delta);
        }
        PlayerVelocity = _skillDir * _config.PlayerSpeed * m.SpeedMultiplier;
        if (_skillDir.LengthSquared() > 0.01f)
            PlayerFacing = _skillDir;
    }

    private void Land(float floor)
    {
        ReliefConfig c = _config;
        float dropLevels = (_takeoffZ - floor) / _arena.LevelHeight;
        PlayerPos.Z = floor;
        PlayerGrounded = true;
        _playerVz = 0f;
        LandingsThisTick++;
        if (dropLevels > c.SafeDropLevels && PlayerInvulnerable <= 0f)
            HurtPlayer(Mathf.Round((dropLevels - c.SafeDropLevels + 0.5f) * c.DropDamagePerLevel));
        if (dropLevels >= c.StompMinDropLevels)
            Stomp();
        PlayerAirborneFromBond = false;
    }

    private void FallIntoVoid()
    {
        VoidFalls++;
        VoidFallThisTick = true;
        Teleport(_lastSafe);
        PlayerInvulnerable = 0f;
        HurtPlayer(_config.VoidDamage);
        PlayerInvulnerable = RespawnInvulnerable;
    }

    /// <summary>Retombée de haut : onde qui blesse et repousse la foule au même niveau, qui peut la jeter du bord.</summary>
    private void Stomp()
    {
        ReliefConfig c = _config;
        Stomps++;
        StompsThisTick++;
        LastStompAt = PlayerPos;
        float radiusSq = c.StompRadius * c.StompRadius;
        for (int i = 0; i < Capacity; i++)
        {
            if (State[i] != EnemyState.Alive || Mathf.Abs(EnemyPos[i].Z - PlayerPos.Z) > 0.6f)
                continue;
            Vector2 away = new(EnemyPos[i].X - PlayerPos.X, EnemyPos[i].Y - PlayerPos.Y);
            float distSq = away.LengthSquared();
            if (distSq > radiusSq)
                continue;
            Vector2 dir = distSq > 0.0001f ? away / Mathf.Sqrt(distSq) : PlayerFacing;
            _push[i] += dir * c.StompPush * (1f - Mathf.Sqrt(distSq) / c.StompRadius * 0.5f);
            DamageEnemy(i, c.StompDamage, Cause.Stomp);
        }
    }

    private void HurtPlayer(float damage)
    {
        PlayerHp -= damage;
        PlayerHurtFlash = 0.25f;
        HurtThisTick = true;
        if (PlayerHp > 0f)
            return;
        Deaths++;
        PlayerHp = _config.PlayerHp;
        // Le bac à sable ne finit pas : la foule proche est repoussée et la partie continue.
        for (int i = 0; i < Capacity; i++)
        {
            if (State[i] != EnemyState.Alive)
                continue;
            Vector2 away = new(EnemyPos[i].X - PlayerPos.X, EnemyPos[i].Y - PlayerPos.Y);
            if (away.LengthSquared() < 16f)
                _push[i] += away.Normalized() * 6f;
        }
    }

    /// <summary>
    /// Déplacement d'un disque, axe par axe, qui bute contre tout sol plus haut que <paramref name="maxHeight"/>. Le vide
    /// ne bloque pas : on y tombe.
    /// </summary>
    private Vector2 MoveBlocked(Vector2 from, Vector2 delta, float maxHeight, float radius)
    {
        Vector2 at = from;
        Vector2 alongX = new(at.X + delta.X, at.Y);
        if (_arena.HighestUnder(alongX.X, alongX.Y, radius) <= maxHeight)
            at = alongX;
        Vector2 alongY = new(at.X, at.Y + delta.Y);
        if (_arena.HighestUnder(alongY.X, alongY.Y, radius) <= maxHeight)
            at = alongY;
        return at;
    }

    private void Spawn(int target)
    {
        int alive = 0;
        for (int i = 0; i < Capacity; i++)
        {
            if (State[i] != EnemyState.Inactive)
                alive++;
        }
        int spawned = 0;
        for (int i = 0; i < Capacity && alive < target && spawned < MaxSpawnsPerTick; i++)
        {
            if (State[i] != EnemyState.Inactive)
                continue;
            for (int attempt = 0; attempt < 8; attempt++)
            {
                float angle = _rng.Randf() * Mathf.Tau;
                float distance = _rng.RandfRange(_config.SpawnMin, _config.SpawnMax);
                float x = PlayerPos.X + Mathf.Cos(angle) * distance;
                float y = PlayerPos.Y + Mathf.Sin(angle) * distance;
                float ground = _arena.GroundAt(x, y);
                if (ReliefArena.IsVoid(ground))
                    continue;
                ActivateEnemy(i, new Vector3(x, y, ground));
                alive++;
                spawned++;
                break;
            }
        }
    }

    private void ActivateEnemy(int i, Vector3 at)
    {
        int roll = _rng.RandiRange(0, _weightTotal - 1);
        int type = 0;
        while (roll >= _config.Creatures[type].Weight)
        {
            roll -= _config.Creatures[type].Weight;
            type++;
        }
        State[i] = EnemyState.Alive;
        EnemyType[i] = type;
        EnemyPos[i] = at;
        EnemyFacing[i] = new Vector2(1f, 1f);
        EnemyClimbing[i] = false;
        EnemyGrounded[i] = true;
        EnemyFlash[i] = 0f;
        EnemyTimer[i] = 0f;
        _enemyHp[i] = _config.Creatures[type].Hp;
        _enemyVz[i] = 0f;
        _push[i] = Vector2.Zero;
    }

    private void RebuildBuckets()
    {
        System.Array.Fill(_bucketHead, -1);
        for (int i = 0; i < Capacity; i++)
        {
            if (State[i] != EnemyState.Alive)
                continue;
            int bucket = BucketOf(EnemyPos[i].X, EnemyPos[i].Y);
            if (bucket < 0)
            {
                _bucketNext[i] = -1;
                continue;
            }
            _bucketNext[i] = _bucketHead[bucket];
            _bucketHead[bucket] = i;
        }
    }

    private int BucketOf(float x, float y)
    {
        int cx = Mathf.FloorToInt(x);
        int cy = Mathf.FloorToInt(y);
        return cx < 0 || cy < 0 || cx >= _arena.Width || cy >= _arena.Depth ? -1 : cy * _arena.Width + cx;
    }

    private Vector2 SeparationOf(int i)
    {
        float diameter = _config.CrowdRadius * 2f;
        Vector3 me = EnemyPos[i];
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
                    if (j == i || Mathf.Abs(EnemyPos[j].Z - me.Z) > 0.5f)
                        continue;
                    Vector2 d = new(me.X - EnemyPos[j].X, me.Y - EnemyPos[j].Y);
                    float len = d.Length();
                    if (len >= diameter)
                        continue;
                    push += len > 0.0001f ? d / len * (1f - len / diameter) : new Vector2(((i * 7) % 3) - 1f, 0.5f);
                }
            }
        }
        return push;
    }

    private void StepCrowd(float dt)
    {
        ReliefConfig c = _config;
        float decay = Mathf.Exp(-c.PushDecay * dt);
        for (int i = 0; i < Capacity; i++)
        {
            EnemyFlash[i] = Mathf.Max(0f, EnemyFlash[i] - dt);
            if (State[i] == EnemyState.Dying)
            {
                EnemyTimer[i] -= dt;
                if (EnemyTimer[i] <= 0f)
                    State[i] = EnemyState.Inactive;
                continue;
            }
            if (State[i] != EnemyState.Alive)
                continue;
            EnemyTimer[i] += dt;
            Vector3 pos = EnemyPos[i];
            ReliefConfig.Creature creature = c.Creatures[EnemyType[i]];
            Vector2 toPlayer = new(PlayerPos.X - pos.X, PlayerPos.Y - pos.Y);
            float dist = toPlayer.Length();
            Vector2 desire = dist > 0.05f ? toPlayer / dist * creature.Speed : Vector2.Zero;
            Vector2 velocity = desire + SeparationOf(i) * c.Separation * creature.Speed + _push[i];
            bool pushed = _push[i].LengthSquared() > 0.25f;
            _push[i] *= decay;
            if (velocity.LengthSquared() > 0.01f)
                EnemyFacing[i] = velocity;

            if (!EnemyGrounded[i])
            {
                StepEnemyAir(i, dt, velocity);
                continue;
            }
            if (EnemyClimbing[i])
            {
                // Grimpe sur place, lentement ; repoussée du mur, elle lâche prise et retombe.
                if (pushed)
                {
                    EnemyClimbing[i] = false;
                    StartEnemyFall(i, true);
                    continue;
                }
                pos.Z += c.ClimbSpeed * dt;
                EnemyFacing[i] = desire;
                if (pos.Z >= _climbTarget[i])
                {
                    pos.Z = _climbTarget[i];
                    EnemyClimbing[i] = false;
                    Vector2 hop = desire.Normalized() * 0.35f;
                    pos.X += hop.X;
                    pos.Y += hop.Y;
                }
                EnemyPos[i] = pos;
                continue;
            }

            Vector2 next = PushOutOfObstacles(new Vector2(pos.X + velocity.X * dt, pos.Y + velocity.Y * dt), c.CrowdRadius);
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
                    EnemyClimbing[i] = true;
                    _climbTarget[i] = ground;
                }
                continue;
            }
            pos.X = next.X;
            pos.Y = next.Y;
            EnemyPos[i] = pos;
            if (ground >= pos.Z - c.Step)
                EnemyPos[i] = pos with { Z = ground };
            else
                StartEnemyFall(i, pushed);
            ContactPlayer(i);
        }
    }

    private Vector2 MoveAvoidingVoid(Vector3 pos, Vector2 delta)
    {
        Vector2 alongX = new(pos.X + delta.X, pos.Y);
        if (!ReliefArena.IsVoid(_arena.GroundAt(alongX.X, alongX.Y)))
            return alongX;
        Vector2 alongY = new(pos.X, pos.Y + delta.Y);
        return ReliefArena.IsVoid(_arena.GroundAt(alongY.X, alongY.Y)) ? new Vector2(pos.X, pos.Y) : alongY;
    }

    private void StartEnemyFall(int i, bool pushed)
    {
        EnemyGrounded[i] = false;
        _pushedFall[i] = pushed;
        _enemyVz[i] = 0f;
        _enemyTakeoff[i] = EnemyPos[i].Z;
    }

    private void StepEnemyAir(int i, float dt, Vector2 velocity)
    {
        ReliefConfig c = _config;
        Vector3 pos = EnemyPos[i];
        Vector2 next = new(pos.X + velocity.X * dt, pos.Y + velocity.Y * dt);
        if (_arena.GroundAt(next.X, next.Y) <= pos.Z)
        {
            pos.X = next.X;
            pos.Y = next.Y;
        }
        _enemyVz[i] -= c.Gravity * dt;
        pos.Z += _enemyVz[i] * dt;
        float floor = _arena.GroundAt(pos.X, pos.Y);
        if (pos.Z <= floor)
        {
            pos.Z = floor;
            EnemyPos[i] = pos;
            EnemyGrounded[i] = true;
            float dropLevels = (_enemyTakeoff[i] - floor) / _arena.LevelHeight;
            if (_pushedFall[i] && dropLevels > c.CrowdSafeDropLevels)
                DamageEnemy(i, (dropLevels - c.CrowdSafeDropLevels + 0.5f) * c.CrowdFallDamagePerLevel, Cause.Fall);
            return;
        }
        EnemyPos[i] = pos;
        if (pos.Z < VoidFloor)
            Kill(i, Cause.Void);
    }

    private void ContactPlayer(int i)
    {
        ReliefConfig c = _config;
        if (PlayerInvulnerable > 0f || Mathf.Abs(EnemyPos[i].Z - PlayerPos.Z) > c.ContactHeight)
            return;
        Vector2 d = new(EnemyPos[i].X - PlayerPos.X, EnemyPos[i].Y - PlayerPos.Y);
        if (d.LengthSquared() > c.ContactReach * c.ContactReach)
            return;
        PlayerInvulnerable = c.HurtInvulnerable;
        HurtPlayer(c.ContactDamage);
    }

    private void StepWeapon(float dt)
    {
        _fireTimer -= dt;
        if (_fireTimer > 0f)
            return;
        float best = _config.WeaponRange * _config.WeaponRange;
        int target = -1;
        for (int i = 0; i < Capacity; i++)
        {
            if (State[i] != EnemyState.Alive)
                continue;
            float d = (EnemyPos[i] - PlayerPos).LengthSquared();
            if (d < best)
            {
                best = d;
                target = i;
            }
        }
        if (target < 0)
            return;
        _fireTimer = _config.WeaponInterval;
        int slot = System.Array.IndexOf(ProjectileActive, false);
        if (slot < 0)
            return;
        Vector3 from = PlayerPos + new Vector3(0f, 0f, ChestHeight + 0.1f);
        Vector3 to = EnemyPos[target] + new Vector3(0f, 0f, ChestHeight);
        Vector3 dir = (to - from).Normalized();
        ProjectileActive[slot] = true;
        ProjectilePos[slot] = from;
        _projectileVel[slot] = dir * _config.WeaponSpeed;
        _projectileLife[slot] = _config.WeaponRange / _config.WeaponSpeed + 0.2f;
    }

    private void StepProjectiles(float dt)
    {
        float hitSq = _config.WeaponHitRadius * _config.WeaponHitRadius;
        for (int p = 0; p < MaxProjectiles; p++)
        {
            if (!ProjectileActive[p])
                continue;
            _projectileLife[p] -= dt;
            ProjectilePos[p] += _projectileVel[p] * dt;
            if (_projectileLife[p] <= 0f)
            {
                ProjectileActive[p] = false;
                continue;
            }
            for (int i = 0; i < Capacity; i++)
            {
                if (State[i] != EnemyState.Alive)
                    continue;
                Vector3 chest = EnemyPos[i] + new Vector3(0f, 0f, ChestHeight);
                if ((chest - ProjectilePos[p]).LengthSquared() > hitSq)
                    continue;
                ProjectileActive[p] = false;
                Vector2 along = new Vector2(_projectileVel[p].X, _projectileVel[p].Y).Normalized();
                _push[i] += along * _config.WeaponPush;
                DamageEnemy(i, _config.WeaponDamage, Cause.Shot);
                break;
            }
        }
    }

    private void DamageEnemy(int i, float damage, Cause cause)
    {
        _enemyHp[i] -= damage;
        EnemyFlash[i] = FlashSeconds;
        if (_enemyHp[i] <= 0f)
            Kill(i, cause);
    }

    private void Kill(int i, Cause cause)
    {
        if (State[i] != EnemyState.Alive)
            return;
        State[i] = EnemyState.Dying;
        EnemyTimer[i] = DyingSeconds;
        EnemyClimbing[i] = false;
        Kills++;
        if (cause == Cause.Fall)
            KillsByFall++;
        else if (cause == Cause.Void)
            KillsByVoid++;
        else if (cause == Cause.Stomp)
            KillsByStomp++;
    }

    public static float DyingDuration => DyingSeconds;
}
