using System;
using System.Collections.Generic;
using System.Linq;
using ProjectM;
using ProjectM.Network;
using ProjectM.Terrain;
using ScarletCore.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

namespace ScarletCore.Services;

/// <summary>
/// Provides comprehensive map-related utilities including world region detection, polygon-based area checking,
/// and player map visibility control (reveal/hide). Manages region polygons loaded from the game world
/// and offers precise control over which areas of the map are visible to players.
/// </summary>
public static class MapService {
  /// <summary>
  /// Model representing a world region with its boundaries and vertices
  /// </summary>
  public class Region {
    public WorldRegionType RegionType { get; set; }
    public Aabb Aabb { get; set; }
    public float2[] Vertices { get; set; }
  }

  public static readonly Dictionary<WorldRegionType, Region> Regions = [];

  public static readonly Dictionary<string, WorldRegionType> RegionNameMap = new() {
    { "farbane", WorldRegionType.FarbaneWoods },
    { "dunley", WorldRegionType.DunleyFarmlands },
    { "floresta", WorldRegionType.CursedForest },
    { "montanha", WorldRegionType.HallowedMountains },
    { "argenta", WorldRegionType.SilverlightHills },
    { "gloomrot", WorldRegionType.Gloomrot_North },
    { "gloomrotsul", WorldRegionType.Gloomrot_South },
    { "gloomrotnorte", WorldRegionType.Gloomrot_North },
    { "mortium", WorldRegionType.RuinsOfMortium },
    { "oakveil", WorldRegionType.Strongblade }
  };

  public static readonly Dictionary<WorldRegionType, string> RegionDisplayNameMap = new() {
    { WorldRegionType.FarbaneWoods, "Floresta de Farbane" },
    { WorldRegionType.DunleyFarmlands, "Fazenda de Dunley" },
    { WorldRegionType.CursedForest, "Floresta Amaldiçoada" },
    { WorldRegionType.HallowedMountains, "Montanhas Sagradas" },
    { WorldRegionType.SilverlightHills, "Colinas da Luz Argenta" },
    { WorldRegionType.Gloomrot_South, "Gloomrot Sul" },
    { WorldRegionType.Gloomrot_North, "Gloomrot Norte" },
    { WorldRegionType.RuinsOfMortium, "Ruinas de Mortium" },
    { WorldRegionType.Strongblade, "Florestas de Oakveil" }
  };
  private const int MAP_CANVAS_SIZE = 256; // The client map canvas size
  private const int BYTES_PER_ROW = 32; // 256 pixels per row, 8 pixels per byte, so 256 / 8 = 32 bytes per row
  private const int BITS_PER_BYTE = 8;
  private const int MAP_BUFFER_SIZE = 8192;
  private const int MAP_CANVAS_MAX_INDEX = MAP_CANVAS_SIZE - 1;
  private static readonly BoundsMinMax MAP_BOUNDS = new(
    new(-2880, 640),
    new(160, -2400)
  );
  private static readonly int2 MAP_SIZE = new(3050, 3050);

  /// <summary>
  /// Initializes the MapService by loading all world region polygons from game entities.
  /// This should be called during server startup to enable region detection and map features.
  /// </summary>
  public static void Initialize() {
    LoadRegionPolygons();
  }

  /// <summary>
  /// Gets the WorldRegionType based on a region name
  /// </summary>
  /// <param name="name">The region name to lookup</param>
  /// <returns>The corresponding WorldRegionType or WorldRegionType.None if not found</returns>
  public static WorldRegionType GetRegionTypeByName(string name) {
    if (RegionNameMap.TryGetValue(name.ToLower(), out var regionType)) {
      return regionType;
    }
    return WorldRegionType.None;
  }

  /// <summary>
  /// Gets the display name for a region type
  /// </summary>
  /// <param name="regionType">The region type</param>
  /// <returns>The display name or the region type name if not found</returns>
  public static string GetRegionDisplayName(WorldRegionType regionType) {
    if (RegionDisplayNameMap.TryGetValue(regionType, out var displayName)) {
      return displayName;
    }
    return regionType.ToString();
  }

  /// <summary>
  /// Gets the region type at a specific position
  /// </summary>
  /// <param name="pos">The position to check</param>
  /// <returns>The WorldRegionType at the position or WorldRegionType.None if not in any region</returns>
  public static WorldRegionType GetRegion(float3 pos) {
    foreach (var worldRegionPolygon in Regions.Values) {
      if (worldRegionPolygon.Aabb.Contains(pos)) {
        if (IsPointInPolygon(worldRegionPolygon.Vertices, pos.xz)) {
          return worldRegionPolygon.RegionType;
        }
      }
    }
    return WorldRegionType.None;
  }

  /// <summary>
  /// Loads region polygons from the world
  /// </summary>
  private static void LoadRegionPolygons() {
    var entities = EntityLookupService.QueryAll(EntityQueryOptions.IncludeDisabled, typeof(WorldRegionPolygon));

    foreach (var worldRegionPolygonEntity in entities) {
      var wrp = worldRegionPolygonEntity.Read<WorldRegionPolygon>();
      var vertices = worldRegionPolygonEntity.ReadBuffer<WorldRegionPolygonVertex>();

      Regions.Add(wrp.WorldRegion, new Region {
        RegionType = wrp.WorldRegion,
        Aabb = wrp.PolygonBounds,
        Vertices = [.. vertices.ToNativeArray(allocator: Allocator.Temp).ToArray().Select(x => x.VertexPos)]
      });
    }
  }

  /// <summary>
  /// Checks if a point is inside a polygon using ray casting algorithm
  /// </summary>
  /// <param name="polygon">Array of polygon vertices</param>
  /// <param name="point">Point to check</param>
  /// <returns>True if the point is inside the polygon</returns>
  private static bool IsPointInPolygon(float2[] polygon, Vector2 point) {
    int intersections = 0;
    int vertexCount = polygon.Length;

    for (int i = 0, j = vertexCount - 1; i < vertexCount; j = i++) {
      if ((polygon[i].y > point.y) != (polygon[j].y > point.y) &&
        (point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x)) {
        intersections++;
      }
    }

    return intersections % 2 != 0;
  }

  /// <summary>
  /// Reveals the entire world map for a player, making all areas visible on their map interface.
  /// </summary>
  /// <param name="playerData">The player data of the target player</param>
  public static void RevealFullMap(PlayerData playerData) {
    ProcessMapZones(playerData, (revealBuffer) => {
      for (int i = 0; i < revealBuffer.Length; ++i) {
        SetPixelMaskInBuffer(revealBuffer, i, 0xFF);
      }
    });
  }

  /// <summary>
  /// Hides the entire world map for a player, resetting all revealed areas to hidden state.
  /// </summary>
  /// <param name="player">The player data of the target player</param>
  public static void HideFullMap(PlayerData player) {
    ProcessMapZones(player, (revealBuffer) => {
      revealBuffer.Clear();
      ClearMapBuffer(revealBuffer);
    });
  }

  /// <summary>
  /// Reveals a circular area of the map centered at a specific world position.
  /// Useful for revealing areas around specific locations or player positions.
  /// </summary>
  /// <param name="player">The player data of the target player</param>
  /// <param name="centerPos">The center position in world coordinates</param>
  /// <param name="radius">The radius of the circle in world units</param>
  public static void RevealMapRadius(PlayerData player, float3 centerPos, float radius) {
    var normalizedCenter = NormalizePosition(centerPos);
    var mapRadius = ConvertWorldRadiusToMapRadius(radius);

    ProcessMapZones(player, (revealBuffer) => {
      RevealPixelsInRadius(revealBuffer, normalizedCenter, mapRadius);
    });
  }

  /// <summary>
  /// Reveals a rectangular area of the map centered at a specific world position.
  /// Ideal for revealing structured areas or grid-based zones.
  /// </summary>
  /// <param name="player">The player data of the target player</param>
  /// <param name="centerPos">The center position in world coordinates</param>
  /// <param name="width">The width of the rectangle in world units</param>
  /// <param name="height">The height of the rectangle in world units</param>
  public static void RevealMapRectangle(PlayerData player, float3 centerPos, float width, float height) {
    var normalizedCenter = NormalizePosition(centerPos);
    var mapWidth = ConvertWorldDistanceToMapDistance(width);
    var mapHeight = ConvertWorldDistanceToMapDistance(height);

    ProcessMapZones(player, (revealBuffer) => {
      RevealPixelsInRectangle(revealBuffer, normalizedCenter, mapWidth, mapHeight);
    });
  }

  /// <summary>
  /// Reveals custom map areas based on a byte array pattern, allowing precise pixel-level control.
  /// Each byte represents 8 horizontal pixels, with each bit controlling one pixel's visibility.
  /// </summary>
  /// <param name="player">The player data of the target player</param>
  /// <param name="mapData">Byte array representing the entire map (must be exactly 8192 bytes: 256x256 pixels / 8 bits per byte)</param>
  public static void RevealMapFromByteArray(PlayerData player, byte[] mapData) {
    // Validate byte array size - must match the exact map buffer size
    if (mapData == null) {
      return;
    }

    if (mapData.Length != MAP_BUFFER_SIZE) {
      return;
    }

    ProcessMapZones(player, (revealBuffer) => {
      ApplyByteArrayToBuffer(revealBuffer, mapData);
    });
  }

  /// <summary>
  /// Processes all map zones for a player, applying the specified action to each zone's reveal buffer.
  /// Automatically synchronizes the updated map data to the player's client after processing.
  /// </summary>
  /// <param name="player">The player data of the target player</param>
  /// <param name="bufferAction">Action to perform on each map zone's reveal buffer</param>
  private static void ProcessMapZones(PlayerData player, Action<DynamicBuffer<UserMapZonePackedRevealElement>> bufferAction) {
    var userEntity = player.UserEntity;
    var mapZoneElements = userEntity.ReadBuffer<UserMapZoneElement>();

    foreach (var zoneElem in mapZoneElements) {
      var zoneEnt = zoneElem.UserZoneEntity.GetEntityOnServer();
      var revealBuffer = zoneEnt.ReadBuffer<UserMapZonePackedRevealElement>();

      bufferAction(revealBuffer);
    }

    SendMapDataToPlayer(userEntity, player.User);
  }

  /// <summary>
  /// Converts world coordinates to map canvas coordinates (normalized to 0-256 range).
  /// </summary>
  /// <param name="position">The world position to normalize</param>
  /// <returns>Normalized position in map canvas space</returns>
  private static float3 NormalizePosition(float3 position) {
    float shiftedX = (position.x - MAP_BOUNDS.Min.x) / (MAP_BOUNDS.Max.x - MAP_BOUNDS.Min.x) * MAP_CANVAS_SIZE;
    float shiftedZ = (position.z - MAP_BOUNDS.Min.y) / (MAP_BOUNDS.Max.y - MAP_BOUNDS.Min.y) * MAP_CANVAS_SIZE;

    return new float3(shiftedX, position.y, shiftedZ);
  }

  /// <summary>
  /// Converts a world space radius to map canvas radius.
  /// </summary>
  /// <param name="worldRadius">The radius in world units</param>
  /// <returns>The radius in map canvas units</returns>
  private static float ConvertWorldRadiusToMapRadius(float worldRadius) {
    float worldScale = (MAP_SIZE.x + MAP_SIZE.y) / 2;
    return worldRadius * (MAP_CANVAS_SIZE / worldScale);
  }

  /// <summary>
  /// Converts a world space distance to map canvas distance.
  /// </summary>
  /// <param name="worldDistance">The distance in world units</param>
  /// <returns>The distance in map canvas units</returns>
  private static float ConvertWorldDistanceToMapDistance(float worldDistance) {
    float worldScale = (MAP_SIZE.x + MAP_SIZE.y) / 2;
    return worldDistance * (MAP_CANVAS_SIZE / worldScale);
  }

  /// <summary>
  /// Reveals pixels within a rectangular area on the map buffer.
  /// </summary>
  /// <param name="revealBuffer">The map reveal buffer to modify</param>
  /// <param name="normalizedCenter">The center position in normalized map coordinates</param>
  /// <param name="mapWidth">The width in map canvas units</param>
  /// <param name="mapHeight">The height in map canvas units</param>
  /// <returns>The number of pixels revealed</returns>
  private static int RevealPixelsInRectangle(DynamicBuffer<UserMapZonePackedRevealElement> revealBuffer, float3 normalizedCenter, float mapWidth, float mapHeight) {
    int revealedPixels = 0;

    // Calculate bounding box for the rectangle
    float halfWidth = mapWidth / 2;
    float halfHeight = mapHeight / 2;

    int minY = math.max(0, (int)(normalizedCenter.z - halfHeight));
    int maxY = math.min(MAP_CANVAS_MAX_INDEX, (int)(normalizedCenter.z + halfHeight));
    int minX = math.max(0, (int)(normalizedCenter.x - halfWidth));
    int maxX = math.min(MAP_CANVAS_MAX_INDEX, (int)(normalizedCenter.x + halfWidth));

    for (int pixelY = minY; pixelY <= maxY; pixelY++) {
      int bufferRow = MAP_CANVAS_MAX_INDEX - pixelY;

      // Calculate which bytes might contain pixels within the rectangle for this row
      int minByteCol = minX / BITS_PER_BYTE;
      int maxByteCol = maxX / BITS_PER_BYTE;

      for (int byteCol = minByteCol; byteCol <= maxByteCol && byteCol < BYTES_PER_ROW; byteCol++) {
        int bufferIndex = bufferRow * BYTES_PER_ROW + byteCol;

        if (bufferIndex >= 0 && bufferIndex < revealBuffer.Length) {
          byte pixelMask = CalculatePixelMaskForByteRectangle(byteCol, pixelY, normalizedCenter, halfWidth, halfHeight, ref revealedPixels);

          if (pixelMask != 0) {
            SetPixelMaskInBuffer(revealBuffer, bufferIndex, pixelMask);
          }
        }
      }
    }

    return revealedPixels;
  }

  /// <summary>
  /// Calculates which pixels within a byte should be revealed for a rectangular area.
  /// </summary>
  /// <param name="byteCol">The byte column index</param>
  /// <param name="pixelY">The pixel Y coordinate</param>
  /// <param name="normalizedCenter">The center position in normalized coordinates</param>
  /// <param name="halfWidth">Half the width of the rectangle</param>
  /// <param name="halfHeight">Half the height of the rectangle</param>
  /// <param name="revealedPixels">Reference counter for revealed pixels</param>
  /// <returns>Byte mask with bits set for pixels to reveal</returns>
  private static byte CalculatePixelMaskForByteRectangle(int byteCol, int pixelY, float3 normalizedCenter, float halfWidth, float halfHeight, ref int revealedPixels) {
    byte pixelMask = 0;

    for (int pixelInByte = 0; pixelInByte < BITS_PER_BYTE; pixelInByte++) {
      int pixelX = byteCol * BITS_PER_BYTE + pixelInByte;

      if (IsPixelWithinRectangle(pixelX, pixelY, normalizedCenter, halfWidth, halfHeight)) {
        pixelMask |= (byte)(1 << pixelInByte);
        revealedPixels++;
      }
    }

    return pixelMask;
  }

  /// <summary>
  /// Checks if a pixel is within a rectangular area.
  /// </summary>
  /// <param name="pixelX">The pixel X coordinate</param>
  /// <param name="pixelY">The pixel Y coordinate</param>
  /// <param name="normalizedCenter">The center of the rectangle</param>
  /// <param name="halfWidth">Half the width of the rectangle</param>
  /// <param name="halfHeight">Half the height of the rectangle</param>
  /// <returns>True if the pixel is inside the rectangle</returns>
  private static bool IsPixelWithinRectangle(int pixelX, int pixelY, float3 normalizedCenter, float halfWidth, float halfHeight) {
    float deltaX = math.abs(pixelX - normalizedCenter.x);
    float deltaY = math.abs(pixelY - normalizedCenter.z);

    return deltaX <= halfWidth && deltaY <= halfHeight;
  }

  /// <summary>
  /// Clears the map buffer by filling it with empty (hidden) pixels.
  /// </summary>
  /// <param name="revealBuffer">The map reveal buffer to clear</param>
  private static void ClearMapBuffer(DynamicBuffer<UserMapZonePackedRevealElement> revealBuffer) {
    for (int i = 0; i < MAP_BUFFER_SIZE; ++i) {
      revealBuffer.Add(new UserMapZonePackedRevealElement { PackedPixel = 0x00 });
    }
  }
  /// <summary>
  /// Reveals pixels within a circular area on the map buffer.
  /// </summary>
  /// <param name="revealBuffer">The map reveal buffer to modify</param>
  /// <param name="normalizedCenter">The center position in normalized map coordinates</param>
  /// <param name="mapRadius">The radius in map canvas units</param>
  /// <returns>The number of pixels revealed</returns>
  private static int RevealPixelsInRadius(DynamicBuffer<UserMapZonePackedRevealElement> revealBuffer, float3 normalizedCenter, float mapRadius) {
    int revealedPixels = 0;

    // Calculate bounding box to limit processing area
    int minY = math.max(0, (int)(normalizedCenter.z - mapRadius));
    int maxY = math.min(MAP_CANVAS_MAX_INDEX, (int)(normalizedCenter.z + mapRadius));
    int minX = math.max(0, (int)(normalizedCenter.x - mapRadius));
    int maxX = math.min(MAP_CANVAS_MAX_INDEX, (int)(normalizedCenter.x + mapRadius));

    for (int pixelY = minY; pixelY <= maxY; pixelY++) {
      int bufferRow = MAP_CANVAS_MAX_INDEX - pixelY;

      // Calculate which bytes might contain pixels within the radius for this row
      int minByteCol = minX / BITS_PER_BYTE;
      int maxByteCol = maxX / BITS_PER_BYTE;

      for (int byteCol = minByteCol; byteCol <= maxByteCol && byteCol < BYTES_PER_ROW; byteCol++) {
        int bufferIndex = bufferRow * BYTES_PER_ROW + byteCol;

        if (bufferIndex >= 0 && bufferIndex < revealBuffer.Length) {
          byte pixelMask = CalculatePixelMaskForByte(byteCol, pixelY, normalizedCenter, mapRadius, ref revealedPixels);

          if (pixelMask != 0) {
            SetPixelMaskInBuffer(revealBuffer, bufferIndex, pixelMask);
          }
        }
      }
    }

    return revealedPixels;
  }
  /// <summary>
  /// Calculates which pixels within a byte should be revealed for a circular area.
  /// </summary>
  /// <param name="byteCol">The byte column index</param>
  /// <param name="pixelY">The pixel Y coordinate</param>
  /// <param name="normalizedCenter">The center position in normalized coordinates</param>
  /// <param name="mapRadius">The radius of the circle</param>
  /// <param name="revealedPixels">Reference counter for revealed pixels</param>
  /// <returns>Byte mask with bits set for pixels to reveal</returns>
  private static byte CalculatePixelMaskForByte(int byteCol, int pixelY, float3 normalizedCenter, float mapRadius, ref int revealedPixels) {
    byte pixelMask = 0;

    for (int pixelInByte = 0; pixelInByte < BITS_PER_BYTE; pixelInByte++) {
      int pixelX = byteCol * BITS_PER_BYTE + pixelInByte;

      if (IsPixelWithinRadius(pixelX, pixelY, normalizedCenter, mapRadius)) {
        pixelMask |= (byte)(1 << pixelInByte);
        revealedPixels++;
      }
    }

    return pixelMask;
  }

  /// <summary>
  /// Checks if a pixel is within a circular area.
  /// </summary>
  /// <param name="pixelX">The pixel X coordinate</param>
  /// <param name="pixelY">The pixel Y coordinate</param>
  /// <param name="normalizedCenter">The center of the circle</param>
  /// <param name="mapRadius">The radius of the circle</param>
  /// <returns>True if the pixel is inside the circle</returns>
  private static bool IsPixelWithinRadius(int pixelX, int pixelY, float3 normalizedCenter, float mapRadius) {
    float distance = math.distance(
      new float2(pixelX, pixelY),
      new float2(normalizedCenter.x, normalizedCenter.z)
    );

    return distance <= mapRadius;
  }

  /// <summary>
  /// Sets the pixel mask in the reveal buffer at the specified index using bitwise OR.
  /// This preserves existing revealed pixels while adding new ones.
  /// </summary>
  /// <param name="revealBuffer">The map reveal buffer to modify</param>
  /// <param name="bufferIndex">The buffer index to update</param>
  /// <param name="pixelMask">The pixel mask to apply</param>
  private static void SetPixelMaskInBuffer(DynamicBuffer<UserMapZonePackedRevealElement> revealBuffer, int bufferIndex, byte pixelMask) {
    var element = revealBuffer[bufferIndex];
    element.PackedPixel |= pixelMask;
    revealBuffer[bufferIndex] = element;
  }

  /// <summary>
  /// Synchronizes the updated map data to the player's client.
  /// </summary>
  /// <param name="userEntity">The user entity</param>
  /// <param name="usr">The user component</param>
  private static void SendMapDataToPlayer(Entity userEntity, User usr) {
    GameSystems.ServerBootstrapSystem.SendRevealedMapData(userEntity, usr);
  }
  /// <summary>
  /// Applies a byte array pattern to the reveal buffer
  /// </summary>
  /// <param name="revealBuffer">The map reveal buffer to modify</param>
  /// <param name="mapData">Byte array with the exact size of MAP_BUFFER_SIZE (8192 bytes).
  /// Each byte represents 8 pixels in the map canvas, where each bit corresponds to one pixel.
  /// Bit 0 (LSB) = leftmost pixel, Bit 7 (MSB) = rightmost pixel in the byte.</param>
  /// <remarks>
  /// This method directly applies the byte pattern to the map buffer without any coordinate transformation.
  /// The byte array must have exactly MAP_BUFFER_SIZE (8192) elements to represent the entire 256x256 map canvas.
  /// Map layout: 256 rows × 32 bytes per row = 8192 bytes total.
  /// </remarks>
  private static void ApplyByteArrayToBuffer(DynamicBuffer<UserMapZonePackedRevealElement> revealBuffer, byte[] mapData) {
    // Apply each byte from the pattern directly to the corresponding buffer position
    // This maintains a 1:1 mapping between array index and buffer index
    for (int i = 0; i < mapData.Length && i < revealBuffer.Length; i++) {
      // Set the pixel mask for this buffer position using bitwise OR
      // This preserves any existing revealed pixels while adding new ones
      SetPixelMaskInBuffer(revealBuffer, i, mapData[i]);
    }
  }
}