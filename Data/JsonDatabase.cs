using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ScarletCore.Utils;
using ScarletCore.Events;

namespace ScarletCore.Data;

/// <summary>
/// Generic database service that provides JSON-based persistence for mods.
/// Each instance gets its own folder based on the database name provided in the constructor.
/// </summary>
public class JsonDatabase {
  // Cache to store data in memory
  private readonly Dictionary<string, object> _cache = [];
  private readonly Dictionary<string, DateTime> _cacheTimestamps = [];
  // Database name (folder name)
  private readonly string _databaseName;

  // Temp storage moved to SharedData service

  // Maximum number of backups to keep
  private int _maxBackups = 50;

  // Auto-backup per-instance
  // Auto-backup uses an instance method handler
  private bool _autoBackupEnabled;
  private string _autoBackupLocation;

  /// <summary>
  /// Temporary storage now provided by the SharedData service.
  /// </summary>

  /// <summary>
  /// Gets or sets the maximum number of backups to keep (default: 50)
  /// </summary>
  public int MaxBackups {
    get => _maxBackups;
    set => _maxBackups = value > 0 ? value : 50; // Ensure positive value
  }

  /// <summary>
  /// JSON serialization options
  /// </summary>
  private static readonly JsonSerializerOptions _jsonOptions = new() {
    WriteIndented = true,
    ReferenceHandler = ReferenceHandler.IgnoreCycles
  };
  /// <summary>
  /// Initializes a new instance of the Database class
  /// </summary>
  /// <param name="pluginGuid">The name of the database folder</param>
  public JsonDatabase(string pluginGuid) {
    if (string.IsNullOrWhiteSpace(pluginGuid))
      throw new ArgumentException("Database name cannot be null or empty", nameof(pluginGuid));

    _databaseName = pluginGuid;
    // auto-backup handler defaults
    _autoBackupEnabled = false;
  }

  /// <summary>
  /// Enable automatic backups for this database instance. When enabled, each server save triggers a backup.
  /// </summary>
  /// <param name="backupLocation">Optional backup folder (defaults to BepInEx config path)</param>
  public void EnableAutoBackup(string backupLocation = null) {
    if (_autoBackupEnabled) return;
    _autoBackupLocation = backupLocation;
    // Register instance method as handler (async void for fire-and-forget)
    EventManager.On(ServerEvents.OnSave, AutoBackupHandler);
    _autoBackupEnabled = true;
  }

  /// <summary>
  /// Disable automatic backups for this database instance.
  /// </summary>
  public void DisableAutoBackup() {
    if (!_autoBackupEnabled) return;
    try {
      EventManager.Off(ServerEvents.OnSave, AutoBackupHandler);
    } catch { }
    _autoBackupEnabled = false;
  }

  /// <summary>
  /// Performs cleanup for this database instance when its parent assembly is unloaded.
  /// Disables automatic backups and unregisters any event handlers or resources associated with this instance.
  /// </summary>
  public void UnregisterAssembly() {
    DisableAutoBackup();
  }

  // Instance handler used for auto-backups. Using `async void` because the
  // event system expects an `Action<string>` and this is fire-and-forget.
  [EventPriority(EventPriority.Last)]
  private async void AutoBackupHandler(string saveName) {
    try {
      saveName = saveName.Replace(".save", ""); // Remove extension if present
      await CreateBackup(_autoBackupLocation, saveName);
    } catch (Exception ex) {
      Log.Error($"Auto-backup failed for '{_databaseName}': {ex.Message}");
    }
  }

  /// <summary>
  /// Gets the configuration path for this database instance
  /// </summary>
  /// <returns>Path specific to this database</returns>
  private string GetConfigPath() {
    return Path.Combine(BepInEx.Paths.ConfigPath, _databaseName);
  }

  /// <summary>
  /// Gets the full filesystem path for the specified database-relative path, appending the <c>.json</c> extension.
  /// </summary>
  /// <param name="path">File name or relative path within the database (without the <c>.json</c> extension).</param>
  /// <returns>The full path to the JSON file inside this database's configuration folder.</returns>
  public string GetFullPath(string path) {
    var configPath = GetConfigPath();
    return Path.Combine(configPath, $"{path}.json");
  }

  /// <summary>
  /// Gets the cache key for a specific path and this database
  /// </summary>
  private string GetCacheKey(string path) {
    return $"{_databaseName}:{path}";
  }
  /// <summary>
  /// Saves data to a JSON file in this database's configuration directory
  /// </summary>
  /// <typeparam name="T">Type of data to save</typeparam>
  /// <param name="path">File name/path (without extension)</param>
  /// <param name="data">Data to serialize and save</param>
  public void Save<T>(string path, T data) {
    var configPath = GetConfigPath();
    string filePath = Path.Combine(configPath, $"{path}.json"); try {
      Directory.CreateDirectory(Path.GetDirectoryName(filePath));
      string jsonData = JsonSerializer.Serialize(data, _jsonOptions);
      File.WriteAllText(filePath, jsonData);

      // Updates cache after successful save
      string cacheKey = GetCacheKey(path);
      _cache[cacheKey] = data;
      _cacheTimestamps[cacheKey] = DateTime.Now;
    } catch (Exception ex) {
      Log.Error($"An error occurred while saving data: {ex.Message}");
    }
  }
  /// <summary>
  /// Loads data from a JSON file in this database's configuration directory
  /// </summary>
  /// <typeparam name="T">Type of data to load</typeparam>
  /// <param name="path">File name/path (without extension)</param>
  /// <returns>Deserialized data or default value if file doesn't exist or error occurs</returns>
  public T Load<T>(string path) {
    var configPath = GetConfigPath();
    string filePath = Path.Combine(configPath, $"{path}.json");

    if (!File.Exists(filePath)) {
      return default;
    }
    try {
      string jsonData = File.ReadAllText(filePath);

      // Return default if file is empty or contains only whitespace
      if (string.IsNullOrWhiteSpace(jsonData)) {
        return default;
      }

      var deserializedData = JsonSerializer.Deserialize<T>(jsonData, _jsonOptions);

      // Updates cache after successful load
      string cacheKey = GetCacheKey(path);
      _cache[cacheKey] = deserializedData;
      _cacheTimestamps[cacheKey] = DateTime.Now;

      return deserializedData;
    } catch (Exception ex) {
      Log.Error($"An error occurred while loading data: {ex.Message}");
    }

    return default;
  }
  /// <summary>
  /// Gets from the cache or loads data from the file if not cached.
  /// Any changes to the cache will not be saved to the file unless explicitly saved.
  /// </summary>
  /// <typeparam name="T">Type of data to get</typeparam>
  /// <param name="path">File name/path (without extension)</param>
  /// <returns>Cached or loaded data</returns>
  public T Get<T>(string path) {
    string cacheKey = GetCacheKey(path);

    // Check if it's in cache
    if (_cache.ContainsKey(cacheKey)) {
      // Check if file was modified since it was cached
      var configPath = GetConfigPath();
      string filePath = Path.Combine(configPath, $"{path}.json");
      bool fileExists = File.Exists(filePath);

      if (fileExists) {
        var fileLastWrite = File.GetLastWriteTimeUtc(filePath);
        var cacheTimestamp = _cacheTimestamps[cacheKey];

        // If file was modified after cache, reload
        if (fileLastWrite > cacheTimestamp) {
          return Load<T>(path);
        }
      }

      // Return from cache if still valid
      if (_cache[cacheKey] is T cachedData) {
        return cachedData;
      }

      // If cache exists but value is null/invalid and no file exists, return default
      if (!fileExists) {
        return default;
      }
    }

    // If not in cache, load from file
    return Load<T>(path);
  }

  /// <summary>
  /// Gets from cache/file or creates and saves data if it doesn't exist
  /// </summary>
  /// <typeparam name="T">Type of data to get or create</typeparam>
  /// <param name="path">File name/path (without extension)</param>
  /// <param name="factory">Factory function to create the data if it doesn't exist</param>
  /// <returns>Existing cached/loaded data or newly created and saved data</returns>
  public T GetOrCreate<T>(string path, Func<T> factory) {
    // First try to get existing data (from cache or file)
    var existingData = Get<T>(path);

    // If data exists and is not null/default, return it
    if (existingData != null && !existingData.Equals(default(T))) {
      return existingData;
    }

    // Create new data and save it
    var newData = factory();
    Save(path, newData);
    return newData;
  }

  /// <summary>
  /// Gets from cache/file or creates and saves data using default constructor if it doesn't exist
  /// </summary>
  /// <typeparam name="T">Type of data to get or create (must have parameterless constructor)</typeparam>
  /// <param name="path">File name/path (without extension)</param>
  /// <returns>Existing cached/loaded data or newly created and saved data</returns>
  public T GetOrCreate<T>(string path) where T : new() {
    return GetOrCreate(path, () => new T());
  }

  /// <summary>
  /// Checks if a data file or directory exists
  /// </summary>
  /// <param name="path">File name/path (without extension) or directory path</param>
  /// <returns>True if file or directory exists</returns>
  public bool Has(string path) {
    var configPath = GetConfigPath();
    string filePath = Path.Combine(configPath, $"{path}.json");
    string directoryPath = Path.Combine(configPath, path);
    return File.Exists(filePath) || Directory.Exists(directoryPath);
  }

  /// <summary>
  /// Deletes a data file
  /// </summary>
  /// <param name="path">File name/path (without extension)</param>
  /// <returns>True if successfully deleted</returns>
  public bool Delete(string path) {
    var configPath = GetConfigPath();
    string filePath = Path.Combine(configPath, $"{path}.json");

    try {
      if (File.Exists(filePath)) {
        File.Delete(filePath);        // Remove from cache after successful deletion
        string cacheKey = GetCacheKey(path);
        _cache.Remove(cacheKey);
        _cacheTimestamps.Remove(cacheKey);

        return true;
      }
    } catch (Exception ex) {
      Log.Error($"An error occurred while deleting data: {ex.Message}");
    }

    return false;
  }
  /// <summary>
  /// Gets all file paths in a folder
  /// </summary>
  /// <param name="folderPath">Folder path relative to database root</param>
  /// <param name="includeSubdirectories">Whether to include files in subdirectories</param>
  /// <returns>Array of file paths (without .json extension) relative to database root</returns>
  public string[] GetFilesInFolder(string folderPath = "", bool includeSubdirectories = false) {
    var configPath = GetConfigPath();
    string fullFolderPath = string.IsNullOrEmpty(folderPath)
      ? configPath
      : Path.Combine(configPath, folderPath);

    try {
      if (!Directory.Exists(fullFolderPath)) {
        return [];
      }

      var searchOption = includeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
      var files = Directory.GetFiles(fullFolderPath, "*.json", searchOption);

      // Convert to relative paths and remove .json extension
      var relativePaths = files.Select(file => {
        var relativePath = Path.GetRelativePath(configPath, file);
        // Remove .json extension
        if (relativePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) {
          relativePath = relativePath[..^5];
        }
        return relativePath;
      }).ToArray();

      return relativePaths;
    } catch (Exception ex) {
      Log.Error($"An error occurred while getting files in folder: {ex.Message}");
      return [];
    }
  }

  /// <summary>
  /// Gets all directory paths in a folder
  /// </summary>
  /// <param name="folderPath">Folder path relative to database root</param>
  /// <param name="includeSubdirectories">Whether to include subdirectories recursively</param>
  /// <returns>Array of directory paths relative to database root</returns>
  public string[] GetDirectoriesInFolder(string folderPath = "", bool includeSubdirectories = false) {
    var configPath = GetConfigPath();
    string fullFolderPath = string.IsNullOrEmpty(folderPath)
      ? configPath
      : Path.Combine(configPath, folderPath);

    try {
      if (!Directory.Exists(fullFolderPath)) {
        return [];
      }

      var searchOption = includeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
      var directories = Directory.GetDirectories(fullFolderPath, "*", searchOption);

      // Convert to relative paths
      var relativePaths = directories.Select(dir =>
        Path.GetRelativePath(configPath, dir)
      ).ToArray();

      return relativePaths;
    } catch (Exception ex) {
      Log.Error($"An error occurred while getting directories in folder: {ex.Message}");
      return [];
    }
  }

  /// <summary>
  /// Clears the cache for all data or specific path
  /// </summary>
  /// <param name="path">Optional specific path to clear, if null clears all cache for this database</param>
  public void ClearCache(string path = null) {
    if (path != null) {
      // Clear specific cache entry
      string cacheKey = GetCacheKey(path);
      _cache.Remove(cacheKey);
      _cacheTimestamps.Remove(cacheKey);
    } else {
      // Clear all cache entries for this database
      var prefix = $"{_databaseName}:";

      var keysToRemove = new List<string>();
      foreach (var key in _cache.Keys) {
        if (key.StartsWith(prefix)) {
          keysToRemove.Add(key);
        }
      }

      foreach (var key in keysToRemove) {
        _cache.Remove(key);
        _cacheTimestamps.Remove(key);
      }
    }
  }
  /// <summary>
  /// Saves all cached data for this database to their respective files
  /// </summary>
  public void SaveAll() {
    var prefix = $"{_databaseName}:";
    var configPath = GetConfigPath();

    foreach (var kvp in _cache) {
      if (kvp.Key.StartsWith(prefix)) {
        try {
          // Extract the path from the cache key
          string path = kvp.Key[prefix.Length..];
          string filePath = Path.Combine(configPath, $"{path}.json"); Directory.CreateDirectory(Path.GetDirectoryName(filePath));

          string jsonData = JsonSerializer.Serialize(kvp.Value, _jsonOptions);
          File.WriteAllText(filePath, jsonData);

          // Update cache timestamp after successful save
          _cacheTimestamps[kvp.Key] = DateTime.Now;
        } catch (Exception ex) {
          Log.Error($"An error occurred while saving cached data for key '{kvp.Key}': {ex.Message}");
        }
      }
    }
  }

  /// <summary>
  /// Creates a backup of all database files by compressing them into a ZIP archive
  /// </summary>
  /// <param name="backupLocation">Optional custom backup location. If null, saves to the BepInEx config directory</param>
  /// <param name="saveName">Optional save name to include in the backup filename</param>
  /// <returns>Task that returns the path to the created backup file, or null if backup failed</returns>
  public async Task<string> CreateBackup(string backupLocation = null, string saveName = null) {
    return await Task.Run(() => {
      try {
        var configPath = GetConfigPath();

        // Check if the database folder exists and has files
        if (!Directory.Exists(configPath)) {
          Log.Warning($"Database folder '{configPath}' does not exist. No backup created.");
          return null;
        }

        var files = Directory.GetFiles(configPath, "*", SearchOption.AllDirectories);
        if (files.Length == 0) {
          Log.Warning($"Database folder '{configPath}' is empty. No backup created.");
          return null;
        }

        // Generate backup filename with timestamp and optional save name (sanitized)
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string safeSaveName = null;
        if (!string.IsNullOrWhiteSpace(saveName)) {
          // Replace invalid filename chars with underscore and collapse runs
          var parts = saveName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries);
          safeSaveName = string.Join("_", parts);
        }

        var backupFileName = safeSaveName == null
          ? $"{_databaseName}_backup_{timestamp}.zip"
          : $"{_databaseName}_backup_{safeSaveName}_{timestamp}.zip";

        // Determine backup location
        var backupPath = backupLocation ?? BepInEx.Paths.ConfigPath;
        var backupFolderPath = Path.Combine(backupPath, $"{_databaseName} Backups");
        var fullBackupPath = Path.Combine(backupFolderPath, backupFileName);

        // Create backup directory if it doesn't exist
        Directory.CreateDirectory(backupFolderPath);

        // Clean up old backups before creating new one
        CleanupOldBackups(backupFolderPath, _maxBackups);

        // Save all cached data before creating backup
        SaveAll();

        // Create ZIP archive
        using (var archive = ZipFile.Open(fullBackupPath, ZipArchiveMode.Create)) {
          foreach (var file in files) {
            // Calculate relative path within the database folder
            var relativePath = Path.GetRelativePath(configPath, file);

            // Add file to archive
            archive.CreateEntryFromFile(file, relativePath);
          }
        }

        Log.Message($"Database backup created successfully for '{_databaseName}'");
        return fullBackupPath;
      } catch (Exception ex) {
        Log.Error($"Failed to create database backup: {ex.Message}");
        return null;
      }
    });
  }

  /// <summary>
  /// Restores database from a backup ZIP file
  /// </summary>
  /// <param name="backupFilePath">Path to the backup ZIP file</param>
  /// <param name="clearExisting">If true, clears existing data before restoring</param>
  /// <returns>Task that returns true if restoration was successful</returns>
  public async Task<bool> RestoreFromBackup(string backupFilePath, bool clearExisting = false) {
    return await Task.Run(() => {
      try {
        if (!File.Exists(backupFilePath)) {
          Log.Error($"Backup file not found: {backupFilePath}");
          return false;
        }

        var configPath = GetConfigPath();

        // Clear existing data if requested
        if (clearExisting && Directory.Exists(configPath)) {
          Directory.Delete(configPath, true);
          ClearCache(); // Clear memory cache as well
        }

        // Create config directory if it doesn't exist
        Directory.CreateDirectory(configPath);

        // Extract backup
        using (var archive = ZipFile.OpenRead(backupFilePath)) {
          foreach (var entry in archive.Entries) {
            // Skip directories
            if (string.IsNullOrEmpty(entry.Name)) continue;

            var destinationPath = Path.Combine(configPath, entry.FullName);

            // Create directory if needed
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

            // Extract file
            entry.ExtractToFile(destinationPath, true);
          }
        }

        // Clear cache to force reload of restored data
        ClearCache();

        Log.Message($"Database restored successfully from: {backupFilePath}");
        return true;
      } catch (Exception ex) {
        Log.Error($"Failed to restore database from backup: {ex.Message}");
        return false;
      }
    });
  }

  /// <summary>
  /// Cleans up old backup files, keeping only the specified number of most recent backups
  /// </summary>
  /// <param name="backupPath">Directory containing backup files</param>
  /// <param name="maxBackups">Maximum number of backups to keep (default: 50)</param>
  private void CleanupOldBackups(string backupPath, int maxBackups = 50) {
    try {
      if (!Directory.Exists(backupPath)) {
        return; // Nothing to clean up
      }

      // Get all backup files for this database
      var backupPattern = $"{_databaseName}_backup_*.zip";
      var backupFiles = Directory.GetFiles(backupPath, backupPattern, SearchOption.TopDirectoryOnly);

      // If we have fewer than or equal to max backups, nothing to clean
      if (backupFiles.Length <= maxBackups) {
        return;
      }

      // Sort files by creation time (oldest first)
      var sortedFiles = backupFiles
        .Select(file => new FileInfo(file))
        .OrderBy(fileInfo => fileInfo.CreationTime)
        .ToArray();

      // Calculate how many files to delete so we end up with at most `maxBackups` files
      int filesToDelete = sortedFiles.Length - maxBackups;

      // Delete the oldest files
      for (int i = 0; i < filesToDelete && i < sortedFiles.Length; i++) {
        try {
          File.Delete(sortedFiles[i].FullName);
          Log.Message($"Deleted old backup: {sortedFiles[i].Name}");
        } catch (Exception ex) {
          Log.Warning($"Failed to delete old backup {sortedFiles[i].Name}: {ex.Message}");
        }
      }

      Log.Message($"Cleanup completed. Kept {Math.Min(maxBackups, sortedFiles.Length - filesToDelete)} backup(s).");
    } catch (Exception ex) {
      Log.Warning($"Error during backup cleanup: {ex.Message}");
    }
  }
}
