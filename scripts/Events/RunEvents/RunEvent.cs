using Godot;
using Vestiges.Infrastructure;

namespace Vestiges.Events.RunEvents;

/// <summary>
/// Micro-événement : une opportunité optionnelle, courte, lisible en une seconde.
/// Le directeur l'avance chaque frame et relaie son objectif, sa progression et sa cible au HUD.
/// </summary>
public abstract class RunEvent
{
    protected RunEventContext Context { get; private set; }
    protected RunEventData Data { get; private set; }

    /// <summary>Jeton porté par les créatures de l'événement (signal EventEnemyKilled).</summary>
    public int Token { get; private set; }
    public float TimeRemaining { get; protected set; }
    public string Objective { get; protected set; } = "";
    public float Progress { get; protected set; }
    public Vector2 Target { get; protected set; }
    public bool HasTarget { get; protected set; }
    public bool IsFinished { get; private set; }
    public bool Succeeded { get; private set; }
    public string Summary { get; private set; } = "";

    public string Title => RunEventContext.Tr(Data.TitleKey);

    public void Begin(RunEventContext context, RunEventData data, int token)
    {
        Context = context;
        Data = data;
        Token = token;
        TimeRemaining = data.DurationSec;
        OnStart();
    }

    public void Tick(float delta)
    {
        if (IsFinished)
            return;
        TimeRemaining -= delta;
        OnTick(delta);
        if (!IsFinished && TimeRemaining <= 0f)
            OnTimeout();
    }

    public virtual void OnEnemyKilled(Vector2 position) { }

    /// <summary>Retire les nœuds de l'événement ; appelé à la fin normale comme à l'arrêt de la run.</summary>
    public virtual void Cleanup() { }

    protected abstract void OnStart();
    protected abstract void OnTick(float delta);
    protected abstract void OnTimeout();

    protected void Succeed(string summary)
    {
        IsFinished = true;
        Succeeded = true;
        Summary = summary;
    }

    protected void Fail(string summary)
    {
        IsFinished = true;
        Succeeded = false;
        Summary = summary;
    }

    protected static string Tr(string key) => RunEventContext.Tr(key);

    /// <summary>« +4 Essence · Coffre · Soins » : la récompense annoncée telle que reçue.</summary>
    protected static string RewardLine(int essence, bool chest, bool heal, bool knowledge)
    {
        System.Collections.Generic.List<string> parts = new();
        if (knowledge)
            parts.Add(Tr("EVENT_REWARD_XP"));
        if (essence > 0)
            parts.Add(string.Format(Tr("EVENT_REWARD_ESSENCE"), essence));
        if (chest)
            parts.Add(Tr("EVENT_REWARD_CHEST"));
        if (heal)
            parts.Add(Tr("EVENT_REWARD_HEAL"));
        return string.Join(" · ", parts);
    }
}
