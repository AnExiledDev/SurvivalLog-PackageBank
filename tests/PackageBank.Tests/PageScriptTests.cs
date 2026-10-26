using PackageBank.Core;
using Xunit;
using static PackageBank.Tests.FakeGame;

namespace PackageBank.Tests;

/// <summary>page.js fed the real core's view the way the plugin feeds it: the panel, the HUD button and the shop's bank.</summary>
public class PageScriptTests
{
    private static readonly BankLimits Defaults = new(IsLimited: true, Large: 4, Medium: 6, Small: 8, Defense: 4);

    private static string ViewOf(FakeGame game, BankLimits? limits = null) => BankView.Of(game.Read(), limits ?? Defaults) ?? throw new InvalidOperationException("no view");

    /// <summary>A yard with a bed and a lamp bought from the shop, and a table already in the bank.</summary>
    private static FakeGame Yard(out HomePackage bed, out HomePackage lamp)
    {
        var game = new FakeGame();
        BankRun.Store(game, Defaults, game.Add(Table).Id);
        bed = game.Add(Bed, paid: 450);
        lamp = game.Add(Lamp, paid: 40);
        game.Shop = new List<ShopRow> { new(0, Bed, 450), new(1, Lamp, 40) };

        return game;
    }

    /// <summary>The shop open on the yard's two rows, with the bank's view sent and one page check run.</summary>
    private static PageScript InShop(FakeGame game, BankLimits? limits = null)
    {
        var page = new PageScript();

        page.Run("openShop(['Bed', 'Lamp'])");
        page.Shop(ViewOf(game, limits));
        page.Poll();

        return page;
    }

    // ===== The panel =====

    private const string Panel = "document.getElementById('pb-root').textContent";
    private const string YardList = "document.querySelectorAll('.pb-list')[0]";
    private const string BankList = "document.querySelectorAll('.pb-list')[1]";

    [Fact]
    public void The_panel_lists_the_yard_and_the_bank_and_a_bank_press_names_the_package()
    {
        var game = Yard(out var bed, out _);
        var page = new PageScript();

        page.Open(ViewOf(game));
        page.Click($"panelButton('store', '{bed.Id}')");

        Assert.Equal(new PageCommand.Store(bed.Id), Assert.Single(page.Sent));
        Assert.True(page.Is("panelButton('takeout', 0)"));
        Assert.Contains("Table", page.Text($"{BankList}.textContent"));
        Assert.Contains("2 in the yard, 1 in the bank", page.Text(Panel));
    }

    [Fact]
    public void Each_shelf_card_shows_what_is_banked_on_it_against_its_limit_with_a_pip_per_place()
    {
        var page = new PageScript();

        page.Open(ViewOf(Yard(out _, out _)));

        Assert.Equal("Medium1 / 6", page.Text("panelButton('filter', 'medium').textContent"));
        Assert.Equal(6, page.Count("panelButton('filter', 'medium').querySelectorAll('.pb-pip')"));
        Assert.Equal(1, page.Count("panelButton('filter', 'medium').querySelectorAll('.pb-fill')"));
        Assert.Equal("Story Items0No limit", page.Text("panelButton('filter', 'story').textContent"));
    }

    [Fact]
    public void A_shelf_with_a_big_limit_draws_one_bar_instead_of_a_pip_per_place()
    {
        var page = new PageScript();

        page.Open(ViewOf(Yard(out _, out _), Defaults with { Medium = 40 }));

        Assert.Equal(2, page.Count("panelButton('filter', 'medium').querySelectorAll('.pb-pip')"));
        Assert.Equal("1", page.Text("panelButton('filter', 'medium').querySelector('.pb-fill').style.flex"));
    }

    [Fact]
    public void With_limits_off_no_shelf_has_a_limit()
    {
        var page = new PageScript();

        page.Open(ViewOf(Yard(out _, out _), Defaults with { IsLimited = false }));

        Assert.Equal(5, page.Count("document.querySelectorAll('.pb-nolimit')"));
        Assert.Equal(0, page.Count("document.querySelectorAll('.pb-pip')"));
    }

    [Fact]
    public void Both_lists_are_grouped_by_shelf_in_the_shelf_cards_order()
    {
        var game = Yard(out _, out _);
        game.Add(Sandbags);
        game.Add(Bed);
        var page = new PageScript();

        page.Open(ViewOf(game));

        Assert.Equal("Large|Small|Defenses", page.Text($"{YardList}.querySelectorAll('.pb-grp').map(function (g) {{ return g.textContent; }}).join('|')"));
        Assert.Equal(2, page.Count($"{YardList}.querySelectorAll('.pb-row').filter(function (r) {{ return r.querySelector('.pb-name').textContent === 'Bed'; }})"));
        Assert.Equal("Bed", page.Text($"{YardList}.querySelectorAll('.pb-row')[1].querySelector('.pb-name').textContent"));
    }

    [Fact]
    public void A_full_shelf_greys_its_yard_packages_says_so_once_on_the_group_and_sends_nothing()
    {
        var game = Yard(out var bed, out _);
        game.Add(Bed);
        var page = new PageScript();

        page.Open(ViewOf(game, Defaults with { Large = 0 }));
        page.Click($"panelButton('store', '{bed.Id}')");

        Assert.Empty(page.Sent);
        Assert.True(page.Is($"panelButton('store', '{bed.Id}').disabled"));
        Assert.Equal("Largebank full", page.Text($"{YardList}.querySelector('.pb-grp').textContent"));
        Assert.Equal(1, page.Count("document.querySelectorAll('.pb-grp-why')"));
        Assert.Equal(0, page.Count("document.querySelectorAll('.pb-why')"));
        Assert.True(page.Is("panelButton('filter', 'large').classList.contains('pb-full')"));
    }

    [Fact]
    public void A_package_refused_for_a_reason_of_its_own_says_why_under_its_name()
    {
        var game = Yard(out _, out _);
        var busy = game.Add(Lamp, isBusy: true);
        var page = new PageScript();

        page.Open(ViewOf(game));
        page.Click($"panelButton('store', '{busy.Id}')");

        Assert.Empty(page.Sent);
        Assert.Equal("Being moved or installed", page.Text("document.querySelector('.pb-why').textContent"));
        Assert.Equal(1, page.Count("document.querySelectorAll('.pb-why')"));
    }

    [Fact]
    public void With_no_free_spot_the_bank_list_says_so_once_and_every_take_out_is_greyed()
    {
        var game = Yard(out _, out _);
        BankRun.Store(game, Defaults, game.Add(Table).Id);
        game.FreeSpots = 0;
        var page = new PageScript();

        page.Open(ViewOf(game));
        page.Click("panelButton('takeout', 0)");
        page.Click("panelButton('takeout', 1)");

        Assert.Empty(page.Sent);
        Assert.Equal(1, page.Count($"{BankList}.querySelectorAll('.pb-notice')"));
        Assert.Contains("Install, refund or bank a package to take one out.", page.Text($"{BankList}.textContent"));
        Assert.True(page.Is("panelButton('takeout', 1).disabled"));
        Assert.Contains("0 free package spots in the yard", page.Text(Panel));
        Assert.True(page.Is("document.querySelector('.pb-spots').classList.contains('pb-bad')"));
    }

    [Fact]
    public void One_free_spot_is_counted_in_the_singular()
    {
        var game = Yard(out _, out _);
        game.FreeSpots = 1;
        var page = new PageScript();

        page.Open(ViewOf(game));

        Assert.Equal("1 free package spot in the yard", page.Text("document.querySelector('.pb-spots').textContent"));
    }

    [Fact]
    public void A_shelf_card_press_shows_only_that_shelf_in_both_lists_and_a_second_press_shows_all_again()
    {
        var page = new PageScript();
        page.Open(ViewOf(Yard(out _, out _)));

        page.Click("panelButton('filter', 'large')");

        Assert.Empty(page.Sent);
        Assert.Contains("Bed", page.Text($"{YardList}.textContent"));
        Assert.DoesNotContain("Lamp", page.Text($"{YardList}.textContent"));
        Assert.Equal("Nothing from the Large shelf here.", page.Text($"{BankList}.textContent"));
        Assert.Contains("Showing Large only", page.Text(Panel));
        Assert.True(page.Is("panelButton('filter', 'large').classList.contains('pb-on')"));

        page.Click("panelButton('filter', 'large')");

        Assert.Contains("Lamp", page.Text($"{YardList}.textContent"));
        Assert.Contains("Table", page.Text($"{BankList}.textContent"));
        Assert.False(page.Is("panelButton('showall')"));
    }

    [Fact]
    public void Show_all_clears_the_shelf_filter_and_a_new_bank_view_keeps_it()
    {
        var game = Yard(out var bed, out _);
        var page = new PageScript();
        page.Open(ViewOf(game));
        page.Click("panelButton('filter', 'small')");

        page.Run($"window.__slpkgbank.setData({ViewOf(game)})");

        Assert.DoesNotContain("Bed", page.Text($"{YardList}.textContent"));

        page.Click("panelButton('showall')");

        Assert.Contains("Bed", page.Text($"{YardList}.textContent"));
        Assert.Empty(page.Sent);
    }

    [Fact]
    public void Empty_lists_say_what_to_do_next()
    {
        var page = new PageScript();

        page.Open(ViewOf(new FakeGame()));

        Assert.Contains("Packages you buy in the build shop land here.", page.Text($"{YardList}.textContent"));
        Assert.Contains("Press Bank on a package in the yard to store it here.", page.Text($"{BankList}.textContent"));
        Assert.Equal(0, page.Count("document.querySelectorAll('.pb-notice')"));
    }

    [Fact]
    public void A_second_press_waits_for_the_answer_to_the_first()
    {
        var game = Yard(out var bed, out var lamp);
        var page = new PageScript();
        page.Open(ViewOf(game));

        page.Click($"panelButton('store', '{bed.Id}')");
        page.Click($"panelButton('store', '{lamp.Id}')");
        page.Status("Bed put in the bank.");
        page.Click($"panelButton('store', '{lamp.Id}')");

        Assert.Equal(new PageCommand[] { new PageCommand.Store(bed.Id), new PageCommand.Store(lamp.Id) }, page.Sent);
    }

    [Fact]
    public void The_panel_takes_the_words_the_plugin_sends_for_a_chinese_game()
    {
        var page = new PageScript();

        page.Run(PageWords.Script(isChinese: true));
        page.Open(ViewOf(new FakeGame()));

        Assert.Contains("院子里没有包裹。", page.Text(Panel));
        Assert.Contains("院子里 0 个，仓库中 0 个", page.Text(Panel));
        Assert.Contains("无上限", page.Text(Panel));
    }

    // ===== The HUD button =====

    [Fact]
    public void At_home_the_HUD_gets_a_button_that_opens_the_panel_and_it_goes_when_the_player_leaves()
    {
        var page = new PageScript();
        page.Run("openHud()");

        page.Live(true);
        page.Click("hudButton()");

        Assert.Equal(new PageCommand.Toggle(), Assert.Single(page.Sent));

        page.Live(false);

        Assert.False(page.Is("hudButton()"));
    }

    [Fact]
    public void The_HUD_button_carries_its_place_for_the_toolkits_four_high_layout_in_both_HUDs()
    {
        var page = new PageScript();
        page.Run("openHud(); openPrepHud()");

        page.Live(true);

        Assert.Equal("20", page.Text("hudButton().getAttribute('data-etk-dock')"));
        Assert.Equal("20", page.Text("prepHudButton().getAttribute('data-etk-dock')"));
    }

    [Fact]
    public void In_the_countdown_before_the_disaster_the_HUD_gets_the_same_button_in_that_HUDs_own_markup()
    {
        var page = new PageScript();
        page.Run("openPrepHud()");

        page.Live(true);
        page.Click("prepHudButton().querySelector('button')");

        Assert.Equal(new PageCommand.Toggle(), Assert.Single(page.Sent));
        Assert.True(page.Is("prepHudButton().classList.contains('tooltip-wrapper')"));
        Assert.Equal("Package Bank", page.Text("prepHudButton().querySelector('.tip-name').textContent"));
    }

    // ===== The build shop =====

    [Fact]
    public void Each_shop_row_gets_a_bank_button_after_its_refund_button_that_banks_that_row()
    {
        var page = InShop(Yard(out _, out _));

        page.Click("shopBankButton(1)");

        Assert.Equal("Bank", page.Text("shopRows()[1].children[2].textContent"));
        Assert.Equal("Refund", page.Text("shopRows()[1].children[1].textContent"));
        Assert.Equal(new PageCommand.StoreShopRow(1), Assert.Single(page.Sent));
    }

    [Fact]
    public void A_shop_row_the_bank_would_refuse_is_greyed_and_sends_nothing()
    {
        var page = InShop(Yard(out _, out _), Defaults with { Large = 0 });

        page.Click("shopBankButton(0)");

        Assert.True(page.Is("shopBankButton(0).disabled"));
        Assert.Empty(page.Sent);
    }

    [Fact]
    public void A_shop_row_with_no_bought_yard_package_behind_it_gets_no_bank_button()
    {
        var game = Yard(out _, out _);
        game.Shop!.Add(new ShopRow(2, Recorder, 0));
        var page = new PageScript();
        page.Run("openShop(['Bed', 'Lamp', 'Recorder'])");

        page.Shop(ViewOf(game));
        page.Poll();

        Assert.True(page.Is("shopBankButton(1)"));
        Assert.False(page.Is("shopBankButton(2)"));
    }

    [Fact]
    public void Banked_packages_are_listed_under_the_shops_rows_and_can_be_taken_out_there()
    {
        var page = InShop(Yard(out _, out _));

        page.Click("shopTakeOut(0)");

        Assert.Contains("Table", page.Text("shopBank().textContent"));
        Assert.True(page.Is("shopPage().querySelector('.left-content-wrapper').children[2] === shopBank()"));
        Assert.Equal(new PageCommand.TakeOut(0), Assert.Single(page.Sent));
    }

    [Fact]
    public void With_no_free_spot_the_shops_take_out_is_greyed_with_the_reason()
    {
        var game = Yard(out _, out _);
        game.FreeSpots = 0;
        var page = InShop(game);

        page.Click("shopTakeOut(0)");

        Assert.Empty(page.Sent);
        Assert.Contains("No free package spot", page.Text("shopBank().textContent"));
    }

    [Fact]
    public void A_row_the_shop_adds_after_a_buy_gets_its_button_and_the_bank_stays_under_the_rows()
    {
        var game = Yard(out _, out _);
        var page = InShop(game);
        game.Add(Lamp, paid: 40);
        game.Shop!.Add(new ShopRow(2, Lamp, 40));

        page.Run("addShopRow('Lamp')");
        page.Shop(ViewOf(game));
        page.Poll();

        Assert.True(page.Is("shopBankButton(2)"));
        Assert.True(page.Is("shopPage().querySelector('.left-content-wrapper').children[3] === shopBank()"));
    }

    [Fact]
    public void When_the_shop_closes_everything_the_bank_added_comes_back_out()
    {
        var page = InShop(Yard(out _, out _));

        page.Shop(null);

        Assert.False(page.Is("shopBank()"));
        Assert.Equal(0, page.Count("shopPage().querySelectorAll('.pb-bank-btn')"));
    }

    [Fact]
    public void A_banked_packages_icon_in_the_shop_is_read_from_the_web_folder_not_the_shops_own_folder()
    {
        var game = Yard(out _, out _);
        game.Icons[Table] = "Res/Furniture/table.png";
        var page = InShop(game);

        Assert.Equal("https://game.local/WebUI/Res/Furniture/table.png", page.Text("shopBank().querySelector('img').src"));
    }

    // ===== The click menu =====

    /// <summary>The game's click menu open on a yard package, with the bank's view sent and one page check run.</summary>
    private static PageScript InMenu(FakeGame game, HomePackage package, BankLimits? limits = null)
    {
        var page = new PageScript();
        game.Menu = package.Id;

        page.Run("openMenu(['Install', 'Return'])");
        page.Shop(ViewOf(game, limits));
        page.Poll();

        return page;
    }

    [Fact]
    public void The_click_menu_on_a_yard_package_gets_a_bank_entry_that_banks_what_the_menu_is_open_on()
    {
        var game = Yard(out var bed, out _);
        var page = InMenu(game, bed);

        page.Click("menuBank()");

        Assert.Equal(3, page.Count("menuEntries()"));
        Assert.Equal("Bank", page.Text("menuBank().querySelector('.menu-btn-text').textContent"));
        Assert.Equal(new PageCommand.StoreMenu(), Assert.Single(page.Sent));
    }

    [Fact]
    public void A_click_menu_bank_entry_the_bank_would_refuse_is_locked_with_its_reason_and_sends_nothing()
    {
        var game = Yard(out var bed, out _);
        var page = InMenu(game, bed, Defaults with { Large = 0 });

        page.Click("menuBank()");

        Assert.True(page.Is("menuBank().classList.contains('locked')"));
        Assert.Contains("Bank full for this size", page.Text("menuBank().textContent"));
        Assert.Empty(page.Sent);
    }

    [Fact]
    public void The_click_menu_bank_entry_goes_when_the_menu_is_no_longer_on_a_package()
    {
        var game = Yard(out var bed, out _);
        var page = InMenu(game, bed);
        game.Menu = null;

        page.Shop(ViewOf(game));

        Assert.False(page.Is("menuBank()"));
        Assert.Equal(2, page.Count("menuEntries()"));
    }

    [Fact]
    public void A_click_menu_drawn_after_the_view_arrived_gets_its_bank_entry_last_without_waiting_for_the_page_check()
    {
        var game = Yard(out var bed, out _);
        var page = new PageScript();
        page.Run("openMenu(['Install', 'Return'])");
        page.Poll();
        page.Run("pageOf(/SelectMenu/).getElementById('menu-container').textContent = ''");
        game.Menu = bed.Id;
        page.Shop(ViewOf(game));

        Assert.False(page.Is("menuBank()"));

        page.Run("openMenu(['Install', 'Return']); settle()");

        Assert.True(page.Is("menuBank()"));
        Assert.Equal("Bank", page.Text("menuEntries()[2].querySelector('.menu-btn-text').textContent"));
    }

    [Fact]
    public void A_click_menu_page_made_after_the_view_arrived_gets_its_bank_entry_as_the_game_draws_it_without_waiting_for_the_page_check()
    {
        var game = Yard(out var bed, out _);
        var page = new PageScript();
        game.Menu = bed.Id;
        page.Shop(ViewOf(game));
        page.Run("frame('https://game.local/UI/SelectMenu/SelectMenu.html'); settle()");

        page.Run("openMenu(['Install', 'Return']); settle()");

        Assert.True(page.Is("menuBank()"));
        Assert.Equal("Bank", page.Text("menuEntries()[2].querySelector('.menu-btn-text').textContent"));
    }

    [Fact]
    public void A_click_menu_the_game_draws_again_gets_its_bank_entry_again()
    {
        var game = Yard(out var bed, out _);
        var page = InMenu(game, bed);

        page.Run("openMenu(['Install', 'Return'])");
        page.Poll();

        Assert.True(page.Is("menuBank()"));
        Assert.Equal(3, page.Count("menuEntries()"));
    }
}
