using System;
using ExiledToolkit.Game;
using GameCore.HotUpdate.ReduxUI;

namespace PackageBank.Adapters;

/// <summary>
/// Tells the session the moment the game opens its click menu, in the same call that opens it. The game sends the
/// menu's buttons to the web view later in the frame, so the Bank entry's data reaches the page first and the entry
/// is drawn with the game's own. Static because a Harmony patch has nowhere else to look.
/// </summary>
internal static class MenuOpening
{
    private static Action<long> _onOpened = _ => { };
    private static PatchKit? _hooks;

    /// <summary>False when the hook could not go in; the session then looks for the menu every frame instead.</summary>
    public static bool Apply(PatchKit hooks, Action<long> onOpened)
    {
        _onOpened = onOpened;
        _hooks = hooks;

        return hooks.Patch(
            typeof(MenuOpening),
            typeof(Ac_SelectMenu_Open),
            nameof(Ac_SelectMenu_Open.SendAction),
            postfix: nameof(Opened),
            done: "the Bank entry is sent as the click menu opens",
            consequence: "the Bank entry may show a moment after the menu");
    }

    // Harmony matches the parameter by its name in the game's method.
    private static void Opened(long FurnitureLogicId)
    {
        try
        {
            _onOpened(FurnitureLogicId);
        }
        catch (Exception ex)
        {
            _hooks?.Fail("sending the Bank entry as the menu opened", ex);
        }
    }
}
