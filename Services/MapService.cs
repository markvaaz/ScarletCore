using System;
using System.Collections.Generic;
using System.Linq;
using ProjectM;
using ProjectM.Network;
using ProjectM.Terrain;
using ScarletCore.Interface;
using ScarletCore.Interface.Models;
using ScarletCore.Localization;
using ScarletCore.Systems;
using ScarletCore.Utils;
using Stunlock.Core;
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
    /// <summary>
    /// The type of world region (e.g., FarbaneWoods, DunleyFarmlands)
    /// </summary>
    public WorldRegionType RegionType { get; set; }

    /// <summary>
    /// The axis-aligned bounding box that encompasses the region polygon
    /// </summary>
    public Aabb Aabb { get; set; }

    /// <summary>
    /// Array of 2D vertices defining the region's polygon boundary
    /// </summary>
    public float2[] Vertices { get; set; }
  }

  /// <summary>
  /// Dictionary containing all loaded world regions mapped by their WorldRegionType.
  /// Populated during initialization by loading region polygons from game entities.
  /// </summary>
  public static readonly Dictionary<WorldRegionType, Region> Regions = [];

  /// <summary>
  /// Maps user-friendly region names (in Portuguese) to their corresponding WorldRegionType.
  /// Used for command parsing and region name lookups.
  /// </summary>
  public static readonly Dictionary<string, WorldRegionType> RegionNameMap = new() {
    { "farbane", WorldRegionType.FarbaneWoods },
    { "dunley", WorldRegionType.DunleyFarmlands },
    { "forest", WorldRegionType.CursedForest },
    { "mountains", WorldRegionType.HallowedMountains },
    { "silverlight", WorldRegionType.SilverlightHills },
    { "gloomrot", WorldRegionType.Gloomrot_North },
    { "gloomrotsouth", WorldRegionType.Gloomrot_South },
    { "gloomrotnorth", WorldRegionType.Gloomrot_North },
    { "mortium", WorldRegionType.RuinsOfMortium },
    { "oakveil", WorldRegionType.Strongblade }
  };

  /// <summary>
  /// Maps WorldRegionType to their localization key GUIDs for game-based translations.
  /// These keys correspond to the game's internal localization system.
  /// </summary>
  public static readonly Dictionary<WorldRegionType, string> RegionLocalizationKeys = new() {
    { WorldRegionType.FarbaneWoods, "21af333f-d58b-422e-adaf-cb9139788794" },
    { WorldRegionType.DunleyFarmlands, "d6067e94-9c9a-4e3c-b151-c4dd5b467c6a" },
    { WorldRegionType.CursedForest, "de2b1608-54cd-4de8-af51-a7350c938a4b" },
    { WorldRegionType.HallowedMountains, "e137642d-9d0b-4b1b-ab0b-19d1e4a20951" },
    { WorldRegionType.SilverlightHills, "53973904-acf8-4c58-89db-f08e047da1db" },
    { WorldRegionType.Gloomrot_South, "997b1df5-6d88-4f8f-abea-62799fe148a5" },
    { WorldRegionType.Gloomrot_North, "ee00edb1-b0a6-449c-a93a-dbb57c37befe" },
    { WorldRegionType.RuinsOfMortium, "dbdbd8fd-ae32-49bb-b0ea-eef178771d60" },
    { WorldRegionType.Strongblade, "ddd81c87-b5c9-46f0-af75-62c302879019" }
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
  /// <param name="language">The language for localization</param>
  /// <returns>The display name or the region type name if not found</returns>
  public static string GetRegionDisplayName(WorldRegionType regionType, Language language) {
    if (RegionLocalizationKeys.TryGetValue(regionType, out var locKey)) {
      return Localizer.GetText(language, locKey);
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

  #region Map Icons

  private static readonly PrefabGUID MAP_ICON_PREFAB = new(-892362184);

  // PlayerMapIcon.UserName is a FixedString64Bytes, which holds 58 bytes of content.
  private const int MAX_USERNAME_BYTES = 58;

  // "plugin:id:platformId" → the spawned icon entity.
  private static readonly Dictionary<string, Entity> MapIcons = [];

  /// <summary>
  /// Places a custom marker on a player's map and minimap.
  /// </summary>
  /// <remarks>
  /// The marker is a real native MapIcon entity, so positioning, edge clamping, map rotation,
  /// zoom, the full map, hover labels and draw order all come from the game itself.
  ///
  /// The icon adapts to the player's client:
  /// <list type="bullet">
  /// <item>Plain V Rising client — the label goes into <c>PlayerMapIcon.UserName</c> and the
  /// default sprite is used. Nothing is sent and nothing looks broken.</item>
  /// <item>ScarletInterface client — the marker's id goes into <c>PlayerMapIcon.UserName</c>
  /// instead, and image plus label travel in a packet keyed by that same id. The client matches
  /// icon to packet by id, so it repaints exactly the right one even when markers overlap.</item>
  /// </list>
  ///
  /// Reusing an id moves and updates that marker rather than creating a second one.
  /// </remarks>
  /// <param name="player">The player who sees the marker</param>
  /// <param name="plugin">Calling plugin — scopes the id so two mods can both use "boss"</param>
  /// <param name="id">Caller-chosen identifier; pass it again to update this marker</param>
  /// <param name="x">World X coordinate</param>
  /// <param name="z">World Z coordinate</param>
  /// <param name="icon">Image for ScarletInterface clients: an http(s)/file URL or a native sprite name; ignored by plain clients</param>
  /// <param name="label">Text shown when hovering the marker on the full map</param>
  /// <param name="color">Optional tint applied by ScarletInterface clients (e.g. "#ff0000")</param>
  /// <param name="scale">Icon size multiplier for ScarletInterface clients; 1 keeps the game's own size. Relative rather than absolute, because the minimap and the full map draw icons at different base sizes</param>
  /// <param name="clamp">Whether the game pins the marker to the minimap edge when off-screen</param>
  /// <param name="showOnMinimap">Whether the marker shows on the minimap; false leaves it on the full map only. The full map has no per-icon toggle natively, so it always shows the marker</param>
  /// <returns>False when the marker could not be created; the reason is logged</returns>
  /// <remarks>
  /// <paramref name="clamp"/> and <paramref name="showOnMinimap"/> map to the game's own
  /// <c>MapIconData.ClampOnMinimap</c>/<c>ShowOnMinimap</c> fields, but that component is not
  /// networked (no ghost serializer), so the values set on the server never reach the client.
  /// They travel in the packet instead and the ScarletInterface client writes them onto the
  /// local icon entity, where the game's own minimap job reads them each frame. Plain clients
  /// therefore always get the prefab defaults for these two.
  /// </remarks>
  public static bool SetIcon(PlayerData player, string plugin, string id, float x, float z,
      string icon = null, string label = null, string color = null, float scale = 1f, bool clamp = true, bool showOnMinimap = true) {
    if (player == null || string.IsNullOrEmpty(plugin) || string.IsNullOrEmpty(id)) return false;

    var token = $"{plugin}:{id}";
    var tokenBytes = System.Text.Encoding.UTF8.GetByteCount(token);

    // Scoped id must stay intact: it is the handle the caller uses to update or remove this
    // marker, and truncating it would silently collapse two distinct markers into one.
    if (tokenBytes > MAX_USERNAME_BYTES) {
      Log.Error($"[MapService] Marker id '{token}' is {tokenBytes} bytes; the game's label field holds {MAX_USERNAME_BYTES}. Use a shorter plugin name or id.");
      return false;
    }

    // The label always lives on the icon, for every client: the game's own tooltip then shows it
    // with no help from us. Identity travels as the icon's NetworkId in the packet instead, so
    // nothing the player can see has to be sacrificed to make markers identifiable.
    var userName = SanitizeLabel(label);

    var position = new float3(x, 0f, z);
    var key = $"{plugin}:{id}:{player.PlatformId}";

    if (MapIcons.TryGetValue(key, out var entity) && entity.Exists()) {
      entity.SetPosition(position);
      SetIconUserName(entity, userName);
    } else {
      entity = MapIcons[key] = SpawnMapIcon(player, position, userName, clamp, showOnMinimap);
    }

    if (PacketManager.HasInterface(player)) {
      // A freshly spawned entity has no NetworkId yet — reading it here returns 0:0, which
      // identifies nothing. The id is assigned later in the frame, so the packet waits for it.
      var pending = entity;
      ActionScheduler.NextFrame(() => {
        if (!pending.Exists()) return;
        var netId = pending.Read<NetworkId>();
        if (netId.Normal_Index == 0 && netId.Normal_Generation == 0) {
          Log.Warning($"[MapService] Marker '{plugin}:{id}' still has no NetworkId; ScarletInterface clients cannot identify it.");
          return;
        }
        var data = new Dictionary<string, string> {
          ["Id"] = id,
          ["Net"] = $"{netId.Normal_Index}:{netId.Normal_Generation}",
          ["Icon"] = icon ?? string.Empty,
        };
        if (!string.IsNullOrEmpty(color)) data["Color"] = color;
        if (scale > 0f && scale != 1f) data["Scale"] = scale.ToString(System.Globalization.CultureInfo.InvariantCulture);
        // Sent only when off the default so the packet stays small; the client treats an absent
        // key as the default, which also makes an update that flips the flag back revert cleanly.
        if (!clamp) data["Clamp"] = "0";
        if (!showOnMinimap) data["Mini"] = "0";
        SendIconPacket(player, "SetMapIcon", plugin, data);
      });
    }

    return true;
  }

  /// <summary>
  /// Places a marker for every connected player, each getting their own icon so the flavour
  /// matches whether that player has ScarletInterface installed.
  /// </summary>
  // ponytail: players connecting later don't receive existing markers — call this again on join
  // if that matters. Storing marker definitions and replaying them on connect is the upgrade path.
  public static void SetIconAll(string plugin, string id, float x, float z,
      string icon = null, string label = null, string color = null, float scale = 1f, bool clamp = true, bool showOnMinimap = true) {
    foreach (var player in PlayerService.GetAllConnected()) {
      SetIcon(player, plugin, id, x, z, icon, label, color, scale, clamp, showOnMinimap);
    }
  }

  /// <summary>Removes a single marker from a single player.</summary>
  public static void RemoveIcon(PlayerData player, string plugin, string id) {
    if (player == null || string.IsNullOrEmpty(plugin) || string.IsNullOrEmpty(id)) return;

    DestroyMapIcon($"{plugin}:{id}:{player.PlatformId}");

    if (PacketManager.HasInterface(player)) {
      SendIconPacket(player, "RemoveMapIcon", plugin, new Dictionary<string, string> { ["Id"] = id });
    }
  }

  /// <summary>Removes a single marker from every connected player.</summary>
  public static void RemoveIconAll(string plugin, string id) {
    foreach (var player in PlayerService.GetAllConnected()) {
      RemoveIcon(player, plugin, id);
    }
  }

  /// <summary>Removes every marker created by the given plugin, for all players.</summary>
  public static void ClearIcons(string plugin) {
    if (string.IsNullOrEmpty(plugin)) return;

    var prefix = $"{plugin}:";
    var stale = new List<string>();
    foreach (var key in MapIcons.Keys) {
      if (key.StartsWith(prefix)) stale.Add(key);
    }
    foreach (var key in stale) {
      DestroyMapIcon(key);
    }

    foreach (var player in PlayerService.GetAllConnected()) {
      if (!PacketManager.HasInterface(player)) continue;
      SendIconPacket(player, "ClearMapIcons", plugin, []);
    }
  }

  private static void SendIconPacket(PlayerData player, string type, string plugin, Dictionary<string, string> data) {
    PacketManager.SendPacket(player, new ScarletPacket {
      Type = type,
      Plugin = plugin,
      Window = string.Empty,
      Data = data
    });
  }

  private static void DestroyMapIcon(string key) {
    if (!MapIcons.TryGetValue(key, out var entity)) return;
    if (entity.Exists()) entity.Destroy();
    MapIcons.Remove(key);
  }

  private static Entity SpawnMapIcon(PlayerData player, float3 position, string userName, bool clamp, bool showOnMinimap) {
    var entity = SpawnerService.ImmediateSpawn(MAP_ICON_PREFAB, position, lifeTime: -1f);

    entity.HasWith((ref MapIconData iconData) => {
      iconData.AllySetting = MapIconShowSettings.Global;
      iconData.EnemySetting = MapIconShowSettings.None;
      iconData.ClampOnMinimap = clamp;
      iconData.RequiresReveal = false;
      iconData.ShowOnMinimap = showOnMinimap;
      iconData.ShowOutsideVision = true;
      iconData.CustomImplementation = true;
      iconData.IsSiegeWeapon = false;
      iconData.TargetUser = player.UserEntity;
    });

    entity.HasWith((ref MapIconTargetEntity iconTarget) => {
      iconTarget.TargetEntity = NetworkedEntity.ServerEntity(entity);
      iconTarget.TargetNetworkId = entity.Read<NetworkId>();
    });

    entity.SetPosition(position);
    entity.SetTeam(player.CharacterEntity);
    entity.ReadBuffer<SyncToUserBuffer>().Add(new SyncToUserBuffer {
      UserEntity = player.UserEntity
    });

    SetIconUserName(entity, userName);
    return entity;
  }

  private static void SetIconUserName(Entity entity, string userName) {
    entity.AddWith((ref PlayerMapIcon playerIcon) => {
      playerIcon.UserName = new(userName);
    });
  }

  /// <summary>
  /// Trims a label to what the game's fixed-size label field can hold and drops markup it
  /// cannot render. Ids go through the length check in <see cref="SetIcon"/> instead, since
  /// silently shortening an identifier would collapse two markers into one.
  /// </summary>
  private static string SanitizeLabel(string text) {
    text = text?.Replace("*", "").Replace("~", "") ?? string.Empty;
    while (text.Length > 0 && System.Text.Encoding.UTF8.GetByteCount(text) > MAX_USERNAME_BYTES) {
      text = text[..^1];
    }
    return text;
  }

  #endregion
}