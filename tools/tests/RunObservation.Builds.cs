using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;
using Vestiges.Infrastructure;
using Vestiges.Progression;

namespace Vestiges.Tests;

/// <summary>
/// Builds imposés et états de progression des mesures du plan 42 (M0c).
/// --start-build id : build de référence de <c>tools/tests/measure_builds.json</c> (armes au départ, cartes prises
/// d'abord, objets posés dans le sac à la minute dite, armes fermées).
/// --meta neuf|milieu|complet : progression du profil isolé, sans --dev. Milieu : une quête sur deux accomplie, dans
/// l'ordre du catalogue (chaque famille de déblocages à moitié), et la moitié des rangs du Campement ; complet : toutes
/// les quêtes et tout le Campement. Les rangs de puissance brute (PV max, dégâts, cadence, vitesse, régénération,
/// armure) ne sont jamais achetés : ils doivent quitter le Campement (DECISIONS §152).
/// </summary>
public partial class RunObservation
{
    private const string BuildsPath = "res://tools/tests/measure_builds.json";
    private static readonly HashSet<string> RawCampStats = new() { "max_hp", "damage", "attack_speed", "speed", "regen_rate", "armor" };

    private sealed record BuildObject(string Id, double Minute);

    private string _buildId;
    private readonly List<string> _buildWeapons = new();
    private bool _buildWeaponsClosed;
    private readonly List<BuildObject> _buildObjects = new();
    private int _nextBuildObject;

    /// <summary>Lit --start-build ; les cartes du build passent avant celles de --prefer.</summary>
    private void ReadBuild(string[] args)
    {
        _buildId = Argument(args, "--start-build", null);
        if (_buildId == null)
            return;
        using JsonDocument document = JsonDocument.Parse(Godot.FileAccess.GetFileAsString(BuildsPath));
        if (!document.RootElement.GetProperty("builds").TryGetProperty(_buildId, out JsonElement build))
            throw new ArgumentException($"--start-build {_buildId} : build absent de {BuildsPath}");
        foreach (JsonElement weapon in build.GetProperty("armes").EnumerateArray())
            _buildWeapons.Add(weapon.GetString());
        _buildWeaponsClosed = build.GetProperty("armes_fermees").GetBoolean();
        List<string> cards = new();
        foreach (JsonElement card in build.GetProperty("cartes").EnumerateArray())
            cards.Add(card.GetString());
        cards.AddRange(_preferredCards);
        _preferredCards = cards.ToArray();
        foreach (JsonElement entry in build.GetProperty("objets").EnumerateArray())
            _buildObjects.Add(new BuildObject(entry.GetProperty("id").GetString(), entry.GetProperty("minute").GetDouble()));
        _buildObjects.Sort((a, b) => a.Minute.CompareTo(b.Minute));
        foreach (string id in _buildWeapons)
            if (WeaponDataLoader.Get(id) == null)
                throw new ArgumentException($"--start-build {_buildId} : arme inconnue {id}");
        foreach (BuildObject entry in _buildObjects)
            if (PassiveSouvenirDataLoader.Get(entry.Id) == null)
                throw new ArgumentException($"--start-build {_buildId} : objet inconnu {entry.Id}");
        // Les armes du build remplacent celle du personnage.
        while (_player.WeaponSlots.Count > 0)
            _player.RemoveWeapon(0);
        foreach (string id in _buildWeapons)
            if (!_player.AddWeapon(WeaponDataLoader.Get(id)))
                GD.PushWarning($"[RunObservation] build {_buildId} : {id} sans emplacement ({_player.WeaponSlotLimit} armes dans cette run)");
        GD.Print($"[RunObservation] build {_buildId} : {string.Join('+', _buildWeapons)}, {_buildObjects.Count} objets prévus");
    }

    /// <summary>Pose dans le sac les objets du build dont la minute est venue.</summary>
    private void GrantBuildObjects(double time)
    {
        while (_nextBuildObject < _buildObjects.Count && time >= _buildObjects[_nextBuildObject].Minute * 60.0)
        {
            string id = _buildObjects[_nextBuildObject++].Id;
            if (!_player.AddOrUpgradePassive(id))
                GD.PushWarning($"[RunObservation] build {_buildId} : {id} refusé (sac plein ou plafond de copies)");
        }
    }

    /// <summary>Armes fermées : une nouvelle arme hors du build n'est jamais prise.</summary>
    private bool BuildRefuses(FragmentOption option) =>
        _buildWeaponsClosed && option.Type == "weapon_new" && !_buildWeapons.Contains(option.Id);

    /// <summary>--meta : progression du profil isolé, avant le chargement de la run.</summary>
    private static void ApplyMetaState(string state)
    {
        if (state == null || state == "neuf")
            return;
        if (state is not ("milieu" or "complet"))
            throw new ArgumentException($"--meta {state} : neuf, milieu ou complet attendu");
        if (DevelopmentMode.IsEnabled)
            throw new ArgumentException("--meta se mesure sans --dev : le mode dev ouvre tout (MEASURE_PROFILE=normal)");
        bool half = state == "milieu";
        IReadOnlyList<QuestDefinition> quests = QuestDataLoader.GetAll();
        int claimed = 0;
        for (int i = 0; i < quests.Count; i++)
            if ((!half || i % 2 == 0) && MetaSaveManager.ClaimQuest(quests[i], out _))
                claimed++;
        int ranks = 0;
        foreach (CampUpgradeData upgrade in CampUpgradeDataLoader.All)
        {
            if (RawCampStats.Contains(upgrade.Stat ?? ""))
                continue;
            int target = half ? (upgrade.Prices.Length + 1) / 2 : upgrade.Prices.Length;
            for (int rank = MetaSaveManager.GetCampRank(upgrade.Id); rank < target; rank++)
            {
                MetaSaveManager.AddVestiges(upgrade.Prices[rank]);
                if (MetaSaveManager.BuyCampRank(upgrade.Id, out _) != CampTransaction.Done)
                    throw new InvalidOperationException($"Campement : rang {rank + 1} de {upgrade.Id} refusé");
                ranks++;
            }
        }
        GD.Print($"[RunObservation] méta {state} : {claimed}/{quests.Count} quêtes, {ranks} rangs du Campement (sans puissance brute)");
    }
}
