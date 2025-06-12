# AdminService

AdminService manages administrator privileges and permissions in V Rising. It provides methods to add, remove, and check admin status for players using multiple identification methods.

## Overview

```csharp
using ScarletCore.Services;

// Add admin privileges
AdminService.AddAdmin("PlayerName");

// Check admin status
bool isAdmin = AdminService.IsAdmin("PlayerName");
```

## Features

- Multiple identification methods (name, platform ID, PlayerData)
- Persistent admin list management
- ECS event integration
- Automatic component management
- Built-in error handling and logging

## Methods

### AddAdmin
Adds admin privileges to a player.

```csharp
// By player name
AdminService.AddAdmin("PlayerName");

// By platform ID
AdminService.AddAdmin(76561198000000000);

// By PlayerData object
if (PlayerService.TryGetByName("PlayerName", out PlayerData player)) {
  AdminService.AddAdmin(player);
}
```

**Parameters:**
- `playerName` - Character name of the player
- `playerId` - Platform ID (Steam ID) of the player  
- `playerData` - PlayerData object containing player information

**Behavior:**
- Creates AdminAuthEvent for online players
- Updates local admin list and persists changes
- Logs warnings for non-existent players

### RemoveAdmin
Removes admin privileges from a player.

```csharp
// By player name
AdminService.RemoveAdmin("PlayerName");

// By platform ID
AdminService.RemoveAdmin(76561198000000000);

// By PlayerData object
if (PlayerService.TryGetByName("PlayerName", out PlayerData player)) {
  AdminService.RemoveAdmin(player);
}
```

**Parameters:**
- `playerName` - Character name of the player
- `playerId` - Platform ID (Steam ID) of the player
- `playerData` - PlayerData object containing player information

**Behavior:**
- Removes Admin from players
- Creates DeauthAdminEvent
- Updates local admin list and persists changes

### IsAdmin
Checks if a player has admin privileges.

```csharp
// By player name
bool isAdmin = AdminService.IsAdmin("PlayerName");

// By platform ID
bool isAdmin = AdminService.IsAdmin(76561198000000000);
```

**Parameters:**
- `playerName` - Character name to check
- `platformId` - Platform ID to check

**Returns:** True if the player is an admin, false otherwise

## Usage Examples

### Basic Admin Management
```csharp
// Promote a player to admin
AdminService.AddAdmin("PlayerName");
Log.Info("Player promoted to admin");

// Check if player is admin
if (AdminService.IsAdmin("PlayerName")) {
  Log.Info("Player has admin privileges");
} else {
  Log.Info("Player is not an admin");
}

// Remove admin privileges
AdminService.RemoveAdmin("PlayerName");
Log.Info("Admin privileges removed");
```

### Integration with PlayerService
```csharp
// Find player and manage admin status
if (PlayerService.TryGetByName("PlayerName", out PlayerData player)) {
  // Use PlayerData object
  AdminService.AddAdmin(player);
  
  // Or use platform ID for efficiency
  AdminService.AddAdmin(player.PlatformId);
  
  // Check using PlayerData's IsAdmin property
  if (player.IsAdmin) {
    Log.Info($"{player.Name} is an admin");
  }
}
```

### Bulk Admin Operations
```csharp
// Promote multiple players
var newAdmins = new[] { "Admin1", "Admin2", "Admin3" };
foreach (var adminName in newAdmins) {
  AdminService.AddAdmin(adminName);
  Log.Info($"Promoted {adminName} to admin");
}

// Get all current admins
var currentAdmins = PlayerService.GetAdmins();
Log.Info($"Current admins: {currentAdmins.Count}");

foreach (var admin in currentAdmins) {
  Log.Info($"Admin: {admin.Name} (ID: {admin.PlatformId})");
}
```

### Event-Driven Admin Management
```csharp
// Example: Auto-promote specific players on connect
EventManager.UserConnected += (sender, args) => {
  var player = args.Player;
  
  // Check if this is a known admin by Steam ID
  var knownAdminIds = new[] { 
    76561198000000001, 
    76561198000000002 
  };
  
  if (knownAdminIds.Contains(player.PlatformId)) {
    AdminService.AddAdmin(player);
    Log.Info($"Auto-promoted {player.Name} to admin");
  }
};

// Example: Remove admin on specific conditions
EventManager.UserDisconnected += (sender, args) => {
  var player = args.Player;
  
  // Example: Remove temp admin status on disconnect
  if (player.IsAdmin && IsTemporaryAdmin(player.PlatformId)) {
    AdminService.RemoveAdmin(player);
    Log.Info($"Removed temporary admin status from {player.Name}");
  }
};
```


## Important Notes

- **Persistent storage** - Admin status persists across server restarts
- **Multiple identification methods** - Use name, platform ID, or PlayerData object
- **Online/offline support** - Works for both connected and disconnected players
