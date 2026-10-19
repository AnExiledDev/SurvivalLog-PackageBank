using System.Linq;

namespace PackageBank.Core;

/// <summary>What a press did, or why it did nothing.</summary>
public abstract record BankOutcome
{
    private BankOutcome()
    {
    }

    public sealed record NoGame : BankOutcome;

    public sealed record NotHome : BankOutcome;

    /// <summary>The package is no longer where the press saw it.</summary>
    public sealed record Gone : BankOutcome;

    public sealed record NotBankable(string Name) : BankOutcome;

    public sealed record Busy(string Name) : BankOutcome;

    public sealed record Full(PackageSize Size, int Capacity) : BankOutcome;

    public sealed record Stored(string Name) : BankOutcome;

    public sealed record NoSpot : BankOutcome;

    public sealed record TakenOut(string Name) : BankOutcome;

    public sealed record Failed : BankOutcome;
}

/// <summary>The bank's presses: bank a yard package (from the panel or the game's click menu), bank a build shop row, and take a package out.</summary>
public static class BankRun
{
    public static BankOutcome Store(IBankGame game, BankLimits limits, long packageId)
    {
        return game.Read() switch
        {
            BankRead.Ready ready => StoreFound(game, limits, ready.Bank, ready.Bank.Home.FirstOrDefault(package => package.Id == packageId), shopIndex: null),
            BankRead.Away => new BankOutcome.NotHome(),
            _ => new BankOutcome.NoGame(),
        };
    }

    /// <summary>
    /// The menu's package is read at the press, not when the page drew the entry, so a menu reopened on another package
    /// in between banks the one the player is looking at.
    /// </summary>
    public static BankOutcome StoreMenu(IBankGame game, BankLimits limits)
    {
        return game.Read() switch
        {
            BankRead.Ready { Bank.Menu: { } id } ready => Store(game, limits, id),
            BankRead.Ready => new BankOutcome.Gone(),
            BankRead.Away => new BankOutcome.NotHome(),
            _ => new BankOutcome.NoGame(),
        };
    }

    /// <summary>
    /// The shop lists what it sold, not which yard package each row is. The row stands for a bought package of the same
    /// furniture that is not being carried, preferring one bought for the same price, so the right refund goes with it.
    /// </summary>
    public static BankOutcome StoreShopRow(IBankGame game, BankLimits limits, int rowIndex)
    {
        switch (game.Read())
        {
            case BankRead.Ready ready:
                var bank = ready.Bank;
                var rows = bank.Shop?.Where(candidate => candidate.Index == rowIndex).ToList();

                if (rows is not { Count: > 0 }) return new BankOutcome.Gone();

                return StoreFound(game, limits, bank, PackageFor(bank, rows[0]), rows[0].Index);
            case BankRead.Away:
                return new BankOutcome.NotHome();
            default:
                return new BankOutcome.NoGame();
        }
    }

    public static BankOutcome TakeOut(IBankGame game, int slot)
    {
        switch (game.Read())
        {
            case BankRead.Ready ready:
                var banked = ready.Bank.Banked.FirstOrDefault(candidate => candidate.Entry.Slot == slot);

                if (banked is null) return new BankOutcome.Gone();
                if (ready.Bank.FreeSpots <= 0) return new BankOutcome.NoSpot();

                return game.TakeOut(banked.Entry) != 0 ? new BankOutcome.TakenOut(banked.Name) : new BankOutcome.Failed();
            case BankRead.Away:
                return new BankOutcome.NotHome();
            default:
                return new BankOutcome.NoGame();
        }
    }

    /// <summary>The yard package a shop row stands for, or null when none of that furniture was bought.</summary>
    public static HomePackage? PackageFor(Bank bank, ShopRow row)
    {
        var sold = bank.Home.Where(package => package.IsPurchased && package.ConfigId == row.ConfigId).ToList();
        var free = sold.Where(package => !package.IsBusy).ToList();

        return free.FirstOrDefault(package => package.Paid == row.Paid) ?? free.FirstOrDefault() ?? sold.FirstOrDefault();
    }

    /// <summary>Why a package cannot go in the bank right now, or null when it can.</summary>
    public static BankOutcome? Refusal(Bank bank, BankLimits limits, HomePackage package)
    {
        if (package.Size is not { } size) return new BankOutcome.NotBankable(package.Name);
        if (package.IsBusy) return new BankOutcome.Busy(package.Name);

        var capacity = limits.Capacity(size, bank.Bonus);

        if (capacity is { } cap && bank.Banked.Count(banked => banked.Size == size) >= cap) return new BankOutcome.Full(size, cap);

        return null;
    }

    private static BankOutcome StoreFound(IBankGame game, BankLimits limits, Bank bank, HomePackage? package, int? shopIndex)
    {
        if (package is null) return new BankOutcome.Gone();

        var refusal = Refusal(bank, limits, package);

        if (refusal is not null) return refusal;

        var slot = BankCounters.NextSlot(bank.Banked.Select(banked => banked.Entry.Slot));
        var entry = new BankEntry(slot, package.ConfigId, package.Dye, package.Paid, package.Source, package.IsPurchased);

        return game.Store(package, entry, shopIndex) ? new BankOutcome.Stored(package.Name) : new BankOutcome.Failed();
    }
}
