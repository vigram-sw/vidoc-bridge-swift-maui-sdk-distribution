using System.Collections.ObjectModel;
using System.Text.Json;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// Represents a single NTRIP connection configuration, including host, port, credentials, and mountpoint.
/// </summary>
public class NtripConnectionConfig
{
    /// <summary>
    /// NTRIP caster hostname or IP address.
    /// </summary>
    public string Hostname { get; set; } = "";

    /// <summary>
    /// NTRIP caster port, default is 2101.
    /// </summary>
    public int Port { get; set; } = 2101;

    /// <summary>
    /// Username for NTRIP authentication.
    /// </summary>
    public string Username { get; set; } = "";

    /// <summary>
    /// Password for NTRIP authentication.
    /// </summary>
    public string Password { get; set; } = "";

    /// <summary>
    /// Mountpoint to connect to on the NTRIP caster.
    /// </summary>
    public string MountPoint { get; set; } = "";

    /// <summary>
    /// Returns a human-readable string representation of the configuration.
    /// Example: "hostname:2101 [MOUNT] (username)"
    /// </summary>
    public override string ToString() => $"{Hostname}:{Port} [{MountPoint}] ({Username})";
}

/// <summary>
/// Manages a collection of NTRIP connection configurations.
/// Supports loading, saving, selecting, adding, and removing configs using MAUI Preferences for persistence.
/// </summary>
public class NtripConfigManager
{
    // Collection of saved NTRIP configurations
    private readonly ObservableCollection<NtripConnectionConfig> _configs = new();

    // Key used for persistent storage
    private const string StorageKey = "NtripConfigs";

    // Currently selected configuration
    private NtripConnectionConfig? _selectedConfig;

    /// <summary>
    /// Initializes a new instance and loads previously saved configurations.
    /// </summary>
    public NtripConfigManager()
    {
        LoadConfigs();
    }

    /// <summary>
    /// Loads saved configurations from MAUI Preferences.
    /// If parsing fails, errors are ignored.
    /// </summary>
    private void LoadConfigs()
    {
        if (Preferences.ContainsKey(StorageKey))
        {
            string? json = Preferences.Get(StorageKey, "");
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var list = JsonSerializer.Deserialize<List<NtripConnectionConfig>>(json);
                    if (list != null)
                        foreach (var c in list)
                            _configs.Add(c);
                }
                catch { /* Parsing errors are ignored */ }
            }
        }
    }

    /// <summary>
    /// Saves the current collection of configurations to MAUI Preferences.
    /// </summary>
    private void SaveConfigs()
    {
        string json = JsonSerializer.Serialize(_configs);
        Preferences.Set(StorageKey, json);
    }

    /// <summary>
    /// Returns a read-only collection of all saved NTRIP configurations.
    /// </summary>
    public IReadOnlyCollection<NtripConnectionConfig> GetConfigs() => _configs;

    /// <summary>
    /// Returns the currently selected configuration, if any.
    /// </summary>
    public NtripConnectionConfig? SelectedConfig => _selectedConfig;

    /// <summary>
    /// Adds a new configuration to the collection if an identical one does not already exist.
    /// Saves the updated collection to persistent storage.
    /// </summary>
    /// <param name="config">The NTRIP configuration to add.</param>
    public void AddIfNotExists(NtripConnectionConfig config)
    {
        if (!_configs.Any(c =>
            c.Hostname == config.Hostname &&
            c.Port == config.Port &&
            c.Username == config.Username &&
            c.MountPoint == config.MountPoint))
        {
            _configs.Add(config);
            SaveConfigs();
        }
    }

    /// <summary>
    /// Shows an action sheet on the specified page to let the user select a configuration.
    /// Updates the selected configuration based on the user's choice.
    /// </summary>
    /// <param name="parentPage">The MAUI page on which to display the selector.</param>
    public async Task ShowConfigSelector(Page parentPage)
    {
        // Convert configs to display strings
        string[] configStrings = _configs.Select(c => c.ToString()).ToArray();

        // Show the action sheet
        string? selected = await parentPage.DisplayActionSheet(
            "Select NTRIP Config",
            "Cancel",
            null,
            configStrings
        );

        // Update selected config if a valid option was chosen
        if (!string.IsNullOrEmpty(selected) && selected != "Cancel")
        {
            _selectedConfig = _configs.FirstOrDefault(c => c.ToString() == selected);
        }
    }

    /// <summary>
    /// Removes a configuration from the collection and updates persistent storage.
    /// </summary>
    /// <param name="config">The configuration to remove.</param>
    public void RemoveConfig(NtripConnectionConfig config)
    {
        if (_configs.Contains(config))
        {
            _configs.Remove(config);
            SaveConfigs();
        }
    }
}