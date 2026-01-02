using System.Collections.Generic;
using ProjectM;
using ProjectM.Network;
using ScarletCore.Systems;
using ScarletCore.Utils;
using Unity.Collections;
using Unity.Entities;

namespace ScarletCore.Services;

/// <summary>
/// Service class for managing clan operations in V Rising.
/// Provides functionality to manage clan members, add/remove players, and retrieve clan information.
/// </summary>
public static class ClanService {

  /// <summary>
  /// Retrieves all members of a specified clan.
  /// </summary>
  /// <param name="clanName">The name of the clan to get members from (case-insensitive)</param>
  /// <returns>A list of PlayerData objects representing all clan members</returns>
  public static List<PlayerData> GetMembers(string clanName) {
    var members = new List<PlayerData>();

    // Get all clan entities in the game
    var clans = GetClanEntities();

    foreach (var clan in clans) {
      // Skip clans that don't match the specified name (case-insensitive comparison)
      if (!clan.Read<ClanTeam>().Name.ToString().Equals(clanName, System.StringComparison.CurrentCultureIgnoreCase)) continue;

      // Get clan member status and user buffer for this clan
      var clanMembers = clan.ReadBuffer<ClanMemberStatus>();
      var userBuffer = clan.ReadBuffer<SyncToUserBuffer>();

      // Iterate through all clan members
      for (var i = 0; i < clanMembers.Length; ++i) {
        var userBufferEntry = userBuffer[i];

        // Try to get player data by platform ID and add to members list
        if (PlayerService.TryGetById(userBufferEntry.UserEntity.Read<User>().PlatformId, out var playerData)) {
          members.Add(playerData);
        }
      }
    }

    // Clean up native array to prevent memory leaks
    clans.Dispose();
    return members;
  }

  /// <summary>
  /// Attempts to add a player to a specified clan.
  /// </summary>
  /// <param name="playerData">The player data of the player to add</param>
  /// <param name="clanName">The name of the clan to add the player to</param>
  /// <returns>True if the player was successfully added, false otherwise</returns>
  public static bool TryAddMember(PlayerData playerData, string clanName) {
    var userEntity = playerData.UserEntity;
    var user = playerData.User;

    // Check if player is already in a clan
    if (!user.ClanEntity.Equals(NetworkedEntity.Empty)) {
      user.ClanEntity.GetEntityOnServer().Read<ClanTeam>();
      return false; // Player is already in a clan
    }

    // Check if the target clan exists
    if (!TryGetClanEntity(clanName, out var clanEntity)) {
      return false; // Clan does not exist
    }

    // Add user to the clan using the game's utility method
    TeamUtility.AddUserToClan(GameSystems.EntityManager, clanEntity, userEntity, ref user, CastleHeartLimitType.User);

    // Update the user entity with the new clan information
    userEntity.Write(user);

    // Get clan members and user buffer to set the new member's role
    var members = clanEntity.ReadBuffer<ClanMemberStatus>();
    var userBuffer = clanEntity.ReadBuffer<SyncToUserBuffer>();

    // Find the newly added member and set their role to Member
    for (var i = 0; i < members.Length; ++i) {
      var member = members[i];
      var userBufferEntry = userBuffer[i];
      var userToTest = userBufferEntry.UserEntity.Read<User>();

      // Match by character name to identify the new member
      if (userToTest.CharacterName.Equals(user.CharacterName)) {
        member.ClanRole = ClanRoleEnum.Member;
        members[i] = member;
      }
    }

    return true;
  }

  /// <summary>
  /// Attempts to remove a player from their current clan.
  /// Only clan leaders can remove members, and leaders cannot be removed.
  /// </summary>
  /// <param name="playerData">The player data of the player to remove</param>
  /// <returns>True if the player was successfully removed, false otherwise</returns>
  public static bool TryRemoveFromClan(PlayerData playerData) {
    // Get the player's clan entity
    var clanEntity = playerData.User.ClanEntity.GetEntityOnServer();

    // Check if player is actually in a clan
    if (clanEntity.Equals(Entity.Null)) {
      return false; // Player is not in a clan
    }

    // Get clan members and user buffer
    var members = clanEntity.ReadBuffer<ClanMemberStatus>();
    var userBuffer = clanEntity.ReadBuffer<SyncToUserBuffer>();
    bool foundLeader = false;

    FromCharacter fromCharacter = default;

    // Find the clan leader to authorize the kick
    for (var i = 0; i < members.Length; ++i) {
      var member = members[i];
      if (member.ClanRole == ClanRoleEnum.Leader) {
        var userBufferEntry = userBuffer[i];
        // Set up the FromCharacter structure for the kick request
        fromCharacter = new FromCharacter() {
          Character = userBufferEntry.UserEntity.Read<User>().LocalCharacter.GetEntityOnServer(),
          User = userBufferEntry.UserEntity
        };
        foundLeader = true;
        break;
      }
    }

    // Cannot remove players if no leader is found
    if (!foundLeader) {
      return false; // No leader found, cannot remove player
    }

    // Find the target player in the clan members
    for (var i = 0; i < members.Length; ++i) {
      var userBufferEntry = userBuffer[i];

      if (userBufferEntry.UserEntity.Equals(playerData.UserEntity)) {
        var member = members[i];

        // Cannot remove the clan leader
        if (member.ClanRole == ClanRoleEnum.Leader) {
          return false; // Cannot remove the clan leader
        }

        // Create a kick request entity using the game's event system
        var entity = GameSystems.EntityManager.CreateEntity(
          ComponentType.ReadWrite<FromCharacter>(),
          ComponentType.ReadWrite<ClanEvents_Client.Kick_Request>()
        );

        // Write the leader's information (who is performing the kick)
        entity.Write(fromCharacter);

        // Write the kick request with the target user index
        entity.Write(new ClanEvents_Client.Kick_Request {
          TargetUserIndex = members[i].UserIndex
        });

        // Log the kick action for debugging
        Log.Message($"Kicking {playerData.Name} from clan {clanEntity.Read<ClanTeam>().Name}");

        return true; // Player successfully removed from clan
      }
    }

    return false; // Player not found in clan members
  }

  /// <summary>
  /// Attempts to find and return the leader of a specified clan.
  /// </summary>
  /// <param name="clanName">The name of the clan to find the leader for (case-insensitive)</param>
  /// <param name="clanLeader">Output parameter containing the clan leader's PlayerData if found</param>
  /// <returns>True if a clan leader was found, false otherwise</returns>
  public static bool TryGetClanLeader(string clanName, out PlayerData clanLeader) {
    clanLeader = null;

    // Get all clan entities
    var clans = GetClanEntities();

    foreach (var clan in clans) {
      // Skip clans that don't match the specified name
      if (!clan.Read<ClanTeam>().Name.ToString().Equals(clanName, System.StringComparison.CurrentCultureIgnoreCase)) continue;

      // Get clan members
      var members = clan.ReadBuffer<ClanMemberStatus>();

      // Skip empty clans
      if (members.Length == 0) continue;

      // Search for the leader among clan members
      for (var i = 0; i < members.Length; ++i) {
        if (members[i].ClanRole == ClanRoleEnum.Leader) {
          // Get user buffer and extract leader's user entity
          var userBuffer = clan.ReadBuffer<SyncToUserBuffer>();
          var userEntity = userBuffer[i].UserEntity;
          // Get player data by platform ID
          PlayerService.TryGetById(userEntity.Read<User>().PlatformId, out clanLeader);
          return true;
        }
      }
    }

    // Clean up native array
    clans.Dispose();
    return false;
  }

  /// <summary>
  /// Retrieves a dictionary of all clans with their members.
  /// </summary>
  /// <returns>A dictionary where keys are clan names and values are lists of PlayerData for each clan</returns>
  public static Dictionary<string, List<PlayerData>> ListClans() {
    var clanList = new Dictionary<string, List<PlayerData>>();

    // Get all clan entities
    var clans = GetClanEntities();

    foreach (var clan in clans) {
      // Get clan name
      var clanName = clan.Read<ClanTeam>().Name.ToString();
      var members = clan.ReadBuffer<ClanMemberStatus>();
      var userBuffer = clan.ReadBuffer<SyncToUserBuffer>();

      var playerDataList = new List<PlayerData>();

      // Collect all valid player data for this clan
      for (var i = 0; i < members.Length; ++i) {
        var userBufferEntry = userBuffer[i];

        if (PlayerService.TryGetById(userBufferEntry.UserEntity.Read<User>().PlatformId, out var playerData)) {
          playerDataList.Add(playerData);
        }
      }

      // Add clan to the list
      clanList[clanName] = playerDataList;
    }

    // Clean up native array
    clans.Dispose();
    return clanList;
  }

  /// <summary>
  /// Attempts to change a player's role within their clan.
  /// </summary>
  /// <param name="playerData">The player whose role should be changed</param>
  /// <param name="newRole">The new clan role to assign</param>
  /// <returns>True if the role was successfully changed, false if the player is not in a clan</returns>
  public static bool TryChangeClanRole(PlayerData playerData, ClanRoleEnum newRole) {
    var user = playerData.User;

    // Check if player is in a clan
    if (user.ClanEntity.Equals(NetworkedEntity.Empty)) {
      return false; // Player is not in a clan
    }

    // Get the current clan role component and update it
    var clanRole = playerData.UserEntity.Read<ClanRole>();
    clanRole.Value = newRole;
    playerData.UserEntity.Write(clanRole);

    return true; // Role changed successfully
  }

  /// <summary>
  /// Renames an existing clan and updates all member references.
  /// </summary>
  /// <param name="oldClanName">The current name of the clan</param>
  /// <param name="newClanName">The new name for the clan</param>
  public static void RenameClan(string oldClanName, string newClanName) {
    // Check if the clan exists
    if (!TryGetClanEntity(oldClanName, out var clanEntity)) {
      Log.Error($"Clan '{oldClanName}' does not exist.");
      return;
    }

    // Update the clan name
    var clanTeam = clanEntity.Read<ClanTeam>();
    clanTeam.Name = newClanName;
    clanEntity.Write(clanTeam);

    // Update all member references to the new clan name
    var members = clanEntity.ReadBuffer<ClanMemberStatus>();
    var userBuffer = clanEntity.ReadBuffer<SyncToUserBuffer>();

    for (var i = 0; i < members.Length; ++i) {
      var userBufferEntry = userBuffer[i];
      var user = userBufferEntry.UserEntity.Read<User>();
      var playerCharacter = user.LocalCharacter.GetEntityOnServer().Read<PlayerCharacter>();

      // Update the smart clan name for display purposes
      playerCharacter.SmartClanName = ClanUtility.GetSmartClanName(newClanName);
      user.LocalCharacter.GetEntityOnServer().Write(playerCharacter);
    }
  }

  /// <summary>
  /// Sets the motto for a specified clan.
  /// </summary>
  /// <param name="clanName">The name of the clan to update</param>
  /// <param name="motto">The new motto text to set for the clan</param>
  public static void SetMotto(string clanName, string motto) {
    // Check if the clan exists
    if (!TryGetClanEntity(clanName, out var clanEntity)) {
      Log.Error($"Clan '{clanName}' does not exist.");
      return;
    }

    var clanTeam = clanEntity.Read<ClanTeam>();
    clanTeam.Motto = motto;
    clanEntity.Write(clanTeam);
  }

  /// <summary>
  /// Disbands a clan by destroying its entity.
  /// This permanently removes the clan and all its data.
  /// </summary>
  /// <param name="clanName">The name of the clan to disband</param>
  public static void DisbandClan(string clanName) {
    // Check if the clan exists
    if (!TryGetClanEntity(clanName, out var clanEntity)) {
      Log.Error($"Clan '{clanName}' does not exist.");
      return;
    }

    clanEntity.Destroy();
  }

  /// <summary>
  /// Sets a custom clan tag for a player's character name.
  /// This changes the visual tag display without affecting actual clan membership.
  /// </summary>
  /// <param name="playerData">The player data of the player to set the tag for</param>
  /// <param name="clanTag">The clan tag text to display on the player's name</param>
  public static void SetTagForPlayer(PlayerData playerData, string clanTag) {
    ClanUtility.SetCharacterClanName(GameSystems.EntityManager, playerData.UserEntity, new(clanTag));
  }

  /// <summary>
  /// Removes the clan tag from a player's character name.
  /// This only removes the visual tag display and does not remove the player from the clan.
  /// </summary>
  /// <param name="playerData">The player data of the player to remove the tag from</param>
  public static void RemoveTagFromPlayer(PlayerData playerData) {
    ClanUtility.SetCharacterClanName(GameSystems.EntityManager, playerData.UserEntity, new(""));
  }

  /// <summary>
  /// Attempts to find a clan entity by name.
  /// WARNING: There is a bug in this method - the comparison logic is inverted.
  /// </summary>
  /// <param name="clanName">The name of the clan to find (case-insensitive)</param>
  /// <param name="clanEntity">Output parameter containing the clan entity if found</param>
  /// <returns>True if the clan was found, false otherwise</returns>
  public static bool TryGetClanEntity(string clanName, out Entity clanEntity) {
    var clans = GetClanEntities();

    foreach (var clan in clans) {
      // BUG: This comparison is inverted - should use != instead of ==
      // Currently this will skip matching clans and return non-matching ones
      if (!clan.Read<ClanTeam>().Name.ToString().Equals(clanName, System.StringComparison.CurrentCultureIgnoreCase)) continue;

      // Get clan members to verify clan is active
      var members = clan.ReadBuffer<ClanMemberStatus>();

      // Skip empty clans
      if (members.Length == 0) continue;

      clanEntity = clan;
      return true;
    }

    // Initialize empty entity if not found
    clanEntity = new Entity();
    return false;
  }

  /// <summary>
  /// Creates a native array of all clan entities in the game.
  /// The caller is responsible for disposing the returned array to prevent memory leaks.
  /// </summary>
  /// <returns>A NativeArray containing all clan entities with ClanTeam component</returns>
  public static NativeArray<Entity> GetClanEntities() {
    return GameSystems.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<ClanTeam>()).ToEntityArray(Allocator.Temp);
  }

  /// <summary>
  /// Retrieves a list of all clan team components from active clans in the game.
  /// This method extracts the ClanTeam component data from all clan entities.
  /// </summary>
  /// <returns>A list of ClanTeam components containing clan information such as names and settings</returns>
  public static List<ClanTeam> GetClanTeams() {
    var clans = new List<ClanTeam>();
    var clanEntities = GetClanEntities();

    foreach (var clanEntity in clanEntities) {
      if (GameSystems.EntityManager.HasComponent<ClanTeam>(clanEntity)) {
        clans.Add(clanEntity.Read<ClanTeam>());
      }
    }

    // Clean up native array to prevent memory leaks
    clanEntities.Dispose();
    return clans;
  }

  /// <summary>
  /// Retrieves the name of the clan that the specified player belongs to.
  /// </summary>
  /// <param name="playerData">The player whose clan name is to be retrieved.</param>
  /// <returns>The name of the player's clan, or an empty string if the player is not in a clan.</returns>
  public static string GetClanName(PlayerData playerData) {
    // Check if the player is in a clan
    if (playerData.User.ClanEntity.Equals(NetworkedEntity.Empty)) {
      return string.Empty; // Not in a clan
    }

    // Get the clan entity and read its name
    var clanEntity = playerData.User.ClanEntity.GetEntityOnServer();
    return clanEntity.Read<ClanTeam>().Name.ToString();
  }
}