using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ScarletCore.Events;
using ScarletCore.Systems;
using ScarletCore.Utils;
using Unity.Collections;

namespace ScarletCore.Patches;

[HarmonyPatch]
internal static class InventoryPatches {
  [HarmonyPatch(typeof(ReactToInventoryChangedSystem), nameof(ReactToInventoryChangedSystem.OnUpdate))]
  [HarmonyPrefix]
  public static void Prefix(ReactToInventoryChangedSystem __instance) {
    if (!GameSystems.Initialized) return;
    // Early exit if no subscribers
    if (EventManager.GetSubscriberCount(PrefixEvents.OnInventoryChanged) == 0) return;
    var query = __instance.__query_2096870026_0.ToEntityArray(Allocator.Temp);

    try {
      if (query.Length == 0) return;
      EventManager.Emit(PrefixEvents.OnInventoryChanged, query);
    } catch (System.Exception ex) {
      Log.Error($"Error in ReactToInventoryChangedSystemPatch: {ex.Message}");
    } finally {
      query.Dispose();
    }
  }

  [HarmonyPatch(typeof(ReactToInventoryChangedSystem), nameof(ReactToInventoryChangedSystem.OnUpdate))]
  [HarmonyPostfix]
  public static void Postfix(ReactToInventoryChangedSystem __instance) {
    if (!GameSystems.Initialized) return;
    // Early exit if no subscribers
    if (EventManager.GetSubscriberCount(PostfixEvents.OnInventoryChanged) == 0) return;
    var query = __instance.__query_2096870026_0.ToEntityArray(Allocator.Temp);

    try {
      if (query.Length == 0) return;
      EventManager.Emit(PostfixEvents.OnInventoryChanged, query);
    } catch (System.Exception ex) {
      Log.Error($"Error in ReactToInventoryChangedSystemPatch: {ex.Message}");
    } finally {
      query.Dispose();
    }
  }
}
