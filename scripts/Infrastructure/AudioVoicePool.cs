using Godot;

namespace Vestiges.Infrastructure;

/// <summary>
/// Voix préallouées : un son ordinaire ne peut interrompre une alerte, et les répétitions d'un même
/// son ne s'empilent pas indéfiniment. Les fondus appartiennent à la voix et cessent à sa réutilisation.
/// </summary>
internal sealed class AudioVoicePool
{
    private sealed class Voice
    {
        public AudioStreamPlayer Player;
        public string Key = "";
        public int Priority;
        public ulong StartedAt;
        public Tween Fade;
    }

    private readonly Voice[] _voices;

    internal AudioVoicePool(Node owner, string prefix, int size, Node.ProcessModeEnum processMode)
    {
        _voices = new Voice[size];
        for (int i = 0; i < size; i++)
        {
            AudioStreamPlayer player = new() { Name = $"{prefix}{i}", Bus = "SFX", ProcessMode = processMode };
            owner.AddChild(player);
            _voices[i] = new Voice { Player = player };
        }
    }

    internal AudioStreamPlayer Acquire(string key, int priority, int maxVoices, out bool stolen)
    {
        stolen = false;
        Voice free = null;
        Voice victim = null;
        int matching = 0;
        foreach (Voice voice in _voices)
        {
            if (!voice.Player.Playing)
            {
                free ??= voice;
                continue;
            }
            if (voice.Key == key)
                matching++;
            // À priorité égale, laisser le son finir plutôt que tronquer sans cesse ses premières syllabes.
            if (voice.Priority < priority && (victim == null || voice.Priority < victim.Priority
                || (voice.Priority == victim.Priority && voice.StartedAt < victim.StartedAt)))
                victim = voice;
        }
        if (matching >= maxVoices)
            return null;
        Voice selected = free ?? victim;
        if (selected == null)
            return null;
        stolen = selected.Player.Playing;
        selected.Fade?.Kill();
        selected.Fade = null;
        selected.Player.Stop();
        selected.Key = key;
        selected.Priority = priority;
        selected.StartedAt = AudioManager.NowMsec;
        return selected.Player;
    }

    internal void FadeOut(AudioStreamPlayer player, string key, float duration)
    {
        foreach (Voice voice in _voices)
        {
            if (voice.Player != player || voice.Key != key || !player.Playing)
                continue;
            voice.Fade?.Kill();
            voice.Fade = player.CreateTween().SetIgnoreTimeScale();
            voice.Fade.SetPauseMode(Tween.TweenPauseMode.Process);
            voice.Fade.TweenProperty(player, "volume_db", -60f, Mathf.Max(0.01f, duration));
            voice.Fade.TweenCallback(Callable.From(player.Stop));
            return;
        }
    }

    internal void Stop()
    {
        foreach (Voice voice in _voices)
        {
            voice.Fade?.Kill();
            voice.Fade = null;
            voice.Player.Stop();
        }
    }
}
