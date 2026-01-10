using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using ScarletCore.Utils;
using ScarletCore.Commanding;
using Unity.Collections;
using Unity.Entities;
using System.Linq.Expressions;
using ScarletCore.Services;

namespace ScarletCore.Events;

/// <summary>
/// Provides a comprehensive event management system for handling game events with support for priorities, custom events, and dynamic subscription.
/// Supports prefix, postfix, player, server, and custom events, allowing for flexible and efficient event-driven programming.
/// </summary>
public static class EventManager {

  /// <summary>
  /// Represents a delegate handler with an associated priority for event invocation ordering.
  /// </summary>
  /// <typeparam name="T">The delegate type.</typeparam>
  private sealed class PrioritizedHandler<T>(T handler) where T : Delegate {
    /// <summary>
    /// The delegate handler to invoke.
    /// </summary>
    public T Handler = handler;
    /// <summary>
    /// The priority of the handler. Higher values are invoked first.
    /// </summary>
    public int Priority = GetPriority(handler);

    private static int GetPriority(T handler) {
      var attr = handler.Method.GetCustomAttribute<EventPriorityAttribute>();
      return attr?.Priority ?? 0;
    }
  }

  private static readonly Dictionary<PrefixEvents, List<PrioritizedHandler<Action<NativeArray<Entity>>>>> _prefixHandlers = [];
  private static readonly Dictionary<PostfixEvents, List<PrioritizedHandler<Action<NativeArray<Entity>>>>> _postfixHandlers = [];
  private static readonly Dictionary<PlayerEvents, List<PrioritizedHandler<Action<PlayerData>>>> _playerHandlers = [];
  private static readonly Dictionary<ServerEvents, List<PrioritizedHandler<Delegate>>> _serverHandlers = [];
  private static readonly Dictionary<CommandEvents, List<PrioritizedHandler<Delegate>>> _commandHandlers = [];


  /// <summary>
  /// Stores information about a custom event handler, including the original delegate, a fast invoker, and type information.
  /// </summary>
  private sealed class EventHandlerInfo {
    /// <summary>
    /// The original delegate provided by the subscriber.
    /// </summary>
    public Delegate Original;
    /// <summary>
    /// A compiled fast invoker for efficient dynamic invocation.
    /// </summary>
    public Action<object> FastInvoker;
    /// <summary>
    /// A compiled fast invoker for handlers that return bool (for cancellable events).
    /// </summary>
    public Func<object, bool> CancellableInvoker;
    /// <summary>
    /// The expected parameter type for the handler, or null for parameterless handlers.
    /// </summary>
    public Type ExpectedType;
    /// <summary>
    /// Indicates whether the handler parameter is nullable.
    /// </summary>
    public bool IsNullable;
    /// <summary>
    /// Indicates whether the handler returns bool and can cancel event propagation.
    /// </summary>
    public bool IsCancellable;
    /// <summary>
    /// The priority of the handler. Higher values are invoked first.
    /// </summary>
    public int Priority;
  }

  private static readonly ConcurrentDictionary<string, List<EventHandlerInfo>> _customHandlers = new();
  private static readonly object _customLock = new();

  // Helper method to compare delegates by method and target instead of reference

  /// <summary>
  /// Compares two delegates for equality based on their method and target.
  /// </summary>
  /// <param name="a">The first delegate.</param>
  /// <param name="b">The second delegate.</param>
  /// <returns>True if both delegates have the same method and target; otherwise, false.</returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private static bool AreDelegatesEqual(Delegate a, Delegate b) {
    if (ReferenceEquals(a, b)) return true;
    if (a == null || b == null) return false;
    return a.Method.Equals(b.Method) && Equals(a.Target, b.Target);
  }

  // --- Built-in methods (optimized with priority) ---
  /// <summary>
  /// Subscribes a callback to a prefix event. Handlers are invoked before the main event logic.
  /// </summary>
  /// <param name="eventType">The prefix event type to subscribe to.</param>
  /// <param name="callback">The callback to invoke when the event is emitted.</param>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void On(PrefixEvents eventType, Action<NativeArray<Entity>> callback) {
    if (callback == null) return;
    if (!_prefixHandlers.TryGetValue(eventType, out var list)) {
      list = new List<PrioritizedHandler<Action<NativeArray<Entity>>>>(4);
      _prefixHandlers[eventType] = list;
    }
    var ph = new PrioritizedHandler<Action<NativeArray<Entity>>>(callback);
    int idx = list.FindIndex(h => h.Priority < ph.Priority);
    if (idx >= 0) list.Insert(idx, ph);
    else list.Add(ph);
  }

  /// <summary>
  /// Unsubscribes a callback from a prefix event.
  /// </summary>
  /// <param name="eventType">The prefix event type to unsubscribe from.</param>
  /// <param name="callback">The callback to remove.</param>
  /// <returns>True if the callback was removed; otherwise, false.</returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static bool Off(PrefixEvents eventType, Action<NativeArray<Entity>> callback) {
    if (callback == null) return false;
    if (_prefixHandlers.TryGetValue(eventType, out var list)) {
      int idx = list.FindIndex(h => AreDelegatesEqual(h.Handler, callback));
      if (idx >= 0) {
        list.RemoveAt(idx);
        if (list.Count == 0) _prefixHandlers.Remove(eventType);
        return true;
      }
    }
    return false;
  }

  /// <summary>
  /// Subscribes a callback to a postfix event. Handlers are invoked after the main event logic.
  /// </summary>
  /// <param name="eventType">The postfix event type to subscribe to.</param>
  /// <param name="callback">The callback to invoke when the event is emitted.</param>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void On(PostfixEvents eventType, Action<NativeArray<Entity>> callback) {
    if (callback == null) return;
    if (!_postfixHandlers.TryGetValue(eventType, out var list)) {
      list = new List<PrioritizedHandler<Action<NativeArray<Entity>>>>(4);
      _postfixHandlers[eventType] = list;
    }
    var ph = new PrioritizedHandler<Action<NativeArray<Entity>>>(callback);
    int idx = list.FindIndex(h => h.Priority < ph.Priority);
    if (idx >= 0) list.Insert(idx, ph);
    else list.Add(ph);
  }

  /// <summary>
  /// Unsubscribes a callback from a postfix event.
  /// </summary>
  /// <param name="eventType">The postfix event type to unsubscribe from.</param>
  /// <param name="callback">The callback to remove.</param>
  /// <returns>True if the callback was removed; otherwise, false.</returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static bool Off(PostfixEvents eventType, Action<NativeArray<Entity>> callback) {
    if (callback == null) return false;
    if (_postfixHandlers.TryGetValue(eventType, out var list)) {
      int idx = list.FindIndex(h => AreDelegatesEqual(h.Handler, callback));
      if (idx >= 0) {
        list.RemoveAt(idx);
        if (list.Count == 0) _postfixHandlers.Remove(eventType);
        return true;
      }
    }
    return false;
  }

  /// <summary>
  /// Subscribes a callback to a player event. Handlers are invoked for player-related events.
  /// </summary>
  /// <param name="eventType">The player event type to subscribe to.</param>
  /// <param name="callback">The callback to invoke when the event is emitted.</param>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void On(PlayerEvents eventType, Action<PlayerData> callback) {
    if (callback == null) return;
    if (!_playerHandlers.TryGetValue(eventType, out var list)) {
      list = new List<PrioritizedHandler<Action<PlayerData>>>(4);
      _playerHandlers[eventType] = list;
    }
    var ph = new PrioritizedHandler<Action<PlayerData>>(callback);
    int idx = list.FindIndex(h => h.Priority < ph.Priority);
    if (idx >= 0) list.Insert(idx, ph);
    else list.Add(ph);
  }

  /// <summary>
  /// Unsubscribes a callback from a player event.
  /// </summary>
  /// <param name="eventType">The player event type to unsubscribe from.</param>
  /// <param name="callback">The callback to remove.</param>
  /// <returns>True if the callback was removed; otherwise, false.</returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static bool Off(PlayerEvents eventType, Action<PlayerData> callback) {
    if (callback == null) return false;
    if (_playerHandlers.TryGetValue(eventType, out var list)) {
      int idx = list.FindIndex(h => AreDelegatesEqual(h.Handler, callback));
      if (idx >= 0) {
        list.RemoveAt(idx);
        if (list.Count == 0) _playerHandlers.Remove(eventType);
        return true;
      }
    }
    return false;
  }

  /// <summary>
  /// Subscribes a callback to a server event. Handlers are invoked for server-wide events.
  /// </summary>
  /// <param name="eventType">The server event type to subscribe to.</param>
  /// <param name="callback">The callback to invoke when the event is emitted.</param>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void On(ServerEvents eventType, Action callback) {
    if (callback == null) return;
    if (!_serverHandlers.TryGetValue(eventType, out var list)) {
      list = new List<PrioritizedHandler<Delegate>>(4);
      _serverHandlers[eventType] = list;
    }
    var ph = new PrioritizedHandler<Delegate>(callback);
    int idx = list.FindIndex(h => h.Priority < ph.Priority);
    if (idx >= 0) list.Insert(idx, ph);
    else list.Add(ph);
  }

  /// <summary>
  /// Subscribes a callback with a string parameter to a server event.
  /// </summary>
  /// <param name="eventType">The server event type to subscribe to.</param>
  /// <param name="callback">The callback to invoke when the event is emitted.</param>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void On(ServerEvents eventType, Action<string> callback) {
    if (callback == null) return;
    if (!_serverHandlers.TryGetValue(eventType, out var list)) {
      list = new List<PrioritizedHandler<Delegate>>(4);
      _serverHandlers[eventType] = list;
    }
    var ph = new PrioritizedHandler<Delegate>(callback);
    int idx = list.FindIndex(h => h.Priority < ph.Priority);
    if (idx >= 0) list.Insert(idx, ph);
    else list.Add(ph);
  }

  /// <summary>
  /// Unsubscribes a callback from a server event.
  /// </summary>
  /// <param name="eventType">The server event type to unsubscribe from.</param>
  /// <param name="callback">The callback to remove.</param>
  /// <returns>True if the callback was removed; otherwise, false.</returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static bool Off(ServerEvents eventType, Action callback) {
    if (callback == null) return false;
    if (_serverHandlers.TryGetValue(eventType, out var list)) {
      int idx = list.FindIndex(h => AreDelegatesEqual(h.Handler, callback));
      if (idx >= 0) {
        list.RemoveAt(idx);
        if (list.Count == 0) _serverHandlers.Remove(eventType);
        return true;
      }
    }
    return false;
  }

  /// <summary>
  /// Unsubscribes a callback with a string parameter from a server event.
  /// </summary>
  /// <param name="eventType">The server event type to unsubscribe from.</param>
  /// <param name="callback">The callback to remove.</param>
  /// <returns>True if the callback was removed; otherwise, false.</returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static bool Off(ServerEvents eventType, Action<string> callback) {
    if (callback == null) return false;
    if (_serverHandlers.TryGetValue(eventType, out var list)) {
      int idx = list.FindIndex(h => AreDelegatesEqual(h.Handler, callback));
      if (idx >= 0) {
        list.RemoveAt(idx);
        if (list.Count == 0) _serverHandlers.Remove(eventType);
        return true;
      }
    }
    return false;
  }

  // --- Command event methods (similar to server events) ---
  /// <summary>
  /// Subscribes a callback to a command event. Handlers are invoked for command-related events.
  /// </summary>
  /// <param name="eventType">The command event type to subscribe to.</param>
  /// <param name="callback">The callback to invoke when the event is emitted.</param>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  internal static void On(CommandEvents eventType, Action<PlayerData, CommandInfo, string[]> callback) {
    if (callback == null) return;
    if (!_commandHandlers.TryGetValue(eventType, out var list)) {
      list = new List<PrioritizedHandler<Delegate>>(4);
      _commandHandlers[eventType] = list;
    }
    var ph = new PrioritizedHandler<Delegate>(callback);
    int idx = list.FindIndex(h => h.Priority < ph.Priority);
    if (idx >= 0) list.Insert(idx, ph);
    else list.Add(ph);
  }

  /// <summary>
  /// Unsubscribes a callback from a command event.
  /// </summary>
  /// <param name="eventType">The command event type to unsubscribe from.</param>
  /// <param name="callback">The callback to remove.</param>
  /// <returns>True if the callback was removed; otherwise, false.</returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  internal static bool Off(CommandEvents eventType, Action<PlayerData, CommandInfo, string[]> callback) {
    if (callback == null) return false;
    if (_commandHandlers.TryGetValue(eventType, out var list)) {
      int idx = list.FindIndex(h => AreDelegatesEqual(h.Handler, callback));
      if (idx >= 0) {
        list.RemoveAt(idx);
        if (list.Count == 0) _commandHandlers.Remove(eventType);
        return true;
      }
    }
    return false;
  }

  /// <summary>
  /// Subscribes a callback to a command event for a single invocation. The handler is automatically removed after being called once.
  /// </summary>
  /// <param name="eventType">The command event type to subscribe to.</param>
  /// <param name="callback">The callback to invoke once when the event is emitted.</param>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  internal static void Once(CommandEvents eventType, Action<PlayerData, CommandInfo, string[]> callback) {
    if (callback == null) return;
    void wrapped(PlayerData player, CommandInfo info, string[] args) {
      try {
        callback(player, info, args);
      } finally {
        Off(eventType, wrapped);
      }
    }
    On(eventType, wrapped);
  }

  /// <summary>
  /// Emits a command event, invoking all registered handlers for the specified event type.
  /// </summary>
  /// <param name="eventType">The command event type to emit.</param>
  /// <param name="player">The player associated with the command event.</param>
  /// <param name="commandInfo">The command info associated with the event.</param>
  /// <param name="args">The command arguments.</param>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  internal static void Emit(CommandEvents eventType, PlayerData player, CommandInfo commandInfo, string[] args) {
    if (!_commandHandlers.TryGetValue(eventType, out var handlers) || handlers.Count == 0) return;
    for (int i = 0; i < handlers.Count; i++) {
      try {
        if (handlers[i].Handler is Action<PlayerData, CommandInfo, string[]> cb) {
          cb(player, commandInfo, args);
        }
      } catch (Exception ex) {
        Log.Error($"[EventManager] Error in CommandEvents handler: {ex}");
      }
    }
  }
  /// <summary>
  /// Gets the number of subscribers for a command event.
  /// </summary>
  /// <param name="eventType">The command event type.</param>
  /// <returns>The number of subscribers.</returns>
  internal static int GetSubscriberCount(CommandEvents eventType) {
    return _commandHandlers.TryGetValue(eventType, out var list) ? list.Count : 0;
  }

  /// <summary>
  /// Subscribes a callback to a prefix event for a single invocation. The handler is automatically removed after being called once.
  /// </summary>
  /// <param name="eventType">The prefix event type to subscribe to.</param>
  /// <param name="callback">The callback to invoke once when the event is emitted.</param>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void Once(PrefixEvents eventType, Action<NativeArray<Entity>> callback) {
    if (callback == null) return;
    void wrapped(NativeArray<Entity> entities) {
      try {
        callback(entities);
      } finally {
        Off(eventType, wrapped);
      }
    }

    On(eventType, wrapped);
  }

  /// <summary>
  /// Subscribes a callback to a postfix event for a single invocation. The handler is automatically removed after being called once.
  /// </summary>
  /// <param name="eventType">The postfix event type to subscribe to.</param>
  /// <param name="callback">The callback to invoke once when the event is emitted.</param>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void Once(PostfixEvents eventType, Action<NativeArray<Entity>> callback) {
    if (callback == null) return;
    void wrapped(NativeArray<Entity> entities) {
      try {
        callback(entities);
      } finally {
        Off(eventType, wrapped);
      }
    }

    On(eventType, wrapped);
  }

  /// <summary>
  /// Subscribes a callback to a player event for a single invocation. The handler is automatically removed after being called once.
  /// </summary>
  /// <param name="eventType">The player event type to subscribe to.</param>
  /// <param name="callback">The callback to invoke once when the event is emitted.</param>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void Once(PlayerEvents eventType, Action<PlayerData> callback) {
    if (callback == null) return;
    void wrapped(PlayerData pdata) {
      try {
        callback(pdata);
      } finally {
        Off(eventType, wrapped);
      }
    }

    On(eventType, wrapped);
  }

  /// <summary>
  /// Subscribes a callback to a server event for a single invocation. The handler is automatically removed after being called once.
  /// </summary>
  /// <param name="eventType">The server event type to subscribe to.</param>
  /// <param name="callback">The callback to invoke once when the event is emitted.</param>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void Once(ServerEvents eventType, Action callback) {
    if (callback == null) return;
    void wrapped() {
      try {
        callback();
      } finally {
        Off(eventType, wrapped);
      }
    }

    On(eventType, wrapped);
  }

  /// <summary>
  /// Subscribes a callback with a string parameter to a server event for a single invocation. The handler is automatically removed after being called once.
  /// </summary>
  /// <param name="eventType">The server event type to subscribe to.</param>
  /// <param name="callback">The callback to invoke once when the event is emitted.</param>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void Once(ServerEvents eventType, Action<string> callback) {
    if (callback == null) return;
    void wrapped(string data) {
      try {
        callback(data);
      } finally {
        Off(eventType, wrapped);
      }
    }

    On(eventType, wrapped);
  }

  /// <summary>
  /// Emits a prefix event, invoking all registered handlers for the specified event type.
  /// </summary>
  /// <param name="eventType">The prefix event type to emit.</param>
  /// <param name="entityArray">The array of entities associated with the event.</param>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void Emit(PrefixEvents eventType, NativeArray<Entity> entityArray) {
    if (!_prefixHandlers.TryGetValue(eventType, out var handlers) || handlers.Count == 0) return;
    for (int i = 0; i < handlers.Count; i++) {
      try { handlers[i].Handler(entityArray); } catch (Exception ex) { Log.Error($"EventManager: Error in prefix '{eventType}': {ex}"); }
    }
  }

  /// <summary>
  /// Emits a postfix event, invoking all registered handlers for the specified event type.
  /// </summary>
  /// <param name="eventType">The postfix event type to emit.</param>
  /// <param name="entityArray">The array of entities associated with the event.</param>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void Emit(PostfixEvents eventType, NativeArray<Entity> entityArray) {
    if (!_postfixHandlers.TryGetValue(eventType, out var handlers) || handlers.Count == 0) return;
    for (int i = 0; i < handlers.Count; i++) {
      try { handlers[i].Handler(entityArray); } catch (Exception ex) { Log.Error($"EventManager: Error in postfix '{eventType}': {ex}"); }
    }
  }

  /// <summary>
  /// Emits a player event, invoking all registered handlers for the specified event type.
  /// </summary>
  /// <param name="eventType">The player event type to emit.</param>
  /// <param name="playerData">The player data associated with the event.</param>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void Emit(PlayerEvents eventType, PlayerData playerData) {
    if (!_playerHandlers.TryGetValue(eventType, out var handlers) || handlers.Count == 0) return;
    for (int i = 0; i < handlers.Count; i++) {
      try { handlers[i].Handler(playerData); } catch (Exception ex) { Log.Error($"EventManager: Error in player '{eventType}': {ex}"); }
    }
  }

  /// <summary>
  /// Emits a server event, invoking all registered handlers for the specified event type.
  /// </summary>
  /// <param name="eventType">The server event type to emit.</param>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void Emit(ServerEvents eventType) {
    if (!_serverHandlers.TryGetValue(eventType, out var handlers) || handlers.Count == 0) return;
    for (int i = 0; i < handlers.Count; i++) {
      try {
        if (handlers[i].Handler is Action action) {
          action();
        } else if (handlers[i].Handler is Action<string> actionWithParam) {
          actionWithParam(null);
        }
      } catch (Exception ex) { Log.Error($"EventManager: Error in server '{eventType}': {ex}"); }
    }
  }

  /// <summary>
  /// Emits a server event with a string parameter, invoking all registered handlers for the specified event type.
  /// </summary>
  /// <param name="eventType">The server event type to emit.</param>
  /// <param name="data">The string data to pass to handlers.</param>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void Emit(ServerEvents eventType, string data) {
    if (!_serverHandlers.TryGetValue(eventType, out var handlers) || handlers.Count == 0) return;
    for (int i = 0; i < handlers.Count; i++) {
      try {
        if (handlers[i].Handler is Action action) {
          action();
        } else if (handlers[i].Handler is Action<string> actionWithParam) {
          actionWithParam(data);
        }
      } catch (Exception ex) { Log.Error($"EventManager: Error in server '{eventType}': {ex}"); }
    }
  }

  // --- Custom event methods (optimized) ---
  /// <summary>
  /// Creates an <see cref="EventHandlerInfo"/> for a custom event handler, compiling a fast invoker and extracting type information.
  /// </summary>
  /// <param name="callback">The delegate to wrap.</param>
  /// <returns>The created <see cref="EventHandlerInfo"/>.</returns>
  private static EventHandlerInfo CreateHandlerInfo(Delegate callback) {
    var invokeMethod = callback.GetType().GetMethod("Invoke");
    var parameters = invokeMethod.GetParameters();
    var returnType = invokeMethod.ReturnType;

    Type expectedType = null;
    bool isNullable = true;
    bool isCancellable = returnType == typeof(bool);
    Action<object> fastInvoker = null;
    Func<object, bool> cancellableInvoker = null;

    // Get priority from the method
    int priority = 0;
    var priorityAttr = callback.Method.GetCustomAttribute<EventPriorityAttribute>();
    if (priorityAttr != null) {
      priority = priorityAttr.Priority;
    }

    if (parameters.Length == 0) {
      if (isCancellable) {
        cancellableInvoker = _ => (bool)callback.DynamicInvoke();
      } else {
        fastInvoker = _ => callback.DynamicInvoke();
      }
    } else if (parameters.Length == 1) {
      expectedType = parameters[0].ParameterType;
      isNullable = !expectedType.IsValueType || Nullable.GetUnderlyingType(expectedType) != null;

      if (isCancellable) {
        var dataParam = Expression.Parameter(typeof(object), "data");
        var convertedParam = Expression.Convert(dataParam, expectedType);
        var invokeExpr = Expression.Invoke(Expression.Constant(callback), convertedParam);
        var lambda = Expression.Lambda<Func<object, bool>>(invokeExpr, dataParam);
        cancellableInvoker = lambda.Compile();
      } else {
        var dataParam = Expression.Parameter(typeof(object), "data");
        var convertedParam = Expression.Convert(dataParam, expectedType);
        var invokeExpr = Expression.Invoke(Expression.Constant(callback), convertedParam);
        var lambda = Expression.Lambda<Action<object>>(invokeExpr, dataParam);
        fastInvoker = lambda.Compile();
      }
    } else {
      expectedType = typeof(object[]);
      if (isCancellable) {
        cancellableInvoker = data => {
          if (data is object[] arr && arr.Length == parameters.Length) {
            return (bool)callback.DynamicInvoke(arr);
          } else {
            Log.Warning($"EventManager: Parameter count mismatch for multi-param delegate");
            return true; // Continue on error
          }
        };
      } else {
        fastInvoker = data => {
          if (data is object[] arr && arr.Length == parameters.Length) {
            callback.DynamicInvoke(arr);
          } else {
            Log.Warning($"EventManager: Parameter count mismatch for multi-param delegate");
          }
        };
      }
    }

    return new EventHandlerInfo {
      Original = callback,
      FastInvoker = fastInvoker,
      CancellableInvoker = cancellableInvoker,
      ExpectedType = expectedType,
      IsNullable = isNullable,
      IsCancellable = isCancellable,
      Priority = priority
    };
  }

  /// <summary>
  /// Subscribes a delegate to a custom event by name.
  /// </summary>
  /// <param name="eventName">The name of the custom event.</param>
  /// <param name="callback">The delegate to invoke when the event is emitted.</param>
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
      // Insert handler sorted by priority (higher first)
      int idx = handlers.FindIndex(h => h.Priority < handlerInfo.Priority);
      if (idx >= 0) handlers.Insert(idx, handlerInfo);
      else handlers.Add(handlerInfo);
    }
  }

  /// <summary>
  /// Unsubscribes a delegate from a custom event by name.
  /// </summary>
  /// <param name="eventName">The name of the custom event.</param>
  /// <param name="callback">The delegate to remove.</param>
  /// <returns>True if the delegate was removed; otherwise, false.</returns>
  public static bool Off(string eventName, Delegate callback) {
    if (string.IsNullOrWhiteSpace(eventName) || callback == null) return false;

    lock (_customLock) {
      if (_customHandlers.TryGetValue(eventName, out var handlers)) {
        int index = handlers.FindIndex(h => AreDelegatesEqual(h.Original, callback));
        if (index >= 0) {
          handlers.RemoveAt(index);
          if (handlers.Count == 0) _customHandlers.TryRemove(eventName, out _);
          return true;
        }
      }
    }
    return false;
  }

  /// <summary>
  /// Subscribes a delegate to a custom event for a single invocation. The handler is automatically removed after being called once.
  /// </summary>
  /// <param name="eventName">The name of the custom event.</param>
  /// <param name="callback">The delegate to invoke once when the event is emitted.</param>
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

  /// <summary>
  /// Emits a custom event by name, invoking all registered handlers with the provided data.
  /// Handlers are invoked in priority order (highest first).
  /// If a handler returns false, the event propagation is cancelled and subsequent handlers are not invoked.
  /// </summary>
  /// <param name="eventName">The name of the custom event to emit.</param>
  /// <param name="data">The data to pass to handlers (optional).</param>
  /// <returns>True if the event propagated through all handlers; false if cancelled by a handler.</returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static bool Emit(string eventName, object data = null) {
    if (string.IsNullOrWhiteSpace(eventName)) {
      Log.Warning("EventManager: Event name cannot be null or empty");
      return true;
    }

    EventHandlerInfo[] handlersToExecute;
    lock (_customLock) {
      if (!_customHandlers.TryGetValue(eventName, out var handlers) || handlers.Count == 0) return true;
      handlersToExecute = [.. handlers];
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

        // If handler is cancellable (returns bool), check the return value
        if (handler.IsCancellable) {
          bool shouldContinue = handler.CancellableInvoker(data);
          if (!shouldContinue) {
            // Event propagation cancelled by handler
            return false;
          }
        } else {
          // Non-cancellable handler (void), always continue
          handler.FastInvoker(data);
        }
      } catch (Exception ex) {
        Log.Error($"EventManager: Error executing callback for '{eventName}': {ex}");
        // Continue to next handler even on error
      }
    }
    return true;
  }

  // --- Utility Methods ---

  /// <summary>
  /// Gets the number of subscribers for a custom event by name.
  /// </summary>
  /// <param name="eventName">The name of the custom event.</param>
  /// <returns>The number of subscribers.</returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static int GetSubscriberCount(string eventName) {
    if (string.IsNullOrWhiteSpace(eventName)) return 0;
    lock (_customLock) {
      return _customHandlers.TryGetValue(eventName, out var handlers) ? handlers.Count : 0;
    }
  }

  /// <summary>
  /// Gets the number of subscribers for a prefix event.
  /// </summary>
  /// <param name="eventType">The prefix event type.</param>
  /// <returns>The number of subscribers.</returns>
  public static int GetSubscriberCount(PrefixEvents eventType) {
    return _prefixHandlers.TryGetValue(eventType, out var list) ? list.Count : 0;
  }

  /// <summary>
  /// Gets the number of subscribers for a postfix event.
  /// </summary>
  /// <param name="eventType">The postfix event type.</param>
  /// <returns>The number of subscribers.</returns>
  public static int GetSubscriberCount(PostfixEvents eventType) {
    return _postfixHandlers.TryGetValue(eventType, out var list) ? list.Count : 0;
  }

  /// <summary>
  /// Gets the number of subscribers for a player event.
  /// </summary>
  /// <param name="eventType">The player event type.</param>
  /// <returns>The number of subscribers.</returns>
  public static int GetSubscriberCount(PlayerEvents eventType) {
    return _playerHandlers.TryGetValue(eventType, out var list) ? list.Count : 0;
  }

  /// <summary>
  /// Gets the number of subscribers for a server event.
  /// </summary>
  /// <param name="eventType">The server event type.</param>
  /// <returns>The number of subscribers.</returns>
  public static int GetSubscriberCount(ServerEvents eventType) {
    return _serverHandlers.TryGetValue(eventType, out var list) ? list.Count : 0;
  }

  /// <summary>
  /// Gets a collection of all registered custom event names.
  /// </summary>
  /// <returns>An enumerable of registered event names.</returns>
  public static IEnumerable<string> GetRegisteredEvents() {
    lock (_customLock) {
      return [.. _customHandlers.Keys];
    }
  }

  /// <summary>
  /// Removes all handlers for a specific custom event by name.
  /// </summary>
  /// <param name="eventName">The name of the custom event to clear.</param>
  /// <returns>True if the event was cleared; otherwise, false.</returns>
  public static bool ClearEvent(string eventName) {
    if (string.IsNullOrWhiteSpace(eventName)) return false;
    lock (_customLock) {
      return _customHandlers.TryRemove(eventName, out _);
    }
  }

  /// <summary>
  /// Removes all custom events and their handlers from the event manager.
  /// </summary>
  public static void ClearAllEvents() {
    lock (_customLock) {
      int count = _customHandlers.Count;
      _customHandlers.Clear();
      Log.Warning($"EventManager: Cleared all {count} custom events");
    }
  }

  /// <summary>
  /// Gets statistics for all custom events, including the number of handlers for each event.
  /// </summary>
  /// <returns>A dictionary mapping event names to their handler counts.</returns>
  public static Dictionary<string, int> GetEventStatistics() {
    var stats = new Dictionary<string, int>();
    lock (_customLock) {
      foreach (var kv in _customHandlers) stats[kv.Key] = kv.Value.Count;
    }
    return stats;
  }

  /// <summary>
  /// Unregisters all event handlers associated with a specific assembly from all event types.
  /// </summary>
  /// <param name="assembly">The assembly to unregister. If null, uses the calling assembly.</param>
  /// <returns>The number of handlers removed.</returns>
  public static int UnregisterAssembly(Assembly assembly = null) {
    Assembly asm;
    if (assembly == null) {
      var stackTrace = new System.Diagnostics.StackTrace();
      var callingMethod = stackTrace.GetFrame(1)?.GetMethod();
      asm = callingMethod?.DeclaringType?.Assembly ?? Assembly.GetExecutingAssembly();
    } else {
      asm = assembly;
    }

    int removed = 0;

    lock (_customLock) {
      foreach (var kv in _customHandlers) {
        var handlers = kv.Value;
        for (int i = handlers.Count - 1; i >= 0; i--) {
          if (handlers[i].Original.Method?.DeclaringType?.Assembly == asm) {
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
        if (handlers[i].Handler.Method?.DeclaringType?.Assembly == asm) {
          handlers.RemoveAt(i);
          removed++;
        }
      }
      if (handlers.Count == 0) _prefixHandlers.Remove(kv.Key);
    }

    foreach (var kv in _postfixHandlers) {
      var handlers = kv.Value;
      for (int i = handlers.Count - 1; i >= 0; i--) {
        if (handlers[i].Handler.Method?.DeclaringType?.Assembly == asm) {
          handlers.RemoveAt(i);
          removed++;
        }
      }
      if (handlers.Count == 0) _postfixHandlers.Remove(kv.Key);
    }

    foreach (var kv in _playerHandlers) {
      var handlers = kv.Value;
      for (int i = handlers.Count - 1; i >= 0; i--) {
        if (handlers[i].Handler.Method?.DeclaringType?.Assembly == asm) {
          handlers.RemoveAt(i);
          removed++;
        }
      }
      if (handlers.Count == 0) _playerHandlers.Remove(kv.Key);
    }

    foreach (var kv in _serverHandlers) {
      var handlers = kv.Value;
      for (int i = handlers.Count - 1; i >= 0; i--) {
        if (handlers[i].Handler is Delegate d && d.Method?.DeclaringType?.Assembly == asm) {
          handlers.RemoveAt(i);
          removed++;
        }
      }
      if (handlers.Count == 0) _serverHandlers.Remove(kv.Key);
    }

    return removed;
  }
}