using System;
using System.Collections.Generic;
using HarmonyLib;
using ProjectM.Gameplay.WarEvents;
using ProjectM.Shared.WarEvents;
using ScarletCore.Events;
using ScarletCore.Utils;
using Unity.Collections;

namespace ScarletCore.Patches;

[HarmonyPatch(typeof(WarEventSystem), nameof(WarEventSystem.OnUpdate))]
public static class WarEventSystemPatch {
  // Static flag to track whether war events are currently active
  // Prevents firing duplicate start/end events when status hasn't changed
  private static bool isActive = false;
  [HarmonyPrefix]
  public static void Prefix(WarEventSystem __instance) {
    // Early exit optimization - skip processing if no mods are listening to war event changes
    if (EventManager.WarEventsStartedSubscriberCount == 0 && EventManager.WarEventsEndedSubscriberCount == 0) return;

    // Always allocate the array to check actual war events
    var activeWarEvents = __instance.__query_303314001_0.ToComponentDataArray<WarEvent>(Allocator.Temp);

    try {
      // Check if there are active war events based on array length
      bool hasActiveEvents = activeWarEvents.Length > 0;

      // State transition: Active -> Inactive
      if (isActive && !hasActiveEvents) {
        EventManager.InvokeWarEventsEnded(__instance);
        isActive = false;
        return;
      }

      // State transition: Inactive -> Active
      if (!isActive && hasActiveEvents) {
        isActive = true;

        // Create a list to hold war event data for subscribers
        var warEvents = new List<WarEvent>(activeWarEvents.Length);

        // Convert the native array to a managed list for easier handling
        foreach (var e in activeWarEvents) {
          warEvents.Add(e);
        }

        // Fire the war events started event with all active events
        EventManager.InvokeWarEventsStarted(warEvents, __instance);
      }
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