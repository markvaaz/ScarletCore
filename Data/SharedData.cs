using System;
using System.Collections.Generic;
using System.Linq;

namespace ScarletCore.Data;

/// <summary>
/// Shared in-memory temporary storage service.
/// Keys are namespaced per-database using the database name.
/// </summary>
public static class SharedData {
  private static readonly Dictionary<string, object> _tempCache = [];

  private static string GetCacheKey(string databaseName, string key) {
    return $"{databaseName}:{key}";
  }


  /// <summary>
  /// Stores a value in the shared cache for a specific database and key.
  /// </summary>
  /// <typeparam name="T">The type of data to store.</typeparam>
  /// <param name="databaseName">The database namespace for the key.</param>
  /// <param name="key">The key to associate with the data.</param>
  /// <param name="data">The data to store.</param>
  public static void Set<T>(string databaseName, string key, T data) {
    string cacheKey = GetCacheKey(databaseName, key);
    _tempCache[cacheKey] = data;
  }


  /// <summary>
  /// Retrieves a value from the shared cache for a specific database and key.
  /// </summary>
  /// <typeparam name="T">The expected type of the data.</typeparam>
  /// <param name="databaseName">The database namespace for the key.</param>
  /// <param name="key">The key associated with the data.</param>
  /// <returns>The cached data if found and of the correct type; otherwise, the default value for T.</returns>
  public static T Get<T>(string databaseName, string key) {
    string cacheKey = GetCacheKey(databaseName, key);
    if (_tempCache.TryGetValue(cacheKey, out object value) && value is T cachedData) {
      return cachedData;
    }
    return default;
  }


  /// <summary>
  /// Retrieves a value from the shared cache or creates and stores it if not present.
  /// </summary>
  /// <typeparam name="T">The type of data to retrieve or create.</typeparam>
  /// <param name="databaseName">The database namespace for the key.</param>
  /// <param name="key">The key associated with the data.</param>
  /// <param name="factory">A function to create the data if it does not exist.</param>
  /// <returns>The cached or newly created data.</returns>
  public static T GetOrCreate<T>(string databaseName, string key, Func<T> factory) {
    string cacheKey = GetCacheKey(databaseName, key);
    if (_tempCache.TryGetValue(cacheKey, out object value) && value is T cachedData) {
      return cachedData;
    }
    var newData = factory();
    _tempCache[cacheKey] = newData;
    return newData;
  }


  /// <summary>
  /// Retrieves a value from the shared cache or creates a new instance if not present.
  /// </summary>
  /// <typeparam name="T">The type of data to retrieve or create. Must have a parameterless constructor.</typeparam>
  /// <param name="databaseName">The database namespace for the key.</param>
  /// <param name="key">The key associated with the data.</param>
  /// <returns>The cached or newly created data.</returns>
  public static T GetOrCreate<T>(string databaseName, string key) where T : new() {
    return GetOrCreate(databaseName, key, () => new T());
  }


  /// <summary>
  /// Determines whether the shared cache contains a value for the specified database and key.
  /// </summary>
  /// <param name="databaseName">The database namespace for the key.</param>
  /// <param name="key">The key to check for existence.</param>
  /// <returns>True if the cache contains the key; otherwise, false.</returns>
  public static bool Has(string databaseName, string key) {
    string cacheKey = GetCacheKey(databaseName, key);
    return _tempCache.ContainsKey(cacheKey);
  }


  /// <summary>
  /// Removes a value from the shared cache for a specific database and key.
  /// </summary>
  /// <param name="databaseName">The database namespace for the key.</param>
  /// <param name="key">The key of the data to remove.</param>
  /// <returns>True if the value was removed; otherwise, false.</returns>
  public static bool Remove(string databaseName, string key) {
    string cacheKey = GetCacheKey(databaseName, key);
    return _tempCache.Remove(cacheKey);
  }


  /// <summary>
  /// Removes all values from the shared cache for a specific database namespace.
  /// </summary>
  /// <param name="databaseName">The database namespace to clear.</param>
  public static void Clear(string databaseName) {
    var prefix = $"{databaseName}:";
    var keysToRemove = _tempCache.Keys.Where(k => k.StartsWith(prefix)).ToList();
    foreach (var k in keysToRemove) {
      _tempCache.Remove(k);
    }
  }

  /// <summary>
  /// Retrieves a list of all keys stored in the shared cache for a specific database namespace.
  /// </summary>
  /// <param name="databaseName">The database namespace to retrieve keys from.</param>
  /// <returns>A list of keys stored for the specified database.</returns>
  public static List<string> GetKeys(string databaseName) {
    var prefix = $"{databaseName}:";
    var keys = new List<string>();
    foreach (var k in _tempCache.Keys) {
      if (k.StartsWith(prefix)) keys.Add(k[prefix.Length..]);
    }
    return keys;
  }
}