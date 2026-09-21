using System;
using System.Globalization;

namespace ScarletCore.Interface.Builders;

/// <summary>
/// Stable identity of one native V Rising map region.
/// </summary>
/// <remarks>
/// These values come from <c>MapZoneData.ChunkCoordinate.X</c>,
/// <c>MapZoneData.ChunkCoordinate.Y</c>, and <c>MapZoneData.ZoneIndex</c>.
/// They are stable wire data; client entity handles and render texture indices are not.
/// </remarks>
public readonly struct MapRegionKey : IEquatable<MapRegionKey> {
  /// <summary>Raw map chunk X coordinate.</summary>
  public int ChunkX { get; }

  /// <summary>Raw map chunk Y coordinate.</summary>
  public int ChunkY { get; }

  /// <summary>Raw zone index inside the chunk.</summary>
  public int ZoneIndex { get; }

  /// <summary>Creates a native map-region key.</summary>
  public MapRegionKey(int chunkX, int chunkY, int zoneIndex) {
    ChunkX = chunkX;
    ChunkY = chunkY;
    ZoneIndex = zoneIndex;
  }

  internal string Serialize() =>
    string.Concat(
      ChunkX.ToString(CultureInfo.InvariantCulture), ",",
      ChunkY.ToString(CultureInfo.InvariantCulture), ",",
      ZoneIndex.ToString(CultureInfo.InvariantCulture));

  /// <inheritdoc />
  public bool Equals(MapRegionKey other) =>
    ChunkX == other.ChunkX && ChunkY == other.ChunkY && ZoneIndex == other.ZoneIndex;

  /// <inheritdoc />
  public override bool Equals(object obj) => obj is MapRegionKey other && Equals(other);

  /// <inheritdoc />
  public override int GetHashCode() => HashCode.Combine(ChunkX, ChunkY, ZoneIndex);

  /// <inheritdoc />
  public override string ToString() => $"({ChunkX},{ChunkY},{ZoneIndex})";
}
