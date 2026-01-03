using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ScarletCore.Utils;
using System.Linq.Expressions;

namespace ScarletCore.Data;

/// <summary>
/// Static shared database accessible across all assemblies.
/// Allows mods to share data without direct references.
/// </summary>
public static class SharedDatabase {
  private static readonly Database _database = new("ScarletCore_SharedData");
  private static readonly object _lock = new();

  /// <summary>
  /// Gets or sets the maximum number of backups to keep (default: 50)
  /// </summary>
  public static int MaxBackups {
    get => _database.MaxBackups;
    set => _database.MaxBackups = value;
  }

  /// <summary>
  /// Saves data to the shared database
  /// </summary>
  /// <typeparam name="T">Type of data to save</typeparam>
  /// <param name="databaseName">Name of the database/namespace (e.g., "MyMod")</param>
  /// <param name="key">Unique identifier for this data</param>
  /// <param name="data">Data to save</param>
  public static void Set<T>(string databaseName, string key, T data) {
    if (string.IsNullOrWhiteSpace(databaseName))
      throw new ArgumentException("Database name cannot be null or empty", nameof(databaseName));

    if (string.IsNullOrWhiteSpace(key))
      throw new ArgumentException("Key cannot be null or empty", nameof(key));

    lock (_lock) {
      var fullKey = $"{databaseName}/{key}";
      _database.Set(fullKey, data);
    }
  }

  /// <summary>
  /// Retrieves data from the shared database
  /// </summary>
  /// <typeparam name="T">Type of data to retrieve</typeparam>
  /// <param name="databaseName">Name of the database/namespace</param>
  /// <param name="key">Key of the data to retrieve</param>
  /// <returns>The data if found, otherwise default(T)</returns>
  public static T Get<T>(string databaseName, string key) {
    if (string.IsNullOrWhiteSpace(databaseName))
      throw new ArgumentException("Database name cannot be null or empty", nameof(databaseName));

    if (string.IsNullOrWhiteSpace(key))
      throw new ArgumentException("Key cannot be null or empty", nameof(key));

    lock (_lock) {
      var fullKey = $"{databaseName}/{key}";
      return _database.Get<T>(fullKey);
    }
  }

  /// <summary>
  /// Gets data or creates it using the factory if it doesn't exist
  /// </summary>
  /// <typeparam name="T">Type of data</typeparam>
  /// <param name="databaseName">Name of the database/namespace</param>
  /// <param name="key">Key of the data</param>
  /// <param name="factory">Factory function to create default value</param>
  /// <returns>Existing or newly created data</returns>
  public static T GetOrCreate<T>(string databaseName, string key, Func<T> factory) {
    if (string.IsNullOrWhiteSpace(databaseName))
      throw new ArgumentException("Database name cannot be null or empty", nameof(databaseName));

    if (string.IsNullOrWhiteSpace(key))
      throw new ArgumentException("Key cannot be null or empty", nameof(key));

    lock (_lock) {
      var fullKey = $"{databaseName}/{key}";
      return _database.GetOrCreate(fullKey, factory);
    }
  }

  /// <summary>
  /// Gets data or creates it using the default constructor if it doesn't exist
  /// </summary>
  public static T GetOrCreate<T>(string databaseName, string key) where T : new() {
    return GetOrCreate(databaseName, key, () => new T());
  }

  /// <summary>
  /// Checks if a key exists in the shared database
  /// </summary>
  /// <param name="databaseName">Name of the database/namespace</param>
  /// <param name="key">Key to check</param>
  /// <returns>True if the key exists</returns>
  public static bool Has(string databaseName, string key) {
    if (string.IsNullOrWhiteSpace(databaseName) || string.IsNullOrWhiteSpace(key))
      return false;

    lock (_lock) {
      var fullKey = $"{databaseName}/{key}";
      return _database.Has(fullKey);
    }
  }

  /// <summary>
  /// Deletes data from the shared database
  /// </summary>
  /// <param name="databaseName">Name of the database/namespace</param>
  /// <param name="key">Key of the data to delete</param>
  /// <returns>True if successfully deleted</returns>
  public static bool Delete(string databaseName, string key) {
    if (string.IsNullOrWhiteSpace(databaseName) || string.IsNullOrWhiteSpace(key))
      return false;

    lock (_lock) {
      var fullKey = $"{databaseName}/{key}";
      return _database.Delete(fullKey);
    }
  }

  /// <summary>
  /// Gets all keys for a specific database/namespace
  /// </summary>
  /// <param name="databaseName">Name of the database/namespace</param>
  /// <returns>Array of all keys (without the database prefix)</returns>
  public static string[] GetAllKeys(string databaseName) {
    if (string.IsNullOrWhiteSpace(databaseName))
      return [];

    lock (_lock) {
      var prefix = $"{databaseName}/";
      return [.. _database.GetKeysByPrefix(prefix).Select(k => k[prefix.Length..])];
    }
  }

  /// <summary>
  /// Gets all keys that start with the specified prefix within a database
  /// </summary>
  /// <param name="databaseName">Name of the database/namespace</param>
  /// <param name="keyPrefix">Prefix to filter keys</param>
  /// <returns>Array of matching keys (without the database prefix)</returns>
  public static string[] GetKeysByPrefix(string databaseName, string keyPrefix) {
    if (string.IsNullOrWhiteSpace(databaseName))
      return [];

    lock (_lock) {
      var fullPrefix = string.IsNullOrEmpty(keyPrefix)
        ? $"{databaseName}/"
        : $"{databaseName}/{keyPrefix}";

      var dbPrefix = $"{databaseName}/";
      return [.. _database.GetKeysByPrefix(fullPrefix).Select(k => k[dbPrefix.Length..])];
    }
  }

  /// <summary>
  /// Queries the database using a predicate to filter entries by key
  /// </summary>
  /// <typeparam name="T">Type of data to retrieve</typeparam>
  /// <param name="databaseName">Name of the database/namespace</param>
  /// <param name="keyPredicate">Predicate to filter keys (without database prefix)</param>
  /// <returns>List of matching data entries</returns>
  public static List<T> Query<T>(string databaseName, Expression<Func<string, bool>> keyPredicate) {
    if (string.IsNullOrWhiteSpace(databaseName))
      return [];

    lock (_lock) {
      var dbPrefix = $"{databaseName}/";

      // Create a new predicate that includes the database prefix check
      var param = Expression.Parameter(typeof(string), "fullKey");
      var startsWithCall = Expression.Call(
        param,
        typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)]),
        Expression.Constant(dbPrefix)
      );

      // Remove prefix for the user's predicate
      var substringCall = Expression.Call(
        param,
        typeof(string).GetMethod(nameof(string.Substring), [typeof(int)]),
        Expression.Constant(dbPrefix.Length)
      );

      var userPredicateBody = Expression.Invoke(keyPredicate, substringCall);
      var combinedPredicate = Expression.AndAlso(startsWithCall, userPredicateBody);
      var lambda = Expression.Lambda<Func<string, bool>>(combinedPredicate, param);

      return _database.Query<T>(lambda);
    }
  }

  /// <summary>
  /// Queries the database and returns both keys and values
  /// </summary>
  /// <typeparam name="T">Type of data to retrieve</typeparam>
  /// <param name="databaseName">Name of the database/namespace</param>
  /// <param name="keyPredicate">Predicate to filter keys (without database prefix)</param>
  /// <returns>Dictionary of key-value pairs (keys without database prefix)</returns>
  public static Dictionary<string, T> QueryWithKeys<T>(string databaseName, Expression<Func<string, bool>> keyPredicate) {
    if (string.IsNullOrWhiteSpace(databaseName))
      return [];

    lock (_lock) {
      var dbPrefix = $"{databaseName}/";

      // Create a new predicate that includes the database prefix check
      var param = Expression.Parameter(typeof(string), "fullKey");
      var startsWithCall = Expression.Call(
        param,
        typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)]),
        Expression.Constant(dbPrefix)
      );

      // Remove prefix for the user's predicate
      var substringCall = Expression.Call(
        param,
        typeof(string).GetMethod(nameof(string.Substring), [typeof(int)]),
        Expression.Constant(dbPrefix.Length)
      );

      var userPredicateBody = Expression.Invoke(keyPredicate, substringCall);
      var combinedPredicate = Expression.AndAlso(startsWithCall, userPredicateBody);
      var lambda = Expression.Lambda<Func<string, bool>>(combinedPredicate, param);

      var allData = _database.QueryWithKeys<T>(lambda);

      // Remove database prefix from keys
      return allData.ToDictionary(
        kvp => kvp.Key[dbPrefix.Length..],
        kvp => kvp.Value
      );
    }
  }

  /// <summary>
  /// Gets all data entries of a specific type for a database
  /// </summary>
  /// <typeparam name="T">Type of data to retrieve</typeparam>
  /// <param name="databaseName">Name of the database/namespace</param>
  /// <returns>List of all data entries</returns>
  public static List<T> GetAll<T>(string databaseName) {
    if (string.IsNullOrWhiteSpace(databaseName))
      return [];

    lock (_lock) {
      var dbPrefix = $"{databaseName}/";
      var allData = _database.QueryWithKeys<T>(k => k.StartsWith(dbPrefix));
      return [.. allData.Values];
    }
  }

  /// <summary>
  /// Gets all data entries with their keys for a database
  /// </summary>
  /// <typeparam name="T">Type of data to retrieve</typeparam>
  /// <param name="databaseName">Name of the database/namespace</param>
  /// <returns>Dictionary of key-value pairs (keys without database prefix)</returns>
  public static Dictionary<string, T> GetAllWithKeys<T>(string databaseName) {
    return GetAllByPrefix<T>(databaseName, "");
  }

  /// <summary>
  /// Gets all data entries for a specific database that match the prefix
  /// </summary>
  /// <typeparam name="T">Type of data to retrieve</typeparam>
  /// <param name="databaseName">Name of the database/namespace</param>
  /// <param name="keyPrefix">Prefix to filter keys (optional)</param>
  /// <returns>Dictionary of key-value pairs (keys without database prefix)</returns>
  public static Dictionary<string, T> GetAllByPrefix<T>(string databaseName, string keyPrefix = "") {
    if (string.IsNullOrWhiteSpace(databaseName))
      return [];

    lock (_lock) {
      var fullPrefix = string.IsNullOrEmpty(keyPrefix)
        ? $"{databaseName}/"
        : $"{databaseName}/{keyPrefix}";

      var dbPrefix = $"{databaseName}/";
      var allData = _database.GetAllByPrefix<T>(fullPrefix);

      // Remove database prefix from keys
      return allData.ToDictionary(
        kvp => kvp.Key[dbPrefix.Length..],
        kvp => kvp.Value
      );
    }
  }

  /// <summary>
  /// Clears all data for a specific database/namespace
  /// </summary>
  /// <param name="databaseName">Name of the database/namespace to clear</param>
  /// <returns>Number of entries deleted</returns>
  public static int ClearDatabase(string databaseName) {
    if (string.IsNullOrWhiteSpace(databaseName))
      return 0;

    lock (_lock) {
      var keys = GetAllKeys(databaseName);
      int deleted = 0;

      foreach (var key in keys) {
        if (Delete(databaseName, key))
          deleted++;
      }

      Log.Message($"Cleared {deleted} entries from shared database '{databaseName}'");
      return deleted;
    }
  }

  /// <summary>
  /// Gets the count of entries for a specific database/namespace
  /// </summary>
  /// <param name="databaseName">Name of the database/namespace</param>
  /// <returns>Number of entries</returns>
  public static int Count(string databaseName) {
    if (string.IsNullOrWhiteSpace(databaseName))
      return 0;

    lock (_lock) {
      return GetAllKeys(databaseName).Length;
    }
  }

  /// <summary>
  /// Gets the total count of all entries across all databases
  /// </summary>
  public static int CountAll() {
    lock (_lock) {
      return _database.Count();
    }
  }

  /// <summary>
  /// Gets all database names currently in use
  /// </summary>
  /// <returns>Array of unique database names</returns>
  public static string[] GetAllDatabaseNames() {
    lock (_lock) {
      return [.. _database.GetAllKeys()
        .Select(k => k.Split('/')[0])
        .Distinct()];
    }
  }

  /// <summary>
  /// Performs a checkpoint to ensure all data is written to disk
  /// </summary>
  public static void Checkpoint() {
    lock (_lock) {
      _database.Checkpoint();
    }
  }

  /// <summary>
  /// Enables automatic backups on server save events
  /// </summary>
  /// <param name="backupLocation">Optional custom backup location</param>
  public static void EnableAutoBackup(string backupLocation = null) {
    lock (_lock) {
      _database.EnableAutoBackup(backupLocation);
    }
  }

  /// <summary>
  /// Disables automatic backups
  /// </summary>
  public static void DisableAutoBackup() {
    lock (_lock) {
      _database.DisableAutoBackup();
    }
  }

  /// <summary>
  /// Creates a backup of the shared database
  /// </summary>
  /// <param name="backupLocation">Optional custom backup location</param>
  /// <param name="saveName">Optional save name to include in filename</param>
  /// <returns>Path to the backup file, or null if failed</returns>
  public static async Task<string> CreateBackup(string backupLocation = null, string saveName = null) {
    // Don't lock here as backup is async
    return await _database.CreateBackup(backupLocation, saveName);
  }

  /// <summary>
  /// Restores the shared database from a backup file
  /// </summary>
  /// <param name="backupFilePath">Path to the backup file</param>
  /// <returns>True if successful</returns>
  public static async Task<bool> RestoreFromBackup(string backupFilePath) {
    // Don't lock here as restore is async
    return await _database.RestoreFromBackup(backupFilePath);
  }

  /// <summary>
  /// Internal cleanup method - should only be called when shutting down
  /// </summary>
  internal static void Shutdown() {
    lock (_lock) {
      _database.Dispose();
    }
  }
}