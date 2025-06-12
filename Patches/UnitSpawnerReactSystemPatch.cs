using System;
using System.Collections.Generic;
using HarmonyLib;
using ProjectM;
using ScarletCore.Events;
using ScarletCore.Utils;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace ScarletCore.Patches;

[HarmonyPatch]
internal class UnitSpawnerReactSystemPatch {
  internal static Dictionary<long, (float duration, Action<Entity> Actions)> PostActions = [];

  [HarmonyPatch(typeof(UnitSpawnerReactSystem), nameof(UnitSpawnerReactSystem.OnUpdate))]
  [HarmonyPostfix]
  internal static bool Prefix(UnitSpawnerReactSystem __instance) {
    // Early exit if no subscribers and no post actions to process
    if (EventManager.UnitSpawnSubscriberCount == 0 && PostActions.Count == 0) return true;

    // Get all entities that were spawned in this frame
    var entityQuery = __instance._Query.ToEntityArray(Allocator.Temp);

    try {
      if (entityQuery.Length == 0) return true; // No entities spawned this frame, nothing to process
      // Collections to track spawned entities and their spawn information
      var spawnedEntities = new List<Entity>();

      // Process each spawned entity
      foreach (var entity in entityQuery) {
        spawnedEntities.Add(entity);

        // Skip post-action processing if no actions are registered
        if (PostActions.Count == 0) continue;

        // Post-spawn action processing
        // Check if this entity has a matching lifetime-based action
        var lifeTimeComponent = entity.Read<LifeTime>();
        var durationHash = (long)Mathf.Round(lifeTimeComponent.Duration);

        if (PostActions.TryGetValue(durationHash, out var actionData)) {
          var duration = actionData.duration;
          var action = actionData.Actions;

          // Remove the action after processing to prevent duplicate execution
          PostActions.Remove(durationHash);

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

          // Execute the custom post-spawn action
          action(entity);
        }
      }

      // Fire the unit spawn event for subscribers (batch processing for performance)
      EventManager.InvokeUnitSpawn(spawnedEntities, __instance);
    } catch (Exception exception) {
      // Log any exceptions that occur during processing
      Log.Error(exception);
    } finally {
      // Always dispose the entity query to prevent memory leaks
      entityQuery.Dispose();
    }

    return true;
  }
}