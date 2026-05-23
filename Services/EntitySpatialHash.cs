using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;

namespace ScarletCore.Services;

/// <summary>
/// Instantiable spatial hash for fast lookup of nearby entities in 2D world space (X/Z plane).
/// Positions must be updated manually via <see cref="Update"/>, <see cref="UpdateAll"/>, or <see cref="Add"/>.
/// </summary>
public class EntitySpatialHash {
  /// <summary>World units per cell. 128 is optimal for typical V Rising world sizes.</summary>
  public readonly int CellSize;

  private readonly float _invCellSize;
  private readonly Func<Entity, float3> _getPosition;
  private readonly Dictionary<(int x, int z), HashSet<Entity>> _table = [];
  private readonly Dictionary<Entity, (int x, int z)> _entityCell = [];

  /// <param name="getPosition">Delegate that returns the world position of a given entity.</param>
  /// <param name="cellSize">World units per cell. Defaults to 128.</param>
  public EntitySpatialHash(Func<Entity, float3> getPosition, int cellSize = 128) {
    _getPosition = getPosition ?? throw new ArgumentNullException(nameof(getPosition));
    CellSize = cellSize > 0 ? cellSize : throw new ArgumentOutOfRangeException(nameof(cellSize), "Cell size must be positive.");
    _invCellSize = 1f / CellSize;
  }

  private (int x, int z) GetCell(float3 position) =>
    ((int)MathF.Floor(position.x * _invCellSize), (int)MathF.Floor(position.z * _invCellSize));

  /// <summary>
  /// Adds an entity to the spatial hash at its current position.
  /// If the entity is already tracked, its entry is updated instead.
  /// </summary>
  public void Add(Entity entity) {
    if (_entityCell.ContainsKey(entity)) {
      Update(entity);
      return;
    }

    var cell = GetCell(_getPosition(entity));
    _entityCell[entity] = cell;
    GetOrCreateCell(cell).Add(entity);
  }

  /// <summary>
  /// Removes an entity from the spatial hash.
  /// </summary>
  public void Remove(Entity entity) {
    if (!_entityCell.TryGetValue(entity, out var cell)) return;

    if (_table.TryGetValue(cell, out var set)) {
      set.Remove(entity);
      if (set.Count == 0) _table.Remove(cell);
    }

    _entityCell.Remove(entity);
  }

  /// <summary>
  /// Updates an entity's cell based on its current position.
  /// No-op if the entity hasn't crossed a cell boundary since last update.
  /// If the entity is not yet tracked, it is added.
  /// </summary>
  public void Update(Entity entity) {
    if (!_entityCell.TryGetValue(entity, out var oldCell)) {
      Add(entity);
      return;
    }

    var newCell = GetCell(_getPosition(entity));
    if (oldCell == newCell) return;

    if (_table.TryGetValue(oldCell, out var oldSet)) {
      oldSet.Remove(entity);
      if (oldSet.Count == 0) _table.Remove(oldCell);
    }

    _entityCell[entity] = newCell;
    GetOrCreateCell(newCell).Add(entity);
  }

  /// <summary>
  /// Updates all tracked entities' cells based on their current positions.
  /// Call this manually when entity positions may have changed.
  /// </summary>
  public void UpdateAll() {
    foreach (var entity in _entityCell.Keys)
      Update(entity);
  }

  /// <summary>
  /// Fills <paramref name="results"/> with all tracked entities within <paramref name="radius"/> world units
  /// of <paramref name="position"/>. The list is cleared before use. Does not allocate.
  /// </summary>
  public void QueryNearby(float3 position, float radius, List<Entity> results) {
    results.Clear();
    var radiusSq = radius * radius;

    int minX = (int)MathF.Floor((position.x - radius) * _invCellSize);
    int maxX = (int)MathF.Floor((position.x + radius) * _invCellSize);
    int minZ = (int)MathF.Floor((position.z - radius) * _invCellSize);
    int maxZ = (int)MathF.Floor((position.z + radius) * _invCellSize);

    for (int cx = minX; cx <= maxX; cx++) {
      for (int cz = minZ; cz <= maxZ; cz++) {
        if (!_table.TryGetValue((cx, cz), out var set)) continue;

        foreach (var candidate in set) {
          var pos = _getPosition(candidate);
          var dx = pos.x - position.x;
          var dz = pos.z - position.z;
          if (dx * dx + dz * dz <= radiusSq)
            results.Add(candidate);
        }
      }
    }
  }

  /// <summary>
  /// Fills <paramref name="results"/> with all tracked entities within <paramref name="radius"/> world units
  /// of <paramref name="origin"/>, excluding <paramref name="origin"/> itself. The list is cleared before use. Does not allocate.
  /// </summary>
  public void QueryNearby(Entity origin, float radius, List<Entity> results) {
    results.Clear();
    var position = _getPosition(origin);
    var radiusSq = radius * radius;

    int minX = (int)MathF.Floor((position.x - radius) * _invCellSize);
    int maxX = (int)MathF.Floor((position.x + radius) * _invCellSize);
    int minZ = (int)MathF.Floor((position.z - radius) * _invCellSize);
    int maxZ = (int)MathF.Floor((position.z + radius) * _invCellSize);

    for (int cx = minX; cx <= maxX; cx++) {
      for (int cz = minZ; cz <= maxZ; cz++) {
        if (!_table.TryGetValue((cx, cz), out var set)) continue;

        foreach (var candidate in set) {
          if (candidate == origin) continue;
          var pos = _getPosition(candidate);
          var dx = pos.x - position.x;
          var dz = pos.z - position.z;
          if (dx * dx + dz * dz <= radiusSq)
            results.Add(candidate);
        }
      }
    }
  }

  /// <summary>Convenience overload that allocates a new list. Prefer the overload that accepts a <see cref="List{T}"/> for hot paths.</summary>
  public List<Entity> QueryNearby(float3 position, float radius) {
    var results = new List<Entity>();
    QueryNearby(position, radius, results);
    return results;
  }

  /// <summary>Convenience overload that allocates a new list. Prefer the overload that accepts a <see cref="List{T}"/> for hot paths.</summary>
  public List<Entity> QueryNearby(Entity origin, float radius) {
    var results = new List<Entity>();
    QueryNearby(origin, radius, results);
    return results;
  }

  /// <summary>Returns whether an entity is currently tracked in the spatial hash.</summary>
  public bool Contains(Entity entity) => _entityCell.ContainsKey(entity);

  /// <summary>Removes all entities from the spatial hash.</summary>
  public void Clear() {
    _table.Clear();
    _entityCell.Clear();
  }

  /// <summary>Number of entities currently tracked.</summary>
  public int Count => _entityCell.Count;

  private HashSet<Entity> GetOrCreateCell((int x, int z) cell) {
    if (!_table.TryGetValue(cell, out var set)) {
      set = [];
      _table[cell] = set;
    }
    return set;
  }
}
