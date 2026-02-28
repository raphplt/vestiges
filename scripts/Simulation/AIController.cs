using Godot;
using Vestiges.Base;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;
using Vestiges.Score;
using Vestiges.World;

namespace Vestiges.Simulation;

/// <summary>
/// Cerveau de l'IA de simulation. Pilote le Player existant via AIInputOverride.
/// Machine à 5 états : Roam, Fight, Retreat, Interact, NightDefend.
/// Gère automatiquement la sélection de perks et la fin de run.
/// </summary>
public partial class AIController : Node
{
    public enum AIState
    {
        Roam,
        Fight,
        Retreat,
        Interact,
        NightDefend
    }

    [Signal]
    public delegate void RunCompletedEventHandler();

    private Player _player;
    private PerkManager _perkManager;
    private ScoreManager _scoreManager;
    private EventBus _eventBus;
    private AIProfile _profile;
    private PerkStrategy _perkStrategy;

    private AIState _currentState = AIState.Roam;
    private float _decisionCooldown;
    private bool _isNight;
    private bool _runEnded;

    // Roam state
    private Vector2 _roamDirection;
    private float _roamTimer;

    // Fight state
    private bool _strafeRight = true;
    private float _strafeFlipTimer;

    // Interact state
    private Node2D _interactTarget;

    public void Initialize(Player player, PerkManager perkManager, ScoreManager scoreManager,
        AIProfile profile, PerkStrategy perkStrategy)
    {
        _player = player;
        _perkManager = perkManager;
        _scoreManager = scoreManager;
        _profile = profile;
        _perkStrategy = perkStrategy;

        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _Ready()
    {
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _eventBus.DayPhaseChanged += OnDayPhaseChanged;
        _eventBus.EntityDied += OnEntityDied;

        if (_perkManager != null)
            _perkManager.PerkChoicesReady += OnPerkChoicesReady;

        _roamDirection = RandomDirection();
        _roamTimer = 2f;
    }

    public override void _ExitTree()
    {
        if (_eventBus != null)
        {
            _eventBus.DayPhaseChanged -= OnDayPhaseChanged;
            _eventBus.EntityDied -= OnEntityDied;
        }

        if (_perkManager != null)
            _perkManager.PerkChoicesReady -= OnPerkChoicesReady;
    }

    public override void _Process(double delta)
    {
        if (_runEnded || _player == null || _player.IsDead)
        {
            if (_player != null)
                _player.AIInputOverride = Vector2.Zero;
            return;
        }

        float dt = (float)delta;
        _decisionCooldown -= dt;

        if (_decisionCooldown <= 0f)
        {
            _decisionCooldown = _profile.DecisionInterval;
            AIState newState = EvaluateState();
            if (newState != _currentState)
                _currentState = newState;
        }

        ExecuteState(dt);
    }

    // --- State Evaluation ---

    private AIState EvaluateState()
    {
        float hpRatio = _player.CurrentHp / _player.EffectiveMaxHp;

        // Priority 1: Retreat when HP is critical
        if (_profile.RetreatThreshold > 0f && hpRatio < _profile.RetreatThreshold)
            return AIState.Retreat;

        // Priority 2: Fight nearby enemies (day or night)
        bool hasEnemy = FindNearestEnemy(out _, out float dist);
        if (hasEnemy && dist < _player.EffectiveAttackRange * 2f)
            return AIState.Fight;

        // Priority 3: During night, move toward Foyer when no enemies close
        if (_isNight && _profile.DefendAtNight)
            return AIState.NightDefend;

        // Priority 4: Interact with POIs/chests during day
        if (_profile.InteractsDuringDay && !_isNight && hpRatio > 0.5f && FindInteractable() != null)
            return AIState.Interact;

        return AIState.Roam;
    }

    // --- State Execution ---

    private void ExecuteState(float dt)
    {
        switch (_currentState)
        {
            case AIState.Roam:
                ExecuteRoam(dt);
                break;
            case AIState.Fight:
                ExecuteFight(dt);
                break;
            case AIState.Retreat:
                ExecuteRetreat();
                break;
            case AIState.Interact:
                ExecuteInteract();
                break;
            case AIState.NightDefend:
                ExecuteNightDefend();
                break;
        }
    }

    private void ExecuteRoam(float dt)
    {
        _roamTimer -= dt;
        if (_roamTimer <= 0f)
        {
            _roamDirection = GetRoamDirection();
            _roamTimer = _profile.RoamChangeDirInterval + (float)GD.RandRange(0, 2);
        }
        _player.AIInputOverride = _roamDirection;
    }

    private void ExecuteFight(float dt)
    {
        if (!FindNearestEnemy(out Vector2 enemyPos, out float dist))
        {
            _player.AIInputOverride = Vector2.Zero;
            return;
        }

        Vector2 toEnemy = (enemyPos - _player.GlobalPosition).Normalized();
        float attackRange = _player.EffectiveAttackRange;

        if (_profile.CanKite && dist < attackRange * 0.5f)
        {
            // Kite : reculer + strafe quand l'ennemi est trop proche
            Vector2 away = -toEnemy;
            Vector2 strafe = new Vector2(-toEnemy.Y, toEnemy.X) * (_strafeRight ? 1f : -1f);
            _player.AIInputOverride = (away * 0.7f + strafe * 0.3f).Normalized();

            _strafeFlipTimer -= dt;
            if (_strafeFlipTimer <= 0f)
            {
                _strafeRight = !_strafeRight;
                _strafeFlipTimer = (float)GD.RandRange(0.5, 1.2);
            }
        }
        else if (dist > attackRange * 0.8f)
        {
            // Avancer pour entrer dans le range
            _player.AIInputOverride = toEnemy;
        }
        else
        {
            // En range : strafe pour esquiver tout en maintenant la distance
            _strafeFlipTimer -= dt;
            if (_strafeFlipTimer <= 0f)
            {
                _strafeRight = !_strafeRight;
                _strafeFlipTimer = (float)GD.RandRange(0.6, 1.5);
            }
            Vector2 strafe = new Vector2(-toEnemy.Y, toEnemy.X) * (_strafeRight ? 1f : -1f);
            _player.AIInputOverride = strafe;
        }
    }

    private void ExecuteRetreat()
    {
        float distToFoyer = _player.GlobalPosition.Length();
        if (distToFoyer < 80f)
        {
            // Arrivé au Foyer : orbiter
            Vector2 orbit = new Vector2(-_player.GlobalPosition.Y, _player.GlobalPosition.X).Normalized();
            _player.AIInputOverride = orbit;
        }
        else
        {
            Vector2 toFoyer = -_player.GlobalPosition.Normalized();
            _player.AIInputOverride = toFoyer;
        }
    }

    private void ExecuteInteract()
    {
        Node2D target = FindInteractable();
        if (target == null)
        {
            _interactTarget = null;
            _player.AIInputOverride = Vector2.Zero;
            return;
        }

        _interactTarget = target;
        float dist = _player.GlobalPosition.DistanceTo(target.GlobalPosition);

        if (dist < _player.InteractRange * 0.9f)
        {
            _player.AIInputOverride = Vector2.Zero;
            if (!_player.IsHarvesting)
                _player.AITriggerInteract();
        }
        else
        {
            Vector2 toTarget = (target.GlobalPosition - _player.GlobalPosition).Normalized();
            _player.AIInputOverride = toTarget;
        }
    }

    private void ExecuteNightDefend()
    {
        float distToFoyer = _player.GlobalPosition.Length();

        if (distToFoyer > 120f)
        {
            Vector2 toFoyer = -_player.GlobalPosition.Normalized();
            _player.AIInputOverride = toFoyer;
        }
        else
        {
            // Orbiter autour du Foyer en combattant
            if (FindNearestEnemy(out Vector2 enemyPos, out float enemyDist) && enemyDist < _player.EffectiveAttackRange * 1.5f)
            {
                Vector2 toEnemy = (enemyPos - _player.GlobalPosition).Normalized();
                if (_profile.CanKite && enemyDist < _player.EffectiveAttackRange * 0.3f)
                    _player.AIInputOverride = -toEnemy;
                else if (enemyDist > _player.EffectiveAttackRange * 0.7f)
                    _player.AIInputOverride = toEnemy;
                else
                    _player.AIInputOverride = new Vector2(-toEnemy.Y, toEnemy.X);
            }
            else
            {
                Vector2 orbit = new Vector2(-_player.GlobalPosition.Y, _player.GlobalPosition.X).Normalized();
                _player.AIInputOverride = orbit;
            }
        }
    }

    // --- Helpers ---

    private bool FindNearestEnemy(out Vector2 position, out float distance)
    {
        position = Vector2.Zero;
        distance = float.MaxValue;

        Godot.Collections.Array<Node> enemies = GetTree().GetNodesInGroup("enemies");
        foreach (Node node in enemies)
        {
            if (node is Node2D enemy && !enemy.IsQueuedForDeletion())
            {
                float d = _player.GlobalPosition.DistanceTo(enemy.GlobalPosition);
                if (d < distance)
                {
                    distance = d;
                    position = enemy.GlobalPosition;
                }
            }
        }

        return distance < float.MaxValue;
    }

    private Node2D FindInteractable()
    {
        float range = _player.InteractRange * 2f;
        Node2D best = null;
        float bestDist = range;

        // POIs
        foreach (Node node in GetTree().GetNodesInGroup("pois"))
        {
            if (node is PointOfInterest poi && poi.CanInteract)
            {
                float d = _player.GlobalPosition.DistanceTo(poi.GlobalPosition);
                if (d < bestDist)
                {
                    best = poi;
                    bestDist = d;
                }
            }
        }

        // Chests
        foreach (Node node in GetTree().GetNodesInGroup("chests"))
        {
            if (node is Chest chest && chest.CanOpen)
            {
                float d = _player.GlobalPosition.DistanceTo(chest.GlobalPosition);
                if (d < bestDist)
                {
                    best = chest;
                    bestDist = d;
                }
            }
        }

        // Resources
        foreach (Node node in GetTree().GetNodesInGroup("resources"))
        {
            if (node is ResourceNode res && !res.IsExhausted)
            {
                float d = _player.GlobalPosition.DistanceTo(res.GlobalPosition);
                if (d < bestDist)
                {
                    best = res;
                    bestDist = d;
                }
            }
        }

        return best;
    }

    private Vector2 GetRoamDirection()
    {
        float distFromCenter = _player.GlobalPosition.Length();

        // Biais vers le centre si trop loin
        if (distFromCenter > _profile.RoamMaxRadius)
            return (-_player.GlobalPosition.Normalized() + RandomDirection() * 0.3f).Normalized();

        // Biais vers l'extérieur pour explorer
        if (distFromCenter < 100f)
            return RandomDirection();

        // Mix entre direction actuelle et random
        return (_roamDirection * 0.5f + RandomDirection() * 0.5f).Normalized();
    }

    private static Vector2 RandomDirection()
    {
        float angle = (float)GD.RandRange(0, Mathf.Tau);
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }

    // --- Event Handlers ---

    private void OnDayPhaseChanged(string phase)
    {
        _isNight = phase is "Night" or "Dusk";
    }

    private void OnPerkChoicesReady(string[] perkIds)
    {
        if (perkIds.Length == 0 || _perkStrategy == null) return;

        string selected = _perkStrategy.SelectPerk(perkIds, _player);
        if (selected == null) return;

        // Unpause si nécessaire (LevelUpScreen pourrait avoir pausé)
        if (GetTree().Paused)
            GetTree().Paused = false;

        CallDeferred(MethodName.DeferredSelectPerk, selected);
    }

    private void DeferredSelectPerk(string perkId)
    {
        _perkManager?.SelectPerk(perkId);
    }

    private void OnEntityDied(Node entity)
    {
        if (entity is not Player || _runEnded) return;

        _runEnded = true;

        // Build le RunRecord sans sauvegarder (protection des données joueur)
        RunRecord record = _scoreManager.BuildRunRecord();

        // Tag avec les métadonnées de simulation
        BatchRunner batchRunner = GetNodeOrNull<BatchRunner>("/root/BatchRunner");
        if (batchRunner != null)
        {
            SimulationRunConfig config = batchRunner.CurrentRunConfig;
            record.SimLabel = config.Label;
            record.SimProfile = config.ProfileName;
            record.SimPerkStrategy = config.PerkStrategyName;

            // Unpause (Die() pause après le tween)
            GetTree().Paused = false;

            batchRunner.OnRunCompleted(record);
        }
        else
        {
            GD.Print($"[AIController] Run ended (standalone) — Night {record.NightsSurvived}, Score {record.Score}");
        }
    }
}
