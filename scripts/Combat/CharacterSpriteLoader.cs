using System.Collections.Generic;
using Godot;

namespace Vestiges.Combat;

/// <summary>
/// Charge et met en cache les SpriteFrames de personnages depuis les fichiers PNG individuels.
/// Convention de nommage : char_{id}_{DIR}_{ACTION}_{FRAME:D2}.png
/// Directions : E, SE, S, SW, W, NW, N, NE (les quatre diagonales seules restent acceptées).
/// Actions : idle, walk, dash, hurt, death.
/// </summary>
public static class CharacterSpriteLoader
{
	private static readonly Dictionary<string, SpriteFrames> _cache = new();

	private static readonly string[] Directions = CharacterFacing.DirectionNames;
	private static readonly StringName[] CardinalIdle = { "E_idle", "S_idle", "W_idle", "N_idle" };
	private static readonly string[] Actions = { "idle", "walk", "dash", "hurt", "death" };

	private static readonly Dictionary<string, float> AnimSpeeds = new()
	{
		{ "idle", 5f },
		{ "walk", 8f },
		{ "dash", 12f },
		{ "hurt", 10f },
		{ "death", 8f }
	};

	private static readonly HashSet<string> LoopingAnims = new() { "idle", "walk" };

	public static SpriteFrames LoadOrGet(string charId, string folder)
	{
		if (_cache.TryGetValue(charId, out SpriteFrames cached))
			return cached;

		SpriteFrames frames = new();

		// Supprimer l'animation "default" créée automatiquement
		if (frames.HasAnimation("default"))
			frames.RemoveAnimation("default");

		string basePath = $"res://assets/characters/{folder}";
		int totalAnims = 0;

		foreach (string dir in Directions)
		{
			foreach (string action in Actions)
			{
				List<Texture2D> textures = LoadFrameSequence(basePath, charId, dir, action);
				if (textures.Count == 0)
					continue;

				string animName = $"{dir}_{action}";
				frames.AddAnimation(animName);
				frames.SetAnimationSpeed(animName, AnimSpeeds[action]);
				frames.SetAnimationLoopMode(animName, LoopingAnims.Contains(action) ? SpriteFrames.LoopMode.Linear : SpriteFrames.LoopMode.None);

				foreach (Texture2D tex in textures)
					frames.AddFrame(animName, tex);

				totalAnims++;
			}
		}

		if (totalAnims > 0)
		{
			_cache[charId] = frames;
			GD.Print($"[CharacterSpriteLoader] '{charId}' : {totalAnims} animations chargées");
			return frames;
		}

		GD.PushWarning($"[CharacterSpriteLoader] Aucun sprite trouvé pour '{charId}' dans {basePath}");
		return null;
	}

	/// <summary>true si le jeu de sprites fournit aussi les quatre directions cardinales.</summary>
	public static bool HasEightDirections(SpriteFrames frames)
	{
		foreach (StringName animation in CardinalIdle)
		{
			if (!frames.HasAnimation(animation))
				return false;
		}
		return true;
	}

	private static List<Texture2D> LoadFrameSequence(string basePath, string charId, string dir, string action)
	{
		List<Texture2D> textures = new();
		string prefix = $"{basePath}/char_{charId}_{dir}_{action}_";

		int startIndex;
		if (ResourceLoader.Exists($"{prefix}00.png"))
			startIndex = 0;
		else if (ResourceLoader.Exists($"{prefix}01.png"))
			startIndex = 1;
		else
			return textures;

		for (int i = startIndex; i < 20; i++)
		{
			string path = $"{prefix}{i:D2}.png";
			if (!ResourceLoader.Exists(path))
				break;

			Texture2D tex = GD.Load<Texture2D>(path);
			if (tex != null)
				textures.Add(tex);
		}

		return textures;
	}
}
