# Event Priorities

Control the execution order of event handlers using the `EventPriority` attribute.

## Overview

When multiple handlers are subscribed to the same event, they execute in order based on their priority. Handlers with **higher** priority values run **before** handlers with lower values.

```csharp
[EventPriority(EventPriority.First)]    // Runs first (999)
private void HighPriorityHandler() { }

[EventPriority(EventPriority.Normal)]   // Runs second (0)
private void NormalPriorityHandler() { }

[EventPriority(EventPriority.Last)]     // Runs last (-999)
private void LowPriorityHandler() { }
```

## Priority Constants

```csharp
public static class EventPriority {
  public const int First = 999;              // Highest priority
  public const int VeryHigh = 300;
  public const int High = 200;
  public const int HigherThanNormal = 100;
  public const int Normal = 0;               // Default priority
  public const int LowerThanNormal = -100;
  public const int Low = -200;
  public const int VeryLow = -300;
  public const int Last = -999;              // Lowest priority
}
```

## Usage

### Basic Priority

```csharp
using ScarletCore.Events;

public class MyEventHandlers {
  public void Initialize() {
    EventManager.On(PlayerEvents.PlayerJoined, OnPlayerJoinedFirst);
    EventManager.On(PlayerEvents.PlayerJoined, OnPlayerJoinedNormal);
    EventManager.On(PlayerEvents.PlayerJoined, OnPlayerJoinedLast);
  }
  
  [EventPriority(EventPriority.First)]
  private void OnPlayerJoinedFirst(PlayerData player) {
    Log.Info("1. First - runs before all others");
  }
  
  [EventPriority(EventPriority.Normal)]
  private void OnPlayerJoinedNormal(PlayerData player) {
    Log.Info("2. Normal - default priority");
  }
  
  [EventPriority(EventPriority.Last)]
  private void OnPlayerJoinedLast(PlayerData player) {
    Log.Info("3. Last - runs after all others");
  }
}
```

### Custom Priority Values

You can use any integer value for fine-grained control:

```csharp
[EventPriority(500)]  // Between First (999) and VeryHigh (300)
private void CriticalHandler(PlayerData player) {
  // Very high priority
}

[EventPriority(-50)]  // Between Normal (0) and LowerThanNormal (-100)
private void CustomPriorityHandler(PlayerData player) {
  // Slightly lower than normal
}

[EventPriority(1000)] // Even higher than First
private void UltraHighPriorityHandler(PlayerData player) {
  // Highest possible priority
}
```

## Common Patterns

### Validation → Processing → Logging

```csharp
public class DataPipeline {
  public void Initialize() {
    EventManager.On(PrefixEvents.OnDealDamage, ValidateDamage);
    EventManager.On(PrefixEvents.OnDealDamage, ProcessDamage);
    EventManager.On(PrefixEvents.OnDealDamage, LogDamage);
  }
  
  // Step 1: Validate (highest priority)
  [EventPriority(EventPriority.VeryHigh)]
  private void ValidateDamage(NativeArray<Entity> entities) {
    Log.Info("1. Validating damage data");
    // Check if damage is valid
    // Prevent invalid damage
  }
  
  // Step 2: Process (normal priority)
  [EventPriority(EventPriority.Normal)]
  private void ProcessDamage(NativeArray<Entity> entities) {
    Log.Info("2. Processing damage");
    // Apply damage modifications
    // Calculate final damage
  }
  
  // Step 3: Log (lowest priority)
  [EventPriority(EventPriority.Last)]
  private void LogDamage(NativeArray<Entity> entities) {
    Log.Info("3. Logging damage statistics");
    // Record to database
    // Update analytics
  }
}
```

### Override System

```csharp
public class OverrideSystem {
  // Core system runs first
  [EventPriority(EventPriority.VeryHigh)]
  private void CoreHandler(PlayerData player) {
    Log.Info("Core: Processing player");
  }
  
  // Mod overrides run second
  [EventPriority(EventPriority.High)]
  private void ModOverrideHandler(PlayerData player) {
    Log.Info("Mod: Applying custom logic");
  }
  
  // UI updates run last
  [EventPriority(EventPriority.Last)]
  private void UIUpdateHandler(PlayerData player) {
    Log.Info("UI: Updating display");
  }
}
```

### Dependency Chain

```csharp
public class DependencyChain {
  // Must run first - other handlers depend on this
  [EventPriority(EventPriority.First)]
  private void InitializeData(PlayerData player) {
    // Set up data structures
    // Other handlers need this to exist
    SharedDatabase.Set(player.PlatformId, new PlayerDataModel());
  }
  
  // Depends on InitializeData
  [EventPriority(EventPriority.Normal)]
  private void ProcessData(PlayerData player) {
    if (!SharedDatabase.Has(player.PlatformId)) {
      // Safe to process
    }
  }
  
  // Cleanup after everything
  [EventPriority(EventPriority.Last)]
  private void CleanupData(PlayerData player) {
    // Clean up temporary data
    SharedDatabase.Delete(player.PlatformId);
  }
}
```

## Multi-Mod Coordination

When multiple mods subscribe to the same event, priorities ensure correct execution order.

### Example: Combat System

```csharp
// Mod A: Core Combat (highest priority)
public class CoreCombatMod {
  [EventPriority(EventPriority.VeryHigh)]
  private void OnDamage(NativeArray<Entity> entities) {
    Log.Info("[CoreCombat] Calculating base damage");
    // Base damage calculation
  }
}

// Mod B: Armor System (high priority)
public class ArmorMod {
  [EventPriority(EventPriority.High)]
  private void OnDamage(NativeArray<Entity> entities) {
    Log.Info("[Armor] Applying armor reduction");
    // Reduce damage based on armor
  }
}

// Mod C: Buff System (normal priority)
public class BuffMod {
  [EventPriority(EventPriority.Normal)]
  private void OnDamage(NativeArray<Entity> entities) {
    Log.Info("[Buffs] Applying buff modifiers");
    // Apply buff/debuff effects
  }
}

// Mod D: Damage Display (lowest priority)
public class DamageDisplayMod {
  [EventPriority(EventPriority.Last)]
  private void OnDamage(NativeArray<Entity> entities) {
    Log.Info("[Display] Showing damage numbers");
    // Display final damage to players
  }
}

// Execution order:
// 1. CoreCombat (VeryHigh: 300)
// 2. Armor (High: 200)
// 3. Buffs (Normal: 0)
// 4. Display (Last: -999)
```

## Priority Rules and Guidelines

### Rule 1: Higher Values = Earlier Execution

```csharp
Priority 1000 → Runs first
Priority 100  → Runs second
Priority 0    → Runs third
Priority -100 → Runs fourth
Priority -999 → Runs last
```

### Rule 2: Same Priority = Registration Order

If two handlers have the same priority, they execute in the order they were registered:

```csharp
EventManager.On(PlayerEvents.PlayerJoined, HandlerA);  // Runs first
EventManager.On(PlayerEvents.PlayerJoined, HandlerB);  // Runs second

[EventPriority(EventPriority.Normal)]  // Both have priority 0
private void HandlerA(PlayerData player) { }

[EventPriority(EventPriority.Normal)]  // Both have priority 0
private void HandlerB(PlayerData player) { }
```

### Rule 3: No Priority = Normal (0)

If you don't specify a priority, the handler defaults to `EventPriority.Normal` (0):

```csharp
// No attribute = Normal priority (0)
private void DefaultPriorityHandler(PlayerData player) {
  // Runs with priority 0
}

[EventPriority(EventPriority.Normal)]
private void ExplicitNormalHandler(PlayerData player) {
  // Also runs with priority 0
}
```

## Best Practices

### 1. Use Semantic Constants

```csharp
// ✅ Good: Use named constants
[EventPriority(EventPriority.VeryHigh)]
[EventPriority(EventPriority.Normal)]
[EventPriority(EventPriority.Last)]

// ⚠️ OK: Custom values when needed
[EventPriority(500)]   // Between First and VeryHigh

// ❌ Avoid: Magic numbers without reason
[EventPriority(42)]    // What does 42 mean?
```

### 2. Document Priority Choices

```csharp
/// <summary>
/// Validates player data before any processing.
/// Runs first to prevent invalid data from propagating.
/// </summary>
[EventPriority(EventPriority.First)]
private void ValidatePlayerData(PlayerData player) {
  // Validation logic
}

/// <summary>
/// Updates UI after all data processing is complete.
/// Runs last to display final state.
/// </summary>
[EventPriority(EventPriority.Last)]
private void UpdatePlayerUI(PlayerData player) {
  // UI update logic
}
```

### 3. Reserve First/Last for Critical Operations

```csharp
// ✅ Appropriate use of First
[EventPriority(EventPriority.First)]
private void ValidateGameState() {
  // Critical validation that must run before everything
}

// ✅ Appropriate use of Last
[EventPriority(EventPriority.Last)]
private void CleanupAndLog() {
  // Cleanup after all processing
}

// ❌ Overuse of extreme priorities
[EventPriority(EventPriority.First)]
private void JustANormalHandler() {
  // This doesn't need to be first
}
```

### 4. Consider Dependencies

```csharp
// Data must be initialized first
[EventPriority(EventPriority.VeryHigh)]
private void InitializePlayerCache(PlayerData player) {
  _cache[player.SteamID] = new PlayerCache();
}

// Use the cache (depends on InitializePlayerCache)
[EventPriority(EventPriority.Normal)]
private void UpdatePlayerCache(PlayerData player) {
  var cache = _cache[player.SteamID];  // Safe to use
  // Update cache
}
```

### 5. Avoid Priority Wars

```csharp
// ❌ Bad: Priority inflation
[EventPriority(10000)]  // "I must run first!"
private void MyHandler1() { }

[EventPriority(20000)]  // "No, I must run first!"
private void MyHandler2() { }

// ✅ Good: Use standard ranges
[EventPriority(EventPriority.VeryHigh)]
private void MyHandler1() { }

[EventPriority(EventPriority.High)]
private void MyHandler2() { }
```

## Debugging Priorities

### Check Handler Order

```csharp
public class PriorityDebugger {
  public void TestPriorities() {
    // Subscribe handlers with different priorities
    EventManager.On(PlayerEvents.PlayerJoined, Handler1);
    EventManager.On(PlayerEvents.PlayerJoined, Handler2);
    EventManager.On(PlayerEvents.PlayerJoined, Handler3);
    
    // Check subscriber count
    int count = EventManager.GetSubscriberCount(PlayerEvents.PlayerJoined);
    Log.Info($"Total handlers: {count}");
  }
  
  [EventPriority(EventPriority.First)]
  private void Handler1(PlayerData player) {
    Log.Info($"[{DateTime.Now:HH:mm:ss.fff}] Handler1 (First)");
  }
  
  [EventPriority(EventPriority.Normal)]
  private void Handler2(PlayerData player) {
    Log.Info($"[{DateTime.Now:HH:mm:ss.fff}] Handler2 (Normal)");
  }
  
  [EventPriority(EventPriority.Last)]
  private void Handler3(PlayerData player) {
    Log.Info($"[{DateTime.Now:HH:mm:ss.fff}] Handler3 (Last)");
  }
}
```

## Summary

- **Higher priority values execute first**
- Use `[EventPriority(value)]` attribute on handler methods
- Standard constants: `First` (999), `Normal` (0), `Last` (-999)
- Custom values allowed for fine-grained control
- Same priority = registration order
- No priority = Normal (0)
- Use priorities to control execution flow and dependencies
- Document why you chose specific priority values
- Avoid extreme priorities unless necessary
