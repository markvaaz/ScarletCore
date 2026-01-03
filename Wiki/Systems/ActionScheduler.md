# ActionScheduler

The `ActionScheduler` is a powerful system for scheduling and executing actions with precise timing control. It supports frame-based and time-based scheduling, repeating actions, delayed execution, and complex action sequences.

## Namespace
```csharp
using ScarletCore.Systems;
```

## Overview

The `ActionScheduler` provides:
- Frame-based and time-based action scheduling
- Repeating actions with configurable intervals
- Delayed execution (seconds or frames)
- Action sequences with waits and conditional cancellation
- Action control (pause, resume, cancel)
- Maximum execution limits
- Random interval support
- Unique action identification and tracking

## Table of Contents

- [ActionId](#actionid)
- [Basic Scheduling](#basic-scheduling)
  - [OncePerFrame](#onceperframe)
  - [NextFrame](#nextframe)
  - [Delayed](#delayed)
  - [DelayedFrames](#delayedframes)
- [Repeating Actions](#repeating-actions)
  - [Repeating](#repeating)
  - [RepeatingFrames](#repeatingframes)
  - [RepeatingRandom](#repeatingrandom)
- [Action Control](#action-control)
  - [CancelAction](#cancelaction)
  - [PauseAction](#pauseaction)
  - [ResumeAction](#resumeaction)
  - [ClearAllActions](#clearallactions)
- [Action Tracking](#action-tracking)
  - [GetExecutionCount](#getexecutioncount)
  - [GetMaxExecutions](#getmaxexecutions)
  - [GetRemainingExecutions](#getremainingexecutions)
  - [Count](#count)
- [Action Sequences](#action-sequences)
  - [CreateSequence](#createsequence)
  - [ActionSequence Methods](#actionsequence-methods)
  - [CancelSequence](#cancelsequence)
- [Complete Examples](#complete-examples)
- [Best Practices](#best-practices)
- [Technical Notes](#technical-notes)

## ActionId

### ActionId Struct
```csharp
public readonly record struct ActionId(int Value)
```

Unique identifier for scheduled actions that allows tracking and control.

**Static Methods:**
- `ActionId.Next()`: Generates the next unique action identifier

**Example:**
```csharp
// ActionId is returned by all scheduling methods
var actionId = ActionScheduler.Delayed(() => {
    Log.Message("Action executed!");
}, 5f);

// Use ActionId to control the action
ActionScheduler.CancelAction(actionId);
```

**Notes:**
- Thread-safe generation using Interlocked operations
- Each action gets a unique ID automatically
- Used for all action control operations

---

## Basic Scheduling

### OncePerFrame
```csharp
public static ActionId OncePerFrame(Action action, int maxExecutions = -1)
public static ActionId OncePerFrame(Action<Action> action, int maxExecutions = -1)
```

Schedules an action to execute once per frame until cancelled or max executions reached.

**Parameters:**
- `action` (Action or Action\<Action\>): Action to execute (with optional cancel callback)
- `maxExecutions` (int, optional): Maximum number of executions (-1 for infinite, default: -1)

**Returns:**
- `ActionId`: Unique identifier for controlling the action

**Example:**
```csharp
// Execute every frame (infinite)
var checkId = ActionScheduler.OncePerFrame(() => {
    if (player.Health <= 0) {
        MessageService.Send(player, "You died!");
        ActionScheduler.CancelAction(checkId); // Must capture in closure
    }
});

// Execute every frame for 100 frames
var countId = ActionScheduler.OncePerFrame(() => {
    Log.Message($"Frame: {Time.frameCount}");
}, maxExecutions: 100);

// With cancel support
var monitorId = ActionScheduler.OncePerFrame(cancel => {
    if (boss.Health <= 0) {
        MessageService.SendAll("Boss defeated!");
        cancel(); // Stop monitoring
    }
});
```

**Notes:**
- Executes once per frame (not multiple times in same frame)
- Use maxExecutions to limit total executions
- With cancel callback, action can stop itself

---

### NextFrame
```csharp
public static ActionId NextFrame(Action action)
public static ActionId NextFrame(Action<Action> action)
```

Schedules an action to execute once on the next frame.

**Parameters:**
- `action` (Action or Action\<Action\>): Action to execute (with optional cancel callback)

**Returns:**
- `ActionId`: Unique identifier for controlling the action

**Example:**
```csharp
// Execute on next frame
ActionScheduler.NextFrame(() => {
    MessageService.SendAll("Next frame action!");
});

// Useful for deferring operations
public void SpawnAndModify() {
    var entity = SpawnerService.ImmediateSpawn(prefabGUID, position);
    
    // Modify on next frame to ensure entity is fully initialized
    ActionScheduler.NextFrame(() => {
        StatModifierService.ApplyModifiers(entity, buffGUID, modifiers);
    });
}

// With cancel support (rare for single-frame actions)
ActionScheduler.NextFrame(cancel => {
    if (ShouldSkipAction()) {
        cancel();
        return;
    }
    PerformAction();
});
```

**Notes:**
- Executes exactly once
- Automatically removed after execution
- Useful for deferring operations by one frame

---

### Delayed
```csharp
public static ActionId Delayed(Action action, float delaySeconds)
public static ActionId Delayed(Action<Action> action, float delaySeconds)
```

Schedules an action to execute once after a time delay.

**Parameters:**
- `action` (Action or Action\<Action\>): Action to execute (with optional cancel callback)
- `delaySeconds` (float): Delay before execution in seconds

**Returns:**
- `ActionId`: Unique identifier for controlling the action

**Example:**
```csharp
// Execute after 5 seconds
ActionScheduler.Delayed(() => {
    MessageService.SendAll("5 seconds have passed!");
}, 5f);

// Delayed buff application
ActionScheduler.Delayed(() => {
    BuffService.ApplyBuff(player.Entity, buffGUID);
    MessageService.Send(player, "Buff applied!");
}, 3f);

// Countdown system
for (int i = 5; i > 0; i--) {
    int count = i; // Capture for closure
    ActionScheduler.Delayed(() => {
        MessageService.SendAll($"{count}...");
    }, (5 - i) * 1f);
}

ActionScheduler.Delayed(() => {
    MessageService.SendAll("GO!");
}, 5f);

// With cancel support
var delayedId = ActionScheduler.Delayed(cancel => {
    if (!player.IsConnected) {
        cancel();
        return;
    }
    TeleportService.Teleport(player.Entity, destination);
}, 10f);
```

**Notes:**
- Executes exactly once after delay
- Automatically removed after execution
- Uses Time.time for timing

---

### DelayedFrames
```csharp
public static ActionId DelayedFrames(Action action, int delayFrames)
public static ActionId DelayedFrames(Action<Action> action, int delayFrames)
```

Schedules an action to execute once after a frame delay.

**Parameters:**
- `action` (Action or Action\<Action\>): Action to execute (with optional cancel callback)
- `delayFrames` (int): Delay before execution in frames

**Returns:**
- `ActionId`: Unique identifier for controlling the action

**Example:**
```csharp
// Execute after 5 frames
ActionScheduler.DelayedFrames(() => {
    Log.Message("5 frames later");
}, 5);

// Wait for entity initialization
var entity = SpawnerService.ImmediateSpawn(prefabGUID, position);

ActionScheduler.DelayedFrames(() => {
    // Entity should be fully initialized now
    StatModifierService.ApplyModifiers(entity, buffGUID, modifiers);
}, 5);

// Staggered spawning
for (int i = 0; i < 10; i++) {
    int index = i;
    ActionScheduler.DelayedFrames(() => {
        SpawnerService.Spawn(enemyGUID, spawnPos);
    }, i * 2); // 2 frames between each spawn
}
```

**Notes:**
- Executes exactly once after frame delay
- Uses Time.frameCount for timing
- More precise than time-based for frame-specific operations

---

## Repeating Actions

### Repeating
```csharp
public static ActionId Repeating(Action action, float intervalSeconds, int maxExecutions = -1)
public static ActionId Repeating(Action<Action> action, float intervalSeconds, int maxExecutions = -1)
```

Schedules an action to execute repeatedly at time intervals.

**Parameters:**
- `action` (Action or Action\<Action\>): Action to execute (with optional cancel callback)
- `intervalSeconds` (float): Time interval between executions in seconds
- `maxExecutions` (int, optional): Maximum number of executions (-1 for infinite, default: -1)

**Returns:**
- `ActionId`: Unique identifier for controlling the action

**Example:**
```csharp
// Repeat every 5 seconds (infinite)
var healId = ActionScheduler.Repeating(() => {
    BuffService.ApplyBuff(player.Entity, healBuffGUID);
    MessageService.Send(player, "Regeneration tick!");
}, 5f);

// Repeat 10 times
ActionScheduler.Repeating(() => {
    SpawnerService.Spawn(enemyGUID, spawnPos);
    MessageService.SendAll("Wave spawned!");
}, 30f, maxExecutions: 10);

// With cancel support
var checkId = ActionScheduler.Repeating(cancel => {
    if (boss.Health <= 0) {
        MessageService.SendAll("Boss defeated! Stopping mechanics.");
        cancel();
        return;
    }
    
    // Boss mechanic
    ApplyBossAbility(boss);
}, 15f);

// Auto-save system
ActionScheduler.Repeating(() => {
    SaveAllPlayerData();
    Log.Message("Auto-save completed");
}, 300f); // Every 5 minutes
```

**Notes:**
- Continues until cancelled or max executions reached
- First execution happens after first interval
- Uses Time.time for timing

---

### RepeatingFrames
```csharp
public static ActionId RepeatingFrames(Action action, int frameInterval, int maxExecutions = -1)
public static ActionId RepeatingFrames(Action<Action> action, int frameInterval, int maxExecutions = -1)
```

Schedules an action to execute repeatedly at frame intervals.

**Parameters:**
- `action` (Action or Action\<Action\>): Action to execute (with optional cancel callback)
- `frameInterval` (int): Frame interval between executions
- `maxExecutions` (int, optional): Maximum number of executions (-1 for infinite, default: -1)

**Returns:**
- `ActionId`: Unique identifier for controlling the action

**Example:**
```csharp
// Execute every 60 frames (about 1 second at 60 FPS)
ActionScheduler.RepeatingFrames(() => {
    UpdateGameState();
}, 60);

// Limited executions
ActionScheduler.RepeatingFrames(() => {
    Log.Message($"Tick {Time.frameCount}");
}, 30, maxExecutions: 20);

// Animation system
var animId = ActionScheduler.RepeatingFrames(cancel => {
    if (!entity.Exists()) {
        cancel();
        return;
    }
    
    UpdateAnimation(entity);
}, 2); // Every 2 frames
```

**Notes:**
- More precise than time-based for frame-specific operations
- Useful for animation systems
- First execution happens after first interval

---

### RepeatingRandom
```csharp
public static ActionId RepeatingRandom(Action action, float minIntervalSeconds, float maxIntervalSeconds, int maxExecutions = -1)
public static ActionId RepeatingRandom(Action<Action> action, float minIntervalSeconds, float maxIntervalSeconds, int maxExecutions = -1)
```

Schedules an action to execute repeatedly at random time intervals.

**Parameters:**
- `action` (Action or Action\<Action\>): Action to execute (with optional cancel callback)
- `minIntervalSeconds` (float): Minimum time interval between executions in seconds
- `maxIntervalSeconds` (float): Maximum time interval between executions in seconds
- `maxExecutions` (int, optional): Maximum number of executions (-1 for infinite, default: -1)

**Returns:**
- `ActionId`: Unique identifier for controlling the action

**Example:**
```csharp
// Random events between 10-30 seconds
ActionScheduler.RepeatingRandom(() => {
    TriggerRandomEvent();
    MessageService.SendAll("Random event triggered!");
}, 10f, 30f);

// Boss random abilities between 5-15 seconds
ActionScheduler.RepeatingRandom(cancel => {
    if (!boss.Exists()) {
        cancel();
        return;
    }
    
    int abilityIndex = UnityEngine.Random.Range(0, 3);
    ApplyBossAbility(boss, abilityIndex);
}, 5f, 15f);

// Limited random executions
ActionScheduler.RepeatingRandom(() => {
    SpawnRandomLoot();
}, 2f, 5f, maxExecutions: 10);
```

**Notes:**
- Interval is randomized after each execution
- Useful for unpredictable events
- Min and max intervals create variety

---

## Action Control

### CancelAction
```csharp
public static bool CancelAction(ActionId actionId)
```

Cancels and removes a scheduled action immediately.

**Parameters:**
- `actionId` (ActionId): ID of the action to cancel

**Returns:**
- `bool`: True if action was found and cancelled, false otherwise

**Example:**
```csharp
// Schedule and store ID
var actionId = ActionScheduler.Repeating(() => {
    Log.Message("Repeating action");
}, 1f);

// Cancel later
if (ActionScheduler.CancelAction(actionId)) {
    Log.Message("Action cancelled");
} else {
    Log.Warning("Action not found");
}

// Conditional cancellation
var buffId = ActionScheduler.Repeating(() => {
    BuffService.ApplyBuff(player.Entity, buffGUID);
}, 5f);

// Cancel when player disconnects
if (!player.IsConnected) {
    ActionScheduler.CancelAction(buffId);
}
```

**Notes:**
- Returns false if action not found
- Immediately removes action from scheduler
- Safe to call multiple times with same ID

---

### PauseAction
```csharp
public static void PauseAction(ActionId actionId)
```

Pauses a scheduled action without removing it. The action can be resumed later.

**Parameters:**
- `actionId` (ActionId): ID of the action to pause

**Example:**
```csharp
// Schedule action
var actionId = ActionScheduler.Repeating(() => {
    UpdateGameLogic();
}, 1f);

// Pause when needed
ActionScheduler.PauseAction(actionId);
Log.Message("Action paused");

// Resume later
ActionScheduler.ResumeAction(actionId);
Log.Message("Action resumed");

// Pause during boss phase
var mechanicId = ActionScheduler.Repeating(() => {
    ApplyBossMechanic(boss);
}, 10f);

if (boss.CurrentPhase == 2) {
    ActionScheduler.PauseAction(mechanicId);
}
```

**Notes:**
- Action remains in scheduler but doesn't execute
- Timing continues from where it left off when resumed
- Does nothing if action not found

---

### ResumeAction
```csharp
public static void ResumeAction(ActionId actionId)
```

Resumes a paused action.

**Parameters:**
- `actionId` (ActionId): ID of the action to resume

**Example:**
```csharp
// Pause and resume system
var updateId = ActionScheduler.Repeating(() => {
    UpdateSystem();
}, 1f);

// Pause during cutscene
ActionScheduler.PauseAction(updateId);
PlayCutscene();

// Resume after cutscene
ActionScheduler.ResumeAction(updateId);

// Toggle system
public void ToggleSystem() {
    if (systemEnabled) {
        ActionScheduler.PauseAction(systemId);
        systemEnabled = false;
    } else {
        ActionScheduler.ResumeAction(systemId);
        systemEnabled = true;
    }
}
```

**Notes:**
- Continues from original schedule
- Does nothing if action not found
- Action continues with same timing as before pause

---

### ClearAllActions
```csharp
public static void ClearAllActions()
```

Removes all scheduled actions and clears the executor.

**Example:**
```csharp
// Clear all actions
ActionScheduler.ClearAllActions();
Log.Message("All actions cleared");

// Use with caution - clears EVERYTHING
public void ResetGame() {
    ActionScheduler.ClearAllActions();
    InitializeGameSystems();
}

// Emergency cleanup
public void OnServerShutdown() {
    ActionScheduler.ClearAllActions();
    SaveAllData();
}
```

**Notes:**
- ⚠️ Use with caution - cancels ALL pending actions
- Cannot be undone
- Useful for cleanup or reset scenarios

---

## Action Tracking

### GetExecutionCount
```csharp
public static int GetExecutionCount(ActionId actionId)
```

Gets the number of times a specific action has been executed.

**Parameters:**
- `actionId` (ActionId): ID of the action to check

**Returns:**
- `int`: Execution count, or 0 if action not found

**Example:**
```csharp
var actionId = ActionScheduler.Repeating(() => {
    Log.Message("Executed");
}, 1f);

// Check later
var count = ActionScheduler.GetExecutionCount(actionId);
Log.Message($"Executed {count} times");

// Progress tracking
var waveId = ActionScheduler.Repeating(() => {
    SpawnWave();
}, 30f, maxExecutions: 10);

// Display progress
ActionScheduler.Repeating(() => {
    var current = ActionScheduler.GetExecutionCount(waveId);
    MessageService.SendAll($"Wave {current}/10");
}, 5f);
```

---

### GetMaxExecutions
```csharp
public static int GetMaxExecutions(ActionId actionId)
```

Gets the maximum execution limit for a specific action.

**Parameters:**
- `actionId` (ActionId): ID of the action to check

**Returns:**
- `int`: Maximum executions, or -1 if action not found or has no limit

**Example:**
```csharp
var actionId = ActionScheduler.Repeating(() => {
    PerformAction();
}, 1f, maxExecutions: 100);

var maxExec = ActionScheduler.GetMaxExecutions(actionId);
Log.Message($"Max executions: {maxExec}"); // 100
```

---

### GetRemainingExecutions
```csharp
public static int GetRemainingExecutions(ActionId actionId)
```

Gets the number of remaining executions for a specific action.

**Parameters:**
- `actionId` (ActionId): ID of the action to check

**Returns:**
- `int`: Remaining executions, or -1 if action not found or has unlimited executions

**Example:**
```csharp
var actionId = ActionScheduler.Repeating(() => {
    SpawnEnemy();
}, 2f, maxExecutions: 20);

// Check remaining
ActionScheduler.Repeating(() => {
    var remaining = ActionScheduler.GetRemainingExecutions(actionId);
    if (remaining > 0) {
        MessageService.SendAll($"{remaining} waves remaining");
    }
}, 5f);
```

---

### Count
```csharp
public static int Count { get; }
```

Gets the total number of currently scheduled actions.

**Example:**
```csharp
// Check how many actions are scheduled
var activeActions = ActionScheduler.Count;
Log.Message($"Active scheduled actions: {activeActions}");

// Monitor system load
ActionScheduler.Repeating(() => {
    if (ActionScheduler.Count > 1000) {
        Log.Warning("Too many scheduled actions!");
    }
}, 10f);
```

---

## Action Sequences

### CreateSequence
```csharp
public static ActionSequence CreateSequence()
```

Creates a new action sequence builder for chaining actions and delays.

**Returns:**
- `ActionSequence`: New sequence builder

**Example:**
```csharp
// Create and execute sequence
var sequenceId = ActionScheduler.CreateSequence()
    .Then(() => MessageService.SendAll("Step 1"))
    .ThenWait(2f)
    .Then(() => MessageService.SendAll("Step 2"))
    .ThenWait(3f)
    .Then(() => MessageService.SendAll("Complete!"))
    .Execute();
```

---

### ActionSequence Methods

#### Then
```csharp
public ActionSequence Then(Action action)
public ActionSequence Then(Action<Action> action)
```

Adds an action to the sequence.

**Example:**
```csharp
ActionScheduler.CreateSequence()
    .Then(() => Log.Message("Action 1"))
    .Then(() => Log.Message("Action 2"))
    .Execute();

// With cancel support
ActionScheduler.CreateSequence()
    .Then(cancel => {
        if (ShouldStop()) {
            cancel(); // Stops entire sequence
            return;
        }
        PerformAction();
    })
    .Execute();
```

---

#### ThenWait
```csharp
public ActionSequence ThenWait(float seconds)
```

Adds a time-based wait to the sequence.

**Example:**
```csharp
ActionScheduler.CreateSequence()
    .Then(() => MessageService.SendAll("Starting..."))
    .ThenWait(3f)
    .Then(() => MessageService.SendAll("3 seconds later"))
    .Execute();
```

---

#### ThenWaitFrames
```csharp
public ActionSequence ThenWaitFrames(int frames)
```

Adds a frame-based wait to the sequence.

**Example:**
```csharp
ActionScheduler.CreateSequence()
    .Then(() => Log.Message("Action"))
    .ThenWaitFrames(60)
    .Then(() => Log.Message("60 frames later"))
    .Execute();
```

---

#### ThenWaitRandom
```csharp
public ActionSequence ThenWaitRandom(float minSeconds, float maxSeconds)
```

Adds a random time-based wait to the sequence.

**Example:**
```csharp
ActionScheduler.CreateSequence()
    .Then(() => Log.Message("Step 1"))
    .ThenWaitRandom(1f, 3f)
    .Then(() => Log.Message("Random delay passed"))
    .Execute();
```

---

#### Execute
```csharp
public ActionId Execute()
```

Starts executing the sequence.

**Returns:**
- `ActionId`: ID for controlling the sequence

---

### CancelSequence
```csharp
public static bool CancelSequence(ActionId sequenceId)
```

Cancels a running sequence by its ActionId.

**Parameters:**
- `sequenceId` (ActionId): ID of the sequence to cancel

**Returns:**
- `bool`: True if sequence was found and cancelled

**Example:**
```csharp
var sequenceId = ActionScheduler.CreateSequence()
    .Then(() => Log.Message("Step 1"))
    .ThenWait(5f)
    .Then(() => Log.Message("Step 2"))
    .Execute();

// Cancel sequence
ActionScheduler.CancelSequence(sequenceId);
```

---

## Complete Examples

### Example 1: Boss Fight System

```csharp
using ScarletCore.Systems;
using ScarletCore.Services;

public class BossFightSystem {
    private ActionId phaseCheckId;
    private ActionId mechanicId;
    private Entity bossEntity;
    
    public void StartBossFight(Entity boss) {
        bossEntity = boss;
        
        // Check boss phase every 2 seconds
        phaseCheckId = ActionScheduler.Repeating(cancel => {
            if (!boss.Exists()) {
                cancel();
                OnBossDefeated();
                return;
            }
            
            var health = boss.Read<Health>();
            var healthPercent = health.Value / health.MaxHealth._Value;
            
            if (healthPercent <= 0.5f && currentPhase == 1) {
                TransitionToPhase2();
            }
        }, 2f);
        
        // Random abilities between 5-15 seconds
        mechanicId = ActionScheduler.RepeatingRandom(cancel => {
            if (!boss.Exists()) {
                cancel();
                return;
            }
            
            ApplyRandomAbility(boss);
        }, 5f, 15f);
        
        // Enrage after 10 minutes
        ActionScheduler.Delayed(() => {
            if (boss.Exists()) {
                EnrageBoss(boss);
            }
        }, 600f);
    }
    
    private void ApplyRandomAbility(Entity boss) {
        var abilities = new Action[] {
            () => TeleportAbility(boss),
            () => AoEAbility(boss),
            () => SummonMinions(boss)
        };
        
        var index = UnityEngine.Random.Range(0, abilities.Length);
        abilities[index]();
    }
    
    private void TeleportAbility(Entity boss) {
        var nearestPlayer = TeleportService.FindNearestPlayer(boss.Position(), 50f);
        if (nearestPlayer != null) {
            var offset = new float3(5, 0, 5);
            TeleportService.TeleportToEntity(boss, nearestPlayer.CharacterEntity, offset);
            MessageService.SendAll("Boss teleported!");
        }
    }
    
    private void OnBossDefeated() {
        ActionScheduler.CancelAction(phaseCheckId);
        ActionScheduler.CancelAction(mechanicId);
        MessageService.SendAll("Boss defeated!");
    }
}
```

### Example 2: Event System with Countdown

```csharp
public class EventSystem {
    public void StartEvent(float3 eventCenter) {
        // Announcement sequence
        ActionScheduler.CreateSequence()
            .Then(() => MessageService.Announce("Event starting in 30 seconds!"))
            .ThenWait(20f)
            .Then(() => MessageService.Announce("10 seconds!"))
            .ThenWait(5f)
            .Then(() => MessageService.Announce("5..."))
            .ThenWait(1f)
            .Then(() => MessageService.Announce("4..."))
            .ThenWait(1f)
            .Then(() => MessageService.Announce("3..."))
            .ThenWait(1f)
            .Then(() => MessageService.Announce("2..."))
            .ThenWait(1f)
            .Then(() => MessageService.Announce("1..."))
            .ThenWait(1f)
            .Then(() => {
                MessageService.Announce("EVENT STARTED!");
                StartEventLogic(eventCenter);
            })
            .Execute();
    }
    
    private void StartEventLogic(float3 center) {
        // Spawn waves every 30 seconds for 5 minutes
        var waveId = ActionScheduler.Repeating(() => {
            SpawnWave(center);
            
            var current = ActionScheduler.GetExecutionCount(waveId);
            var remaining = ActionScheduler.GetRemainingExecutions(waveId);
            MessageService.SendAll($"Wave {current} - {remaining} remaining");
        }, 30f, maxExecutions: 10);
        
        // End event after 5 minutes
        ActionScheduler.Delayed(() => {
            ActionScheduler.CancelAction(waveId);
            MessageService.Announce("Event ended!");
        }, 300f);
    }
    
    private void SpawnWave(float3 center) {
        SpawnerService.SpawnInRadius(
            new PrefabGUID(-1905691330),
            center,
            20f,
            180f,
            10
        );
    }
}
```

### Example 3: Buff System with Duration

```csharp
public class BuffDurationSystem {
    private Dictionary<ulong, ActionId> activeBuffs = new();
    
    public void ApplyTimedBuff(PlayerData player, PrefabGUID buffGUID, float duration) {
        // Cancel existing buff if any
        if (activeBuffs.ContainsKey(player.PlatformId)) {
            ActionScheduler.CancelAction(activeBuffs[player.PlatformId]);
        }
        
        // Apply buff
        BuffService.ApplyBuff(player.Entity, buffGUID);
        MessageService.SendSuccess(player, $"Buff applied for {duration} seconds!");
        
        // Warning at 10 seconds remaining
        if (duration > 10f) {
            ActionScheduler.Delayed(() => {
                if (player.IsConnected) {
                    MessageService.SendWarning(player, "Buff expires in 10 seconds!");
                }
            }, duration - 10f);
        }
        
        // Remove buff after duration
        var buffId = ActionScheduler.Delayed(() => {
            BuffService.TryRemoveBuff(player.Entity, buffGUID);
            MessageService.Send(player, "Buff expired.");
            activeBuffs.Remove(player.PlatformId);
        }, duration);
        
        activeBuffs[player.PlatformId] = buffId;
    }
    
    public void ExtendBuff(PlayerData player, float additionalTime) {
        if (!activeBuffs.ContainsKey(player.PlatformId)) {
            MessageService.SendError(player, "No active buff to extend!");
            return;
        }
        
        // Cancel old timer
        ActionScheduler.CancelAction(activeBuffs[player.PlatformId]);
        
        // Create new timer (you'd need to track remaining time in practice)
        MessageService.SendSuccess(player, $"Buff extended by {additionalTime} seconds!");
    }
}
```

### Example 4: Animation System

```csharp
public class AnimationSystem {
    public ActionId PlayAnimation(Entity entity, int frameCount, Action<int> onFrame) {
        int currentFrame = 0;
        
        return ActionScheduler.RepeatingFrames(cancel => {
            if (!entity.Exists()) {
                cancel();
                return;
            }
            
            onFrame(currentFrame);
            currentFrame++;
            
            if (currentFrame >= frameCount) {
                cancel();
            }
        }, 1, maxExecutions: frameCount);
    }
    
    public void PlayEntityAnimation() {
        var entity = SpawnerService.ImmediateSpawn(prefabGUID, position);
        
        // Play 120 frame animation
        PlayAnimation(entity, 120, frame => {
            // Update entity position/rotation each frame
            var offset = new float3(
                Mathf.Sin(frame * 0.1f),
                0,
                Mathf.Cos(frame * 0.1f)
            );
            entity.SetPosition(position + offset);
        });
    }
}
```

### Example 5: Cooldown System

```csharp
public class CooldownSystem {
    private Dictionary<string, DateTime> cooldowns = new();
    private Dictionary<string, ActionId> cooldownNotifiers = new();
    
    public bool IsOnCooldown(PlayerData player, string abilityName) {
        var key = $"{player.PlatformId}_{abilityName}";
        
        if (!cooldowns.ContainsKey(key)) return false;
        
        var remaining = cooldowns[key] - DateTime.Now;
        return remaining.TotalSeconds > 0;
    }
    
    public void StartCooldown(PlayerData player, string abilityName, float cooldownSeconds) {
        var key = $"{player.PlatformId}_{abilityName}";
        cooldowns[key] = DateTime.Now.AddSeconds(cooldownSeconds);
        
        // Notify at 50% remaining
        ActionScheduler.Delayed(() => {
            if (player.IsConnected) {
                MessageService.Send(player, $"{abilityName} cooldown 50% complete");
            }
        }, cooldownSeconds * 0.5f);
        
        // Clear cooldown
        var notifierId = ActionScheduler.Delayed(() => {
            cooldowns.Remove(key);
            cooldownNotifiers.Remove(key);
            
            if (player.IsConnected) {
                MessageService.SendSuccess(player, $"{abilityName} ready!");
            }
        }, cooldownSeconds);
        
        cooldownNotifiers[key] = notifierId;
    }
    
    public float GetRemainingCooldown(PlayerData player, string abilityName) {
        var key = $"{player.PlatformId}_{abilityName}";
        
        if (!cooldowns.ContainsKey(key)) return 0f;
        
        var remaining = cooldowns[key] - DateTime.Now;
        return (float)Math.Max(0, remaining.TotalSeconds);
    }
}
```

### Example 6: Tutorial System

```csharp
public class TutorialSystem {
    public void StartTutorial(PlayerData player) {
        ActionScheduler.CreateSequence()
            .Then(() => {
                MessageService.Send(player, "Welcome to the tutorial!");
                TeleportService.Teleport(player.CharacterEntity, tutorialSpawn);
            })
            .ThenWait(3f)
            .Then(() => {
                MessageService.Send(player, "Let's learn about movement...");
            })
            .ThenWait(5f)
            .Then(() => {
                MessageService.Send(player, "Now let's try combat!");
                SpawnTrainingDummy(player);
            })
            .ThenWait(10f)
            .Then(cancel => {
                if (!HasKilledDummy(player)) {
                    MessageService.Send(player, "Take your time...");
                    // Wait for player to complete
                    WaitForDummyKill(player, cancel);
                } else {
                    // Continue sequence
                }
            })
            .ThenWait(2f)
            .Then(() => {
                MessageService.Send(player, "Great job! Tutorial complete!");
                GiveTutorialReward(player);
            })
            .Execute();
    }
    
    private void WaitForDummyKill(PlayerData player, Action cancel) {
        ActionScheduler.Repeating(innerCancel => {
            if (HasKilledDummy(player)) {
                innerCancel(); // Stop checking
                cancel(); // Continue sequence (actually doesn't cancel, just signals done)
            }
        }, 1f);
    }
}
```

---

## Best Practices

### 1. Always Store ActionId for Long-Running Actions

```csharp
// Good - Store ID for control
private ActionId updateActionId;

public void StartSystem() {
    updateActionId = ActionScheduler.Repeating(() => {
        UpdateSystem();
    }, 1f);
}

public void StopSystem() {
    ActionScheduler.CancelAction(updateActionId);
}

// Avoid - Can't control action later
ActionScheduler.Repeating(() => {
    UpdateSystem();
}, 1f);
```

### 2. Use Cancel Callbacks for Self-Stopping Actions

```csharp
// Good - Action can stop itself
ActionScheduler.Repeating(cancel => {
    if (ShouldStop()) {
        cancel();
        return;
    }
    PerformAction();
}, 1f);

// Avoid - Harder to manage
var actionId = ActionScheduler.Repeating(() => {
    if (ShouldStop()) {
        ActionScheduler.CancelAction(actionId); // Needs closure
        return;
    }
    PerformAction();
}, 1f);
```

### 3. Clean Up Actions Properly

```csharp
// Good - Cancel on cleanup
public class MySystem {
    private List<ActionId> systemActions = new();
    
    public void Initialize() {
        systemActions.Add(ActionScheduler.Repeating(() => Update(), 1f));
    }
    
    public void Cleanup() {
        foreach (var actionId in systemActions) {
            ActionScheduler.CancelAction(actionId);
        }
        systemActions.Clear();
    }
}
```

### 4. Use Appropriate Timing Method

```csharp
// Good - Use frames for precise frame operations
ActionScheduler.RepeatingFrames(() => {
    UpdateAnimation();
}, 2);

// Good - Use seconds for time-based operations
ActionScheduler.Repeating(() => {
    SaveData();
}, 60f);

// Good - Use random for variety
ActionScheduler.RepeatingRandom(() => {
    TriggerEvent();
}, 10f, 30f);
```

### 5. Limit Maximum Executions When Appropriate

```csharp
// Good - Limit executions for finite actions
ActionScheduler.Repeating(() => {
    SpawnWave();
}, 30f, maxExecutions: 10);

// Avoid - Infinite when it should be finite
ActionScheduler.Repeating(() => {
    SpawnWave();
}, 30f); // Will run forever
```

### 6. Use Sequences for Complex Timing

```csharp
// Good - Clear sequence of events
ActionScheduler.CreateSequence()
    .Then(() => Start())
    .ThenWait(2f)
    .Then(() => Middle())
    .ThenWait(3f)
    .Then(() => End())
    .Execute();

// Avoid - Nested delays (harder to read)
ActionScheduler.Delayed(() => {
    Start();
    ActionScheduler.Delayed(() => {
        Middle();
        ActionScheduler.Delayed(() => {
            End();
        }, 3f);
    }, 2f);
}, 0f);
```

---

## Technical Notes

### Thread Safety
- All scheduling methods are thread-safe
- Uses locks for collection access
- ActionId generation uses Interlocked operations

### Performance
- O(1) lookups using internal dictionary cache
- Reusable collections to avoid allocations
- Efficient execution loop

### Timing Precision
- Time-based: Uses Unity's `Time.time`
- Frame-based: Uses Unity's `Time.frameCount`
- Frame-based is more precise for frame-specific operations

### Execution Order
- Actions execute in order they were scheduled
- Same-frame actions execute in scheduling order
- No guaranteed order between different action types

### Memory Management
- Completed actions are automatically cleaned up
- Cancelled actions are removed immediately
- Use `ClearAllActions()` for mass cleanup

### Cancel Callbacks
- Cancel callbacks receive an `Action` parameter
- Calling the callback stops the action
- For sequences, cancels entire sequence

---

## Related Systems
- [GameSystems](GameSystems.md) - Core game systems
- [EventManager](../Events/EventManager.md) - Event handling system

## Notes
- Execute() must be called every frame (handled by ScarletCore automatically)
- All methods return ActionId for tracking
- Actions with maxExecutions automatically remove themselves
- Sequences use OncePerFrame internally
- Random intervals are recalculated after each execution
