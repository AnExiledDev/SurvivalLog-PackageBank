namespace PackageBank.Core;

/// <summary>
/// What the game's Layout screen knows about a placed piece. HasMove and HasRemove are Config_Furniture.MoveFunc and
/// RemoveFunc being set; IsRemoveHiddenHere is the game hiding Remove for this piece in this save (a few pieces come down
/// only in an endless save); MapType is the Config_MapPoint.Type of the map the piece stands on, null when the game has
/// no row for it.
/// </summary>
public sealed record PieceFacts(bool HasMove, bool HasRemove, bool IsRemoveHiddenHere, int? MapType);

public static class PackRules
{
    /// <summary>Config_MapPoint.Type for the home and its floors; the shops and other places are 2.</summary>
    public const int HomeMapType = 1;

    /// <summary>
    /// Pack up takes a piece only where the game itself lets the player both move it and take it down: a home fixture
    /// with Move but no Remove (the basement entrance) has no place to go back to once packed, and the counters of the
    /// shops visited before the disaster are not the player's. A map the game has no row for is left as home.
    /// </summary>
    public static bool IsPackable(PieceFacts facts) =>
        facts.HasMove && facts.HasRemove && !facts.IsRemoveHiddenHere && facts.MapType is null or HomeMapType;
}
