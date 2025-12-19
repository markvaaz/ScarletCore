using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using ProjectM.Network;
using ScarletCore.Data;
using ScarletCore.Events;
using ScarletCore.Utils;
using Stunlock.Core;
using Unity.Entities;

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
  private static readonly Dictionary<string, string> _translations = new();

  /// <summary>
  /// Dictionary mapping PrefabGUID hash values to localization GUIDs
  /// </summary>
  private static readonly Dictionary<int, string> _prefabToGuid = new();

  /// <summary>
  /// Flag indicating whether the service has been initialized
  /// </summary>
  private static bool _initialized = false;

  /// <summary>
  /// The currently loaded language code
  /// </summary>
  private static string _currentLanguage = "english";

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
  public static string[] AvailableLanguages => _languageFileMapping.Keys.ToArray();

  /// <summary>
  /// Get the current loaded language
  /// </summary>
  public static string CurrentLanguage => _currentLanguage;

  /// <summary>
  /// Initialize the localization service. Loads English by default.
  /// Call this during game initialization to load embedded localization resources.
  /// </summary>
  public static void Initialize() {
    var language = Plugin.Settings.Get<string>("language") ?? "english";

    if (_initialized) {
      LoadLanguage(language);
      return;
    }

    try {
      LoadPrefabMapping();
      LoadLanguage(language);
      _initialized = true;
      EventManager.On(PrefixEvents.OnChatMessage, (entities) => {
        foreach (var entity in entities) {
          HandleLanguageCommand(entity);
        }
      });
      Log.Info($"LocalizationService initialized with {_translations.Count} translations and {_prefabToGuid.Count} prefab mappings");
    } catch (Exception ex) {
      Log.Error($"Failed to initialize LocalizationService: {ex}");
    }
  }

  /// <summary>
  /// Handles the .setlanguage chat command for changing the localization language.
  /// Only admins can execute this command. Changes the language globally for all translations.
  /// </summary>
  /// <param name="messageEntity">The chat message entity containing the command</param>
  public static void HandleLanguageCommand(Entity messageEntity) {
    if (!messageEntity.Exists() || !messageEntity.Has<ChatMessageEvent>()) return;
    var chatMessageEvent = messageEntity.Read<ChatMessageEvent>();

    if (chatMessageEvent.MessageType != ChatMessageType.Local) return;

    var messageText = chatMessageEvent.MessageText.Value;

    if (!messageText.StartsWith(".setlanguage")) return;

    var character = messageEntity.Read<FromCharacter>().Character;
    var player = character.GetPlayerData();

    if (player == null || !player.IsAdmin) return;

    var parts = messageText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

    if (parts.Length < 2) {
      LanguageNotFound(player);
      messageEntity.Destroy(true);
      return;
    }

    var newLanguage = parts[1].ToLower();

    if (!LocalizationService.IsLanguageAvailable(newLanguage)) {
      LanguageNotFound(player);
      messageEntity.Destroy(true);
      return;
    }

    if (LocalizationService.ChangeLanguage(newLanguage)) {
      Plugin.Settings.Set("language", newLanguage);
      player.SendMessage($"~ScarletCore~ localization language changed to: {newLanguage}".FormatSuccess());
      Utils.Log.Info($"ScarletCore localization language changed to: {newLanguage} by admin {player.Name}");
    } else {
      player.SendMessage($"Failed to change language to: ~{newLanguage}~".FormatError());
    }

    messageEntity.Destroy(true);
  }

  public static void LanguageNotFound(PlayerData player) {
    var availableLanguages = string.Join(", ", LocalizationService.AvailableLanguages);
    player.SendMessage($"~**Usage:** '.setlanguage <language_code>'~".FormatError());
    player.SendMessage($"~Available languages:~ {availableLanguages}".FormatError());
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
        Log.Warning($"Language not supported: {language}. Available languages: {string.Join(", ", AvailableLanguages)}");
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

      _currentLanguage = normalizedLanguage;
      Log.Info($"Loaded {_translations.Count} translations for language: {normalizedLanguage}");
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

      Log.Info($"Loaded {_prefabToGuid.Count} prefab mappings");
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

    return _translations.TryGetValue(guid, out var text) ? text : guid;
  }

  /// <summary>
  /// Get localized text by PrefabGUID.
  /// Returns the PrefabGUID string representation if no translation is found.
  /// </summary>
  /// <param name="prefabGuid">The PrefabGUID to get the localized name for</param>
  /// <returns>The localized name, or the PrefabGUID string if not found</returns>
  public static string GetText(PrefabGUID prefabGuid) {
    if (!_initialized) Initialize();

    if (_prefabToGuid.TryGetValue(prefabGuid.GuidHash, out var guid)) {
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
  /// Check if a translation exists for the given GUID.
  /// Useful for validating whether a localization key is present.
  /// </summary>
  /// <param name="guid">The localization GUID to check</param>
  /// <returns>True if a translation exists, false otherwise</returns>
  public static bool HasTranslation(string guid) {
    if (!_initialized) Initialize();
    return !string.IsNullOrEmpty(guid) && _translations.ContainsKey(guid);
  }

  /// <summary>
  /// Check if a translation exists for the given PrefabGUID.
  /// Verifies if the prefab has a localization mapping.
  /// </summary>
  /// <param name="prefabGuid">The PrefabGUID to check</param>
  /// <returns>True if a translation exists, false otherwise</returns>
  public static bool HasTranslation(PrefabGUID prefabGuid) {
    if (!_initialized) Initialize();
    return _prefabToGuid.ContainsKey(prefabGuid.GuidHash);
  }

  /// <summary>
  /// Get the localization GUID string for a PrefabGUID.
  /// Returns the underlying localization key used for translations.
  /// </summary>
  /// <param name="prefabGuid">The PrefabGUID to get the localization GUID for</param>
  /// <returns>The localization GUID string, or null if not found</returns>
  public static string GetGuidForPrefab(PrefabGUID prefabGuid) {
    if (!_initialized) Initialize();
    return _prefabToGuid.TryGetValue(prefabGuid.GuidHash, out var guid) ? guid : null;
  }

  /// <summary>
  /// Search translations by text content.
  /// Performs a case-insensitive search across all loaded translations.
  /// </summary>
  /// <param name="searchText">The text to search for</param>
  /// <returns>Collection of matching GUID-text pairs</returns>
  public static IEnumerable<KeyValuePair<string, string>> SearchTranslations(string searchText) {
    if (!_initialized) Initialize();
    if (string.IsNullOrEmpty(searchText)) return Enumerable.Empty<KeyValuePair<string, string>>();

    return _translations.Where(kvp => kvp.Value.Contains(searchText, StringComparison.OrdinalIgnoreCase));
  }

  /// <summary>
  /// Get total count of loaded translations
  /// </summary>
  public static int TranslationCount => _translations.Count;

  /// <summary>
  /// Get total count of prefab mappings
  /// </summary>
  public static int PrefabMappingCount => _prefabToGuid.Count;

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