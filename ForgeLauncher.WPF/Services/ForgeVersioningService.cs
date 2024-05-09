﻿using ForgeLauncher.WPF.Attributes;
using ForgeLauncher.WPF.Extensions;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ForgeLauncher.WPF.Services;

// TODO:
// snapshot and release
// better version compare
[Export(typeof(IForgeVersioningService)), Shared]
public class ForgeVersioningService : IForgeVersioningService
{
    private const string LastVersionFile = "Forge.Launcher.WPF.LastVersion.txt";

    private ISettingsService SettingsService { get; }
    private IDownloadService DownloadService { get; }

    public ForgeVersioningService(ISettingsService settingsService, IDownloadService downloadService)
    {
        SettingsService = settingsService;
        DownloadService = downloadService;
    }

    public async Task<string> GetLocalVersionAsync(CancellationToken cancellationToken)
    {
        // check last version file
        if (File.Exists(LastVersionFile))
        {
            var lines = await File.ReadAllLinesAsync(LastVersionFile, cancellationToken);
            var version = lines?.FirstOrDefault();
            if (version != null)
                return version;
        }
        // then check jar version
        var forgePath = SettingsService.ForgeInstallationFolder;
        var guiDesktopSnapshotJarVersions = Directory.EnumerateFiles(forgePath, "forge-gui-desktop-*-SNAPSHOT-jar-with-dependencies.jar").Select(x => ExtractVersionFromJar(x)).ToList();
        var localVersion = guiDesktopSnapshotJarVersions.OrderByDescending(x => x).FirstOrDefault();
        return localVersion!;
    }

    public async Task SaveLatestVersionAsync(string version, CancellationToken cancellationToken)
    {
        await File.WriteAllLinesAsync(LastVersionFile, new[] { version }, cancellationToken);
    }

    public async Task<(string serverVersion, string serverVersionFilename)> GetServerVersionAsync(CancellationToken cancellationToken)
    {
        var dailySnapshotsUrl = SettingsService.DailySnapshotsUrl;

        var html = await DownloadService.DownloadHtmlAsync(dailySnapshotsUrl, cancellationToken);

        // search for <a href="forge-gui-desktop
        var guiDesktopAhrefLine = html.Split(Environment.NewLine.ToCharArray()).FirstOrDefault(x => x.TrimStart().StartsWith("<a href=\"forge-gui-desktop"));
        if (guiDesktopAhrefLine != null)
        {
            var firstDoubleQuoteIndex = guiDesktopAhrefLine.IndexOf("\"");
            if (firstDoubleQuoteIndex != -1)
            {
                var secondDoubleQuoteIndex = guiDesktopAhrefLine.IndexOf("\"", firstDoubleQuoteIndex + 1);
                if (secondDoubleQuoteIndex != -1)
                {
                    var serverVersionFilename = guiDesktopAhrefLine.Substring(firstDoubleQuoteIndex + 1, secondDoubleQuoteIndex - firstDoubleQuoteIndex - 1);
                    if (serverVersionFilename.EndsWith(".tar.bz2"))
                    {
                        var serverVersion = serverVersionFilename.Replace("forge-gui-desktop-", string.Empty).Replace(".tar.bz2", string.Empty);
                        return (serverVersion, serverVersionFilename);
                    }
                }
            }
        }
        return default;
    }

    public bool IsVersionOutdated(string localVersion, string serverVersion)
    {
        if (localVersion.Contains("SNAPSHOT"))
            return VersionComparer.Compare(localVersion, serverVersion) < 0;
        return !serverVersion.Contains(localVersion);
    }

    private static string ExtractVersionFromJar(string filename)
        => Path.GetFileNameWithoutExtension(filename).Replace("forge-gui-desktop-", string.Empty).Replace("-SNAPSHOT-jar-with-dependencies", string.Empty);
}
