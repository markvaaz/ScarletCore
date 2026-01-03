# EntityLookupService

## Overview

`EntityLookupService` is a powerful utility service that provides comprehensive methods for querying and manipulating entities in the game world using Unity's Entity Component System (ECS). It offers a fluent API for building complex entity queries, spatial operations, and optimized query caching for better performance.

## Table of Contents

- [Cache Management](#cache-management)
- [Query Builder Pattern](#query-builder-pattern)
- [Simplified Query Methods](#simplified-query-methods)
- [Spatial Queries](#spatial-queries)
- [Entity Destruction](#entity-destruction)
- [Utility Methods](#utility-methods)
- [Examples](#examples)
- [Best Practices](#best-practices)
- [Performance Considerations](#performance-considerations)

---

## Cache Management

EntityLookupService caches EntityQuery objects for better performance when repeatedly executing the same queries.

### ClearQueryCache

```csharp
public static void ClearQueryCache()
```

Clears all cached queries. Call this when unloading or reloading your mod to prevent memory leaks.

**Example:**
```csharp
// During mod unload
EntityLookupService.ClearQueryCache();
```

### GetCachedQueryCount

```csharp
public static int GetCachedQueryCount()
```

Returns the number of currently cached queries. Useful for debugging and monitoring cache size.

**Returns:** `int` - Number of cached queries

**Example:**
```csharp
int cacheSize = EntityLookupService.GetCachedQueryCount();
Logger.Info($"Query cache contains {cacheSize} queries");
```

---

## Query Builder Pattern

The Query Builder provides a fluent API for constructing complex entity queries with multiple conditions. Queries are automatically cached for optimal performance.

### Creating a Query

```csharp
var query = EntityLookupService.Query();
```

### Available Methods

#### Component Filtering

| Method | Description |
|--------|-------------|
| `WithAll(params Type[])` | Entities **must have ALL** of these components (enabled) |
| `WithAll(params ComponentType[])` | Entities **must have ALL** of these component types (enabled) |
| `WithAny(params Type[])` | Entities **must have AT LEAST ONE** of these components |
| `WithAny(params ComponentType[])` | Entities **must have AT LEAST ONE** of these component types |
| `WithNone(params Type[])` | Entities **must NOT have** any of these components (enabled) |
| `WithNone(params ComponentType[])` | Entities **must NOT have** any of these component types (enabled) |
| `WithPresent(params Type[])` | Components must be **present** (enabled or disabled) |
| `WithPresent(params ComponentType[])` | Component types must be **present** (enabled or disabled) |
| `WithAbsent(params Type[])` | Components must **NOT be present** at all |
| `WithAbsent(params ComponentType[])` | Component types must **NOT be present** at all |
| `WithDisabled(params Type[])` | Components must be **explicitly disabled** |
| `WithDisabled(params ComponentType[])` | Component types must be **explicitly disabled** |

#### Query Options

| Method | Description |
|--------|-------------|
| `WithOptions(EntityQueryOptions)` | Sets custom query options |
| `IncludeDisabled()` | Include disabled entities in the query |
| `IncludePrefabs()` | Include prefab entities in the query |
| `IncludeSystems()` | Include system entities in the query |
| `FilterWriteGroup()` | Filter entities that do not have EntityGuid component (usually runtime entities) |

#### Execution Methods

| Method | Return Type | Description |
|--------|------------|-------------|
| `ToArray()` | `NativeArray<Entity>` | Executes query and returns all matching entities (uses cached query) |
| `FirstOrDefault()` | `Entity` | Returns first matching entity or `Entity.Null` |
| `Count()` | `int` | Returns the count of matching entities |
| `Any()` | `bool` | Returns true if any entities match |

### Basic Query Example

```csharp
var entities = EntityLookupService.Query()
    .WithAll(typeof(Health), typeof(LocalToWorld))
    .WithNone(typeof(Disabled))
    .ToArray();

foreach (var entity in entities) {
    // Process entity
}

entities.Dispose();
```

### Complex Query Example

```csharp
// Find all living enemies that are not bosses, have AI enabled, and are stunnable
var entities = EntityLookupService.Query()
    .WithAll(typeof(Enemy), typeof(Health), typeof(LocalToWorld))  // Must have all these (enabled)
    .WithAny(typeof(Minion), typeof(NPC))                           // Must have at least one
    .WithNone(typeof(Disabled))                                     // Must NOT have Disabled (enabled)
    .WithPresent(typeof(Stunned))                                   // Must have Stunned (enabled or disabled)
    .WithAbsent(typeof(Boss))                                       // Must NOT have Boss at all
    .WithDisabled(typeof(Sleeping))                                 // Must have Sleeping but it's DISABLED
    .IncludeDisabled()                                              // Include disabled entities
    .ToArray();

foreach (var entity in entities) {
    // Process entity
}

entities.Dispose();
```

### Understanding Component States

- **All/Any/None**: Checks if component is **present AND enabled**
- **Present**: Checks if component exists (enabled or disabled)
- **Absent**: Checks if component does not exist at all
- **Disabled**: Checks if component exists but is **explicitly disabled**

```csharp
// Example: Find enemies with disabled AI
var dormantEnemies = EntityLookupService.Query()
    .WithAll(typeof(Enemy))       // Must have Enemy component (enabled)
    .WithDisabled(typeof(AI))     // Must have AI component but it's disabled
    .ToArray();
```

---

## Simplified Query Methods

For simple queries, use these convenience methods that don't require the builder pattern. These methods also benefit from query caching.

### QueryAll - Single Type

```csharp
public static NativeArray<Entity> QueryAll<T>() where T : IComponentData
public static NativeArray<Entity> QueryAll<T>(EntityQueryOptions options) where T : IComponentData
```

Query entities that have the specified component type T.

**Parameters:**
- `options` (optional): Query options like `EntityQueryOptions.IncludeDisabledEntities`

**Returns:** `NativeArray<Entity>` - Must be disposed after use

**Example:**
```csharp
// Find all entities with User component
var users = EntityLookupService.QueryAll<User>();
foreach (var user in users) {
    // Process user
}
users.Dispose();

// Include disabled entities
var allUsers = EntityLookupService.QueryAll<User>(EntityQueryOptions.IncludeDisabledEntities);
```

### QueryAll - Multiple Types (Generic)

```csharp
public static NativeArray<Entity> QueryAll<T1, T2>() 
    where T1 : IComponentData where T2 : IComponentData
    
public static NativeArray<Entity> QueryAll<T1, T2, T3>() 
    where T1 : IComponentData where T2 : IComponentData where T3 : IComponentData
```

Query entities that have **ALL** specified component types.

**Example:**
```csharp
// Find entities with both PlayerCharacter and Equipment
var players = EntityLookupService.QueryAll<PlayerCharacter, Equipment>();

// Find entities with three specific components
var entities = EntityLookupService.QueryAll<Health, LocalToWorld, Movement>();
```

### QueryAll - Type Array

```csharp
public static NativeArray<Entity> QueryAll(params Type[] types)
public static NativeArray<Entity> QueryAll(EntityQueryOptions options, params Type[] types)
public static NativeArray<Entity> QueryAll(params ComponentType[] componentTypes)
public static NativeArray<Entity> QueryAll(EntityQueryOptions options, params ComponentType[] componentTypes)
```

Query entities using Type[] or ComponentType[] arrays.

**Example:**
```csharp
// Using Type array
var entities = EntityLookupService.QueryAll(typeof(Health), typeof(LocalToWorld));

// Using ComponentType array
var types = new ComponentType[] { 
    ComponentType.ReadOnly<Health>(), 
    ComponentType.ReadOnly<LocalToWorld>() 
};
var entities = EntityLookupService.QueryAll(types);

// With options
var allEntities = EntityLookupService.QueryAll(
    EntityQueryOptions.IncludeDisabledEntities, 
    typeof(Health)
);
```

### QueryAny

```csharp
public static NativeArray<Entity> QueryAny(params Type[] types)
public static NativeArray<Entity> QueryAny(EntityQueryOptions options, params Type[] types)
public static NativeArray<Entity> QueryAny(params ComponentType[] componentTypes)
public static NativeArray<Entity> QueryAny(EntityQueryOptions options, params ComponentType[] componentTypes)
```

Query entities that have **AT LEAST ONE** of the specified components.

**Example:**
```csharp
// Find entities that are either Enemy or Boss
var hostiles = EntityLookupService.QueryAny(typeof(Enemy), typeof(Boss));

// With options
var spawners = EntityLookupService.QueryAny(
    EntityQueryOptions.IncludePrefab, 
    typeof(UnitSpawner), 
    typeof(TileSpawner)
);
```

---

## Spatial Queries

Query entities based on their position in the world using the game's tile-based spatial lookup system.

### GetAllEntitiesInRadius

```csharp
public static NativeList<Entity> GetAllEntitiesInRadius(float2 center, float radius)
public static NativeList<Entity> GetAllEntitiesInRadius(float3 center, float radius)
```

Get **ALL** entities within a radius, regardless of components.

**Parameters:**
- `center`: World position center (float2 uses x/z, float3 uses x/z and ignores y)
- `radius`: Search radius in world units

**Returns:** `NativeList<Entity>` - Must be disposed after use

**Example:**
```csharp
// Using float2 (x, z coordinates)
var entities = EntityLookupService.GetAllEntitiesInRadius(new float2(100, 100), 50f);
Logger.Info($"Found {entities.Length} entities within 50 units");
entities.Dispose();

// Using float3 (automatically uses x and z)
var playerPos = player.Read<LocalToWorld>().Position;
var nearbyEntities = EntityLookupService.GetAllEntitiesInRadius(playerPos, 30f);
```

### GetEntitiesInRadius - Single Type

```csharp
public static NativeList<Entity> GetEntitiesInRadius<T>(float2 center, float radius) 
    where T : IComponentData
    
public static NativeList<Entity> GetEntitiesInRadius<T>(float3 center, float radius) 
    where T : IComponentData
```

Get entities within a radius that have the specified component type T.

**Example:**
```csharp
// Find all enemies within 30 units
var playerPos = player.Read<LocalToWorld>().Position;
var nearbyEnemies = EntityLookupService.GetEntitiesInRadius<Enemy>(playerPos, 30f);

foreach (var enemy in nearbyEnemies) {
    // Process enemy
}

nearbyEnemies.Dispose();
```

### GetEntitiesInRadius - Multiple Types (Generic)

```csharp
public static NativeList<Entity> GetEntitiesInRadius<T1, T2>(float2 center, float radius)
    where T1 : IComponentData where T2 : IComponentData
    
public static NativeList<Entity> GetEntitiesInRadius<T1, T2>(float3 center, float radius)
    where T1 : IComponentData where T2 : IComponentData

public static NativeList<Entity> GetEntitiesInRadius<T1, T2, T3>(float2 center, float radius)
    where T1 : IComponentData where T2 : IComponentData where T3 : IComponentData
    
public static NativeList<Entity> GetEntitiesInRadius<T1, T2, T3>(float3 center, float radius)
    where T1 : IComponentData where T2 : IComponentData where T3 : IComponentData
```

Get entities within a radius that have **ALL** specified component types.

**Example:**
```csharp
// Find enemies with health component nearby
var enemiesWithHealth = EntityLookupService.GetEntitiesInRadius<Enemy, Health>(
    playerPos, 
    50f
);

// Find entities with three specific components
var fullQuery = EntityLookupService.GetEntitiesInRadius<Enemy, Health, LocalToWorld>(
    centerPos, 
    100f
);
```

### GetEntitiesInRadius - Type/ComponentType Arrays

```csharp
public static NativeList<Entity> GetEntitiesInRadius(float2 center, float radius, params Type[] types)
public static NativeList<Entity> GetEntitiesInRadius(float3 center, float radius, params Type[] types)
public static NativeList<Entity> GetEntitiesInRadius(float2 center, float radius, params ComponentType[] componentTypes)
public static NativeList<Entity> GetEntitiesInRadius(float3 center, float radius, params ComponentType[] componentTypes)
```

Get entities within a radius using Type[] or ComponentType[] arrays for component filtering.

**Example:**
```csharp
// Using Type array
var center = new float2(100, 100);
var entities = EntityLookupService.GetEntitiesInRadius(
    center, 
    50f, 
    typeof(Enemy), 
    typeof(Health)
);

// Using ComponentType array
var componentTypes = new ComponentType[] { 
    ComponentType.ReadOnly<Enemy>(), 
    ComponentType.ReadOnly<Health>() 
};
var entities = EntityLookupService.GetEntitiesInRadius(center, 50f, componentTypes);

// Float3 version
var entities = EntityLookupService.GetEntitiesInRadius(
    new float3(100, 0, 100), 
    50f, 
    typeof(Destructible)
);
```

---

## Entity Destruction

Destroy entities within a radius based on component filters. These methods return the number of entities destroyed.

### ClearAllEntitiesInRadius

```csharp
public static int ClearAllEntitiesInRadius(float2 center, float radius)
public static int ClearAllEntitiesInRadius(float3 center, float radius)
```

Destroy **ALL** entities within a radius (except PlayerCharacter and User entities, which are automatically protected).

**Parameters:**
- `center`: World position center
- `radius`: Destruction radius in world units

**Returns:** `int` - Number of entities destroyed

**Example:**
```csharp
// Clear 50 unit radius area
int destroyed = EntityLookupService.ClearAllEntitiesInRadius(
    new float2(100, 100), 
    50f
);
Logger.Info($"Destroyed {destroyed} entities");

// Using float3 (uses x and z)
var explosionCenter = new float3(100, 0, 100);
int count = EntityLookupService.ClearAllEntitiesInRadius(explosionCenter, 25f);
```

### ClearEntitiesInRadius - Single Type

```csharp
public static int ClearEntitiesInRadius<T>(float2 center, float radius) 
    where T : IComponentData
    
public static int ClearEntitiesInRadius<T>(float3 center, float radius) 
    where T : IComponentData
```

Destroy entities within a radius that have the specified component type T.

**Example:**
```csharp
// Destroy all enemies within 50 units
var center = new float2(100, 100);
int enemiesDestroyed = EntityLookupService.ClearEntitiesInRadius<Enemy>(center, 50f);

// Using float3
var bossPos = boss.Read<LocalToWorld>().Position;
int minionsDestroyed = EntityLookupService.ClearEntitiesInRadius<Minion>(bossPos, 100f);
```

### ClearEntitiesInRadius - Type/ComponentType Arrays

```csharp
public static int ClearEntitiesInRadius(float2 center, float radius, params Type[] types)
public static int ClearEntitiesInRadius(float3 center, float radius, params Type[] types)
public static int ClearEntitiesInRadius(float2 center, float radius, params ComponentType[] componentTypes)
public static int ClearEntitiesInRadius(float3 center, float radius, params ComponentType[] componentTypes)
```

Destroy entities within a radius that have **ALL** specified components.

**Parameters:**
- `center`: World position center
- `radius`: Destruction radius
- `types` or `componentTypes`: Components that entities must have to be destroyed

**Returns:** `int` - Number of entities destroyed

**Example:**
```csharp
// Destroy entities that are both Destructible and PhysicsObject
var explosionCenter = new float3(100, 0, 100);
int destroyed = EntityLookupService.ClearEntitiesInRadius(
    explosionCenter, 
    15f, 
    typeof(Destructible), 
    typeof(PhysicsObject)
);

// Using ComponentType array
var types = new ComponentType[] { 
    ComponentType.ReadOnly<Enemy>(),
    ComponentType.ReadOnly<Minion>()
};
int minionCount = EntityLookupService.ClearEntitiesInRadius(center, 50f, types);

// Clear temporary spawned objects
int cleared = EntityLookupService.ClearEntitiesInRadius(
    playerPos,
    200f,
    typeof(TempObject),
    typeof(Expired)
);
```

---

## Utility Methods

### ConvertPosToTileGrid

```csharp
public static float2 ConvertPosToTileGrid(float2 pos)
public static float3 ConvertPosToTileGrid(float3 pos)
```

Convert world positions to tile grid coordinates used by the spatial lookup system.

**Parameters:**
- `pos`: World position to convert

**Returns:** Grid coordinates (formula: `(pos * 2) + 6400`)

**Example:**
```csharp
// float2 version
var worldPos = new float2(100, 100);
var gridPos = EntityLookupService.ConvertPosToTileGrid(worldPos);
// Result: float2(6600, 6600)

// float3 version (y coordinate is preserved)
var worldPos3D = new float3(100, 50, 100);
var gridPos3D = EntityLookupService.ConvertPosToTileGrid(worldPos3D);
// Result: float3(6600, 50, 6600)
```

**Technical Note:** The game uses a tile-based grid system offset by 6400 units. This method converts world coordinates to grid coordinates for internal spatial queries.

---

## Examples

### Example 1: Find All Nearby Enemies

```csharp
using ProjectM;
using Unity.Entities;

var playerPos = player.Read<LocalToWorld>().Position;
var nearbyEnemies = EntityLookupService.GetEntitiesInRadius<Enemy>(playerPos, 20f);

Logger.Info($"Found {nearbyEnemies.Length} enemies nearby");

foreach (var enemy in nearbyEnemies) {
    var enemyHealth = enemy.Read<Health>();
    Logger.Info($"Enemy health: {enemyHealth.Value}/{enemyHealth.MaxHealth}");
}

nearbyEnemies.Dispose(); // Always dispose!
```

### Example 2: Area Destruction with Filters

```csharp
// Destroy specific entity types in an explosion radius
var explosionCenter = new float3(100, 0, 100);
var radius = 15f;

int destroyed = EntityLookupService.ClearEntitiesInRadius(
    explosionCenter, 
    radius, 
    typeof(Destructible), 
    typeof(PhysicsObject)
);

MessageService.SendSystemMessage($"Explosion destroyed {destroyed} objects!");
```

### Example 3: Complex Entity Query with Filters

```csharp
// Find all living enemies that are not bosses, have health, and aren't invulnerable
var entities = EntityLookupService.Query()
    .WithAll(typeof(Enemy), typeof(Health), typeof(LocalToWorld))
    .WithNone(typeof(Disabled), typeof(Invulnerable))
    .WithAbsent(typeof(Boss))
    .ToArray();

Logger.Info($"Found {entities.Length} targetable enemies");

foreach (var entity in entities) {
    var health = entity.Read<Health>();
    var position = entity.Read<LocalToWorld>().Position;
    Logger.Info($"Enemy at {position} with {health.Value} HP");
}

entities.Dispose();
```

### Example 4: Check Entity Existence

```csharp
// Check if any enemies exist nearby before spawning more
var playerPos = player.Read<LocalToWorld>().Position;

bool hasEnemiesNearby = EntityLookupService.Query()
    .WithAll(typeof(Enemy))
    .WithNone(typeof(Disabled))
    .Any();

if (!hasEnemiesNearby) {
    // Spawn enemies
    SpawnerService.Spawn(enemyPrefab, playerPos);
}
```

### Example 5: Find First Available Target

```csharp
// Find the first valid enemy target for auto-targeting
var target = EntityLookupService.Query()
    .WithAll(typeof(Enemy), typeof(Health))
    .WithNone(typeof(Disabled), typeof(Invulnerable), typeof(Stealth))
    .FirstOrDefault();

if (target != Entity.Null) {
    // Attack target
    var targetHealth = target.Read<Health>();
    Logger.Info($"Targeting enemy with {targetHealth.Value} HP");
} else {
    Logger.Info("No valid targets found");
}
```

### Example 6: Query with Disabled Components

```csharp
// Find enemies that have AI component but it's currently disabled (sleeping)
var dormantEnemies = EntityLookupService.Query()
    .WithAll(typeof(Enemy))
    .WithDisabled(typeof(Aggroable))
    .ToArray();

Logger.Info($"Found {dormantEnemies.Length} dormant enemies");

foreach (var enemy in dormantEnemies) {
    // Wake up enemy by enabling the Aggroable component
    enemy.Write(new Aggroable { Value = true });
}

dormantEnemies.Dispose();
```

### Example 7: Radius Query with Multiple Component Types

```csharp
// Find all spawners within a large radius
var centerPos = new float3(0, 0, 0);
var spawners = EntityLookupService.GetEntitiesInRadius<UnitSpawner, SpawnerSettings>(
    centerPos, 
    500f
);

Logger.Info($"Found {spawners.Length} active spawners");

foreach (var spawner in spawners) {
    var settings = spawner.Read<SpawnerSettings>();
    var position = spawner.Read<LocalToWorld>().Position;
    Logger.Info($"Spawner at {position} with interval {settings.Interval}");
}

spawners.Dispose();
```

### Example 8: Periodic Cleanup Task

```csharp
// Clean up expired temporary entities every 60 seconds
ActionScheduler.Repeating(() => {
    int cleaned = EntityLookupService.Query()
        .WithAll(typeof(TemporaryEntity), typeof(Expired))
        .Count();
    
    if (cleaned > 0) {
        var expiredEntities = EntityLookupService.QueryAll<TemporaryEntity, Expired>();
        
        foreach (var entity in expiredEntities) {
            entity.Destroy();
        }
        
        expiredEntities.Dispose();
        Logger.Info($"Cleaned up {cleaned} expired entities");
    }
}, 60f);
```

### Example 9: Cache Management

```csharp
// Monitor query cache size
EventManager.On(ServerEvents.OnInitialize, () => {
    ActionScheduler.Repeating(() => {
        int cacheSize = EntityLookupService.GetCachedQueryCount();
        
        if (cacheSize > 100) {
            Logger.Warning($"Query cache has grown to {cacheSize} queries");
        }
    }, 300f); // Check every 5 minutes
});

// Clear cache on mod unload
public void OnUnload() {
    EntityLookupService.ClearQueryCache();
    Logger.Info("Cleared EntityLookupService query cache");
}
```
## Best Practices

### 1. Always Dispose NativeCollections

**Always dispose** `NativeArray<Entity>` and `NativeList<Entity>` when you're done to prevent memory leaks.

```csharp
// ✅ Good
var entities = EntityLookupService.QueryAll<Enemy>();
try {
    foreach (var entity in entities) {
        // Process entity
    }
} finally {
    entities.Dispose();
}

// ❌ Bad - Memory leak!
var entities = EntityLookupService.QueryAll<Enemy>();
foreach (var entity in entities) {
    // Process entity
}
// Never disposed!
```

### 2. Use Generic Methods for Type Safety

Prefer generic methods over Type[] arrays when possible for better type safety and performance.

```csharp
// ✅ Good - Type-safe and faster
var enemies = EntityLookupService.GetEntitiesInRadius<Enemy>(pos, radius);

// ⚠️ Works but not ideal
var enemies = EntityLookupService.GetEntitiesInRadius(pos, radius, typeof(Enemy));
```

### 3. Use Query Builder for Complex Queries

Use the Query Builder instead of manually filtering results.

```csharp
// ✅ Good - Efficient ECS query
var entities = EntityLookupService.Query()
    .WithAll(typeof(Enemy))
    .WithNone(typeof(Disabled))
    .ToArray();

// ❌ Bad - Manual filtering is slower
var allEnemies = EntityLookupService.QueryAll<Enemy>();
var filtered = new List<Entity>();
foreach (var enemy in allEnemies) {
    if (!EntityManager.HasComponent<Disabled>(enemy)) {
        filtered.Add(enemy);
    }
}
allEnemies.Dispose();
```

### 4. Prefer Count() or Any() Over ToArray().Length

When you only need to check existence or count, avoid creating the full array.

```csharp
// ✅ Good - More efficient
bool hasEnemies = EntityLookupService.Query()
    .WithAll(typeof(Enemy))
    .Any();

int enemyCount = EntityLookupService.Query()
    .WithAll(typeof(Enemy))
    .Count();

// ❌ Bad - Unnecessary array allocation
var enemies = EntityLookupService.QueryAll<Enemy>();
bool hasEnemies = enemies.Length > 0;
int enemyCount = enemies.Length;
enemies.Dispose();
```

### 5. Be Cautious with Large Radius Queries

Spatial queries with very large radii can be expensive. Consider chunking or using smaller radii.

```csharp
// ✅ Good - Reasonable radius
var nearby = EntityLookupService.GetEntitiesInRadius<Enemy>(pos, 50f);

// ⚠️ Potentially expensive
var veryLarge = EntityLookupService.GetEntitiesInRadius<Enemy>(pos, 1000f);
```

### 6. Clear Query Cache on Mod Unload

Always clear the query cache when unloading your mod to prevent memory leaks.

```csharp
public void OnModUnload() {
    EntityLookupService.ClearQueryCache();
    Logger.Info("Cleared query cache");
}
```

### 7. Understand Component State Differences

Know the difference between All/None, Present/Absent, and Disabled filters.

```csharp
// WithAll: Component exists AND is enabled
.WithAll(typeof(AI))

// WithNone: Component doesn't exist OR is disabled
.WithNone(typeof(AI))

// WithPresent: Component exists (enabled OR disabled)
.WithPresent(typeof(AI))

// WithAbsent: Component doesn't exist at all
.WithAbsent(typeof(AI))

// WithDisabled: Component exists but is explicitly disabled
.WithDisabled(typeof(AI))
```

### 8. Reuse Queries When Possible

The service caches queries automatically, so repeated identical queries are optimized.

```csharp
// These will use the same cached query internally
var enemies1 = EntityLookupService.QueryAll<Enemy>();
enemies1.Dispose();

var enemies2 = EntityLookupService.QueryAll<Enemy>(); // Reuses cached query
enemies2.Dispose();
```

## Performance Considerations

### Query Caching

- EntityLookupService automatically caches `EntityQuery` objects based on query parameters
- Identical queries reuse the same cached query for optimal performance
- Cache is thread-safe using internal locking mechanisms
- Clear cache during mod unload with `ClearQueryCache()` to prevent memory leaks

### Spatial Query Performance

- Spatial queries use V Rising's tile-based spatial lookup system (`UpdateTileCellsSystem`)
- The game divides the world into tiles for efficient spatial queries
- `GetAllEntitiesInRadius()` queries the tile grid, then filters by distance
- Larger radii query more tiles, increasing performance cost
- Consider limiting radius to 100 units or less for frequent queries

### Query Complexity

- Simple queries (WithAll with 1-2 components) are very fast
- Complex queries with multiple filters (Any, None, Present, Absent, Disabled) are slower
- Each additional filter adds overhead to the query
- Prefer fewer, more specific filters over many loose filters

### Memory Management

- `NativeArray<Entity>` uses unmanaged memory - must be disposed
- `NativeList<Entity>` uses unmanaged memory - must be disposed
- Not disposing causes memory leaks that accumulate over time
- Use try-finally blocks or using statements (if supported) to ensure disposal

### Optimization Tips

1. **Cache entity references** instead of querying repeatedly
2. **Use `FirstOrDefault()`** when you only need one entity
3. **Use `Any()` or `Count()`** instead of `ToArray()` when checking existence
4. **Limit spatial query radius** to necessary range
5. **Batch operations** instead of querying in loops
6. **Avoid querying every frame** - use timers or events instead

### Example: Efficient Periodic Query

```csharp
// ✅ Good - Query once per second
ActionScheduler.Repeating(() => {
    var enemies = EntityLookupService.QueryAll<Enemy>();
    ProcessEnemies(enemies);
    enemies.Dispose();
}, 1f);

// ❌ Bad - Query every frame (expensive!)
ActionScheduler.OncePerFrame(() => {
    var enemies = EntityLookupService.QueryAll<Enemy>();
    ProcessEnemies(enemies);
    enemies.Dispose();
});
```

---

## Thread Safety

⚠️ **Warning:** EntityLookupService is **NOT thread-safe**. 

- All methods must be called from the **main thread**
- Methods interact with Unity's `EntityManager` which is not thread-safe
- Spatial queries access game systems that require main thread
- Query cache uses locking, but underlying ECS operations do not

**Do NOT** use EntityLookupService in:
- Background threads
- Parallel jobs
- Async callbacks running on thread pool

---

## Common Pitfalls

### 1. Forgetting to Dispose

```csharp
// ❌ Memory leak
void Update() {
    var entities = EntityLookupService.QueryAll<Enemy>();
    // ... use entities
    // FORGOT TO DISPOSE!
}
```

### 2. Querying Every Frame

```csharp
// ❌ Performance killer
void Update() {
    var enemies = EntityLookupService.GetEntitiesInRadius<Enemy>(pos, 100f);
    // Process...
    enemies.Dispose();
}

// ✅ Better - Query periodically
ActionScheduler.Repeating(() => {
    var enemies = EntityLookupService.GetEntitiesInRadius<Enemy>(pos, 100f);
    // Process...
    enemies.Dispose();
}, 0.5f); // Every half second
```

### 3. Using Wrong Component Filter

```csharp
// ❌ Wrong - WithNone matches disabled components too
var entities = EntityLookupService.Query()
    .WithAll(typeof(Enemy))
    .WithNone(typeof(Dead)) // This matches Dead disabled OR absent
    .ToArray();

// ✅ Correct - WithAbsent only matches if component doesn't exist
var entities = EntityLookupService.Query()
    .WithAll(typeof(Enemy))
    .WithAbsent(typeof(Dead))
    .ToArray();
```

### 4. Not Checking Entity.Null

```csharp
// ❌ Potential error
var target = EntityLookupService.Query()
    .WithAll(typeof(Enemy))
    .FirstOrDefault();

target.Write(new Health { Value = 0 }); // Crashes if target is Entity.Null!

// ✅ Correct - Check before use
var target = EntityLookupService.Query()
    .WithAll(typeof(Enemy))
    .FirstOrDefault();

if (target != Entity.Null) {
    target.Write(new Health { Value = 0 });
}
```

### 5. Large Radius on High-Frequency Queries

```csharp
// ❌ Very expensive
ActionScheduler.Repeating(() => {
    var entities = EntityLookupService.GetAllEntitiesInRadius(pos, 1000f); // Huge radius!
    // Process...
    entities.Dispose();
}, 0.1f); // Every 0.1 seconds!

// ✅ Better
ActionScheduler.Repeating(() => {
    var entities = EntityLookupService.GetAllEntitiesInRadius(pos, 50f); // Reasonable radius
    // Process...
    entities.Dispose();
}, 1f); // Every second
```

---

## Technical Details

### Spatial Lookup System

EntityLookupService uses V Rising's `UpdateTileCellsSystem` for spatial queries:

- World is divided into a tile grid
- Each tile is 0.5 world units (hence the `* 2` in conversion)
- Grid is offset by 6400 units (center of the map)
- Formula: `gridPos = (worldPos * 2) + 6400`
- Queries retrieve entities from relevant tiles based on radius

### Query Hashing

Queries are cached using a hash computed from:
- All component types (All, Any, None, Present, Absent, Disabled)
- Entity query options
- Hash collisions are handled by exact comparison

The cache uses a `Dictionary<int, EntityQuery>` protected by a lock object.

---

## Related Services

- [PlayerService](PlayerService.md) - Player-specific entity operations
- [SpawnerService](SpawnerService.md) - Entity spawning
- [BuffService](BuffService.md) - Buff/debuff management
- [TeleportService](TeleportService.md) - Entity teleportation
- [GameSystems](../Systems/GameSystems.md) - Core ECS systems access

---

## Requirements

- **Unity ECS** (Entities package)
- **Il2CppInterop** (for Il2Cpp type conversion)
- **Game Systems**: `UpdateTileCellsSystem`, `GameSystems.EntityManager`
- **ScarletCore Systems**: `GameSystems`

---

## License

Part of the ScarletCore framework.