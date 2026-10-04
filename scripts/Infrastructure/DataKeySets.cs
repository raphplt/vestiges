using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Clés de premier niveau d'un fichier JSON de référence (banque audio, manifeste d'images), lues une fois et sans
/// charger les ressources : les validateurs de catalogues y contrôlent leurs références.
/// </summary>
public static class DataKeySets
{
	private static readonly Dictionary<string, HashSet<string>> _cache = new();

	public static HashSet<string> TopLevelKeys(string path)
	{
		if (_cache.TryGetValue(path, out HashSet<string> keys))
			return keys;

		keys = new HashSet<string>();
		_cache[path] = keys;
		using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		if (file == null)
		{
			GD.PushError($"[DataKeySets] {path} introuvable");
			return keys;
		}
		try
		{
			using JsonDocument document = JsonDocument.Parse(file.GetAsText());
			foreach (JsonProperty entry in document.RootElement.EnumerateObject())
				keys.Add(entry.Name);
		}
		catch (JsonException ex)
		{
			GD.PushError($"[DataKeySets] {path} illisible : {ex.Message}");
		}
		return keys;
	}
}
