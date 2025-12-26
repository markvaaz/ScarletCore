using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using ScarletCore.Utils;
using ScarletCore.Data;
using Stunlock.Core;
using ScarletCore.Events;

namespace ScarletCore.Services;

/// <summary>
/// Service for managing game localization and translations.
/// Provides access to localized text for items, buffs, and other game elements by PrefabGUID.
/// Supports multiple languages loaded from embedded JSON resources.
/// </summary>
public static class LocalizationService {
  /// <summary>
  /// Dictionary mapping localization GUIDs to translated text strings
  /// </summary>
  private static readonly ConcurrentDictionary<string, string> _translations = new();

  /// <summary>
  /// Dictionary mapping PrefabGUID hash values to localization GUIDs
  /// </summary>
  private static readonly ConcurrentDictionary<int, string> _prefabToGuid = new();

  /// <summary>
  /// Custom localization keys created at runtime. Composite key (assembly:key) -> (language -> text)
  /// </summary>
  private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _customKeys = new(StringComparer.OrdinalIgnoreCase);

  /// <summary>
  /// Read-only view of translations for safe external access
  /// </summary>
  public static IReadOnlyDictionary<string, string> Translations => _translations;

  /// <summary>
  /// Read-only view of prefab mappings for safe external access
  /// </summary>
  public static IReadOnlyDictionary<int, string> PrefabMappings => _prefabToGuid;

  /// <summary>
  /// Flag indicating whether the service has been initialized
  /// </summary>
  private static bool _initialized = false;

  /// <summary>
  /// The currently loaded language code
  /// </summary>
  private static string _currentServerLanguage = "english";

  /// <summary>
  /// Mapping of language codes to their corresponding embedded resource file names
  /// </summary>
  private static readonly Dictionary<string, string> _languageFileMapping = new() {
    { "portuguese", "Brazilian" },
    { "english", "English" },
    { "french", "French" },
    { "german", "German" },
    { "hungarian", "Hungarian" },
    { "italian", "Italian" },
    { "japanese", "Japanese" },
    { "korean", "Korean" },
    { "latam", "Latam" },
    { "polish", "Polish" },
    { "russian", "Russian" },
    { "spanish", "Spanish" },
    { "chinese_simplified", "ChineseSimplified" },
    { "chinese_traditional", "ChineseTraditional" },
    { "thai", "Thai" },
    { "turkish", "Turkish" },
    { "ukrainian", "Ukrainian" },
    { "vietnamese", "Vietnamese" }
  };

  /// <summary>
  /// Get all available language codes
  /// </summary>
  public static string[] AvailableServerLanguages => [.. _languageFileMapping.Keys];

  /// <summary>
  /// Get the current loaded language
  /// </summary>
  public static string CurrentServerLanguage => _currentServerLanguage;

  /// <summary>
  /// Initialize the localization service. Loads English by default.
  /// Call this during game initialization to load embedded localization resources.
  /// </summary>
  public static void Initialize() {
    var language = Plugin.Settings.Get<string>("PrefabLocalizationLanguage") ?? "english";

    try {
      LoadPrefabMapping();
      LoadLanguage(language);
      EventManager.On(PlayerEvents.PlayerJoined, CheckLanguageOnJoin);
      _initialized = true;
      Log.Info($"LocalizationService initialized with {_translations.Count} translations and {PrefabMappings.Count} prefab mappings");
    } catch (Exception ex) {
      Log.Error($"Failed to initialize LocalizationService: {ex}");
    }
  }

  private static void CheckLanguageOnJoin(PlayerData player) {
    try {
      if (player == null) return;

      var lang = GetPlayerLanguage(player);
      if (!string.IsNullOrWhiteSpace(lang)) return; // already set

      // Prompt the player to choose a language
      var available = string.Join(", ", AvailableServerLanguages);
      player.SendMessage($"Welcome! Please set your language using ~.setlang <language>~.\nAvailable: {available}");
    } catch (Exception ex) {
      Log.Error($"CheckLanguageOnJoin failed: {ex}");
    }
  }

  /// <summary>
  /// Load a language file from embedded resources.
  /// Replaces all current translations with the new language data.
  /// </summary>
  /// <param name="language">The language code to load (e.g., "english", "portuguese")</param>
  public static void LoadLanguage(string language) {
    try {
      var normalizedLanguage = language.ToLower().Trim();

      if (!_languageFileMapping.TryGetValue(normalizedLanguage, out var fileName)) {
        Log.Warning($"Language not supported: {language}. Available languages: {string.Join(", ", AvailableServerLanguages)}");
        return;
      }

      // Use ScarletCore assembly for loading game localization files
      var assembly = typeof(LocalizationService).Assembly;
      var resourceName = $"ScarletCore.Localization.{fileName}.json";

      using var stream = assembly.GetManifestResourceStream(resourceName);
      if (stream == null) {
        Log.Warning($"Language file not found: {resourceName}");
        return;
      }

      using var reader = new StreamReader(stream);
      var json = reader.ReadToEnd();

      var doc = JsonDocument.Parse(json);
      var root = doc.RootElement;

      _translations.Clear();

      // Parse nodes array (case-insensitive)
      if (root.TryGetProperty("nodes", out var nodes) || root.TryGetProperty("Nodes", out nodes)) {
        foreach (var node in nodes.EnumerateArray()) {
          var guid = node.TryGetProperty("guid", out var g1) ? g1.GetString() :
                     node.TryGetProperty("Guid", out var g2) ? g2.GetString() : null;
          var text = node.TryGetProperty("text", out var t1) ? t1.GetString() :
                     node.TryGetProperty("Text", out var t2) ? t2.GetString() : null;

          if (!string.IsNullOrEmpty(guid) && !string.IsNullOrEmpty(text)) {
            _translations[guid] = text;
          }
        }
      }

      _currentServerLanguage = normalizedLanguage;
      Log.Info($"Loaded {Translations.Count} translations for language: {normalizedLanguage}");
    } catch (Exception ex) {
      Log.Error($"Error loading language {language}: {ex}");
    }
  }

  /// <summary>
  /// Load prefab to GUID mapping from embedded resources
  /// </summary>
  private static void LoadPrefabMapping() {
    try {
      // Use ScarletCore assembly for loading game prefab mapping
      var assembly = typeof(LocalizationService).Assembly;
      var resourceName = "ScarletCore.Localization.PrefabToGuidMap.json";

      using var stream = assembly.GetManifestResourceStream(resourceName);
      if (stream == null) {
        Log.Warning("PrefabToGuidMap.json not found in embedded resources");
        return;
      }

      using var reader = new StreamReader(stream);
      var json = reader.ReadToEnd();

      var doc = JsonDocument.Parse(json);
      var root = doc.RootElement;

      _prefabToGuid.Clear();

      foreach (var property in root.EnumerateObject()) {
        if (int.TryParse(property.Name, out var prefabId)) {
          _prefabToGuid[prefabId] = property.Value.GetString();
        }
      }

      Log.Info($"Loaded {PrefabMappings.Count} prefab mappings");
    } catch (Exception ex) {
      Log.Error($"Error loading prefab mapping: {ex}");
    }
  }

  /// <summary>
  /// Formats a localized string with the provided parameters.
  /// Replaces {0}, {1}, {2}, etc. with the corresponding parameter values.
  /// </summary>
  /// <param name="text">The text template with placeholders</param>
  /// <param name="parameters">The values to insert into the placeholders</param>
  /// <returns>The formatted string</returns>
  private static string FormatString(string text, params object[] parameters) {
    if (parameters == null || parameters.Length == 0) return text;

    try {
      return string.Format(text, parameters);
    } catch (FormatException ex) {
      Log.Warning($"Failed to format localized string: {ex.Message}");
      return text;
    }
  }

  /// <summary>
  /// Get localized text by GUID string.
  /// Returns the GUID itself if no translation is found.
  /// </summary>
  /// <param name="guid">The localization GUID to look up</param>
  /// <param name="parameters">Optional parameters to format the localized string (e.g., {0}, {1})</param>
  /// <returns>The localized text, or the GUID if not found</returns>
  public static string GetText(string guid, params object[] parameters) {
    if (!_initialized) Initialize();
    if (string.IsNullOrEmpty(guid)) return string.Empty;

    var text = Translations.TryGetValue(guid, out var translation) ? translation : guid;
    return FormatString(text, parameters);
  }

  /// <summary>
  /// Get localized text by PrefabGUID.
  /// Returns the PrefabGUID string representation if no translation is found.
  /// </summary>
  /// <param name="prefabGuid">The PrefabGUID to get the localized name for</param>
  /// <param name="parameters">Optional parameters to format the localized string (e.g., {0}, {1})</param>
  /// <returns>The localized name, or the PrefabGUID string if not found</returns>
  public static string GetText(PrefabGUID prefabGuid, params object[] parameters) {
    if (!_initialized) Initialize();

    if (PrefabMappings.TryGetValue(prefabGuid.GuidHash, out var guid)) {
      return GetText(guid, parameters);
    }

    return prefabGuid.ToString();
  }

  /// <summary>
  /// Get prefab name (alias for GetText).
  /// Convenience method for getting localized item, buff, or entity names.
  /// </summary>
  /// <param name="prefabGuid">The PrefabGUID to get the name for</param>
  /// <param name="parameters">Optional parameters to format the localized string (e.g., {0}, {1})</param>
  /// <returns>The localized name of the prefab</returns>
  public static string GetPrefabName(PrefabGUID prefabGuid, params object[] parameters) {
    return GetText(prefabGuid, parameters);
  }

  /// <summary>
  /// Gets the calling assembly from the stack trace, skipping LocalizationService methods.
  /// This ensures we get the actual calling assembly, not LocalizationService itself.
  /// </summary>
  private static Assembly GetCallingAssemblyFromStack() {
    var stackTrace = new System.Diagnostics.StackTrace();
    var frames = stackTrace.GetFrames();

    if (frames == null) return Assembly.GetCallingAssembly();

    // Skip frames until we're out of LocalizationService
    foreach (var frame in frames) {
      var method = frame.GetMethod();
      if (method == null) continue;

      var declaringType = method.DeclaringType;
      if (declaringType == null) continue;

      // Skip if it's LocalizationService itself
      if (declaringType == typeof(LocalizationService)) continue;

      // Return the first assembly we find that's not LocalizationService
      return declaringType.Assembly;
    }

    return Assembly.GetCallingAssembly();
  }

  /// <summary>
  /// Builds a composite key from assembly name and key string.
  /// Format: "AssemblyName:key"
  /// </summary>
  private static string BuildCompositeKey(Assembly assembly, string key) {
    var assemblyName = assembly.GetName().Name;
    return $"{assemblyName}:{key}";
  }

  /// <summary>
  /// Register a new custom localization key with translations for multiple languages.
  /// Language codes should match the server/player language keys (e.g. "english", "portuguese").
  /// The key is automatically prefixed with the calling assembly name to avoid conflicts.
  /// </summary>
  /// <param name="key">The localization key (will be prefixed with assembly name)</param>
  /// <param name="translations">Dictionary mapping language codes to translated text</param>
  public static void NewKey(string key, IDictionary<string, string> translations) {
    if (string.IsNullOrWhiteSpace(key) || translations == null || translations.Count == 0) return;

    var callingAssembly = GetCallingAssemblyFromStack();
    var compositeKey = BuildCompositeKey(callingAssembly, key.Trim());

    var map = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    foreach (var kv in translations) {
      if (string.IsNullOrWhiteSpace(kv.Key) || kv.Value == null) continue;
      map[kv.Key.ToLower().Trim()] = kv.Value;
    }

    if (map.IsEmpty) return;

    _customKeys[compositeKey] = map;
  }

  /// <summary>
  /// Loads multiple custom keys from a dictionary organized by language.
  /// Expected format:
  /// {
  ///   "portuguese": { "help message": "mensagem de ajuda" },
  ///   "english": { "help message": "help message" }
  /// }
  /// The outer key is the language code and the value is a map of (key -> translated text).
  /// Keys are automatically prefixed with the calling assembly name.
  /// </summary>
  public static void LoadCustomKeys(IDictionary<string, IDictionary<string, string>> languageMap) {
    if (languageMap == null || languageMap.Count == 0) return;

    var callingAssembly = GetCallingAssemblyFromStack();

    foreach (var langEntry in languageMap) {
      if (string.IsNullOrWhiteSpace(langEntry.Key) || langEntry.Value == null) continue;

      var lang = langEntry.Key.ToLower().Trim();

      foreach (var kv in langEntry.Value) {
        if (kv.Key == null || kv.Value == null) continue;

        var normalizedKey = kv.Key.Trim();
        if (string.IsNullOrEmpty(normalizedKey)) continue;

        var compositeKey = BuildCompositeKey(callingAssembly, normalizedKey);
        var map = _customKeys.GetOrAdd(compositeKey, _ => new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        map[lang] = kv.Value;
      }
    }

    Log.Info($"Loaded custom localization keys for {callingAssembly.GetName().Name}: {languageMap.SelectMany(x => x.Value.Keys).Distinct().Count()} unique keys");
  }

  /// <summary>
  /// Get a localized string for a player from a custom key. Falls back to server language, then first available language.
  /// Returns the key string if not found.
  /// The key is automatically prefixed with the calling assembly name.
  /// </summary>
  /// <param name="player">The player to get the localized text for</param>
  /// <param name="key">The localization key (will be prefixed with assembly name)</param>
  /// <param name="parameters">Optional parameters to format the localized string (e.g., {0}, {1})</param>
  /// <returns>The localized text, or the key if not found</returns>
  public static string Get(PlayerData player, string key, params object[] parameters) {
    if (string.IsNullOrWhiteSpace(key)) return string.Empty;
    if (!_initialized) Initialize();

    var callingAssembly = GetCallingAssemblyFromStack();
    var compositeKey = BuildCompositeKey(callingAssembly, key.Trim());

    if (!_customKeys.TryGetValue(compositeKey, out var translations) || translations == null || translations.IsEmpty) {
      return key;
    }

    // Determine player language: try per-player, then DefaultPlayerLanguage setting, then server language
    var playerLang = GetPlayerLanguage(player) ?? Plugin.Settings.Get<string>("DefaultPlayerLanguage") ?? _currentServerLanguage;
    playerLang = playerLang.ToLower().Trim();


    if (translations.TryGetValue(playerLang, out string text) && !string.IsNullOrEmpty(text)) {
      return FormatString(text, parameters);
    }

    if (!string.Equals(playerLang, _currentServerLanguage, StringComparison.OrdinalIgnoreCase) &&
        translations.TryGetValue(_currentServerLanguage, out var serverText) && !string.IsNullOrEmpty(serverText)) {
      return FormatString(serverText, parameters);
    }

    // Last resort: return first available translation
    var first = translations.Values.FirstOrDefault(v => !string.IsNullOrEmpty(v));
    return first != null ? FormatString(first, parameters) : key;
  }

  /// <summary>
  /// Get a localized string for a player for a specific assembly.
  /// This overload allows callers to specify which assembly owns the key,
  /// avoiding issues when intermediate helper methods (like CommandContext)
  /// are in a different assembly.
  /// </summary>
  public static string Get(PlayerData player, string key, Assembly assembly, params object[] parameters) {
    if (string.IsNullOrWhiteSpace(key)) return string.Empty;
    if (!_initialized) Initialize();

    var useAssembly = assembly ?? GetCallingAssemblyFromStack();
    var compositeKey = BuildCompositeKey(useAssembly, key.Trim());

    if (!_customKeys.TryGetValue(compositeKey, out var translations) || translations == null || translations.IsEmpty) {
      return key;
    }

    var playerLang = GetPlayerLanguage(player) ?? Plugin.Settings.Get<string>("DefaultPlayerLanguage") ?? _currentServerLanguage;
    playerLang = playerLang.ToLower().Trim();

    if (translations.TryGetValue(playerLang, out string text) && !string.IsNullOrEmpty(text)) {
      return FormatString(text, parameters);
    }

    if (!string.Equals(playerLang, _currentServerLanguage, StringComparison.OrdinalIgnoreCase) &&
        translations.TryGetValue(_currentServerLanguage, out var serverText) && !string.IsNullOrEmpty(serverText)) {
      return FormatString(serverText, parameters);
    }

    // Last resort: return first available translation
    var first = translations.Values.FirstOrDefault(v => !string.IsNullOrEmpty(v));
    return first != null ? FormatString(first, parameters) : key;
  }

  /// <summary>
  /// Get a localized string directly by composite key (assembly:key format).
  /// This method is for internal use or when you need to access keys from other assemblies.
  /// Falls back to server language, then first available language.
  /// </summary>
  /// <param name="player">The player to get the localized text for</param>
  /// <param name="compositeKey">The full composite key in format "AssemblyName:key"</param>
  /// <param name="parameters">Optional parameters to format the localized string (e.g., {0}, {1})</param>
  /// <returns>The localized text, or the key portion if not found</returns>
  public static string GetByCompositeKey(PlayerData player, string compositeKey, params object[] parameters) {
    if (string.IsNullOrWhiteSpace(compositeKey)) return string.Empty;
    if (!_initialized) Initialize();

    if (!_customKeys.TryGetValue(compositeKey, out var translations) || translations == null || translations.IsEmpty) {
      // Return just the key portion (after the colon) if not found
      var parts = compositeKey.Split(':', 2);
      return parts.Length > 1 ? parts[1] : compositeKey;
    }

    var playerLang = GetPlayerLanguage(player) ?? Plugin.Settings.Get<string>("DefaultPlayerLanguage") ?? _currentServerLanguage;
    playerLang = playerLang.ToLower().Trim();

    if (translations.TryGetValue(playerLang, out string text) && !string.IsNullOrEmpty(text)) {
      return FormatString(text, parameters);
    }

    if (!string.Equals(playerLang, _currentServerLanguage, StringComparison.OrdinalIgnoreCase) &&
        translations.TryGetValue(_currentServerLanguage, out var serverText) && !string.IsNullOrEmpty(serverText)) {
      return FormatString(serverText, parameters);
    }

    var first = translations.Values.FirstOrDefault(v => !string.IsNullOrEmpty(v));
    var parts2 = compositeKey.Split(':', 2);
    var fallback = parts2.Length > 1 ? parts2[1] : compositeKey;
    return first != null ? FormatString(first, parameters) : fallback;
  }

  /// <summary>
  /// Get all registered custom keys for a specific assembly.
  /// Returns the key names without the assembly prefix.
  /// </summary>
  /// <param name="assembly">The assembly to get keys for (null for calling assembly)</param>
  /// <returns>Collection of key names</returns>
  public static IEnumerable<string> GetKeysForAssembly(Assembly assembly = null) {
    if (!_initialized) Initialize();

    assembly ??= GetCallingAssemblyFromStack();
    var assemblyName = assembly.GetName().Name;
    var prefix = $"{assemblyName}:";

    return _customKeys.Keys
      .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
      .Select(k => k[prefix.Length..]);
  }

  /// <summary>
  /// Check if a custom key exists for the calling assembly.
  /// </summary>
  /// <param name="key">The key to check (without assembly prefix)</param>
  /// <returns>True if the key exists, false otherwise</returns>
  public static bool HasCustomKey(string key) {
    if (string.IsNullOrWhiteSpace(key)) return false;
    if (!_initialized) Initialize();

    var callingAssembly = GetCallingAssemblyFromStack();
    var compositeKey = BuildCompositeKey(callingAssembly, key.Trim());
    return _customKeys.ContainsKey(compositeKey);
  }

  /// <summary>
  /// Unregister all custom localization keys for the given assembly (or calling assembly if null).
  /// Removes entries from the custom key map and returns the number of keys removed.
  /// </summary>
  /// <param name="assembly">The assembly whose keys should be removed (null for calling assembly)</param>
  /// <returns>The number of custom keys removed for the assembly</returns>
  public static int Dispose(Assembly assembly = null) {
    if (!_initialized) Initialize();

    assembly ??= GetCallingAssemblyFromStack();
    var assemblyName = assembly.GetName().Name;
    var prefix = $"{assemblyName}:";

    var keys = _customKeys.Keys
      .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
      .ToList();

    var removed = 0;
    foreach (var k in keys) {
      if (_customKeys.TryRemove(k, out _)) removed++;
    }

    Log.Info($"Disposed {removed} localization keys for assembly: {assemblyName}");
    return removed;
  }

  /// <summary>
  /// Set a player's preferred language. Stored using PlayerData.SetData for this assembly.
  /// </summary>
  public static void SetPlayerLanguage(PlayerData player, string language) {
    if (player == null) return;
    if (string.IsNullOrWhiteSpace(language)) return;

    try {
      var lang = language.ToLower().Trim();

      // Load or create the player languages map from the database
      var map = Plugin.Database.GetOrCreate("player_languages", () => new Dictionary<string, string>());
      var key = player.PlatformId.ToString();
      map[key] = lang;
      Plugin.Database.Save("player_languages", map);
    } catch (Exception ex) {
      Log.Error($"Failed to set player language: {ex}");
    }
  }

  /// <summary>
  /// Get a player's preferred language if set, otherwise null.
  /// </summary>
  public static string GetPlayerLanguage(PlayerData player) {
    if (player == null) return null;
    try {
      var map = Plugin.Database.Get<Dictionary<string, string>>("player_languages");
      if (map == null) return null;
      var key = player.PlatformId.ToString();
      if (map.TryGetValue(key, out var lang) && !string.IsNullOrWhiteSpace(lang)) return lang.ToLower().Trim();
      return null;
    } catch (Exception ex) {
      Log.Error($"Failed to get player language: {ex}");
      return null;
    }
  }

  /// <summary>
  /// Check if a translation exists for the given GUID.
  /// Useful for validating whether a localization key is present.
  /// </summary>
  /// <param name="guid">The localization GUID to check</param>
  /// <returns>True if a translation exists, false otherwise</returns>
  public static bool HasTranslation(string guid) {
    if (!_initialized) Initialize();
    return !string.IsNullOrEmpty(guid) && Translations.ContainsKey(guid);
  }

  /// <summary>
  /// Check if a translation exists for the given PrefabGUID.
  /// Verifies if the prefab has a localization mapping.
  /// </summary>
  /// <param name="prefabGuid">The PrefabGUID to check</param>
  /// <returns>True if a translation exists, false otherwise</returns>
  public static bool HasTranslation(PrefabGUID prefabGuid) {
    if (!_initialized) Initialize();
    return PrefabMappings.ContainsKey(prefabGuid.GuidHash);
  }

  /// <summary>
  /// Get the localization GUID string for a PrefabGUID.
  /// Returns the underlying localization key used for translations.
  /// </summary>
  /// <param name="prefabGuid">The PrefabGUID to get the localization GUID for</param>
  /// <returns>The localization GUID string, or null if not found</returns>
  public static string GetGuidForPrefab(PrefabGUID prefabGuid) {
    if (!_initialized) Initialize();
    return PrefabMappings.TryGetValue(prefabGuid.GuidHash, out var guid) ? guid : null;
  }

  /// <summary>
  /// Search translations by text content.
  /// Performs a case-insensitive search across all loaded translations.
  /// </summary>
  /// <param name="searchText">The text to search for</param>
  /// <returns>Collection of matching GUID-text pairs</returns>
  public static IEnumerable<KeyValuePair<string, string>> SearchTranslations(string searchText) {
    if (!_initialized) Initialize();
    if (string.IsNullOrEmpty(searchText)) return [];

    return Translations.Where(kvp => kvp.Value.Contains(searchText, StringComparison.OrdinalIgnoreCase));
  }

  /// <summary>
  /// Get total count of loaded translations
  /// </summary>
  public static int TranslationCount => Translations.Count;

  /// <summary>
  /// Get total count of prefab mappings
  /// </summary>
  public static int PrefabMappingCount => PrefabMappings.Count;

  /// <summary>
  /// Get total count of custom localization keys across all assemblies
  /// </summary>
  public static int CustomKeyCount => _customKeys.Count;

  /// <summary>
  /// Change the current language at runtime.
  /// Reloads all translations with the new language data.
  /// </summary>
  /// <param name="language">The language code to switch to</param>
  /// <returns>True if the language was successfully changed, false if not supported</returns>
  public static bool ChangeLanguage(string language) {
    var normalizedLanguage = language.ToLower().Trim();

    if (!_languageFileMapping.ContainsKey(normalizedLanguage)) {
      Log.Warning($"Language not supported: {language}");
      return false;
    }

    LoadLanguage(normalizedLanguage);
    return true;
  }

  /// <summary>
  /// Check if a language is available.
  /// Validates if the language code exists in the supported languages.
  /// </summary>
  /// <param name="language">The language code to check</param>
  /// <returns>True if the language is supported, false otherwise</returns>
  public static bool IsLanguageAvailable(string language) {
    return _languageFileMapping.ContainsKey(language.ToLower().Trim());
  }
}