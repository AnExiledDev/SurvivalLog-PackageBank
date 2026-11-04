using System.Collections.Generic;
using System.Linq;

namespace PackageBank.Core;

/// <summary>
/// What another mod may ask of the bank: which furniture is banked, which entry to take out for one, and the entry a
/// deposit writes.
/// </summary>
public static class BankShelf
{
    /// <summary>The furniture id of every banked package, one per package, oldest first.</summary>
    public static int[] Furniture(IEnumerable<BankEntry> entries) => entries.OrderBy(entry => entry.Slot).Select(entry => entry.ConfigId).ToArray();

    /// <summary>The longest-banked package of that furniture, or null when none is banked.</summary>
    public static BankEntry? Oldest(IEnumerable<BankEntry> entries, int configId) =>
        entries.Where(entry => entry.ConfigId == configId).OrderBy(entry => entry.Slot).FirstOrDefault();

    /// <summary>
    /// The entry for a package another mod puts in: the slot after the newest, its color kept, not bought, nothing paid.
    /// It takes no size limit into account; the mod that deposits has nowhere else to put the package.
    /// </summary>
    public static BankEntry Deposit(IEnumerable<BankEntry> entries, int configId, int dye) =>
        new(BankCounters.NextSlot(entries.Select(entry => entry.Slot)), configId, dye, BankCounters.UnknownPaid, 0, false);
}
