using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;

namespace ScarletCore.Data;

/// <summary>
/// Base class for managing plugin configuration settings with BepInEx.
/// Provides a simplified API for creating, reading, and managing configuration entries.
/// </summary>
/// <param name="pluginGuid">Unique identifier for the plugin</param>
/// <param name="pluginInstance">Reference to the plugin instance</param>
public class Settings(string pluginGuid, BasePlugin pluginInstance) {

  #region Private Fields

  /// <summary>Dictionary containing all configuration entries indexed by key</summary>
  private readonly Dictionary<string, ConfigEntryBase> Entries = [];

  /// <summary>Cached reference to the configuration file</summary>
  private ConfigFile _configFile;

  /// <summary>Plugin GUID used for configuration file naming</summary>
  private readonly string _pluginGuid = pluginGuid ?? throw new ArgumentNullException(nameof(pluginGuid));

  /// <summary>Reference to the plugin instance for accessing BepInEx configuration</summary>
  private readonly BasePlugin _pluginInstance = pluginInstance ?? throw new ArgumentNullException(nameof(pluginInstance));

  #endregion

  #region Properties

  /// <summary>Gets the full path to the configuration file</summary>
  private string ConfigFilePath => Path.Combine(Paths.ConfigPath, $"{_pluginGuid}.cfg");

  /// <summary>Gets all configuration entry keys</summary>
  public Dictionary<string, ConfigEntryBase>.KeyCollection Keys => Entries.Keys;

  /// <summary>Gets all configuration entry values</summary>
  public Dictionary<string, ConfigEntryBase>.ValueCollection Values => Entries.Values;

  #endregion

  #region Lifecycle Management

  /// <summary>
  /// Cleans up resources and clears all configuration entries.
  /// Should be called when the plugin is being unloaded.
  /// </summary>
  public void Dispose() {
    _configFile = null;
    Entries.Clear();
    _pluginInstance.Config.Clear();
  }

  /// <summary>
  /// Saves the current configuration to disk and clears the cached config file.
  /// </summary>
  public void Save() {
    _pluginInstance.Config.Save();
    _configFile = null; // Clear cache to force reload on next access
  }

  #endregion

  #region Section Management

  /// <summary>
  /// Creates a fluent interface for adding multiple settings to a specific section.
  /// </summary>
  /// <param name="sectionName">Name of the configuration section</param>
  /// <returns>SettingsSection instance for chaining configuration additions</returns>
  /// <exception cref="ArgumentException">Thrown when sectionName is null or empty</exception>
  public SettingsSection Section(string sectionName) {
    if (string.IsNullOrWhiteSpace(sectionName))
      throw new ArgumentException("Section name cannot be null or empty", nameof(sectionName));

    return new SettingsSection(this, sectionName);
  }

  #endregion

  #region Entry Management

  /// <summary>
  /// Adds a new configuration entry with the specified parameters.
  /// </summary>
  /// <typeparam name="T">Type of the configuration value</typeparam>
  /// <param name="section">Configuration section name</param>
  /// <param name="key">Configuration key identifier</param>
  /// <param name="defaultValue">Default value if not found in config file</param>
  /// <param name="description">Description of the configuration entry</param>
  /// <exception cref="ArgumentException">Thrown when section or key is null or empty</exception>
  public void Add<T>(string section, string key, T defaultValue, string description) {
    if (string.IsNullOrWhiteSpace(section))
      throw new ArgumentException("Section cannot be null or empty", nameof(section));
    if (string.IsNullOrWhiteSpace(key))
      throw new ArgumentException("Key cannot be null or empty", nameof(key));

    var entry = InitConfigEntry(section, key, defaultValue, description);
    Entries[key] = entry;
  }

  /// <summary>
  /// Retrieves the value of a configuration entry by key.
  /// </summary>
  /// <typeparam name="T">Expected type of the configuration value</typeparam>
  /// <param name="key">Configuration key to retrieve</param>
  /// <returns>The configuration value</returns>
  /// <exception cref="KeyNotFoundException">Thrown when the key is not found</exception>
  public T Get<T>(string key) {
    if (!Entries.TryGetValue(key, out var entry))
      throw new KeyNotFoundException($"Configuration key '{key}' not found");

    return ((ConfigEntry<T>)entry).Value;
  }

  /// <summary>
  /// Sets the value of an existing configuration entry.
  /// </summary>
  /// <typeparam name="T">Type of the configuration value</typeparam>
  /// <param name="key">Configuration key to update</param>
  /// <param name="value">New value to set</param>
  /// <exception cref="KeyNotFoundException">Thrown when the key is not found</exception>
  public void Set<T>(string key, T value) {
    if (!Entries.TryGetValue(key, out var entry))
      throw new KeyNotFoundException($"Configuration key '{key}' not found");

    ((ConfigEntry<T>)entry).Value = value;
  }

  /// <summary>
  /// Checks if a configuration entry exists with the specified key.
  /// </summary>
  /// <param name="key">Configuration key to check</param>
  /// <returns>True if the key exists, false otherwise</returns>
  public bool Has(string key) => Entries.ContainsKey(key);

  #endregion

  #region Private Helper Methods

  /// <summary>
  /// Initializes a configuration entry and loads existing values from file if available.
  /// </summary>
  /// <typeparam name="T">Type of the configuration value</typeparam>
  /// <param name="section">Configuration section name</param>
  /// <param name="key">Configuration key identifier</param>
  /// <param name="defaultValue">Default value to use</param>
  /// <param name="description">Description of the configuration entry</param>
  /// <returns>Initialized ConfigEntry instance</returns>
  private ConfigEntry<T> InitConfigEntry<T>(string section, string key, T defaultValue, string description) {
    // Create the configuration entry with BepInEx
    var entry = _pluginInstance.Config.Bind(section, key, defaultValue, description);

    // Load existing value from config file if it exists
    if (File.Exists(ConfigFilePath)) {
      var config = GetConfigFile();
      if (config.TryGetEntry(section, key, out ConfigEntry<T> existingEntry)) {
        entry.Value = existingEntry.Value;
      }
    }

    return entry;
  }

  /// <summary>
  /// Gets or creates a cached ConfigFile instance for reading existing configuration.
  /// </summary>
  /// <returns>ConfigFile instance for the plugin's configuration</returns>
  private ConfigFile GetConfigFile() {
    _configFile ??= new ConfigFile(ConfigFilePath, true);
    return _configFile;
  }

  #endregion
}

/// <summary>
/// Fluent interface helper class for adding multiple configuration entries to a specific section.
/// Provides a chainable API for organizing related configuration settings.
/// </summary>
public class SettingsSection {

  #region Private Fields

  /// <summary>Reference to the parent Settings instance</summary>
  private readonly Settings _settings;

  /// <summary>Name of the configuration section</summary>
  private readonly string _sectionName;

  #endregion

  #region Constructor

  /// <summary>
  /// Initializes a new SettingsSection for the specified settings and section.
  /// </summary>
  /// <param name="settings">Parent Settings instance</param>
  /// <param name="sectionName">Name of the configuration section</param>
  internal SettingsSection(Settings settings, string sectionName) {
    _settings = settings;
    _sectionName = sectionName;
  }

  #endregion

  #region Fluent Interface

  /// <summary>
  /// Adds a configuration entry to this section and returns the section for method chaining.
  /// </summary>
  /// <typeparam name="T">Type of the configuration value</typeparam>
  /// <param name="key">Configuration key identifier</param>
  /// <param name="defaultValue">Default value if not found in config file</param>
  /// <param name="description">Description of the configuration entry</param>
  /// <returns>This SettingsSection instance for method chaining</returns>
  public SettingsSection Add<T>(string key, T defaultValue, string description) {
    _settings.Add(_sectionName, key, defaultValue, description);
    return this; // Enable method chaining
  }

  #endregion
}