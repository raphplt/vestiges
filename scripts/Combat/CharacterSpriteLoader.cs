using System.Collections.Generic;
using Godot;

namespace Vestiges.Combat;

/// <summary>
/// Charge et met en cache les SpriteFrames de personnages depuis les fichiers PNG individuels.
/// Convention de nommage : char_{id}_{DIR}_{ACTION}_{FRAME:D2}.png
/// Directions : NE, NW, SE, SW. Actions : idle, walk, dash, hurt, death.
/// </summary>
public static class CharacterSpriteLoader
{
	private static readonly Dictionary<string, SpriteFrames> _cache = new();

	private static readonly string[] Directions = { "NE", "NW", "SE", "SW" };
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
				frames.SetAnimationLoop(animName, LoopingAnims.Contains(action));

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

	private static List<Texture2D> LoadFrameSequence(string basePath, string charId, string dir, string action)
	{
		List<Texture2D> textures = new();
		string prefix = $"{basePath}/char_{charId}_{dir}_{action}_";

		int startIndex;
		if (FileAccess.FileExists($"{prefix}00.png"))
			startIndex = 0;
		else if (FileAccess.FileExists($"{prefix}01.png"))
			startIndex = 1;
		else
			return textures;

		for (int i = startIndex; i < 20; i++)
		{
			string path = $"{prefix}{i:D2}.png";
			if (!FileAccess.FileExists(path))
				break;

			Texture2D tex = GD.Load<Texture2D>(path);
			if (tex != null)
				textures.Add(tex);
		}

		return textures;
	}
}
