using System;
using System.Collections.Generic;
using ProjectM;
using ScarletCore.Systems;
using ScarletCore.Patches;
using ScarletCore.Utils;
using Stunlock.Core;
using Unity.Entities;
using Unity.Mathematics;

namespace ScarletCore.Services;

/// <summary>
/// Service for spawning units with various configurations and options
/// </summary>
public static class UnitSpawnerService {
  #region Public Methods

  /// <summary>
  /// Spawns units at a specific position with validation and error handling
  /// </summary>
  /// <param name="prefabGUID">The prefab GUID of the unit to spawn</param>
  /// <param name="position">The position to spawn at</param>
  /// <param name="count">Number of units to spawn (default: 1)</param>
  /// <param name="minRange">Minimum spawn range (default: 1)</param>
  /// <param name="maxRange">Maximum spawn range (default: 8)</param>
  /// <param name="lifeTime">How long the units should live (0 = permanent, default: 0)</param>
  /// <returns>True if spawn was successful</returns>
  public static bool Spawn(PrefabGUID prefabGUID, float3 position, int count = 1, float minRange = 1, float maxRange = 8, float lifeTime = 0f) {
    try {
      // Validate parameters
      if (prefabGUID.GuidHash == 0) {
        Log.Warning("Invalid prefab GUID provided to UnitSpawnerService.TrySpawn");
        return false;
      }

      if (count <= 0) {
        Log.Warning("Invalid count provided to UnitSpawnerService.TrySpawn");
        return false;
      }

      if (minRange < 0 || maxRange < minRange) {
        Log.Warning("Invalid range values provided to UnitSpawnerService.TrySpawn");
        return false;
      }

      // Use Entity.Null as spawner
      var spawnerEntity = Entity.Null;
      GameSystems.UnitSpawnerUpdateSystem.SpawnUnit(spawnerEntity, prefabGUID, position, count, minRange, maxRange, lifeTime);

      Log.Info($"Successfully spawned {count} units of {prefabGUID.GuidHash} at {position}");
      return true;
    } catch (Exception ex) {
      Log.Error($"Error spawning units: {ex.Message}");
      return false;
    }
  }

  /// <summary>
  /// Immediately spawns a single unit at the specified position
  /// </summary>
  /// <param name="prefabGUID">The prefab GUID of the unit to spawn</param>
  /// <param name="position">The position to spawn at</param>
  /// <param name="minRange">Minimum spawn range (default: 1)</param>
  /// <param name="maxRange">Maximum spawn range (default: 1)</param>
  /// <param name="lifeTime">How long the unit should live (0 = permanent, default: 0)</param>
  /// <param name="owner">The owner entity (default: default)</param>
  /// <returns>The spawned entity or Entity.Null if failed</returns>
  public static Entity ImmediateSpawn(PrefabGUID prefabGUID, float3 position, float minRange = 1, float maxRange = 1, float lifeTime = 0f, Entity owner = default) {
    if (prefabGUID.GuidHash == 0) {
      Log.Warning("Invalid prefab GUID provided to UnitSpawnerService.ImmediateSpawn");
      return Entity.Null;
    }

    if (minRange < 0 || maxRange < minRange) {
      Log.Warning("Invalid range values provided to UnitSpawnerService.ImmediateSpawn");
      return Entity.Null;
    }

    var entity = GameSystems.ServerGameManager.InstantiateEntityImmediate(owner, prefabGUID);

    entity.AddWith((ref Age age) => {
      age.Value = 0;
    });

    entity.AddWith((ref LifeTime lt) => {
      lt.Duration = lifeTime;
      lt.EndAction = lifeTime <= 0 ? LifeTimeEndAction.None : LifeTimeEndAction.Destroy;
    });

    // Calculate random spawn position within range
    var spawnPosition = position + new float3(
      UnityEngine.Random.Range(-maxRange, maxRange),
      0,
      UnityEngine.Random.Range(-maxRange, maxRange)
    );

    TeleportService.TeleportToPosition(entity, spawnPosition);
    return entity;
  }

  /// <summary>
  /// Immediately spawns multiple units around the specified position
  /// </summary>
  /// <param name="prefabGUID">The prefab GUID of the unit to spawn</param>
  /// <param name="position">The center position to spawn around</param>
  /// <param name="count">Number of units to spawn</param>
  /// <param name="minRange">Minimum spawn range (default: 1)</param>
  /// <param name="maxRange">Maximum spawn range (default: 8)</param>
  /// <param name="lifeTime">How long the units should live (0 = permanent, default: 0)</param>
  /// <param name="owner">The owner entity (default: default)</param>
  /// <returns>List of spawned entities (empty if failed)</returns>
  public static List<Entity> ImmediateSpawn(PrefabGUID prefabGUID, float3 position, int count, float minRange = 1, float maxRange = 8, float lifeTime = 0f, Entity owner = default) {
    var entities = new List<Entity>();

    if (prefabGUID.GuidHash == 0) {
      Log.Warning("Invalid prefab GUID provided to UnitSpawnerService.ImmediateSpawn");
      return entities;
    }

    if (count <= 0) {
      Log.Warning("Invalid count provided to UnitSpawnerService.ImmediateSpawn");
      return entities;
    }

    if (minRange < 0 || maxRange < minRange) {
      Log.Warning("Invalid range values provided to UnitSpawnerService.ImmediateSpawn");
      return entities;
    }

    for (var i = 0; i < count; i++) {
      var entity = GameSystems.ServerGameManager.InstantiateEntityImmediate(owner, prefabGUID);

      entity.AddWith((ref Age age) => {
        age.Value = 0;
      });

      entity.AddWith((ref LifeTime lt) => {
        lt.Duration = lifeTime;
        lt.EndAction = lifeTime <= 0 ? LifeTimeEndAction.None : LifeTimeEndAction.Destroy;
      });

      var spawnPosition = position + new float3(
        UnityEngine.Random.Range(-maxRange, maxRange),
        0,
        UnityEngine.Random.Range(-maxRange, maxRange)
      );

      TeleportService.TeleportToPosition(entity, spawnPosition);
      entities.Add(entity);
    }

    return entities;
  }

  /// <summary>
  /// Spawns a copy of an entity by directly instantiating from the prefab collection
  /// </summary>
  /// <param name="prefabGUID">The prefab GUID of the unit to spawn</param>
  /// <param name="position">The position to spawn at</param>
  /// <param name="minRange">Minimum spawn range (default: 1)</param>
  /// <param name="maxRange">Maximum spawn range (default: 1)</param>
  /// <param name="lifeTime">How long the unit should live (0 = permanent, default: 0)</param>
  /// <returns>The spawned entity or Entity.Null if failed</returns>
  public static Entity SpawnCopy(PrefabGUID prefabGUID, float3 position, float minRange = 1, float maxRange = 1, float lifeTime = 0f) {
    if (prefabGUID.GuidHash == 0) {
      Log.Warning("Invalid prefab GUID provided to UnitSpawnerService.SpawnCopy");
      return Entity.Null;
    }

    if (minRange < 0 || maxRange < minRange) {
      Log.Warning("Invalid range values provided to UnitSpawnerService.SpawnCopy");
      return Entity.Null;
    }

    var defaultPrefab = GameSystems.PrefabCollectionSystem._PrefabGuidToEntityMap[prefabGUID];

    if (!defaultPrefab.Exists()) {
      Log.Warning($"Prefab with GUID {prefabGUID.GuidHash} not found in PrefabCollectionSystem");
      return Entity.Null;
    }

    var copy = GameSystems.EntityManager.Instantiate(defaultPrefab);

    copy.AddWith((ref Age age) => {
      age.Value = 0;
    });

    copy.AddWith((ref LifeTime lt) => {
      lt.Duration = lifeTime;
      lt.EndAction = lifeTime <= 0 ? LifeTimeEndAction.None : LifeTimeEndAction.Destroy;
    });

    // Calculate random spawn position within range
    var spawnPosition = position + new float3(
      UnityEngine.Random.Range(-maxRange, maxRange),
      0,
      UnityEngine.Random.Range(-maxRange, maxRange)
    );

    TeleportService.TeleportToPosition(copy, spawnPosition);
    return copy;
  }

  /// <summary>
  /// Spawns multiple copies of an entity around the specified position
  /// </summary>
  /// <param name="prefabGUID">The prefab GUID of the unit to spawn</param>
  /// <param name="position">The center position to spawn around</param>
  /// <param name="count">Number of units to spawn</param>
  /// <param name="minRange">Minimum spawn range (default: 1)</param>
  /// <param name="maxRange">Maximum spawn range (default: 8)</param>
  /// <param name="lifeTime">How long the units should live (0 = permanent, default: 0)</param>
  /// <returns>List of spawned entities (empty if failed)</returns>
  public static List<Entity> SpawnCopy(PrefabGUID prefabGUID, float3 position, int count, float minRange = 1, float maxRange = 8, float lifeTime = 0f) {
    var entities = new List<Entity>();

    if (prefabGUID.GuidHash == 0) {
      Log.Warning("Invalid prefab GUID provided to UnitSpawnerService.SpawnCopy");
      return entities;
    }

    if (count <= 0) {
      Log.Warning("Invalid count provided to UnitSpawnerService.SpawnCopy");
      return entities;
    }

    if (minRange < 0 || maxRange < minRange) {
      Log.Warning("Invalid range values provided to UnitSpawnerService.SpawnCopy");
      return entities;
    }

    var defaultPrefab = GameSystems.PrefabCollectionSystem._PrefabGuidToEntityMap[prefabGUID];

    if (!defaultPrefab.Exists()) {
      Log.Warning($"Prefab with GUID {prefabGUID.GuidHash} not found in PrefabCollectionSystem");
      return entities;
    }

    for (var i = 0; i < count; i++) {
      var copy = GameSystems.EntityManager.Instantiate(defaultPrefab);

      copy.AddWith((ref Age age) => {
        age.Value = 0;
      });

      copy.AddWith((ref LifeTime lt) => {
        lt.Duration = lifeTime;
        lt.EndAction = lifeTime <= 0 ? LifeTimeEndAction.None : LifeTimeEndAction.Destroy;
      });

      var spawnPosition = position + new float3(
        UnityEngine.Random.Range(-maxRange, maxRange),
        0,
        UnityEngine.Random.Range(-maxRange, maxRange)
      );

      TeleportService.TeleportToPosition(copy, spawnPosition);
      entities.Add(copy);
    }

    return entities;
  }

  /// <summary>
  /// Spawns a single unit at exact position
  /// </summary>
  /// <param name="prefabGUID">The prefab GUID of the unit to spawn</param>
  /// <param name="position">The exact position to spawn at</param>
  /// <param name="lifeTime">How long the unit should live</param>
  /// <returns>True if spawn was successful</returns>
  public static bool SpawnAtPosition(PrefabGUID prefabGUID, float3 position, float lifeTime) {
    return Spawn(prefabGUID, position, count: 1, minRange: 0, maxRange: 0, lifeTime: lifeTime);
  }

  /// <summary>
  /// Spawns multiple units in a radius around a position
  /// </summary>
  /// <param name="prefabGUID">The prefab GUID of the unit to spawn</param>
  /// <param name="centerPosition">The center position to spawn around</param>
  /// <param name="count">Number of units to spawn</param>
  /// <param name="radius">Spawn radius around the center position</param>
  /// <param name="lifeTime">How long the units should live</param>
  /// <returns>True if spawn was successful</returns>
  public static bool SpawnInRadius(PrefabGUID prefabGUID, float3 centerPosition, int count, float radius, float lifeTime) {
    int range = (int)math.ceil(radius);
    return Spawn(prefabGUID, centerPosition, count, minRange: 0, maxRange: range, lifeTime: lifeTime);
  }

  /// <summary>
  /// Spawns a unit with a post-spawn action to be executed
  /// </summary>
  /// <param name="prefabGUID">The prefab GUID of the unit to spawn</param>
  /// <param name="position">The position to spawn at</param>
  /// <param name="lifeTime">How long the unit should live</param>
  /// <param name="postSpawnAction">Action to execute after spawning</param>
  /// <returns>True if spawn was successful</returns>
  public static bool SpawnWithPostAction(PrefabGUID prefabGUID, float3 position, float lifeTime, Action<Entity> postSpawnAction) {
    try {
      var durationHash = GetDurationHash();

      if (Spawn(prefabGUID, position, count: 1, minRange: 0, maxRange: 0, lifeTime: durationHash)) {
        UnitSpawnerReactSystemPatch.PostActions.Add(durationHash, (lifeTime, postSpawnAction));
      } else {
        Log.Warning("Failed to spawn unit with post action");
        return false;
      }

      return true;
    } catch (Exception ex) {
      Log.Error($"Error spawning unit with post action: {ex.Message}");
      return false;
    }
  }

  #endregion

  #region Helper Methods

  /// <summary>
  /// Generates a unique duration hash based on current time
  /// </summary>
  /// <returns>Unique hash value</returns>
  public static long GetDurationHash() {
    // Generate a unique hash for the duration based on current time
    return (long)math.round(DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond);
  }

  #endregion
}