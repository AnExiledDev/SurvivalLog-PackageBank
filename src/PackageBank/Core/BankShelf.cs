using System.Collections.Generic;
using System.Linq;

namespace PackageBank.Core;

/// <summary>What another mod may ask of the bank: which furniture is banked, and which entry to take out for one.</summary>
public static class BankShelf
{
    /// <summary>The furniture id of every banked package, one per package, oldest first.</summary>
    public static int[] Furniture(IEnumerable<BankEntry> entries) => entries.OrderBy(entry => entry.Slot).Select(entry => entry.ConfigId).ToArray();

    /// <summary>The longest-banked package of that furniture, or null when none is banked.</summary>
    public static BankEntry? Oldest(IEnumerable<BankEntry> entries, int configId) =>
        entries.Where(entry => entry.ConfigId == configId).OrderBy(entry => entry.Slot).FirstOrDefault();
}
