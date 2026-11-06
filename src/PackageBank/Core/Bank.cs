using System.Collections.Generic;

namespace PackageBank.Core;

/// <summary>
/// A package on one of the home's yard spots. Size is null when it cannot be banked; IsBusy when the player is
/// carrying or installing it. Paid is -1 when it was not bought or the game did not record the price.
/// </summary>
public sealed record HomePackage(long Id, int ConfigId, string Name, string? Icon, PackageSize? Size, bool IsBusy, bool IsPurchased, int Paid, long Source, int Dye);

/// <summary>A package in the bank, with its name and icon read from the game.</summary>
public sealed record BankedPackage(BankEntry Entry, string Name, string? Icon, PackageSize? Size);

/// <summary>A row of the build shop's own package list: its place in the list, what it is, and what was paid.</summary>
public readonly record struct ShopRow(int Index, int ConfigId, int Paid);

/// <summary>
/// A placed piece of furniture the game's click menu is open on, one the game lets the player move. Size is the bank
/// shelf its package goes on, null when the bank cannot take it. HoldsItems is a storage piece with something still in
/// it. Block is the game's own reason it cannot be taken down right now (in use, switched on, a plant growing), null
/// when it can. IsStory is a piece the story uses. IsInLayout: the game's Layout screen is open, where a yard package cannot be clicked.
/// </summary>
public sealed record PlacedPiece(long Id, int ConfigId, string Name, PackageSize? Size, int Dye, bool HoldsItems, string? Block, bool IsStory, bool IsInLayout = false);

/// <summary>
/// The bank and the home's packages, read in one go. FreeSpots is how many yard spots are empty. Shop is the build
/// shop's package list while that shop is open, null otherwise. Menu is the id of what the game's click menu (Install,
/// Return) is open on, null while that menu is shut. Piece is the placed furniture that menu is open on, when it is on
/// one Pack up can work on.
/// </summary>
public sealed record Bank(IReadOnlyList<HomePackage> Home, IReadOnlyList<BankedPackage> Banked, BankBonus Bonus, int FreeSpots, IReadOnlyList<ShopRow>? Shop, long? Menu = null, PlacedPiece? Piece = null);

/// <summary>What a read of the game found.</summary>
public abstract record BankRead
{
    private BankRead()
    {
    }

    /// <summary>The title screen, a save loading, or the prologue.</summary>
    public sealed record NoGame : BankRead;

    /// <summary>A game is loaded but the player is not at home, where the packages are.</summary>
    public sealed record Away : BankRead;

    public sealed record Ready(Bank Bank) : BankRead;
}

/// <summary>The game side of the bank.</summary>
public interface IBankGame
{
    /// <summary>Where the player is and which bank screens are open: cheap enough for every refresh.</summary>
    BankState State();

    /// <summary>
    /// A number that changes whenever anything Read would return changes (a save loaded, a package moved, banked,
    /// shown or dyed, the shop's list, the click menu, the icons arriving), so an unchanged print skips the read. Asked
    /// only at home.
    /// </summary>
    long Print();

    BankRead Read();

    /// <summary>
    /// Writes the entry to the save and takes the package out of the yard, with no refund. ShopIndex is the build shop
    /// row it was banked from, when it was. False when it could not be done, with nothing changed.
    /// </summary>
    bool Store(HomePackage package, BankEntry entry, int? shopIndex);

    /// <summary>Puts the package on a free yard spot and clears its entry. The new package's id, or 0 when it could not.</summary>
    long TakeOut(BankEntry entry);

    /// <summary>
    /// Takes the placed piece down and puts its package, in its color, on a free yard spot, refundable at the shop's
    /// price. False when it could not be done, with nothing changed.
    /// </summary>
    bool PackUp(PlacedPiece piece);

    /// <summary>Takes the placed piece down straight into the bank under this entry. False when it could not, with nothing changed.</summary>
    bool PackUpIntoBank(PlacedPiece piece, BankEntry entry);
}
