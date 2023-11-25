using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ForgeLauncher.WPF
{
    public class MainVM : ObservableObject
    {
        public MainVM()
        {
            Logs = new ObservableCollection<string>
            {
                "Application started"
            };

            CheckLatestVersionAsync(CancellationToken.None);
        }

        // current version
        // TODO

        //  Latest version
        private string _latestFilename = null!;
        public string LatestFilename
        {
            get => _latestFilename;
            protected set => SetProperty(ref _latestFilename, value);
        }

        private async Task CheckLatestVersionAsync(CancellationToken cancellationToken)
        {
            try
            {
                AddLog("Checking latest version...");

                var dailySnapshotsUrl = ConfigurationManager.AppSettings["DailySnapshotsUrl"];

                var download = new Download();
                var html = await download.DownloadHtmlAsync(dailySnapshotsUrl, cancellationToken);

                // search for <a href="forge-gui-desktop
                var guiDesktopAhrefLine = html.Split(Environment.NewLine.ToCharArray()).FirstOrDefault(x => x.TrimStart().StartsWith("<a href=\"forge-gui-desktop"));
                if (guiDesktopAhrefLine != null)
                {
                    var firstDoubleQuoteIndex = guiDesktopAhrefLine.IndexOf("\"");
                    if (firstDoubleQuoteIndex == -1)
                        AddLog("Error while parsing filename (code 1)");
                    else
                    {
                        var secondDoubleQuoteIndex = guiDesktopAhrefLine.IndexOf("\"", firstDoubleQuoteIndex + 1);
                        if (secondDoubleQuoteIndex == -1)
                            AddLog("Error while parsing filename (code 2)");
                        else
                        {
                            var latestFilename = guiDesktopAhrefLine.Substring(firstDoubleQuoteIndex + 1, secondDoubleQuoteIndex - firstDoubleQuoteIndex - 1);
                            if (!latestFilename.EndsWith(".tar.bz2"))
                                AddLog("Error while parsing filename (code 3)");
                            else
                            {
                                var latestVersion = latestFilename.Replace("forge-gui-desktop-", string.Empty).Replace(".tar.bz2", string.Empty);
                                AddLog($"Latest version is {latestVersion}");
                                LatestFilename = latestFilename;
                            }
                        }
                    }
                }
                else
                    AddLog("Error while parsing website");
            }
            catch (Exception ex)
            {
                AddLog("Error while checking lastest version");
            }
        }

        public bool IsInProgress => IsDownloading || IsExtracting;

        // Update

        private ICommand? _updateCommand;
        public ICommand UpdateCommand => _updateCommand ??= new AsyncRelayCommand(UpdateAsync);

        private async Task UpdateAsync(CancellationToken cancellationToken)
        {
            await DownloadAsync(cancellationToken)
                .ContinueWith(_ => Extract(), cancellationToken);
        }

        private bool _isDownloading;
        public bool IsDownloading
        {
            get => _isDownloading;
            protected set => SetProperty(ref _isDownloading, value, nameof(IsDownloading), nameof(IsInProgress));
        }

        private double _downloadProgress;
        public double DownloadProgress
        {
            get => _downloadProgress;
            protected set => SetProperty(ref _downloadProgress, value);
        }

        private async Task DownloadAsync(CancellationToken cancellationToken)
        {
            try
            {
                AddLog("Downloading data...");

                IsDownloading = true;
                var dailySnapshotsUrl = ConfigurationManager.AppSettings["DailySnapshotsUrl"];
                var downloadUrl = dailySnapshotsUrl + LatestFilename;
                var destinationFilePath = Path.Combine(Path.GetTempPath(), LatestFilename);

                var download = new Download();
                await download.DownloadFileAsync(downloadUrl, destinationFilePath, HandleDownloadProgressChanged, cancellationToken);

                AddLog("Data downloaded.");
            }
            catch (Exception ex)
            {
                AddLog("Download failed!");
            }
            finally
            {
                IsDownloading = false;
            }
        }

        private bool _isExtracting;
        public bool IsExtracting
        {
            get => _isExtracting;
            protected set => SetProperty(ref _isExtracting, value, nameof(IsExtracting), nameof(IsInProgress));
        }

        private void Extract()
        {
            try
            {
                AddLog("Extracting data...");

                IsExtracting = true;
                var sourceFilePath = Path.Combine(Path.GetTempPath(), LatestFilename);
                var forgePath = ConfigurationManager.AppSettings["ForgeInstallationFolder"];
                var extract = new Extract();
                extract.ExtractTarBz2(sourceFilePath, forgePath);

                AddLog("Extraction complete.");
            }
            catch (Exception ex)
            {
                AddLog("Extraction failed!");
            }
            finally
            {
                IsExtracting = false;
            }
        }

        private void HandleDownloadProgressChanged(long? totalBytes, long totalBytesRead)
        {
            DownloadProgress = totalBytes.HasValue
                ? (double)totalBytesRead / totalBytes.Value * 100.0
                : 0;
        }

        // Launch
        private ICommand? _launchCommand;
        public ICommand LaunchCommand => _launchCommand ??= new RelayCommand(Launch);

        private void Launch()
        {
            try
            {
                var forgePath = ConfigurationManager.AppSettings["ForgeInstallationFolder"];
                // TODO: use exe from combo
                var exePath = Path.Combine(forgePath, "forge.exe");
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    WorkingDirectory = forgePath,
                    UseShellExecute = false,
                };
                AddLog("Launching forge");
                var process = Process.Start(processStartInfo);
            }
            catch (Exception ex)
            {
                AddLog("Error while launching forge");
            }
        }

        // Logs
        private ObservableCollection<string> _logs = null!;
        public ObservableCollection<string> Logs
        {
            get => _logs;
            protected set => SetProperty(ref _logs, value);
        }

        private void AddLog(string logEntry)
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(() => Logs.Add(logEntry));
        }

        //
        private bool SetProperty<T>([NotNullIfNotNull(nameof(newValue))] ref T field, T newValue, string propertyName, params string[] additionalPropertyNames)
        {
            if (EqualityComparer<T>.Default.Equals(field, newValue))
            {
                return false;
            }

            OnPropertyChanging(propertyName);
            if (additionalPropertyNames?.Length > 0)
            {
                foreach (string additionalPropertyName in additionalPropertyNames)
                    OnPropertyChanging(additionalPropertyName);
            }

            field = newValue;

            OnPropertyChanged(propertyName);
            if (additionalPropertyNames?.Length > 0)
            {
                foreach (string additionalPropertyName in additionalPropertyNames)
                    OnPropertyChanged(additionalPropertyName);
            }

            return true;
        }
    }

    internal sealed class MainVMDesignData : MainVM
    {
        public MainVMDesignData()
        {
            IsDownloading = true;
            DownloadProgress = 15;

            Logs.Add("Line 1");
            Logs.Add("Line 2");
        }
    }
}
