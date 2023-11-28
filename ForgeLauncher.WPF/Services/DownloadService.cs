using ForgeLauncher.WPF.Attributes;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ForgeLauncher.WPF.Services
{
    [Export(typeof(IDownloadService)), Shared]
    public class DownloadService : IDownloadService
    {
        public async Task<string> DownloadHtmlAsync(string url, CancellationToken cancellationToken)
        {
            using var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromDays(1)
            };
            return await httpClient.GetStringAsync(url, cancellationToken);
        }

        public async Task DownloadFileAsync(string downloadUrl, string destinationFilePath, Action<long?, long> progressChanged, CancellationToken cancellationToken)
        {
            using var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromDays(1)
            };
            using var response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            response.EnsureSuccessStatusCode();
            var totalBytes = response.Content.Headers.ContentLength;

            using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var totalBytesRead = 0L;
            var readCount = 0L;
            var buffer = new byte[8192];
            var isMoreToRead = true;

            using var fileStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            do
            {
                var bytesRead = await contentStream.ReadAsync(buffer, cancellationToken);
                if (bytesRead == 0)
                {
                    isMoreToRead = false;
                    progressChanged(totalBytes, totalBytesRead);
                }
                else
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);

                    totalBytesRead += bytesRead;
                    readCount++;

                    if (readCount % 100 == 0)
                        progressChanged(totalBytes, totalBytesRead);
                }
            }
            while (isMoreToRead);
        }
    }
}
