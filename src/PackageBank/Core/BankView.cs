using System.Globalization;
using System.Linq;
using System.Text;

namespace PackageBank.Core;

/// <summary>
/// The JSON page.js draws the panel and the shop's bank rows from. Every button the page draws is enabled or greyed
/// here, with the reason as a key into the page's words, so the page holds no rules of its own.
/// </summary>
public static class BankView
{
    /// <summary>Null when there is nothing to show: no game, or the player is away from home.</summary>
    public static string? Of(BankRead read, BankLimits limits) => read is BankRead.Ready ready ? Of(ready.Bank, limits) : null;

    public static string Of(Bank bank, BankLimits limits)
    {
        var json = new StringBuilder(512);

        json.Append("{\"limited\":").Append(Bool(limits.IsLimited));
        json.Append(",\"spots\":").Append(Num(bank.FreeSpots));
        json.Append(",\"sizes\":[").Append(string.Join(",", PackageSizes.All.Select(size => Size(bank, limits, size)))).Append(']');
        json.Append(",\"home\":[").Append(string.Join(",", bank.Home.Select(package => Home(bank, limits, package)))).Append(']');
        json.Append(",\"bank\":[").Append(string.Join(",", bank.Banked.Select(Banked))).Append(']');
        json.Append(",\"takeWhy\":").Append(bank.FreeSpots > 0 ? "null" : Str("noSpot"));
        json.Append(",\"menu\":").Append(Menu(bank, limits));
        json.Append(",\"shop\":").Append(bank.Shop is null ? "null" : "[" + string.Join(",", bank.Shop.Select(row => ShopRow(bank, limits, row)).Where(row => row != null)) + "]");

        return json.Append('}').ToString();
    }

    private static string Size(Bank bank, BankLimits limits, PackageSize size)
    {
        var used = bank.Banked.Count(banked => banked.Size == size);
        var capacity = limits.Capacity(size, bank.Bonus);

        return "{\"key\":" + Str(PackageSizes.Key(size)) + ",\"used\":" + Num(used) + ",\"cap\":" + (capacity is { } cap ? Num(cap) : "null") + "}";
    }

    private static string Home(Bank bank, BankLimits limits, HomePackage package) =>
        "{\"id\":" + Str(package.Id.ToString(CultureInfo.InvariantCulture))
        + ",\"name\":" + Str(package.Name)
        + ",\"icon\":" + Str(package.Icon)
        + ",\"size\":" + SizeKey(package.Size)
        + ",\"looted\":" + Bool(!package.IsPurchased)
        + ",\"why\":" + Why(BankRun.Refusal(bank, limits, package))
        + "}";

    private static string Banked(BankedPackage banked) =>
        "{\"slot\":" + Num(banked.Entry.Slot)
        + ",\"name\":" + Str(banked.Name)
        + ",\"icon\":" + Str(banked.Icon)
        + ",\"size\":" + SizeKey(banked.Size)
        + ",\"looted\":" + Bool(!banked.Entry.IsPurchased)
        + "}";

    /// <summary>Null unless the game's click menu is open on a yard package the bank lists; the car's and the doors' boxes are not.</summary>
    private static string Menu(Bank bank, BankLimits limits)
    {
        var package = bank.Home.FirstOrDefault(candidate => candidate.Id == bank.Menu);

        return package is null ? "null" : "{\"why\":" + Why(BankRun.Refusal(bank, limits, package)) + "}";
    }

    /// <summary>A shop row with no bought yard package behind it gets no Bank button, so it is left out.</summary>
    private static string? ShopRow(Bank bank, BankLimits limits, ShopRow row)
    {
        var package = BankRun.PackageFor(bank, row);

        return package is null ? null : "{\"index\":" + Num(row.Index) + ",\"why\":" + Why(BankRun.Refusal(bank, limits, package)) + "}";
    }

    private static string Why(BankOutcome? refusal) => refusal switch
    {
        null => "null",
        BankOutcome.Busy => Str("busy"),
        BankOutcome.Full => Str("full"),
        _ => Str("notBankable"),
    };

    private static string SizeKey(PackageSize? size) => size is { } known ? Str(PackageSizes.Key(known)) : "null";

    private static string Str(string? text) => text is null ? "null" : JsText.Literal(text);

    private static string Num(int number) => number.ToString(CultureInfo.InvariantCulture);

    private static string Bool(bool value) => value ? "true" : "false";
}
