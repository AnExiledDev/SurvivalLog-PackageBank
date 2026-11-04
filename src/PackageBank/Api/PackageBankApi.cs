using System;
using PackageBank.Adapters;
using PackageBank.Core;

namespace PackageBank.Api;

/// <summary>
/// For other mods. Call only on the game's main thread, at home, with a game loaded. Every call fails soft: an error
/// answers as an empty bank or a take-out that did not happen.
/// </summary>
public static class PackageBankApi
{
    /// <summary>TakeOut's answer when every yard package spot is taken, so nothing was taken out.</summary>
    public const long NoFreeSpot = -1;

    private static GameBank? _bank;
    private static Action<string> _log = _ => { };

    internal static void Use(GameBank bank, Action<string> log)
    {
        _bank = bank;
        _log = log;
    }

    /// <summary>The furniture id of every banked package, one per package, oldest first. Empty with no game loaded.</summary>
    public static int[] BankedFurniture()
    {
        try
        {
            return _bank == null ? Array.Empty<int>() : BankShelf.Furniture(_bank.Entries());
        }
        catch (Exception ex)
        {
            _log("another mod's read of the bank failed: " + ex.Message);

            return Array.Empty<int>();
        }
    }

    /// <summary>
    /// Takes the longest-banked package of that furniture out onto a free yard spot, as the bank's own Take out does.
    /// The new yard package's id; <see cref="NoFreeSpot"/> when every spot is taken; 0 when none is banked or it failed.
    /// </summary>
    public static long TakeOut(int configId)
    {
        try
        {
            var bank = _bank;
            var entry = bank == null ? null : BankShelf.Oldest(bank.Entries(), configId);

            if (bank == null || entry == null) return 0;
            if (bank.FreeSpots() <= 0) return NoFreeSpot;

            var id = bank.TakeOut(entry);

            _log(id == 0 ? $"another mod asked for furniture {configId}, taking it out failed" : $"another mod took furniture {configId} out as package {id}");

            return id;
        }
        catch (Exception ex)
        {
            _log("another mod's take-out failed: " + ex.Message);

            return 0;
        }
    }

    /// <summary>
    /// Puts a package of that furniture, in that color, into the bank, whatever its size and however full the bank is.
    /// For a mod that has taken the furniture away and has nowhere else to put it. False when no game is loaded or it
    /// failed, so the caller keeps the furniture.
    /// </summary>
    public static bool Deposit(int configId, int dye)
    {
        try
        {
            var bank = _bank;

            if (bank == null || configId <= 0) return false;

            var entry = BankShelf.Deposit(bank.Entries(), configId, dye);

            if (!bank.Deposit(entry)) return false;

            _log($"another mod put furniture {configId} (color {dye}) into the bank, slot {entry.Slot}");

            return true;
        }
        catch (Exception ex)
        {
            _log("another mod's deposit failed: " + ex.Message);

            return false;
        }
    }
}
