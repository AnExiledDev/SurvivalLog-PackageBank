using System.Text.Json;
using PackageBank.Core;
using Xunit;
using static PackageBank.Tests.FakeGame;

namespace PackageBank.Tests;

/// <summary>What the panel and the shop are told to draw: which buttons are greyed and why.</summary>
public class BankViewTests
{
    private static readonly BankLimits Defaults = new(IsLimited: true, Large: 4, Medium: 6, Small: 8, Defense: 4);

    private static JsonElement View(FakeGame game, BankLimits? limits = null) =>
        JsonDocument.Parse(BankView.Of(game.Read(), limits ?? Defaults) ?? throw new InvalidOperationException("no view")).RootElement;

    private static JsonElement Size(JsonElement view, string key) => view.GetProperty("sizes").EnumerateArray().Single(size => size.GetProperty("key").GetString() == key);

    [Fact]
    public void Away_from_home_or_with_no_game_there_is_nothing_to_draw()
    {
        Assert.Null(BankView.Of(new FakeGame { IsAway = true }.Read(), Defaults));
        Assert.Null(BankView.Of(new FakeGame { IsNoGame = true }.Read(), Defaults));
    }

    [Fact]
    public void Each_size_shows_how_many_are_banked_against_its_limit_and_story_has_none()
    {
        var game = new FakeGame();
        BankRun.Store(game, Defaults, game.Add(Bed).Id);
        BankRun.Store(game, Defaults, game.Add(Recorder, isPurchased: false).Id);

        var view = View(game);

        Assert.Equal((1, 4), (Size(view, "large").GetProperty("used").GetInt32(), Size(view, "large").GetProperty("cap").GetInt32()));
        Assert.Equal(JsonValueKind.Null, Size(view, "story").GetProperty("cap").ValueKind);
    }

    [Fact]
    public void With_limits_off_no_size_shows_a_limit()
    {
        var view = View(new FakeGame(), new BankLimits(IsLimited: false, Large: 4, Medium: 6, Small: 8, Defense: 4));

        Assert.All(view.GetProperty("sizes").EnumerateArray(), size => Assert.Equal(JsonValueKind.Null, size.GetProperty("cap").ValueKind));
    }

    [Fact]
    public void A_yard_package_whose_size_is_full_shows_why_it_cannot_go_in()
    {
        var game = new FakeGame();
        var limits = Defaults with { Large = 0 };
        game.Add(Bed);
        game.Add(Lamp, isPurchased: false);

        var home = View(game, limits).GetProperty("home").EnumerateArray().ToList();

        Assert.Equal("full", home[0].GetProperty("why").GetString());
        Assert.Equal(JsonValueKind.Null, home[1].GetProperty("why").ValueKind);
        Assert.True(home[1].GetProperty("looted").GetBoolean());
    }

    [Fact]
    public void A_busy_package_and_an_unbankable_one_say_so()
    {
        var game = new FakeGame();
        game.Add(Bed, isBusy: true);
        game.Add(Barricade);

        var home = View(game).GetProperty("home").EnumerateArray().Select(row => row.GetProperty("why").GetString());

        Assert.Equal(new[] { "busy", "notBankable" }, home);
    }

    [Fact]
    public void With_no_free_yard_spot_take_out_says_why()
    {
        var game = new FakeGame();
        BankRun.Store(game, Defaults, game.Add(Bed).Id);
        game.FreeSpots = 0;

        Assert.Equal("noSpot", View(game).GetProperty("takeWhy").GetString());
    }

    [Fact]
    public void A_shut_shop_gets_no_rows()
    {
        Assert.Equal(JsonValueKind.Null, View(new FakeGame()).GetProperty("shop").ValueKind);
    }

    [Fact]
    public void Only_shop_rows_with_a_bought_yard_package_behind_them_get_a_bank_button()
    {
        var game = new FakeGame();
        game.Add(Bed, isBusy: true);
        game.Add(Lamp, isPurchased: false);
        game.Shop = new List<ShopRow> { new(0, Bed, 100), new(1, Lamp, 40) };

        var row = Assert.Single(View(game).GetProperty("shop").EnumerateArray());

        Assert.Equal(0, row.GetProperty("index").GetInt32());
        Assert.Equal("busy", row.GetProperty("why").GetString());
    }

    [Fact]
    public void Names_with_quotes_survive_into_the_page()
    {
        var game = new FakeGame();
        game.Add(Lamp);
        game.Home[0] = game.Home[0] with { Name = "Bob's \"lamp\" \\ <b>" };

        Assert.Equal("Bob's \"lamp\" \\ <b>", View(game).GetProperty("home")[0].GetProperty("name").GetString());
    }

    [Fact]
    public void The_click_menu_gets_a_bank_entry_only_on_a_yard_package_and_with_the_reason_it_is_refused()
    {
        var game = new FakeGame();
        var bed = game.Add(Bed);

        game.Menu = bed.Id;
        var refused = View(game, Defaults with { Large = 0 }).GetProperty("menu");
        game.Menu = 77;
        var notOurs = View(game).GetProperty("menu");

        Assert.Equal("full", refused.GetProperty("why").GetString());
        Assert.Equal(JsonValueKind.Null, notOurs.ValueKind);
    }
}
