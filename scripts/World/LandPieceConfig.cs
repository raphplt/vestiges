using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Pièces maîtresses des terres (data/world/land_pieces.json, plan 58) : par biome, la pièce du cœur, autour du repaire,
/// et des pièces uniques dont la terre tire quelques-unes. Chaque pièce est un <see cref="SitePlan"/> avec, au besoin,
/// la place de son Mémorial.
/// </summary>
public sealed class LandPieceConfig
{
    /// <summary>Une pièce : son plan et, s'il y en a une, la place de son Mémorial (pixels écran depuis son centre).</summary>
    public sealed class Piece
    {
        public string Id = "";
        public SitePlan Plan = new();
        public Vector2? Memorial;
        /// <summary>Sol imposé dans l'ellipse de l'emprise (l'eau y sèche) ; nul, le sol de la terre reste. Exigé du cœur.</summary>
        public TerrainType? Ground;
    }

    /// <summary>Pièces d'un biome, décors tirés de son dossier.</summary>
    public sealed class BiomePieces
    {
        public string Folder = "";
        public Piece Heart;
        public readonly List<Piece> Pieces = new();
    }

    public const string FilePath = "res://data/world/land_pieces.json";

    public int MinPieces = 3;
    public int MaxPieces = 4;
    /// <summary>Écart minimal entre deux pièces, cœur compris, en pixels écran.</summary>
    public float MinSpacingPx = 1400f;
    /// <summary>Emplacements tirés dans la bande des Mémoriaux pour chaque pièce.</summary>
    public int Attempts = 240;
    /// <summary>Emplacements valides comparés pour une pièce : le plus éloigné des pièces déjà posées gagne.</summary>
    public int CandidatesPerPiece = 12;
    /// <summary>Part d'eau tolérée sous une pièce sans sol imposé ; au-delà, l'emplacement est refusé.</summary>
    public float MaxWaterShare = 0.15f;
    public readonly Dictionary<string, BiomePieces> Biomes = new();

    private static readonly string[] PieceKeys = BuildPieceKeys();
    // Sols qu'une pièce peut imposer : pas l'eau, une pièce est un lieu où l'on marche.
    private static readonly string[] GroundNames = { "grass", "concrete", "forest" };

    /// <summary>Réglages valides, ou une exception qui nomme le champ fautif (génération, bancs).</summary>
    public static LandPieceConfig Load()
    {
        if (!TryLoad(out LandPieceConfig config, out string error))
            throw new InvalidOperationException(error);
        return config;
    }

    /// <summary>Pièces lues strictement : décors présents dans le dossier du biome, identifiants uniques.</summary>
    public static bool TryLoad(out LandPieceConfig config, out string error)
    {
        config = null;
        if (!SitePlan.ReadText(FilePath, out string text, out error))
            return false;
        if (!TryParse(text, id => BiomeDataLoader.Get(id) != null, (folder, stem) => ResourceLoader.Exists($"{folder}{stem}.png"),
                out config, out error))
        {
            error = $"{FilePath} : {error}";
            return false;
        }
        return true;
    }

    public static bool TryParse(string text, Func<string, bool> biomeExists, Func<string, string, bool> spriteExists,
        out LandPieceConfig config, out string error)
    {
        config = null;
        try
        {
            using JsonDocument document = JsonDocument.Parse(text);
            JsonConfigReader reader = new(document.RootElement);
            JsonElement root = reader.Root;
            reader.AllowOnly(root, "pièces", "pieces_per_land", "min_spacing_px", "attempts", "candidates_per_piece", "max_water_share",
                "biomes");
            LandPieceConfig parsed = new();
            (float min, float max) = reader.Range(root, "pieces_per_land", 0f, 8f);
            parsed.MinPieces = Mathf.RoundToInt(min);
            parsed.MaxPieces = Mathf.RoundToInt(max);
            if (reader.Error == null && (parsed.MinPieces != min || parsed.MaxPieces != max))
                reader.Fail("pieces_per_land : deux entiers [min, max] attendus");
            parsed.MinSpacingPx = reader.Positive(root, "min_spacing_px");
            parsed.Attempts = reader.Integer(root, "attempts", 1, 4096);
            parsed.CandidatesPerPiece = reader.Integer(root, "candidates_per_piece", 1, 64);
            parsed.MaxWaterShare = reader.Chance(root, "max_water_share");
            foreach ((string biomeId, JsonElement entry) in reader.Entries(reader.Section(root, "biomes"), "biomes"))
            {
                if (reader.Error != null)
                    break;
                if (!biomeExists(biomeId))
                {
                    reader.Fail($"biomes : biome « {biomeId} » inconnu");
                    break;
                }
                parsed.Biomes[biomeId] = ReadBiome(reader, entry, biomeId, spriteExists);
            }
            error = reader.Error;
            if (error != null)
                return false;
            config = parsed;
            return true;
        }
        catch (JsonException exception)
        {
            error = $"JSON invalide : {exception.Message}";
            return false;
        }
    }

    private static BiomePieces ReadBiome(JsonConfigReader reader, JsonElement entry, string biomeId, Func<string, string, bool> spriteExists)
    {
        reader.AllowOnly(entry, biomeId, "folder", "heart", "pieces");
        BiomePieces biome = new() { Folder = reader.Text(entry, "folder") };
        if (reader.Error != null)
            return biome;
        if (!biome.Folder.StartsWith("res://", StringComparison.Ordinal) || !biome.Folder.EndsWith('/'))
            reader.Fail($"{biomeId}.folder : dossier « res://…/ » attendu");
        HashSet<string> ids = new();
        if (JsonConfigReader.Has(entry, "heart"))
            biome.Heart = ReadPiece(reader, reader.Section(entry, "heart"), biomeId, biome.Folder, ids, spriteExists);
        if (reader.Error == null && biome.Heart != null && biome.Heart.Ground == null)
            reader.Fail($"{biomeId}, cœur « {biome.Heart.Id} » : ground exigé, le repaire ne tombe jamais dans l'eau");
        if (JsonConfigReader.Has(entry, "pieces"))
        {
            foreach (JsonElement item in reader.List(entry, "pieces", 0))
            {
                Piece piece = ReadPiece(reader, item, biomeId, biome.Folder, ids, spriteExists);
                if (reader.Error != null)
                    break;
                biome.Pieces.Add(piece);
            }
        }
        return biome;
    }

    private static Piece ReadPiece(JsonConfigReader reader, JsonElement item, string biomeId, string folder, HashSet<string> ids,
        Func<string, string, bool> spriteExists)
    {
        reader.AllowOnly(item, $"{biomeId}, pièce", PieceKeys);
        Piece piece = new() { Id = reader.Text(item, "id") };
        if (reader.Error == null && !ids.Add(piece.Id))
            reader.Fail($"{biomeId} : pièce « {piece.Id} » déjà déclarée");
        piece.Plan = SitePlan.Read(reader, item, biomeId, folder, piece.Id, stem => spriteExists(folder, stem));
        if (JsonConfigReader.Has(item, "memorial"))
            piece.Memorial = SitePlan.Vector(reader, item, "memorial");
        string ground = JsonConfigReader.Has(item, "ground") ? reader.OneOf(item, "ground", GroundNames) : null;
        if (ground != null)
            piece.Ground = Enum.Parse<TerrainType>(ground, true);
        // Dans l'ellipse inscrite dans l'emprise, là où le sol imposé assèche l'eau.
        Vector2 half = piece.Plan.HalfExtentPx;
        if (reader.Error == null && piece.Memorial is Vector2 slot
            && (slot.X * slot.X) / (half.X * half.X) + (slot.Y * slot.Y) / (half.Y * half.Y) > 1f)
            reader.Fail($"{biomeId}, pièce « {piece.Id} » : memorial hors de l'ellipse de half_extent_px");
        return piece;
    }

    private static string[] BuildPieceKeys()
    {
        List<string> keys = new(SitePlan.CommonKeys) { "id", "memorial", "ground" };
        return keys.ToArray();
    }
}
