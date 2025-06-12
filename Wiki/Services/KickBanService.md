# KickBanService

KickBanService provides functionality for managing player kicks and bans in V Rising server, handling ban list operations and player removal from the server.

## Overview

```csharp
using ScarletCore.Services;

// Ban a player
KickBanService.Ban("PlayerName");

// Check if player is banned
bool isBanned = KickBanService.IsBanned("PlayerName");

// Kick a player
KickBanService.Kick("PlayerName");
```

## Features

- Player banning with persistent storage
- Player kicking without permanent restrictions
- Multiple identification methods (name, platform ID, PlayerData)
- Ban status checking and management
- Automatic ban list persistence
- Built-in error handling and validation

## Methods

### Ban (PlayerData)
Adds a player to the ban list using PlayerData.

```csharp
if (PlayerService.TryGetByName("PlayerName", out PlayerData player)) {
  KickBanService.Ban(player);
}
```

**Parameters:**
- `playerData` - The player data containing platform ID

### Ban (String)
Adds a player to the ban list by name.

```csharp
KickBanService.Ban("PlayerName");
```

**Parameters:**
- `playerName` - The name of the player to ban

**Behavior:**
- Logs warning if player is not found

### Ban (PlatformId)
Adds a player to the ban list and kicks them if they are currently online.

```csharp
KickBanService.Ban(7656119800000000);
```

**Parameters:**
- `platformId` - The platform ID of the player to ban

**Behavior:**
- Creates ban event entity to immediately kick online players
- Adds player to persistent ban list
- Automatically saves and refreshes ban list

### Unban (PlayerData)
Removes a player from the ban list using PlayerData.

```csharp
if (PlayerService.TryGetByName("PlayerName", out PlayerData player)) {
  KickBanService.Unban(player);
}
```

**Parameters:**
- `playerData` - The player data containing platform ID

### Unban (String)
Removes a player from the ban list by name.

```csharp
KickBanService.Unban("PlayerName");
```

**Parameters:**
- `playerName` - The name of the player to unban

**Behavior:**
- Logs warning if player is not found

### Unban (PlatformId)
Removes a player from the ban list, allowing them to join the server again.

```csharp
KickBanService.Unban(7656119800000000);
```

**Parameters:**
- `platformId` - The platform ID of the player to unban

**Behavior:**
- Removes player from ban list
- Automatically saves and refreshes ban list

### IsBanned (PlayerData)
Checks if a player is currently banned using PlayerData.

```csharp
if (PlayerService.TryGetByName("PlayerName", out PlayerData player)) {
  bool banned = KickBanService.IsBanned(player);
  if (banned) {
    Log.Info("Player is banned");
  }
}
```

**Parameters:**
- `playerData` - The player data containing platform ID

**Returns:** True if the player is banned, false otherwise

### IsBanned (String)
Checks if a player is currently banned by name.

```csharp
if (KickBanService.IsBanned("PlayerName")) {
  Log.Info("Player is banned");
}
```

**Parameters:**
- `playerName` - The name of the player to check

**Returns:** True if the player is banned, false otherwise

**Behavior:**
- Logs warning if player is not found
- Returns false if player doesn't exist

### IsBanned (PlatformId)
Checks if a player is currently banned.

```csharp
bool banned = KickBanService.IsBanned(7656119800000000);
```

**Parameters:**
- `platformId` - The platform ID of the player to check

**Returns:** True if the player is banned, false otherwise

### Kick (PlayerData)
Kicks a player from the server using PlayerData.

```csharp
if (PlayerService.TryGetByName("PlayerName", out PlayerData player)) {
  KickBanService.Kick(player);
}
```

**Parameters:**
- `playerData` - The player data containing platform ID

### Kick (String)
Kicks a player from the server by name.

```csharp
KickBanService.Kick("PlayerName");
```

**Parameters:**
- `playerName` - The name of the player to kick

**Behavior:**
- Logs warning if player is not found

### Kick (UInt64)
Kicks a player from the server without adding them to the ban list.

```csharp
KickBanService.Kick(7656119800000000);
```

**Parameters:**
- `platformId` - The platform ID of the player to kick

**Behavior:**
- Player can reconnect immediately after being kicked
- Creates kick event entity with appropriate network configuration

## Usage Examples

### Basic Ban Management
```csharp
using ScarletCore.Services;

// Ban a player by name
KickBanService.Ban("PlayerName");
Log.Info("Player banned");

// Check ban status
if (KickBanService.IsBanned("PlayerName")) {
  Log.Info("Player is currently banned");
}

// Unban a player
KickBanService.Unban("PlayerName");
Log.Info("Player unbanned");
```

### Using Platform IDs
```csharp
ulong platformId = 7656119800000000;

// Ban by platform ID
KickBanService.Ban(platformId);

// Check ban status by platform ID
if (KickBanService.IsBanned(platformId)) {
  Log.Info("Player is banned");
}

// Unban by platform ID
KickBanService.Unban(platformId);
```

### Using PlayerData Objects
```csharp
if (PlayerService.TryGetByName("PlayerName", out PlayerData player)) {
  // Ban using PlayerData
  KickBanService.Ban(player);
  
  // Check ban status
  if (KickBanService.IsBanned(player)) {
    Log.Info($"{player.Name} is banned");
  }
  
  // Unban the player
  KickBanService.Unban(player);
}
```

### Temporary Kicks vs Permanent Bans
```csharp
// Temporary kick - player can reconnect immediately
KickBanService.Kick("PlayerName");
Log.Info("Player kicked (can reconnect)");

// Permanent ban - player cannot reconnect until unbanned
KickBanService.Ban("PlayerName");
Log.Info("Player banned (cannot reconnect)");
```

### Moderation Workflow
```csharp
// Check if player is already banned before taking action
if (!KickBanService.IsBanned("PlayerName")) {
  // First offense - kick
  KickBanService.Kick("PlayerName");
  Log.Info("First offense - player kicked");
} else {
  Log.Info("Player is already banned");
}

// For serious offenses - immediate ban
KickBanService.Ban("PlayerName");
Log.Info("Player banned for serious offense");
```

## Important Notes

- **Persistent storage** - Ban list persists across server restarts
- **Multiple identification** - Use name, platform ID, or PlayerData object
- **Kick vs Ban** - Kicks are temporary, bans are permanent until removed
- **Online player handling** - Banning online players automatically kicks them
- **Ban list management** - Ban list is automatically saved and refreshed
