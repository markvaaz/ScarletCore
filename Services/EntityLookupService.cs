using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using ProjectM;
using ProjectM.Network;
using ProjectM.Pathfinding;
using ScarletCore.Systems;
using Stunlock.Core;
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
        Options = _options
      };

      // Only set non-null arrays to avoid NullReferenceException in Unity ECS
      if (_all != null && _all.Length > 0) queryDesc.All = _all;
      if (_any != null && _any.Length > 0) queryDesc.Any = _any;
      if (_none != null && _none.Length > 0) queryDesc.None = _none;
      if (_present != null && _present.Length > 0) queryDesc.Present = _present;
      if (_absent != null && _absent.Length > 0) queryDesc.Absent = _absent;
      if (_disabled != null && _disabled.Length > 0) queryDesc.Disabled = _disabled;

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
    var entities = GetAllEntitiesInRadius(center, radius);
    if (componentTypes == null || componentTypes.Length == 0) return entities;

    // Filter in place: a NativeList constructed from managed code crashes under
    // Il2CppInterop (AccessViolation in get_Pointer), so only mutate the game-created list.
    for (int i = entities.Length - 1; i >= 0; i--) {
      var entity = entities[i];
      bool keep = EntityManager.Exists(entity);
      if (keep) {
        for (int j = 0; j < componentTypes.Length; j++) {
          if (!EntityManager.HasComponent(entity, componentTypes[j])) {
            keep = false;
            break;
          }
        }
      }
      if (!keep) entities.RemoveAtSwapBack(i);
    }

    return entities;
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
    return GetEntitiesInRadius(center, radius, typeof(T));
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

  #region Component Dump

  // Reflection is expensive; cache the layout fields per type.
  private static readonly Dictionary<Type, FieldInfo[]> _dumpFieldCache = [];

  /// <summary>
  /// Dumps every component of an entity with its field values as JSON-friendly dictionaries.
  /// Returns id ("Index:Version"), prefab hash/name, tag component names, component values and
  /// buffers. Returns null if the entity no longer exists. By default buffers are listed by name
  /// only; with <paramref name="includeBuffers"/> their elements are serialized too (capped at
  /// <paramref name="maxBufferElements"/> per buffer).
  /// </summary>
  public static unsafe Dictionary<string, object> DumpComponents(Entity entity, int depth = 2, bool includeBuffers = false, int maxBufferElements = 32) {
    if (!EntityManager.Exists(entity)) return null;

    var components = new Dictionary<string, object>();
    var bufferNames = new List<string>();
    var bufferContents = new Dictionary<string, object>();
    var tags = new List<string>();

    var types = EntityManager.GetComponentTypes(entity);
    try {
      foreach (var ct in types) {
        var il2cppType = ct.GetManagedType();
        var managedType = ResolveManagedType(il2cppType?.FullName);
        var name = managedType?.Name ?? il2cppType?.Name ?? "Unknown";

        if (ct.IsZeroSized) { tags.Add(name); continue; }
        if (ct.IsBuffer) {
          if (!includeBuffers) { bufferNames.Add(name); continue; }
          try {
            bufferContents[name] = managedType == null
              ? "<unresolved>"
              : SerializeBuffer(new BufferCursor { Entity = entity, TypeIndex = ct.TypeIndex, ElementType = managedType, Length = EntityManager.GetBufferLength(entity, ct.TypeIndex) }, depth, maxBufferElements);
          } catch (Exception ex) {
            bufferContents[name] = $"<error: {ex.Message}>";
          }
          continue;
        }
        if (managedType == null) { components[name] = "<unresolved>"; continue; }
        if (ct.IsSharedComponent) { components[name] = "<shared>"; continue; }

        try {
          void* raw = EntityManager.GetComponentDataRawRO(entity, ct.TypeIndex);
          components[name] = (IntPtr)raw == IntPtr.Zero
            ? null
            : SerializeStruct(Marshal.PtrToStructure((IntPtr)raw, managedType), managedType, depth);
        } catch (Exception ex) {
          components[name] = $"<error: {ex.Message}>";
        }
      }
    } finally {
      types.Dispose();
    }

    var guid = entity.GetPrefabGuid();
    return new Dictionary<string, object> {
      ["id"] = $"{entity.Index}:{entity.Version}",
      ["prefab"] = guid.GuidHash,
      ["prefabName"] = guid.GetName(),
      ["tags"] = tags,
      ["components"] = components,
      ["buffers"] = includeBuffers ? bufferContents : bufferNames,
    };
  }

  /// <summary>
  /// Serializes the elements of one dynamic buffer of the entity by buffer name (e.g. "BuffBuffer").
  /// Returns { count, truncated, elements }. Throws <see cref="ArgumentException"/> if the entity
  /// has no such buffer.
  /// </summary>
  public static object DumpBuffer(Entity entity, string bufferName, int depth = 2, int maxElements = 32) {
    var cursor = FindBuffer(entity, bufferName)
      ?? throw new ArgumentException($"Entity {entity.Index}:{entity.Version} has no buffer '{bufferName}'");
    return SerializeBuffer(cursor, depth, maxElements);
  }

  private sealed class BufferCursor {
    public Entity Entity;
    public int TypeIndex;
    public Type ElementType;
    public int Length;
  }

  private static BufferCursor FindBuffer(Entity entity, string name) {
    var types = EntityManager.GetComponentTypes(entity);
    try {
      foreach (var ct in types) {
        if (!ct.IsBuffer) continue;
        var il2cppType = ct.GetManagedType();
        var managed = ResolveManagedType(il2cppType?.FullName);
        var simpleName = managed?.Name ?? il2cppType?.Name;
        if (!name.Equals(simpleName, StringComparison.OrdinalIgnoreCase) &&
            !name.Equals(managed?.FullName ?? il2cppType?.FullName, StringComparison.OrdinalIgnoreCase)) continue;
        if (managed == null) throw new ArgumentException($"Buffer '{simpleName}' could not be resolved to a managed type");
        return new BufferCursor { Entity = entity, TypeIndex = ct.TypeIndex, ElementType = managed, Length = EntityManager.GetBufferLength(entity, ct.TypeIndex) };
      }
    } finally {
      types.Dispose();
    }
    return null;
  }

  private static unsafe object ReadBufferElement(BufferCursor cursor, int index) {
    void* raw = EntityManager.GetBufferRawRO(cursor.Entity, cursor.TypeIndex);
    if ((IntPtr)raw == IntPtr.Zero) throw new ArgumentException($"Buffer '{cursor.ElementType.Name}' has no readable data");
    return Marshal.PtrToStructure((IntPtr)raw + index * Marshal.SizeOf(cursor.ElementType), cursor.ElementType);
  }

  private static object SerializeBuffer(BufferCursor cursor, int depth, int maxElements) {
    var elements = new List<object>();
    var count = Math.Min(cursor.Length, maxElements);
    for (var i = 0; i < count; i++) {
      try {
        elements.Add(SerializeStruct(ReadBufferElement(cursor, i), cursor.ElementType, depth));
      } catch (Exception ex) {
        elements.Add($"<error: {ex.Message}>");
      }
    }
    return new Dictionary<string, object> {
      ["count"] = cursor.Length,
      ["truncated"] = cursor.Length > maxElements,
      ["elements"] = elements,
    };
  }

  /// <summary>
  /// Navigates a dot-separated path starting at a component and walking into its fields, e.g.
  /// "SpellTarget.Target" or "ModifyRotationDuringCast.ModifyRotation.Rotation". Whenever the
  /// current value is an <see cref="Entity"/> or a NetworkedEntity, it is dereferenced: the next
  /// segment is looked up as a component on the referenced entity, so paths can hop across
  /// entities ("SpellTarget.Target.Health.Value"). If the path ends on an entity reference, the
  /// full <see cref="DumpComponents"/> of that entity is returned; otherwise the value at the
  /// path, serialized to the given depth. Throws <see cref="ArgumentException"/> with the valid
  /// candidates on a bad segment.
  /// </summary>
  public static object DumpComponentPath(Entity entity, string path, int depth = 3) {
    var segments = (path ?? "").Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    if (segments.Length == 0) throw new ArgumentException("Empty path — expected \"Component.Field.SubField...\"");

    var currentEntity = entity;
    object current = null;
    Type currentType = null;

    var i = 0;
    while (i < segments.Length) {
      if (current == null) {
        if (!EntityManager.Exists(currentEntity)) {
          throw new ArgumentException($"Entity {currentEntity.Index}:{currentEntity.Version} no longer exists");
        }
        (current, currentType) = ReadComponentBoxed(currentEntity, segments[i]);
        i++;
        continue;
      }

      // Buffer mid-path: the next segment is a numeric element index.
      if (current is BufferCursor cursor) {
        if (!int.TryParse(segments[i], out var index)) {
          throw new ArgumentException($"'{segments[i]}' — expected a numeric index into buffer '{cursor.ElementType.Name}' (0..{cursor.Length - 1})");
        }
        if (index < 0 || index >= cursor.Length) {
          throw new ArgumentException($"Index {index} out of range — buffer '{cursor.ElementType.Name}' has {cursor.Length} elements");
        }
        current = ReadBufferElement(cursor, index);
        currentType = cursor.ElementType;
        i++;
        continue;
      }

      // Entity reference mid-path: hop into the referenced entity, re-reading this segment as a component.
      if (TryUnwrapEntity(current, currentType, out var referenced)) {
        currentEntity = referenced;
        current = null;
        currentType = null;
        continue;
      }

      var field = DumpFieldsOf(currentType).FirstOrDefault(f => f.Name.TrimStart('_').Equals(segments[i], StringComparison.OrdinalIgnoreCase));
      if (field == null) {
        var candidates = string.Join(", ", DumpFieldsOf(currentType).Select(f => f.Name.TrimStart('_')));
        throw new ArgumentException($"'{currentType.Name}' has no field '{segments[i]}'. Fields: {candidates}");
      }
      current = field.GetValue(current);
      currentType = field.FieldType;
      i++;
    }

    if (current is BufferCursor finalCursor) return SerializeBuffer(finalCursor, depth, 32);
    if (TryUnwrapEntity(current, currentType, out var target)) {
      return EntityManager.Exists(target)
        ? DumpComponents(target, depth)
        : $"{target.Index}:{target.Version} <destroyed>";
    }
    return current == null ? null : SerializeValue(current, currentType, depth);
  }

  /// <summary>
  /// Searches every component and buffer of an entity for a text: matches field names and
  /// serialized values (case-insensitive contains). Returns up to 100 hits as { path, value },
  /// where path is navigable with <see cref="DumpComponentPath"/> (e.g. "BuffBuffer.4.PrefabGuid.name").
  /// Useful to find where a value lives, or which component references an entity handle.
  /// </summary>
  public static List<Dictionary<string, object>> FindInComponents(Entity entity, string text, int depth = 3) {
    if (string.IsNullOrEmpty(text)) throw new ArgumentException("Search text is empty");
    var dump = DumpComponents(entity, depth, includeBuffers: true)
      ?? throw new ArgumentException($"Entity {entity.Index}:{entity.Version} no longer exists");

    var results = new List<Dictionary<string, object>>();
    FindWalk(dump["components"], "", text, results);
    foreach (var kv in (Dictionary<string, object>)dump["buffers"]) {
      // Unwrap the {count,truncated,elements} envelope so hit paths match nav syntax (Buffer.index.field).
      var node = kv.Value is Dictionary<string, object> env && env.TryGetValue("elements", out var elements) ? elements : kv.Value;
      FindWalk(node, kv.Key, text, results);
    }
    foreach (var tag in (List<string>)dump["tags"]) {
      if (tag.Contains(text, StringComparison.OrdinalIgnoreCase)) {
        results.Add(new Dictionary<string, object> { ["path"] = tag, ["value"] = "<tag>" });
      }
    }
    return results;
  }

  private const int MaxFindResults = 100;

  private static void FindWalk(object node, string path, string needle, List<Dictionary<string, object>> results) {
    if (results.Count >= MaxFindResults) return;
    if (node is Dictionary<string, object> dict) {
      foreach (var kv in dict) {
        var childPath = path.Length == 0 ? kv.Key : $"{path}.{kv.Key}";
        if (kv.Key.Contains(needle, StringComparison.OrdinalIgnoreCase)) {
          results.Add(new Dictionary<string, object> { ["path"] = childPath, ["value"] = Compact(kv.Value) });
          if (results.Count >= MaxFindResults) return;
        } else {
          FindWalk(kv.Value, childPath, needle, results);
        }
      }
    } else if (node is System.Collections.IEnumerable enumerable && node is not string) {
      var i = 0;
      foreach (var item in enumerable) {
        FindWalk(item, $"{path}.{i}", needle, results);
        i++;
      }
    } else if (node?.ToString()?.Contains(needle, StringComparison.OrdinalIgnoreCase) == true) {
      results.Add(new Dictionary<string, object> { ["path"] = path, ["value"] = node });
    }
  }

  private static object Compact(object value) =>
    value is Dictionary<string, object> || (value is System.Collections.IEnumerable && value is not string) ? "<...>" : value;

  /// <summary>
  /// Compares the components of two entities and returns only what differs, as
  /// { path, a, b } entries ("&lt;missing&gt;" when one side lacks the component/field), plus tag
  /// differences. Buffers are not compared. Capped at 200 entries.
  /// </summary>
  public static List<Dictionary<string, object>> DiffComponents(Entity a, Entity b, int depth = 2) {
    var dumpA = DumpComponents(a, depth) ?? throw new ArgumentException($"Entity {a.Index}:{a.Version} no longer exists");
    var dumpB = DumpComponents(b, depth) ?? throw new ArgumentException($"Entity {b.Index}:{b.Version} no longer exists");

    var results = new List<Dictionary<string, object>>();
    DiffWalk(dumpA["components"], dumpB["components"], "", results);

    var tagsA = (List<string>)dumpA["tags"];
    var tagsB = (List<string>)dumpB["tags"];
    foreach (var tag in tagsA.Except(tagsB)) {
      results.Add(new Dictionary<string, object> { ["path"] = $"tags.{tag}", ["a"] = "<tag>", ["b"] = "<missing>" });
    }
    foreach (var tag in tagsB.Except(tagsA)) {
      results.Add(new Dictionary<string, object> { ["path"] = $"tags.{tag}", ["a"] = "<missing>", ["b"] = "<tag>" });
    }
    return results;
  }

  private const int MaxDiffResults = 200;

  private static void DiffWalk(object x, object y, string path, List<Dictionary<string, object>> results) {
    if (results.Count >= MaxDiffResults) return;
    if (x is Dictionary<string, object> dx && y is Dictionary<string, object> dy) {
      foreach (var key in dx.Keys.Union(dy.Keys)) {
        if (key == "m_Align8Union") continue; // raw memory addresses — always differ, never meaningful
        DiffWalk(
          dx.TryGetValue(key, out var vx) ? vx : "<missing>",
          dy.TryGetValue(key, out var vy) ? vy : "<missing>",
          path.Length == 0 ? key : $"{path}.{key}", results);
      }
    } else if (Stringify(x) != Stringify(y)) {
      results.Add(new Dictionary<string, object> { ["path"] = path, ["a"] = x, ["b"] = y });
    }
  }

  private static string Stringify(object value) {
    if (value == null) return "null";
    if (value is System.Collections.IEnumerable enumerable && value is not string) {
      return string.Join(",", enumerable.Cast<object>().Select(Stringify));
    }
    return value.ToString();
  }

  private static bool TryUnwrapEntity(object val, Type t, out Entity entity) {
    if (t == typeof(Entity)) {
      entity = (Entity)val;
      return entity != Entity.Null;
    }
    if (t?.Name == "NetworkedEntity") {
      var entityField = DumpFieldsOf(t).FirstOrDefault(f => f.FieldType == typeof(Entity));
      if (entityField != null) {
        entity = (Entity)entityField.GetValue(val);
        return entity != Entity.Null;
      }
    }
    entity = Entity.Null;
    return false;
  }

  private static unsafe (object boxed, Type type) ReadComponentBoxed(Entity entity, string name) {
    var types = EntityManager.GetComponentTypes(entity);
    try {
      foreach (var ct in types) {
        var il2cppType = ct.GetManagedType();
        var managed = ResolveManagedType(il2cppType?.FullName);
        var simpleName = managed?.Name ?? il2cppType?.Name;
        var fullName = managed?.FullName ?? il2cppType?.FullName;
        if (!name.Equals(simpleName, StringComparison.OrdinalIgnoreCase) &&
            !name.Equals(fullName, StringComparison.OrdinalIgnoreCase)) continue;

        if (managed == null) throw new ArgumentException($"'{simpleName}' could not be resolved to a managed type");
        if (ct.IsBuffer) {
          return (new BufferCursor { Entity = entity, TypeIndex = ct.TypeIndex, ElementType = managed, Length = EntityManager.GetBufferLength(entity, ct.TypeIndex) }, managed);
        }
        if (ct.IsZeroSized) throw new ArgumentException($"'{simpleName}' is a tag component (no data to navigate)");
        if (ct.IsSharedComponent) throw new ArgumentException($"'{simpleName}' is a shared component (not navigable)");

        void* raw = EntityManager.GetComponentDataRawRO(entity, ct.TypeIndex);
        if ((IntPtr)raw == IntPtr.Zero) throw new ArgumentException($"'{simpleName}' has no readable data");
        return (Marshal.PtrToStructure((IntPtr)raw, managed), managed);
      }
    } finally {
      types.Dispose();
    }
    throw new ArgumentException($"Entity {entity.Index}:{entity.Version} has no component '{name}' (use DumpComponents to list them)");
  }

  // GetManagedType() returns an Il2CppSystem.Type; Marshal needs the managed struct Type with the
  // same FullName from the interop assemblies. Index built lazily once (reflection is expensive).
  private static Dictionary<string, Type> _managedTypeIndex;

  private static Type ResolveManagedType(string fullName) {
    if (string.IsNullOrEmpty(fullName)) return null;
    if (_managedTypeIndex == null) {
      var index = new Dictionary<string, Type>();
      foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()) {
        var asmName = asm.GetName().Name ?? string.Empty;
        if (asmName.StartsWith("System") || asmName.StartsWith("netstandard") ||
            asmName.StartsWith("mscorlib") || asmName.StartsWith("Il2CppInterop") ||
            asmName.StartsWith("Il2CppSystem") || asmName.StartsWith("Il2Cppmscorlib")) continue;

        Type[] asmTypes;
        try {
          asmTypes = asm.GetTypes();
        } catch (ReflectionTypeLoadException e) {
          asmTypes = e.Types.Where(t => t != null).ToArray();
        } catch {
          continue;
        }

        foreach (var t in asmTypes) {
          // ECS components/buffers/tags are all structs.
          if (t == null || !t.IsValueType || t.IsEnum || t.IsPrimitive || string.IsNullOrEmpty(t.FullName)) continue;
          index[t.FullName] = t;
        }
      }
      _managedTypeIndex = index;
    }
    return _managedTypeIndex.TryGetValue(fullName, out var managed) ? managed : null;
  }

  private static FieldInfo[] DumpFieldsOf(Type t) {
    if (_dumpFieldCache.TryGetValue(t, out var cached)) return cached;
    var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
      .Where(f => !f.IsStatic && f.FieldType != typeof(IntPtr))
      .ToArray();
    _dumpFieldCache[t] = fields;
    return fields;
  }

  // Per-field try/catch so one bad field never fails the whole component.
  private static object SerializeStruct(object boxed, Type type, int depth) {
    var dict = new Dictionary<string, object>();
    foreach (var field in DumpFieldsOf(type)) {
      var name = field.Name.TrimStart('_');
      try {
        dict[name] = SerializeValue(field.GetValue(boxed), field.FieldType, depth);
      } catch (Exception ex) {
        dict[name] = $"<error: {ex.Message}>";
      }
    }
    return dict;
  }

  private static object SerializeValue(object val, Type t, int depth) {
    if (val == null) return null;

    if (t == typeof(bool) || t == typeof(int) || t == typeof(uint) || t == typeof(long) ||
        t == typeof(ulong) || t == typeof(short) || t == typeof(ushort) || t == typeof(byte) || t == typeof(sbyte)) {
      return val;
    }
    if (t == typeof(float)) return Round((float)val);
    if (t == typeof(double)) return Math.Round((double)val, 2);
    if (t.IsEnum) return val.ToString();

    if (t == typeof(Entity)) { var e = (Entity)val; return $"{e.Index}:{e.Version}"; }
    if (t == typeof(PrefabGUID)) {
      var g = (PrefabGUID)val;
      return new Dictionary<string, object> { ["hash"] = g.GuidHash, ["name"] = g.GetName() };
    }
    if (t == typeof(float3)) { var v = (float3)val; return new[] { Round(v.x), Round(v.y), Round(v.z) }; }
    if (t == typeof(float2)) { var v = (float2)val; return new[] { Round(v.x), Round(v.y) }; }
    if (t == typeof(float4)) { var v = (float4)val; return new[] { Round(v.x), Round(v.y), Round(v.z), Round(v.w) }; }
    if (t == typeof(quaternion)) { var q = ((quaternion)val).value; return new[] { Round(q.x), Round(q.y), Round(q.z), Round(q.w) }; }
    if (t == typeof(int2)) { var v = (int2)val; return new[] { v.x, v.y }; }
    if (t == typeof(int3)) { var v = (int3)val; return new[] { v.x, v.y, v.z }; }

    var fullName = t.FullName ?? string.Empty;
    if (fullName.StartsWith("Unity.Collections.FixedString")) {
      try { return val.ToString(); } catch { return "<fixedstring>"; }
    }

    // ModifiableFloat / ModifiableInt / ModifiableBool → unwrap the single value field.
    if (t.Name.StartsWith("Modifiable")) {
      var vf = DumpFieldsOf(t).FirstOrDefault(f => f.Name.TrimStart('_').Equals("Value", StringComparison.OrdinalIgnoreCase));
      if (vf != null) {
        try { return SerializeValue(vf.GetValue(val), vf.FieldType, depth); } catch { /* fall through */ }
      }
    }

    // Nested struct: recurse one level down, then stop to keep output bounded.
    if (t.IsValueType && !t.IsPrimitive) {
      if (depth <= 0) return $"<{t.Name}>";
      return SerializeStruct(val, t, depth - 1);
    }

    // il2cpp reference types aren't blittable data — don't chase them.
    return $"<{t.Name}>";
  }

  private static float Round(float f) => (float)Math.Round(f, 2);

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