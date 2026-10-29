using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using PackageBank.Adapters;
using PackageBank.Api;
using PackageBank.Core;

namespace PackageBank;

[BepInPlugin(Guid, "Package Bank", Version)]
[BepInDependency(OptionalLive.ToolkitGuid, BepInDependency.DependencyFlags.SoftDependency)]
public sealed class Plugin : BasePlugin
{
    private const string Guid = "com.dev.survivallog.packagebank";
    private const string Version = "0.5.0";
    private const int MaxSlots = 100;
    private const string CoreNamespace = "PackageBank.Core";
    private const string AdaptersNamespace = "PackageBank.Adapters";
    private const BindingFlags Declared = BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public override void Load()
    {
        if (!OptionalLive.IsToolkitInstalled) Log.LogInfo(OptionalLive.MissingNote);

        OptionalUiSize.Bind(Config);

        var limitBySize = Config.Bind("Bank", "LimitBySize", true,
            "Limit how many packages of each size the bank holds, set by LargeSlots, MediumSlots, SmallSlots and DefenseSlots. Set to false for no limit. Story items never have a limit.\n" +
            "限制仓库中每种尺寸的包裹数量，由 LargeSlots、MediumSlots、SmallSlots 和 DefenseSlots 设置。设为 false 则不限数量。剧情物品始终不限数量。");
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
        var defenseSlots = Config.Bind("Bank", "DefenseSlots", 4, new ConfigDescription(
            "How many defense packages (sandbags, spike barriers and other defenses) the bank holds while LimitBySize is on. 4 by default, from 0 to 100.\n" +
            "LimitBySize 开启时仓库可存放的防御设施包裹（沙包、尖刺障碍等）数量。默认 4，可设为 0 到 100。",
            new AcceptableValueRange<int>(0, MaxSlots)));
        var log = new BankLog(Log, Path.Combine(Paths.BepInExRootPath, "package-bank.log"));
        var harmony = new Harmony(Guid);
        var bank = new GameBank(new GameShop(log.Write), log.Write);
        var session = new BankSession(bank, new WebPage(log.Write), log.Write);

        PackageBankApi.Use(bank, log.Write);

        log.Write($"===== Package Bank {Version} started");
        InputGate.Apply(harmony, log.Write);
        PageMessages.Apply(harmony, log.Write);
        session.MenuHooked(MenuOpening.Apply(harmony, session.MenuOpened, log.Write));

        OptionalLive.Watch(() =>
        {
            var limits = new BankLimits(limitBySize.Value, largeSlots.Value, mediumSlots.Value, smallSlots.Value, defenseSlots.Value);

            session.UseLimits(limits);
            log.Write(limits.IsLimited ? $"limits: {limits.Large} large, {limits.Medium} medium, {limits.Small} small, {limits.Defense} defense" : "limits off");
        }, limitBySize, largeSlots, mediumSlots, smallSlots, defenseSlots);

        BankTicker.Session = session;
        BankTicker.Log = log.Write;
        AddComponent<BankTicker>();
        WarmUpInBackground();
    }

    /// <summary>
    /// Compiles the mod's own code on a worker without running it, so the first click menu, shop or panel of a session
    /// does not compile the read, the view and the page calls on the game's frame. Nothing on this thread touches the game.
    /// </summary>
    private static void WarmUpInBackground() =>
        System.Threading.ThreadPool.QueueUserWorkItem(_ =>
        {
            try
            {
                foreach (var type in typeof(Plugin).Assembly.GetTypes())
                {
                    if (type.Namespace is CoreNamespace or AdaptersNamespace && !typeof(UnityEngine.MonoBehaviour).IsAssignableFrom(type)) Prepare(type);
                }
            }
            catch (Exception)
            {
                // Only a head start; the first real use compiles them anyway.
            }
        });

    private static void Prepare(Type type)
    {
        if (type.ContainsGenericParameters) return;

        foreach (var method in type.GetMethods(Declared))
        {
            if (!method.IsAbstract && !method.ContainsGenericParameters) RuntimeHelpers.PrepareMethod(method.MethodHandle);
        }
    }
}
