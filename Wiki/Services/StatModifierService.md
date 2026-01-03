# StatModifierService

The `StatModifierService` provides functionality for applying, removing, and managing stat modifiers on entities using modifier buffs. It allows dynamic modification of unit stats like health, damage, speed, and more.

## Namespace
```csharp
using ScarletCore.Services;
```

## Overview

The `StatModifierService` is a static utility class that provides:
- Stat modification through modifier buffs
- Support for multiple modification types (Add, Multiply, Set)
- Persistent modifiers that survive death
- Dynamic buffer management for stat modifications
- Easy modifier application and removal

## Table of Contents

- [Modifier Struct](#modifier-struct)
- [Core Methods](#core-methods)
  - [ApplyModifiers](#applymodifiers)
  - [RemoveModifiers](#removemodifiers)
  - [TryRemoveModifierBuff](#tryremovemodifierbuff)
- [Buffer Management](#buffer-management)
  - [GetModifierBuffer](#getmodifierbuffer)
  - [AddModifiers](#addmodifiers)
- [Faction Management](#faction-management)
  - [SetFaction](#setfaction)
- [Unit Stat Types](#unit-stat-types)
- [Modification Types](#modification-types)
- [Complete Examples](#complete-examples)
- [Best Practices](#best-practices)
- [Related Services](#related-services)

## Modifier Struct

### Modifier
```csharp
public struct Modifier(float value, UnitStatType statType, ModificationType modificationType = ModificationType.Add)
```

Represents a single stat modification to be applied to a unit.

**Properties:**
- `Value` (float): The value of the modification to apply
- `StatType` (UnitStatType): The type of stat to modify (e.g., health, attack power)
- `ModificationType` (ModificationType): The type of modification (default: Add)

**Example:**
```csharp
// Create a health modifier that adds 1000 HP
var healthMod = new Modifier(1000f, UnitStatType.MaxHealth, ModificationType.Add);

// Create a speed modifier that multiplies by 1.5x
var speedMod = new Modifier(1.5f, UnitStatType.MovementSpeed, ModificationType.Multiply);

// Create a damage modifier (Add is default)
var damageMod = new Modifier(500f, UnitStatType.PhysicalPower);
```

---

## Core Methods

### ApplyModifiers
```csharp
public static void ApplyModifiers(Entity character, PrefabGUID modifierBuff, Modifier[] modifiers)
```

Applies an array of stat modifiers to a character entity using a specified modifier buff. Removes any existing modifier buff before applying the new one.

**Parameters:**
- `character` (Entity): The character entity to modify
- `modifierBuff` (PrefabGUID): The prefab GUID of the modifier buff to apply
- `modifiers` (Modifier[]): An array of modifiers to apply to the character

**Example:**
```csharp
using Stunlock.Core;

// Get player entity
var playerEntity = player.Entity;

// Define custom buff GUID (must be a modifier buff type)
var customBuffGUID = new PrefabGUID(1234567890);

// Create modifiers
var modifiers = new Modifier[] {
    new Modifier(2000f, UnitStatType.MaxHealth, ModificationType.Add),
    new Modifier(500f, UnitStatType.PhysicalPower, ModificationType.Add),
    new Modifier(1.3f, UnitStatType.MovementSpeed, ModificationType.Multiply)
};

// Apply modifiers
StatModifierService.ApplyModifiers(playerEntity, customBuffGUID, modifiers);

MessageService.SendSuccess(player, "Stat bonuses applied!");
```

**Notes:**
- Automatically removes existing buff before applying
- Uses 5-frame delay to ensure clean removal
- Sets buff as persisting through death
- Sets max stacks to 1
- If entity doesn't exist, method returns without error

---

### RemoveModifiers
```csharp
public static void RemoveModifiers(Entity character, PrefabGUID modifierBuff)
```

Removes all stat modifiers from a character entity by applying an empty modifier array.

**Parameters:**
- `character` (Entity): The character entity to remove modifiers from
- `modifierBuff` (PrefabGUID): The prefab GUID of the modifier buff to remove

**Example:**
```csharp
// Remove VIP bonuses
var vipBuffGUID = new PrefabGUID(1234567890);
StatModifierService.RemoveModifiers(playerEntity, vipBuffGUID);

MessageService.Send(player, "VIP bonuses removed.");

// Remove temporary event buffs
var eventBuffGUID = new PrefabGUID(987654321);
StatModifierService.RemoveModifiers(playerEntity, eventBuffGUID);
```

**Notes:**
- Internally calls ApplyModifiers with empty array
- Cleans up modifier buff from entity
- Safe to call even if buff doesn't exist

---

### TryRemoveModifierBuff
```csharp
public static bool TryRemoveModifierBuff(Entity character, PrefabGUID modifierBuff)
```

Attempts to remove a specific modifier buff from a character entity.

**Parameters:**
- `character` (Entity): The character entity to remove the buff from
- `modifierBuff` (PrefabGUID): The prefab GUID of the modifier buff to remove

**Returns:**
- `bool`: True if the buff was removed, false otherwise

**Example:**
```csharp
// Try to remove buff
var buffGUID = new PrefabGUID(1234567890);

if (StatModifierService.TryRemoveModifierBuff(playerEntity, buffGUID)) {
    MessageService.SendSuccess(player, "Buff removed successfully!");
} else {
    MessageService.SendWarning(player, "You don't have that buff.");
}

// Cleanup system
public void RemoveAllCustomBuffs(Entity entity) {
    var buffGuids = new[] {
        new PrefabGUID(111111111),
        new PrefabGUID(222222222),
        new PrefabGUID(333333333)
    };
    
    foreach (var buffGuid in buffGuids) {
        StatModifierService.TryRemoveModifierBuff(entity, buffGuid);
    }
}
```

**Notes:**
- Returns false if buff doesn't exist on entity
- Uses BuffService.TryRemoveBuff internally
- Non-throwing method (safe to call repeatedly)

---

## Buffer Management

### GetModifierBuffer
```csharp
public static DynamicBuffer<ModifyUnitStatBuff_DOTS> GetModifierBuffer(Entity buffEntity)
```

Gets or creates the modifier buffer on a buff entity for storing stat modifications.

**Parameters:**
- `buffEntity` (Entity): The buff entity to get or create the modifier buffer on

**Returns:**
- `DynamicBuffer<ModifyUnitStatBuff_DOTS>`: The dynamic buffer for stat modifications

**Example:**
```csharp
// Get buff entity
BuffService.TryApplyBuff(playerEntity, customBuffGUID, -1, out var buffEntity);

// Get modifier buffer
var modifierBuffer = StatModifierService.GetModifierBuffer(buffEntity);

if (modifierBuffer.IsCreated) {
    Log.Message($"Buffer has {modifierBuffer.Length} modifiers");
    
    // Manually add modifiers if needed
    // (Generally use ApplyModifiers instead)
}
```

**Notes:**
- Creates buffer if it doesn't exist
- Returns default buffer if creation fails
- Logs error on failure
- Generally used internally, prefer ApplyModifiers for most cases

---

### AddModifiers
```csharp
public static void AddModifiers(DynamicBuffer<ModifyUnitStatBuff_DOTS> modifiersBuffer, Modifier[] modifiers)
```

Adds an array of stat modifiers to the specified modifier buffer.

**Parameters:**
- `modifiersBuffer` (DynamicBuffer\<ModifyUnitStatBuff_DOTS\>): The buffer to add modifiers to
- `modifiers` (Modifier[]): The array of modifiers to add

**Example:**
```csharp
// Get buffer
var buffer = StatModifierService.GetModifierBuffer(buffEntity);

// Create modifiers
var additionalMods = new Modifier[] {
    new Modifier(100f, UnitStatType.PhysicalResistance, ModificationType.Add),
    new Modifier(50f, UnitStatType.SpellResistance, ModificationType.Add)
};

// Add to buffer
StatModifierService.AddModifiers(buffer, additionalMods);
```

**Notes:**
- Skips modifiers with value of 0
- Sets AttributeCapType to Uncapped
- Generates unique ModificationId for each modifier
- Generally used internally

---

## Faction Management

### SetFaction
```csharp
public static void SetFaction(this Entity entity, PrefabGUID factionPrefabGuid)
```

Sets the faction reference of an entity to the specified faction GUID. This is an extension method from ECSExtensions.

**Parameters:**
- `entity` (Entity): The entity to set the faction for
- `factionPrefabGuid` (PrefabGUID): The faction PrefabGUID to assign

**Example:**
```csharp
using ProjectM;

// Make entity friendly to players
entity.SetFaction(new PrefabGUID((int)FactionEnum.PlayerFaction));

// Make entity hostile
entity.SetFaction(new PrefabGUID((int)FactionEnum.NPC));

// Make entity neutral
entity.SetFaction(new PrefabGUID((int)FactionEnum.Trader));

// Common faction changes for spawned entities
var spawnedUnit = SpawnerService.ImmediateSpawn(unitGUID, position);
spawnedUnit.SetFaction(new PrefabGUID((int)FactionEnum.PlayerFaction));
MessageService.SendAll("Friendly unit spawned!");
```

**Common Factions:**
```csharp
public enum FactionEnum {
    PlayerFaction = 1106458752,    // Friendly to players
    NPC = -1212426608,             // Hostile to players
    Trader = -86032601,            // Neutral
    VBlood = -1094599966,          // VBlood bosses
    Bandit = 1913685020,           // Bandit faction
    Undead = -1039737026,          // Undead faction
    Militia = -528363064,          // Militia faction
    Werewolf = -1829615048,        // Werewolf faction
}
```

**Notes:**
- Only works if entity has FactionReference component
- Changes how entity interacts with other entities
- Affects aggro and alliance behavior

---

## Unit Stat Types

The service supports all V Rising unit stats:

### Combat Stats
```csharp
UnitStatType.MaxHealth          // Maximum health points
UnitStatType.PhysicalPower      // Physical damage output
UnitStatType.SpellPower         // Spell damage output
UnitStatType.PhysicalResistance // Physical damage reduction
UnitStatType.SpellResistance    // Spell damage reduction
UnitStatType.PhysicalCriticalStrikeChance    // Physical crit chance
UnitStatType.PhysicalCriticalStrikeDamage    // Physical crit damage
UnitStatType.SpellCriticalStrikeChance       // Spell crit chance
UnitStatType.SpellCriticalStrikeDamage       // Spell crit damage
```

### Movement & Utility Stats
```csharp
UnitStatType.MovementSpeed      // Movement speed
UnitStatType.AttackSpeed        // Attack speed
UnitStatType.CooldownModifier   // Cooldown reduction
UnitStatType.DamageVsUndeadPercent      // Damage bonus vs undead
UnitStatType.DamageVsHumansPercent      // Damage bonus vs humans
UnitStatType.DamageVsDemons            // Damage bonus vs demons
UnitStatType.DamageVsCastleObjects      // Damage bonus vs structures
```

### Resource Stats
```csharp
UnitStatType.ResourcePower      // Resource gathering power
UnitStatType.ResourceYield      // Resource gathering yield
UnitStatType.BloodEfficiency    // Blood feeding efficiency
```

### Other Stats
```csharp
UnitStatType.SunResistance      // Sun damage resistance
UnitStatType.GarlicResistance   // Garlic damage resistance
UnitStatType.SilverResistance   // Silver damage resistance
UnitStatType.HolyResistance     // Holy damage resistance
UnitStatType.PassiveHealthRegen // Health regeneration
```

---

## Modification Types

### ModificationType Enum

```csharp
ModificationType.Add        // Adds value to base stat
ModificationType.Multiply   // Multiplies stat by value
ModificationType.Set        // Sets stat to exact value
```

**Examples:**
```csharp
// Add 1000 to max health (base + 1000)
new Modifier(1000f, UnitStatType.MaxHealth, ModificationType.Add)

// Multiply movement speed by 1.5x (base * 1.5)
new Modifier(1.5f, UnitStatType.MovementSpeed, ModificationType.Multiply)

// Set physical power to 500 (exactly 500)
new Modifier(500f, UnitStatType.PhysicalPower, ModificationType.Set)

// Add 30% crit chance (add 0.3)
new Modifier(0.3f, UnitStatType.PhysicalCriticalStrikeChance, ModificationType.Add)
```

---

## Complete Examples

### Example 1: VIP Stat Bonus System

```csharp
using ScarletCore.Services;
using ScarletCore.Commanding;
using Stunlock.Core;
using ProjectM;

public class VIPStatSystem {
    private static readonly PrefabGUID VIP_BUFF_GUID = new PrefabGUID(1234567890);
    
    public static void ApplyVIPBonuses(PlayerData player) {
        var modifiers = new Modifier[] {
            // Health bonus: +2000 HP
            new Modifier(2000f, UnitStatType.MaxHealth, ModificationType.Add),
            
            // Damage bonuses: +500 physical, +500 spell
            new Modifier(500f, UnitStatType.PhysicalPower, ModificationType.Add),
            new Modifier(500f, UnitStatType.SpellPower, ModificationType.Add),
            
            // Speed bonus: +30%
            new Modifier(1.3f, UnitStatType.MovementSpeed, ModificationType.Multiply),
            
            // Resistances: +100 each
            new Modifier(100f, UnitStatType.PhysicalResistance, ModificationType.Add),
            new Modifier(100f, UnitStatType.SpellResistance, ModificationType.Add),
            
            // Crit bonuses: +20% chance, +50% damage
            new Modifier(0.2f, UnitStatType.PhysicalCriticalStrikeChance, ModificationType.Add),
            new Modifier(0.5f, UnitStatType.PhysicalCriticalStrikeDamage, ModificationType.Add),
            
            // Resource bonuses
            new Modifier(1.5f, UnitStatType.ResourceYield, ModificationType.Multiply),
            
            // Sun resistance: +50
            new Modifier(50f, UnitStatType.SunResistance, ModificationType.Add)
        };
        
        StatModifierService.ApplyModifiers(player.Entity, VIP_BUFF_GUID, modifiers);
        
        MessageService.SendSuccess(player, "VIP bonuses activated!");
        MessageService.Send(player, "Bonuses: +2000 HP, +500 Power, +30% Speed, and more!");
    }
    
    public static void RemoveVIPBonuses(PlayerData player) {
        StatModifierService.RemoveModifiers(player.Entity, VIP_BUFF_GUID);
        MessageService.SendWarning(player, "VIP bonuses removed.");
    }
    
    [Command("vip activate", description: "Activate VIP bonuses", adminOnly: true)]
    public static void ActivateVIPCommand(CommandContext ctx, PlayerData target) {
        ApplyVIPBonuses(target);
        MessageService.SendSuccess(ctx.Player, $"VIP bonuses applied to {target.Name}");
    }
}
```

### Example 2: Dynamic Class System

```csharp
public class ClassSystem {
    private static readonly Dictionary<string, PrefabGUID> ClassBuffs = new() {
        { "Warrior", new PrefabGUID(1111111111) },
        { "Mage", new PrefabGUID(2222222222) },
        { "Rogue", new PrefabGUID(3333333333) },
        { "Tank", new PrefabGUID(4444444444) }
    };
    
    public static void ApplyWarriorClass(PlayerData player) {
        var modifiers = new Modifier[] {
            new Modifier(3000f, UnitStatType.MaxHealth, ModificationType.Add),
            new Modifier(800f, UnitStatType.PhysicalPower, ModificationType.Add),
            new Modifier(150f, UnitStatType.PhysicalResistance, ModificationType.Add),
            new Modifier(0.9f, UnitStatType.MovementSpeed, ModificationType.Multiply) // -10% speed
        };
        
        StatModifierService.ApplyModifiers(player.Entity, ClassBuffs["Warrior"], modifiers);
        MessageService.Send(player, "Class: Warrior".WithColor("red"));
    }
    
    public static void ApplyMageClass(PlayerData player) {
        var modifiers = new Modifier[] {
            new Modifier(1000f, UnitStatType.MaxHealth, ModificationType.Add),
            new Modifier(1200f, UnitStatType.SpellPower, ModificationType.Add),
            new Modifier(0.5f, UnitStatType.CooldownModifier, ModificationType.Multiply), // -50% cooldowns
            new Modifier(50f, UnitStatType.SpellResistance, ModificationType.Add)
        };
        
        StatModifierService.ApplyModifiers(player.Entity, ClassBuffs["Mage"], modifiers);
        MessageService.Send(player, "Class: Mage".WithColor("blue"));
    }
    
    public static void ApplyRogueClass(PlayerData player) {
        var modifiers = new Modifier[] {
            new Modifier(1500f, UnitStatType.MaxHealth, ModificationType.Add),
            new Modifier(600f, UnitStatType.PhysicalPower, ModificationType.Add),
            new Modifier(1.5f, UnitStatType.MovementSpeed, ModificationType.Multiply), // +50% speed
            new Modifier(0.4f, UnitStatType.PhysicalCriticalStrikeChance, ModificationType.Add), // +40% crit
            new Modifier(1.0f, UnitStatType.PhysicalCriticalStrikeDamage, ModificationType.Add) // +100% crit dmg
        };
        
        StatModifierService.ApplyModifiers(player.Entity, ClassBuffs["Rogue"], modifiers);
        MessageService.Send(player, "Class: Rogue".WithColor("green"));
    }
    
    public static void ApplyTankClass(PlayerData player) {
        var modifiers = new Modifier[] {
            new Modifier(5000f, UnitStatType.MaxHealth, ModificationType.Add),
            new Modifier(300f, UnitStatType.PhysicalResistance, ModificationType.Add),
            new Modifier(200f, UnitStatType.SpellResistance, ModificationType.Add),
            new Modifier(100f, UnitStatType.PassiveHealthRegen, ModificationType.Add),
            new Modifier(0.8f, UnitStatType.MovementSpeed, ModificationType.Multiply) // -20% speed
        };
        
        StatModifierService.ApplyModifiers(player.Entity, ClassBuffs["Tank"], modifiers);
        MessageService.Send(player, "Class: Tank".WithColor("yellow"));
    }
    
    [Command("class", description: "Choose your class")]
    public static void ChooseClassCommand(CommandContext ctx, string className) {
        var player = ctx.Player;
        
        // Remove existing class
        foreach (var buff in ClassBuffs.Values) {
            StatModifierService.TryRemoveModifierBuff(player.Entity, buff);
        }
        
        // Apply new class
        switch (className.ToLower()) {
            case "warrior":
                ApplyWarriorClass(player);
                break;
            case "mage":
                ApplyMageClass(player);
                break;
            case "rogue":
                ApplyRogueClass(player);
                break;
            case "tank":
                ApplyTankClass(player);
                break;
            default:
                MessageService.SendError(player, "Unknown class! Available: warrior, mage, rogue, tank");
                return;
        }
        
        MessageService.SendSuccess(player, $"Class changed to {className}!");
    }
}
```

### Example 3: Temporary Buff System

```csharp
using ScarletCore.Systems;

public class TempBuffSystem {
    private static readonly PrefabGUID POWER_BUFF = new PrefabGUID(5555555555);
    private static readonly PrefabGUID SPEED_BUFF = new PrefabGUID(6666666666);
    private static readonly PrefabGUID TANK_BUFF = new PrefabGUID(7777777777);
    
    [Command("buff power", description: "Get a temporary power buff")]
    public static void PowerBuffCommand(CommandContext ctx, int duration = 60) {
        var player = ctx.Player;
        
        var modifiers = new Modifier[] {
            new Modifier(1000f, UnitStatType.PhysicalPower, ModificationType.Add),
            new Modifier(1000f, UnitStatType.SpellPower, ModificationType.Add)
        };
        
        StatModifierService.ApplyModifiers(player.Entity, POWER_BUFF, modifiers);
        MessageService.SendSuccess(player, $"Power buff activated for {duration} seconds!");
        
        // Remove after duration
        ActionScheduler.Schedule(() => {
            StatModifierService.RemoveModifiers(player.Entity, POWER_BUFF);
            MessageService.Send(player, "Power buff expired.");
        }, duration);
    }
    
    [Command("buff speed", description: "Get a temporary speed buff")]
    public static void SpeedBuffCommand(CommandContext ctx, int duration = 60) {
        var player = ctx.Player;
        
        var modifiers = new Modifier[] {
            new Modifier(2.0f, UnitStatType.MovementSpeed, ModificationType.Multiply),
            new Modifier(1.5f, UnitStatType.AttackSpeed, ModificationType.Multiply)
        };
        
        StatModifierService.ApplyModifiers(player.Entity, SPEED_BUFF, modifiers);
        MessageService.SendSuccess(player, $"Speed buff activated for {duration} seconds!");
        
        ActionScheduler.Schedule(() => {
            StatModifierService.RemoveModifiers(player.Entity, SPEED_BUFF);
            MessageService.Send(player, "Speed buff expired.");
        }, duration);
    }
    
    [Command("buff tank", description: "Get a temporary tank buff")]
    public static void TankBuffCommand(CommandContext ctx, int duration = 60) {
        var player = ctx.Player;
        
        var modifiers = new Modifier[] {
            new Modifier(5000f, UnitStatType.MaxHealth, ModificationType.Add),
            new Modifier(500f, UnitStatType.PhysicalResistance, ModificationType.Add),
            new Modifier(500f, UnitStatType.SpellResistance, ModificationType.Add),
            new Modifier(200f, UnitStatType.PassiveHealthRegen, ModificationType.Add)
        };
        
        StatModifierService.ApplyModifiers(player.Entity, TANK_BUFF, modifiers);
        MessageService.SendSuccess(player, $"Tank buff activated for {duration} seconds!");
        
        ActionScheduler.Schedule(() => {
            StatModifierService.RemoveModifiers(player.Entity, TANK_BUFF);
            MessageService.Send(player, "Tank buff expired.");
        }, duration);
    }
}
```

### Example 4: Boss Scaling System

```csharp
public class BossScalingSystem {
    public static void ScaleBoss(Entity bossEntity, int playerCount) {
        var scalingBuffGUID = new PrefabGUID(8888888888);
        
        // Scale based on player count
        float healthMultiplier = 1f + (playerCount * 0.5f); // +50% per player
        float damageMultiplier = 1f + (playerCount * 0.2f); // +20% per player
        float speedMultiplier = 1f + (playerCount * 0.1f);  // +10% per player
        
        var modifiers = new Modifier[] {
            new Modifier(healthMultiplier, UnitStatType.MaxHealth, ModificationType.Multiply),
            new Modifier(damageMultiplier, UnitStatType.PhysicalPower, ModificationType.Multiply),
            new Modifier(damageMultiplier, UnitStatType.SpellPower, ModificationType.Multiply),
            new Modifier(speedMultiplier, UnitStatType.MovementSpeed, ModificationType.Multiply),
            new Modifier(speedMultiplier, UnitStatType.AttackSpeed, ModificationType.Multiply)
        };
        
        StatModifierService.ApplyModifiers(bossEntity, scalingBuffGUID, modifiers);
        
        Log.Message($"Boss scaled for {playerCount} players: " +
                   $"{healthMultiplier}x HP, {damageMultiplier}x DMG, {speedMultiplier}x SPD");
    }
    
    public static void SpawnScaledBoss(float3 position) {
        var bossGUID = new PrefabGUID(-1905691330);
        var playerCount = PlayerService.GetAllConnected().Count();
        
        SpawnerService.SpawnWithPostAction(bossGUID, position, 600f, (boss) => {
            ScaleBoss(boss, playerCount);
            MessageService.Announce($"Scaled boss spawned for {playerCount} players!");
        });
    }
}
```

### Example 5: Faction System with Stats

```csharp
public class FactionStatSystem {
    private static readonly Dictionary<string, (PrefabGUID Buff, FactionEnum Faction)> Factions = new() {
        { "Vampire", (new PrefabGUID(9999999991), FactionEnum.PlayerFaction) },
        { "Werewolf", (new PrefabGUID(9999999992), FactionEnum.Werewolf) },
        { "Human", (new PrefabGUID(9999999993), FactionEnum.Militia) }
    };
    
    public static void JoinVampireFaction(PlayerData player) {
        var (buffGuid, faction) = Factions["Vampire"];
        
        var modifiers = new Modifier[] {
            new Modifier(1500f, UnitStatType.MaxHealth, ModificationType.Add),
            new Modifier(800f, UnitStatType.SpellPower, ModificationType.Add),
            new Modifier(100f, UnitStatType.BloodEfficiency, ModificationType.Add),
            new Modifier(-50f, UnitStatType.SunResistance, ModificationType.Add) // Weak to sun
        };
        
        StatModifierService.ApplyModifiers(player.Entity, buffGuid, modifiers);
        player.Entity.SetFaction(new PrefabGUID((int)faction));
        
        MessageService.Send(player, "You joined the Vampire faction!".WithColor("red"));
    }
    
    public static void JoinWerewolfFaction(PlayerData player) {
        var (buffGuid, faction) = Factions["Werewolf"];
        
        var modifiers = new Modifier[] {
            new Modifier(2500f, UnitStatType.MaxHealth, ModificationType.Add),
            new Modifier(1000f, UnitStatType.PhysicalPower, ModificationType.Add),
            new Modifier(1.4f, UnitStatType.MovementSpeed, ModificationType.Multiply),
            new Modifier(100f, UnitStatType.SilverResistance, ModificationType.Add) // Weak to silver
        };
        
        StatModifierService.ApplyModifiers(player.Entity, buffGuid, modifiers);
        player.Entity.SetFaction(new PrefabGUID((int)faction));
        
        MessageService.Send(player, "You joined the Werewolf faction!".WithColor("orange"));
    }
    
    public static void JoinHumanFaction(PlayerData player) {
        var (buffGuid, faction) = Factions["Human"];
        
        var modifiers = new Modifier[] {
            new Modifier(2000f, UnitStatType.MaxHealth, ModificationType.Add),
            new Modifier(600f, UnitStatType.PhysicalPower, ModificationType.Add),
            new Modifier(600f, UnitStatType.SpellPower, ModificationType.Add),
            new Modifier(1.5f, UnitStatType.ResourceYield, ModificationType.Multiply),
            new Modifier(100f, UnitStatType.HolyResistance, ModificationType.Add)
        };
        
        StatModifierService.ApplyModifiers(player.Entity, buffGuid, modifiers);
        player.Entity.SetFaction(new PrefabGUID((int)faction));
        
        MessageService.Send(player, "You joined the Human faction!".WithColor("white"));
    }
    
    [Command("faction", description: "Join a faction")]
    public static void JoinFactionCommand(CommandContext ctx, string factionName) {
        var player = ctx.Player;
        
        // Remove all faction buffs
        foreach (var (buffGuid, _) in Factions.Values) {
            StatModifierService.TryRemoveModifierBuff(player.Entity, buffGuid);
        }
        
        // Apply new faction
        switch (factionName.ToLower()) {
            case "vampire":
                JoinVampireFaction(player);
                break;
            case "werewolf":
                JoinWerewolfFaction(player);
                break;
            case "human":
                JoinHumanFaction(player);
                break;
            default:
                MessageService.SendError(player, "Unknown faction! Available: vampire, werewolf, human");
                return;
        }
    }
}
```

### Example 6: Level System with Stat Scaling

```csharp
public class LevelSystem {
    private static readonly PrefabGUID LEVEL_BUFF = new PrefabGUID(1010101010);
    private static Dictionary<ulong, int> playerLevels = new();
    
    public static void SetLevel(PlayerData player, int level) {
        playerLevels[player.PlatformId] = level;
        
        // Calculate stat bonuses based on level
        float healthBonus = level * 100f;        // +100 HP per level
        float damageBonus = level * 25f;         // +25 damage per level
        float resistanceBonus = level * 10f;     // +10 resistance per level
        float speedBonus = 1f + (level * 0.02f); // +2% speed per level
        
        var modifiers = new Modifier[] {
            new Modifier(healthBonus, UnitStatType.MaxHealth, ModificationType.Add),
            new Modifier(damageBonus, UnitStatType.PhysicalPower, ModificationType.Add),
            new Modifier(damageBonus, UnitStatType.SpellPower, ModificationType.Add),
            new Modifier(resistanceBonus, UnitStatType.PhysicalResistance, ModificationType.Add),
            new Modifier(resistanceBonus, UnitStatType.SpellResistance, ModificationType.Add),
            new Modifier(speedBonus, UnitStatType.MovementSpeed, ModificationType.Multiply)
        };
        
        StatModifierService.ApplyModifiers(player.Entity, LEVEL_BUFF, modifiers);
        
        MessageService.Send(player, $"Level {level} bonuses applied!".WithColor("gold"));
        MessageService.Send(player, $"+{healthBonus} HP, +{damageBonus} DMG, +{resistanceBonus} RES, +{(speedBonus - 1) * 100}% SPD");
    }
    
    public static int GetLevel(PlayerData player) {
        return playerLevels.GetValueOrDefault(player.PlatformId, 1);
    }
    
    [Command("setlevel", description: "Set player level", adminOnly: true)]
    public static void SetLevelCommand(CommandContext ctx, PlayerData target, int level) {
        if (level < 1 || level > 100) {
            MessageService.SendError(ctx.Player, "Level must be between 1 and 100!");
            return;
        }
        
        SetLevel(target, level);
        MessageService.SendSuccess(ctx.Player, $"Set {target.Name}'s level to {level}");
    }
}
```

---

## Best Practices

### 1. Use Appropriate Modification Types

```csharp
// Good - Use Add for flat bonuses
new Modifier(1000f, UnitStatType.MaxHealth, ModificationType.Add)

// Good - Use Multiply for percentage bonuses
new Modifier(1.5f, UnitStatType.MovementSpeed, ModificationType.Multiply) // 50% increase

// Good - Use Set for exact values (rare cases)
new Modifier(500f, UnitStatType.PhysicalPower, ModificationType.Set)
```

### 2. Clean Up Modifiers Properly

```csharp
// Good - Remove old modifiers before applying new ones
StatModifierService.RemoveModifiers(entity, oldBuffGUID);
StatModifierService.ApplyModifiers(entity, newBuffGUID, newModifiers);

// Good - Use TryRemoveModifierBuff for conditional removal
if (StatModifierService.TryRemoveModifierBuff(entity, buffGUID)) {
    MessageService.Send(player, "Buff removed.");
}
```

### 3. Use Unique Buff GUIDs

```csharp
// Good - Use unique GUIDs for different modifier sets
private static readonly PrefabGUID VIP_BUFF = new PrefabGUID(1111111111);
private static readonly PrefabGUID EVENT_BUFF = new PrefabGUID(2222222222);
private static readonly PrefabGUID CLASS_BUFF = new PrefabGUID(3333333333);

// Avoid - Reusing same GUID for different purposes
```

### 4. Validate Entity Existence

```csharp
// Good - Check entity before applying modifiers
if (entity.Exists()) {
    StatModifierService.ApplyModifiers(entity, buffGUID, modifiers);
}

// Service already does this internally, but good practice for clarity
```

### 5. Group Related Modifiers

```csharp
// Good - Group modifiers logically
var combatMods = new Modifier[] {
    new Modifier(1000f, UnitStatType.MaxHealth, ModificationType.Add),
    new Modifier(500f, UnitStatType.PhysicalPower, ModificationType.Add),
    new Modifier(100f, UnitStatType.PhysicalResistance, ModificationType.Add)
};

var utilityMods = new Modifier[] {
    new Modifier(1.3f, UnitStatType.MovementSpeed, ModificationType.Multiply),
    new Modifier(1.5f, UnitStatType.ResourceYield, ModificationType.Multiply)
};
```

### 6. Schedule Temporary Buffs

```csharp
// Good - Use ActionScheduler for temporary modifiers
StatModifierService.ApplyModifiers(entity, tempBuffGUID, modifiers);

ActionScheduler.Schedule(() => {
    StatModifierService.RemoveModifiers(entity, tempBuffGUID);
    MessageService.Send(player, "Buff expired.");
}, duration);
```

---

## Technical Notes

### Buff Persistence
- Modifiers are set to persist through death by default
- Uses `Buff_Persists_Through_Death` component
- Max stacks automatically set to 1

### Frame Delay
- ApplyModifiers uses 5-frame delay before reapplication
- Ensures clean buff removal to prevent errors
- Uses ActionScheduler.DelayedFrames internally

### Modification IDs
- Each modifier gets unique ModificationID
- Generated via ModificationIDs.Create().NewModificationId()
- Ensures proper tracking and removal

### Attribute Capping
- All modifiers use AttributeCapType.Uncapped by default
- Allows stats to exceed normal game limits
- Can be modified if capping is desired

### Buffer Management
- Uses DynamicBuffer<ModifyUnitStatBuff_DOTS> for storage
- Buffer is cleared before adding new modifiers
- Automatically creates buffer if not present

---

## Related Services
- [BuffService](BuffService.md) - For applying and removing buffs
- [PlayerService](PlayerService.md) - For player entity retrieval
- [SpawnerService](SpawnerService.md) - For spawning entities with modifiers
- [MessageService](MessageService.md) - For player notifications

## Notes
- Modifier buffs must be valid buff prefab GUIDs
- Zero-value modifiers are automatically skipped
- ApplyModifiers automatically removes existing buff before applying
- Entity existence is checked automatically
- Modifiers persist through death by default
