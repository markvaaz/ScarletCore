using System.Reflection;
using System;
using System.Collections.Generic;
using System.Threading;
using ScarletCore.Utils;
using UnityEngine;

namespace ScarletCore.Systems;

/// <summary>
/// Unique identifier for scheduled actions that allows tracking and control.
/// Uses thread-safe incrementing to ensure uniqueness across all actions.
/// </summary>
public readonly record struct ActionId(int Value) {
  // Thread-safe counter to generate unique IDs
  private static int _nextId = 0;

  /// <summary>
  /// Generates the next unique action identifier.
  /// Thread-safe implementation using Interlocked operations.
  /// </summary>
  /// <returns>A new unique ActionId</returns>
  public static ActionId Next() => new(Interlocked.Increment(ref _nextId));
}

/// <summary>
/// Defines the execution behavior and timing for scheduled actions.
/// </summary>
public enum ActionType {
  /// <summary>Executes once per frame until manually cancelled or max executions reached</summary>
  OncePerFrame,
  /// <summary>Executes once on the next frame then removes itself</summary>
  NextFrame,
  /// <summary>Repeats execution at specified second intervals</summary>
  RepeatingSeconds,
  /// <summary>Repeats execution at specified frame intervals</summary>
  RepeatingFrames,
  /// <summary>Executes once after a specified delay in seconds</summary>
  DelayedSeconds,
  /// <summary>Executes once after a specified delay in frames</summary>
  DelayedFrames
}

/// <summary>
/// Represents a scheduled action with its execution parameters and state.
/// Handles both simple actions and actions that support cancellation callbacks.
/// </summary>
public class ScheduledAction {
  /// <summary>Action that receives a cancel callback parameter</summary>
  public Action<Action> ActionWithCancel { get; set; }

  /// <summary>Simple action without cancel support</summary>
  public Action Action { get; set; }

  /// <summary>Type of scheduling behavior for this action</summary>
  public ActionType Type { get; set; }

  /// <summary>Interval in seconds for time-based scheduling</summary>
  public float Interval { get; set; }

  /// <summary>Next execution time for time-based scheduling</summary>
  public float NextExecutionTime { get; set; }

  /// <summary>Interval in frames for frame-based scheduling</summary>
  public int FrameInterval { get; set; }

  /// <summary>Next execution frame for frame-based scheduling</summary>
  public int NextExecutionFrame { get; set; }

  /// <summary>Last frame this action was executed (prevents duplicate executions)</summary>
  public int LastExecutionFrame { get; set; } = -1;

  /// <summary>Whether this action is currently active and should be processed</summary>
  public bool IsActive { get; set; } = true;

  /// <summary>Unique identifier for this action</summary>
  public ActionId Id { get; set; }

  /// <summary>Flag indicating the action should be cancelled and removed</summary>
  public bool ShouldCancel { get; set; } = false;

  /// <summary>Maximum number of executions (-1 for infinite)</summary>
  public int MaxExecutions { get; set; } = -1;

  /// <summary>Current number of times this action has been executed</summary>
  public int ExecutionCount { get; set; } = 0;

  /// <summary>Minimum interval for random scheduling</summary>
  public float MinInterval { get; set; } = 0f;

  /// <summary>Maximum interval for random scheduling</summary>
  public float MaxInterval { get; set; } = 0f;

  /// <summary>
  /// Creates a scheduled action with a simple action delegate.
  /// </summary>
  /// <param name="action">Action to execute</param>
  /// <param name="type">Scheduling behavior type</param>
  /// <param name="interval">Time interval for time-based scheduling</param>
  /// <param name="frameInterval">Frame interval for frame-based scheduling</param>
  /// <param name="id">Optional custom ID (generates new one if not provided)</param>
  /// <param name="maxExecutions">Maximum executions (-1 for infinite)</param>
  public ScheduledAction(Action action, ActionType type, float interval = 0f, int frameInterval = 0, ActionId id = default, int maxExecutions = -1) {
    Action = action;
    Type = type;
    Interval = interval;
    FrameInterval = frameInterval;
    Id = id.Value == 0 ? ActionId.Next() : id;
    MaxExecutions = maxExecutions;

    InitializeTiming(type, interval, frameInterval);
  }

  /// <summary>
  /// Creates a scheduled action with a cancellation-aware action delegate.
  /// </summary>
  /// <param name="actionWithCancel">Action that receives a cancel callback</param>
  /// <param name="type">Scheduling behavior type</param>
  /// <param name="interval">Time interval for time-based scheduling</param>
  /// <param name="frameInterval">Frame interval for frame-based scheduling</param>
  /// <param name="id">Optional custom ID (generates new one if not provided)</param>
  /// <param name="maxExecutions">Maximum executions (-1 for infinite)</param>
  public ScheduledAction(Action<Action> actionWithCancel, ActionType type, float interval = 0f, int frameInterval = 0, ActionId id = default, int maxExecutions = -1) {
    ActionWithCancel = actionWithCancel;
    Type = type;
    Interval = interval;
    FrameInterval = frameInterval;
    Id = id.Value == 0 ? ActionId.Next() : id;
    MaxExecutions = maxExecutions;

    InitializeTiming(type, interval, frameInterval);
  }

  /// <summary>
  /// Initializes timing parameters based on the action type.
  /// Sets up the next execution time or frame based on the scheduling behavior.
  /// </summary>
  /// <param name="type">Action scheduling type</param>
  /// <param name="interval">Time interval for time-based actions</param>
  /// <param name="frameInterval">Frame interval for frame-based actions</param>
  private void InitializeTiming(ActionType type, float interval, int frameInterval) {
    switch (type) {
      case ActionType.DelayedSeconds:
      case ActionType.RepeatingSeconds:
        // Set next execution to current time plus the specified interval
        NextExecutionTime = Time.time + interval;
        break;
      case ActionType.DelayedFrames:
      case ActionType.RepeatingFrames:
        // Set next execution to current frame plus the specified frame interval
        NextExecutionFrame = Time.frameCount + frameInterval;
        break;
      case ActionType.NextFrame:
        // Execute on the very next frame
        NextExecutionFrame = Time.frameCount + 1;
        break;
        // OncePerFrame doesn't need timing initialization
    }
  }
}

/// <summary>
/// Static executor system for managing and running scheduled actions.
/// Provides various scheduling methods and execution control for delayed, repeating, and frame-based actions.
/// </summary>
public static class ActionScheduler {
  // Collections to manage scheduled actions and cleanup
  private static readonly List<ScheduledAction> _scheduledActions = new();
  private static readonly List<ScheduledAction> _actionsToRemove = new();

  // Cache for fast O(1) lookups by ActionId - PERFORMANCE OPTIMIZATION
  private static readonly Dictionary<ActionId, ScheduledAction> _actionLookup = new();

  // Reusable list to avoid allocations in Execute() - PERFORMANCE OPTIMIZATION  
  private static readonly List<ScheduledAction> _activeActions = new();

  /// <summary>
  /// Removes all scheduled actions associated with a specific assembly.
  /// </summary>
  /// <param name="asm">Assembly whose actions should be removed</param>
  public static void UnregisterAssembly(Assembly assembly = null) {
    Assembly asm;
    if (assembly == null) {
      var stackTrace = new System.Diagnostics.StackTrace();
      var callingMethod = stackTrace.GetFrame(1)?.GetMethod();
      asm = callingMethod?.DeclaringType?.Assembly ?? Assembly.GetExecutingAssembly();
    } else {
      asm = assembly;
    }
    lock (_scheduledActions) {
      // Collect actions to remove
      var toRemove = new List<ScheduledAction>();
      foreach (var action in _scheduledActions) {
        var method = action.Action?.Method ?? action.ActionWithCancel?.Method;
        if (method != null && method.DeclaringType != null && method.DeclaringType.Assembly == asm) {
          toRemove.Add(action);
        }
      }
      foreach (var action in toRemove) {
        _scheduledActions.Remove(action);
        _actionLookup.Remove(action.Id);
      }
    }
  }

  /// <summary>
  /// Main execution method that processes all scheduled actions.
  /// Should be called every frame from the main game loop.
  /// </summary>
  public static void Execute() {
    var currentTime = Time.time;
    var currentFrame = Time.frameCount;

    // Use reusable list to avoid allocations - PERFORMANCE OPTIMIZATION
    // Clear and copy all actions to avoid modification during enumeration
    lock (_scheduledActions) {
      _activeActions.Clear();
      _activeActions.AddRange(_scheduledActions);
    }

    // Process each scheduled action using the safe copy
    foreach (var action in _activeActions) {
      // Skip if action was removed from the main list while we were processing
      // IMPORTANT: Keep existing logic to maintain thread safety
      bool stillExists;
      lock (_scheduledActions) {
        stillExists = _scheduledActions.Contains(action);
      }

      if (!stillExists) continue;

      // Skip inactive or cancelled actions
      if (!action.IsActive || action.ShouldCancel) {
        if (action.ShouldCancel) {
          _actionsToRemove.Add(action);
        }
        continue;
      }

      bool shouldExecute = false;
      bool shouldRemove = false;

      // Determine if action should execute based on its type and timing
      switch (action.Type) {
        case ActionType.OncePerFrame:
          // Execute once per frame, but not multiple times in the same frame
          if (action.LastExecutionFrame != currentFrame) {
            shouldExecute = true;
            action.LastExecutionFrame = currentFrame;
          }
          break;

        case ActionType.NextFrame:
          // Execute once when the target frame is reached
          if (currentFrame >= action.NextExecutionFrame) {
            shouldExecute = true;
            shouldRemove = true; // Remove after single execution
          }
          break;
        case ActionType.RepeatingSeconds:
          // Execute repeatedly at time intervals
          if (currentTime >= action.NextExecutionTime) {
            shouldExecute = true;
            // Use random interval if MinInterval and MaxInterval are set
            if (action.MinInterval > 0 && action.MaxInterval > action.MinInterval) {
              action.NextExecutionTime = currentTime + GetRandomInterval(action.MinInterval, action.MaxInterval);
            } else {
              action.NextExecutionTime = currentTime + action.Interval; // Schedule next execution
            }
          }
          break;

        case ActionType.RepeatingFrames:
          // Execute repeatedly at frame intervals
          if (currentFrame >= action.NextExecutionFrame) {
            shouldExecute = true;
            action.NextExecutionFrame = currentFrame + action.FrameInterval; // Schedule next execution
          }
          break;

        case ActionType.DelayedSeconds:
          // Execute once after time delay
          if (currentTime >= action.NextExecutionTime) {
            shouldExecute = true;
            shouldRemove = true; // Remove after single execution
          }
          break;

        case ActionType.DelayedFrames:
          // Execute once after frame delay
          if (currentFrame >= action.NextExecutionFrame) {
            shouldExecute = true;
            shouldRemove = true; // Remove after single execution
          }
          break;
      }

      // Execute the action if conditions are met
      if (shouldExecute) {
        try {
          // Execute action with or without cancel callback support
          if (action.ActionWithCancel != null) {
            // Provide cancel callback that sets the ShouldCancel flag
            action.ActionWithCancel(() => action.ShouldCancel = true);
          } else {
            // Execute simple action
            action.Action?.Invoke();
          }

          action.ExecutionCount++;

          // Check if maximum executions reached for repeating actions
          if (action.MaxExecutions > 0 && action.ExecutionCount >= action.MaxExecutions) {
            shouldRemove = true;
          }
        } catch (Exception ex) {
          // Log execution errors without crashing the executor
          Log.Error($"Error executing scheduled action {action.Id}: {ex}");
          shouldRemove = true; // Remove broken actions
        }
      }

      // Mark action for removal if it should be cleaned up
      if (shouldRemove) {
        _actionsToRemove.Add(action);
      }
    }

    // Clean up completed or cancelled actions - UPDATE CACHE TOO
    if (_actionsToRemove.Count > 0) {
      lock (_scheduledActions) {
        foreach (var action in _actionsToRemove) {
          _scheduledActions.Remove(action);
          _actionLookup.Remove(action.Id); // Remove from cache too - PERFORMANCE OPTIMIZATION
        }
      }
      _actionsToRemove.Clear();
    }
  }

  #region Scheduling Methods with Cancel Support

  /// <summary>
  /// Internal helper method to safely add an action and maintain cache synchronization.
  /// PERFORMANCE OPTIMIZATION: Maintains both list and dictionary in sync.
  /// </summary>
  /// <param name="scheduledAction">The action to add</param>
  private static void AddActionInternal(ScheduledAction scheduledAction) {
    lock (_scheduledActions) {
      _scheduledActions.Add(scheduledAction);
      _actionLookup[scheduledAction.Id] = scheduledAction;
    }
  }

  /// <summary>
  /// Schedules an action to execute once per frame with cancel support.
  /// </summary>
  /// <param name="action">Action that receives a cancel callback</param>
  /// <param name="maxExecutions">Maximum number of executions (-1 for infinite)</param>
  /// <returns>ActionId for controlling the scheduled action</returns>
  public static ActionId OncePerFrame(Action<Action> action, int maxExecutions = -1) {
    var scheduledAction = new ScheduledAction(action, ActionType.OncePerFrame, maxExecutions: maxExecutions);
    AddActionInternal(scheduledAction);
    return scheduledAction.Id;
  }

  /// <summary>
  /// Schedules a simple action to execute once per frame.
  /// </summary>
  /// <param name="action">Action to execute</param>
  /// <param name="maxExecutions">Maximum number of executions (-1 for infinite)</param>
  /// <returns>ActionId for controlling the scheduled action</returns>
  public static ActionId OncePerFrame(Action action, int maxExecutions = -1) {
    var scheduledAction = new ScheduledAction(action, ActionType.OncePerFrame, maxExecutions: maxExecutions);
    AddActionInternal(scheduledAction);
    return scheduledAction.Id;
  }

  /// <summary>
  /// Schedules an action to execute once on the next frame with cancel support.
  /// </summary>
  /// <param name="action">Action that receives a cancel callback</param>
  /// <returns>ActionId for controlling the scheduled action</returns>
  public static ActionId NextFrame(Action<Action> action) {
    var scheduledAction = new ScheduledAction(action, ActionType.NextFrame);
    AddActionInternal(scheduledAction);
    return scheduledAction.Id;
  }

  /// <summary>
  /// Schedules a simple action to execute once on the next frame.
  /// </summary>
  /// <param name="action">Action to execute</param>
  /// <returns>ActionId for controlling the scheduled action</returns>
  public static ActionId NextFrame(Action action) {
    var scheduledAction = new ScheduledAction(action, ActionType.NextFrame);
    AddActionInternal(scheduledAction);
    return scheduledAction.Id;
  }

  /// <summary>
  /// Schedules an action to execute repeatedly at time intervals with cancel support.
  /// </summary>
  /// <param name="action">Action that receives a cancel callback</param>
  /// <param name="intervalSeconds">Time interval between executions in seconds</param>
  /// <param name="maxExecutions">Maximum number of executions (-1 for infinite)</param>
  /// <returns>ActionId for controlling the scheduled action</returns>
  public static ActionId Repeating(Action<Action> action, float intervalSeconds, int maxExecutions = -1) {
    var scheduledAction = new ScheduledAction(action, ActionType.RepeatingSeconds, intervalSeconds, maxExecutions: maxExecutions);
    AddActionInternal(scheduledAction);
    return scheduledAction.Id;
  }

  /// <summary>
  /// Schedules a simple action to execute repeatedly at time intervals.
  /// </summary>
  /// <param name="action">Action to execute</param>
  /// <param name="intervalSeconds">Time interval between executions in seconds</param>
  /// <param name="maxExecutions">Maximum number of executions (-1 for infinite)</param>
  /// <returns>ActionId for controlling the scheduled action</returns>
  public static ActionId Repeating(Action action, float intervalSeconds, int maxExecutions = -1) {
    var scheduledAction = new ScheduledAction(action, ActionType.RepeatingSeconds, intervalSeconds, maxExecutions: maxExecutions);
    AddActionInternal(scheduledAction);
    return scheduledAction.Id;
  }

  /// <summary>
  /// Schedules an action to execute repeatedly at frame intervals with cancel support.
  /// </summary>
  /// <param name="action">Action that receives a cancel callback</param>
  /// <param name="frameInterval">Frame interval between executions</param>
  /// <param name="maxExecutions">Maximum number of executions (-1 for infinite)</param>
  /// <returns>ActionId for controlling the scheduled action</returns>
  public static ActionId RepeatingFrames(Action<Action> action, int frameInterval, int maxExecutions = -1) {
    var scheduledAction = new ScheduledAction(action, ActionType.RepeatingFrames, frameInterval: frameInterval, maxExecutions: maxExecutions);
    AddActionInternal(scheduledAction);
    return scheduledAction.Id;
  }

  /// <summary>
  /// Schedules a simple action to execute repeatedly at frame intervals.
  /// </summary>
  /// <param name="action">Action to execute</param>
  /// <param name="frameInterval">Frame interval between executions</param>
  /// <param name="maxExecutions">Maximum number of executions (-1 for infinite)</param>
  /// <returns>ActionId for controlling the scheduled action</returns>
  public static ActionId RepeatingFrames(Action action, int frameInterval, int maxExecutions = -1) {
    var scheduledAction = new ScheduledAction(action, ActionType.RepeatingFrames, frameInterval: frameInterval, maxExecutions: maxExecutions);
    AddActionInternal(scheduledAction);
    return scheduledAction.Id;
  }

  /// <summary>
  /// Schedules an action to execute once after a time delay with cancel support.
  /// </summary>
  /// <param name="action">Action that receives a cancel callback</param>
  /// <param name="delaySeconds">Delay before execution in seconds</param>
  /// <returns>ActionId for controlling the scheduled action</returns>
  public static ActionId Delayed(Action<Action> action, float delaySeconds) {
    var scheduledAction = new ScheduledAction(action, ActionType.DelayedSeconds, delaySeconds);
    AddActionInternal(scheduledAction);
    return scheduledAction.Id;
  }

  /// <summary>
  /// Schedules a simple action to execute once after a time delay.
  /// </summary>
  /// <param name="action">Action to execute</param>
  /// <param name="delaySeconds">Delay before execution in seconds</param>
  /// <returns>ActionId for controlling the scheduled action</returns>
  public static ActionId Delayed(Action action, float delaySeconds) {
    var scheduledAction = new ScheduledAction(action, ActionType.DelayedSeconds, delaySeconds);
    AddActionInternal(scheduledAction);
    return scheduledAction.Id;
  }

  /// <summary>
  /// Schedules an action to execute once after a frame delay with cancel support.
  /// </summary>
  /// <param name="action">Action that receives a cancel callback</param>
  /// <param name="delayFrames">Delay before execution in frames</param>
  /// <returns>ActionId for controlling the scheduled action</returns>
  public static ActionId DelayedFrames(Action<Action> action, int delayFrames) {
    var scheduledAction = new ScheduledAction(action, ActionType.DelayedFrames, frameInterval: delayFrames);
    AddActionInternal(scheduledAction);
    return scheduledAction.Id;
  }

  /// <summary>
  /// Schedules a simple action to execute once after a frame delay.
  /// </summary>
  /// <param name="action">Action to execute</param>
  /// <param name="delayFrames">Delay before execution in frames</param>
  /// <returns>ActionId for controlling the scheduled action</returns>
  public static ActionId DelayedFrames(Action action, int delayFrames) {
    var scheduledAction = new ScheduledAction(action, ActionType.DelayedFrames, frameInterval: delayFrames);
    AddActionInternal(scheduledAction);
    return scheduledAction.Id;
  }

  #endregion

  #region Control Methods

  /// <summary>
  /// Cancels and removes a scheduled action immediately.
  /// </summary>
  /// <param name="actionId">ID of the action to cancel</param>
  /// <returns>True if action was found and cancelled, false otherwise</returns>
  public static bool CancelAction(ActionId actionId) {
    lock (_scheduledActions) {
      // Use cache for O(1) lookup - PERFORMANCE OPTIMIZATION
      if (_actionLookup.TryGetValue(actionId, out var action)) {
        var removed = _scheduledActions.Remove(action);
        if (removed) {
          _actionLookup.Remove(actionId);
        }
        return removed;
      }
      return false;
    }
  }

  /// <summary>
  /// Pauses a scheduled action without removing it.
  /// The action can be resumed later and will continue from where it left off.
  /// </summary>
  /// <param name="actionId">ID of the action to pause</param>
  public static void PauseAction(ActionId actionId) {
    lock (_scheduledActions) {
      // Use cache for O(1) lookup - PERFORMANCE OPTIMIZATION
      if (_actionLookup.TryGetValue(actionId, out var action)) {
        action.IsActive = false;
      }
    }
  }

  /// <summary>
  /// Resumes a paused action.
  /// The action will continue executing according to its original schedule.
  /// </summary>
  /// <param name="actionId">ID of the action to resume</param>
  public static void ResumeAction(ActionId actionId) {
    lock (_scheduledActions) {
      // Use cache for O(1) lookup - PERFORMANCE OPTIMIZATION
      if (_actionLookup.TryGetValue(actionId, out var action)) {
        action.IsActive = true;
      }
    }
  }

  /// <summary>
  /// Removes all scheduled actions and clears the executor.
  /// Use with caution as this will cancel all pending actions.
  /// </summary>
  public static void ClearAllActions() {
    lock (_scheduledActions) {
      _scheduledActions.Clear();
      _actionLookup.Clear(); // Clear cache too - PERFORMANCE OPTIMIZATION
    }
    _actionsToRemove.Clear();
  }

  #endregion

  #region Utility Properties and Methods

  /// <summary>Gets the total number of currently scheduled actions</summary>
  public static int Count => _scheduledActions.Count;

  /// <summary>
  /// Gets the number of times a specific action has been executed.
  /// </summary>
  /// <param name="actionId">ID of the action to check</param>
  /// <returns>Execution count, or 0 if action not found</returns>
  public static int GetExecutionCount(ActionId actionId) {
    // Use cache for O(1) lookup - PERFORMANCE OPTIMIZATION
    if (_actionLookup.TryGetValue(actionId, out var action)) {
      return action.ExecutionCount;
    }
    return 0;
  }

  /// <summary>
  /// Gets the maximum execution limit for a specific action.
  /// </summary>
  /// <param name="actionId">ID of the action to check</param>
  /// <returns>Maximum executions, or -1 if action not found or has no limit</returns>
  public static int GetMaxExecutions(ActionId actionId) {
    // Use cache for O(1) lookup - PERFORMANCE OPTIMIZATION
    if (_actionLookup.TryGetValue(actionId, out var action)) {
      return action.MaxExecutions;
    }
    return -1;
  }

  /// <summary>
  /// Gets the number of remaining executions for a specific action.
  /// </summary>
  /// <param name="actionId">ID of the action to check</param>
  /// <returns>Remaining executions, or -1 if action not found or has unlimited executions</returns>
  public static int GetRemainingExecutions(ActionId actionId) {
    // Use cache for O(1) lookup - PERFORMANCE OPTIMIZATION
    if (_actionLookup.TryGetValue(actionId, out var action)) {
      if (action.MaxExecutions <= 0) return -1;
      return Math.Max(0, action.MaxExecutions - action.ExecutionCount);
    }
    return -1;
  }

  #endregion

  #region Sequence Support

  /// <summary>
  /// Creates a new action sequence builder for chaining actions and delays.
  /// </summary>
  /// <returns>New ActionSequence for building sequential actions</returns>
  public static ActionSequence CreateSequence() {
    return new ActionSequence();
  }

  /// <summary>
  /// Cancels a running sequence by its ActionId.
  /// </summary>
  /// <param name="sequenceId">ID of the sequence to cancel</param>
  /// <returns>True if sequence was found and cancelled</returns>
  public static bool CancelSequence(ActionId sequenceId) {
    return CancelAction(sequenceId);
  }

  #endregion

  #region Random Interval Methods

  /// <summary>
  /// Schedules an action to execute repeatedly at random time intervals with cancel support.
  /// </summary>
  /// <param name="action">Action that receives a cancel callback</param>
  /// <param name="minIntervalSeconds">Minimum time interval between executions in seconds</param>
  /// <param name="maxIntervalSeconds">Maximum time interval between executions in seconds</param>
  /// <param name="maxExecutions">Maximum number of executions (-1 for infinite)</param>
  /// <returns>ActionId for controlling the scheduled action</returns>
  public static ActionId RepeatingRandom(Action<Action> action, float minIntervalSeconds, float maxIntervalSeconds, int maxExecutions = -1) {
    var scheduledAction = new ScheduledAction(action, ActionType.RepeatingSeconds, GetRandomInterval(minIntervalSeconds, maxIntervalSeconds), maxExecutions: maxExecutions);
    scheduledAction.MinInterval = minIntervalSeconds;
    scheduledAction.MaxInterval = maxIntervalSeconds;
    AddActionInternal(scheduledAction);
    return scheduledAction.Id;
  }

  /// <summary>
  /// Schedules a simple action to execute repeatedly at random time intervals.
  /// </summary>
  /// <param name="action">Action to execute</param>
  /// <param name="minIntervalSeconds">Minimum time interval between executions in seconds</param>
  /// <param name="maxIntervalSeconds">Maximum time interval between executions in seconds</param>
  /// <param name="maxExecutions">Maximum number of executions (-1 for infinite)</param>
  /// <returns>ActionId for controlling the scheduled action</returns>
  public static ActionId RepeatingRandom(Action action, float minIntervalSeconds, float maxIntervalSeconds, int maxExecutions = -1) {
    var scheduledAction = new ScheduledAction(action, ActionType.RepeatingSeconds, GetRandomInterval(minIntervalSeconds, maxIntervalSeconds), maxExecutions: maxExecutions);
    scheduledAction.MinInterval = minIntervalSeconds;
    scheduledAction.MaxInterval = maxIntervalSeconds;
    AddActionInternal(scheduledAction);
    return scheduledAction.Id;
  }

  /// <summary>
  /// Helper method to generate a random interval between min and max values.
  /// </summary>
  /// <param name="minInterval">Minimum interval value</param>
  /// <param name="maxInterval">Maximum interval value</param>
  /// <returns>Random interval between min and max</returns>
  private static float GetRandomInterval(float minInterval, float maxInterval) {
    return UnityEngine.Random.Range(minInterval, maxInterval);
  }

  #endregion
}

/// <summary>
/// Represents a step in an action sequence with its type and parameters.
/// </summary>
public class SequenceStep {
  public enum StepType {
    Action,
    ActionWithCancel,
    WaitSeconds,
    WaitFrames,
    WaitRandomSeconds
  }

  public StepType Type { get; set; }
  public Action Action { get; set; }
  public Action<Action> ActionWithCancel { get; set; }
  public float WaitSeconds { get; set; }
  public int WaitFrames { get; set; }
  public float MinWaitSeconds { get; set; }
  public float MaxWaitSeconds { get; set; }
}

/// <summary>
/// Builder class for creating sequential action chains with delays and conditional cancellation.
/// Provides a fluent interface for building complex action sequences.
/// </summary>
public class ActionSequence {
  private readonly List<SequenceStep> _steps = new();
  private int _currentStepIndex = 0;
  private float _waitUntilTime;
  private int _waitUntilFrame;
  private bool _isWaiting = false;
  private bool _isCancelled = false;
  private ActionId _actionId;

  /// <summary>
  /// Adds a simple action to the sequence.
  /// </summary>
  /// <param name="action">Action to execute</param>
  /// <returns>This sequence for method chaining</returns>
  public ActionSequence Then(Action action) {
    _steps.Add(new SequenceStep {
      Type = SequenceStep.StepType.Action,
      Action = action
    });
    return this;
  }

  /// <summary>
  /// Adds an action with cancel support to the sequence.
  /// The action can call the cancel callback to stop the entire sequence.
  /// </summary>
  /// <param name="action">Action that receives a cancel callback</param>
  /// <returns>This sequence for method chaining</returns>
  public ActionSequence Then(Action<Action> action) {
    _steps.Add(new SequenceStep {
      Type = SequenceStep.StepType.ActionWithCancel,
      ActionWithCancel = action
    });
    return this;
  }

  /// <summary>
  /// Adds a time-based wait to the sequence.
  /// </summary>
  /// <param name="seconds">Time to wait in seconds</param>
  /// <returns>This sequence for method chaining</returns>
  public ActionSequence ThenWait(float seconds) {
    _steps.Add(new SequenceStep {
      Type = SequenceStep.StepType.WaitSeconds,
      WaitSeconds = seconds
    });
    return this;
  }

  /// <summary>
  /// Adds a frame-based wait to the sequence.
  /// </summary>
  /// <param name="frames">Number of frames to wait</param>
  /// <returns>This sequence for method chaining</returns>
  public ActionSequence ThenWaitFrames(int frames) {
    _steps.Add(new SequenceStep {
      Type = SequenceStep.StepType.WaitFrames,
      WaitFrames = frames
    });
    return this;
  }

  /// <summary>
  /// Adds a random time-based wait to the sequence.
  /// </summary>
  /// <param name="minSeconds">Minimum time to wait in seconds</param>
  /// <param name="maxSeconds">Maximum time to wait in seconds</param>
  /// <returns>This sequence for method chaining</returns>
  public ActionSequence ThenWaitRandom(float minSeconds, float maxSeconds) {
    _steps.Add(new SequenceStep {
      Type = SequenceStep.StepType.WaitRandomSeconds,
      MinWaitSeconds = minSeconds,
      MaxWaitSeconds = maxSeconds
    });
    return this;
  }

  /// <summary>
  /// Starts executing the sequence.
  /// </summary>
  /// <returns>ActionId for controlling the sequence execution</returns>
  public ActionId Execute() {
    // Reset sequence state
    _currentStepIndex = 0;
    _isWaiting = false;
    _isCancelled = false;

    // Schedule the sequence execution using OncePerFrame
    _actionId = ActionScheduler.OncePerFrame(cancel => ExecuteNextStep(cancel));
    return _actionId;
  }

  /// <summary>
  /// Executes the next step in the sequence or handles waiting.
  /// </summary>
  /// <param name="cancelSequence">Callback to cancel the entire sequence</param>
  private void ExecuteNextStep(Action cancelSequence) {
    // Check if sequence was cancelled
    if (_isCancelled) {
      cancelSequence();
      return;
    }

    // Handle waiting states
    if (_isWaiting) {
      var currentStep = _steps[_currentStepIndex]; if (currentStep.Type == SequenceStep.StepType.WaitSeconds) {
        // Check if time-based wait is complete
        if (Time.time >= _waitUntilTime) {
          _isWaiting = false;
          _currentStepIndex++;
        }
      } else if (currentStep.Type == SequenceStep.StepType.WaitFrames) {
        // Check if frame-based wait is complete
        if (Time.frameCount >= _waitUntilFrame) {
          _isWaiting = false;
          _currentStepIndex++;
        }
      } else if (currentStep.Type == SequenceStep.StepType.WaitRandomSeconds) {
        // Check if random time-based wait is complete
        if (Time.time >= _waitUntilTime) {
          _isWaiting = false;
          _currentStepIndex++;
        }
      }
      return; // Continue waiting
    }

    // Check if sequence is complete
    if (_currentStepIndex >= _steps.Count) {
      cancelSequence(); // Sequence finished
      return;
    }

    // Execute current step
    var step = _steps[_currentStepIndex];

    try {
      switch (step.Type) {
        case SequenceStep.StepType.Action:
          // Execute simple action and move to next step
          step.Action?.Invoke();
          _currentStepIndex++;
          break;

        case SequenceStep.StepType.ActionWithCancel:
          // Execute action with cancel support
          step.ActionWithCancel?.Invoke(() => {
            _isCancelled = true; // Cancel entire sequence
          });
          _currentStepIndex++;
          break;

        case SequenceStep.StepType.WaitSeconds:
          // Start time-based wait
          _waitUntilTime = Time.time + step.WaitSeconds;
          _isWaiting = true;
          break;
        case SequenceStep.StepType.WaitFrames:
          // Start frame-based wait
          _waitUntilFrame = Time.frameCount + step.WaitFrames;
          _isWaiting = true;
          break;

        case SequenceStep.StepType.WaitRandomSeconds:
          // Start random time-based wait
          var randomWait = UnityEngine.Random.Range(step.MinWaitSeconds, step.MaxWaitSeconds);
          _waitUntilTime = Time.time + randomWait;
          _isWaiting = true;
          break;
      }
    } catch (Exception ex) {
      // Log errors and cancel sequence on exception
      Log.Error($"Error executing sequence step {_currentStepIndex}: {ex}");
      cancelSequence();
    }
  }

  /// <summary>
  /// Cancels the sequence execution.
  /// </summary>
  public void Cancel() {
    _isCancelled = true;
  }

  /// <summary>
  /// Gets the current step index in the sequence.
  /// </summary>
  public int CurrentStepIndex => _currentStepIndex;

  /// <summary>
  /// Gets the total number of steps in the sequence.
  /// </summary>
  public int TotalSteps => _steps.Count;

  /// <summary>
  /// Gets whether the sequence is currently in a waiting state.
  /// </summary>
  public bool IsWaiting => _isWaiting;

  /// <summary>
  /// Gets whether the sequence has been cancelled.
  /// </summary>
  public bool IsCancelled => _isCancelled;

  /// <summary>
  /// Gets whether the sequence has completed all steps.
  /// </summary>
  public bool IsComplete => _currentStepIndex >= _steps.Count && !_isWaiting;
}
