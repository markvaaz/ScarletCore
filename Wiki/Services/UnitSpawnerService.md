# UnitSpawnerService

## Overview
The `UnitSpawnerService` provides methods for spawning units (NPCs, creatures, etc.) in the game world with various configurations. It supports spawning single or multiple units, immediate instantiation, custom lifetimes, spawn radii, and post-spawn actions.

## Methods

### Spawn
Spawns units at a specific position with validation and error handling.

**Signature:**
```csharp
bool Spawn(PrefabGUID prefabGUID, float3 position, int count = 1, float minRange = 1, float maxRange = 8, float lifeTime = 0f)
```
**Parameters:**
- `prefabGUID`: The prefab GUID of the unit to spawn.
- `position`: The position to spawn at.
- `count`: Number of units to spawn (default: 1).
- `minRange`: Minimum spawn range (default: 1).
- `maxRange`: Maximum spawn range (default: 8).
- `lifeTime`: How long the units should live (0 = permanent, default: 0).

**Returns:**
- `bool`: True if spawn was successful.

**Example:**
```csharp
UnitSpawnerService.Spawn(prefabGUID, new float3(10, 0, 20), 3);
```

---

### ImmediateSpawn (Single)
Immediately spawns a single unit at the specified position.

**Signature:**
```csharp
Entity ImmediateSpawn(PrefabGUID prefabGUID, float3 position, float minRange = 1, float maxRange = 1, float lifeTime = 0f, Entity owner = default)
```
**Parameters:**
- `prefabGUID`: The prefab GUID of the unit to spawn.
- `position`: The position to spawn at.
- `minRange`: Minimum spawn range (default: 1).
- `maxRange`: Maximum spawn range (default: 1).
- `lifeTime`: How long the unit should live (0 = permanent, default: 0).
- `owner`: The owner entity (optional).

**Returns:**
- `Entity`: The spawned entity or `Entity.Null` if failed.

**Example:**
```csharp
var entity = UnitSpawnerService.ImmediateSpawn(prefabGUID, new float3(5, 0, 5));
```

---

### ImmediateSpawn (Multiple)
Immediately spawns multiple units around the specified position.

**Signature:**
```csharp
List<Entity> ImmediateSpawn(PrefabGUID prefabGUID, float3 position, int count, float minRange = 1, float maxRange = 8, float lifeTime = 0f, Entity owner = default)
```
**Parameters:**
- `prefabGUID`: The prefab GUID of the unit to spawn.
- `position`: The center position to spawn around.
- `count`: Number of units to spawn.
- `minRange`: Minimum spawn range (default: 1).
- `maxRange`: Maximum spawn range (default: 8).
- `lifeTime`: How long the units should live (0 = permanent, default: 0).
- `owner`: The owner entity (optional).

**Returns:**
- `List<Entity>`: List of spawned entities (empty if failed).

**Example:**
```csharp
var entities = UnitSpawnerService.ImmediateSpawn(prefabGUID, new float3(0, 0, 0), 5);
```

---

### SpawnAtPosition
Spawns a single unit at an exact position.

**Signature:**
```csharp
bool SpawnAtPosition(PrefabGUID prefabGUID, float3 position, float lifeTime)
```
**Parameters:**
- `prefabGUID`: The prefab GUID of the unit to spawn.
- `position`: The exact position to spawn at.
- `lifeTime`: How long the unit should live.

**Returns:**
- `bool`: True if spawn was successful.

**Example:**
```csharp
UnitSpawnerService.SpawnAtPosition(prefabGUID, new float3(1, 0, 1), 60f);
```

---

### SpawnInRadius
Spawns multiple units in a radius around a position.

**Signature:**
```csharp
bool SpawnInRadius(PrefabGUID prefabGUID, float3 centerPosition, int count, float radius, float lifeTime)
```
**Parameters:**
- `prefabGUID`: The prefab GUID of the unit to spawn.
- `centerPosition`: The center position to spawn around.
- `count`: Number of units to spawn.
- `radius`: Spawn radius around the center position.
- `lifeTime`: How long the units should live.

**Returns:**
- `bool`: True if spawn was successful.

**Example:**
```csharp
UnitSpawnerService.SpawnInRadius(prefabGUID, new float3(0, 0, 0), 10, 5f, 30f);
```

---

### SpawnWithPostAction
Spawns a unit and executes a post-spawn action.

**Signature:**
```csharp
bool SpawnWithPostAction(PrefabGUID prefabGUID, float3 position, float lifeTime, Action<Entity> postSpawnAction)
```
**Parameters:**
- `prefabGUID`: The prefab GUID of the unit to spawn.
- `position`: The position to spawn at.
- `lifeTime`: How long the unit should live.
- `postSpawnAction`: Action to execute after spawning (receives the spawned entity).

**Returns:**
- `bool`: True if spawn was successful.

**Example:**
```csharp
UnitSpawnerService.SpawnWithPostAction(prefabGUID, new float3(2, 0, 2), 10f, entity => {
    // Custom logic after spawn
});
```

---

### GetDurationHash
Generates a unique duration hash based on the current time.

**Signature:**
```csharp
long GetDurationHash()
```
**Returns:**
- `long`: Unique hash value.

**Example:**
```csharp
long hash = UnitSpawnerService.GetDurationHash();
```

## Notes
- Always validate the `prefabGUID` and input parameters before spawning.
- Use `ImmediateSpawn` for direct instantiation and access to the spawned entity/entities.
- Use `SpawnWithPostAction` to perform custom logic after a unit is spawned.
- Lifetimes set to `0` mean the unit is permanent.
- For random spawn positions, the range is calculated using `minRange` and `maxRange`.
