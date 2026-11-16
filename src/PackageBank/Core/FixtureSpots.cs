using System;
using System.Collections.Generic;

namespace PackageBank.Core;

/// <summary>
/// A build spot Package Bank adds where a home fixture stood: Id and Name are its Config_MapSlot row, Type the fixture's
/// slot type (so the fixture's own package fits it again), Floor the row's MapID (1 ground, 101 basement, 102 upstairs)
/// and Point the fixture's point on the map, where the spot goes.
/// </summary>
public sealed record FixtureSpot(int FurnitureId, int Id, string Name, int Type, int Floor, string Point);

/// <summary>
/// Many home fixtures (cabinets, workbenches, the radio) stand on a fixed point of the map, not on a build spot. Once one
/// is taken down, by Pack up or by Remove on the Layout screen, the game never sets it down again and nothing can go where it
/// stood. Package Bank adds a build spot there of the fixture's size, so its package, or anything else that size, can go
/// back. A save keeps the spot's name, so the rows are added for every such fixture before any save loads; a row with
/// no point on the map shows nowhere. The spot itself (its point and its box) shows only while the fixture is down.
/// </summary>
public static class FixtureSpots
{
    public const string NamePrefix = "PackageBank_Fixture_";

    /// <summary>Above the game's highest slot id (305288) and Floor Plan's range (9000000 plus 1000 per home).</summary>
    public const int FirstId = 9_500_000;

    public const int GroundFloor = 1;
    public const int Basement = 101;
    public const int Upstairs = 102;

    /// <summary>Small, Medium, Large, Wall, Center, Tabletop and Bed. Doors, windows and defense squares are not house fixtures.</summary>
    private const int FirstSpotType = 1;
    private const int LastSpotType = 7;

    /// <summary>
    /// One spot per home fixture on a fixed point that the player can take down (Move and Remove) and whose package has a
    /// kind of build spot to go on. A fixture with no Remove (the basement entrance) is never taken down, and one with
    /// slot type 0 (a ladder, a lamp) fits no spot.
    /// </summary>
    public static IReadOnlyList<FixtureSpot> Plan(IEnumerable<FixtureRow> rows)
    {
        var spots = new List<FixtureSpot>();
        var seen = new HashSet<int>();

        foreach (var row in rows)
        {
            if (!GetsSpot(row) || row.PosPoint is not { } point || !seen.Add(row.FurnitureId)) continue;

            spots.Add(new FixtureSpot(row.FurnitureId, FirstId + row.FurnitureId, NamePrefix + row.FurnitureId, row.SlotType, FloorOf(row.MapPointId), point));
        }

        return spots;
    }

    public static bool GetsSpot(FixtureRow row) =>
        row.PosType == FixtureRepair.MapPointPosType
        && row.MapType == PackRules.HomeMapType
        && row.HasMove
        && row.HasRemove
        && !row.IsLootSeed
        && row.SlotType is >= FirstSpotType and <= LastSpotType
        && !string.IsNullOrEmpty(row.PosPoint);

    public static bool IsOurs(string? name) => name != null && name.StartsWith(NamePrefix, StringComparison.Ordinal);

    /// <summary>
    /// The spots to show in the loaded home: those whose fixture is down (in the game's record of taken-down fixtures)
    /// and whose point is on this home's map, while the setting is on and Uninstall prep is off; and always every spot a
    /// piece stands on, so the piece keeps the point the game reads for it.
    /// </summary>
    public static IReadOnlySet<string> Shown(
        IEnumerable<FixtureSpot> spots,
        IReadOnlySet<int> demolished,
        IReadOnlySet<string> pointsOnMap,
        IReadOnlySet<string> taken,
        bool isOn,
        bool isUninstalling)
    {
        var shown = new HashSet<string>();
        var opens = isOn && !isUninstalling;

        foreach (var spot in spots)
        {
            if (!pointsOnMap.Contains(spot.Point)) continue;

            if (taken.Contains(spot.Name) || (opens && demolished.Contains(spot.FurnitureId))) shown.Add(spot.Name);
        }

        return shown;
    }

    /// <summary>The floor a fixture's map point is on; the home's other maps (the ground floor, a neighbour's flat) are ground floors.</summary>
    public static int FloorOf(int mapPointId) => mapPointId is Basement or Upstairs ? mapPointId : GroundFloor;
}
