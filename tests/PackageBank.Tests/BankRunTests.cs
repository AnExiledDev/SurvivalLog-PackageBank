using PackageBank.Core;
using Xunit;
using static PackageBank.Tests.FakeGame;

namespace PackageBank.Tests;

public class BankRunTests
{
    private static readonly BankLimits Defaults = new(IsLimited: true, Large: 4, Medium: 6, Small: 8);

    private static Bank BankOf(FakeGame game) => Assert.IsType<BankRead.Ready>(game.Read()).Bank;

    // ===== Banking =====

    [Fact]
    public void Banking_a_yard_package_takes_it_off_the_yard_into_the_bank_and_frees_its_spot()
    {
        var game = new FakeGame();
        var bed = game.Add(Bed);

        var outcome = BankRun.Store(game, Defaults, bed.Id);

        Assert.Equal(new BankOutcome.Stored("Bed"), outcome);
        Assert.Empty(game.Home);
        Assert.Equal(4, game.FreeSpots);
        Assert.Equal(Bed, Assert.Single(BankOf(game).Banked).Entry.ConfigId);
    }

    [Fact]
    public void A_package_taken_out_comes_back_with_its_color_and_the_price_the_shop_refunds()
    {
        var game = new FakeGame();
        var bed = game.Add(Bed, paid: 450, dye: 3);

        BankRun.Store(game, Defaults, bed.Id);
        var outcome = BankRun.TakeOut(game, BankOf(game).Banked[0].Entry.Slot);

        var back = Assert.Single(game.Home);
        Assert.Equal(new BankOutcome.TakenOut("Bed"), outcome);
        Assert.Equal((Bed, 3, 450, ShopId, true), (back.ConfigId, back.Dye, back.Paid, back.Source, back.IsPurchased));
        Assert.Empty(BankOf(game).Banked);
        Assert.Empty(game.Counters);
    }

    [Fact]
    public void A_looted_package_stays_unrefundable_after_a_trip_through_the_bank()
    {
        var game = new FakeGame();
        var lamp = game.Add(Lamp, isPurchased: false);

        BankRun.Store(game, Defaults, lamp.Id);
        BankRun.TakeOut(game, BankOf(game).Banked[0].Entry.Slot);

        Assert.False(Assert.Single(game.Home).IsPurchased);
    }

    [Fact]
    public void A_size_at_its_limit_refuses_another_package_and_leaves_it_in_the_yard()
    {
        var game = new FakeGame();
        var limits = Defaults with { Large = 1 };
        BankRun.Store(game, limits, game.Add(Bed).Id);
        var second = game.Add(Bed);

        var outcome = BankRun.Store(game, limits, second.Id);

        Assert.Equal(new BankOutcome.Full(PackageSize.Large, 1), outcome);
        Assert.Contains(second, game.Home);
        Assert.Single(BankOf(game).Banked);
    }

    [Fact]
    public void A_full_size_does_not_stop_another_size_going_in()
    {
        var game = new FakeGame();
        var limits = Defaults with { Large = 0 };

        Assert.IsType<BankOutcome.Stored>(BankRun.Store(game, limits, game.Add(Table).Id));
    }

    [Fact]
    public void With_limits_off_a_size_takes_any_number()
    {
        var game = new FakeGame();
        var limits = new BankLimits(IsLimited: false, Large: 0, Medium: 0, Small: 0);

        BankRun.Store(game, limits, game.Add(Bed).Id);

        Assert.IsType<BankOutcome.Stored>(BankRun.Store(game, limits, game.Add(Bed).Id));
    }

    [Fact]
    public void Another_mod_raising_the_bonus_counter_lets_the_size_hold_more()
    {
        var game = new FakeGame();
        var limits = Defaults with { Large = 1 };
        game.Counters[BankCounters.BonusLarge] = 1;
        BankRun.Store(game, limits, game.Add(Bed).Id);

        Assert.IsType<BankOutcome.Stored>(BankRun.Store(game, limits, game.Add(Bed).Id));
        Assert.IsType<BankOutcome.Full>(BankRun.Store(game, limits, game.Add(Bed).Id));
    }

    [Fact]
    public void Story_items_have_no_limit_even_with_every_size_set_to_zero()
    {
        var game = new FakeGame();
        var limits = new BankLimits(IsLimited: true, Large: 0, Medium: 0, Small: 0);

        BankRun.Store(game, limits, game.Add(Recorder, isPurchased: false).Id);

        Assert.IsType<BankOutcome.Stored>(BankRun.Store(game, limits, game.Add(Recorder, isPurchased: false).Id));
    }

    [Fact]
    public void A_package_being_moved_or_installed_is_never_banked()
    {
        var game = new FakeGame();
        var bed = game.Add(Bed, isBusy: true);

        Assert.Equal(new BankOutcome.Busy("Bed"), BankRun.Store(game, Defaults, bed.Id));
        Assert.Contains(bed, game.Home);
    }

    [Fact]
    public void A_package_of_no_bank_size_is_refused()
    {
        var game = new FakeGame();
        var barricade = game.Add(Barricade);

        Assert.Equal(new BankOutcome.NotBankable("Barricade"), BankRun.Store(game, Defaults, barricade.Id));
    }

    [Fact]
    public void A_package_the_game_would_not_remove_is_reported_as_a_failure_and_not_banked()
    {
        var game = new FakeGame { IsStoreBroken = true };
        var bed = game.Add(Bed);

        Assert.IsType<BankOutcome.Failed>(BankRun.Store(game, Defaults, bed.Id));
        Assert.Contains(bed, game.Home);
        Assert.Empty(BankOf(game).Banked);
    }

    [Fact]
    public void A_package_gone_since_the_panel_drew_it_is_reported_gone()
    {
        Assert.IsType<BankOutcome.Gone>(BankRun.Store(new FakeGame(), Defaults, 4242));
    }

    [Fact]
    public void Away_from_home_or_with_no_game_nothing_is_banked()
    {
        var away = new FakeGame();
        var bed = away.Add(Bed);
        away.IsAway = true;

        Assert.IsType<BankOutcome.NotHome>(BankRun.Store(away, Defaults, bed.Id));
        Assert.IsType<BankOutcome.NoGame>(BankRun.Store(new FakeGame { IsNoGame = true }, Defaults, 1));
        Assert.Contains(bed, away.Home);
    }

    [Fact]
    public void Packages_keep_the_order_they_went_in()
    {
        var game = new FakeGame();
        BankRun.Store(game, Defaults, game.Add(Bed).Id);
        BankRun.Store(game, Defaults, game.Add(Lamp).Id);

        Assert.Equal(new[] { Bed, Lamp }, BankOf(game).Banked.Select(banked => banked.Entry.ConfigId));
    }

    // ===== Taking out =====

    [Fact]
    public void With_no_free_yard_spot_a_package_stays_in_the_bank()
    {
        var game = new FakeGame();
        BankRun.Store(game, Defaults, game.Add(Bed).Id);
        game.FreeSpots = 0;

        Assert.IsType<BankOutcome.NoSpot>(BankRun.TakeOut(game, BankOf(game).Banked[0].Entry.Slot));
        Assert.Single(BankOf(game).Banked);
    }

    [Fact]
    public void Taking_out_a_slot_that_is_no_longer_there_is_reported_gone()
    {
        Assert.IsType<BankOutcome.Gone>(BankRun.TakeOut(new FakeGame(), 7));
    }

    // ===== Banking from the build shop's list =====

    [Fact]
    public void A_shop_row_banks_the_package_bought_at_that_rows_price()
    {
        var game = new FakeGame();
        game.Add(Bed, paid: 300);
        var dearer = game.Add(Bed, paid: 450);
        game.Shop = new List<ShopRow> { new(0, Bed, 300), new(1, Bed, 450) };

        var outcome = BankRun.StoreShopRow(game, Defaults, 1);

        Assert.IsType<BankOutcome.Stored>(outcome);
        Assert.DoesNotContain(dearer, game.Home);
        Assert.Equal(450, BankOf(game).Banked[0].Entry.Paid);
        Assert.Equal(new ShopRow(0, Bed, 300), Assert.Single(game.Shop));
    }

    [Fact]
    public void A_shop_row_passes_over_a_package_being_carried()
    {
        var game = new FakeGame();
        var carried = game.Add(Lamp, paid: 40, isBusy: true);
        game.Add(Lamp, paid: 40);
        game.Shop = new List<ShopRow> { new(0, Lamp, 40) };

        Assert.IsType<BankOutcome.Stored>(BankRun.StoreShopRow(game, Defaults, 0));
        Assert.Equal(new[] { carried }, game.Home);
    }

    [Fact]
    public void A_shop_row_that_is_gone_banks_nothing()
    {
        var game = new FakeGame();
        game.Add(Bed);
        game.Shop = new List<ShopRow> { new(0, Bed, 100) };

        Assert.IsType<BankOutcome.Gone>(BankRun.StoreShopRow(game, Defaults, 3));
        Assert.Single(game.Home);
    }

    // ===== The click menu =====

    [Fact]
    public void Bank_in_the_click_menu_banks_the_package_the_menu_is_open_on()
    {
        var game = new FakeGame();
        game.Add(Lamp);
        var bed = game.Add(Bed);
        game.Menu = bed.Id;

        var outcome = BankRun.StoreMenu(game, Defaults);

        Assert.Equal(new BankOutcome.Stored("Bed"), outcome);
        Assert.Equal(Lamp, Assert.Single(game.Home).ConfigId);
    }

    [Fact]
    public void Bank_in_a_click_menu_that_has_already_shut_banks_nothing()
    {
        var game = new FakeGame();
        game.Add(Bed);

        var outcome = BankRun.StoreMenu(game, Defaults);

        Assert.Equal(new BankOutcome.Gone(), outcome);
        Assert.Single(game.Home);
    }
}
