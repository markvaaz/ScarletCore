using System;
using System.Collections.Generic;
using ScarletCore.Utils;

namespace ScarletCore.Events;

/// <summary>
/// Deprecated compatibility shim. Use EventManager for all event operations.
/// This class forwards calls to EventManager and exists only to avoid breaking
/// existing call sites while migrating away from the old API.
/// </summary>
[Obsolete("CustomEventManager is deprecated. Use EventManager instead.")]
public static class CustomEventManager {
  // Forwarders to EventManager for backward compatibility (custom events only)
  public static void On(string eventName, Delegate callback) => EventManager.On(eventName, callback);

  public static bool Off(string eventName, Delegate callback) => EventManager.Off(eventName, callback);

  public static void Once(string eventName, Delegate callback) => EventManager.Once(eventName, callback);

  public static void Emit(string eventName, object data = null) => EventManager.Emit(eventName, data);

  public static int GetSubscriberCount(string eventName) => EventManager.GetSubscriberCount(eventName);

  public static IEnumerable<string> GetRegisteredEvents() => EventManager.GetRegisteredEvents();

  public static bool ClearEvent(string eventName) => EventManager.ClearEvent(eventName);

  public static void ClearAllEvents() => EventManager.ClearAllEvents();

  public static Dictionary<string, int> GetEventStatistics() => EventManager.GetEventStatistics();

  public static int UnregisterAssembly(System.Reflection.Assembly assembly) => EventManager.UnregisterAssembly(assembly);
}
