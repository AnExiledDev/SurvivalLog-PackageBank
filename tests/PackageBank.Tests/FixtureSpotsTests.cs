using System.Collections.Generic;
using PackageBank.Core;
using Xunit;

namespace PackageBank.Tests;

/// <summary>
/// The bug report: a player packed up the College Student's own cabinet on the 2nd floor (furniture 80086, which stands
/// on the map point Neighbor_Girl_After_2Floor_03, not on a build spot), and nothing could go where it stood again, not
/// even that cabinet. Package Bank now adds a build spot there once the fixture is down.
/// </summary>
public class FixtureSpotsTests
{
    private const int Cabinet = 80086;
    private const int Medium = 2;
    private const string CabinetPoint = "Neighbor_Girl_After_2Floor_03";
    private const string CabinetSpot = FixtureSpots.NamePrefix + "80086";

    private static readonly FixtureRow CabinetRow = new(Cabinet, ChapterType: 1, PlayerSelectId: 2, MapPointId: 102, MapChapter: 0, MapType: 1, PosType: 1, IsLootSeed: false, HasMove: true, HasRemove: true, SlotType: Medium, PosPoint: CabinetPoint);

    private static readonly IReadOnlySet<string> OnThisMap = new HashSet<string> { CabinetPoint };
    private static readonly IReadOnlySet<string> None = new HashSet<string>();
    private static readonly IReadOnlySet<int> CabinetDown = new HashSet<int> { Cabinet };
    private static readonly IReadOnlySet<int> NothingDown = new HashSet<int>();

    [Fact]
    public void The_cabinet_gets_a_spot_of_its_own_size_on_its_floor_at_its_point()
    {
        var spot = Assert.Single(FixtureSpots.Plan(new[] { CabinetRow }));

        Assert.Equal(new FixtureSpot(Cabinet, FixtureSpots.FirstId + Cabinet, CabinetSpot, Medium, FixtureSpots.Upstairs, CabinetPoint), spot);
    }

    [Fact]
    public void Spot_ids_stay_clear_of_the_games_and_Floor_Plans()
    {
        var spot = Assert.Single(FixtureSpots.Plan(new[] { CabinetRow with { FurnitureId = 1 } }));

        Assert.True(spot.Id > 305_288);
        Assert.True(spot.Id >= 9_100_000);
    }

    [Fact]
    public void Fixtures_that_cannot_be_taken_down_or_fit_no_spot_get_none()
    {
        var rows = new[]
        {
            CabinetRow with { HasRemove = false },
            CabinetRow with { HasMove = false },
            CabinetRow with { SlotType = PackRules.NoSpotSlotType },
            CabinetRow with { SlotType = 8 },
            CabinetRow with { SlotType = 10 },
        };

        Assert.Empty(FixtureSpots.Plan(rows));
    }

    [Fact]
    public void Rows_on_a_build_spot_away_from_home_or_without_a_point_get_none()
    {
        var rows = new[]
        {
            CabinetRow with { PosType = 2 },
            CabinetRow with { PosType = 3 },
            CabinetRow with { MapType = 2 },
            CabinetRow with { MapType = null },
            CabinetRow with { IsLootSeed = true },
            CabinetRow with { PosPoint = null },
            CabinetRow with { PosPoint = "" },
        };

        Assert.Empty(FixtureSpots.Plan(rows));
    }

    [Fact]
    public void Two_rows_for_one_fixture_make_one_spot()
    {
        Assert.Single(FixtureSpots.Plan(new[] { CabinetRow, CabinetRow with { PlayerSelectId = 0 } }));
    }

    [Theory]
    [InlineData(1, FixtureSpots.GroundFloor)]
    [InlineData(11, FixtureSpots.GroundFloor)]
    [InlineData(101, FixtureSpots.Basement)]
    [InlineData(102, FixtureSpots.Upstairs)]
    public void A_spot_is_on_its_fixtures_floor(int mapPointId, int floor)
    {
        Assert.Equal(floor, FixtureSpots.FloorOf(mapPointId));
    }

    [Fact]
    public void The_spot_shows_once_the_fixture_is_down()
    {
        var spots = FixtureSpots.Plan(new[] { CabinetRow });

        Assert.Equal(new[] { CabinetSpot }, FixtureSpots.Shown(spots, CabinetDown, OnThisMap, None, isOn: true, isUninstalling: false));
        Assert.Empty(FixtureSpots.Shown(spots, NothingDown, OnThisMap, None, isOn: true, isUninstalling: false));
    }

    [Fact]
    public void Another_homes_fixture_shows_no_spot_here()
    {
        var spots = FixtureSpots.Plan(new[] { CabinetRow });

        Assert.Empty(FixtureSpots.Shown(spots, CabinetDown, None, None, isOn: true, isUninstalling: false));
    }

    [Fact]
    public void With_the_setting_off_or_Uninstall_prep_on_no_new_spot_shows()
    {
        var spots = FixtureSpots.Plan(new[] { CabinetRow });

        Assert.Empty(FixtureSpots.Shown(spots, CabinetDown, OnThisMap, None, isOn: false, isUninstalling: false));
        Assert.Empty(FixtureSpots.Shown(spots, CabinetDown, OnThisMap, None, isOn: true, isUninstalling: true));
    }

    [Fact]
    public void A_spot_with_a_piece_on_it_stays_whatever_the_settings()
    {
        var spots = FixtureSpots.Plan(new[] { CabinetRow });
        var taken = new HashSet<string> { CabinetSpot };

        Assert.Equal(new[] { CabinetSpot }, FixtureSpots.Shown(spots, NothingDown, OnThisMap, taken, isOn: false, isUninstalling: true));
    }

    [Fact]
    public void A_reused_set_holds_only_the_spots_shown_now()
    {
        var spots = FixtureSpots.Plan(new[] { CabinetRow });
        var shown = new HashSet<string>();

        FixtureSpots.Shown(spots, CabinetDown, OnThisMap, None, isOn: true, isUninstalling: false, shown);
        FixtureSpots.Shown(spots, NothingDown, OnThisMap, None, isOn: true, isUninstalling: false, shown);

        Assert.Empty(shown);
    }

    // The cabinet's spot is medium, upstairs. Its box is copied from a game spot's box, which sets how big it is to click.

    [Fact]
    public void A_box_of_another_size_is_never_copied()
    {
        var spot = Assert.Single(FixtureSpots.Plan(new[] { CabinetRow }));

        Assert.Null(FixtureSpots.TemplateScore(spot, type: 3, floor: FixtureSpots.Upstairs, distance: 0.5f));
    }

    [Fact]
    public void A_box_on_the_same_floor_wins_over_a_nearer_one_on_another_floor()
    {
        var spot = Assert.Single(FixtureSpots.Plan(new[] { CabinetRow }));
        var sameFloorFar = FixtureSpots.TemplateScore(spot, Medium, FixtureSpots.Upstairs, distance: 30);
        var groundFloorNear = FixtureSpots.TemplateScore(spot, Medium, FixtureSpots.GroundFloor, distance: 3);

        Assert.True(sameFloorFar < groundFloorNear);
    }

    [Fact]
    public void Among_boxes_on_the_same_floor_the_nearest_wins()
    {
        var spot = Assert.Single(FixtureSpots.Plan(new[] { CabinetRow }));

        Assert.True(FixtureSpots.TemplateScore(spot, Medium, FixtureSpots.Upstairs, 2) < FixtureSpots.TemplateScore(spot, Medium, FixtureSpots.Upstairs, 5));
    }

    [Fact]
    public void A_box_on_another_floor_is_still_copied_when_none_on_this_floor_fits()
    {
        var spot = Assert.Single(FixtureSpots.Plan(new[] { CabinetRow }));

        Assert.NotNull(FixtureSpots.TemplateScore(spot, Medium, FixtureSpots.GroundFloor, 12));
    }

    [Fact]
    public void While_nothing_changes_the_full_check_waits_for_its_timer()
    {
        Assert.False(FixtureSpots.IsFullCheckDue(isStampSame: true, isUninstalling: false, now: 104, nextFullCheck: 110));
        Assert.True(FixtureSpots.IsFullCheckDue(isStampSame: true, isUninstalling: false, now: 110, nextFullCheck: 110));
    }

    [Fact]
    public void A_change_at_home_or_Uninstall_prep_checks_in_full_at_once()
    {
        Assert.True(FixtureSpots.IsFullCheckDue(isStampSame: false, isUninstalling: false, now: 104, nextFullCheck: 110));
        Assert.True(FixtureSpots.IsFullCheckDue(isStampSame: true, isUninstalling: true, now: 104, nextFullCheck: 110));
    }

    [Fact]
    public void Only_Package_Banks_spot_names_are_its_own()
    {
        Assert.True(FixtureSpots.IsOurs(CabinetSpot));
        Assert.False(FixtureSpots.IsOurs(CabinetPoint));
        Assert.False(FixtureSpots.IsOurs("FloorPlan_home1_3"));
        Assert.False(FixtureSpots.IsOurs(null));
    }
}
