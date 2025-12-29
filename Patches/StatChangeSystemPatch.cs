using ProjectM.Gameplay.Systems;
using Unity.Collections;
using HarmonyLib;
using System;
using ScarletCore.Events;
using ScarletCore.Utils;
using ScarletCore.Systems;

namespace ScarletCore.Patches;

[HarmonyPatch]
internal class StatChangeSystemPatch {
  [HarmonyPatch(typeof(StatChangeSystem), nameof(StatChangeSystem.OnUpdate))]
  [HarmonyPrefix]
  internal static void Prefix(StatChangeSystem __instance) {
    if (!GameSystems.Initialized) return;
    // Early exit if no listeners for damage events
    if (EventManager.GetSubscriberCount(PrefixEvents.OnDealDamage) == 0) return;
    var damageTakenEvent = __instance._DamageTakenEventQuery.ToEntityArray(Allocator.Temp);

    try {
      if (damageTakenEvent.Length == 0) return;
      EventManager.Emit(PrefixEvents.OnDealDamage, damageTakenEvent);
    } catch (Exception ex) {
      // Log any errors that occur during damage event processing
      // Return true skips the original method, but we want it to run normally
      Log.Error($"Error processing DealDamageEvent: {ex}");
    } finally {
      // Always dispose the damage events array to prevent memory leaks
      // Critical for Unity's native collections with Allocator.Temp
      damageTakenEvent.Dispose();
    }
  }

  [HarmonyPatch(typeof(StatChangeSystem), nameof(StatChangeSystem.OnUpdate))]
  [HarmonyPostfix]
  internal static void Postfix(StatChangeSystem __instance) {
    if (!GameSystems.Initialized) return;
    // Early exit if no listeners for damage events
    if (EventManager.GetSubscriberCount(PostfixEvents.OnDealDamage) == 0) return;
    var damageTakenEvent = __instance._DamageTakenEventQuery.ToEntityArray(Allocator.Temp);

    try {
      EventManager.Emit(PostfixEvents.OnDealDamage, damageTakenEvent);
    } catch (Exception ex) {
      // Log any errors that occur during damage event processing
      // Return true skips the original method, but we want it to run normally
      Log.Error($"Error processing DealDamageEvent: {ex}");
    } finally {
      // Always dispose the damage events array to prevent memory leaks
      // Critical for Unity's native collections with Allocator.Temp
      damageTakenEvent.Dispose();
    }
  }
}

