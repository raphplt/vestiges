using System.Collections.Generic;
using System.Text;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.Progression;

/// <summary>
/// Lecture des quêtes de déblocage pour les menus (plan 06 §9) : noms des pièces, avancée retenue et phrase de verrou.
/// Rien n'est affiché en run ; le Hub et le bilan s'en servent.
/// </summary>
public static class QuestBook
{
    /// <summary>Nom affiché d'une pièce débloquée, ou son id si le catalogue ne la connaît pas.</summary>
    public static string UnlockName(QuestUnlock unlock) => unlock.Kind switch
    {
        UnlockKind.Weapon => WeaponDataLoader.Get(unlock.Id)?.Name ?? unlock.Id,
        UnlockKind.Object or UnlockKind.Badge => PassiveSouvenirDataLoader.Get(unlock.Id)?.Name ?? unlock.Id,
        UnlockKind.Reminiscence => PerkSpecializationDataLoader.Get(unlock.Id)?.Name ?? ReminiscenceArt(unlock.Id)?.Name ?? unlock.Id,
        _ => unlock.Name ?? CharacterDataLoader.Get(unlock.Id)?.Name ?? unlock.Id,
    };

    /// <summary>Icône d'une pièce (arme ou objet), ou null.</summary>
    public static string UnlockIcon(QuestUnlock unlock) => unlock.Kind switch
    {
        UnlockKind.Weapon => WeaponDataLoader.Get(unlock.Id)?.Sprite,
        UnlockKind.Object or UnlockKind.Badge => PassiveSouvenirDataLoader.Get(unlock.Id)?.Icon,
        UnlockKind.Reminiscence => PerkSpecializationDataLoader.Get(unlock.Id)?.Icon ?? ReminiscenceArt(unlock.Id)?.Icon,
        _ => null,
    };

    private static CollectionArtDataLoader.Entry? ReminiscenceArt(string id)
    {
        foreach (CollectionArtDataLoader.Entry entry in CollectionArtDataLoader.Perks)
            if (entry.Id == id)
                return entry;
        return null;
    }

    /// <summary>
    /// Quête en attente : elle ne débloque que des Réminiscences dont l'effet n'est pas encore codé (§117), ou elle est
    /// en réserve avec le système qu'elle exerce (plan 56 B2). Elle reste prête dans le catalogue et garde sa pièce,
    /// mais n'est ni affichée ni suivie : le joueur ne peut pas l'accomplir pour rien, ni s'y heurter.
    /// </summary>
    public static bool IsDormant(QuestDefinition quest)
    {
        if (quest.Reserve && !ReserveSwitch.Revived)
            return true;
        foreach (QuestUnlock unlock in quest.Unlocks)
        {
            if (unlock.Kind != UnlockKind.Reminiscence)
                return false;
            PerkSpecializationData perk = PerkSpecializationDataLoader.Get(unlock.Id);
            if (perk != null && PerkSpecializationEffects.IsImplemented(perk.Effect))
                return false;
        }
        return true;
    }

    /// <summary>« Le Traqueur et Arc du gymnase ».</summary>
    public static string RewardSummary(QuestDefinition quest)
    {
        StringBuilder text = new();
        for (int i = 0; i < quest.Unlocks.Count; i++)
        {
            if (i > 0)
                text.Append(i == quest.Unlocks.Count - 1 ? " et " : ", ");
            text.Append(UnlockName(quest.Unlocks[i]));
        }
        return text.ToString();
    }

    /// <summary>Part accomplie de 0 à 1 : la condition la moins avancée fait foi.</summary>
    public static float Fraction(QuestDefinition quest, IReadOnlyList<float> values)
    {
        float fraction = 1f;
        for (int i = 0; i < quest.Conditions.Count; i++)
        {
            float value = i < values.Count ? values[i] : 0f;
            fraction = Mathf.Min(fraction, Mathf.Clamp(value / quest.Conditions[i].Target, 0f, 1f));
        }
        return fraction;
    }

    /// <summary>« 120 / 290 », une paire par condition ; une durée s'écrit en minutes.</summary>
    public static string ProgressText(QuestDefinition quest, IReadOnlyList<float> values)
    {
        StringBuilder text = new();
        for (int i = 0; i < quest.Conditions.Count; i++)
        {
            QuestCondition condition = quest.Conditions[i];
            float value = Mathf.Min(i < values.Count ? values[i] : 0f, condition.Target);
            if (i > 0)
                text.Append("  ·  ");
            text.Append(condition.Stat == QuestStat.HealthyTime
                ? $"{FormatDuration(value)} / {FormatDuration(condition.Target)}"
                : $"{Mathf.FloorToInt(value):N0} / {Mathf.FloorToInt(condition.Target):N0}");
        }
        return text.ToString();
    }

    /// <summary>Phrase d'une pièce verrouillée : la quête qui l'ouvre et sa condition ; vide si elle est libre.</summary>
    public static string LockText(UnlockKind kind, string id)
    {
        QuestDefinition quest = QuestDataLoader.FindUnlocking(kind, id);
        if (quest == null)
            return "";
        // Quête en réserve : son système est éteint, sa condition ne se remplit pas en run (plan 56 B2).
        return quest.Reserve && IsDormant(quest) ? "En réserve pour l'instant." : $"Quête « {quest.Name} » : {quest.Description}";
    }

    private static string FormatDuration(float seconds)
    {
        int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
        return $"{total / 60}:{total % 60:00}";
    }
}
