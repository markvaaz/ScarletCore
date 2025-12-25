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
  /// Custom localization keys created at runtime. Key -> (language -> text)
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

      var assembly = Assembly.GetExecutingAssembly();
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
      var assembly = Assembly.GetExecutingAssembly();
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
  /// Get localized text by GUID string.
  /// Returns the GUID itself if no translation is found.
  /// </summary>
  /// <param name="guid">The localization GUID to look up</param>
  /// <returns>The localized text, or the GUID if not found</returns>
  public static string GetText(string guid) {
    if (!_initialized) Initialize();
    if (string.IsNullOrEmpty(guid)) return string.Empty;

    return Translations.TryGetValue(guid, out var text) ? text : guid;
  }

  /// <summary>
  /// Get localized text by PrefabGUID.
  /// Returns the PrefabGUID string representation if no translation is found.
  /// </summary>
  /// <param name="prefabGuid">The PrefabGUID to get the localized name for</param>
  /// <returns>The localized name, or the PrefabGUID string if not found</returns>
  public static string GetText(PrefabGUID prefabGuid) {
    if (!_initialized) Initialize();

    if (PrefabMappings.TryGetValue(prefabGuid.GuidHash, out var guid)) {
      return GetText(guid);
    }

    return prefabGuid.ToString();
  }

  /// <summary>
  /// Get prefab name (alias for GetText).
  /// Convenience method for getting localized item, buff, or entity names.
  /// </summary>
  /// <param name="prefabGuid">The PrefabGUID to get the name for</param>
  /// <returns>The localized name of the prefab</returns>
  public static string GetPrefabName(PrefabGUID prefabGuid) {
    return GetText(prefabGuid);
  }

  /// <summary>
  /// Register a new custom localization key with translations for multiple languages.
  /// Language codes should match the server/player language keys (e.g. "english", "portuguese").
  /// </summary>
  public static void NewKey(string key, IDictionary<string, string> translations) {
    if (string.IsNullOrWhiteSpace(key) || translations == null || translations.Count == 0) return;

    var normalizedKey = key.Trim();
    var map = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    foreach (var kv in translations) {
      if (string.IsNullOrWhiteSpace(kv.Key) || kv.Value == null) continue;
      map[kv.Key.ToLower().Trim()] = kv.Value;
    }

    if (map.IsEmpty) return;

    _customKeys[normalizedKey] = map;
  }

  /// <summary>
  /// Get a localized string for a player from a custom key. Falls back to server language, then first available language.
  /// Returns the key string if not found.
  /// </summary>
  public static string Get(PlayerData player, string key) {
    if (string.IsNullOrWhiteSpace(key)) return string.Empty;
    if (!_initialized) Initialize();

    var normalizedKey = key.Trim();

    if (!_customKeys.TryGetValue(normalizedKey, out var translations) || translations == null || translations.IsEmpty) {
      return key;
    }

    // Determine player language: try per-player, then DefaultPlayerLanguage setting, then server language
    var playerLang = GetPlayerLanguage(player) ?? Plugin.Settings.Get<string>("DefaultPlayerLanguage") ?? _currentServerLanguage;
    playerLang = playerLang.ToLower().Trim();

    if (translations.TryGetValue(playerLang, out var text) && !string.IsNullOrEmpty(text)) return text;

    if (!string.Equals(playerLang, _currentServerLanguage, StringComparison.OrdinalIgnoreCase) && translations.TryGetValue(_currentServerLanguage, out var serverText) && !string.IsNullOrEmpty(serverText)) return serverText;

    // Last resort: return first available translation
    var first = translations.Values.FirstOrDefault(v => !string.IsNullOrEmpty(v));
    return first ?? key;
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