using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using Vestiges.Combat;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;
using Vestiges.UI;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>Les quinze nouveaux déblocages : faits de jeu, seuils, persistance et accès aux vraies réserves.</summary>
public partial class QuestRegression
{
    private static readonly string[] ItemQuestIds =
    {
        "item_small_fortunes", "item_make_room", "item_hold_the_thread", "item_same_tune",
        "item_keep_in_touch", "item_find_north", "item_lost_and_found", "item_straight_through",
        "item_returning_colour", "item_clear_splinters", "item_chain_reaction", "item_open_every_lock",
        "item_crossed_signatures", "item_stay_together", "item_one_less_trace",
    };

    private void CheckObjectQuestsV2()
    {
        FieldInfo dataField = typeof(MetaSaveManager).GetField("_data", BindingFlags.Static | BindingFlags.NonPublic)!;
        object previous = dataField.GetValue(null);
        try
        {
            ResetItemProfile(dataField);
            Check(!DevelopmentMode.IsEnabled && ItemLootPool.For(_player).Eligible("chest_common").Count == 26,
                "profil normal neuf : 26 objets dans les coffres, aucun déblocage dev");
            Check(ItemLootPool.For(_player).Eligible("workshop").Count == 26, "Atelier neuf : les mêmes 26 objets");
            foreach (string id in ItemQuestIds)
            {
                ResetItemProfile(dataField);
                QuestDefinition quest = QuestDataLoader.Get(id);
                string item = quest.Unlocks[0].Id;
                Check(!QuestBook.IsDormant(quest) && QuestBook.UnlockName(quest.Unlocks[0]) == PassiveSouvenirDataLoader.Get(item).Name
                    && QuestBook.UnlockIcon(quest.Unlocks[0]) == PassiveSouvenirDataLoader.Get(item).Icon
                    && QuestBook.LockText(UnlockKind.Object, item).Contains(quest.Name), $"{id} : nom, icône et verrou reliés à l'objet");
                CheckItemAccess(item, false);
                StartRun();
                CheckWrongItemFacts(quest);
                if (quest.Scope == QuestScope.Cumulative)
                    DriveCumulativeItem(quest);
                else
                    DriveRunItem(quest);
                Check(MetaSaveManager.HasCompletedQuest(id) && Count(id) == 1, $"{id} : seuil atteint, déblocage et notification uniques");
                CheckItemAccess(item, true);
                EndRun();
                MetaSaveManager.ReloadProfile();
                Check(MetaSaveManager.HasCompletedQuest(id) && MetaSaveManager.IsObjectUnlocked(item), $"{id} : déblocage conservé après relecture");
                StartRun();
                ItemFact(quest.Conditions[0], 1);
                Check(Count(id) == 1, $"{id} : un nouveau fait ne notifie pas une deuxième fois");
                EndRun();
            }
            ResetItemProfile(dataField);
            CheckRunItemSeparation();
            ResetItemProfile(dataField);
            CheckSingleRunItemCounter();
            ResetItemProfile(dataField);
            // Les scénarios précédents prouvent les conditions ; cette fixture vérifie la réserve complète.
            foreach (QuestDefinition quest in QuestDataLoader.GetAll())
                if (quest.Unlocks.Any(unlock => unlock.Kind == UnlockKind.Object))
                    MetaSaveManager.ClaimQuest(quest, out _);
            MetaSaveManager.ReloadProfile();
            Check(ItemLootPool.For(_player).Eligible("chest_common").Count == 55
                && ItemLootPool.For(_player).Eligible("workshop").Count == 55,
                "29 quêtes d'objets accomplies : les 55 objets entrent dans les coffres et l'Atelier");
            CheckItemToasts();
        }
        finally
        {
            dataField.SetValue(null, previous);
            MetaSaveManager.Save();
        }
    }

    private void ResetItemProfile(FieldInfo dataField)
    {
        dataField.SetValue(null, new MetaSaveData());
        MetaSaveManager.Save();
        _signalled.Clear();
        while (_player.WeaponSlots.Count > 0)
            _player.RemoveWeapon(0);
    }

    private void CheckItemAccess(string item, bool available)
    {
        ItemLootPool pool = ItemLootPool.For(_player);
        Check(MetaSaveManager.IsObjectUnlocked(item) == available
            && pool.Eligible("chest_common").Any(candidate => candidate.Id == item) == available
            && pool.Eligible("workshop").Any(candidate => candidate.Id == item) == available,
            $"{item} : accès méta, coffre et Atelier {(available ? "ouvert" : "fermé")}");
    }

    private void DriveCumulativeItem(QuestDefinition quest)
    {
        QuestCondition condition = quest.Conditions[0];
        int half = (int)condition.Target / 2;
        ItemFact(condition, half);
        MethodInfo flush = typeof(QuestTracker).GetMethod("Flush", Private)!;
        flush.Invoke(_tracker, null);
        flush.Invoke(_tracker, null);
        EndRun();
        MetaSaveManager.ReloadProfile();
        Check(Near(MetaSaveManager.GetQuestProgress(quest.Id), half), $"{quest.Id} : double flush et sortie ne doublent pas le cumul sauvegardé");
        StartRun();
        ItemFact(condition, (int)condition.Target - half - 1);
        Check(!MetaSaveManager.HasCompletedQuest(quest.Id) && Count(quest.Id) == 0,
            $"{quest.Id} : deux runs, cible moins un, toujours verrouillé");
        if (quest.Id == "item_open_every_lock")
        {
            Check(condition.Target == 50 && QuestBook.ProgressText(quest, new[] { 49f }).Contains("49 / 50"),
                "Pied-de-biche : contrat et interface à 49 / 50 coffres cumulés");
            PurchaseLastQuestChest(quest);
        }
        else
            ItemFact(condition, 1);
    }

    private void DriveRunItem(QuestDefinition quest)
    {
        for (int i = 0; i < quest.Conditions.Count; i++)
        {
            QuestCondition condition = quest.Conditions[i];
            if (condition.Stat == QuestStat.DistanceMeters)
                ItemFact(condition, (int)condition.Target - 1);
            else
            {
                if (condition.Stat == QuestStat.KillBurst)
                    AdvanceRun(10f);
                ItemFact(condition, (int)condition.Target - 1);
            }
            float progress = _tracker.RunProgress(quest.Id)[i];
            Check(Mathf.IsEqualApprox(progress, condition.Target - 1)
                && !MetaSaveManager.HasCompletedQuest(quest.Id), $"{quest.Id} : {condition.Stat} exactement sous le seuil ({progress})");
            if (condition.Stat == QuestStat.DistanceMeters)
                ItemFact(condition, (int)condition.Target);
            else
                ItemFact(condition, IsMaximum(condition.Stat) && condition.Stat != QuestStat.KillBurst ? (int)condition.Target : 1);
            if (i < quest.Conditions.Count - 1)
                Check(!MetaSaveManager.HasCompletedQuest(quest.Id), $"{quest.Id} : les autres conditions de cette run restent nécessaires");
        }
    }

    private void ItemFact(QuestCondition condition, int count)
    {
        if (condition.Stat == QuestStat.WeaponsAtLevel)
        { WeaponsAtLevel(count, (int)condition.Param); return; }
        if (condition.Stat == QuestStat.Peril)
        { _bus.EmitSignal(EventBus.SignalName.PerilChanged, count); return; }
        if (condition.Stat == QuestStat.DistanceMeters)
        {
            RunJourney journey = _run.GetNode<RunTracker>("RunTracker").Journey;
            if (journey.TravelledMeters == 0f) journey.Track(Vector2.Zero);
            journey.Track(new Vector2(count * RunSummaryConfig.Load().PixelsPerMeter, 0f));
            AdvanceRun(0.5f);
            return;
        }
        for (int i = 0; i < count; i++)
        {
            if (condition.Stat != QuestStat.KillBurst)
                AdvanceRun(1f);
            switch (condition.Stat)
            {
                case QuestStat.HpHealed:
                    typeof(Player).GetField("_currentHp", Private)!.SetValue(_player, _player.EffectiveMaxHp - 1f);
                    _player.Heal(10f);
                    break;
                case QuestStat.CleanCrises: Crisis(0f); break;
                case QuestStat.CloseKills: Kill(position: new Vector2(condition.Param - 1f, 0f), advance: false); break;
                case QuestStat.KillBurst: Kill(advance: false); break;
                default: Step(condition, 1); break;
            }
        }
    }

    private void CheckWrongItemFacts(QuestDefinition quest)
    {
        foreach (QuestCondition condition in quest.Conditions)
        {
            switch (condition.Stat)
            {
                case QuestStat.WeaponKills:
                    Kill(weapon: "whip");
                    WeaponInstance weapon = new(WeaponDataLoader.Get(condition.Weapon));
                    AttackContext source = new(_player.GetInstanceId(), weapon, 1, DamageKind.Passive);
                    EnemyLife life = new(++_life, 1);
                    DamageResult passive = new(life, source, 10f, 10f, 0f, 10f, true, true);
                    _bus.PublishEnemyKill(new EnemyKillResult(life, "crawler", new Vector2(400f, 0f), passive, default, default, default, false, false));
                    break;
                case QuestStat.MeleeKills:
                case QuestStat.RangedKills: Kill(); break;
                case QuestStat.CloseKills: Kill(position: new Vector2(0f, condition.Param - 1f)); break;
                case QuestStat.SlowedKills: Kill(); break;
                case QuestStat.CritKills:
                    Kill();
                    DamageResult hit = new DamageResult(new EnemyLife(++_life, 1), default, 10f, 10f, 0f, 10f, false, true) { Critical = true };
                    _bus.PublishEnemyDamage(hit);
                    break;
                case QuestStat.HpHealed:
                    SetHp(1f);
                    _player.Heal(1000f);
                    _bus.PublishPlayerHealing(new HealingResult(_player.GetInstanceId(), HealingKind.Normal, 1000f, 0f, 1000f, true));
                    break;
                case QuestStat.CrisesSurvived:
                    _bus.EmitSignal(EventBus.SignalName.CrisisEnded, 1);
                    break;
                case QuestStat.CleanCrises:
                    Crisis(_player.EffectiveMaxHp * condition.Param);
                    break;
                case QuestStat.WeaponsAtLevel:
                    WeaponsAtLevel((int)condition.Target, (int)condition.Param - 1);
                    break;
            }
        }
        // Les éliminations volontairement invalides peuvent contribuer à une autre condition (rafale) ;
        // on ne contrôle ici que les faits filtrés ci-dessus, puis on expire leur fenêtre.
        foreach (QuestCondition condition in quest.Conditions)
            if (condition.Stat is QuestStat.WeaponKills or QuestStat.CloseKills or QuestStat.SlowedKills or QuestStat.CritKills
                or QuestStat.HpHealed or QuestStat.CrisesSurvived or QuestStat.CleanCrises or QuestStat.WeaponsAtLevel
                or QuestStat.MeleeKills or QuestStat.RangedKills)
                Check(_tracker.RunProgress(quest.Id)[quest.Conditions.IndexOf(condition)] == 0,
                    $"{quest.Id} : mauvais faits ignorés pour {condition.Stat}");
        AdvanceRun(10f);
    }

    private void PurchaseLastQuestChest(QuestDefinition quest)
    {
        Chest chest = new();
        _run.AddChild(chest);
        chest.Initialize(ChestDataLoader.Get("chest_common"));
        EssenceTracker essence = new();
        _run.AddChild(essence);
        using RandomNumberGenerator rng = new() { Seed = 913UL };
        Check(!chest.TryPurchase(_player, essence, 0, rng, out _) && !MetaSaveManager.HasCompletedQuest(quest.Id),
            "49 coffres : un achat refusé faute d'Essence ne compte pas");
        essence.AddEssence(chest.QuotedCost);
        Check(chest.TryPurchase(_player, essence, 0, rng, out _) && essence.CurrentEssence == 0
            && MetaSaveManager.HasCompletedQuest(quest.Id), "50e coffre réellement payé : Pied-de-biche débloqué immédiatement");
        Check(!chest.TryPurchase(_player, essence, 0, rng, out _) && Count(quest.Id) == 1,
            "la seconde interaction du coffre ne compte pas et ne renotifie pas");
    }

    private void CheckRunItemSeparation()
    {
        const string id = "item_keep_in_touch";
        StartRun();
        WeaponsAtLevel(2, 6);
        EndRun();
        MetaSaveManager.ReloadProfile();
        while (_player.WeaponSlots.Count > 0) _player.RemoveWeapon(0);
        StartRun();
        Crisis(CostlyCrisis);
        Check(!MetaSaveManager.HasCompletedQuest(id) && Near(_tracker.RunProgress(id), 0f, 1f),
            "run 1 : deux armes niveau 6 ; run 2 : Résurgence ; les conditions ne se combinent pas");
        WeaponsAtLevel(2, 6);
        Check(MetaSaveManager.HasCompletedQuest(id), "les deux conditions dans la même run débloquent Cordon spiralé");
        EndRun();
    }

    private void CheckSingleRunItemCounter()
    {
        QuestDefinition quest = QuestDataLoader.Get("item_straight_through");
        StartRun();
        ItemFact(quest.Conditions[0], 179);
        EndRun();
        MetaSaveManager.ReloadProfile();
        StartRun();
        ItemFact(quest.Conditions[0], 1);
        Check(!MetaSaveManager.HasCompletedQuest(quest.Id) && Near(_tracker.RunProgress(quest.Id), 1f),
            "Peigne : 179 éliminations puis une dans une autre run ne font pas 180");
        EndRun();
    }

    private void CheckItemToasts()
    {
        foreach (string id in new[] { "item_small_fortunes", "item_open_every_lock", "item_one_less_trace" })
        {
            QuestToast toast = new();
            AddChild(toast);
            _bus.EmitSignal(EventBus.SignalName.QuestCompleted, id);
            QuestDefinition quest = QuestDataLoader.Get(id);
            Check(toast.ShownText.Contains(quest.Name) && toast.ShownText.Contains(QuestBook.UnlockName(quest.Unlocks[0])),
                $"{id} : bandeau avec le nom de la quête et de l'objet");
            RemoveChild(toast);
            toast.QueueFree();
        }
    }
}
