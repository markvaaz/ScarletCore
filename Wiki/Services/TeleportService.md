# TeleportService

## Overview
The `TeleportService` provides functionality for teleporting entities and players within the game world. It includes methods for safe teleportation, distance calculations, position validation, and player-specific teleportation features.

## Features
- Teleport entities to specific positions or other entities.
- Teleport players to coordinates or other players.
- Calculate distances between entities.
- Find players within a radius or nearest to a position.
- Validate positions for safety.

## Methods

### Basic Teleportation

#### `TeleportToEntity`
Teleports an entity to another entity's position.

**Parameters:**
- `Entity entity`: The entity to teleport.
- `Entity target`: The target entity to teleport to.
- `float3 offset`: Optional offset from the target position.
- `bool validatePosition`: Whether to validate the target position for safety.

**Returns:**
- `bool`: `true` if teleportation was successful, `false` otherwise.

**Example:**
```csharp
Entity entity = ...;
Entity target = ...;
float3 offset = new float3(1, 0, 0);
bool success = TeleportService.TeleportToEntity(entity, target, offset);
```

#### `TeleportToPosition`
Teleports an entity to a specific position with optional validation.

**Parameters:**
- `Entity entity`: The entity to teleport.
- `float3 position`: The target position.
- `bool preserveRotation`: Whether to keep the current rotation.

**Returns:**
- `bool`: `true` if teleportation was successful, `false` otherwise.

**Example:**
```csharp
Entity entity = ...;
float3 position = new float3(10, 20, 30);
bool success = TeleportService.TeleportToPosition(entity, position);
```

### Player-Specific Methods

#### `TeleportPlayerToPlayer`
Teleports a player to another player's position.

**Parameters:**
- `string playerName`: Name of the player to teleport.
- `string targetPlayerName`: Name of the target player.
- `float3 offset`: Optional offset from the target position.

**Returns:**
- `bool`: `true` if teleportation was successful, `false` otherwise.

**Example:**
```csharp
string playerName = "Player1";
string targetPlayerName = "Player2";
float3 offset = new float3(0, 0, 5);
bool success = TeleportService.TeleportPlayerToPlayer(playerName, targetPlayerName, offset);
```

#### `TeleportPlayer`
Teleports a player to specific coordinates with safety checks.

**Parameters:**
- `string playerName`: Name of the player to teleport.
- `float x`: X coordinate.
- `float y`: Y coordinate.
- `float z`: Z coordinate.
- `bool validatePosition`: Whether to validate the position.

**Returns:**
- `bool`: `true` if teleportation was successful, `false` otherwise.

**Example:**
```csharp
string playerName = "Player1";
float x = 10;
float y = 20;
float z = 30;
bool success = TeleportService.TeleportPlayer(playerName, x, y, z);
```

### Utility Methods

#### `GetDistance`
Calculates the distance between two entities.

**Parameters:**
- `Entity entity1`: The first entity.
- `Entity entity2`: The second entity.

**Returns:**
- `float`: Distance between the entities, or `-1` if either entity is invalid.

**Example:**
```csharp
Entity entity1 = ...;
Entity entity2 = ...;
float distance = TeleportService.GetDistance(entity1, entity2);
```

#### `FindNearestPlayer`
Finds the nearest player to a given position.

**Parameters:**
- `float3 position`: The reference position.
- `float maxDistance`: Maximum distance to consider.

**Returns:**
- `PlayerData`: The nearest player, or `null` if none found.

**Example:**
```csharp
float3 position = new float3(10, 20, 30);
PlayerData nearestPlayer = TeleportService.FindNearestPlayer(position);
```

#### `GetPlayersInRadius`
Gets all players within a specified radius of a position.

**Parameters:**
- `float3 center`: The center position.
- `float radius`: The search radius.

**Returns:**
- `List<PlayerData>`: List of players within the radius.

**Example:**
```csharp
float3 center = new float3(10, 20, 30);
float radius = 50;
List<PlayerData> players = TeleportService.GetPlayersInRadius(center, radius);
```

## Notes
- Ensure entities are valid before attempting teleportation.
- Position validation is crucial for avoiding unsafe teleportation scenarios.
- Player-specific methods rely on `PlayerService` for player data retrieval.
