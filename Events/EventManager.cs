using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using ScarletCore.Data;
using ScarletCore.Utils;
using Unity.Collections;
using Unity.Entities;
using System.Linq.Expressions;

namespace ScarletCore.Events;

public static class EventManager {
  private static readonly Dictionary<PrefixEvents, List<Action<NativeArray<Entity>>>> _prefixHandlers = new();
  private static readonly Dictionary<PostfixEvents, List<Action<NativeArray<Entity>>>> _postfixHandlers = new();
  private static readonly Dictionary<PlayerEvents, List<Action<PlayerData>>> _playerHandlers = new();
  private static readonly Dictionary<ServerEvents, List<Action>> _serverHandlers = new();

  private sealed class EventHandlerInfo {
    public Delegate Original;
    public Action<object> FastInvoker;
    public Type ExpectedType;
    public bool IsNullable;
  }

  private static readonly ConcurrentDictionary<string, List<EventHandlerInfo>> _customHandlers = new();
  private static readonly object _customLock = new();

  // --- Built-in methods (optimized) ---
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void On(PrefixEvents eventType, Action<NativeArray<Entity>> callback) {
    if (callback == null) return;
    if (!_prefixHandlers.TryGetValue(eventType, out var list)) {
      list = new List<Action<NativeArray<Entity>>>(4);
      _prefixHandlers[eventType] = list;
    }
    list.Add(callback);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static bool Off(PrefixEvents eventType, Action<NativeArray<Entity>> callback) {
    if (callback == null) return false;
    if (_prefixHandlers.TryGetValue(eventType, out var list)) {
      var removed = list.Remove(callback);
      if (removed && list.Count == 0) _prefixHandlers.Remove(eventType);
      return removed;
    }
    return false;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void On(PostfixEvents eventType, Action<NativeArray<Entity>> callback) {
    if (callback == null) return;
    if (!_postfixHandlers.TryGetValue(eventType, out var list)) {
      list = new List<Action<NativeArray<Entity>>>(4);
      _postfixHandlers[eventType] = list;
    }
    list.Add(callback);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static bool Off(PostfixEvents eventType, Action<NativeArray<Entity>> callback) {
    if (callback == null) return false;
    if (_postfixHandlers.TryGetValue(eventType, out var list)) {
      var removed = list.Remove(callback);
      if (removed && list.Count == 0) _postfixHandlers.Remove(eventType);
      return removed;
    }
    return false;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void On(PlayerEvents eventType, Action<PlayerData> callback) {
    if (callback == null) return;
    if (!_playerHandlers.TryGetValue(eventType, out var list)) {
      list = new List<Action<PlayerData>>(4);
      _playerHandlers[eventType] = list;
    }
    list.Add(callback);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static bool Off(PlayerEvents eventType, Action<PlayerData> callback) {
    if (callback == null) return false;
    if (_playerHandlers.TryGetValue(eventType, out var list)) {
      var removed = list.Remove(callback);
      if (removed && list.Count == 0) _playerHandlers.Remove(eventType);
      return removed;
    }
    return false;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void On(ServerEvents eventType, Action callback) {
    if (callback == null) return;
    if (!_serverHandlers.TryGetValue(eventType, out var list)) {
      list = new List<Action>(4);
      _serverHandlers[eventType] = list;
    }
    list.Add(callback);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static bool Off(ServerEvents eventType, Action callback) {
    if (callback == null) return false;
    if (_serverHandlers.TryGetValue(eventType, out var list)) {
      var removed = list.Remove(callback);
      if (removed && list.Count == 0) _serverHandlers.Remove(eventType);
      return removed;
    }
    return false;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void Once(PrefixEvents eventType, Action<NativeArray<Entity>> callback) {
    if (callback == null) return;
    Action<NativeArray<Entity>> wrapped = null;
    wrapped = (entities) => {
      try {
        callback(entities);
      } finally {
        Off(eventType, wrapped);
      }
    };
    On(eventType, wrapped);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void Once(PostfixEvents eventType, Action<NativeArray<Entity>> callback) {
    if (callback == null) return;
    Action<NativeArray<Entity>> wrapped = null;
    wrapped = (entities) => {
      try {
        callback(entities);
      } finally {
        Off(eventType, wrapped);
      }
    };
    On(eventType, wrapped);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void Once(PlayerEvents eventType, Action<PlayerData> callback) {
    if (callback == null) return;
    Action<PlayerData> wrapped = null;
    wrapped = (pdata) => {
      try {
        callback(pdata);
      } finally {
        Off(eventType, wrapped);
      }
    };
    On(eventType, wrapped);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void Once(ServerEvents eventType, Action callback) {
    if (callback == null) return;
    Action wrapped = null;
    wrapped = () => {
      try {
        callback();
      } finally {
        Off(eventType, wrapped);
      }
    };
    On(eventType, wrapped);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void Emit(PrefixEvents eventType, NativeArray<Entity> entityArray) {
    if (!_prefixHandlers.TryGetValue(eventType, out var handlers) || handlers.Count == 0) return;
    for (int i = 0; i < handlers.Count; i++) {
      try { handlers[i](entityArray); } catch (Exception ex) { Log.Error($"EventManager: Error in prefix '{eventType}': {ex}"); }
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void Emit(PostfixEvents eventType, NativeArray<Entity> entityArray) {
    if (!_postfixHandlers.TryGetValue(eventType, out var handlers) || handlers.Count == 0) return;
    for (int i = 0; i < handlers.Count; i++) {
      try { handlers[i](entityArray); } catch (Exception ex) { Log.Error($"EventManager: Error in postfix '{eventType}': {ex}"); }
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void Emit(PlayerEvents eventType, PlayerData playerData) {
    if (!_playerHandlers.TryGetValue(eventType, out var handlers) || handlers.Count == 0) return;
    for (int i = 0; i < handlers.Count; i++) {
      try { handlers[i](playerData); } catch (Exception ex) { Log.Error($"EventManager: Error in player '{eventType}': {ex}"); }
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void Emit(ServerEvents eventType) {
    if (!_serverHandlers.TryGetValue(eventType, out var handlers) || handlers.Count == 0) return;
    for (int i = 0; i < handlers.Count; i++) {
      try { handlers[i](); } catch (Exception ex) { Log.Error($"EventManager: Error in server '{eventType}': {ex}"); }
    }
  }

  // --- Custom event methods (optimized) ---
  private static EventHandlerInfo CreateHandlerInfo(Delegate callback) {
    var invokeMethod = callback.GetType().GetMethod("Invoke");
    var parameters = invokeMethod.GetParameters();

    Type expectedType = null;
    bool isNullable = true;
    Action<object> fastInvoker;

    if (parameters.Length == 0) {
      fastInvoker = _ => callback.DynamicInvoke();
    } else if (parameters.Length == 1) {
      expectedType = parameters[0].ParameterType;
      isNullable = !expectedType.IsValueType || Nullable.GetUnderlyingType(expectedType) != null;

      var dataParam = Expression.Parameter(typeof(object), "data");
      var convertedParam = Expression.Convert(dataParam, expectedType);
      var invokeExpr = Expression.Invoke(Expression.Constant(callback), convertedParam);
      var lambda = Expression.Lambda<Action<object>>(invokeExpr, dataParam);

      fastInvoker = lambda.Compile();
    } else {
      expectedType = typeof(object[]);
      fastInvoker = data => {
        if (data is object[] arr && arr.Length == parameters.Length) {
          callback.DynamicInvoke(arr);
        } else {
          Log.Warning($"EventManager: Parameter count mismatch for multi-param delegate");
        }
      };
    }

    return new EventHandlerInfo {
      Original = callback,
      FastInvoker = fastInvoker,
      ExpectedType = expectedType,
      IsNullable = isNullable
    };
  }

  public static void On(string eventName, Delegate callback) {
    if (string.IsNullOrWhiteSpace(eventName)) {
      Log.Warning("EventManager: Event name cannot be null or empty");
      return;
    }
    if (callback == null) {
      Log.Warning("EventManager: Callback cannot be null");
      return;
    }

    var handlerInfo = CreateHandlerInfo(callback);

    lock (_customLock) {
      if (!_customHandlers.TryGetValue(eventName, out var handlers)) {
        handlers = new List<EventHandlerInfo>(4);
        _customHandlers[eventName] = handlers;
      }
      handlers.Add(handlerInfo);
    }
  }

  public static bool Off(string eventName, Delegate callback) {
    if (string.IsNullOrWhiteSpace(eventName) || callback == null) return false;

    lock (_customLock) {
      if (_customHandlers.TryGetValue(eventName, out var handlers)) {
        int index = handlers.FindIndex(h => ReferenceEquals(h.Original, callback));
        if (index >= 0) {
          handlers.RemoveAt(index);
          if (handlers.Count == 0) _customHandlers.TryRemove(eventName, out _);
          return true;
        }
      }
    }
    return false;
  }

  public static void Once(string eventName, Delegate callback) {
    if (string.IsNullOrWhiteSpace(eventName) || callback == null) return;

    Delegate wrapped = null;
    var invokeMethod = callback.GetType().GetMethod("Invoke");
    var parameters = invokeMethod.GetParameters();

    if (parameters.Length == 0) {
      wrapped = new Action(() => {
        try { callback.DynamicInvoke(); } finally { Off(eventName, wrapped); }
      });
    } else if (parameters.Length == 1) {
      wrapped = new Action<object>(data => {
        try { callback.DynamicInvoke(data); } finally { Off(eventName, wrapped); }
      });
    } else {
      wrapped = callback;
    }

    On(eventName, wrapped);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void Emit(string eventName, object data = null) {
    if (string.IsNullOrWhiteSpace(eventName)) {
      Log.Warning("EventManager: Event name cannot be null or empty");
      return;
    }

    EventHandlerInfo[] handlersToExecute;
    lock (_customLock) {
      if (!_customHandlers.TryGetValue(eventName, out var handlers) || handlers.Count == 0) return;
      handlersToExecute = handlers.ToArray();
    }

    for (int i = 0; i < handlersToExecute.Length; i++) {
      var handler = handlersToExecute[i];
      try {
        if (handler.ExpectedType != null) {
          if (data == null) {
            if (!handler.IsNullable) {
              Log.Warning($"EventManager: Cannot pass null to non-nullable type for '{eventName}'");
              continue;
            }
          } else if (!handler.ExpectedType.IsAssignableFrom(data.GetType())) {
            Log.Warning($"EventManager: Type mismatch for '{eventName}'. Expected {handler.ExpectedType.Name}, got {data.GetType().Name}");
            continue;
          }
        }

        handler.FastInvoker(data);
      } catch (Exception ex) {
        Log.Error($"EventManager: Error executing callback for '{eventName}': {ex}");
      }
    }
  }

  // --- Utility Methods ---

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static int GetSubscriberCount(string eventName) {
    if (string.IsNullOrWhiteSpace(eventName)) return 0;
    lock (_customLock) {
      return _customHandlers.TryGetValue(eventName, out var handlers) ? handlers.Count : 0;
    }
  }

  // Overloads for built-in event enums so callers (patches) can early-exit
  public static int GetSubscriberCount(PrefixEvents eventType) {
    return _prefixHandlers.TryGetValue(eventType, out var list) ? list.Count : 0;
  }

  public static int GetSubscriberCount(PostfixEvents eventType) {
    return _postfixHandlers.TryGetValue(eventType, out var list) ? list.Count : 0;
  }

  public static int GetSubscriberCount(PlayerEvents eventType) {
    return _playerHandlers.TryGetValue(eventType, out var list) ? list.Count : 0;
  }

  public static int GetSubscriberCount(ServerEvents eventType) {
    return _serverHandlers.TryGetValue(eventType, out var list) ? list.Count : 0;
  }

  public static IEnumerable<string> GetRegisteredEvents() {
    lock (_customLock) {
      return _customHandlers.Keys.ToList();
    }
  }

  public static bool ClearEvent(string eventName) {
    if (string.IsNullOrWhiteSpace(eventName)) return false;
    lock (_customLock) {
      return _customHandlers.TryRemove(eventName, out _);
    }
  }

  public static void ClearAllEvents() {
    lock (_customLock) {
      int count = _customHandlers.Count;
      _customHandlers.Clear();
      Log.Warning($"EventManager: Cleared all {count} custom events");
    }
  }

  public static Dictionary<string, int> GetEventStatistics() {
    var stats = new Dictionary<string, int>();
    lock (_customLock) {
      foreach (var kv in _customHandlers) stats[kv.Key] = kv.Value.Count;
    }
    return stats;
  }

  public static int UnregisterAssembly(Assembly assembly) {
    if (assembly == null) return 0;
    int removed = 0;

    lock (_customLock) {
      foreach (var kv in _customHandlers) {
        var handlers = kv.Value;
        for (int i = handlers.Count - 1; i >= 0; i--) {
          if (handlers[i].Original.Method?.DeclaringType?.Assembly == assembly) {
            handlers.RemoveAt(i);
            removed++;
          }
        }
        if (handlers.Count == 0) _customHandlers.TryRemove(kv.Key, out _);
      }
    }

    foreach (var kv in _prefixHandlers) {
      var handlers = kv.Value;
      for (int i = handlers.Count - 1; i >= 0; i--) {
        if (handlers[i].Method?.DeclaringType?.Assembly == assembly) {
          handlers.RemoveAt(i);
          removed++;
        }
      }
      if (handlers.Count == 0) _prefixHandlers.Remove(kv.Key);
    }

    foreach (var kv in _postfixHandlers) {
      var handlers = kv.Value;
      for (int i = handlers.Count - 1; i >= 0; i--) {
        if (handlers[i].Method?.DeclaringType?.Assembly == assembly) {
          handlers.RemoveAt(i);
          removed++;
        }
      }
      if (handlers.Count == 0) _postfixHandlers.Remove(kv.Key);
    }

    foreach (var kv in _playerHandlers) {
      var handlers = kv.Value;
      for (int i = handlers.Count - 1; i >= 0; i--) {
        if (handlers[i].Method?.DeclaringType?.Assembly == assembly) {
          handlers.RemoveAt(i);
          removed++;
        }
      }
      if (handlers.Count == 0) _playerHandlers.Remove(kv.Key);
    }

    foreach (var kv in _serverHandlers) {
      var handlers = kv.Value;
      for (int i = handlers.Count - 1; i >= 0; i--) {
        if (handlers[i].Method?.DeclaringType?.Assembly == assembly) {
          handlers.RemoveAt(i);
          removed++;
        }
      }
      if (handlers.Count == 0) _serverHandlers.Remove(kv.Key);
    }

    return removed;
  }
}