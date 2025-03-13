using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ForgeLauncher.WPF.Services;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;

namespace ForgeLauncher.WPF;

public class SettingsVM : ObservableObject
{
    protected SettingsVM()
    {
    }

    public SettingsVM(ISettingsService settingsService)
    {
        ForgeInstallationFolder = settingsService.ForgeInstallationFolder;
        ForgeExecutable = settingsService.ForgeExecutable;
        ForgeDailySnapshotUrl = settingsService.ForgeDailySnapshotUrl;
        LauncherReleaseUrl = settingsService.LauncherReleaseUrl;
        CloseWhenStartingForge = settingsService.CloseWhenStartingForge;
    }

    //
    private ICommand _selectForgeInstallationFolderCommand = null!;
    public ICommand SelectForgeInstallationFolderCommand => _selectForgeInstallationFolderCommand ??= new RelayCommand(SelectForgeInstallationFolder);

    private void SelectForgeInstallationFolder()
    {
        using (var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            InitialDirectory = ForgeInstallationFolder
        })
        {
            var result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
                ForgeInstallationFolder = dialog.SelectedPath;
        }
    }

    private string _forgeInstallationFolder = null!;
    public string ForgeInstallationFolder
    {
        get => _forgeInstallationFolder;
        set => SetProperty(ref _forgeInstallationFolder, value);
    }

    //
    private ICommand _selectForgeExecutableCommand = null!;
    public ICommand SelectForgeExecutableCommand => _selectForgeExecutableCommand ??= new RelayCommand(SelectForgeExecutable);

    private void SelectForgeExecutable()
    {
        using (var dialog = new System.Windows.Forms.OpenFileDialog
        {
            InitialDirectory = ForgeInstallationFolder,
            FileName = ForgeExecutable,
            DefaultExt = "exe",
            Filter = "exe files (*.exe)|*.exe",
            CheckFileExists = true,
            CheckPathExists = true,
        })
        {
            var result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
                ForgeExecutable = Path.GetFileName(dialog.FileName);
        }
    }

    private string _forgeExecutable = null!;
    public string ForgeExecutable
    {
        get => _forgeExecutable;
        set => SetProperty(ref _forgeExecutable, value);
    }

    //
    private ICommand _goToForgeDailySnapshotUrlCommand = null!;
    public ICommand GoToForgeDailySnapshotUrlCommand => _goToForgeDailySnapshotUrlCommand ??= new RelayCommand(GoToForgeDailySnapshotUrl);

    private void GoToForgeDailySnapshotUrl()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = ForgeDailySnapshotUrl,
            UseShellExecute = true
        });
    }

    private string _forgeDailySnapshotUrl = null!;
    public string ForgeDailySnapshotUrl
    {
        get => _forgeDailySnapshotUrl;
        set => SetProperty(ref _forgeDailySnapshotUrl, value);
    }

    //
    private ICommand _goToReleaseUrlCommand = null!;
    public ICommand GoToReleaseUrlCommand => _goToReleaseUrlCommand ??= new RelayCommand(GoToReleaseUrl);

    private void GoToReleaseUrl()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = LauncherReleaseUrl,
            UseShellExecute = true
        });
    }

    private string _launcherReleaseUrl = null!;
    public string LauncherReleaseUrl
    {
        get => _launcherReleaseUrl;
        set => SetProperty(ref _launcherReleaseUrl, value);
    }

    //
    private bool _closeWhenStartingForge;
    public bool CloseWhenStartingForge
    {
        get => _closeWhenStartingForge;
        set => SetProperty(ref _closeWhenStartingForge, value);
    }
}

internal class SettingsVMDesignData : SettingsVM
{
    public SettingsVMDesignData() : base()
    {
        ForgeInstallationFolder = @"F:\Forge\";
        ForgeExecutable = "forge.exe";
        ForgeDailySnapshotUrl = @"https://github.com/Card-Forge/forge/releases/";
        LauncherReleaseUrl = @"https://github.com/SinaC/ForgeLauncher/releases/";
        CloseWhenStartingForge = true;
    }
}
