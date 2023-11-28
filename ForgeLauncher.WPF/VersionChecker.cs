using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ForgeLauncher.WPF.Services;

namespace ForgeLauncher.WPF
{
    // TODO:
    // snapshot and release
    // better version compare
    public class VersionChecker
    {
        private ISettingsService SettingsService { get; }
        private IDownloadService DownloadService { get; }

        public VersionChecker(ISettingsService settingsService, IDownloadService downloadService)
        {
            SettingsService = settingsService;
            DownloadService = downloadService;
        }

        public string CheckLocalVersion()
        {
            var forgePath = SettingsService.ForgeInstallationFolder;
            var guiDesktopSnapshotJarVersions = Directory.EnumerateFiles(forgePath, "forge-gui-desktop-*-SNAPSHOT-jar-with-dependencies.jar").Select(x => ExtractVersionFromJar(x)).ToList();
            var localVersion = guiDesktopSnapshotJarVersions.OrderByDescending(x => x).FirstOrDefault();
            return localVersion!;
        }

        public async Task<(string serverVersion, string serverVersionFilename)> CheckServerVersionAsync(CancellationToken cancellationToken)
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

        private static string ExtractVersionFromJar(string filename)
            => Path.GetFileNameWithoutExtension(filename).Replace("forge-gui-desktop-", string.Empty).Replace("-SNAPSHOT-jar-with-dependencies", string.Empty);
    }
}
