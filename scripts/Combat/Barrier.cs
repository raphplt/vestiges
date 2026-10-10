using System;
using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Spawn;
using Vestiges.World;

namespace Vestiges.Combat;

/// <summary>
/// La Barrière (plan 07 B2) : une grille en travers de la route, ses battants cadenassés et deux ailes de chaînes qui
/// sortent de l'écran. Seuls les battants se frappent (parties de boss à PV propres) ; un battant brisé s'ouvre, laisse
/// passer le joueur et rend un coffre, le dernier plus riche. Le reste de la grille arrête le joueur, dash compris.
/// Horizontale à l'écran si le joueur va vers le haut ou le bas, en diagonale (trois-quarts vertical) sinon ; les pas
/// entre pièces viennent du générateur de sprites (assets/bosses/barrier/barrier_layout.json).
/// Nœud trié en Y à la racine de Main : ses sprites se trient avec les entités.
/// </summary>
public partial class Barrier : Node2D
{
	public const string AssetFolder = "res://assets/bosses/barrier";
	private const uint WallLayer = 4;
	private const float HitFlashSec = 0.12f;

	private sealed class Leaf
	{
		public Enemy Part;
		public Sprite2D Sprite;
		public StaticBody2D Wall;
		public Vector2 Position;
		public bool Damaged;
		public bool Broken;
		public Tween Flash;
	}

	private readonly List<Leaf> _leaves = new();
	private readonly Dictionary<Enemy, Leaf> _leafByPart = new();
	private BarrierConfig _config;
	private EventBus _eventBus;
	private Player _player;
	private BossHealth _health;
	private string _suffix;
	private Vector2 _stride;
	private Vector2 _axis;
	private Vector2 _lineStart;
	private Vector2 _lineEnd;
	private Vector2 _coreStart;
	private Vector2 _coreEnd;
	private bool _ended;
	private BarrierAttacks _attacks;
	// Côté d'où vient le joueur : on ne laisse la Barrière derrière soi qu'en passant de l'autre côté.
	private float _approachSide;

	public BossHealth Health => _health;
	public BarrierAttacks Attacks => _attacks;
	public bool IsHorizontal => _suffix == "h";
	public bool IsEnded => _ended;
	/// <summary>Direction de la grille à l'écran, normée.</summary>
	public Vector2 Axis => _axis;
	public IReadOnlyList<Vector2> LeafPositions
	{
		get
		{
			List<Vector2> positions = new(_leaves.Count);
			foreach (Leaf leaf in _leaves)
				positions.Add(leaf.Position);
			return positions;
		}
	}

	/// <summary>Le combat est fini : battants tous brisés, ou joueur parti au loin.</summary>
	public event Action<bool> Ended;

	/// <summary>
	/// Pose la grille autour de <paramref name="center"/> : horizontale si <paramref name="horizontal"/>, sinon en
	/// diagonale. Faux si un sprite ou la disposition manquent : rien n'est posé.
	/// </summary>
	public bool Build(BarrierConfig config, Player player, SpawnManager spawner, Node propContainer, Vector2 center,
		bool horizontal, int leafCount, string displayName)
	{
		_config = config;
		_player = player;
		_eventBus = GetNode<EventBus>("/root/EventBus");
		_suffix = horizontal ? "h" : "v";
		if (!TryReadLayout(_suffix, out _stride, out Vector2 wingStride, out Vector2 firstWing))
			return false;
		_axis = _stride.Normalized();
		YSortEnabled = true;

		int spans = leafCount + 2 * config.FixedSpansEachSide;
		Vector2[] pillars = new Vector2[spans + 1];
		for (int k = 0; k <= spans; k++)
			pillars[k] = center + _stride * (k - spans * 0.5f);
		_coreStart = pillars[0];
		_coreEnd = pillars[spans];
		int wingCount = Mathf.CeilToInt(config.WingLength / wingStride.Length());
		_lineStart = _coreStart - firstWing - wingStride * wingCount;
		_lineEnd = _coreEnd + firstWing + wingStride * wingCount;
		Texture2D span = LoadTexture($"barrier_span_{_suffix}");
		Texture2D intact = LoadTexture($"barrier_leaf_intact_{_suffix}");
		Texture2D pillar = LoadTexture($"barrier_pillar_{_suffix}");
		Texture2D wing = LoadTexture($"barrier_wing_{_suffix}");
		if (span == null || intact == null || pillar == null || wing == null)
			return false;

		_health = new BossHealth(displayName, 0f);
		// Un battant se touche sur un peu moins de la moitié du pas : deux battants voisins ne se recouvrent pas.
		float radius = Mathf.Min(config.LeafBodyRadius, _stride.Length() * 0.42f);
		for (int j = 0; j < spans; j++)
		{
			Vector2 middle = (pillars[j] + pillars[j + 1]) * 0.5f;
			bool isLeaf = j >= config.FixedSpansEachSide && j < config.FixedSpansEachSide + leafCount;
			Sprite2D sprite = AddPiece(isLeaf ? intact : span, middle);
			if (!isLeaf)
				continue;
			Enemy part = spawner.SpawnEventEnemy(EnemyGrammar.BossPartId, middle);
			if (part == null)
				return false;
			Leaf leaf = new()
			{
				Part = part, Sprite = sprite, Position = middle,
				Wall = AddWall($"LeafWall{j}", pillars[j], pillars[j + 1], config.WallThickness),
			};
			_health.AddPart(part, radius, config.LeafHpFor(leafCount));
			_leaves.Add(leaf);
			_leafByPart[part] = leaf;
		}
		// Les piliers après les travées : à même hauteur (grille horizontale), ils se dessinent devant elles.
		for (int k = 0; k <= spans; k++)
			AddPiece(pillar, pillars[k]);
		for (int m = 0; m < wingCount; m++)
		{
			AddPiece(wing, _coreStart - firstWing - wingStride * (m + 1));
			AddPiece(wing, _coreEnd + firstWing + wingStride * m);
		}

		// Le mur fixe : ailes, travées fixes et piliers ; les battants ont chacun le leur, retiré à leur chute.
		AddWall("WallStart", _lineStart, pillars[config.FixedSpansEachSide], config.WallThickness);
		AddWall("WallEnd", pillars[config.FixedSpansEachSide + leafCount], _lineEnd, config.WallThickness);
		for (int k = config.FixedSpansEachSide + 1; k < config.FixedSpansEachSide + leafCount; k++)
			AddWall($"Pillar{k}", pillars[k] - _axis * 10f, pillars[k] + _axis * 10f, config.WallThickness);

		// Les décors ne se retirent qu'une fois la grille posée : une pose ratée ne laisse pas de trouée.
		ClearProps(propContainer);
		_health.PartHit += OnPartHit;
		_health.PartBroken += OnPartBroken;
		_health.Depleted += OnDepleted;
		_health.ShowBar(_eventBus);
		_approachSide = SideOf(player.GlobalPosition);
		_attacks = new BarrierAttacks(this, config, _suffix);
		Rise();
		GD.Print($"[Barrier] Levée à {center} ({(horizontal ? "horizontale" : "verticale")}), {leafCount} battant(s) de {config.LeafHpFor(leafCount):F0} PV");
		return true;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_ended || _player == null || !IsInstanceValid(_player))
			return;
		float distance = DistanceToLine(_player.GlobalPosition);
		bool passed = SideOf(_player.GlobalPosition) != _approachSide;
		if (distance > (passed ? _config.LeaveDistance : _config.LostDistance))
		{
			End(false);
			return;
		}
		_attacks.Tick(_player, (float)delta, _health.BrokenPartCount > 0);
	}

	/// <summary>Distance au sol du point au cœur de la grille (battants et travées fixes), sans les ailes.</summary>
	public float DistanceToCore(Vector2 point) => Iso.GroundDistanceToSegment(point, _coreStart, _coreEnd);

	/// <summary>Battant encore debout le plus proche du point, au sol ; null s'il n'en reste aucun.</summary>
	public Vector2? NearestStandingLeaf(Vector2 point)
	{
		Vector2? nearest = null;
		float best = float.MaxValue;
		foreach (Leaf leaf in _leaves)
		{
			if (leaf.Broken)
				continue;
			float distance = Iso.GroundDistanceSquared(point, leaf.Position);
			if (distance < best)
			{
				best = distance;
				nearest = leaf.Position;
			}
		}
		return nearest;
	}

	/// <summary>Distance du point à la grille entière, ailes comprises.</summary>
	public float DistanceToLine(Vector2 point) => point.DistanceTo(Geometry2D.GetClosestPointToSegment(point, _lineStart, _lineEnd));

	/// <summary>Côté de la grille où se trouve le point : +1 ou −1.</summary>
	public float SideOf(Vector2 point) => _axis.Cross(point - _coreStart) < 0f ? -1f : 1f;

	/// <summary>Normale à la grille, à l'écran, tournée vers le côté du point.</summary>
	public Vector2 NormalToward(Vector2 point)
	{
		Vector2 normal = _axis.Orthogonal();
		return normal.Dot(point - _coreStart) >= 0f ? normal : -normal;
	}

	private void OnPartHit(Enemy part, float lost)
	{
		if (!_leafByPart.TryGetValue(part, out Leaf leaf) || leaf.Broken)
			return;
		if (!leaf.Damaged && part.HpRatio < _config.DamagedBelow)
		{
			leaf.Damaged = true;
			leaf.Sprite.Texture = LoadTexture($"barrier_leaf_damaged_{_suffix}") ?? leaf.Sprite.Texture;
			ScreenShake.Instance?.ShakeLight();
		}
		leaf.Flash?.Kill();
		leaf.Sprite.SelfModulate = new Color(1.9f, 1.9f, 1.9f);
		leaf.Flash = CreateTween();
		leaf.Flash.TweenProperty(leaf.Sprite, "self_modulate", Colors.White, HitFlashSec);
	}

	private void OnPartBroken(Enemy part)
	{
		if (!_leafByPart.TryGetValue(part, out Leaf leaf) || leaf.Broken)
			return;
		leaf.Broken = true;
		leaf.Flash?.Kill();
		leaf.Sprite.SelfModulate = Colors.White;
		Texture2D broken = LoadTexture($"barrier_leaf_broken_{_suffix}");
		if (broken != null)
			SetPiece(leaf.Sprite, broken, leaf.Position);
		leaf.Wall.QueueFree();
		leaf.Wall = null;
		AudioManager.Play(_config.LeafBrokenAudio, 0.05f);
		ScreenShake.Instance?.ShakeHeavy();
		ScreenShake.Instance?.Hitstop(0.06f);
		bool last = _health.BrokenPartCount >= _health.OwnPartCount;
		GD.Print($"[Barrier] Battant brisé ({_health.BrokenPartCount}/{_health.OwnPartCount})");
		SpawnChest(last ? _config.LastChest : _config.Chest, leaf.Position);
	}

	private void OnDepleted()
	{
		AudioManager.Play(_config.DefeatedAudio, 0.02f);
		// La Barrière entière compte une fois comme une élimination de boss (score, quêtes, Essence).
		_eventBus.EmitSignal(EventBus.SignalName.EnemyKilled, EnemyGrammar.BossPartId, _leaves[^1].Position);
		End(true);
	}

	/// <summary>Pose interrompue : les parties déjà apparues retournent au Néant, la barre se ferme si elle était ouverte.</summary>
	public void Discard()
	{
		_ended = true;
		foreach (Leaf leaf in _leaves)
			if (leaf.Part.IsActive && !leaf.Part.IsDying)
				leaf.Part.Vanish();
		_health?.EndEncounter();
	}

	/// <summary>Fin du combat : la barre se ferme, les battants encore debout deviennent un décor qui ne se frappe plus.</summary>
	public void End(bool defeated)
	{
		if (_ended)
			return;
		_ended = true;
		_attacks?.Stop();
		foreach (Leaf leaf in _leaves)
		{
			if (!leaf.Broken && leaf.Part.IsActive && !leaf.Part.IsDying && leaf.Part.Boss == _health)
				leaf.Part.Vanish();
		}
		_health.EndEncounter();
		GD.Print($"[Barrier] Fin du combat ({(defeated ? "brisée" : "laissée derrière")}), {_health.BrokenPartCount}/{_health.OwnPartCount} battant(s)");
		Ended?.Invoke(defeated);
	}

	private void Rise()
	{
		Modulate = Colors.Transparent;
		Tween rise = CreateTween();
		rise.TweenProperty(this, "modulate", Colors.White, _config.RiseSec);
		AudioManager.Play(_config.RiseAudio, 0f);
		ScreenShake.Instance?.ShakeMedium();
	}

	private Sprite2D AddPiece(Texture2D texture, Vector2 position)
	{
		Sprite2D sprite = new() { Centered = false };
		SetPiece(sprite, texture, position);
		AddChild(sprite);
		return sprite;
	}

	private static void SetPiece(Sprite2D sprite, Texture2D texture, Vector2 position)
	{
		sprite.Texture = texture;
		Vector2 pivot = PropManifest.TryGet(texture, out PropManifest.Entry entry) ? entry.Pivot : texture.GetSize() * new Vector2(0.5f, 1f);
		sprite.Offset = -pivot;
		sprite.Position = position;
	}

	private StaticBody2D AddWall(string name, Vector2 from, Vector2 to, float thickness)
	{
		StaticBody2D body = new() { Name = name, CollisionLayer = WallLayer, CollisionMask = 0 };
		Vector2 segment = to - from;
		body.Position = (from + to) * 0.5f;
		body.Rotation = segment.Angle();
		body.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = new Vector2(segment.Length(), thickness) } });
		AddChild(body);
		return body;
	}

	/// <summary>
	/// Masque les décors posés sur la ligne de la grille, une seule fois à sa levée : un arbre ou un mur ne doit ni
	/// traverser la grille ni boucher un battant.
	/// </summary>
	private void ClearProps(Node propContainer)
	{
		if (propContainer == null)
			return;
		int cleared = 0;
		// Les décors sont rangés en tronçons (PropChunks) : un niveau de plus sous le conteneur.
		foreach (Node chunk in propContainer.GetChildren())
		{
			foreach (Node node in chunk.GetChildren())
			{
				if (node is not EnvironmentProp prop || !prop.Visible)
					continue;
				Vector2 p = prop.GlobalPosition;
				float core = p.DistanceTo(Geometry2D.GetClosestPointToSegment(p, _coreStart, _coreEnd)) - prop.GroundRadius;
				float line = p.DistanceTo(Geometry2D.GetClosestPointToSegment(p, _lineStart, _lineEnd)) - prop.GroundRadius;
				if (core > _config.ClearCore && line > _config.ClearWings)
					continue;
				prop.Withdraw();
				cleared++;
			}
		}
		GD.Print($"[Barrier] {cleared} décor(s) masqué(s) sur la ligne");
	}

	private void SpawnChest(string chestId, Vector2 leafPosition)
	{
		ChestDataLoader.Load();
		ChestData data = ChestDataLoader.Get(chestId);
		if (data == null)
			return;
		// Du côté du joueur, au pied du battant ouvert.
		Vector2 normal = NormalToward(_player.GlobalPosition);
		Vector2 position = leafPosition + normal * 36f;
		PackedScene scene = GD.Load<PackedScene>("res://scenes/world/Chest.tscn");
		Callable.From(() =>
		{
			Chest chest = scene.Instantiate<Chest>();
			chest.GlobalPosition = position;
			GetTree().CurrentScene.AddChild(chest);
			chest.Initialize(data);
		}).CallDeferred();
	}

	private static Texture2D LoadTexture(string stem)
	{
		string path = $"{AssetFolder}/{stem}.png";
		if (ResourceLoader.Exists(path))
			return GD.Load<Texture2D>(path);
		GD.PushError($"[Barrier] Sprite absent : {path}");
		return null;
	}

	/// <summary>Pas écran entre piliers et entre bornes d'aile, écrits par tools/generate_props.py barrier.</summary>
	public static bool TryReadLayout(string suffix, out Vector2 pillarStride, out Vector2 wingStride, out Vector2 firstWing)
	{
		pillarStride = wingStride = firstWing = Vector2.Zero;
		string path = $"{AssetFolder}/barrier_layout.json";
		using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		if (file == null || Json.ParseString(file.GetAsText()).AsGodotDictionary() is not { } root
			|| !root.ContainsKey(suffix))
		{
			GD.PushError($"[Barrier] Disposition illisible : {path}");
			return false;
		}
		Godot.Collections.Dictionary layout = root[suffix].AsGodotDictionary();
		pillarStride = Vector(layout, "pillar_stride");
		wingStride = Vector(layout, "wing_stride");
		firstWing = Vector(layout, "first_wing");
		if (pillarStride.LengthSquared() < 1f || wingStride.LengthSquared() < 1f)
		{
			GD.PushError($"[Barrier] Disposition sans pas : {path}");
			return false;
		}
		return true;
	}

	private static Vector2 Vector(Godot.Collections.Dictionary layout, string key)
	{
		if (!layout.ContainsKey(key))
			return Vector2.Zero;
		Godot.Collections.Array values = layout[key].AsGodotArray();
		return values.Count == 2 ? new Vector2((float)values[0].AsDouble(), (float)values[1].AsDouble()) : Vector2.Zero;
	}
}
