using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ForgeLauncher.WPF.Attributes;
using ForgeLauncher.WPF.Services;
using MaterialDesignThemes.Wpf;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ForgeLauncher.WPF;

[Export]
public class MainVM : ObservableObject
{
    private ILogger Logger { get; }
    private ISettingsService SettingsService { get; }
    private IDownloadService DownloadService { get; }
    private IUnpackService UnpackService { get; }
    private IForgeVersioningService ForgeVersioningService { get; }
    private ILauncherVersioningService LauncherVersioningService { get; }

    public MainVM(ILogger logger, ISettingsService settingsService, IDownloadService downloadService, IUnpackService unpackService, IForgeVersioningService forgeVersioningService, ILauncherVersioningService launcherVersioningService)
    {
        Logger = logger;
        SettingsService = settingsService;
        DownloadService = downloadService;
        UnpackService = unpackService;
        ForgeVersioningService = forgeVersioningService;
        LauncherVersioningService = launcherVersioningService;

        Logs = new ObservableCollection<string>
        {
            "Application started."
        };

        if (!DesignMode.IsInDesignModeStatic)
            //Task.Run(() => InitializeAsync(CancellationToken.None)); // when using this, UI will not be blocked by MessageBox
            InitializeAsync(CancellationToken.None);
    }

    private string ForgeServerVersion { get; set; } = null!;
    private string ForgeServerVersionFilename { get; set; } = null!;

    // Check current version, check latest version, update if needed then launch
    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var restartConfirmed = await InitializeLauncherAsync(cancellationToken);
        if (!restartConfirmed)
            await InitializeForgeAsync(cancellationToken);
    }

    private async Task<bool> InitializeLauncherAsync(CancellationToken cancellationToken)
    {
        try
        {
            File.Delete("Forge.Launcher.WPF.exe.temp");
            Log("Checking local Launcher version...");
            var localVersion = await LauncherVersioningService.GetLocalVersionAsync(cancellationToken);
            if (localVersion == null)
                Log("Launcher is not installed!");
            else
                Log($"Local Launcher version is {localVersion}");

            Log("Checking server Launcher version...");
            var serverVersion = await LauncherVersioningService.GetServerVersionAsync(cancellationToken);
            if (serverVersion == default)
                LogError($"Cannot retrieve server Launcher version!");
            else
                Log($"Server Launcher version is {serverVersion.serverVersion}");

            return await UpdateIfNeededAndRestartLauncherAsync(localVersion!, serverVersion.serverVersion, serverVersion.serverVersionFilename, cancellationToken);
        }
        catch (Exception ex)
        {
            LogException(ex);
            LogError("Error while checking version!");
        }
        return false;
    }

    private async Task InitializeForgeAsync(CancellationToken cancellationToken)
    {
        try
        {
            Log("Checking local Forge version...");
            var localVersion = await ForgeVersioningService.GetLocalVersionAsync(cancellationToken);
            if (localVersion == null)
                Log("Forge is not installed!");
            else
                Log($"Local Forge version is {localVersion}");

            Log("Checking server Forge version...");
            var serverVersion = await ForgeVersioningService.GetServerVersionAsync(cancellationToken);
            if (serverVersion == default)
                LogError($"Cannot retrieve server Forge version!");
            else
            {
                Log($"Server Forge version is {serverVersion.serverVersion}");
                ForgeServerVersion = serverVersion.serverVersion;
                ForgeServerVersionFilename = serverVersion.serverVersionFilename;
            }
            await UpdateIfNeededAndStartForgeAsync(localVersion!, serverVersion.serverVersion, cancellationToken);
        }
        catch (Exception ex)
        {
            LogException(ex);
            LogError("Error while checking version!");
        }
    }

    private async Task<bool> UpdateIfNeededAndRestartLauncherAsync(string localVersion, string serverVersion, string serverVersionFilename, CancellationToken cancellationToken)
    {
        if (serverVersion == null)
            return false;
        if (LauncherVersioningService.IsVersionOutdated(localVersion, serverVersion))
        {
            Log("Forge Launcher is outdated.");
            var messageBoxResult = MessageBox.Show("Forge Launcher is outdated, do you want to update and restart ?", "Forge Launcher", MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                Log("Updating Forge Launcher to lastest version...");
                // TODO
                await DownloadLauncherAsync(serverVersion, serverVersionFilename, cancellationToken)
                    .ContinueWith(t => InstallLauncher(t.Result), cancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default)
                    .ContinueWith(_ => Log("Forge Launcher Update complete."), cancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default)
                    .ContinueWith(_ => RestartLauncher(), cancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
                return true;
            }
            else
            {
                Log("Forge Launcher not updated.");
            }
        }
        return false;
    }

    private async Task UpdateIfNeededAndStartForgeAsync(string localVersion, string serverVersion, CancellationToken cancellationToken)
    {
        if (serverVersion == null)
            return;
        if (localVersion == null) // not installed
        {
            var messageBoxResult = MessageBox.Show("Forge is not installed, do you want to install and start Forge ?", "Forge Launcher", MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                Log("Installing Forge...");
                await DownloadForgeAsync(cancellationToken)
                    .ContinueWith(t => UnpackForge(t.Result), cancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default)
                    .ContinueWith(_ => ForgeVersioningService.SaveLatestVersionAsync(serverVersion, cancellationToken), cancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default)
                    .ContinueWith(_ => Log("Forge installation complete."), cancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default)
                    .ContinueWith(_ => Launch(), cancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
            }
        }
        else if (ForgeVersioningService.IsVersionOutdated(localVersion, serverVersion))
        {
            Log("Forge is outdated.");
            var messageBoxResult = MessageBox.Show("Forge is outdated, do you want to update and start Forge ?", "Forge Launcher", MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                Log("Updating Forge to lastest version...");
                await DownloadForgeAsync(cancellationToken)
                    .ContinueWith(t => UnpackForge(t.Result), cancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default)
                    .ContinueWith(_ => ForgeVersioningService.SaveLatestVersionAsync(serverVersion, cancellationToken), cancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default)
                    .ContinueWith(_ => Log("Forge update complete."), cancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default)
                    .ContinueWith(_ => Launch(), cancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
            }
            else
            {
                Log("Forge not updated.");
                messageBoxResult = MessageBox.Show("Do you want to start Forge ?", "Forge Launcher", MessageBoxButton.YesNo);
                if (messageBoxResult == MessageBoxResult.Yes)
                    Launch();
            }
        }
        else
        {
            Log("Forge is up-to-date.");
            var messageBoxResult = MessageBox.Show("Forge is up-to-date, do you want to start Forge ?", "Forge Launcher", MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
                Launch();
        }
    }

    public bool IsInProgress => IsDownloading || IsUnpacking;

    // Update

    private ICommand? _updateCommand;
    public ICommand UpdateCommand => _updateCommand ??= new AsyncRelayCommand(UpdateAsync);

    private async Task UpdateAsync(CancellationToken cancellationToken)
    {
        if (ForgeServerVersionFilename == null)
        {
            LogError("Cannot update. Server issue!");
            MessageBox.Show("Cannot update. Server issue!");
            return;
        }
        await DownloadForgeAsync(cancellationToken)
            .ContinueWith(t => UnpackForge(t.Result), cancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default)
            .ContinueWith(_ => ForgeVersioningService.SaveLatestVersionAsync(ForgeServerVersion, cancellationToken), cancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
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

    private async Task<string> DownloadLauncherAsync(string serverVersion, string serverVersionFilename, CancellationToken cancellationToken)
    {
        try
        {
            Log($"Downloading Forge Launcher update...");

            IsDownloading = true;
            var filename = $"{serverVersion}_{serverVersionFilename}";
            var destinationFilePath = Path.Combine(Path.GetTempPath(), filename);
            var downloadUrl = $"https://github.com/SinaC/ForgeLauncher/releases/download/{serverVersion}/{serverVersionFilename}";
            await DownloadService.DownloadFileAsync(downloadUrl, destinationFilePath, HandleDownloadProgressChanged, cancellationToken);

            Log("Forge Launcher update downloaded.");

            return destinationFilePath;
        }
        catch (Exception ex)
        {
            LogException(ex);
            LogError("Forge Launcher update download failed!");

            return null!;
        }
        finally
        {
            IsDownloading = false;
        }
    }

    private async Task<string> DownloadForgeAsync(CancellationToken cancellationToken)
    {
        try
        {
            Log($"Downloading Forge update...");

            IsDownloading = true;
            var dailySnapshotsUrl = SettingsService.DailySnapshotsUrl;
            var downloadUrl = dailySnapshotsUrl + ForgeServerVersionFilename;
            var destinationFilePath = Path.Combine(Path.GetTempPath(), ForgeServerVersionFilename);

            await DownloadService.DownloadFileAsync(downloadUrl, destinationFilePath, HandleDownloadProgressChanged, cancellationToken);

            Log("Forge update downloaded.");


            return destinationFilePath;
        }
        catch (Exception ex)
        {
            LogException(ex);
            LogError("Forge update download failed!");

            return null!;
        }
        finally
        {
            IsDownloading = false;
        }
    }

    private bool _isUnpacking;
    public bool IsUnpacking
    {
        get => _isUnpacking;
        protected set => SetProperty(ref _isUnpacking, value, nameof(IsUnpacking), nameof(IsInProgress));
    }

    private void InstallLauncher(string archiveFilePath)
    {
        if (archiveFilePath == null)
        {
            Log("Failed to install Forge Launcher");
            return;
        }

        try
        {
            Log("Installing Forge Launcher update...");

            IsUnpacking = true;
            var destinationFilePath = Path.GetTempPath();
            Logger.Information($"Extracting {archiveFilePath} to {destinationFilePath}");
            UnpackService.ExtractTarBz2(archiveFilePath, destinationFilePath);
            
            Logger.Information($"Rename Forge.Launcher.WPF.exe to Forge.Launcher.WPF.exe.temp");
            File.Move("Forge.Launcher.WPF.exe", "Forge.Launcher.WPF.exe.temp", true);

            var copyDestinationFilePath = Path.Combine(destinationFilePath, "Forge.Launcher.WPF.exe");
            Logger.Information($"Copy {copyDestinationFilePath} to Forge.Launcher.WPF.exe");
            File.Copy(copyDestinationFilePath, "Forge.Launcher.WPF.exe");

            Logger.Information($"Delete {archiveFilePath}");
            File.Delete(archiveFilePath);

            Log("Forge Launcher update installed.");
        }
        catch (Exception ex)
        {
            LogException(ex);
            LogError("Forge Launcher install failed!");
        }
        finally
        {
            IsUnpacking = false;
        }
    }

    private void UnpackForge(string archiveFilePath)
    {
        if (archiveFilePath == null)
        {
            Log("Failed to unpack forge");
            return;
        }

        try
        {
            Log("Unpacking Forge update...");

            IsUnpacking = true;
            var forgePath = SettingsService.ForgeInstallationFolder;
            UnpackService.ExtractTarBz2(archiveFilePath, forgePath);
            File.Delete(archiveFilePath);

            Log("Forge update unpacked.");
        }
        catch (Exception ex)
        {
            LogException(ex);
            LogError("Forge unpack failed!");
        }
        finally
        {
            IsUnpacking = false;
        }
    }

    private void HandleDownloadProgressChanged(long? totalBytes, long totalBytesRead)
    {
        DownloadProgress = totalBytes.HasValue
            ? (double)totalBytesRead / totalBytes.Value * 100.0
            : 0;
    }

    // Restart
    public void RestartLauncher()
    {
        Process.Start("Forge.Launcher.WPF.exe");
        //Application.Current.Shutdown();
        //Application.Current.MainWindow.Close();
        Environment.Exit(0);
    }

    // Launch
    private ICommand? _launchCommand;
    public ICommand LaunchCommand => _launchCommand ??= new RelayCommand(Launch);

    private void Launch()
    {
        try
        {
            Log("Launching forge...");
            var forgePath = SettingsService.ForgeInstallationFolder;
            // TODO: use exe from combo
            var exePath = Path.Combine(forgePath, SettingsService.ForgeExecutable);
            if (!File.Exists(exePath))
            {
                Log("Forge executable not found!");
                MessageBox.Show("Forge executable not found!");
                return;
            }
            var processStartInfo = new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = forgePath,
                UseShellExecute = false,
            };
            var process = Process.Start(processStartInfo);
            if (SettingsService.CloseWhenStartingForge)
                Application.Current.Dispatcher.Invoke(() => Application.Current.Shutdown());
        }
        catch (Exception ex)
        {
            LogException(ex);
            LogError("Error while launching forge");
        }
    }

    // Settings
    private ICommand? _displaySettingsEditorCommand;
    public ICommand DisplaySettingsEditorCommand => _displaySettingsEditorCommand ??= new AsyncRelayCommand(DisplaySettingsEditorAsync);

    private async Task DisplaySettingsEditorAsync()
    {
        var settingsVM = new SettingsVM(SettingsService);
        var view = new SettingsView
        {
            DataContext = settingsVM
        };

        var result = await DialogHost.Show(view);
        // save if accept clicked
        if (result is bool accept && accept)
        {
            SettingsService.ForgeInstallationFolder = settingsVM.ForgeInstallationFolder;
            SettingsService.DailySnapshotsUrl = settingsVM.DailySnapshotsUrl;
            SettingsService.ReleaseUrl = settingsVM.ReleaseUrl;
            SettingsService.CloseWhenStartingForge = settingsVM.CloseWhenStartingForge;
            SettingsService.ForgeExecutable = settingsVM.ForgeExecutable;
            await SettingsService.SaveAsync(CancellationToken.None);
        }
    }

    // Logs
    private ObservableCollection<string> _logs = null!;
    public ObservableCollection<string> Logs
    {
        get => _logs;
        protected set => SetProperty(ref _logs, value);
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

    //
    public void Log(string logEntry)
    {
        if (!DesignMode.IsInDesignModeStatic)
            Logger.Information(logEntry);
        Application.Current.Dispatcher.BeginInvoke(() => Logs.Insert(0, logEntry));
    }

    public void LogError(string logEntry)
    {
        if (!DesignMode.IsInDesignModeStatic)
            Logger.Error(logEntry);
        Application.Current.Dispatcher.BeginInvoke(() => Logs.Insert(0, logEntry));
    }

    public void LogException(Exception ex)
    {
        if (!DesignMode.IsInDesignModeStatic)
            Logger.Error(ex.ToString());
    }
}

internal sealed class MainVMDesignData : MainVM
{
    public MainVMDesignData(): base(null!, null!, null!, null!, null!, null!)
    {
        IsDownloading = false;
        DownloadProgress = 15;

        Logs.Add("Line 1");
        Logs.Add("Line 2");
    }
}
