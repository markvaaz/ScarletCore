using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LiteDB;
using ScarletCore.Utils;
using ScarletCore.Events;
using System.Linq.Expressions;

namespace ScarletCore.Data;

/// <summary>
/// Generic database service using LiteDB for persistent storage.
/// Provides simple key-value storage with strong typing.
/// </summary>
public class Database : IDisposable {
  private LiteDatabase _db;
  private readonly string _pluginGuid;
  private ILiteCollection<DataEntry> _collection;

  private bool _autoBackupEnabled;
  private string _autoBackupLocation;
  private int _maxBackups = 50;

  // Cache for reusable BsonMapper
  private static readonly BsonMapper SharedMapper = BsonMapper.Global;

  /// <summary>
  /// Gets or sets the maximum number of backups to keep (default: 50)
  /// </summary>
  public int MaxBackups {
    get => _maxBackups;
    set => _maxBackups = value > 0 ? value : 50;
  }

  /// <summary>
  /// Initializes a new database instance for the specified plugin
  /// </summary>
  /// <param name="pluginGuid">Unique identifier for this plugin's database</param>
  public Database(string pluginGuid) {
    if (string.IsNullOrWhiteSpace(pluginGuid))
      throw new ArgumentException("Plugin GUID cannot be null or empty", nameof(pluginGuid));

    _pluginGuid = pluginGuid;
    InitializeDatabase();
  }

  /// <summary>
  /// Initializes or reinitializes the database connection
  /// </summary>
  private void InitializeDatabase() {
    var dbPath = GetDatabasePath();
    var dirPath = Path.GetDirectoryName(dbPath);

    if (!Directory.Exists(dirPath))
      Directory.CreateDirectory(dirPath);

    var connectionString = new ConnectionString {
      Filename = dbPath,
      Connection = ConnectionType.Shared
    };

    _db = new LiteDatabase(connectionString);
    _collection = _db.GetCollection<DataEntry>("data");

    // Create index on Id to improve query performance
    _collection.EnsureIndex(x => x.Id);
  }

  /// <summary>
  /// Internal class to store data entries
  /// </summary>
  private class DataEntry {
    public string Id { get; set; }
    public BsonValue Data { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
  }

  /// <summary>
  /// Gets the full path to the database file
  /// </summary>
  private string GetDatabasePath() {
    var configPath = Path.Combine(BepInEx.Paths.ConfigPath, _pluginGuid);
    return Path.Combine(configPath, $"{_pluginGuid}.db");
  }

  /// <summary>
  /// Detects circular references in an object graph
  /// </summary>
  private static string DetectCircularReference<T>(T data) {
    if (data == null) return null;

    var type = typeof(T);

    // Skip primitive types, strings, and value types - early exit
    if (type.IsPrimitive || type == typeof(string) || type.IsValueType)
      return null;

    try {
      var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
      return DetectCircularReferenceRecursive(data, visited, type.Name);
    } catch {
      return null;
    }
  }

  private static string DetectCircularReferenceRecursive(object obj, HashSet<object> visited, string currentPath) {
    if (obj == null) return null;

    var type = obj.GetType();

    // Skip primitive types, strings, and value types
    if (type.IsPrimitive || type == typeof(string) || type.IsValueType)
      return null;

    // Skip collections (handled differently)
    if (obj is System.Collections.IEnumerable && type != typeof(string))
      return null;

    // Check circular reference
    if (!visited.Add(obj))
      return currentPath;

    try {
      // Check properties
      var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

      foreach (var prop in properties) {
        if (!prop.CanRead) continue;

        // Skip ignored properties
        if (ShouldIgnoreMember(prop))
          continue;

        try {
          var value = prop.GetValue(obj);
          if (value == null) continue;

          var result = DetectCircularReferenceRecursive(value, visited, $"{currentPath}.{prop.Name}");
          if (result != null) return result;
        } catch {
          continue;
        }
      }

      // Check fields
      var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

      foreach (var field in fields) {
        if (ShouldIgnoreMember(field))
          continue;

        try {
          var value = field.GetValue(obj);
          if (value == null) continue;

          var result = DetectCircularReferenceRecursive(value, visited, $"{currentPath}.{field.Name}");
          if (result != null) return result;
        } catch {
          continue;
        }
      }
    } finally {
      visited.Remove(obj);
    }

    return null;
  }

  /// <summary>
  /// Checks if a member should be ignored during serialization
  /// </summary>
  private static bool ShouldIgnoreMember(MemberInfo member) {
    try {
      var attrs = member.GetCustomAttributes(true);
      if (attrs == null || attrs.Length == 0) return false;

      for (int i = 0; i < attrs.Length; i++) {
        var name = attrs[i].GetType().Name;
        if (name == "BsonIgnoreAttribute" || name == "JsonIgnoreAttribute" || name == "JsonIgnore")
          return true;
      }
    } catch { }

    return false;
  }

  /// <summary>
  /// Helper class for reference equality comparison
  /// </summary>
  private class ReferenceEqualityComparer : IEqualityComparer<object> {
    public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();

    public new bool Equals(object x, object y) {
      return ReferenceEquals(x, y);
    }

    public int GetHashCode(object obj) {
      return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
  }

  /// <summary>
  /// Saves data to the database with the specified key
  /// </summary>
  /// <typeparam name="T">Type of data to save</typeparam>
  /// <param name="key">Unique identifier for this data</param>
  /// <param name="data">Data to save</param>
  public void Set<T>(string key, T data) {
    if (string.IsNullOrWhiteSpace(key))
      throw new ArgumentException("Key cannot be null or empty", nameof(key));

    // Check circular references
    var circularPath = DetectCircularReference(data);
    if (circularPath != null) {
      Log.Error($"Cannot save data with key '{key}': Circular reference detected at path '{circularPath}'");
      throw new InvalidOperationException($"Circular reference detected at: {circularPath}");
    }

    try {
      var bsonData = SharedMapper.Serialize(data);
      var now = DateTime.Now;

      // Using FindById is faster than complex queries
      var existing = _collection.FindById(key);

      var entry = new DataEntry {
        Id = key,
        Data = bsonData,
        CreatedAt = existing?.CreatedAt ?? now,
        UpdatedAt = now
      };

      _collection.Upsert(entry);
    } catch (LiteException ex) when (ex.Message.Contains("circular") || ex.Message.Contains("reference")) {
      Log.Error($"Cannot save data with key '{key}': Circular reference detected during serialization");
      throw new InvalidOperationException("Circular reference detected during serialization", ex);
    } catch (Exception ex) {
      Log.Error($"Failed to save data with key '{key}': {ex.Message}");
      throw;
    }
  }

  /// <summary>
  /// Retrieves data from the database by key
  /// </summary>
  /// <typeparam name="T">Type of data to retrieve</typeparam>
  /// <param name="key">Key of the data to retrieve</param>
  /// <returns>The data if found, otherwise default(T)</returns>
  public T Get<T>(string key) {
    if (string.IsNullOrWhiteSpace(key))
      throw new ArgumentException("Key cannot be null or empty", nameof(key));

    try {
      var entry = _collection.FindById(key);

      if (entry == null) {
        Log.Warning($"[Database] Key '{key}' not found in database");
        return default;
      }

      if (entry.Data == null || entry.Data.IsNull) {
        Log.Warning($"[Database] Key '{key}' has null data");
        return default;
      }

      return SharedMapper.Deserialize<T>(entry.Data);
    } catch (Exception ex) {
      Log.Error($"Failed to load data with key '{key}': {ex.Message}");
      return default;
    }
  }

  /// <summary>
  /// Gets data or creates it using the factory if it doesn't exist
  /// </summary>
  /// <typeparam name="T">Type of data</typeparam>
  /// <param name="key">Key of the data</param>
  /// <param name="factory">Factory function to create default value</param>
  /// <returns>Existing or newly created data</returns>
  public T GetOrCreate<T>(string key, Func<T> factory) {
    if (string.IsNullOrWhiteSpace(key))
      throw new ArgumentException("Key cannot be null or empty", nameof(key));

    try {
      var entry = _collection.FindById(key);

      if (entry != null && entry.Data != null && !entry.Data.IsNull) {
        return SharedMapper.Deserialize<T>(entry.Data);
      }

      // Key doesn't exist, create and save
      var newData = factory();
      Set(key, newData);
      return newData;
    } catch {
      var newData = factory();
      Set(key, newData);
      return newData;
    }
  }

  /// <summary>
  /// Gets data or creates it using the default constructor if it doesn't exist
  /// </summary>
  public T GetOrCreate<T>(string key) where T : new() {
    return GetOrCreate(key, () => new T());
  }

  /// <summary>
  /// Checks if a key exists in the database
  /// </summary>
  /// <param name="key">Key to check</param>
  /// <returns>True if the key exists</returns>
  public bool Has(string key) {
    if (string.IsNullOrWhiteSpace(key))
      return false;

    try {
      // FindById is faster than Exists with a query
      return _collection.FindById(key) != null;
    } catch {
      return false;
    }
  }

  /// <summary>
  /// Deletes data with the specified key
  /// </summary>
  /// <param name="key">Key of the data to delete</param>
  /// <returns>True if successfully deleted</returns>
  public bool Delete(string key) {
    if (string.IsNullOrWhiteSpace(key))
      return false;

    try {
      return _collection.Delete(key);
    } catch (Exception ex) {
      Log.Error($"Failed to delete data with key '{key}': {ex.Message}");
      return false;
    }
  }

  /// <summary>
  /// Gets all keys in the database
  /// </summary>
  /// <returns>Array of all keys</returns>
  public string[] GetAllKeys() {
    try {
      // Use Query instead of FindAll for more efficient iteration
      var keys = _collection.Query()
        .Select(e => e.Id)
        .ToArray();

      return keys;
    } catch (Exception ex) {
      Log.Error($"Failed to get all keys: {ex.Message}");
      return Array.Empty<string>();
    }
  }

  /// <summary>
  /// Gets all keys that start with the specified prefix
  /// </summary>
  /// <param name="prefix">Prefix to filter keys</param>
  /// <returns>Array of matching keys</returns>
  public string[] GetKeysByPrefix(string prefix) {
    if (string.IsNullOrEmpty(prefix))
      return GetAllKeys();

    try {
      // Using Query is more efficient
      var keys = _collection.Query()
        .Where(x => x.Id.StartsWith(prefix))
        .Select(e => e.Id)
        .ToArray();

      return keys;
    } catch (Exception ex) {
      Log.Error($"Failed to get keys by prefix '{prefix}': {ex.Message}");
      return Array.Empty<string>();
    }
  }

  /// <summary>
  /// Gets all data entries that match the specified prefix
  /// </summary>
  /// <typeparam name="T">Type of data to retrieve</typeparam>
  /// <param name="prefix">Prefix to filter keys</param>
  /// <returns>Dictionary of key-value pairs</returns>
  public Dictionary<string, T> GetAllByPrefix<T>(string prefix) {
    try {
      IEnumerable<DataEntry> entries;

      if (string.IsNullOrEmpty(prefix)) {
        entries = _collection.Query().ToEnumerable();
      } else {
        entries = _collection.Query()
          .Where(x => x.Id.StartsWith(prefix))
          .ToEnumerable();
      }

      var result = new Dictionary<string, T>();

      foreach (var entry in entries) {
        try {
          var data = SharedMapper.Deserialize<T>(entry.Data);
          result[entry.Id] = data;
        } catch (Exception ex) {
          Log.Warning($"Failed to deserialize data for key '{entry.Id}': {ex.Message}");
        }
      }

      return result;
    } catch (Exception ex) {
      Log.Error($"Failed to get all data by prefix '{prefix}': {ex.Message}");
      return new Dictionary<string, T>();
    }
  }

  /// <summary>
  /// Clears all data from the database
  /// </summary>
  public void Clear() {
    try {
      _collection.DeleteAll();
      Log.Message($"Database '{_pluginGuid}' cleared successfully");
    } catch (Exception ex) {
      Log.Error($"Failed to clear database: {ex.Message}");
    }
  }

  /// <summary>
  /// Gets the count of entries in the database
  /// </summary>
  public int Count() {
    try {
      return _collection.Count();
    } catch {
      return 0;
    }
  }

  /// <summary>
  /// Queries the database using a predicate to filter entries by key
  /// </summary>
  /// <typeparam name="T">Type of data to retrieve</typeparam>
  /// <param name="keyPredicate">Predicate to filter keys (e.g., x => x.StartsWith("players/"))</param>
  /// <returns>List of matching data entries</returns>
  public List<T> Query<T>(Expression<Func<string, bool>> keyPredicate) {
    try {
      // Convert string expression to DataEntry
      var param = Expression.Parameter(typeof(DataEntry), "x");
      var idProperty = Expression.Property(param, nameof(DataEntry.Id));
      var body = Expression.Invoke(keyPredicate, idProperty);
      var lambda = Expression.Lambda<Func<DataEntry, bool>>(body, param);

      var entries = _collection.Query()
        .Where(lambda)
        .ToEnumerable();

      var result = new List<T>();

      foreach (var entry in entries) {
        try {
          var data = SharedMapper.Deserialize<T>(entry.Data);
          result.Add(data);
        } catch (Exception ex) {
          Log.Warning($"Failed to deserialize data for key '{entry.Id}': {ex.Message}");
        }
      }

      return result;
    } catch (Exception ex) {
      Log.Error($"Failed to query database: {ex.Message}");
      return new List<T>();
    }
  }

  /// <summary>
  /// Queries the database and returns both keys and values
  /// </summary>
  /// <typeparam name="T">Type of data to retrieve</typeparam>
  /// <param name="keyPredicate">Predicate to filter keys</param>
  /// <returns>Dictionary of key-value pairs</returns>
  public Dictionary<string, T> QueryWithKeys<T>(Expression<Func<string, bool>> keyPredicate) {
    try {
      var param = Expression.Parameter(typeof(DataEntry), "x");
      var idProperty = Expression.Property(param, nameof(DataEntry.Id));
      var body = Expression.Invoke(keyPredicate, idProperty);
      var lambda = Expression.Lambda<Func<DataEntry, bool>>(body, param);

      var entries = _collection.Query()
        .Where(lambda)
        .ToEnumerable();

      var result = new Dictionary<string, T>();

      foreach (var entry in entries) {
        try {
          var data = SharedMapper.Deserialize<T>(entry.Data);
          result[entry.Id] = data;
        } catch (Exception ex) {
          Log.Warning($"Failed to deserialize data for key '{entry.Id}': {ex.Message}");
        }
      }

      return result;
    } catch (Exception ex) {
      Log.Error($"Failed to query database: {ex.Message}");
      return new Dictionary<string, T>();
    }
  }

  /// <summary>
  /// Gets all data entries of a specific type
  /// </summary>
  /// <typeparam name="T">Type of data to retrieve</typeparam>
  /// <returns>List of all data entries</returns>
  public List<T> GetAll<T>() {
    try {
      var entries = _collection.Query().ToEnumerable();
      var result = new List<T>();

      foreach (var entry in entries) {
        try {
          var data = SharedMapper.Deserialize<T>(entry.Data);
          result.Add(data);
        } catch (Exception ex) {
          Log.Warning($"Failed to deserialize data for key '{entry.Id}': {ex.Message}");
        }
      }

      return result;
    } catch (Exception ex) {
      Log.Error($"Failed to get all data: {ex.Message}");
      return new List<T>();
    }
  }

  /// <summary>
  /// Gets all data entries with their keys
  /// </summary>
  /// <typeparam name="T">Type of data to retrieve</typeparam>
  /// <returns>Dictionary of key-value pairs</returns>
  public Dictionary<string, T> GetAllWithKeys<T>() {
    try {
      var entries = _collection.Query().ToEnumerable();
      var result = new Dictionary<string, T>();

      foreach (var entry in entries) {
        try {
          var data = SharedMapper.Deserialize<T>(entry.Data);
          result[entry.Id] = data;
        } catch (Exception ex) {
          Log.Warning($"Failed to deserialize data for key '{entry.Id}': {ex.Message}");
        }
      }

      return result;
    } catch (Exception ex) {
      Log.Error($"Failed to get all data: {ex.Message}");
      return new Dictionary<string, T>();
    }
  }

  /// <summary>
  /// Performs a checkpoint to ensure all data is written to disk
  /// </summary>
  public void Checkpoint() {
    const int maxRetries = 3;
    const int delayMs = 100;

    for (int i = 0; i < maxRetries; i++) {
      try {
        _db.Checkpoint();
        return;
      } catch (IOException ex) when (ex.Message.Contains("being used by another process")) {
        if (i < maxRetries - 1) {
          Log.Warning($"Checkpoint retry {i + 1}/{maxRetries}: File is locked, waiting {delayMs}ms...");
          System.Threading.Thread.Sleep(delayMs);
        } else {
          Log.Error($"Failed to perform checkpoint after {maxRetries} attempts: {ex.Message}");
        }
      } catch (Exception ex) {
        Log.Error($"Failed to perform checkpoint: {ex.Message}");
        return;
      }
    }
  }

  #region Auto-Backup

  /// <summary>
  /// Enables automatic backups on server save events
  /// </summary>
  /// <param name="backupLocation">Optional custom backup location</param>
  public void EnableAutoBackup(string backupLocation = null) {
    if (_autoBackupEnabled) return;

    _autoBackupLocation = backupLocation;
    EventManager.On(ServerEvents.OnSave, AutoBackupHandler);
    _autoBackupEnabled = true;

    Log.Message($"Auto-backup enabled for database '{_pluginGuid}'");
  }

  /// <summary>
  /// Disables automatic backups
  /// </summary>
  public void DisableAutoBackup() {
    if (!_autoBackupEnabled) return;

    try {
      EventManager.Off(ServerEvents.OnSave, AutoBackupHandler);
    } catch { }

    _autoBackupEnabled = false;
    Log.Message($"Auto-backup disabled for database '{_pluginGuid}'");
  }

  [EventPriority(EventPriority.Last)]
  private async void AutoBackupHandler(string saveName) {
    try {
      saveName = saveName?.Replace(".save", "") ?? "auto";
      await CreateBackup(_autoBackupLocation, saveName);
    } catch (Exception ex) {
      Log.Error($"Auto-backup failed for '{_pluginGuid}': {ex.Message}");
    }
  }

  /// <summary>
  /// Creates a backup of the database
  /// </summary>
  /// <param name="backupLocation">Optional custom backup location</param>
  /// <param name="saveName">Optional save name to include in filename</param>
  /// <returns>Path to the backup file, or null if failed</returns>
  public async Task<string> CreateBackup(string backupLocation = null, string saveName = null) {
    return await Task.Run(() => {
      try {
        Checkpoint();

        var dbPath = GetDatabasePath();
        if (!File.Exists(dbPath)) {
          Log.Warning($"Database file does not exist: {dbPath}");
          return null;
        }

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var safeSaveName = string.IsNullOrWhiteSpace(saveName)
          ? null
          : string.Join("_", saveName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));

        var backupFileName = safeSaveName == null
          ? $"{_pluginGuid}_{timestamp}.db"
          : $"{_pluginGuid}_{saveName}_{timestamp}.db";

        var backupDir = Path.Combine(
          backupLocation ?? BepInEx.Paths.ConfigPath,
          $"{_pluginGuid}_Backups"
        );

        if (!Directory.Exists(backupDir))
          Directory.CreateDirectory(backupDir);

        var backupPath = Path.Combine(backupDir, backupFileName);

        const int maxRetries = 3;
        const int delayMs = 100;
        bool copied = false;

        for (int i = 0; i < maxRetries && !copied; i++) {
          try {
            File.Copy(dbPath, backupPath, true);
            copied = true;
          } catch (IOException ex) when (ex.Message.Contains("being used by another process")) {
            if (i < maxRetries - 1) {
              Log.Warning($"Backup copy retry {i + 1}/{maxRetries}: File is locked, waiting {delayMs}ms...");
              System.Threading.Thread.Sleep(delayMs);
            } else {
              throw;
            }
          }
        }

        if (!copied) {
          Log.Error("Failed to copy database file after multiple attempts");
          return null;
        }

        CleanupOldBackups(backupDir);

        return backupPath;
      } catch (Exception ex) {
        Log.Error($"Failed to create backup: {ex.Message}");
        return null;
      }
    });
  }

  /// <summary>
  /// Restores the database from a backup file
  /// </summary>
  /// <param name="backupFilePath">Path to the backup file</param>
  /// <returns>True if successful</returns>
  public async Task<bool> RestoreFromBackup(string backupFilePath) {
    return await Task.Run(() => {
      try {
        if (!File.Exists(backupFilePath)) {
          Log.Error($"Backup file not found: {backupFilePath}");
          return false;
        }

        var dbPath = GetDatabasePath();

        _db.Dispose();

        File.Copy(backupFilePath, dbPath, true);

        Log.Message($"Database restored from: {backupFilePath}");
        Log.Warning("Application restart required for changes to take effect");

        return true;
      } catch (Exception ex) {
        Log.Error($"Failed to restore backup: {ex.Message}");
        return false;
      }
    });
  }

  private void CleanupOldBackups(string backupDir) {
    try {
      var pattern = $"{_pluginGuid}_*.db";
      var files = Directory.GetFiles(backupDir, pattern);

      if (files.Length <= _maxBackups)
        return;

      // Sort by creation time descending
      Array.Sort(files, (a, b) => {
        var timeA = File.GetCreationTime(a);
        var timeB = File.GetCreationTime(b);
        return timeB.CompareTo(timeA);
      });

      for (int i = _maxBackups; i < files.Length; i++) {
        try {
          File.Delete(files[i]);
        } catch (Exception ex) {
          Log.Warning($"Failed to delete old backup: {ex.Message}");
        }
      }
    } catch (Exception ex) {
      Log.Warning($"Backup cleanup failed: {ex.Message}");
    }
  }

  #endregion

  /// <summary>
  /// Cleanup method for when the plugin is unloaded
  /// </summary>
  public void UnregisterAssembly() {
    DisableAutoBackup();
    Dispose();
  }

  /// <summary>
  /// Releases database resources, performs a final checkpoint and disables auto-backup.
  /// Also suppresses finalization for this instance.
  /// </summary>
  public void Dispose() {
    try {
      DisableAutoBackup();
      Checkpoint();
      _db?.Dispose();
    } catch (Exception ex) {
      Log.Warning($"Error during database disposal: {ex.Message}");
    } finally {
      GC.SuppressFinalize(this);
    }
  }
}