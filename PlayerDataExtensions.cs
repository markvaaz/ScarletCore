using ProjectM;
using ProjectM.Network;
using ScarletCore.Data;
using ScarletCore.Services;
using Unity.Entities;

namespace ScarletCore;

public static class PlayerDataExtensions {
  public static PlayerData GetPlayerData(this string playerName) {
    return PlayerService.TryGetByName(playerName, out PlayerData playerData) ? playerData : null;
  }

  public static PlayerData GetPlayerData(this ulong playerId) {
    return PlayerService.TryGetById(playerId, out PlayerData playerData) ? playerData : null;
  }

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

  public static PlayerData GetPlayerData(this NetworkId networkId) {
    return PlayerService.TryGetByNetworkId(networkId, out PlayerData playerData) ? playerData : null;
  }

  public static PlayerData GetPlayerData(this User user) {
    return PlayerService.TryGetById(user.PlatformId, out PlayerData playerData) ? playerData : null;
  }
}