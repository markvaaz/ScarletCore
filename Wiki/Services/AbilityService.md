# AbilityService Documentation

## Overview

`AbilityService` is a utility service for managing entity abilities in V Rising. It provides methods to cast abilities and modify ability slots for both players and NPCs. The service supports both temporary (soft) and permanent (hard) ability replacements.

## Table of Contents

- [Ability Casting](#ability-casting)
- [Soft Ability Replacement](#soft-ability-replacement)
- [Hard Ability Replacement](#hard-ability-replacement)
- [Understanding Ability Slots](#understanding-ability-slots)
- [Examples](#examples)
- [Best Practices](#best-practices)

---

## Ability Casting

### CastAbility

```csharp
public static void CastAbility(Entity entity, PrefabGUID abilityGroup)
```

Casts an ability for the specified entity using the given ability group.

**Parameters:**
- `entity` - The entity that will cast the ability (player or NPC)
- `abilityGroup` - The GUID of the ability group to be cast

**Behavior:**
- Automatically detects if the entity is a player or NPC
- Handles user context appropriately for both entity types
- Executes the ability through the game's debug events system

**Example:**
```csharp
// Cast a specific ability
var abilityGuid = new PrefabGUID(1234567890);
AbilityService.CastAbility(playerEntity, abilityGuid);

// Cast an NPC ability
var npcAbility = new PrefabGUID(987654321);
AbilityService.CastAbility(npcEntity, npcAbility);
```

**Use Cases:**
- Force players to cast specific abilities
- Make NPCs use special abilities
- Create custom ability sequences
- Trigger abilities as part of events or mechanics

---

## Soft Ability Replacement

### ReplaceAbilityOnSlotSoft

```csharp
public static void ReplaceAbilityOnSlotSoft(
    Entity entity, 
    (PrefabGUID PrefabGUID, int Slot, int Priority, bool CopyCooldown)[] abilities
)
```

Performs a temporary (non-permanent) replacement of ability groups. The replacement is managed by the buff system and will be removed when the buff expires or is removed.

**Parameters:**
- `entity` - The target entity (player character or NPC)
- `abilities` - Array of tuples with the following structure:
  - `PrefabGUID` - The GUID of the new ability
  - `Slot` - The ability slot index (0-based)
  - `Priority` - The priority of the ability replacement
  - `CopyCooldown` - Whether to copy the cooldown from the previous ability

**Behavior:**
- Applies an internal replace buff to the entity
- Replacement is temporary and managed by the buff system
- Multiple abilities can be replaced in a single call
- Entries with empty PrefabGUID (GuidHash == 0) are ignored
- Returns early if buff application fails

**Example:**
```csharp
// Replace multiple abilities temporarily
var abilities = new[] {
    (new PrefabGUID(111111), 0, 100, true),  // Slot 0, priority 100, copy cooldown
    (new PrefabGUID(222222), 1, 100, false), // Slot 1, priority 100, don't copy cooldown
    (new PrefabGUID(333333), 2, 50, true)    // Slot 2, priority 50, copy cooldown
};

AbilityService.ReplaceAbilityOnSlotSoft(playerEntity, abilities);

// Replace single ability
var singleAbility = new[] {
    (new PrefabGUID(444444), 0, 99, false)
};

AbilityService.ReplaceAbilityOnSlotSoft(playerEntity, singleAbility);
```

**When to Use:**
- Temporary ability transformations
- Power-up effects that change abilities
- Class-switching mechanics
- Event-based ability modifications
- Testing abilities without permanent changes

---

## Hard Ability Replacement

### ReplaceAbilityOnSlotHard

```csharp
public static void ReplaceAbilityOnSlotHard(
    Entity entity, 
    PrefabGUID newAbilityGuid, 
    int abilitySlotIndex = 0, 
    int priority = 99
)
```

Permanently replaces an ability in a specific slot for an entity.

**Parameters:**
- `entity` - The entity whose ability slot will be modified
- `newAbilityGuid` - The GUID of the new ability to assign
- `abilitySlotIndex` - The slot index (default: 0)
- `priority` - The priority of the ability (default: 99)

**Behavior:**
- Permanently modifies the entity's ability group slot buffer
- Requires entity to have `AbilityGroupSlotBuffer` component
- Logs warning if component is missing
- Changes persist until manually reverted

**Example:**
```csharp
// Replace ability in first slot
var newAbility = new PrefabGUID(555555);
AbilityService.ReplaceAbilityOnSlotHard(playerEntity, newAbility);

// Replace ability in specific slot with custom priority
AbilityService.ReplaceAbilityOnSlotHard(
    playerEntity, 
    newAbility, 
    abilitySlotIndex: 2, 
    priority: 150
);
```

**When to Use:**
- Permanent ability changes
- Custom class systems
- Ability unlocks/upgrades
- NPC ability modifications
- Skill progression systems

---

## Understanding Ability Slots

### Slot Indices

Ability slots are zero-based and typically follow this structure:

| Slot | Common Use |
|------|------------|
| 0 | Primary ability (usually weapon-based) |
| 1 | Secondary ability |
| 2 | Third ability |
| 3 | Fourth ability |
| 4 | Ultimate ability |
| 5+ | Additional abilities (class/weapon dependent) |

**Note:** The exact number and purpose of slots may vary depending on the entity type (player, weapon, class, NPC).

### Priority System

Priority determines which ability replacement takes precedence when multiple replacements target the same slot:

| Priority | Use Case |
|----------|----------|
| 1-50 | Low priority (easily overridden) |
| 51-99 | Normal priority (default) |
| 100-150 | High priority (hard to override) |
| 150+ | Very high priority (dominant) |

**Higher priority values take precedence over lower values.**

### Copy Cooldown

When `CopyCooldown` is `true`:
- The new ability inherits the remaining cooldown from the previous ability
- Useful for seamless ability transitions
- Prevents cooldown abuse

When `CopyCooldown` is `false`:
- The new ability starts with its default cooldown state
- Useful when you want the ability available immediately

---

## Examples

### Example 1: Temporary Weapon Transform

```csharp
public class WeaponTransform {
    private static PrefabGUID WOLF_FORM_ABILITY = new PrefabGUID(123456);
    private static PrefabGUID BAT_FORM_ABILITY = new PrefabGUID(234567);
    
    public static void TransformToWolf(Entity player, float duration) {
        // Replace abilities with wolf form abilities
        var wolfAbilities = new[] {
            (WOLF_FORM_ABILITY, 0, 100, false)
        };
        
        AbilityService.ReplaceAbilityOnSlotSoft(player, wolfAbilities);
        
        // Schedule revert after duration
        ScheduleRevert(player, duration);
        
        Log.Info("Player transformed into wolf form");
    }
    
    public static void RemoveTransform(Entity player) {
        // Remove the replace buff to revert abilities
        BuffService.RemoveBuff(player, AbilityService.AbilityReplaceBuff);
        
        Log.Info("Player transformation removed");
    }
}
```

### Example 2: Dynamic Class System

```csharp
public class ClassSystem {
    // Define class abilities
    private static Dictionary<string, PrefabGUID[]> _classAbilities = new() {
        ["Warrior"] = new[] { 
            new PrefabGUID(111111), // Charge
            new PrefabGUID(111112), // Shield Bash
            new PrefabGUID(111113)  // War Cry
        },
        ["Mage"] = new[] { 
            new PrefabGUID(222221), // Fireball
            new PrefabGUID(222222), // Ice Nova
            new PrefabGUID(222223)  // Lightning Strike
        },
        ["Rogue"] = new[] { 
            new PrefabGUID(333331), // Backstab
            new PrefabGUID(333332), // Smoke Bomb
            new PrefabGUID(333333)  // Poison Dart
        }
    };
    
    public static void SetClass(Entity player, string className) {
        if (!_classAbilities.TryGetValue(className, out var abilities)) {
            Log.Error($"Unknown class: {className}");
            return;
        }
        
        // Permanently replace abilities for the class
        for (int i = 0; i < abilities.Length; i++) {
            AbilityService.ReplaceAbilityOnSlotHard(
                player, 
                abilities[i], 
                abilitySlotIndex: i, 
                priority: 100
            );
        }
        
        Log.Info($"Player class set to {className}");
    }
}
```

### Example 3: Power-Up System

```csharp
public class PowerUpSystem {
    public static void ApplyBerserkMode(Entity player, float duration) {
        // Enhance abilities temporarily
        var berserkAbilities = new[] {
            (new PrefabGUID(444441), 0, 150, true),  // Enhanced attack
            (new PrefabGUID(444442), 1, 150, true),  // Rage ability
            (new PrefabGUID(444443), 2, 150, true)   // Battle cry
        };
        
        AbilityService.ReplaceAbilityOnSlotSoft(player, berserkAbilities);
        
        // Visual feedback
        PlayerService.SendSystemMessage(player, "⚔️ BERSERK MODE ACTIVATED!");
        
        // Schedule removal
        ScheduleRemoval(player, duration);
    }
    
    public static void ApplyStealthMode(Entity player, float duration) {
        var stealthAbilities = new[] {
            (new PrefabGUID(555551), 0, 150, false), // Silent strike
            (new PrefabGUID(555552), 1, 150, false), // Shadow step
            (PrefabGUID.Empty, 2, 0, false)          // Remove third ability
        };
        
        AbilityService.ReplaceAbilityOnSlotSoft(player, stealthAbilities);
        
        PlayerService.SendSystemMessage(player, "👁️ STEALTH MODE ACTIVATED!");
    }
}
```

### Example 4: Boss Phase System

```csharp
public class BossPhases {
    private Entity _bossEntity;
    private int _currentPhase = 1;
    
    public void UpdatePhase(int newPhase) {
        _currentPhase = newPhase;
        
        switch (newPhase) {
            case 1:
                SetPhase1Abilities();
                break;
            case 2:
                SetPhase2Abilities();
                break;
            case 3:
                SetPhase3Abilities();
                break;
        }
    }
    
    private void SetPhase1Abilities() {
        // Basic abilities for phase 1
        AbilityService.ReplaceAbilityOnSlotHard(_bossEntity, new PrefabGUID(111111), 0);
        AbilityService.ReplaceAbilityOnSlotHard(_bossEntity, new PrefabGUID(111112), 1);
    }
    
    private void SetPhase2Abilities() {
        // Enhanced abilities for phase 2
        AbilityService.ReplaceAbilityOnSlotHard(_bossEntity, new PrefabGUID(222221), 0);
        AbilityService.ReplaceAbilityOnSlotHard(_bossEntity, new PrefabGUID(222222), 1);
        AbilityService.ReplaceAbilityOnSlotHard(_bossEntity, new PrefabGUID(222223), 2);
    }
    
    private void SetPhase3Abilities() {
        // Ultimate abilities for phase 3
        AbilityService.ReplaceAbilityOnSlotHard(_bossEntity, new PrefabGUID(333331), 0, priority: 200);
        AbilityService.ReplaceAbilityOnSlotHard(_bossEntity, new PrefabGUID(333332), 1, priority: 200);
        AbilityService.ReplaceAbilityOnSlotHard(_bossEntity, new PrefabGUID(333333), 2, priority: 200);
        AbilityService.ReplaceAbilityOnSlotHard(_bossEntity, new PrefabGUID(333334), 3, priority: 200);
    }
    
    public void TriggerUltimateAbility() {
        var ultimateAbility = new PrefabGUID(999999);
        AbilityService.CastAbility(_bossEntity, ultimateAbility);
    }
}
```

### Example 5: Ability Rotation System

```csharp
public class AbilityRotation {
    private Entity _entity;
    private Queue<PrefabGUID> _abilityQueue = new();
    private float _rotationDelay = 2.0f;
    private float _lastCastTime = 0f;
    
    public void AddAbilityToRotation(PrefabGUID abilityGuid) {
        _abilityQueue.Enqueue(abilityGuid);
    }
    
    public void Update() {
        if (_abilityQueue.Count == 0) return;
        
        float currentTime = Time.time;
        if (currentTime - _lastCastTime < _rotationDelay) return;
        
        // Get next ability in rotation
        var nextAbility = _abilityQueue.Dequeue();
        
        // Cast the ability
        AbilityService.CastAbility(_entity, nextAbility);
        
        // Add it back to the end of the queue for continuous rotation
        _abilityQueue.Enqueue(nextAbility);
        
        _lastCastTime = currentTime;
    }
    
    public void SetupWarriorRotation() {
        _abilityQueue.Clear();
        AddAbilityToRotation(new PrefabGUID(111111)); // Basic attack
        AddAbilityToRotation(new PrefabGUID(111112)); // Shield bash
        AddAbilityToRotation(new PrefabGUID(111113)); // Charge
        AddAbilityToRotation(new PrefabGUID(111114)); // War cry
    }
}
```

### Example 6: Conditional Ability Replacement

```csharp
public class ConditionalAbilities {
    public static void UpdateAbilitiesBasedOnHealth(Entity entity) {
        var health = entity.Read<Health>();
        float healthPercent = health.Value / health.MaxHealth;
        
        if (healthPercent < 0.3f) {
            // Low health - defensive abilities
            var defensiveAbilities = new[] {
                (new PrefabGUID(777771), 0, 120, false), // Heal
                (new PrefabGUID(777772), 1, 120, false), // Shield
                (new PrefabGUID(777773), 2, 120, false)  // Escape
            };
            
            AbilityService.ReplaceAbilityOnSlotSoft(entity, defensiveAbilities);
            PlayerService.SendSystemMessage(entity, "💔 LOW HEALTH - Defensive abilities activated!");
        } else if (healthPercent > 0.7f) {
            // High health - offensive abilities
            var offensiveAbilities = new[] {
                (new PrefabGUID(888881), 0, 120, false), // Power attack
                (new PrefabGUID(888882), 1, 120, false), // Berserker
                (new PrefabGUID(888883), 2, 120, false)  // Execute
            };
            
            AbilityService.ReplaceAbilityOnSlotSoft(entity, offensiveAbilities);
        }
    }
    
    public static void UpdateAbilitiesBasedOnWeather(Entity entity, string weather) {
        switch (weather) {
            case "Storm":
                // Lightning abilities during storm
                var stormAbilities = new[] {
                    (new PrefabGUID(999991), 0, 100, false)
                };
                AbilityService.ReplaceAbilityOnSlotSoft(entity, stormAbilities);
                break;
                
            case "Night":
                // Shadow abilities at night
                var nightAbilities = new[] {
                    (new PrefabGUID(999992), 0, 100, false)
                };
                AbilityService.ReplaceAbilityOnSlotSoft(entity, nightAbilities);
                break;
        }
    }
}
```

---

## Best Practices

### 1. Choose the Right Replacement Type

```csharp
// Use Soft for temporary changes
AbilityService.ReplaceAbilityOnSlotSoft(entity, tempAbilities); // Buff-managed, auto-reverts

// Use Hard for permanent changes
AbilityService.ReplaceAbilityOnSlotHard(entity, newAbility); // Requires manual revert
```

### 2. Always Validate Entity State

```csharp
// Good
if (!entity.Exists()) {
    Log.Error("Entity doesn't exist");
    return;
}

if (!entity.Has<AbilityGroupSlotBuffer>()) {
    Log.Error("Entity doesn't have ability slots");
    return;
}

AbilityService.ReplaceAbilityOnSlotHard(entity, ability);

// Bad - may cause errors
AbilityService.ReplaceAbilityOnSlotHard(entity, ability);
```

### 3. Use Appropriate Priority Values

```csharp
// Good - clear priority hierarchy
const int PRIORITY_LOW = 50;
const int PRIORITY_NORMAL = 100;
const int PRIORITY_HIGH = 150;
const int PRIORITY_ULTIMATE = 200;

// Use appropriate priority for the context
AbilityService.ReplaceAbilityOnSlotSoft(entity, new[] {
    (ability, 0, PRIORITY_HIGH, false) // High priority for important ability
});

// Bad - magic numbers without context
AbilityService.ReplaceAbilityOnSlotSoft(entity, new[] {
    (ability, 0, 73, false) // What does 73 mean?
});
```

### 4. Handle Copy Cooldown Thoughtfully

```csharp
// Copy cooldown for seamless transitions
var transition = new[] {
    (newAbility, 0, 100, true) // User won't notice the switch
};
AbilityService.ReplaceAbilityOnSlotSoft(entity, transition);

// Don't copy cooldown for power-ups (want immediate availability)
var powerUp = new[] {
    (powerfulAbility, 0, 150, false) // Ready to use immediately
};
AbilityService.ReplaceAbilityOnSlotSoft(entity, powerUp);
```

### 5. Clean Up Soft Replacements

```csharp
// Soft replacements are buff-managed but can be manually cleaned up
public static void RemoveAbilityReplacement(Entity entity) {
    // Remove the replace buff to restore original abilities
    var replaceBuff = new PrefabGUID(-1327674928);
    BuffService.RemoveBuff(entity, replaceBuff);
}
```

### 6. Log Important Ability Changes

```csharp
// Good - log ability modifications for debugging
Log.Info($"Replacing ability in slot {slot} with {newAbility.GuidHash}");
AbilityService.ReplaceAbilityOnSlotHard(entity, newAbility, slot);

// Consider logging for both players and admins
PlayerService.SendSystemMessage(entity, "Your abilities have been updated!");
```

### 7. Batch Ability Replacements

```csharp
// Good - replace multiple abilities at once (soft replacement)
var allAbilities = new[] {
    (ability1, 0, 100, false),
    (ability2, 1, 100, false),
    (ability3, 2, 100, false)
};
AbilityService.ReplaceAbilityOnSlotSoft(entity, allAbilities);

// Less efficient - multiple hard replacements
AbilityService.ReplaceAbilityOnSlotHard(entity, ability1, 0);
AbilityService.ReplaceAbilityOnSlotHard(entity, ability2, 1);
AbilityService.ReplaceAbilityOnSlotHard(entity, ability3, 2);
```

### 8. Test Ability Replacements Safely

```csharp
// Use soft replacement for testing
public static void TestAbility(Entity player, PrefabGUID abilityToTest) {
    var testAbility = new[] {
        (abilityToTest, 0, 999, false) // Very high priority, no cooldown copy
    };
    
    AbilityService.ReplaceAbilityOnSlotSoft(player, testAbility);
    
    // Automatically remove after 30 seconds
    ScheduleBuffRemoval(player, 30f);
}
```

---

## Key Differences: Soft vs Hard Replacement

| Feature | Soft Replacement | Hard Replacement |
|---------|------------------|------------------|
| **Duration** | Temporary (buff-based) | Permanent |
| **Persistence** | Until buff expires/removed | Until manually reverted |
| **Multiple Slots** | ✅ Yes (single call) | ❌ No (one at a time) |
| **Auto-Revert** | ✅ Yes (buff system) | ❌ No (manual only) |
| **Priority Support** | ✅ Yes | ✅ Yes |
| **Copy Cooldown** | ✅ Yes | ❌ No |
| **Use Case** | Transformations, power-ups | Class changes, upgrades |
| **Performance** | Slightly slower (buff overhead) | Direct modification |

---

## Important Notes

### Ability Group GUIDs
- Ability GUIDs must be valid ability group prefabs
- Invalid GUIDs may cause the game to crash or behave unexpectedly
- Use PrefabCollector or similar tools to find valid ability GUIDs

### Entity Requirements
- For **Hard Replacement**: Entity must have `AbilityGroupSlotBuffer` component
- For **Soft Replacement**: Entity must be able to receive buffs
- Both methods work with players and NPCs

### Casting Abilities
- `CastAbility` triggers the ability immediately
- The entity must have the ability available (not on cooldown)
- Some abilities require specific conditions (target, resources, etc.)

### Buff-Based Replacement
- Soft replacement uses buff: `EquipBuff_Weapon_Reaper_Ability01` (PrefabGUID: -1327674928)
- Removing this buff restores original abilities
- Multiple soft replacements on the same entity may conflict

---

## Thread Safety

⚠️ **Warning:** This service is **NOT thread-safe**. All methods must be called from the main thread as they interact with Unity's EntityManager and game systems.

---

## Requirements

- Unity ECS (Entities package)
- ProjectM namespace (game-specific)
- `BuffService` - for soft ability replacement (buff management)
- `GameSystems` - for accessing ServerGameManager and DebugEventsSystem
- Entity extensions (`IsPlayer()`, `GetUserEntity()`, etc.)

---

## License

Part of the ScarletCore framework.