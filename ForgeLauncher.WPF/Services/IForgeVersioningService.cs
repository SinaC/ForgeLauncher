using System.Threading;
using System.Threading.Tasks;

namespace ForgeLauncher.WPF;

public interface IForgeVersioningService : IVersioningService
{
    Task SaveLatestVersionAsync(string version, CancellationToken cancellationToken);

}
