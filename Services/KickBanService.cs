using ProjectM.Network;
using Unity.Entities;
using ScarletCore.Data;
using ScarletCore.Utils;
using ScarletCore.Systems;

namespace ScarletCore.Services;

/// <summary>
/// Provides services for managing player kicks and bans in V Rising server.
/// Handles ban list operations and player removal from the server.
/// </summary>
public static class KickBanService {
  /// <summary>
  /// Adds a player to the ban list using PlayerData.
  /// </summary>
  /// <param name="playerData">The player data containing platform ID</param>
  public static void Ban(PlayerData playerData) {
    Ban(playerData.PlatformId);
  }

  /// <summary>
  /// Adds a player to the ban list by name.
  /// </summary>
  /// <param name="playerName">The name of the player to ban</param>
  public static void Ban(string playerName) {
    if (PlayerService.TryGetByName(playerName, out var player)) {
      Ban(player.PlatformId);
    } else {
      Log.Warning($"KickBanService: Player '{playerName}' not found for ban addition.");
    }
  }

  /// <summary>
  /// Adds a player to the ban list and kicks them if they are currently online.
  /// </summary>
  /// <param name="platformId">The platform ID of the player to ban</param>
  public static void Ban(ulong platformId) {
    // Check if player exists and is currently online
    if (PlayerService.TryGetById(platformId, out var player) || player.IsOnline) {
      // Create a ban event entity to immediately kick the online player
      var entityEvent = GameSystems.EntityManager.CreateEntity(
        ComponentType.ReadWrite<FromCharacter>(),
        ComponentType.ReadWrite<BanEvent>()
      );

      // Associate the ban event with the player's character and user entities
      entityEvent.Write(new FromCharacter() {
        Character = player.CharacterEntity,
        User = player.UserEntity
      });
    }

    // Add player to the persistent ban list
    GameSystems.KickBanSystem._LocalBanList.Add(platformId);
    GameSystems.KickBanSystem._LocalBanList.Save();      // Persist changes to disk
    GameSystems.KickBanSystem._LocalBanList.Refresh();   // Reload the ban list
  }

  /// <summary>
  /// Removes a player from the ban list using PlayerData.
  /// </summary>
  /// <param name="playerData">The player data containing platform ID</param>
  public static void Unban(PlayerData playerData) {
    Unban(playerData.PlatformId);
  }

  /// <summary>
  /// Removes a player from the ban list by name.
  /// </summary>
  /// <param name="playerName">The name of the player to unban</param>
  public static void Unban(string playerName) {
    if (PlayerService.TryGetByName(playerName, out var player)) {
      Unban(player.PlatformId);
    } else {
      Log.Warning($"KickBanService: Player '{playerName}' not found for ban removal.");
    }
  }

  /// <summary>
  /// Removes a player from the ban list, allowing them to join the server again.
  /// </summary>
  /// <param name="platformId">The platform ID of the player to unban</param>
  public static void Unban(ulong platformId) {
    // Remove player from the ban list
    GameSystems.KickBanSystem._LocalBanList.Remove(platformId);
    GameSystems.KickBanSystem._LocalBanList.Save();      // Persist changes to disk
    GameSystems.KickBanSystem._LocalBanList.Refresh();   // Reload the ban list
  }

  /// <summary>
  /// Checks if a player is currently banned using PlayerData.
  /// </summary>
  /// <param name="playerData">The player data containing platform ID</param>
  /// <returns>True if the player is banned, false otherwise</returns>
  public static bool IsBanned(PlayerData playerData) {
    return GameSystems.KickBanSystem.IsBanned(playerData.PlatformId);
  }

  /// <summary>
  /// Checks if a player is currently banned by name.
  /// </summary>
  /// <param name="playerName">The name of the player to check</param>
  /// <returns>True if the player is banned, false otherwise</returns>
  public static bool IsBanned(string playerName) {
    if (PlayerService.TryGetByName(playerName, out var player)) {
      return IsBanned(player.PlatformId);
    }

    Log.Warning($"KickBanService: Player '{playerName}' not found for ban check.");
    return false;
  }

  /// <summary>
  /// Checks if a player is currently banned.
  /// </summary>
  /// <param name="platformId">The platform ID of the player to check</param>
  /// <returns>True if the player is banned, false otherwise</returns>
  public static bool IsBanned(ulong platformId) {
    return GameSystems.KickBanSystem.IsBanned(platformId);
  }

  /// <summary>
  /// Kicks a player from the server using PlayerData.
  /// </summary>
  /// <param name="playerData">The player data containing platform ID</param>
  public static void Kick(PlayerData playerData) {
    Kick(playerData.PlatformId);
  }

  /// <summary>
  /// Kicks a player from the server by name.
  /// </summary>
  /// <param name="playerName">The name of the player to kick</param>
  public static void Kick(string playerName) {
    if (PlayerService.TryGetByName(playerName, out var player)) {
      Kick(player.PlatformId);
    } else {
      Log.Warning($"KickBanService: Player '{playerName}' not found for kick.");
    }
  }

  /// <summary>
  /// Kicks a player from the server without adding them to the ban list.
  /// The player can reconnect immediately after being kicked.
  /// </summary>
  /// <param name="platformId">The platform ID of the player to kick</param>
  public static void Kick(ulong platformId) {
    // Create a kick event entity with required components
    Entity eventEntity = GameSystems.EntityManager.CreateEntity([
      ComponentType.ReadOnly<NetworkEventType>(),
      ComponentType.ReadOnly<SendEventToUser>(),
      ComponentType.ReadOnly<KickEvent>()
    ]);

    // Set the target player for the kick event
    eventEntity.Write(new KickEvent {
      PlatformId = platformId
    });

    // Configure the network event properties
    eventEntity.Write(new NetworkEventType {
      EventId = NetworkEvents.EventId_KickEvent,
      IsAdminEvent = false,    // Not an admin-only event
      IsDebugEvent = false     // Not a debug event
    });
  }
}