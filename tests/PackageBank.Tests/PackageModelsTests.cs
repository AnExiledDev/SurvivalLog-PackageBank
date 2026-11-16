using PackageBank.Core;
using Xunit;

namespace PackageBank.Tests;

/// <summary>
/// The bug report: on the Warehouse Manager's map, Pack up on the Jammed Old Printer, the Old Office Supplies, the Dusty
/// Office Junk, the Wooden Crate and the Rusted Hydraulic Pallet Jack failed, since the game has no package model for
/// them. Every piece should pack, refunded at its pawn price when it has no shop price, and Uninstall prep should handle
/// them.
/// </summary>
public class PackageModelsTests
{
    [Theory]
    [InlineData(1, PackageModels.SmallBox)]
    [InlineData(6, PackageModels.SmallBox)]
    [InlineData(2, PackageModels.MediumBox)]
    [InlineData(3, PackageModels.LargeBox)]
    [InlineData(10, PackageModels.MediumBox)]
    public void A_piece_with_no_package_model_gets_the_box_of_its_size(int slotType, string box)
    {
        Assert.Equal(box, PackageModels.BoxFor(new FurnitureRow("", HasMove: true, HasRemove: true, slotType)));
    }

    [Fact]
    public void A_piece_that_has_a_package_model_keeps_it()
    {
        Assert.Null(PackageModels.BoxFor(new FurnitureRow("P_trashbag02", HasMove: true, HasRemove: true, SlotType: 1)));
    }

    [Theory]
    [InlineData(false, true, 1)]
    [InlineData(true, false, 1)]
    [InlineData(true, true, PackRules.NoSpotSlotType)]
    public void A_piece_Pack_up_never_takes_gets_no_box(bool hasMove, bool hasRemove, int slotType)
    {
        Assert.Null(PackageModels.BoxFor(new FurnitureRow(null, hasMove, hasRemove, slotType)));
    }

    [Fact]
    public void A_piece_with_a_shop_price_refunds_at_what_the_game_works_out()
    {
        Assert.Equal(PackRun.PaidUnknown, PackageModels.RefundPaid(furniturePrice: 120, pawnPrice: 25));
    }

    [Fact]
    public void A_piece_with_no_shop_price_refunds_its_pawn_price()
    {
        Assert.Equal(25, PackageModels.RefundPaid(furniturePrice: 0, pawnPrice: 25));
    }
}

public class UninstallBoxedTests
{
    private const int Crate = 382;
    private const int Shelf = 385;
    private const int FlowerBed = 80085;
    private const int Chapter = 1;
    private const int WarehouseManager = 3;

    private static readonly HashSet<int> Boxed = new() { Crate, Shelf, FlowerBed };
    private static readonly HashSet<int> NoneBlocked = new();

    /// <summary>The Wooden Crate of the Warehouse Manager's home: ChapterType 1, map 1, a build spot (WarehousePos70).</summary>
    private static readonly FixtureRow CrateRow = new(Crate, ChapterType: 1, PlayerSelectId: WarehouseManager, MapPointId: 1, MapChapter: 0, MapType: 1, PosType: 2, IsLootSeed: false, HasMove: true, HasRemove: true, SlotType: 1, PosPoint: "WarehousePos70");

    /// <summary>The Shelf next to it, which the game fills with items when it sets it down.</summary>
    private static readonly FixtureRow ShelfRow = CrateRow with { FurnitureId = Shelf, SlotType = 3, HasStartingItems = true, PosPoint = "WarehousePos62" };

    private static readonly FixtureRow[] Rows = { CrateRow, ShelfRow };

    private static HomeCopies Home(int[]? standing = null, (long, int)[]? packages = null, (int, int)[]? banked = null) =>
        new(
            new HashSet<int>(standing ?? Array.Empty<int>()),
            Array.Empty<(long, int)>(),
            packages ?? Array.Empty<(long, int)>(),
            banked ?? Array.Empty<(int, int)>(),
            new HashSet<int>());

    [Fact]
    public void A_packed_crate_in_the_yard_goes_back_where_it_stood()
    {
        var plan = FixtureRepair.FindBoxed(Rows, Chapter, WarehouseManager, Home(packages: new[] { (900L, Crate) }), Boxed, NoneBlocked);

        Assert.Equal(new LostFixture(Crate, new FixtureCopy.Package(900)), Assert.Single(plan.SetDown));
        Assert.Empty(plan.Kept);
    }

    [Fact]
    public void A_banked_crate_goes_back_where_it_stood()
    {
        var plan = FixtureRepair.FindBoxed(Rows, Chapter, WarehouseManager, Home(banked: new[] { (4, Crate) }), Boxed, NoneBlocked);

        Assert.Equal(new LostFixture(Crate, new FixtureCopy.Banked(4)), Assert.Single(plan.SetDown));
    }

    [Fact]
    public void A_second_crate_package_is_kept_for_the_player_since_the_house_had_one()
    {
        var plan = FixtureRepair.FindBoxed(Rows, Chapter, WarehouseManager, Home(packages: new[] { (900L, Crate), (901L, Crate) }), Boxed, NoneBlocked);

        Assert.Equal(new LostFixture(Crate, new FixtureCopy.Package(900)), Assert.Single(plan.SetDown));
        Assert.Equal(new LostFixture(Crate, new FixtureCopy.Package(901)), Assert.Single(plan.Kept));
    }

    [Fact]
    public void A_crate_package_while_the_crate_still_stands_is_kept()
    {
        var plan = FixtureRepair.FindBoxed(Rows, Chapter, WarehouseManager, Home(standing: new[] { Crate }, packages: new[] { (900L, Crate) }), Boxed, NoneBlocked);

        Assert.Empty(plan.SetDown);
        Assert.Single(plan.Kept);
    }

    [Fact]
    public void A_crate_whose_place_a_piece_stands_on_is_kept()
    {
        var plan = FixtureRepair.FindBoxed(Rows, Chapter, WarehouseManager, Home(packages: new[] { (900L, Crate) }), Boxed, new HashSet<int> { Crate });

        Assert.Empty(plan.SetDown);
        Assert.Single(plan.Kept);
    }

    [Fact]
    public void A_shelf_the_game_would_fill_again_is_kept_so_no_items_are_made()
    {
        var plan = FixtureRepair.FindBoxed(Rows, Chapter, WarehouseManager, Home(packages: new[] { (900L, Shelf) }), Boxed, NoneBlocked);

        Assert.Empty(plan.SetDown);
        Assert.Equal(new LostFixture(Shelf, new FixtureCopy.Package(900)), Assert.Single(plan.Kept));
    }

    [Fact]
    public void A_piece_the_game_never_sets_down_in_this_home_is_kept()
    {
        var plan = FixtureRepair.FindBoxed(Rows, Chapter, WarehouseManager, Home(packages: new[] { (900L, FlowerBed) }), Boxed, NoneBlocked);

        Assert.Empty(plan.SetDown);
        Assert.Single(plan.Kept);
    }

    [Fact]
    public void The_crate_of_another_character_is_not_set_down_in_this_home()
    {
        var plan = FixtureRepair.FindBoxed(Rows, Chapter, playerSelectId: 1, Home(packages: new[] { (900L, Crate) }), Boxed, NoneBlocked);

        Assert.Empty(plan.SetDown);
        Assert.Single(plan.Kept);
    }

    [Fact]
    public void A_package_of_furniture_that_has_its_own_model_is_left_alone()
    {
        var plan = FixtureRepair.FindBoxed(Rows, Chapter, WarehouseManager, Home(packages: new[] { (900L, 111) }), Boxed, NoneBlocked);

        Assert.Empty(plan.SetDown);
        Assert.Empty(plan.Kept);
    }
}
