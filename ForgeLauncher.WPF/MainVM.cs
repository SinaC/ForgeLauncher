﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ForgeLauncher.WPF.Attributes;
using ForgeLauncher.WPF.Services;
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

    public MainVM(ILogger logger, ISettingsService settingsService, IDownloadService downloadService, IUnpackService unpackService)
    {
        Logger = logger;
        SettingsService = settingsService;
        DownloadService = downloadService;
        UnpackService = unpackService;

        Logs = new ObservableCollection<string>
        {
            "Application started."
        };

        if (!DesignMode.IsInDesignModeStatic)
            InitializeAsync(CancellationToken.None);
    }

    private string ServerVersionFilename { get; set; } = null!;

    // Check current version, check latest version, update if needed then launch
    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        try
        {
            var versionChecker = new VersionChecker(SettingsService, DownloadService);
            Log("Checking local version...");
            var localVersion = versionChecker.CheckLocalVersion();
            if (localVersion == null)
                Log("Forge is not installed!");
            else
                Log($"Local version is {localVersion}");
            Log("Checking server version...");
            await versionChecker.CheckServerVersionAsync(cancellationToken)
                .ContinueWith(t =>
                {
                    if (t.Result == default)
                        LogError($"Cannot retrieve server version!");
                    else
                    {
                        Log($"Server version is {t.Result.serverVersion}");
                        ServerVersionFilename = t.Result.serverVersionFilename;
                    }
                    return t.Result;
                }, cancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default)
                .ContinueWith(t => UpdateIfNeededAsync(localVersion!, t.Result.serverVersion, cancellationToken), cancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
        }
        catch (Exception ex)
        {
            LogError("Error while checking version!");
        }
    }

    private async Task UpdateIfNeededAsync(string localVersion, string serverVersion, CancellationToken cancellationToken)
    {
        if (serverVersion == null)
            return;
        // TODO: center message box on Wpf window
        if (localVersion == null) // not installed
        {
            var messageBoxResult = MessageBox.Show("Forge is not installed, do you want to install and start Forge ?", "Forge Launcher", MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                Log("Installing Forge...");
                await DownloadAsync(cancellationToken)
                    .ContinueWith(_ => Unpack(), cancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default)
                    .ContinueWith(_ => Log("Installation complete."), cancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default)
                    .ContinueWith(_ => Launch(), cancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
            }
        }
        else if (!serverVersion.Contains(localVersion)) // updated needed
        {
            Log("Forge is outdated.");
            var messageBoxResult = MessageBox.Show("Forge is outdated, do you want to update and start Forge ?", "Forge Launcher", MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                Log("Updating to lastest version...");
                await DownloadAsync(cancellationToken)
                    .ContinueWith(_ => Unpack(), cancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default)
                    .ContinueWith(_ => Log("Update complete."), cancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default)
                    .ContinueWith(_ => Launch(), cancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
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
        if (ServerVersionFilename == null)
        {
            LogError("Cannot update. Server issue!");
            MessageBox.Show("Cannot update. Server issue!");
            return;
        }
        await DownloadAsync(cancellationToken)
            .ContinueWith(_ => Unpack(), cancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
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
            Log($"Downloading update...");

            IsDownloading = true;
            var dailySnapshotsUrl = SettingsService.DailySnapshotsUrl;
            var downloadUrl = dailySnapshotsUrl + ServerVersionFilename;
            var destinationFilePath = Path.Combine(Path.GetTempPath(), ServerVersionFilename);

            await DownloadService.DownloadFileAsync(downloadUrl, destinationFilePath, HandleDownloadProgressChanged, cancellationToken);

            Log("Update downloaded.");
        }
        catch (Exception ex)
        {
            LogError("Download failed!");
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

    private void Unpack()
    {
        try
        {
            Log("Unpacking update...");

            IsUnpacking = true;
            var sourceFilePath = Path.Combine(Path.GetTempPath(), ServerVersionFilename);
            var forgePath = SettingsService.ForgeInstallationFolder;
            UnpackService.ExtractTarBz2(sourceFilePath, forgePath);

            Log("Update unpacked.");
        }
        catch (Exception ex)
        {
            LogError("Unpack failed!");
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
            var exePath = Path.Combine(forgePath, "forge.exe");
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
        }
        catch (Exception ex)
        {
            LogError("Error while launching forge");
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
        Application.Current.Dispatcher.BeginInvoke(() => Logs.Add(logEntry));
    }

    public void LogError(string logEntry)
    {
        if (!DesignMode.IsInDesignModeStatic)
            Logger.Error(logEntry);
        Application.Current.Dispatcher.BeginInvoke(() => Logs.Add(logEntry));
    }
}

internal sealed class MainVMDesignData : MainVM
{
    public MainVMDesignData(): base(null!, null!, null!, null!)
    {
        IsDownloading = true;
        DownloadProgress = 15;

        Logs.Add("Line 1");
        Logs.Add("Line 2");
    }
}
