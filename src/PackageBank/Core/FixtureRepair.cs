using System.Collections.Generic;

namespace PackageBank.Core;

/// <summary>
/// One Config_GuidanceFurniture row: a piece the game sets down itself when a save loads. MapChapter and MapType are
/// the Config_MapPoint row of MapPointId (null when there is none); HasMove and HasRemove are the furniture's MoveFunc
/// and RemoveFunc being set, SlotType its Config_Furniture.SlotType.
/// </summary>
public sealed record FixtureRow(
    int FurnitureId,
    int ChapterType,
    int PlayerSelectId,
    int MapPointId,
    int? MapChapter,
    int? MapType,
    int PosType,
    bool IsLootSeed,
    bool HasMove,
    bool HasRemove,
    int SlotType);

/// <summary>A copy of a fixture that is not where the game put it: a placed piece, a yard package, or a bank entry.</summary>
public abstract record FixtureCopy
{
    private FixtureCopy()
    {
    }

    /// <summary>The fixture installed somewhere else from its package, as a piece of its own.</summary>
    public sealed record Placed(long Id) : FixtureCopy;

    public sealed record Package(long Id) : FixtureCopy;

    public sealed record Banked(int Slot) : FixtureCopy;
}

/// <summary>
/// What the loaded home holds. Standing are the config ids of pieces the game set down itself and that still stand;
/// Placed, Packages and Banked are the other copies by config id; Demolished is BuildComponent's set of fixtures taken
/// down, which the game never sets down again.
/// </summary>
public sealed record HomeCopies(
    IReadOnlySet<int> Standing,
    IReadOnlyList<(long Id, int ConfigId)> Placed,
    IReadOnlyList<(long Id, int ConfigId)> Packages,
    IReadOnlyList<(int Slot, int ConfigId)> Banked,
    IReadOnlySet<int> Demolished);

/// <summary>A home fixture to set down again where the game puts it, and the copy to take away once it stands (null when there is none).</summary>
public sealed record LostFixture(int FurnitureId, FixtureCopy? Copy);

/// <summary>
/// Pack up before 0.7.1 also took the basement entrance, which the game never lets the player take down, and the
/// ladders between floors, which stand on no build spot; neither can go back where it was. This finds such fixtures in a
/// loaded save, so the game can set them down again.
/// </summary>
public static class FixtureRepair
{
    /// <summary>PosType 1: a fixed point on the map. 2 is a build spot and 3 a yard package, which the player may move.</summary>
    public const int MapPointPosType = 1;

    /// <summary>
    /// A fixture of the home, on a fixed point, that an older Pack up took but this one does not (see IsTakenByOldPackUp),
    /// which the game sets down for this save (as AgentManager.LoadFurnitureFromGuidance picks rows), and that no longer
    /// stands. Each gets one copy to take away: a placed one first (it holds a build spot), then a yard package, then a
    /// bank entry. A fixture with no copy left is set down again only when it has no Remove and the game recorded taking
    /// it down: only Pack up can take such a piece down, while a ladder the record holds may be one the player removed on
    /// the Layout screen.
    /// </summary>
    public static IReadOnlyList<LostFixture> Find(IEnumerable<FixtureRow> rows, int chapterId, int playerSelectId, HomeCopies home)
    {
        var lost = new List<LostFixture>();
        var seen = new HashSet<int>();

        foreach (var row in rows)
        {
            if (!IsTakenByOldPackUp(row) || !IsLoaded(row, chapterId, playerSelectId)) continue;
            if (home.Standing.Contains(row.FurnitureId) || !seen.Add(row.FurnitureId)) continue;

            var copy = CopyOf(home, row.FurnitureId);

            if (copy != null || (!row.HasRemove && home.Demolished.Contains(row.FurnitureId))) lost.Add(new LostFixture(row.FurnitureId, copy));
        }

        return lost;
    }

    /// <summary>
    /// A home fixture on a fixed point that Pack up offered before 0.7.1 (it has Move) and no longer offers: no Remove
    /// (the basement entrance) or no kind of build spot (a ladder).
    /// </summary>
    public static bool IsTakenByOldPackUp(FixtureRow row) =>
        row.HasMove
        && row.PosType == MapPointPosType
        && row.MapType == PackRules.HomeMapType
        && !PackRules.IsPackable(new PieceFacts(row.HasMove, row.HasRemove, IsRemoveHiddenHere: false, row.MapType, row.SlotType));

    /// <summary>What the Cheat Menu button reports: the fixtures set down again, and those that would not go.</summary>
    public static string Describe(IReadOnlyList<string> setDown, IReadOnlyList<string> kept)
    {
        if (setDown.Count == 0 && kept.Count == 0) return "nothing needed fixing: the basement entrance and the ladders stand where the game puts them";

        var parts = new List<string>();

        if (setDown.Count > 0) parts.Add($"set down again where the game puts them: {string.Join(", ", setDown)}");
        if (kept.Count > 0) parts.Add($"could not set down, so the copy you have stays (see the log): {string.Join(", ", kept)}");

        return string.Join(". ", parts);
    }

    /// <summary>The same test as AgentManager.ShouldLoadFurniture: which ChapterType rows load in which chapter.</summary>
    public static bool IsLoadedInChapter(int chapterType, int chapterId)
    {
        if (chapterId == 0) return chapterType == 4;
        if (chapterId is >= 1000 and <= 1999) return chapterType == 5;

        return chapterType switch
        {
            1 => true,
            2 => chapterId == 2,
            3 => chapterId == 1,
            _ => chapterType == -1,
        };
    }

    private static bool IsLoaded(FixtureRow row, int chapterId, int playerSelectId)
    {
        if (row.IsLootSeed || !IsLoadedInChapter(row.ChapterType, chapterId)) return false;
        if (playerSelectId > 0 && row.PlayerSelectId > 0 && row.PlayerSelectId != playerSelectId) return false;

        return chapterId is < 1000 or > 1999 || row.MapPointId <= 0 || row.MapChapter == chapterId;
    }

    private static FixtureCopy? CopyOf(HomeCopies home, int configId)
    {
        foreach (var placed in home.Placed)
        {
            if (placed.ConfigId == configId) return new FixtureCopy.Placed(placed.Id);
        }

        foreach (var package in home.Packages)
        {
            if (package.ConfigId == configId) return new FixtureCopy.Package(package.Id);
        }

        foreach (var entry in home.Banked)
        {
            if (entry.ConfigId == configId) return new FixtureCopy.Banked(entry.Slot);
        }

        return null;
    }
}
