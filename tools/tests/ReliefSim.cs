using Godot;

using Vestiges.World3D;

namespace Vestiges.Tests;

/// <summary>
/// Simulation 2,5D du bac à sable de la verticalité (plan 59), commune aux deux vues : sol carré (x, y), hauteur z.
/// Le joueur est celui de la run 3D (<see cref="PlayerMotor3D"/>) ; la horde fonce sur lui, grimpe lentement les falaises, saute en bas,
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

    private readonly ReliefConfig _config;
    private readonly ReliefArena _arena;
    private readonly RandomNumberGenerator _rng = new();
    private readonly int[] _bucketHead;
    private readonly int[] _bucketNext;
    private readonly int _weightTotal;
    private float _fireTimer;

    public readonly PlayerMotor3D Player;

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

    // Compteurs affichés.
    public int Deaths;
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
        Player = new PlayerMotor3D(config, arena);
    }

    public void Step(float dt, Vector2 wish, int crowdTarget)
    {
        Elapsed += dt;
        Player.Step(dt, wish);
        if (Player.StompedThisStep)
            Stomp();
        ReviveIfDead();
        Spawn(crowdTarget);
        RebuildBuckets();
        StepCrowd(dt);
        StepWeapon(dt);
        StepProjectiles(dt);
    }

    /// <summary>Retombée de haut : onde qui blesse et repousse la foule au même niveau, qui peut la jeter du bord.</summary>
    private void Stomp()
    {
        ReliefConfig c = _config;
        Vector3 at = Player.LastStompAt;
        float radiusSq = c.StompRadius * c.StompRadius;
        for (int i = 0; i < Capacity; i++)
        {
            if (State[i] != EnemyState.Alive || Mathf.Abs(EnemyPos[i].Z - at.Z) > 0.6f)
                continue;
            Vector2 away = new(EnemyPos[i].X - at.X, EnemyPos[i].Y - at.Y);
            float distSq = away.LengthSquared();
            if (distSq > radiusSq)
                continue;
            Vector2 dir = distSq > 0.0001f ? away / Mathf.Sqrt(distSq) : Player.Facing;
            _push[i] += dir * c.StompPush * (1f - Mathf.Sqrt(distSq) / c.StompRadius * 0.5f);
            DamageEnemy(i, c.StompDamage, Cause.Stomp);
        }
    }

    private void ReviveIfDead()
    {
        if (!Player.Dead)
            return;
        Deaths++;
        Player.Revive();
        // Le bac à sable ne finit pas : la foule proche est repoussée et la partie continue.
        for (int i = 0; i < Capacity; i++)
        {
            if (State[i] != EnemyState.Alive)
                continue;
            Vector2 away = new(EnemyPos[i].X - Player.Position.X, EnemyPos[i].Y - Player.Position.Y);
            if (away.LengthSquared() < 16f)
                _push[i] += away.Normalized() * 6f;
        }
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
                float x = Player.Position.X + Mathf.Cos(angle) * distance;
                float y = Player.Position.Y + Mathf.Sin(angle) * distance;
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
            Vector2 toPlayer = new(Player.Position.X - pos.X, Player.Position.Y - pos.Y);
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
        if (Player.Invulnerable > 0f || Mathf.Abs(EnemyPos[i].Z - Player.Position.Z) > c.ContactHeight)
            return;
        Vector2 d = new(EnemyPos[i].X - Player.Position.X, EnemyPos[i].Y - Player.Position.Y);
        if (d.LengthSquared() > c.ContactReach * c.ContactReach)
            return;
        Player.Hurt(c.ContactDamage);
        Player.Invulnerable = c.HurtInvulnerable;
        ReviveIfDead();
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
            float d = (EnemyPos[i] - Player.Position).LengthSquared();
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
        Vector3 from = Player.Position + new Vector3(0f, 0f, ChestHeight + 0.1f);
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
