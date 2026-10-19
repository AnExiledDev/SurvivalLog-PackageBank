using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using PackageBank.Adapters;
using PackageBank.Core;

namespace PackageBank;

[BepInPlugin(Guid, "Package Bank", Version)]
[BepInDependency(OptionalLive.ToolkitGuid, BepInDependency.DependencyFlags.SoftDependency)]
public sealed class Plugin : BasePlugin
{
    private const string Guid = "com.dev.survivallog.packagebank";
    private const string Version = "0.1.0";
    private const int MaxSlots = 100;

    public override void Load()
    {
        if (!OptionalLive.IsToolkitInstalled) Log.LogInfo(OptionalLive.MissingNote);

        var limitBySize = Config.Bind("Bank", "LimitBySize", true,
            "Limit how many packages of each size the bank holds, set by LargeSlots, MediumSlots and SmallSlots. Set to false for no limit. Story items never have a limit.\n" +
            "限制仓库中每种尺寸的包裹数量，由 LargeSlots、MediumSlots 和 SmallSlots 设置。设为 false 则不限数量。剧情物品始终不限数量。");
        var largeSlots = Config.Bind("Bank", "LargeSlots", 4, new ConfigDescription(
            "How many large packages the bank holds while LimitBySize is on. 4 by default, from 0 to 100.\n" +
            "LimitBySize 开启时仓库可存放的大型包裹数量。默认 4，可设为 0 到 100。",
            new AcceptableValueRange<int>(0, MaxSlots)));
        var mediumSlots = Config.Bind("Bank", "MediumSlots", 6, new ConfigDescription(
            "How many medium packages the bank holds while LimitBySize is on. 6 by default, from 0 to 100.\n" +
            "LimitBySize 开启时仓库可存放的中型包裹数量。默认 6，可设为 0 到 100。",
            new AcceptableValueRange<int>(0, MaxSlots)));
        var smallSlots = Config.Bind("Bank", "SmallSlots", 8, new ConfigDescription(
            "How many small packages the bank holds while LimitBySize is on. 8 by default, from 0 to 100.\n" +
            "LimitBySize 开启时仓库可存放的小型包裹数量。默认 8，可设为 0 到 100。",
            new AcceptableValueRange<int>(0, MaxSlots)));
        var log = new BankLog(Log, Path.Combine(Paths.BepInExRootPath, "package-bank.log"));
        var harmony = new Harmony(Guid);
        var session = new BankSession(new GameBank(new GameShop(log.Write), log.Write), new WebPage(log.Write), log.Write);

        log.Write($"===== Package Bank {Version} started");
        InputGate.Apply(harmony, log.Write);
        PageMessages.Apply(harmony, log.Write);

        OptionalLive.Watch(() =>
        {
            var limits = new BankLimits(limitBySize.Value, largeSlots.Value, mediumSlots.Value, smallSlots.Value);

            session.UseLimits(limits);
            log.Write(limits.IsLimited ? $"limits: {limits.Large} large, {limits.Medium} medium, {limits.Small} small" : "limits off");
        }, limitBySize, largeSlots, mediumSlots, smallSlots);

        BankTicker.Session = session;
        BankTicker.Log = log.Write;
        AddComponent<BankTicker>();
    }
}
