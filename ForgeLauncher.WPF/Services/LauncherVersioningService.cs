using ForgeLauncher.WPF.Attributes;
using ForgeLauncher.WPF.Extensions;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace ForgeLauncher.WPF.Services;

[Export(typeof(ILauncherVersioningService)), Shared]
public class LauncherVersioningService : ILauncherVersioningService
{
    private ISettingsService SettingsService { get; }
    private IDownloadService DownloadService { get; }

    public LauncherVersioningService(ISettingsService settingsService, IDownloadService downloadService)
    {
        SettingsService = settingsService;
        DownloadService = downloadService;
    }

    public async Task<string> GetLocalVersionAsync(CancellationToken cancellationToken)
    {
        var version = Assembly.GetExecutingAssembly().GetName()!.Version!.ToString();
        return await Task.FromResult(version);
    }

    public async Task<(string serverVersion, string serverVersionFilename)> GetServerVersionAsync(CancellationToken cancellationToken)
    {
        var releaseUrl = SettingsService.ReleaseUrl;

        var html = await DownloadService.DownloadHtmlAsync(releaseUrl, cancellationToken);

        // search for <a href="/SinaC/ForgeLauncher/releases/tag/v1.0.2" data-view-component="true" class="Link--primary Link">v1.0.2</a>
        var releaseAhrefLine = html.Split(Environment.NewLine.ToCharArray()).FirstOrDefault(x => x.Contains("<a href=\"/SinaC/ForgeLauncher/releases/tag/")); // releases are ordered from latest to oldest
        if (releaseAhrefLine != null)
        {
            // keep v1.0.2
            var tagIndex = releaseAhrefLine.IndexOf("tag/");
            if (tagIndex != -1)
            {
                var quoteAfterTagIndex = releaseAhrefLine.IndexOf("\"", tagIndex);
                var serverVersion = releaseAhrefLine.Substring(tagIndex + 4, quoteAfterTagIndex - tagIndex - 4);
                // TODO .7z ? check in assets and search for Forge.Launcher.WPF
                var serverVersionFilename = "Forge.Launcher.WPF.zip";
                return (serverVersion, serverVersionFilename);
            }
        }
        return default;
    }

    public bool IsVersionOutdated(string localVersion, string serverVersion)
    {
        if (serverVersion.StartsWith("v"))
            serverVersion = serverVersion[1..];
        if (localVersion.Count(x => x == '.') > 2)
        {
            var dotLastIndex = localVersion.LastIndexOf(".");
            localVersion = localVersion.Substring(0, dotLastIndex);
        }
        return VersionComparer.Compare(localVersion, serverVersion) < 0;
    }
}
