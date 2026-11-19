using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.World3D;

/// <summary>
/// Le mouvement du joueur sur le relief 3D (plan 60, 60.1b), sans nœud ni rendu : marche avec inertie, saut commun,
/// capacité du personnage (poussée, glissade orientable, saut flottant), chutes et leurs dégâts, retombée qui frappe, vide
/// qui ramène au dernier sol sûr. Le corps est un disque qui bute contre tout sol plus haut que sa marche et contre les
/// emprises des décors ; il tient sur le plus haut sol sous lui.
/// Repère du relief : sol carré (x, y) en cellules, hauteur z ; <see cref="WorldPosition"/> le donne en coordonnées Godot.
/// La run 3D et le bac à sable partagent ce moteur.
/// </summary>
public sealed class PlayerMotor3D
{
    // Sous cette hauteur, le corps est tombé dans le vide.
    private const float VoidFloor = -5f;
    private const float RespawnInvulnerable = 1.2f;
    private const float HurtFlashSeconds = 0.25f;
    // Saut flottant (scaphandrière) : sa poussée, en part du saut commun, et sa gravité tant qu'il dure.
    private const float FloatJumpLift = 0.8f;
    private const float FloatGravity = 0.45f;

    private readonly ReliefConfig _config;
    private readonly ReliefArena _arena;
    private bool _jumpQueued;
    private bool _skillQueued;
    private Vector2 _skillDir;
    private float _vz;
    private float _takeoffZ;
    private float _jumpCooldown;
    private Vector2 _lastSafe;

    /// <summary>Pieds du joueur : sol (x, y) en cellules, hauteur z.</summary>
    public Vector3 Position;
    public Vector2 Facing = new(1f, 1f);
    public Vector2 Velocity;
    public bool Grounded = true;
    public float Hp;
    public float Invulnerable;
    public float HurtFlash;
    /// <summary>En l'air par un saut (pas par une chute d'un rebord) : la pose de saut se joue.</summary>
    public bool AirborneFromJump;
    public float SkillTime;
    public float SkillCooldown;

    public bool StompedThisStep;
    public Vector3 LastStompAt;
    public bool HurtThisStep;
    public bool VoidFallThisStep;
    public int Landings, Jumps, Skills, Stomps, VoidFalls;

    public PlayerMotor3D(ReliefConfig config, ReliefArena arena)
    {
        _config = config;
        _arena = arena;
        Hp = config.PlayerHp;
        Teleport(config.Spawn);
    }

    public ReliefConfig Config => _config;

    /// <summary>Les pieds en coordonnées Godot : x, hauteur, y du relief.</summary>
    public Vector3 WorldPosition => new(Position.X, Position.Z, Position.Y);

    public float Level => Position.Z / _arena.LevelHeight;

    public bool Dead => Hp <= 0f;

    /// <summary>Le plus haut sol sous le corps, où se pose son ombre.</summary>
    public float GroundUnder => _arena.HighestUnder(Position.X, Position.Y, _config.PlayerRadius);

    /// <summary>Le saut est demandé : il part au prochain pas, s'il est au sol et prêt.</summary>
    public void QueueJump() => _jumpQueued = true;

    /// <summary>La capacité du personnage est demandée : elle part au prochain pas, s'il est au sol et qu'elle est prête.</summary>
    public void QueueSkill() => _skillQueued = true;

    /// <summary>Pose le joueur au sol à cet endroit, à l'arrêt.</summary>
    public void Teleport(Vector2 at)
    {
        Position = new Vector3(at.X, at.Y, _arena.HighestUnder(at.X, at.Y, _config.PlayerRadius));
        Grounded = true;
        Velocity = Vector2.Zero;
        SkillTime = 0f;
        _vz = 0f;
        _lastSafe = at;
    }

    /// <summary>Un pas de <paramref name="dt"/> secondes vers <paramref name="wish"/>, direction au sol de longueur 1 au plus.</summary>
    public void Step(float dt, Vector2 wish)
    {
        ReliefConfig c = _config;
        StompedThisStep = false;
        HurtThisStep = false;
        VoidFallThisStep = false;
        Invulnerable = Mathf.Max(0f, Invulnerable - dt);
        HurtFlash = Mathf.Max(0f, HurtFlash - dt);
        _jumpCooldown = Mathf.Max(0f, _jumpCooldown - dt);
        SkillCooldown = Mathf.Max(0f, SkillCooldown - dt);
        if (wish.LengthSquared() > 0.01f)
            Facing = wish.Normalized();

        bool jump = _jumpQueued;
        _jumpQueued = false;
        bool skill = _skillQueued;
        _skillQueued = false;
        if (skill && SkillCooldown <= 0f && Grounded)
            StartSkill(wish);
        if (SkillTime > 0f)
            SteerSkill(dt, wish);

        if (Grounded)
        {
            if (jump && _jumpCooldown <= 0f && SkillTime <= 0f)
            {
                // Le saut garde l'élan du sol, sans poussée : la portée vient de la course, pas du saut.
                TakeOff(c.JumpVelocity);
                Invulnerable = Mathf.Max(Invulnerable, c.JumpInvulnerable);
                _jumpCooldown = c.JumpCooldown;
                Jumps++;
                return;
            }
            if (SkillTime <= 0f)
            {
                // Inertie : la vitesse se prend et se perd en une fraction de seconde, au lieu de sauter d'un coup.
                Vector2 target = wish * c.PlayerSpeed;
                float seconds = target.LengthSquared() > 0.0001f ? c.AccelerationSeconds : c.BrakingSeconds;
                Velocity = Velocity.MoveToward(target, c.PlayerSpeed / seconds * dt);
            }
            Vector2 at = new(Position.X, Position.Y);
            at = MoveBlocked(at, Velocity * dt, Position.Z + c.Step, c.PlayerRadius);
            at = _arena.Obstacles.PushOut(at, c.PlayerRadius);
            // Le joueur tient sur le plus haut sol sous son corps : il ne tombe d'un rebord qu'une fois sorti tout entier,
            // sinon il retomberait à cheval sur la paroi et y resterait collé.
            float ground = _arena.HighestUnder(at.X, at.Y, c.PlayerRadius);
            Position = new Vector3(at.X, at.Y, Position.Z);
            if (ground >= Position.Z - c.Step)
            {
                Position.Z = ground;
                _lastSafe = at;
            }
            else
            {
                Grounded = false;
                AirborneFromJump = false;
                _vz = 0f;
                _takeoffZ = Position.Z;
            }
            return;
        }

        if (SkillTime <= 0f)
            Velocity = Velocity.MoveToward(wish * c.PlayerSpeed, c.AirControl * dt);
        // Le saut flottant pèse moins tant qu'il dure.
        float gravity = SkillTime > 0f && c.Mobility.VisualHeightPixels > 0f ? c.Gravity * FloatGravity : c.Gravity;
        _vz -= gravity * dt;
        Vector2 air = MoveBlocked(new Vector2(Position.X, Position.Y), Velocity * dt, Position.Z, c.PlayerRadius);
        float z = Position.Z + _vz * dt;
        float floor = _arena.HighestUnder(air.X, air.Y, c.PlayerRadius);
        Position = new Vector3(air.X, air.Y, z);
        if (z <= floor)
        {
            Land(floor);
            return;
        }
        if (z < VoidFloor)
            FallIntoVoid();
    }

    /// <summary>Blesse le joueur, sauf s'il est invulnérable ; renvoie vrai si le coup a porté.</summary>
    public bool Hurt(float damage)
    {
        if (Invulnerable > 0f)
            return false;
        TakeDamage(damage);
        return true;
    }

    /// <summary>Rend ses PV au joueur (le bac à sable ne finit pas).</summary>
    public void Revive() => Hp = _config.PlayerHp;

    private void TakeDamage(float damage)
    {
        Hp -= damage;
        HurtFlash = HurtFlashSeconds;
        HurtThisStep = true;
    }

    private void TakeOff(float verticalSpeed)
    {
        Grounded = false;
        AirborneFromJump = true;
        _vz = verticalSpeed;
        _takeoffZ = Position.Z;
    }

    /// <summary>
    /// La capacité du personnage, d'après son profil du jeu : une poussée dans la direction voulue (dash, déphasage), une
    /// glissade qu'on oriente en route, ou un saut flottant qui quitte le sol.
    /// </summary>
    private void StartSkill(Vector2 wish)
    {
        MobilityConfig m = _config.Mobility;
        _skillDir = wish.LengthSquared() > 0.01f ? wish.Normalized() : Facing.Normalized();
        SkillTime = m.DurationSeconds;
        SkillCooldown = m.CooldownSeconds;
        Invulnerable = Mathf.Max(Invulnerable, m.InvulnerabilitySeconds);
        Skills++;
        if (m.VisualHeightPixels > 0f)
            TakeOff(_config.JumpVelocity * FloatJumpLift);
    }

    private void SteerSkill(float dt, Vector2 wish)
    {
        MobilityConfig m = _config.Mobility;
        SkillTime = Mathf.Max(0f, SkillTime - dt);
        if (m.TurnRateDegrees > 0f && wish.LengthSquared() > 0.01f)
        {
            float turn = Mathf.DegToRad(m.TurnRateDegrees) * dt;
            float delta = Mathf.Clamp(_skillDir.AngleTo(wish), -turn, turn);
            _skillDir = _skillDir.Rotated(delta);
        }
        Velocity = _skillDir * _config.PlayerSpeed * m.SpeedMultiplier;
        if (_skillDir.LengthSquared() > 0.01f)
            Facing = _skillDir;
    }

    private void Land(float floor)
    {
        ReliefConfig c = _config;
        float dropLevels = (_takeoffZ - floor) / _arena.LevelHeight;
        Position.Z = floor;
        Grounded = true;
        _vz = 0f;
        Landings++;
        if (dropLevels > c.SafeDropLevels && Invulnerable <= 0f)
            TakeDamage(Mathf.Round((dropLevels - c.SafeDropLevels + 0.5f) * c.DropDamagePerLevel));
        if (dropLevels >= c.StompMinDropLevels)
        {
            // La retombée frappe la horde au même niveau ; c'est à elle d'en tirer les coups.
            Stomps++;
            StompedThisStep = true;
            LastStompAt = Position;
        }
        AirborneFromJump = false;
    }

    private void FallIntoVoid()
    {
        VoidFalls++;
        VoidFallThisStep = true;
        Teleport(_lastSafe);
        TakeDamage(_config.VoidDamage);
        Invulnerable = RespawnInvulnerable;
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
}
