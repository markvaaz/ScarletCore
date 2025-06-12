using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ScarletCore.Utils;

/// <summary>
/// Utility class providing mathematical operations including distance calculations, 
/// positioning, and geometric operations for entities and world positions.
/// </summary>
public static class MathUtility {

  #region Distance Calculations

  /// <summary>
  /// Calculates the 3D distance between two entities.
  /// </summary>
  /// <param name="A">First entity</param>
  /// <param name="B">Second entity</param>
  /// <returns>Distance between entities, or float.MaxValue if either entity lacks LocalTransform</returns>
  public static float Distance(Entity A, Entity B) {
    // Check if both entities have transform components
    if (!A.Has<LocalTransform>() || !B.Has<LocalTransform>()) {
      return float.MaxValue;
    }

    var transformA = A.Read<LocalTransform>();
    var transformB = B.Read<LocalTransform>();

    return math.distance(transformA.Position, transformB.Position);
  }

  /// <summary>
  /// Calculates the 2D distance between two entities (ignoring Y axis).
  /// Useful for ground-based range checks.
  /// </summary>
  /// <param name="A">First entity</param>
  /// <param name="B">Second entity</param>
  /// <returns>2D distance between entities, or float.MaxValue if either entity lacks LocalTransform</returns>
  public static float Distance2D(Entity A, Entity B) {
    if (!A.Has<LocalTransform>() || !B.Has<LocalTransform>()) {
      return float.MaxValue;
    }

    var transformA = A.Read<LocalTransform>();
    var transformB = B.Read<LocalTransform>();

    var posA = transformA.Position;
    var posB = transformB.Position;

    // Calculate distance using only X and Z coordinates
    return math.distance(new float2(posA.x, posA.z), new float2(posB.x, posB.z));
  }

  /// <summary>
  /// Calculates the 3D distance between an entity and a world position.
  /// </summary>
  /// <param name="entity">Source entity</param>
  /// <param name="position">Target position</param>
  /// <returns>Distance to position, or float.MaxValue if entity lacks LocalTransform</returns>
  public static float Distance(Entity entity, float3 position) {
    if (!entity.Has<LocalTransform>()) {
      return float.MaxValue;
    }

    var transform = entity.Read<LocalTransform>();
    return math.distance(transform.Position, position);
  }

  /// <summary>
  /// Calculates the 2D distance between an entity and a world position (ignoring Y axis).
  /// </summary>
  /// <param name="entity">Source entity</param>
  /// <param name="position">Target position</param>
  /// <returns>2D distance to position, or float.MaxValue if entity lacks LocalTransform</returns>
  public static float Distance2D(Entity entity, float3 position) {
    if (!entity.Has<LocalTransform>()) {
      return float.MaxValue;
    }

    var transform = entity.Read<LocalTransform>();
    var entityPos = transform.Position;

    return math.distance(new float2(entityPos.x, entityPos.z), new float2(position.x, position.z));
  }

  /// <summary>
  /// Calculates the 3D distance between two world positions.
  /// </summary>
  /// <param name="positionA">First position</param>
  /// <param name="positionB">Second position</param>
  /// <returns>Distance between positions</returns>
  public static float Distance(float3 positionA, float3 positionB) {
    return math.distance(positionA, positionB);
  }

  /// <summary>
  /// Calculates the 2D distance between two world positions (ignoring Y axis).
  /// </summary>
  /// <param name="positionA">First position</param>
  /// <param name="positionB">Second position</param>
  /// <returns>2D distance between positions</returns>
  public static float Distance2D(float3 positionA, float3 positionB) {
    return math.distance(new float2(positionA.x, positionA.z), new float2(positionB.x, positionB.z));
  }

  #endregion

  #region Range Checks

  /// <summary>
  /// Checks if two entities are within a specified range (3D).
  /// </summary>
  /// <param name="A">First entity</param>
  /// <param name="B">Second entity</param>
  /// <param name="range">Maximum allowed distance</param>
  /// <returns>True if entities are within range</returns>
  public static bool IsInRange(Entity A, Entity B, float range) {
    return Distance(A, B) <= range;
  }

  /// <summary>
  /// Checks if two entities are within a specified range (2D).
  /// Useful for ground-based proximity checks.
  /// </summary>
  /// <param name="A">First entity</param>
  /// <param name="B">Second entity</param>
  /// <param name="range">Maximum allowed distance</param>
  /// <returns>True if entities are within 2D range</returns>
  public static bool IsInRange2D(Entity A, Entity B, float range) {
    return Distance2D(A, B) <= range;
  }

  /// <summary>
  /// Checks if an entity is within a specified range of a world position (3D).
  /// </summary>
  /// <param name="entity">Source entity</param>
  /// <param name="position">Target position</param>
  /// <param name="range">Maximum allowed distance</param>
  /// <returns>True if entity is within range of position</returns>
  public static bool IsInRange(Entity entity, float3 position, float range) {
    return Distance(entity, position) <= range;
  }

  /// <summary>
  /// Checks if an entity is within a specified range of a world position (2D).
  /// </summary>
  /// <param name="entity">Source entity</param>
  /// <param name="position">Target position</param>
  /// <param name="range">Maximum allowed distance</param>
  /// <returns>True if entity is within 2D range of position</returns>
  public static bool IsInRange2D(Entity entity, float3 position, float range) {
    return Distance2D(entity, position) <= range;
  }

  /// <summary>
  /// Checks if two world positions are within a specified range (3D).
  /// </summary>
  /// <param name="positionA">First position</param>
  /// <param name="positionB">Second position</param>
  /// <param name="range">Maximum allowed distance</param>
  /// <returns>True if positions are within range</returns>
  public static bool IsInRange(float3 positionA, float3 positionB, float range) {
    return Distance(positionA, positionB) <= range;
  }

  /// <summary>
  /// Checks if two world positions are within a specified range (2D).
  /// </summary>
  /// <param name="positionA">First position</param>
  /// <param name="positionB">Second position</param>
  /// <param name="range">Maximum allowed distance</param>
  /// <returns>True if positions are within 2D range</returns>
  public static bool IsInRange2D(float3 positionA, float3 positionB, float range) {
    return Distance2D(positionA, positionB) <= range;
  }

  #endregion

  #region Random Position Generation

  /// <summary>
  /// Generates a random position within a circular radius around a center point.
  /// Y coordinate remains unchanged from center.
  /// </summary>
  /// <param name="center">Center position</param>
  /// <param name="radius">Maximum radius</param>
  /// <returns>Random position within the specified radius</returns>
  public static float3 GetRandomPositionInRadius(float3 center, float radius) {
    var randomAngle = UnityEngine.Random.Range(0f, 2f * math.PI);
    var randomDistance = UnityEngine.Random.Range(0f, radius);

    // Calculate position using polar coordinates
    var x = center.x + randomDistance * math.cos(randomAngle);
    var z = center.z + randomDistance * math.sin(randomAngle);

    return new float3(x, center.y, z);
  }

  /// <summary>
  /// Generates a random position within a ring (between two radii) around a center point.
  /// Y coordinate remains unchanged from center.
  /// </summary>
  /// <param name="center">Center position</param>
  /// <param name="minRadius">Minimum radius</param>
  /// <param name="maxRadius">Maximum radius</param>
  /// <returns>Random position within the specified ring</returns>
  public static float3 GetRandomPositionInRing(float3 center, float minRadius, float maxRadius) {
    var randomAngle = UnityEngine.Random.Range(0f, 2f * math.PI);
    var randomDistance = UnityEngine.Random.Range(minRadius, maxRadius);

    var x = center.x + randomDistance * math.cos(randomAngle);
    var z = center.z + randomDistance * math.sin(randomAngle);

    return new float3(x, center.y, z);
  }

  /// <summary>
  /// Generates a random position within a radius around an entity's position.
  /// </summary>
  /// <param name="entity">Center entity</param>
  /// <param name="radius">Maximum radius</param>
  /// <returns>Random position around entity, or float3.zero if entity lacks LocalTransform</returns>
  public static float3 GetRandomPositionAroundEntity(Entity entity, float radius) {
    if (!entity.Has<LocalTransform>()) {
      return float3.zero;
    }

    var transform = entity.Read<LocalTransform>();
    return GetRandomPositionInRadius(transform.Position, radius);
  }

  /// <summary>
  /// Generates a random position within a ring around an entity's position.
  /// </summary>
  /// <param name="entity">Center entity</param>
  /// <param name="minRadius">Minimum radius</param>
  /// <param name="maxRadius">Maximum radius</param>
  /// <returns>Random position around entity, or float3.zero if entity lacks LocalTransform</returns>
  public static float3 GetRandomPositionAroundEntity(Entity entity, float minRadius, float maxRadius) {
    if (!entity.Has<LocalTransform>()) {
      return float3.zero;
    }

    var transform = entity.Read<LocalTransform>();
    return GetRandomPositionInRing(transform.Position, minRadius, maxRadius);
  }

  #endregion

  #region Angle Calculations

  /// <summary>
  /// Normalizes an angle to be within the range [0, 2π].
  /// </summary>
  /// <param name="angle">Angle in radians</param>
  /// <returns>Normalized angle</returns>
  public static float NormalizeAngle(float angle) {
    while (angle < 0) angle += 2f * math.PI;
    while (angle >= 2f * math.PI) angle -= 2f * math.PI;
    return angle;
  }

  /// <summary>
  /// Calculates the angle between two positions.
  /// </summary>
  /// <param name="from">Source position</param>
  /// <param name="to">Target position</param>
  /// <returns>Angle in radians</returns>
  public static float GetAngleBetween(float3 from, float3 to) {
    var direction = to - from;
    return math.atan2(direction.z, direction.x);
  }

  /// <summary>
  /// Calculates the angle between two entities.
  /// </summary>
  /// <param name="from">Source entity</param>
  /// <param name="to">Target entity</param>
  /// <returns>Angle in radians, or 0 if either entity lacks LocalTransform</returns>
  public static float GetAngleBetween(Entity from, Entity to) {
    if (!from.Has<LocalTransform>() || !to.Has<LocalTransform>()) {
      return 0f;
    }

    var fromPos = from.Read<LocalTransform>().Position;
    var toPos = to.Read<LocalTransform>().Position;

    return GetAngleBetween(fromPos, toPos);
  }

  #endregion

  #region Interpolation and Clamping

  /// <summary>
  /// Linear interpolation between two positions with clamped t value.
  /// </summary>
  /// <param name="from">Start position</param>
  /// <param name="to">End position</param>
  /// <param name="t">Interpolation factor (0-1)</param>
  /// <returns>Interpolated position</returns>
  public static float3 Lerp(float3 from, float3 to, float t) {
    return math.lerp(from, to, math.clamp(t, 0f, 1f));
  }

  /// <summary>
  /// Clamps a position within specified bounds.
  /// </summary>
  /// <param name="position">Position to clamp</param>
  /// <param name="minBounds">Minimum bounds</param>
  /// <param name="maxBounds">Maximum bounds</param>
  /// <returns>Clamped position</returns>
  public static float3 ClampPosition(float3 position, float3 minBounds, float3 maxBounds) {
    return new float3(
      math.clamp(position.x, minBounds.x, maxBounds.x),
      math.clamp(position.y, minBounds.y, maxBounds.y),
      math.clamp(position.z, minBounds.z, maxBounds.z)
    );
  }

  /// <summary>
  /// Checks if a position is within specified bounds.
  /// </summary>
  /// <param name="position">Position to check</param>
  /// <param name="minBounds">Minimum bounds</param>
  /// <param name="maxBounds">Maximum bounds</param>
  /// <returns>True if position is within bounds</returns>
  public static bool IsWithinBounds(float3 position, float3 minBounds, float3 maxBounds) {
    return position.x >= minBounds.x && position.x <= maxBounds.x &&
           position.y >= minBounds.y && position.y <= maxBounds.y &&
           position.z >= minBounds.z && position.z <= maxBounds.z;
  }

  #endregion

  #region Direction Calculations

  /// <summary>
  /// Calculates the normalized direction vector between two positions.
  /// </summary>
  /// <param name="from">Source position</param>
  /// <param name="to">Target position</param>
  /// <returns>Normalized direction vector</returns>
  public static float3 GetDirection(float3 from, float3 to) {
    var direction = to - from;
    return math.normalize(direction);
  }

  /// <summary>
  /// Calculates the normalized direction vector between two entities.
  /// </summary>
  /// <param name="from">Source entity</param>
  /// <param name="to">Target entity</param>
  /// <returns>Normalized direction vector, or float3.zero if either entity lacks LocalTransform</returns>
  public static float3 GetDirection(Entity from, Entity to) {
    if (!from.Has<LocalTransform>() || !to.Has<LocalTransform>()) {
      return float3.zero;
    }

    var fromPos = from.Read<LocalTransform>().Position;
    var toPos = to.Read<LocalTransform>().Position;

    return GetDirection(fromPos, toPos);
  }

  #endregion

  #region Geometric Operations

  /// <summary>
  /// Finds the closest point on a line segment to a given point.
  /// </summary>
  /// <param name="point">Reference point</param>
  /// <param name="lineStart">Start of line segment</param>
  /// <param name="lineEnd">End of line segment</param>
  /// <returns>Closest point on the line segment</returns>
  public static float3 GetClosestPointOnLine(float3 point, float3 lineStart, float3 lineEnd) {
    var lineDirection = lineEnd - lineStart;
    var lineLength = math.length(lineDirection);

    // Handle degenerate case where line has no length
    if (lineLength == 0) return lineStart;

    var normalizedDirection = lineDirection / lineLength;
    var toPoint = point - lineStart;
    var projection = math.dot(toPoint, normalizedDirection);

    // Clamp projection to line segment
    projection = math.clamp(projection, 0f, lineLength);
    return lineStart + normalizedDirection * projection;
  }

  #endregion

  #region Grid Conversions

  /// <summary>
  /// Converts world position to grid coordinates.
  /// </summary>
  /// <param name="worldPosition">World position</param>
  /// <param name="gridSize">Size of each grid cell</param>
  /// <returns>Grid coordinates</returns>
  public static int2 WorldToGrid(float3 worldPosition, float gridSize = 1f) {
    return new int2(
      (int)math.floor(worldPosition.x / gridSize),
      (int)math.floor(worldPosition.z / gridSize)
    );
  }

  /// <summary>
  /// Converts grid coordinates to world position (center of grid cell).
  /// </summary>
  /// <param name="gridPosition">Grid coordinates</param>
  /// <param name="gridSize">Size of each grid cell</param>
  /// <param name="yHeight">Y coordinate for the world position</param>
  /// <returns>World position at center of grid cell</returns>
  public static float3 GridToWorld(int2 gridPosition, float gridSize = 1f, float yHeight = 0f) {
    return new float3(
      gridPosition.x * gridSize + gridSize * 0.5f,
      yHeight,
      gridPosition.y * gridSize + gridSize * 0.5f
    );
  }

  #endregion

  #region Shape Collision Detection

  /// <summary>
  /// Checks if a point is within a circular area (2D).
  /// </summary>
  /// <param name="point">Point to check</param>
  /// <param name="circleCenter">Center of the circle</param>
  /// <param name="radius">Radius of the circle</param>
  /// <returns>True if point is within the circle</returns>
  public static bool IsPointInCircle(float3 point, float3 circleCenter, float radius) {
    return Distance2D(point, circleCenter) <= radius;
  }

  /// <summary>
  /// Checks if a point is within a rectangular area (2D).
  /// </summary>
  /// <param name="point">Point to check</param>
  /// <param name="rectCenter">Center of the rectangle</param>
  /// <param name="rectSize">Size of the rectangle (width, height)</param>
  /// <returns>True if point is within the rectangle</returns>
  public static bool IsPointInRectangle(float3 point, float3 rectCenter, float2 rectSize) {
    var halfSize = rectSize * 0.5f;
    var distance = new float2(
      math.abs(point.x - rectCenter.x),
      math.abs(point.z - rectCenter.z)
    );

    return distance.x <= halfSize.x && distance.y <= halfSize.y;
  }

  #endregion

  #region Rotation Operations

  /// <summary>
  /// Rotates a point around a pivot point by a specified angle.
  /// </summary>
  /// <param name="point">Point to rotate</param>
  /// <param name="pivot">Pivot point</param>
  /// <param name="angleRadians">Rotation angle in radians</param>
  /// <returns>Rotated point</returns>
  public static float3 RotatePointAround(float3 point, float3 pivot, float angleRadians) {
    var cos = math.cos(angleRadians);
    var sin = math.sin(angleRadians);

    // Translate point to origin relative to pivot
    var translated = point - pivot;

    // Apply 2D rotation matrix (Y axis unchanged)
    var rotated = new float3(
      translated.x * cos - translated.z * sin,
      translated.y,
      translated.x * sin + translated.z * cos
    );

    // Translate back to original position
    return rotated + pivot;
  }

  #endregion
}