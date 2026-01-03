# SpawnerService

The `SpawnerService` provides comprehensive functionality for spawning entities in V Rising with various configurations, including position control, lifetime management, and post-spawn actions.

## Namespace
```csharp
using ScarletCore.Services;
```

## Overview

The `SpawnerService` is a static utility class that provides:
- Entity spawning at specific positions with range control
- Immediate entity instantiation for direct manipulation
- Entity copying from prefab collection
- Lifetime management (permanent or temporary entities)
- Multiple entity spawning with randomized positioning
- Post-spawn action execution

## Table of Contents

- [Core Spawning Methods](#core-spawning-methods)
  - [Spawn](#spawn)
  - [SpawnAtPosition](#spawnatposition)
  - [SpawnInRadius](#spawninradius)
  - [SpawnWithPostAction](#spawnwithpostaction)
- [Immediate Spawning](#immediate-spawning)
  - [ImmediateSpawn (Single)](#immediatespawn-single)
  - [ImmediateSpawn (Multiple)](#immediatespawn-multiple)
- [Copy Spawning](#copy-spawning)
  - [SpawnCopy (Single)](#spawncopy-single)
  - [SpawnCopy (Multiple)](#spawncopy-multiple)
- [Helper Methods](#helper-methods)
  - [GetDurationHash](#getdurationhash)
- [Complete Examples](#complete-examples)
- [Best Practices](#best-practices)
- [Related Services](#related-services)

## Core Spawning Methods

### Spawn
```csharp
public static bool Spawn(PrefabGUID prefabGUID, float3 position, float minRange = 0f, float maxRange = 0f, float lifeTime = 0f, int count = 1)
```

Spawns entities at a specific position with validation and error handling.

**Parameters:**
- `prefabGUID` (PrefabGUID): The prefab GUID of the entity to spawn
- `position` (float3): The position to spawn at
- `minRange` (float, optional): Minimum spawn offset from position (default: 0, must be >= 0)
- `maxRange` (float, optional): Maximum spawn offset from position (default: 0, must be >= minRange)
- `lifeTime` (float, optional): How long the entities should live in seconds (0 = permanent, default: 0)
- `count` (int, optional): Number of entities to spawn (default: 1, must be > 0)

**Returns:**
- `bool`: True if spawn was successful, false otherwise

**Example:**
```csharp
using Stunlock.Core;
using Unity.Mathematics;

// Spawn a single bear permanently
var bearGUID = new PrefabGUID(-1905691330);
SpawnerService.Spawn(bearGUID, new float3(100, 0, 100));

// Spawn 5 wolves in a 10-unit radius for 60 seconds
var wolfGUID = new PrefabGUID(-1342764880);
SpawnerService.Spawn(wolfGUID, playerPos, minRange: 5f, maxRange: 10f, lifeTime: 60f, count: 5);

// Spawn 3 enemies at exact position for 30 seconds
var enemyGUID = new PrefabGUID(1234567890);
SpawnerService.Spawn(enemyGUID, targetPosition, lifeTime: 30f, count: 3);
```

**Notes:**
- Returns false if prefabGUID is invalid (GuidHash == 0)
- Validates count > 0 and range values
- Entities with lifeTime = 0 are permanent
- Uses Unity's UnitSpawnerUpdateSystem internally

---

### SpawnAtPosition
```csharp
public static bool SpawnAtPosition(PrefabGUID prefabGUID, float3 position, float lifeTime)
```

Spawns a single entity at an exact position (convenience method).

**Parameters:**
- `prefabGUID` (PrefabGUID): The prefab GUID of the entity to spawn
- `position` (float3): The exact position to spawn at
- `lifeTime` (float): How long the entity should live in seconds (0 = permanent)

**Returns:**
- `bool`: True if spawn was successful

**Example:**
```csharp
// Spawn a chest at exact location
var chestGUID = new PrefabGUID(1530083623);
SpawnerService.SpawnAtPosition(chestGUID, new float3(100, 0, 50), 0f);

// Spawn a temporary boss at player location
var bossGUID = new PrefabGUID(-1905691330);
SpawnerService.SpawnAtPosition(bossGUID, player.Position, 300f);
```

---

### SpawnInRadius
```csharp
public static bool SpawnInRadius(PrefabGUID prefabGUID, float3 centerPosition, float radius, float lifeTime, int count)
```

Spawns multiple entities in a radius around a position.

**Parameters:**
- `prefabGUID` (PrefabGUID): The prefab GUID of the entity to spawn
- `centerPosition` (float3): The center position to spawn around
- `radius` (float): Spawn radius around the center position
- `lifeTime` (float): How long the entities should live in seconds (0 = permanent)
- `count` (int): Number of entities to spawn

**Returns:**
- `bool`: True if spawn was successful

**Example:**
```csharp
// Spawn 10 wolves in a 20-unit radius
var wolfGUID = new PrefabGUID(-1342764880);
SpawnerService.SpawnInRadius(wolfGUID, centerPos, 20f, 120f, 10);

// Spawn resource nodes in a circle
var ironGUID = new PrefabGUID(-1234567890);
SpawnerService.SpawnInRadius(ironGUID, mineLocation, 15f, 0f, 5);
```

**Notes:**
- Radius is rounded up to nearest integer for range calculation
- Entities are randomly distributed within the radius

---

### SpawnWithPostAction
```csharp
public static bool SpawnWithPostAction(PrefabGUID prefabGUID, float3 position, float lifeTime, Action<Entity> postSpawnAction)
```

Spawns an entity with a post-spawn action to be executed after spawning completes.

**Parameters:**
- `prefabGUID` (PrefabGUID): The prefab GUID of the entity to spawn
- `position` (float3): The position to spawn at
- `lifeTime` (float): How long the entity should live in seconds
- `postSpawnAction` (Action\<Entity\>): Action to execute after spawning (receives spawned entity)

**Returns:**
- `bool`: True if spawn was successful

**Example:**
```csharp
// Spawn a unit and modify its stats after spawn
var unitGUID = new PrefabGUID(1234567890);
SpawnerService.SpawnWithPostAction(unitGUID, spawnPos, 60f, (entity) => {
    // Modify health
    if (entity.Has<Health>()) {
        entity.Write((ref Health health) => {
            health.MaxHealth._Value = 10000;
            health.Value = 10000;
        });
    }
    
    // Set as friendly
    StatModifierService.SetFaction(entity, FactionEnum.PlayerFaction);
});

// Spawn a boss with custom buffs
var bossGUID = new PrefabGUID(-1905691330);
SpawnerService.SpawnWithPostAction(bossGUID, bossPos, 300f, (entity) => {
    BuffService.ApplyBuff(entity, new PrefabGUID(-123456789)); // Power buff
    MessageService.SendAll("A powerful boss has spawned!");
});
```

**Notes:**
- Uses UnitSpawnerReactSystemPatch for post-spawn execution
- Action is executed after the entity is fully spawned
- Useful for applying buffs, modifying stats, or setting faction

---

## Immediate Spawning

### ImmediateSpawn (Single)
```csharp
public static Entity ImmediateSpawn(PrefabGUID prefabGUID, float3 position, float minRange = 0f, float maxRange = 0f, float lifeTime = 0f, Entity owner = default)
```

Immediately instantiates a single entity and teleports it to a randomized position. Returns the spawned entity for direct manipulation.

**Parameters:**
- `prefabGUID` (PrefabGUID): The prefab GUID of the entity to spawn
- `position` (float3): The center position to spawn at
- `minRange` (float, optional): Minimum spawn offset from position (default: 0, must be >= 0)
- `maxRange` (float, optional): Maximum spawn offset from position (default: 0, must be >= minRange)
- `lifeTime` (float, optional): How long the entity should live in seconds (0 = permanent, default: 0)
- `owner` (Entity, optional): The owner entity (default: Entity.Null)

**Returns:**
- `Entity`: The spawned entity, or Entity.Null if failed

**Example:**
```csharp
// Spawn a unit and get its entity
var unitGUID = new PrefabGUID(1234567890);
var spawnedUnit = SpawnerService.ImmediateSpawn(unitGUID, spawnPos, 0f, 5f, 60f);

if (spawnedUnit != Entity.Null) {
    // Modify the entity directly
    BuffService.ApplyBuff(spawnedUnit, strengthBuffGUID);
    StatModifierService.SetFaction(spawnedUnit, FactionEnum.PlayerFaction);
    
    // Store reference for later use
    activeMinions.Add(spawnedUnit);
}

// Spawn item with owner
var itemGUID = new PrefabGUID(987654321);
var item = SpawnerService.ImmediateSpawn(itemGUID, dropPos, owner: playerEntity);
```

**Notes:**
- Returns Entity.Null on failure for validation
- Entity is available immediately for manipulation
- Position is randomized within min/max range

---

### ImmediateSpawn (Multiple)
```csharp
public static List<Entity> ImmediateSpawn(PrefabGUID prefabGUID, float3 position, float minRange, float maxRange, float lifeTime, int count, Entity owner = default)
```

Immediately instantiates multiple entities and teleports each to a randomized position.

**Parameters:**
- `prefabGUID` (PrefabGUID): The prefab GUID of the entity to spawn
- `position` (float3): The center position to spawn around
- `minRange` (float): Minimum spawn offset from position (must be >= 0)
- `maxRange` (float): Maximum spawn offset from position (must be >= minRange)
- `lifeTime` (float): How long the entities should live in seconds (0 = permanent)
- `count` (int): Number of entities to spawn (must be > 0)
- `owner` (Entity, optional): The owner entity (default: Entity.Null)

**Returns:**
- `List<Entity>`: List of spawned entities (empty if failed)

**Example:**
```csharp
// Spawn 10 guards and manage them
var guardGUID = new PrefabGUID(1234567890);
var guards = SpawnerService.ImmediateSpawn(guardGUID, castlePos, 5f, 15f, 0f, 10);

foreach (var guard in guards) {
    // Set all guards as friendly
    StatModifierService.SetFaction(guard, FactionEnum.PlayerFaction);
    
    // Apply guard buff
    BuffService.ApplyBuff(guard, guardBuffGUID);
}

MessageService.SendAll($"Spawned {guards.Count} castle guards!");

// Spawn temporary minions
var minionGUID = new PrefabGUID(987654321);
var minions = SpawnerService.ImmediateSpawn(minionGUID, playerPos, 3f, 8f, 120f, 5);
```

**Notes:**
- Returns empty list on validation failure
- Each entity can be manipulated individually
- All entities have same lifetime

---

## Copy Spawning

### SpawnCopy (Single)
```csharp
public static Entity SpawnCopy(PrefabGUID prefabGUID, float3 position, float minRange = 0, float maxRange = 0, float lifeTime = 0f)
```

Spawns a copy of an entity by instantiating the prefab from the prefab collection and teleporting it to a randomized position.

**Parameters:**
- `prefabGUID` (PrefabGUID): The prefab GUID of the entity to spawn
- `position` (float3): The center position to spawn at
- `minRange` (float, optional): Minimum spawn offset from position (default: 0, must be >= 0)
- `maxRange` (float, optional): Maximum spawn offset from position (default: 0, must be >= minRange)
- `lifeTime` (float, optional): How long the entity should live in seconds (0 = permanent, default: 0)

**Returns:**
- `Entity`: The spawned entity, or Entity.Null if failed

**Example:**
```csharp
// Copy an existing entity's prefab
var enemyPrefab = new PrefabGUID(-1905691330);
var copy = SpawnerService.SpawnCopy(enemyPrefab, spawnPos, 0f, 5f, 300f);

if (copy != Entity.Null) {
    // The copy inherits default prefab properties
    MessageService.SendAll("Elite enemy spawned!");
}

// Copy structure prefab
var structureGUID = new PrefabGUID(1234567890);
var structure = SpawnerService.SpawnCopy(structureGUID, buildPos);
```

**Notes:**
- Uses EntityManager.Instantiate instead of spawn system
- Copies prefab from PrefabCollectionSystem
- Returns Entity.Null if prefab not found
- Useful for spawning complex entities with default configurations

---

### SpawnCopy (Multiple)
```csharp
public static List<Entity> SpawnCopy(PrefabGUID prefabGUID, float3 position, float minRange, float maxRange, float lifeTime, int count)
```

Spawns multiple copies of an entity by instantiating the prefab from the prefab collection.

**Parameters:**
- `prefabGUID` (PrefabGUID): The prefab GUID of the entity to spawn
- `position` (float3): The center position to spawn around
- `minRange` (float): Minimum spawn offset from position (must be >= 0)
- `maxRange` (float): Maximum spawn offset from position (must be >= minRange)
- `lifeTime` (float): How long the entities should live in seconds (0 = permanent)
- `count` (int): Number of entities to spawn (must be > 0)

**Returns:**
- `List<Entity>`: List of spawned entities (empty if failed)

**Example:**
```csharp
// Spawn multiple enemy copies
var enemyGUID = new PrefabGUID(-1905691330);
var enemies = SpawnerService.SpawnCopy(enemyGUID, arenaCenter, 10f, 20f, 600f, 15);

MessageService.SendAll($"Wave started! {enemies.Count} enemies spawned!");

// Spawn decorative objects
var decorGUID = new PrefabGUID(123456789);
var decorations = SpawnerService.SpawnCopy(decorGUID, eventArea, 5f, 25f, 0f, 20);
```

**Notes:**
- Returns empty list on validation failure
- All copies are instantiated from same prefab
- Each copy is independent after spawning

---

## Helper Methods

### GetDurationHash
```csharp
public static long GetDurationHash()
```

Generates a unique duration hash based on current time. Used internally for post-spawn action tracking.

**Returns:**
- `long`: Unique hash value based on current timestamp

**Example:**
```csharp
// Used internally by SpawnWithPostAction
var hash = SpawnerService.GetDurationHash();
// Example output: 63817468942 (seconds since epoch)
```

**Notes:**
- Based on DateTime.Now.Ticks / TimeSpan.TicksPerSecond
- Ensures unique identification for post-spawn actions
- Generally not needed for external use

---

## Complete Examples

### Example 1: Wave Spawner System

```csharp
using ScarletCore.Services;
using ScarletCore.Systems;
using Unity.Mathematics;
using Stunlock.Core;

public class WaveSpawner {
    private int currentWave = 0;
    private float3 arenaCenter;
    private List<Entity> activeEnemies = new List<Entity>();
    
    public void StartWaveEvent(float3 center) {
        arenaCenter = center;
        currentWave = 0;
        SpawnNextWave();
    }
    
    private void SpawnNextWave() {
        currentWave++;
        
        // Clear previous wave
        activeEnemies.Clear();
        
        // Calculate wave difficulty
        int enemyCount = 5 + (currentWave * 2);
        float spawnRadius = 20f;
        float enemyLifetime = 180f; // 3 minutes
        
        // Announce wave
        MessageService.SendAll($"Wave {currentWave} starting! {enemyCount} enemies incoming!");
        
        // Spawn wave based on current wave number
        if (currentWave % 5 == 0) {
            // Boss wave every 5 waves
            SpawnBossWave();
        } else {
            // Regular wave
            SpawnRegularWave(enemyCount, spawnRadius, enemyLifetime);
        }
    }
    
    private void SpawnRegularWave(int count, float radius, float lifetime) {
        var wolfGUID = new PrefabGUID(-1342764880);
        var bearGUID = new PrefabGUID(-1905691330);
        
        // Spawn mix of enemies
        int wolfCount = (int)(count * 0.7f);
        int bearCount = count - wolfCount;
        
        // Spawn wolves
        var wolves = SpawnerService.ImmediateSpawn(
            wolfGUID,
            arenaCenter,
            10f,
            radius,
            lifetime,
            wolfCount
        );
        
        // Spawn bears
        var bears = SpawnerService.ImmediateSpawn(
            bearGUID,
            arenaCenter,
            10f,
            radius,
            lifetime,
            bearCount
        );
        
        // Set all as hostile and buff them
        foreach (var enemy in wolves.Concat(bears)) {
            activeEnemies.Add(enemy);
            
            // Apply wave buff (increased stats)
            BuffService.ApplyBuff(enemy, new PrefabGUID(1234567890));
            
            // Scale health based on wave
            if (enemy.Has<Health>()) {
                enemy.Write((ref Health health) => {
                    float multiplier = 1f + (currentWave * 0.2f);
                    health.MaxHealth._Value *= multiplier;
                    health.Value = health.MaxHealth._Value;
                });
            }
        }
    }
    
    private void SpawnBossWave() {
        var bossGUID = new PrefabGUID(-1905691330);
        
        MessageService.SendAll("BOSS WAVE!".WithColor("red"));
        
        // Spawn boss with post-spawn modifications
        SpawnerService.SpawnWithPostAction(bossGUID, arenaCenter, 300f, (bossEntity) => {
            // Make boss powerful
            if (bossEntity.Has<Health>()) {
                bossEntity.Write((ref Health health) => {
                    health.MaxHealth._Value = 50000;
                    health.Value = 50000;
                });
            }
            
            // Apply boss buffs
            BuffService.ApplyBuff(bossEntity, new PrefabGUID(111111111));
            BuffService.ApplyBuff(bossEntity, new PrefabGUID(222222222));
            
            activeEnemies.Add(bossEntity);
            MessageService.SendAll("The boss has appeared!");
        });
        
        // Spawn minions around boss
        var minionGUID = new PrefabGUID(123456789);
        var minions = SpawnerService.ImmediateSpawn(minionGUID, arenaCenter, 5f, 15f, 300f, 10);
        activeEnemies.AddRange(minions);
    }
    
    public void CheckWaveCompletion() {
        // Remove dead/despawned enemies
        activeEnemies.RemoveAll(e => !e.Exists());
        
        if (activeEnemies.Count == 0) {
            MessageService.SendAll($"Wave {currentWave} completed!");
            
            // Wait 10 seconds before next wave
            ActionScheduler.Schedule(() => {
                SpawnNextWave();
            }, 10);
        }
    }
}
```

### Example 2: Pet Summoning System

```csharp
public class PetSystem {
    private Dictionary<ulong, Entity> playerPets = new Dictionary<ulong, Entity>();
    
    [Command("summon", description: "Summon a pet companion")]
    public static void SummonPetCommand(CommandContext ctx, string petType) {
        var player = ctx.Player;
        
        // Check if player already has a pet
        if (playerPets.ContainsKey(player.PlatformId)) {
            var existingPet = playerPets[player.PlatformId];
            if (existingPet.Exists()) {
                MessageService.SendWarning(player, "You already have a pet! Dismiss it first.");
                return;
            }
        }
        
        // Get pet GUID based on type
        PrefabGUID petGUID;
        switch (petType.ToLower()) {
            case "wolf":
                petGUID = new PrefabGUID(-1342764880);
                break;
            case "bear":
                petGUID = new PrefabGUID(-1905691330);
                break;
            case "bat":
                petGUID = new PrefabGUID(1234567890);
                break;
            default:
                MessageService.SendError(player, "Unknown pet type! Available: wolf, bear, bat");
                return;
        }
        
        // Spawn pet near player
        var pet = SpawnerService.ImmediateSpawn(
            petGUID,
            player.Position,
            2f,
            4f,
            0f, // Permanent
            player.Entity // Set player as owner
        );
        
        if (pet != Entity.Null) {
            // Make pet friendly
            StatModifierService.SetFaction(pet, FactionEnum.PlayerFaction);
            
            // Apply pet buff (follow player, bonus stats)
            BuffService.ApplyBuff(pet, new PrefabGUID(987654321));
            
            // Store pet reference
            playerPets[player.PlatformId] = pet;
            
            MessageService.SendSuccess(player, $"Summoned {petType} companion!");
        } else {
            MessageService.SendError(player, "Failed to summon pet!");
        }
    }
    
    [Command("dismiss", description: "Dismiss your pet")]
    public static void DismissPetCommand(CommandContext ctx) {
        var player = ctx.Player;
        
        if (!playerPets.ContainsKey(player.PlatformId)) {
            MessageService.SendWarning(player, "You don't have a pet!");
            return;
        }
        
        var pet = playerPets[player.PlatformId];
        if (pet.Exists()) {
            // Despawn pet
            pet.Destroy();
            MessageService.SendSuccess(player, "Pet dismissed.");
        }
        
        playerPets.Remove(player.PlatformId);
    }
}
```

### Example 3: Resource Node Spawner

```csharp
public class ResourceSpawner {
    private List<Entity> spawnedNodes = new List<Entity>();
    
    public void SpawnResourceField(float3 centerPosition, string resourceType, int nodeCount) {
        PrefabGUID resourceGUID;
        float spawnRadius;
        
        switch (resourceType.ToLower()) {
            case "iron":
                resourceGUID = new PrefabGUID(1479774797);
                spawnRadius = 25f;
                break;
            case "copper":
                resourceGUID = new PrefabGUID(631552602);
                spawnRadius = 20f;
                break;
            case "stone":
                resourceGUID = new PrefabGUID(-1289923088);
                spawnRadius = 30f;
                break;
            default:
                Log.Warning($"Unknown resource type: {resourceType}");
                return;
        }
        
        // Spawn nodes using SpawnCopy for better structure handling
        var nodes = SpawnerService.SpawnCopy(
            resourceGUID,
            centerPosition,
            5f,
            spawnRadius,
            0f, // Permanent nodes
            nodeCount
        );
        
        spawnedNodes.AddRange(nodes);
        
        MessageService.SendAll($"Resource field spawned: {nodeCount} {resourceType} nodes!");
        Log.Message($"Spawned {nodes.Count} {resourceType} nodes at {centerPosition}");
    }
    
    public void ClearResourceField() {
        foreach (var node in spawnedNodes) {
            if (node.Exists()) {
                node.Destroy();
            }
        }
        
        spawnedNodes.Clear();
        MessageService.SendAll("Resource field cleared.");
    }
    
    [Command("spawnfield", description: "Spawn a resource field", adminOnly: true)]
    public static void SpawnFieldCommand(CommandContext ctx, string resourceType, int count) {
        var spawner = new ResourceSpawner();
        spawner.SpawnResourceField(ctx.Player.Position, resourceType, count);
    }
}
```

### Example 4: Event Mob Spawner

```csharp
public class EventMobSpawner {
    public static void SpawnBloodMoonEvent(float3 eventCenter) {
        MessageService.Announce("Blood Moon rises!".WithColor("red"));
        
        // Spawn boss with special modifications
        var vampireBossGUID = new PrefabGUID(-1905691330);
        
        SpawnerService.SpawnWithPostAction(vampireBossGUID, eventCenter, 600f, (boss) => {
            // Boss modifications
            if (boss.Has<Health>()) {
                boss.Write((ref Health health) => {
                    health.MaxHealth._Value = 100000;
                    health.Value = 100000;
                });
            }
            
            // Apply blood moon buffs
            BuffService.ApplyBuff(boss, new PrefabGUID(111111111)); // Strength
            BuffService.ApplyBuff(boss, new PrefabGUID(222222222)); // Speed
            BuffService.ApplyBuff(boss, new PrefabGUID(333333333)); // Regeneration
            
            MessageService.SendAll("The Blood Moon Boss has awakened!");
            
            // Spawn minions around boss after 2 seconds
            ActionScheduler.Schedule(() => {
                SpawnBossMinions(boss, eventCenter);
            }, 2);
        });
    }
    
    private static void SpawnBossMinions(Entity boss, float3 center) {
        var minionGUID = new PrefabGUID(123456789);
        
        // Spawn waves of minions
        for (int wave = 0; wave < 3; wave++) {
            ActionScheduler.Schedule(() => {
                var minions = SpawnerService.ImmediateSpawn(
                    minionGUID,
                    center,
                    10f,
                    25f,
                    120f, // 2 minute lifetime
                    8 // 8 minions per wave
                );
                
                // Link minions to boss
                foreach (var minion in minions) {
                    StatModifierService.SetFaction(minion, FactionEnum.NPC);
                }
                
                MessageService.SendAll($"Wave {wave + 1} of minions spawned!");
            }, wave * 30); // 30 seconds between waves
        }
    }
}
```

### Example 5: Loot Drop System

```csharp
public class LootDropSystem {
    public static void DropLoot(float3 dropPosition, List<PrefabGUID> lootItems) {
        foreach (var itemGUID in lootItems) {
            // Spawn item copies in a small radius
            SpawnerService.SpawnWithPostAction(itemGUID, dropPosition, 300f, (itemEntity) => {
                // Make item pickable
                if (itemEntity.Has<DropTableBuffer>()) {
                    // Configure drop table
                }
                
                // Add visual effect
                BuffService.ApplyBuff(itemEntity, new PrefabGUID(999999999)); // Glow effect
            });
            
            // Small delay between drops for visual effect
            System.Threading.Thread.Sleep(100);
        }
    }
    
    public static void DropBossLoot(float3 bossPosition) {
        var lootItems = new List<PrefabGUID> {
            new PrefabGUID(111111111), // Legendary weapon
            new PrefabGUID(222222222), // Rare armor
            new PrefabGUID(333333333), // Gold
            new PrefabGUID(444444444), // Gems
        };
        
        MessageService.Announce("Boss defeated! Legendary loot dropped!");
        
        // Spawn loot in circle around boss
        for (int i = 0; i < lootItems.Count; i++) {
            float angle = (360f / lootItems.Count) * i;
            float radians = angle * (math.PI / 180f);
            
            var offset = new float3(
                math.cos(radians) * 5f,
                0,
                math.sin(radians) * 5f
            );
            
            SpawnerService.SpawnCopy(lootItems[i], bossPosition + offset, 0f, 1f, 600f);
        }
    }
}
```

### Example 6: Guard Spawning System

```csharp
public class GuardSystem {
    private Dictionary<float3, List<Entity>> guardPosts = new Dictionary<float3, List<Entity>>();
    
    public void CreateGuardPost(float3 position, int guardCount) {
        var guardGUID = new PrefabGUID(1234567890);
        
        // Spawn guards in formation
        var guards = SpawnerService.ImmediateSpawn(
            guardGUID,
            position,
            3f,
            8f,
            0f, // Permanent guards
            guardCount
        );
        
        // Configure each guard
        foreach (var guard in guards) {
            // Set faction to friendly
            StatModifierService.SetFaction(guard, FactionEnum.PlayerFaction);
            
            // Apply guard buffs
            BuffService.ApplyBuff(guard, new PrefabGUID(111111111)); // Vigilance
            BuffService.ApplyBuff(guard, new PrefabGUID(222222222)); // Armor
            
            // Increase guard stats
            if (guard.Has<Health>()) {
                guard.Write((ref Health health) => {
                    health.MaxHealth._Value = 5000;
                    health.Value = 5000;
                });
            }
        }
        
        // Store guard post
        guardPosts[position] = guards;
        
        MessageService.SendAll($"Guard post established with {guards.Count} guards!");
    }
    
    public void RespawnGuards(float3 position) {
        if (!guardPosts.ContainsKey(position)) {
            Log.Warning("Guard post not found at position");
            return;
        }
        
        var guards = guardPosts[position];
        
        // Check for dead guards
        var deadGuards = guards.Where(g => !g.Exists()).ToList();
        
        if (deadGuards.Count > 0) {
            Log.Message($"Respawning {deadGuards.Count} guards at {position}");
            CreateGuardPost(position, deadGuards.Count);
        }
    }
    
    [Command("createguards", description: "Create a guard post", adminOnly: true)]
    public static void CreateGuardsCommand(CommandContext ctx, int count) {
        var system = new GuardSystem();
        system.CreateGuardPost(ctx.Player.Position, count);
    }
}
```

---

## Best Practices

### 1. Validate Parameters

```csharp
// Good - Validate before spawning
if (prefabGUID.GuidHash != 0 && count > 0) {
    SpawnerService.Spawn(prefabGUID, position, count: count);
} else {
    Log.Warning("Invalid spawn parameters");
}

// Good - Check spawn result
var entity = SpawnerService.ImmediateSpawn(enemyGUID, pos);
if (entity != Entity.Null) {
    // Entity spawned successfully
    activeEntities.Add(entity);
}
```

### 2. Use Appropriate Spawn Method

```csharp
// Good - Use Spawn for fire-and-forget spawning
SpawnerService.Spawn(enemyGUID, position, count: 10);

// Good - Use ImmediateSpawn when you need entity reference
var minion = SpawnerService.ImmediateSpawn(minionGUID, pos);
BuffService.ApplyBuff(minion, buffGUID);

// Good - Use SpawnCopy for structures/complex prefabs
var structure = SpawnerService.SpawnCopy(structureGUID, buildPos);

// Good - Use SpawnWithPostAction for deferred modifications
SpawnerService.SpawnWithPostAction(bossGUID, pos, 300f, (boss) => {
    ModifyBossStats(boss);
});
```

### 3. Manage Entity Lifetime

```csharp
// Good - Permanent entities (lifeTime = 0)
SpawnerService.Spawn(structureGUID, position, lifeTime: 0f);

// Good - Temporary entities with appropriate lifetime
SpawnerService.Spawn(effectGUID, position, lifeTime: 5f); // 5 second effect

// Good - Event entities with cleanup
SpawnerService.SpawnInRadius(enemyGUID, arenaCenter, 20f, 300f, 10);
```

### 4. Handle Spawn Positions Correctly

```csharp
// Good - Exact position spawning
SpawnerService.SpawnAtPosition(itemGUID, dropPosition, 60f);

// Good - Radius spawning for multiple entities
SpawnerService.SpawnInRadius(mobGUID, centerPos, 15f, 120f, 20);

// Good - Range control for spread
SpawnerService.Spawn(enemyGUID, position, minRange: 5f, maxRange: 15f, count: 10);
```

### 5. Clean Up Spawned Entities

```csharp
// Good - Track spawned entities
private List<Entity> spawnedEntities = new List<Entity>();

public void SpawnWave() {
    var entities = SpawnerService.ImmediateSpawn(enemyGUID, pos, 5f, 15f, 0f, 10);
    spawnedEntities.AddRange(entities);
}

public void CleanUp() {
    foreach (var entity in spawnedEntities) {
        if (entity.Exists()) {
            entity.Destroy();
        }
    }
    spawnedEntities.Clear();
}

// Good - Use lifetime for automatic cleanup
SpawnerService.Spawn(tempGUID, position, lifeTime: 60f);
```

### 6. Use Post-Spawn Actions Wisely

```csharp
// Good - Complex modifications after spawn
SpawnerService.SpawnWithPostAction(bossGUID, pos, 300f, (boss) => {
    ModifyHealth(boss, 50000);
    ApplyBuffs(boss);
    SetFaction(boss);
    MessagePlayers($"Boss spawned with {boss.GetHashCode()}");
});

// Avoid - Use ImmediateSpawn for simple modifications
var enemy = SpawnerService.ImmediateSpawn(enemyGUID, pos);
BuffService.ApplyBuff(enemy, buffGUID); // Simpler than post-action
```

---

## Technical Notes

### Spawn Methods Comparison

| Method | Returns Entity | Immediate | Use Case |
|--------|---------------|-----------|----------|
| `Spawn` | ❌ (bool) | ❌ | Fire-and-forget spawning |
| `ImmediateSpawn` | ✅ | ✅ | Need entity reference |
| `SpawnCopy` | ✅ | ✅ | Complex prefabs/structures |
| `SpawnWithPostAction` | ❌ (bool) | ❌ | Deferred modifications |

### Position Randomization
- Random position calculated as: `position + random(-maxRange, maxRange)`
- Y-axis (vertical) is not randomized (keeps spawn on ground)
- MinRange creates a "dead zone" around center position

### Lifetime System
- `lifeTime = 0`: Permanent entity (no automatic cleanup)
- `lifeTime > 0`: Entity auto-destroys after specified seconds
- Uses LifeTime component with LifeTimeEndAction.Destroy

### Owner System
- Owner parameter links spawned entity to another entity
- Useful for player-owned entities (pets, minions)
- Does not automatically set faction (use StatModifierService)

---

## Related Services
- [TeleportService](TeleportService.md) - For entity teleportation
- [BuffService](BuffService.md) - For applying buffs to spawned entities
- [StatModifierService](StatModifierService.md) - For modifying spawned entity stats
- [PlayerService](PlayerService.md) - For player position retrieval

## Notes
- All spawn methods validate parameters before execution
- Returns false/Entity.Null on validation failure
- Position is randomized within specified range (except Y-axis)
- Use `SpawnWithPostAction` for modifications that require entity to be fully initialized
- Entity.Null is used as default spawner for all spawn operations
