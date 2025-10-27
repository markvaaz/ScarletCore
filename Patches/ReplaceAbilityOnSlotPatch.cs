

using HarmonyLib;
using ProjectM;
using ScarletCore.Events;
using ScarletCore.Systems;
using Unity.Collections;
using Unity.Entities;

namespace ScarletCore.Patches;

[HarmonyPatch]
internal static class ReplaceAbilityOnSlotSystemPatch {
  [HarmonyPatch(typeof(ReplaceAbilityOnSlotSystem), nameof(ReplaceAbilityOnSlotSystem.OnUpdate))]
  [HarmonyPrefix]
  static void Prefix(ReplaceAbilityOnSlotSystem __instance) {
    if (!GameSystems.Initialized) return;

    // Early exit if no subscribers
    if (EventManager.GetSubscriberCount(PrefixEvents.OnReplaceAbilityOnSlot) == 0) return;
    NativeArray<Entity> entities = __instance.__query_1482480545_0.ToEntityArray(Allocator.Temp);

    try {
      if (entities.Length == 0) return;
      EventManager.Emit(PrefixEvents.OnReplaceAbilityOnSlot, entities);
    } finally {
      entities.Dispose();
    }
  }

  [HarmonyPatch(typeof(ReplaceAbilityOnSlotSystem), nameof(ReplaceAbilityOnSlotSystem.OnUpdate))]
  [HarmonyPostfix]
  static void Postfix(ReplaceAbilityOnSlotSystem __instance) {
    if (!GameSystems.Initialized) return;

    // Early exit if no subscribers
    if (EventManager.GetSubscriberCount(PostfixEvents.OnReplaceAbilityOnSlot) == 0) return;
    NativeArray<Entity> entities = __instance.__query_1482480545_0.ToEntityArray(Allocator.Temp);

    try {
      if (entities.Length == 0) return;
      EventManager.Emit(PostfixEvents.OnReplaceAbilityOnSlot, entities);
    } finally {
      entities.Dispose();
    }
  }
}