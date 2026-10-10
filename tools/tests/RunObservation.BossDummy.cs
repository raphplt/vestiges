using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Infrastructure;
using Vestiges.Spawn;

namespace Vestiges.Tests;

/// <summary>
/// --measure-boss-dummy [--weapons id1,id2] [--seconds 10] [--distance 120] [--radius 50] (plan 07 B1c) : chaque arme,
/// équipée seule au niveau 1, tire sur un mannequin de boss sans autre créature, le joueur immobile. Deux épreuves :
/// une partie à PV propres posée à --distance px à droite, puis trois parties en réserve commune sur un arc à la même
/// distance. Par arme : PV perdus et coups portés à chaque épreuve. Une arme qui n'entame pas le mannequin est un
/// chemin de ciblage qui ne voit pas les parties de boss, ou une portée plus courte que la distance.
/// </summary>
public partial class RunObservation
{
	private async Task MeasureBossDummy(string weaponList, double seconds, float distance, float radius)
	{
		_world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
		SpawnManager spawner = _world.GetNode<SpawnManager>("SpawnManager");
		EnemyPool pool = _world.GetNode<EnemyPool>("EnemyPool");
		for (int frame = 0; frame < 600 && _world.FindChild("GameLoadingOverlay", true, false) != null; frame++)
			await Frames(1);
		await Frames(30);
		_player.AIInputOverride = Vector2.Zero;
		BossPartsConfig.TryLoad(out BossPartsConfig config, out string configError);
		if (config == null || radius > config.MaxBodyRadius)
		{
			GD.PushError($"[BossDummy] Mannequin impossible : {configError ?? $"rayon {radius} au-delà de {config.MaxBodyRadius}"}");
			return;
		}

		List<string> ids = new();
		if (string.IsNullOrEmpty(weaponList))
			foreach (WeaponData data in WeaponDataLoader.GetAll())
				ids.Add(data.Id);
		else
			ids.AddRange(weaponList.Split(','));

		int frames = (int)(seconds * 60);
		int missSingle = 0, missShared = 0;
		List<string> blind = new();
		foreach (string id in ids)
		{
			WeaponData weapon = WeaponDataLoader.Get(id);
			if (weapon == null)
			{
				GD.PushError($"[BossDummy] Arme inconnue : {id}");
				continue;
			}
			(float singleLost, int singleHits) = await DummyTrial(spawner, pool, weapon, frames, radius, new[] { 0f }, false, distance);
			(float sharedLost, int sharedHits) = await DummyTrial(spawner, pool, weapon, frames, radius, new[] { -0.7f, 0f, 0.7f }, true, distance);
			missSingle += singleLost > 0f ? 0 : 1;
			missShared += sharedLost > 0f ? 0 : 1;
			if (singleLost <= 0f || sharedLost <= 0f)
				blind.Add(id);
			GD.Print(string.Create(CultureInfo.InvariantCulture,
				$"[BossDummy] arme={id} famille={weapon.AttackPattern} portee={weapon.Stats.GetValueOrDefault("range"):F0} une_partie_pv={singleLost:F0} coups={singleHits} reserve_commune_pv={sharedLost:F0} coups={sharedHits}"));
		}
		GD.Print(string.Create(CultureInfo.InvariantCulture,
			$"[RunObservation] RESULT boss-dummy armes={ids.Count} distance={distance:F0} rayon={radius:F0} secondes={seconds:F0} sans_degats_une_partie={missSingle} sans_degats_reserve={missShared} aveugles={string.Join(",", blind)}"));
	}

	/// <summary>Une épreuve : l'arme seule, le mannequin posé, <paramref name="frames"/> images de jeu.</summary>
	private async Task<(float Lost, int Hits)> DummyTrial(SpawnManager spawner, EnemyPool pool, WeaponData weapon, int frames,
		float radius, float[] angles, bool shared, float distance)
	{
		while (_player.WeaponSlots.Count > 0)
			_player.RemoveWeapon(0);
		foreach (Node node in GetTree().GetNodesInGroup("enemies"))
			if (node is Enemy existing && existing.IsActive)
				pool.Return(existing);
		// Laisse les effets de l'épreuve précédente s'éteindre.
		await Frames(60);

		const float PartHp = 1_000_000f;
		BossHealth dummy = new("Mannequin", shared ? PartHp : 0f);
		foreach (float angle in angles)
		{
			Enemy part = spawner.SpawnEventEnemy(EnemyGrammar.BossPartId, _player.GlobalPosition + Vector2.FromAngle(angle) * distance);
			dummy.AddPart(part, radius, shared ? 0f : PartHp);
		}
		int hits = 0;
		dummy.PartHit += (_, _) => hits++;
		// Le cône continu part où regarde le joueur, sans viser : immobile, il fait face au mannequin.
		typeof(Core.Player).GetField("_facingDirection", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
			.SetValue(_player, Vector2.Right);
		_player.AddWeapon(weapon);
		for (int frame = 0; frame < frames; frame++)
		{
			_player.AIInputOverride = Vector2.Zero;
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			if (GetTree().Paused)
				AutoPickLevelUp();
		}
		float lost = dummy.Max - dummy.Current;
		foreach (Enemy part in new List<Enemy>(dummy.Parts))
			if (part.IsActive && !part.IsDying)
				pool.Return(part);
		return (lost, hits);
	}
}
