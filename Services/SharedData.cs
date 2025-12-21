using System;
using System.Collections.Generic;
using System.Linq;

namespace ScarletCore.Services {
  /// <summary>
  /// Shared in-memory temporary storage service.
  /// Keys are namespaced per-database using the database name.
  /// </summary>
  public static class SharedData {
    private static readonly Dictionary<string, object> _tempCache = new();

    private static string GetCacheKey(string databaseName, string key) {
      return $"{databaseName}:{key}";
    }

    public static void Set<T>(string databaseName, string key, T data) {
      string cacheKey = GetCacheKey(databaseName, key);
      _tempCache[cacheKey] = data;
    }

    public static T Get<T>(string databaseName, string key) {
      string cacheKey = GetCacheKey(databaseName, key);
      if (_tempCache.TryGetValue(cacheKey, out object value) && value is T cachedData) {
        return cachedData;
      }
      return default;
    }

    public static T GetOrCreate<T>(string databaseName, string key, Func<T> factory) {
      string cacheKey = GetCacheKey(databaseName, key);
      if (_tempCache.TryGetValue(cacheKey, out object value) && value is T cachedData) {
        return cachedData;
      }
      var newData = factory();
      _tempCache[cacheKey] = newData;
      return newData;
    }

    public static T GetOrCreate<T>(string databaseName, string key) where T : new() {
      return GetOrCreate(databaseName, key, () => new T());
    }

    public static bool Has(string databaseName, string key) {
      string cacheKey = GetCacheKey(databaseName, key);
      return _tempCache.ContainsKey(cacheKey);
    }

    public static bool Remove(string databaseName, string key) {
      string cacheKey = GetCacheKey(databaseName, key);
      return _tempCache.Remove(cacheKey);
    }

    public static void Clear(string databaseName) {
      var prefix = $"{databaseName}:";
      var keysToRemove = _tempCache.Keys.Where(k => k.StartsWith(prefix)).ToList();
      foreach (var k in keysToRemove) {
        _tempCache.Remove(k);
      }
    }

    public static List<string> GetKeys(string databaseName) {
      var prefix = $"{databaseName}:";
      var keys = new List<string>();
      foreach (var k in _tempCache.Keys) {
        if (k.StartsWith(prefix)) keys.Add(k[prefix.Length..]);
      }
      return keys;
    }
  }
}
