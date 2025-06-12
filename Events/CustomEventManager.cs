using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using ScarletCore.Utils;

namespace ScarletCore.Events;

/// <summary>
/// Manager for custom events that allows dynamic event registration and triggering
/// </summary>
public static class CustomEventManager {
  private static readonly ConcurrentDictionary<string, List<Action<object>>> _eventHandlers = new();
  private static readonly object _lock = new();

  /// <summary>
  /// Registers a callback for a custom event
  /// </summary>
  /// <param name="eventName">Name of the custom event</param>
  /// <param name="callback">Callback to execute when event is triggered</param>
  public static void On(string eventName, Action<object> callback) {
    if (string.IsNullOrWhiteSpace(eventName)) {
      Log.Warning("CustomEventManager: Event name cannot be null or empty");
      return;
    }

    if (callback == null) {
      Log.Warning("CustomEventManager: Callback cannot be null");
      return;
    }

    lock (_lock) {
      if (!_eventHandlers.ContainsKey(eventName)) {
        _eventHandlers[eventName] = [];
      }

      _eventHandlers[eventName].Add(callback);
    }

    Log.Info($"CustomEventManager: Registered callback for event '{eventName}'");
  }

  /// <summary>
  /// Registers a callback for a custom event with typed data
  /// </summary>
  /// <typeparam name="T">Type of the event data</typeparam>
  /// <param name="eventName">Name of the custom event</param>
  /// <param name="callback">Typed callback to execute when event is triggered</param>
  public static void On<T>(string eventName, Action<T> callback) {
    On(eventName, data => {
      try {
        if (data is T typedData) {
          callback(typedData);
        } else {
          Log.Warning($"CustomEventManager: Type mismatch for event '{eventName}'. Expected {typeof(T).Name}, got {data?.GetType().Name ?? "null"}");
        }
      } catch (Exception ex) {
        Log.Error($"CustomEventManager: Error in typed callback for event '{eventName}': {ex}");
      }
    });
  }

  /// <summary>
  /// Unregisters a callback from a custom event
  /// </summary>
  /// <param name="eventName">Name of the custom event</param>
  /// <param name="callback">Callback to remove</param>
  /// <returns>True if callback was found and removed</returns>
  public static bool Off(string eventName, Action<object> callback) {
    if (string.IsNullOrWhiteSpace(eventName) || callback == null)
      return false;

    lock (_lock) {
      if (_eventHandlers.TryGetValue(eventName, out var handlers)) {
        bool removed = handlers.Remove(callback);
        if (removed) {
          Log.Info($"CustomEventManager: Unregistered callback for event '{eventName}'");

          // Clean up empty event lists
          if (handlers.Count == 0) {
            _eventHandlers.TryRemove(eventName, out _);
          }
        }
        return removed;
      }
    }

    return false;
  }

  /// <summary>
  /// Triggers a custom event with optional data
  /// </summary>
  /// <param name="eventName">Name of the custom event to trigger</param>
  /// <param name="data">Optional data to pass to callbacks</param>
  public static void Emit(string eventName, object data = null) {
    if (string.IsNullOrWhiteSpace(eventName)) {
      Log.Warning("CustomEventManager: Event name cannot be null or empty");
      return;
    }

    List<Action<object>> handlersToExecute = null;

    lock (_lock) {
      if (_eventHandlers.TryGetValue(eventName, out var handlers) && handlers.Count > 0) {
        // Create a copy to avoid holding the lock during execution
        handlersToExecute = [.. handlers];
      }
    }

    if (handlersToExecute != null) {
      Log.Info($"CustomEventManager: Emitting event '{eventName}' to {handlersToExecute.Count} subscribers");

      foreach (var handler in handlersToExecute) {
        try {
          handler(data);
        } catch (Exception ex) {
          Log.Error($"CustomEventManager: Error executing callback for event '{eventName}': {ex}");
        }
      }
    }
  }

  /// <summary>
  /// Gets the number of subscribers for a custom event
  /// </summary>
  /// <param name="eventName">Name of the custom event</param>
  /// <returns>Number of subscribers</returns>
  public static int GetSubscriberCount(string eventName) {
    if (string.IsNullOrWhiteSpace(eventName))
      return 0;

    lock (_lock) {
      return _eventHandlers.TryGetValue(eventName, out var handlers) ? handlers.Count : 0;
    }
  }

  /// <summary>
  /// Gets all registered event names
  /// </summary>
  /// <returns>Collection of event names</returns>
  public static IEnumerable<string> GetRegisteredEvents() {
    lock (_lock) {
      return [.. _eventHandlers.Keys];
    }
  }

  /// <summary>
  /// Clears all subscribers for a specific event
  /// </summary>
  /// <param name="eventName">Name of the event to clear</param>
  /// <returns>True if event existed and was cleared</returns>
  public static bool ClearEvent(string eventName) {
    if (string.IsNullOrWhiteSpace(eventName))
      return false;

    lock (_lock) {
      bool removed = _eventHandlers.TryRemove(eventName, out _);
      if (removed) {
        Log.Info($"CustomEventManager: Cleared all subscribers for event '{eventName}'");
      }
      return removed;
    }
  }

  /// <summary>
  /// Clears all custom events and their subscribers
  /// </summary>
  public static void ClearAllEvents() {
    lock (_lock) {
      int eventCount = _eventHandlers.Count;
      _eventHandlers.Clear();
      Log.Warning($"CustomEventManager: Cleared all {eventCount} custom events and their subscribers");
    }
  }

  /// <summary>
  /// Gets statistics about the custom event system
  /// </summary>
  /// <returns>Dictionary with event names and subscriber counts</returns>
  public static Dictionary<string, int> GetEventStatistics() {
    var stats = new Dictionary<string, int>();

    lock (_lock) {
      foreach (var kvp in _eventHandlers) {
        stats[kvp.Key] = kvp.Value.Count;
      }
    }

    return stats;
  }
}
