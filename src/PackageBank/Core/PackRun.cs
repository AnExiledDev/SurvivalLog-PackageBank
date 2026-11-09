using System.Linq;

namespace PackageBank.Core;

/// <summary>
/// Pack up: a placed piece of furniture back into its package. The package goes on a free yard spot, or straight into
/// the bank when the yard has none and the piece's shelf has room. Every packed piece can be refunded at the shop's
/// price, wherever it came from.
/// </summary>
public static class PackRun
{
    /// <summary>The game's own "price not recorded" (BuildComponent.PAID_UNKNOWN): its refund then charges the current shop price.</summary>
    public const int PaidUnknown = -1;

    /// <summary>
    /// The piece is read at the press, not when the page drew the entry, so a menu reopened on another piece in between
    /// packs the one the player is looking at.
    /// </summary>
    public static BankOutcome PackMenu(IBankGame game, BankLimits limits)
    {
        if (!limits.IsPackUpOn) return new BankOutcome.Gone();

        return game.Read() switch
        {
            BankRead.Ready { Bank.Piece: { } piece } ready => PackFound(game, limits, ready.Bank, piece),
            BankRead.Ready => new BankOutcome.Gone(),
            BankRead.Away => new BankOutcome.NotHome(),
            _ => new BankOutcome.NoGame(),
        };
    }

    /// <summary>Why the piece cannot be packed up right now, or null when it can.</summary>
    public static BankOutcome? Refusal(Bank bank, BankLimits limits, PlacedPiece piece)
    {
        if (piece.HoldsItems) return new BankOutcome.HoldsItems(piece.Name);
        if (piece.Block is { } reason) return new BankOutcome.Blocked(piece.Name, reason);
        if (bank.FreeSpots > 0) return null;
        if (piece.Size is not { } size || !BankRun.HasRoom(bank, limits, size)) return new BankOutcome.NoRoom(piece.Name);

        return null;
    }

    /// <summary>With no free yard spot, a piece Pack up takes goes into the bank.</summary>
    public static bool GoesToBank(Bank bank) => bank.FreeSpots <= 0;

    private static BankOutcome PackFound(IBankGame game, BankLimits limits, Bank bank, PlacedPiece piece)
    {
        var refusal = Refusal(bank, limits, piece);

        if (refusal is not null) return refusal;

        var isIntoBank = GoesToBank(bank);
        var isDone = isIntoBank ? game.PackUpIntoBank(piece, EntryFor(bank, piece)) : game.PackUp(piece);

        return isDone ? new BankOutcome.PackedUp(piece.Name, isIntoBank, piece.IsStory, piece.IsInLayout && !isIntoBank) : new BankOutcome.Failed();
    }

    /// <summary>Refundable at the shop's price: no paid price is kept, so the game works it out at the refund, and no shop restocks it.</summary>
    private static BankEntry EntryFor(Bank bank, PlacedPiece piece)
    {
        var slot = BankCounters.NextSlot(bank.Banked.Select(banked => banked.Entry.Slot));

        return new BankEntry(slot, piece.ConfigId, piece.Dye, Paid: PaidUnknown, Source: 0, IsPurchased: true);
    }
}
