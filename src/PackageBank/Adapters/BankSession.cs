using System;
using PackageBank.Core;
using UnityEngine;

namespace PackageBank.Adapters;

/// <summary>
/// The bank's game side, run once a frame: the panel's open state, the view sent to page.js for the panel and for the
/// build shop's list, and the presses the page asks for. Page messages arrive on the web view's thread, so they are
/// queued and handled here, in this frame.
/// </summary>
internal sealed class BankSession
{
    private const float RefreshSeconds = 1f;

    private readonly IBankGame _game;
    private readonly WebPage _page;
    private readonly Action<string> _log;
    private BankLimits _limits = new(IsLimited: true, Large: 4, Medium: 6, Small: 8);

    private string? _shownView;
    private string? _shownShop;
    private bool _hasSentShop;
    private float _nextRefreshAt;
    private bool? _sentLive;
    private bool _isOpen;

    public BankSession(IBankGame game, WebPage page, Action<string> log)
    {
        _game = game;
        _page = page;
        _log = log;
    }

    /// <summary>Called on the main thread at start and after each settings change; the next refresh shows the new limits.</summary>
    public void UseLimits(BankLimits limits)
    {
        _limits = limits;
        _nextRefreshAt = 0f;
    }

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

        if (now >= _nextRefreshAt) Refresh();
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
            case PageCommand.TakeOut takeOut:
                Press($"take out slot {takeOut.Slot}", () => BankRun.TakeOut(_game, takeOut.Slot));
                break;
            case PageCommand.Ready:
                _log("page script ready");
                _sentLive = null;
                _hasSentShop = false;
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
    /// One read a second feeds everything the page shows: the HUD button (only at home), the open panel, and the bank
    /// rows under the build shop's list. Each is sent only when it changed.
    /// </summary>
    private void Refresh()
    {
        _nextRefreshAt = Time.unscaledTime + RefreshSeconds;
        var view = BankView.Of(_game.Read(), _limits);

        SendLive(view != null);
        SendShop(view);

        if (!_isOpen) return;

        if (view == null)
        {
            Close();

            return;
        }

        if (view == _shownView) return;

        _shownView = view;
        _page.Show(view);
    }

    private void SendLive(bool isLive)
    {
        if (isLive == _sentLive) return;

        if (_page.Live(isLive))
        {
            _sentLive = isLive;
            _log($"HUD button {(isLive ? "shown" : "hidden")}");
        }
    }

    /// <summary>The shop's part of the view goes only while that shop is open; null takes the bank rows away.</summary>
    private void SendShop(string? view)
    {
        var shop = view != null && !view.Contains("\"shop\":null", StringComparison.Ordinal) ? view : null;

        if (_hasSentShop && shop == _shownShop) return;

        if (!_page.Shop(shop)) return;

        _hasSentShop = true;
        _shownShop = shop;
    }

    private void Toggle()
    {
        if (_isOpen) Close();
        else Open();
    }

    /// <summary>Away from home, or with no game loaded, there is nothing to bank and the panel stays shut.</summary>
    private void Open()
    {
        var view = BankView.Of(_game.Read(), _limits);

        if (view == null) return;

        _isOpen = true;
        InputGate.PanelOpened();
        _shownView = view;
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

        var view = BankView.Of(_game.Read(), _limits);

        if (view == null)
        {
            Close();

            return;
        }

        _shownView = view;
        _page.Open(view);
    }

    /// <summary>Runs one press, shows what it did, and redraws the panel and the shop rows straight after.</summary>
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
        Refresh();
    }
}
