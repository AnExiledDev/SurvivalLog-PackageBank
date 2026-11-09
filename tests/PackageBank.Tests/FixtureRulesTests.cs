using PackageBank.Core;
using Xunit;

namespace PackageBank.Tests;

/// <summary>
/// Which placed pieces Pack up offers, and finding the home fixtures an older Pack up took away. The bug report: the
/// basement entrance (Move, no Remove) got packed and could not go back, and the counters of a shop visited before the
/// disaster offered Pack up and failed.
/// </summary>
public class PackRulesTests
{
    private const int Home = PackRules.HomeMapType;
    private const int Shop = 2;
    private const int Large = 3;

    [Fact]
    public void A_piece_the_game_lets_the_player_move_and_take_down_at_home_packs()
    {
        Assert.True(PackRules.IsPackable(new PieceFacts(HasMove: true, HasRemove: true, IsRemoveHiddenHere: false, MapType: Home, SlotType: Large)));
    }

    [Fact]
    public void The_basement_entrance_has_Move_but_no_Remove_so_it_does_not_pack()
    {
        Assert.False(PackRules.IsPackable(new PieceFacts(HasMove: true, HasRemove: false, IsRemoveHiddenHere: false, MapType: Home, SlotType: Large)));
    }

    [Fact]
    public void A_shop_counter_visited_before_the_disaster_does_not_pack()
    {
        Assert.False(PackRules.IsPackable(new PieceFacts(HasMove: true, HasRemove: true, IsRemoveHiddenHere: false, MapType: Shop, SlotType: Large)));
    }

    [Fact]
    public void A_piece_whose_Remove_the_game_hides_in_this_save_does_not_pack()
    {
        Assert.False(PackRules.IsPackable(new PieceFacts(HasMove: true, HasRemove: true, IsRemoveHiddenHere: true, MapType: Home, SlotType: Large)));
    }

    [Fact]
    public void A_piece_with_no_Move_does_not_pack()
    {
        Assert.False(PackRules.IsPackable(new PieceFacts(HasMove: false, HasRemove: true, IsRemoveHiddenHere: false, MapType: Home, SlotType: Large)));
    }

    [Fact]
    public void A_ladder_stands_on_no_build_spot_so_it_does_not_pack_though_it_has_Move_and_Remove()
    {
        Assert.False(PackRules.IsPackable(new PieceFacts(HasMove: true, HasRemove: true, IsRemoveHiddenHere: false, MapType: Home, SlotType: PackRules.NoSpotSlotType)));
    }

    [Fact]
    public void A_piece_on_a_map_the_game_has_no_row_for_still_packs()
    {
        Assert.True(PackRules.IsPackable(new PieceFacts(HasMove: true, HasRemove: true, IsRemoveHiddenHere: false, MapType: null, SlotType: Large)));
    }
}

public class FixtureRepairTests
{
    private const int Entrance = 80057;
    private const int Chapter = 1;
    private const int Student = 2;

    /// <summary>The Female College Student's basement entrance: ChapterType 1, map 1 (home), a fixed point, no Remove.</summary>
    private static readonly FixtureRow EntranceRow = new(Entrance, ChapterType: 1, PlayerSelectId: Student, MapPointId: 1, MapChapter: 0, MapType: 1, PosType: 1, IsLootSeed: false, HasMove: true, HasRemove: false, SlotType: 3);

    private const int Ladder = 80127;

    /// <summary>The Female College Student's basement ladder up to the house: ChapterType 2, map 101 (the basement, a home floor), a fixed point, Move and Remove, no build spot.</summary>
    private static readonly FixtureRow LadderRow = new(Ladder, ChapterType: 2, PlayerSelectId: Student, MapPointId: 101, MapChapter: 0, MapType: 1, PosType: 1, IsLootSeed: false, HasMove: true, HasRemove: true, SlotType: PackRules.NoSpotSlotType);

    private static HomeCopies Copies(
        int[]? standing = null,
        (long, int)[]? placed = null,
        (long, int)[]? packages = null,
        (int, int)[]? banked = null,
        int[]? demolished = null) =>
        new(
            new HashSet<int>(standing ?? Array.Empty<int>()),
            placed ?? Array.Empty<(long, int)>(),
            packages ?? Array.Empty<(long, int)>(),
            banked ?? Array.Empty<(int, int)>(),
            new HashSet<int>(demolished ?? Array.Empty<int>()));

    [Fact]
    public void A_packed_entrance_in_the_yard_is_found_with_its_package()
    {
        var lost = FixtureRepair.Find(new[] { EntranceRow }, Chapter, Student, Copies(packages: new[] { (900L, Entrance) }, demolished: new[] { Entrance }));

        Assert.Equal(new LostFixture(Entrance, new FixtureCopy.Package(900)), Assert.Single(lost));
    }

    [Fact]
    public void A_packed_entrance_in_the_bank_is_found_with_its_slot()
    {
        var lost = FixtureRepair.Find(new[] { EntranceRow }, Chapter, Student, Copies(banked: new[] { (3, Entrance) }));

        Assert.Equal(new LostFixture(Entrance, new FixtureCopy.Banked(3)), Assert.Single(lost));
    }

    [Fact]
    public void An_entrance_installed_on_a_Large_spot_is_taken_away_before_any_package()
    {
        var lost = FixtureRepair.Find(new[] { EntranceRow }, Chapter, Student, Copies(placed: new[] { (77L, Entrance) }, packages: new[] { (900L, Entrance) }));

        Assert.Equal(new LostFixture(Entrance, new FixtureCopy.Placed(77)), Assert.Single(lost));
    }

    [Fact]
    public void An_entrance_packed_and_sold_back_is_still_set_down_again()
    {
        var lost = FixtureRepair.Find(new[] { EntranceRow }, Chapter, Student, Copies(demolished: new[] { Entrance }));

        Assert.Equal(new LostFixture(Entrance, null), Assert.Single(lost));
    }

    [Fact]
    public void An_entrance_still_standing_is_left_alone_even_with_a_package_of_it()
    {
        var lost = FixtureRepair.Find(new[] { EntranceRow }, Chapter, Student, Copies(standing: new[] { Entrance }, packages: new[] { (900L, Entrance) }, demolished: new[] { Entrance }));

        Assert.Empty(lost);
    }

    [Fact]
    public void A_fixture_never_set_down_and_never_taken_down_is_not_made()
    {
        Assert.Empty(FixtureRepair.Find(new[] { EntranceRow }, Chapter, Student, Copies()));
    }

    [Fact]
    public void Another_characters_entrance_is_not_made()
    {
        var lost = FixtureRepair.Find(new[] { EntranceRow }, Chapter, playerSelectId: 1, Copies(packages: new[] { (900L, Entrance) }));

        Assert.Empty(lost);
    }

    [Fact]
    public void A_row_the_game_does_not_load_in_this_chapter_is_not_made()
    {
        var chapterTwoOnly = EntranceRow with { ChapterType = 2 };

        Assert.Empty(FixtureRepair.Find(new[] { chapterTwoOnly }, Chapter, Student, Copies(packages: new[] { (900L, Entrance) })));
        Assert.Single(FixtureRepair.Find(new[] { chapterTwoOnly }, chapterId: 2, Student, Copies(packages: new[] { (900L, Entrance) })));
    }

    [Fact]
    public void Furniture_with_Remove_is_the_players_to_pack_and_is_never_made_again()
    {
        var bed = EntranceRow with { HasRemove = true };

        Assert.Empty(FixtureRepair.Find(new[] { bed }, Chapter, Student, Copies(packages: new[] { (900L, Entrance) }, demolished: new[] { Entrance })));
    }

    [Fact]
    public void A_fixture_with_no_Move_was_never_Pack_ups_to_take_and_is_left_to_the_game()
    {
        var lamp = EntranceRow with { HasMove = false };

        Assert.Empty(FixtureRepair.Find(new[] { lamp }, Chapter, Student, Copies(demolished: new[] { Entrance })));
    }

    [Fact]
    public void Rows_away_from_home_or_on_a_spot_or_meant_as_packages_are_not_made()
    {
        var rows = new[]
        {
            EntranceRow with { MapType = 2 },
            EntranceRow with { PosType = 2 },
            EntranceRow with { PosType = 3 },
            EntranceRow with { IsLootSeed = true },
        };

        Assert.Empty(FixtureRepair.Find(rows, Chapter, Student, Copies(packages: new[] { (900L, Entrance) }, demolished: new[] { Entrance })));
    }

    [Fact]
    public void Two_rows_for_one_fixture_take_away_one_copy()
    {
        var lost = FixtureRepair.Find(new[] { EntranceRow, EntranceRow with { PlayerSelectId = 0 } }, Chapter, Student, Copies(packages: new[] { (900L, Entrance), (901L, Entrance) }));

        Assert.Single(lost);
    }

    [Fact]
    public void A_packed_ladder_in_the_yard_is_found_with_its_package()
    {
        var lost = FixtureRepair.Find(new[] { LadderRow }, chapterId: 2, Student, Copies(packages: new[] { (901L, Ladder) }));

        Assert.Equal(new LostFixture(Ladder, new FixtureCopy.Package(901)), Assert.Single(lost));
    }

    [Fact]
    public void A_packed_ladder_in_the_bank_is_found_with_its_slot()
    {
        var lost = FixtureRepair.Find(new[] { LadderRow }, chapterId: 2, Student, Copies(banked: new[] { (5, Ladder) }, demolished: new[] { Ladder }));

        Assert.Equal(new LostFixture(Ladder, new FixtureCopy.Banked(5)), Assert.Single(lost));
    }

    [Fact]
    public void A_ladder_the_player_removed_on_the_Layout_screen_stays_removed()
    {
        Assert.Empty(FixtureRepair.Find(new[] { LadderRow }, chapterId: 2, Student, Copies(demolished: new[] { Ladder })));
    }

    [Fact]
    public void Only_the_entrance_and_the_ladders_count_as_taken_by_an_older_Pack_up()
    {
        Assert.True(FixtureRepair.IsTakenByOldPackUp(EntranceRow));
        Assert.True(FixtureRepair.IsTakenByOldPackUp(LadderRow));
        Assert.False(FixtureRepair.IsTakenByOldPackUp(LadderRow with { SlotType = 1 }));
        Assert.False(FixtureRepair.IsTakenByOldPackUp(LadderRow with { HasMove = false }));
        Assert.False(FixtureRepair.IsTakenByOldPackUp(LadderRow with { MapType = 2 }));
    }

    [Fact]
    public void The_repair_button_says_when_nothing_needed_fixing()
    {
        Assert.StartsWith("nothing needed fixing", FixtureRepair.Describe(Array.Empty<string>(), Array.Empty<string>()));
    }

    [Fact]
    public void The_repair_button_names_what_it_set_down_and_what_it_could_not()
    {
        var text = FixtureRepair.Describe(new[] { "Basement entrance (80057)", "Stairs (80127)" }, new[] { "Stairs (80055)" });

        Assert.Contains("set down again where the game puts them: Basement entrance (80057), Stairs (80127)", text);
        Assert.Contains("could not set down", text);
        Assert.Contains("Stairs (80055)", text);
        Assert.DoesNotContain("nothing needed fixing", text);
    }

    [Theory]
    [InlineData(4, 0, true)]
    [InlineData(1, 0, false)]
    [InlineData(5, 1500, true)]
    [InlineData(1, 1500, false)]
    [InlineData(1, 1, true)]
    [InlineData(2, 2, true)]
    [InlineData(2, 1, false)]
    [InlineData(3, 1, true)]
    [InlineData(3, 2, false)]
    [InlineData(-1, 3, true)]
    [InlineData(4, 3, false)]
    public void Rows_load_in_the_chapters_the_game_loads_them_in(int chapterType, int chapterId, bool isLoaded)
    {
        Assert.Equal(isLoaded, FixtureRepair.IsLoadedInChapter(chapterType, chapterId));
    }
}
