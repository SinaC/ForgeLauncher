using ForgeLauncher.WPF.Attributes;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ForgeLauncher.WPF.Services;

[Export(typeof(ISettingsService)), Shared]
public class SettingsService : ISettingsService
{
    private const string SettingFileName = "Forge.Launcher.WPF.config";
    private const string DefaultDailySnapshotUrl = "https://downloads.cardforge.org/dailysnapshots/";

    private const string ForgeInstallationFolderKey = "ForgeInstallationFolder";
    private const string DailySnapshotsUrlKey = "DailySnapshotsUrl";

    private ILogger Logger { get; }
    private IDictionary<string, string> Settings { get; } = new Dictionary<string, string>();

    public SettingsService(ILogger logger)
    {
        Logger = logger;
    }

    public string ForgeInstallationFolder
    {
        get => GetOrDefault(ForgeInstallationFolderKey, AppDomain.CurrentDomain.BaseDirectory);
        set => Set(ForgeInstallationFolderKey, value);
    }

    public string DailySnapshotsUrl
    {
        get => GetOrDefault(DailySnapshotsUrlKey, DefaultDailySnapshotUrl);
        set => Set(DailySnapshotsUrlKey, value);
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
                Logger.Error(ex.ToString());
            }
        }
    }

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingFileName, json);
        }
        catch (Exception ex)
        {
            Logger.Error(ex.ToString());
        }
    }

    private string GetOrDefault(string key, string defaultValue)
    {
        if (Settings.Count == 0)
            Load();
        if (!Settings.TryGetValue(key, out var value))
            return defaultValue;
        return value;
    }

    private void Set(string key, string value)
    {
        Settings[key] = value;
        Save();
    }
}
