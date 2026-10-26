using GameCore.HotUpdate.ReduxUI;
using Il2CppInterop.Runtime;
using UnityEngine;

namespace PackageBank.Adapters;

/// <summary>
/// The game's click menu, the one with Install and Return that opens on a yard package. Its state keeps the id of
/// what it was opened on; the page adds a Bank entry to it, and a banked package's menu is shut, since the package is gone.
/// </summary>
internal static class GameMenu
{
    private static Il2CppSystem.Type? _menuPage;
    private static Il2CppSystem.Type? _menuState;
    private static long _openingOn;
    private static int _openingFrame = -1;

    private static Il2CppSystem.Type MenuPage => _menuPage ??= Il2CppType.Of<WebUI_SelectMenu>();

    private static Il2CppSystem.Type MenuState => _menuState ??= Il2CppType.Of<State_Web_SelectMenu>();

    /// <summary>
    /// The menu just opened on this, in this frame. The game may not have stored the menu's state yet, so for the rest
    /// of the frame this wins over what the state says.
    /// </summary>
    public static void Opening(long id)
    {
        _openingOn = id;
        _openingFrame = Time.frameCount;
    }

    /// <summary>What the menu is open on, or null while it is shut.</summary>
    public static long? OpenOn()
    {
        if (_openingFrame == Time.frameCount) return _openingOn == 0 ? null : _openingOn;

        var redux = GameSingleton.Existing<ReduxUISystem>();

        if (redux == null || !redux.IsUIActive(MenuPage)) return null;

        var tree = redux.reduxStoreLayer?.stateTree;

        if (tree == null || !tree.TryGetValue(MenuState, out var raw)) return null;

        var state = raw?.TryCast<State_Web_SelectMenu>();

        return state == null || state.SelectLogicId == 0 ? null : state.SelectLogicId;
    }

    public static void CloseIfOn(long id)
    {
        if (OpenOn() == id) Ac_SelectMenu_Close.SendAction();
    }
}
