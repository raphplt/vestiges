using System.Collections.Generic;
using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.Combat;

/// <summary>
/// État ajouté à une créature de base : variante (élite, Souverain, Aberration), affixes,
/// traversée forcée d'une harde et rattachement à un micro-événement. Remis à zéro au retour au pool.
/// </summary>
public sealed class EnemyModifiers
{
    private readonly List<EnemyAffixData> _affixes = new();
    private float _regenTimer;
    private float _sinceDamaged = float.MaxValue;

    public EnemyVariantData Variant { get; private set; }
    public IReadOnlyList<EnemyAffixData> Affixes => _affixes;
    public bool IsVariant => Variant != null;
    public float DamageTakenMultiplier { get; private set; } = 1f;
    public float RegenRatioPerSec { get; private set; }
    public float DeathExplosionRadius { get; private set; }
    public float DeathExplosionDamageMult { get; private set; }
    public float RegenPauseAfterHitSec { get; private set; }

    /// <summary>Jeton du micro-événement qui a fait apparaître la créature (0 = aucun).</summary>
    public int EventToken { get; set; }
    /// <summary>Une créature d'événement n'est jamais retirée par l'éloignement.</summary>
    public bool IsEventBound { get; set; }

    public Vector2 TravelDirection { get; private set; }
    public float TravelSpeedMultiplier { get; private set; } = 1f;
    public float TravelTimeRemaining { get; private set; }
    public bool IsTraveling => TravelTimeRemaining > 0f;

    public void SetVariant(EnemyVariantData variant) => Variant = variant;

    public void AddAffix(EnemyAffixData affix)
    {
        _affixes.Add(affix);
        DamageTakenMultiplier *= affix.DamageTakenMult;
        RegenRatioPerSec += affix.RegenRatioPerSec;
        RegenPauseAfterHitSec = Mathf.Max(RegenPauseAfterHitSec, affix.RegenPauseAfterHitSec);
        if (affix.DeathExplosionRadius > DeathExplosionRadius)
        {
            DeathExplosionRadius = affix.DeathExplosionRadius;
            DeathExplosionDamageMult = affix.DeathExplosionDamageMult;
        }
    }

    public void StartTravel(Vector2 direction, float speedMultiplier, float duration)
    {
        TravelDirection = direction.Normalized();
        TravelSpeedMultiplier = speedMultiplier;
        TravelTimeRemaining = duration;
    }

    public void TickTravel(float delta) => TravelTimeRemaining = Mathf.Max(0f, TravelTimeRemaining - delta);

    /// <summary>La régénération reprend seulement après une pause sans coup reçu : l'élite punit la cible lâchée, sans devenir intuable.</summary>
    public void NotifyDamaged() => _sinceDamaged = 0f;

    /// <summary>PV régénérés pendant ce pas (0 hors affixe régénérant), appliqués chaque seconde.</summary>
    public float TickRegen(float delta, float maxHp)
    {
        if (RegenRatioPerSec <= 0f)
            return 0f;
        _sinceDamaged += delta;
        if (_sinceDamaged < RegenPauseAfterHitSec)
        {
            _regenTimer = 0f;
            return 0f;
        }
        _regenTimer += delta;
        if (_regenTimer < 1f)
            return 0f;
        _regenTimer -= 1f;
        return maxHp * RegenRatioPerSec;
    }

    /// <summary>Nom affiché : « Tisseuse Souveraine », « Rôdeur Enragé ».</summary>
    public string BuildDisplayName(string baseName, bool feminine)
    {
        string title = Variant == null ? "" : (feminine ? Variant.TitleFeminine : Variant.TitleMasculine);
        if (!string.IsNullOrEmpty(title))
            return $"{baseName} {title}";
        if (_affixes.Count > 0)
            return $"{baseName} {AffixName(_affixes[0], feminine)}";
        return baseName;
    }

    /// <summary>Affixes non repris dans le nom, séparés par un point médian.</summary>
    public string BuildAffixLine(bool feminine)
    {
        bool titled = Variant != null && !string.IsNullOrEmpty(feminine ? Variant.TitleFeminine : Variant.TitleMasculine);
        int start = titled ? 0 : 1;
        if (_affixes.Count <= start)
            return "";
        List<string> names = new();
        for (int i = start; i < _affixes.Count; i++)
            names.Add(AffixName(_affixes[i], feminine));
        return string.Join(" · ", names);
    }

    public void Reset()
    {
        Variant = null;
        _affixes.Clear();
        _regenTimer = 0f;
        _sinceDamaged = float.MaxValue;
        RegenPauseAfterHitSec = 0f;
        DamageTakenMultiplier = 1f;
        RegenRatioPerSec = 0f;
        DeathExplosionRadius = 0f;
        DeathExplosionDamageMult = 0f;
        EventToken = 0;
        IsEventBound = false;
        TravelTimeRemaining = 0f;
        TravelSpeedMultiplier = 1f;
    }

    private static string AffixName(EnemyAffixData affix, bool feminine) => feminine ? affix.NameFeminine : affix.NameMasculine;
}
