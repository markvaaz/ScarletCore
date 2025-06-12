using BepInEx.Logging;
using ScarletCore.Systems;
using ScarletCore.Data;
using Unity.Entities;

namespace ScarletCore.Utils;

/// <summary>
/// Static logger utility for easier access to Plugin.LogInstance.
/// </summary>
public static class Log {
  /// <summary>
  /// Gets the Plugin's LogInstance for logging operations
  /// </summary>
  private static ManualLogSource MLS => Plugin.LogInstance;

  /// <summary>
  /// Logs a debug message
  /// </summary>
  /// <param name="message">The message to log</param>
  public static void Debug(object message) {
    MLS?.LogDebug(message);
  }

  /// <summary>
  /// Logs an info message
  /// </summary>
  /// <param name="message">The message to log</param>
  public static void Info(object message) {
    MLS?.LogInfo(message);
  }

  /// <summary>
  /// Logs a warning message
  /// </summary>
  /// <param name="message">The message to log</param>
  public static void Warning(object message) {
    MLS?.LogWarning(message);
  }

  /// <summary>
  /// Logs an error message
  /// </summary>
  /// <param name="message">The message to log</param>
  public static void Error(object message) {
    MLS?.LogError(message);
  }

  /// <summary>
  /// Logs a fatal error message
  /// </summary>
  /// <param name="message">The message to log</param>
  public static void Fatal(object message) {
    MLS?.LogFatal(message);
  }

  /// <summary>
  /// Logs a message with specified log level
  /// </summary>
  /// <param name="level">The log level</param>
  /// <param name="message">The message to log</param>
  public static void LogLevel(LogLevel level, object message) {
    MLS?.Log(level, message);
  }

  public static void Components(Entity entity) {
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
    Info($"  Connected Since: {playerData.ConnectedSince}");
    Info($"  Clan Name: {playerData.ClanName ?? "No Clan"}");
    Info($"  User Entity: {playerData.UserEntity}");
    Info($"  Character Entity: {playerData.CharacterEntity}");
    Info("==============================");
  }
}
