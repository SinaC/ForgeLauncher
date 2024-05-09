using System.Threading.Tasks;
using System.Threading;

namespace ForgeLauncher.WPF;

public interface IForgeVersioningService : IVersioningService
{
    Task SaveLatestVersionAsync(string version, CancellationToken cancellationToken);

}
