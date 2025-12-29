using System;
using ProjectM;
using ProjectM.CastleBuilding;
using ProjectM.Network;
using ScarletCore.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ScarletCore.Services;

/// <summary>
/// Provides utility methods for querying and manipulating entities in the game world using Unity's ECS.
/// </summary>
public static class EntityLookupService {
  private static EntityManager EntityManager => GameSystems.EntityManager;
  private static GenerateCastleSystem GenerateCastleSystem => GameSystems.GenerateCastleSystem;

  /// <summary>
  /// Queries entities that match the specified component types and query options.
  /// </summary>
  /// <param name="options">EntityQueryOptions to use.</param>
  /// <param name="types">Component types to match.</param>
  /// <returns>Array of matching entities.</returns>
  public static NativeArray<Entity> Query(EntityQueryOptions options, params Type[] types) {
    return Query(options, ToComponentTypes(types));
  }

  /// <summary>
  /// Queries entities that match the specified component types and query options.
  /// </summary>
  public static NativeArray<Entity> Query(EntityQueryOptions options, params ComponentType[] componentTypes) {
    var queryDesc = new EntityQueryDesc {
      All = componentTypes,
      Options = options
    };
    var query = EntityManager.CreateEntityQuery(queryDesc);
    var entities = query.ToEntityArray(Allocator.Temp);
    query.Dispose();
    return entities;
  }

  /// <summary>
  /// Queries entities that match the specified component types.
  /// </summary>
  public static NativeArray<Entity> Query(params ComponentType[] componentTypes) {
    var queryDesc = new EntityQueryDesc {
      All = componentTypes
    };
    var query = EntityManager.CreateEntityQuery(queryDesc);
    var entities = query.ToEntityArray(Allocator.Temp);
    query.Dispose();
    return entities;
  }

  /// <summary>
  /// Queries entities that match the specified component types (provided as Type[]).
  /// </summary>
  public static NativeArray<Entity> Query(params Type[] types) {
    return Query(ToComponentTypes(types));
  }

  /// <summary>
  /// Queries entities that have the specified component type T and query options.
  /// </summary>
  public static NativeArray<Entity> Query<T>(EntityQueryOptions options) where T : IComponentData {
    return Query(options, typeof(T));
  }

  /// <summary>
  /// Queries entities that have the specified component type T.
  /// </summary>
  public static NativeArray<Entity> Query<T>() where T : IComponentData {
    return Query(typeof(T));
  }

  /// <summary>
  /// Destroys all entities with the specified component types within a given radius from the center.
  /// </summary>
  public static int ClearEntitiesInRadius(float2 center, float radius, params ComponentType[] componentTypes) {
    var entities = GetEntitiesInRadius(center, radius, componentTypes);
    int destroyed = 0;
    for (int i = 0; i < entities.Length; i++) {
      var entity = entities[i];
      EntityManager.DestroyEntity(entity);
      destroyed++;
    }
    return destroyed;
  }

  /// <summary>
  /// Destroys all entities with the specified component types (provided as Type[]) within a given radius from the center.
  /// </summary>
  public static int ClearEntitiesInRadius(float2 center, float radius, params Type[] types) {
    return ClearEntitiesInRadius(center, radius, ToComponentTypes(types));
  }

  /// <summary>
  /// Destroys all entities with the specified component types within a given radius from the center (float3 overload).
  /// </summary>
  public static int ClearEntitiesInRadius(float3 center, float radius, params ComponentType[] componentTypes) {
    return ClearEntitiesInRadius(new float2(center.x, center.z), radius, componentTypes);
  }

  /// <summary>
  /// Destroys all entities with the specified component types (provided as Type[]) within a given radius from the center (float3 overload).
  /// </summary>
  public static int ClearEntitiesInRadius(float3 center, float radius, params Type[] types) {
    return ClearEntitiesInRadius(new float2(center.x, center.z), radius, types);
  }

  /// <summary>
  /// Destroys all entities with the specified component type T within a given radius from the center (float3 overload).
  /// </summary>
  public static int ClearEntitiesInRadius<T>(float3 center, float radius) {
    return ClearEntitiesInRadius<T>(new float2(center.x, center.z), radius);
  }

  /// <summary>
  /// Destroys all entities with the specified component type T within a given radius from the center.
  /// </summary>
  public static int ClearEntitiesInRadius<T>(float2 center, float radius) {
    var entities = GetEntitiesInRadius<T>(center, radius);
    var destroyed = 0;
    foreach (var entity in entities) {
      if (EntityManager.HasComponent<T>(entity)) {
        EntityManager.DestroyEntity(entity);
        destroyed++;
      }
    }
    return destroyed;
  }

  /// <summary>
  /// Destroys all entities within a given radius from the center (float3 overload).
  /// </summary>
  public static int ClearAllEntitiesInRadius(float3 center, float radius) {
    return ClearAllEntitiesInRadius(new float2(center.x, center.z), radius);
  }

  /// <summary>
  /// Destroys all entities within a given radius from the center.
  /// </summary>
  public static int ClearAllEntitiesInRadius(float2 center, float radius) {
    var entities = GetAllEntitiesInRadius(center, radius);
    var destroyed = 0;

    foreach (var entity in entities) {
      if (!entity.Exists() || entity.Has<PlayerCharacter>() || entity.Has<User>()) continue;
      destroyed++;
      entity.Destroy();
    }

    return destroyed;
  }

  /// <summary>
  /// Gets all entities within a given radius from the center that have all specified component types.
  /// </summary>
  public static NativeList<Entity> GetEntitiesInRadius(float2 center, float radius, params ComponentType[] componentTypes) {
    var entities = GetAllEntitiesInRadius(center, radius);
    var filtered = new NativeList<Entity>(Allocator.Temp);

    for (int i = 0; i < entities.Length; i++) {
      var entity = entities[i];
      bool hasAll = true;
      foreach (var type in componentTypes) {
        if (!EntityManager.HasComponent(entity, type)) {
          hasAll = false;
          break;
        }
      }
      if (hasAll) {
        filtered.Add(ref entity);
      }
    }
    return filtered;
  }

  /// <summary>
  /// Gets all entities within a given radius from the center that have all specified component types (provided as Type[]).
  /// </summary>
  public static NativeList<Entity> GetEntitiesInRadius(float2 center, float radius, params Type[] types) {
    return GetEntitiesInRadius(center, radius, ToComponentTypes(types));
  }

  /// <summary>
  /// Gets all entities within a given radius from the center that have the specified component type T (float3 overload).
  /// </summary>
  public static NativeList<Entity> GetEntitiesInRadius<T>(float3 center, float radius) {
    return GetEntitiesInRadius<T>(new float2(center.x, center.z), radius);
  }

  /// <summary>
  /// Gets all entities within a given radius from the center that have the specified component type T.
  /// </summary>
  public static NativeList<Entity> GetEntitiesInRadius<T>(float2 center, float radius) {
    var entities = GetAllEntitiesInRadius(center, radius);
    var filtered = new NativeList<Entity>(Allocator.Temp);

    for (int i = 0; i < entities.Length; i++) {
      var entity = entities[i];
      if (EntityManager.HasComponent<T>(entity)) {
        filtered.Add(ref entity);
      }
    }

    return filtered;
  }

  /// <summary>
  /// Gets all entities within a given radius from the center (float3 overload).
  /// </summary>
  public static NativeList<Entity> GetAllEntitiesInRadius(float3 center, float radius) {
    return GetAllEntitiesInRadius(new float2(center.x, center.z), radius);
  }

  /// <summary>
  /// Gets all entities within a given radius from the center.
  /// </summary>
  public static NativeList<Entity> GetAllEntitiesInRadius(float2 center, float radius) {
    var spatialData = GenerateCastleSystem._TileModelLookupSystemData;
    var tileModelSpatialLookupRO = spatialData.GetSpatialLookupReadOnlyAndComplete(GenerateCastleSystem);

    var gridPosMin = ConvertPosToTileGrid(center - radius);
    var gridPosMax = ConvertPosToTileGrid(center + radius);
    var bounds = new BoundsMinMax(Mathf.FloorToInt(gridPosMin.x), Mathf.FloorToInt(gridPosMin.y), Mathf.CeilToInt(gridPosMax.x), Mathf.CeilToInt(gridPosMax.y));

    var entities = tileModelSpatialLookupRO.GetEntities(ref bounds, TileType.All);
    return entities;
  }

  /// <summary>
  /// Converts a float2 world position to tile grid coordinates.
  /// </summary>
  public static float2 ConvertPosToTileGrid(float2 pos) {
    return new float2(Mathf.FloorToInt(pos.x * 2) + 6400, Mathf.FloorToInt(pos.y * 2) + 6400);
  }

  /// <summary>
  /// Converts a float3 world position to tile grid coordinates.
  /// </summary>
  public static float3 ConvertPosToTileGrid(float3 pos) {
    return new float3(Mathf.FloorToInt(pos.x * 2) + 6400, pos.y, Mathf.FloorToInt(pos.z * 2) + 6400);
  }

  /// <summary>
  /// Utility method to convert an array of System.Type to an array of ComponentType (read-only).
  /// </summary>
  /// <param name="types">Array of component types.</param>
  /// <returns>Array of ComponentType (read-only).</returns>
  private static ComponentType[] ToComponentTypes(params Type[] types) {
    var componentTypes = new ComponentType[types.Length];
    for (int i = 0; i < types.Length; i++) {
      var il2cppType = Il2CppInterop.Runtime.Il2CppType.From(types[i]);
      componentTypes[i] = ComponentType.ReadOnly(il2cppType);
    }
    return componentTypes;
  }
}