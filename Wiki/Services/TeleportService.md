# TeleportService

The `TeleportService` provides comprehensive functionality for teleporting entities with safety checks, validation, and advanced positioning features. It handles teleportation for players, NPCs, and any entity in the game world.

## Namespace
```csharp
using ScarletCore.Services;
```

## Overview

The `TeleportService` is a static utility class that provides:
- Safe entity teleportation with validation
- Player-specific teleportation methods
- Entity-to-entity teleportation
- Distance calculations between entities
- Position-based entity queries (nearest player, players in radius)
- Automatic position component updates

## Table of Contents

- [Basic Teleportation](#basic-teleportation)
  - [Teleport](#teleport)
  - [TeleportToEntity](#teleporttoentity)
  - [TeleportToPosition](#teleporttoposition)
- [Player-Specific Methods](#player-specific-methods)
  - [TeleportPlayerToPlayer](#teleportplayertoplayer)
  - [TeleportPlayer](#teleportplayer)
- [Utility Methods](#utility-methods)
  - [GetDistance](#getdistance)
  - [FindNearestPlayer](#findnearestplayer)
  - [GetPlayersInRadius](#getplayersinradius)
- [Complete Examples](#complete-examples)
- [Best Practices](#best-practices)
- [Related Services](#related-services)

## Basic Teleportation

### Teleport
```csharp
public static bool Teleport(Entity entity, float3 position)
```

Teleports the specified entity to the given world position using a teleport buff.

**Parameters:**
- `entity` (Entity): The entity to teleport
- `position` (float3): The target world position to teleport to

**Returns:**
- `bool`: True if teleportation was successful, false otherwise

**Example:**
```csharp
using Unity.Mathematics;

// Teleport player to coordinates
var targetPos = new float3(100, 0, 50);
if (TeleportService.Teleport(player.CharacterEntity, targetPos)) {
    MessageService.SendSuccess(player, "Teleported successfully!");
} else {
    MessageService.SendError(player, "Teleport failed!");
}

// Teleport enemy to arena center
var enemyEntity = SpawnerService.ImmediateSpawn(enemyGUID, spawnPos);
TeleportService.Teleport(enemyEntity, arenaCenter);

// Teleport to player position
TeleportService.Teleport(npcEntity, player.Position);
```

**Notes:**
- Uses teleport buff (PrefabGUID: 150521246)
- Returns false if buff application fails
- Handles all entity types (players, NPCs, units)

---

### TeleportToEntity
```csharp
public static bool TeleportToEntity(Entity entity, Entity target, float3 offset = default, bool validatePosition = true)
```

Teleports an entity to another entity's position with optional offset.

**Parameters:**
- `entity` (Entity): Entity to teleport
- `target` (Entity): Target entity to teleport to
- `offset` (float3, optional): Optional offset from target position (default: no offset)
- `validatePosition` (bool, optional): Whether to validate the target position is safe (default: true)

**Returns:**
- `bool`: True if teleportation was successful

**Example:**
```csharp
// Teleport player to another player
var targetPlayer = PlayerService.GetByName("TargetPlayer");
TeleportService.TeleportToEntity(player.CharacterEntity, targetPlayer.CharacterEntity);

// Teleport with offset (5 units behind target)
var offset = new float3(0, 0, -5);
TeleportService.TeleportToEntity(player.CharacterEntity, boss, offset);

// Teleport minion to player (no validation)
TeleportService.TeleportToEntity(minionEntity, player.CharacterEntity, default, false);

// Teleport to nearest enemy
var nearestEnemy = FindNearestEnemy(player.Position);
if (nearestEnemy != Entity.Null) {
    TeleportService.TeleportToEntity(player.CharacterEntity, nearestEnemy);
}
```

**Notes:**
- Validates both entities before teleporting
- Offset is added to target's position
- Returns false if either entity is invalid

---

### TeleportToPosition
```csharp
public static bool TeleportToPosition(Entity entity, float3 position, bool preserveRotation = true)
```

Teleports an entity to a specific position with optional rotation preservation.

**Parameters:**
- `entity` (Entity): Entity to teleport
- `position` (float3): Target position
- `preserveRotation` (bool, optional): Whether to keep current rotation (default: true)

**Returns:**
- `bool`: True if teleportation was successful

**Example:**
```csharp
// Teleport to exact coordinates
var position = new float3(100, 0, 50);
TeleportService.TeleportToPosition(player.CharacterEntity, position);

// Teleport and reset rotation
TeleportService.TeleportToPosition(enemyEntity, spawnPoint, preserveRotation: false);

// Teleport to calculated position
var centerPos = new float3(0, 0, 0);
var randomOffset = new float3(
    UnityEngine.Random.Range(-10f, 10f),
    0,
    UnityEngine.Random.Range(-10f, 10f)
);
TeleportService.TeleportToPosition(player.CharacterEntity, centerPos + randomOffset);
```

**Notes:**
- Updates all position-related components (Translation, LocalTransform, etc.)
- Preserves rotation by default
- Catches and logs errors

---

## Player-Specific Methods

### TeleportPlayerToPlayer
```csharp
public static bool TeleportPlayerToPlayer(string playerName, string targetPlayerName, float3 offset = default)
```

Teleports a player to another player's position by name.

**Parameters:**
- `playerName` (string): Name of player to teleport
- `targetPlayerName` (string): Name of target player
- `offset` (float3, optional): Optional offset from target (default: no offset)

**Returns:**
- `bool`: True if teleportation was successful

**Example:**
```csharp
// Teleport player to another player
if (TeleportService.TeleportPlayerToPlayer("Player1", "Player2")) {
    MessageService.SendAll("Player1 teleported to Player2!");
} else {
    Log.Warning("Failed to teleport - player not found");
}

// Teleport with offset
var offset = new float3(3, 0, 3);
TeleportService.TeleportPlayerToPlayer("Helper", "Target", offset);

// Command implementation
[Command("tpto", description: "Teleport to another player")]
public static void TeleportToPlayerCommand(CommandContext ctx, string targetName) {
    if (TeleportService.TeleportPlayerToPlayer(ctx.Player.Name, targetName)) {
        MessageService.SendSuccess(ctx.Player, $"Teleported to {targetName}!");
    } else {
        MessageService.SendError(ctx.Player, $"Player {targetName} not found!");
    }
}
```

**Notes:**
- Looks up both players by name
- Returns false if either player not found
- Uses TeleportToEntity internally

---

### TeleportPlayer
```csharp
public static bool TeleportPlayer(string playerName, float x, float y, float z, bool validatePosition = true)
```

Teleports a player to coordinates with safety checks.

**Parameters:**
- `playerName` (string): Name of player to teleport
- `x` (float): X coordinate
- `y` (float): Y coordinate
- `z` (float): Z coordinate
- `validatePosition` (bool, optional): Whether to validate the position (default: true)

**Returns:**
- `bool`: True if teleportation was successful

**Example:**
```csharp
// Teleport player to coordinates
TeleportService.TeleportPlayer("PlayerName", 100, 0, 50);

// Teleport to spawn point
TeleportService.TeleportPlayer("NewPlayer", 0, 0, 0);

// Teleport without validation (unsafe areas)
TeleportService.TeleportPlayer("Admin", 999, 0, 999, validatePosition: false);

// Command implementation
[Command("tp", description: "Teleport to coordinates")]
public static void TeleportCommand(CommandContext ctx, float x, float y, float z) {
    if (TeleportService.TeleportPlayer(ctx.Player.Name, x, y, z)) {
        MessageService.SendSuccess(ctx.Player, $"Teleported to ({x}, {y}, {z})!");
    } else {
        MessageService.SendError(ctx.Player, "Teleport failed!");
    }
}
```

**Notes:**
- Looks up player by name
- Returns false if player not found
- Y coordinate is typically 0 (ground level)

---

## Utility Methods

### GetDistance
```csharp
public static float GetDistance(Entity entity1, Entity entity2)
```

Calculates the distance between two entities.

**Parameters:**
- `entity1` (Entity): First entity
- `entity2` (Entity): Second entity

**Returns:**
- `float`: Distance between entities, or -1 if either entity is invalid

**Example:**
```csharp
// Check distance between player and boss
var distance = TeleportService.GetDistance(player.CharacterEntity, bossEntity);

if (distance < 10f) {
    MessageService.Send(player, "You're too close to the boss!");
} else if (distance > 50f) {
    MessageService.Send(player, "Boss is too far away!");
}

// Range check for ability
var targetDistance = TeleportService.GetDistance(caster, target);
if (targetDistance > 20f) {
    MessageService.SendError(player, "Target is out of range! (Max: 20m)");
    return;
}

// Find if players are nearby
var player1 = PlayerService.GetByName("Player1");
var player2 = PlayerService.GetByName("Player2");
var distance = TeleportService.GetDistance(player1.CharacterEntity, player2.CharacterEntity);

MessageService.SendAll($"Distance between players: {distance:F2} meters");
```

**Notes:**
- Returns -1 if either entity is invalid
- Uses MathUtility.Distance internally
- Calculates 3D Euclidean distance

---

### FindNearestPlayer
```csharp
public static PlayerData FindNearestPlayer(float3 position, float maxDistance = float.MaxValue)
```

Finds the nearest player to a given position within a maximum distance.

**Parameters:**
- `position` (float3): Reference position
- `maxDistance` (float, optional): Maximum distance to consider (default: unlimited)

**Returns:**
- `PlayerData`: PlayerData of nearest player, or null if none found

**Example:**
```csharp
// Find nearest player to position
var nearestPlayer = TeleportService.FindNearestPlayer(eventCenter);

if (nearestPlayer != null) {
    MessageService.Send(nearestPlayer, "You are the closest to the event!");
}

// Find nearest player within 50 units
var nearestInRange = TeleportService.FindNearestPlayer(bossPosition, 50f);

if (nearestInRange != null) {
    MessageService.Send(nearestInRange, "Boss is targeting you!");
} else {
    MessageService.SendAll("No players in range of boss!");
}

// Event targeting system
public static void StartTargetedEvent(float3 eventPos) {
    var target = TeleportService.FindNearestPlayer(eventPos, 100f);
    
    if (target != null) {
        MessageService.Send(target, "A special event is targeting you!");
        SpawnerService.SpawnInRadius(enemyGUID, target.Position, 15f, 300f, 10);
    }
}
```

**Notes:**
- Returns null if no players found
- Only considers valid player entities
- Searches through PlayerService.AllPlayers

---

### GetPlayersInRadius
```csharp
public static List<PlayerData> GetPlayersInRadius(float3 center, float radius)
```

Gets all players within a specified radius of a position.

**Parameters:**
- `center` (float3): Center position
- `radius` (float): Search radius

**Returns:**
- `List<PlayerData>`: List of players within radius (empty if none found)

**Example:**
```csharp
// Get players near event
var playersNearby = TeleportService.GetPlayersInRadius(eventCenter, 30f);

MessageService.SendAll($"{playersNearby.Count} players near the event!");

foreach (var player in playersNearby) {
    MessageService.Send(player, "You're in the event area!");
    BuffService.ApplyBuff(player.CharacterEntity, eventBuffGUID);
}

// Area damage system
public static void ApplyAreaDamage(float3 explosionCenter, float radius) {
    var affectedPlayers = TeleportService.GetPlayersInRadius(explosionCenter, radius);
    
    foreach (var player in affectedPlayers) {
        var distance = TeleportService.GetDistance(
            player.CharacterEntity, 
            explosionCenter
        );
        
        float damageMultiplier = 1f - (distance / radius);
        int damage = (int)(1000 * damageMultiplier);
        
        // Apply damage logic here
        MessageService.SendWarning(player, $"Hit by explosion! {damage} damage!");
    }
}

// Reward nearby players
var playersToReward = TeleportService.GetPlayersInRadius(questNPC, 10f);
foreach (var player in playersToReward) {
    GiveReward(player);
    MessageService.SendSuccess(player, "Quest completed!");
}
```

**Notes:**
- Returns empty list if no players found
- Checks all players, even offline (validates entity)
- Inclusive radius check (distance <= radius)

---

## Complete Examples

### Example 1: Teleport Command System

```csharp
using ScarletCore.Services;
using ScarletCore.Commanding;
using Unity.Mathematics;

public class TeleportCommands {
    private static Dictionary<ulong, float3> savedLocations = new();
    
    [Command("tp", description: "Teleport to coordinates")]
    public static void TeleportCommand(CommandContext ctx, float x, float y, float z) {
        if (TeleportService.TeleportPlayer(ctx.Player.Name, x, y, z)) {
            MessageService.SendSuccess(ctx.Player, $"Teleported to ({x}, {y}, {z})!");
        } else {
            MessageService.SendError(ctx.Player, "Teleport failed!");
        }
    }
    
    [Command("tpto", description: "Teleport to another player")]
    public static void TeleportToPlayerCommand(CommandContext ctx, string targetName) {
        if (TeleportService.TeleportPlayerToPlayer(ctx.Player.Name, targetName)) {
            MessageService.SendSuccess(ctx.Player, $"Teleported to {targetName}!");
        } else {
            MessageService.SendError(ctx.Player, $"Player '{targetName}' not found!");
        }
    }
    
    [Command("tphere", description: "Teleport a player to you", adminOnly: true)]
    public static void TeleportHereCommand(CommandContext ctx, string targetName) {
        if (TeleportService.TeleportPlayerToPlayer(targetName, ctx.Player.Name)) {
            MessageService.SendSuccess(ctx.Player, $"Teleported {targetName} to you!");
            
            if (PlayerService.TryGetByName(targetName, out var target)) {
                MessageService.Send(target, $"You were teleported to {ctx.Player.Name}");
            }
        } else {
            MessageService.SendError(ctx.Player, $"Player '{targetName}' not found!");
        }
    }
    
    [Command("tpall", description: "Teleport all players to you", adminOnly: true)]
    public static void TeleportAllCommand(CommandContext ctx) {
        var adminPos = ctx.Player.Position;
        int count = 0;
        
        foreach (var player in PlayerService.GetAllConnected()) {
            if (player.PlatformId == ctx.Player.PlatformId) continue;
            
            if (TeleportService.TeleportToPosition(player.CharacterEntity, adminPos)) {
                MessageService.Send(player, $"Teleported to {ctx.Player.Name}");
                count++;
            }
        }
        
        MessageService.SendSuccess(ctx.Player, $"Teleported {count} players to you!");
    }
    
    [Command("tpsave", description: "Save your current location")]
    public static void SaveLocationCommand(CommandContext ctx, string name) {
        savedLocations[ctx.Player.PlatformId] = ctx.Player.Position;
        MessageService.SendSuccess(ctx.Player, $"Location '{name}' saved!");
    }
    
    [Command("tpload", description: "Teleport to saved location")]
    public static void LoadLocationCommand(CommandContext ctx) {
        if (!savedLocations.ContainsKey(ctx.Player.PlatformId)) {
            MessageService.SendError(ctx.Player, "No saved location!");
            return;
        }
        
        var savedPos = savedLocations[ctx.Player.PlatformId];
        if (TeleportService.TeleportToPosition(ctx.Player.CharacterEntity, savedPos)) {
            MessageService.SendSuccess(ctx.Player, "Teleported to saved location!");
        }
    }
}
```

### Example 2: Home System

```csharp
public class HomeSystem {
    private static Dictionary<ulong, float3> playerHomes = new();
    private static Dictionary<ulong, DateTime> lastTeleport = new();
    private static readonly int COOLDOWN_SECONDS = 300; // 5 minutes
    
    [Command("sethome", description: "Set your home location")]
    public static void SetHomeCommand(CommandContext ctx) {
        playerHomes[ctx.Player.PlatformId] = ctx.Player.Position;
        MessageService.SendSuccess(ctx.Player, "Home location set!");
    }
    
    [Command("home", description: "Teleport to your home")]
    public static void HomeCommand(CommandContext ctx) {
        // Check if home is set
        if (!playerHomes.ContainsKey(ctx.Player.PlatformId)) {
            MessageService.SendError(ctx.Player, "You haven't set a home! Use /sethome first.");
            return;
        }
        
        // Check cooldown
        if (lastTeleport.ContainsKey(ctx.Player.PlatformId)) {
            var timeSinceLastTP = DateTime.Now - lastTeleport[ctx.Player.PlatformId];
            var remaining = COOLDOWN_SECONDS - timeSinceLastTP.TotalSeconds;
            
            if (remaining > 0) {
                MessageService.SendWarning(ctx.Player, 
                    $"Home teleport on cooldown! Wait {remaining:F0} seconds.");
                return;
            }
        }
        
        // Teleport to home
        var homePos = playerHomes[ctx.Player.PlatformId];
        if (TeleportService.TeleportToPosition(ctx.Player.CharacterEntity, homePos)) {
            MessageService.SendSuccess(ctx.Player, "Welcome home!");
            lastTeleport[ctx.Player.PlatformId] = DateTime.Now;
        } else {
            MessageService.SendError(ctx.Player, "Failed to teleport home!");
        }
    }
    
    [Command("delhome", description: "Delete your home location")]
    public static void DeleteHomeCommand(CommandContext ctx) {
        if (playerHomes.Remove(ctx.Player.PlatformId)) {
            MessageService.SendSuccess(ctx.Player, "Home location deleted.");
        } else {
            MessageService.SendError(ctx.Player, "You don't have a home set!");
        }
    }
}
```

### Example 3: Arena System with Teleportation

```csharp
using ScarletCore.Systems;

public class ArenaSystem {
    private static float3 arenaCenter = new float3(0, 0, 0);
    private static float arenaRadius = 30f;
    private static List<PlayerData> arenaPlayers = new();
    private static bool arenaActive = false;
    
    [Command("arena join", description: "Join the arena")]
    public static void JoinArenaCommand(CommandContext ctx) {
        if (!arenaActive) {
            MessageService.SendError(ctx.Player, "No arena event is active!");
            return;
        }
        
        if (arenaPlayers.Contains(ctx.Player)) {
            MessageService.SendWarning(ctx.Player, "You're already in the arena!");
            return;
        }
        
        // Teleport to random position in arena
        var randomAngle = UnityEngine.Random.Range(0f, 360f) * (math.PI / 180f);
        var randomRadius = UnityEngine.Random.Range(5f, arenaRadius - 5f);
        
        var spawnPos = arenaCenter + new float3(
            math.cos(randomAngle) * randomRadius,
            0,
            math.sin(randomAngle) * randomRadius
        );
        
        if (TeleportService.TeleportToPosition(ctx.Player.CharacterEntity, spawnPos)) {
            arenaPlayers.Add(ctx.Player);
            MessageService.SendSuccess(ctx.Player, "You joined the arena!");
            MessageService.SendAll($"{ctx.Player.Name} joined the arena! ({arenaPlayers.Count} players)");
        }
    }
    
    [Command("arena start", description: "Start arena event", adminOnly: true)]
    public static void StartArenaCommand(CommandContext ctx) {
        arenaActive = true;
        arenaPlayers.Clear();
        arenaCenter = ctx.Player.Position;
        
        MessageService.Announce("Arena event starting! Use /arena join to participate!");
        
        // Start countdown
        ActionScheduler.Schedule(() => {
            StartArenaFight();
        }, 30);
    }
    
    private static void StartArenaFight() {
        if (arenaPlayers.Count < 2) {
            MessageService.Announce("Not enough players! Arena cancelled.");
            arenaActive = false;
            return;
        }
        
        MessageService.Announce($"Arena fight starting with {arenaPlayers.Count} players!");
        
        // Spawn enemies around arena
        SpawnerService.SpawnInRadius(
            new PrefabGUID(-1905691330),
            arenaCenter,
            arenaRadius,
            300f,
            arenaPlayers.Count * 2
        );
        
        // Monitor arena
        MonitorArena();
    }
    
    private static void MonitorArena() {
        ActionScheduler.Repeating(() => {
            if (!arenaActive) return;
            
            // Check players out of bounds
            foreach (var player in arenaPlayers.ToList()) {
                var distance = TeleportService.GetDistance(
                    player.CharacterEntity,
                    arenaCenter
                );
                
                if (distance > arenaRadius) {
                    MessageService.Send(player, "You left the arena!");
                    arenaPlayers.Remove(player);
                }
            }
            
            // Check if arena is complete
            if (arenaPlayers.Count == 0) {
                MessageService.Announce("Arena event ended!");
                arenaActive = false;
            }
        }, 2);
    }
}
```

### Example 4: Boss Teleportation Mechanics

```csharp
public class BossMechanics {
    public static void ApplyTeleportMechanic(Entity bossEntity, List<PlayerData> players) {
        // Teleport boss every 30 seconds
        ActionScheduler.Repeating(() => {
            if (!bossEntity.Exists()) return;
            
            // Find nearest player
            var bossPos = bossEntity.Position();
            var nearestPlayer = TeleportService.FindNearestPlayer(bossPos, 50f);
            
            if (nearestPlayer != null) {
                // Teleport boss near player
                var offset = new float3(5, 0, 5);
                TeleportService.TeleportToEntity(bossEntity, nearestPlayer.CharacterEntity, offset);
                
                MessageService.SendWarning(nearestPlayer, "The boss teleported to you!");
            }
        }, 30);
    }
    
    public static void ApplyPlayerScatterMechanic(Entity bossEntity) {
        ActionScheduler.Repeating(() => {
            if (!bossEntity.Exists()) return;
            
            var bossPos = bossEntity.Position();
            var nearbyPlayers = TeleportService.GetPlayersInRadius(bossPos, 15f);
            
            if (nearbyPlayers.Count > 0) {
                MessageService.SendAll("Boss used Scatter! Players thrown back!");
                
                foreach (var player in nearbyPlayers) {
                    // Calculate direction away from boss
                    var playerPos = player.Position;
                    var direction = math.normalize(playerPos - bossPos);
                    var scatterPos = playerPos + (direction * 20f); // 20 units away
                    
                    TeleportService.TeleportToPosition(player.CharacterEntity, scatterPos);
                    MessageService.Send(player, "You were thrown back!");
                }
            }
        }, 45);
    }
}
```

### Example 5: Waypoint System

```csharp
public class WaypointSystem {
    private static Dictionary<string, float3> globalWaypoints = new() {
        { "spawn", new float3(0, 0, 0) },
        { "dungeon", new float3(100, 0, 100) },
        { "boss", new float3(-50, 0, 75) },
        { "pvp", new float3(200, 0, 200) }
    };
    
    [Command("waypoint", description: "Teleport to a waypoint")]
    public static void WaypointCommand(CommandContext ctx, string waypointName) {
        if (!globalWaypoints.ContainsKey(waypointName.ToLower())) {
            MessageService.SendError(ctx.Player, "Waypoint not found!");
            MessageService.Send(ctx.Player, 
                $"Available: {string.Join(", ", globalWaypoints.Keys)}");
            return;
        }
        
        var waypointPos = globalWaypoints[waypointName.ToLower()];
        
        if (TeleportService.TeleportToPosition(ctx.Player.CharacterEntity, waypointPos)) {
            MessageService.SendSuccess(ctx.Player, $"Teleported to {waypointName}!");
        }
    }
    
    [Command("waypoint list", description: "List available waypoints")]
    public static void ListWaypointsCommand(CommandContext ctx) {
        MessageService.Send(ctx.Player, "Available waypoints:");
        
        foreach (var waypoint in globalWaypoints) {
            var pos = waypoint.Value;
            MessageService.Send(ctx.Player, 
                $"  {waypoint.Key}: ({pos.x:F0}, {pos.y:F0}, {pos.z:F0})");
        }
    }
    
    [Command("waypoint add", description: "Add a waypoint", adminOnly: true)]
    public static void AddWaypointCommand(CommandContext ctx, string name) {
        globalWaypoints[name.ToLower()] = ctx.Player.Position;
        MessageService.SendSuccess(ctx.Player, $"Waypoint '{name}' added!");
    }
    
    [Command("waypoint remove", description: "Remove a waypoint", adminOnly: true)]
    public static void RemoveWaypointCommand(CommandContext ctx, string name) {
        if (globalWaypoints.Remove(name.ToLower())) {
            MessageService.SendSuccess(ctx.Player, $"Waypoint '{name}' removed!");
        } else {
            MessageService.SendError(ctx.Player, "Waypoint not found!");
        }
    }
}
```

### Example 6: Random Teleport System

```csharp
public class RandomTeleportSystem {
    [Command("rtp", description: "Random teleport within bounds")]
    public static void RandomTeleportCommand(CommandContext ctx, float range = 100f) {
        var currentPos = ctx.Player.Position;
        
        // Generate random position within range
        var randomPos = new float3(
            currentPos.x + UnityEngine.Random.Range(-range, range),
            0,
            currentPos.z + UnityEngine.Random.Range(-range, range)
        );
        
        if (TeleportService.TeleportToPosition(ctx.Player.CharacterEntity, randomPos)) {
            var distance = TeleportService.GetDistance(
                currentPos,
                randomPos
            );
            
            MessageService.SendSuccess(ctx.Player, 
                $"Randomly teleported {distance:F1} meters away!");
        }
    }
    
    [Command("scatter", description: "Scatter all players", adminOnly: true)]
    public static void ScatterPlayersCommand(CommandContext ctx, float range = 50f) {
        var center = ctx.Player.Position;
        
        foreach (var player in PlayerService.GetAllConnected()) {
            var randomAngle = UnityEngine.Random.Range(0f, 360f) * (math.PI / 180f);
            var randomDistance = UnityEngine.Random.Range(10f, range);
            
            var scatterPos = center + new float3(
                math.cos(randomAngle) * randomDistance,
                0,
                math.sin(randomAngle) * randomDistance
            );
            
            TeleportService.TeleportToPosition(player.CharacterEntity, scatterPos);
            MessageService.Send(player, "You were scattered!");
        }
        
        MessageService.SendSuccess(ctx.Player, "All players scattered!");
    }
}
```

---

## Best Practices

### 1. Always Check Teleport Success

```csharp
// Good - Check return value
if (TeleportService.Teleport(entity, position)) {
    MessageService.SendSuccess(player, "Teleport successful!");
} else {
    MessageService.SendError(player, "Teleport failed!");
}

// Good - Handle failure
var success = TeleportService.TeleportToEntity(player.CharacterEntity, target);
if (!success) {
    Log.Warning($"Failed to teleport {player.Name} to target");
}
```

### 2. Validate Entities Before Teleporting

```csharp
// Good - Check entity exists
if (entity.Exists()) {
    TeleportService.TeleportToPosition(entity, position);
}

// Good - Check player is online
if (PlayerService.TryGetByName(playerName, out var player)) {
    if (player.IsConnected) {
        TeleportService.Teleport(player.CharacterEntity, position);
    }
}
```

### 3. Use Appropriate Method for Context

```csharp
// Good - Use Teleport for simple position teleport
TeleportService.Teleport(entity, targetPos);

// Good - Use TeleportToEntity when teleporting to another entity
TeleportService.TeleportToEntity(player.CharacterEntity, boss);

// Good - Use player-specific methods for player teleportation
TeleportService.TeleportPlayerToPlayer("Player1", "Player2");
```

### 4. Handle Distance Checks

```csharp
// Good - Check distance before allowing teleport
var distance = TeleportService.GetDistance(player.CharacterEntity, waypoint);

if (distance > 100f) {
    MessageService.SendError(player, "Waypoint is too far away!");
    return;
}

TeleportService.TeleportToPosition(player.CharacterEntity, waypointPos);
```

### 5. Use Offsets for Safety

```csharp
// Good - Add offset to avoid overlapping entities
var offset = new float3(2, 0, 2);
TeleportService.TeleportToEntity(player.CharacterEntity, target, offset);

// Good - Random offset for multiple teleports
var randomOffset = new float3(
    UnityEngine.Random.Range(-3f, 3f),
    0,
    UnityEngine.Random.Range(-3f, 3f)
);
TeleportService.TeleportToEntity(minion, player.CharacterEntity, randomOffset);
```

### 6. Implement Cooldowns for Player Teleports

```csharp
// Good - Track last teleport time
private static Dictionary<ulong, DateTime> teleportCooldowns = new();
private const int COOLDOWN_SECONDS = 60;

public static bool CanTeleport(PlayerData player) {
    if (!teleportCooldowns.ContainsKey(player.PlatformId)) return true;
    
    var timeSince = DateTime.Now - teleportCooldowns[player.PlatformId];
    return timeSince.TotalSeconds >= COOLDOWN_SECONDS;
}

public static void TeleportWithCooldown(PlayerData player, float3 position) {
    if (!CanTeleport(player)) {
        MessageService.SendWarning(player, "Teleport on cooldown!");
        return;
    }
    
    if (TeleportService.TeleportToPosition(player.CharacterEntity, position)) {
        teleportCooldowns[player.PlatformId] = DateTime.Now;
    }
}
```

---

## Technical Notes

### Position Components Updated
The service updates the following components when teleporting:
- `Translation` - Current world position
- `LastTranslation` - Previous position (for interpolation)
- `LocalTransform` - Local transform matrix
- `SpawnTransform` - Spawn position reference
- `Height` - Height tracking component

### Teleport Buff
- Uses PrefabGUID: 150521246 (game's built-in teleport buff)
- Buff duration: 0 (instant)
- Sets `TeleportBuff.EndPosition` to target location

### Entity Validation
- Checks Entity.Null before operations
- Validates entity exists in EntityManager
- Returns false for invalid entities rather than throwing

### Distance Calculations
- Uses 3D Euclidean distance
- Returns -1 for invalid entities
- Calculation: `sqrt((x2-x1)² + (y2-y1)² + (z2-z1)²)`

### Y-Coordinate (Vertical Position)
- Typically 0 for ground level
- Game handles height adjustment automatically
- Can use actual Y values for elevated positions

---

## Related Services
- [PlayerService](PlayerService.md) - For player data and entity retrieval
- [SpawnerService](SpawnerService.md) - For spawning entities at positions
- [BuffService](BuffService.md) - For buff application (used internally)
- [MessageService](MessageService.md) - For player notifications

## Notes
- All teleport methods return boolean for success/failure
- Entity validation is automatic in most methods
- Position components are updated comprehensively
- Rotation can be preserved or reset during teleportation
- Uses game's built-in teleport buff for safe teleportation
- Distance calculations handle invalid entities gracefully
