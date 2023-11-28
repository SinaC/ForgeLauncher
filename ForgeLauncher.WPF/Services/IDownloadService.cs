using System;
using System.Threading;
using System.Threading.Tasks;

namespace ForgeLauncher.WPF.Services
{
    public interface IDownloadService
    {
        Task<string> DownloadHtmlAsync(string url, CancellationToken cancellationToken);
        Task DownloadFileAsync(string downloadUrl, string destinationFilePath, Action<long?, long> progressChanged, CancellationToken cancellationToken);
    }
}
