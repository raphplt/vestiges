using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Terrain rythmé d'une terre (plan 58 T1a) : un bruit lent découpe le biome en zones (clairières, fourrés, places,
/// paliers…), chacune avec sa densité de décors, ses décors favoris et parfois son sol. Lu dans la clé « rhythm » du
/// fichier de décors du biome ; le code ne connaît que des zones pondérées. Une terre peut aussi y tracer des chenaux
/// (plan 58 T2a) : des rubans d'eau que les chemins franchissent sur des passerelles.
/// </summary>
public sealed class TerrainRhythm
{
    public const byte NoZone = byte.MaxValue;
    // L'index d'une zone tient sous le bit de trouée, loin de NoZone.
    private const int MaxZones = 16;
    // Bit de la carte des zones : la cellule est sur une trouée, rien n'y bloque.
    private const byte LaneBit = 0x80;
    // Bit de la carte des zones : la cellule est l'eau d'un chenal, qu'un chemin peut franchir.
    private const byte ChannelBit = 0x40;
    private const ulong ZoneSalt = 0x7A11E5UL;
    private const ulong LaneSalt = 0x1A4E5UL;
    private const ulong ChannelSalt = 0xC4A77E1UL;
    private const ulong FordSalt = 0xF02DUL;
    // Le départ reste au sec : deux cellules de plus que son dégagement.
    private const int SpawnDryMargin = 2;

    public sealed class Zone
    {
        public string Id;
        /// <summary>Borne haute du bruit lent (dans [0 ; 1]) : la première zone dont la borne dépasse le bruit gagne.</summary>
        public float UpTo;
        public float DensityScale = 1f;
        /// <summary>Sol imposé (herbe, sous-bois) ; l'eau et le béton restent.</summary>
        public TerrainType? Ground;
        /// <summary>Multiplicateur des poids des décors absents de <see cref="Weights"/>.</summary>
        public float OtherWeightScale = 1f;
        public Dictionary<string, float> Weights = new();
        /// <summary>Trouées : une ligne sinueuse du bruit de trouée (isoligne 0,5), de demi-largeur LaneWidth, où rien ne bloque.</summary>
        public float LaneScalePx;
        public float LaneWidth;
    }

    /// <summary>
    /// Chenaux : l'isoligne 0,5 d'un bruit de pas <see cref="ScalePx"/>, de demi-largeur <see cref="Width"/>, devient de
    /// l'eau, sauf aux gués (bruit des gués sous <see cref="FordBelow"/>). Un chemin y passe sur une passerelle,
    /// tournée selon sa traversée (<see cref="BridgeHeading"/>).
    /// </summary>
    public sealed class Channels
    {
        public float ScalePx;
        public float Width;
        public float FordScalePx;
        public float FordBelow;
        /// <summary>Image de passerelle par sens, indexée par <see cref="BridgeHeading"/>.</summary>
        public string[] Bridges;
    }

    /// <summary>Taille des zones à l'écran, en pixels : le pas du réseau du bruit lent.</summary>
    public float ScalePx;
    public List<Zone> Zones = new();
    public Channels ChannelSet;

    public static byte ZoneIndex(byte packed) => packed == NoZone ? NoZone : (byte)(packed & ~(LaneBit | ChannelBit));
    public static bool IsLane(byte packed) => packed != NoZone && (packed & LaneBit) != 0;
    public static bool IsChannel(byte packed) => packed != NoZone && (packed & ChannelBit) != 0;

    /// <summary>
    /// Zones de chaque cellule des biomes rythmés, sur le thread de génération ; impose leur sol et l'eau des chenaux à la
    /// grille de terrain. Rend nul si aucun biome actif n'a de rythme.
    /// </summary>
    public static byte[,] Apply(WorldGenerator generator, TerrainType[,] terrain, IReadOnlyDictionary<string, TerrainRhythm> rhythms,
        ulong seed)
    {
        bool any = false;
        foreach (BiomeData biome in generator.ActiveBiomes)
            any |= rhythms.ContainsKey(biome.Id);
        if (!any)
            return null;

        int sizeX = terrain.GetLength(0);
        int sizeY = terrain.GetLength(1);
        byte[,] zones = new byte[sizeX, sizeY];
        Dictionary<string, int> cellsByZone = new();
        int laneCells = 0;
        int channelCells = 0;
        ulong zoneSalt = seed ^ ZoneSalt;
        ulong laneSalt = seed ^ LaneSalt;
        ulong channelSalt = seed ^ ChannelSalt;
        ulong fordSalt = seed ^ FordSalt;
        int dry = generator.SpawnClearance + SpawnDryMargin;
        for (int gx = 0; gx < sizeX; gx++)
        {
            for (int gy = 0; gy < sizeY; gy++)
            {
                zones[gx, gy] = NoZone;
                int x = gx - generator.MapRadiusX;
                int y = gy - generator.MapRadiusY;
                if (!generator.IsWithinBounds(x, y) || !rhythms.TryGetValue(generator.GetBiome(x, y).Id, out TerrainRhythm rhythm))
                    continue;
                Vector2 point = PathNetworkGenerator.CellCenter(new Vector2I(x, y));
                int index = rhythm.ZoneAt(Fractal(point, rhythm.ScalePx, zoneSalt));
                Zone zone = rhythm.Zones[index];
                byte packed = (byte)index;
                if (zone.LaneWidth > 0f && Mathf.Abs(Fractal(point, zone.LaneScalePx, laneSalt) - 0.5f) < zone.LaneWidth)
                {
                    packed |= LaneBit;
                    laneCells++;
                }
                cellsByZone[zone.Id] = cellsByZone.GetValueOrDefault(zone.Id) + 1;
                zones[gx, gy] = packed;
                if (zone.Ground is TerrainType ground && terrain[gx, gy] is TerrainType.Grass or TerrainType.Forest)
                    terrain[gx, gy] = ground;
                if (rhythm.ChannelSet is Channels channels && terrain[gx, gy] is TerrainType.Grass or TerrainType.Forest
                    && (Mathf.Abs(x) > dry || Mathf.Abs(y) > dry)
                    && Mathf.Abs(Fractal(point, channels.ScalePx, channelSalt) - 0.5f) < channels.Width
                    && Fractal(point, channels.FordScalePx, fordSalt) >= channels.FordBelow)
                {
                    terrain[gx, gy] = TerrainType.Water;
                    zones[gx, gy] |= ChannelBit;
                    channelCells++;
                }
            }
        }
        int total = 0;
        foreach (int count in cellsByZone.Values)
            total += count;
        List<string> shares = new();
        foreach ((string id, int count) in cellsByZone)
            shares.Add($"{id} {100 * count / Mathf.Max(1, total)} %");
        GD.Print($"[TerrainRhythm] {string.Join(" · ", shares)} · trouées {laneCells} cellules · chenaux {channelCells} cellules");
        return zones;
    }

    private int ZoneAt(float value)
    {
        for (int i = 0; i < Zones.Count - 1; i++)
        {
            if (value < Zones[i].UpTo)
                return i;
        }
        return Zones.Count - 1;
    }

    /// <summary>Bruit de valeur lissé, deux octaves, dans [0 ; 1] : pur C#, sans état, sûr sur le thread de génération.</summary>
    private static float Fractal(Vector2 point, float scalePx, ulong salt)
    {
        float value = Smooth(point / scalePx, salt) * 0.7f + Smooth(point / (scalePx * 0.45f), salt + 1UL) * 0.3f;
        return Mathf.Clamp(value, 0f, 1f);
    }

    private static float Smooth(Vector2 p, ulong salt)
    {
        int ix = Mathf.FloorToInt(p.X);
        int iy = Mathf.FloorToInt(p.Y);
        float tx = p.X - ix;
        float ty = p.Y - iy;
        tx = tx * tx * (3f - 2f * tx);
        ty = ty * ty * (3f - 2f * ty);
        float top = Mathf.Lerp(CellHash.Unit(ix, iy, salt), CellHash.Unit(ix + 1, iy, salt), tx);
        float bottom = Mathf.Lerp(CellHash.Unit(ix, iy + 1, salt), CellHash.Unit(ix + 1, iy + 1, salt), tx);
        return Mathf.Lerp(top, bottom, ty);
    }

    /// <summary>Clé « rhythm » d'un fichier de décors ; les décors nommés dans les poids et les images des passerelles doivent exister.</summary>
    public static TerrainRhythm Parse(JsonConfigReader reader, JsonElement element, IReadOnlyCollection<string> propIds,
        Func<string, bool> spriteExists)
    {
        reader.AllowOnly(element, "rhythm", "scale_px", "zones", "channels");
        TerrainRhythm rhythm = new() { ScalePx = reader.Between(element, "scale_px", 128f, 8192f) };
        if (JsonConfigReader.Has(element, "channels"))
            rhythm.ChannelSet = ParseChannels(reader, reader.Section(element, "channels"), spriteExists);
        HashSet<string> ids = new();
        float previous = 0f;
        List<JsonElement> zones = reader.List(element, "zones", 2);
        if (zones.Count > MaxZones)
            reader.Fail($"rhythm : {zones.Count} zones, {MaxZones} au plus");
        for (int i = 0; i < zones.Count; i++)
        {
            JsonElement item = zones[i];
            reader.AllowOnly(item, "rhythm.zones", "id", "up_to", "density", "ground", "others", "weights", "lanes");
            Zone zone = new()
            {
                Id = reader.Text(item, "id"),
                UpTo = reader.Ratio(item, "up_to"),
                DensityScale = JsonConfigReader.Has(item, "density") ? reader.NonNegative(item, "density") : 1f,
                OtherWeightScale = JsonConfigReader.Has(item, "others") ? reader.NonNegative(item, "others") : 1f,
            };
            if (reader.Error == null && !ids.Add(zone.Id))
                reader.Fail($"rhythm : zone « {zone.Id} » en double");
            if (reader.Error == null && zone.UpTo <= previous)
                reader.Fail($"rhythm : zone « {zone.Id} », up_to doit croître d'une zone à la suivante");
            if (reader.Error == null && i == zones.Count - 1 && zone.UpTo < 1f)
                reader.Fail($"rhythm : la dernière zone « {zone.Id} » doit aller jusqu'à 1");
            previous = zone.UpTo;
            if (JsonConfigReader.Has(item, "ground"))
            {
                string ground = reader.Text(item, "ground");
                if (ground == "grass")
                    zone.Ground = TerrainType.Grass;
                else if (ground == "forest")
                    zone.Ground = TerrainType.Forest;
                else
                    reader.Fail($"rhythm : zone « {zone.Id} », sol « {ground} » inconnu (grass, forest)");
            }
            if (JsonConfigReader.Has(item, "weights"))
            {
                JsonElement weights = reader.Section(item, "weights");
                foreach ((string id, JsonElement _) in reader.Entries(weights, "rhythm.weights"))
                {
                    if (!JsonConfigReader.Contains(propIds, id))
                        reader.Fail($"rhythm : zone « {zone.Id} », décor « {id} » absent de props");
                    zone.Weights[id] = reader.NonNegative(weights, id);
                }
            }
            if (JsonConfigReader.Has(item, "lanes"))
            {
                JsonElement lanes = reader.Section(item, "lanes");
                reader.AllowOnly(lanes, "rhythm.lanes", "scale_px", "width");
                zone.LaneScalePx = reader.Between(lanes, "scale_px", 64f, 4096f);
                zone.LaneWidth = reader.Between(lanes, "width", 0.01f, 0.25f);
            }
            if (reader.Error != null)
                break;
            rhythm.Zones.Add(zone);
        }
        return rhythm;
    }

    /// <summary>
    /// Clé « channels » : bruits des chenaux et des gués, et les images des quatre passerelles (chemins sans « res:// »,
    /// comme les décors du fichier).
    /// </summary>
    private static Channels ParseChannels(JsonConfigReader reader, JsonElement element, Func<string, bool> spriteExists)
    {
        reader.AllowOnly(element, "rhythm.channels", "scale_px", "width", "fords", "bridges");
        Channels channels = new()
        {
            ScalePx = reader.Between(element, "scale_px", 128f, 8192f),
            Width = reader.Between(element, "width", 0.005f, 0.2f),
        };
        JsonElement fords = reader.Section(element, "fords");
        reader.AllowOnly(fords, "rhythm.channels.fords", "scale_px", "below");
        channels.FordScalePx = reader.Between(fords, "scale_px", 64f, 4096f);
        channels.FordBelow = reader.Between(fords, "below", 0.05f, 0.95f);
        JsonElement bridges = reader.Section(element, "bridges");
        string[] keys = { "along_x", "along_y", "falling", "rising" };
        reader.AllowOnly(bridges, "rhythm.channels.bridges", keys);
        channels.Bridges = new string[keys.Length];
        for (int i = 0; i < keys.Length; i++)
        {
            channels.Bridges[i] = reader.Text(bridges, keys[i]);
            if (reader.Error == null && !spriteExists(channels.Bridges[i]))
                reader.Fail($"rhythm.channels.bridges : image « {channels.Bridges[i]} » introuvable");
        }
        return channels;
    }
}
