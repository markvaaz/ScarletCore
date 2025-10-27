using System;
using HarmonyLib;
using ProjectM;
using ScarletCore.Events;
using ScarletCore.Systems;
using ScarletCore.Utils;
using Unity.Collections;

namespace ScarletCore.Patches;

[HarmonyPatch]
public class Destroy_TravelBuffSystem_Patch {
  [HarmonyPatch(typeof(Destroy_TravelBuffSystem), nameof(Destroy_TravelBuffSystem.OnUpdate))]
  [HarmonyPrefix]
  private static void Prefix(Destroy_TravelBuffSystem __instance) {
    if (!GameSystems.Initialized) return;
    // Early exit if no subscribers
    if (EventManager.GetSubscriberCount(PrefixEvents.OnDestroyTravelBuff) == 0) return;
    var query = __instance.__query_615927226_0.ToEntityArray(Allocator.Temp);
    try {
      if (query.Length == 0) return;
      EventManager.Emit(PrefixEvents.OnDestroyTravelBuff, query);
    } catch (Exception ex) {
      Log.Error($"Error processing Destroy_TravelBuffSystem: {ex}");
    } finally {
      query.Dispose();
    }
  }

  [HarmonyPatch(typeof(Destroy_TravelBuffSystem), nameof(Destroy_TravelBuffSystem.OnUpdate))]
  [HarmonyPostfix]
  private static void Postfix(Destroy_TravelBuffSystem __instance) {
    if (!GameSystems.Initialized) return;
    // Early exit if no subscribers
    if (EventManager.GetSubscriberCount(PostfixEvents.OnDestroyTravelBuff) == 0) return;
    var query = __instance.__query_615927226_0.ToEntityArray(Allocator.Temp);
    try {
      if (query.Length == 0) return;
      EventManager.Emit(PostfixEvents.OnDestroyTravelBuff, query);
    } catch (Exception ex) {
      Log.Error($"Error processing Destroy_TravelBuffSystem: {ex}");
    } finally {
      query.Dispose();
    }
  }
}