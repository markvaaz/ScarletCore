using System;
using System.Collections.Generic;
using ProjectM;
using ScarletCore.Utils;
using Stunlock.Core;
using Unity.Entities;
using Unity.Mathematics;

namespace ScarletCore.Services;

/// <summary>
/// Obsolete wrapper for backwards compatibility.
/// Use <see cref="SpawnerService"/> instead — this class forwards calls to the new implementation.
/// </summary>
[Obsolete("UnitSpawnerService está obsoleto. Use SpawnerService em seu lugar.", false)]
public static class UnitSpawnerService {
  #region Public Methods

  [Obsolete("Use SpawnerService.Spawn instead.", false)]
  public static bool Spawn(PrefabGUID prefabGUID, float3 position, int count = 1, float minRange = 1, float maxRange = 8, float lifeTime = 0f) {
    return SpawnerService.Spawn(prefabGUID, position, minRange, maxRange, lifeTime, count);
  }

  [Obsolete("Use SpawnerService.ImmediateSpawn instead.", false)]
  public static Entity ImmediateSpawn(PrefabGUID prefabGUID, float3 position, float minRange = 1, float maxRange = 1, float lifeTime = 0f, Entity owner = default) {
    return SpawnerService.ImmediateSpawn(prefabGUID, position, minRange, maxRange, lifeTime, owner);
  }

  [Obsolete("Use SpawnerService.ImmediateSpawn (list) instead.", false)]
  public static List<Entity> ImmediateSpawn(PrefabGUID prefabGUID, float3 position, int count, float minRange = 1, float maxRange = 8, float lifeTime = 0f, Entity owner = default) {
    return SpawnerService.ImmediateSpawn(prefabGUID, position, minRange, maxRange, lifeTime, count, owner);
  }

  [Obsolete("Use SpawnerService.SpawnCopy instead.", false)]
  public static Entity SpawnCopy(PrefabGUID prefabGUID, float3 position, float minRange = 1, float maxRange = 1, float lifeTime = 0f) {
    return SpawnerService.SpawnCopy(prefabGUID, position, minRange, maxRange, lifeTime);
  }

  [Obsolete("Use SpawnerService.SpawnCopy (list) instead.", false)]
  public static List<Entity> SpawnCopy(PrefabGUID prefabGUID, float3 position, int count, float minRange = 1, float maxRange = 8, float lifeTime = 0f) {
    return SpawnerService.SpawnCopy(prefabGUID, position, minRange, maxRange, lifeTime, count);
  }

  [Obsolete("Use SpawnerService.SpawnAtPosition instead.", false)]
  public static bool SpawnAtPosition(PrefabGUID prefabGUID, float3 position, float lifeTime) {
    return SpawnerService.SpawnAtPosition(prefabGUID, position, lifeTime);
  }

  [Obsolete("Use SpawnerService.SpawnInRadius instead.", false)]
  public static bool SpawnInRadius(PrefabGUID prefabGUID, float3 centerPosition, int count, float radius, float lifeTime) {
    return SpawnerService.SpawnInRadius(prefabGUID, centerPosition, radius, lifeTime, count);
  }

  [Obsolete("Use SpawnerService.SpawnWithPostAction instead.", false)]
  public static bool SpawnWithPostAction(PrefabGUID prefabGUID, float3 position, float lifeTime, Action<Entity> postSpawnAction) {
    return SpawnerService.SpawnWithPostAction(prefabGUID, position, lifeTime, postSpawnAction);
  }

  #endregion

  #region Helper Methods

  [Obsolete("Use SpawnerService.GetDurationHash instead.", false)]
  public static long GetDurationHash() {
    return SpawnerService.GetDurationHash();
  }

  #endregion
}