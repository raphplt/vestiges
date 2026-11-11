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
        /// <summary>
        /// Pan de vide creusé dans la pièce (plan 58 T1b) : centre en pixels écran depuis le centre de la pièce, nul sans
        /// vide. Ses cellules d'Effacement sont tombées dès l'arrivée et ne se relèvent jamais.
        /// </summary>
        public Vector2? VoidCenter;
        /// <summary>Demi-axes de l'ellipse du vide, en pixels écran.</summary>
        public Vector2 VoidRadii;
        /// <summary>
        /// Bassins d'eau posés par la pièce (plan 58 T2c), rectangles alignés sur l'écran, en pixels écran depuis son centre.
        /// Ils remplissent les cellules dont le centre y tombe, sauf celles d'un chemin.
        /// </summary>
        public readonly List<Rect2> Water = new();
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
    // Un vide plus petit qu'une cellule d'Effacement (128 px) n'en creuserait aucune.
    private const float VoidMinRadiusPx = 128f;
    // Le cercle d'un Mémorial (radius_px de landmarks.json, 220 px) reste sur le sol et frôle le bord du vide.
    private const float VoidMemorialMarginPx = 240f;
    // Pas de la grille des tuiles (PathNetworkGenerator.CellCenter) : 32 px en x d'une rangée à l'autre, 16 px en y.
    private const int TileStepXPx = 32;
    private const int TileStepYPx = 16;
    // Un bassin plus petit qu'une cellule de tuile (64 × 32 px) pourrait n'en remplir aucune.
    private const float BasinMinHalfXPx = 32f;
    private const float BasinMinHalfYPx = 16f;
    // Le Mémorial se pose au sec, son socle à distance de la berge.
    private const float BasinMemorialMarginPx = 64f;

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
        if (reader.Error == null && biome.Heart?.VoidCenter != null)
            reader.Fail($"{biomeId}, cœur « {biome.Heart.Id} » : pas de void, le combat du gardien garde son sol");
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
        if (JsonConfigReader.Has(item, "void"))
            ReadVoid(reader, reader.Section(item, "void"), biomeId, piece);
        if (JsonConfigReader.Has(item, "water"))
        {
            foreach (JsonElement basin in reader.List(item, "water", 1))
            {
                ReadBasin(reader, basin, biomeId, piece);
                if (reader.Error != null)
                    break;
            }
        }
        return piece;
    }

    /// <summary>Le vide tient dans l'emprise et laisse la place du Mémorial sur le sol.</summary>
    private static void ReadVoid(JsonConfigReader reader, JsonElement element, string biomeId, Piece piece)
    {
        reader.AllowOnly(element, $"{biomeId}, pièce « {piece.Id} », void", "at", "radii");
        Vector2 center = SitePlan.Vector(reader, element, "at");
        Vector2 radii = SitePlan.Vector(reader, element, "radii");
        if (reader.Error != null)
            return;
        Vector2 half = piece.Plan.HalfExtentPx;
        if (radii.X < VoidMinRadiusPx || radii.Y < VoidMinRadiusPx)
            reader.Fail($"{biomeId}, pièce « {piece.Id} » : void.radii d'au moins {VoidMinRadiusPx} px, une cellule d'Effacement");
        else if (Mathf.Abs(center.X) + radii.X > half.X || Mathf.Abs(center.Y) + radii.Y > half.Y)
            reader.Fail($"{biomeId}, pièce « {piece.Id} » : void déborde de half_extent_px");
        else if (piece.Memorial is Vector2 slot && InEllipse(slot, center, radii + Vector2.One * VoidMemorialMarginPx))
            reader.Fail($"{biomeId}, pièce « {piece.Id} » : memorial dans le void ou à moins de {VoidMemorialMarginPx} px de son bord");
        else if (!OnTileGrid(center))
            reader.Fail($"{biomeId}, pièce « {piece.Id} » : void.at hors de la grille des tuiles (x multiple de {TileStepXPx}, "
                + $"y de {TileStepYPx}, x/{TileStepXPx} + y/{TileStepYPx} pair), son bord ne tomberait pas juste");
        piece.VoidCenter = center;
        piece.VoidRadii = radii;
    }

    /// <summary>Un bassin couvre au moins une cellule de tuile, tient dans l'emprise et laisse la place du Mémorial au sec.</summary>
    private static void ReadBasin(JsonConfigReader reader, JsonElement element, string biomeId, Piece piece)
    {
        string where = $"{biomeId}, pièce « {piece.Id} », water";
        reader.AllowOnly(element, where, "at", "half_extent_px");
        Vector2 center = SitePlan.Vector(reader, element, "at");
        Vector2 half = SitePlan.Vector(reader, element, "half_extent_px");
        if (reader.Error != null)
            return;
        Vector2 extent = piece.Plan.HalfExtentPx;
        Rect2 basin = new(center - half, half * 2f);
        if (half.X < BasinMinHalfXPx || half.Y < BasinMinHalfYPx)
            reader.Fail($"{where} : half_extent_px d'au moins {BasinMinHalfXPx} × {BasinMinHalfYPx} px, une cellule de tuile");
        else if (Mathf.Abs(center.X) + half.X > extent.X || Mathf.Abs(center.Y) + half.Y > extent.Y)
            reader.Fail($"{where} : bassin hors de half_extent_px");
        else if (piece.Memorial is Vector2 slot && Mathf.Abs(slot.X - center.X) <= half.X + BasinMemorialMarginPx
                 && Mathf.Abs(slot.Y - center.Y) <= half.Y + BasinMemorialMarginPx)
            reader.Fail($"{where} : memorial dans un bassin ou à moins de {BasinMemorialMarginPx} px de son bord");
        if (reader.Error == null)
            piece.Water.Add(basin);
    }

    /// <summary>
    /// Une ancre de tuile plus ce décalage peut-elle tomber pile au centre d'une cellule d'Effacement ? Les rangées paires
    /// et impaires décalent x de 32 px : x et y doivent changer de rangée ensemble, retourné ou non.
    /// </summary>
    private static bool OnTileGrid(Vector2 offset)
    {
        if (offset.X != Mathf.Round(offset.X) || offset.Y != Mathf.Round(offset.Y))
            return false;
        int x = (int)offset.X;
        int y = (int)offset.Y;
        return x % TileStepXPx == 0 && y % TileStepYPx == 0 && (x / TileStepXPx + y / TileStepYPx) % 2 == 0;
    }

    /// <summary>Le point tombe-t-il dans l'ellipse alignée sur les axes (bord compris) ?</summary>
    public static bool InEllipse(Vector2 point, Vector2 center, Vector2 radii)
    {
        Vector2 offset = point - center;
        return (offset.X * offset.X) / (radii.X * radii.X) + (offset.Y * offset.Y) / (radii.Y * radii.Y) <= 1f;
    }

    private static string[] BuildPieceKeys()
    {
        List<string> keys = new(SitePlan.CommonKeys) { "id", "memorial", "ground", "void", "water" };
        return keys.ToArray();
    }
}
