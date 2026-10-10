using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Réglages de la Barrière (plan 07 B2), lus depuis <c>data/events/barrier.json</c> et contrôlés en entier comme ceux de
/// l'Indicible (plan 26 Q6b) : une valeur invalide est refusée avec le nom du champ et la Barrière n'apparaît pas.
/// </summary>
public sealed class BarrierConfig
{
	private const string ConfigPath = "res://data/events/barrier.json";
	private static BarrierConfig _cached;

	public string NameKey { get; private init; }
	public float AppearAtSec { get; private init; }
	public float HeadingWindowSec { get; private init; }
	public float DistanceAhead { get; private init; }
	public int LeavesBase { get; private init; }
	public int LeavesPerMemorial { get; private init; }
	public int LeavesMax { get; private init; }
	public float LeafHpBase { get; private init; }
	public float LeafHpPerLeaf { get; private init; }
	public float LeafBodyRadius { get; private init; }
	public float DamagedBelow { get; private init; }
	public int FixedSpansEachSide { get; private init; }
	public float WingLength { get; private init; }
	public float ClearCore { get; private init; }
	public float ClearWings { get; private init; }
	public float WallThickness { get; private init; }
	public float RiseSec { get; private init; }
	public float CrowdDensity { get; private init; }
	public float LeaveDistance { get; private init; }
	public float LostDistance { get; private init; }
	public float FirstAttackDelay { get; private init; }
	public string AttackFamily { get; private init; }
	public float FistInterval { get; private init; }
	public float FistWarning { get; private init; }
	public float FistRange { get; private init; }
	public float FistRadius { get; private init; }
	public float FistHitMargin { get; private init; }
	public float FistDamage { get; private init; }
	public float LockDelay { get; private init; }
	public float LockLead { get; private init; }
	public float ChainInterval { get; private init; }
	public float ChainWarning { get; private init; }
	public float ChainSweep { get; private init; }
	public float ChainRadius { get; private init; }
	public float ChainArcDeg { get; private init; }
	public float ChainWidthDeg { get; private init; }
	public float ChainDamage { get; private init; }
	public string FistWarningAudio { get; private init; }
	public string FistImpactAudio { get; private init; }
	public string ChainWarningAudio { get; private init; }
	public string ChainSweepAudio { get; private init; }
	public string RiseAudio { get; private init; }
	public string LeafBrokenAudio { get; private init; }
	public string DefeatedAudio { get; private init; }
	public string Chest { get; private init; }
	public string LastChest { get; private init; }

	/// <summary>Battants selon les Mémoriaux ravivés dans la run.</summary>
	public int LeavesFor(int memorials) => Mathf.Clamp(LeavesBase + LeavesPerMemorial * memorials, 1, LeavesMax);

	/// <summary>PV d'un battant d'une grille de <paramref name="leaves"/> battants.</summary>
	public float LeafHpFor(int leaves) => LeafHpBase / Mathf.Max(1, leaves) + LeafHpPerLeaf;

	/// <summary>Configuration du jeu, lue une fois, avec les réglages communs des parties ; faux avec la raison sinon.</summary>
	public static bool TryLoad(out BarrierConfig config, out string error)
	{
		if (_cached != null)
		{
			config = _cached;
			error = null;
			return true;
		}
		config = null;
		if (!BossPartsConfig.TryLoad(out BossPartsConfig parts, out error))
			return false;
		using FileAccess file = FileAccess.Open(ConfigPath, FileAccess.ModeFlags.Read);
		if (file == null)
		{
			error = $"{ConfigPath} absent";
			return false;
		}
		if (!TryParse(file.GetAsText(), parts.MaxBodyRadius, out config, out string parseError))
		{
			error = $"{ConfigPath} : {parseError}";
			return false;
		}
		_cached = config;
		error = null;
		return true;
	}

	public static bool TryParse(string json, float maxBodyRadius, out BarrierConfig config, out string error)
	{
		config = null;
		try
		{
			using JsonDocument document = JsonDocument.Parse(json);
			JsonElement root = document.RootElement;
			JsonConfigReader reader = new(root);
			reader.AllowOnly(root, "racine", "name_key", "appear_at_sec", "heading_window_sec", "distance_ahead", "leaves", "layout", "fight", "attacks", "audio", "rewards");
			JsonElement leaves = reader.Section("leaves");
			reader.AllowOnly(leaves, "leaves", "base", "per_memorial", "max", "hp_base", "hp_per_leaf", "body_radius", "damaged_below");
			JsonElement layout = reader.Section("layout");
			reader.AllowOnly(layout, "layout", "fixed_spans_each_side", "wing_length", "clear_core", "clear_wings", "wall_thickness", "rise_sec");
			JsonElement fight = reader.Section("fight");
			reader.AllowOnly(fight, "fight", "crowd_density", "leave_distance", "lost_distance");
			JsonElement attacks = reader.Section("attacks");
			reader.AllowOnly(attacks, "attacks", "first_delay_sec", "fx_family", "fist", "chain");
			JsonElement fist = reader.Section(attacks, "fist");
			reader.AllowOnly(fist, "attacks.fist", "interval_sec", "warning_sec", "range", "radius", "hit_margin", "damage", "lock_delay_sec", "lock_lead_sec");
			JsonElement chain = reader.Section(attacks, "chain");
			reader.AllowOnly(chain, "attacks.chain", "interval_sec", "warning_sec", "sweep_sec", "radius", "arc_deg", "width_deg", "damage");
			JsonElement audio = reader.Section("audio");
			reader.AllowOnly(audio, "audio", "rise", "leaf_broken", "defeated", "fist_warning", "fist_impact", "chain_warning", "chain_sweep");
			JsonElement rewards = reader.Section("rewards");
			reader.AllowOnly(rewards, "rewards", "chest", "last_chest");
			BarrierConfig parsed = new()
			{
				NameKey = reader.Text(root, "name_key"),
				AppearAtSec = reader.NonNegative(root, "appear_at_sec"),
				HeadingWindowSec = reader.Positive(root, "heading_window_sec"),
				DistanceAhead = reader.Positive(root, "distance_ahead"),
				LeavesBase = reader.Integer(leaves, "base", 1, 8),
				LeavesPerMemorial = reader.Integer(leaves, "per_memorial", 0, 8),
				LeavesMax = reader.Integer(leaves, "max", 1, 8),
				LeafHpBase = reader.NonNegative(leaves, "hp_base"),
				LeafHpPerLeaf = reader.Positive(leaves, "hp_per_leaf"),
				LeafBodyRadius = reader.Positive(leaves, "body_radius"),
				DamagedBelow = reader.Ratio(leaves, "damaged_below"),
				FixedSpansEachSide = reader.Integer(layout, "fixed_spans_each_side", 0, 8),
				WingLength = reader.Positive(layout, "wing_length"),
				ClearCore = reader.NonNegative(layout, "clear_core"),
				ClearWings = reader.NonNegative(layout, "clear_wings"),
				WallThickness = reader.Positive(layout, "wall_thickness"),
				RiseSec = reader.Positive(layout, "rise_sec"),
				CrowdDensity = reader.Ratio(fight, "crowd_density"),
				LeaveDistance = reader.Positive(fight, "leave_distance"),
				LostDistance = reader.Positive(fight, "lost_distance"),
				FirstAttackDelay = reader.NonNegative(attacks, "first_delay_sec"),
				AttackFamily = reader.Text(attacks, "fx_family"),
				FistInterval = reader.Positive(fist, "interval_sec"),
				FistWarning = reader.Positive(fist, "warning_sec"),
				FistRange = reader.Positive(fist, "range"),
				FistRadius = reader.Positive(fist, "radius"),
				FistHitMargin = reader.NonNegative(fist, "hit_margin"),
				FistDamage = reader.NonNegative(fist, "damage"),
				LockDelay = reader.Positive(fist, "lock_delay_sec"),
				LockLead = reader.NonNegative(fist, "lock_lead_sec"),
				ChainInterval = reader.Positive(chain, "interval_sec"),
				ChainWarning = reader.Positive(chain, "warning_sec"),
				ChainSweep = reader.Positive(chain, "sweep_sec"),
				ChainRadius = reader.Positive(chain, "radius"),
				ChainArcDeg = reader.Number(chain, "arc_deg"),
				ChainWidthDeg = reader.Positive(chain, "width_deg"),
				ChainDamage = reader.NonNegative(chain, "damage"),
				FistWarningAudio = reader.Text(audio, "fist_warning"),
				FistImpactAudio = reader.Text(audio, "fist_impact"),
				ChainWarningAudio = reader.Text(audio, "chain_warning"),
				ChainSweepAudio = reader.Text(audio, "chain_sweep"),
				RiseAudio = reader.Text(audio, "rise"),
				LeafBrokenAudio = reader.Text(audio, "leaf_broken"),
				DefeatedAudio = reader.Text(audio, "defeated"),
				Chest = reader.Text(rewards, "chest"),
				LastChest = reader.Text(rewards, "last_chest"),
			};
			if (reader.Error == null && parsed.LeavesMax < parsed.LeavesBase)
				reader.Fail($"leaves.max ({parsed.LeavesMax}) inférieur à leaves.base ({parsed.LeavesBase})");
			if (reader.Error == null && parsed.LeafBodyRadius > maxBodyRadius)
				reader.Fail($"leaves.body_radius : {parsed.LeafBodyRadius} au-delà du rayon de touche permis ({maxBodyRadius})");
			if (reader.Error == null && (parsed.ChainArcDeg <= 0f || parsed.ChainArcDeg > 180f))
				reader.Fail($"attacks.chain.arc_deg : {parsed.ChainArcDeg} hors de ]0 ; 180]");
			if (reader.Error == null && !EnemyContract.IsFamily(parsed.AttackFamily))
				reader.Fail($"attacks.fx_family : famille « {parsed.AttackFamily} » inconnue");
			foreach (string sound in new[] { parsed.RiseAudio, parsed.LeafBrokenAudio, parsed.DefeatedAudio, parsed.FistWarningAudio,
				parsed.FistImpactAudio, parsed.ChainWarningAudio, parsed.ChainSweepAudio })
				if (reader.Error == null && !DataKeySets.TopLevelKeys(AudioManager.SoundBankPath).Contains(sound))
					reader.Fail($"audio : son « {sound} » absent de la banque");
			ChestDataLoader.Load();
			foreach (string chest in new[] { parsed.Chest, parsed.LastChest })
				if (reader.Error == null && ChestDataLoader.Get(chest) == null)
					reader.Fail($"rewards : coffre « {chest} » inconnu");
			error = reader.Error;
			config = error == null ? parsed : null;
			return error == null;
		}
		catch (JsonException ex)
		{
			error = $"JSON illisible : {ex.Message}";
			return false;
		}
	}
}
