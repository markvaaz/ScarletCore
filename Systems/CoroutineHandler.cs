using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using ProjectM.Physics;
using UnityEngine;
using ScarletCore.Utils;

namespace ScarletCore.Systems;

/// <summary>
/// Represents a unique identifier for a managed coroutine.
/// </summary>
public readonly record struct CoroutineId(int Value) {
  private static int _nextId = 0;

  /// <summary>
  /// Generates the next unique CoroutineId.
  /// </summary>
  /// <returns>A new unique CoroutineId</returns>
  public static CoroutineId Next() => new(Interlocked.Increment(ref _nextId));
}

/// <summary>
/// Represents a managed coroutine with additional metadata and control capabilities.
/// </summary>
public class ManagedCoroutine {
  /// <summary>
  /// The Unity Coroutine instance.
  /// </summary>
  public Coroutine Coroutine { get; set; }

  /// <summary>
  /// The unique identifier for this coroutine.
  /// </summary>
  public CoroutineId Id { get; set; }

  /// <summary>
  /// Indicates whether the coroutine is currently active.
  /// </summary>
  public bool IsActive { get; set; } = true;

  /// <summary>
  /// Optional name for the coroutine for debugging purposes.
  /// </summary>
  public string Name { get; set; }

  /// <summary>
  /// Optional callback to execute when the coroutine completes.
  /// </summary>
  public Action OnComplete { get; set; }

  /// <summary>
  /// Maximum number of executions allowed (-1 for infinite).
  /// </summary>
  public int MaxExecutions { get; set; } = -1;
  /// <summary>
  /// Current number of executions completed.
  /// </summary>
  public int ExecutionCount { get; set; } = 0;

  /// <summary>
  /// Indicates if coroutine should pause execution (checked during yield points).
  /// </summary>
  public bool IsPaused { get; set; } = false;

  /// <summary>
  /// Timestamp when coroutine was paused (for resume calculations).
  /// </summary>
  public DateTime? PausedAt { get; set; }

  /// <summary>
  /// Initializes a new instance of the ManagedCoroutine class.
  /// </summary>
  /// <param name="coroutine">The Unity Coroutine instance</param>
  /// <param name="id">The unique identifier</param>
  /// <param name="name">Optional name for debugging</param>
  /// <param name="maxExecutions">Maximum executions allowed (-1 for infinite)</param>
  public ManagedCoroutine(Coroutine coroutine, CoroutineId id, string name = null, int maxExecutions = -1) {
    Coroutine = coroutine;
    Id = id;
    Name = name ?? $"Coroutine_{id.Value}";
    MaxExecutions = maxExecutions;
  }
}

/// <summary>
/// Provides a comprehensive coroutine management system with tracking, control, and cancellation capabilities.
/// Uses Unity's GameObject-based coroutine system while providing advanced management features.
/// </summary>
public class CoroutineHandler {
  private static GameObject _gameObject;
  private static IgnorePhysicsDebugSystem _coroutineManager;
  private static readonly Dictionary<CoroutineId, ManagedCoroutine> _managedCoroutines = new();

  /// <summary>
  /// Gets or creates the coroutine manager GameObject and component.
  /// Ensures the GameObject persists across scene changes.
  /// </summary>
  /// <returns>The IgnorePhysicsDebugSystem component used for coroutine management</returns>
  private static IgnorePhysicsDebugSystem GetCoroutineManager() {
    if (_gameObject == null) {
      _gameObject = new GameObject("ScarletCore_CoroutineHandler");
      UnityEngine.Object.DontDestroyOnLoad(_gameObject);
    }

    if (_coroutineManager == null) {
      _coroutineManager = _gameObject.AddComponent<IgnorePhysicsDebugSystem>();
    }

    return _coroutineManager;
  }
  /// <summary>
  /// Helper method to register and start a managed coroutine with tracking capabilities.
  /// </summary>
  /// <param name="coroutineEnumerator">The coroutine enumerator to execute</param>
  /// <param name="name">Optional name for debugging purposes</param>
  /// <param name="onComplete">Optional callback when coroutine completes</param>
  /// <param name="maxExecutions">Maximum executions allowed (-1 for infinite)</param>
  /// <returns>The unique CoroutineId for the started coroutine</returns>
  private static CoroutineId StartManagedCoroutine(IEnumerator coroutineEnumerator, string name = null, Action onComplete = null, int maxExecutions = -1) {
    var id = CoroutineId.Next();
    var managedCoroutine = new ManagedCoroutine(null, id, name, maxExecutions) { OnComplete = onComplete }; _managedCoroutines[id] = managedCoroutine;

    // Wrapper that updates ExecutionCount and removes from tracking when completed
    var wrappedCoroutine = WrapCoroutineWithTracking(coroutineEnumerator, id);
    var coroutine = GetCoroutineManager().StartCoroutine(wrappedCoroutine.WrapToIl2Cpp());
    managedCoroutine.Coroutine = coroutine;

    return id;
  }  /// <summary>
     /// Wraps a coroutine to provide execution tracking, pause/resume support, and automatic cleanup.
     /// </summary>
  private static IEnumerator WrapCoroutineWithTracking(IEnumerator originalCoroutine, CoroutineId id) {
    try {
      while (originalCoroutine.MoveNext()) {
        // Check for pause state before yielding
        if (_managedCoroutines.TryGetValue(id, out var managedCoroutine)) {
          while (managedCoroutine.IsPaused && managedCoroutine.IsActive) {
            yield return new WaitForSeconds(0.1f); // Check pause state every 100ms
          }

          // If coroutine was stopped while paused, exit
          if (!managedCoroutine.IsActive) {
            break;
          }
        }

        yield return originalCoroutine.Current;
      }
    } finally {
      // Cleanup when coroutine completes
      if (_managedCoroutines.TryGetValue(id, out var managedCoroutine)) {
        managedCoroutine.OnComplete?.Invoke();
        _managedCoroutines.Remove(id);
      }
    }
  }

  /// <summary>
  /// Starts a one-time coroutine that executes an action after a specified delay.
  /// </summary>
  /// <param name="action">The action to execute</param>
  /// <param name="delay">Delay in seconds before execution</param>
  /// <param name="name">Optional name for debugging</param>
  /// <returns>CoroutineId for management purposes</returns>
  public static CoroutineId StartGeneric(Action action, float delay, string name = null) {
    return StartManagedCoroutine(GenericCoroutine(action, delay), name);
  }

  /// <summary>
  /// Internal coroutine implementation for delayed execution.
  /// </summary>
  private static IEnumerator GenericCoroutine(Action action, float delay) {
    yield return new WaitForSeconds(delay);
    action?.Invoke();
  }  /// <summary>
     /// Starts a coroutine that repeats an action indefinitely at specified intervals.
     /// </summary>
     /// <param name="action">The action to execute repeatedly</param>
     /// <param name="delay">Interval in seconds between executions</param>
     /// <param name="name">Optional name for debugging</param>
     /// <param name="maxExecutions">Maximum number of executions (-1 for infinite)</param>
     /// <returns>CoroutineId for management purposes</returns>
  public static CoroutineId StartRepeating(Action action, float delay, string name = null, int maxExecutions = -1) {
    var id = CoroutineId.Next();
    var managedCoroutine = new ManagedCoroutine(null, id, name, maxExecutions);
    _managedCoroutines[id] = managedCoroutine;

    var wrappedCoroutine = WrapCoroutineWithTracking(RepeatingCoroutineWithMax(action, delay, maxExecutions, id), id);
    var coroutine = GetCoroutineManager().StartCoroutine(wrappedCoroutine.WrapToIl2Cpp());
    managedCoroutine.Coroutine = coroutine;

    return id;
  }
  /// <summary>
  /// Starts a coroutine that repeats an action indefinitely with cancellation support.
  /// The action receives a cancel callback that can be used to stop the coroutine.
  /// </summary>
  /// <param name="actionWithCancel">The action to execute with cancel callback</param>
  /// <param name="delay">Interval in seconds between executions</param>
  /// <param name="name">Optional name for debugging</param>
  /// <param name="maxExecutions">Maximum number of executions (-1 for infinite)</param>
  /// <returns>CoroutineId for management purposes</returns>
  public static CoroutineId StartRepeating(Action<Action> actionWithCancel, float delay, string name = null, int maxExecutions = -1) {
    var id = CoroutineId.Next();
    var managedCoroutine = new ManagedCoroutine(null, id, name, maxExecutions);
    _managedCoroutines[id] = managedCoroutine;

    var wrappedCoroutine = WrapCoroutineWithTracking(RepeatingCoroutineWithCancelAndMax(actionWithCancel, delay, maxExecutions, id), id);
    var coroutine = GetCoroutineManager().StartCoroutine(wrappedCoroutine.WrapToIl2Cpp());
    managedCoroutine.Coroutine = coroutine;

    return id;
  }/// <summary>
   /// Internal coroutine implementation for repeating execution with max executions.
   /// </summary>
  private static IEnumerator RepeatingCoroutineWithMax(Action action, float delay, int maxExecutions, CoroutineId id) {
    int executionCount = 0;
    while (maxExecutions < 0 || executionCount < maxExecutions) {
      yield return new WaitForSeconds(delay);
      action?.Invoke();
      executionCount++;

      // Update the managed coroutine's execution count
      if (_managedCoroutines.TryGetValue(id, out var managedCoroutine)) {
        managedCoroutine.ExecutionCount = executionCount;
      }
    }
  }
  /// <summary>
  /// Internal coroutine implementation for repeating execution with cancellation and max executions.
  /// </summary>
  private static IEnumerator RepeatingCoroutineWithCancelAndMax(Action<Action> action, float delay, int maxExecutions, CoroutineId id) {
    bool shouldCancel = false;
    Action cancelAction = () => shouldCancel = true;
    int executionCount = 0;

    while (!shouldCancel && (maxExecutions < 0 || executionCount < maxExecutions)) {
      yield return new WaitForSeconds(delay);
      if (!shouldCancel) {
        action?.Invoke(cancelAction);
        executionCount++;

        // Update the managed coroutine's execution count
        if (_managedCoroutines.TryGetValue(id, out var managedCoroutine)) {
          managedCoroutine.ExecutionCount = executionCount;
        }
      }
    }
  }

  /// <summary>
  /// Starts a coroutine that repeats an action for a specified number of times.
  /// </summary>
  /// <param name="action">The action to execute</param>
  /// <param name="delay">Interval in seconds between executions</param>
  /// <param name="repeatCount">Number of times to repeat the action</param>
  /// <param name="name">Optional name for debugging</param>
  /// <param name="onComplete">Optional callback when all repetitions complete</param>
  /// <returns>CoroutineId for management purposes</returns>
  public static CoroutineId StartRepeating(Action action, float delay, int repeatCount, string name = null, Action onComplete = null) {
    return StartManagedCoroutine(RepeatingCoroutine(action, delay, repeatCount), name, onComplete);
  }

  /// <summary>
  /// Internal coroutine implementation for limited repeating execution.
  /// </summary>
  private static IEnumerator RepeatingCoroutine(Action action, float delay, int repeatCount) {
    for (int i = 0; i < repeatCount; i++) {
      yield return new WaitForSeconds(delay);
      action?.Invoke();
    }
  }
  /// <summary>
  /// Starts a coroutine that repeats an action indefinitely at specified frame intervals.
  /// </summary>
  /// <param name="action">The action to execute</param>
  /// <param name="frameInterval">Number of frames to wait between executions</param>
  /// <param name="name">Optional name for debugging</param>
  /// <param name="maxExecutions">Maximum number of executions (-1 for infinite)</param>
  /// <returns>CoroutineId for management purposes</returns>
  public static CoroutineId StartFrameRepeating(Action action, int frameInterval, string name = null, int maxExecutions = -1) {
    var id = CoroutineId.Next();
    var managedCoroutine = new ManagedCoroutine(null, id, name, maxExecutions);
    _managedCoroutines[id] = managedCoroutine;

    var wrappedCoroutine = WrapCoroutineWithTracking(FrameCoroutineWithMax(action, frameInterval, maxExecutions, id), id);
    var coroutine = GetCoroutineManager().StartCoroutine(wrappedCoroutine.WrapToIl2Cpp());
    managedCoroutine.Coroutine = coroutine;

    return id;
  }
  /// <summary>
  /// Starts a coroutine that repeats an action indefinitely at specified frame intervals with cancellation support.
  /// </summary>
  /// <param name="actionWithCancel">The action to execute with cancel callback</param>
  /// <param name="frameInterval">Number of frames to wait between executions</param>
  /// <param name="name">Optional name for debugging</param>
  /// <param name="maxExecutions">Maximum number of executions (-1 for infinite)</param>
  /// <returns>CoroutineId for management purposes</returns>
  public static CoroutineId StartFrameRepeating(Action<Action> actionWithCancel, int frameInterval, string name = null, int maxExecutions = -1) {
    var id = CoroutineId.Next();
    var managedCoroutine = new ManagedCoroutine(null, id, name, maxExecutions);
    _managedCoroutines[id] = managedCoroutine;

    var wrappedCoroutine = WrapCoroutineWithTracking(FrameCoroutineWithCancelAndMax(actionWithCancel, frameInterval, maxExecutions, id), id);
    var coroutine = GetCoroutineManager().StartCoroutine(wrappedCoroutine.WrapToIl2Cpp());
    managedCoroutine.Coroutine = coroutine;

    return id;
  }
  /// <summary>
  /// Internal coroutine implementation for frame-based repeating execution with max executions.
  /// </summary>
  private static IEnumerator FrameCoroutineWithMax(Action action, int frameInterval, int maxExecutions, CoroutineId id) {
    int executionCount = 0;
    while (maxExecutions < 0 || executionCount < maxExecutions) {
      for (int i = 0; i < frameInterval; i++) {
        yield return null; // Wait one frame
      }
      action?.Invoke();
      executionCount++;

      // Update the managed coroutine's execution count
      if (_managedCoroutines.TryGetValue(id, out var managedCoroutine)) {
        managedCoroutine.ExecutionCount = executionCount;
      }
    }
  }
  /// <summary>
  /// Internal coroutine implementation for frame-based repeating execution with cancellation and max executions.
  /// </summary>
  private static IEnumerator FrameCoroutineWithCancelAndMax(Action<Action> action, int frameInterval, int maxExecutions, CoroutineId id) {
    bool shouldCancel = false;
    Action cancelAction = () => shouldCancel = true;
    int executionCount = 0;

    while (!shouldCancel && (maxExecutions < 0 || executionCount < maxExecutions)) {
      for (int i = 0; i < frameInterval && !shouldCancel; i++) {
        yield return null; // Wait one frame
      }
      if (!shouldCancel) {
        action?.Invoke(cancelAction);
        executionCount++;

        // Update the managed coroutine's execution count
        if (_managedCoroutines.TryGetValue(id, out var managedCoroutine)) {
          managedCoroutine.ExecutionCount = executionCount;
        }
      }
    }
  }
  /// <summary>
  /// Starts a coroutine that repeats an action indefinitely with random intervals between executions.
  /// </summary>
  /// <param name="action">The action to execute</param>
  /// <param name="minDelay">Minimum delay in seconds</param>
  /// <param name="maxDelay">Maximum delay in seconds</param>
  /// <param name="name">Optional name for debugging</param>
  /// <param name="maxExecutions">Maximum number of executions (-1 for infinite)</param>
  /// <returns>CoroutineId for management purposes</returns>
  public static CoroutineId StartRandomInterval(Action action, float minDelay, float maxDelay, string name = null, int maxExecutions = -1) {
    var id = CoroutineId.Next();
    var managedCoroutine = new ManagedCoroutine(null, id, name, maxExecutions);
    _managedCoroutines[id] = managedCoroutine;

    var wrappedCoroutine = WrapCoroutineWithTracking(RandomIntervalCoroutineWithMax(action, minDelay, maxDelay, maxExecutions, id), id);
    var coroutine = GetCoroutineManager().StartCoroutine(wrappedCoroutine.WrapToIl2Cpp());
    managedCoroutine.Coroutine = coroutine;

    return id;
  }
  /// <summary>
  /// Starts a coroutine that repeats an action indefinitely with random intervals and cancellation support.
  /// </summary>
  /// <param name="actionWithCancel">The action to execute with cancel callback</param>
  /// <param name="minDelay">Minimum delay in seconds</param>
  /// <param name="maxDelay">Maximum delay in seconds</param>
  /// <param name="name">Optional name for debugging</param>
  /// <param name="maxExecutions">Maximum number of executions (-1 for infinite)</param>
  /// <returns>CoroutineId for management purposes</returns>
  public static CoroutineId StartRandomInterval(Action<Action> actionWithCancel, float minDelay, float maxDelay, string name = null, int maxExecutions = -1) {
    var id = CoroutineId.Next();
    var managedCoroutine = new ManagedCoroutine(null, id, name, maxExecutions);
    _managedCoroutines[id] = managedCoroutine;

    var wrappedCoroutine = WrapCoroutineWithTracking(RandomIntervalCoroutineWithCancelAndMax(actionWithCancel, minDelay, maxDelay, maxExecutions, id), id);
    var coroutine = GetCoroutineManager().StartCoroutine(wrappedCoroutine.WrapToIl2Cpp());
    managedCoroutine.Coroutine = coroutine;

    return id;
  }
  /// <summary>
  /// Internal coroutine implementation for random interval execution with max executions.
  /// </summary>
  private static IEnumerator RandomIntervalCoroutineWithMax(Action action, float minDelay, float maxDelay, int maxExecutions, CoroutineId id) {
    int executionCount = 0;
    while (maxExecutions < 0 || executionCount < maxExecutions) {
      float delay = UnityEngine.Random.Range(minDelay, maxDelay);
      yield return new WaitForSeconds(delay);
      action?.Invoke();
      executionCount++;

      // Update the managed coroutine's execution count
      if (_managedCoroutines.TryGetValue(id, out var managedCoroutine)) {
        managedCoroutine.ExecutionCount = executionCount;
      }
    }
  }
  /// <summary>
  /// Internal coroutine implementation for random interval execution with cancellation and max executions.
  /// </summary>
  private static IEnumerator RandomIntervalCoroutineWithCancelAndMax(Action<Action> action, float minDelay, float maxDelay, int maxExecutions, CoroutineId id) {
    bool shouldCancel = false;
    Action cancelAction = () => shouldCancel = true;
    int executionCount = 0;

    while (!shouldCancel && (maxExecutions < 0 || executionCount < maxExecutions)) {
      float delay = UnityEngine.Random.Range(minDelay, maxDelay);
      yield return new WaitForSeconds(delay);
      if (!shouldCancel) {
        action?.Invoke(cancelAction);
        executionCount++;

        // Update the managed coroutine's execution count
        if (_managedCoroutines.TryGetValue(id, out var managedCoroutine)) {
          managedCoroutine.ExecutionCount = executionCount;
        }
      }
    }
  }

  /// <summary>
  /// Schedules an action to execute on the next frame.
  /// </summary>
  /// <param name="action">The action to execute</param>
  /// <param name="name">Optional name for debugging</param>
  /// <returns>CoroutineId for management purposes</returns>
  public static CoroutineId NextFrame(Action action, string name = null) {
    return StartManagedCoroutine(NextFrameCoroutine(action), name);
  }

  /// <summary>
  /// Internal coroutine implementation for next frame execution.
  /// </summary>
  private static IEnumerator NextFrameCoroutine(Action action) {
    yield return null; // Wait one frame
    action?.Invoke();
  }

  // Management and Control Methods

  /// <summary>
  /// Stops a specific coroutine by its ID and removes it from tracking.
  /// </summary>
  /// <param name="id">The CoroutineId of the coroutine to stop</param>
  /// <returns>True if the coroutine was found and stopped, false otherwise</returns>
  public static bool StopCoroutine(CoroutineId id) {
    if (_managedCoroutines.TryGetValue(id, out var managedCoroutine)) {
      if (managedCoroutine.Coroutine != null && _coroutineManager != null) {
        _coroutineManager.StopCoroutine(managedCoroutine.Coroutine);
      }
      managedCoroutine.OnComplete?.Invoke();
      return _managedCoroutines.Remove(id);
    }
    return false;
  }
  /// <summary>
  /// Pauses a specific coroutine by setting its pause state.
  /// The coroutine will pause at the next yield point and remain paused until resumed.
  /// </summary>
  /// <param name="id">The CoroutineId of the coroutine to pause</param>
  public static void PauseCoroutine(CoroutineId id) {
    if (_managedCoroutines.TryGetValue(id, out var managedCoroutine)) {
      managedCoroutine.IsPaused = true;
      managedCoroutine.PausedAt = DateTime.UtcNow;
    }
  }

  /// <summary>
  /// Resumes a specific coroutine by clearing its pause state.
  /// The coroutine will resume execution at the next yield point.
  /// </summary>
  /// <param name="id">The CoroutineId of the coroutine to resume</param>
  public static void ResumeCoroutine(CoroutineId id) {
    if (_managedCoroutines.TryGetValue(id, out var managedCoroutine)) {
      managedCoroutine.IsPaused = false;
      managedCoroutine.PausedAt = null;
    }
  }

  /// <summary>
  /// Stops all managed coroutines and clears the tracking dictionary.
  /// </summary>
  public static void StopAllCoroutines() {
    if (_coroutineManager != null) {
      _coroutineManager.StopAllCoroutines();
    }
    foreach (var coroutine in _managedCoroutines.Values) {
      coroutine.OnComplete?.Invoke();
    }
    _managedCoroutines.Clear();
  }

  /// <summary>
  /// Checks if a specific coroutine is currently running and active.
  /// </summary>
  /// <param name="id">The CoroutineId to check</param>
  /// <returns>True if the coroutine exists and is marked as active</returns>
  public static bool IsCoroutineRunning(CoroutineId id) {
    return _managedCoroutines.ContainsKey(id) && _managedCoroutines[id].IsActive;
  }

  /// <summary>
  /// Gets the current number of active managed coroutines.
  /// </summary>
  public static int ActiveCoroutineCount => _managedCoroutines.Count;
  /// <summary>
  /// Gets the names of all active managed coroutines for debugging purposes.
  /// </summary>
  /// <returns>An array of coroutine names</returns>
  public static string[] GetActiveCoroutineNames() {
    var names = new string[_managedCoroutines.Count];
    int index = 0;
    foreach (var coroutine in _managedCoroutines.Values) {
      names[index++] = coroutine.Name;
    }
    return names;
  }

  // New utility methods
  /// <summary>
  /// Gets the current execution count for a specific coroutine.
  /// </summary>
  /// <param name="id">The CoroutineId to check</param>
  /// <returns>The current execution count, or 0 if not found</returns>
  public static int GetExecutionCount(CoroutineId id) {
    return _managedCoroutines.TryGetValue(id, out var coroutine) ? coroutine.ExecutionCount : 0;
  }

  /// <summary>
  /// Gets the maximum executions allowed for a specific coroutine.
  /// </summary>
  /// <param name="id">The CoroutineId to check</param>
  /// <returns>The maximum executions (-1 for infinite), or -1 if not found</returns>
  public static int GetMaxExecutions(CoroutineId id) {
    return _managedCoroutines.TryGetValue(id, out var coroutine) ? coroutine.MaxExecutions : -1;
  }

  /// <summary>
  /// Gets the remaining executions for a specific coroutine.
  /// </summary>
  /// <param name="id">The CoroutineId to check</param>
  /// <returns>The remaining executions, or -1 if infinite or not found</returns>
  public static int GetRemainingExecutions(CoroutineId id) {
    if (_managedCoroutines.TryGetValue(id, out var coroutine)) {
      if (coroutine.MaxExecutions <= 0) return -1;
      return Math.Max(0, coroutine.MaxExecutions - coroutine.ExecutionCount);
    }
    return -1;
  }
  /// <summary>
  /// Cleanup method to remove completed coroutines (call periodically if needed).
  /// This method should be called occasionally to prevent memory leaks from coroutines
  /// that may have completed without proper cleanup.
  /// </summary>
  /// <returns>The number of coroutines that were cleaned up</returns>
  public static int CleanupCompletedCoroutines() {
    var toRemove = new List<CoroutineId>();
    var cleanupCount = 0;

    foreach (var kvp in _managedCoroutines) {
      var id = kvp.Key;
      var managedCoroutine = kvp.Value;
      bool shouldRemove = false;

      // Strategy 1: Check if Unity Coroutine reference is null (completed/stopped)
      if (managedCoroutine.Coroutine == null) {
        shouldRemove = true;
      }

      // Strategy 2: Check if coroutine has reached max executions
      else if (managedCoroutine.MaxExecutions > 0 &&
               managedCoroutine.ExecutionCount >= managedCoroutine.MaxExecutions) {
        shouldRemove = true;
      }      // Strategy 3: Try to detect if coroutine manager is null/destroyed (but only if GameObject is also null)
      else if (_coroutineManager == null && _gameObject == null) {
        shouldRemove = true;
      }

      if (shouldRemove) {
        toRemove.Add(id);
      }
    }

    // Remove completed coroutines and invoke their completion callbacks
    foreach (var id in toRemove) {
      if (_managedCoroutines.TryGetValue(id, out var managedCoroutine)) {
        try {
          managedCoroutine.OnComplete?.Invoke();
        } catch (Exception ex) {
          // Log error but continue cleanup
          Log.Error($"Error invoking OnComplete for coroutine {managedCoroutine.Name}: {ex.Message}");
        }

        if (_managedCoroutines.Remove(id)) {
          cleanupCount++;
        }
      }
    }

    return cleanupCount;
  }
  // Auto-cleanup functionality
  private static CoroutineId? _autoCleanupId;
  private static bool _autoCleanupEnabled = false;
  private static readonly object _autoCleanupLock = new object();

  /// <summary>
  /// Enables automatic periodic cleanup of completed coroutines.
  /// </summary>
  /// <param name="intervalSeconds">Interval between cleanup runs in seconds</param>
  /// <returns>CoroutineId of the auto-cleanup coroutine</returns>
  public static CoroutineId EnableAutoCleanup(float intervalSeconds = 30.0f) {
    lock (_autoCleanupLock) {
      // Stop existing auto-cleanup if running
      if (_autoCleanupEnabled && _autoCleanupId.HasValue) {
        StopCoroutine(_autoCleanupId.Value);
        _autoCleanupEnabled = false;
        _autoCleanupId = null;
      }

      // Start new auto-cleanup
      _autoCleanupId = StartRepeating(() => {
        var cleaned = CleanupCompletedCoroutines();
        if (cleaned > 0) {
          Log.Info($"Auto-cleanup removed {cleaned} completed coroutines");
        }
      }, intervalSeconds, "AutoCleanup");

      _autoCleanupEnabled = true;
      return _autoCleanupId.Value;
    }
  }
  /// <summary>
  /// Disables automatic cleanup of completed coroutines.
  /// </summary>
  public static void DisableAutoCleanup() {
    lock (_autoCleanupLock) {
      if (_autoCleanupEnabled && _autoCleanupId.HasValue) {
        StopCoroutine(_autoCleanupId.Value);
        _autoCleanupEnabled = false;
        _autoCleanupId = null;
      }
    }
  }

  /// <summary>
  /// Gets statistics about the current state of managed coroutines.
  /// </summary>
  /// <returns>A dictionary with various statistics</returns>
  public static Dictionary<string, object> GetCoroutineStatistics() {
    var stats = new Dictionary<string, object>();
    var activeCount = 0;
    var inactiveCount = 0;
    var infiniteCount = 0;
    var limitedCount = 0;
    var totalExecutions = 0;

    foreach (var coroutine in _managedCoroutines.Values) {
      if (coroutine.IsActive) activeCount++;
      else inactiveCount++;

      if (coroutine.MaxExecutions < 0) infiniteCount++;
      else limitedCount++;

      totalExecutions += coroutine.ExecutionCount;
    }

    stats["TotalCoroutines"] = _managedCoroutines.Count;
    stats["ActiveCoroutines"] = activeCount;
    stats["InactiveCoroutines"] = inactiveCount;
    stats["InfiniteCoroutines"] = infiniteCount;
    stats["LimitedCoroutines"] = limitedCount;
    stats["TotalExecutions"] = totalExecutions;
    stats["AutoCleanupEnabled"] = _autoCleanupEnabled;

    return stats;
  }

  /// <summary>
  /// Forces cleanup of specific types of coroutines.
  /// </summary>
  /// <param name="onlyInactive">If true, only removes inactive coroutines</param>
  /// <param name="onlyCompleted">If true, only removes coroutines that have reached max executions</param>
  /// <returns>Number of coroutines removed</returns>
  public static int ForceCleanup(bool onlyInactive = false, bool onlyCompleted = true) {
    var toRemove = new List<CoroutineId>();
    var cleanupCount = 0;

    foreach (var kvp in _managedCoroutines) {
      var id = kvp.Key;
      var managedCoroutine = kvp.Value;
      bool shouldRemove = false;

      if (onlyInactive && !managedCoroutine.IsActive) {
        shouldRemove = true;
      } else if (onlyCompleted && managedCoroutine.MaxExecutions > 0 &&
                 managedCoroutine.ExecutionCount >= managedCoroutine.MaxExecutions) {
        shouldRemove = true;
      } else if (!onlyInactive && !onlyCompleted) {
        // Remove everything if no specific filter
        shouldRemove = true;
      }

      if (shouldRemove) {
        toRemove.Add(id);
      }
    }

    // Stop and remove the selected coroutines
    foreach (var id in toRemove) {
      if (StopCoroutine(id)) {
        cleanupCount++;
      }
    }

    return cleanupCount;
  }

  /// <summary>
  /// Checks if a specific coroutine is currently paused.
  /// </summary>
  /// <param name="id">The CoroutineId to check</param>
  /// <returns>True if the coroutine exists and is paused</returns>
  public static bool IsCoroutinePaused(CoroutineId id) {
    return _managedCoroutines.TryGetValue(id, out var coroutine) && coroutine.IsPaused;
  }

  /// <summary>
  /// Gets the duration a coroutine has been paused.
  /// </summary>
  /// <param name="id">The CoroutineId to check</param>
  /// <returns>TimeSpan of pause duration, or null if not paused or not found</returns>
  public static TimeSpan? GetPauseDuration(CoroutineId id) {
    if (_managedCoroutines.TryGetValue(id, out var coroutine) &&
        coroutine.IsPaused && coroutine.PausedAt.HasValue) {
      return DateTime.UtcNow - coroutine.PausedAt.Value;
    }
    return null;
  }

  /// <summary>
  /// Toggles the pause state of a coroutine.
  /// </summary>
  /// <param name="id">The CoroutineId to toggle</param>
  /// <returns>True if coroutine was paused, false if resumed or not found</returns>
  public static bool TogglePauseCoroutine(CoroutineId id) {
    if (_managedCoroutines.TryGetValue(id, out var managedCoroutine)) {
      if (managedCoroutine.IsPaused) {
        ResumeCoroutine(id);
        return false;
      } else {
        PauseCoroutine(id);
        return true;
      }
    }
    return false;
  }

  /// <summary>
  /// Pauses all active coroutines.
  /// </summary>
  /// <returns>Number of coroutines that were paused</returns>
  public static int PauseAllCoroutines() {
    var pausedCount = 0;
    foreach (var coroutine in _managedCoroutines.Values) {
      if (!coroutine.IsPaused && coroutine.IsActive) {
        coroutine.IsPaused = true;
        coroutine.PausedAt = DateTime.UtcNow;
        pausedCount++;
      }
    }
    return pausedCount;
  }

  /// <summary>
  /// Resumes all paused coroutines.
  /// </summary>
  /// <returns>Number of coroutines that were resumed</returns>
  public static int ResumeAllCoroutines() {
    var resumedCount = 0;
    foreach (var coroutine in _managedCoroutines.Values) {
      if (coroutine.IsPaused) {
        coroutine.IsPaused = false;
        coroutine.PausedAt = null;
        resumedCount++;
      }
    }
    return resumedCount;
  }
}