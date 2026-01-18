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
/// Service for spawning entities (not limited to units) with various configurations and options
/// </summary>
public static class SpawnerService {
  private static long _counter = 0;

  #region Public Methods

  /// <summary>
  /// Spawns an Unit with a post-spawn action to be executed
  /// </summary>
  /// <param name="prefabGUID">The prefab GUID of the entity to spawn.</param>
  /// <param name="position">The position to spawn at.</param>
  /// <param name="minRange">Minimum spawn offset from <paramref name="position"/> (default: 0; must be &gt;= 0).</param>
  /// <param name="maxRange">Maximum spawn offset from <paramref name="position"/> (default: 0; must be &gt;= <paramref name="minRange"/>).</param>
  /// <param name="lifeTime">How long the entities should live in seconds (0 = permanent, default: 0).</param>
  /// <param name="count">Number of entities to spawn (default: 1; must be &gt; 0).</param>
  /// <param name="postSpawnAction">Action to execute after the entity is spawned.</param>
  /// <returns>True if spawn was successful.</returns>
  public static bool Spawn(PrefabGUID prefabGUID, float3 position, float minRange = 0f, float maxRange = 0f, float lifeTime = 0f, int count = 1, Action<Entity> postSpawnAction = null) {
    try {
      // Validate parameters
      if (prefabGUID.GuidHash == 0) {
        Log.Warning("Invalid prefab GUID provided to SpawnerService.Spawn");
        return false;
      }

      var name = GameSystems.PrefabCollectionSystem._PrefabLookupMap.GetName(prefabGUID);
      var isUnit = name.StartsWith("CHAR_");

      if (!isUnit) {
        Log.Warning("SpawnerService.Spawn only supports unit entities");
        return false;
      }

      if (count <= 0) {
        Log.Warning("Invalid count provided to SpawnerService.Spawn");
        return false;
      }

      if (minRange < 0 || maxRange < minRange) {
        Log.Warning("Invalid range values provided to SpawnerService.Spawn");
        return false;
      }

      float lifeTimeHash = GetLifeTimeHash();
      GameSystems.UnitSpawnerUpdateSystem.SpawnUnit(Entity.Null, prefabGUID, position, count, minRange, maxRange, lifeTimeHash);
      UnitSpawnerReactSystemPatch.PostActions.Add(lifeTimeHash, (lifeTime, position, postSpawnAction));
      return true;
    } catch (Exception ex) {
      Log.Error($"Error spawning entities: {ex.Message}");
      return false;
    }
  }

  /// <summary>
  /// Spawns an Unit with a post-spawn action to be executed
  /// </summary>
  /// <param name="prefabGUID">The prefab GUID of the entity to spawn</param>
  /// <param name="position">The position to spawn at</param>
  /// <param name="lifeTime">How long the entity should live</param>
  /// <param name="postSpawnAction">Action to execute after spawning</param>
  /// <returns>True if spawn was successful</returns>
  [Obsolete("Use Spawn method instead")]
  public static bool SpawnWithPostAction(PrefabGUID prefabGUID, float3 position, float lifeTime, Action<Entity> postSpawnAction) {
    return Spawn(prefabGUID, position, 0f, 0f, lifeTime, 1, postSpawnAction);
  }

  /// <summary>
  /// Immediately instantiates a single entity and teleports it to a randomized position
  /// around the provided <paramref name="position"/> using the provided range values.
  /// </summary>
  /// <param name="prefabGUID">The prefab GUID of the entity to spawn.</param>
  /// <param name="position">The center position to spawn at.</param>
  /// <param name="minRange">Minimum spawn offset from <paramref name="position"/> (default: 0; must be &gt;= 0).</param>
  /// <param name="maxRange">Maximum spawn offset from <paramref name="position"/> (default: 0; must be &gt;= <paramref name="minRange"/>).</param>
  /// <param name="lifeTime">How long the entity should live in seconds (0 = permanent, default: 0).</param>
  /// <param name="owner">The owner entity (default: <c>default</c>).</param>
  /// <returns>The spawned entity or <see cref="Entity.Null"/> if failed.</returns>
  public static Entity ImmediateSpawn(PrefabGUID prefabGUID, float3 position, float minRange = 0f, float maxRange = 0f, float lifeTime = 0f, Entity owner = default) {
    if (prefabGUID.GuidHash == 0) {
      Log.Warning("Invalid prefab GUID provided to SpawnerService.ImmediateSpawn");
      return Entity.Null;
    }

    if (minRange < 0 || maxRange < minRange) {
      Log.Warning("Invalid range values provided to SpawnerService.ImmediateSpawn");
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
  /// Immediately instantiates multiple entities and teleports each to a randomized
  /// position around the provided <paramref name="position"/>.
  /// </summary>
  /// <param name="prefabGUID">The prefab GUID of the entity to spawn.</param>
  /// <param name="position">The center position to spawn around.</param>
  /// <param name="count">Number of entities to spawn (must be &gt; 0).</param>
  /// <param name="minRange">Minimum spawn offset from <paramref name="position"/> (must be &gt;= 0).</param>
  /// <param name="maxRange">Maximum spawn offset from <paramref name="position"/> (must be &gt;= <paramref name="minRange"/>).</param>
  /// <param name="lifeTime">How long the entities should live in seconds (0 = permanent).</param>
  /// <param name="owner">The owner entity (default: <c>default</c>).</param>
  /// <returns>List of spawned entities (empty if failed).</returns>
  public static List<Entity> ImmediateSpawn(PrefabGUID prefabGUID, float3 position, float minRange, float maxRange, float lifeTime, int count, Entity owner = default) {
    var entities = new List<Entity>();

    if (prefabGUID.GuidHash == 0) {
      Log.Warning("Invalid prefab GUID provided to SpawnerService.ImmediateSpawn");
      return entities;
    }

    if (count <= 0) {
      Log.Warning("Invalid count provided to SpawnerService.ImmediateSpawn");
      return entities;
    }

    if (minRange < 0 || maxRange < minRange) {
      Log.Warning("Invalid range values provided to SpawnerService.ImmediateSpawn");
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
  /// Spawns a copy of an entity by instantiating the prefab from the prefab collection
  /// and teleporting it to a randomized position around the provided <paramref name="position"/>.
  /// </summary>
  /// <param name="prefabGUID">The prefab GUID of the entity to spawn.</param>
  /// <param name="position">The center position to spawn at.</param>
  /// <param name="minRange">Minimum spawn offset from <paramref name="position"/> (default: 0; must be &gt;= 0).</param>
  /// <param name="maxRange">Maximum spawn offset from <paramref name="position"/> (default: 0; must be &gt;= <paramref name="minRange"/>).</param>
  /// <param name="lifeTime">How long the entity should live in seconds (0 = permanent, default: 0).</param>
  /// <returns>The spawned entity or <see cref="Entity.Null"/> if failed.</returns>
  public static Entity SpawnCopy(PrefabGUID prefabGUID, float3 position, float minRange = 0, float maxRange = 0, float lifeTime = 0f) {
    if (prefabGUID.GuidHash == 0) {
      Log.Warning("Invalid prefab GUID provided to SpawnerService.SpawnCopy");
      return Entity.Null;
    }

    if (minRange < 0 || maxRange < minRange) {
      Log.Warning("Invalid range values provided to SpawnerService.SpawnCopy");
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
  /// Spawns multiple copies of an entity by instantiating the prefab from the
  /// prefab collection and teleporting each copy to a randomized position
  /// around the provided <paramref name="position"/>.
  /// </summary>
  /// <param name="prefabGUID">The prefab GUID of the entity to spawn.</param>
  /// <param name="position">The center position to spawn around.</param>
  /// <param name="count">Number of entities to spawn (must be &gt; 0).</param>
  /// <param name="minRange">Minimum spawn offset from <paramref name="position"/> (must be &gt;= 0).</param>
  /// <param name="maxRange">Maximum spawn offset from <paramref name="position"/> (must be &gt;= <paramref name="minRange"/>).</param>
  /// <param name="lifeTime">How long the entities should live in seconds (0 = permanent).</param>
  /// <returns>List of spawned entities (empty if failed).</returns>
  public static List<Entity> SpawnCopy(PrefabGUID prefabGUID, float3 position, float minRange, float maxRange, float lifeTime, int count) {
    var entities = new List<Entity>();

    if (prefabGUID.GuidHash == 0) {
      Log.Warning("Invalid prefab GUID provided to SpawnerService.SpawnCopy");
      return entities;
    }

    if (count <= 0) {
      Log.Warning("Invalid count provided to SpawnerService.SpawnCopy");
      return entities;
    }

    if (minRange < 0 || maxRange < minRange) {
      Log.Warning("Invalid range values provided to SpawnerService.SpawnCopy");
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
  /// Spawns a single entity at exact position
  /// </summary>
  /// <param name="prefabGUID">The prefab GUID of the entity to spawn</param>
  /// <param name="position">The exact position to spawn at</param>
  /// <param name="lifeTime">How long the entity should live</param>
  /// <returns>True if spawn was successful</returns>
  public static bool SpawnAtPosition(PrefabGUID prefabGUID, float3 position, float lifeTime) {
    return Spawn(prefabGUID, position, count: 1, minRange: 0, maxRange: 0, lifeTime: lifeTime);
  }

  /// <summary>
  /// Spawns multiple entities in a radius around a position
  /// </summary>
  /// <param name="prefabGUID">The prefab GUID of the entity to spawn</param>
  /// <param name="centerPosition">The center position to spawn around</param>
  /// <param name="count">Number of entities to spawn</param>
  /// <param name="radius">Spawn radius around the center position</param>
  /// <param name="lifeTime">How long the entities should live</param>
  /// <returns>True if spawn was successful</returns>
  public static bool SpawnInRadius(PrefabGUID prefabGUID, float3 centerPosition, float radius, float lifeTime, int count) {
    int range = (int)math.ceil(radius);
    return Spawn(prefabGUID, centerPosition, minRange: 0, maxRange: range, lifeTime: lifeTime, count: count);
  }

  #endregion

  #region Helper Methods

  /// <summary>
  /// Generates a unique duration hash based on current time
  /// </summary>
  /// <returns>Unique hash value</returns>
  public static float GetLifeTimeHash() => math.round(DateTime.Now.Ticks / TimeSpan.TicksPerSecond) + ++_counter;

  #endregion
}