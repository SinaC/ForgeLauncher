using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ForgeLauncher.WPF.Services;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;

namespace ForgeLauncher.WPF
{
    public class SettingsVM : ObservableObject
    {
        protected SettingsVM()
        {
        }

        public SettingsVM(ISettingsService settingsService)
        {
            ForgeInstallationFolder = settingsService.ForgeInstallationFolder;
            ForgeExecutable = settingsService.ForgeExecutable;
            DailySnapshotsUrl = settingsService.DailySnapshotsUrl;
            ReleaseUrl = settingsService.ReleaseUrl;
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
        private ICommand _goToDailySnapshotUrlCommand = null!;
        public ICommand GoToDailySnapshotUrlCommand => _goToDailySnapshotUrlCommand ??= new RelayCommand(GoToDailySnapshotUrl);

        private void GoToDailySnapshotUrl()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = DailySnapshotsUrl,
                UseShellExecute = true
            });
        }

        private string _dailySnapshotsUrl = null!;
        public string DailySnapshotsUrl
        {
            get => _dailySnapshotsUrl;
            set => SetProperty(ref _dailySnapshotsUrl, value);
        }

        //
        private ICommand _goToReleaseUrlCommand = null!;
        public ICommand GoToReleaseUrlCommand => _goToReleaseUrlCommand ??= new RelayCommand(GoToReleaseUrl);

        private void GoToReleaseUrl()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = ReleaseUrl,
                UseShellExecute = true
            });
        }

        private string _releaseUrl = null!;
        public string ReleaseUrl
        {
            get => _releaseUrl;
            set => SetProperty(ref _releaseUrl, value);
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
            DailySnapshotsUrl = @"https://downloads.cardforge.org/dailysnapshots/";
            ReleaseUrl = @"https://github.com/SinaC/ForgeLauncher/releases/";
            CloseWhenStartingForge = true;
        }
    }
}
