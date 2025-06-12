# ActionScheduler

`ActionScheduler` is a static system for scheduling, executing, and controlling timed actions, supporting single, repeated, delayed, frame-based, or random interval executions. Ideal for game logic, automation, and event sequences.

## Overview

```csharp
using ScarletCore.Systems;

// Schedule an action to run every 5 seconds, 3 times
var id = ActionScheduler.Repeating(() => Log.Info("Running!"), 5f, 3);

// Cancel the action before it completes
ActionScheduler.CancelAction(id);
```

## Features

- Schedule actions by time, frames, or random intervals
- Support for cancellation, pausing, and resuming actions
- Execution of chained action sequences (ActionSequence)
- Control of maximum executions and execution count
- Performance optimizations with cache and reusable lists
- **Support for actions with cancellation callback (ActionWithCancel)**

## Cancellation-Aware Actions

Some scheduling methods accept an `Action<Action>` delegate (called ActionWithCancel). This allows your action to receive a callback that, when invoked, cancels the scheduled action immediately.

This is useful for:
- Stopping a repeating or long-running action from inside itself
- Cancelling a sequence step based on custom logic
- Early exit on error or condition

### How it works
- The scheduled action receives a callback parameter (commonly named `cancel` or `cancelAction`).
- If you call this callback, the action will be marked for cancellation and will not run again.

### Example: Using ActionWithCancel
```csharp
// Schedule a repeating action that cancels itself after a condition
ActionScheduler.Repeating((cancel) => {
    Log.Info($"Tick: {Time.time}");
    if (Time.time > 10f) cancel(); // Stop after 10 seconds
}, 1f);
```

You can use ActionWithCancel in all scheduling methods:
- `OncePerFrame(Action<Action> action, int maxExecutions = -1)`
- `NextFrame(Action<Action> action)`
- `Repeating(Action<Action> action, float intervalSeconds, int maxExecutions = -1)`
- `RepeatingFrames(Action<Action> action, int frameInterval, int maxExecutions = -1)`
- `Delayed(Action<Action> action, float delaySeconds)`
- `DelayedFrames(Action<Action> action, int delayFrames)`
- `RepeatingRandom(Action<Action> action, float minIntervalSeconds, float maxIntervalSeconds, int maxExecutions = -1)`

**Tip:** If you don't need cancellation, use the simpler `Action` overloads.

## Action Sequences

Allows you to create chained sequences of actions and delays:

```csharp
var seqId = ActionScheduler.CreateSequence()
    .Then(() => Log.Info("Step 1"))
    .ThenWait(2f)
    .Then(() => Log.Info("Step 2"))
    .Execute();
```

- `Then(Action)` / `Then(Action<Action>)`: Adds an action (with or without cancel support)
- `ThenWait(float seconds)`: Adds a delay in seconds
- `ThenWaitFrames(int frames)`: Adds a delay in frames
- `ThenWaitRandom(float min, float max)`: Random delay
- `Cancel()`: Cancels the sequence

## Scheduling Methods

### OncePerFrame
Executes an action every frame.

```csharp
var id = ActionScheduler.OncePerFrame(() => Log.Info("Every frame!"));
```

Or with cancellation support:
```csharp
var id = ActionScheduler.OncePerFrame(cancel => {
    // ...
    if (shouldStop) cancel();
});
```

**Parameters:**
- `action` - Action to execute (can be `Action` or `Action<Action>`)
- `maxExecutions` - Maximum executions (-1 for infinite)

**Returns:** `ActionId` for control

### NextFrame
Executes an action on the next frame.

```csharp
var id = ActionScheduler.NextFrame(() => Log.Info("Next frame!"));
```
Or with cancellation support:
```csharp
var id = ActionScheduler.NextFrame(cancel => {
    // ...
});
```

**Parameters:**
- `action` - Action to execute (can be `Action` or `Action<Action>`)

**Returns:** `ActionId` for control

### Repeating
Executes repeatedly at time intervals (seconds).

```csharp
var id = ActionScheduler.Repeating(() => Log.Info("Repeating!"), 2f);
```
Or with cancellation support:
```csharp
var id = ActionScheduler.Repeating(cancel => {
    // ...
    if (shouldStop) cancel();
}, 2f);
```

**Parameters:**
- `action` - Action to execute (can be `Action` or `Action<Action>`)
- `intervalSeconds` - Interval in seconds
- `maxExecutions` - Maximum executions (-1 for infinite)

**Returns:** `ActionId` for control

### RepeatingFrames
Executes repeatedly at frame intervals.

```csharp
var id = ActionScheduler.RepeatingFrames(() => Log.Info("Every 10 frames!"), 10);
```
Or with cancellation support:
```csharp
var id = ActionScheduler.RepeatingFrames(cancel => {
    // ...
    if (shouldStop) cancel();
}, 10);
```

**Parameters:**
- `action` - Action to execute (can be `Action` or `Action<Action>`)
- `frameInterval` - Interval in frames
- `maxExecutions` - Maximum executions (-1 for infinite)

**Returns:** `ActionId` for control

### Delayed
Executes once after a delay in seconds.

```csharp
ActionScheduler.Delayed(() => Log.Info("3s delay!"), 3f);
```
Or with cancellation support:
```csharp
ActionScheduler.Delayed(cancel => {
    // ...
}, 3f);
```

**Parameters:**
- `action` - Action to execute (can be `Action` or `Action<Action>`)
- `delaySeconds` - Delay in seconds

**Returns:** `ActionId` for control

### DelayedFrames
Executes once after a delay in frames.

```csharp
ActionScheduler.DelayedFrames(() => Log.Info("5 frames delay!"), 5);
```
Or with cancellation support:
```csharp
ActionScheduler.DelayedFrames(cancel => {
    // ...
}, 5);
```

**Parameters:**
- `action` - Action to execute (can be `Action` or `Action<Action>`)
- `delayFrames` - Delay in frames

**Returns:** `ActionId` for control

### RepeatingRandom
Executes repeatedly at random intervals in seconds.

```csharp
var id = ActionScheduler.RepeatingRandom(() => Log.Info("Random interval!"), 1f, 5f);
```
Or with cancellation support:
```csharp
var id = ActionScheduler.RepeatingRandom(cancel => {
    // ...
    if (shouldStop) cancel();
}, 1f, 5f);
```

**Parameters:**
- `action` - Action to execute (can be `Action` or `Action<Action>`)
- `minIntervalSeconds` - Minimum interval
- `maxIntervalSeconds` - Maximum interval
- `maxExecutions` - Maximum executions (-1 for infinite)

**Returns:** `ActionId` for control

## Control Methods

### CancelAction
Cancels and removes a scheduled action.

```csharp
ActionScheduler.CancelAction(id);
```

**Parameters:**
- `actionId` - Action ID

**Returns:** True if the action was found and cancelled

### PauseAction / ResumeAction
Pauses or resumes a scheduled action.

```csharp
ActionScheduler.PauseAction(id);
ActionScheduler.ResumeAction(id);
```

**Parameters:**
- `actionId` - Action ID

### ClearAllActions
Removes all scheduled actions.

```csharp
ActionScheduler.ClearAllActions();
```

## Query Methods

### Count
Total number of scheduled actions.

```csharp
var total = ActionScheduler.Count;
```

### GetExecutionCount / GetMaxExecutions / GetRemainingExecutions
Query executions performed, limit, and remaining executions for an action.

```csharp
var execs = ActionScheduler.GetExecutionCount(id);
var max = ActionScheduler.GetMaxExecutions(id);
var left = ActionScheduler.GetRemainingExecutions(id);
```

## Usage Examples

### Simple scheduling
```csharp
// Executes an action every second, 5 times
var id = ActionScheduler.Repeating(() => Log.Info("Tick!"), 1f, 5);
```

### ActionWithCancel for early exit
```csharp
ActionScheduler.Repeating(cancel => {
    if (SomeCondition()) cancel();
    else Log.Info("Still running");
}, 1f);
```

### Action sequence with cancellation
```csharp
var seq = ActionScheduler.CreateSequence()
    .Then(() => Log.Info("Start"))
    .ThenWait(1f)
    .Then(cancel => {
        if (ShouldStop()) cancel();
        else Log.Info("Continue");
    })
    .Execute();
```

### Cancellation
```csharp
ActionScheduler.CancelAction(id);
```

## Important Notes

- **Do NOT call `ActionScheduler.Execute()` manually** – it is called automatically by ScarletCore. You should never call it yourself in your code or game loop.
- **Actions can be paused and resumed**
- **Actions with callback receive a cancellation method**
- **Sequences can be cancelled at any time**
- **Maximum executions control how many times the action will run**
- **Actions are identified by a unique, thread-safe `ActionId`**

## When to Use CoroutineHandler Instead

While `ActionScheduler` is ideal for most timed, repeated, or delayed actions, there are scenarios where Unity coroutines (managed by `CoroutineHandler`) are a better fit:

- **Complex asynchronous flows:** If you need to yield for custom conditions, wait for Unity events, or perform multi-step logic with multiple yields, use `CoroutineHandler`.
- **Waiting for Unity objects or events:** For example, waiting for a scene to load, an animation to finish, or a physics event.
- **Pausing/resuming at arbitrary yield points:** Coroutines can be paused and resumed at any yield, not just between executions.
- **Integration with Unity's coroutine system:** If you need to use `yield return` with `WaitForSeconds`, `WaitUntil`, or other Unity yield instructions.

> **Tip:** Use `ActionScheduler` for most simple, repeated, or delayed actions. Use `CoroutineHandler` when you need full coroutine control, custom yields, or integration with Unity's coroutine system.
