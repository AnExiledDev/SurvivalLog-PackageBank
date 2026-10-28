using PackageBank.Core;
using Xunit;
using static PackageBank.Tests.FakeGame;

namespace PackageBank.Tests;

/// <summary>
/// What a refresh costs and what it sends. The game is read once a second, so a refresh with nothing changed, or with
/// no bank screen open, must read nothing and send nothing; a real change, a save load or a language switch must
/// still reach the page.
/// </summary>
public class BankFeedTests
{
    private static readonly BankLimits Defaults = new(IsLimited: true, Large: 4, Medium: 6, Small: 8, Defense: 4);

    /// <summary>Records every call the feed makes to the page.</summary>
    private sealed class FakePage : IBankPage
    {
        public List<bool> Lives { get; } = new();

        public List<string?> Shops { get; } = new();

        public List<string> Shows { get; } = new();

        public int Calls => Lives.Count + Shops.Count + Shows.Count;

        public bool Live(bool isLive)
        {
            Lives.Add(isLive);

            return true;
        }

        public bool Shop(string? viewJson)
        {
            Shops.Add(viewJson);

            return true;
        }

        public void Show(string viewJson) => Shows.Add(viewJson);
    }

    private static (FakeGame Game, FakePage Page, BankFeed Feed) Start()
    {
        var game = new FakeGame();
        var page = new FakePage();

        game.Add(Bed);

        return (game, page, new BankFeed(game, page, Defaults, _ => { }));
    }

    /// <summary>The panel as BankSession opens it: the view, marked shown.</summary>
    private static void OpenPanel(BankFeed feed) => feed.Shown(Assert.IsType<string>(feed.View()));

    // ===== Nothing changed =====

    [Fact]
    public void With_the_panel_open_an_unchanged_game_is_not_read_again_and_nothing_is_sent()
    {
        var (game, page, feed) = Start();

        OpenPanel(feed);
        feed.Refresh(isPanelOpen: true);
        var reads = game.Reads;
        var calls = page.Calls;

        for (var i = 0; i < 5; i++) Assert.True(feed.Refresh(isPanelOpen: true));

        Assert.Equal(reads, game.Reads);
        Assert.Equal(calls, page.Calls);
    }

    [Fact]
    public void With_the_shop_open_an_unchanged_game_is_not_read_again_and_the_shop_rows_go_once()
    {
        var (game, page, feed) = Start();
        game.Shop = new List<ShopRow> { new(0, Bed, 100) };

        feed.Refresh(isPanelOpen: false);
        var reads = game.Reads;

        for (var i = 0; i < 5; i++) feed.Refresh(isPanelOpen: false);

        Assert.Equal(reads, game.Reads);
        Assert.NotNull(Assert.Single(page.Shops));
    }

    // ===== Closed screens =====

    /// <summary>
    /// The first refresh at home reads the bank once ahead of time, so the first click menu of a session does not run
    /// the read and the view for the first time on its frame. After that, with no screen open, nothing is read.
    /// </summary>
    [Fact]
    public void With_no_bank_screen_open_the_bank_is_read_once_ahead_of_time_and_only_the_first_refresh_sends()
    {
        var (game, page, feed) = Start();

        for (var i = 0; i < 5; i++) feed.Refresh(isPanelOpen: false);

        Assert.Equal(1, game.Reads);
        Assert.Equal(1, game.Prints);
        Assert.Equal(new[] { true }, page.Lives);
        Assert.Equal(new string?[] { null }, page.Shops);
        Assert.Empty(page.Shows);
    }

    [Fact]
    public void The_bank_read_ahead_of_time_waits_until_the_player_is_home()
    {
        var (game, _, feed) = Start();
        game.IsNoGame = true;
        feed.Refresh(isPanelOpen: false);
        game.IsNoGame = false;
        game.IsAway = true;
        feed.Refresh(isPanelOpen: false);
        Assert.Equal(0, game.Reads);

        game.IsAway = false;
        feed.Refresh(isPanelOpen: false);
        feed.Refresh(isPanelOpen: false);

        Assert.Equal(1, game.Reads);
    }

    /// <summary>The panel opened before any refresh already built the view, so no extra read follows.</summary>
    [Fact]
    public void A_panel_opened_first_leaves_nothing_to_read_ahead_of_time()
    {
        var (game, _, feed) = Start();

        OpenPanel(feed);
        feed.Refresh(isPanelOpen: false);

        Assert.Equal(1, game.Reads);
    }

    [Fact]
    public void Away_from_home_a_refresh_never_reads_the_bank()
    {
        var (game, page, feed) = Start();
        game.IsAway = true;
        game.Shop = new List<ShopRow> { new(0, Bed, 100) };

        for (var i = 0; i < 3; i++) feed.Refresh(isPanelOpen: false);

        Assert.Equal(0, game.Reads);
        Assert.Equal(0, game.Prints);
        Assert.Equal(new[] { false }, page.Lives);
    }

    [Fact]
    public void The_shop_closing_takes_the_bank_out_of_it_once()
    {
        var (game, page, feed) = Start();
        game.Shop = new List<ShopRow> { new(0, Bed, 100) };

        feed.Refresh(isPanelOpen: false);
        game.Shop = null;
        feed.Refresh(isPanelOpen: false);
        feed.Refresh(isPanelOpen: false);

        Assert.Equal(2, page.Shops.Count);
        Assert.Null(page.Shops[1]);
    }

    // ===== Real changes =====

    [Fact]
    public void A_package_landing_in_the_yard_redraws_the_open_panel()
    {
        var (game, page, feed) = Start();

        OpenPanel(feed);
        feed.Refresh(isPanelOpen: true);
        game.Add(Lamp);
        feed.Refresh(isPanelOpen: true);

        Assert.Contains("\"Lamp\"", Assert.Single(page.Shows));
    }

    [Fact]
    public void A_package_banked_outside_the_panel_redraws_it()
    {
        var (game, page, feed) = Start();

        OpenPanel(feed);
        BankRun.Store(game, Defaults, game.Home[0].Id);
        feed.Refresh(isPanelOpen: true);

        Assert.Contains("\"bank\":[{", Assert.Single(page.Shows));
    }

    [Fact]
    public void A_new_shop_row_is_sent_to_the_shop()
    {
        var (game, page, feed) = Start();
        game.Shop = new List<ShopRow>();

        feed.Refresh(isPanelOpen: false);
        game.Shop = new List<ShopRow> { new(0, Bed, 100) };
        feed.Refresh(isPanelOpen: false);

        Assert.Equal(2, page.Shops.Count);
        Assert.Contains("\"index\":0", page.Shops[1]);
    }

    [Fact]
    public void The_click_menu_opening_on_a_yard_package_sends_its_bank_entry()
    {
        var (game, page, feed) = Start();

        feed.Refresh(isPanelOpen: false);
        game.Menu = game.Home[0].Id;
        feed.Refresh(isPanelOpen: false);

        Assert.Equal(2, page.Shops.Count);
        Assert.Contains("\"menu\":{\"why\":null}", page.Shops[1]);
    }

    [Fact]
    public void After_a_press_the_view_is_read_fresh_even_with_the_print_unchanged()
    {
        var (game, _, feed) = Start();

        OpenPanel(feed);
        var reads = game.Reads;
        feed.Forget();
        feed.Refresh(isPanelOpen: true);

        Assert.Equal(reads + 1, game.Reads);
    }

    // ===== Save load and language =====

    [Fact]
    public void Another_save_loaded_drops_the_view_and_shows_its_yard()
    {
        var (game, page, feed) = Start();

        OpenPanel(feed);
        game.SaveId = 2;
        game.Home.Clear();
        game.Add(Table);
        feed.Refresh(isPanelOpen: true);

        var shown = Assert.Single(page.Shows);
        Assert.Contains("\"Table\"", shown);
        Assert.DoesNotContain("\"Bed\"", shown);
    }

    [Fact]
    public void A_language_switch_reads_and_redraws_everything_even_when_the_view_is_the_same()
    {
        var (game, page, feed) = Start();
        game.Shop = new List<ShopRow> { new(0, Bed, 100) };

        OpenPanel(feed);
        feed.Refresh(isPanelOpen: true);
        var reads = game.Reads;
        var lives = page.Lives.Count;
        var shops = page.Shops.Count;
        var shows = page.Shows.Count;

        game.IsChinese = true;
        feed.Refresh(isPanelOpen: true);

        Assert.Equal(reads + 1, game.Reads);
        Assert.Equal(lives + 1, page.Lives.Count);
        Assert.Equal(shops + 1, page.Shops.Count);
        Assert.Equal(shows + 1, page.Shows.Count);
    }

    [Fact]
    public void A_language_switch_drops_the_view_even_if_the_print_did_not_move()
    {
        var game = new PrintlessGame();
        var page = new FakePage();
        var feed = new BankFeed(game, page, Defaults, _ => { });

        game.Inner.Add(Bed);
        OpenPanel(feed);
        var reads = game.Inner.Reads;

        game.Inner.IsChinese = true;
        feed.Refresh(isPanelOpen: true);

        Assert.Equal(reads + 1, game.Inner.Reads);
    }

    [Fact]
    public void New_limits_rebuild_the_view()
    {
        var (game, page, feed) = Start();

        OpenPanel(feed);
        feed.UseLimits(Defaults with { Large = 9 });
        feed.Refresh(isPanelOpen: true);

        Assert.Contains("\"cap\":9", Assert.Single(page.Shows));
    }

    [Fact]
    public void A_reloaded_page_gets_everything_again()
    {
        var (game, page, feed) = Start();
        game.Shop = new List<ShopRow> { new(0, Bed, 100) };

        OpenPanel(feed);
        feed.Refresh(isPanelOpen: true);
        var reads = game.Reads;
        feed.PageReloaded();
        feed.Refresh(isPanelOpen: true);

        Assert.Equal(2, page.Lives.Count);
        Assert.Equal(2, page.Shops.Count);
        Assert.Single(page.Shows);
        Assert.Equal(reads, game.Reads);
    }

    [Fact]
    public void A_call_the_page_missed_goes_again_next_refresh()
    {
        var game = new FakeGame();
        var page = new DeafPage();
        var feed = new BankFeed(game, page, Defaults, _ => { });

        feed.Refresh(isPanelOpen: false);
        page.IsDeaf = false;
        feed.Refresh(isPanelOpen: false);

        Assert.Equal(new[] { true }, page.Lives);
    }

    // ===== The panel closing =====

    [Fact]
    public void The_open_panel_closes_when_the_player_leaves_home()
    {
        var (game, _, feed) = Start();

        OpenPanel(feed);
        game.IsAway = true;

        Assert.False(feed.Refresh(isPanelOpen: true));
    }

    [Fact]
    public void Away_from_home_the_panel_does_not_open()
    {
        var (game, _, feed) = Start();
        game.IsAway = true;

        Assert.Null(feed.View());
    }

    /// <summary>A game whose print never moves, so only the language switch itself can drop the view.</summary>
    private sealed class PrintlessGame : IBankGame
    {
        public FakeGame Inner { get; } = new();

        public BankState State() => Inner.State();

        public long Print() => 7;

        public BankRead Read() => Inner.Read();

        public bool Store(HomePackage package, BankEntry entry, int? shopIndex) => Inner.Store(package, entry, shopIndex);

        public long TakeOut(BankEntry entry) => Inner.TakeOut(entry);
    }

    /// <summary>A page that is not there yet: its calls do not land until IsDeaf is off.</summary>
    private sealed class DeafPage : IBankPage
    {
        public bool IsDeaf { get; set; } = true;

        public List<bool> Lives { get; } = new();

        public bool Live(bool isLive)
        {
            if (IsDeaf) return false;

            Lives.Add(isLive);

            return true;
        }

        public bool Shop(string? viewJson) => !IsDeaf;

        public void Show(string viewJson)
        {
        }
    }
}

public class KeyedMemoTests
{
    [Fact]
    public void The_same_scope_looks_each_id_up_once()
    {
        var made = 0;
        var memo = new KeyedMemo<int, string>(id => { made++; return "row " + id; });

        Assert.Equal("row 4", memo.Get(1, 4));
        Assert.Equal("row 4", memo.Get(1, 4));
        Assert.Equal("row 5", memo.Get(1, 5));

        Assert.Equal(2, made);
    }

    [Fact]
    public void A_new_scope_forgets_every_id()
    {
        var language = "en";
        var memo = new KeyedMemo<int, string>(id => language + id);

        memo.Get(1, 4);
        memo.Get(1, 5);
        language = "zh";

        Assert.Equal("zh4", memo.Get(2, 4));
        Assert.Equal(1, memo.Count);
    }

    [Fact]
    public void A_tuple_scope_changes_when_any_part_does()
    {
        var made = 0;
        var memo = new KeyedMemo<(long Tables, bool IsChinese), int>(id => ++made);

        memo.Get((10, false), 1);
        memo.Get((10, true), 1);
        memo.Get((11, true), 1);
        memo.Get((11, true), 1);

        Assert.Equal(3, made);
    }

    [Fact]
    public void A_print_moves_with_each_number_and_its_order()
    {
        Assert.NotEqual(PrintOf(1, 2), PrintOf(2, 1));
        Assert.NotEqual(PrintOf(1, 2), PrintOf(1, 3));
        Assert.Equal(PrintOf(1, 2), PrintOf(1, 2));
    }

    [Fact]
    public void Contents_with_the_same_entries_and_bonus_are_the_same()
    {
        var counters = new Dictionary<string, int>();

        foreach (var (key, value) in BankCounters.Write(new BankEntry(0, FakeGame.Bed, 3, 450, FakeGame.ShopId, true))) counters[key] = value;

        Assert.True(BankCounters.Read(counters).SameAs(BankCounters.Read(new Dictionary<string, int>(counters))));
        Assert.False(BankCounters.Read(counters).SameAs(BankCounters.Read(new Dictionary<string, int>())));
    }

    private static long PrintOf(long first, long second)
    {
        var print = new ChangePrint();
        print.Add(first);
        print.Add(second);

        return print.Value;
    }
}
