using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Vestiges.Infrastructure;
using Vestiges.Progression;
using Vestiges.UI;
using Vestiges.World;

namespace Vestiges.Tests;

/// <summary>
/// --capture-oublis : prend les neuf Oublis de carte d'un coup (plan 17 lot 3D) et mesure chaque système avant et
/// après : élites, affixes, perception, Effacement, Résurgence, brouillard, colonnes et flèches des coffres, rareté des
/// coffres, Mémoriaux effondrés. Captures avant et après, ligne RESULT.
/// </summary>
public partial class RunObservation
{
    private const BindingFlags PrivateField = BindingFlags.NonPublic | BindingFlags.Instance;

    private async Task CaptureOublis()
    {
        ProcessMode = ProcessModeEnum.Always;
        _player.AIInputOverride = Vector2.Zero;
        await Frames(120);
        SaveFrame("oublis-1-before");

        Node spawn = _world.GetNode("SpawnManager");
        ErasureManager erasure = _world.GetNode<ErasureManager>("ErasureManager");
        Vestiges.Events.CrisisManager crisis = _world.GetNode<Vestiges.Events.CrisisManager>("CrisisManager");
        FogOfWar fog = _world.GetNode<FogOfWar>("FogOfWar");
        string chestsBefore = ChestRarities();
        int dormantBefore = CountMemorials(Memorial.MemorialState.Dormant);
        float crisisBefore = crisis.TimeUntilNextCrisis;
        int fogBefore = (int)typeof(FogOfWar).GetField("_fogRevealRadius", PrivateField).GetValue(fog);
        int columnsBefore = CountVisibleColumns();

        PerilManager peril = _world.GetNode<PerilManager>("PerilManager");
        foreach (OubliData oubli in OubliDataLoader.All)
            peril.AddOubli(oubli);
        await Frames(60);
        SaveFrame("oublis-2-after");

        float decay = (float)typeof(ErasureManager).GetField("_decayMultiplier", PrivateField).GetValue(erasure);
        int extraElites = (int)spawn.GetType().GetField("_extraElites", PrivateField).GetValue(spawn);
        float affix = (float)spawn.GetType().GetField("_affixChanceBonus", PrivateField).GetValue(spawn);
        float detection = (float)typeof(Vestiges.Combat.EnemyTracking).GetField("_detectionScaleSq", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
        int fogAfter = (int)typeof(FogOfWar).GetField("_fogRevealRadius", PrivateField).GetValue(fog);
        bool pointersVisible = _world.GetNode("HUD").FindChild("ChestPointers", true, false) is ChestPointers { Visible: true };

        GD.Print(string.Create(CultureInfo.InvariantCulture,
            $"[RunObservation] RESULT oublis count={peril.Oublis.Count} extra_elites={extraElites} affix_bonus={affix:F2} detection_scale={Mathf.Sqrt(detection):F2} decay_mult={decay:F2} crisis_s={crisisBefore:F0}->{crisis.TimeUntilNextCrisis:F0} fog_radius={fogBefore}->{fogAfter} columns={columnsBefore}->{CountVisibleColumns()} pointers_visible={pointersVisible} chests={chestsBefore}->{ChestRarities()} dormant_memorials={dormantBefore}->{CountMemorials(Memorial.MemorialState.Dormant)}"));

        // Coffre posé après les Oublis (butin d'événement) : sa colonne doit naître raccourcie.
        Chest lateChest = GD.Load<PackedScene>("res://scenes/world/Chest.tscn").Instantiate<Chest>();
        _world.AddChild(lateChest);
        lateChest.Initialize(ChestDataLoader.Get("chest_common"));
        float lateSignal = (float)typeof(Chest).GetField("_signalFactor", PrivateField).GetValue(lateChest);
        lateChest.QueueFree();
        GD.Print(string.Create(CultureInfo.InvariantCulture, $"[RunObservation] RESULT oublis late_chest_signal={lateSignal:F2}"));
    }

    private static string ChestRarities()
    {
        Dictionary<string, int> counts = new();
        foreach (Chest chest in Chest.Closed)
            counts[chest.Rarity] = counts.GetValueOrDefault(chest.Rarity) + 1;
        List<string> parts = new();
        foreach (string rarity in new[] { "common", "rare", "epic", "lore" })
            parts.Add($"{rarity[0]}{counts.GetValueOrDefault(rarity)}");
        return string.Join("/", parts);
    }

    private static int CountMemorials(Memorial.MemorialState state)
    {
        int count = 0;
        foreach (Memorial memorial in Memorial.All)
            if (memorial.State == state)
                count++;
        return count;
    }

    private static int CountVisibleColumns()
    {
        int count = 0;
        foreach (Chest chest in Chest.Closed)
            if (chest.GetNodeOrNull<LightColumn>("LightColumn") is { Visible: true } column && column.Scale.Y > 0f)
                count++;
        return count;
    }
}
