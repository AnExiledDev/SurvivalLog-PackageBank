using System;
using System.Collections.Generic;
using System.Linq;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using PackageBank.Core;

namespace PackageBank.Adapters;

/// <summary>
/// The game's side of the bank. Yard packages are read off the home's build component (which yard spot holds which
/// package, and what the shop was paid for it); the bank itself is kept in the save's own counters, so it saves and
/// loads with the game. Taking a package out goes through the same calls the build shop's Buy makes. Each call fails
/// soft: an exception is logged once per kind of call and answered as "nothing happened".
/// </summary>
internal sealed class GameBank : IBankGame
{
    private const string TakeOutReason = "packagebank";

    private readonly Action<string> _log;
    private readonly GameShop _shop;
    private readonly HashSet<string> _reportedFailures = new();
    private IReadOnlySet<int>? _storyFurniture;
    private IconIndex? _icons;
    private int _loggedHomeCount = -1;
    private string? _loggedNotReady;

    public GameBank(GameShop shop, Action<string> log)
    {
        _shop = shop;
        _log = log;
    }

    public BankRead Read()
    {
        try
        {
            return ReadBank();
        }
        catch (Exception ex)
        {
            ReportOnce("read", ex);

            return new BankRead.NoGame();
        }
    }

    /// <summary>
    /// The entry is written before the package leaves the yard, and taken back out if the removal throws, so a failure
    /// cannot lose the package. A bought package also leaves the build shop's list when that shop is open.
    /// </summary>
    public bool Store(HomePackage package, BankEntry entry, int? shopIndex)
    {
        var counters = Counters();
        var build = Build();

        if (counters == null || build == null) return false;

        WriteEntry(counters, entry);

        try
        {
            build.DestroyBagFurniture(package.Id);

            if (package.IsPurchased) build.RemovePurchased(package.Id);
        }
        catch (Exception ex)
        {
            ClearEntry(counters, entry.Slot);
            ReportOnce("store", ex);

            return false;
        }

        _log($"banked {package.Name} (furniture {package.ConfigId}, package {package.Id}) into slot {entry.Slot}");
        Softly("close the click menu", () => GameMenu.CloseIfOn(package.Id));

        if (package.IsPurchased) _shop.Banked(shopIndex, package.ConfigId, package.Paid, SlotTypeOf(package.ConfigId));

        return true;
    }

    /// <summary>
    /// Puts the package on the free yard spot nearest the player, as the build shop's Buy does, gives back its color
    /// and its shop record (so the shop's Refund still pays the same), then clears its entry.
    /// </summary>
    public long TakeOut(BankEntry entry)
    {
        try
        {
            var game = LoadedGame();
            var counters = Counters();

            if (game == null || counters == null) return 0;

            var (_, config, agents, role) = game.Value;
            var build = AgentTools.GetAgentComponent<BuildComponent>(role);

            if (build == null) return 0;

            build.ReconcilePackagePoints(TakeOutReason);

            if (build.GetFreePackagePointCount() <= 0) return 0;

            var id = build.CreateBagFurniture(entry.ConfigId, PositionOf(role), false, 0);

            if (id == 0) return 0;

            if (entry.IsPurchased) build.AddPurchased(id, entry.Source, entry.Paid);

            var bag = agents.GetAgent(id)?.TryCast<BagFurniture>();

            if (bag != null) bag.DyeSlot = entry.Dye;

            ClearEntry(counters, entry.Slot);
            _log($"took slot {entry.Slot} (furniture {entry.ConfigId}) out as package {id}");

            if (entry.IsPurchased) _shop.TakenOut(entry.ConfigId, entry.Paid, NameOf(config, entry.ConfigId), config.Get_Config_Furniture(entry.ConfigId));

            return id;
        }
        catch (Exception ex)
        {
            ReportOnce("take out", ex);

            return 0;
        }
    }

    private BankRead ReadBank()
    {
        var game = LoadedGame();

        if (game == null) return NotReady(new BankRead.NoGame(), "no game loaded");

        var (world, config, agents, role) = game.Value;
        var mode = world.GameModeKey;

        if (mode == GameMode.Explore) return NotReady(new BankRead.Away(), "on an expedition");
        if (mode != GameMode.Default) return NotReady(new BankRead.NoGame(), $"game mode {mode}");

        var build = AgentTools.GetAgentComponent<BuildComponent>(role);
        var counters = Counters();

        if (build == null) return NotReady(new BankRead.NoGame(), "the character has no build component");
        if (counters == null) return NotReady(new BankRead.NoGame(), "the save's counters are missing");

        _loggedNotReady = null;

        var contents = BankCounters.Read(Copy(counters));
        var home = ReadHome(config, agents, build);
        var banked = contents.Entries.Select(entry => Banked(config, entry)).ToList();

        LogHomeCount(home.Count);

        return new BankRead.Ready(new Bank(home, banked, contents.Bonus, build.GetFreePackagePointCount(), _shop.Rows(), MenuOpenOn()));
    }

    /// <summary>Only the yard's numbered spots: the car's spot and the boxes by the doors are left alone.</summary>
    private List<HomePackage> ReadHome(ConfigManager config, AgentManager agents, BuildComponent build)
    {
        var home = new List<HomePackage>();
        var sources = build.PurchaseSourceMap;

        foreach (var spot in build.PackagePointMap)
        {
            if (!PackageSizes.IsHomePackageSpot(spot.Value)) continue;

            var bag = agents.GetAgent(spot.Key)?.TryCast<BagFurniture>();

            if (bag == null || bag.IsDoorBox) continue;

            var id = spot.Key;
            var configId = bag.FurnitureConfigId;
            var isPurchased = sources != null && sources.ContainsKey(id);
            var row = config.Get_Config_Furniture(configId);
            var size = row == null ? null : PackageSizes.Of(row.SlotType, StoryFurniture(config).Contains(configId));
            // IsSelected and IsAltSelected are only the hover highlight (name shown, outline), which every package the
            // click menu is open on has; a package being carried or placed is hidden instead.
            var isBusy = !bag.IsShow;

            home.Add(new HomePackage(
                id,
                configId,
                NameOf(config, configId),
                Icons().Resolve(row?.ICON),
                size,
                isBusy,
                isPurchased,
                isPurchased ? build.GetPurchasePaid(id) : BuildComponent.PAID_UNKNOWN,
                isPurchased ? sources![id] : 0,
                bag.DyeSlot));
        }

        return home.OrderBy(package => package.Id).ToList();
    }

    private BankedPackage Banked(ConfigManager config, BankEntry entry)
    {
        var row = config.Get_Config_Furniture(entry.ConfigId);
        var size = row == null ? null : PackageSizes.Of(row.SlotType, StoryFurniture(config).Contains(entry.ConfigId));

        return new BankedPackage(entry, NameOf(config, entry.ConfigId), Icons().Resolve(row?.ICON), size);
    }

    /// <summary>Read once: the item table does not change while the game runs.</summary>
    private IReadOnlySet<int> StoryFurniture(ConfigManager config)
    {
        if (_storyFurniture != null) return _storyFurniture;

        var items = new List<(int, int)>();
        var all = config.Get_Config_Item_All();

        if (all != null)
        {
            foreach (var item in all)
            {
                if (item.Value != null) items.Add((item.Value.TargetFurnitureID, item.Value.price));
            }
        }

        _storyFurniture = StoryPackages.From(items);
        _log($"{_storyFurniture.Count} story furniture found");

        return _storyFurniture;
    }

    private static string NameOf(ConfigManager config, int configId)
    {
        var row = config.Get_Config_Furniture(configId);

        return Translate(config, row?.Name) ?? NonEmpty(row?.Name_Local) ?? NonEmpty(row?.Name) ?? $"Package {configId}";
    }

    private static int? SlotTypeOf(int configId) => GameSingleton.Existing<ConfigManager>()?.Get_Config_Furniture(configId)?.SlotType;

    private static void WriteEntry(Il2CppSystem.Collections.Generic.Dictionary<string, int> counters, BankEntry entry)
    {
        foreach (var (key, value) in BankCounters.Write(entry)) counters[key] = value;
    }

    private static void ClearEntry(Il2CppSystem.Collections.Generic.Dictionary<string, int> counters, int slot)
    {
        foreach (var key in BankCounters.Keys(slot)) counters.Remove(key);
    }

    private static List<KeyValuePair<string, int>> Copy(Il2CppSystem.Collections.Generic.Dictionary<string, int> counters)
    {
        var copy = new List<KeyValuePair<string, int>>();

        foreach (var counter in counters)
        {
            if (counter.Key != null && counter.Key.StartsWith(BankCounters.Prefix, StringComparison.Ordinal)) copy.Add(new KeyValuePair<string, int>(counter.Key, counter.Value));
        }

        return copy;
    }

    /// <summary>The save's counters: null on the title screen and while a save loads.</summary>
    private static Il2CppSystem.Collections.Generic.Dictionary<string, int>? Counters() => GameCounter.Cur?.GameCounterDict;

    private static BuildComponent? Build()
    {
        var role = LoadedGame()?.Role;

        return role == null ? null : AgentTools.GetAgentComponent<BuildComponent>(role);
    }

    /// <summary>
    /// Null on the title screen and while a save loads. The countdown before the disaster counts: that is when each loop
    /// opens with shopping, so the bank must work there too.
    /// </summary>
    private static (BattleLogicWorld World, ConfigManager Config, AgentManager Agents, LeadingRole Role)? LoadedGame()
    {
        var world = GameSingleton.Existing<BattleLogicWorld>();
        var config = GameSingleton.Existing<ConfigManager>();
        var agents = world?._AgentManager;
        var role = agents?.GetLeadingRole();

        if (world == null || config == null || agents == null || role == null) return null;

        return (world, config, agents, role);
    }

    /// <summary>Logs why the bank is shut, once each time the reason changes, so a hidden HUD button can be explained.</summary>
    private BankRead NotReady(BankRead read, string reason)
    {
        if (reason != _loggedNotReady) _log($"bank shut: {reason}");

        _loggedNotReady = reason;

        return read;
    }

    private static UnityEngine.Vector3 PositionOf(BaseAgent agent) => AgentTools.GetAgentComponent<TransformComponent>(agent)?.Position ?? UnityEngine.Vector3.zero;

    private IconIndex Icons() => _icons ??= WebIcons.Load(_log);

    private void LogHomeCount(int count)
    {
        if (count == _loggedHomeCount) return;

        _loggedHomeCount = count;
        _log($"{count} packages in the yard");
    }

    private static string? Translate(ConfigManager config, string? key)
    {
        if (string.IsNullOrEmpty(key)) return null;

        var text = config.GetLocalTxt(key);

        return string.IsNullOrEmpty(text) || text == key ? null : text;
    }

    private static string? NonEmpty(string? text) => string.IsNullOrWhiteSpace(text) ? null : text;

    /// <summary>A broken click menu costs only the menu's Bank entry, never the rest of the bank.</summary>
    private long? MenuOpenOn()
    {
        long? id = null;

        Softly("read the click menu", () => id = GameMenu.OpenOn());

        return id;
    }

    private void Softly(string operation, Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            ReportOnce(operation, ex);
        }
    }

    private void ReportOnce(string operation, Exception ex)
    {
        if (_reportedFailures.Add(operation)) _log($"{operation} failed (each kind reported once): {ex}");
    }
}
