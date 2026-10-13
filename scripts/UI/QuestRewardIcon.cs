using Godot;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.UI;

/// <summary>Icône de ce qu'une quête débloque : le personnage, l'arme ou l'objet, la première qui existe.</summary>
public static class QuestRewardIcon
{
    public static Texture2D For(QuestDefinition quest)
    {
        foreach (QuestUnlock unlock in quest.Unlocks)
        {
            Texture2D icon = Of(unlock);
            if (icon != null)
                return icon;
        }
        return null;
    }

    public static Texture2D Of(QuestUnlock unlock)
    {
        if (unlock.Kind == UnlockKind.Character)
            return CharacterPortrait.Idle(unlock.Id);
        string path = QuestBook.UnlockIcon(unlock);
        return !string.IsNullOrEmpty(path) && ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
    }
}
