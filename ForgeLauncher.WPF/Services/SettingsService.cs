using ForgeLauncher.WPF.Attributes;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ForgeLauncher.WPF.Services;

[Export(typeof(ISettingsService)), Shared]
public class SettingsService : ISettingsService
{
    private const string SettingFileName = "Forge.Launcher.WPF.config";
    private const string DefaultDailySnapshotUrl = "https://downloads.cardforge.org/dailysnapshots/";

    private const string ForgeInstallationFolderKey = "ForgeInstallationFolder";
    private const string DailySnapshotsUrlKey = "DailySnapshotsUrl";
    private const string CloseWhenStartingForgeKey = "CloseWhenStartingForge";

    private readonly Lazy<IDictionary<string, string>> _settings = new(() => InitializeSettings());

    private ILogger Logger { get; }
    private IDictionary<string, string> Settings => _settings.Value;

    public SettingsService(ILogger logger)
    {
        Logger = logger;
    }

    public string ForgeInstallationFolder
    {
        get => Get(ForgeInstallationFolderKey, AppDomain.CurrentDomain.BaseDirectory);
        set => Set(ForgeInstallationFolderKey, value);
    }

    public string DailySnapshotsUrl
    {
        get => Get(DailySnapshotsUrlKey, DefaultDailySnapshotUrl);
        set => Set(DailySnapshotsUrlKey, value);
    }

    public bool CloseWhenStartingForge
    {
        get => Get(CloseWhenStartingForgeKey, true);
        set => Set(CloseWhenStartingForgeKey, value);
    }

    public async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(SettingFileName, json, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.Error(ex.ToString());
        }
    }

    private static IDictionary<string, string> InitializeSettings()
    {
        try
        {
            if (File.Exists(SettingFileName))
            {
                var json = File.ReadAllText(SettingFileName);
                var dictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                return dictionary!;
            }
        }
        catch (Exception ex)
        {
        }
        return new Dictionary<string, string>
        {
            { ForgeInstallationFolderKey, AppDomain.CurrentDomain.BaseDirectory },
            { DailySnapshotsUrlKey, DefaultDailySnapshotUrl },
            { CloseWhenStartingForgeKey, "true" }
        };
    }

    private string Get(string key, string defaultValue)
    {
        if (!Settings.TryGetValue(key, out var value))
            return defaultValue;
        return value;
    }

    private void Set(string key, string value)
    {
        Settings[key] = value;
    }

    private bool Get(string key, bool defaultValue)
    {
        if (!Settings.TryGetValue(key, out var value))
            return defaultValue;
        return bool.Parse(value);
    }

    private void Set(string key, bool value)
    {
        Settings[key] = value.ToString();
    }
}
