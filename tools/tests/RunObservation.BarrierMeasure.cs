using System.Globalization;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Events;
using Vestiges.Infrastructure;

namespace Vestiges.Tests;

/// <summary>
/// --measure-barrier [--seconds 30] [--memorials 2] (plan 07 B2c) : la Barrière levée devant le bot, sans autre
/// créature, avec le build de la mesure de l'Indicible (arme de départ, arbalète, haches, fronde). Trois postures,
/// chacune --seconds secondes de jeu, sur une grille neuve : immobile face au battant central ; va-et-vient le long de
/// la grille (deux secondes dans chaque sens) ; aller-retour vers la grille puis en arrière ; va-et-vient avec un battant
/// déjà brisé (verrou : chaque poing est doublé sur la position anticipée). Par posture : poings et
/// chaînes lancés et portés, PV pris aux battants, battants brisés. Les annonces doivent toucher le joueur immobile
/// (poings) et celui qui fait des allers et retours prévisibles (chaîne, verrou).
/// </summary>
public partial class RunObservation
{
	private async Task MeasureBarrier(double seconds, int memorials)
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
		BarrierConfig.TryLoad(out BarrierConfig config, out _);
		BarrierDirector director = _world.GetNode<BarrierDirector>("BarrierDirector");
		int frames = (int)(seconds * 60);
		Vector2 near = Vector2.Zero, far = Vector2.Zero;
		(string Name, System.Func<int, Barrier, Vector2> Move)[] postures =
		{
			("immobile", (_, _) => Vector2.Zero),
			("va-et-vient", (frame, barrier) => barrier.Axis * (frame / 120 % 2 == 0 ? 1f : -1f)),
			// Contre la grille puis en arrière, en boucle, toutes les 1,5 s.
			("aller-retour", (frame, _) => ((frame / 90 % 2 == 0 ? near : far) - _player.GlobalPosition).LimitLength(1f)),
			// Un battant brisé d'emblée : le verrou double chaque poing sur la position anticipée.
			("verrou", (frame, barrier) => barrier.Axis * (frame / 120 % 2 == 0 ? 1f : -1f)),
		};
		Vector2 origin = _player.GlobalPosition;
		foreach ((string name, System.Func<int, Barrier, Vector2> move) in postures)
		{
			_player.GlobalPosition = origin;
			for (int frame = 0; frame < 60; frame++)
			{
				_player.AIInputOverride = Vector2.Up;
				await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			}
			_player.AIInputOverride = Vector2.Zero;
			director.ResetForMeasure();
			if (!director.Raise(memorials))
			{
				GD.PushError("[BarrierMeasure] Barrière non levée");
				return;
			}
			Barrier barrier = director.Barrier;
			Vector2 normal = barrier.NormalToward(_player.GlobalPosition);
			Vector2 middle = barrier.LeafPositions[barrier.LeafPositions.Count / 2];
			_player.GlobalPosition = middle + normal * 60f;
			near = middle + normal * 25f;
			far = middle + normal * 140f;
			_camera.ResetSmoothing();
			if (name == "verrou")
				barrier.Health.Parts[0].TakeDamage(barrier.Health.Parts[0].MaxHp * 2f);
			float startHp = barrier.Health.Current;
			int ended = -1;
			for (int frame = 0; frame < frames; frame++)
			{
				_player.AIInputOverride = move(frame, barrier);
				await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
				if (GetTree().Paused)
					AutoPickLevelUp();
				if (barrier.IsEnded && ended < 0)
					ended = frame;
			}
			_player.AIInputOverride = Vector2.Zero;
			BarrierAttacks attacks = barrier.Attacks;
			GD.Print(string.Create(CultureInfo.InvariantCulture,
				$"[BarrierMeasure] posture={name} secondes={seconds:F0} battants={barrier.Health.OwnPartCount} pv={startHp:F0}->{barrier.Health.Current:F0} brises={barrier.Health.BrokenPartCount} fin_s={(ended < 0 ? -1 : ended / 60f):F1} poings={attacks.FistsLaunched} poings_portes={attacks.FistHits} chaines={attacks.ChainsLaunched} chaines_portees={attacks.ChainHits} distance_grille={barrier.DistanceToCore(_player.GlobalPosition):F0}"));
			barrier.End(false);
			barrier.QueueFree();
			await Frames(30);
		}
		GD.Print($"[RunObservation] RESULT barrier-measure done dégâts_poing={config.FistDamage} dégâts_chaîne={config.ChainDamage}");
	}
}
