using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using ScarletCore.Commanding;
using ScarletCore.Data;
using ScarletCore.Localization;

namespace ScarletCore;

/// <summary>
/// Main entry point for the ScarletCore BepInEx plugin.
/// </summary>
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin {
  static Harmony _harmony;
  /// <summary>
  /// Gets the Harmony instance used for patching.
  /// </summary>
  public static Harmony Harmony => _harmony;
  /// <summary>
  /// Gets the singleton instance of the plugin.
  /// </summary>
  public static Plugin Instance { get; private set; }
  /// <summary>
  /// Gets the log source for plugin logging.
  /// </summary>
  public static ManualLogSource LogInstance { get; private set; }
  /// <summary>
  /// Gets the plugin settings instance.
  /// </summary>
  public static Settings Settings { get; private set; }
  /// <summary>
  /// Gets the plugin database instance.
  /// </summary>
  public static Database Database { get; private set; }

  /// <summary>
  /// Called by BepInEx to load and initialize the plugin.
  /// </summary>
  public override void Load() {
    Instance = this;
    LogInstance = Log;
    Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} version {MyPluginInfo.PLUGIN_VERSION} is loaded!");

    _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
    _harmony.PatchAll(System.Reflection.Assembly.GetExecutingAssembly());

    Settings = new Settings(MyPluginInfo.PLUGIN_NAME, Instance);
    Database = new Database(MyPluginInfo.PLUGIN_NAME);

    Settings.Section("Language")
      .Add("PrefabLocalizationLanguage", Language.English, $"Language code for localization. Available languages: {string.Join(", ", Localizer.AvailableServerLanguages)}")
      .Add("DefaultPlayerLanguage", Language.English, $"Default language code for new players. Available languages: {string.Join(", ", Localizer.AvailableServerLanguages)}")
      .Add("WelcomeMessage", "~Welcome to {ServerName}!~\n\nThis server uses ~ScarletMods~ to enhance your experience.\n\nTo get started, please set ~your preferred language~:\n\n{AvailableLanguages}\nUse: ~.language <language>~", "Welcome message shown to players who haven't set their language. Placeholders: {ServerName}, {AvailableLanguages}, {PlayerName}");
    Localizer.Initialize();
    CommandHandler.Initialize();
  }

  /// <summary>
  /// Called by BepInEx to unload and clean up the plugin.
  /// </summary>
  /// <returns>True if the plugin was unloaded successfully.</returns>
  public override bool Unload() {
    _harmony?.UnpatchSelf();
    SharedDatabase.Shutdown();
    return true;
  }
}
