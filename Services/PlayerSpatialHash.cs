using System;
using System.Collections.Generic;
using Unity.Mathematics;
using ScarletCore.Events;
using ScarletCore.Systems;

namespace ScarletCore.Services;

/// <summary>
/// Static service for fast lookup of nearby online players in 2D world space (X/Z plane).
/// Offline players are never tracked. Positions are auto-updated every <see cref="UpdateFrameInterval"/> frames.
/// </summary>
public static class PlayerSpatialHash {
  /// <summary>World units per cell. 128 is optimal for ~60 players in a 4000x4000 world.</summary>
  public const int CellSize = 128;

  /// <summary>Precomputed inverse of <see cref="CellSize"/>. Replaces float division with faster multiplication.</summary>
  private const float InvCellSize = 1f / CellSize;

  /// <summary>How often (in frames) all online player positions are automatically refreshed.</summary>
  public const int UpdateFrameInterval = 10;

  private static readonly Dictionary<(int x, int z), HashSet<PlayerData>> _table = [];
  private static readonly Dictionary<PlayerData, (int x, int z)> _playerCell = [];
  private static readonly HashSet<PlayerData> _onlinePlayers = [];
  private static bool _initialized;

  internal static void Initialize() {
    if (_initialized) return;
    _initialized = true;
    ActionScheduler.RepeatingFrames(UpdateAll, UpdateFrameInterval);
    EventManager.On(PlayerEvents.CharacterCreated, Add);
  }

  private static (int x, int z) GetCell(float3 position) {
    return ((int)MathF.Floor(position.x * InvCellSize), (int)MathF.Floor(position.z * InvCellSize));
  }

  private static void UpdateAll() {
    foreach (var player in _onlinePlayers)
      Update(player);
  }

  /// <summary>
  /// Adds an online player to the spatial hash at their current position.
  /// If the player is already tracked, their entry is updated instead.
  /// </summary>
  public static void Add(PlayerData player) {
    if (!player.CharacterEntity.Exists()) return;

    if (_playerCell.ContainsKey(player)) {
      Update(player);
      return;
    }

    var cell = GetCell(player.Position);
    _onlinePlayers.Add(player);
    _playerCell[player] = cell;
    GetOrCreateCell(cell).Add(player);
  }

  /// <summary>
  /// Removes a player from the spatial hash (e.g. when they go offline).
  /// </summary>
  public static void Remove(PlayerData player) {
    if (!_playerCell.TryGetValue(player, out var cell)) return;

    if (_table.TryGetValue(cell, out var set)) {
      set.Remove(player);
      if (set.Count == 0) _table.Remove(cell);
    }

    _playerCell.Remove(player);
    _onlinePlayers.Remove(player);
  }

  /// <summary>
  /// Updates a player's cell in the spatial hash based on their current position.
  /// No-op if the player hasn't crossed a cell boundary since last update.
  /// </summary>
  public static void Update(PlayerData player) {
    if (!_playerCell.TryGetValue(player, out var oldCell)) {
      Add(player);
      return;
    }

    var newCell = GetCell(player.Position);
    if (oldCell == newCell) return;

    if (_table.TryGetValue(oldCell, out var oldSet)) {
      oldSet.Remove(player);
      if (oldSet.Count == 0) _table.Remove(oldCell);
    }

    _playerCell[player] = newCell;
    GetOrCreateCell(newCell).Add(player);
  }

  /// <summary>
  /// Fills <paramref name="results"/> with all online players within <paramref name="radius"/> world units
  /// of <paramref name="position"/>. The list is cleared before use. Does not allocate.
  /// </summary>
  public static void QueryNearby(float3 position, float radius, List<PlayerData> results) {
    results.Clear();
    var radiusSq = radius * radius;

    int minX = (int)MathF.Floor((position.x - radius) * InvCellSize);
    int maxX = (int)MathF.Floor((position.x + radius) * InvCellSize);
    int minZ = (int)MathF.Floor((position.z - radius) * InvCellSize);
    int maxZ = (int)MathF.Floor((position.z + radius) * InvCellSize);

    for (int cx = minX; cx <= maxX; cx++) {
      for (int cz = minZ; cz <= maxZ; cz++) {
        if (!_table.TryGetValue((cx, cz), out var set)) continue;

        foreach (var candidate in set) {
          var pos = candidate.Position;
          var dx = pos.x - position.x;
          var dz = pos.z - position.z;
          if (dx * dx + dz * dz <= radiusSq)
            results.Add(candidate);
        }
      }
    }
  }

  /// <summary>
  /// Fills <paramref name="results"/> with all online players within <paramref name="radius"/> world units
  /// of <paramref name="player"/>, excluding the player themselves. The list is cleared before use. Does not allocate.
  /// </summary>
  public static void QueryNearby(PlayerData player, float radius, List<PlayerData> results) {
    results.Clear();
    var origin = player.Position;
    var radiusSq = radius * radius;

    int minX = (int)MathF.Floor((origin.x - radius) * InvCellSize);
    int maxX = (int)MathF.Floor((origin.x + radius) * InvCellSize);
    int minZ = (int)MathF.Floor((origin.z - radius) * InvCellSize);
    int maxZ = (int)MathF.Floor((origin.z + radius) * InvCellSize);

    for (int cx = minX; cx <= maxX; cx++) {
      for (int cz = minZ; cz <= maxZ; cz++) {
        if (!_table.TryGetValue((cx, cz), out var set)) continue;

        foreach (var candidate in set) {
          if (candidate == player) continue;
          var pos = candidate.Position;
          var dx = pos.x - origin.x;
          var dz = pos.z - origin.z;
          if (dx * dx + dz * dz <= radiusSq)
            results.Add(candidate);
        }
      }
    }
  }

  /// <summary>Convenience overload that allocates a new list. Prefer the overload that accepts a <see cref="List{T}"/> for hot paths.</summary>
  public static List<PlayerData> QueryNearby(float3 position, float radius) {
    var results = new List<PlayerData>();
    QueryNearby(position, radius, results);
    return results;
  }

  /// <summary>Convenience overload that allocates a new list. Prefer the overload that accepts a <see cref="List{T}"/> for hot paths.</summary>
  public static List<PlayerData> QueryNearby(PlayerData player, float radius) {
    var results = new List<PlayerData>();
    QueryNearby(player, radius, results);
    return results;
  }

  /// <summary>Returns whether a player is currently tracked in the spatial hash.</summary>
  public static bool Contains(PlayerData player) => _playerCell.ContainsKey(player);

  /// <summary>Removes all players from the spatial hash.</summary>
  public static void Clear() {
    _table.Clear();
    _playerCell.Clear();
    _onlinePlayers.Clear();
  }

  /// <summary>Number of players currently tracked.</summary>
  public static int Count => _playerCell.Count;

  private static HashSet<PlayerData> GetOrCreateCell((int x, int z) cell) {
    if (!_table.TryGetValue(cell, out var set)) {
      set = [];
      _table[cell] = set;
    }
    return set;
  }
}
