using System;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using ScarletCore.Data;
using ScarletCore.Services;
using ScarletCore.Systems;

namespace ScarletCore;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin {
  static Harmony _harmony;
  public static Harmony Harmony => _harmony;
  public static Plugin Instance { get; private set; }
  public static ManualLogSource LogInstance { get; private set; }
  public static Settings Settings { get; private set; }

  public override void Load() {
    Instance = this;
    LogInstance = Log;
    Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} version {MyPluginInfo.PLUGIN_VERSION} is loaded!");

    _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
    _harmony.PatchAll(System.Reflection.Assembly.GetExecutingAssembly());

    Settings = new Settings(MyPluginInfo.PLUGIN_GUID, Instance);

    Settings.Section("Language")
      .Add("language", "english", $"Language code for localization. Available languages: {string.Join(", ", LocalizationService.AvailableLanguages)}");

    GameSystems.OnInitialize(LocalizationService.Initialize);
  }

  public override bool Unload() {
    _harmony?.UnpatchSelf();
    return true;
  }
}
