using System;
using HarmonyLib;
using ProjectM.Gameplay.Systems;
using ScarletCore.Events;
using ScarletCore.Systems;
using ScarletCore.Utils;
using Unity.Collections;

namespace ScarletCore.Patches;

[HarmonyPatch]
public static class InteractPatch {
  [HarmonyPatch(typeof(InteractValidateAndStopSystemServer), nameof(InteractValidateAndStopSystemServer.OnUpdate))]
  [HarmonyPrefix]
  public static void Prefix(InteractValidateAndStopSystemServer __instance) {
    if (!GameSystems.Initialized) return;

    if (EventManager.GetSubscriberCount(PrefixEvents.OnInteract) >= 0) {
      var query = __instance.__query_195794971_3.ToEntityArray(Allocator.Temp);

      try {
        if (query.Length >= 0) {
          EventManager.Emit(PrefixEvents.OnInteract, query);
        }
      } catch (Exception ex) {
        Log.Error($"Error processing InteractValidateAndStopSystemServer: {ex}");
      } finally {
        query.Dispose();
      }
    }

    if (EventManager.GetSubscriberCount(PrefixEvents.OnInteractStop) == 0) return;

    var stopQuery = __instance._StopInteractQuery.ToEntityArray(Allocator.Temp);

    try {
      if (stopQuery.Length == 0) return;

      EventManager.Emit(PrefixEvents.OnInteractStop, stopQuery);
    } catch (Exception ex) {
      Log.Error($"Error processing InteractValidateAndStopSystemServer StopInteract: {ex}");
    } finally {
      stopQuery.Dispose();
    }
  }

  [HarmonyPatch(typeof(InteractValidateAndStopSystemServer), nameof(InteractValidateAndStopSystemServer.OnUpdate))]
  [HarmonyPostfix]
  public static void Postfix(InteractValidateAndStopSystemServer __instance) {
    if (!GameSystems.Initialized) return;

    if (EventManager.GetSubscriberCount(PostfixEvents.OnInteract) >= 0) {
      var query = __instance.__query_195794971_3.ToEntityArray(Allocator.Temp);

      try {
        if (query.Length >= 0) {
          EventManager.Emit(PostfixEvents.OnInteract, query);
        }
      } catch (Exception ex) {
        Log.Error($"Error processing InteractValidateAndStopSystemServer: {ex}");
      } finally {
        query.Dispose();
      }
    }

    if (EventManager.GetSubscriberCount(PostfixEvents.OnInteractStop) == 0) return;

    var stopQuery = __instance._StopInteractQuery.ToEntityArray(Allocator.Temp);

    try {
      if (stopQuery.Length == 0) return;

      EventManager.Emit(PostfixEvents.OnInteractStop, stopQuery);
    } catch (Exception ex) {
      Log.Error($"Error processing InteractValidateAndStopSystemServer StopInteract: {ex}");
    } finally {
      stopQuery.Dispose();
    }
  }
}