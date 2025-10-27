using System;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ScarletCore.Events;
using ScarletCore.Services;
using ScarletCore.Systems;
using ScarletCore.Utils;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace ScarletCore.Patches;

[HarmonyPatch]
public static class WaypointPatch {
  [HarmonyPatch(typeof(TeleportToWaypointEventSystem), nameof(TeleportToWaypointEventSystem.OnUpdate))]
  [HarmonyPrefix]
  public static void Prefix(TeleportToWaypointEventSystem __instance) {
    if (!GameSystems.Initialized) return;
    // Early exit if no subscribers
    if (EventManager.GetSubscriberCount(PrefixEvents.OnWaypointTeleport) == 0) return;
    var query = __instance.__query_1956534509_0.ToEntityArray(Allocator.Temp);

    try {
      if (query.Length == 0) return;
      EventManager.Emit(PrefixEvents.OnWaypointTeleport, query);
    } catch (Exception ex) {
      Log.Error($"Error processing TeleportToWaypointEventSystem: {ex}");
    } finally {
      query.Dispose();
    }
  }

  [HarmonyPatch(typeof(TeleportToWaypointEventSystem), nameof(TeleportToWaypointEventSystem.OnUpdate))]
  [HarmonyPostfix]
  public static void Postfix(TeleportToWaypointEventSystem __instance) {
    if (!GameSystems.Initialized) return;
    // Early exit if no subscribers
    if (EventManager.GetSubscriberCount(PostfixEvents.OnWaypointTeleport) == 0) return;
    var query = __instance.__query_1956534509_0.ToEntityArray(Allocator.Temp);

    try {
      if (query.Length == 0) return;
      EventManager.Emit(PostfixEvents.OnWaypointTeleport, query);
    } catch (Exception ex) {
      Log.Error($"Error processing TeleportToWaypointEventSystem: {ex}");
    } finally {
      query.Dispose();
    }
  }
}