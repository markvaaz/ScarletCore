using System;
using System.Collections.Generic;
using HarmonyLib;
using ProjectM;
using ScarletCore.Events;
using ScarletCore.Systems;
using ScarletCore.Utils;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ScarletCore.Patches;

[HarmonyPatch]
internal class UnitSpawnerReactSystemPatch {
  internal static Dictionary<float, (float duration, float3 position, Action<Entity> action)> PostActions = [];

  [HarmonyPatch(typeof(UnitSpawnerReactSystem), nameof(UnitSpawnerReactSystem.OnUpdate))]
  [HarmonyPrefix]
  internal static void Prefix(UnitSpawnerReactSystem __instance) {
    if (!GameSystems.Initialized) return;
    // Early exit if no subscribers and no post action to process
    if (PostActions.Count == 0) return;

    // Get all entities that were spawned in this frame
    var entityQuery = __instance._Query.ToEntityArray(Allocator.Temp);

    try {
      // Process each spawned entity
      foreach (var entity in entityQuery) {
        // Skip post-action processing if no action are registered
        if (PostActions.Count == 0) continue;

        // if (!entity.Has<LifeTime>()) continue;

        // Post-spawn action processing
        // Check if this entity has a matching lifetime-based action
        var lifeTimeComponent = entity.Read<LifeTime>();
        var durationHash = Mathf.Round(lifeTimeComponent.Duration);

        if (PostActions.TryGetValue(durationHash, out var actionData)) {
          var duration = actionData.duration;
          var action = actionData.action;

          // Determine the appropriate end action based on duration
          LifeTimeEndAction endAction;
          if (duration == -1f) {
            endAction = LifeTimeEndAction.None; // Infinite duration - no automatic destruction
          } else {
            endAction = LifeTimeEndAction.Destroy; // Finite duration - destroy when expired
          }

          // Update the entity's lifetime component with the new duration and end action
          entity.With((ref LifeTime lt) => {
            lt.Duration = duration;
            lt.EndAction = endAction;
          });

          entity.SetPosition(actionData.position);

          // Execute the custom post-spawn action
          action?.Invoke(entity);
        }

        // Remove the action after processing to prevent duplicate execution
        PostActions.Remove(durationHash);
      }

      // Fire the unit spawn event for subscribers (batch processing for performance)
      if (EventManager.GetSubscriberCount(PrefixEvents.OnUnitSpawned) > 0) {
        EventManager.Emit(PrefixEvents.OnUnitSpawned, entityQuery);
      }
    } catch (Exception exception) {
      // Log any exceptions that occur during processing
      Log.Error(exception);
    } finally {
      // Always dispose the entity query to prevent memory leaks
      entityQuery.Dispose();
    }
  }

  [HarmonyPatch(typeof(UnitSpawnerReactSystem), nameof(UnitSpawnerReactSystem.OnUpdate))]
  [HarmonyPostfix]
  internal static void Postfix(UnitSpawnerReactSystem __instance) {
    if (!GameSystems.Initialized) return;
    // Get all entities that were spawned in this frame
    var entityQuery = __instance._Query.ToEntityArray(Allocator.Temp);

    try {
      if (entityQuery.Length == 0) return;
      if (EventManager.GetSubscriberCount(PostfixEvents.OnUnitSpawned) == 0) return;
      EventManager.Emit(PostfixEvents.OnUnitSpawned, entityQuery);
    } catch (Exception exception) {
      // Log any exceptions that occur during processing
      Log.Error(exception);
    } finally {
      // Always dispose the entity query to prevent memory leaks
      entityQuery.Dispose();
    }
  }
}
