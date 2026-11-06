using PackageBank.Core;
using Xunit;
using static PackageBank.Tests.FakeGame;

namespace PackageBank.Tests;

public class PackRunTests
{
    private static readonly BankLimits Defaults = new(IsLimited: true, Large: 4, Medium: 6, Small: 8, Defense: 4);

    private static Bank BankOf(FakeGame game) => Assert.IsType<BankRead.Ready>(game.Read()).Bank;

    [Fact]
    public void Pack_up_turns_the_placed_piece_into_a_yard_package_in_its_color_that_the_shop_refunds_at_its_own_price()
    {
        var game = new FakeGame();
        var bed = game.OpenMenuOnPiece(Bed, dye: 4);

        var outcome = PackRun.PackMenu(game, Defaults);

        var package = Assert.Single(game.Home);
        Assert.Equal(new BankOutcome.PackedUp("Bed", IsIntoBank: false, IsStory: false), outcome);
        Assert.Equal(bed, Assert.Single(game.TakenDown));
        Assert.Equal((Bed, 4, true, PackRun.PaidUnknown, 0L), (package.ConfigId, package.Dye, package.IsPurchased, package.Paid, package.Source));
        Assert.Equal(2, game.FreeSpots);
    }

    [Fact]
    public void With_no_free_yard_spot_the_piece_goes_straight_into_the_bank_and_comes_out_refundable_in_its_color()
    {
        var game = new FakeGame { FreeSpots = 0 };
        game.OpenMenuOnPiece(Table, dye: 2);

        var outcome = PackRun.PackMenu(game, Defaults);
        game.FreeSpots = 1;
        BankRun.TakeOut(game, BankOf(game).Banked[0].Entry.Slot);

        var back = Assert.Single(game.Home);
        Assert.Equal(new BankOutcome.PackedUp("Table", IsIntoBank: true, IsStory: false), outcome);
        Assert.Equal((Table, 2, true, PackRun.PaidUnknown), (back.ConfigId, back.Dye, back.IsPurchased, back.Paid));
    }

    [Fact]
    public void A_storage_piece_that_still_holds_items_is_refused_and_stays_where_it_is()
    {
        var game = new FakeGame();
        game.OpenMenuOnPiece(Table, holdsItems: true);

        var outcome = PackRun.PackMenu(game, Defaults);

        Assert.Equal(new BankOutcome.HoldsItems("Table"), outcome);
        Assert.Empty(game.TakenDown);
        Assert.Empty(game.Home);
    }

    [Fact]
    public void A_piece_the_game_will_not_take_down_is_refused_with_the_games_own_reason()
    {
        var game = new FakeGame();
        game.OpenMenuOnPiece(Lamp, block: "Turn it off first");

        var outcome = PackRun.PackMenu(game, Defaults);

        Assert.Equal(new BankOutcome.Blocked("Lamp", "Turn it off first"), outcome);
        Assert.Empty(game.TakenDown);
    }

    [Fact]
    public void With_the_yard_full_and_the_pieces_shelf_full_nothing_is_packed()
    {
        var game = new FakeGame { FreeSpots = 0 };
        game.OpenMenuOnPiece(Bed);

        var outcome = PackRun.PackMenu(game, Defaults with { Large = 0 });

        Assert.Equal(new BankOutcome.NoRoom("Bed"), outcome);
        Assert.Empty(game.TakenDown);
        Assert.Empty(game.Counters);
    }

    [Fact]
    public void With_the_yard_full_a_piece_the_bank_has_no_shelf_for_is_refused()
    {
        var game = new FakeGame { FreeSpots = 0 };
        game.OpenMenuOnPiece(Barricade);

        Assert.Equal(new BankOutcome.NoRoom("Barricade"), PackRun.PackMenu(game, Defaults));
        Assert.Empty(game.TakenDown);
    }

    [Fact]
    public void A_piece_with_no_bank_shelf_still_packs_up_into_a_free_yard_spot()
    {
        var game = new FakeGame();
        game.OpenMenuOnPiece(Barricade);

        Assert.IsType<BankOutcome.PackedUp>(PackRun.PackMenu(game, Defaults));
        Assert.Equal(Barricade, Assert.Single(game.Home).ConfigId);
    }

    [Fact]
    public void A_story_piece_packs_up_and_says_so()
    {
        var game = new FakeGame();
        game.OpenMenuOnPiece(Recorder, isStory: true);

        var outcome = PackRun.PackMenu(game, Defaults);

        Assert.Equal(new BankOutcome.PackedUp("Recorder", IsIntoBank: false, IsStory: true), outcome);
        Assert.Contains("story", OutcomeMessage.Describe(outcome, isChinese: false));
    }

    [Fact]
    public void Packing_up_on_the_Layout_screen_tells_the_player_to_leave_Layout_to_install_the_package()
    {
        var game = new FakeGame();
        game.OpenMenuOnPiece(Bed, isInLayout: true);

        var outcome = PackRun.PackMenu(game, Defaults);

        Assert.Equal(new BankOutcome.PackedUp("Bed", IsIntoBank: false, IsStory: false, IsInLayout: true), outcome);
        Assert.EndsWith("Leave Layout to install it.", OutcomeMessage.Describe(outcome, isChinese: false));
    }

    [Fact]
    public void A_piece_packed_into_the_bank_on_the_Layout_screen_gets_no_Layout_hint()
    {
        var game = new FakeGame { FreeSpots = 0 };
        game.OpenMenuOnPiece(Bed, isInLayout: true);

        var message = OutcomeMessage.Describe(PackRun.PackMenu(game, Defaults), isChinese: false);

        Assert.DoesNotContain("Layout", message);
    }

    [Fact]
    public void Pack_up_with_the_click_menu_already_shut_packs_nothing()
    {
        var game = new FakeGame();

        Assert.Equal(new BankOutcome.Gone(), PackRun.PackMenu(game, Defaults));
    }

    [Fact]
    public void A_piece_the_game_failed_to_take_down_is_reported_as_a_failure()
    {
        var game = new FakeGame { IsPackUpBroken = true };
        game.OpenMenuOnPiece(Bed);

        Assert.Equal(new BankOutcome.Failed(), PackRun.PackMenu(game, Defaults));
        Assert.Empty(game.Home);
    }
}
