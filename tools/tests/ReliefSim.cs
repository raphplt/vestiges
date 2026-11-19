using Godot;

using Vestiges.World3D;

namespace Vestiges.Tests;

/// <summary>
/// Simulation 2,5D du bac à sable de la verticalité (plan 59), commune aux deux vues : sol carré (x, y), hauteur z.
/// Le joueur et la horde sont ceux de la run 3D (<see cref="PlayerMotor3D"/>, <see cref="Crowd3D"/>) ; le bac à sable y
/// ajoute son arme d'essai, un tir automatique vers la créature la plus proche. Les tirs ignorent les étages.
/// Aucune allocation par image : tableaux de taille fixe et grille de voisinage chaînée.
/// </summary>
public sealed class ReliefSim
{
    private const int MaxProjectiles = 64;
    private const float ChestHeight = 0.35f;
    // La horde n'a pas de minute de run : toutes ses sortes sont là dès le départ.
    private const float AllKinds = float.MaxValue;

    private readonly ReliefConfig _config;
    private float _fireTimer;

    public readonly PlayerMotor3D Player;
    public readonly Crowd3D Crowd;

    // Tirs.
    public readonly Vector3[] ProjectilePos = new Vector3[MaxProjectiles];
    public readonly bool[] ProjectileActive = new bool[MaxProjectiles];
    private readonly Vector3[] _projectileVel = new Vector3[MaxProjectiles];
    private readonly float[] _projectileLife = new float[MaxProjectiles];

    public int Deaths;
    public float Elapsed;

    public ReliefConfig Config => _config;

    public ReliefSim(ReliefConfig config, ReliefArena arena, int capacity, ulong seed)
    {
        _config = config;
        Player = new PlayerMotor3D(config, arena);
        Crowd = new Crowd3D(config, arena, config.Creatures, capacity, seed);
    }

    public void Step(float dt, Vector2 wish, int crowdTarget)
    {
        Elapsed += dt;
        Player.Step(dt, wish);
        if (Player.StompedThisStep)
            Crowd.Stomp(Player.LastStompAt, Player.Facing);
        ReviveIfDead();
        Crowd.Fill(Player, crowdTarget, AllKinds);
        Crowd.Step(dt, Player);
        ReviveIfDead();
        StepWeapon(dt);
        StepProjectiles(dt);
    }

    private void ReviveIfDead()
    {
        if (!Player.Dead)
            return;
        Deaths++;
        Player.Revive();
        // Le bac à sable ne finit pas : la foule proche est repoussée et la partie continue.
        Crowd.Repel(new Vector2(Player.Position.X, Player.Position.Y), _config.RespawnClearRadius, _config.RespawnClearPush);
    }

    private void StepWeapon(float dt)
    {
        _fireTimer -= dt;
        if (_fireTimer > 0f)
            return;
        int target = Crowd.Nearest(Player.Position, _config.WeaponRange);
        if (target < 0)
            return;
        _fireTimer = _config.WeaponInterval;
        int slot = System.Array.IndexOf(ProjectileActive, false);
        if (slot < 0)
            return;
        Vector3 from = Player.Position + new Vector3(0f, 0f, ChestHeight + 0.1f);
        Vector3 to = Crowd.Position[target] + new Vector3(0f, 0f, ChestHeight);
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
            for (int i = 0; i < Crowd.Capacity; i++)
            {
                if (Crowd.State[i] != Crowd3D.Life.Alive)
                    continue;
                Vector3 chest = Crowd.Position[i] + new Vector3(0f, 0f, ChestHeight);
                if ((chest - ProjectilePos[p]).LengthSquared() > hitSq)
                    continue;
                ProjectileActive[p] = false;
                Vector2 along = new Vector2(_projectileVel[p].X, _projectileVel[p].Y).Normalized();
                Crowd.Push(i, along * _config.WeaponPush);
                Crowd.Damage(i, _config.WeaponDamage, Crowd3D.Cause.Hit);
                break;
            }
        }
    }
}
