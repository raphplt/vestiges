using System.Collections.Generic;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.World;

/// <summary>
/// Météo de la run (plan 39 A5), à part des micro-événements : une météo à la fois, tirée selon le biome où se trouve
/// le joueur, suspendue pendant un boss. Annonce chaque changement par <c>WeatherChanged</c> ; l'atmosphère, les
/// particules et le son la jouent.
/// </summary>
public partial class WeatherDirector : Node
{
    // Biome sans météo possible : nouvel essai un peu plus tard.
    private const float RetrySec = 10f;

    private WeatherConfig _config;
    private EventBus _eventBus;
    private GameManager _gameManager;
    private RandomNumberGenerator _rng;
    private WeatherData _active;
    private string _biomeId = "";
    private float _timer;
    private int _bosses;

    public string ActiveId => _active?.Id ?? "";

    public override void _Ready()
    {
        if (!WeatherConfig.TryLoad(out _config, out string error))
        {
            GD.PushError($"[WeatherDirector] {error} : run sans météo");
            SetProcess(false);
            return;
        }
        // Flux à part : la météo ne décale aucun tirage du jeu.
        _rng = RunRandom.Create("weather");
        _eventBus = GetNode<EventBus>("/root/EventBus");
        _gameManager = GetNode<GameManager>("/root/GameManager");
        _timer = _rng.RandfRange(_config.FirstMinS, _config.FirstMaxS);
        _eventBus.PlayerBiomeChanged += OnPlayerBiomeChanged;
        _eventBus.BossEncounterStarted += OnBossEncounterStarted;
        _eventBus.BossEncounterEnded += OnBossEncounterEnded;
    }

    public override void _ExitTree()
    {
        if (_eventBus == null)
            return;
        _eventBus.PlayerBiomeChanged -= OnPlayerBiomeChanged;
        _eventBus.BossEncounterStarted -= OnBossEncounterStarted;
        _eventBus.BossEncounterEnded -= OnBossEncounterEnded;
    }

    public override void _Process(double delta)
    {
        if (_gameManager.CurrentState != GameManager.GameState.Run || _bosses > 0)
            return;
        _timer -= (float)delta;
        if (_timer > 0f)
            return;
        if (_active != null)
        {
            Stop();
            return;
        }
        WeatherData next = Pick();
        if (next == null)
            _timer = RetrySec;
        else
            Begin(next);
    }

    /// <summary>Impose une météo (mode dev, captures) ; vide : ciel dégagé.</summary>
    public bool ForceStart(string weatherId)
    {
        if (_config == null)
            return false;
        if (string.IsNullOrEmpty(weatherId))
        {
            if (_active != null)
                Stop();
            return true;
        }
        WeatherData weather = _config.Find(weatherId);
        if (weather == null)
            return false;
        Begin(weather);
        return true;
    }

    private WeatherData Pick()
    {
        float total = 0f;
        foreach (WeatherData weather in _config.Weathers)
            total += weather.BiomeWeights.GetValueOrDefault(_biomeId);
        if (total <= 0f)
            return null;
        float roll = _rng.Randf() * total;
        foreach (WeatherData weather in _config.Weathers)
        {
            roll -= weather.BiomeWeights.GetValueOrDefault(_biomeId);
            if (roll < 0f)
                return weather;
        }
        return null;
    }

    private void Begin(WeatherData weather)
    {
        _active = weather;
        _timer = _rng.RandfRange(weather.DurationMinS, weather.DurationMaxS);
        GD.Print($"[WeatherDirector] {weather.Id} ({_biomeId}), {_timer:F0} s");
        _eventBus.EmitSignal(EventBus.SignalName.WeatherChanged, weather.Id);
    }

    private void Stop()
    {
        _active = null;
        _timer = _rng.RandfRange(_config.GapMinS, _config.GapMaxS);
        _eventBus.EmitSignal(EventBus.SignalName.WeatherChanged, "");
    }

    private void OnPlayerBiomeChanged(string biomeId) => _biomeId = biomeId;

    /// <summary>Un boss a son propre ciel (l'Indicible, sa nuit) : la météo se lève et attend la fin du combat.</summary>
    private void OnBossEncounterStarted(int encounterId, string bossName, float maxHp, int notches)
    {
        _bosses++;
        if (_active != null)
            Stop();
    }

    private void OnBossEncounterEnded(int encounterId, bool defeated) => _bosses = Mathf.Max(0, _bosses - 1);
}
