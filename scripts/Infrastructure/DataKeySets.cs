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

	/// <summary>Identifiants (« id ») des entrées d'une liste <paramref name="arrayKey"/> d'un fichier de référence.</summary>
	public static HashSet<string> ListIds(string path, string arrayKey)
	{
		string cacheKey = path + "#" + arrayKey;
		if (_cache.TryGetValue(cacheKey, out HashSet<string> ids))
			return ids;

		ids = new HashSet<string>();
		_cache[cacheKey] = ids;
		if (!FileAccess.FileExists(path))
		{
			GD.PushError($"[DataKeySets] {path} introuvable");
			return ids;
		}
		try
		{
			using JsonDocument document = JsonDocument.Parse(FileAccess.GetFileAsString(path));
			if (document.RootElement.TryGetProperty(arrayKey, out JsonElement list) && list.ValueKind == JsonValueKind.Array)
			{
				foreach (JsonElement entry in list.EnumerateArray())
				{
					if (entry.ValueKind == JsonValueKind.Object && entry.TryGetProperty("id", out JsonElement id) && id.ValueKind == JsonValueKind.String)
						ids.Add(id.GetString());
				}
			}
		}
		catch (JsonException ex)
		{
			GD.PushError($"[DataKeySets] {path} illisible : {ex.Message}");
		}
		return ids;
	}
}
