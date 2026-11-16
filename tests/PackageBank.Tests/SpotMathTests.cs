using System;
using System.Numerics;
using PackageBank.Core;
using Xunit;

namespace PackageBank.Tests;

/// <summary>
/// A new spot copies a game spot's stand point and box. Each must sit where it sat beside the game spot, turned with the
/// new spot, or the character stands in a wall and the box sits off to one side of the furniture.
/// </summary>
public class SpotMathTests
{
    private static readonly Quaternion Quarter = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2);

    [Fact]
    public void A_spot_that_only_moves_carries_its_points_the_same_distance()
    {
        var from = new SpotPose(new Vector3(1, 0, 1), Quaternion.Identity);
        var to = new SpotPose(new Vector3(11, 2, -4), Quaternion.Identity);
        var stand = new SpotPose(new Vector3(1, 0, 2), Quaternion.Identity);

        var moved = SpotMath.Follow(from, to, stand);

        AssertNear(new Vector3(11, 2, -3), moved.Position);
        AssertSameTurn(Quaternion.Identity, moved.Rotation);
    }

    [Fact]
    public void A_spot_turned_a_quarter_turn_swings_its_stand_point_round_with_it()
    {
        var from = new SpotPose(Vector3.Zero, Quaternion.Identity);
        var to = new SpotPose(new Vector3(5, 0, 5), Quarter);
        var stand = new SpotPose(new Vector3(0, 0, 1), Quaternion.Identity);

        var moved = SpotMath.Follow(from, to, stand);

        // A quarter turn about the vertical axis takes "one ahead" (+z) to "one to the right" (+x), as Unity turns it.
        AssertNear(new Vector3(6, 0, 5), moved.Position);
        AssertSameTurn(Quarter, moved.Rotation);
    }

    [Fact]
    public void A_point_keeps_its_place_as_seen_from_a_turned_spot()
    {
        var from = new SpotPose(new Vector3(2, 0, 0), Quarter);
        var to = new SpotPose(new Vector3(-3, 1, 4), Quaternion.Identity);
        var box = new SpotPose(new Vector3(3, 0, 0), Quarter);

        var moved = SpotMath.Follow(from, to, box);

        // The box is one step ahead of a spot that faces +x, so it is one step ahead (+z) of a spot that faces +z.
        AssertNear(new Vector3(-3, 1, 5), moved.Position);
        AssertSameTurn(Quaternion.Identity, moved.Rotation);
    }

    [Fact]
    public void A_point_on_the_spot_lands_on_the_new_spot()
    {
        var from = new SpotPose(new Vector3(7, 0, 3), Quarter);
        var to = new SpotPose(new Vector3(-1, 0, 2), Quaternion.CreateFromAxisAngle(Vector3.UnitY, 0.3f));

        var moved = SpotMath.Follow(from, to, from);

        AssertNear(to.Position, moved.Position);
        AssertSameTurn(to.Rotation, moved.Rotation);
    }

    private static void AssertNear(Vector3 expected, Vector3 actual) =>
        Assert.True(Vector3.Distance(expected, actual) < 1e-4f, $"expected {expected}, got {actual}");

    /// <summary>q and -q are the same turn.</summary>
    private static void AssertSameTurn(Quaternion expected, Quaternion actual) =>
        Assert.True(MathF.Abs(Quaternion.Dot(expected, actual)) > 1 - 1e-5f, $"expected {expected}, got {actual}");
}
