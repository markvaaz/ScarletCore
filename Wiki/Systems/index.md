# ScarletCore Systems

## Overview

ScarletCore Systems provide essential infrastructure for timing, scheduling, and accessing V Rising's core game systems. These systems form the foundation upon which services and gameplay features are built, offering controlled access to Unity ECS, server state, and asynchronous operations.

All systems are static classes in the `ScarletCore.Systems` namespace and are initialized during plugin load.

---

## Core Systems

### [GameSystems](GameSystems.md)
Central access point to V Rising's core Unity ECS systems and server infrastructure. Provides unified access to EntityManager, ServerGameManager, and 15+ game systems including Admin, PrefabCollection, Debug, Clan, Trade, and more.

**Key Features:**
- **Server** — World reference for game state
- **EntityManager** — Direct ECS entity operations
- **ServerGameManager** — High-level game management
- **System Properties** — Pre-configured access to AdminAuthSystem, PrefabCollectionSystem, DebugEventsSystem, and other critical systems
- **Initialization Checks** — Safe access with `Initialized` property and `OnInitialize` callback

**Primary Use Cases:**
- Direct entity queries and manipulation
- System-level operations requiring specific game systems
- Initialization sequencing for mods
- Accessing prefab collections and debug tools

---

### [ActionScheduler](ActionScheduler.md)
Flexible action scheduling system for frame-based and time-based delayed execution. Supports one-time, repeating, and chained actions with full lifecycle control including pause, resume, and cancellation.

**Key Features:**
- **Frame-Based Scheduling** — Execute actions on specific frames or frame intervals
- **Time-Based Scheduling** — Delay and repeat actions based on seconds
- **Random Intervals** — Repeating actions with randomized delays
- **Action Control** — Pause, resume, and cancel scheduled actions by ID
- **Execution Tracking** — Monitor execution counts and remaining executions
- **Action Sequences** — Chain multiple actions with delays (ActionSequence API)

**Scheduling Types:**
- `OncePerFrame` — Execute every frame (use sparingly)
- `NextFrame` — Execute on next frame
- `Delayed` — Execute after time delay
- `DelayedFrames` — Execute after frame count
- `Repeating` — Repeat with time interval
- `RepeatingFrames` — Repeat with frame interval
- `RepeatingRandom` — Repeat with randomized intervals

**Primary Use Cases:**
- Delayed ability cooldowns and buff durations
- Periodic system updates and maintenance tasks
- Animation and effect timing
- Tutorial sequences and staged events
- Countdown timers and scheduled announcements

---

## System Architecture

### Initialization Flow

1. **Plugin Load** — ScarletCore plugin initializes
2. **GameSystems Setup** — Core systems detected and cached
3. **ActionScheduler Start** — Frame and time-based update loops begin
4. **Mod Initialization** — `GameSystems.OnInitialize` callbacks execute
5. **Ready State** — Systems available for use

### Update Cycles

**ActionScheduler** operates on two update loops:

- **Frame Update** — Runs every Unity frame, processes frame-based actions
- **Time Update** — Runs based on delta time, processes time-based actions

Both loops are managed internally and require no manual updates from mods.

---

## Common Patterns

### Safe System Access
Always check `GameSystems.Initialized` before accessing systems during early initialization phases. Most operations are safe during event callbacks and command execution.

### Action Lifecycle Management
Store `ActionId` return values when you need to cancel, pause, or query actions later. Actions without stored IDs run to completion without external control.

### Avoid Frame-Based Spam
Minimize use of `OncePerFrame` — it executes 60+ times per second. Prefer time-based repeating actions with reasonable intervals (≥0.1s for frequent updates).

### Action Cleanup
ActionScheduler automatically cleans up completed actions. Manual cancellation is only needed when stopping actions early.

---

## System Dependencies

### GameSystems Dependencies
- **BepInEx.Unity.IL2CPP** — IL2CPP plugin framework
- **Unity.Entities** — ECS foundation
- **ProjectM** — V Rising game assemblies
- **Bloodstone** — Additional server utilities

### ActionScheduler Dependencies
- **Unity Update Loop** — Frame timing
- **System.Diagnostics** — Stopwatch for time tracking
- **Thread Safety** — Lock-based synchronization for action management

---

## Best Practices

**Initialize Early** — Use `GameSystems.OnInitialize` for mod setup requiring game systems access.

**Cache System References** — Store frequently used system references (e.g., `EntityManager`) in local variables when used in tight loops.

**Use Appropriate Intervals** — Balance responsiveness with performance; 0.5-1.0 second intervals work well for most periodic tasks.

**Manage Action IDs** — Store IDs only when you need control; one-shot actions don't require ID management.

**Prefer Time Over Frames** — Time-based scheduling (`Delayed`, `Repeating`) is more predictable than frame-based for gameplay timing.

**Clean Up on Unload** — Cancel long-running repeating actions when your mod unloads to prevent orphaned callbacks.

---

## Thread Safety

**GameSystems** — Safe to access from main thread only. Never call from background threads or tasks.

**ActionScheduler** — Thread-safe for scheduling operations. Action callbacks execute on main thread.

---

## Performance Considerations

### GameSystems
- System property access is cached — negligible overhead
- EntityManager operations follow standard ECS performance characteristics
- PrefabCollectionSystem lookups are optimized but cache results when used repeatedly

### ActionScheduler
- Frame-based actions have minimal overhead per frame
- Time-based actions use Stopwatch for precise timing
- Action count impacts per-frame processing time; keep under 1000 active actions for best performance
- Sequence chaining creates multiple internal actions; use sparingly for complex sequences

---

## Documentation Standards

Each system documentation includes:
- Complete API reference with method signatures
- Practical usage patterns and examples
- Performance characteristics and optimization tips
- Common pitfalls and solutions
- Integration with other ScarletCore systems

---

## Additional Resources

- [Services Documentation](../Services/) — High-level service APIs built on these systems
- [Extensions](../Extensions.md) — ECS extensions for cleaner entity operations
- [Events](../Events/) — Event system built on ActionScheduler and GameSystems
- [Utils](../Utils/) — Supporting utilities (Logger, MathUtility, etc.)

---

## Future Systems

Potential future additions to ScarletCore Systems:
- **NetworkingSystem** — Abstraction for server-client communication
- **SaveSystem** — Structured save/load with versioning
- **ConfigSystem** — Centralized configuration management
- **MetricsSystem** — Performance monitoring and profiling
