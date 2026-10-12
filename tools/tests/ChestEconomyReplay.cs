using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using Godot;
using Vestiges.Core;
using Vestiges.Infrastructure;
using Vestiges.Progression;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// Rejoue des revenus mesurés, sans en fabriquer : même calendrier de coffres avant/après, vrais tirages et paiements.
/// Le déplacement, le combat, le temps de lecture et les autres dépenses ne sont pas simulés par ce banc économique.
/// </summary>
public partial class ChestEconomyReplay : Node
{
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    private readonly StringBuilder _items = new("second,source,id,rarity,destination\n");
    private readonly StringBuilder _attempts = new("second,chest_index,source,quoted_cost,essence_before,essence_after,opened\n");
    private int _time, _received, _ground, _firstItem = -1, _firstHigh = -1;
    private string _source;

    public override async void _Ready()
    {
        try
        {
            string[] args = OS.GetCmdlineUserArgs();
            string input = Argument(args, "--income", "");
            string output = Argument(args, "--output", "/tmp/vestiges-economy-replay");
            ulong seed = ulong.Parse(Argument(args, "--seed", "42"), CultureInfo.InvariantCulture);
            DirAccess.MakeDirRecursiveAbsolute(output);
            RunRandom.Begin(seed);
            GD.Seed(seed);
            Dictionary<int, int> income = new();
            string[] rows = FileAccess.GetFileAsString(input).Split('\n', StringSplitOptions.RemoveEmptyEntries);
            string[] columns = rows[0].Split(',');
            int timeColumn = Array.IndexOf(columns, "t"), incomeColumn = Array.IndexOf(columns, "essence_gained");
            if (timeColumn < 0 || incomeColumn < 0) throw new InvalidOperationException("CSV de revenus incompatible");
            int end = 0;
            foreach (string row in rows.AsSpan(1))
            {
                string[] values = row.Split(',');
                int second = (int)float.Parse(values[timeColumn], CultureInfo.InvariantCulture);
                income[second] = (int)float.Parse(values[incomeColumn], CultureInfo.InvariantCulture);
                end = Math.Max(end, second);
            }
            Player player = GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate<Player>(); AddChild(player);
            player.InitializeCharacter(CharacterDataLoader.Get("traqueur"));
            player.SetPhysicsProcess(false);
            player.IsGodMode = true;
            EssenceTracker essence = new(); AddChild(essence);
            WorldInteraction interaction = new(); AddChild(interaction); interaction.Setup(player);
            // Cette réflexion permet d’exécuter exactement le même banc sur le snapshot gratuit d’avant I10.
            typeof(WorldInteraction).GetField("_essence", Private)?.SetValue(interaction, essence);
            MethodInfo open = typeof(WorldInteraction).GetMethod("OpenChest", Private);
            FieldInfo elapsed = typeof(WorldInteraction).GetField("_elapsed", Private);
            PropertyInfo quoted = typeof(Chest).GetProperty("QuotedCost");
            EventBus bus = GetNode<EventBus>("/root/EventBus");
            void Received(string type, string id, int amount)
            {
                if (type != "object") return;
                for (int i = 0; i < amount; i++) RecordItem(id, "bag");
            }
            void Dropped(string id, Vector2 position) => RecordItem(id, "ground");
            bus.LootReceived += Received;
            bus.ItemDropRequested += Dropped;
            string[] pattern = { "common", "common", "rare", "common", "lore", "common", "rare", "common", "epic", "rare" };
            List<Chest> chests = new();
            int credited = 0, spent = 0, failedAttempts = 0, firstOpened = -1;
            int firstTwenty = -1;
            for (_time = 1; _time <= end; _time++)
            {
                if (income.TryGetValue(_time, out int total))
                {
                    essence.AddEssence(Math.Max(0, total - credited));
                    credited = total;
                    if (firstTwenty < 0 && credited >= 20) firstTwenty = _time;
                }
                elapsed?.SetValue(interaction, (float)_time);
                // Premier commun à 30 s ; une rencontre toutes les 90 s, maximum vingt sur trente minutes.
                if (_time >= 30 && (_time - 30) % 90 == 0)
                {
                    Chest chest = new(); AddChild(chest);
                    chest.Initialize(ChestDataLoader.Get("chest_" + pattern[chests.Count % pattern.Length]));
                    chests.Add(chest);
                }
                // Revisite les coffres encore fermés toutes les dix secondes, du plus ancien au plus récent.
                if (_time % 10 == 0)
                    for (int index = 0; index < chests.Count; index++)
                    {
                        Chest chest = chests[index];
                        if (chest.IsOpened) continue;
                        _source = chest.ChestId;
                        int before = essence.CurrentEssence;
                        open.Invoke(interaction, new object[] { chest });
                        int after = essence.CurrentEssence;
                        spent += before - after;
                        if (!chest.IsOpened) failedAttempts++;
                        else if (firstOpened < 0) firstOpened = _time;
                        _attempts.Append(CultureInfo.InvariantCulture,
                            $"{_time},{index},{chest.ChestId},{quoted?.GetValue(chest) ?? 0},{before},{after},{(chest.IsOpened ? 1 : 0)}\n");
                    }
                // Le moteur traite les nœuds et signaux, sans simuler artificiellement une seconde de combat.
                if (_time % 30 == 0) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            int opened = 0;
            foreach (Chest chest in chests) if (chest.IsOpened) opened++;
            bus.LootReceived -= Received; bus.ItemDropRequested -= Dropped;
            using (FileAccess file = FileAccess.Open(output + "/items.csv", FileAccess.ModeFlags.Write)) file.StoreString(_items.ToString());
            using (FileAccess file = FileAccess.Open(output + "/attempts.csv", FileAccess.ModeFlags.Write)) file.StoreString(_attempts.ToString());
            GD.Print($"[ChestEconomyReplay] RESULT seed={seed} seconds={end} income={credited} spent={spent} balance={essence.CurrentEssence} offered={chests.Count} opened={opened} left_closed={chests.Count - opened} failed_attempts={failedAttempts} bag_received={_received} ground={_ground} first_twenty_s={firstTwenty} first_open_s={firstOpened} first_item_s={_firstItem} first_high_s={_firstHigh}");
            GetTree().Quit();
        }
        catch (Exception exception)
        {
            GD.PushError($"[ChestEconomyReplay] FAIL {exception}");
            GetTree().Quit(1);
        }
    }

    private void RecordItem(string id, string destination)
    {
        PassiveSouvenirData item = PassiveSouvenirDataLoader.Get(id);
        string rarity = item?.GetType().GetField("Rarity")?.GetValue(item) as string ?? "unrated";
        if (destination == "bag") _received++; else _ground++;
        if (_firstItem < 0) _firstItem = _time;
        if (_firstHigh < 0 && rarity is "epic" or "legendary") _firstHigh = _time;
        _items.Append(CultureInfo.InvariantCulture, $"{_time},{_source},{id},{rarity},{destination}\n");
    }
    private static string Argument(string[] args, string name, string fallback)
    {
        int index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : fallback;
    }
}
