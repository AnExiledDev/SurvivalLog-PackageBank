using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

    private static readonly Comparison<HomeSpot> ById = (a, b) => a.Id.CompareTo(b.Id);

    private readonly Action<string> _log;
    private readonly GameShop _shop;
    private readonly HashSet<string> _reportedFailures = new();
    private readonly KeyedMemo<(IntPtr Config, bool IsChinese, bool HasIcons), FurnitureInfo> _furniture;
    private readonly Task<(IconIndex Icons, string Note)>? _iconLoad;
    private readonly List<HomeSpot> _spots = new();

    private IconIndex? _icons;
    private IReadOnlySet<int>? _storyFurniture;
    private IntPtr _storyConfig;
    private ConfigManager? _describing;

    private (IntPtr Agents, IntPtr Map, int Version, int Count) _spotsStamp;
    private bool _hasSpots;
    private (IntPtr Dict, int Version, int Count) _contentsStamp;
    private BankContents? _contents;
    private long _contentsGeneration;
    private long _failedPrint;

    private int _loggedHomeCount = -1;
    private string? _loggedNotReady;
    private int _loggedNotReadyMode;

    public GameBank(GameShop shop, Action<string> log)
    {
        _shop = shop;
        _log = log;
        _furniture = new KeyedMemo<(IntPtr, bool, bool), FurnitureInfo>(Describe);

        try
        {
            _iconLoad = WebIcons.LoadInBackground();
        }
        catch (Exception ex)
        {
            _log("could not start reading the web icons, packages will show letters instead: " + ex.Message);
            _icons = IconIndex.Empty;
        }
    }

    public BankState State()
    {
        var isChinese = GameLanguage.IsChinese();

        try
        {
            return ReadState(isChinese);
        }
        catch (Exception ex)
        {
            ReportOnce("read", ex);

            return new BankState(BankPlace.NoGame, IsShopOpen: false, Menu: null, isChinese);
        }
    }

    public long Print()
    {
        try
        {
            return ReadPrint();
        }
        catch (Exception ex)
        {
            ReportOnce("print", ex);

            // A print that cannot be read never matches, so the bank is read the old way, every refresh.
            return --_failedPrint;
        }
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
    /// The entry is written before the package leaves the yard. If the removal throws, the entry is taken back out only
    /// while the package is still in the yard, so a failure can neither lose the package nor leave it in both places. A
    /// bought package also leaves the build shop's list when that shop is open.
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
            ReportOnce("store", ex);

            if (build.PackagePointMap.ContainsKey(package.Id))
            {
                ClearEntry(counters, entry.Slot);

                return false;
            }

            _log($"{package.Name} left the yard before the error, so it stays banked");
        }

        _log($"banked {package.Name} (furniture {package.ConfigId}, package {package.Id}) into slot {entry.Slot}");

        try
        {
            GameMenu.CloseIfOn(package.Id);
        }
        catch (Exception ex)
        {
            ReportOnce("close the click menu", ex);
        }

        if (package.IsPurchased) _shop.Banked(shopIndex, package.ConfigId, package.Paid, SlotTypeOf(package.ConfigId));

        _shop.Recount(build);

        return true;
    }

    /// <summary>
    /// Puts the package on the free yard spot nearest the player, as the build shop's Buy does, and clears its entry the
    /// moment it is in the yard, so a later error cannot let a second press make a copy. Then gives back its shop
    /// record (so the shop's Refund still pays the same) and its color.
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

            ClearEntry(counters, entry.Slot);
            _log($"took slot {entry.Slot} (furniture {entry.ConfigId}) out as package {id}");

            if (entry.IsPurchased)
            {
                try
                {
                    build.AddPurchased(id, entry.Source, entry.Paid);
                }
                catch (Exception ex)
                {
                    ReportOnce("give back the shop record", ex);
                }
            }

            try
            {
                Dye(agents, id, entry.Dye);
            }
            catch (Exception ex)
            {
                ReportOnce("give back the color", ex);
            }

            if (entry.IsPurchased) _shop.TakenOut(entry.ConfigId, entry.Paid, NameOf(config, entry.ConfigId), Row(config, entry.ConfigId));

            _shop.Recount(build);

            return id;
        }
        catch (Exception ex)
        {
            ReportOnce("take out", ex);

            return 0;
        }
    }

    private void Dye(AgentManager agents, long id, int dye)
    {
        var bag = agents.GetAgent(id)?.TryCast<BagFurniture>();

        if (bag == null)
        {
            _log($"package {id} was not found to give back its color");

            return;
        }

        bag.DyeSlot = dye;
    }

    /// <summary>The bank's entries, empty with no save loaded. Cheap: the counters are read again only when they changed.</summary>
    public IReadOnlyList<BankEntry> Entries()
    {
        var counters = Counters();

        return counters == null ? Array.Empty<BankEntry>() : Contents(counters).Entries;
    }

    /// <summary>How many yard package spots are empty, 0 with no game loaded.</summary>
    public int FreeSpots() => Build()?.GetFreePackagePointCount() ?? 0;

    /// <summary>Where the player is and which screens are open, with no read of the yard or the bank.</summary>
    private BankState ReadState(bool isChinese)
    {
        var game = LoadedGame();

        if (game == null) return NotReady(BankPlace.NoGame, "no game loaded", isChinese);

        var (world, _, _, role) = game.Value;
        var mode = world.GameModeKey;

        if (mode == GameMode.Explore) return NotReady(BankPlace.Away, "on an expedition", isChinese);
        if (mode != GameMode.Default) return NotReady(BankPlace.NoGame, "game mode", isChinese, (int)mode);
        if (AgentTools.GetAgentComponent<BuildComponent>(role) == null) return NotReady(BankPlace.NoGame, "the character has no build component", isChinese);
        if (Counters() == null) return NotReady(BankPlace.NoGame, "the save's counters are missing", isChinese);

        _loggedNotReady = null;

        return new BankState(BankPlace.Home, _shop.IsOpen(), MenuOpenOn(), isChinese);
    }

    /// <summary>
    /// Everything Read draws from, as stamps rather than values: the loaded game's objects (a new save makes new ones),
    /// the bank's counters (re-read only when the counters changed, and counted as changed only when the bank's own
    /// entries did), the yard's spot map and purchase maps, each yard package's shown flag and color, the free spot
    /// count, the shop's list,
    /// the click menu and the icons arriving. A changed dictionary always has a new version: the game bumps it on every
    /// add, overwrite and removal.
    /// </summary>
    private long ReadPrint()
    {
        var game = LoadedGame();

        if (game == null) return 0;

        var (world, config, agents, role) = game.Value;
        var build = AgentTools.GetAgentComponent<BuildComponent>(role);
        var counters = Counters();

        if (build == null || counters == null) return 0;

        var print = new ChangePrint();
        var points = build._PackagePointMap_k__BackingField;

        print.Add(world.Pointer.ToInt64());
        print.Add(config.Pointer.ToInt64());
        print.Add(agents.Pointer.ToInt64());
        Contents(counters);
        print.Add(_contentsGeneration);
        AddStamp(ref print, points);
        AddStamp(ref print, build._PurchaseSourceMap_k__BackingField);
        AddStamp(ref print, build._PurchasePaidMap_k__BackingField);

        if (points != null)
        {
            foreach (var spot in Spots(agents, points))
            {
                print.Add(spot.Bag._IsShow_k__BackingField);
                print.Add(spot.Bag._DyeSlot_k__BackingField);
            }
        }

        // The free spot count goes in as itself: the home can gain spots with no package moving.
        print.Add(build.GetFreePackagePointCount());
        print.Add(_shop.Print());
        print.Add(MenuOpenOn() ?? 0);
        print.Add(Icons() != null);

        return print.Value;
    }

    private static void AddStamp<TKey, TValue>(ref ChangePrint print, Il2CppSystem.Collections.Generic.Dictionary<TKey, TValue>? map)
    {
        if (map == null)
        {
            print.Add(0L);

            return;
        }

        print.Add(map.Pointer.ToInt64());
        print.Add(map._version);
        print.Add(map.Count);
    }

    private BankRead ReadBank()
    {
        var game = LoadedGame();

        if (game == null) return new BankRead.NoGame();

        var (world, config, agents, role) = game.Value;
        var mode = world.GameModeKey;

        if (mode == GameMode.Explore) return new BankRead.Away();
        if (mode != GameMode.Default) return new BankRead.NoGame();

        var build = AgentTools.GetAgentComponent<BuildComponent>(role);
        var counters = Counters();

        if (build == null || counters == null) return new BankRead.NoGame();

        var contents = Contents(counters);
        var scope = (config.Pointer, GameLanguage.IsChinese(), Icons() != null);
        var home = ReadHome(config, agents, build, scope);
        var banked = new List<BankedPackage>(contents.Entries.Count);

        foreach (var entry in contents.Entries)
        {
            var info = Info(config, scope, entry.ConfigId);

            banked.Add(new BankedPackage(entry, info.Name, info.Icon, info.Size));
        }

        return new BankRead.Ready(new Bank(home, banked, contents.Bonus, build.GetFreePackagePointCount(), _shop.Rows(), MenuOpenOn()));
    }

    /// <summary>Only the yard's numbered spots: the car's spot and the boxes by the doors are left alone.</summary>
    private List<HomePackage> ReadHome(ConfigManager config, AgentManager agents, BuildComponent build, (IntPtr, bool, bool) scope)
    {
        var home = new List<HomePackage>();
        var points = build._PackagePointMap_k__BackingField;

        if (points == null) return home;

        var sources = build.PurchaseSourceMap;

        foreach (var spot in Spots(agents, points))
        {
            var id = spot.Id;
            var bag = spot.Bag;
            var info = Info(config, scope, spot.ConfigId);
            var isPurchased = sources != null && sources.ContainsKey(id);

            // IsSelected and IsAltSelected are only the hover highlight (name shown, outline), which every package the
            // click menu is open on has; a package being carried or placed is hidden instead.
            home.Add(new HomePackage(
                id,
                spot.ConfigId,
                info.Name,
                info.Icon,
                info.Size,
                !bag._IsShow_k__BackingField,
                isPurchased,
                isPurchased ? build.GetPurchasePaid(id) : BuildComponent.PAID_UNKNOWN,
                isPurchased ? sources![id] : 0,
                bag._DyeSlot_k__BackingField));
        }

        return home;
    }

    /// <summary>
    /// The yard's packages, in id order, looked up again only when the spot map changes (a package placed, moved off,
    /// banked or taken out, or a new save). A spot whose package the game has not made yet is looked for again next time.
    /// </summary>
    private List<HomeSpot> Spots(AgentManager agents, Il2CppSystem.Collections.Generic.Dictionary<long, string> points)
    {
        var stamp = (agents.Pointer, points.Pointer, points._version, points.Count);

        if (_hasSpots && stamp == _spotsStamp) return _spots;

        var isComplete = true;

        _spots.Clear();

        foreach (var spot in points)
        {
            if (!PackageSizes.IsHomePackageSpot(spot.Value)) continue;

            var bag = agents.GetAgent(spot.Key)?.TryCast<BagFurniture>();

            if (bag == null)
            {
                isComplete = false;

                continue;
            }

            if (bag._IsDoorBox_k__BackingField) continue;

            _spots.Add(new HomeSpot(spot.Key, bag, bag._FurnitureConfigId_k__BackingField));
        }

        _spots.Sort(ById);
        _spotsStamp = stamp;
        _hasSpots = isComplete;
        LogHomeCount(_spots.Count);

        return _spots;
    }

    /// <summary>
    /// The bank's entries, read again only when the save's counters changed. The counters hold much more than the bank,
    /// so a re-read that finds the same entries keeps the generation, and the view is not rebuilt for it.
    /// </summary>
    private BankContents Contents(Il2CppSystem.Collections.Generic.Dictionary<string, int> counters)
    {
        var stamp = (counters.Pointer, counters._version, counters.Count);

        if (_contents != null && stamp == _contentsStamp) return _contents;

        var contents = BankCounters.Read(Copy(counters));

        if (_contents == null || !contents.SameAs(_contents)) _contentsGeneration++;

        _contents = contents;
        _contentsStamp = stamp;

        return contents;
    }

    /// <summary>A furniture's name, icon and shelf, looked up once per loaded tables, language and icon index.</summary>
    private FurnitureInfo Info(ConfigManager config, (IntPtr, bool, bool) scope, int configId)
    {
        _describing = config;

        return _furniture.Get(scope, configId);
    }

    private FurnitureInfo Describe(int configId)
    {
        var config = _describing!;
        var row = Row(config, configId);
        var size = row == null ? null : PackageSizes.Of(row.SlotType, StoryFurniture(config).Contains(configId));

        return new FurnitureInfo(NameOf(config, configId), Icons()?.Resolve(row?.ICON), size);
    }

    /// <summary>Read once per loaded tables: the item table does not change while the game runs.</summary>
    private IReadOnlySet<int> StoryFurniture(ConfigManager config)
    {
        if (_storyFurniture != null && config.Pointer == _storyConfig) return _storyFurniture;

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
        _storyConfig = config.Pointer;
        _log($"{_storyFurniture.Count} story furniture found");

        return _storyFurniture;
    }

    /// <summary>
    /// The furniture table read directly: the game's getter logs an error for every id it lacks, and a log write is slow.
    /// </summary>
    private static Config_Furniture? Row(ConfigManager config, int configId)
    {
        var table = config._Config_Furniture_Dict;

        return table != null && table.TryGetValue(configId, out var row) ? row : null;
    }

    private static string NameOf(ConfigManager config, int configId)
    {
        var row = Row(config, configId);

        return Translate(config, row?.Name) ?? NonEmpty(row?.Name_Local) ?? NonEmpty(row?.Name) ?? $"Package {configId}";
    }

    private static int? SlotTypeOf(int configId)
    {
        var config = GameSingleton.Existing<ConfigManager>();

        return config == null ? null : Row(config, configId)?.SlotType;
    }

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

    /// <summary>
    /// Logs why the bank is shut, once each time the reason changes, so a hidden HUD button can be explained. The line is
    /// built only then, not every refresh.
    /// </summary>
    private BankState NotReady(BankPlace place, string reason, bool isChinese, int mode = 0)
    {
        if (!ReferenceEquals(reason, _loggedNotReady) || mode != _loggedNotReadyMode)
        {
            _log(mode == 0 ? "bank shut: " + reason : $"bank shut: {reason} {(GameMode)mode}");
            _loggedNotReady = reason;
            _loggedNotReadyMode = mode;
        }

        return new BankState(place, IsShopOpen: false, Menu: null, isChinese);
    }

    private static UnityEngine.Vector3 PositionOf(BaseAgent agent) => AgentTools.GetAgentComponent<TransformComponent>(agent)?.Position ?? UnityEngine.Vector3.zero;

    /// <summary>The web icons once the worker has walked the folder; null until then, and the panel shows letters.</summary>
    private IconIndex? Icons()
    {
        if (_icons != null) return _icons;
        if (_iconLoad == null || !_iconLoad.IsCompleted) return null;

        if (_iconLoad.IsCompletedSuccessfully)
        {
            _log(_iconLoad.Result.Note);
            _icons = _iconLoad.Result.Icons;
        }
        else
        {
            _log("reading the web icons failed, packages will show letters instead: " + _iconLoad.Exception?.GetBaseException().Message);
            _icons = IconIndex.Empty;
        }

        return _icons;
    }

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
        try
        {
            return GameMenu.OpenOn();
        }
        catch (Exception ex)
        {
            ReportOnce("read the click menu", ex);

            return null;
        }
    }

    private void ReportOnce(string operation, Exception ex)
    {
        if (_reportedFailures.Add(operation)) _log($"{operation} failed (each kind reported once): {ex}");
    }

    private readonly record struct HomeSpot(long Id, BagFurniture Bag, int ConfigId);

    private sealed record FurnitureInfo(string Name, string? Icon, PackageSize? Size);
}
