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
    private readonly Action<string> _log;
    private readonly HashSet<string> _reportedFailures = new();

    public GameShop(Action<string> log)
    {
        _log = log;
    }

    /// <summary>The shop's rows while it is open, null otherwise.</summary>
    public IReadOnlyList<ShopRow>? Rows()
    {
        try
        {
            var state = OpenShop()?.State;

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
    /// first row of that furniture, bought for the same price when there is one.
    /// </summary>
    public void Banked(int? index, int configId, int paid, int? slotType)
    {
        try
        {
            var shop = OpenShop();
            var packages = shop?.State.Packages;

            if (shop == null || packages == null) return;

            var at = RowToRemove(packages, index, configId, paid);

            if (at < 0) return;

            packages.RemoveAt(at);

            for (var i = 0; i < packages.Count; i++) packages[i].Index = i;

            Shift(shop.Value.State, slotType, -1);
            shop.Value.Redux.TriggerStateSubscribe(shop.Value.State, true);
        }
        catch (Exception ex)
        {
            ReportOnce("shop bank", ex);
        }
    }

    /// <summary>Adds the row the shop's own Buy would have added for this package.</summary>
    public void TakenOut(int configId, int paid, string name, Config_Furniture? row)
    {
        try
        {
            var shop = OpenShop();
            var packages = shop?.State.Packages;

            if (shop == null || packages == null) return;

            var package = new Data_Web_BuildShop_Package { Index = packages.Count, PaidGold = paid };

            package.ConfigId.Value = configId;
            package.Name.Value = name;
            package.Icon.Value = row?.ICON ?? "";
            package.SizeName.Value = row == null ? "" : GameKey.GetSlotTypeName(row.SlotType);
            packages.Add(package);

            Shift(shop.Value.State, row?.SlotType, +1);
            shop.Value.Redux.TriggerStateSubscribe(shop.Value.State, true);
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
    /// banked or taken out changes the free yard spots too, and so how many more packages can be bought.
    /// </summary>
    public void Recount(BuildComponent build)
    {
        try
        {
            var shop = OpenShop();

            if (shop == null) return;

            var bought = build.GetPurchaseCount();

            shop.Value.State.PackageCapacity.Value = bought;
            shop.Value.State.PackageMaxCapacity.Value = Math.Min(build.GetMaxCapacity(), bought + build.GetFreePackagePointCount());
            shop.Value.Redux.TriggerStateSubscribe(shop.Value.State, true);
        }
        catch (Exception ex)
        {
            ReportOnce("shop recount", ex);
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

    private static (ReduxUISystem Redux, State_Web_BuildShop State)? OpenShop()
    {
        var redux = GameSingleton.Existing<ReduxUISystem>();

        if (redux == null || !redux.IsUIActive(Il2CppType.Of<WebUI_BuildShop>())) return null;

        var tree = redux.reduxStoreLayer?.stateTree;

        if (tree == null || !tree.TryGetValue(Il2CppType.Of<State_Web_BuildShop>(), out var raw)) return null;

        var state = raw?.TryCast<State_Web_BuildShop>();

        if (state == null) return null;

        state.EnsureReactivePropertiesInitialized();

        return (redux, state);
    }

    private void ReportOnce(string operation, Exception ex)
    {
        if (_reportedFailures.Add(operation)) _log($"{operation} failed (each kind reported once): {ex}");
    }
}
