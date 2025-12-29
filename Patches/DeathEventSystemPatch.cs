using System;
using HarmonyLib;
using ProjectM;
using ScarletCore.Events;
using ScarletCore.Systems;
using ScarletCore.Utils;
using Unity.Collections;

namespace ScarletCore.Patches;

[HarmonyPatch]
internal static class DeathEventListenerSystemPatch {
  [HarmonyPatch(typeof(DeathEventListenerSystem), nameof(DeathEventListenerSystem.OnUpdate))]
  [HarmonyPrefix]
  public static void Prefix(DeathEventListenerSystem __instance) {
    if (!GameSystems.Initialized) return;
    // Early exit if there are no subscribers for death events
    if (EventManager.GetSubscriberCount(PrefixEvents.OnDeath) == 0) return;
    var deathEvents = __instance._DeathEventQuery.ToEntityArray(Allocator.Temp);

    try {
      if (deathEvents.Length == 0) return;
      EventManager.Emit(PrefixEvents.OnDeath, deathEvents);
    } catch (Exception ex) {
      // Log any exceptions that occur during death event processing
      // This prevents crashes in the main game loop if something goes wrong
      Log.Error($"Error processing death events: {ex}");
    } finally {
      // Always dispose the death events array to prevent memory leaks
      // This is crucial when working with Unity's NativeArray/Allocator.Temp
      // Failure to dispose can cause memory issues and crashes
      deathEvents.Dispose();
    }
  }

  [HarmonyPatch(typeof(DeathEventListenerSystem), nameof(DeathEventListenerSystem.OnUpdate))]
  [HarmonyPostfix]
  public static void Postfix(DeathEventListenerSystem __instance) {
    // Early exit if there are no subscribers for death events
    if (EventManager.GetSubscriberCount(PostfixEvents.OnDeath) == 0) return;
    var deathEvents = __instance._DeathEventQuery.ToEntityArray(Allocator.Temp);

    try {
      EventManager.Emit(PostfixEvents.OnDeath, deathEvents);
    } catch (Exception ex) {
      // Log any exceptions that occur during death event processing
      // This prevents crashes in the main game loop if something goes wrong
      Log.Error($"Error processing death events: {ex}");
    } finally {
      // Always dispose the death events array to prevent memory leaks
      // This is crucial when working with Unity's NativeArray/Allocator.Temp
      // Failure to dispose can cause memory issues and crashes
      deathEvents.Dispose();
    }
  }
}
