# PlayerService

PlayerService manages player data caching and retrieval with multiple indexing strategies for optimal performance. It handles player lifecycle including connection, disconnection, and name changes.

## Overview

```csharp
using ScarletCore.Services;

// Access player data through static methods
if (PlayerService.TryGetByName("PlayerName", out PlayerData player)) {
  // Use player data
}
```

## Features

- Multiple indexing strategies for fast lookups
- Automatic player lifecycle management
- Name change detection and handling
- Memory-efficient caching system
- Support for unnamed players

## Lookup Methods

### TryGetByName
Attempts to retrieve a player by their character name (case-insensitive).

```csharp
if (PlayerService.TryGetByName("PlayerName", out PlayerData player)) {
  Log.Info($"Found player: {player.Name}");
}
```

**Parameters:**
- `name` - Character name to search for
- `playerData` - Found player data (out parameter)

**Returns:** True if player was found

### TryGetById
Attempts to retrieve a player by their platform ID (Steam ID).

```csharp
if (PlayerService.TryGetById(76561198000000000, out PlayerData player)) {
  Log.Info($"Found player: {player.Name}");
}
```

**Parameters:**
- `platformId` - Platform ID to search for
- `playerData` - Found player data (out parameter)

**Returns:** True if player was found

### TryGetByNetworkId
Attempts to retrieve a player by their network ID (online players only).

```csharp
if (PlayerService.TryGetByNetworkId(networkId, out PlayerData player)) {
  Log.Info($"Found online player: {player.Name}");
}
```

**Parameters:**
- `networkId` - Network ID to search for
- `playerData` - Found player data (out parameter)

**Returns:** True if player was found

## Collection Methods

### GetAllConnected
Gets all currently connected players.

```csharp
var onlinePlayers = PlayerService.GetAllConnected();
Log.Info($"Online players: {onlinePlayers.Count}");
```

**Returns:** List of online PlayerData objects

### GetAdmins
Gets all players with admin privileges.

```csharp
var admins = PlayerService.GetAdmins();
foreach (var admin in admins) {
  Log.Info($"Admin: {admin.Name}");
}
```

**Returns:** List of admin PlayerData objects

## Public Collections

### AllPlayers
Complete list of all known players (online and offline).

```csharp
var totalPlayers = PlayerService.AllPlayers.Count;
Log.Info($"Total known players: {totalPlayers}");
```

### PlayerNames
Dictionary for fast lookup by character name (case-insensitive).

```csharp
var playerExists = PlayerService.PlayerNames.ContainsKey("playername");
```

### PlayerIds
Dictionary for fast lookup by platform ID.

```csharp
var playerExists = PlayerService.PlayerIds.ContainsKey(steamId);
```

### PlayerNetworkIds
Dictionary for fast lookup by network ID (active connections only).

```csharp
var playerExists = PlayerService.PlayerNetworkIds.ContainsKey(networkId);
```

## Usage Examples

### Basic Player Lookup
```csharp
// Find player by name
if (PlayerService.TryGetByName("PlayerName", out PlayerData player)) {
  Log.Info($"Player {player.Name} is {(player.IsOnline ? "online" : "offline")}");
  
  if (player.IsAdmin) {
    Log.Info("Player has admin privileges");
  }
  
  if (!string.IsNullOrEmpty(player.ClanName)) {
    Log.Info($"Player is in clan: {player.ClanName}");
  }
}
```

### Working with Collections
```csharp
// Get all online players
var onlinePlayers = PlayerService.GetAllConnected();
Log.Info($"Currently {onlinePlayers.Count} players online");

// Get all admins
var admins = PlayerService.GetAdmins();
foreach (var admin in admins) {
  // Send admin notification
  MessageService.SendMessage(admin, "Admin notification");
}

// Iterate through all known players
foreach (var player in PlayerService.AllPlayers) {
  if (player.IsOnline) {
    // Process online players
  }
}
```

### Custom Data Integration
```csharp
// Find player and work with custom data
if (PlayerService.TryGetByName("PlayerName", out PlayerData player)) {
  // Get or create custom mod data
  var customData = player.GetData<MyModData>() ?? new MyModData();
  
  // Update custom data
  customData.LastSeen = DateTime.Now;
  customData.LoginCount++;
  
  // Store updated data
  player.SetData(customData);
}
```

## Important Notes

- **PlayerData objects are cached and reused** - modifications persist across lookups
- **Name lookups are case-insensitive** - "PlayerName" and "playername" find the same player
- **Network ID lookups only work for online players** - offline players are not indexed by NetworkId
- **Unnamed players are handled automatically** - players without character names are tracked separately
