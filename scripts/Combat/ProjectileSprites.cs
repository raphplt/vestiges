using System.Collections.Generic;
using Godot;

namespace Vestiges.Combat;

/// <summary>
/// Sprites des projectiles du joueur, générés par tools/generate_projectiles.py : une planche par projectile,
/// une colonne par direction d'écran (16, depuis l'est, sens horaire), une ligne par frame.
/// Découpés une seule fois en AtlasTexture ; le jeu choisit la direction au lieu de tourner le sprite.
/// </summary>
public static class ProjectileSprites
{
    private const string Folder = "res://assets/vfx/projectiles/";

    public sealed class SpriteSet
    {
        private readonly Texture2D[] _textures;

        public int Directions { get; }
        public int Frames { get; }
        public float Fps { get; }

        public SpriteSet(Texture2D sheet, Vector2I frameSize, int directions, int frames, float fps)
        {
            Directions = directions;
            Frames = frames;
            Fps = fps;
            _textures = new Texture2D[directions * frames];
            for (int frame = 0; frame < frames; frame++)
            {
                for (int direction = 0; direction < directions; direction++)
                {
                    _textures[frame * directions + direction] = new AtlasTexture
                    {
                        Atlas = sheet,
                        Region = new Rect2(direction * frameSize.X, frame * frameSize.Y, frameSize.X, frameSize.Y),
                    };
                }
            }
        }

        public int DirectionIndex(Vector2 direction)
        {
            if (Directions <= 1)
                return 0;
            float step = Mathf.Tau / Directions;
            int index = Mathf.RoundToInt(Mathf.PosMod(direction.Angle(), Mathf.Tau) / step);
            return index % Directions;
        }

        public Texture2D Get(int direction, int frame) => _textures[(frame % Frames) * Directions + direction];
    }

    private static Dictionary<string, SpriteSet> _sets;

    public static SpriteSet Get(string id)
    {
        if (_sets == null)
            Load();
        return id != null && _sets.TryGetValue(id, out SpriteSet set) ? set : null;
    }

    private static void Load()
    {
        _sets = new Dictionary<string, SpriteSet>();
        using FileAccess file = FileAccess.Open(Folder + "projectiles_manifest.json", FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PushError("[ProjectileSprites] projectiles_manifest.json introuvable");
            return;
        }
        Json json = new();
        if (json.Parse(file.GetAsText()) != Error.Ok)
        {
            GD.PushError($"[ProjectileSprites] Manifeste illisible : {json.GetErrorMessage()}");
            return;
        }
        Godot.Collections.Dictionary manifest = json.Data.AsGodotDictionary();
        foreach (Variant key in manifest.Keys)
        {
            string id = key.AsString();
            Godot.Collections.Dictionary entry = manifest[key].AsGodotDictionary();
            Texture2D sheet = GD.Load<Texture2D>($"{Folder}proj_{id}.png");
            if (sheet == null)
            {
                GD.PushError($"[ProjectileSprites] Planche manquante : proj_{id}.png");
                continue;
            }
            Godot.Collections.Array frame = entry["frame"].AsGodotArray();
            _sets[id] = new SpriteSet(
                sheet,
                new Vector2I(frame[0].AsInt32(), frame[1].AsInt32()),
                entry["directions"].AsInt32(),
                entry["frames"].AsInt32(),
                (float)entry["fps"].AsDouble());
        }
    }
}
