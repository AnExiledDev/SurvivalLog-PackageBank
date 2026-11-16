using System;
using System.Collections.Generic;
using GameCore.HotUpdate;
using PackageBank.Core;

namespace PackageBank.Adapters;

/// <summary>
/// Gives every piece Pack up can take that has no package model in the game the wooden box of its size
/// (<see cref="PackageModels.BoxFor"/>), so the game makes and shows a package for it. Runs before every save loads, so a
/// saved package of one shows, and before each Pack up and Take out; the table is walked again only when the game made
/// a new one. Boxed holds the config ids it gave a box, for Uninstall prep.
/// </summary>
internal sealed class PackageBoxes
{
    private readonly Action<string> _log;
    private readonly HashSet<int> _boxed = new();
    private IntPtr _table;
    private int _tableVersion = -1;

    public PackageBoxes(Action<string> log) => _log = log;

    /// <summary>The config ids given a box, as of the last Ensure. Read on the game's thread only.</summary>
    public IReadOnlySet<int> Boxed => _boxed;

    public void Ensure(ConfigManager config)
    {
        var table = config._Config_Furniture_Dict;

        if (table == null || (table.Pointer == _table && table._version == _tableVersion)) return;

        // A row boxed on an earlier walk of the same table has its box now, so the walk would not count it again.
        if (table.Pointer != _table) _boxed.Clear();

        var added = 0;

        foreach (var pair in table)
        {
            var row = pair.Value;

            if (row == null) continue;

            var box = PackageModels.BoxFor(new FurnitureRow(row.BagResName, row.MoveFunc is { Count: > 0 }, row.RemoveFunc is { Count: > 0 }, row.SlotType));

            if (box == null) continue;

            row.BagResName = box;
            _boxed.Add(pair.Key);
            added++;
        }

        _table = table.Pointer;
        _tableVersion = table._version;

        if (added > 0) _log($"gave {added} pieces with no package in the game a wooden box, so Pack up works on them");
    }
}
