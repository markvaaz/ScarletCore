using HarmonyLib;
using ProjectM;
using ScarletCore.Events;
using ScarletCore.Systems;
using ScarletCore.Utils;
using Unity.Collections;

namespace ScarletCore.Patches;

[HarmonyPatch]
public static class VampireDownedServerEventSystemPatch {
  [HarmonyPatch(typeof(VampireDownedServerEventSystem), nameof(VampireDownedServerEventSystem.OnUpdate))]
  [HarmonyPrefix]
  public static void Prefix(VampireDownedServerEventSystem __instance) {
    if (!GameSystems.Initialized) return;
    // Early exit if no subscribers
    if (EventManager.GetSubscriberCount(PrefixEvents.OnPlayerDowned) == 0) return;
    // Always allocate the array to check actual downed entities
    var downedQuery = __instance.__query_1174204813_0.ToEntityArray(Allocator.Temp);

    try {
      if (downedQuery.Length == 0) return;
      EventManager.Emit(PrefixEvents.OnPlayerDowned, downedQuery);
    } catch (System.Exception ex) {
      Log.Error($"Error processing VampireDownedServerEventSystem: {ex}");
    } finally {
      // Always dispose the native array to prevent memory leaks
      downedQuery.Dispose();
    }
  }

  [HarmonyPatch(typeof(VampireDownedServerEventSystem), nameof(VampireDownedServerEventSystem.OnUpdate))]
  [HarmonyPostfix]
  public static void Postfix(VampireDownedServerEventSystem __instance) {
    if (!GameSystems.Initialized) return;
    // Early exit if no subscribers
    if (EventManager.GetSubscriberCount(PostfixEvents.OnPlayerDowned) == 0) return;
    // Always allocate the array to check actual downed entities
    var downedQuery = __instance.__query_1174204813_0.ToEntityArray(Allocator.Temp);

    try {
      if (downedQuery.Length == 0) return;
      EventManager.Emit(PostfixEvents.OnPlayerDowned, downedQuery);
    } catch (System.Exception ex) {
      Log.Error($"Error processing VampireDownedServerEventSystem: {ex}");
    } finally {
      // Always dispose the native array to prevent memory leaks
      downedQuery.Dispose();
    }
  }
}
