using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace ForgeLauncher.WPF
{
    public class SettingsVM : ObservableObject
    {
        private ICommand _selectFolderCommand = null!;
        public ICommand SelectFolderCommand => _selectFolderCommand ??= new RelayCommand(SelectFolder);

        private void SelectFolder()
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

        private string _dailySnapshotsUrl = null!;
        public string DailySnapshotsUrl
        {
            get => _dailySnapshotsUrl;
            set => SetProperty(ref _dailySnapshotsUrl, value);
        }
    }

    internal class SettingsVMDesignData : SettingsVM
    {
    }
}
