# BuffService

BuffService provides functionality to manage buffs on entities in V Rising, including application, removal, and duration control. It offers simplified methods for working with the game's buff system.

## Overview

```csharp
using ScarletCore.Services;
using ProjectM;
using Stunlock.Core;

// Apply a buff to an entity
BuffService.TryApplyBuff(entity, buffPrefabGUID, 30f);

// Check if entity has a specific buff
bool hasBuff = BuffService.HasBuff(entity, buffPrefabGUID);
```

## Features

- Buff application with duration control
- Buff removal and existence checking
- Duration modification and querying
- Permanent and temporary buff support
- Built-in error handling and logging

## Methods

### TryApplyBuff
Applies a buff to an entity with optional duration control.

```csharp
// Apply buff with specific duration (30 seconds)
bool success = BuffService.TryApplyBuff(entity, buffPrefabGUID, 30f);

// Apply permanent buff
bool success = BuffService.TryApplyBuff(entity, buffPrefabGUID, -1f);
```

**Parameters:**
- `entity` - Target entity to receive the buff
- `prefabGUID` - GUID of the buff prefab to apply
- `duration` - Duration in seconds (-1 = permanent)

**Returns:** True if buff was successfully applied, false otherwise


### TryGetBuff
Retrieves a specific buff entity from an entity.

```csharp
if (BuffService.TryGetBuff(entity, buffPrefabGUID, out Entity buffEntity)) {
  // Work with the buff entity directly
  Log.Info("Buff found and retrieved");
}
```

**Parameters:**
- `entity` - Entity to search for the buff on
- `prefabGUID` - GUID of the buff prefab to look for
- `buff` - Output parameter containing the buff entity if found

**Returns:** True if the buff was found and retrieved, false otherwise

### HasBuff
Checks if an entity currently has a specific buff applied.

```csharp
if (BuffService.HasBuff(entity, buffPrefabGUID)) {
  Log.Info("Entity has the specified buff");
}
```

**Parameters:**
- `entity` - Entity to check for the buff
- `prefabGUID` - GUID of the buff prefab to look for

**Returns:** True if the entity has the buff, false otherwise

### TryRemoveBuff
Removes a specific buff from an entity if it exists.

```csharp
bool removed = BuffService.TryRemoveBuff(entity, buffPrefabGUID);
if (removed) {
  Log.Info("Buff successfully removed");
}
```

**Parameters:**
- `entity` - Entity to remove the buff from
- `prefabGUID` - GUID of the buff prefab to remove

**Returns:** True if buff was found and removed, false if buff didn't exist

### GetBuffRemainingDuration
Gets the remaining duration of a buff on an entity.

```csharp
float remaining = BuffService.GetBuffRemainingDuration(entity, buffPrefabGUID);
if (remaining > 0) {
  Log.Info($"Buff expires in {remaining} seconds");
} else if (remaining == -1) {
  Log.Info("Buff is permanent or doesn't exist");
}
```

**Parameters:**
- `entity` - Entity to check
- `prefabGUID` - GUID of the buff to check duration for

**Returns:** Remaining duration in seconds, or -1 if buff doesn't exist or is permanent

### ModifyBuffDuration
Modifies the duration of an existing buff.

```csharp
// Extend buff duration to 60 seconds
bool modified = BuffService.ModifyBuffDuration(entity, buffPrefabGUID, 60f);

// Make buff permanent
bool modified = BuffService.ModifyBuffDuration(entity, buffPrefabGUID, -1f);
```

**Parameters:**
- `entity` - Entity with the buff
- `prefabGUID` - GUID of the buff to modify
- `newDuration` - New duration in seconds (-1 for permanent)

**Returns:** True if duration was successfully modified, false if buff doesn't exist

## Usage Examples

### Basic Buff Management
```csharp
using ProjectM;
using Stunlock.Core;
using ScarletCore.Services;

// Get a player entity
if (PlayerService.TryGetByName("PlayerName", out PlayerData player)) {
  var entity = player.Character;
  var buffGUID = new PrefabGUID(1234567890); // Example buff GUID
  
  // Apply a 30-second buff
  if (BuffService.TryApplyBuff(entity, buffGUID, 30f)) {
    Log.Info("Buff applied successfully");
  }
  
  // Check if buff exists
  if (BuffService.HasBuff(entity, buffGUID)) {
    Log.Info("Player has the buff");
  }
  
  // Remove the buff
  if (BuffService.TryRemoveBuff(entity, buffGUID)) {
    Log.Info("Buff removed");
  }
}
```

### Duration Management
```csharp
var entity = player.Character;
var buffGUID = new PrefabGUID(1234567890);

// Apply buff with initial duration
BuffService.TryApplyBuff(entity, buffGUID, 60f);

// Check remaining time
float remaining = BuffService.GetBuffRemainingDuration(entity, buffGUID);
Log.Info($"Buff has {remaining} seconds remaining");

// Extend the duration
BuffService.ModifyBuffDuration(entity, buffGUID, 120f);
Log.Info("Buff duration extended to 120 seconds");

// Make it permanent
BuffService.ModifyBuffDuration(entity, buffGUID, -1f);
Log.Info("Buff is now permanent");
```

## Important Notes

- **Duration handling** - Use -1 for permanent buffs, 0 for default game duration, positive values for specific seconds
- **Entity validation** - Methods check for entity and buff existence before performing operations
