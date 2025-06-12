using System;
using System.Collections.Generic;
using HarmonyLib;
using ProjectM;
using ScarletCore.Events;
using ScarletCore.Utils;
using Unity.Collections;

namespace ScarletCore.Patches {
  [HarmonyPatch(typeof(DeathEventListenerSystem), nameof(DeathEventListenerSystem.OnUpdate))]
  public static class DeathEventListenerSystemPatch {

    public static void Postfix(DeathEventListenerSystem __instance) {
      // Early exit optimization - skip processing if no mods are listening to death events
      // This prevents unnecessary work when no subscribers are registered
      if (EventManager.AnyDeathSubscriberCount == 0 && EventManager.OtherDeathSubscriberCount == 0 &&
          EventManager.PlayerDeathSubscriberCount == 0 && EventManager.VBloodDeathSubscriberCount == 0 &&
          EventManager.ServantDeathSubscriberCount == 0) return;

      // Get all death events that occurred this frame from the ECS query
      // Using Allocator.Temp for short-lived temporary allocation
      var deathEvents = __instance._DeathEventQuery.ToComponentDataArray<DeathEvent>(Allocator.Temp);

      // Early exit if no deaths occurred this frame
      if (deathEvents.Length == 0) {
        deathEvents.Dispose(); // Always dispose temp allocations
        return;
      }

      try {
        // Create lists only if there are subscribers to avoid unnecessary allocations
        // This ensures data integrity when events use coroutines or ActionScheduler
        List<DeathInfo> otherDeathList = EventManager.OtherDeathSubscriberCount > 0 ? [] : null;
        List<DeathInfo> playerDeathList = EventManager.PlayerDeathSubscriberCount > 0 ? [] : null;
        List<DeathInfo> vBloodDeathList = EventManager.VBloodDeathSubscriberCount > 0 ? [] : null;
        List<DeathInfo> allDeaths = EventManager.AnyDeathSubscriberCount > 0 ? [] : null;
        List<DeathInfo> servantDeathList = EventManager.ServantDeathSubscriberCount > 0 ? [] : null;

        // Process each death event and categorize by entity type
        foreach (var e in deathEvents) {
          // Extract death event components for easier access
          var died = e.Died;     // The entity that died
          var killer = e.Killer; // The entity that caused the death (if any)
          var source = e.Source; // The damage source or cause of death          // Create death info object for this death event
          var deathInfo = new DeathInfo(died, killer, source);

          // Add to comprehensive death list (all deaths regardless of type)
          allDeaths?.Add(deathInfo);

          // Categorize death based on the type of entity that died
          // Order matters here - more specific checks first
          if (vBloodDeathList != null && died.Has<VBloodUnit>()) {
            // V Blood units are special boss-type enemies with unique rewards
            vBloodDeathList.Add(deathInfo);
          } else if (playerDeathList != null && died.Has<PlayerCharacter>()) {
            // Player character deaths (actual players, not NPCs)
            playerDeathList.Add(deathInfo);
          } else if (servantDeathList != null && died.Has<ServantData>()) {
            // Player-owned servants (converted NPCs)
            servantDeathList.Add(deathInfo);
          } else {
            // Everything else falls into the "other" category
            // This includes monsters, wildlife, neutral NPCs, etc.
            otherDeathList?.Add(deathInfo);
          }
        }        // Fire death events for subscribers using batch processing for optimal performance
        // Only fire events that have both subscribers and actual deaths to process

        // Fire comprehensive death event (all deaths)
        if (allDeaths?.Count > 0) {
          EventManager.InvokeAnyDeath(allDeaths, __instance);
        }

        // Fire other/NPC death events
        if (otherDeathList?.Count > 0) {
          EventManager.InvokeOtherDeath(otherDeathList, __instance);
        }

        // Fire player death events
        if (playerDeathList?.Count > 0) {
          EventManager.InvokePlayerDeath(playerDeathList, __instance);
        }

        // Fire V Blood boss death events
        if (vBloodDeathList?.Count > 0) {
          EventManager.InvokeVBloodDeath(vBloodDeathList, __instance);
        }

        // Fire servant death events
        if (servantDeathList?.Count > 0) {
          EventManager.InvokeServantDeath(servantDeathList, __instance);
        }
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
}