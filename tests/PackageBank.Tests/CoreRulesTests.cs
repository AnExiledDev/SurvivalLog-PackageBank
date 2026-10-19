using PackageBank.Core;
using Xunit;

namespace PackageBank.Tests;

/// <summary>The save format and the rules for which package goes on which shelf.</summary>
public class BankCountersTests
{
    [Theory]
    [InlineData(0x7FFF_FFFF_FFFF_FFFFL)]
    [InlineData(0x1_8000_0001L)]
    [InlineData(-5L)]
    [InlineData(0L)]
    public void A_shop_id_wider_than_one_counter_reads_back_whole(long source)
    {
        var entry = new BankEntry(2, 30, 1, 450, source, IsPurchased: true);

        var read = BankCounters.Read(Pairs(BankCounters.Write(entry)));

        Assert.Equal(entry, Assert.Single(read.Entries));
    }

    [Fact]
    public void Clearing_a_slot_removes_every_counter_banking_wrote()
    {
        var written = BankCounters.Write(new BankEntry(4, 30, 0, -1, 0, IsPurchased: false)).Select(pair => pair.Key);

        Assert.Equal(written.OrderBy(key => key), BankCounters.Keys(4).OrderBy(key => key));
    }

    [Fact]
    public void Counters_that_are_not_a_whole_slot_are_ignored()
    {
        var counters = new Dictionary<string, int>
        {
            ["PkgBank_3_dye"] = 2,
            ["PkgBank_x_cfg"] = 30,
            ["PkgBank_5_color"] = 1,
            ["OtherMod_1_cfg"] = 30,
            ["PkgBank_1_cfg"] = 10,
        };

        var read = BankCounters.Read(counters);

        Assert.Equal(new BankEntry(1, 10, 0, -1, 0, false), Assert.Single(read.Entries));
    }

    [Fact]
    public void The_bonus_counters_are_read_and_a_negative_one_counts_as_none()
    {
        var read = BankCounters.Read(new Dictionary<string, int> { [BankCounters.BonusLarge] = 2, [BankCounters.BonusSmall] = -3 });

        Assert.Equal(new BankBonus(2, 0, 0), read.Bonus);
    }

    [Fact]
    public void A_new_package_goes_after_the_newest_even_when_an_older_slot_was_emptied()
    {
        Assert.Equal(0, BankCounters.NextSlot(Array.Empty<int>()));
        Assert.Equal(6, BankCounters.NextSlot(new[] { 5, 2 }));
    }

    private static IEnumerable<KeyValuePair<string, int>> Pairs(IEnumerable<(string Key, int Value)> pairs) => pairs.Select(pair => new KeyValuePair<string, int>(pair.Key, pair.Value));
}

public class PackageSizesTests
{
    [Theory]
    [InlineData(0, PackageSize.Small)]
    [InlineData(1, PackageSize.Small)]
    [InlineData(4, PackageSize.Small)]
    [InlineData(6, PackageSize.Small)]
    [InlineData(2, PackageSize.Medium)]
    [InlineData(9, PackageSize.Medium)]
    [InlineData(3, PackageSize.Large)]
    [InlineData(7, PackageSize.Large)]
    [InlineData(8, PackageSize.Large)]
    public void Each_slot_type_has_its_shelf(int slotType, PackageSize size)
    {
        Assert.Equal(size, PackageSizes.Of(slotType, isStory: false));
    }

    [Fact]
    public void Outdoor_defenses_are_never_banked_even_when_story_items()
    {
        Assert.Null(PackageSizes.Of(PackageSizes.DefenseSlotType, isStory: true));
    }

    [Fact]
    public void A_story_item_goes_on_the_story_shelf_whatever_its_slot()
    {
        Assert.Equal(PackageSize.Story, PackageSizes.Of(3, isStory: true));
    }

    [Fact]
    public void A_slot_type_the_bank_does_not_know_cannot_be_banked()
    {
        Assert.Null(PackageSizes.Of(5, isStory: false));
    }

    [Theory]
    [InlineData("HomePackage1", true)]
    [InlineData("HomePackage12", true)]
    [InlineData("HomePackage", false)]
    [InlineData("HomePackage_car", false)]
    [InlineData("HomePackagesuliaodai1", false)]
    [InlineData(null, false)]
    public void Only_the_numbered_yard_spots_hold_bankable_packages(string? node, bool isSpot)
    {
        Assert.Equal(isSpot, PackageSizes.IsHomePackageSpot(node));
    }
}

public class StoryPackagesTests
{
    [Fact]
    public void Furniture_whose_items_are_all_free_is_a_story_item()
    {
        var story = StoryPackages.From(new[] { (90, 0), (90, 1) });

        Assert.Contains(90, story);
    }

    [Fact]
    public void Furniture_with_any_priced_item_is_not_a_story_item()
    {
        var story = StoryPackages.From(new[] { (30, 0), (30, 450) });

        Assert.DoesNotContain(30, story);
    }

    [Fact]
    public void Items_that_unpack_into_no_furniture_are_ignored()
    {
        Assert.Empty(StoryPackages.From(new[] { (0, 0), (-1, 1) }));
    }
}

public class PageCommandTests
{
    [Theory]
    [InlineData("slmod|packagebank|toggle")]
    [InlineData("slmod|packagebank|close")]
    [InlineData("slmod|packagebank|ready")]
    [InlineData("slmod|packagebank|noscript")]
    public void The_scripts_plain_verbs_parse(string message)
    {
        Assert.IsNotType<PageCommand.Unknown>(PageCommand.Parse(message));
    }

    [Fact]
    public void Row_buttons_carry_their_package_slot_or_shop_row()
    {
        Assert.Equal(new PageCommand.Store(8589934594L), PageCommand.Parse("slmod|packagebank|store|8589934594"));
        Assert.Equal(new PageCommand.TakeOut(3), PageCommand.Parse("slmod|packagebank|takeout|3"));
        Assert.Equal(new PageCommand.StoreShopRow(2), PageCommand.Parse("slmod|packagebank|bank|2"));
        Assert.Equal(new PageCommand.StoreMenu(), PageCommand.Parse("slmod|packagebank|menubank"));
    }

    [Theory]
    [InlineData("slmod|packagebank|takeout|-1")]
    [InlineData("slmod|packagebank|bank|x")]
    [InlineData("slmod|packagebank|store")]
    [InlineData("slmod|packagebank|diag|HUD button placed")]
    [InlineData("slmod|defensemanager|toggle")]
    public void Anything_else_is_only_logged(string message)
    {
        Assert.IsType<PageCommand.Unknown>(PageCommand.Parse(message));
    }
}

public class TextsTests
{
    [Fact]
    public void Every_phrase_has_both_languages_with_the_same_placeholders()
    {
        foreach (var phrase in Texts.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(phrase.Zh), phrase.En);
            Assert.Equal(Placeholders(phrase.En), Placeholders(phrase.Zh));
        }
    }

    [Fact]
    public void English_text_is_plain_ascii()
    {
        Assert.All(Texts.All, phrase => Assert.True(phrase.En.All(character => character < 128), phrase.En));
    }

    [Fact]
    public void A_full_size_says_which_size_and_its_limit()
    {
        Assert.Equal("The bank is full for large packages (4 of 4).", OutcomeMessage.Describe(new BankOutcome.Full(PackageSize.Large, 4), isChinese: false));
        Assert.Equal("仓库的大型包裹已存满（4/4）。", OutcomeMessage.Describe(new BankOutcome.Full(PackageSize.Large, 4), isChinese: true));
    }

    private static string Placeholders(string text) => string.Join(",", System.Text.RegularExpressions.Regex.Matches(text, @"\{\d\}").Select(match => match.Value).Distinct().OrderBy(value => value));
}
