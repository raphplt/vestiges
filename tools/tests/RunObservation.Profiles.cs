using System;
using System.Collections.Generic;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.Tests;

/// <summary>
/// Niveaux de joueur du bot de mesure (plan 42 M0b), par --player-profile debutant|moyen|fort. Sans l'option, le bot
/// garde son comportement historique (première carte, --nomad et --visit à part), pour que les anciennes mesures
/// restent comparables.
/// - Débutant : erre autour du départ, ne se détourne que vers les lieux tout proches, prend une carte au hasard,
///   n'esquive pas, ne dashe pas.
/// - Moyen : garde un cap, visite les lieux vus (détour habituel), prend d'abord ce qu'il possède déjà.
/// - Fort : garde un cap, fait de longs détours, choisit par score, relance une offre sans intérêt et bannit
///   gratuitement ce qui ne sert pas, s'écarte de la foule et dashe hors d'un encerclement (trois créatures au contact) ou pour fuir, PV bas.
/// Le profil fort n'esquive pas les projectiles : aucun index des tirs ennemis n'existe, il ne lit que la foule.
/// </summary>
public partial class RunObservation
{
    private enum BotProfile { Legacy, Beginner, Average, Strong }

    private const float BeginnerDetourRange = 220f;
    private const float StrongDetourRange = 1100f;
    // Foule lue par le profil fort : distance d'écart, rayon et effectif d'un encerclement qui déclenche le dash.
    private const float DodgeRadius = 170f;
    private const float DodgeWeight = 1.6f;
    private const float EncircledRadius = 110f;
    // L'écart à la foule marche : quatre créatures à moins de 110 px n'arrivent que quelques images par run (mesure du
    // 13 octobre), d'où un seuil bas. Sous 40 % des PV, une seule créature au contact suffit pour fuir.
    private const int EncircledCount = 3;
    private const float FleeHpRatio = 0.4f;

    private BotProfile _profile = BotProfile.Legacy;
    private RandomNumberGenerator _profileRng;
    private int _profileRerolls;
    private int _profileBanishes;

    /// <summary>Lit --player-profile ; renvoie faux sans profil (bot historique).</summary>
    private bool ReadProfile(string[] args, ulong seed)
    {
        string name = Argument(args, "--player-profile", null);
        _profile = name switch
        {
            null => BotProfile.Legacy,
            "debutant" => BotProfile.Beginner,
            "moyen" => BotProfile.Average,
            "fort" => BotProfile.Strong,
            _ => throw new ArgumentException($"--player-profile {name} : debutant, moyen ou fort attendu"),
        };
        // Flux à part : le hasard des cartes du débutant ne décale pas celui des déplacements.
        _profileRng = new RandomNumberGenerator { Seed = seed ^ 0x9E3779B97F4A7C15UL };
        if (_profile != BotProfile.Legacy)
            GD.Print($"[RunObservation] profil de joueur : {name}");
        return _profile != BotProfile.Legacy;
    }

    /// <summary>Ce que le profil a fait de ses outils : relances, bannissements, dashs.</summary>
    private void AppendProfile(System.Text.StringBuilder summary)
    {
        if (_profile == BotProfile.Legacy)
            return;
        summary.Append(System.Globalization.CultureInfo.InvariantCulture,
            $" profile={_profile} rerolls_used={_profileRerolls} banishes_used={_profileBanishes} dashes={_player.Mobility.DashesStarted}");
    }

    private float ProfileDetourRange => _profile switch
    {
        BotProfile.Beginner => BeginnerDetourRange,
        BotProfile.Strong => StrongDetourRange,
        _ => PlaceDetourRange,
    };

    /// <summary>
    /// Carte du profil, ou null s'il a relancé ou banni (l'offre renouvelée est relue au passage suivant).
    /// <paramref name="fallback"/> est le choix du bot historique (première carte hors Péril, --prefer d'abord).
    /// </summary>
    private FragmentOption PickProfileCard(FragmentManager fragments, FragmentOption fallback)
    {
        IReadOnlyList<FragmentOption> options = fragments.PendingChoices;
        // --prefer reste prioritaire pour tous les profils : il impose un build.
        if (Array.IndexOf(_preferredCards, fallback.Id) >= 0)
            return fallback;
        switch (_profile)
        {
            case BotProfile.Beginner:
            {
                List<FragmentOption> allowed = new();
                foreach (FragmentOption option in options)
                    if (option.Id != PerilObjectId && !BuildRefuses(option))
                        allowed.Add(option);
                return allowed.Count > 0 ? allowed[_profileRng.RandiRange(0, allowed.Count - 1)] : fallback;
            }
            case BotProfile.Average:
            {
                foreach (FragmentOption option in options)
                    if (IsOwnedOption(option))
                        return option;
                return fallback;
            }
            case BotProfile.Strong:
            {
                FragmentOption best = fallback, worst = null;
                float bestScore = float.MinValue, worstScore = float.MaxValue;
                foreach (FragmentOption option in options)
                {
                    float score = StrongScore(option);
                    if (score > bestScore) { bestScore = score; best = option; }
                    if (score < worstScore) { worstScore = score; worst = option; }
                }
                if (bestScore < StrongRerollBelow && fragments.RerollsRemaining > 0)
                {
                    fragments.Reroll();
                    _profileRerolls++;
                    return null;
                }
                // Bannissement gratuit seulement : un bannissement payé monterait le Péril de la mesure.
                if (worst != null && worst != best && worstScore < StrongBanishBelow && fragments.BanishesRemaining > 0
                    && worst.Type is "weapon_new" or "passive_new")
                {
                    int before = fragments.BanishesRemaining;
                    fragments.BanishFragment(worst.Id);
                    if (fragments.BanishesRemaining < before)
                    {
                        _profileBanishes++;
                        return null;
                    }
                }
                return best;
            }
            default:
                return fallback;
        }
    }

    private const float StrongRerollBelow = 30f;
    private const float StrongBanishBelow = 15f;

    private bool IsOwnedOption(FragmentOption option) => option.Type is FragmentOption.AscensionType or "weapon_upgrade"
        or "passive_upgrade" or PerkSpecializationOffers.OptionType;

    /// <summary>
    /// Score du profil fort : il pousse son arme principale, monte ce qu'il a, remplit ses emplacements d'armes tôt et
    /// préfère un objet qui renforce une stat déjà travaillée par ses objets portés ; la rareté départage.
    /// </summary>
    private float StrongScore(FragmentOption option)
    {
        if (option.Id == PerilObjectId || BuildRefuses(option))
            return -100f;
        float rarity = (option.Rarity?.Rank ?? 0) * 8f;
        return rarity + option.Type switch
        {
            FragmentOption.AscensionType => 100f,
            PerkSpecializationOffers.OptionType => 45f,
            "weapon_upgrade" => option.Id == MainWeaponId() ? 60f : 40f,
            "passive_upgrade" => 35f + (SharesOwnedStat(option.Id) ? 10f : 0f),
            "weapon_new" => _player.WeaponSlots.Count < 4 ? 30f : 0f,
            "passive_new" => 20f + (SharesOwnedStat(option.Id) ? 15f : 0f),
            _ => 5f,
        };
    }

    /// <summary>Arme qui a fait le plus de dégâts jusqu'ici.</summary>
    private string MainWeaponId()
    {
        string main = null;
        float best = -1f;
        foreach (WeaponInstance weapon in _player.WeaponSlots)
        {
            float dealt = _player.GetDamageDealt(weapon.Id);
            if (dealt > best) { best = dealt; main = weapon.Id; }
        }
        return main;
    }

    /// <summary>L'objet agit sur une stat qu'un autre objet porté (sac ou Écusson) travaille déjà.</summary>
    private bool SharesOwnedStat(string objectId)
    {
        PassiveSouvenirData data = PassiveSouvenirDataLoader.Get(objectId);
        if (data == null)
            return false;
        foreach (IReadOnlyList<ActivePassiveSouvenir> shelf in new[] { _player.PassiveSlots, _player.BadgeSlots })
            foreach (ActivePassiveSouvenir owned in shelf)
            {
                if (owned.Data.Id == objectId)
                    continue;
                foreach (PassiveEffectData mine in owned.Data.Effects)
                    foreach (PassiveEffectData theirs in data.Effects)
                        if (mine.Stat == theirs.Stat)
                            return true;
            }
        return false;
    }

    /// <summary>
    /// Profil fort : la marche vers le point de passage s'écarte de la foule proche, et un encerclement déclenche un
    /// dash vers le côté le plus libre.
    /// </summary>
    private Vector2 StrongSteer(Vector2 direction)
    {
        Vector2 position = _player.GlobalPosition;
        Vector2 away = Vector2.Zero;
        int close = 0;
        using (CrowdQuery crowd = CrowdIndex.Near(position, DodgeRadius))
        {
            foreach (Node2D target in crowd.Targets)
            {
                Vector2 offset = position - target.GlobalPosition;
                float distance = offset.Length();
                if (distance > DodgeRadius || distance < 0.01f)
                    continue;
                // Plus la créature est proche, plus elle repousse.
                away += offset / distance * (1f - distance / DodgeRadius);
                if (distance <= EncircledRadius)
                    close++;
            }
        }
        Vector2 steered = (direction + away * DodgeWeight).Normalized();
        if (steered == Vector2.Zero)
            steered = direction;
        bool threatened = close >= EncircledCount || (close > 0 && _player.CurrentHp < _player.EffectiveMaxHp * FleeHpRatio);
        if (threatened && _player.Mobility.SecondsSinceDash >= _player.Mobility.Config.CooldownSeconds)
            _player.Mobility.Request();
        return steered;
    }
}
