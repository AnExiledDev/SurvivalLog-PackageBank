using System.Numerics;

namespace PackageBank.Core;

/// <summary>A place and a turn, as a spot point or a box keeps them. Quaternions multiply and turn vectors as Unity's do.</summary>
public readonly record struct SpotPose(Vector3 Position, Quaternion Rotation);

public static class SpotMath
{
    /// <summary>
    /// Where <paramref name="pose"/> ends up when the spot it belongs to goes from <paramref name="from"/> to
    /// <paramref name="to"/>: it keeps its place and turn as seen from the spot. A new spot copies a game spot's stand
    /// point and box this way.
    /// </summary>
    public static SpotPose Follow(SpotPose from, SpotPose to, SpotPose pose)
    {
        var back = Quaternion.Inverse(from.Rotation);
        var offset = Vector3.Transform(pose.Position - from.Position, back);

        return new SpotPose(to.Position + Vector3.Transform(offset, to.Rotation), to.Rotation * back * pose.Rotation);
    }
}
