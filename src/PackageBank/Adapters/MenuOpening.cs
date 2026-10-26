using System;
using GameCore.HotUpdate.ReduxUI;
using HarmonyLib;

namespace PackageBank.Adapters;

/// <summary>
/// Tells the session the moment the game opens its click menu, in the same call that opens it. The game sends the
/// menu's buttons to the web view later in the frame, so the Bank entry's data reaches the page first and the entry
/// is drawn with the game's own. Static because a Harmony patch has nowhere else to look.
/// </summary>
internal static class MenuOpening
{
    private static Action<long> _onOpened = _ => { };
    private static Action<string> _log = _ => { };
    private static bool _hasReportedFailure;

    public static void Apply(Harmony harmony, Action<long> onOpened, Action<string> log)
    {
        _onOpened = onOpened;
        _log = log;

        try
        {
            harmony.Patch(
                AccessTools.Method(typeof(Ac_SelectMenu_Open), nameof(Ac_SelectMenu_Open.SendAction)),
                postfix: new HarmonyMethod(typeof(MenuOpening), nameof(Opened)));
            log("the Bank entry is sent as the click menu opens");
        }
        catch (Exception ex)
        {
            log($"could not hook Ac_SelectMenu_Open.SendAction, the Bank entry may show a moment after the menu: {ex.Message}");
        }
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
            if (!_hasReportedFailure) _log("sending the Bank entry as the menu opened failed (reported once): " + ex);

            _hasReportedFailure = true;
        }
    }
}
