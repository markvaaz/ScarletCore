using BepInEx.Logging;
using ScarletCore.Systems;
using ScarletCore.Data;
using Unity.Entities;
using System;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace ScarletCore.Utils;

/// <summary>
/// Static logger utility for easier access to Plugin.LogInstance.
/// </summary>
public static class Log {
  /// <summary>
  /// Gets or sets whether debug mode is enabled for enhanced logging with caller information.
  /// When false, caller information is not included to save performance.
  /// </summary>
  public static bool DebugMode { get; set; } = false;

  // Cache of resolved ManualLogSource per assembly to avoid repeated reflection
  private static readonly ConcurrentDictionary<Assembly, ManualLogSource> _assemblyLogCache = new();

  /// <summary>
  /// Gets the Plugin's LogInstance for logging operations. Tries to resolve a
  /// ManualLogSource from the calling assembly first; falls back to the core
  /// Plugin.LogInstance when none is found.
  /// </summary>
  private static ManualLogSource MLS {
    get {
      try {
        var caller = GetCallerLogSource();
        if (caller != null) return caller;
      } catch {
        // ignore and fallback
      }
      return Plugin.LogInstance;
    }
  }

  // Walk the stack trace, find the first assembly that is not this one and try
  // to resolve a ManualLogSource for it (cached).
  private static ManualLogSource GetCallerLogSource() {
    var thisAssembly = typeof(Log).Assembly;
    var frames = new StackTrace(skipFrames: 1, fNeedFileInfo: false).GetFrames();
    if (frames == null) return null;

    foreach (var frame in frames) {
      var method = frame.GetMethod();
      if (method == null) continue;
      var declType = method.DeclaringType;
      if (declType == null) continue;
      var asm = declType.Assembly;
      if (asm == thisAssembly) continue;
      // Prefer assemblies that are not framework assemblies
      if (asm.FullName != null && (asm.FullName.StartsWith("System", StringComparison.Ordinal) || asm.FullName.StartsWith("Microsoft", StringComparison.Ordinal))) continue;

      if (_assemblyLogCache.TryGetValue(asm, out var cached)) return cached;
      var found = FindManualLogSourceInAssembly(asm);
      _assemblyLogCache[asm] = found; // cache even if null to avoid repeated work
      if (found != null) return found;
    }
    return null;
  }

  // Search the assembly for a static ManualLogSource (property or field).
  private static ManualLogSource FindManualLogSourceInAssembly(Assembly asm) {
    try {
      foreach (var type in asm.GetTypes()) {
        // Fast path: check for well-known static member names
        var pFast = type.GetProperty("LogInstance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (pFast != null && pFast.PropertyType == typeof(ManualLogSource)) {
          try { return pFast.GetValue(null) as ManualLogSource; } catch { }
        }
        var fFast = type.GetField("LogInstance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                 ?? type.GetField("Logger", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (fFast != null && fFast.FieldType == typeof(ManualLogSource)) {
          try { return fFast.GetValue(null) as ManualLogSource; } catch { }
        }

        // General path: any static property/field of type ManualLogSource
        var props = type.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (var p in props) {
          if (p.PropertyType == typeof(ManualLogSource)) {
            try { return p.GetValue(null) as ManualLogSource; } catch { }
          }
        }
        var fields = type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (var f in fields) {
          if (f.FieldType == typeof(ManualLogSource)) {
            try { return f.GetValue(null) as ManualLogSource; } catch { }
          }
        }
      }
    } catch {
      // Reflection may fail for some assemblies; ignore and return null so fallback is used
    }
    return null;
  }

  /// <summary>
  /// Gets the calling method information for enhanced logging
  /// </summary>
  /// <returns>Formatted string with class and method name</returns>
  private static string GetCallerInfo() {
    try {
      var thisAssembly = typeof(Log).Assembly;
      var frames = new StackTrace(skipFrames: 2, fNeedFileInfo: true).GetFrames();
      if (frames == null) return string.Empty;

      foreach (var frame in frames) {
        var method = frame.GetMethod();
        if (method == null) continue;
        var declType = method.DeclaringType;
        if (declType == null) continue;
        var asm = declType.Assembly;
        if (asm == thisAssembly) continue;
        // Skip framework assemblies
        if (asm.FullName != null && (asm.FullName.StartsWith("System", StringComparison.Ordinal) || asm.FullName.StartsWith("Microsoft", StringComparison.Ordinal))) continue;

        var lineNumber = frame.GetFileLineNumber();
        var lineInfo = lineNumber > 0 ? $":L{lineNumber}" : "";
        return $"[{declType.Name}.{method.Name}{lineInfo}]";
      }
    } catch {
      // ignore and return empty
    }
    return string.Empty;
  }

  /// <summary>
  /// Logs a debug message
  /// </summary>
  /// <param name="messages">The messages to log</param>
  public static void Debug(params object[] messages) {
    var callerInfo = DebugMode ? GetCallerInfo() : "";
    var message = string.Join(" ", messages);
    MLS?.LogDebug(DebugMode ? $"{callerInfo} {message}" : message.ToString());
  }

  /// <summary>
  /// Logs an info message
  /// </summary>
  /// <param name="messages">The messages to log</param>
  public static void Info(params object[] messages) {
    var callerInfo = DebugMode ? GetCallerInfo() : "";
    var message = string.Join(" ", messages);
    var coloredMessage = $"\x1b[38;2;222;254;255m{message}\x1b[0m";
    MLS?.LogInfo(DebugMode ? $"{callerInfo} {coloredMessage}" : coloredMessage);
  }

  /// <summary>
  /// Logs a message to the message log with optional formatting and caller information.
  /// </summary>
  /// <param name="messages">The objects to log as a message.</param>
  public static void Message(params object[] messages) {
    var callerInfo = DebugMode ? GetCallerInfo() : "";
    var message = string.Join(" ", messages);
    var coloredMessage = $"\x1b[38;2;222;254;255m{message}\x1b[0m";
    MLS?.LogMessage(DebugMode ? $"{callerInfo} {coloredMessage}" : coloredMessage);
  }

  /// <summary>
  /// Logs a warning message
  /// </summary>
  /// <param name="messages">The messages to log</param>
  public static void Warning(params object[] messages) {
    var callerInfo = DebugMode ? GetCallerInfo() : "";
    var message = string.Join(" ", messages);
    MLS?.LogWarning(DebugMode ? $"{callerInfo} {message}" : message.ToString());
  }

  /// <summary>
  /// Logs an error message
  /// </summary>
  /// <param name="messages">The messages to log</param>
  public static void Error(params object[] messages) {
    var callerInfo = DebugMode ? GetCallerInfo() : "";
    var message = string.Join(" ", messages);
    MLS?.LogError(DebugMode ? $"{callerInfo} {message}" : message.ToString());
  }

  /// <summary>
  /// Logs a fatal error message
  /// </summary>
  /// <param name="messages">The messages to log</param>
  public static void Fatal(params object[] messages) {
    var callerInfo = DebugMode ? GetCallerInfo() : "";
    var message = string.Join(" ", messages);
    MLS?.LogFatal(DebugMode ? $"{callerInfo} {message}" : message.ToString());
  }

  /// <summary>
  /// Logs a message with specified log level
  /// </summary>
  /// <param name="level">The log level</param>
  /// <param name="message">The message to log</param>
  public static void LogLevel(LogLevel level, object message) {
    var callerInfo = DebugMode ? GetCallerInfo() : "";
    MLS?.Log(level, DebugMode ? $"{callerInfo} {message}" : message.ToString());
  }

  /// <summary>
  /// Logs all component types attached to the specified entity.
  /// </summary>
  /// <param name="entity">The entity whose components will be logged.</param>
  public static void Components(Entity entity) {
    if (!entity.Exists()) {
      Warning("Cannot log components for non-existing entity.");
      return;
    }
    var components = GameSystems.EntityManager.GetComponentTypes(entity);
    foreach (var component in components) {
      Info(component);
    }
  }
  /// <summary>
  /// Logs comprehensive PlayerData information for debugging purposes
  /// </summary>
  /// <param name="playerData">The PlayerData to log</param>
  public static void Player(PlayerData playerData) {
    if (playerData == null) {
      Warning("PlayerData is null");
      return;
    }

    Info("=== PlayerData Information ===");
    Info($"  Name: {playerData.Name}");
    Info($"  Cached Name: {playerData.CachedName ?? "null"}");
    Info($"  Platform ID: {playerData.PlatformId}");
    Info($"  Network ID: {playerData.NetworkId}");
    Info($"  Is Online: {playerData.IsOnline}");
    Info($"  Is Admin: {playerData.IsAdmin}");
    Info($"  Connected Since: {playerData.LastConnected}");
    Info($"  Clan Name: {playerData.ClanName ?? "No Clan"}");
    Info($"  User Entity: {playerData.UserEntity}");
    Info($"  Character Entity: {playerData.CharacterEntity}");
    Info("==============================");
  }
}
