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
  /// Registra um callback para um evento customizado sem dados (Action sem parâmetros)
  /// </summary>
  /// <param name="eventName">Nome do evento customizado</param>
  /// <param name="callback">Callback a ser executado quando o evento for disparado</param>
  public static void On(string eventName, Action callback) {
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
  /// Remove um callback sem parâmetros de um evento customizado
  /// </summary>
  /// <param name="eventName">Nome do evento customizado</param>
  /// <param name="callback">Callback a ser removido</param>
  /// <returns>True se o callback foi encontrado e removido</returns>
  public static bool Off(string eventName, Action callback) {
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
  /// Registers a callback for a custom event with typed data
  /// </summary>
  /// <typeparam name="T">Type of the event data</typeparam>
  /// <param name="eventName">Name of the custom event</param>
  /// <param name="callback">Typed callback to execute when event is triggered</param>
  public static void On<T>(string eventName, Action<T> callback) {
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
  /// Unregisters a callback from a custom event with typed data
  /// </summary>
  /// <typeparam name="T">Type of the event data</typeparam>
  /// <param name="eventName">Name of the custom event</param>
  /// <param name="callback">Typed callback to remove</param>
  /// <returns>True if callback was found and removed</returns>
  public static bool Off<T>(string eventName, Action<T> callback) {
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
          if (parameters != null && parameters.Length == 1) {
            var paramType = parameters[0].ParameterType;
            if (data == null && paramType.IsClass) {
              handler.DynamicInvoke(null);
            } else if (data != null && paramType.IsAssignableFrom(data.GetType())) {
              handler.DynamicInvoke(data);
            } else {
              Log.Warning($"CustomEventManager: Type mismatch for event '{eventName}'. Expected {paramType.Name}, got {data?.GetType().Name ?? "null"}");
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
}
