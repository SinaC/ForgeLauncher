using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace ForgeLauncher.WPF
{
    public class SettingsManager
    {
        private const string SettingFileName = "ForgeLauncher.settings";
        private const string DefaultDailySnapshotUrl = "https://downloads.cardforge.org/dailysnapshots/";

        private Dictionary<string, string> Settings { get; } = new Dictionary<string, string>();

        public SettingsManager()
        {
        }

        public string ForgeInstallationFolder => GetOrDefault(Settings, "ForgeInstallationFolder", System.AppDomain.CurrentDomain.BaseDirectory);
        public string DailySnapshotsUrl => GetOrDefault(Settings, "DailySnapshotsUrl", DefaultDailySnapshotUrl);

        public async Task Load()
        {
            if (File.Exists(SettingFileName))
            {
                var json = File.ReadAllText(SettingFileName);
                var dictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                Settings.Clear();
                foreach (var kv in dictionary)
                    Settings.Add(kv.Key, kv.Value);
            }
            // TODO: set default value
        }

        public void Save()
        {
            var json = JsonSerializer.Serialize(Settings);
            File.WriteAllText(json, SettingFileName);
        }

        private static TValue GetOrDefault<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
        {
            if (!dictionary.TryGetValue(key, out var value))
                return defaultValue;
            return value;
        }
    }
}
