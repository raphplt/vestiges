#if TOOLS
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Progression;

namespace Vestiges.Infrastructure;

/// <summary>
/// Trace de survie des parties jouées dans un build d'édition (plan 42 M0e), mode dev ou non, jamais dans les bancs :
/// à la fin de la run, <c>traces/&lt;date&gt;-&lt;personnage&gt;.csv</c>, son résumé <c>.txt</c> et le build final
/// <c>-build.txt</c> (armes, ascensions, Écussons, objets, Réminiscences, dégâts par arme) dans le profil. Sert à vérifier
/// que le profil « fort » du bot de mesure ressemble à un vrai joueur (<c>tools/compare_play_traces.py</c>) et à
/// retrouver le build d'une run cassée (DECISIONS §154). Absent de l'export.
/// </summary>
public partial class PlayTraceRecorder : Node
{
    private PlayTrace _trace;
    private Player _player;
    private PlayerProgression _progression;
    private GameManager _gameManager;
    private EventBus _bus;
    private readonly Dictionary<(string Weapon, DamageKind Kind), double> _damage = new();

    public void Setup(Player player) => _player = player;

    public override void _Ready()
    {
        _gameManager = GetNode<GameManager>("/root/GameManager");
        _progression = _player.GetNode<PlayerProgression>("PlayerProgression");
        _bus = GetNode<EventBus>("/root/EventBus");
        _trace = new PlayTrace(_bus, _player);
        _bus.EnemyDamageResolved += OnDamage;
    }

    private void OnDamage(DamageResult result)
    {
        if (!result.Applied || !result.Source.IsPlayerOwned || result.HpLost <= 0f)
            return;
        (string, DamageKind) key = (result.Source.Weapon?.Id ?? "-", result.Source.Kind);
        _damage[key] = _damage.GetValueOrDefault(key) + result.HpLost;
    }

    public override void _Process(double delta)
    {
        if (_gameManager.CurrentState == GameManager.GameState.Run)
            _trace.Advance(delta);
    }

    public override void _ExitTree()
    {
        if (_trace == null)
            return;
        _trace.Dispose();
        _bus.EnemyDamageResolved -= OnDamage;
        // Une run abandonnée dans les premières secondes n'apprend rien. Le joueur, ajouté avant, sort de l'arbre après.
        if (_trace.Seconds >= 30.0 && IsInstanceValid(_player))
        {
            string stem = DevelopmentMode.GetSavePath(
                $"traces/{DateTime.Now:yyyyMMdd-HHmmss}-{_gameManager.SelectedCharacterId}");
            int level = _progression.CurrentLevel;
            Write($"{stem}.csv", _trace.ToCsv());
            Write($"{stem}.txt", _trace.Summary(_gameManager.SelectedCharacterId, _gameManager.EffectiveSeed, level) + "\n");
            Write($"{stem}-build.txt", BuildReport());
            GD.Print($"[PlayTraceRecorder] Trace écrite : {ProjectSettings.GlobalizePath(stem)}.csv");
        }
        _trace = null;
    }

    private string BuildReport()
    {
        StringBuilder text = new();
        foreach (WeaponInstance weapon in _player.WeaponSlots)
            text.Append(CultureInfo.InvariantCulture, $"arme {weapon.Id} niveau={weapon.Level} ascension={weapon.Ascension?.Id ?? "-"}\n");
        foreach (ActivePassiveSouvenir badge in _player.BadgeSlots)
            text.Append(CultureInfo.InvariantCulture, $"ecusson {badge.Id} niveau={badge.Level}\n");
        foreach (ActivePassiveSouvenir passive in _player.PassiveSlots)
            text.Append(CultureInfo.InvariantCulture, $"objet {passive.Id} copies={passive.Copies}\n");
        foreach (PerkSpecializationData perk in _player.Specializations)
            text.Append(CultureInfo.InvariantCulture, $"reminiscence {perk.Id}\n");
        double total = _damage.Values.Sum();
        foreach (KeyValuePair<(string Weapon, DamageKind Kind), double> entry in _damage.OrderByDescending(entry => entry.Value))
            text.Append(CultureInfo.InvariantCulture,
                $"degats {entry.Key.Weapon} {entry.Key.Kind} {entry.Value:F0} ({100.0 * entry.Value / total:F1} %)\n");
        return text.ToString();
    }

    private static void Write(string path, string text)
    {
        using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        if (file == null)
            GD.PushWarning($"[PlayTraceRecorder] Trace non écrite : {path} ({FileAccess.GetOpenError()})");
        else
            file.StoreString(text);
    }
}
#endif
