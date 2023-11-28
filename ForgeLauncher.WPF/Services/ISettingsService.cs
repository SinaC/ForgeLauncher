namespace ForgeLauncher.WPF.Services
{
    public interface ISettingsService
    {
        public string ForgeInstallationFolder { get; set; }
        public string DailySnapshotsUrl { get; set; }
    }
}
