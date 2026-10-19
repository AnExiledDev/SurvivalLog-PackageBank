using System;
using System.Collections.Generic;
using System.Linq;

namespace PackageBank.Core;

/// <summary>The bank's shelves. Story items are one-time packages from the story; they have a shelf of their own with no limit.</summary>
public enum PackageSize
{
    Small,
    Medium,
    Large,
    Story,
}

public static class PackageSizes
{
    /// <summary>The slot type of the home's outdoor defenses (traps and barricades): never banked.</summary>
    public const int DefenseSlotType = 10;

    private const string HomeSpotPrefix = "HomePackage";

    public static readonly IReadOnlyList<PackageSize> All = new[] { PackageSize.Large, PackageSize.Medium, PackageSize.Small, PackageSize.Story };

    /// <summary>The shelf a package goes on, from its furniture's slot type; null when it cannot be banked at all.</summary>
    public static PackageSize? Of(int slotType, bool isStory)
    {
        if (slotType == DefenseSlotType) return null;
        if (isStory) return PackageSize.Story;

        return slotType switch
        {
            0 or 1 or 4 or 6 => PackageSize.Small,
            2 or 9 => PackageSize.Medium,
            3 or 7 or 8 => PackageSize.Large,
            _ => null,
        };
    }

    /// <summary>The name the view and the counters use for a shelf.</summary>
    public static string Key(PackageSize size) => size switch
    {
        PackageSize.Small => "small",
        PackageSize.Medium => "medium",
        PackageSize.Large => "large",
        PackageSize.Story => "story",
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, null),
    };

    /// <summary>
    /// A normal package spot in the yard: "HomePackage" and a number. The car's spot ("HomePackage_car") and the boxes
    /// by the doors ("HomePackagesuliaodai...") hold packages the bank leaves alone.
    /// </summary>
    public static bool IsHomePackageSpot(string? node) =>
        node is not null
        && node.Length > HomeSpotPrefix.Length
        && node.StartsWith(HomeSpotPrefix, StringComparison.Ordinal)
        && node.Skip(HomeSpotPrefix.Length).All(character => character is >= '0' and <= '9');
}

/// <summary>Which furniture counts as a story item: the game sells or gives its item only for nothing (a price of 0 or 1).</summary>
public static class StoryPackages
{
    private const int MaxStoryPrice = 1;

    /// <summary>
    /// Takes every item that unpacks into furniture (its target furniture and its price). A furniture is a story item
    /// when it has at least one such item and every one of them is priced at 1 or less, so a craftable or shop-sold
    /// piece never lands on the unlimited shelf.
    /// </summary>
    public static IReadOnlySet<int> From(IEnumerable<(int TargetFurnitureId, int Price)> items) =>
        items
            .Where(item => item.TargetFurnitureId > 0)
            .GroupBy(item => item.TargetFurnitureId)
            .Where(group => group.All(item => item.Price <= MaxStoryPrice))
            .Select(group => group.Key)
            .ToHashSet();
}
