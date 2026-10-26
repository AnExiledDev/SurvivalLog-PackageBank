using System;

namespace PackageBank.Core;

/// <summary>Where the player is, as far as the bank cares.</summary>
public enum BankPlace
{
    /// <summary>The title screen, a save loading, or the prologue.</summary>
    NoGame,

    /// <summary>A game is loaded but the player is not at home, where the packages are.</summary>
    Away,

    Home,
}

/// <summary>
/// The cheap part of a read, taken every refresh: where the player is, whether the build shop is open, what the game's
/// click menu is open on (null while it is shut), and the game's language.
/// </summary>
public readonly record struct BankState(BankPlace Place, bool IsShopOpen, long? Menu, bool IsChinese);

/// <summary>What the bank sends to page.js. Live and Shop say whether the call reached the page.</summary>
public interface IBankPage
{
    bool Live(bool isLive);

    bool Shop(string? viewJson);

    void Show(string viewJson);
}

/// <summary>
/// Keeps the page in step with the game, once a refresh. The full read and the view's JSON are built only while a
/// screen that shows the bank is open (the panel, the build shop, the click menu) and only when the game's print says
/// something the view is drawn from changed; every call to the page goes only when what it carries changed.
/// </summary>
public sealed class BankFeed
{
    private readonly IBankGame _game;
    private readonly IBankPage _page;
    private readonly Action<string> _log;
    private BankLimits _limits;

    private bool _hasView;
    private long _viewPrint;
    private string? _view;
    private bool _isViewForPages;

    private string? _shownView;
    private bool? _sentLive;
    private bool _hasSentShop;
    private string? _sentShop;
    private bool? _language;

    public BankFeed(IBankGame game, IBankPage page, BankLimits limits, Action<string> log)
    {
        _game = game;
        _page = page;
        _limits = limits;
        _log = log;
    }

    public void UseLimits(BankLimits limits)
    {
        _limits = limits;
        _hasView = false;
    }

    /// <summary>After a press: the game changed under the view, so the next view is read fresh whatever the print says.</summary>
    public void Forget() => _hasView = false;

    /// <summary>A reloaded page has nothing the bank sent before, so everything goes again.</summary>
    public void PageReloaded()
    {
        _sentLive = null;
        _hasSentShop = false;
        _sentShop = null;
        _shownView = null;
    }

    /// <summary>The panel's view, or null when the panel cannot open (no game, or away from home).</summary>
    public string? View()
    {
        var state = _game.State();

        NoteLanguage(state.IsChinese);

        return state.Place == BankPlace.Home ? ViewNow() : null;
    }

    /// <summary>The view the panel was just opened with, so the next refresh does not send it again.</summary>
    public void Shown(string view) => _shownView = view;

    /// <summary>
    /// Sends the HUD button's state, the shop's and the click menu's bank rows, and the open panel's view, each only
    /// when it changed. False when the panel is open but has nothing to show any more, so it must close.
    /// </summary>
    public bool Refresh(bool isPanelOpen)
    {
        var state = _game.State();
        var isHome = state.Place == BankPlace.Home;

        NoteLanguage(state.IsChinese);
        SendLive(isHome);

        var view = isHome && (isPanelOpen || state.IsShopOpen || state.Menu != null) ? ViewNow() : null;

        SendShop(view != null && _isViewForPages ? view : null);

        if (!isPanelOpen) return true;
        if (view == null) return false;
        if (view == _shownView) return true;

        _shownView = view;
        _page.Show(view);

        return true;
    }

    private string? ViewNow()
    {
        var print = _game.Print();

        if (_hasView && print == _viewPrint) return _view;

        var view = BankView.Of(_game.Read(), _limits);

        _hasView = true;
        _viewPrint = print;
        _view = view;
        // The game's pages want the view only while the build shop is open or the click menu is on a yard package.
        _isViewForPages = view != null && (!view.Contains("\"shop\":null", StringComparison.Ordinal) || !view.Contains("\"menu\":null", StringComparison.Ordinal));

        return view;
    }

    /// <summary>A switch of the game's language redraws everything in the new words, even where the view's JSON is the same.</summary>
    private void NoteLanguage(bool isChinese)
    {
        if (_language == isChinese) return;

        if (_language != null)
        {
            _hasView = false;
            PageReloaded();
        }

        _language = isChinese;
    }

    private void SendLive(bool isLive)
    {
        if (isLive == _sentLive) return;

        if (!_page.Live(isLive)) return;

        _sentLive = isLive;
        _log(isLive ? "HUD button shown" : "HUD button hidden");
    }

    private void SendShop(string? shop)
    {
        if (_hasSentShop && shop == _sentShop) return;

        if (!_page.Shop(shop)) return;

        _hasSentShop = true;
        _sentShop = shop;
    }
}
