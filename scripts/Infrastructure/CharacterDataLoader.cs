using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

namespace Vestiges.Infrastructure;

public class CharacterStats
{
    public float Speed { get; set; }
    public float AttackDamage { get; set; }
    public float AttackSpeed { get; set; }
    public float AttackRange { get; set; }
    public float MaxHp { get; set; }
    public float RegenRate { get; set; }
    public float Shield { get; set; }
    public float InteractRange { get; set; }
}

public class CharacterData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string StartingWeaponId { get; set; }
    public CharacterStats BaseStats { get; set; }
    public string PassivePerk { get; set; }
    public Color VisualColor { get; set; }
    /// <summary>Couleur identitaire des traînées du dash (plan 54 G3) ; la couleur du corps à défaut.</summary>
    public Color AccentColor { get; set; }
    public string SpriteFolder { get; set; }
    /// <summary>Distance en pixels du centre de la frame aux pieds : ancre le sprite sur la position au sol.</summary>
    public float SpriteFeetOffset { get; set; }
    /// <summary>Profil de mobilité de data/movement/mobility.json ; null : le dash commun.</summary>
    public string MobilityProfile { get; set; }
}

/// <summary>
/// Personnages jouables (data/characters/characters.json), lus strictement une fois (plan 26 Q7f-1) : arme de départ,
/// signature, profil de mobilité et sprites doivent exister. Un fichier refusé n'est pas publié et arrête le chargement
/// de la run (GameBootstrap).
/// </summary>
public static class CharacterDataLoader
{
    public const string DataPath = "res://data/characters/characters.json";

    /// <summary>Catalogues que citent les personnages.</summary>
    public sealed record References(Func<string, bool> WeaponExists, Func<string, PerkData> Perk,
        Func<string, string> MobilityError, Func<string, string, bool> SpriteExists);

    private static List<CharacterData> _allCharacters = new();
    private static Dictionary<string, CharacterData> _byId = new();
    private static bool _loaded;
    private static string _loadError;

    /// <summary>Le fichier lu et contrôlé ; faux, avec la raison, s'il a été refusé (la run ne démarre pas).</summary>
    public static bool TryLoad(out string error)
    {
        Load();
        error = _loadError;
        return error == null;
    }

    public static void Load()
    {
        if (_loaded)
            return;
        _loaded = true;
        _loadError = $"{DataPath} : lecture interrompue";
        using FileAccess file = FileAccess.Open(DataPath, FileAccess.ModeFlags.Read);
        _loadError = file == null ? $"{DataPath} : fichier illisible" : Apply(file.GetAsText(), GameReferences());
        if (_loadError != null)
        {
            GD.PushError($"[CharacterDataLoader] {_loadError}");
            return;
        }
        GD.Print($"[CharacterDataLoader] Loaded {_allCharacters.Count} characters");
    }

    public static References GameReferences() => new(
        id => WeaponDataLoader.Get(id) != null,
        id => PerkDataLoader.GetAll().Find(perk => perk.Id == id),
        MobilityError,
        (folder, id) => ResourceLoader.Exists($"res://assets/characters/{folder}/char_{id}_S_idle_01.png"));

    /// <summary>Contrôle <paramref name="text"/> et ne publie qu'un fichier accepté ; rend l'erreur, nulle si tout est publié.</summary>
    public static string Apply(string text, References references)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(text);
            if (document.RootElement.ValueKind != JsonValueKind.Array || document.RootElement.GetArrayLength() == 0)
                return "liste de personnages attendue à la racine";
            List<CharacterData> characters = new();
            Dictionary<string, CharacterData> byId = new();
            foreach (JsonElement entry in document.RootElement.EnumerateArray())
            {
                JsonConfigReader reader = new(entry);
                CharacterData character = Parse(reader, entry, references);
                if (reader.Error == null && byId.ContainsKey(character.Id))
                    reader.Fail("identifiant déjà déclaré");
                if (reader.Error != null)
                    return $"personnage {Identifier(entry)} : {reader.Error}";
                characters.Add(character);
                byId[character.Id] = character;
            }
            _allCharacters = characters;
            _byId = byId;
            return null;
        }
        catch (JsonException exception)
        {
            return $"JSON invalide : {exception.Message}";
        }
    }

    public static CharacterData Get(string id)
    {
        Load();
        return id == null ? null : _byId.GetValueOrDefault(id);
    }

    public static List<CharacterData> GetAll()
    {
        Load();
        return _allCharacters;
    }

    private static CharacterData Parse(JsonConfigReader reader, JsonElement entry, References references)
    {
        reader.AllowOnly(entry, "personnage", "id", "name", "description", "starting_weapon", "base_stats", "passive_perk",
            "visual_color", "accent_color", "sprite_folder", "sprite_feet_offset", "mobility");
        string id = reader.Text(entry, "id");
        JsonElement statsSection = reader.Section(entry, "base_stats");
        reader.AllowOnly(statsSection, "base_stats", "speed", "attack_damage", "attack_speed", "attack_range", "max_hp",
            "regen_rate", "shield", "interact_range");
        CharacterStats stats = new()
        {
            Speed = reader.Positive(statsSection, "speed"),
            AttackDamage = reader.NonNegative(statsSection, "attack_damage"),
            AttackSpeed = reader.Positive(statsSection, "attack_speed"),
            AttackRange = reader.Positive(statsSection, "attack_range"),
            MaxHp = reader.Positive(statsSection, "max_hp"),
            RegenRate = reader.NonNegative(statsSection, "regen_rate"),
            Shield = JsonConfigReader.Has(statsSection, "shield") ? reader.NonNegative(statsSection, "shield") : 0f,
            InteractRange = reader.Positive(statsSection, "interact_range"),
        };
        Color visual = Rgba(reader, entry, "visual_color");
        CharacterData character = new()
        {
            Id = id,
            Name = reader.Text(entry, "name"),
            Description = reader.Text(entry, "description"),
            StartingWeaponId = JsonConfigReader.Has(entry, "starting_weapon") ? reader.Text(entry, "starting_weapon") : null,
            BaseStats = stats,
            PassivePerk = reader.Text(entry, "passive_perk"),
            VisualColor = visual,
            AccentColor = JsonConfigReader.Has(entry, "accent_color") ? Rgba(reader, entry, "accent_color") : visual,
            SpriteFolder = JsonConfigReader.Has(entry, "sprite_folder") ? reader.Text(entry, "sprite_folder") : null,
            SpriteFeetOffset = JsonConfigReader.Has(entry, "sprite_feet_offset") ? reader.NonNegative(entry, "sprite_feet_offset") : 0f,
            MobilityProfile = JsonConfigReader.Has(entry, "mobility") ? reader.Text(entry, "mobility") : null,
        };
        if (reader.Error != null)
            return character;
        if (character.StartingWeaponId != null && !references.WeaponExists(character.StartingWeaponId))
            reader.Fail($"starting_weapon : arme « {character.StartingWeaponId} » inconnue");
        PerkData passive = references.Perk(character.PassivePerk);
        if (passive == null || !passive.IsPassive || passive.CharacterId != id)
            reader.Fail($"passive_perk : « {character.PassivePerk} » n'est pas une signature de {id}");
        string mobilityError = character.MobilityProfile != null ? references.MobilityError(character.MobilityProfile) : null;
        if (mobilityError != null)
            reader.Fail($"mobility : {mobilityError}");
        if (character.SpriteFolder != null && (character.SpriteFolder.Contains('/') || character.SpriteFolder.Contains('.')
                || !references.SpriteExists(character.SpriteFolder, id)))
            reader.Fail($"sprite_folder : « {character.SpriteFolder} » sans char_{id}_S_idle_01.png");
        return character;
    }

    /// <summary>Couleur écrite [r, g, b, a], chaque composante de 0 à 1.</summary>
    private static Color Rgba(JsonConfigReader reader, JsonElement owner, string key)
    {
        if (reader.Error != null)
            return default;
        if (!owner.TryGetProperty(key, out JsonElement rgba) || rgba.ValueKind != JsonValueKind.Array || rgba.GetArrayLength() != 4)
        {
            reader.Fail($"{key} : quatre composantes de 0 à 1 attendues");
            return default;
        }
        float[] values = new float[4];
        for (int i = 0; i < 4; i++)
        {
            if (rgba[i].ValueKind != JsonValueKind.Number || rgba[i].GetDouble() < 0.0 || rgba[i].GetDouble() > 1.0)
            {
                reader.Fail($"{key} : quatre composantes de 0 à 1 attendues");
                return default;
            }
            values[i] = (float)rgba[i].GetDouble();
        }
        return new Color(values[0], values[1], values[2], values[3]);
    }

    /// <summary>Raison du refus du profil par MobilityConfig (inconnu, réglage hors bornes), nulle s'il est valide.</summary>
    private static string MobilityError(string profile)
    {
        try
        {
            MobilityConfig.Load(profile);
            return null;
        }
        catch (InvalidOperationException exception)
        {
            return exception.Message;
        }
    }

    private static string Identifier(JsonElement entry) =>
        entry.ValueKind == JsonValueKind.Object && entry.TryGetProperty("id", out JsonElement id) && id.ValueKind == JsonValueKind.String
            ? $"« {id.GetString()} »" : "sans identifiant";
}
