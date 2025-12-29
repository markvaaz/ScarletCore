using System;
using HarmonyLib;
using ProjectM;
using ScarletCore.Events;
using ScarletCore.Systems;
using ScarletCore.Utils;
using Unity.Collections;
using Unity.Entities;

namespace ScarletCore.Patches;

[HarmonyPatch]
internal static class ShapeshiftSystemPatch {
  [HarmonyPatch(typeof(ShapeshiftSystem), nameof(ShapeshiftSystem.OnUpdate))]
  [HarmonyPrefix]
  static void Prefix(ShapeshiftSystem __instance) {
    if (!GameSystems.Initialized) return;
    // Early exit if no subscribers
    if (EventManager.GetSubscriberCount(PrefixEvents.OnShapeshift) == 0) return;
    NativeArray<Entity> query = __instance._Query.ToEntityArray(Allocator.Temp);

    try {
      if (query.Length == 0) return;
      EventManager.Emit(PrefixEvents.OnShapeshift, query);
    } catch (Exception ex) {
      Log.Error($"Error processing ShapeshiftSystem: {ex}");
    } finally {
      query.Dispose();
    }
  }

  [HarmonyPatch(typeof(ShapeshiftSystem), nameof(ShapeshiftSystem.OnUpdate))]
  [HarmonyPostfix]
  static void Postfix(ShapeshiftSystem __instance) {
    if (!GameSystems.Initialized) return;
    // Early exit if no subscribers
    if (EventManager.GetSubscriberCount(PostfixEvents.OnShapeshift) == 0) return;
    NativeArray<Entity> query = __instance._Query.ToEntityArray(Allocator.Temp);

    try {
      if (query.Length == 0) return;
      EventManager.Emit(PostfixEvents.OnShapeshift, query);
    } catch (Exception ex) {
      Log.Error($"Error processing ShapeshiftSystem: {ex}");
    } finally {
      query.Dispose();
    }
  }
}

