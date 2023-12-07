using System.Threading;
using System.Threading.Tasks;

namespace ForgeLauncher.WPF;

public interface IVersioningService
{
    Task<string> GetLocalVersionAsync(CancellationToken cancellationToken);
    Task SaveLatestVersionAsync(string version, CancellationToken cancellationToken);
    Task<(string serverVersion, string serverVersionFilename)> GetServerVersionAsync(CancellationToken cancellationToken);
    bool IsVersionOutdated(string localVersion, string serverVersion);
}
