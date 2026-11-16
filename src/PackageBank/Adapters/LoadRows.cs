using System;
using ExiledToolkit.Game;
using GameCore.HotUpdate.Battle.Logic;

namespace PackageBank.Adapters;

/// <summary>
/// Makes sure the spot rows are in the table before the game builds a save's furniture: a piece saved on a spot with no
/// row loads with no kind of spot and cannot be moved or packed. The target was checked in GameAssembly.dll (no relative
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
            done: "the spots where house furniture stood are ready before a save loads",
            consequence: "a piece on a spot where house furniture stood may load without its spot if the game made a new spot table");
    }

    private static void BeforeLoad() => _beforeLoad();
}
