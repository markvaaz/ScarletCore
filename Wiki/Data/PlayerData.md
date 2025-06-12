# PlayerData

PlayerData represents comprehensive player information and is managed internally by the PlayerService. This class should not be instantiated directly - access player data through PlayerService methods.

## Overview

```csharp
// Get PlayerData through PlayerService
if (PlayerService.TryGetByName("PlayerName", out PlayerData player)) {
    // Access all player information
    var name = player.Name;
    var isOnline = player.IsOnline;
    var platformId = player.PlatformId;
}
```

## Core Properties

### Identity
- `Name` - Player's character name (cached)
- `PlatformId` - Platform-specific ID (Steam ID, etc.)
- `NetworkId` - Network identifier for connected players

### Status
- `IsOnline` - Whether player is currently connected
- `IsAdmin` - Whether player has admin privileges
- `ConnectedSince` - Connection timestamp

### Game Data
- `CharacterEntity` - Player's character entity in the game world
- `ClanName` - Name of player's clan (null if no clan)
- `UserEntity` - Underlying Unity ECS entity
- `User` - User component with core information

## Custom Data System

PlayerData provides a custom data storage system where each mod can store one custom data object per player. This object can be of any type - from simple values to complex classes.

### SetData
Stores one custom data object for your mod.

```csharp
// Each mod can store ONE object (any type, but only one at a time):
player.SetData<int>(playerLevel);                    // Simple value
// OR
player.SetData<List<string>>(playerAchievements);    // Collection  
// OR
player.SetData<CustomPlayerData>(playerModData);     // Complex class
// OR any other type you need
```

**Parameters:**
- `value` - Data object to store (any type)

**Returns:** The stored value

### GetData
Retrieves the custom data object for your mod.

```csharp
// Retrieve based on what you stored:
var playerLevel = player.GetData<int>();             // If you stored an int
var achievements = player.GetData<List<string>>();   // If you stored a List<string>
var modData = player.GetData<CustomPlayerData>();    // If you stored a custom class
```

**Returns:** Stored data object or default value if not found

## Data Isolation

Each mod can store exactly one data object, isolated by assembly name:

```csharp
// Mod A stores ONE object:
player.SetData<int>(100);

// Mod B stores ONE object independently:
player.SetData<MyModData>(customData);

// Each subsequent SetData call overwrites the previous data:
player.SetData<int>(100);        // Stores an int
player.SetData<string>("test");   // OVERWRITES the int, now stores a string

// Each mod only sees its own data
var modAValue = player.GetData<int>();           // Returns what Mod A stored
var modBValue = player.GetData<MyModData>();     // Returns what Mod B stored
```

## Usage Examples

### Basic Player Information
```csharp
if (PlayerService.TryGetByName("PlayerName", out PlayerData player)) {
    Log.Info($"Player: {player.Name}");
    Log.Info($"Platform ID: {player.PlatformId}");
    Log.Info($"Online: {player.IsOnline}");
    Log.Info($"Admin: {player.IsAdmin}");
    Log.Info($"Clan: {player.ClanName ?? "No clan"}");
}
```

### Custom Data Storage
```csharp
// Example: Complex player data class
public class CustomPlayerData {
    public int MaxTeleports { get; set; } = 10;
    public bool BypassCost { get; set; } = false;
    public bool BypassCooldown { get; set; } = false;
    public HashSet<TeleportData> Teleports { get; set; } = [];
    public DateTime LastTeleportTime { get; set; } = DateTime.Now.AddHours(-1);

    public void AddTeleport(TeleportData teleport) {
        if (teleport == null) return;
        
        var existing = Teleports.FirstOrDefault(t => 
            t.Name.Equals(teleport.Name, StringComparison.OrdinalIgnoreCase));
        
        if (existing != null) {
            Teleports.Remove(existing);
        }
        
        Teleports.Add(teleport);
    }

    public bool HasTeleport(string name) {
        return Teleports.Any(t => 
            t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}

// Usage
if (PlayerService.TryGetByName("PlayerName", out PlayerData player)) {
    // Store complex custom data
    player.SetData(new CustomPlayerData());
    
    // Retrieve and use
    var customData = player.GetData<CustomPlayerData>();
    if (customData != null) {
        customData.AddTeleport(newTeleport);
        customData.MaxTeleports = 20;
        
        // Data is automatically updated in the player object
        Log.Info($"Player has {customData.Teleports.Count} teleports");
    }
}
```

### Simple Value Storage
```csharp
// Store collections or simple values
if (PlayerService.TryGetByName("PlayerName", out PlayerData player)) {
    // Store a list of achievements
    var achievements = new List<string> { "First Kill", "Explorer", "Builder" };
    player.SetData(achievements);
    
    // Retrieve later
    var playerAchievements = player.GetData<List<string>>();
    if (playerAchievements != null) {
        playerAchievements.Add("New Achievement");
        Log.Info($"Player has {playerAchievements.Count} achievements");
    }
}
```

## Important Notes

- **Never instantiate PlayerData directly** - always use PlayerService
- **One data object per mod** - each mod can store exactly one custom data object
- **Any type supported** - store simple values, collections, or complex classes
- Custom data is isolated per mod automatically  
- Data persists only during server runtime (not saved to disk)
- Use Database class for persistent storage if needed
- PlayerData objects are cached and reused by PlayerService
- Modifications to retrieved objects update the stored data automatically
