using System.Threading;
using System.Threading.Tasks;

namespace ForgeLauncher.WPF.Services;

public interface ISettingsService
{
    string ForgeInstallationFolder { get; set; }
    string ForgeExecutable { get; set; }
    string ForgeDailySnapshotUrl { get; set; }
    bool CloseWhenStartingForge { get; set; }
    string LauncherReleaseUrl { get; set; }

    Task SaveAsync(CancellationToken cancellationToken);
}
