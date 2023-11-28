using ForgeLauncher.WPF.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ForgeLauncher.WPF.Services
{
    [Export(typeof(ISettingsService)), Shared]
    public class SettingsService : ISettingsService
    {
        private const string SettingFileName = "ForgeLauncher.settings";
        private const string DefaultDailySnapshotUrl = "https://downloads.cardforge.org/dailysnapshots/";

        private const string ForgeInstallationFolderKey = "ForgeInstallationFolder";
        private const string DailySnapshotsUrlKey = "DailySnapshotsUrl";

        private Dictionary<string, string> Settings { get; } = new Dictionary<string, string>();

        public string ForgeInstallationFolder
        {
            get => GetOrDefault(Settings, ForgeInstallationFolderKey, AppDomain.CurrentDomain.BaseDirectory);
            set
            {
                Settings[ForgeInstallationFolderKey] = value;
                Save();
            }
        }
        public string DailySnapshotsUrl
        {
            get => GetOrDefault(Settings, DailySnapshotsUrlKey, DefaultDailySnapshotUrl);
            set
            {
                Settings[DailySnapshotsUrlKey] = value;
                Save();
            }
        }

        private void Load()
        {
            if (File.Exists(SettingFileName))
            {
                try
                {
                    var json = File.ReadAllText(SettingFileName);
                    var dictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (dictionary != null)
                    {
                        Settings.Clear();
                        foreach (var kv in dictionary)
                            Settings.Add(kv.Key, kv.Value);
                    }
                }
                catch (Exception ex)
                {
                    //TODO
                }
            }
        }

        private void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(Settings);
                File.WriteAllText(json, SettingFileName);
            }
            catch (Exception ex)
            {
                //TODO
            }
        }

        private TValue GetOrDefault<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
        {
            if (dictionary.Count == 0)
                Load();
            if (!dictionary.TryGetValue(key, out var value))
                return defaultValue;
            return value;
        }
    }
}
