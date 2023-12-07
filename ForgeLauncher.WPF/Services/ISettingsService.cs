using System.Threading.Tasks;
using System.Threading;

namespace ForgeLauncher.WPF.Services;

public interface ISettingsService
{
    public string ForgeInstallationFolder { get; set; }
    public string DailySnapshotsUrl { get; set; }
    public bool CloseWhenStartingForge { get; set; }

    Task SaveAsync(CancellationToken cancellationToken);
}
