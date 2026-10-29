using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PackageBank.Core;

/// <summary>One piece of on-screen text in the two languages the game ships. {0}, {1} are filled in by Say.</summary>
public readonly record struct Phrase(string En, string Zh)
{
    public string Pick(bool isChinese) => isChinese ? Zh : En;

    public string Say(bool isChinese, params object[] args) => string.Format(CultureInfo.InvariantCulture, Pick(isChinese), args);
}

/// <summary>The words the mod puts on screen: press results and the panel's own labels.</summary>
public static class Texts
{
    private const string Log = "BepInEx/package-bank.log";

    public static readonly Phrase NoGame = new("Load a game first.", "请先载入游戏。");
    public static readonly Phrase NotHome = new("Go home first: your packages are there.", "请先回家：你的包裹在那里。");
    public static readonly Phrase Gone = new("That package is gone.", "这个包裹已经不在了。");
    public static readonly Phrase NotBankable = new("{0} can't go in the bank.", "{0}无法存入仓库。");
    public static readonly Phrase Busy = new("{0} is being moved or installed.", "{0}正在搬运或安装。");

    /// <summary>{0} is the size, {1} the limit.</summary>
    public static readonly Phrase Full = new("The bank is full for {0} packages ({1} of {1}).", "仓库的{0}包裹已存满（{1}/{1}）。");

    public static readonly Phrase Stored = new("{0} put in the bank.", "{0}已存入仓库。");
    public static readonly Phrase NoSpot = new("No free package spot at home: install, refund or bank a package first.", "家里没有空的包裹位置：请先安装、退还或存入一个包裹。");
    public static readonly Phrase TakenOut = new("{0} taken out of the bank.", "{0}已从仓库取出。");
    /// <summary>Who a toast is from, on its first line.</summary>
    public static readonly Phrase Source = new("Package Bank", "包裹仓库");

    public static readonly Phrase Failed = new("Package Bank failed, see " + Log + ".", "Package Bank 出错，详见 " + Log + "。");

    public static readonly Phrase Large = new("large", "大型");
    public static readonly Phrase Medium = new("medium", "中型");
    public static readonly Phrase Small = new("small", "小型");
    public static readonly Phrase Story = new("story", "剧情");
    public static readonly Phrase Defense = new("defense", "防御设施");

    /// <summary>The words page.js draws the panel, the HUD button's tip and the shop's bank rows with, by the key it looks them up by.</summary>
    public static readonly IReadOnlyList<(string Key, Phrase Phrase)> Page = new (string, Phrase)[]
    {
        ("title", new("Package Bank", "包裹仓库")),
        ("tip", new("Store packages for later and take them out again", "把包裹存起来，需要时再取出")),
        ("counts", new("{0} in the yard, {1} in the bank", "院子里 {0} 个，仓库中 {1} 个")),
        ("inYard", new("In the yard", "院子里")),
        ("inBankTitle", new("In the bank", "仓库中")),
        ("inBank", new("In the bank ({0})", "仓库中（{0}）")),
        ("bank", new("Bank", "存入")),
        ("takeOut", new("Take out", "取出")),
        ("large", new("Large", "大型")),
        ("medium", new("Medium", "中型")),
        ("small", new("Small", "小型")),
        ("story", new("Story Items", "剧情物品")),
        ("defense", new("Defenses", "防御设施")),
        ("noLimit", new("No limit", "无上限")),
        ("spotsFree", new("{0} free package spots in the yard", "院子里有 {0} 个空的包裹位置")),
        ("spotFree", new("1 free package spot in the yard", "院子里有 1 个空的包裹位置")),
        ("noRefund", new("Not refundable", "不可退还")),
        ("busy", new("Being moved or installed", "正在搬运或安装")),
        ("notBankable", new("Can't be banked", "无法存入")),
        ("full", new("Bank full for this size", "该尺寸已存满")),
        ("shelfFull", new("bank full", "仓库已满")),
        ("noSpot", new("No free package spot", "没有空的包裹位置")),
        ("noSpotNote", new("No free package spot in the yard. Install, refund or bank a package to take one out.", "院子里没有空的包裹位置。请先安装、退还或存入一个包裹，再取出。")),
        ("emptyHome", new("No packages in the yard.", "院子里没有包裹。")),
        ("emptyHomeHint", new("Packages you buy in the build shop land here.", "在建造商店买的包裹会出现在这里。")),
        ("emptyBank", new("The bank is empty.", "仓库是空的。")),
        ("emptyBankHint", new("Press Bank on a package in the yard to store it here.", "在院子里的包裹上按「存入」，就能把它存到这里。")),
        ("emptyShelf", new("Nothing from the {0} shelf here.", "这里没有「{0}」类的包裹。")),
        ("showing", new("Showing {0} only", "只显示「{0}」")),
        ("showAll", new("Show all", "显示全部")),
        ("escClose", new("Esc to close", "按 Esc 关闭")),
    };

    public static IReadOnlyList<Phrase> All { get; } = Collect();

    public static Phrase SizeName(PackageSize size) => size switch
    {
        PackageSize.Large => Large,
        PackageSize.Medium => Medium,
        PackageSize.Small => Small,
        PackageSize.Defense => Defense,
        _ => Story,
    };

    private static List<Phrase> Collect()
    {
        var all = new List<Phrase> { NoGame, NotHome, Gone, NotBankable, Busy, Full, Stored, NoSpot, TakenOut, Failed, Large, Medium, Small, Story, Defense };

        all.AddRange(Page.Select(word => word.Phrase));

        return all;
    }
}

/// <summary>A press result as the line the player reads.</summary>
public static class OutcomeMessage
{
    public static string Describe(BankOutcome outcome, bool isChinese) => outcome switch
    {
        BankOutcome.NoGame => Texts.NoGame.Pick(isChinese),
        BankOutcome.NotHome => Texts.NotHome.Pick(isChinese),
        BankOutcome.Gone => Texts.Gone.Pick(isChinese),
        BankOutcome.NotBankable notBankable => Texts.NotBankable.Say(isChinese, notBankable.Name),
        BankOutcome.Busy busy => Texts.Busy.Say(isChinese, busy.Name),
        BankOutcome.Full full => Texts.Full.Say(isChinese, Texts.SizeName(full.Size).Pick(isChinese), full.Capacity),
        BankOutcome.Stored stored => Texts.Stored.Say(isChinese, stored.Name),
        BankOutcome.NoSpot => Texts.NoSpot.Pick(isChinese),
        BankOutcome.TakenOut takenOut => Texts.TakenOut.Say(isChinese, takenOut.Name),
        BankOutcome.Failed => Texts.Failed.Pick(isChinese),
        _ => Texts.Failed.Pick(isChinese),
    };
}

/// <summary>The panel's own words in the game's language, as the statement that hands them to page.js before every call.</summary>
public static class PageWords
{
    private static readonly string English = Statement(isChinese: false);
    private static readonly string Chinese = Statement(isChinese: true);

    public static string Script(bool isChinese) => isChinese ? Chinese : English;

    private static string Statement(bool isChinese) =>
        "window.__slpkgbankWords={" + string.Join(",", Texts.Page.Select(word => JsText.Literal(word.Key) + ":" + JsText.Literal(word.Phrase.Pick(isChinese)))) + "};";
}
