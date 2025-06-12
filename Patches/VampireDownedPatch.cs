using HarmonyLib;
using ProjectM;
using ScarletCore.Events;
using ScarletCore.Utils;
using Unity.Collections;

namespace ScarletCore.Patches {
  [HarmonyPatch(typeof(VampireDownedServerEventSystem), nameof(VampireDownedServerEventSystem.OnUpdate))]
  public static class VampireDownedServerEventSystemPatch {
    public static bool Prefix(VampireDownedServerEventSystem __instance) {
      // Early exit optimization - skip processing if no mods are listening
      if (EventManager.PlayerDownedSubscriberCount == 0) return true;

      // Always allocate the array to check actual downed entities
      var downedQuery = __instance.__query_1174204813_0.ToEntityArray(Allocator.Temp);

      // Early exit if no downed entities this frame
      if (downedQuery.Length == 0) {
        downedQuery.Dispose(); // Always dispose temp allocations
        return true;
      }

      try {
        EventManager.InvokeVampireDowned(downedQuery, __instance);
      } catch (System.Exception ex) {
        Log.Error($"Error processing VampireDownedServerEventSystem: {ex}");
      } finally {
        // Always dispose the native array to prevent memory leaks
        downedQuery.Dispose();
      }

      return true;
    }
  }
}