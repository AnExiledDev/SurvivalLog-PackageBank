using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using ExiledToolkit;
using ExiledToolkit.Game;
using ExiledToolkit.Kit;
using HarmonyLib;
using PackageBank.Adapters;
using PackageBank.Api;
using PackageBank.Core;

namespace PackageBank;

[BepInPlugin(Guid, "Package Bank", Version)]
[BepInDependency(ToolkitGuid, ToolkitVersion)]
public sealed class Plugin : BasePlugin
{
    private const string Guid = "com.dev.survivallog.packagebank";
    private const string Version = "0.8.1";

    /// <summary>Exiled's Toolkit: its shared helpers, toasts, live settings and UI size all come from it.</summary>
    private const string ToolkitGuid = "com.dev.survivallog.exiledtoolkit";
    private const string ToolkitVersion = ">=0.19.0";

    private const string LogFileName = "package-bank.log";

    /// <summary>The name the page script hands its panel to the Toolkit's UI size with.</summary>
    private const string UiSizeName = "PackageBank";

    private const int MaxSlots = 100;
    private const string CoreNamespace = "PackageBank.Core";
    private const string AdaptersNamespace = "PackageBank.Adapters";
    private const BindingFlags Declared = BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public override void Load()
    {
        ModUiSize.Bind(Config, UiSizeName);

        var limitBySize = Config.Bind("Bank", "LimitBySize", true, new ConfigDescription(
            "Limit how many packages of each size the bank holds, set by LargeSlots, MediumSlots, SmallSlots and DefenseSlots. Set to false for no limit. Story items never have a limit.\n" +
            "限制仓库中每种尺寸的包裹数量，由 LargeSlots、MediumSlots、SmallSlots 和 DefenseSlots 设置。设为 false 则不限数量。剧情物品始终不限数量。",
            null, WorldRule.Tag));
        var largeSlots = Config.Bind("Bank", "LargeSlots", 4, new ConfigDescription(
            "How many large packages the bank holds while LimitBySize is on. 4 by default, from 0 to 100.\n" +
            "LimitBySize 开启时仓库可存放的大型包裹数量。默认 4，可设为 0 到 100。",
            new AcceptableValueRange<int>(0, MaxSlots), WorldRule.Tag));
        var mediumSlots = Config.Bind("Bank", "MediumSlots", 6, new ConfigDescription(
            "How many medium packages the bank holds while LimitBySize is on. 6 by default, from 0 to 100.\n" +
            "LimitBySize 开启时仓库可存放的中型包裹数量。默认 6，可设为 0 到 100。",
            new AcceptableValueRange<int>(0, MaxSlots), WorldRule.Tag));
        var smallSlots = Config.Bind("Bank", "SmallSlots", 8, new ConfigDescription(
            "How many small packages the bank holds while LimitBySize is on. 8 by default, from 0 to 100.\n" +
            "LimitBySize 开启时仓库可存放的小型包裹数量。默认 8，可设为 0 到 100。",
            new AcceptableValueRange<int>(0, MaxSlots), WorldRule.Tag));
        var defenseSlots = Config.Bind("Bank", "DefenseSlots", 4, new ConfigDescription(
            "How many defense packages (sandbags, spike barriers and other defenses) the bank holds while LimitBySize is on. 4 by default, from 0 to 100.\n" +
            "LimitBySize 开启时仓库可存放的防御设施包裹（沙包、尖刺障碍等）数量。默认 4，可设为 0 到 100。",
            new AcceptableValueRange<int>(0, MaxSlots), WorldRule.Tag));
        var packUp = Config.Bind("Furniture", "PackUp", true, new ConfigDescription(
            "Add Pack up to the click menu on placed furniture, to turn a piece back into its package. Set to false to take the entry away.\n" +
            "在已摆放家具的点击菜单中加入「打包」，把家具变回包裹。设为 false 则不显示该选项。"));
        var fixtureSpots = Config.Bind("Furniture", "SpotsWhereFixturesStood", false, new ConfigDescription(
            "Some of the house's own furniture (cabinets, workbenches, the radio) stands on no build spot, so once one is packed up or removed, nothing can go where it stood. Set to true to open a build spot of its size there, so it or anything else that size can go back. Turning it off again opens no new ones; a spot with furniture on it stays.\n" +
            "房子自带的部分家具（柜子、工作台、收音机）不在建造位上，打包或拆除后，原位置就放不了任何东西。设为 true 则在原位置出现一个同尺寸的建造位，可以把它或其他同尺寸家具放回去。之后再关闭则不再出现新的建造位；已放了家具的建造位会保留。"));
        var uninstallPrep = Config.Bind("Uninstall", "UninstallPrep", false, new ConfigDescription(
            "Turn this on before you remove Package Bank, then load your save at home. Every piece standing on a spot where house furniture stood is packed into a yard package, since the game without the mod has no such spot. Then take everything out of the bank, save, quit, and remove PackageBank.dll. package-bank.log lists every piece, and any that could not be packed (one that still holds items, say).\n" +
            "移除 Package Bank 之前请先打开此项，然后在家中读取存档。所有放在房子原有家具位置建造位上的家具都会打包成屋外空地上的包裹，因为没有此模组时游戏里没有这些建造位。然后把仓库里的东西全部取出，存档、退出游戏，再删除 PackageBank.dll。package-bank.log 会列出每件家具，以及无法打包的家具（比如里面还有物品）。"));
        var log = new ModLog(Log, LogFileName);
        var hooks = new PatchKit(new Harmony(Guid), log.Write);
        var bank = new GameBank(new GameShop(log.Write), log.Write);
        var session = new BankSession(bank, new BankPage(log.Write), log.Write);

        var spots = new FixtureSpotsLive(bank.PackUpForUninstall, log.Write);

        LoadRows.Apply(hooks, spots.BeforeLoad);
        LayoutScreen.Apply(hooks);
        LiveSettings.Watch(() =>
        {
            spots.Use(fixtureSpots.Value, uninstallPrep.Value);
            log.Write((fixtureSpots.Value ? "spots where house furniture stood: on" : "spots where house furniture stood: off") + (uninstallPrep.Value ? ", uninstall prep on" : ""));
        }, fixtureSpots, uninstallPrep);
        PackageBankApi.Use(bank, log.Write);
        DevCheats.Use(bank);

        log.Write($"===== Package Bank {Version} started");
        session.MenuHooked(MenuOpening.Apply(hooks, session.MenuOpened));

        LiveSettings.Watch(() =>
        {
            var limits = new BankLimits(limitBySize.Value, largeSlots.Value, mediumSlots.Value, smallSlots.Value, defenseSlots.Value, packUp.Value);

            session.UseLimits(limits);
            log.Write((limits.IsLimited ? $"limits: {limits.Large} large, {limits.Medium} medium, {limits.Small} small, {limits.Defense} defense" : "limits off") + (limits.IsPackUpOn ? ", pack up on" : ", pack up off"));
        }, limitBySize, largeSlots, mediumSlots, smallSlots, defenseSlots, packUp);

        BankTicker.Session = session;
        BankTicker.Spots = spots;
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
