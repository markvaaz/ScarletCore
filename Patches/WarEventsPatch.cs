using System;
using HarmonyLib;
using ProjectM.Gameplay.WarEvents;
using ScarletCore.Events;
using ScarletCore.Systems;
using ScarletCore.Utils;
using Unity.Collections;

namespace ScarletCore.Patches;

[HarmonyPatch]
internal static class WarEventSystemPatch {
  [HarmonyPatch(typeof(WarEventSystem), nameof(WarEventSystem.OnUpdate))]
  [HarmonyPrefix]
  public static void Prefix(WarEventSystem __instance) {
    if (!GameSystems.Initialized) return;
    // Early exit if no subscribers
    if (EventManager.GetSubscriberCount(PrefixEvents.OnWarEvent) == 0) return;
    var activeWarEvents = __instance.__query_303314001_0.ToEntityArray(Allocator.Temp);

    try {
      if (activeWarEvents.Length == 0) return;

      EventManager.Emit(PrefixEvents.OnWarEvent, activeWarEvents);
    } catch (Exception ex) {
      // Log any errors that occur during war event processing
      // Don't let exceptions crash the main game loop
      Log.Error($"Error processing WarEventSystem: {ex}");
    } finally {
      // Always dispose the native array to prevent memory leaks
      activeWarEvents.Dispose();
    }
  }

  [HarmonyPatch(typeof(WarEventSystem), nameof(WarEventSystem.OnUpdate))]
  [HarmonyPostfix]
  public static void Postfix(WarEventSystem __instance) {
    if (!GameSystems.Initialized) return;
    // Early exit if no subscribers
    if (EventManager.GetSubscriberCount(PostfixEvents.OnWarEvent) == 0) return;
    var activeWarEvents = __instance.__query_303314001_0.ToEntityArray(Allocator.Temp);

    try {
      if (activeWarEvents.Length == 0) return;

      EventManager.Emit(PostfixEvents.OnWarEvent, activeWarEvents);
    } catch (Exception ex) {
      // Log any errors that occur during war event processing
      // Don't let exceptions crash the main game loop
      Log.Error($"Error processing WarEventSystem: {ex}");
    } finally {
      // Always dispose the native array to prevent memory leaks
      activeWarEvents.Dispose();
    }
  }
}