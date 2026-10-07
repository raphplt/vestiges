using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Vestiges.Combat;
using Vestiges.Combat.Abilities;
using Vestiges.Core;
using Vestiges.Infrastructure;

namespace Vestiges.Tests;

/// <summary>
/// Plan 27 V4 : un Instable blessé montre au sol le rayon de sa détonation, l'efface à sa mort, et son explosion est la
/// cause de mort retenue.
/// </summary>
public partial class EnemyAbilityRegression
{
    private async Task RunExplosionWarningChecks()
    {
        CombatPools pools = new() { Name = "CombatPools" };
        AddChild(pools);
        Enemy unstable = await SpawnReady("rodeur", new Vector2(40f, 0f));
        unstable.ApplyAffix(EnemyVariantDataLoader.GetAffix("explosive"));
        FieldInfo hp = typeof(Enemy).GetField("_currentHp", BindingFlags.NonPublic | BindingFlags.Instance);
        await Step(2);
        GroundTelegraph warning = unstable.GetNodeOrNull<GroundTelegraph>("ExplosionWarning");
        Check(warning == null || !warning.Visible, "Instable intact : pas d'anneau");

        hp.SetValue(unstable, unstable.MaxHp * 0.2f);
        await Step(2);
        warning = unstable.GetNodeOrNull<GroundTelegraph>("ExplosionWarning");
        Check(warning is { Visible: true }, "Instable blessé : rayon de détonation affiché au sol");

        string lastHitBy = null;
        void OnHit(string id, float damage) => lastHitBy = id;
        EventBus eventBus = GetNode<EventBus>("/root/EventBus");
        eventBus.PlayerHitBy += OnHit;
        await WaitHurtRecovery();
        // L'explosion touche le joueur : à pleine vie, elle ne doit pas finir le banc.
        _player.Heal(_player.EffectiveMaxHp);
        unstable.TakeDamage(1000000f);
        eventBus.PlayerHitBy -= OnHit;
        Check(!warning.Visible && lastHitBy == unstable.EnemyId && _player.CurrentHp > 0f,
            $"Instable tué : anneau effacé, explosion retenue comme cause ({lastHitBy}, joueur à {_player.CurrentHp:0} PV)");
        Despawn(unstable);
        await WaitHurtRecovery();
        pools.QueueFree();

        // Un événement qui tue s'affiche à l'écran de mort sous son nom, pas sous sa clé.
        if (RunEventDataLoader.Events.Count == 0)
            RunEventDataLoader.Load();
        RunEventData shardRain = null;
        foreach (RunEventData runEvent in RunEventDataLoader.Events)
            if (runEvent.Id == "shard_rain")
                shardRain = runEvent;
        Control card = shardRain == null ? null
            : UI.RunSummaryPanels.KillerCard(Events.RunEvents.RunEventContext.DeathCausePrefix + shardRain.Id, 60f);
        string title = shardRain == null ? "" : TranslationServer.Translate(shardRain.TitleKey);
        Check(card != null && HasLabel(card, title) && !HasLabel(card, "event:shard_rain"),
            $"Écran de mort : un événement est nommé ({title})");
        card?.QueueFree();
    }

    private static bool HasLabel(Node root, string text)
    {
        if (root is Label label && label.Text == text)
            return true;
        foreach (Node child in root.GetChildren())
            if (HasLabel(child, text))
                return true;
        return false;
    }
}
