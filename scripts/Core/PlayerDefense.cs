using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.Core;

/// <summary>
/// Défense du joueur (plan 03, lot 8B). Le bouclier encaisse un coup entier, quelle que soit sa force :
/// il empêche d'être tué d'un seul coup, puis se recharge après quelques secondes sans être touché.
/// Une blessure accorde une invulnérabilité brève, pour qu'une foule au contact ne vide pas la vie d'un coup.
/// L'armure réduit les dégâts en pourcentage, avec un rendement décroissant.
/// </summary>
public sealed class PlayerDefense
{
    /// <summary>Issue d'un coup reçu : ce que l'armure laisse passer, ce qui atteint les PV, ce que le bouclier a encaissé.</summary>
    public readonly record struct Outcome(float Reduced, float HpDamage, bool ShieldAbsorbed, bool ShieldBroke);

    private float _baseShield;
    private float _bonusShield;
    private float _invulnerableTimer;
    private float _sinceLastHit;
    private bool _disabled;

    public PlayerDefense(DefenseConfig config)
    {
        Config = config;
    }

    public DefenseConfig Config { get; }
    public float MaxShield => _baseShield + _bonusShield;
    public float Shield { get; private set; }
    public bool IsInvulnerable => _invulnerableTimer > 0f;
    public float InvulnerableTimer => _invulnerableTimer;

    public void SetBaseShield(float value)
    {
        _baseShield = Mathf.Max(0f, value);
        Shield = MaxShield;
    }

    public void AddShield(float value)
    {
        _bonusShield += value;
        Shield = Mathf.Clamp(Shield + value, 0f, MaxShield);
    }

    /// <summary>Bancs de régression : sans bouclier ni invulnérabilité, chaque coup se lit sur les PV.</summary>
    public void Disable()
    {
        _baseShield = 0f;
        _bonusShield = 0f;
        Shield = 0f;
        _disabled = true;
        _invulnerableTimer = 0f;
    }

    public float ArmorReduction(float armor) =>
        armor <= 0f ? 0f : Mathf.Min(Config.ArmorMaxReduction, armor / (armor + Config.ArmorHalfValue));

    /// <summary>Avance les minuteurs. Rend vrai si la valeur du bouclier a changé.</summary>
    public bool Step(float dt)
    {
        if (_invulnerableTimer > 0f)
            _invulnerableTimer = Mathf.Max(0f, _invulnerableTimer - dt);

        _sinceLastHit += dt;
        if (Shield >= MaxShield || _sinceLastHit < Config.ShieldRechargeDelaySeconds)
            return false;

        float rate = MaxShield / Mathf.Max(0.01f, Config.ShieldRechargeSeconds);
        Shield = Mathf.Min(MaxShield, Shield + rate * dt);
        return true;
    }

    /// <summary>Invulnérabilité accordée par un effet (Boîte de pansements) ; ne raccourcit jamais celle en cours.</summary>
    public void GrantInvulnerability(float seconds) => _invulnerableTimer = Mathf.Max(_invulnerableTimer, seconds);

    /// <summary>Résout un coup déjà accepté (ni esquivé ni pendant l'invulnérabilité).</summary>
    public Outcome Absorb(float damage, float armor)
    {
        float reduced = damage * (1f - ArmorReduction(armor));
        _sinceLastHit = 0f;
        _invulnerableTimer = _disabled ? 0f : Config.HurtInvulnerabilitySeconds;

        if (Shield > 0f)
        {
            Shield -= reduced;
            bool broke = Shield <= 0f;
            if (broke)
                Shield = 0f;
            return new Outcome(reduced, 0f, true, broke);
        }

        return new Outcome(reduced, Mathf.Max(1f, reduced), false, false);
    }
}
