using System.Collections.Generic;
using Godot;

namespace Vestiges.Core;

/// <summary>
/// Ombres au sol des entités nombreuses (créatures), dessinées en lots : un <c>MultiMeshInstance2D</c> par largeur
/// d'ombre, rempli en un seul appel par image. Mille ombres en nœuds coûtaient mille éléments au rendu et à chaque
/// déplacement (plan 29, lot C). Même calque que les ombres en nœuds (z −1), sous les entités.
/// </summary>
public partial class GroundShadowLayer : Node2D
{
	// Transform2D sur 8 flottants puis couleur sur 4, dans l'ordre du tampon d'un MultiMesh 2D avec couleurs.
	private const int FloatsPerInstance = 12;

	private sealed class Batch
	{
		public MultiMeshInstance2D Instance;
		public MultiMesh Mesh;
		// Toujours de la taille exacte de la capacité du MultiMesh : transmis tel quel, sans copie.
		public float[] Buffer = System.Array.Empty<float>();
		public int Count;
	}

	private static readonly List<ShadowCaster> Casters = new(512);
	private static GroundShadowLayer _layer;
	private readonly Dictionary<int, Batch> _batches = new();

	public static void Add(ShadowCaster caster)
	{
		if (caster.Slot >= 0)
			return;
		caster.Slot = Casters.Count;
		Casters.Add(caster);
		EnsureLayer(caster.Owner);
	}

	public static void Remove(ShadowCaster caster)
	{
		int slot = caster.Slot;
		if (slot < 0)
			return;
		int last = Casters.Count - 1;
		ShadowCaster moved = Casters[last];
		Casters[slot] = moved;
		moved.Slot = slot;
		Casters.RemoveAt(last);
		caster.Slot = -1;
	}

	/// <summary>La couche naît avec la première ombre, dans la scène courante : bancs et tests compris.</summary>
	private static void EnsureLayer(Node2D owner)
	{
		// Pas IsInsideTree : la couche n'entre dans l'arbre qu'en différé, les inscriptions de la même image la partagent.
		if (_layer != null && IsInstanceValid(_layer))
			return;
		Node scene = owner.GetTree().CurrentScene ?? owner.GetTree().Root;
		_layer = new GroundShadowLayer { Name = nameof(GroundShadowLayer), ZIndex = -1 };
		scene.CallDeferred(Node.MethodName.AddChild, _layer);
	}

	public override void _ExitTree()
	{
		if (_layer == this)
			_layer = null;
	}

	public override void _Process(double delta)
	{
		foreach (Batch batch in _batches.Values)
			batch.Count = 0;
		for (int i = Casters.Count - 1; i >= 0; i--)
		{
			ShadowCaster caster = Casters[i];
			// Entité libérée sans s'être retirée (fin de scène) : elle ne projette plus rien.
			if (!IsInstanceValid(caster.Owner))
			{
				Remove(caster);
				continue;
			}
			Batch batch = BatchFor(caster.Width);
			int count = batch.Count++;
			EnsureCapacity(batch, batch.Count);
			Vector2 center = caster.Owner.GlobalPosition + caster.Offset * caster.Scale;
			int at = count * FloatsPerInstance;
			float[] buffer = batch.Buffer;
			buffer[at] = caster.Scale;
			buffer[at + 1] = 0f;
			buffer[at + 2] = 0f;
			buffer[at + 3] = Mathf.Round(center.X);
			buffer[at + 4] = 0f;
			buffer[at + 5] = caster.Scale;
			buffer[at + 6] = 0f;
			buffer[at + 7] = Mathf.Round(center.Y);
			buffer[at + 8] = 1f;
			buffer[at + 9] = 1f;
			buffer[at + 10] = 1f;
			buffer[at + 11] = caster.Alpha;
		}
		foreach (Batch batch in _batches.Values)
			Upload(batch);
	}

	private Batch BatchFor(int width)
	{
		if (_batches.TryGetValue(width, out Batch batch))
			return batch;
		ImageTexture texture = GroundShadow.TextureFor(width);
		Vector2 half = texture.GetSize() * 0.5f;
		ArrayMesh quad = new();
		Godot.Collections.Array arrays = new();
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = new Vector2[] { -half, new(half.X, -half.Y), half, new(-half.X, half.Y) };
		arrays[(int)Mesh.ArrayType.TexUV] = new Vector2[] { Vector2.Zero, Vector2.Right, Vector2.One, Vector2.Down };
		arrays[(int)Mesh.ArrayType.Index] = new int[] { 0, 1, 2, 0, 2, 3 };
		quad.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
		MultiMesh mesh = new()
		{
			TransformFormat = MultiMesh.TransformFormatEnum.Transform2D,
			UseColors = true,
			Mesh = quad,
		};
		MultiMeshInstance2D instance = new()
		{
			Name = $"Shadows{width}",
			Multimesh = mesh,
			Texture = texture,
			TextureFilter = TextureFilterEnum.Nearest,
		};
		AddChild(instance);
		batch = new Batch { Instance = instance, Mesh = mesh };
		_batches[width] = batch;
		return batch;
	}

	private static void EnsureCapacity(Batch batch, int needed)
	{
		int capacity = batch.Mesh.InstanceCount;
		if (capacity >= needed)
			return;
		capacity = Mathf.Max(needed, Mathf.Max(64, capacity * 2));
		batch.Mesh.InstanceCount = capacity;
		System.Array.Resize(ref batch.Buffer, capacity * FloatsPerInstance);
	}

	private static void Upload(Batch batch)
	{
		if (batch.Count > 0)
			RenderingServer.MultimeshSetBuffer(batch.Mesh.GetRid(), batch.Buffer);
		batch.Mesh.VisibleInstanceCount = batch.Count;
	}
}
