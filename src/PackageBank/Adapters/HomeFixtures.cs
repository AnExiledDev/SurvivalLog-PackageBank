using System;
using System.Collections.Generic;
using ExiledToolkit.Game;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using PackageBank.Core;

namespace PackageBank.Adapters;

/// <summary>
/// Sets the home's fixtures down again when an older Pack up took them (the basement entrance): once per loaded save,
/// at home. The game's own load call puts each one back where it belongs, with every other fixture held back, and the
/// copy the player was left with (a piece on a build spot, a yard package or a bank entry) goes only once the fixture
/// stands again. The game never sets down a fixture it recorded as taken down, so that record is dropped first, and
/// put back if the fixture does not appear.
/// </summary>
internal sealed class HomeFixtures
{
    private readonly Action<string> _log;
    private IntPtr _checked;

    public HomeFixtures(Action<string> log) => _log = log;

    /// <summary>True when something was set down again, so the shop's counts change.</summary>
    public bool RepairOnce(
        ConfigManager config,
        AgentManager agents,
        LeadingRole role,
        BuildComponent build,
        IReadOnlyList<BankEntry> banked,
        Action<int> clearEntry)
    {
        if (agents.Pointer == _checked || !IsAtHome(config, role)) return false;

        var save = GameSingleton.Existing<GameSaveManager>()?.gameSave;
        var table = config._Config_GuidanceFurniture_Dict;

        if (save == null || table == null) return false;

        _checked = agents.Pointer;

        var chapter = save.InitChapterId;
        var character = ConfigManager.GetCurrentPlayerSelectId();
        var rows = Rows(config, table);
        var lost = FixtureRepair.Find(rows, chapter, character, Copies(config, agents, build, banked, rows));
        var isRepaired = false;

        foreach (var fixture in lost) isRepaired |= SetDown(agents, build, table, chapter, character, fixture, clearEntry);

        return isRepaired;
    }

    private static bool IsAtHome(ConfigManager config, LeadingRole role)
    {
        var user = AgentTools.GetAgentComponent<UserComponent>(role);

        return user != null && config.Get_Config_MapPoint(user.MapConfigId)?.Type == PackRules.HomeMapType;
    }

    private static List<FixtureRow> Rows(ConfigManager config, Il2CppSystem.Collections.Generic.Dictionary<int, Config_GuidanceFurniture> table)
    {
        var rows = new List<FixtureRow>();

        foreach (var pair in table)
        {
            var row = pair.Value;

            if (row == null) continue;

            var map = row.MapPointID > 0 ? config.Get_Config_MapPoint(row.MapPointID) : null;
            var furniture = config.Get_Config_Furniture(row.FurnitureId);

            rows.Add(new FixtureRow(
                row.FurnitureId,
                row.ChapterType,
                row.PlayerSelectId,
                row.MapPointID,
                map?.ChapterID,
                map?.Type,
                row.PosType,
                row.IsLootSeed,
                furniture?.MoveFunc is { Count: > 0 },
                furniture?.RemoveFunc is { Count: > 0 }));
        }

        return rows;
    }

    private static HomeCopies Copies(ConfigManager config, AgentManager agents, BuildComponent build, IReadOnlyList<BankEntry> banked, List<FixtureRow> rows)
    {
        var fixtures = new HashSet<int>();

        foreach (var row in rows)
        {
            if (row.HasMove && !row.HasRemove) fixtures.Add(row.FurnitureId);
        }

        var standing = new HashSet<int>();
        var placed = new List<(long, int)>();
        var furnitures = agents.GetAllFurnitures();

        for (var i = 0; furnitures != null && i < furnitures.Count; i++)
        {
            var furniture = furnitures[i];

            if (furniture == null || furniture.IsDead || !furniture._IsShow_k__BackingField || !fixtures.Contains(furniture.AgentConfigId)) continue;

            if (furniture._IsGuidanceFurniture_k__BackingField) standing.Add(furniture.AgentConfigId);
            else if (config.Get_Config_MapPoint(furniture.MapConfigId)?.Type == PackRules.HomeMapType) placed.Add((furniture.InstanceId, furniture.AgentConfigId));
        }

        var packages = new List<(long, int)>();
        var points = build._PackagePointMap_k__BackingField;

        if (points != null)
        {
            foreach (var point in points)
            {
                var bag = agents.GetAgent(point.Key)?.TryCast<BagFurniture>();

                if (bag != null && fixtures.Contains(bag._FurnitureConfigId_k__BackingField)) packages.Add((point.Key, bag._FurnitureConfigId_k__BackingField));
            }
        }

        var entries = new List<(int, int)>();

        foreach (var entry in banked)
        {
            if (fixtures.Contains(entry.ConfigId)) entries.Add((entry.Slot, entry.ConfigId));
        }

        var demolished = new HashSet<int>();
        var record = build.GetDemolishedGuidanceIds();

        if (record != null)
        {
            foreach (var id in record) demolished.Add(id);
        }

        return new HomeCopies(standing, placed, packages, entries, demolished);
    }

    private bool SetDown(
        AgentManager agents,
        BuildComponent build,
        Il2CppSystem.Collections.Generic.Dictionary<int, Config_GuidanceFurniture> table,
        int chapter,
        int character,
        LostFixture fixture,
        Action<int> clearEntry)
    {
        var id = fixture.FurnitureId;
        var record = build.GetDemolishedGuidanceIds();
        var wasRecorded = record != null && record.Remove(id);
        var others = new Il2CppSystem.Collections.Generic.HashSet<int>();

        foreach (var pair in table)
        {
            if (pair.Value != null && pair.Value.FurnitureId != id) others.Add(pair.Value.FurnitureId);
        }

        try
        {
            agents.LoadFurnitureFromGuidance(chapter, character, others, null);
        }
        catch (Exception ex)
        {
            _log($"setting furniture {id} down again threw: {ex}");
        }

        if (Standing(agents, id) == null)
        {
            if (wasRecorded) record!.Add(id);

            _log($"furniture {id} is not where the game puts it and could not be set down again, so its copy {fixture.Copy} is kept");

            return false;
        }

        _log($"furniture {id} was packed up by an older version; it stands where the game puts it again");
        TakeAway(agents, build, id, fixture.Copy, clearEntry);

        return true;
    }

    private static Furniture? Standing(AgentManager agents, int configId)
    {
        var furnitures = agents.GetAllFurnitures();

        for (var i = 0; furnitures != null && i < furnitures.Count; i++)
        {
            var furniture = furnitures[i];

            if (furniture != null && !furniture.IsDead && furniture._IsShow_k__BackingField && furniture._IsGuidanceFurniture_k__BackingField && furniture.AgentConfigId == configId) return furniture;
        }

        return null;
    }

    /// <summary>Each step on its own: a copy that will not go is logged and left, never a reason to undo the fixture.</summary>
    private void TakeAway(AgentManager agents, BuildComponent build, int configId, FixtureCopy? copy, Action<int> clearEntry)
    {
        try
        {
            switch (copy)
            {
                case FixtureCopy.Placed placed:
                    if (agents.GetAgent(placed.Id)?.TryCast<Furniture>() is { } piece) AE_DisableFurniture.DemolishFurniture(piece);

                    _log($"took away the copy of furniture {configId} on a build spot (piece {placed.Id})");
                    break;
                case FixtureCopy.Package package:
                    build.DestroyBagFurniture(package.Id);

                    if (build.PurchaseSourceMap?.ContainsKey(package.Id) == true) build.RemovePurchased(package.Id);

                    GameMenu.CloseIfOn(package.Id);
                    _log($"took away the package of furniture {configId} (package {package.Id})");
                    break;
                case FixtureCopy.Banked banked:
                    clearEntry(banked.Slot);
                    _log($"took the banked copy of furniture {configId} out of slot {banked.Slot}");
                    break;
            }
        }
        catch (Exception ex)
        {
            _log($"could not take away the copy {copy} of furniture {configId}, so there may be two of it: {ex}");
        }
    }
}
