using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Progression;

/// <summary>
/// Retour des objets (DECISIONS §53, plan 21 F4) : quand un objet agit (explosion, soin, statut posé, bonus de
/// dégâts qui s'applique), sa petite icône s'élève un instant au-dessus du personnage. Au plus une fois par
/// <see cref="CooldownSeconds"/> et par objet ; six sprites recyclés, rien n'est créé pendant le combat.
/// </summary>
public partial class ObjectProcs : Node2D
{
    private const int PoolSize = 6;
    private const float CooldownSeconds = 1f;
    private const float LifeSeconds = 0.7f;
    private const float RisePx = 14f;
    private const float StartHeight = 40f;
    private const float Spacing = 18f;

    private static readonly Dictionary<string, PassiveSouvenirData> ObjectByStat = new();
    private static bool _indexed;

    private readonly Sprite2D[] _icons = new Sprite2D[PoolSize];
    private readonly float[] _ages = new float[PoolSize];
    private readonly Dictionary<string, ulong> _nextAllowedMsec = new();
    private readonly Dictionary<string, Texture2D> _textures = new();
    private Player _player;
    private int _next;
    private int _active;

    /// <summary>Icônes en cours d'animation (bancs).</summary>
    public int ActiveCount => _active;

    public override void _Ready()
    {
        _player = GetParent<Player>();
        ZAsRelative = false;
        ZIndex = 25;
        for (int i = 0; i < PoolSize; i++)
        {
            _icons[i] = new Sprite2D { TextureFilter = TextureFilterEnum.Nearest, Visible = false };
            AddChild(_icons[i]);
            _ages[i] = -1f;
        }
        SetProcess(false);
    }

    /// <summary>L'objet dont l'effet principal est <paramref name="stat"/> vient d'agir.</summary>
    public void Show(string stat)
    {
        // Une stat peut venir d'ailleurs (bonus de coffre, Repère) : l'icône ne montre qu'un objet porté.
        PassiveSouvenirData data = ObjectFor(stat);
        if (data == null || _player.GetPassiveLevel(data.Id) <= 0)
            return;
        ulong now = Time.GetTicksMsec();
        if (_nextAllowedMsec.TryGetValue(data.Id, out ulong allowed) && now < allowed)
            return;
        _nextAllowedMsec[data.Id] = now + (ulong)(CooldownSeconds * 1000f);
        if (!_textures.TryGetValue(data.Id, out Texture2D texture))
        {
            string path = string.IsNullOrEmpty(data.IconSmall) ? data.Icon : data.IconSmall;
            texture = string.IsNullOrEmpty(path) ? null : GD.Load<Texture2D>(path);
            _textures[data.Id] = texture;
        }
        if (texture == null)
            return;

        int slot = _next;
        _next = (_next + 1) % PoolSize;
        if (_ages[slot] < 0f)
            _active++;
        // Icônes simultanées décalées en éventail, pour rester lisibles.
        float offset = (_active - 1) % 3 * Spacing - Spacing;
        _icons[slot].Texture = texture;
        _icons[slot].Position = new Vector2(offset, -StartHeight);
        _icons[slot].Modulate = Colors.White;
        _icons[slot].Visible = true;
        _ages[slot] = 0f;
        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        for (int i = 0; i < PoolSize; i++)
        {
            if (_ages[i] < 0f)
                continue;
            _ages[i] += dt;
            if (_ages[i] >= LifeSeconds)
            {
                _ages[i] = -1f;
                _icons[i].Visible = false;
                _active--;
                continue;
            }
            float t = _ages[i] / LifeSeconds;
            _icons[i].Position = new Vector2(_icons[i].Position.X, -StartHeight - RisePx * t);
            _icons[i].Modulate = new Color(1f, 1f, 1f, t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f);
        }
        if (_active <= 0)
        {
            _active = 0;
            SetProcess(false);
        }
    }

    private static PassiveSouvenirData ObjectFor(string stat)
    {
        if (!_indexed)
        {
            _indexed = true;
            foreach (PassiveSouvenirData data in PassiveSouvenirDataLoader.GetAll())
                foreach (PassiveEffectData effect in data.Effects)
                    ObjectByStat.TryAdd(effect.Stat, data);
        }
        return ObjectByStat.GetValueOrDefault(stat);
    }
}
