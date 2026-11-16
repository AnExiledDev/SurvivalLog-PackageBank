using System;
using ExiledToolkit.Game;
using GameCore.HotUpdate.Battle.Logic;

namespace PackageBank.Adapters;

/// <summary>
/// Makes sure the spot rows and the boxes are in the tables before the game builds a save's furniture: a piece saved on a
/// spot with no row loads with no kind of spot and cannot be moved or packed, and a saved package of a piece with no box
/// would be invisible. The target was checked in GameAssembly.dll (no relative
/// jump or call in its first 14 bytes); Wind Power hooks it too. Static because a Harmony patch has nowhere else to look.
/// </summary>
internal static class LoadRows
{
    private static Action _beforeLoad = () => { };

    public static void Apply(PatchKit hooks, Action beforeLoad)
    {
        _beforeLoad = beforeLoad;

        hooks.Patch(
            typeof(LoadRows),
            typeof(AgentManager),
            "DeduplicateAgentSavesForLoad",
            prefix: nameof(BeforeLoad),
            done: "the spots where house furniture stood and the boxes for house furniture are ready before a save loads",
            consequence: "a piece on a spot where house furniture stood may load without its spot if the game made a new spot table, and a saved package of house furniture may not show until you pack up or take out something");
    }

    private static void BeforeLoad() => _beforeLoad();
}
