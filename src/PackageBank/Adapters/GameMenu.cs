using GameCore.HotUpdate.ReduxUI;
using Il2CppInterop.Runtime;

namespace PackageBank.Adapters;

/// <summary>
/// The game's click menu, the one with Install and Return that opens on a yard package. Its state keeps the id of
/// what it was opened on; the page adds a Bank entry to it, and a banked package's menu is shut, since the package is gone.
/// </summary>
internal static class GameMenu
{
    /// <summary>What the menu is open on, or null while it is shut.</summary>
    public static long? OpenOn()
    {
        var redux = GameSingleton.Existing<ReduxUISystem>();

        if (redux == null || !redux.IsUIActive(Il2CppType.Of<WebUI_SelectMenu>())) return null;

        var tree = redux.reduxStoreLayer?.stateTree;

        if (tree == null || !tree.TryGetValue(Il2CppType.Of<State_Web_SelectMenu>(), out var raw)) return null;

        var state = raw?.TryCast<State_Web_SelectMenu>();

        return state == null || state.SelectLogicId == 0 ? null : state.SelectLogicId;
    }

    public static void CloseIfOn(long id)
    {
        if (OpenOn() == id) Ac_SelectMenu_Close.SendAction();
    }
}
