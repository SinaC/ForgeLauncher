namespace ForgeLauncher.WPF.Services
{
    public interface IUnpackService
    {
        void ExtractTarBz2(string archiveName, string destinationFolder);
    }
}
