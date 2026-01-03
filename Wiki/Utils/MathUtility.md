# MathUtility

## Overview

`MathUtility` is a comprehensive utility class providing mathematical operations for game development, including distance calculations, positioning, geometric operations, and collision detection. It's designed to work seamlessly with Unity's Entity Component System (ECS) and provides both 2D and 3D variants of most operations.

## Table of Contents

- [Distance Calculations](#distance-calculations)
- [Range Checks](#range-checks)
- [Random Position Generation](#random-position-generation)
- [Angle Calculations](#angle-calculations)
- [Interpolation and Clamping](#interpolation-and-clamping)
- [Direction Calculations](#direction-calculations)
- [Geometric Operations](#geometric-operations)
- [Grid Conversions](#grid-conversions)
- [Shape Collision Detection](#shape-collision-detection)
- [Rotation Operations](#rotation-operations)
- [Examples](#examples)
- [Best Practices](#best-practices)

---

## Distance Calculations

### Distance (Entity to Entity)

```csharp
public static float Distance(Entity A, Entity B)
public static float Distance2D(Entity A, Entity B)
```

Calculates the distance between two entities. The 2D version ignores the Y axis (useful for ground-based checks).

**Parameters:**
- `A`: First entity
- `B`: Second entity

**Returns:** Distance between entities, or `float.MaxValue` if either entity lacks `LocalTransform`

**Example:**
```csharp
var distance = MathUtility.Distance(player, enemy);
if (distance < 10f) {
    Log.Info("Enemy is close!");
}

// 2D distance (ignoring height difference)
var groundDistance = MathUtility.Distance2D(player, enemy);
```

### Distance (Entity to Position)

```csharp
public static float Distance(Entity entity, float3 position)
public static float Distance2D(Entity entity, float3 position)
```

Calculates the distance between an entity and a world position.

**Parameters:**
- `entity`: Source entity
- `position`: Target world position

**Returns:** Distance to position, or `float.MaxValue` if entity lacks `LocalTransform`

**Example:**
```csharp
var targetPos = new float3(100, 0, 100);
var distance = MathUtility.Distance(player, targetPos);
```

### Distance (Position to Position)

```csharp
public static float Distance(float3 positionA, float3 positionB)
public static float Distance2D(float3 positionA, float3 positionB)
```

Calculates the distance between two world positions.

**Parameters:**
- `positionA`: First position
- `positionB`: Second position

**Returns:** Distance between positions

**Example:**
```csharp
var pos1 = new float3(0, 0, 0);
var pos2 = new float3(10, 0, 10);
var distance = MathUtility.Distance(pos1, pos2);
// Result: ~14.14
```

---

## Range Checks

### IsInRange (Entity to Entity)

```csharp
public static bool IsInRange(Entity A, Entity B, float range)
public static bool IsInRange2D(Entity A, Entity B, float range)
```

Checks if two entities are within a specified range.

**Parameters:**
- `A`: First entity
- `B`: Second entity
- `range`: Maximum allowed distance

**Returns:** `true` if entities are within range

**Example:**
```csharp
if (MathUtility.IsInRange(player, enemy, 20f)) {
    // Enemy is within attack range
    AttackEnemy(enemy);
}

// Ground-based check (useful for abilities)
if (MathUtility.IsInRange2D(caster, target, 15f)) {
    CastSpell(target);
}
```

### IsInRange (Entity to Position)

```csharp
public static bool IsInRange(Entity entity, float3 position, float range)
public static bool IsInRange2D(Entity entity, float3 position, float range)
```

Checks if an entity is within range of a world position.

**Example:**
```csharp
var homePos = new float3(0, 0, 0);
if (MathUtility.IsInRange(player, homePos, 50f)) {
    Log.Info("Player is near home");
}
```

### IsInRange (Position to Position)

```csharp
public static bool IsInRange(float3 positionA, float3 positionB, float range)
public static bool IsInRange2D(float3 positionA, float3 positionB, float range)
```

Checks if two positions are within range of each other.

**Example:**
```csharp
if (MathUtility.IsInRange(spawnPoint, playerPos, 100f)) {
    // Too close to player, don't spawn here
}
```

---

## Random Position Generation

### GetRandomPositionInRadius

```csharp
public static float3 GetRandomPositionInRadius(float3 center, float radius)
```

Generates a random position within a circular radius around a center point. Y coordinate remains unchanged.

**Parameters:**
- `center`: Center position
- `radius`: Maximum radius

**Returns:** Random position within the specified radius

**Example:**
```csharp
var spawnCenter = new float3(100, 0, 100);
var randomPos = MathUtility.GetRandomPositionInRadius(spawnCenter, 25f);
SpawnerService.Spawn(enemyPrefab, randomPos);
```

### GetRandomPositionInRing

```csharp
public static float3 GetRandomPositionInRing(float3 center, float minRadius, float maxRadius)
```

Generates a random position within a ring (between two radii) around a center point. Useful for creating safe zones or spawn rings.

**Parameters:**
- `center`: Center position
- `minRadius`: Minimum radius (inner ring)
- `maxRadius`: Maximum radius (outer ring)

**Returns:** Random position within the ring

**Example:**
```csharp
// Spawn enemies in a ring around the boss (not too close)
var bossPos = boss.Read<LocalTransform>().Position;
var spawnPos = MathUtility.GetRandomPositionInRing(bossPos, 15f, 30f);
SpawnerService.Spawn(minionPrefab, spawnPos);
```

### GetRandomPositionAroundEntity

```csharp
public static float3 GetRandomPositionAroundEntity(Entity entity, float radius)
public static float3 GetRandomPositionAroundEntity(Entity entity, float minRadius, float maxRadius)
```

Generates a random position around an entity.

**Parameters:**
- `entity`: Center entity
- `radius`: Maximum radius (or `minRadius`/`maxRadius` for ring variant)

**Returns:** Random position around entity, or `float3.zero` if entity lacks `LocalTransform`

**Example:**
```csharp
// Spawn loot around killed enemy
var lootPos = MathUtility.GetRandomPositionAroundEntity(deadEnemy, 5f);
SpawnerService.Spawn(lootPrefab, lootPos);

// Spawn guards in a ring around castle
var guardPos = MathUtility.GetRandomPositionAroundEntity(castle, 10f, 20f);
```

---

## Angle Calculations

### NormalizeAngle

```csharp
public static float NormalizeAngle(float angle)
```

Normalizes an angle to be within the range [0, 2π].

**Parameters:**
- `angle`: Angle in radians

**Returns:** Normalized angle

**Example:**
```csharp
var angle = 7.5f; // > 2π
var normalized = MathUtility.NormalizeAngle(angle);
// Result: ~1.22 radians
```

### GetAngleBetween

```csharp
public static float GetAngleBetween(float3 from, float3 to)
public static float GetAngleBetween(Entity from, Entity to)
```

Calculates the angle between two positions or entities.

**Parameters:**
- `from`: Source position/entity
- `to`: Target position/entity

**Returns:** Angle in radians (0 if entity variant and either lacks `LocalTransform`)

**Example:**
```csharp
var angle = MathUtility.GetAngleBetween(player, enemy);
// Use angle for rotation, aiming, etc.

var angleRad = MathUtility.GetAngleBetween(startPos, endPos);
var angleDeg = angleRad * (180f / math.PI);
Log.Info("Angle:", angleDeg, "degrees");
```

---

## Interpolation and Clamping

### Lerp

```csharp
public static float3 Lerp(float3 from, float3 to, float t)
```

Linear interpolation between two positions with clamped t value (0-1).

**Parameters:**
- `from`: Start position
- `to`: End position
- `t`: Interpolation factor (0-1, automatically clamped)

**Returns:** Interpolated position

**Example:**
```csharp
var start = new float3(0, 0, 0);
var end = new float3(100, 0, 100);

// Midpoint
var mid = MathUtility.Lerp(start, end, 0.5f);

// Smooth movement over time
ActionScheduler.Repeating(() => {
    progress += 0.01f;
    var currentPos = MathUtility.Lerp(start, end, progress);
    entity.Write(new LocalTransform { Position = currentPos });
}, 0.016f); // ~60 FPS
```

### ClampPosition

```csharp
public static float3 ClampPosition(float3 position, float3 minBounds, float3 maxBounds)
```

Clamps a position within specified bounds.

**Parameters:**
- `position`: Position to clamp
- `minBounds`: Minimum bounds (x, y, z)
- `maxBounds`: Maximum bounds (x, y, z)

**Returns:** Clamped position

**Example:**
```csharp
var minBounds = new float3(-100, 0, -100);
var maxBounds = new float3(100, 50, 100);

var playerPos = player.Read<LocalTransform>().Position;
var clampedPos = MathUtility.ClampPosition(playerPos, minBounds, maxBounds);

if (clampedPos != playerPos) {
    // Player went out of bounds, teleport back
    TeleportService.Teleport(player, clampedPos);
}
```

### IsWithinBounds

```csharp
public static bool IsWithinBounds(float3 position, float3 minBounds, float3 maxBounds)
```

Checks if a position is within specified bounds.

**Parameters:**
- `position`: Position to check
- `minBounds`: Minimum bounds
- `maxBounds`: Maximum bounds

**Returns:** `true` if position is within bounds

**Example:**
```csharp
var arenaMin = new float3(-50, 0, -50);
var arenaMax = new float3(50, 20, 50);

if (!MathUtility.IsWithinBounds(playerPos, arenaMin, arenaMax)) {
    MessageService.SendMessage(player, "You left the arena!");
}
```

---

## Direction Calculations

### GetDirection

```csharp
public static float3 GetDirection(float3 from, float3 to)
public static float3 GetDirection(Entity from, Entity to)
```

Calculates the normalized direction vector between two positions or entities.

**Parameters:**
- `from`: Source position/entity
- `to`: Target position/entity

**Returns:** Normalized direction vector (`float3.zero` if entity variant and either lacks `LocalTransform`)

**Example:**
```csharp
// Calculate knockback direction
var direction = MathUtility.GetDirection(attacker, target);
var knockbackForce = direction * 10f;

// Move entity towards target
var moveDirection = MathUtility.GetDirection(entity, target);
var newPos = entityPos + moveDirection * speed * deltaTime;
```

---

## Geometric Operations

### GetClosestPointOnLine

```csharp
public static float3 GetClosestPointOnLine(float3 point, float3 lineStart, float3 lineEnd)
```

Finds the closest point on a line segment to a given point.

**Parameters:**
- `point`: Reference point
- `lineStart`: Start of line segment
- `lineEnd`: End of line segment

**Returns:** Closest point on the line segment

**Example:**
```csharp
// Find closest point on a wall segment
var wallStart = new float3(0, 0, 0);
var wallEnd = new float3(100, 0, 0);
var playerPos = new float3(50, 0, 25);

var closestPoint = MathUtility.GetClosestPointOnLine(playerPos, wallStart, wallEnd);
// Result: float3(50, 0, 0) - on the wall

// Calculate distance to wall
var distanceToWall = MathUtility.Distance(playerPos, closestPoint);
```

---

## Grid Conversions

### WorldToGrid

```csharp
public static int2 WorldToGrid(float3 worldPosition, float gridSize = 1f)
```

Converts world position to grid coordinates.

**Parameters:**
- `worldPosition`: World position
- `gridSize`: Size of each grid cell (default: 1.0)

**Returns:** Grid coordinates (int2)

**Example:**
```csharp
var worldPos = new float3(15.7f, 0, 23.4f);
var gridPos = MathUtility.WorldToGrid(worldPos, 5f);
// Result: int2(3, 4) - grid cell at [3, 4]
```

### GridToWorld

```csharp
public static float3 GridToWorld(int2 gridPosition, float gridSize = 1f, float yHeight = 0f)
```

Converts grid coordinates to world position (center of grid cell).

**Parameters:**
- `gridPosition`: Grid coordinates
- `gridSize`: Size of each grid cell (default: 1.0)
- `yHeight`: Y coordinate for the world position (default: 0.0)

**Returns:** World position at center of grid cell

**Example:**
```csharp
var gridPos = new int2(3, 4);
var worldPos = MathUtility.GridToWorld(gridPos, 5f, 0f);
// Result: float3(17.5, 0, 22.5) - center of grid cell
```

---

## Shape Collision Detection

### IsPointInCircle

```csharp
public static bool IsPointInCircle(float3 point, float3 circleCenter, float radius)
```

Checks if a point is within a circular area (2D, ignores Y axis).

**Parameters:**
- `point`: Point to check
- `circleCenter`: Center of the circle
- `radius`: Radius of the circle

**Returns:** `true` if point is within the circle

**Example:**
```csharp
var abilityCenter = new float3(100, 0, 100);
var abilityRadius = 20f;

if (MathUtility.IsPointInCircle(playerPos, abilityCenter, abilityRadius)) {
    // Player is hit by AoE ability
    DamagePlayer(player, 50);
}
```

### IsPointInRectangle

```csharp
public static bool IsPointInRectangle(float3 point, float3 rectCenter, float2 rectSize)
```

Checks if a point is within a rectangular area (2D, ignores Y axis).

**Parameters:**
- `point`: Point to check
- `rectCenter`: Center of the rectangle
- `rectSize`: Size of the rectangle (width, height)

**Returns:** `true` if point is within the rectangle

**Example:**
```csharp
var zoneCenter = new float3(0, 0, 0);
var zoneSize = new float2(100, 50); // 100 wide, 50 deep

if (MathUtility.IsPointInRectangle(playerPos, zoneCenter, zoneSize)) {
    // Player entered the zone
    MessageService.SendMessage(player, "You entered the safe zone");
}
```

---

## Rotation Operations

### RotatePointAround

```csharp
public static float3 RotatePointAround(float3 point, float3 pivot, float angleRadians)
```

Rotates a point around a pivot point by a specified angle (2D rotation, Y axis unchanged).

**Parameters:**
- `point`: Point to rotate
- `pivot`: Pivot point (center of rotation)
- `angleRadians`: Rotation angle in radians

**Returns:** Rotated point

**Example:**
```csharp
// Rotate spawn points around a boss
var bossPos = new float3(100, 0, 100);
var spawnOffset = new float3(120, 0, 100); // 20 units to the right

for (int i = 0; i < 4; i++) {
    var angle = (math.PI * 2 / 4) * i; // 90 degrees each
    var rotatedPos = MathUtility.RotatePointAround(spawnOffset, bossPos, angle);
    SpawnerService.Spawn(minionPrefab, rotatedPos);
}
```

---

## Examples

### Example 1: Proximity Detection System

```csharp
// Check if enemies are close to player
var players = PlayerService.GetOnlinePlayers();

foreach (var player in players) {
    var enemies = EntityLookupService.GetEntitiesInRadius<Enemy>(
        player.CharacterEntity.Read<LocalTransform>().Position, 
        30f
    );
    
    int closeEnemies = 0;
    foreach (var enemy in enemies) {
        if (MathUtility.IsInRange2D(player.CharacterEntity, enemy, 10f)) {
            closeEnemies++;
        }
    }
    
    if (closeEnemies > 5) {
        MessageService.SendMessage(player, $"Warning: {closeEnemies} enemies nearby!");
    }
    
    enemies.Dispose();
}
```

### Example 2: Circle Spawn Pattern

```csharp
// Spawn enemies in a circle around a point
public void SpawnCirclePattern(float3 center, PrefabGUID prefab, int count, float radius) {
    for (int i = 0; i < count; i++) {
        var angle = (2 * math.PI / count) * i;
        var offset = new float3(
            math.cos(angle) * radius,
            0,
            math.sin(angle) * radius
        );
        
        var spawnPos = center + offset;
        SpawnerService.Spawn(prefab, spawnPos);
    }
}

// Usage
var bossPos = boss.Read<LocalTransform>().Position;
SpawnCirclePattern(bossPos, minionPrefab, 8, 15f);
```

### Example 3: Random Patrol Points

```csharp
// Generate random patrol points around a guard post
public List<float3> GeneratePatrolPoints(float3 guardPost, int count, float minRange, float maxRange) {
    var points = new List<float3>();
    
    for (int i = 0; i < count; i++) {
        var point = MathUtility.GetRandomPositionInRing(guardPost, minRange, maxRange);
        points.Add(point);
    }
    
    return points;
}

// Usage
var guardPos = new float3(100, 0, 100);
var patrolPoints = GeneratePatrolPoints(guardPos, 5, 10f, 25f);

int currentPoint = 0;
ActionScheduler.Repeating(() => {
    if (MathUtility.IsInRange(guard, patrolPoints[currentPoint], 2f)) {
        currentPoint = (currentPoint + 1) % patrolPoints.Count;
    }
}, 0.5f);
```

### Example 4: Line of Sight Check

```csharp
// Simple line of sight check using distance and angle
public bool HasLineOfSight(Entity viewer, Entity target, float maxDistance, float viewAngle) {
    if (!MathUtility.IsInRange(viewer, target, maxDistance)) {
        return false; // Too far
    }
    
    var viewerPos = viewer.Read<LocalTransform>().Position;
    var targetPos = target.Read<LocalTransform>().Position;
    var viewerRotation = viewer.Read<LocalTransform>().Rotation;
    
    // Calculate angle to target
    var angleToTarget = MathUtility.GetAngleBetween(viewerPos, targetPos);
    
    // Get viewer's forward direction angle
    var forward = math.mul(viewerRotation, new float3(0, 0, 1));
    var viewerAngle = math.atan2(forward.z, forward.x);
    
    // Calculate angle difference
    var angleDiff = math.abs(MathUtility.NormalizeAngle(angleToTarget - viewerAngle));
    
    return angleDiff <= viewAngle / 2;
}
```

### Example 5: Area Bounds Enforcement

```csharp
// Keep entities within a defined arena
public void EnforceArenaBounds() {
    var arenaMin = new float3(-50, 0, -50);
    var arenaMax = new float3(50, 20, 50);
    
    var entities = EntityLookupService.QueryAll<PlayerCharacter>();
    
    foreach (var entity in entities) {
        var pos = entity.Read<LocalTransform>().Position;
        
        if (!MathUtility.IsWithinBounds(pos, arenaMin, arenaMax)) {
            // Clamp to bounds and teleport back
            var clampedPos = MathUtility.ClampPosition(pos, arenaMin, arenaMax);
            TeleportService.Teleport(entity, clampedPos);
            
            var player = PlayerService.GetPlayerFromCharacter(entity);
            MessageService.SendMessage(player, "You cannot leave the arena!");
        }
    }
    
    entities.Dispose();
}

// Run periodically
ActionScheduler.Repeating(() => EnforceArenaBounds(), 1f);
```

### Example 6: Grid-Based Territory System

```csharp
// Divide map into grid cells and track ownership
private Dictionary<int2, Entity> territoryOwners = new();

public void ClaimTerritory(Entity entity) {
    var pos = entity.Read<LocalTransform>().Position;
    var gridPos = MathUtility.WorldToGrid(pos, 25f); // 25-unit cells
    
    territoryOwners[gridPos] = entity;
    
    // Visual feedback: spawn marker at grid center
    var markerPos = MathUtility.GridToWorld(gridPos, 25f, 0f);
    SpawnerService.Spawn(territoryMarkerPrefab, markerPos);
}

public Entity GetTerritoryOwner(float3 position) {
    var gridPos = MathUtility.WorldToGrid(position, 25f);
    return territoryOwners.TryGetValue(gridPos, out var owner) ? owner : Entity.Null;
}
```

### Example 7: Smooth Entity Movement

```csharp
// Move entity smoothly from A to B
public void MoveEntitySmooth(Entity entity, float3 targetPos, float duration) {
    var startPos = entity.Read<LocalTransform>().Position;
    float elapsed = 0f;
    
    ActionScheduler.Repeating(() => {
        elapsed += 0.05f;
        float t = elapsed / duration;
        
        if (t >= 1f) {
            // Finished
            var transform = entity.Read<LocalTransform>();
            transform.Position = targetPos;
            entity.Write(transform);
            return false; // Stop repeating
        }
        
        // Interpolate position
        var currentPos = MathUtility.Lerp(startPos, targetPos, t);
        var transform = entity.Read<LocalTransform>();
        transform.Position = currentPos;
        entity.Write(transform);
        
        return true; // Continue
    }, 0.05f);
}
```

### Example 8: Circular AoE Damage

```csharp
// Deal damage to all enemies in a circular area
public void CircularAoE(float3 center, float radius, float damage) {
    var enemies = EntityLookupService.GetEntitiesInRadius<Enemy>(center, radius);
    
    int hitCount = 0;
    foreach (var enemy in enemies) {
        var enemyPos = enemy.Read<LocalTransform>().Position;
        
        // Double-check with precise circle collision
        if (MathUtility.IsPointInCircle(enemyPos, center, radius)) {
            var health = enemy.Read<Health>();
            health.Value -= damage;
            enemy.Write(health);
            hitCount++;
        }
    }
    
    enemies.Dispose();
    Log.Info($"AoE hit {hitCount} enemies");
}
```

---

## Best Practices

### 1. Use 2D Variants for Ground-Based Checks

```csharp
// ✅ Good - Use 2D for ground distance
if (MathUtility.IsInRange2D(player, enemy, 20f)) {
    // Ground-based ability range
}

// ❌ Less appropriate - 3D includes vertical distance
if (MathUtility.IsInRange(player, enemy, 20f)) {
    // Will fail if enemy is above/below player
}
```

### 2. Check float.MaxValue Return Values

```csharp
// ✅ Good - Check for error value
var distance = MathUtility.Distance(entityA, entityB);
if (distance != float.MaxValue && distance < 50f) {
    // Safe to use
}

// ❌ Bad - Could be comparing to MaxValue
if (MathUtility.Distance(entityA, entityB) < 50f) {
    // Might work with invalid entities
}
```

### 3. Cache Positions in Loops

```csharp
// ✅ Good - Cache position
var playerPos = player.Read<LocalTransform>().Position;
foreach (var enemy in enemies) {
    var distance = MathUtility.Distance(playerPos, enemy.Read<LocalTransform>().Position);
}

// ❌ Bad - Repeated reads
foreach (var enemy in enemies) {
    var distance = MathUtility.Distance(
        player.Read<LocalTransform>().Position,  // Reading every iteration!
        enemy.Read<LocalTransform>().Position
    );
}
```

### 4. Use Appropriate Grid Size

```csharp
// ✅ Good - Grid size matches use case
var gridPos = MathUtility.WorldToGrid(pos, 10f); // 10-unit cells for territories

// ❌ Bad - Too small grid (many cells)
var gridPos = MathUtility.WorldToGrid(pos, 0.1f); // Huge number of cells!

// ❌ Bad - Too large grid (imprecise)
var gridPos = MathUtility.WorldToGrid(pos, 1000f); // Few cells, no detail
```

### 5. Clamp Values Before Using

```csharp
// ✅ Good - Lerp automatically clamps t
var pos = MathUtility.Lerp(start, end, t); // t is auto-clamped to [0,1]

// ⚠️ Without MathUtility - needs manual clamping
var pos = math.lerp(start, end, math.clamp(t, 0f, 1f));
```

### 6. Use Ring Spawning for Safe Zones

```csharp
// ✅ Good - Spawn away from center (safe zone)
var pos = MathUtility.GetRandomPositionInRing(center, 10f, 20f);

// ❌ Bad - Might spawn too close
var pos = MathUtility.GetRandomPositionInRadius(center, 20f);
```

### 7. Consider Performance in Loops

```csharp
// ✅ Good - Simple range check
if (MathUtility.IsInRange(a, b, range)) { }

// ❌ Less efficient - Calculates then compares
if (MathUtility.Distance(a, b) <= range) { }

// Note: IsInRange internally does Distance <= range,
// but it's clearer and allows for future optimization
```

### 8. Normalize Angles for Comparisons

```csharp
// ✅ Good - Normalize before comparing
var angle1 = MathUtility.NormalizeAngle(rawAngle1);
var angle2 = MathUtility.NormalizeAngle(rawAngle2);
var diff = math.abs(angle1 - angle2);

// ❌ Bad - Could have issues with wraparound
var diff = math.abs(rawAngle1 - rawAngle2);
```

---

## Performance Considerations

### Distance vs Range Checks

- **IsInRange**: Internally calculates distance and compares
- **Distance**: Returns the actual value
- Use `IsInRange` when you only need a boolean result
- Use `Distance` when you need the actual distance value

### 2D vs 3D Operations

- **2D variants** are slightly faster (skip Y axis calculations)
- Use 2D for ground-based gameplay (movement, ability ranges)
- Use 3D for aerial gameplay or precise 3D positioning

### Random Position Generation

- Uses `UnityEngine.Random` (thread-safe but not deterministic)
- Relatively fast for gameplay use
- For procedural generation, consider using Unity.Mathematics.Random for determinism

### Grid Conversions

- Very fast (simple arithmetic)
- Use larger grid sizes to reduce memory for grid-based storage
- World to Grid is O(1) complexity

### Geometric Operations

- `GetClosestPointOnLine`: O(1) but has vector math overhead
- Use sparingly in hot paths (60+ times per second)

---

## Common Pitfalls

### 1. Forgetting to Check float.MaxValue

```csharp
// ❌ Bug - MaxValue will always be > threshold
var dist = MathUtility.Distance(a, b); // Returns MaxValue if no transform
if (dist > 100f) {
    // Will execute even with invalid entities!
}

// ✅ Fix
if (dist != float.MaxValue && dist > 100f) { }
```

### 2. Using Wrong Distance Function

```csharp
// ❌ 3D distance includes height difference
if (MathUtility.Distance(player, target, 10f)) {
    // Fails if target is on a cliff above player
}

// ✅ Use 2D for ground-based checks
if (MathUtility.Distance2D(player, target, 10f)) { }
```

### 3. Incorrect Grid Size Usage

```csharp
// ❌ Inconsistent grid sizes
var gridPos = MathUtility.WorldToGrid(pos, 5f);
var worldPos = MathUtility.GridToWorld(gridPos, 10f); // Different size!

// ✅ Use same grid size
const float GRID_SIZE = 5f;
var gridPos = MathUtility.WorldToGrid(pos, GRID_SIZE);
var worldPos = MathUtility.GridToWorld(gridPos, GRID_SIZE);
```

### 4. Angle Units Confusion

```csharp
// ❌ MathUtility uses radians, not degrees
var angle = 45f; // Degrees?
var rotated = MathUtility.RotatePointAround(point, pivot, angle);

// ✅ Convert degrees to radians
var angleDegrees = 45f;
var angleRadians = angleDegrees * (math.PI / 180f);
var rotated = MathUtility.RotatePointAround(point, pivot, angleRadians);
```

### 5. Not Caching Entity Transforms

```csharp
// ❌ Reading component repeatedly
for (int i = 0; i < 100; i++) {
    var dist = MathUtility.Distance(
        entity.Read<LocalTransform>().Position,  // Read every iteration!
        targets[i]
    );
}

// ✅ Cache transform
var entityPos = entity.Read<LocalTransform>().Position;
for (int i = 0; i < 100; i++) {
    var dist = MathUtility.Distance(entityPos, targets[i]);
}
```

---

## Technical Details

### Coordinate Systems

- **World Space**: Global 3D coordinates (float3)
- **Grid Space**: Integer 2D coordinates (int2) for tile/cell systems
- **Y Axis**: Vertical (height) in 3D operations

### Angle Format

All angle methods use **radians**, not degrees:
- Full circle: 2π radians (≈ 6.28)
- Right angle: π/2 radians (≈ 1.57)
- Conversion: `degrees * (π / 180)` = radians

### Random Distribution

- `GetRandomPositionInRadius`: Uniform distribution within circle
- `GetRandomPositionInRing`: Uniform distribution within ring
- Uses polar coordinates for even distribution

---

## Related Utilities

- [Logger](Logger.md) - Logging and debugging
- [EntityLookupService](../Services/EntityLookupService.md) - Entity queries with spatial lookups
- [TeleportService](../Services/TeleportService.md) - Entity teleportation

---

## Requirements

- **Unity.Mathematics** - Core math library (float3, float2, int2, math functions)
- **Unity.Entities** - ECS support (Entity, LocalTransform component)
- **Unity.Transforms** - Transform components
- **UnityEngine.Random** - Random number generation
