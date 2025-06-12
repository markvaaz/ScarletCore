using System;
using ProjectM;
using ProjectM.Network;
using ScarletCore.Data;
using ScarletCore.Systems;
using Unity.Entities;
using Unity.Mathematics;

namespace ScarletCore.Services;

public static class RevealMapService {
  private const int MAP_CANVAS_SIZE = 256; // The client map canvas size
  private const int BYTES_PER_ROW = 32; // 256 pixels per row, 8 pixels per byte, so 256 / 8 = 32 bytes per row
  private const int BITS_PER_BYTE = 8;
  private const int MAP_BUFFER_SIZE = 8192;
  private const int MAP_CANVAS_MAX_INDEX = MAP_CANVAS_SIZE - 1;
  private static readonly BoundsMinMax MAP_BOUNDS = new(
    new(-2880, 635),
    new(170, -2415)
  );
  private static readonly int2 MAP_SIZE = new(3050, 3050);

  /// <summary>
  /// Reveals the entire map for a player by name
  /// </summary>
  /// <param name="playerName">The name of the player</param>
  public static void RevealFullMap(PlayerData playerData) {
    ProcessMapZones(playerData, (revealBuffer) => {
      for (int i = 0; i < revealBuffer.Length; ++i) {
        SetPixelMaskInBuffer(revealBuffer, i, 0xFF);
      }
    });
  }

  /// <summary>
  /// Hides the entire map for a player
  /// </summary>
  /// <param name="player">The player data</param>
  public static void HideFullMap(PlayerData player) {
    ProcessMapZones(player, (revealBuffer) => {
      revealBuffer.Clear();
      ClearMapBuffer(revealBuffer);
    });
  }

  /// <summary>
  /// Reveals a circular area of the map for a player
  /// </summary>
  /// <param name="player">The player data</param>
  /// <param name="centerPos">The center position in world coordinates</param>
  /// <param name="radius">The radius in world units</param>
  /// <returns>A message describing the operation result</returns>
  public static void RevealMapRadius(PlayerData player, float3 centerPos, float radius) {
    var normalizedCenter = NormalizePosition(centerPos);
    var mapRadius = ConvertWorldRadiusToMapRadius(radius);

    ProcessMapZones(player, (revealBuffer) => {
      RevealPixelsInRadius(revealBuffer, normalizedCenter, mapRadius);
    });
  }

  /// <summary>
  /// Reveals a rectangular area of the map for a player
  /// </summary>
  /// <param name="player">The player data</param>
  /// <param name="centerPos">The center position in world coordinates</param>
  /// <param name="width">The width of the rectangle in world units</param>
  /// <param name="height">The height of the rectangle in world units</param>
  /// <returns>A message describing the operation result</returns>
  public static void RevealMapRectangle(PlayerData player, float3 centerPos, float width, float height) {
    var normalizedCenter = NormalizePosition(centerPos);
    var mapWidth = ConvertWorldDistanceToMapDistance(width);
    var mapHeight = ConvertWorldDistanceToMapDistance(height);

    ProcessMapZones(player, (revealBuffer) => {
      RevealPixelsInRectangle(revealBuffer, normalizedCenter, mapWidth, mapHeight);
    });
  }

  /// <summary>
  /// Reveals map areas based on a byte array pattern
  /// </summary>
  /// <param name="player">The player data</param>
  /// <param name="mapData">Byte array representing the entire map</param>
  /// <returns>A message describing the operation result</returns>
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
  /// Processes all map zones for a player with the given action
  /// </summary>
  /// <param name="player">The player data</param>
  /// <param name="bufferAction">Action to perform on each reveal buffer</param>
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

  private static float3 NormalizePosition(float3 position) {
    float shiftedX = (position.x - MAP_BOUNDS.Min.x) / (MAP_BOUNDS.Max.x - MAP_BOUNDS.Min.x) * MAP_CANVAS_SIZE;
    float shiftedZ = (position.z - MAP_BOUNDS.Min.y) / (MAP_BOUNDS.Max.y - MAP_BOUNDS.Min.y) * MAP_CANVAS_SIZE;

    return new float3(shiftedX, position.y, shiftedZ);
  }

  private static float ConvertWorldRadiusToMapRadius(float worldRadius) {
    float worldScale = (MAP_SIZE.x + MAP_SIZE.y) / 2;
    return worldRadius * (MAP_CANVAS_SIZE / worldScale);
  }

  private static float ConvertWorldDistanceToMapDistance(float worldDistance) {
    float worldScale = (MAP_SIZE.x + MAP_SIZE.y) / 2;
    return worldDistance * (MAP_CANVAS_SIZE / worldScale);
  }

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

  private static bool IsPixelWithinRectangle(int pixelX, int pixelY, float3 normalizedCenter, float halfWidth, float halfHeight) {
    float deltaX = math.abs(pixelX - normalizedCenter.x);
    float deltaY = math.abs(pixelY - normalizedCenter.z);

    return deltaX <= halfWidth && deltaY <= halfHeight;
  }

  private static void ClearMapBuffer(DynamicBuffer<UserMapZonePackedRevealElement> revealBuffer) {
    for (int i = 0; i < MAP_BUFFER_SIZE; ++i) {
      revealBuffer.Add(new UserMapZonePackedRevealElement { PackedPixel = 0x00 });
    }
  }
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

  private static bool IsPixelWithinRadius(int pixelX, int pixelY, float3 normalizedCenter, float mapRadius) {
    float distance = math.distance(
      new float2(pixelX, pixelY),
      new float2(normalizedCenter.x, normalizedCenter.z)
    );

    return distance <= mapRadius;
  }

  private static void SetPixelMaskInBuffer(DynamicBuffer<UserMapZonePackedRevealElement> revealBuffer, int bufferIndex, byte pixelMask) {
    var element = revealBuffer[bufferIndex];
    element.PackedPixel |= pixelMask;
    revealBuffer[bufferIndex] = element;
  }

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