using System;
using ProjectM;
using ProjectM.Network;
using ProjectM.Shared;
using ScarletCore.Systems;
using ScarletCore.Utils;
using Stunlock.Core;
using Unity.Entities;

namespace ScarletCore.Services;

/// <summary>
/// Service for managing buffs on entities, including application, removal, and duration control.
/// Provides simplified methods for working with the game's buff system.
/// </summary>
public static class BuffService {
  /// <summary>
  /// Attempts to apply a buff to an entity with optional duration control.
  /// </summary>
  /// <param name="entity">Target entity to receive the buff</param>
  /// <param name="prefabGUID">GUID of the buff prefab to apply</param>
  /// <param name="duration">Duration in seconds (-1 for permanent/indefinite)</param>
  /// <returns>True if buff was successfully applied, false otherwise</returns>
  public static bool TryApplyBuff(Entity entity, PrefabGUID prefabGUID, float duration, out Entity buffEntity) {
    buffEntity = Entity.Null; // Initialize output parameter
    try {
      // Create the buff application event using the game's debug system
      // This is the standard way to programmatically apply buffs in V Rising
      ApplyBuffDebugEvent applyBuffDebugEvent = new() {
        BuffPrefabGUID = prefabGUID,
      };

      // Set up the character context for the buff application
      // Both Character and User point to the same entity for direct application
      FromCharacter fromCharacter = new() {
        Character = entity,
        User = entity
      };

      // Apply the buff through the game's debug events system
      // This ensures proper buff initialization and component setup
      GameSystems.DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);

      // Verify the buff was actually applied by trying to retrieve it
      // BuffUtility.TryGetBuff checks if the buff entity exists and is valid
      if (!BuffUtility.TryGetBuff(GameSystems.EntityManager, entity, prefabGUID, out buffEntity)) {
        return false;
      }

      // Remove gameplay event components to prevent unwanted side effects
      // These components can cause buffs to trigger additional events we might not want
      if (buffEntity.Has<CreateGameplayEventsOnSpawn>()) {
        buffEntity.Remove<CreateGameplayEventsOnSpawn>();
      }

      if (buffEntity.Has<GameplayEventListeners>()) {
        buffEntity.Remove<GameplayEventListeners>();
      }

      // Handle custom duration settings
      if (duration > 0) {
        // Set a specific duration for the buff
        var lifeTime = buffEntity.Read<LifeTime>();

        lifeTime.Duration = duration;
        lifeTime.EndAction = LifeTimeEndAction.Destroy; // Destroy when duration expires

        buffEntity.Write(lifeTime);
      }

      // Handle permanent/indefinite buffs
      if (duration <= 0 && buffEntity.Has<LifeTime>()) {
        // Make the buff permanent by disabling its end action
        var lifetime = buffEntity.Read<LifeTime>();

        lifetime.EndAction = LifeTimeEndAction.None; // Don't destroy when duration expires

        buffEntity.Write(lifetime);
      }

      return true;
    } catch (Exception e) {
      // Log any errors that occur during buff application
      // Return false to indicate the operation failed
      Log.Error($"An error occurred while applying buff: {e.Message}");
      return false;
    }
  }

  /// <summary>
  /// Attempts to apply a buff to an entity with optional duration control.
  /// </summary>
  /// <param name="entity">Target entity to receive the buff</param>
  /// <param name="prefabGUID">GUID of the buff prefab to apply</param>
  /// <param name="duration">Duration in seconds (-1 for permanent/indefinite)</param>
  /// <returns>True if buff was successfully applied, false otherwise</returns>
  public static bool TryApplyBuff(Entity entity, PrefabGUID prefabGUID, float duration = 0) {
    // Call the overloaded method with an output parameter for the buff entity
    return TryApplyBuff(entity, prefabGUID, duration, out _);
  }

  /// <summary>
  /// Attempts to retrieve a specific buff entity from an entity.
  /// </summary>
  /// <param name="entity">Entity to search for the buff on</param>
  /// <param name="prefabGUID">GUID of the buff prefab to look for</param>
  /// <param name="buff">Output parameter that will contain the buff entity if found</param>
  /// <returns>True if the buff was found and retrieved, false otherwise</returns>
  public static bool TryGetBuff(Entity entity, PrefabGUID prefabGUID, out Entity buff) {
    // Use the game's utility to retrieve the buff entity
    // This handles all the internal entity queries and validation
    return BuffUtility.TryGetBuff(GameSystems.EntityManager, entity, prefabGUID, out buff);
  }

  /// <summary>
  /// Checks if an entity currently has a specific buff applied.
  /// </summary>
  /// <param name="entity">Entity to check for the buff</param>
  /// <param name="prefabGUID">GUID of the buff prefab to look for</param>
  /// <returns>True if the entity has the buff, false otherwise</returns>
  public static bool HasBuff(Entity entity, PrefabGUID prefabGUID) {
    // Use the game's built-in utility to check for buff existence
    // This handles all the internal entity queries and validation
    return BuffUtility.HasBuff(GameSystems.EntityManager, entity, prefabGUID);
  }

  /// <summary>
  /// Removes a specific buff from an entity if it exists.
  /// </summary>
  /// <param name="entity">Entity to remove the buff from</param>
  /// <param name="prefabGUID">GUID of the buff prefab to remove</param>
  public static bool TryRemoveBuff(Entity entity, PrefabGUID prefabGUID) {
    if (!BuffUtility.TryGetBuff(GameSystems.EntityManager, entity, prefabGUID, out var buff)) {
      return false;
    }

    DestroyUtility.Destroy(GameSystems.EntityManager, buff, DestroyDebugReason.TryRemoveBuff);

    return true; // Successfully removed the buff
  }

  /// <summary>
  /// Gets the remaining duration of a buff on an entity.
  /// </summary>
  /// <param name="entity">Entity to check</param>
  /// <param name="prefabGUID">GUID of the buff to check duration for</param>
  /// <returns>Remaining duration in seconds, or -1 if buff doesn't exist or is permanent</returns>
  public static float GetBuffRemainingDuration(Entity entity, PrefabGUID prefabGUID) {
    // Try to get the buff entity first
    if (!BuffUtility.TryGetBuff(GameSystems.EntityManager, entity, prefabGUID, out var buff)) {
      return -1; // Buff doesn't exist
    }

    // Check if the buff has a lifetime component
    if (!buff.Has<LifeTime>()) {
      return -1; // Permanent buff (no lifetime)
    }

    var lifetime = buff.Read<LifeTime>();

    // Return -1 if it's set to not destroy (permanent buff)
    if (lifetime.EndAction == LifeTimeEndAction.None) {
      return -1;
    }

    // Check if the buff has an age component to track elapsed time
    if (!buff.Has<Age>()) {
      // No age component means we can't calculate remaining time accurately
      // Return the full duration as fallback
      return lifetime.Duration;
    }

    var age = buff.Read<Age>();

    // Calculate remaining duration: total duration minus elapsed time
    float remainingDuration = lifetime.Duration - age.Value;

    // Ensure we don't return negative values (buff should expire soon)
    return Math.Max(0f, remainingDuration);
  }

  /// <summary>
  /// Extends or reduces the duration of an existing buff.
  /// </summary>
  /// <param name="entity">Entity with the buff</param>
  /// <param name="prefabGUID">GUID of the buff to modify</param>
  /// <param name="newDuration">New duration in seconds (-1 for permanent)</param>
  /// <returns>True if duration was successfully modified, false if buff doesn't exist</returns>
  public static bool ModifyBuffDuration(Entity entity, PrefabGUID prefabGUID, float newDuration) {
    // Try to get the buff entity
    if (!BuffUtility.TryGetBuff(GameSystems.EntityManager, entity, prefabGUID, out var buff)) {
      return false; // Buff doesn't exist
    }

    try {
      if (newDuration <= 0) {
        // Make the buff permanent
        if (buff.Has<LifeTime>()) {
          buff.With((ref LifeTime lifeTime) => {
            lifeTime.EndAction = LifeTimeEndAction.None; // Disable destruction on expiration
          });
        }
      } else {
        buff.With((ref LifeTime lifeTime) => {
          lifeTime.Duration = newDuration;
          lifeTime.EndAction = LifeTimeEndAction.Destroy; // Set to destroy when duration expires
        });

        buff.With((ref Age age) => {
          age.Value = newDuration; // Reset age to 0 for new duration
        });
      }

      return true;
    } catch (Exception e) {
      Log.Error($"Error modifying buff duration: {e.Message}");
      return false;
    }
  }
}