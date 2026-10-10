using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Spawn;

namespace Vestiges.Events;

/// <summary>
/// Lève la Barrière une fois par run (plan 07 B2, DECISIONS §84–85) : à <c>appear_at_sec</c>, ou à la fin de la
/// Résurgence, de son annonce ou de l'accalmie qui la suit. Elle se dresse devant le joueur, en travers de son cap des
/// dernières secondes, avec un battant de plus par Mémorial ravivé. Pendant le combat, la foule visée est réduite.
/// Réglages invalides : la Barrière n'apparaît pas, et le dit au début de la run.
/// </summary>
public partial class BarrierDirector : Node
{
	private const float HeadingSampleSec = 0.25f;
	private const float MinHeadingPx = 24f;
	// Plus qu'un dash entre deux échantillons : un saut, pas une marche.
	private const float MaxStepPx = 160f;
	// Marge aux bords de la carte : le Néant du bord n'est pas un terrain de combat.
	private const float PlacementMargin = 400f;

	private BarrierConfig _config;
	private EventBus _eventBus;
	private SpawnManager _spawner;
	private CrisisManager _crisisManager;
	private Player _player;
	private Barrier _barrier;
	private Vector2[] _trail;
	private int _trailIndex;
	private int _trailCount;
	private float _sampleTimer;
	private Vector2 _lastHeading = Vector2.Up;
	private int _memorials;
	private bool _calm;
	private bool _done;

	public Barrier Barrier => _barrier;
	public int MemorialsAwakened => _memorials;

	public override void _Ready()
	{
		if (!BarrierConfig.TryLoad(out _config, out string error))
		{
			GD.PushError($"[BarrierDirector] La Barrière n'apparaîtra pas pendant cette run : {error}.");
			SetProcess(false);
			return;
		}
		_eventBus = GetNode<EventBus>("/root/EventBus");
		_spawner = GetParent().GetNode<SpawnManager>("SpawnManager");
		_crisisManager = GetParent().GetNodeOrNull<CrisisManager>("CrisisManager");
		_trail = new Vector2[Mathf.Max(2, Mathf.CeilToInt(_config.HeadingWindowSec / HeadingSampleSec) + 1)];
		_eventBus.MemorialActivated += OnMemorialActivated;
		_eventBus.CrisisCalmChanged += OnCalmChanged;
	}

	public override void _ExitTree()
	{
		if (_eventBus == null)
			return;
		_eventBus.MemorialActivated -= OnMemorialActivated;
		_eventBus.CrisisCalmChanged -= OnCalmChanged;
	}

	public override void _Process(double delta)
	{
		if (_done)
			return;
		_player ??= GetTree().GetFirstNodeInGroup("player") as Player;
		if (_player == null)
			return;
		SampleHeading((float)delta);
		if (_spawner.ElapsedSeconds < _config.AppearAtSec || IsBlocked())
			return;
		Raise();
	}

	/// <summary>Lève la Barrière maintenant, devant le joueur (modes de mesure et de capture).</summary>
	public bool Raise(int? memorials = null)
	{
		if (_done || _config == null)
			return false;
		_done = true;
		SetProcess(false);
		_player ??= GetTree().GetFirstNodeInGroup("player") as Player;
		int leaves = _config.LeavesFor(memorials ?? _memorials);
		(bool horizontal, Vector2 center) = PickPlacement(leaves);

		_barrier = new Barrier { Name = "Barrier" };
		Node main = _spawner.GetParent();
		main.AddChild(_barrier);
		if (!_barrier.Build(_config, _player, _spawner, main.GetNodeOrNull("PropContainer"), center, horizontal, leaves,
			Tr(_config.NameKey)))
		{
			GD.PushError("[BarrierDirector] Barrière impossible à poser : retirée.");
			_barrier.Discard();
			_barrier.QueueFree();
			_barrier = null;
			return false;
		}
		_spawner.EncounterDensityMultiplier = _config.CrowdDensity;
		_barrier.Ended += OnBarrierEnded;
		GD.Print($"[BarrierDirector] Barrière levée à {_spawner.ElapsedSeconds:F0} s, {_memorials} Mémorial(aux) ravivé(s)");
		return true;
	}

	/// <summary>
	/// Devant le joueur, en travers de son cap ; si la grille y sortirait de la carte ou tomberait dans l'eau, l'une des
	/// trois autres directions, la plus proche du cap d'abord. À défaut, devant quand même.
	/// </summary>
	private (bool Horizontal, Vector2 Center) PickPlacement(int leaves)
	{
		Vector2 heading = Heading();
		Vector2[] directions = { Vector2.Up, Vector2.Down, Vector2.Left, Vector2.Right };
		System.Array.Sort(directions, (a, b) => b.Dot(heading).CompareTo(a.Dot(heading)));
		World.WorldSetup world = _spawner.GetParent() as World.WorldSetup;
		// Demi-longueur du cœur de la grille (battants et travées fixes), au pas de l'orientation la plus longue.
		Barrier.TryReadLayout("h", out Vector2 strideH, out _, out _);
		Barrier.TryReadLayout("v", out Vector2 strideV, out _, out _);
		float halfCore = (leaves + 2 * _config.FixedSpansEachSide) * 0.5f * Mathf.Max(strideH.Length(), strideV.Length());
		foreach (Vector2 direction in directions)
		{
			bool horizontal = direction.X == 0f;
			Vector2 center = _player.GlobalPosition + direction * _config.DistanceAhead;
			Vector2 along = horizontal ? Vector2.Right : Vector2.Down;
			if (world == null || IsPlaceable(world, center, along * halfCore))
				return (horizontal, center);
			GD.Print($"[BarrierDirector] Pose écartée vers {direction} : hors de la carte ou dans l'eau");
		}
		bool fallback = directions[0].X == 0f;
		return (fallback, _player.GlobalPosition + directions[0] * _config.DistanceAhead);
	}

	private static bool IsPlaceable(World.WorldSetup world, Vector2 center, Vector2 halfSpan)
	{
		Rect2 inside = world.WorldBounds.Grow(-PlacementMargin);
		return inside.HasPoint(center + halfSpan) && inside.HasPoint(center - halfSpan) && !world.IsWaterAt(center);
	}

	private void OnBarrierEnded(bool defeated)
	{
		_spawner.EncounterDensityMultiplier = 1f;
		GD.Print($"[BarrierDirector] Combat fini à {_spawner.ElapsedSeconds:F0} s ({(defeated ? "brisée" : "laissée derrière")}), {_barrier.Health.BrokenPartCount}/{_barrier.Health.OwnPartCount} battant(s), coups reçus : {_barrier.Attacks.FistHits} poing(s), {_barrier.Attacks.ChainHits} chaîne(s)");
	}

#if TOOLS
	/// <summary>Mesure : une nouvelle Barrière peut être levée, la précédente retirée par l'appelant.</summary>
	public void ResetForMeasure()
	{
		DevelopmentMode.RequireTestAccess();
		_done = false;
		_barrier = null;
		_spawner.EncounterDensityMultiplier = 1f;
	}
#endif

	/// <summary>Même règle que les rendez-vous des Souverains : jamais pendant une Résurgence, son annonce ou l'accalmie.</summary>
	private bool IsBlocked() => _calm || _crisisManager != null && (_crisisManager.IsCrisisActive || _crisisManager.IsWarningActive);

	private void SampleHeading(float delta)
	{
		_sampleTimer -= delta;
		if (_sampleTimer > 0f)
			return;
		_sampleTimer = HeadingSampleSec;
		_trail[_trailIndex] = _player.GlobalPosition;
		_trailIndex = (_trailIndex + 1) % _trail.Length;
		_trailCount = Mathf.Min(_trailCount + 1, _trail.Length);
	}

	/// <summary>
	/// Déplacement des dernières secondes, pas à pas : un saut (Faille, téléportation) ne compte pas. À l'arrêt, le
	/// dernier cap connu, vers le haut par défaut.
	/// </summary>
	private Vector2 Heading()
	{
		Vector2 moved = Vector2.Zero;
		for (int i = 1; i < _trailCount; i++)
		{
			Vector2 from = _trail[(_trailIndex - _trailCount + i - 1 + _trail.Length) % _trail.Length];
			Vector2 to = _trail[(_trailIndex - _trailCount + i + _trail.Length) % _trail.Length];
			if (from.DistanceTo(to) <= MaxStepPx)
				moved += to - from;
		}
		if (moved.Length() >= MinHeadingPx)
			_lastHeading = moved;
		return _lastHeading;
	}

	private void OnMemorialActivated() => _memorials++;

	private void OnCalmChanged(bool active) => _calm = active;
}
