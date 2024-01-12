using System.Threading;
using System.Threading.Tasks;

namespace ForgeLauncher.WPF.Services;

public interface ISettingsService
{
    public string ForgeInstallationFolder { get; set; }
    public string ForgeExecutable { get; set; }
    public string DailySnapshotsUrl { get; set; }
    public bool CloseWhenStartingForge { get; set; }

    Task SaveAsync(CancellationToken cancellationToken);
}
