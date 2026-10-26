using System;
using PackageBank.Core;
using UnityEngine;

namespace PackageBank.Adapters;

/// <summary>
/// The bank's game side, run once a frame: the panel's open state, the presses the page asks for, and a refresh once a
/// second that keeps the page in step (see BankFeed). Page messages arrive on the web view's thread, so they are
/// queued and handled here, in this frame.
/// </summary>
internal sealed class BankSession
{
    private const float RefreshSeconds = 1f;

    private readonly IBankGame _game;
    private readonly WebPage _page;
    private readonly BankFeed _feed;
    private readonly Action<string> _log;
    private BankLimits _limits = new(IsLimited: true, Large: 4, Medium: 6, Small: 8, Defense: 4);

    private float _nextRefreshAt;
    private bool _isOpen;
    private bool _watchesMenuEveryFrame = true;
    private long? _menuOpenOn;
    private bool _hasReportedMenuReadFailure;

    public BankSession(IBankGame game, WebPage page, Action<string> log)
    {
        _game = game;
        _page = page;
        _log = log;
        _feed = new BankFeed(game, page, _limits, log);
    }

    /// <summary>Called on the main thread at start and after each settings change; the next refresh shows the new limits.</summary>
    public void UseLimits(BankLimits limits)
    {
        _limits = limits;
        _feed.UseLimits(limits);
        _nextRefreshAt = 0f;
    }

    /// <summary>
    /// With the menu-open hook in, every menu that opens reports itself, so the menu is no longer looked for every frame;
    /// the once-a-second refresh sees it close.
    /// </summary>
    public void MenuHooked(bool isHooked) => _watchesMenuEveryFrame = !isHooked;

    public void Update()
    {
        var now = Time.unscaledTime;
        _page.KeepInstalled(now);

        while (PageMessages.TryTake(out var command)) HandleSafely(command);

        if (_isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            Close();

            return;
        }

        if (now >= _nextRefreshAt || (_watchesMenuEveryFrame && HasMenuChanged())) Refresh();
    }

    /// <summary>
    /// Only without the menu-open hook: the click menu's Bank entry must be there as soon as the menu opens, or a quick
    /// click lands on Destroy, so the menu is checked every frame and a change refreshes at once.
    /// </summary>
    private bool HasMenuChanged()
    {
        long? openOn;

        try
        {
            openOn = GameMenu.OpenOn();
        }
        catch (Exception ex)
        {
            if (!_hasReportedMenuReadFailure) _log("reading the click menu failed (reported once): " + ex);

            _hasReportedMenuReadFailure = true;

            return false;
        }

        if (openOn == _menuOpenOn) return false;

        _menuOpenOn = openOn;

        return true;
    }

    /// <summary>
    /// Called by the game's own menu-open call, before the game sends the menu to the web view, so the page has the Bank
    /// entry's data by the time the menu's buttons are drawn.
    /// </summary>
    public void MenuOpened(long id)
    {
        GameMenu.Opening(id);
        _menuOpenOn = id == 0 ? null : id;
        Refresh();
    }

    /// <summary>One command that throws must not cost the rest of the frame its commands.</summary>
    private void HandleSafely(PageCommand command)
    {
        try
        {
            Handle(command);
        }
        catch (Exception ex)
        {
            _log($"page command {command} failed: {ex}");
        }
    }

    private void Handle(PageCommand command)
    {
        switch (command)
        {
            case PageCommand.Toggle:
                Toggle();
                break;
            case PageCommand.Close:
                Close();
                break;
            case PageCommand.Store store:
                Press($"bank package {store.PackageId}", () => BankRun.Store(_game, _limits, store.PackageId));
                break;
            case PageCommand.StoreShopRow row:
                Press($"bank shop row {row.Index}", () => BankRun.StoreShopRow(_game, _limits, row.Index));
                break;
            case PageCommand.StoreMenu:
                Press("bank from the click menu", () => BankRun.StoreMenu(_game, _limits));
                break;
            case PageCommand.TakeOut takeOut:
                Press($"take out slot {takeOut.Slot}", () => BankRun.TakeOut(_game, takeOut.Slot));
                break;
            case PageCommand.Ready:
                _log("page script ready");
                _page.Reloaded();
                _feed.PageReloaded();
                _nextRefreshAt = 0f;
                ReopenAfterReload();
                break;
            case PageCommand.NoScript:
                _page.Install();
                break;
            case PageCommand.Unknown unknown:
                _log("page said: " + unknown.Text);
                break;
            default:
                throw new InvalidOperationException("unhandled page command " + command);
        }
    }

    /// <summary>
    /// Once a second, and straight after anything that changes what the page shows. Sends only what changed, and reads
    /// the bank only while a screen that shows it is open.
    /// </summary>
    private void Refresh()
    {
        _nextRefreshAt = Time.unscaledTime + RefreshSeconds;

        if (!_feed.Refresh(_isOpen)) Close();
    }

    private void Toggle()
    {
        if (_isOpen) Close();
        else Open();
    }

    /// <summary>Away from home, or with no game loaded, there is nothing to bank and the panel stays shut.</summary>
    private void Open()
    {
        var view = _feed.View();

        if (view == null) return;

        _isOpen = true;
        InputGate.PanelOpened();
        _feed.Shown(view);
        _page.Open(view);
        _log("panel opened");
    }

    private void Close()
    {
        if (!_isOpen) return;

        _isOpen = false;
        InputGate.PanelClosed();
        _page.Close();
    }

    /// <summary>A reloaded web view starts with the panel shut, so a panel the game side thinks is open is drawn again.</summary>
    private void ReopenAfterReload()
    {
        if (!_isOpen) return;

        var view = _feed.View();

        if (view == null)
        {
            Close();

            return;
        }

        _feed.Shown(view);
        _page.Open(view);
    }

    /// <summary>
    /// Runs one press, shows what it did, and redraws the panel and the shop rows straight after. Only a page command
    /// gets here, never a plain frame.
    /// </summary>
    private void Press(string name, Func<BankOutcome> press)
    {
        var isChinese = GameLanguage.IsChinese();
        string message;

        try
        {
            var outcome = press();
            message = OutcomeMessage.Describe(outcome, isChinese);
            _log($"{name}: {outcome}");
        }
        catch (Exception ex)
        {
            message = Texts.Failed.Pick(isChinese);
            _log($"{name} failed: {ex}");
        }

        _page.Status(message);
        _feed.Forget();
        Refresh();
    }
}
