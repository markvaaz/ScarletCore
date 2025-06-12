using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ScarletCore.Utils;

namespace ScarletCore.Data;

/// <summary>
/// Generic database service that provides JSON-based persistence for mods.
/// Each instance gets its own folder based on the database name provided in the constructor.
/// </summary>
public class Database {
  // Cache to store data in memory
  private readonly Dictionary<string, object> _cache = [];
  private readonly Dictionary<string, DateTime> _cacheTimestamps = [];
  // Database name (folder name)
  private readonly string _databaseName;

  // Temp storage instance
  private readonly TempStorage _temp;

  /// <summary>
  /// Gets the temporary storage instance for this database
  /// </summary>
  public TempStorage Temp => _temp;

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
  public Database(string pluginGuid) {
    if (string.IsNullOrWhiteSpace(pluginGuid))
      throw new ArgumentException("Database name cannot be null or empty", nameof(pluginGuid));

    _databaseName = pluginGuid;
    _temp = new TempStorage(pluginGuid);
  }
  /// <summary>
  /// Gets the configuration path for this database instance
  /// </summary>
  /// <returns>Path specific to this database</returns>
  private string GetConfigPath() {
    return Path.Combine(BepInEx.Paths.ConfigPath, _databaseName);
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
      string jsonData = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
      File.WriteAllText(filePath, jsonData);

      // Updates cache after successful save
      string cacheKey = GetCacheKey(path);
      _cache[cacheKey] = data;
      _cacheTimestamps[cacheKey] = DateTime.UtcNow;
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
      var deserializedData = JsonSerializer.Deserialize<T>(jsonData, _jsonOptions);

      // Updates cache after successful load
      string cacheKey = GetCacheKey(path);
      _cache[cacheKey] = deserializedData;
      _cacheTimestamps[cacheKey] = DateTime.UtcNow;

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

      if (File.Exists(filePath)) {
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
    }

    // If not in cache, load from file
    return Load<T>(path);
  }

  /// <summary>
  /// Checks if a data file exists
  /// </summary>
  /// <param name="path">File name/path (without extension)</param>
  /// <returns>True if file exists</returns>
  public bool Has(string path) {
    var configPath = GetConfigPath();
    string filePath = Path.Combine(configPath, $"{path}.json");
    return File.Exists(filePath);
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
          string path = kvp.Key.Substring(prefix.Length);
          string filePath = Path.Combine(configPath, $"{path}.json"); Directory.CreateDirectory(Path.GetDirectoryName(filePath));

          string jsonData = JsonSerializer.Serialize(kvp.Value, new JsonSerializerOptions { WriteIndented = true });
          File.WriteAllText(filePath, jsonData);

          // Update cache timestamp after successful save
          _cacheTimestamps[kvp.Key] = DateTime.UtcNow;
        } catch (Exception ex) {
          Log.Error($"An error occurred while saving cached data for key '{kvp.Key}': {ex.Message}");
        }
      }
    }
  }  /// <summary>
     /// Temporary data storage that exists only in memory and is lost on restart
     /// </summary>
  public class TempStorage {
    // Exclusive cache for temporary data
    private readonly Dictionary<string, object> _tempCache = [];
    private readonly string _databaseName;

    internal TempStorage(string databaseName) {
      _databaseName = databaseName;
    }

    /// <summary>
    /// Gets the temporary cache key for a specific key and this database
    /// </summary>
    private string GetTempCacheKey(string key) {
      return $"temp:{_databaseName}:{key}";
    }    /// <summary>
         /// Sets temporary data in memory cache only
         /// </summary>
         /// <typeparam name="T">Type of data to set</typeparam>
         /// <param name="key">Key to store the data</param>
         /// <param name="data">Data to store temporarily</param>
    public void Set<T>(string key, T data) {
      string cacheKey = GetTempCacheKey(key);
      _tempCache[cacheKey] = data;
    }

    /// <summary>
    /// Gets temporary data from memory cache
    /// </summary>
    /// <typeparam name="T">Type of data to get</typeparam>
    /// <param name="key">Key of the data to retrieve</param>
    /// <returns>Cached data or default value if not found</returns>
    public T Get<T>(string key) {
      string cacheKey = GetTempCacheKey(key);

      if (_tempCache.ContainsKey(cacheKey) && _tempCache[cacheKey] is T cachedData) {
        return cachedData;
      }

      return default;
    }

    /// <summary>
    /// Checks if temporary data exists for the given key
    /// </summary>
    /// <param name="key">Key to check</param>
    /// <returns>True if data exists in temporary cache</returns>
    public bool Has(string key) {
      string cacheKey = GetTempCacheKey(key);
      return _tempCache.ContainsKey(cacheKey);
    }

    /// <summary>
    /// Removes temporary data for the given key
    /// </summary>
    /// <param name="key">Key to remove</param>
    /// <returns>True if data was removed</returns>
    public bool Remove(string key) {
      string cacheKey = GetTempCacheKey(key);
      return _tempCache.Remove(cacheKey);
    }

    /// <summary>
    /// Clears all temporary data for this database
    /// </summary>
    public void Clear() {
      var prefix = $"temp:{_databaseName}:";

      var keysToRemove = new List<string>();
      foreach (var key in _tempCache.Keys) {
        if (key.StartsWith(prefix)) {
          keysToRemove.Add(key);
        }
      }

      foreach (var key in keysToRemove) {
        _tempCache.Remove(key);
      }
    }

    /// <summary>
    /// Gets all temporary data keys for this database
    /// </summary>
    /// <returns>List of keys without the database prefix</returns>
    public List<string> GetKeys() {
      var prefix = $"temp:{_databaseName}:";

      var keys = new List<string>();
      foreach (var key in _tempCache.Keys) {
        if (key.StartsWith(prefix)) {
          keys.Add(key.Substring(prefix.Length));
        }
      }

      return keys;
    }
  }

}
