using System.Collections.Generic;
using ProjectM;
using ScarletCore.Systems;
using ScarletCore.Data;
using ScarletCore.Utils;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ScarletCore.Services;

/// <summary>
/// Service for teleporting entities with safety checks, validation, and advanced positioning features.
/// Provides methods for safe teleportation, distance calculations, and position validation.
/// </summary>
public static class TeleportService {

  #region Basic Teleportation


  /// <summary>
  /// Teleports the specified entity to the given world position using a teleport buff.
  /// </summary>
  /// <param name="entity">The entity to teleport.</param>
  /// <param name="position">The target world position to teleport to.</param>
  /// <returns>True if teleportation was successful; otherwise, false.</returns>
  public static bool Teleport(Entity entity, float3 position) {
    if (!BuffService.TryApplyBuff(entity, new(150521246), 0, out var buffEntity)) return false;

    buffEntity.With((ref TeleportBuff teleportBuff) => {
      teleportBuff.EndPosition = position;
    });

    return true;
  }

  /// <summary>
  /// Teleports an entity to another entity's position.
  /// </summary>
  /// <param name="entity">Entity to teleport</param>
  /// <param name="target">Target entity to teleport to</param>
  /// <param name="offset">Optional offset from target position</param>
  /// <param name="validatePosition">Whether to validate the target position is safe</param>
  /// <returns>True if teleportation was successful</returns>
  public static bool TeleportToEntity(Entity entity, Entity target, float3 offset = default, bool validatePosition = true) {
    if (!IsValidEntity(entity) || !IsValidEntity(target)) {
      return false;
    }

    var position = target.Position() + offset;
    return TeleportToPosition(entity, position, validatePosition);
  }

  /// <summary>
  /// Teleports an entity to a specific position with optional validation.
  /// </summary>
  /// <param name="entity">Entity to teleport</param>
  /// <param name="position">Target position</param>
  /// <param name="preserveRotation">Whether to keep current rotation</param>
  /// <returns>True if teleportation was successful</returns>
  public static bool TeleportToPosition(Entity entity, float3 position, bool preserveRotation = true) {
    if (!IsValidEntity(entity)) {
      return false;
    }

    try {
      // Update all position-related components
      UpdateEntityPosition(entity, position);

      // Restore rotation if preserving
      if (preserveRotation && entity.Has<LocalTransform>()) {
        var transform = entity.Read<LocalTransform>();
        entity.Write(transform);
      }

      UpdateEntityPosition(entity, position);
      return true;
    } catch (System.Exception ex) {
      Log.Error($"Error teleporting entity {entity.Index}: {ex.Message}");
      return false;
    }
  }

  #endregion

  #region Player-Specific Methods

  /// <summary>
  /// Teleports a player to another player's position.
  /// </summary>
  /// <param name="playerName">Name of player to teleport</param>
  /// <param name="targetPlayerName">Name of target player</param>
  /// <param name="offset">Optional offset from target</param>
  /// <returns>True if teleportation was successful</returns>
  public static bool TeleportPlayerToPlayer(string playerName, string targetPlayerName, float3 offset = default) {
    if (!PlayerService.TryGetByName(playerName, out var player) ||
        !PlayerService.TryGetByName(targetPlayerName, out var targetPlayer)) {
      return false;
    }

    return TeleportToEntity(player.CharacterEntity, targetPlayer.CharacterEntity, offset);
  }

  /// <summary>
  /// Teleports a player to coordinates with safety checks.
  /// </summary>
  /// <param name="playerName">Name of player to teleport</param>
  /// <param name="x">X coordinate</param>
  /// <param name="y">Y coordinate</param>
  /// <param name="z">Z coordinate</param>
  /// <param name="validatePosition">Whether to validate the position</param>
  /// <returns>True if teleportation was successful</returns>
  public static bool TeleportPlayer(string playerName, float x, float y, float z, bool validatePosition = true) {
    if (!PlayerService.TryGetByName(playerName, out var player)) {
      return false;
    }

    return TeleportToPosition(player.CharacterEntity, new float3(x, y, z), validatePosition);
  }

  #endregion

  #region Utility Methods

  /// <summary>
  /// Calculates the distance between two entities.
  /// </summary>
  /// <param name="entity1">First entity</param>
  /// <param name="entity2">Second entity</param>
  /// <returns>Distance between entities, or -1 if either entity is invalid</returns>
  public static float GetDistance(Entity entity1, Entity entity2) {
    if (!IsValidEntity(entity1) || !IsValidEntity(entity2)) {
      return -1f;
    }

    return MathUtility.Distance(entity1.Position(), entity2.Position());
  }


  /// <summary>
  /// Finds the nearest player to a given position.
  /// </summary>
  /// <param name="position">Reference position</param>
  /// <param name="maxDistance">Maximum distance to consider</param>
  /// <returns>PlayerData of nearest player, or null if none found</returns>
  public static PlayerData FindNearestPlayer(float3 position, float maxDistance = float.MaxValue) {
    PlayerData nearestPlayer = null;
    float nearestDistance = maxDistance;

    foreach (var player in PlayerService.AllPlayers) {
      if (!IsValidEntity(player.CharacterEntity)) continue;

      float distance = MathUtility.Distance(position, player.CharacterEntity.Position());
      if (distance < nearestDistance) {
        nearestDistance = distance;
        nearestPlayer = player;
      }
    }

    return nearestPlayer;
  }

  /// <summary>
  /// Gets all players within a specified radius of a position.
  /// </summary>
  /// <param name="center">Center position</param>
  /// <param name="radius">Search radius</param>
  /// <returns>List of players within radius</returns>
  public static List<PlayerData> GetPlayersInRadius(float3 center, float radius) {
    var playersInRange = new List<PlayerData>();

    foreach (var player in PlayerService.AllPlayers) {
      if (!IsValidEntity(player.CharacterEntity)) continue;

      float distance = MathUtility.Distance(center, player.CharacterEntity.Position());
      if (distance <= radius) {
        playersInRange.Add(player);
      }
    }

    return playersInRange;
  }

  #endregion

  #region Private Helper Methods

  /// <summary>
  /// Validates that an entity exists and is valid for teleportation.
  /// </summary>
  /// <param name="entity">Entity to validate</param>
  /// <returns>True if entity is valid</returns>
  private static bool IsValidEntity(Entity entity) {
    return entity != Entity.Null && GameSystems.EntityManager.Exists(entity);
  }

  /// <summary>
  /// Updates all position-related components for an entity.
  /// </summary>
  /// <param name="entity">Entity to update</param>
  /// <param name="position">New position</param>
  private static void UpdateEntityPosition(Entity entity, float3 position) {
    if (entity.Has<SpawnTransform>()) {
      var spawnTransform = entity.Read<SpawnTransform>();
      spawnTransform.Position = position;
      entity.Write(spawnTransform);
    }

    if (entity.Has<Height>()) {
      var height = entity.Read<Height>();
      height.LastPosition = position;
      entity.Write(height);
    }

    if (entity.Has<LocalTransform>()) {
      var localTransform = entity.Read<LocalTransform>();
      localTransform.Position = position;
      entity.Write(localTransform);
    }

    if (entity.Has<Translation>()) {
      var translation = entity.Read<Translation>();
      translation.Value = position;
      entity.Write(translation);
    }

    if (entity.Has<LastTranslation>()) {
      var lastTranslation = entity.Read<LastTranslation>();
      lastTranslation.Value = position;
      entity.Write(lastTranslation);
    }
  }

  #endregion
}