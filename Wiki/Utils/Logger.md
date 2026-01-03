# Logger (Log)

## Overview

`Log` is a static utility class that provides convenient logging functionality for ScarletCore and other mods. It automatically resolves the appropriate `ManualLogSource` from the calling assembly and supports enhanced debug mode with caller information, colored output, and specialized logging methods for entities and player data.

## Table of Contents

- [Basic Logging Methods](#basic-logging-methods)
- [Debug Mode](#debug-mode)
- [Specialized Logging](#specialized-logging)
- [Log Levels](#log-levels)
- [Examples](#examples)
- [Best Practices](#best-practices)
- [Performance Considerations](#performance-considerations)

---

## Basic Logging Methods

### Debug

```csharp
public static void Debug(params object[] messages)
```

Logs a debug message. Typically used for detailed diagnostic information during development.

**Parameters:**
- `messages`: Variable number of objects to log (joined with spaces)

**Example:**
```csharp
Log.Debug("Player position:", player.Read<LocalToWorld>().Position);
Log.Debug("Processing", enemyCount, "enemies");
```

### Info

```csharp
public static void Info(params object[] messages)
```

Logs an informational message with cyan color formatting. Use for general information and status updates.

**Parameters:**
- `messages`: Variable number of objects to log (joined with spaces)

**Example:**
```csharp
Log.Info("Mod initialized successfully");
Log.Info("Found", playerCount, "players online");
```

### Message

```csharp
public static void Message(params object[] messages)
```

Logs a general message with cyan color formatting. Similar to Info but uses LogMessage level.

**Parameters:**
- `messages`: Variable number of objects to log (joined with spaces)

**Example:**
```csharp
Log.Message("Server started on port 9876");
Log.Message("Event system loaded");
```

### Warning

```csharp
public static void Warning(params object[] messages)
```

Logs a warning message. Use for non-critical issues that should be noted.

**Parameters:**
- `messages`: Variable number of objects to log (joined with spaces)

**Example:**
```csharp
Log.Warning("Player not found:", playerName);
Log.Warning("Configuration file missing, using defaults");
```

### Error

```csharp
public static void Error(params object[] messages)
```

Logs an error message. Use for errors that affect functionality but don't crash the application.

**Parameters:**
- `messages`: Variable number of objects to log (joined with spaces)

**Example:**
```csharp
Log.Error("Failed to load database:", exception.Message);
Log.Error("Invalid entity reference");
```

### Fatal

```csharp
public static void Fatal(params object[] messages)
```

Logs a fatal error message. Use for critical errors that may cause application termination.

**Parameters:**
- `messages`: Variable number of objects to log (joined with spaces)

**Example:**
```csharp
Log.Fatal("Critical system failure:", errorMessage);
Log.Fatal("Unable to initialize core systems");
```

### LogLevel

```csharp
public static void LogLevel(LogLevel level, object message)
```

Logs a message with a specific BepInEx log level.

**Parameters:**
- `level`: BepInEx `LogLevel` enum value
- `message`: The message to log

**Example:**
```csharp
Log.LogLevel(LogLevel.Debug, "Custom debug message");
Log.LogLevel(LogLevel.Info, "Custom info message");
```

---

## Debug Mode

### DebugMode Property

```csharp
public static bool DebugMode { get; set; } = false;
```

When enabled, adds caller information (class name, method name, and line number) to all log messages.

**Features:**
- Automatically includes `[ClassName.MethodName:L123]` prefix
- Shows file line number when available
- Useful for debugging complex issues
- Disabled by default for performance

**Example:**
```csharp
// Enable debug mode
Log.DebugMode = true;

Log.Info("Player spawned");
// Output: [PlayerManager.OnPlayerSpawn:L45] Player spawned

// Disable debug mode
Log.DebugMode = false;

Log.Info("Player spawned");
// Output: Player spawned
```

---

## Specialized Logging

### Components

```csharp
public static void Components(Entity entity)
```

Logs all component types attached to an entity. Useful for debugging entity composition.

**Parameters:**
- `entity`: The entity to inspect

**Example:**
```csharp
var player = PlayerService.GetOnlinePlayers().First().CharacterEntity;
Log.Components(player);

// Output:
// ProjectM.PlayerCharacter
// Unity.Transforms.LocalToWorld
// ProjectM.Health
// ProjectM.Equipment
// ...
```

### Player

```csharp
public static void Player(PlayerData playerData)
```

Logs comprehensive PlayerData information in a formatted layout.

**Parameters:**
- `playerData`: The PlayerData to inspect

**Example:**
```csharp
var player = PlayerService.GetOnlinePlayers().First();
Log.Player(player);

// Output:
// === PlayerData Information ===
//   Name: PlayerName
//   Cached Name: PlayerName
//   Platform ID: 123456789
//   Network ID: 1
//   Is Online: True
//   Is Admin: False
//   Connected Since: 2026-01-03 10:30:00
//   Clan Name: MyClan
//   User Entity: Entity(1:2)
//   Character Entity: Entity(3:4)
// ==============================
```

---

## Log Levels

The logger supports all BepInEx log levels:

| Level | Method | Usage |
|-------|--------|-------|
| `Debug` | `Log.Debug()` | Detailed diagnostic information |
| `Info` | `Log.Info()` | General informational messages |
| `Message` | `Log.Message()` | General messages |
| `Warning` | `Log.Warning()` | Non-critical warnings |
| `Error` | `Log.Error()` | Error conditions |
| `Fatal` | `Log.Fatal()` | Critical failures |

---

## Examples

### Example 1: Basic Logging

```csharp
using ScarletCore.Utils;

public class MyMod {
    public void Initialize() {
        Log.Info("MyMod initializing...");
        
        try {
            // Setup code
            Log.Debug("Loading configuration");
            LoadConfig();
            
            Log.Debug("Registering events");
            RegisterEvents();
            
            Log.Info("MyMod initialized successfully!");
        } catch (Exception ex) {
            Log.Error("Failed to initialize:", ex.Message);
            Log.Fatal("MyMod cannot continue");
        }
    }
}
```

### Example 2: Debug Mode for Troubleshooting

```csharp
// Enable during development
Log.DebugMode = true;

public void OnPlayerDamaged(Entity player, float damage) {
    Log.Debug("Player damaged:", damage);
    // Output: [CombatSystem.OnPlayerDamaged:L123] Player damaged: 50
    
    if (damage > 100) {
        Log.Warning("High damage detected!");
        // Output: [CombatSystem.OnPlayerDamaged:L126] High damage detected!
    }
}

// Disable in production for performance
Log.DebugMode = false;
```

### Example 3: Entity Component Inspection

```csharp
public void InspectEntity(Entity entity) {
    if (!entity.Exists()) {
        Log.Warning("Entity does not exist");
        return;
    }
    
    Log.Info("=== Entity Inspection ===");
    Log.Info("Entity:", entity);
    Log.Components(entity);
    
    if (entity.Has<Health>()) {
        var health = entity.Read<Health>();
        Log.Info($"Health: {health.Value}/{health.MaxHealth}");
    }
}
```

### Example 4: Player Information Logging

```csharp
public void LogPlayerInfo(PlayerData player) {
    // Quick check
    if (player == null) {
        Log.Warning("Player is null");
        return;
    }
    
    // Detailed logging
    Log.Player(player);
    
    // Additional custom info
    if (player.IsAdmin) {
        Log.Info("Player has admin privileges");
    }
}
```

### Example 5: Conditional Logging

```csharp
private const bool VERBOSE_LOGGING = true;

public void ProcessEnemies(NativeArray<Entity> enemies) {
    Log.Info("Processing", enemies.Length, "enemies");
    
    foreach (var enemy in enemies) {
        if (VERBOSE_LOGGING) {
            Log.Debug("Processing enemy:", enemy);
            Log.Components(enemy);
        }
        
        // Process enemy...
    }
    
    Log.Info("Enemy processing complete");
}
```

### Example 6: Error Handling with Context

```csharp
public void SafeLoadDatabase() {
    try {
        Log.Info("Loading database...");
        var data = Database.Load<MyData>();
        Log.Info("Database loaded successfully:", data.Count, "entries");
    } catch (FileNotFoundException ex) {
        Log.Warning("Database file not found, creating new:", ex.Message);
        Database.Create<MyData>();
    } catch (Exception ex) {
        Log.Error("Failed to load database:", ex.Message);
        Log.Debug("Stack trace:", ex.StackTrace);
        throw;
    }
}
```

### Example 7: Multi-Object Logging

```csharp
public void LogCombatEvent(Entity attacker, Entity target, float damage) {
    // Multiple objects in one call
    Log.Info("Combat:", attacker, "->", target, "for", damage, "damage");
    
    // Equivalent to:
    // Log.Info($"Combat: {attacker} -> {target} for {damage} damage");
}
```

### Example 8: Performance Monitoring

```csharp
public void ExpensiveOperation() {
    var sw = System.Diagnostics.Stopwatch.StartNew();
    
    Log.Debug("Starting expensive operation");
    
    // Do work...
    
    sw.Stop();
    
    if (sw.ElapsedMilliseconds > 100) {
        Log.Warning("Operation took", sw.ElapsedMilliseconds, "ms (expected < 100ms)");
    } else {
        Log.Debug("Operation completed in", sw.ElapsedMilliseconds, "ms");
    }
}
```

---

## Best Practices

### 1. Use Appropriate Log Levels

```csharp
// ✅ Good - Appropriate levels
Log.Debug("Loop iteration:", i);           // Development detail
Log.Info("Player connected:", playerName);  // Important status
Log.Warning("Low memory:", memUsage);       // Potential issue
Log.Error("Failed to spawn:", exception);   // Actual problem
Log.Fatal("Core system crashed");           // Critical failure

// ❌ Bad - Wrong levels
Log.Info("Variable value:", x);             // Use Debug
Log.Error("Player not found");              // Use Warning
```

### 2. Enable Debug Mode Only When Needed

```csharp
// ✅ Good - Conditional debug mode
#if DEBUG
    Log.DebugMode = true;
#else
    Log.DebugMode = false;
#endif

// ❌ Bad - Always enabled in production
Log.DebugMode = true; // Performance impact!
```

### 3. Provide Useful Context

```csharp
// ✅ Good - Includes context
Log.Error("Failed to teleport player", playerName, "to", position);
Log.Warning("Invalid configuration value:", key, "=", value);

// ❌ Bad - No context
Log.Error("Teleport failed");
Log.Warning("Invalid value");
```

### 4. Use Specialized Methods When Available

```csharp
// ✅ Good - Use specialized methods
Log.Player(playerData);
Log.Components(entity);

// ❌ Bad - Manual formatting
Log.Info("Player:", playerData.Name, playerData.IsOnline, playerData.IsAdmin);
// ... (lots of manual logging)
```

### 5. Don't Log in Hot Paths

```csharp
// ❌ Bad - Logging every frame
ActionScheduler.OncePerFrame(() => {
    Log.Debug("Frame update");  // Spam!
});

// ✅ Good - Log periodically or conditionally
ActionScheduler.Repeating(() => {
    Log.Debug("Update tick");  // Once per second
}, 1f);

// ✅ Good - Log only when something happens
if (playerHealth < 10) {
    Log.Warning("Player health critical:", playerHealth);
}
```

### 6. Handle Null Values Gracefully

```csharp
// ✅ Good - Check before logging
if (playerData != null) {
    Log.Player(playerData);
} else {
    Log.Warning("PlayerData is null");
}

// ❌ Bad - Potential null reference
Log.Info("Player name:", playerData.Name);  // Crash if null!
```

### 7. Use String Interpolation Carefully

```csharp
// ✅ Good - Simple interpolation
Log.Info($"Player {playerName} dealt {damage} damage");

// ✅ Good - Multiple parameters
Log.Info("Player", playerName, "dealt", damage, "damage");

// ⚠️ Expensive - Complex formatting in hot paths
// Only use when necessary
Log.Debug($"Complex: {string.Join(",", list.Select(x => x.ToString()))}");
```

### 8. Dispose NativeCollections Before Logging

```csharp
// ✅ Good - Dispose after getting count
var entities = EntityLookupService.QueryAll<Enemy>();
var count = entities.Length;
entities.Dispose();
Log.Info("Found", count, "enemies");

// ❌ Bad - Log before dispose (memory leak risk)
var entities = EntityLookupService.QueryAll<Enemy>();
Log.Info("Found", entities.Length, "enemies");
// Forgot to dispose!
```

---

## Performance Considerations

### Debug Mode Overhead

- **DebugMode = false**: Minimal overhead, fast logging
- **DebugMode = true**: Uses StackTrace reflection, slower but provides detailed context
- Stack trace walking can be expensive (1-5ms per call)
- Only enable during development or troubleshooting

### Assembly Resolution Caching

- Logger caches ManualLogSource per assembly
- First call from an assembly uses reflection (slower)
- Subsequent calls use cached instance (fast)
- Cache is thread-safe using ConcurrentDictionary

### String Formatting

```csharp
// Fast - Simple concatenation
Log.Info("Player spawned");

// Fast - Variable arguments (params)
Log.Info("Player", playerName, "spawned");

// Slower - String interpolation (allocates)
Log.Info($"Player {playerName} spawned");

// Slowest - Complex string operations
Log.Info($"Players: {string.Join(", ", players.Select(p => p.Name))}");
```

### Logging Frequency

```csharp
// ❌ Expensive - Every frame
ActionScheduler.OncePerFrame(() => {
    Log.Debug("Update");  // 60+ times per second!
});

// ✅ Better - Periodic
ActionScheduler.Repeating(() => {
    Log.Debug("Update");  // Once per second
}, 1f);

// ✅ Best - Event-driven
EventManager.On(ServerEvents.OnPlayerConnect, (player) => {
    Log.Info("Player connected:", player.Name);
});
```

---

## Technical Details

### Assembly Resolution

The logger automatically resolves the calling assembly's `ManualLogSource`:

1. Walks the stack trace to find the calling assembly
2. Searches for static `ManualLogSource` properties/fields
3. Checks common names: `LogInstance`, `Logger`
4. Caches the result per assembly
5. Falls back to `Plugin.LogInstance` if not found

**Supported patterns:**
```csharp
// These are automatically detected
public static ManualLogSource LogInstance { get; set; }
public static ManualLogSource Logger;
```

### Color Formatting

Info and Message methods use ANSI color codes:
- Cyan color: `\x1b[38;2;222;254;255m`
- Reset: `\x1b[0m`

Works in BepInEx console with color support enabled.

### Caller Information

When `DebugMode = true`, the logger extracts:
- **Class Name**: From `DeclaringType`
- **Method Name**: From `MethodBase`
- **Line Number**: From `StackFrame.GetFileLineNumber()` (requires debug symbols)

Format: `[ClassName.MethodName:L123]`

---

## Common Pitfalls

### 1. Forgetting to Disable Debug Mode

```csharp
// ❌ Left enabled in production
Log.DebugMode = true;

// ✅ Use conditional compilation
#if DEBUG
    Log.DebugMode = true;
#endif
```

### 2. Logging in Tight Loops

```csharp
// ❌ Excessive logging
foreach (var entity in entities) {
    Log.Debug("Processing:", entity);  // Could be thousands!
}

// ✅ Log summary instead
Log.Debug("Processing", entities.Length, "entities");
```

### 3. Not Handling Null in Specialized Methods

```csharp
// ❌ Will log warning if null
Log.Player(playerData);  // "PlayerData is null"

// ✅ Check first if you want different behavior
if (playerData != null) {
    Log.Player(playerData);
}
```

### 4. Complex String Operations in Logs

```csharp
// ❌ Expensive even if log level is disabled
Log.Debug($"Data: {string.Join(", ", ExpensiveOperation())}");

// ✅ Check if logging is needed first (if custom log level filtering)
if (ShouldLogDebug) {
    Log.Debug($"Data: {string.Join(", ", ExpensiveOperation())}");
}
```

---

## Related Utilities

- [RichTextFormatter](RichTextFormatter.md) - Text formatting for in-game messages
- [Symbols](Symbols.md) - Unicode symbols for enhanced logging

---

## Requirements

- **BepInEx.Logging** - Core logging functionality
- **ScarletCore.Systems** - GameSystems access for Components method
- **System.Reflection** - Assembly and stack trace analysis
