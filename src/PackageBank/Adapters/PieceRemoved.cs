using System;
using ExiledToolkit.Game;
using GameCore.HotUpdate.Battle.Logic;

namespace PackageBank.Adapters;

/// <summary>
/// Tells the spots where house furniture stood that a piece was packed up or removed: the Layout screen's Remove, story
/// removals and Pack up all go through AE_DisableFurniture.DemolishFurniture, which records a house fixture as
/// taken down. The target was checked in GameAssembly.dll (no relative jump or call in its first 14 bytes). Static
/// because a Harmony patch has nowhere else to look.
/// </summary>
internal static class PieceRemoved
{
    private static Action _removed = () => { };

    public static void Apply(PatchKit hooks, Action removed)
    {
        _removed = removed;

        hooks.Patch(
            typeof(PieceRemoved),
            typeof(AE_DisableFurniture),
            nameof(AE_DisableFurniture.DemolishFurniture),
            postfix: nameof(AfterRemove),
            done: "a spot opens as soon as house furniture is packed up or removed",
            consequence: "a spot where house furniture stood opens only on the next load or trip, not when the furniture is taken down");
    }

    private static void AfterRemove() => _removed();
}
