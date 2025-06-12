# RevealMapService

RevealMapService provides functionality for managing map visibility in V Rising, allowing control over which areas of the map are revealed to players.

## Overview

```csharp
using ScarletCore.Services;
using Unity.Mathematics;

// Reveal entire map for a player
RevealMapService.RevealFullMap(player);

// Reveal a circular area
RevealMapService.RevealMapRadius(player, position, 100f);

// Hide entire map
RevealMapService.HideFullMap(player);
```

## Features

- Full map reveal and hide functionality
- Circular area revelation with custom radius
- Rectangular area revelation with custom dimensions
- Custom map patterns using byte arrays
- World coordinate to map coordinate conversion
- Precise pixel-level map control
- Map data synchronization with client

## Methods

### RevealFullMap
Reveals the entire map for a player.

```csharp
if (PlayerService.TryGetByName("PlayerName", out PlayerData player)) {
  RevealMapService.RevealFullMap(player);
}
```

**Parameters:**
- `playerData` - The player data for the target player

**Behavior:**
- Reveals all areas of the map
- Updates map data immediately
- Synchronizes changes with the client

### HideFullMap
Hides the entire map for a player.

```csharp
RevealMapService.HideFullMap(player);
```

**Parameters:**
- `player` - The player data for the target player

**Behavior:**
- Clears all revealed areas
- Resets map to unexplored state
- Synchronizes changes with the client

### RevealMapRadius
Reveals a circular area of the map for a player.

```csharp
var position = new float3(100f, 0f, 200f);
RevealMapService.RevealMapRadius(player, position, 150f);
```

**Parameters:**
- `player` - The player data for the target player
- `centerPos` - The center position in world coordinates
- `radius` - The radius in world units

**Behavior:**
- Converts world coordinates to map coordinates
- Reveals pixels within the specified circular area
- Preserves existing revealed areas

### RevealMapRectangle
Reveals a rectangular area of the map for a player.

```csharp
var centerPosition = new float3(0f, 0f, 0f);
RevealMapService.RevealMapRectangle(player, centerPosition, 200f, 300f);
```

**Parameters:**
- `player` - The player data for the target player
- `centerPos` - The center position in world coordinates
- `width` - The width of the rectangle in world units
- `height` - The height of the rectangle in world units

**Behavior:**
- Converts world coordinates to map coordinates
- Reveals pixels within the specified rectangular area
- Preserves existing revealed areas

### RevealMapFromByteArray
Reveals map areas based on a byte array pattern.

```csharp
byte[] customMapPattern = new byte[8192]; // Must be exactly 8192 bytes
// ... populate the byte array with desired pattern ...
RevealMapService.RevealMapFromByteArray(player, customMapPattern);
```

**Parameters:**
- `player` - The player data for the target player
- `mapData` - Byte array representing the entire map (must be 8192 bytes)

**Behavior:**
- Validates byte array size (must be exactly 8192 bytes)
- Each byte represents 8 pixels in the map canvas
- Each bit in a byte corresponds to one pixel
- Returns early if array is null or wrong size

## Technical Details

### Map Specifications
- **Map Canvas Size**: 256x256 pixels
- **Buffer Size**: 8192 bytes (256 rows × 32 bytes per row)
- **Pixels per Byte**: 8 pixels (1 bit per pixel)
- **World Bounds**: X(-2880 to 170), Z(635 to -2415)
- **Map Size**: 3050x3050 world units

### Coordinate System
The service handles conversion between world coordinates and map coordinates:
- World coordinates are the actual game positions
- Map coordinates are normalized to fit the 256x256 pixel canvas
- Position normalization accounts for map bounds and scaling

### Byte Array Format
When using `RevealMapFromByteArray`:
- Array must be exactly 8192 bytes
- Each byte represents 8 horizontal pixels
- Bit 0 (LSB) = leftmost pixel in byte
- Bit 7 (MSB) = rightmost pixel in byte
- Array index maps directly to buffer position

## Usage Examples

### Basic Map Control
```csharp
using ScarletCore.Services;
using Unity.Mathematics;

// Reveal entire map for a player
if (PlayerService.TryGetByName("PlayerName", out PlayerData player)) {
  RevealMapService.RevealFullMap(player);
  Log.Info("Full map revealed for player");
}

// Hide map completely
RevealMapService.HideFullMap(player);
Log.Info("Map hidden for player");
```

### Area-Based Revelation
```csharp
// Reveal circular area around player position
var playerPosition = player.Position;
RevealMapService.RevealMapRadius(player, playerPosition, 200f);

// Reveal rectangular area
var centerPos = new float3(0f, 0f, 0f);
RevealMapService.RevealMapRectangle(player, centerPos, 500f, 300f);
```

### Custom Map Patterns
```csharp
// Create custom map pattern
byte[] customPattern = new byte[8192];

// Example: Reveal only specific areas
for (int i = 0; i < customPattern.Length; i++) {
  // Reveal every 10th byte area
  if (i % 10 == 0) {
    customPattern[i] = 0xFF; // Reveal all 8 pixels in this byte
  }
}

RevealMapService.RevealMapFromByteArray(player, customPattern);
```

### Progressive Map Revelation
```csharp
// Reveal map progressively around player
var currentPos = player.Position;
float baseRadius = 50f;

// Reveal small area first
RevealMapService.RevealMapRadius(player, currentPos, baseRadius);

// Later expand the revealed area
RevealMapService.RevealMapRadius(player, currentPos, baseRadius * 2);
```

### Administrative Map Management
```csharp
// Reveal full map for all admins
var admins = PlayerService.GetAdmins();
foreach (var admin in admins) {
  RevealMapService.RevealFullMap(admin);
}

// Reset map for new players
var newPlayers = PlayerService.GetNewPlayers();
foreach (var newPlayer in newPlayers) {
  RevealMapService.HideFullMap(newPlayer);
}
```

## Important Notes

- **Buffer size validation** - Byte arrays must be exactly 8192 bytes for `RevealMapFromByteArray`
- **Coordinate conversion** - World coordinates are automatically converted to map coordinates
- **Additive revelation** - Revealing areas preserves existing revealed pixels
- **Client synchronization** - Map changes are automatically synchronized with the client
- **Performance considerations** - Large radius operations may affect performance
- **Bit manipulation** - Uses bitwise operations for precise pixel control
- **Map bounds** - Coordinates outside map bounds are automatically clamped
