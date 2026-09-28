using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Godot;

namespace Vestiges.Infrastructure;

/// <summary>Conditions d'offre : elles seront évaluées par la progression, jamais par le lecteur de données.</summary>
public sealed class PerkSpecializationEligibility
{
    public bool TargetingWeapon { get; init; }
    public bool DirectDamageWeapon { get; init; }
    public bool NativeControlWeapon { get; init; }
    public int MinUpgradeableWeapons { get; init; }
    public bool ItemChoiceRewards { get; init; }
    public bool OwnedItem { get; init; }
}

public sealed class PerkSpecializationData
{
    public string Id { get; init; }
    public string Effect { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public string Family { get; init; }
    public PerkSpecializationEligibility Eligibility { get; init; }
    public IReadOnlyDictionary<string, float> Parameters { get; init; }
}

public sealed class PerkSpecializationConfig
{
    public int MaxEquipped { get; init; }
    public int OfferSize { get; init; }
    public IReadOnlyList<int> OfferLevels { get; init; }
}

public sealed class PerkSpecializationCatalogue
{
    public PerkSpecializationConfig Config { get; init; }
    public IReadOnlyList<PerkSpecializationData> Perks { get; init; }
}

/// <summary>
/// Catalogue B indépendant de l'ancien catalogue de Dons. Le lire n'active aucun effet ni aucune offre.
/// Les identifiants d'effets et le nombre de définitions restent indépendants des quatre emplacements.
/// </summary>
public static class PerkSpecializationDataLoader
{
    private static PerkSpecializationCatalogue _catalogue;
    private static readonly Dictionary<string, PerkSpecializationData> _byId = new();

    public static PerkSpecializationConfig Config
    {
        get { Load(); return _catalogue?.Config; }
    }

    public static bool Load()
    {
        if (_catalogue != null)
            return true;

        using FileAccess file = FileAccess.Open("res://data/progression/perk_specializations.json", FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PushError("[PerkSpecializationDataLoader] Catalogue de spécialisations introuvable.");
            return false;
        }
        if (!TryParse(file.GetAsText(), out PerkSpecializationCatalogue catalogue, out string error))
        {
            GD.PushError($"[PerkSpecializationDataLoader] {error}");
            return false;
        }

        // Publication atomique : une erreur ne laisse pas de définitions partielles dans le cache.
        foreach (PerkSpecializationData perk in catalogue.Perks)
            _byId.Add(perk.Id, perk);
        _catalogue = catalogue;
        return true;
    }

    public static PerkSpecializationData Get(string id)
    {
        return Load() && id != null ? _byId.GetValueOrDefault(id) : null;
    }

    public static IReadOnlyList<PerkSpecializationData> GetAll()
    {
        return Load() ? _catalogue.Perks : Array.Empty<PerkSpecializationData>();
    }

    public static bool TryParse(string text, out PerkSpecializationCatalogue catalogue, out string error)
    {
        catalogue = null;
        error = "";
        using Json json = new();
        if (json.Parse(text) != Error.Ok)
        {
            error = $"JSON invalide : {json.GetErrorMessage()}";
            return false;
        }
        try
        {
            Godot.Collections.Dictionary root = Dictionary(json.Data, "racine");
            if (PositiveInteger(root, "schema_version") != 1)
                throw new FormatException("Version de schéma inconnue.");
            Godot.Collections.Dictionary acquisition = Dictionary(Required(root, "acquisition"), "acquisition");
            int maxEquipped = PositiveInteger(acquisition, "max_equipped");
            int offerSize = PositiveInteger(acquisition, "offer_size");
            Godot.Collections.Array levels = ArrayValue(Required(acquisition, "offer_levels"), "offer_levels");
            List<int> offerLevels = new();
            int previous = 1;
            foreach (Variant level in levels)
            {
                int current = Integer(level, "offer_levels");
                if (current <= previous)
                    throw new FormatException("Les paliers doivent être strictement croissants, après le niveau 1.");
                offerLevels.Add(current);
                previous = current;
            }
            if (offerLevels.Count != maxEquipped)
                throw new FormatException("Chaque emplacement doit avoir un palier d'acquisition.");

            List<PerkSpecializationData> perks = new();
            HashSet<string> ids = new(StringComparer.Ordinal);
            foreach (Variant entry in ArrayValue(Required(root, "perks"), "perks"))
            {
                Godot.Collections.Dictionary data = Dictionary(entry, "perk");
                if (data.ContainsKey("rarity") || data.ContainsKey("max_level") || data.ContainsKey("max_stacks")
                    || data.ContainsKey("level") || data.ContainsKey("levels") || data.ContainsKey("per_level") || data.ContainsKey("stacks"))
                    throw new FormatException("Une spécialisation n'a ni rareté, ni niveaux, ni piles.");
                string id = Text(data, "id");
                if (!ids.Add(id))
                    throw new FormatException($"Identifiant de perk dupliqué : {id}.");
                Godot.Collections.Dictionary eligibility = Dictionary(Required(data, "eligibility"), "eligibility");
                Dictionary<string, float> parameters = new(StringComparer.Ordinal);
                foreach (KeyValuePair<Variant, Variant> parameter in Dictionary(Required(data, "parameters"), "parameters"))
                {
                    if (parameter.Key.VariantType != Variant.Type.String || string.IsNullOrWhiteSpace(parameter.Key.AsString()))
                        throw new FormatException("Nom de paramètre invalide.");
                    double number = Number(parameter.Value, parameter.Key.AsString());
                    if (number < 0 || number > float.MaxValue)
                        throw new FormatException("Un coefficient doit être positif ou nul et représentable.");
                    parameters.Add(parameter.Key.AsString(), (float)number);
                }
                perks.Add(new PerkSpecializationData
                {
                    Id = id,
                    Effect = Text(data, "effect"),
                    Name = Text(data, "name"),
                    Description = Text(data, "description"),
                    Family = Text(data, "family"),
                    Eligibility = ParseEligibility(eligibility),
                    Parameters = new ReadOnlyDictionary<string, float>(parameters)
                });
            }
            if (perks.Count < maxEquipped)
                throw new FormatException("Le catalogue doit contenir au moins autant de perks que d'emplacements.");
            catalogue = new PerkSpecializationCatalogue
            {
                Config = new PerkSpecializationConfig { MaxEquipped = maxEquipped, OfferSize = offerSize, OfferLevels = offerLevels.AsReadOnly() },
                Perks = perks.AsReadOnly()
            };
            return true;
        }
        catch (FormatException exception)
        {
            error = exception.Message;
            return false;
        }
    }

    private static PerkSpecializationEligibility ParseEligibility(Godot.Collections.Dictionary data)
    {
        foreach (Variant key in data.Keys)
        {
            string name = key.AsString();
            if (name is not ("targeting_weapon" or "direct_damage_weapon" or "native_control_weapon" or "min_upgradeable_weapons" or "item_choice_rewards" or "owned_item"))
                throw new FormatException($"Condition d'éligibilité inconnue : {name}.");
        }
        int minimum = data.ContainsKey("min_upgradeable_weapons") ? Integer(data["min_upgradeable_weapons"], "min_upgradeable_weapons") : 0;
        if (minimum < 0)
            throw new FormatException("Le nombre d'armes améliorables ne peut pas être négatif.");
        return new PerkSpecializationEligibility
        {
            TargetingWeapon = Boolean(data, "targeting_weapon"),
            DirectDamageWeapon = Boolean(data, "direct_damage_weapon"),
            NativeControlWeapon = Boolean(data, "native_control_weapon"),
            MinUpgradeableWeapons = minimum,
            ItemChoiceRewards = Boolean(data, "item_choice_rewards"),
            OwnedItem = Boolean(data, "owned_item")
        };
    }

    private static Variant Required(Godot.Collections.Dictionary data, string key)
    {
        return data.ContainsKey(key) ? data[key] : throw new FormatException($"Champ requis absent : {key}.");
    }

    private static Godot.Collections.Dictionary Dictionary(Variant value, string name)
    {
        return value.VariantType == Variant.Type.Dictionary ? value.AsGodotDictionary() : throw new FormatException($"{name} doit être un objet.");
    }

    private static Godot.Collections.Array ArrayValue(Variant value, string name)
    {
        return value.VariantType == Variant.Type.Array ? value.AsGodotArray() : throw new FormatException($"{name} doit être une liste.");
    }

    private static string Text(Godot.Collections.Dictionary data, string key)
    {
        Variant value = Required(data, key);
        if (value.VariantType != Variant.Type.String || string.IsNullOrWhiteSpace(value.AsString()))
            throw new FormatException($"{key} doit être un texte non vide.");
        return value.AsString();
    }

    private static double Number(Variant value, string name)
    {
        if (value.VariantType is not (Variant.Type.Int or Variant.Type.Float) || !double.IsFinite(value.AsDouble()))
            throw new FormatException($"{name} doit être un nombre fini.");
        return value.AsDouble();
    }

    private static int Integer(Variant value, string name)
    {
        double number = Number(value, name);
        if (number != Math.Truncate(number) || number > int.MaxValue || number < int.MinValue)
            throw new FormatException($"{name} doit être un entier.");
        return (int)number;
    }

    private static int PositiveInteger(Godot.Collections.Dictionary data, string key)
    {
        int number = Integer(Required(data, key), key);
        return number > 0 ? number : throw new FormatException($"{key} doit être strictement positif.");
    }

    private static bool Boolean(Godot.Collections.Dictionary data, string key)
    {
        if (!data.ContainsKey(key))
            return false;
        return data[key].VariantType == Variant.Type.Bool ? data[key].AsBool() : throw new FormatException($"{key} doit être un booléen.");
    }
}
