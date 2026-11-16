using System;
using ExiledToolkit.Game;
using GameCore.HotUpdate.Battle.Show;
using GameCore.Scripts;
using UnityEngine;

namespace PackageBank.Adapters;

/// <summary>
/// The Layout screen's list of boxes it shows. The game fills it when the screen opens, from every box under
/// BuildingPos, and its floor buttons and package filter switch only the boxes in it. So a spot box made while the screen
/// is open joins the list the way the game adds its own, and a box that goes leaves it first. The screen is remembered
/// from a postfix on ShowBuildingPos (Floor Plan hooks it too); static because a Harmony patch has nowhere else to look.
/// </summary>
internal static class LayoutScreen
{
    private const string CubeName = "BuildingPos_Cube";
    private const string BaseState = "State01";

    private static MapNodeSearch? _search;

    public static void Apply(PatchKit hooks) =>
        hooks.Patch(
            typeof(LayoutScreen),
            typeof(MapNodeSearch),
            nameof(MapNodeSearch.ShowBuildingPos),
            postfix: nameof(AfterShowBuildingPos),
            consequence: "a spot where house furniture stood that opens while the Layout screen is open shows on every floor until the screen is opened again");

    private static void AfterShowBuildingPos(MapNodeSearch __instance) => _search = __instance;

    /// <summary>
    /// Adds a new box to the open screen. The box is a copy of a shown one, and a fixture that was just taken down here
    /// stood in a part of the home that is open, so it passes the checks the game makes when the screen opens.
    /// </summary>
    public static void Join(Transform root, GameObject box)
    {
        if (!root.gameObject.activeSelf || OpenSearch() is not { } search || search._buildingPosShowStates is not { } states) return;
        if (box.transform.Find(CubeName) is not { } cube || cube.GetComponent<ShowState>() is not { } state) return;

        var name = box.name;

        box.SetActive(true);
        MapNodeSearch.ApplyWallSlotCubePose(name, cube);
        states[name] = state;
        search.SetSlotState(name, state, BaseState, "PackageBank");

        // The game's filters then decide whether the floor in view and a waiting package keep it.
        if (search._pendingFilterSlotType > 0) search.FilterBuildingPosSlots(search._pendingFilterSlotType);
        else if (search._pendingFilterFloorId > 0) search.OnSwitchFloor(search._pendingFilterFloorId);
    }

    /// <summary>Takes a box off the open screen before it is destroyed, so a floor switch does not reach for it.</summary>
    public static void Leave(string name)
    {
        if (OpenSearch() is not { } search) return;

        search._buildingPosShowStates?.Remove(name);
        search._slotBaseStates?.Remove(name);

        if (search._hoveredSlotNode == name) search._hoveredSlotNode = null;
        if (search._hoverPaintedSlot == name) search._hoverPaintedSlot = null;
    }

    private static MapNodeSearch? OpenSearch() => _search != null && _search.Pointer != IntPtr.Zero ? _search : null;
}
