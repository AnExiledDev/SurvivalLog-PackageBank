using System;
using System.Globalization;

namespace PackageBank.Core;

/// <summary>
/// What the page script asks for. It sends "slmod|packagebank|" followed by a verb and, for a row button, the package
/// id, bank slot or shop row; anything else parses to Unknown so it can be logged rather than acted on (the script's
/// "diag|..." notes land there on purpose).
/// </summary>
public abstract record PageCommand
{
    public const string Prefix = "slmod|packagebank|";

    private PageCommand()
    {
    }

    /// <summary>The HUD button was clicked: open the panel, or close it when it is open.</summary>
    public sealed record Toggle : PageCommand;

    public sealed record Close : PageCommand;

    /// <summary>A yard package's Bank button in the panel.</summary>
    public sealed record Store(long PackageId) : PageCommand;

    /// <summary>A banked package's Take out button, in the panel or the shop.</summary>
    public sealed record TakeOut(int Slot) : PageCommand;

    /// <summary>The Bank button beside a row of the build shop's package list.</summary>
    public sealed record StoreShopRow(int Index) : PageCommand;

    /// <summary>The page script just installed itself.</summary>
    public sealed record Ready : PageCommand;

    /// <summary>The Bank entry added to the game's click menu on a yard package.</summary>
    public sealed record StoreMenu : PageCommand;

    /// <summary>The page has no script, usually because the web view reloaded.</summary>
    public sealed record NoScript : PageCommand;

    public sealed record Unknown(string Text) : PageCommand;

    public static bool IsOurs(string? message) => message is not null && message.StartsWith(Prefix, StringComparison.Ordinal);

    /// <summary>Parses a whole message, prefix included.</summary>
    public static PageCommand Parse(string message)
    {
        if (!IsOurs(message)) return new Unknown(message);

        var parts = message.Substring(Prefix.Length).Split('|');

        return (parts[0], parts.Length) switch
        {
            ("toggle", 1) => new Toggle(),
            ("close", 1) => new Close(),
            ("store", 2) when long.TryParse(parts[1], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var id) => new Store(id),
            ("takeout", 2) when TryNumber(parts[1], out var slot) => new TakeOut(slot),
            ("bank", 2) when TryNumber(parts[1], out var index) => new StoreShopRow(index),
            ("menubank", 1) => new StoreMenu(),
            ("ready", 1) => new Ready(),
            ("noscript", 1) => new NoScript(),
            _ => new Unknown(message),
        };
    }

    private static bool TryNumber(string text, out int number) => int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out number);
}
