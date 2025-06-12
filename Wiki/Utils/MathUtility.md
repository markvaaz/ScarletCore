# MathUtility

`MathUtility` is a static utility class that provides a wide range of mathematical and geometric operations for use in ScarletCore mods and systems. It includes helpers for distance calculations, range checks, random position generation, angle and direction math, interpolation, clamping, grid conversions, collision detection, and more.

## Overview

- All methods are static and can be used anywhere in your mod or ScarletCore internals.
- Designed to simplify common math operations involving entities, world positions, and geometry.
- Handles both 3D and 2D (XZ-plane) calculations.

## Features

- 3D and 2D distance calculations between entities and positions
- Range checks for proximity logic
- Random position generation in circles and rings (around points or entities)
- Angle and direction math (including normalization and angle between points/entities)
- Linear interpolation and clamping
- Grid coordinate conversions
- Shape collision detection (circle, rectangle)
- Rotation operations (rotate point around pivot)
- Geometric helpers (closest point on line, etc.)

## Example Usage

```csharp
using ScarletCore.Utils;
using Unity.Entities;
using Unity.Mathematics;

// Distance between two entities
float dist = MathUtility.Distance(entityA, entityB);

// 2D range check
bool close = MathUtility.IsInRange2D(entityA, entityB, 5f);

// Random position in a radius around a point
float3 randomPos = MathUtility.GetRandomPositionInRadius(center, 10f);

// Angle between two positions
float angle = MathUtility.GetAngleBetween(posA, posB);

// Clamp a position within bounds
float3 clamped = MathUtility.ClampPosition(pos, minBounds, maxBounds);

// Check if a point is inside a circle
bool inside = MathUtility.IsPointInCircle(point, circleCenter, radius);
```

## API Highlights

### Distance & Range
- `Distance(Entity, Entity)` / `Distance2D(Entity, Entity)`
- `Distance(Entity, float3)` / `Distance2D(Entity, float3)`
- `Distance(float3, float3)` / `Distance2D(float3, float3)`
- `IsInRange(...)` / `IsInRange2D(...)`

### Random Position
- `GetRandomPositionInRadius(float3 center, float radius)`
- `GetRandomPositionInRing(float3 center, float minRadius, float maxRadius)`
- `GetRandomPositionAroundEntity(Entity entity, float radius)`
- `GetRandomPositionAroundEntity(Entity entity, float minRadius, float maxRadius)`

### Angles & Directions
- `NormalizeAngle(float angle)`
- `GetAngleBetween(float3 from, float3 to)` / `GetAngleBetween(Entity, Entity)`
- `GetDirection(float3 from, float3 to)` / `GetDirection(Entity, Entity)`

### Interpolation & Clamping
- `Lerp(float3 from, float3 to, float t)`
- `ClampPosition(float3 position, float3 minBounds, float3 maxBounds)`
- `IsWithinBounds(float3 position, float3 minBounds, float3 maxBounds)`

### Grid & Geometry
- `WorldToGrid(float3 worldPosition, float gridSize = 1f)`
- `GridToWorld(int2 gridPosition, float gridSize = 1f, float yHeight = 0f)`
- `GetClosestPointOnLine(float3 point, float3 lineStart, float3 lineEnd)`

### Collision & Rotation
- `IsPointInCircle(float3 point, float3 center, float radius)`
- `IsPointInRectangle(float3 point, float3 center, float2 size)`
- `RotatePointAround(float3 point, float3 pivot, float angleRadians)`

## Notes

- All entity-based methods require the entity to have a `LocalTransform` component.
- 2D methods operate on the XZ-plane (Y is ignored).
- Random position methods use UnityEngine.Random.

---

For more details, see the source code in `Utils/MathUtility.cs`.
