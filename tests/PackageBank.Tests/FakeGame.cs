using PackageBank.Core;

namespace PackageBank.Tests;

/// <summary>
/// A home yard and a save in memory. The bank lives in Counters exactly as the plugin keeps it in the save, so every
/// test that banks and takes out also goes through the real counter format.
/// </summary>
internal sealed class FakeGame : IBankGame
{
    private long _nextId = 1000;

    public List<HomePackage> Home { get; } = new();

    public Dictionary<string, int> Counters { get; } = new();

    /// <summary>The build shop's package list while it is open, null while it is shut.</summary>
    public List<ShopRow>? Shop { get; set; }

    /// <summary>The package the game's click menu is open on, null while it is shut.</summary>
    public long? Menu { get; set; }

    /// <summary>Web icon paths, as the plugin finds them under the WebUI folder; furniture not listed has none.</summary>
    public Dictionary<int, string> Icons { get; } = new();

    public int FreeSpots { get; set; } = 3;

    public bool IsAway { get; set; }

    public bool IsNoGame { get; set; }

    /// <summary>The game refusing to take a package off the yard.</summary>
    public bool IsStoreBroken { get; set; }

    /// <summary>The shelf each furniture goes on, as the game's tables say; what is not listed cannot be banked.</summary>
    public Dictionary<int, PackageSize> Sizes { get; } = new()
    {
        [Bed] = PackageSize.Large,
        [Table] = PackageSize.Medium,
        [Lamp] = PackageSize.Small,
        [Recorder] = PackageSize.Story,
        [Sandbags] = PackageSize.Defense,
    };

    public const int Bed = 30;
    public const int Table = 20;
    public const int Lamp = 10;
    public const int Recorder = 90;
    public const int Barricade = 50;
    public const int Sandbags = 36001;

    /// <summary>The shop the packages in these tests were bought from.</summary>
    public const long ShopId = 0x1_0000_0002L;

    public HomePackage Add(int configId, int paid = 100, bool isPurchased = true, bool isBusy = false, int dye = 0)
    {
        var package = new HomePackage(_nextId++, configId, NameOf(configId), null, SizeOf(configId), isBusy, isPurchased, isPurchased ? paid : -1, isPurchased ? ShopId : 0, dye);

        Home.Add(package);

        return package;
    }

    public BankRead Read()
    {
        if (IsNoGame) return new BankRead.NoGame();
        if (IsAway) return new BankRead.Away();

        var contents = BankCounters.Read(Counters);
        var banked = contents.Entries.Select(entry => new BankedPackage(entry, NameOf(entry.ConfigId), Icons.GetValueOrDefault(entry.ConfigId), SizeOf(entry.ConfigId))).ToList();

        return new BankRead.Ready(new Bank(Home.ToList(), banked, contents.Bonus, FreeSpots, Shop?.ToList(), Menu));
    }

    public bool Store(HomePackage package, BankEntry entry, int? shopIndex)
    {
        if (IsStoreBroken) return false;

        foreach (var (key, value) in BankCounters.Write(entry)) Counters[key] = value;

        Home.RemoveAll(candidate => candidate.Id == package.Id);
        FreeSpots++;

        if (Shop != null && shopIndex is { } index) Shop = Shop.Where(row => row.Index != index).Select((row, i) => row with { Index = i }).ToList();

        return true;
    }

    public long TakeOut(BankEntry entry)
    {
        var package = new HomePackage(_nextId++, entry.ConfigId, NameOf(entry.ConfigId), null, SizeOf(entry.ConfigId), false, entry.IsPurchased, entry.Paid, entry.Source, entry.Dye);

        Home.Add(package);
        FreeSpots--;

        foreach (var key in BankCounters.Keys(entry.Slot)) Counters.Remove(key);

        return package.Id;
    }

    private PackageSize? SizeOf(int configId) => Sizes.TryGetValue(configId, out var size) ? size : null;

    private static string NameOf(int configId) => configId switch
    {
        Bed => "Bed",
        Table => "Table",
        Lamp => "Lamp",
        Recorder => "Recorder",
        Barricade => "Barricade",
        Sandbags => "Sandbags",
        _ => "Package " + configId,
    };
}
