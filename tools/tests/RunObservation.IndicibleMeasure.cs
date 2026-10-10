using System.Globalization;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Infrastructure;
using Vestiges.Spawn;

namespace Vestiges.Tests;

/// <summary>
/// --measure-indicible [--seconds 20] [--phase 1|2|3] (plan 07 B3) : l'Indicible levé sur le joueur, sans autre créature, avec le build
/// de la capture de fin de partie (arme de départ, arbalète, haches, fronde). Trois postures, chacune --seconds secondes
/// de jeu sur un boss neuf à réserve immense (×100) : immobile ; en cercle ; va-et-vient en ligne droite ; en jouant la brèche des vagues. Par posture :
/// PV pris à la réserve, mains levées, éclairs et prises lancés et portés, dérive due au vent. Les éclairs et les prises
/// doivent toucher le joueur immobile, beaucoup moins celui qui bouge.
/// </summary>
public partial class RunObservation
{
	private Indicible _measuredIndicible;

	private async Task MeasureIndicible(double phaseSeconds, int phase)
	{
		_world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
		for (int frame = 0; frame < 600 && _world.FindChild("GameLoadingOverlay", true, false) != null; frame++)
			await Frames(1);
		foreach (string weapon in new[] { "crossbow", "throwing_axes", "sling" })
			_player.AddWeapon(WeaponDataLoader.Get(weapon));
		// Chaque coup doit porter pour être compté : ni mode dieu ni invulnérabilité après un coup, mais une vie immense.
		_player.IsGodMode = false;
		_player.DisableDefenseForTests();
		typeof(Core.Player).GetField("_currentHp", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
			.SetValue(_player, 1_000_000f);
		SpawnManager spawner = _world.GetNode<SpawnManager>("SpawnManager");
		int frames = (int)(phaseSeconds * 60);
		(string Name, System.Func<int, Vector2> Move)[] postures =
		{
			("immobile", _ => Vector2.Zero),
			("cercle", frame => Vector2.FromAngle(frame / 60f * 0.9f)),
			("va-et-vient", frame => frame / 90 % 2 == 0 ? Vector2.Right : Vector2.Left),
			// Joue la brèche : dès l'annonce d'une vague, il va dans la brèche la plus proche et y reste.
			("brèche", _ => _measuredIndicible?.Wave.NearestBreach(_player.GlobalPosition) is Vector2 safe
				&& safe.DistanceTo(_player.GlobalPosition) > 6f ? (safe - _player.GlobalPosition).Normalized() : Vector2.Zero),
		};
		foreach ((string name, System.Func<int, Vector2> move) in postures)
		{
			Indicible boss = new() { Name = "IndicibleMeasureBoss" };
			_measuredIndicible = boss;
			_world.AddChild(boss);
			if (!boss.Initialize(100f, 1f, _player, spawner))
			{
				GD.PushError("[IndicibleMeasure] Indicible non levé");
				return;
			}
			await ForceIndiciblePhase(boss, phase);
			float startHp = boss.Health.Current;
			Vector2 start = _player.GlobalPosition;
			for (int frame = 0; frame < frames; frame++)
			{
				_player.AIInputOverride = move(frame);
				await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
				if (GetTree().Paused)
					AutoPickLevelUp();
			}
			_player.AIInputOverride = Vector2.Zero;
			GD.Print(string.Create(CultureInfo.InvariantCulture,
				$"[IndicibleMeasure] phase={boss.Health.Phase + 1} posture={name} secondes={phaseSeconds:F0} reserve={startHp:F0}->{boss.Health.Current:F0} mains={boss.HandsRaised} eclairs_portes={boss.LightningHits}/{boss.LightningStrikes} prises_portees={boss.GrabHits}/{boss.Grabs} eau_profonde_s={boss.Tide.DeepSeconds:F1} eau_basse_s={boss.Tide.ShallowSeconds:F1} coups_de_l_eau={boss.TideHits} vagues_portees={boss.Wave.WaveHits}/{boss.Wave.Waves} pv_pris_decouvert={boss.Wave.WindowDamage:F0} deplacement={_player.GlobalPosition.DistanceTo(start):F0}"));
			boss.QueueFree();
			await Frames(30);
		}
		GD.Print("[RunObservation] RESULT indicible-measure done");
	}

	/// <summary>Phase <paramref name="phase"/> (1 à 3) : la première main levée prend ce qu'il faut de réserve pour y entrer.</summary>
	private async Task ForceIndiciblePhase(Indicible boss, int phase)
	{
		IndicibleConfig.TryLoad(out IndicibleConfig config, out _);
		int thresholds = Mathf.Min(phase - 1, config.PhaseThresholds.Count);
		for (int frame = 0; frame < 600 && boss.Health.Phase < thresholds; frame++)
		{
			foreach (Enemy hand in boss.Health.Parts)
			{
				if (!hand.IsActive || hand.IsDying)
					continue;
				hand.TakeDamage(boss.Health.Current - boss.Health.Max * config.PhaseThresholds[thresholds - 1] + 1f);
				break;
			}
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}
}
