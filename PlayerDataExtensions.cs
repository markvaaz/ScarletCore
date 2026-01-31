using ProjectM;
using ProjectM.Network;
using ScarletCore.Services;
using Unity.Entities;

namespace ScarletCore;

/// <summary>
/// Provides extension methods for retrieving PlayerData from various types.
/// </summary>
public static class PlayerDataExtensions {
  /// <summary>
  /// Gets the PlayerData for the specified player name.
  /// </summary>
  /// <param name="playerName">The player name.</param>
  /// <returns>The PlayerData if found; otherwise, null.</returns>
  public static PlayerData GetPlayerData(this string playerName) {
    return PlayerService.TryGetByName(playerName, out PlayerData playerData) ? playerData : null;
  }

  /// <summary>
  /// Gets the PlayerData for the specified player ID.
  /// </summary>
  /// <param name="playerId">The player ID.</param>
  /// <returns>The PlayerData if found; otherwise, null.</returns>
  public static PlayerData GetPlayerData(this ulong playerId) {
    return PlayerService.TryGetById(playerId, out PlayerData playerData) ? playerData : null;
  }

  /// <summary>
  /// Gets the PlayerData for the specified entity, if it represents a user or player character.
  /// </summary>
  /// <param name="entity">The entity to query.</param>
  /// <returns>The PlayerData if found; otherwise, null.</returns>
  public static PlayerData GetPlayerData(this Entity entity) {
    if (entity.Has<User>()) {
      return PlayerService.TryGetById(entity.Read<User>().PlatformId, out PlayerData playerData) ? playerData : null;
    }

    if (entity.Has<PlayerCharacter>()) {
      var playerCharacter = entity.Read<PlayerCharacter>();
      var user = playerCharacter.UserEntity.Read<User>();
      return PlayerService.TryGetById(user.PlatformId, out PlayerData playerData) ? playerData : null;
    }

    return null;
  }

  /// <summary>
  /// Gets the PlayerData for the specified network ID.
  /// </summary>
  /// <param name="networkId">The network ID.</param>
  /// <returns>The PlayerData if found; otherwise, null.</returns>
  public static PlayerData GetPlayerData(this NetworkId networkId) {
    return PlayerService.TryGetByNetworkId(networkId, out PlayerData playerData) ? playerData : null;
  }

  /// <summary>
  /// Gets the PlayerData for the specified User.
  /// </summary>
  /// <param name="user">The User instance.</param>
  /// <returns>The PlayerData if found; otherwise, null.</returns>
  public static PlayerData GetPlayerData(this User user) {
    return PlayerService.TryGetById(user.PlatformId, out PlayerData playerData) ? playerData : null;
  }

  /// <summary>
  /// Tries to get the PlayerData for the specified player name.
  /// </summary>
  /// <param name="playerName">The player name.</param>
  /// <param name="playerData">Output PlayerData if found.</param>
  /// <returns>True if found; otherwise, false.</returns>
  public static bool TryGetPlayerData(this string playerName, out PlayerData playerData) {
    return PlayerService.TryGetByName(playerName, out playerData);
  }

  /// <summary>
  /// Tries to get the PlayerData for the specified player ID.
  /// </summary>
  /// <param name="playerId">The player ID.</param>
  /// <param name="playerData">Output PlayerData if found.</param>
  /// <returns>True if found; otherwise, false.</returns>
  public static bool TryGetPlayerData(this ulong playerId, out PlayerData playerData) {
    return PlayerService.TryGetById(playerId, out playerData);
  }

  /// <summary>
  /// Tries to get the PlayerData for the specified entity, if it represents a user or player character.
  /// </summary>
  /// <param name="entity">The entity to query.</param>
  /// <param name="playerData">Output PlayerData if found.</param>
  /// <returns>True if found; otherwise, false.</returns>
  public static bool TryGetPlayerData(this Entity entity, out PlayerData playerData) {
    playerData = null;
    if (entity.Has<User>()) {
      var user = entity.Read<User>();
      return PlayerService.TryGetById(user.PlatformId, out playerData);
    }

    if (entity.Has<PlayerCharacter>()) {
      var playerCharacter = entity.Read<PlayerCharacter>();
      var user = playerCharacter.UserEntity.Read<User>();
      return PlayerService.TryGetById(user.PlatformId, out playerData);
    }

    return false;
  }

  /// <summary>
  /// Tries to get the PlayerData for the specified network ID.
  /// </summary>
  /// <param name="networkId">The network ID.</param>
  /// <param name="playerData">Output PlayerData if found.</param>
  /// <returns>True if found; otherwise, false.</returns>
  public static bool TryGetPlayerData(this NetworkId networkId, out PlayerData playerData) {
    return PlayerService.TryGetByNetworkId(networkId, out playerData);
  }

  /// <summary>
  /// Tries to get the PlayerData for the specified User.
  /// </summary>
  /// <param name="user">The User instance.</param>
  /// <param name="playerData">Output PlayerData if found.</param>
  /// <returns>True if found; otherwise, false.</returns>
  public static bool TryGetPlayerData(this User user, out PlayerData playerData) {
    return PlayerService.TryGetById(user.PlatformId, out playerData);
  }
}