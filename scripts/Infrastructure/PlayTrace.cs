using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Vestiges.Core;

namespace Vestiges.Infrastructure;

/// <summary>
/// Trace de survie d'une run (plan 42 M0e) : PV perdus par minute et par source, coups encaissés, coups fatals et dashs.
/// Un coup fatal compte même si le joueur s'en relève (ticket de consigne, bot de mesure qui ne meurt pas) : c'est la
/// seule mesure commune au joueur et au bot.
/// Le même compte sert aux parties de Raphaël (<see cref="PlayTraceRecorder"/>, builds d'édition) et au bot de
/// mesure : c'est ce qui permet de comparer un profil du bot à un vrai joueur.
/// </summary>
public sealed class PlayTrace : System.IDisposable
{
    private readonly EventBus _bus;
    private readonly Player _player;
    private readonly Dictionary<(int Minute, string Source), (double HpLost, int Hits)> _lost = new();
    private string _pendingSource;
    private ulong _pendingFrame;
    private double _time;

    public int FatalHits { get; private set; }
    public double Seconds => _time;

    public PlayTrace(EventBus bus, Player player)
    {
        _bus = bus;
        _player = player;
        _bus.PlayerHitBy += OnHitBy;
        _bus.PlayerDamageResolved += OnDamage;
    }

    /// <summary>Temps de jeu écoulé, pauses exclues.</summary>
    public void Advance(double delta) => _time += delta;

    // L'annonce du coup précède sa résolution dans la même image : elle en donne la source. Un coup annoncé puis rejeté
    // (invulnérabilité, dash) ne publie pas de résolution : son annonce ne vaut que pour son image.
    private void OnHitBy(string source, float damage)
    {
        _pendingSource = source;
        _pendingFrame = Godot.Engine.GetPhysicsFrames();
    }

    private void OnDamage(PlayerDamageResult result)
    {
        if (result.PlayerId != _player.GetInstanceId())
            return;
        bool announced = _pendingSource != null && _pendingFrame == Godot.Engine.GetPhysicsFrames();
        string source = result.Kind == PlayerDamageKind.Erasure ? "erasure" : announced ? _pendingSource : "unknown";
        _pendingSource = null;
        if (result.Fatal)
            FatalHits++;
        if (result.HpLost <= 0f)
            return;
        (int, string) key = ((int)(_time / 60.0), source);
        (double hpLost, int hits) = _lost.GetValueOrDefault(key);
        _lost[key] = (hpLost + result.HpLost, hits + 1);
    }

    /// <summary>Une ligne par minute et par source : minute,source,hp_lost,hits.</summary>
    public string ToCsv()
    {
        StringBuilder csv = new("minute,source,hp_lost,hits\n");
        foreach (KeyValuePair<(int Minute, string Source), (double HpLost, int Hits)> entry in _lost)
            csv.Append(CultureInfo.InvariantCulture, $"{entry.Key.Minute},{entry.Key.Source},{entry.Value.HpLost:F1},{entry.Value.Hits}\n");
        return csv.ToString();
    }

    /// <summary>Résumé d'une ligne : durée, coups fatals, dashs, niveau et personnage.</summary>
    public string Summary(string characterId, ulong seed, int level) => string.Create(CultureInfo.InvariantCulture,
        $"character={characterId} seed={seed} seconds={_time:F0} fatal_hits={FatalHits} dashes={_player.Mobility.DashesStarted} level={level}");

    public void Dispose()
    {
        _bus.PlayerHitBy -= OnHitBy;
        _bus.PlayerDamageResolved -= OnDamage;
    }
}
