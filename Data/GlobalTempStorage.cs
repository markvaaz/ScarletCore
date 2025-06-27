using System;
using System.Collections.Generic;
using System.Reflection;

namespace ScarletCore.Data;

/// <summary>
/// Global temporary data storage that persists across mod reloads.
/// This static storage is perfect for reloadable mods that need to maintain data between reloads.
/// Data is automatically namespaced by the calling assembly to prevent conflicts between mods.
/// </summary>
public static class GlobalTempStorage {
  // Global cache for temporary data across all mods
  private static readonly Dictionary<string, object> _globalTempCache = [];
  private static readonly object _lock = new();

  /// <summary>
  /// Gets the temporary cache key for a specific key and calling assembly
  /// </summary>
  private static string GetTempCacheKey(string key) {
    var assemblyName = Assembly.GetCallingAssembly().GetName().Name;
    return $"global_temp:{assemblyName}:{key}";
  }

  /// <summary>
  /// Sets temporary data in global memory cache
  /// Data is automatically namespaced by the calling assembly name
  /// </summary>
  /// <typeparam name="T">Type of data to set</typeparam>
  /// <param name="key">Key to store the data</param>
  /// <param name="data">Data to store temporarily</param>
  public static void Set<T>(string key, T data) {
    string cacheKey = GetTempCacheKey(key);
    lock (_lock) {
      _globalTempCache[cacheKey] = data;
    }
  }

  /// <summary>
  /// Gets temporary data from global memory cache
  /// Data is automatically retrieved from the calling assembly's namespace
  /// </summary>
  /// <typeparam name="T">Type of data to get</typeparam>
  /// <param name="key">Key of the data to retrieve</param>
  /// <returns>Cached data or default value if not found</returns>
  public static T Get<T>(string key) {
    string cacheKey = GetTempCacheKey(key);
    lock (_lock) {
      if (_globalTempCache.ContainsKey(cacheKey) && _globalTempCache[cacheKey] is T cachedData) {
        return cachedData;
      }
    }
    return default;
  }

  /// <summary>
  /// Gets temporary data from global memory cache or creates it if it doesn't exist
  /// Data is automatically namespaced by the calling assembly name
  /// </summary>
  /// <typeparam name="T">Type of data to get or create</typeparam>
  /// <param name="key">Key of the data to retrieve or create</param>
  /// <param name="factory">Factory function to create the data if it doesn't exist</param>
  /// <returns>Existing cached data or newly created data</returns>
  public static T GetOrCreate<T>(string key, Func<T> factory) {
    string cacheKey = GetTempCacheKey(key);
    lock (_lock) {
      if (_globalTempCache.ContainsKey(cacheKey) && _globalTempCache[cacheKey] is T cachedData) {
        return cachedData;
      }

      var newData = factory();
      _globalTempCache[cacheKey] = newData;
      return newData;
    }
  }

  /// <summary>
  /// Gets temporary data from global memory cache or creates it using default constructor if it doesn't exist
  /// Data is automatically namespaced by the calling assembly name
  /// </summary>
  /// <typeparam name="T">Type of data to get or create (must have parameterless constructor)</typeparam>
  /// <param name="key">Key of the data to retrieve or create</param>
  /// <returns>Existing cached data or newly created data</returns>
  public static T GetOrCreate<T>(string key) where T : new() {
    return GetOrCreate(key, () => new T());
  }

  /// <summary>
  /// Checks if temporary data exists for the given key in the calling assembly's namespace
  /// </summary>
  /// <param name="key">Key to check</param>
  /// <returns>True if data exists in temporary cache</returns>
  public static bool Has(string key) {
    string cacheKey = GetTempCacheKey(key);
    lock (_lock) {
      return _globalTempCache.ContainsKey(cacheKey);
    }
  }

  /// <summary>
  /// Removes temporary data for the given key from the calling assembly's namespace
  /// </summary>
  /// <param name="key">Key to remove</param>
  /// <returns>True if data was removed</returns>
  public static bool Remove(string key) {
    string cacheKey = GetTempCacheKey(key);
    lock (_lock) {
      return _globalTempCache.Remove(cacheKey);
    }
  }

  /// <summary>
  /// Clears all temporary data for the calling assembly
  /// </summary>
  public static void Clear() {
    var assemblyName = Assembly.GetCallingAssembly().GetName().Name;
    var prefix = $"global_temp:{assemblyName}:";

    lock (_lock) {
      var keysToRemove = new List<string>();
      foreach (var key in _globalTempCache.Keys) {
        if (key.StartsWith(prefix)) {
          keysToRemove.Add(key);
        }
      }

      foreach (var key in keysToRemove) {
        _globalTempCache.Remove(key);
      }
    }
  }

  /// <summary>
  /// Gets all temporary data keys for the calling assembly
  /// </summary>
  /// <returns>List of keys without the assembly prefix</returns>
  public static List<string> GetKeys() {
    var assemblyName = Assembly.GetCallingAssembly().GetName().Name;
    var prefix = $"global_temp:{assemblyName}:";

    lock (_lock) {
      var keys = new List<string>();
      foreach (var key in _globalTempCache.Keys) {
        if (key.StartsWith(prefix)) {
          keys.Add(key.Substring(prefix.Length));
        }
      }
      return keys;
    }
  }

  /// <summary>
  /// Gets all temporary data keys for a specific assembly (admin/debug function)
  /// </summary>
  /// <param name="assemblyName">Name of the assembly to get keys for</param>
  /// <returns>List of keys without the assembly prefix</returns>
  public static List<string> GetKeysForAssembly(string assemblyName) {
    var prefix = $"global_temp:{assemblyName}:";

    lock (_lock) {
      var keys = new List<string>();
      foreach (var key in _globalTempCache.Keys) {
        if (key.StartsWith(prefix)) {
          keys.Add(key.Substring(prefix.Length));
        }
      }
      return keys;
    }
  }

  /// <summary>
  /// Gets total count of cached items across all assemblies (admin/debug function)
  /// </summary>
  /// <returns>Total number of cached items</returns>
  public static int GetTotalCount() {
    lock (_lock) {
      return _globalTempCache.Count;
    }
  }

  /// <summary>
  /// Gets count of cached items for the calling assembly
  /// </summary>
  /// <returns>Number of cached items for this assembly</returns>
  public static int GetCount() {
    var assemblyName = Assembly.GetCallingAssembly().GetName().Name;
    var prefix = $"global_temp:{assemblyName}:";

    lock (_lock) {
      int count = 0;
      foreach (var key in _globalTempCache.Keys) {
        if (key.StartsWith(prefix)) {
          count++;
        }
      }
      return count;
    }
  }

  /// <summary>
  /// Clears all temporary data for all assemblies (admin/debug function)
  /// Use with caution as this affects all mods
  /// </summary>
  public static void ClearAll() {
    lock (_lock) {
      _globalTempCache.Clear();
    }
  }
}
