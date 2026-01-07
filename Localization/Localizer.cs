using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using ScarletCore.Utils;
using Stunlock.Core;
using ScarletCore.Events;
using ProjectM;
using ScarletCore.Services;

namespace ScarletCore.Localization;

/// <summary>
/// Service for managing game localization and translations.
/// Provides access to localized text for items, buffs, and other game elements by PrefabGUID.
/// Supports multiple languages loaded from embedded JSON resources.
/// </summary>
public static class Localizer {
  /// <summary>
  /// All translations: GUID -> (Language -> Text)
  /// This replaces both _translations and _customKeys
  /// </summary>
  private static readonly ConcurrentDictionary<string, ConcurrentDictionary<Language, string>> _allTranslations = new(StringComparer.OrdinalIgnoreCase);

  /// <summary>
  /// Dictionary mapping PrefabGUID hash values to localization GUIDs
  /// </summary>
  private static readonly ConcurrentDictionary<int, string> _prefabToGuid = new();

  /// <summary>
  /// Flag indicating whether the service has been initialized
  /// </summary>
  private static bool _initialized = false;

  /// <summary>
  /// The currently loaded language
  /// </summary>
  private static Language _currentServerLanguage = Language.English;

  /// <summary>
  /// Mapping of language enum values to their corresponding names in JSON
  /// </summary>
  private static readonly Language[] _languages = [
    Language.Portuguese,
    Language.English,
    Language.French,
    Language.German,
    Language.Hungarian,
    Language.Italian,
    Language.Japanese,
    Language.Korean,
    Language.Latam,
    Language.Polish,
    Language.Russian,
    Language.Spanish,
    Language.ChineseSimplified,
    Language.ChineseTraditional,
    Language.Thai,
    Language.Turkish,
    Language.Ukrainian,
    Language.Vietnamese,
  ];

  /// <summary>
  /// Read-only view of prefab mappings
  /// </summary>
  public static IReadOnlyDictionary<int, string> PrefabMappings => _prefabToGuid;

  /// <summary>
  /// Get all available languages
  /// </summary>
  public static Language[] AvailableServerLanguages => _languages;

  /// <summary>
  /// Get the current loaded language
  /// </summary>
  public static Language CurrentServerLanguage => _currentServerLanguage;


  /// <summary>
  /// Determines whether the specified language is available for localization.
  /// </summary>
  /// <param name="language">The language to check for availability.</param>
  /// <returns>True if the language is available; otherwise, false.</returns>
  public static bool IsLanguageAvailable(Language language) {
    return _languages.Contains(language);
  }

  /// <summary>
  /// Converts a string representation to GameLanguage enum.
  /// Returns null if the string doesn't match any language.
  /// </summary>
  public static Language GetLanguageFromString(string languageString) {
    if (string.IsNullOrWhiteSpace(languageString)) return _currentServerLanguage;

    var normalized = languageString.ToLower().Trim()
      .Replace(" ", "")
      .Replace("_", "")
      .Replace("-", "");

    foreach (Language lang in Enum.GetValues<Language>()) {
      var enumName = lang.ToString().ToLower();
      if (enumName == normalized) return lang;
    }

    // Special cases for common variations
    return normalized switch {
      "pt" or "ptbr" or "brazilian" or "portuguese" or "portugues" or "português" => Language.Portuguese,
      "en" or "enus" or "engb" or "english" => Language.English,
      "fr" or "french" or "francais" or "français" => Language.French,
      "de" or "german" or "deutsch" => Language.German,
      "hu" or "hungarian" or "magyar" => Language.Hungarian,
      "it" or "italian" or "italiano" => Language.Italian,
      "ja" or "jp" or "japanese" or "nihongo" => Language.Japanese,
      "ko" or "kr" or "korean" or "hangul" => Language.Korean,
      "es" or "eses" or "spanish" or "espanol" or "español" => Language.Spanish,
      "esl" or "latam" or "latinamerican" or "latinamerica" => Language.Latam,
      "pl" or "polish" or "polski" => Language.Polish,
      "ru" or "russian" or "russkiy" or "русский" => Language.Russian,
      "zh" or "zhcn" or "chs" or "chinesesimplified" or "simplifiedchinese" => Language.ChineseSimplified,
      "zhtw" or "cht" or "chinesetraditional" or "traditionalchinese" => Language.ChineseTraditional,
      "th" or "thai" or "thaithai" => Language.Thai,
      "tr" or "turkish" or "turkce" or "türkçe" => Language.Turkish,
      "uk" or "ua" or "ukrainian" or "українська" => Language.Ukrainian,
      "vi" or "vn" or "vietnamese" or "tiếngviệt" => Language.Vietnamese,
      _ => Language.None
    };
  }

  /// <summary>
  /// Initialize the localization service. Loads English by default.
  /// Call this during game initialization to load embedded localization resources.
  /// </summary>
  public static void Initialize() {
    var language = Plugin.Settings.Get<Language>("PrefabLocalizationLanguage");

    try {
      // LoadGameTranslations();
      AutoLoadFromLocalizationFolder();
      LoadPrefabMapping();
      LoadGameTranslations();
      EventManager.On(PlayerEvents.PlayerJoined, CheckLanguageOnJoin);
      _initialized = true;
      _currentServerLanguage = language;
      Log.Message($"Localizer initialized with {_allTranslations.Count} translation keys and {_prefabToGuid.Count} prefab mappings");
    } catch (Exception ex) {
      Log.Error($"[Localizer] Failed to initialize LocalizationService: {ex}");
    }
  }

  private static void CheckLanguageOnJoin(PlayerData player) {
    try {
      if (Plugin.Settings.Get<bool>("DisableLanguageSelectionPrompt") || player == null) return;

      var lang = GetPlayerLanguage(player);

      if (lang != Language.None) return;

      var welcomeMessage = Plugin.Settings.Get<string>("WelcomeMessage");
      var serverName = SettingsManager.ServerHostSettings.Name ?? "the Server";
      var availableLanguages = string.Join(", ", AvailableServerLanguages);
      var playerName = player.Name;

      welcomeMessage = welcomeMessage
        .Replace("{ServerName}", serverName)
        .Replace("{AvailableLanguages}", availableLanguages.WithColor("#ffd93d"))
        .Replace("{PlayerName}", playerName);

      var messages = welcomeMessage.Split("\n");

      foreach (var line in messages) {
        player.SendMessage(line.Trim());
      }
    } catch (Exception ex) {
      Log.Error($"[Localizer] CheckLanguageOnJoin failed: {ex}");
    }
  }

  /// <summary>
  /// <para>Automatically finds and loads all JSON translation files from embedded resources 
  /// in the "Localization" folder of the calling assembly.</para>
  /// <para>The JSON files must be in the format: { key: { languageEnum: text } }.</para>
  /// <para>Note: JSON files must be included as embedded resources in your .csproj file:</para>
  /// <code>
  /// &lt;ItemGroup&gt;
  ///   &lt;EmbeddedResource Include="Localization\**\*.*" /&gt;
  /// &lt;/ItemGroup&gt;
  /// </code>
  /// </summary>
  public static void AutoLoadFromLocalizationFolder() {
    try {
      // Get the calling assembly (not the Localizer's assembly)
      var callingAssembly = Assembly.GetCallingAssembly();
      var assemblyName = callingAssembly.GetName().Name;
      var localizationPrefix = $"{assemblyName}.Localization.";

      // Get all embedded resource names that start with the localization prefix
      var resourceNames = callingAssembly.GetManifestResourceNames()
        .Where(name => name.StartsWith(localizationPrefix) && name.EndsWith(".json"))
        .ToArray();

      if (resourceNames.Length == 0) {
        Log.Message($"[Localizer] No JSON files found in embedded resources '{localizationPrefix}' for assembly: {assemblyName}");
        return;
      }

      int successCount = 0;
      int errorCount = 0;

      foreach (var resourceName in resourceNames) {
        try {
          var jsonContent = LoadResourceFromAssembly(callingAssembly, resourceName);

          if (string.IsNullOrEmpty(jsonContent)) {
            continue; // Silently skip empty resources
          }

          var deserialized = JsonSerializer.Deserialize<IDictionary<string, IDictionary<Language, string>>>(jsonContent);

          if (deserialized == null || deserialized.Count == 0) {
            continue; // Silently skip incompatible format
          }

          LoadKeys(deserialized);
          successCount++;

          var fileName = resourceName[localizationPrefix.Length..];
          Log.Message($"[Localizer] Loaded translations from: {fileName} ({deserialized.Count} keys)");
        } catch (JsonException) {
          // Silently skip files with incompatible JSON format
          continue;
        } catch (Exception ex) {
          errorCount++;
          Log.Error($"[Localizer] Error loading translation resource '{resourceName}': {ex.Message}");
        }
      }

      if (successCount > 0) {
        Log.Message($"[Localizer] Successfully loaded {successCount} translation file(s) from '{assemblyName}.Localization' embedded resources");
      }

      if (errorCount > 0) {
        Log.Warning($"[Localizer] Failed to load {errorCount} translation file(s) due to read errors");
      }
    } catch (Exception ex) {
      Log.Error($"[Localizer] Error loading translations from Localization embedded resources: {ex}");
    }
  }

  /// <summary>
  /// Helper method to load an embedded resource from an assembly.
  /// </summary>
  private static string LoadResourceFromAssembly(Assembly assembly, string resourceName) {
    using var stream = assembly.GetManifestResourceStream(resourceName);

    if (stream == null) {
      Log.Error($"[Localizer] Resource '{resourceName}' not found in assembly '{assembly.FullName}'");
      return null;
    }

    using var reader = new StreamReader(stream);
    return reader.ReadToEnd();
  }

  /// <summary>
  /// Loads additional translations from an embedded JSON resource.
  /// The JSON must be in the format: { key: { languageEnum: text } }.
  /// </summary>
  /// <param name="resourceName">The manifest resource name to load (e.g. "Namespace.FileName.json").</param>
  public static void LoadFromResource(string resourceName) {
    try {
      var jsonContent = LoadResource(resourceName);

      if (string.IsNullOrEmpty(jsonContent)) {
        Log.Warning($"[Localizer] No content found in resource: {resourceName}");
        return;
      }

      var deserialized = JsonSerializer.Deserialize<IDictionary<string, IDictionary<Language, string>>>(jsonContent);

      LoadKeys(deserialized);

      Log.Message($"[Localizer] Loaded additional translations from resource: {resourceName}");
    } catch (Exception ex) {
      Log.Error($"[Localizer] Error loading translations from resource '{resourceName}': {ex}");
    }
  }

  /// <summary>
  /// Loads additional translations from a JSON file.
  /// The JSON must be in the format: { key: { languageEnum: text } }.
  /// </summary>
  /// <param name="path">The file path to load (e.g. "C:\Translations\custom.json").</param>
  public static void LoadFromFile(string path) {
    try {
      if (string.IsNullOrWhiteSpace(path)) {
        Log.Warning("[Localizer] File path cannot be null or empty");
        return;
      }

      if (!File.Exists(path)) {
        Log.Warning($"[Localizer] Translation file not found: {path}");
        return;
      }

      var jsonContent = File.ReadAllText(path);

      if (string.IsNullOrEmpty(jsonContent)) {
        Log.Warning($"[Localizer] No content found in file: {path}");
        return;
      }

      var deserialized = JsonSerializer.Deserialize<IDictionary<string, IDictionary<Language, string>>>(jsonContent);

      LoadKeys(deserialized);

      Log.Message($"[Localizer] Loaded additional translations from file: {path}");
    } catch (Exception ex) {
      Log.Error($"[Localizer] Error loading translations from file '{path}': {ex}");
    }
  }

  private static void LoadGameTranslations() {
    try {
      var resourceName = "ScarletCore.Localization.GameTranslations.json";
      var jsonContent = LoadResource(resourceName);

      if (string.IsNullOrEmpty(jsonContent)) {
        Log.Warning($"[Localizer] No content found in resource: {resourceName}");
        return;
      }

      var deserialized = JsonSerializer.Deserialize<IDictionary<string, IDictionary<Language, string>>>(jsonContent);

      _allTranslations.Clear();

      LoadKeys(deserialized);

      Log.Message($"[Localizer] Loaded {_allTranslations.Count} game translation keys");
    } catch (Exception ex) {
      Log.Error($"[Localizer] Error loading game translations: {ex}");
    }
  }

  private static string LoadResource(string resourceName) {
    var assembly = Assembly.GetExecutingAssembly();
    var stream = assembly.GetManifestResourceStream(resourceName);
    string jsonContent = null;
    if (stream != null) {
      using var reader = new StreamReader(stream);
      jsonContent = reader.ReadToEnd();

    } else {
      Log.Error($"[Localizer] Resource '{resourceName}' not found in assembly '{assembly.FullName}'");
    }

    return jsonContent;
  }

  /// <summary>
  /// Load prefab to GUID mapping from embedded resources
  /// </summary>
  private static void LoadPrefabMapping() {
    try {
      var assembly = typeof(Localizer).Assembly;
      var resourceName = "ScarletCore.Localization.PrefabToGuidMap.json";

      using var stream = assembly.GetManifestResourceStream(resourceName);
      if (stream == null) {
        Log.Warning("[Localizer] PrefabToGuidMap.json not found in embedded resources");
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

      Log.Message($"[Localizer] Loaded {_prefabToGuid.Count} prefab mappings");
    } catch (Exception ex) {
      Log.Error($"[Localizer] Error loading prefab mapping: {ex}");
    }
  }

  /// <summary>
  /// Formats a localized string with the provided parameters.
  /// </summary>
  private static string FormatString(string text, params object[] parameters) {
    if (parameters == null || parameters.Length == 0) return text;

    try {
      return string.Format(text, parameters);
    } catch (FormatException ex) {
      Log.Warning($"[Localizer] Failed to format localized string: {ex.Message}");
      return text;
    }
  }

  /// <summary>
  /// Get localized text by GUID string for a specific language.
  /// </summary>
  private static string GetTextForLanguage(string guid, Language language, params object[] parameters) {
    if (string.IsNullOrEmpty(guid)) return string.Empty;

    if (_allTranslations.TryGetValue(guid, out var translations)) {
      if (translations.TryGetValue(language, out var text) && !string.IsNullOrEmpty(text)) {
        return FormatString(text, parameters);
      }

      // Fallback to server language
      if (language != _currentServerLanguage && translations.TryGetValue(_currentServerLanguage, out var serverText) && !string.IsNullOrEmpty(serverText)) {
        return FormatString(serverText, parameters);
      }

      // Last resort: return first available translation
      var first = translations.Values.FirstOrDefault(v => !string.IsNullOrEmpty(v));
      if (first != null) return FormatString(first, parameters);
    }

    return guid;
  }

  /// <summary>
  /// Get localized text by GUID string using server language.
  /// </summary>
  public static string GetText(string guid, params object[] parameters) {
    if (!_initialized) Initialize();
    return GetTextForLanguage(guid, _currentServerLanguage, parameters);
  }

  /// <summary>
  /// Get localized text by PrefabGUID using server language.
  /// </summary>
  public static string GetText(PrefabGUID prefabGuid, params object[] parameters) {
    if (!_initialized) Initialize();

    if (_prefabToGuid.TryGetValue(prefabGuid.GuidHash, out var guid)) {
      return GetTextForLanguage(guid, _currentServerLanguage, parameters);
    }

    return prefabGuid.ToString();
  }

  /// <summary>
  /// Get localized text by PrefabGUID using specified language.
  /// </summary>
  public static string GetText(Language language, PrefabGUID prefabGuid, params object[] parameters) {
    if (!_initialized) Initialize();

    if (_prefabToGuid.TryGetValue(prefabGuid.GuidHash, out var guid)) {
      return GetTextForLanguage(guid, language, parameters);
    }

    return prefabGuid.ToString();
  }

  /// <summary>
  /// Get prefab name (alias for GetText).
  /// </summary>
  public static string GetPrefabName(PrefabGUID prefabGuid, params object[] parameters) {
    return GetText(prefabGuid, parameters);
  }


  /// <summary>
  /// Get prefab name (alias for GetText).
  /// </summary>
  public static string GetPrefabName(Language language, PrefabGUID prefabGuid, params object[] parameters) {
    return GetText(language, prefabGuid, parameters);
  }

  /// <summary>
  /// Gets the calling assembly from the stack trace, skipping LocalizationService methods.
  /// </summary>
  private static Assembly GetCallingAssemblyFromStack() {
    var stackTrace = new System.Diagnostics.StackTrace();
    var frames = stackTrace.GetFrames();

    if (frames == null) return Assembly.GetCallingAssembly();

    foreach (var frame in frames) {
      var method = frame.GetMethod();
      if (method == null) continue;

      var declaringType = method.DeclaringType;
      if (declaringType == null) continue;

      if (declaringType == typeof(Localizer)) continue;

      return declaringType.Assembly;
    }

    return Assembly.GetCallingAssembly();
  }

  /// <summary>
  /// Builds a composite key from assembly name and key string.
  /// </summary>
  private static string BuildCompositeKey(Assembly assembly, string key) {
    var assemblyName = assembly.GetName().Name;
    return $"{assemblyName}:{key}";
  }

  /// <summary>
  /// Register a new custom localization key with translations for multiple languages.
  /// </summary>
  public static void NewKey(string key, IDictionary<Language, string> translations) {
    if (string.IsNullOrWhiteSpace(key) || translations == null || translations.Count == 0) return;

    var callingAssembly = GetCallingAssemblyFromStack();
    var compositeKey = BuildCompositeKey(callingAssembly, key.Trim());

    var map = new ConcurrentDictionary<Language, string>();

    foreach (var kv in translations) {
      if (kv.Value == null) continue;
      map[kv.Key] = kv.Value;
    }

    if (map.IsEmpty) return;

    _allTranslations[compositeKey] = map;
  }

  /// <summary>
  /// Loads multiple custom keys from a dictionary organized by key -> (language -> text).
  /// </summary>
  public static void LoadKeys(IDictionary<string, IDictionary<Language, string>> languageMap) {
    if (languageMap == null || languageMap.Count == 0) return;

    var callingAssembly = GetCallingAssemblyFromStack();

    foreach (var kvp in languageMap) {
      var key = kvp.Key;
      var translations = kvp.Value;

      if (string.IsNullOrWhiteSpace(key) || translations == null || translations.Count == 0) continue;

      var compositeKey = BuildCompositeKey(callingAssembly, key.Trim());
      var map = new ConcurrentDictionary<Language, string>();

      foreach (var langKv in translations) {
        if (langKv.Value == null) continue;
        map[langKv.Key] = langKv.Value;
      }

      if (!map.IsEmpty) {
        _allTranslations[compositeKey] = map;
      }
    }
  }

  /// <summary>
  /// Get a localized string for a player from a custom key.
  /// </summary>
  public static string Get(PlayerData player, string key, params object[] parameters) {
    if (string.IsNullOrWhiteSpace(key)) return string.Empty;
    if (!_initialized) Initialize();

    var callingAssembly = GetCallingAssemblyFromStack();
    var compositeKey = BuildCompositeKey(callingAssembly, key.Trim());

    var playerLang = GetPlayerLanguage(player);
    if (playerLang == Language.None) {
      playerLang = _currentServerLanguage;
    }

    return GetTextForLanguage(compositeKey, playerLang, parameters);
  }

  /// <summary>
  /// Get a localized string for a player for a specific assembly.
  /// </summary>
  public static string Get(PlayerData player, string key, Assembly assembly, params object[] parameters) {
    if (string.IsNullOrWhiteSpace(key)) return string.Empty;
    if (!_initialized) Initialize();

    var useAssembly = assembly ?? GetCallingAssemblyFromStack();
    var compositeKey = BuildCompositeKey(useAssembly, key.Trim());

    var playerLang = GetPlayerLanguage(player);
    if (playerLang == Language.None) {
      playerLang = _currentServerLanguage;
    }

    return GetTextForLanguage(compositeKey, playerLang, parameters);
  }

  /// <summary>
  /// Get a localized string using the server language.
  /// </summary>
  public static string GetServer(string key, params object[] parameters) {
    if (string.IsNullOrWhiteSpace(key)) return string.Empty;
    if (!_initialized) Initialize();

    var callingAssembly = GetCallingAssemblyFromStack();
    var compositeKey = BuildCompositeKey(callingAssembly, key.Trim());

    return GetTextForLanguage(compositeKey, _currentServerLanguage, parameters);
  }

  /// <summary>
  /// Get a localized string using the server language for a specific assembly.
  /// </summary>
  public static string GetServer(string key, Assembly assembly, params object[] parameters) {
    if (string.IsNullOrWhiteSpace(key)) return string.Empty;
    if (!_initialized) Initialize();

    var useAssembly = assembly ?? GetCallingAssemblyFromStack();
    var compositeKey = BuildCompositeKey(useAssembly, key.Trim());

    return GetTextForLanguage(compositeKey, _currentServerLanguage, parameters);
  }

  /// <summary>
  /// Get a localized string directly by composite key.
  /// </summary>
  public static string GetByCompositeKey(PlayerData player, string compositeKey, params object[] parameters) {
    if (string.IsNullOrWhiteSpace(compositeKey)) return string.Empty;
    if (!_initialized) Initialize();

    var playerLang = GetPlayerLanguage(player);
    if (playerLang == Language.None) {
      playerLang = _currentServerLanguage;
    }

    return GetTextForLanguage(compositeKey, playerLang, parameters);
  }

  /// <summary>
  /// Check if a custom key exists for the calling assembly.
  /// </summary>
  public static bool HasCustomKey(string key) {
    if (string.IsNullOrWhiteSpace(key)) return false;
    if (!_initialized) Initialize();

    var callingAssembly = GetCallingAssemblyFromStack();
    var compositeKey = BuildCompositeKey(callingAssembly, key.Trim());
    return _allTranslations.ContainsKey(compositeKey);
  }

  /// <summary>
  /// Unregister all custom localization keys for the given assembly.
  /// </summary>
  public static int Dispose(Assembly assembly = null) {
    if (!_initialized) Initialize();

    assembly ??= GetCallingAssemblyFromStack();
    var assemblyName = assembly.GetName().Name;
    var prefix = $"{assemblyName}:";

    var keys = _allTranslations.Keys
      .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
      .ToList();

    var removed = 0;
    foreach (var k in keys) {
      if (_allTranslations.TryRemove(k, out _)) removed++;
    }

    Log.Message($"[Localizer] Disposed {removed} localization keys for assembly: {assemblyName}");
    return removed;
  }

  /// <summary>
  /// Set a player's preferred language.
  /// </summary>
  public static void SetPlayerLanguage(PlayerData player, Language language) {
    if (player == null) return;

    try {
      var map = Plugin.Database.GetOrCreate("player_languages", () => new Dictionary<string, string>());
      var key = player.PlatformId.ToString();
      map[key] = language.ToString();
      Plugin.Database.Set("player_languages", map);
    } catch (Exception ex) {
      Log.Error($"[Localizer] Failed to set player language: {ex}");
    }
  }

  /// <summary>
  /// Get a player's preferred language if set, otherwise Language.None.
  /// </summary>
  public static Language GetPlayerLanguage(PlayerData player) {
    if (player == null) return Language.None;
    try {
      var map = Plugin.Database.GetOrCreate<Dictionary<string, string>>("player_languages");

      if (map == null) return Language.None;

      var key = player.PlatformId.ToString();
      if (map.TryGetValue(key, out var langString) && !string.IsNullOrWhiteSpace(langString)) {
        return GetLanguageFromString(langString);
      }
      return Language.None;
    } catch (Exception ex) {
      Log.Error($"[Localizer] Failed to get player language: {ex}");
      return Language.None;
    }
  }

  /// <summary>
  /// Check if a translation exists for the given GUID.
  /// </summary>
  public static bool HasTranslation(string guid) {
    if (!_initialized) Initialize();
    return !string.IsNullOrEmpty(guid) && _allTranslations.ContainsKey(guid);
  }

  /// <summary>
  /// Check if a translation exists for the given PrefabGUID.
  /// </summary>
  public static bool HasTranslation(PrefabGUID prefabGuid) {
    if (!_initialized) Initialize();
    return _prefabToGuid.ContainsKey(prefabGuid.GuidHash);
  }

  /// <summary>
  /// Get the localization GUID string for a PrefabGUID.
  /// </summary>
  public static string GetGuidForPrefab(PrefabGUID prefabGuid) {
    if (!_initialized) Initialize();
    return _prefabToGuid.TryGetValue(prefabGuid.GuidHash, out var guid) ? guid : null;
  }

  /// <summary>
  /// Search translations by text content in the current server language.
  /// </summary>
  public static IEnumerable<KeyValuePair<string, string>> SearchTranslations(string searchText) {
    if (!_initialized) Initialize();
    if (string.IsNullOrEmpty(searchText)) return [];

    return _allTranslations
      .Where(kvp => kvp.Value.TryGetValue(_currentServerLanguage, out var text) &&
                    text.Contains(searchText, StringComparison.OrdinalIgnoreCase))
      .Select(kvp => new KeyValuePair<string, string>(kvp.Key, kvp.Value[_currentServerLanguage]));
  }

  /// <summary>
  /// Get total count of loaded translation keys
  /// </summary>
  public static int TranslationCount => _allTranslations.Count;

  /// <summary>
  /// Get total count of prefab mappings
  /// </summary>
  public static int PrefabMappingCount => _prefabToGuid.Count;

  /// <summary>
  /// Get total count of custom localization keys across all assemblies
  /// </summary>
  public static int CustomKeyCount => _allTranslations.Keys.Count(k => k.Contains(':'));

  /// <summary>
  /// Change the current language at runtime.
  /// </summary>
  public static bool ChangeLanguage(Language language) {
    if (!_languages.Contains(language)) {
      Log.Warning($"[Localizer] Language not supported: {language}");
      return false;
    }

    _currentServerLanguage = language;
    Log.Message($"[Localizer] Changed server language to: {language}");
    return true;
  }
}