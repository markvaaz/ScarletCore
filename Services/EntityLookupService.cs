using System;
using System.Collections.Generic;
using ProjectM;
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

  // Query cache for performance optimization
  private static readonly Dictionary<int, EntityQuery> _queryCache = [];
  private static readonly object _cacheLock = new();

  #region Cache Management

  /// <summary>
  /// Clears all cached queries. Call this when unloading/reloading the mod.
  /// </summary>
  public static void ClearQueryCache() {
    lock (_cacheLock) {
      foreach (var query in _queryCache.Values) {
        if (query.IsEmpty) continue;
        try {
          query.Dispose();
        } catch {
          // Ignore disposal errors
        }
      }
      _queryCache.Clear();
    }
  }

  /// <summary>
  /// Gets the number of cached queries.
  /// </summary>
  public static int GetCachedQueryCount() {
    lock (_cacheLock) {
      return _queryCache.Count;
    }
  }

  #endregion

  #region Query Builder

  /// <summary>
  /// Creates a new query builder for constructing complex entity queries.
  /// </summary>
  public static QueryBuilder Query() => new();

  /// <summary>
  /// Builder class for constructing entity queries with fluent API.
  /// </summary>
  public class QueryBuilder {
    private ComponentType[] _all;
    private ComponentType[] _any;
    private ComponentType[] _none;
    private ComponentType[] _present;
    private ComponentType[] _absent;
    private ComponentType[] _disabled;
    private EntityQueryOptions _options = EntityQueryOptions.Default;

    /// <summary>
    /// Specifies components that entities MUST have (All).
    /// </summary>
    public QueryBuilder WithAll(params Type[] types) {
      _all = ToComponentTypes(types);
      return this;
    }

    /// <summary>
    /// Specifies components that entities MUST have (All).
    /// </summary>
    public QueryBuilder WithAll(params ComponentType[] componentTypes) {
      _all = componentTypes;
      return this;
    }

    /// <summary>
    /// Specifies that entities must have at least ONE of these components (Any).
    /// </summary>
    public QueryBuilder WithAny(params Type[] types) {
      _any = ToComponentTypes(types);
      return this;
    }

    /// <summary>
    /// Specifies that entities must have at least ONE of these components (Any).
    /// </summary>
    public QueryBuilder WithAny(params ComponentType[] componentTypes) {
      _any = componentTypes;
      return this;
    }

    /// <summary>
    /// Specifies components that entities MUST NOT have (None).
    /// </summary>
    public QueryBuilder WithNone(params Type[] types) {
      _none = ToComponentTypes(types);
      return this;
    }

    /// <summary>
    /// Specifies components that entities MUST NOT have (None).
    /// </summary>
    public QueryBuilder WithNone(params ComponentType[] componentTypes) {
      _none = componentTypes;
      return this;
    }

    /// <summary>
    /// Specifies components that must be present (enabled or disabled) on entities (Present).
    /// </summary>
    public QueryBuilder WithPresent(params Type[] types) {
      _present = ToComponentTypes(types);
      return this;
    }

    /// <summary>
    /// Specifies components that must be present (enabled or disabled) on entities (Present).
    /// </summary>
    public QueryBuilder WithPresent(params ComponentType[] componentTypes) {
      _present = componentTypes;
      return this;
    }

    /// <summary>
    /// Specifies components that must NOT be present on entities (Absent).
    /// </summary>
    public QueryBuilder WithAbsent(params Type[] types) {
      _absent = ToComponentTypes(types);
      return this;
    }

    /// <summary>
    /// Specifies components that must NOT be present on entities (Absent).
    /// </summary>
    public QueryBuilder WithAbsent(params ComponentType[] componentTypes) {
      _absent = componentTypes;
      return this;
    }

    /// <summary>
    /// Specifies components that must be disabled on entities (Disabled).
    /// </summary>
    public QueryBuilder WithDisabled(params Type[] types) {
      _disabled = ToComponentTypes(types);
      return this;
    }

    /// <summary>
    /// Specifies components that must be disabled on entities (Disabled).
    /// </summary>
    public QueryBuilder WithDisabled(params ComponentType[] componentTypes) {
      _disabled = componentTypes;
      return this;
    }

    /// <summary>
    /// Sets the query options.
    /// </summary>
    public QueryBuilder WithOptions(EntityQueryOptions options) {
      _options = options;
      return this;
    }

    /// <summary>
    /// Includes disabled entities in the query.
    /// </summary>
    public QueryBuilder IncludeDisabled() {
      _options |= EntityQueryOptions.IncludeDisabledEntities;
      return this;
    }

    /// <summary>
    /// Includes prefab entities in the query.
    /// </summary>
    public QueryBuilder IncludePrefabs() {
      _options |= EntityQueryOptions.IncludePrefab;
      return this;
    }

    /// <summary>
    /// Includes system entities in the query.
    /// </summary>
    public QueryBuilder IncludeSystems() {
      _options |= EntityQueryOptions.IncludeSystems;
      return this;
    }

    /// <summary>
    /// Filters entities that do not have EntityGuid component (usually runtime entities).
    /// </summary>
    public QueryBuilder FilterWriteGroup() {
      _options |= EntityQueryOptions.FilterWriteGroup;
      return this;
    }

    /// <summary>
    /// Executes the query and returns matching entities.
    /// Uses cached queries for better performance.
    /// </summary>
    public NativeArray<Entity> ToArray() {
      var queryDesc = new EntityQueryDesc {
        All = _all,
        Any = _any,
        None = _none,
        Present = _present,
        Absent = _absent,
        Disabled = _disabled,
        Options = _options
      };

      var query = GetOrCreateCachedQuery(queryDesc);
      return query.ToEntityArray(Allocator.Temp);
    }

    /// <summary>
    /// Executes the query and returns the first matching entity, or Entity.Null if none found.
    /// </summary>
    public Entity FirstOrDefault() {
      var entities = ToArray();
      var result = entities.Length > 0 ? entities[0] : Entity.Null;
      entities.Dispose();
      return result;
    }

    /// <summary>
    /// Executes the query and returns the count of matching entities.
    /// </summary>
    public int Count() {
      var entities = ToArray();
      var count = entities.Length;
      entities.Dispose();
      return count;
    }

    /// <summary>
    /// Executes the query and returns true if any entities match.
    /// </summary>
    public bool Any() {
      return Count() > 0;
    }

    /// <summary>
    /// Gets or creates a cached query based on the query descriptor.
    /// </summary>
    private static EntityQuery GetOrCreateCachedQuery(EntityQueryDesc desc) {
      int hash = ComputeQueryHash(desc);

      lock (_cacheLock) {
        if (_queryCache.TryGetValue(hash, out var cachedQuery)) {
          return cachedQuery;
        }

        var newQuery = EntityManager.CreateEntityQuery(desc);
        _queryCache[hash] = newQuery;
        return newQuery;
      }
    }

    /// <summary>
    /// Computes a hash code for a query descriptor.
    /// </summary>
    private static int ComputeQueryHash(EntityQueryDesc desc) {
      unchecked {
        int hash = 17;
        hash = hash * 31 + HashComponentTypes(desc.All);
        hash = hash * 31 + HashComponentTypes(desc.Any);
        hash = hash * 31 + HashComponentTypes(desc.None);
        hash = hash * 31 + HashComponentTypes(desc.Present);
        hash = hash * 31 + HashComponentTypes(desc.Absent);
        hash = hash * 31 + HashComponentTypes(desc.Disabled);
        hash = hash * 31 + (int)desc.Options;
        return hash;
      }
    }

    /// <summary>
    /// Computes a hash code for an array of ComponentTypes.
    /// </summary>
    private static int HashComponentTypes(ComponentType[] types) {
      if (types == null || types.Length == 0) return 0;

      unchecked {
        int hash = 19;
        for (int i = 0; i < types.Length; i++) {
          hash = hash * 31 + types[i].GetHashCode();
        }
        return hash;
      }
    }
  }

  #endregion

  #region Simplified Query Methods

  /// <summary>
  /// Queries entities that have ALL specified component types.
  /// </summary>
  public static NativeArray<Entity> QueryAll(params Type[] types) {
    return Query().WithAll(types).ToArray();
  }

  /// <summary>
  /// Queries entities that have ALL specified component types.
  /// </summary>
  public static NativeArray<Entity> QueryAll(params ComponentType[] componentTypes) {
    return Query().WithAll(componentTypes).ToArray();
  }

  /// <summary>
  /// Queries entities that have ALL specified component types with options.
  /// </summary>
  public static NativeArray<Entity> QueryAll(EntityQueryOptions options, params Type[] types) {
    return Query().WithAll(types).WithOptions(options).ToArray();
  }

  /// <summary>
  /// Queries entities that have ALL specified component types with options.
  /// </summary>
  public static NativeArray<Entity> QueryAll(EntityQueryOptions options, params ComponentType[] componentTypes) {
    return Query().WithAll(componentTypes).WithOptions(options).ToArray();
  }

  /// <summary>
  /// Queries entities that have at least ONE of the specified component types.
  /// </summary>
  public static NativeArray<Entity> QueryAny(params Type[] types) {
    return Query().WithAny(types).ToArray();
  }

  /// <summary>
  /// Queries entities that have at least ONE of the specified component types.
  /// </summary>
  public static NativeArray<Entity> QueryAny(params ComponentType[] componentTypes) {
    return Query().WithAny(componentTypes).ToArray();
  }

  /// <summary>
  /// Queries entities that have at least ONE of the specified component types with options.
  /// </summary>
  public static NativeArray<Entity> QueryAny(EntityQueryOptions options, params Type[] types) {
    return Query().WithAny(types).WithOptions(options).ToArray();
  }

  /// <summary>
  /// Queries entities that have at least ONE of the specified component types with options.
  /// </summary>
  public static NativeArray<Entity> QueryAny(EntityQueryOptions options, params ComponentType[] componentTypes) {
    return Query().WithAny(componentTypes).WithOptions(options).ToArray();
  }

  /// <summary>
  /// Queries entities that have the specified component type T.
  /// </summary>
  public static NativeArray<Entity> QueryAll<T>() where T : IComponentData {
    return QueryAll(typeof(T));
  }

  /// <summary>
  /// Queries entities that have the specified component type T with options.
  /// </summary>
  public static NativeArray<Entity> QueryAll<T>(EntityQueryOptions options) where T : IComponentData {
    return QueryAll(options, typeof(T));
  }

  /// <summary>
  /// Queries entities that have ALL of the specified component types T1 and T2.
  /// </summary>
  public static NativeArray<Entity> QueryAll<T1, T2>()
    where T1 : IComponentData
    where T2 : IComponentData {
    return QueryAll(typeof(T1), typeof(T2));
  }

  /// <summary>
  /// Queries entities that have ALL of the specified component types T1, T2, and T3.
  /// </summary>
  public static NativeArray<Entity> QueryAll<T1, T2, T3>()
    where T1 : IComponentData
    where T2 : IComponentData
    where T3 : IComponentData {
    return QueryAll(typeof(T1), typeof(T2), typeof(T3));
  }

  #endregion

  #region Radius-based Operations

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
    entities.Dispose();
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
  public static int ClearEntitiesInRadius<T>(float3 center, float radius) where T : IComponentData {
    return ClearEntitiesInRadius<T>(new float2(center.x, center.z), radius);
  }

  /// <summary>
  /// Destroys all entities with the specified component type T within a given radius from the center.
  /// </summary>
  public static int ClearEntitiesInRadius<T>(float2 center, float radius) where T : IComponentData {
    var entities = GetEntitiesInRadius<T>(center, radius);
    var destroyed = 0;
    foreach (var entity in entities) {
      if (EntityManager.HasComponent<T>(entity)) {
        EntityManager.DestroyEntity(entity);
        destroyed++;
      }
    }
    entities.Dispose();
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

    entities.Dispose();
    return destroyed;
  }

  /// <summary>
  /// Gets all entities within a given radius from the center that have all specified component types.
  /// </summary>
  public static NativeList<Entity> GetEntitiesInRadius(float2 center, float radius, params ComponentType[] componentTypes) {
    if (componentTypes == null || componentTypes.Length == 0) {
      return GetAllEntitiesInRadius(center, radius);
    }

    var allEntities = GetAllEntitiesInRadius(center, radius);
    var filtered = new NativeList<Entity>(Allocator.Temp);

    for (int i = 0; i < allEntities.Length; i++) {
      var entity = allEntities[i];
      if (!EntityManager.Exists(entity)) continue;

      bool hasAll = true;
      for (int j = 0; j < componentTypes.Length; j++) {
        if (!EntityManager.HasComponent(entity, componentTypes[j])) {
          hasAll = false;
          break;
        }
      }

      if (hasAll) {
        filtered.Add(ref entity);
      }
    }

    allEntities.Dispose();
    return filtered;
  }

  /// <summary>
  /// Gets all entities within a given radius from the center that have all specified component types (provided as Type[]).
  /// </summary>
  public static NativeList<Entity> GetEntitiesInRadius(float2 center, float radius, params Type[] types) {
    return GetEntitiesInRadius(center, radius, ToComponentTypes(types));
  }

  /// <summary>
  /// Gets all entities within a given radius from the center that have all specified component types (float3 overload).
  /// </summary>
  public static NativeList<Entity> GetEntitiesInRadius(float3 center, float radius, params ComponentType[] componentTypes) {
    return GetEntitiesInRadius(new float2(center.x, center.z), radius, componentTypes);
  }

  /// <summary>
  /// Gets all entities within a given radius from the center that have all specified component types (float3 overload with Type[]).
  /// </summary>
  public static NativeList<Entity> GetEntitiesInRadius(float3 center, float radius, params Type[] types) {
    return GetEntitiesInRadius(new float2(center.x, center.z), radius, types);
  }

  /// <summary>
  /// Gets all entities within a given radius from the center that have the specified component type T (float3 overload).
  /// </summary>
  public static NativeList<Entity> GetEntitiesInRadius<T>(float3 center, float radius) where T : IComponentData {
    return GetEntitiesInRadius<T>(new float2(center.x, center.z), radius);
  }

  /// <summary>
  /// Gets all entities within a given radius from the center that have the specified component type T.
  /// </summary>
  public static NativeList<Entity> GetEntitiesInRadius<T>(float2 center, float radius) where T : IComponentData {
    var allEntities = GetAllEntitiesInRadius(center, radius);
    var filtered = new NativeList<Entity>(Allocator.Temp);

    for (int i = 0; i < allEntities.Length; i++) {
      var entity = allEntities[i];
      if (EntityManager.Exists(entity) && EntityManager.HasComponent<T>(entity)) {
        filtered.Add(ref entity);
      }
    }

    allEntities.Dispose();
    return filtered;
  }

  /// <summary>
  /// Gets all entities within a given radius from the center that have ALL of the specified component types T1 and T2.
  /// </summary>
  public static NativeList<Entity> GetEntitiesInRadius<T1, T2>(float2 center, float radius)
    where T1 : IComponentData
    where T2 : IComponentData {
    return GetEntitiesInRadius(center, radius, typeof(T1), typeof(T2));
  }

  /// <summary>
  /// Gets all entities within a given radius from the center that have ALL of the specified component types T1 and T2 (float3 overload).
  /// </summary>
  public static NativeList<Entity> GetEntitiesInRadius<T1, T2>(float3 center, float radius)
    where T1 : IComponentData
    where T2 : IComponentData {
    return GetEntitiesInRadius<T1, T2>(new float2(center.x, center.z), radius);
  }

  /// <summary>
  /// Gets all entities within a given radius from the center that have ALL of the specified component types T1, T2, and T3.
  /// </summary>
  public static NativeList<Entity> GetEntitiesInRadius<T1, T2, T3>(float2 center, float radius)
    where T1 : IComponentData
    where T2 : IComponentData
    where T3 : IComponentData {
    return GetEntitiesInRadius(center, radius, typeof(T1), typeof(T2), typeof(T3));
  }

  /// <summary>
  /// Gets all entities within a given radius from the center that have ALL of the specified component types T1, T2, and T3 (float3 overload).
  /// </summary>
  public static NativeList<Entity> GetEntitiesInRadius<T1, T2, T3>(float3 center, float radius)
    where T1 : IComponentData
    where T2 : IComponentData
    where T3 : IComponentData {
    return GetEntitiesInRadius<T1, T2, T3>(new float2(center.x, center.z), radius);
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
    var utcs = GameSystems.Server.GetOrCreateSystemManaged<UpdateTileCellsSystem>();
    var spatialData = utcs.TileModelSpatialLookupSystemData;
    var tileModelSpatialLookupRO = spatialData.GetSpatialLookupReadOnlyAndComplete(utcs);

    var gridPosMin = ConvertPosToTileGrid(center - radius);
    var gridPosMax = ConvertPosToTileGrid(center + radius);
    var bounds = new BoundsMinMax(Mathf.FloorToInt(gridPosMin.x), Mathf.FloorToInt(gridPosMin.y), Mathf.CeilToInt(gridPosMax.x), Mathf.CeilToInt(gridPosMax.y));

    var entities = tileModelSpatialLookupRO.GetEntities(ref bounds, TileType.All);
    return entities;
  }

  #endregion

  #region Utility Methods

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
  private static ComponentType[] ToComponentTypes(params Type[] types) {
    var componentTypes = new ComponentType[types.Length];
    for (int i = 0; i < types.Length; i++) {
      var il2cppType = Il2CppInterop.Runtime.Il2CppType.From(types[i]);
      componentTypes[i] = ComponentType.ReadOnly(il2cppType);
    }
    return componentTypes;
  }

  #endregion
}