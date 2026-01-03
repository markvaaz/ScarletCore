# GameSystems

The `GameSystems` class provides a centralized static access point for core V Rising game systems and managers. It caches system references for performance and convenience, making it easy to access game functionality throughout your mod.

## Namespace
```csharp
using ScarletCore.Systems;
```

## Overview

The `GameSystems` provides:
- Centralized access to V Rising core systems
- Cached references for optimal performance
- Automatic initialization on server startup
- Type-safe access to game managers
- Server world and time access

## Table of Contents

- [Initialization](#initialization)
  - [Initialized](#initialized)
  - [OnInitialize](#oninitialize)
- [Core Systems](#core-systems)
  - [Server](#server)
  - [ServerTime](#servertime)
  - [EntityManager](#entitymanager)
  - [ServerGameManager](#servergamemanager)
- [Game Systems](#game-systems)
  - [ServerBootstrapSystem](#serverbootstrapsystem)
  - [AdminAuthSystem](#adminauthsystem)
  - [PrefabCollectionSystem](#prefabcollectionsystem)
  - [KickBanSystem](#kickbansystem)
  - [UnitSpawnerUpdateSystem](#unitspawnerupdatesystem)
  - [EntityCommandBufferSystem](#entitycommandbuffersystem)
  - [DebugEventsSystem](#debugeventssystem)
  - [TriggerPersistenceSaveSystem](#triggerpersistencesavesystem)
  - [EndSimulationEntityCommandBufferSystem](#endsimulationentitycommandbuffersystem)
  - [InstantiateMapIconsSystem_Spawn](#instantiatemapIconssystem_spawn)
  - [ServerScriptMapper](#serverscriptmapper)
  - [NetworkIdSystem](#networkidsystem)
- [Complete Examples](#complete-examples)
- [Best Practices](#best-practices)
- [Technical Notes](#technical-notes)

## Initialization

### Initialized
```csharp
public static bool Initialized { get; }
```

Gets a value indicating whether the game systems have been initialized.

**Returns:**
- `bool`: True if systems are initialized and ready to use

**Example:**
```csharp
if (GameSystems.Initialized) {
    Log.Message("Game systems are ready!");
    var entityManager = GameSystems.EntityManager;
} else {
    Log.Warning("Game systems not yet initialized!");
}

// Wait for initialization
public void InitializeMyMod() {
    if (!GameSystems.Initialized) {
        GameSystems.OnInitialize(() => {
            // Systems are now ready
            SetupMod();
        });
    } else {
        SetupMod();
    }
}
```

**Notes:**
- Automatically initialized by ScarletCore on server startup
- All system properties throw exception if accessed before initialization
- Check this property before accessing systems in early mod initialization

---

### OnInitialize
```csharp
public static void OnInitialize(Action action)
```

Registers an action to be called when the game systems are initialized, or invokes it immediately if already initialized.

**Parameters:**
- `action` (Action): The action to invoke on initialization

**Example:**
```csharp
// Register initialization callback
GameSystems.OnInitialize(() => {
    Log.Message("Systems initialized! Setting up mod...");
    InitializeMyMod();
});

// Multiple registrations
GameSystems.OnInitialize(() => {
    LoadConfiguration();
});

GameSystems.OnInitialize(() => {
    RegisterCommands();
});

// Safe initialization pattern
public class MyMod {
    public static void Initialize() {
        GameSystems.OnInitialize(() => {
            // Safe to access all systems here
            var entityManager = GameSystems.EntityManager;
            SetupEventHandlers();
            LoadData();
        });
    }
}
```

**Notes:**
- If already initialized, action is invoked immediately
- If not initialized, action is registered with EventManager
- Safe to call multiple times with different actions
- Ideal for mod initialization code

---

## Core Systems

### Server
```csharp
public static World Server { get; }
```

Provides access to the server world system instance.

**Returns:**
- `World`: The server world instance

**Example:**
```csharp
// Access server world
var serverWorld = GameSystems.Server;
Log.Message($"Server world: {serverWorld.Name}");

// Get systems from world
var customSystem = GameSystems.Server.GetExistingSystemManaged<MyCustomSystem>();

// Access world time
var worldTime = GameSystems.Server.Time;
Log.Message($"Delta time: {worldTime.DeltaTime}");
```

**Notes:**
- Throws exception if accessed before initialization
- Main entry point for Unity ECS World operations
- Used internally by ScarletCore services

---

### ServerTime
```csharp
public static double ServerTime { get; }
```

Provides access to the current server elapsed time.

**Returns:**
- `double`: Server elapsed time in seconds

**Example:**
```csharp
// Get current server time
var currentTime = GameSystems.ServerTime;
Log.Message($"Server time: {currentTime:F2} seconds");

// Track time-based events
private double lastEventTime = 0;

public void CheckEvent() {
    var elapsed = GameSystems.ServerTime - lastEventTime;
    
    if (elapsed >= 60.0) {
        TriggerEvent();
        lastEventTime = GameSystems.ServerTime;
    }
}

// Timestamp events
public void LogEvent(string message) {
    Log.Message($"[{GameSystems.ServerTime:F2}] {message}");
}
```

**Notes:**
- Returns time since server start
- More precise than DateTime for game logic
- Used for timing and scheduling

---

### EntityManager
```csharp
public static EntityManager EntityManager { get; }
```

Provides access to the entity manager system instance.

**Returns:**
- `EntityManager`: The Unity ECS EntityManager

**Example:**
```csharp
// Access entity manager
var entityManager = GameSystems.EntityManager;

// Check entity existence
if (entityManager.Exists(entity)) {
    Log.Message("Entity exists!");
}

// Get component data
if (entityManager.HasComponent<Health>(entity)) {
    var health = entityManager.GetComponentData<Health>(entity);
    Log.Message($"Health: {health.Value}/{health.MaxHealth._Value}");
}

// Set component data
var newHealth = entityManager.GetComponentData<Health>(entity);
newHealth.Value = newHealth.MaxHealth._Value;
entityManager.SetComponentData(entity, newHealth);

// Query entities
var query = entityManager.CreateEntityQuery(
    ComponentType.ReadOnly<PlayerCharacter>()
);
var playerEntities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
```

**Notes:**
- Central access point for all entity operations
- Used by most ScarletCore services internally
- Direct ECS access for advanced operations

---

### ServerGameManager
```csharp
public static ServerGameManager ServerGameManager { get; }
```

Provides access to the server game manager system instance.

**Returns:**
- `ServerGameManager`: The game's server manager

**Example:**
```csharp
// Access server game manager
var serverManager = GameSystems.ServerGameManager;

// Get game settings
var settings = serverManager.Settings;
Log.Message($"Server name: {settings.Name}");

// Instantiate entity
var entity = serverManager.InstantiateEntityImmediate(owner, prefabGUID);

// Check if entities are allies
bool isAlly = serverManager.IsAllies(entity1, entity2);

// Get server info
Log.Message($"Game mode: {serverManager.Settings.GameModeType}");
```

**Notes:**
- Core game manager for server-side operations
- Used for entity instantiation
- Provides game settings and configuration

---

## Game Systems

### ServerBootstrapSystem
```csharp
public static ServerBootstrapSystem ServerBootstrapSystem { get; }
```

Provides access to the server bootstrap system instance.

**Returns:**
- `ServerBootstrapSystem`: The bootstrap system

**Example:**
```csharp
var bootstrap = GameSystems.ServerBootstrapSystem;
// Access bootstrap-specific functionality
```

---

### AdminAuthSystem
```csharp
public static AdminAuthSystem AdminAuthSystem { get; }
```

Provides access to the admin authentication system instance.

**Returns:**
- `AdminAuthSystem`: The admin auth system

**Example:**
```csharp
var adminAuth = GameSystems.AdminAuthSystem;

// Check admin status
bool isAdmin = adminAuth.IsAdmin(userEntity);

// Get admin level
var adminLevel = adminAuth.GetAdminLevel(userEntity);
Log.Message($"Admin level: {adminLevel}");
```

**Notes:**
- Used for admin permission checks
- Provides admin level information
- Used internally by AdminService

---

### PrefabCollectionSystem
```csharp
public static PrefabCollectionSystem PrefabCollectionSystem { get; }
```

Provides access to the prefab collection system instance.

**Returns:**
- `PrefabCollectionSystem`: The prefab collection system

**Example:**
```csharp
var prefabSystem = GameSystems.PrefabCollectionSystem;

// Get prefab entity from GUID
var prefabGUID = new PrefabGUID(1234567890);
if (prefabSystem._PrefabGuidToEntityMap.TryGetValue(prefabGUID, out var prefabEntity)) {
    Log.Message($"Found prefab: {prefabEntity}");
    
    // Instantiate prefab
    var instance = GameSystems.EntityManager.Instantiate(prefabEntity);
}

// Get name collection
var nameCollection = prefabSystem.PrefabLookupMap;
```

**Notes:**
- Maps PrefabGUIDs to entity prefabs
- Essential for spawning and item operations
- Used by SpawnerService internally

---

### KickBanSystem
```csharp
public static KickBanSystem_Server KickBanSystem { get; }
```

Provides access to the kick/ban system instance.

**Returns:**
- `KickBanSystem_Server`: The kick/ban system

**Example:**
```csharp
var kickBanSystem = GameSystems.KickBanSystem;

// Kick player
kickBanSystem.KickPlayer(userEntity, "Kicked for rule violation");

// Ban player
kickBanSystem.BanPlayer(userEntity, "Banned for cheating", duration);

// Check if banned
bool isBanned = kickBanSystem.IsBanned(platformId);
```

**Notes:**
- Used for player moderation
- Handles kicks and bans
- Used internally by KickBanService

---

### UnitSpawnerUpdateSystem
```csharp
public static UnitSpawnerUpdateSystem UnitSpawnerUpdateSystem { get; }
```

Provides access to the unit spawner update system instance.

**Returns:**
- `UnitSpawnerUpdateSystem`: The spawner system

**Example:**
```csharp
var spawnerSystem = GameSystems.UnitSpawnerUpdateSystem;

// Spawn unit
spawnerSystem.SpawnUnit(
    spawnerEntity,
    prefabGUID,
    position,
    count: 1,
    minRange: 0f,
    maxRange: 0f,
    duration: 0f
);
```

**Notes:**
- Core spawning system
- Used by SpawnerService internally
- Handles unit spawn mechanics

---

### EntityCommandBufferSystem
```csharp
public static EntityCommandBufferSystem EntityCommandBufferSystem { get; }
```

Provides access to the entity command buffer system instance.

**Returns:**
- `EntityCommandBufferSystem`: The command buffer system

**Example:**
```csharp
var commandBufferSystem = GameSystems.EntityCommandBufferSystem;

// Create command buffer
var commandBuffer = commandBufferSystem.CreateCommandBuffer();

// Queue entity operations
commandBuffer.AddComponent<MyComponent>(entity);
commandBuffer.SetComponent(entity, new MyComponent { Value = 100 });

// Playback happens automatically at system execution
```

**Notes:**
- Used for deferred entity operations
- Important for thread-safe entity modifications
- Advanced ECS usage

---

### DebugEventsSystem
```csharp
public static DebugEventsSystem DebugEventsSystem { get; }
```

Provides access to the debug events system instance.

**Returns:**
- `DebugEventsSystem`: The debug events system

**Example:**
```csharp
var debugEvents = GameSystems.DebugEventsSystem;
// Access debug event functionality
```

---

### TriggerPersistenceSaveSystem
```csharp
public static TriggerPersistenceSaveSystem TriggerPersistenceSaveSystem { get; }
```

Provides access to the trigger persistence save system instance.

**Returns:**
- `TriggerPersistenceSaveSystem`: The persistence save system

**Example:**
```csharp
var saveSystem = GameSystems.TriggerPersistenceSaveSystem;

// Trigger manual save
saveSystem.TriggerSave();
```

**Notes:**
- Handles game save operations
- Can trigger manual saves
- Used for persistence management

---

### EndSimulationEntityCommandBufferSystem
```csharp
public static EndSimulationEntityCommandBufferSystem EndSimulationEntityCommandBufferSystem { get; }
```

Provides access to the end simulation entity command buffer system instance.

**Returns:**
- `EndSimulationEntityCommandBufferSystem`: The end simulation command buffer system

**Example:**
```csharp
var endSimBuffer = GameSystems.EndSimulationEntityCommandBufferSystem;

// Create command buffer for end of frame
var commandBuffer = endSimBuffer.CreateCommandBuffer();
commandBuffer.DestroyEntity(entity);
```

---

### InstantiateMapIconsSystem_Spawn
```csharp
public static InstantiateMapIconsSystem_Spawn InstantiateMapIconsSystem_Spawn { get; }
```

Provides access to the instantiate map icons system instance.

**Returns:**
- `InstantiateMapIconsSystem_Spawn`: The map icons system

**Example:**
```csharp
var mapIconsSystem = GameSystems.InstantiateMapIconsSystem_Spawn;
// Access map icon spawning functionality
```

---

### ServerScriptMapper
```csharp
public static ServerScriptMapper ServerScriptMapper { get; }
```

Provides access to the server script mapper system instance.

**Returns:**
- `ServerScriptMapper`: The script mapper

**Example:**
```csharp
var scriptMapper = GameSystems.ServerScriptMapper;

// Get singleton
var mySingleton = scriptMapper.GetSingleton<MySingleton>();

// Access game manager
var gameManager = scriptMapper.GetServerGameManager();
```

**Notes:**
- Maps server scripts and singletons
- Used for accessing game singletons
- Important for advanced system access

---

### NetworkIdSystem
```csharp
public static NetworkIdSystem.Singleton NetworkIdSystem { get; }
```

Provides access to the network ID system singleton instance.

**Returns:**
- `NetworkIdSystem.Singleton`: The network ID system singleton

**Example:**
```csharp
var networkIdSystem = GameSystems.NetworkIdSystem;

// Access network ID functionality
// Used for network entity tracking
```

---

## Complete Examples

### Example 1: Custom System Initialization

```csharp
using ScarletCore.Systems;
using ScarletCore.Services;

public class CustomGameSystem {
    private static bool _initialized = false;
    
    public static void Initialize() {
        // Wait for GameSystems to be ready
        GameSystems.OnInitialize(() => {
            if (_initialized) return;
            
            Log.Message("Initializing custom game system...");
            
            // Access game systems safely
            var entityManager = GameSystems.EntityManager;
            var serverManager = GameSystems.ServerGameManager;
            
            // Setup system
            SetupEventHandlers();
            LoadConfiguration();
            RegisterCommands();
            
            _initialized = true;
            Log.Message("Custom game system initialized!");
        });
    }
    
    private static void SetupEventHandlers() {
        // Setup periodic updates with ActionScheduler
        ActionScheduler.Repeating(() => {
            // Safe to use GameSystems here
            UpdateSystem();
        }, 1f);
    }
}
```

### Example 2: Entity Query System

```csharp
public class EntityQuerySystem {
    public static List<Entity> GetAllPlayersInRadius(float3 center, float radius) {
        if (!GameSystems.Initialized) {
            Log.Warning("GameSystems not initialized!");
            return new List<Entity>();
        }
        
        var entityManager = GameSystems.EntityManager;
        var results = new List<Entity>();
        
        // Create query for player characters
        var query = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<PlayerCharacter>(),
            ComponentType.ReadOnly<LocalTransform>()
        );
        
        var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        
        foreach (var entity in entities) {
            var transform = entityManager.GetComponentData<LocalTransform>(entity);
            var distance = math.distance(transform.Position, center);
            
            if (distance <= radius) {
                results.Add(entity);
            }
        }
        
        entities.Dispose();
        return results;
    }
    
    public static void ModifyAllEntitiesWithComponent<T>() where T : struct, IComponentData {
        var entityManager = GameSystems.EntityManager;
        
        var query = entityManager.CreateEntityQuery(ComponentType.ReadWrite<T>());
        var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        
        foreach (var entity in entities) {
            var component = entityManager.GetComponentData<T>(entity);
            // Modify component
            entityManager.SetComponentData(entity, component);
        }
        
        entities.Dispose();
    }
}
```

### Example 3: Server Information System

```csharp
public class ServerInfoSystem {
    public static void DisplayServerInfo() {
        if (!GameSystems.Initialized) {
            Log.Warning("Cannot display server info - systems not initialized");
            return;
        }
        
        var serverManager = GameSystems.ServerGameManager;
        var settings = serverManager.Settings;
        
        Log.Message("=== Server Information ===");
        Log.Message($"Server Name: {settings.Name}");
        Log.Message($"Game Mode: {settings.GameModeType}");
        Log.Message($"Server Time: {GameSystems.ServerTime:F2}s");
        Log.Message($"Max Players: {settings.MaxConnectedUsers}");
        
        var entityManager = GameSystems.EntityManager;
        var playerQuery = entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<PlayerCharacter>()
        );
        var playerCount = playerQuery.CalculateEntityCount();
        
        Log.Message($"Connected Players: {playerCount}");
        Log.Message($"Total Entities: {entityManager.EntityCount}");
    }
    
    [Command("serverinfo", description: "Display server information", adminOnly: true)]
    public static void ServerInfoCommand(CommandContext ctx) {
        DisplayServerInfo();
        MessageService.SendSuccess(ctx.Player, "Server info logged to console");
    }
}
```

### Example 4: Custom Spawn System

```csharp
public class CustomSpawnSystem {
    public static Entity SpawnCustomEntity(PrefabGUID prefabGUID, float3 position) {
        var prefabSystem = GameSystems.PrefabCollectionSystem;
        var entityManager = GameSystems.EntityManager;
        
        // Get prefab entity
        if (!prefabSystem._PrefabGuidToEntityMap.TryGetValue(prefabGUID, out var prefabEntity)) {
            Log.Error($"Prefab not found: {prefabGUID.GuidHash}");
            return Entity.Null;
        }
        
        // Instantiate
        var entity = entityManager.Instantiate(prefabEntity);
        
        // Set position
        if (entityManager.HasComponent<LocalTransform>(entity)) {
            var transform = entityManager.GetComponentData<LocalTransform>(entity);
            transform.Position = position;
            entityManager.SetComponentData(entity, transform);
        }
        
        // Set translation
        if (entityManager.HasComponent<Translation>(entity)) {
            var translation = entityManager.GetComponentData<Translation>(entity);
            translation.Value = position;
            entityManager.SetComponentData(entity, translation);
        }
        
        Log.Message($"Spawned entity {entity.Index} at {position}");
        return entity;
    }
    
    public static void SpawnWave(float3 center, int count) {
        var spawnerSystem = GameSystems.UnitSpawnerUpdateSystem;
        var enemyGUID = new PrefabGUID(-1905691330);
        
        spawnerSystem.SpawnUnit(
            Entity.Null,
            enemyGUID,
            center,
            count,
            minRange: 5f,
            maxRange: 15f,
            duration: 0f
        );
        
        MessageService.SendAll($"Spawned {count} enemies!");
    }
}
```

### Example 5: Save System Integration

```csharp
public class CustomSaveSystem {
    private static Dictionary<string, object> customData = new();
    
    public static void TriggerSave() {
        if (!GameSystems.Initialized) {
            Log.Warning("Cannot save - systems not initialized");
            return;
        }
        
        // Save custom data first
        SaveCustomData();
        
        // Trigger game save
        var saveSystem = GameSystems.TriggerPersistenceSaveSystem;
        saveSystem.TriggerSave();
        
        Log.Message($"Save triggered at {GameSystems.ServerTime:F2}s");
        MessageService.SendAll("Game saved!");
    }
    
    private static void SaveCustomData() {
        // Save player data
        foreach (var player in PlayerService.AllPlayers) {
            customData[$"player_{player.PlatformId}"] = new {
                LastSeen = DateTime.Now,
                PlayTime = GameSystems.ServerTime
            };
        }
        
        Log.Message($"Saved data for {PlayerService.AllPlayers.Count()} players");
    }
    
    [Command("save", description: "Trigger manual save", adminOnly: true)]
    public static void SaveCommand(CommandContext ctx) {
        TriggerSave();
        MessageService.SendSuccess(ctx.Player, "Manual save triggered!");
    }
    
    // Auto-save every 5 minutes
    public static void StartAutoSave() {
        ActionScheduler.Repeating(() => {
            TriggerSave();
        }, 300f);
    }
}
```

### Example 6: Admin System Integration

```csharp
public class CustomAdminSystem {
    public static bool IsPlayerAdmin(PlayerData player) {
        if (!GameSystems.Initialized) return false;
        
        var adminAuth = GameSystems.AdminAuthSystem;
        var userEntity = player.User;
        
        return adminAuth.IsAdmin(userEntity);
    }
    
    public static int GetAdminLevel(PlayerData player) {
        if (!GameSystems.Initialized) return 0;
        
        var adminAuth = GameSystems.AdminAuthSystem;
        return adminAuth.GetAdminLevel(player.User);
    }
    
    [Command("checkadmin", description: "Check admin status")]
    public static void CheckAdminCommand(CommandContext ctx, PlayerData target = null) {
        target = target ?? ctx.Player;
        
        bool isAdmin = IsPlayerAdmin(target);
        int level = GetAdminLevel(target);
        
        if (isAdmin) {
            MessageService.Send(ctx.Player, 
                $"{target.Name} is an admin (Level {level})");
        } else {
            MessageService.Send(ctx.Player, 
                $"{target.Name} is not an admin");
        }
    }
    
    public static void PromoteToAdmin(PlayerData player, int level) {
        if (!GameSystems.Initialized) {
            Log.Error("Cannot promote - systems not initialized");
            return;
        }
        
        // Use game's admin system
        var adminAuth = GameSystems.AdminAuthSystem;
        // Set admin level through admin system
        
        MessageService.Send(player, $"You have been promoted to admin level {level}!");
        Log.Message($"{player.Name} promoted to admin level {level}");
    }
}
```

---

## Best Practices

### 1. Always Check Initialization

```csharp
// Good - Check before access
if (GameSystems.Initialized) {
    var entityManager = GameSystems.EntityManager;
    PerformOperation();
} else {
    Log.Warning("Systems not ready yet");
}

// Better - Use OnInitialize
GameSystems.OnInitialize(() => {
    // Safe to access systems here
    var entityManager = GameSystems.EntityManager;
});

// Avoid - Direct access without check
var entityManager = GameSystems.EntityManager; // May throw exception
```

### 2. Use OnInitialize for Mod Setup

```csharp
// Good - Wait for initialization
public class MyMod {
    public static void Initialize() {
        GameSystems.OnInitialize(() => {
            SetupMod();
            RegisterCommands();
            LoadData();
        });
    }
}

// Avoid - Immediate access
public class MyMod {
    public static void Initialize() {
        var entityManager = GameSystems.EntityManager; // May fail
    }
}
```

### 3. Cache System References When Appropriate

```csharp
// Good - Cache for repeated use
public class MySystem {
    private EntityManager entityManager;
    
    public void Initialize() {
        GameSystems.OnInitialize(() => {
            entityManager = GameSystems.EntityManager;
        });
    }
    
    public void Update() {
        // Use cached reference
        var count = entityManager.EntityCount;
    }
}

// Acceptable - Direct access for rare operations
public void OneTimeOperation() {
    var prefabSystem = GameSystems.PrefabCollectionSystem;
    DoSomething(prefabSystem);
}
```

### 4. Use ServerTime for Game Logic

```csharp
// Good - Use ServerTime
private double lastUpdateTime = 0;

public void Update() {
    var currentTime = GameSystems.ServerTime;
    
    if (currentTime - lastUpdateTime >= 1.0) {
        PerformUpdate();
        lastUpdateTime = currentTime;
    }
}

// Avoid - DateTime for game timing
private DateTime lastUpdate = DateTime.Now; // Less precise
```

### 5. Handle Initialization Errors Gracefully

```csharp
// Good - Graceful handling
public void PerformOperation() {
    try {
        if (!GameSystems.Initialized) {
            Log.Warning("Systems not initialized, deferring operation");
            GameSystems.OnInitialize(() => PerformOperation());
            return;
        }
        
        var entityManager = GameSystems.EntityManager;
        // Perform operation
    } catch (Exception ex) {
        Log.Error($"Operation failed: {ex.Message}");
    }
}
```

---

## Technical Notes

### Initialization Order
- GameSystems.Initialize() is called by ScarletCore on server startup
- Happens during server world initialization
- All systems are cached for performance
- RoleService and PlayerService are initialized automatically

### Performance
- All systems are cached on initialization
- O(1) access time for all properties
- No repeated system lookups
- Efficient reference management

### Thread Safety
- Properties are thread-safe after initialization
- Initialization happens on main thread
- System access should be from main thread
- Entity operations must respect ECS thread safety rules

### Error Handling
- All properties throw exception if accessed before initialization
- NotInitializedException provides clear error message
- OnInitialize safely handles both initialized and pre-initialized states

### System Availability
- All V Rising core systems are exposed
- Some systems may not be used in typical mods
- Advanced systems available for custom implementations
- System references never become null after initialization

---

## Related Systems
- [ActionScheduler](ActionScheduler.md) - For scheduled operations
- [EventManager](../Events/EventManager.md) - For event handling

## Notes
- Automatically initialized by ScarletCore
- Do not call Initialize() manually
- All systems remain valid for server lifetime
- Use OnInitialize for safe mod initialization
- Check Initialized property before critical operations
- ServerTime is more precise than DateTime for game logic
