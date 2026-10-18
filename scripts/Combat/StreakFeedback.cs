using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Combat;

/// <summary>
/// Retours de la Série autour du joueur (plan 52 S4) : une aura au sol à la couleur du rang, une onde et une secousse au
/// rang gagné, et les sons (rang de plus en plus aigu, verre au coup reçu, encaissement, tic de la jauge presque vide).
/// Enfant du joueur, piloté par lui : <see cref="Present"/> à chaque changement, <see cref="Step"/> à chaque pas. Les
/// ellipses sont cuites au chargement ; le nœud ne redessine qu'avec une aura ou une onde.
/// </summary>
public partial class StreakFeedback : Node2D
{
    private const float WaveSeconds = 0.45f;
    private static readonly int[] WaveRadii = { 18, 26, 34, 42, 50, 58 };
    // Aura : une ellipse pleine un peu plus large à chaque rang, qui respire.
    private const int AuraBaseRadius = 13;
    private const float AuraRadiusPerRank = 1.5f;
    private const float AuraBreathSpeed = 3.2f;

    private StreakFeedbackSettings _settings;
    private PlayerStreak _streak;
    private int _rank;
    private Color _rankColor;
    private float _wave;
    private Color _waveColor;
    private float _tickClock;
    private float _clock;

    public void Setup(PlayerStreak streak)
    {
        _streak = streak;
        _settings = streak.Config.Feedback;
    }

    public override void _Ready()
    {
        ShowBehindParent = true;
        foreach (int radius in WaveRadii)
            PixelEllipseArt.Get(radius, false);
        for (int rank = 1; rank <= (_streak?.RankCount ?? 0); rank++)
            PixelEllipseArt.Get(AuraRadius(rank), true);
        SetProcess(false);
    }

    /// <summary>Un changement de la Série : sons, onde et secousse selon ce qui vient d'arriver.</summary>
    public void Present(StreakChange change)
    {
        if (_settings == null)
            return;
        switch (change.Kind)
        {
            case StreakChangeKind.Gain when change.Rank > change.PreviousRank:
                AudioManager.Play(_settings.RankAudio, 0.01f, 0f, 1f + _settings.RankPitchStep * (change.Rank - 1));
                _wave = WaveSeconds;
                _waveColor = _streak.ColorAt(change.Rank);
                ScreenShake.Instance?.AddTrauma(_settings.RankShake);
                break;
            case StreakChangeKind.Hit:
                AudioManager.Play(_settings.HitAudio, 0.06f, 0f, change.Rank < change.PreviousRank ? 0.8f : 1f);
                break;
            case StreakChangeKind.CashOut when change.Essence > 0:
                AudioManager.Play(_settings.CashOutAudio, 0.02f, 0f, 1f + _settings.RankPitchStep * (change.PreviousRank - 1));
                break;
        }
        _rank = change.Rank;
        _rankColor = _streak.ColorAt(_rank);
        _tickClock = 0f;
        SetProcess(_rank > 0 || _wave > 0f);
        QueueRedraw();
    }

    /// <summary>Le tic de la jauge presque vide, de plus en plus serré, pressé sous la moitié du seuil.</summary>
    public void Step(float delta)
    {
        if (_settings == null || _streak.Count < 2)
            return;
        float fraction = _streak.WindowFraction;
        if (fraction >= _settings.TickFrom)
        {
            _tickClock = 0f;
            return;
        }
        _tickClock -= delta;
        if (_tickClock > 0f)
            return;
        float urgency = 1f - fraction / _settings.TickFrom;
        _tickClock = Mathf.Lerp(_settings.TickSlowSeconds, _settings.TickFastSeconds, urgency);
        AudioManager.Play(urgency > 0.5f ? _settings.TickUrgentAudio : _settings.TickAudio, 0.02f, 0f, 1f + urgency * 0.25f);
    }

    public override void _Process(double delta)
    {
        _clock += (float)delta;
        _wave = Mathf.Max(0f, _wave - (float)delta);
        if (_rank == 0 && _wave <= 0f)
            SetProcess(false);
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_rank > 0 && VfxFactory.CurrentParticleLevel != ParticleLevel.Off)
        {
            int radius = AuraRadius(_rank);
            float breath = 0.75f + 0.25f * Mathf.Sin(_clock * AuraBreathSpeed);
            DrawTexture(PixelEllipseArt.Get(radius, true), PixelEllipseArt.Offset(radius), _rankColor with { A = _settings.AuraAlpha * breath });
        }
        if (_wave > 0f)
        {
            float progress = 1f - _wave / WaveSeconds;
            int radius = WaveRadii[Mathf.Min(WaveRadii.Length - 1, (int)(progress * WaveRadii.Length))];
            DrawTexture(PixelEllipseArt.Get(radius, false), PixelEllipseArt.Offset(radius), _waveColor with { A = 1f - progress * 0.85f });
        }
    }

    private static int AuraRadius(int rank) => AuraBaseRadius + Mathf.RoundToInt(AuraRadiusPerRank * rank);
}
