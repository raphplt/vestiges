using System.Collections.Generic;
using Godot;

namespace Vestiges.Combat;

/// <summary>
/// Charge et met en cache les SpriteFrames d'ennemis depuis les fichiers PNG individuels.
/// Convention directionnelle : enemy_{folder}_{DIR}_{ACTION}_{FRAME:D2}.png
/// Convention non-directionnelle : enemy_{folder}_{ACTION}_{FRAME:D2}.png (dupliqué sur 4 dirs)
/// Directions : NE, NW, SE, SW. Actions : idle, walk, attack, death.
/// </summary>
public static class EnemySpriteLoader
{
	private static readonly Dictionary<string, SpriteFrames> _cache = new();

	private static readonly string[] Directions = { "NE", "NW", "SE", "SW" };
	private static readonly string[] Actions = { "idle", "walk", "attack", "death" };

	private static readonly Dictionary<string, float> AnimSpeeds = new()
	{
		{ "idle", 5f },
		{ "walk", 8f },
		{ "attack", 10f },
		{ "death", 8f }
	};

	private static readonly HashSet<string> LoopingAnims = new() { "idle", "walk" };

	public static SpriteFrames LoadOrGet(string enemyId, string folder)
	{
		if (_cache.TryGetValue(enemyId, out SpriteFrames cached))
			return cached;

		SpriteFrames frames = new();

		if (frames.HasAnimation("default"))
			frames.RemoveAnimation("default");

		string basePath = $"res://assets/enemies/{folder}";
		int totalAnims = 0;

		// Pré-charger les séquences non-directionnelles (partagées entre les 4 dirs)
		Dictionary<string, List<Texture2D>> nonDirCache = new();

		foreach (string dir in Directions)
		{
			foreach (string action in Actions)
			{
				// Format directionnel : enemy_{folder}_{DIR}_{ACTION}_{FRAME}
				List<Texture2D> textures = LoadFrameSequence(basePath, folder, $"{dir}_{action}");

				// Fallback non-directionnel : enemy_{folder}_{ACTION}_{FRAME}
				if (textures.Count == 0)
				{
					if (!nonDirCache.TryGetValue(action, out textures))
					{
						textures = LoadFrameSequence(basePath, folder, action);
						nonDirCache[action] = textures;
					}
				}

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
			_cache[enemyId] = frames;
			GD.Print($"[EnemySpriteLoader] '{enemyId}' : {totalAnims} animations chargées depuis '{folder}'");
			return frames;
		}

		GD.PushWarning($"[EnemySpriteLoader] Aucun sprite trouvé pour '{enemyId}' dans {basePath}");
		return null;
	}

	private static List<Texture2D> LoadFrameSequence(string basePath, string folder, string suffix)
	{
		List<Texture2D> textures = new();

		for (int i = 0; i < 20; i++)
		{
			string path = $"{basePath}/enemy_{folder}_{suffix}_{i:D2}.png";
			if (!ResourceLoader.Exists(path))
			{
				if (textures.Count == 0)
					continue;
				break;
			}

			Texture2D tex = GD.Load<Texture2D>(path);
			if (tex == null)
				break;

			textures.Add(tex);
		}

		return textures;
	}
}
