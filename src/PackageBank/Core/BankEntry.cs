using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PackageBank.Core;

/// <summary>
/// One banked package: enough to put it back as it was. Slot is its place in the bank (oldest first), Dye its color,
/// and for a package bought from a shop, which shop (Source) and what it cost (Paid, -1 when the game did not know),
/// so the shop's own Refund still works after it is taken out.
/// </summary>
public sealed record BankEntry(int Slot, int ConfigId, int Dye, int Paid, long Source, bool IsPurchased);

/// <summary>How many more of each size a later reward can let the bank hold, on top of the configured limits.</summary>
public readonly record struct BankBonus(int Large, int Medium, int Small)
{
    public static readonly BankBonus None = new(0, 0, 0);

    public int For(PackageSize size) => size switch
    {
        PackageSize.Large => Large,
        PackageSize.Medium => Medium,
        PackageSize.Small => Small,
        _ => 0,
    };
}

/// <summary>The size limits from the settings. Story items never have one, and nothing does when limits are off.</summary>
public sealed record BankLimits(bool IsLimited, int Large, int Medium, int Small, int Defense)
{
    /// <summary>How many packages of a size the bank holds, or null for no limit.</summary>
    public int? Capacity(PackageSize size, BankBonus bonus)
    {
        if (!IsLimited) return null;

        int? configured = size switch
        {
            PackageSize.Large => Large,
            PackageSize.Medium => Medium,
            PackageSize.Small => Small,
            PackageSize.Defense => Defense,
            _ => null,
        };

        return configured + bonus.For(size);
    }
}

/// <summary>What the save's counters say the bank holds.</summary>
public sealed record BankContents(IReadOnlyList<BankEntry> Entries, BankBonus Bonus)
{
    /// <summary>The same entries in the same order and the same bonus: a re-read of the counters that changed nothing the bank shows.</summary>
    public bool SameAs(BankContents other) => Bonus == other.Bonus && Entries.SequenceEqual(other.Entries);
}

/// <summary>
/// The bank in the save's own counters (whole numbers by name), so it is saved, loaded and cleared with the save:
/// "PkgBank_3_cfg" is slot 3's furniture. A shop's id does not fit one counter, so it takes two halves. The
/// "PkgBank_Bonus..." counters are left for other mods to raise a size's limit.
/// </summary>
public static class BankCounters
{
    public const string Prefix = "PkgBank_";
    public const string BonusLarge = Prefix + "BonusLarge";
    public const string BonusMedium = Prefix + "BonusMedium";
    public const string BonusSmall = Prefix + "BonusSmall";

    private const string ConfigField = "cfg";
    private const string DyeField = "dye";
    private const string PaidField = "paid";
    private const string SourceHighField = "srcHi";
    private const string SourceLowField = "srcLo";
    private const string BoughtField = "bought";
    private const int UnknownPaid = -1;

    private static readonly string[] Fields = { ConfigField, DyeField, PaidField, SourceHighField, SourceLowField, BoughtField };

    public static IReadOnlyList<(string Key, int Value)> Write(BankEntry entry) => new[]
    {
        (Key(entry.Slot, ConfigField), entry.ConfigId),
        (Key(entry.Slot, DyeField), entry.Dye),
        (Key(entry.Slot, PaidField), entry.Paid),
        (Key(entry.Slot, SourceHighField), unchecked((int)(entry.Source >> 32))),
        (Key(entry.Slot, SourceLowField), unchecked((int)entry.Source)),
        (Key(entry.Slot, BoughtField), entry.IsPurchased ? 1 : 0),
    };

    /// <summary>Every counter a slot can have, to clear it when its package is taken out.</summary>
    public static IReadOnlyList<string> Keys(int slot) => Fields.Select(field => Key(slot, field)).ToList();

    /// <summary>A slot counts only when its furniture counter is there; anything else under the prefix is ignored.</summary>
    public static BankContents Read(IEnumerable<KeyValuePair<string, int>> counters)
    {
        var fieldsBySlot = new Dictionary<int, Dictionary<string, int>>();
        var bonus = BankBonus.None;

        foreach (var (key, value) in counters)
        {
            if (!key.StartsWith(Prefix, StringComparison.Ordinal)) continue;

            switch (key)
            {
                case BonusLarge:
                    bonus = bonus with { Large = Math.Max(0, value) };
                    continue;
                case BonusMedium:
                    bonus = bonus with { Medium = Math.Max(0, value) };
                    continue;
                case BonusSmall:
                    bonus = bonus with { Small = Math.Max(0, value) };
                    continue;
            }

            if (!TrySplit(key, out var slot, out var field)) continue;

            if (!fieldsBySlot.TryGetValue(slot, out var fields)) fieldsBySlot[slot] = fields = new Dictionary<string, int>();

            fields[field] = value;
        }

        var entries = fieldsBySlot
            .Where(slot => slot.Value.ContainsKey(ConfigField))
            .OrderBy(slot => slot.Key)
            .Select(slot => Entry(slot.Key, slot.Value))
            .ToList();

        return new BankContents(entries, bonus);
    }

    /// <summary>After the newest, so the bank keeps the order packages went in.</summary>
    public static int NextSlot(IEnumerable<int> usedSlots) => usedSlots.DefaultIfEmpty(-1).Max() + 1;

    private static BankEntry Entry(int slot, IReadOnlyDictionary<string, int> fields)
    {
        int Field(string name, int fallback) => fields.TryGetValue(name, out var value) ? value : fallback;

        var source = ((long)Field(SourceHighField, 0) << 32) | unchecked((uint)Field(SourceLowField, 0));

        return new BankEntry(slot, fields[ConfigField], Field(DyeField, 0), Field(PaidField, UnknownPaid), source, Field(BoughtField, 0) == 1);
    }

    private static string Key(int slot, string field) => Prefix + slot.ToString(CultureInfo.InvariantCulture) + "_" + field;

    private static bool TrySplit(string key, out int slot, out string field)
    {
        var rest = key.Substring(Prefix.Length);
        var underscore = rest.IndexOf('_');
        field = underscore < 0 ? "" : rest.Substring(underscore + 1);
        slot = 0;

        return underscore > 0
            && Array.IndexOf(Fields, field) >= 0
            && int.TryParse(rest.Substring(0, underscore), NumberStyles.None, CultureInfo.InvariantCulture, out slot);
    }
}
