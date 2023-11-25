using SharpCompress.Common;
using SharpCompress.Readers;
using System.IO;
using System.Linq;

namespace ForgeLauncher.WPF
{
    public class Unpacker
    {
        public void ExtractTarBz2(string archiveName, string destinationFolder)
        {
            using var stream = File.OpenRead(archiveName);
            using var reader = ReaderFactory.Open(stream);
            while (reader.MoveToNextEntry())
            {
                if (!reader.Entry.IsDirectory)
                {
                    if (reader.Entry.Key.Any(x => x < 32))
                    {
                        var fixedEntryKeyAsCharArray = reader.Entry.Key.Select(x => x < 32 ? '_' : x).ToArray();
                        var fixedEntryKey = new string(fixedEntryKeyAsCharArray);
                        var filename = Path.Combine(destinationFolder, fixedEntryKey);
                        reader.WriteEntryToFile(filename, new ExtractionOptions
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });
                    }
                    else
                        reader.WriteEntryToDirectory(destinationFolder, new ExtractionOptions
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });
                }
            }
        }
    }
}
