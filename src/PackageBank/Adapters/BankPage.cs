using System;
using ExiledToolkit;
using ExiledToolkit.Game;
using ExiledToolkit.Web;
using PackageBank.Core;

namespace PackageBank.Adapters;

/// <summary>
/// The mod's half that lives in the game's web UI: page.js, run in the root document the game's own pages sit in. It
/// draws the bank panel, puts the Package Bank button in the HUD's tool column, and adds the bank to the build shop's
/// package list. The web view can reload at any time and lose the script, so Exiled's Toolkit checks for it every
/// couple of seconds and the session puts it back when the page says it is missing.
/// </summary>
internal sealed class BankPage : IBankPage
{
    private const string ScriptResource = "PackageBank.page.js";
    private const string Global = "window.__slpkgbank";

    private readonly WebPage _page;

    public BankPage(Action<string> log)
    {
        // The page keeps its words, so they go only with the first call after a load and after a switch of the game's
        // language, ahead of that call, so the call is drawn in them.
        _page = new WebPage(typeof(BankPage).Assembly, ScriptResource, Global, PageCommand.Prefix + "noscript", log)
        {
            Words = PageWords.Script,
        };
    }

    public void KeepInstalled(float now) => _page.KeepInstalled(now);

    public void Install() => _page.Install();

    /// <summary>The page reloaded and lost its words with everything else.</summary>
    public void Reloaded() => _page.ForgetWords();

    public void Open(string viewJson) => _page.Call($"open({viewJson})");

    public void Show(string viewJson) => _page.Call($"setData({viewJson})");

    public void Close() => _page.Call("close()");

    /// <summary>
    /// A result line: in the panel's footer while it is open, as an Exiled's Toolkit toast otherwise. The page hears it
    /// either way, as it frees the button that was pressed.
    /// </summary>
    public void Status(string text, bool isPanelOpen, ToastKind kind)
    {
        _page.Call($"status({JsText.Literal(text)})");

        if (isPanelOpen || string.IsNullOrEmpty(text)) return;

        Toasts.Show(new ToastRequest(Texts.Source.Pick(GameLanguage.IsChinese()), text) { Kind = kind });
    }

    /// <summary>Whether a game the bank works in is loaded: the HUD button shows only then.</summary>
    public bool Live(bool isLive) => _page.Call($"live({(isLive ? "true" : "false")})");

    /// <summary>What the build shop's package list draws the bank from, or null to take the bank out of the shop.</summary>
    public bool Shop(string? viewJson) => _page.Call($"shop({viewJson ?? "null"})");
}
