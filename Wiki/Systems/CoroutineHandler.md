# CoroutineHandler

`CoroutineHandler` is a static system for scheduling, executing, and controlling Unity coroutines with advanced management features. It supports single, repeated, delayed, frame-based, and random interval executions, with full control over pausing, resuming, cancellation, execution limits, and statistics. Ideal for complex asynchronous flows, custom yields, and integration with Unity's coroutine system.

## Overview

```csharp
using ScarletCore.Systems;

// Start a coroutine that runs every 2 seconds, 5 times
var id = CoroutineHandler.StartRepeating(() => Log.Info("Running!"), 2f, "MyCoroutine", 5);

// Pause, resume, or stop the coroutine
CoroutineHandler.PauseCoroutine(id);
CoroutineHandler.ResumeCoroutine(id);
CoroutineHandler.StopCoroutine(id);
```

## Features

- Start coroutines with time, frame, or random intervals
- Support for cancellation, pausing, and resuming coroutines
- Control of maximum executions and execution count
- Completion callbacks and custom names for debugging
- Automatic and manual cleanup of completed coroutines
- Query statistics and active coroutine names
- **Support for coroutines with cancellation callback (Action<Action>)**

## Cancellation-Aware Coroutines

Some scheduling methods accept an `Action<Action>` delegate (with a cancel callback). This allows your coroutine to receive a callback that, when invoked, cancels the coroutine immediately.

This is useful for:
- Stopping a repeating or long-running coroutine from inside itself
- Cancelling based on custom logic or error
- Early exit on condition

### How it works
- The coroutine action receives a callback parameter (commonly named `cancel` or `cancelAction`).
- If you call this callback, the coroutine will be marked for cancellation and will not run again.

### Example: Using ActionWithCancel
```csharp
CoroutineHandler.StartRepeating((cancel) => {
    Log.Info($"Tick: {Time.time}");
    if (Time.time > 10f) cancel(); // Stop after 10 seconds
}, 1f);
```

You can use ActionWithCancel in all repeating and random interval methods:
- `StartRepeating(Action<Action> actionWithCancel, float delay, string name = null, int maxExecutions = -1)`
- `StartFrameRepeating(Action<Action> actionWithCancel, int frameInterval, string name = null, int maxExecutions = -1)`
- `StartRandomInterval(Action<Action> actionWithCancel, float minDelay, float maxDelay, string name = null, int maxExecutions = -1)`

**Tip:** If you don't need cancellation, use the simpler `Action` overloads.

## Coroutine Scheduling Methods

### StartGeneric
Executes an action after a delay.

```csharp
var id = CoroutineHandler.StartGeneric(() => Log.Info("Delayed!"), 2f);
```

**Parameters:**
- `action` - Action to execute
- `delay` - Delay in seconds
- `name` - Optional name

**Returns:** `CoroutineId` for control

### StartRepeating
Executes repeatedly at time intervals (seconds).

```csharp
var id = CoroutineHandler.StartRepeating(() => Log.Info("Repeating!"), 1f, "Repeat", 5);
```
Or with cancellation support:
```csharp
var id = CoroutineHandler.StartRepeating(cancel => {
    if (shouldStop) cancel();
    else Log.Info("Still running");
}, 1f);
```

**Parameters:**
- `action` - Action to execute (can be `Action` or `Action<Action>`)
- `delay` - Interval in seconds
- `name` - Optional name
- `maxExecutions` - Maximum executions (-1 for infinite)

**Returns:** `CoroutineId` for control

### StartFrameRepeating
Executes repeatedly at frame intervals.

```csharp
var id = CoroutineHandler.StartFrameRepeating(() => Log.Info("Every 10 frames!"), 10);
```
Or with cancellation support:
```csharp
var id = CoroutineHandler.StartFrameRepeating(cancel => {
    if (shouldStop) cancel();
}, 10);
```

**Parameters:**
- `action` - Action to execute (can be `Action` or `Action<Action>`)
- `frameInterval` - Interval in frames
- `name` - Optional name
- `maxExecutions` - Maximum executions (-1 for infinite)

**Returns:** `CoroutineId` for control

### StartRandomInterval
Executes repeatedly at random intervals in seconds.

```csharp
var id = CoroutineHandler.StartRandomInterval(() => Log.Info("Random interval!"), 1f, 5f);
```
Or with cancellation support:
```csharp
var id = CoroutineHandler.StartRandomInterval(cancel => {
    if (shouldStop) cancel();
}, 1f, 5f);
```

**Parameters:**
- `action` - Action to execute (can be `Action` or `Action<Action>`)
- `minDelay` - Minimum interval
- `maxDelay` - Maximum interval
- `name` - Optional name
- `maxExecutions` - Maximum executions (-1 for infinite)

**Returns:** `CoroutineId` for control

### NextFrame
Executes an action on the next frame.

```csharp
var id = CoroutineHandler.NextFrame(() => Log.Info("Next frame!"));
```

**Parameters:**
- `action` - Action to execute
- `name` - Optional name

**Returns:** `CoroutineId` for control

## Control Methods

### StopCoroutine
Stops and removes a coroutine.

```csharp
CoroutineHandler.StopCoroutine(id);
```

**Parameters:**
- `id` - CoroutineId

**Returns:** True if found and stopped

### PauseCoroutine / ResumeCoroutine
Pauses or resumes a coroutine.

```csharp
CoroutineHandler.PauseCoroutine(id);
CoroutineHandler.ResumeCoroutine(id);
```

**Parameters:**
- `id` - CoroutineId

### PauseAllCoroutines / ResumeAllCoroutines
Pauses or resumes all active coroutines.

```csharp
CoroutineHandler.PauseAllCoroutines();
CoroutineHandler.ResumeAllCoroutines();
```

### TogglePauseCoroutine
Toggles the pause state of a coroutine.

```csharp
CoroutineHandler.TogglePauseCoroutine(id);
```

**Returns:** True if paused, false if resumed or not found

### StopAllCoroutines
Stops all managed coroutines.

```csharp
CoroutineHandler.StopAllCoroutines();
```

## Query Methods

### IsCoroutineRunning / IsCoroutinePaused
Checks if a coroutine is running or paused.

```csharp
CoroutineHandler.IsCoroutineRunning(id);
CoroutineHandler.IsCoroutinePaused(id);
```

### GetExecutionCount / GetMaxExecutions / GetRemainingExecutions
Query executions performed, limit, and remaining executions for a coroutine.

```csharp
var execs = CoroutineHandler.GetExecutionCount(id);
var max = CoroutineHandler.GetMaxExecutions(id);
var left = CoroutineHandler.GetRemainingExecutions(id);
```

### GetActiveCoroutineNames
List names of active coroutines.

```csharp
var names = CoroutineHandler.GetActiveCoroutineNames();
```

### GetCoroutineStatistics
Returns aggregate statistics (active, inactive, infinite, limited, total executions, auto-cleanup).

```csharp
var stats = CoroutineHandler.GetCoroutineStatistics();
```

### GetPauseDuration
Returns how long a coroutine has been paused.

```csharp
var duration = CoroutineHandler.GetPauseDuration(id);
```

### ActiveCoroutineCount
Number of active coroutines.

```csharp
var count = CoroutineHandler.ActiveCoroutineCount;
```

## Cleanup & Maintenance

### CleanupCompletedCoroutines
Removes finished or invalid coroutines, invoking completion callbacks.

```csharp
CoroutineHandler.CleanupCompletedCoroutines();
```

### EnableAutoCleanup / DisableAutoCleanup
Enables/disables periodic automatic cleanup.

```csharp
CoroutineHandler.EnableAutoCleanup(30f); // every 30 seconds
CoroutineHandler.DisableAutoCleanup();
```

### ForceCleanup
Forces cleanup of coroutines by criteria (inactive, completed, all).

```csharp
CoroutineHandler.ForceCleanup(onlyInactive: true);
```

## Usage Examples

### Simple scheduling
```csharp
// Executes an action every second, 5 times
var id = CoroutineHandler.StartRepeating(() => Log.Info("Tick!"), 1f, "TickCoroutine", 5);
```

### ActionWithCancel for early exit
```csharp
CoroutineHandler.StartRepeating(cancel => {
    if (SomeCondition()) cancel();
    else Log.Info("Still running");
}, 1f);
```

### Pause, resume, and cancellation
```csharp
CoroutineHandler.PauseCoroutine(id);
CoroutineHandler.ResumeCoroutine(id);
CoroutineHandler.StopCoroutine(id);
```

## Important Notes

- **Coroutines are identified by a unique, thread-safe `CoroutineId`**
- **Do NOT call Unity's `StartCoroutine` directly for managed coroutines** – always use `CoroutineHandler` methods for tracking and control
- **Coroutines can be paused and resumed at yield points**
- **Completion callbacks (`OnComplete`) are called on both natural completion and cancellation**
- **Use `EnableAutoCleanup` in long-running systems to avoid memory leaks**
- **The system is thread-safe for control operations, but actions executed in coroutines should be thread-safe if accessing shared resources**

## When to Use ActionScheduler Instead

While `CoroutineHandler` is ideal for complex asynchronous flows, custom yields, and integration with Unity's coroutine system, there are scenarios where `ActionScheduler` is a better fit:

- **Simple timed, repeated, or delayed actions:** Use `ActionScheduler` for most simple automation, timers, or event sequences.
- **Performance-critical bulk scheduling:** `ActionScheduler` is optimized for lightweight, high-frequency actions.
- **No need for custom yields or Unity yield instructions:** If you don't need to yield for Unity objects/events or use `WaitForSeconds`, prefer `ActionScheduler`.

> **Tip:** Use `CoroutineHandler` for advanced coroutine control, custom yields, or when you need to interact with Unity's coroutine/yield system. Use `ActionScheduler` for most simple, repeated, or delayed actions.
