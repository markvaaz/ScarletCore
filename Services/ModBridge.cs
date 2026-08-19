using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using ScarletCore.Utils;

namespace ScarletCore.Services;

/// <summary>
/// Provides request/response (two-way) communication between mods without direct assembly references.
/// <para>
/// Unlike <see cref="ScarletCore.Events.EventManager"/> — which broadcasts fire-and-forget events to any
/// number of listeners — a bridge service has a <b>single provider</b> that answers a call and returns a value.
/// Use this when one mod needs to ask another for something and get an answer back (RPC).
/// </para>
/// <para>
/// The request/response types must be the <b>same types both sides reference</b>: use primitives, collections
/// (e.g. <c>Dictionary&lt;string, object&gt;</c>), or a small shared "contracts" assembly holding the DTOs.
/// The consumer never needs a reference to the provider's assembly — only to the shared contract types.
/// </para>
/// <example>
/// <code>
/// // Provider (e.g. a chat mod):
/// ModBridge.Register("ScarletChat.GetChannels", (Func&lt;string, List&lt;string&gt;&gt;)(filter =&gt; ...));
///
/// // Consumer (any mod, no reference to the chat mod):
/// if (ModBridge.TryCall("ScarletChat.GetChannels", "trade", out List&lt;string&gt; channels)) { ... }
/// </code>
/// </example>
/// </summary>
public static class ModBridge {
  private static readonly ConcurrentDictionary<string, Delegate> _services = new();

  /// <summary>
  /// Registers a service handler that takes a request and returns a response.
  /// If a service with the same id already exists it is overwritten (a warning is logged).
  /// </summary>
  /// <typeparam name="TReq">The request type.</typeparam>
  /// <typeparam name="TRes">The response type.</typeparam>
  /// <param name="serviceId">Unique service identifier, e.g. "MyMod.DoThing".</param>
  /// <param name="handler">The function that produces the response.</param>
  public static void Register<TReq, TRes>(string serviceId, Func<TReq, TRes> handler) {
    RegisterInternal(serviceId, handler);
  }

  /// <summary>
  /// Registers a parameterless service handler that returns a response.
  /// If a service with the same id already exists it is overwritten (a warning is logged).
  /// </summary>
  /// <typeparam name="TRes">The response type.</typeparam>
  /// <param name="serviceId">Unique service identifier, e.g. "MyMod.GetState".</param>
  /// <param name="handler">The function that produces the response.</param>
  public static void Register<TRes>(string serviceId, Func<TRes> handler) {
    RegisterInternal(serviceId, handler);
  }

  private static void RegisterInternal(string serviceId, Delegate handler) {
    if (string.IsNullOrWhiteSpace(serviceId))
      throw new ArgumentException("Service id cannot be null or empty", nameof(serviceId));
    if (handler == null)
      throw new ArgumentNullException(nameof(handler));

    if (_services.ContainsKey(serviceId))
      Log.Warning($"ModBridge: Service '{serviceId}' is being overwritten by {handler.Method?.DeclaringType?.Assembly.GetName().Name}");

    _services[serviceId] = handler;
  }

  /// <summary>
  /// Removes a registered service.
  /// </summary>
  /// <param name="serviceId">The service identifier to remove.</param>
  /// <returns>True if a service was removed; otherwise, false.</returns>
  public static bool Unregister(string serviceId) {
    if (string.IsNullOrWhiteSpace(serviceId)) return false;
    return _services.TryRemove(serviceId, out _);
  }

  /// <summary>
  /// Checks whether a provider is registered for the given service id.
  /// Use this for capability discovery before calling (the provider mod may not be installed).
  /// </summary>
  /// <param name="serviceId">The service identifier.</param>
  /// <returns>True if a provider is registered.</returns>
  public static bool Has(string serviceId) {
    return !string.IsNullOrWhiteSpace(serviceId) && _services.ContainsKey(serviceId);
  }

  /// <summary>
  /// Calls a service with a request and returns its response.
  /// Throws if no provider is registered or the signature does not match — use <see cref="TryCall{TReq, TRes}"/> for a safe call.
  /// </summary>
  /// <typeparam name="TReq">The request type.</typeparam>
  /// <typeparam name="TRes">The response type.</typeparam>
  /// <param name="serviceId">The service identifier.</param>
  /// <param name="request">The request to pass to the provider.</param>
  /// <returns>The provider's response.</returns>
  /// <exception cref="InvalidOperationException">No provider is registered for the service id.</exception>
  /// <exception cref="InvalidCastException">The registered handler does not match the requested signature.</exception>
  public static TRes Call<TReq, TRes>(string serviceId, TReq request) {
    if (!_services.TryGetValue(serviceId, out var handler))
      throw new InvalidOperationException($"ModBridge: No provider registered for service '{serviceId}'");
    if (handler is not Func<TReq, TRes> typed)
      throw new InvalidCastException($"ModBridge: Service '{serviceId}' does not match Func<{typeof(TReq).Name}, {typeof(TRes).Name}>");
    return typed(request);
  }

  /// <summary>
  /// Calls a parameterless service and returns its response.
  /// Throws if no provider is registered or the signature does not match — use <see cref="TryCall{TRes}"/> for a safe call.
  /// </summary>
  /// <typeparam name="TRes">The response type.</typeparam>
  /// <param name="serviceId">The service identifier.</param>
  /// <returns>The provider's response.</returns>
  /// <exception cref="InvalidOperationException">No provider is registered for the service id.</exception>
  /// <exception cref="InvalidCastException">The registered handler does not match the requested signature.</exception>
  public static TRes Call<TRes>(string serviceId) {
    if (!_services.TryGetValue(serviceId, out var handler))
      throw new InvalidOperationException($"ModBridge: No provider registered for service '{serviceId}'");
    if (handler is not Func<TRes> typed)
      throw new InvalidCastException($"ModBridge: Service '{serviceId}' does not match Func<{typeof(TRes).Name}>");
    return typed();
  }

  /// <summary>
  /// Safely calls a service with a request. Returns false (without throwing) if no provider is registered,
  /// the signature does not match, or the handler throws.
  /// </summary>
  /// <typeparam name="TReq">The request type.</typeparam>
  /// <typeparam name="TRes">The response type.</typeparam>
  /// <param name="serviceId">The service identifier.</param>
  /// <param name="request">The request to pass to the provider.</param>
  /// <param name="response">The provider's response, or default if the call did not succeed.</param>
  /// <returns>True if the call succeeded.</returns>
  public static bool TryCall<TReq, TRes>(string serviceId, TReq request, out TRes response) {
    response = default;
    if (string.IsNullOrWhiteSpace(serviceId)) return false;
    if (!_services.TryGetValue(serviceId, out var handler) || handler is not Func<TReq, TRes> typed) return false;
    try {
      response = typed(request);
      return true;
    } catch (Exception ex) {
      Log.Error($"ModBridge: Service '{serviceId}' threw: {ex}");
      return false;
    }
  }

  /// <summary>
  /// Safely calls a parameterless service. Returns false (without throwing) if no provider is registered,
  /// the signature does not match, or the handler throws.
  /// </summary>
  /// <typeparam name="TRes">The response type.</typeparam>
  /// <param name="serviceId">The service identifier.</param>
  /// <param name="response">The provider's response, or default if the call did not succeed.</param>
  /// <returns>True if the call succeeded.</returns>
  public static bool TryCall<TRes>(string serviceId, out TRes response) {
    response = default;
    if (string.IsNullOrWhiteSpace(serviceId)) return false;
    if (!_services.TryGetValue(serviceId, out var handler) || handler is not Func<TRes> typed) return false;
    try {
      response = typed();
      return true;
    } catch (Exception ex) {
      Log.Error($"ModBridge: Service '{serviceId}' threw: {ex}");
      return false;
    }
  }

  /// <summary>
  /// Gets all registered service ids.
  /// </summary>
  /// <returns>An array of registered service identifiers.</returns>
  public static string[] GetRegisteredServices() {
    return [.. _services.Keys];
  }

  /// <summary>
  /// Removes all services registered by a specific assembly. Call this when a mod unloads
  /// to avoid leaving dangling handlers (mirrors <see cref="ScarletCore.Events.EventManager.UnregisterAssembly"/>).
  /// </summary>
  /// <param name="assembly">The assembly to unregister. If null, uses the calling assembly.</param>
  /// <returns>The number of services removed.</returns>
  public static int UnregisterAssembly(Assembly assembly = null) {
    Assembly asm = assembly;
    if (asm == null) {
      var callingMethod = new StackTrace().GetFrame(1)?.GetMethod();
      asm = callingMethod?.DeclaringType?.Assembly ?? Assembly.GetExecutingAssembly();
    }

    int removed = 0;
    foreach (var id in _services.Where(kv => kv.Value.Method?.DeclaringType?.Assembly == asm).Select(kv => kv.Key).ToList()) {
      if (_services.TryRemove(id, out _)) removed++;
    }
    return removed;
  }
}
