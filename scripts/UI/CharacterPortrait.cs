using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.UI;

/// <summary>
/// Portrait d'un personnage pour les menus : l'image soignée <c>portrait.png</c> de son dossier si elle existe (images
/// promises par Raphaël, plan 06 §9.10), sinon les poses de repos du sprite de jeu, de face.
/// </summary>
public static class CharacterPortrait
{
    public const int IdleFrames = 6;

    /// <summary>Image soignée du personnage, ou null tant qu'elle n'est pas fournie.</summary>
    public static Texture2D Artwork(string characterId)
    {
        string path = $"res://assets/characters/{Folder(characterId)}/portrait.png";
        return ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
    }

    /// <summary>Pose de repos de face (1 à <see cref="IdleFrames"/>), ou null si le sprite manque.</summary>
    public static Texture2D Idle(string characterId, int frame = 1)
    {
        string folder = Folder(characterId);
        string path = $"res://assets/characters/{folder}/char_{characterId}_S_idle_{frame:00}.png";
        return ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
    }

    private static string Folder(string characterId) =>
        CharacterDataLoader.Get(characterId)?.SpriteFolder ?? characterId;
}
