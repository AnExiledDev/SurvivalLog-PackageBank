using System;
using System.Collections.Generic;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using GameCore.HotUpdate.ReduxUI;
using Il2CppInterop.Runtime;
using PackageBank.Core;

namespace PackageBank.Adapters;

/// <summary>
/// The open build shop's own package list. The shop builds that list when it opens and then only changes it on its
/// own Buy and Refund, so a package banked or taken out while it is open is mirrored here the way the shop's Buy
/// does it: the row, the package count, and the count of the furniture's slot. The shop's stock is never touched.
/// Nothing here runs while the shop is shut; it rebuilds the list from the game when it next opens.
/// </summary>
internal sealed class GameShop
{
    private static Il2CppSystem.Type? _shopPage;
    private static Il2CppSystem.Type? _shopState;

    private readonly Action<string> _log;
    private readonly HashSet<string> _reportedFailures = new();
    private long _failedPrint;

    public GameShop(Action<string> log)
    {
        _log = log;
    }

    private static Il2CppSystem.Type ShopPage => _shopPage ??= Il2CppType.Of<WebUI_BuildShop>();

    private static Il2CppSystem.Type ShopState => _shopState ??= Il2CppType.Of<State_Web_BuildShop>();

    public bool IsOpen()
    {
        try
        {
            return GameSingleton.Existing<ReduxUISystem>()?.IsUIActive(ShopPage) == true;
        }
        catch (Exception ex)
        {
            ReportOnce("shop check", ex);

            return false;
        }
    }

    /// <summary>
    /// Changes whenever the open shop's package list does: the shop's Buy, Refund and the bank's own rows all add or
    /// remove a row, and each bumps the list's version. 0 while the shop is shut.
    /// </summary>
    public long Print()
    {
        try
        {
            var shop = OpenShop(prepareValues: false);

            if (shop == null) return 0;

            var packages = shop.Value.State.Packages;
            var print = new ChangePrint();

            print.Add(shop.Value.State.Pointer.ToInt64());

            if (packages?.list is { } list)
            {
                print.Add(list.Pointer.ToInt64());
                print.Add(list._version);
                print.Add(list.Count);
            }

            return print.Value;
        }
        catch (Exception ex)
        {
            ReportOnce("shop print", ex);

            // A print that cannot be read never matches, so the rows are read the old way, every refresh.
            return --_failedPrint;
        }
    }

    /// <summary>The shop's rows while it is open, null otherwise.</summary>
    public IReadOnlyList<ShopRow>? Rows()
    {
        try
        {
            var state = OpenShop(prepareValues: true)?.State;

            if (state?.Packages == null) return null;

            var rows = new List<ShopRow>();

            for (var i = 0; i < state.Packages.Count; i++)
            {
                var row = state.Packages[i];

                if (row?.ConfigId != null) rows.Add(new ShopRow(i, row.ConfigId.Value, row.PaidGold));
            }

            return rows;
        }
        catch (Exception ex)
        {
            ReportOnce("shop read", ex);

            return null;
        }
    }

    /// <summary>
    /// Takes the banked package's row out: the row it was banked from when the press came from the shop, otherwise the
    /// first row of that furniture, bought for the same price when there is one. The shop's screen is refreshed by the
    /// Recount that always follows, so one press sends the game's refresh once.
    /// </summary>
    public void Banked(int? index, int configId, int paid, int? slotType)
    {
        try
        {
            var shop = OpenShop(prepareValues: true);
            var packages = shop?.State.Packages;

            if (shop == null || packages == null) return;

            var at = RowToRemove(packages, index, configId, paid);

            if (at < 0) return;

            packages.RemoveAt(at);

            for (var i = 0; i < packages.Count; i++) packages[i].Index = i;

            Shift(shop.Value.State, slotType, -1);
        }
        catch (Exception ex)
        {
            ReportOnce("shop bank", ex);
        }
    }

    /// <summary>Adds the row the shop's own Buy would have added for this package; the Recount that follows refreshes the screen.</summary>
    public void TakenOut(int configId, int paid, string name, Config_Furniture? row)
    {
        try
        {
            var shop = OpenShop(prepareValues: true);
            var packages = shop?.State.Packages;

            if (shop == null || packages == null) return;

            var package = new Data_Web_BuildShop_Package { Index = packages.Count, PaidGold = paid };

            package.ConfigId.Value = configId;
            package.Name.Value = name;
            package.Icon.Value = row?.ICON ?? "";
            package.SizeName.Value = row == null ? "" : GameKey.GetSlotTypeName(row.SlotType);
            packages.Add(package);

            Shift(shop.Value.State, row?.SlotType, +1);
        }
        catch (Exception ex)
        {
            ReportOnce("shop take out", ex);
        }
    }

    private static int RowToRemove(ObservableCollections.ObservableList<Data_Web_BuildShop_Package> packages, int? index, int configId, int paid)
    {
        if (index is { } wanted && wanted >= 0 && wanted < packages.Count && packages[wanted].ConfigId.Value == configId) return wanted;

        var sameFurniture = -1;

        for (var i = 0; i < packages.Count; i++)
        {
            if (packages[i].ConfigId.Value != configId) continue;
            if (packages[i].PaidGold == paid) return i;

            if (sameFurniture < 0) sameFurniture = i;
        }

        return sameFurniture;
    }

    /// <summary>
    /// The shop's package count and how many it may hold, worked out as the shop does when it opens. A looted package
    /// banked or taken out changes the free yard spots too, and so how many more packages can be bought. Then sends the
    /// shop's screen its one refresh for the press, the row change made just before included, even when the count fails.
    /// </summary>
    public void Recount(BuildComponent build)
    {
        (ReduxUISystem Redux, State_Web_BuildShop State)? shop;

        try
        {
            shop = OpenShop(prepareValues: true);
        }
        catch (Exception ex)
        {
            ReportOnce("shop recount", ex);

            return;
        }

        if (shop == null) return;

        try
        {
            var bought = build.GetPurchaseCount();

            shop.Value.State.PackageCapacity.Value = bought;
            shop.Value.State.PackageMaxCapacity.Value = Math.Min(build.GetMaxCapacity(), bought + build.GetFreePackagePointCount());
        }
        catch (Exception ex)
        {
            ReportOnce("shop recount", ex);
        }

        try
        {
            shop.Value.Redux.TriggerStateSubscribe(shop.Value.State, true);
        }
        catch (Exception ex)
        {
            ReportOnce("shop refresh", ex);
        }
    }

    /// <summary>The shop's count for the furniture's slot, never below zero, as the shop's own Refund keeps it.</summary>
    private static void Shift(State_Web_BuildShop state, int? slotType, int by)
    {
        if (slotType is not { } type || state.Slots == null) return;

        for (var i = 0; i < state.Slots.Count; i++)
        {
            var slot = state.Slots[i];

            if (slot == null || slot.SlotTypeId != type) continue;

            slot.Current.Value = Math.Max(0, slot.Current.Value + by);

            return;
        }
    }

    /// <summary>The open shop's state; its reactive values are made ready only for a rows read or a write, never for the print.</summary>
    private static (ReduxUISystem Redux, State_Web_BuildShop State)? OpenShop(bool prepareValues)
    {
        var redux = GameSingleton.Existing<ReduxUISystem>();

        if (redux == null || !redux.IsUIActive(ShopPage)) return null;

        var tree = redux.reduxStoreLayer?.stateTree;

        if (tree == null || !tree.TryGetValue(ShopState, out var raw)) return null;

        var state = raw?.TryCast<State_Web_BuildShop>();

        if (state == null) return null;

        if (prepareValues) state.EnsureReactivePropertiesInitialized();

        return (redux, state);
    }

    private void ReportOnce(string operation, Exception ex)
    {
        if (_reportedFailures.Add(operation)) _log($"{operation} failed (each kind reported once): {ex}");
    }
}
