using System.Collections.Generic;
using Godot;

namespace Vestiges.World;

/// <summary>
/// Dessine les routes urbaines comme un overlay continu au-dessus du sol,
/// afin d'éviter l'effet de pincement inhérent aux tiles isométriques.
/// </summary>
public partial class UrbanRoadOverlay : Node2D
{
	private UrbanLayout _layout;
	private TileMapLayer _ground;

	private static readonly Color ShoulderColor = new(0.55f, 0.52f, 0.47f, 0.95f);
	private static readonly Color AsphaltColor = new(0.28f, 0.28f, 0.30f, 0.96f);
	private static readonly Color LaneColor = new(0.70f, 0.58f, 0.25f, 0.80f);
	private const float ShoulderWidth = 14f;
	private const float AsphaltWidth = 11f;
	private const float LaneWidth = 2f;
	private const float HubRadius = 7f;

	public void Initialize(UrbanLayout layout, TileMapLayer ground)
	{
		_layout = layout;
		_ground = ground;
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (_layout == null || _ground == null)
			return;

		foreach (Vector2I roadCell in _layout.RoadCells)
		{
			Vector2 center = _ground.MapToLocal(roadCell);

			DrawConnection(center, roadCell, Vector2I.Right);
			DrawConnection(center, roadCell, Vector2I.Down);

			int connectionCount = 0;
			bool vertical = false;
			bool horizontal = false;

			if (_layout.RoadCells.Contains(roadCell + Vector2I.Up))
			{
				connectionCount++;
				vertical = true;
			}
			if (_layout.RoadCells.Contains(roadCell + Vector2I.Down))
			{
				connectionCount++;
				vertical = true;
			}
			if (_layout.RoadCells.Contains(roadCell + Vector2I.Left))
			{
				connectionCount++;
				horizontal = true;
			}
			if (_layout.RoadCells.Contains(roadCell + Vector2I.Right))
			{
				connectionCount++;
				horizontal = true;
			}

			if (connectionCount >= 3)
				DrawCircle(center, HubRadius + 1.5f, ShoulderColor);

			DrawCircle(center, HubRadius, AsphaltColor);

			if (vertical && !horizontal)
				DrawLine(center + new Vector2(0f, -4f), center + new Vector2(0f, 4f), LaneColor, LaneWidth);
			else if (horizontal && !vertical)
				DrawLine(center + new Vector2(-4f, 0f), center + new Vector2(4f, 0f), LaneColor, LaneWidth);
		}
	}

	private void DrawConnection(Vector2 center, Vector2I roadCell, Vector2I direction)
	{
		Vector2I neighborCell = roadCell + direction;
		if (!_layout.RoadCells.Contains(neighborCell))
			return;

		Vector2 neighbor = _ground.MapToLocal(neighborCell);
		DrawLine(center, neighbor, ShoulderColor, ShoulderWidth);
		DrawLine(center, neighbor, AsphaltColor, AsphaltWidth);

		bool horizontal = direction == Vector2I.Left || direction == Vector2I.Right;
		Vector2 tangent = horizontal ? new Vector2(1f, 0f) : new Vector2(0f, 1f);
		DrawLine(center + tangent * 3f, neighbor - tangent * 3f, LaneColor, LaneWidth);
	}
}
