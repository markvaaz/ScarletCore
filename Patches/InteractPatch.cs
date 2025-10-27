using System;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Shared;
using ScarletCore.Events;
using ScarletCore.Services;
using ScarletCore.Systems;
using ScarletCore.Utils;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace ScarletCore.Patches;

public static class InteractPatch {
  [HarmonyPatch(typeof(InteractValidateAndStopSystemServer), nameof(InteractValidateAndStopSystemServer.OnUpdate))]
  [HarmonyPrefix]
  public static void Prefix(InteractValidateAndStopSystemServer __instance) {
    if (!GameSystems.Initialized) return;
    // Early exit if no subscribers to avoid allocating the array
    if (EventManager.GetSubscriberCount(PrefixEvents.OnInteract) == 0) return;
    var query = __instance.__query_195794971_3.ToEntityArray(Allocator.Temp);

    try {
      if (query.Length == 0) return;

      EventManager.Emit(PrefixEvents.OnInteract, query);
    } catch (Exception ex) {
      Log.Error($"Error processing InteractValidateAndStopSystemServer: {ex}");
    } finally {
      query.Dispose();
    }
  }

  [HarmonyPatch(typeof(InteractValidateAndStopSystemServer), nameof(InteractValidateAndStopSystemServer.OnUpdate))]
  [HarmonyPostfix]
  public static void Postfix(InteractValidateAndStopSystemServer __instance) {
    if (!GameSystems.Initialized) return;
    // Early exit if no subscribers to avoid allocating the array
    if (EventManager.GetSubscriberCount(PostfixEvents.OnInteract) == 0) return;
    var query = __instance.__query_195794971_3.ToEntityArray(Allocator.Temp);

    try {
      if (query.Length == 0) return;

      EventManager.Emit(PostfixEvents.OnInteract, query);
    } catch (Exception ex) {
      Log.Error($"Error processing InteractValidateAndStopSystemServer: {ex}");
    } finally {
      query.Dispose();
    }
  }
}