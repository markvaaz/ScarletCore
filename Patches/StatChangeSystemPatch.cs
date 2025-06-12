using ProjectM.Gameplay.Systems;
using ProjectM;
using Unity.Entities;
using Unity.Collections;
using HarmonyLib;
using System;
using System.Collections.Generic;
using ScarletCore.Events;
using ScarletCore.Utils;
using ScarletCore.Systems;

namespace ScarletCore.Patches;

[HarmonyPatch(typeof(StatChangeSystem), nameof(StatChangeSystem.OnUpdate))]
internal class StatChangeSystemPatch {
  internal static bool Prefix(StatChangeSystem __instance) {
    // Early exit optimization - skip processing if no mods are listening to damage events
    // Returning true allows the original method to continue executing
    if (EventManager.DealDamageSubscriberCount == 0) return true;

    // Always allocate the array to check actual damage events
    var damageTakenEvent = __instance._DamageTakenEventQuery.ToComponentDataArray<DamageTakenEvent>(Allocator.Temp);

    // Early exit if no damage events occurred this frame
    if (damageTakenEvent.Length == 0) {
      damageTakenEvent.Dispose(); // Always dispose temp allocations
      return true;
    }

    try {
      // Pre-size the collection to avoid internal reallocations
      List<DamageInfo> allDamageTaken = new List<DamageInfo>(damageTakenEvent.Length);

      // Process each damage event from the ECS system
      foreach (var e in damageTakenEvent) {
        // Skip invalid damage events where source or target is null
        // This can happen during entity cleanup or invalid damage scenarios
        if (Entity.Null.Equals(e.Source) || Entity.Null.Equals(e.Entity)) {
          continue;
        }

        var attacker = e.Source; // The entity that dealt the damage
        var target = e.Entity;   // The entity that took the damage

        // Validate that both entities still exist in the world
        // Entities can be destroyed between frames, causing crashes if not checked
        if (!GameSystems.EntityManager.Exists(attacker)) {
          continue;
        }

        // Handle cases where the damage source is owned by another entity
        // For example: projectiles, pets, or other controlled entities
        // We want to attribute damage to the actual owner (usually a player)
        if (attacker.Has<EntityOwner>()) {
          var owner = attacker.Read<EntityOwner>().Owner;
          if (GameSystems.EntityManager.Exists(owner)) {
            attacker = owner; // Use the owner as the actual attacker
          }
        }

        // Double-check target still exists after potential attacker changes
        // This prevents edge cases where entity state changes during processing
        if (!GameSystems.EntityManager.Exists(target)) {
          continue;
        }

        // Create damage info object containing relevant damage event data
        var damageInfo = new DamageInfo(attacker, target);

        allDamageTaken.Add(damageInfo);
      }

      // Fire damage events for all subscribers if we have valid damage data
      if (allDamageTaken.Count > 0) {
        EventManager.InvokeDealDamage(allDamageTaken, __instance);
      }
    } catch (Exception ex) {
      // Log any errors that occur during damage event processing
      // Return true skips the original method, but we want it to run normally
      Log.Error($"Error processing DealDamageEvent: {ex}");
      return true;
    } finally {
      // Always dispose the damage events array to prevent memory leaks
      // Critical for Unity's native collections with Allocator.Temp
      damageTakenEvent.Dispose();
    }

    // Return true to allow the original StatChangeSystem.OnUpdate to execute
    // This ensures game functionality remains intact while adding our event system
    return true;
  }
}

