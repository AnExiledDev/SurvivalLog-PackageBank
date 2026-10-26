using System;
using GameCore.HotUpdate.Battle.Show;
using GameCore.HotUpdate.ReduxUI;
using HarmonyLib;
using UnityEngine;

namespace PackageBank.Adapters;

/// <summary>
/// While the panel is open: stops the game's hotkeys, so a key pressed over the panel does not also open a menu, and
/// sends every click to the web UI. The game only passes a click to the web UI where one of its own pages reported a
/// button, which the panel is not. Static because a Harmony patch has nowhere else to look.
/// </summary>
internal static class InputGate
{
    private static bool _isPanelOpen;
    private static bool _isBlocking;
    private static int _blockedThroughFrame = -1;
    private static bool _hasRoutedAClick;
    private static Action<string> _log = _ => { };

    public static void Apply(Harmony harmony, Action<string> log)
    {
        _log = log;
        Patch(harmony, typeof(HotKeyManager), "Update", prefix: nameof(SkipWhileBlocked), done: "hotkeys are blocked while the panel is open", consequence: "keys pressed over the panel will also reach the game");
        Patch(harmony, typeof(WebUILayer), "ValidateRaycast", postfix: nameof(RouteClicksToPanel), done: "clicks go to the panel while it is open", consequence: "clicks on the panel may fall through to the game");
    }

    public static void PanelOpened()
    {
        _isPanelOpen = true;
        _isBlocking = true;
    }

    /// <summary>Keeps hotkeys blocked through the next frame so the key that closed the panel (Esc) is not seen by the game too.</summary>
    public static void PanelClosed()
    {
        _isPanelOpen = false;
        _isBlocking = true;
        _blockedThroughFrame = Time.frameCount + 1;
    }

    private static void Patch(Harmony harmony, Type type, string method, string? prefix = null, string? postfix = null, string done = "", string consequence = "")
    {
        try
        {
            harmony.Patch(
                AccessTools.Method(type, method),
                prefix: prefix == null ? null : new HarmonyMethod(typeof(InputGate), prefix),
                postfix: postfix == null ? null : new HarmonyMethod(typeof(InputGate), postfix));
            _log(done);
        }
        catch (Exception ex)
        {
            _log($"could not hook {type.Name}.{method}, {consequence}: {ex.Message}");
        }
    }

    /// <summary>Runs before the game's hotkeys every frame, so with the panel shut and no close to finish it reads only a flag.</summary>
    private static bool SkipWhileBlocked()
    {
        if (!_isBlocking) return true;
        if (_isPanelOpen || Time.frameCount <= _blockedThroughFrame) return false;

        _isBlocking = false;

        return true;
    }

    private static void RouteClicksToPanel(ref bool __result)
    {
        if (!_isPanelOpen || __result) return;

        __result = true;

        if (!_hasRoutedAClick) _log("first click routed to the panel");

        _hasRoutedAClick = true;
    }
}
