# AbilityService

AbilityService handles ability-related operations for V Rising mods. It provides methods to cast abilities and modify NPC ability slots for both players and NPCs.

## Overview

```csharp
using ScarletCore.Services;
using Stunlock.Core;

// Cast an ability for an entity
AbilityService.CastAbility(entity, abilityGroupGuid);
```

## Features

- Cast abilities for players and NPCs
- Modify NPC ability slots
- *Others in development*

## Methods

### CastAbility
Casts an ability for the specified entity using the given ability group.

```csharp
AbilityService.CastAbility(playerEntity, abilityGroupGuid);
AbilityService.CastAbility(npcEntity, abilityGroupGuid);
```

**Parameters:**
- `entity` - The entity that will cast the ability (player or NPC)
- `abilityGroup` - The GUID of the ability group to be cast

**Behavior:**
- Automatically detects if entity is a player or NPC
- Handles user context appropriately for each entity type
- Uses debug events system for ability execution

### ReplaceNpcAbilityOnSlot
Replaces an ability in a specific slot for an NPC entity.

```csharp
AbilityService.ReplaceNpcAbilityOnSlot(npcEntity, newAbilityGuid);
AbilityService.ReplaceNpcAbilityOnSlot(npcEntity, newAbilityGuid, 2);
```

**Parameters:**
- `npc` - The NPC entity whose ability will be replaced
- `newAbilityGuid` - The GUID of the new ability to assign
- `abilitySlotIndex` - The slot index where the ability will be placed (default: 0)

**Requirements:**
- NPC must have AbilityGroupSlotBuffer component
- Slot index must be valid (within buffer bounds)

## Usage Examples

### Basic Ability Casting
```csharp
using ScarletCore.Services;
using Stunlock.Core;

// Get player entity
if (PlayerService.TryGetByName("PlayerName", out PlayerData player)) {
  var playerEntity = player.CharacterEntity;
  
  // Cast a specific ability
  var abilityGuid = new PrefabGUID(-1234567890); // Replace with actual ability GUID
  AbilityService.CastAbility(playerEntity, abilityGuid);
}
```

### NPC Ability Modification
```csharp
// Find an NPC and modify its abilities
var npcEntity = someNpcEntity; // Get NPC entity through your preferred method

var newAbilityGuid = new PrefabGUID(-987654321);

// Replace ability in a specific slot
AbilityService.ReplaceNpcAbilityOnSlot(npcEntity, newAbilityGuid, 2);
```

### Integration with Events
```csharp
// Example: Cast ability when player connects
EventManager.UserConnected += (sender, args) => {
  var player = args.Player;
  var welcomeAbility = new PrefabGUID(-1111111111);
  
  // Cast a welcome ability effect
  AbilityService.CastAbility(player.CharacterEntity, welcomeAbility);
};

// Example: Modify VBlood abilities on spawn
EventManager.UnitSpawn += (sender, args) => {
  var unit = args.Entity;
  
  if (unit.Has<VBloodUnit>()) {
    var enhancedAbility = new PrefabGUID(2222222222);
    AbilityService.ReplaceNpcAbilityOnSlot(unit, enhancedAbility, 0);
    Log.Info("VBlood abilities modified successfully");
  }
};
```

## Working with PrefabGUIDs


```csharp
// Example ability GUIDs (replace with actual values)
var guidString = "1234567890"; // Example GUID as string

if (PrefabGUID.TryParse(guidString, out PrefabGUID abilityGuid)) {
  // Successfully parsed GUID
  AbilityService.CastAbility(entity, abilityGuid);
} else {
  Log.Error("Invalid ability GUID format");
}

var abilityGuid = new PrefabGUID(-1434153969); // Integer GUID for an ability

// Use in ability casting
AbilityService.CastAbility(entity, abilityGuid);
```

## Important Notes

- **Entity validation** - Always verify entities exist before calling methods
- **NPC requirements** - ReplaceNpcAbilityOnSlot requires AbilityGroupSlotBuffer component
- **Slot indexing** - Ability slots are zero-based indexed
- **Error handling** - Methods include built-in validation and logging
