using System.Threading;
using System.Threading.Tasks;

namespace ForgeLauncher.WPF.Services;

public interface ISettingsService
{
    string ForgeInstallationFolder { get; set; }
    string ForgeExecutable { get; set; }
    string DailySnapshotsUrl { get; set; }
    bool CloseWhenStartingForge { get; set; }
    string ReleaseUrl { get; set; }

    Task SaveAsync(CancellationToken cancellationToken);
}
