using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

public enum QuestScope { Run, Cumulative }

public enum UnlockKind { Character, Weapon, Object }

/// <summary>Un fait de run (<see cref="QuestStat"/>) et sa cible ; <c>BeforeSec</c> ne compte que ce qui arrive avant.</summary>
public sealed class QuestCondition
{
    public string Stat { get; init; }
    public float Target { get; init; }
    public float Param { get; init; }
    public string Weapon { get; init; }
    public float BeforeSec { get; init; }
}

/// <summary>Pièce débloquée. <c>Name</c> nomme un personnage pas encore jouable, absent du catalogue.</summary>
public sealed class QuestUnlock
{
    public UnlockKind Kind { get; init; }
    public string Id { get; init; }
    public string Name { get; init; }
}

/// <summary>Quête de déblocage (plan 06 §9) : toutes ses conditions, remplies dans une même run ou cumulées.</summary>
public sealed class QuestDefinition
{
    public string Id { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    /// <summary>De 1 (une ou deux runs normales) à 3 (fin de run, risque ou maîtrise).</summary>
    public int Difficulty { get; init; }
    public QuestScope Scope { get; init; }
    public List<QuestCondition> Conditions { get; init; } = new();
    public List<QuestUnlock> Unlocks { get; init; } = new();
}

/// <summary>
/// Quêtes de déblocage (data/quests/quests.json), contrôlées en entier : faits connus, cibles positives, pièces
/// existantes et débloquées par une seule quête. Une pièce qu'aucune quête ne débloque est disponible dès le départ :
/// la réserve de départ se déduit de ce fichier.
/// </summary>
public static class QuestDataLoader
{
    private const string CatalogPath = "res://data/quests/quests.json";

    private static readonly List<QuestDefinition> _all = new();
    private static readonly Dictionary<string, QuestDefinition> _byId = new();
    private static readonly Dictionary<(UnlockKind, string), QuestDefinition> _byUnlock = new();
    private static bool _loaded;
    private static string _loadError;

    public static void Load()
    {
        if (_loaded)
            return;
        _loaded = true;
        WeaponDataLoader.Load();
        PassiveSouvenirDataLoader.Load();
        CharacterDataLoader.Load();
        string error = FileAccess.FileExists(CatalogPath)
            ? Apply(FileAccess.GetFileAsString(CatalogPath), id => WeaponDataLoader.Get(id) != null,
                id => PassiveSouvenirDataLoader.GetAll().Exists(item => item.Id == id), id => CharacterDataLoader.Get(id) != null)
            : "absent";
        if (error != null)
        {
            _loadError = $"{CatalogPath} : {error}";
            GD.PushError($"[QuestDataLoader] {_loadError}");
            return;
        }
        GD.Print($"[QuestDataLoader] Loaded {_all.Count} quests");
    }

    /// <summary>Catalogue lu et contrôlé ; faux, avec la raison, s'il a été refusé.</summary>
    public static bool TryLoad(out string error)
    {
        Load();
        error = _loadError;
        return error == null;
    }

    public static QuestDefinition Get(string questId)
    {
        Load();
        return string.IsNullOrWhiteSpace(questId) ? null : _byId.GetValueOrDefault(questId);
    }

    public static IReadOnlyList<QuestDefinition> GetAll()
    {
        Load();
        return _all;
    }

    /// <summary>La quête qui débloque cette pièce, ou null si elle est disponible dès le départ.</summary>
    public static QuestDefinition FindUnlocking(UnlockKind kind, string id)
    {
        Load();
        return string.IsNullOrEmpty(id) ? null : _byUnlock.GetValueOrDefault((kind, id));
    }

    /// <summary>Contrôle un texte de quêtes et ne le publie que s'il est entièrement valide. Rend l'erreur, ou null.</summary>
    public static string Apply(string json, Func<string, bool> weaponExists, Func<string, bool> objectExists,
        Func<string, bool> characterExists)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonConfigReader reader = new(document.RootElement);
            reader.AllowOnly(reader.Root, "quêtes", "quests");
            List<QuestDefinition> quests = new();
            Dictionary<string, QuestDefinition> byId = new();
            Dictionary<(UnlockKind, string), QuestDefinition> byUnlock = new();
            foreach (JsonElement entry in reader.List(reader.Root, "quests", 1))
            {
                if (entry.ValueKind != JsonValueKind.Object)
                    return "chaque quête doit être un objet";
                JsonConfigReader item = new(entry);
                item.AllowOnly(entry, "clés", "id", "name", "description", "difficulty", "scope", "conditions", "unlocks");
                string id = item.Text(entry, "id");
                QuestScope scope = item.OneOf(entry, "scope", new[] { "run", "cumulative" }) == "cumulative"
                    ? QuestScope.Cumulative : QuestScope.Run;
                QuestDefinition quest = new()
                {
                    Id = id,
                    Name = item.Text(entry, "name"),
                    Description = item.Text(entry, "description"),
                    Difficulty = item.Integer(entry, "difficulty", 1, 3),
                    Scope = scope,
                };
                foreach (JsonElement condition in item.List(entry, "conditions", 1))
                    quest.Conditions.Add(ReadCondition(item, condition, scope, weaponExists));
                foreach (JsonElement unlock in item.List(entry, "unlocks", 1))
                {
                    QuestUnlock read = ReadUnlock(item, unlock, weaponExists, objectExists, characterExists);
                    if (item.Error == null && !byUnlock.TryAdd((read.Kind, read.Id), quest))
                        item.Fail($"{read.Id} déjà débloqué par la quête {byUnlock[(read.Kind, read.Id)].Id}");
                    quest.Unlocks.Add(read);
                }
                if (item.Error == null && !byId.TryAdd(id, quest))
                    item.Fail("id en double");
                if (item.Error != null)
                    return $"quête {id ?? "?"} : {item.Error}";
                quests.Add(quest);
            }
            if (reader.Error != null)
                return reader.Error;

            _all.Clear();
            _all.AddRange(quests);
            _byId.Clear();
            foreach ((string key, QuestDefinition value) in byId)
                _byId[key] = value;
            _byUnlock.Clear();
            foreach (((UnlockKind, string) key, QuestDefinition value) in byUnlock)
                _byUnlock[key] = value;
            _loadError = null;
            return null;
        }
        catch (JsonException ex)
        {
            return $"JSON illisible : {ex.Message}";
        }
    }

    private static QuestCondition ReadCondition(JsonConfigReader item, JsonElement condition, QuestScope scope,
        Func<string, bool> weaponExists)
    {
        if (condition.ValueKind != JsonValueKind.Object)
        {
            item.Fail("chaque condition doit être un objet");
            return new QuestCondition();
        }
        item.AllowOnly(condition, "condition", "stat", "target", "param", "weapon", "before_sec");
        string stat = item.OneOf(condition, "stat", QuestStat.All);
        bool hasParam = condition.TryGetProperty("param", out _);
        if (item.Error == null && hasParam != QuestStat.WithParam.Contains(stat))
            item.Fail(hasParam ? $"param : sans objet pour {stat}" : $"param : attendu pour {stat}");
        bool hasWeapon = condition.TryGetProperty("weapon", out _);
        if (item.Error == null && hasWeapon != (stat == QuestStat.WeaponKills))
            item.Fail(hasWeapon ? $"weapon : sans objet pour {stat}" : $"weapon : attendu pour {stat}");
        if (item.Error == null && scope == QuestScope.Cumulative && !QuestStat.Cumulable.Contains(stat))
            item.Fail($"{stat} ne se cumule pas d'une run à l'autre");
        bool hasDeadline = condition.TryGetProperty("before_sec", out _);
        if (item.Error == null && hasDeadline && scope == QuestScope.Cumulative)
            item.Fail("before_sec : réservé aux quêtes d'une run");
        QuestCondition read = new()
        {
            Stat = stat,
            Target = item.Positive(condition, "target"),
            Param = hasParam ? item.Positive(condition, "param") : 0f,
            Weapon = hasWeapon ? item.Text(condition, "weapon") : null,
            BeforeSec = hasDeadline ? item.Positive(condition, "before_sec") : 0f,
        };
        if (item.Error == null && hasWeapon && !weaponExists(read.Weapon))
            item.Fail($"weapon : « {read.Weapon} » inconnue");
        return read;
    }

    private static QuestUnlock ReadUnlock(JsonConfigReader item, JsonElement unlock, Func<string, bool> weaponExists,
        Func<string, bool> objectExists, Func<string, bool> characterExists)
    {
        if (unlock.ValueKind != JsonValueKind.Object)
        {
            item.Fail("chaque déblocage doit être un objet");
            return new QuestUnlock();
        }
        item.AllowOnly(unlock, "déblocage", "type", "id", "name");
        UnlockKind kind = item.OneOf(unlock, "type", new[] { "character", "weapon", "object" }) switch
        {
            "weapon" => UnlockKind.Weapon,
            "object" => UnlockKind.Object,
            _ => UnlockKind.Character,
        };
        string id = item.Text(unlock, "id");
        bool named = unlock.TryGetProperty("name", out _);
        QuestUnlock read = new() { Kind = kind, Id = id, Name = named ? item.Text(unlock, "name") : null };
        if (item.Error != null)
            return read;
        bool exists = kind switch
        {
            UnlockKind.Weapon => weaponExists(id),
            UnlockKind.Object => objectExists(id),
            _ => characterExists(id),
        };
        // Un personnage à venir n'est pas encore au catalogue : son nom est alors donné ici, et seulement alors.
        if (kind == UnlockKind.Character && named == exists)
            item.Fail(exists ? $"name : {id} est déjà au catalogue des personnages" : $"personnage « {id} » inconnu sans name");
        else if (kind != UnlockKind.Character && (named || !exists))
            item.Fail(named ? "name : réservé aux personnages à venir" : $"{id} : pièce inconnue");
        return read;
    }
}
