using System;

namespace PackageBank.Core;

/// <summary>
/// One Config_Furniture row as Pack up needs it. PackageModel is BagResName, the model the game shows for the piece's
/// package; HasMove and HasRemove are MoveFunc and RemoveFunc being set; SlotType is the kind of build spot it goes on.
/// </summary>
public sealed record FurnitureRow(string? PackageModel, bool HasMove, bool HasRemove, int SlotType);

/// <summary>
/// Some house furniture has no package in the game (the office clutter of the Warehouse Manager's home,
/// for one): its BagResName is empty, and the game's CreateBagFurniture makes nothing for it. The model is only the
/// wooden box the package shows as, the same three boxes for every piece, so such a piece gets the box of its size.
/// CreateBagFurniture and AgentModelManager.CreateBagFurnitureShow are the only game code that reads BagResName.
/// </summary>
public static class PackageModels
{
    public const string SmallBox = "P_WoodBox_01";
    public const string MediumBox = "P_WoodBox_02";
    public const string LargeBox = "P_WoodBox_03";

    /// <summary>The box to give a piece Pack up could take that has no package, or null when it has one or Pack up never takes it.</summary>
    public static string? BoxFor(FurnitureRow row)
    {
        if (!string.IsNullOrEmpty(row.PackageModel)) return null;
        if (!row.HasMove || !row.HasRemove || row.SlotType == PackRules.NoSpotSlotType) return null;

        return PackageSizes.Of(row.SlotType, isStory: false) switch
        {
            PackageSize.Small => SmallBox,
            PackageSize.Medium or PackageSize.Defense => MediumBox,
            PackageSize.Large => LargeBox,
            _ => null,
        };
    }

    /// <summary>
    /// What a packed piece refunds. With a shop price, the game works it out at the refund (PaidUnknown); with none (house
    /// furniture), it refunds what the pawn shop pays for it, since the game refunds a known price as is.
    /// </summary>
    public static int RefundPaid(int furniturePrice, int pawnPrice) => furniturePrice > 0 ? PackRun.PaidUnknown : Math.Max(pawnPrice, 0);
}
