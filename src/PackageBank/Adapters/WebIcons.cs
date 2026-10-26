using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PackageBank.Core;
using UnityEngine;

namespace PackageBank.Adapters;

/// <summary>
/// Indexes the icon files the game ships for its web UI, so the panel can show the same pictures. The folder holds
/// about 1800 files, so it is walked on a worker started at load; the panel shows letters until it is done. The worker
/// never logs: its line is handed back and written on the main thread.
/// </summary>
internal static class WebIcons
{
    /// <summary>Asks Unity for the folder here, on the main thread, and walks it on a worker.</summary>
    public static Task<(IconIndex Icons, string Note)> LoadInBackground()
    {
        var webUi = Path.Combine(Application.streamingAssetsPath, "WebUI");

        return Task.Run(() => Load(webUi));
    }

    private static (IconIndex, string) Load(string webUi)
    {
        var res = Path.Combine(webUi, "Res");

        try
        {
            var files = new List<string>();

            foreach (var file in Directory.EnumerateFiles(res, "*.png", SearchOption.AllDirectories)) files.Add(Path.GetRelativePath(webUi, file));

            return (IconIndex.Build(files), $"{files.Count} web icons found under {res}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return (IconIndex.Empty, $"no web icons, packages will show letters instead: {ex.Message}");
        }
    }
}
