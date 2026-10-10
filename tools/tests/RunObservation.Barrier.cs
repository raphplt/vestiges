using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Events;

namespace Vestiges.Tests;

/// <summary>
/// --capture-barrier [--heading up|right] [--memorials N] (plan 07 B2b) : le bot marche quelques secondes vers le cap
/// donné, la Barrière se lève devant lui avec 1 + N battants, puis il s'en approche. Captures plein écran : levée,
/// grille devant le joueur, battant entamé, battant brisé et son coffre. Contrôles : le joueur pousse puis dashe contre
/// un battant fermé sans passer, puis traverse le battant brisé ; RESULT résume les passages.
/// </summary>
public partial class RunObservation
{
	private async Task CaptureBarrier(string headingName, int memorials)
	{
		_world.GetNode("SpawnManager").ProcessMode = ProcessModeEnum.Disabled;
		for (int frame = 0; frame < 600 && _world.FindChild("GameLoadingOverlay", true, false) != null; frame++)
			await Frames(1);
		// Terrain dégagé : le bot ne bute sur rien pendant qu'il prend son cap.
		await MoveToOpenGround(new Vector2(260f, 160f));
		Vector2 heading = headingName == "right" ? Vector2.Right : Vector2.Up;
		for (int frame = 0; frame < 150; frame++)
		{
			_player.AIInputOverride = heading;
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
		_player.AIInputOverride = Vector2.Zero;
		BarrierDirector director = _world.GetNode<BarrierDirector>("BarrierDirector");
		if (!director.Raise(memorials))
		{
			GD.PushError("[RunObservation] Barrière non levée");
			return;
		}
		Barrier barrier = director.Barrier;
		await Frames(20);
		SaveScreen("barrier-0-levee");
		await Frames(60);
		SaveScreen("barrier-1-devant");

		IReadOnlyList<Vector2> leaves = barrier.LeafPositions;
		Vector2 normal = barrier.NormalToward(_player.GlobalPosition);
		// Contre un battant fermé : 2 s de marche vers la grille, puis un dash.
		Vector2 closedLeaf = leaves[0];
		_player.GlobalPosition = closedLeaf + normal * 60f;
		_camera.ResetSmoothing();
		float side = barrier.SideOf(_player.GlobalPosition);
		await Push(-normal, 120, false);
		bool blockedWalking = barrier.SideOf(_player.GlobalPosition) == side;
		await Push(-normal, 40, true);
		bool blockedDash = barrier.SideOf(_player.GlobalPosition) == side;
		float gap = barrier.DistanceToLine(_player.GlobalPosition);

		// Un battant entamé, un autre brisé (le dernier si la grille n'en a qu'un).
		barrier.Health.Parts[0].TakeDamage(barrier.Health.Parts[0].MaxHp * 0.6f);
		Enemy breaking = barrier.Health.Parts[barrier.Health.Parts.Count > 1 ? 1 : 0];
		Vector2 brokenLeaf = breaking.GlobalPosition;
		breaking.TakeDamage(breaking.MaxHp * 2f);
		_player.GlobalPosition = brokenLeaf + normal * 90f;
		_camera.ResetSmoothing();
		await Frames(30);
		SaveScreen("barrier-2-entame-brise");

		side = barrier.SideOf(_player.GlobalPosition);
		await Push(-normal, 120, false);
		bool crossed = barrier.SideOf(_player.GlobalPosition) != side;
		await Frames(10);
		SaveScreen("barrier-3-traverse");
		GD.Print(string.Create(CultureInfo.InvariantCulture,
			$"[RunObservation] RESULT barrier cap={headingName} horizontale={barrier.IsHorizontal} battants={leaves.Count} arrete_a_pied={blockedWalking} arrete_au_dash={blockedDash} ecart_a_la_grille={gap:F0} passe_par_le_battant_brise={crossed} brises={barrier.Health.BrokenPartCount} fin={barrier.IsEnded}"));
	}

	private async Task Push(Vector2 direction, int frames, bool dash)
	{
		for (int frame = 0; frame < frames; frame++)
		{
			_player.AIInputOverride = direction;
			if (dash && frame == 2)
				_player.Mobility.Request();
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
		_player.AIInputOverride = Vector2.Zero;
	}

	private void SaveScreen(string name)
	{
		using Image image = GetViewport().GetTexture().GetImage();
		image.SavePng($"{_output}/{name}.png");
	}
}
