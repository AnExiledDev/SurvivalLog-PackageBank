using System;
using System.Collections.Generic;
using ExiledToolkit.Kit;
using UnityEngine;

namespace PackageBank.Adapters;

/// <summary>
/// The Unity object that hands frames to the session. Each distinct failure is logged once, never rethrown into the
/// game; a failure that repeats every frame would otherwise flood the log.
/// </summary>
public sealed class BankTicker : MonoBehaviour
{
    private const int MaxReportedFailures = 20;

    internal static BankSession? Session;
    internal static FixtureSpotsLive? Spots;
    internal static Action<string> Log = _ => { };
    private static readonly HashSet<string> ReportedFailures = new();
    private static readonly double LoadedAt = Clock.Now;

    /// <summary>
    /// Real seconds since the mod loaded, from the Toolkit clock. Counted from load so a float keeps its precision
    /// however long the computer has been on.
    /// </summary>
    internal static float Now => (float)(Clock.Now - LoadedAt);

    public BankTicker(IntPtr pointer) : base(pointer)
    {
    }

    private void Update()
    {
        try
        {
            Session?.Update();
        }
        catch (Exception ex)
        {
            ReportFailure(ex);
        }

        try
        {
            Spots?.Tick();
        }
        catch (Exception ex)
        {
            ReportFailure(ex);
        }
    }

    private static void ReportFailure(Exception ex)
    {
        if (ReportedFailures.Count >= MaxReportedFailures || !ReportedFailures.Add(ex.GetType().FullName + ": " + ex.Message)) return;

        Log("update failed (each distinct failure reported once): " + ex);

        if (ReportedFailures.Count == MaxReportedFailures) Log($"{MaxReportedFailures} distinct update failures reported; further ones are not logged");
    }
}
