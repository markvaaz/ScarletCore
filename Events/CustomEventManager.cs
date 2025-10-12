using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using ScarletCore.Utils;

namespace ScarletCore.Events;

/// <summary>
/// Manager for custom events that allows dynamic event registration and triggering
/// </summary>
public static class CustomEventManager {
  private static readonly ConcurrentDictionary<string, List<Delegate>> _eventHandlers = new();
  private static readonly object _lock = new();

  /// <summary>
  /// Registers a callback (any delegate type) for a custom event.
  /// </summary>
  /// <param name="eventName">Name of the custom event</param>
  /// <param name="callback">Delegate to execute when the event is triggered</param>
  public static void On(string eventName, Delegate callback) {
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
  }

  /// <summary>
  /// Unregisters a delegate from a custom event.
  /// </summary>
  /// <param name="eventName">Name of the custom event</param>
  /// <param name="callback">Delegate to remove</param>
  /// <returns>True if the callback was found and removed</returns>
  public static bool Off(string eventName, Delegate callback) {
    if (string.IsNullOrWhiteSpace(eventName) || callback == null)
      return false;
    lock (_lock) {
      if (_eventHandlers.TryGetValue(eventName, out var handlers)) {
        bool removed = handlers.Remove(callback);
        if (removed) {
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
  /// Generic overload for registering a callback for a custom event with type parameter.
  /// </summary>
  /// <typeparam name="T">The type of data the event will handle</typeparam>
  /// <param name="eventName">Name of the custom event</param>
  /// <param name="callback">Delegate to execute when the event is triggered</param>
  public static void On<T>(string eventName, Delegate callback) => On(eventName, callback);

  /// <summary>
  /// Generic overload for unregistering a delegate from a custom event with type parameter.
  /// </summary>
  /// <typeparam name="T">The type of data the event handles</typeparam>
  /// <param name="eventName">Name of the custom event</param>
  /// <param name="callback">Delegate to remove</param>
  /// <returns>True if the callback was found and removed</returns>
  public static bool Off<T>(string eventName, Delegate callback) => Off(eventName, callback);

  /// <summary>
  /// Registers a callback that will be executed only once and then automatically removed.
  /// </summary>
  /// <param name="eventName">Name of the custom event</param>
  /// <param name="callback">Delegate to execute when the event is triggered</param>
  public static void Once(string eventName, Delegate callback) {
    if (string.IsNullOrWhiteSpace(eventName) || callback == null) return;

    Delegate wrappedCallback = null;
    wrappedCallback = CreateWrappedCallback(callback, () => Off(eventName, wrappedCallback));
    On(eventName, wrappedCallback);
  }

  /// <summary>
  /// Generic overload for registering a one-time callback for a custom event with type parameter.
  /// </summary>
  /// <typeparam name="T">The type of data the event will handle</typeparam>
  /// <param name="eventName">Name of the custom event</param>
  /// <param name="callback">Delegate to execute when the event is triggered</param>
  public static void Once<T>(string eventName, Delegate callback) => Once(eventName, callback);

  private static Delegate CreateWrappedCallback(Delegate originalCallback, Action removeCallback) {
    var handlerType = originalCallback.GetType();
    var invokeMethod = handlerType.GetMethod("Invoke");
    var parameters = invokeMethod?.GetParameters();

    if (parameters?.Length == 0) {
      return new Action(() => {
        try {
          originalCallback.DynamicInvoke();
        } finally {
          removeCallback();
        }
      });
    } else if (parameters?.Length == 1) {
      return new Action<object>(data => {
        try {
          originalCallback.DynamicInvoke(data);
        } finally {
          removeCallback();
        }
      });
    }

    return originalCallback;
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
    List<Delegate> handlersToExecute = null;
    lock (_lock) {
      if (_eventHandlers.TryGetValue(eventName, out var handlers) && handlers.Count > 0) {
        handlersToExecute = [.. handlers];
      }
    }
    if (handlersToExecute != null) {
      foreach (var handler in handlersToExecute) {
        try {
          var handlerType = handler.GetType();
          var invokeMethod = handlerType.GetMethod("Invoke");
          var parameters = invokeMethod?.GetParameters();
          if (parameters != null) {
            if (parameters.Length == 0) {
              handler.DynamicInvoke();
            } else if (parameters.Length == 1) {
              var paramType = parameters[0].ParameterType;
              if (data == null && paramType.IsClass) {
                handler.DynamicInvoke(null);
              } else if (data != null && paramType.IsAssignableFrom(data.GetType())) {
                handler.DynamicInvoke(data);
              } else {
                Log.Warning($"CustomEventManager: Type mismatch for event '{eventName}'. Expected {paramType.Name}, got {data?.GetType().Name ?? "null"}");
              }
            }
          }
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
      return _eventHandlers.TryRemove(eventName, out _);
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

  /// <summary>
  /// Unregisters all callbacks from a given assembly for all events
  /// </summary>
  /// <param name="assembly">The assembly whose callbacks should be removed</param>
  /// <returns>The number of callbacks removed</returns>
  public static int UnregisterAssembly(System.Reflection.Assembly assembly) {
    if (assembly == null) return 0;
    int removedCount = 0;
    lock (_lock) {
      var keysToRemove = new List<string>();
      foreach (var kvp in _eventHandlers) {
        var handlers = kvp.Value;
        int before = handlers.Count;
        handlers.RemoveAll(d => d.Method?.DeclaringType?.Assembly == assembly);
        removedCount += before - handlers.Count;
        if (handlers.Count == 0) keysToRemove.Add(kvp.Key);
      }
      foreach (var key in keysToRemove) {
        _eventHandlers.TryRemove(key, out _);
      }
    }
    return removedCount;
  }

  // Communication system for two-way events
  private static readonly ConcurrentDictionary<string, List<Delegate>> _communicationHandlers = new();
  private static readonly object _commLock = new();

  /// <summary>
  /// Registers a communication handler for two-way events that can return a response
  /// </summary>
  /// <param name="eventName">Name of the communication event</param>
  /// <param name="callback">Delegate that can return a response</param>
  public static void OnCommunicate(string eventName, Delegate callback) {
    if (string.IsNullOrWhiteSpace(eventName)) {
      Log.Warning("CustomEventManager: Communication event name cannot be null or empty");
      return;
    }
    if (callback == null) {
      Log.Warning("CustomEventManager: Communication callback cannot be null");
      return;
    }
    lock (_commLock) {
      if (!_communicationHandlers.ContainsKey(eventName)) {
        _communicationHandlers[eventName] = [];
      }
      _communicationHandlers[eventName].Add(callback);
    }
  }

  /// <summary>
  /// Generic overload for registering a communication callback with type parameter
  /// </summary>
  /// <typeparam name="T">The type of data the event will handle</typeparam>
  /// <param name="eventName">Name of the communication event</param>
  /// <param name="callback">Delegate that can return a response</param>
  public static void OnCommunicate<T>(string eventName, Delegate callback) => OnCommunicate(eventName, callback);

  /// <summary>
  /// Unregisters a communication handler from a two-way event
  /// </summary>
  /// <param name="eventName">Name of the communication event</param>
  /// <param name="callback">Delegate to remove</param>
  /// <returns>True if the callback was found and removed</returns>
  public static bool OffCommunicate(string eventName, Delegate callback) {
    if (string.IsNullOrWhiteSpace(eventName) || callback == null)
      return false;
    lock (_commLock) {
      if (_communicationHandlers.TryGetValue(eventName, out var handlers)) {
        bool removed = handlers.Remove(callback);
        if (removed && handlers.Count == 0) {
          _communicationHandlers.TryRemove(eventName, out _);
        }
        return removed;
      }
    }
    return false;
  }

  /// <summary>
  /// Generic overload for unregistering a communication callback with type parameter
  /// </summary>
  /// <typeparam name="T">The type of data the event handles</typeparam>
  /// <param name="eventName">Name of the communication event</param>
  /// <param name="callback">Delegate to remove</param>
  /// <returns>True if the callback was found and removed</returns>
  public static bool OffCommunicate<T>(string eventName, Delegate callback) => OffCommunicate(eventName, callback);

  /// <summary>
  /// Emits a communication event and returns the first response received
  /// </summary>
  /// <param name="eventName">Name of the communication event</param>
  /// <param name="data">Data to send to handlers</param>
  /// <returns>Response from the first handler, or null if no handlers or no response</returns>
  public static object EmitCommunication(string eventName, object data = null) {
    if (string.IsNullOrWhiteSpace(eventName)) {
      Log.Warning("CustomEventManager: Communication event name cannot be null or empty");
      return null;
    }
    List<Delegate> handlersToExecute = null;
    lock (_commLock) {
      if (_communicationHandlers.TryGetValue(eventName, out var handlers) && handlers.Count > 0) {
        handlersToExecute = [.. handlers];
      }
    }
    if (handlersToExecute != null) {
      foreach (var handler in handlersToExecute) {
        try {
          var handlerType = handler.GetType();
          var invokeMethod = handlerType.GetMethod("Invoke");
          var parameters = invokeMethod?.GetParameters();
          var returnType = invokeMethod?.ReturnType;

          if (returnType == typeof(void)) {
            continue; // Skip void methods as they can't return a response
          }

          object response = null;
          if (parameters != null) {
            if (parameters.Length == 0) {
              response = handler.DynamicInvoke();
            } else if (parameters.Length == 1) {
              var paramType = parameters[0].ParameterType;
              if (data == null && paramType.IsClass) {
                response = handler.DynamicInvoke(null);
              } else if (data != null && paramType.IsAssignableFrom(data.GetType())) {
                response = handler.DynamicInvoke(data);
              } else {
                Log.Warning($"CustomEventManager: Type mismatch for communication event '{eventName}'. Expected {paramType.Name}, got {data?.GetType().Name ?? "null"}");
                continue;
              }
            }
          }

          if (response != null) {
            return response;
          }
        } catch (Exception ex) {
          Log.Error($"CustomEventManager: Error executing communication handler for event '{eventName}': {ex}");
        }
      }
    }
    return null;
  }
}
