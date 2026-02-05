using HarmonyLib;
using ScarletCore.Systems;
using ScarletCore.Utils;
using Unity.Entities;

namespace ScarletCore.Patches;

[HarmonyPatch]
internal class PerformanceRecorderSystemPatch {

  [HarmonyPatch(typeof(PerformanceRecorderSystem), nameof(PerformanceRecorderSystem.OnUpdate))]
  [HarmonyPrefix]
  static bool Prefix() {
    if (!GameSystems.Initialized || ActionScheduler.Count == 0) return false;

    try {
      ActionScheduler.Execute();
    } catch (System.Exception ex) {
      Log.Error($"ActionScheduler execution failed: {ex}");
    }

    return false;
  }
}
