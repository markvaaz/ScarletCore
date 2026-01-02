using ProjectM;
using ProjectM.Network;
using ScarletCore.Systems;
using ScarletCore.Data;
using ScarletCore.Utils;
using Unity.Entities;
using ScarletCore.Events;
using ScarletCore.Commanding;
using ScarletCore.Localization;

namespace ScarletCore.Services;

/// <summary>
/// Service for managing administrator privileges and permissions in the game.
/// Provides methods to add, remove, and check admin status for players.
/// </summary>
public static class AdminService {

  internal static void Initialize() {
    EventManager.On(PlayerEvents.PlayerJoined, (PlayerData playerData) => {
      var autoAdmin = Plugin.Database.Get<bool>($"auto_admin_{playerData.PlatformId}");

      if (autoAdmin) {
        AddAdmin(playerData);
      }
    });
  }

  [Command("autoadmin", Language.English, adminOnly: true)]
  internal static void AutoAdminCommand(CommandContext context) {
    var autoAdmin = Plugin.Database.Get<bool>($"auto_admin_{context.Sender.PlatformId}");
    autoAdmin = !autoAdmin;
    Plugin.Database.Set($"auto_admin_{context.Sender.PlatformId}", autoAdmin);
    var status = autoAdmin ? "enabled" : "disabled";
    context.ReplySuccess($"Auto admin has been ~{status}~ for you.");
  }

  [Command("toggleadmin", Language.English, requiredPermissions: ["admin.toggle"])]
  internal static void ToggleAdminCommand(CommandContext context, string targetPlayerName = null) {
    PlayerData targetPlayer = null;

    if (!string.IsNullOrEmpty(targetPlayerName) && !PlayerService.TryGetByName(targetPlayerName, out targetPlayer)) {
      context.ReplyError($"Player ~{targetPlayerName}~ not found.");
      return;
    }

    targetPlayer ??= context.Sender;

    if (IsAdmin(targetPlayer.PlatformId)) {
      RemoveAdmin(targetPlayer.PlatformId);
      context.ReplySuccess($"Player ~{targetPlayer.Name}~ has been ~demoted~ from admin.");
    } else {
      AddAdmin(targetPlayer.PlatformId);
      context.ReplySuccess($"Player ~{targetPlayer.Name}~ has been ~promoted~ to admin.");
    }
  }

  /// <summary>
  /// Adds admin privileges to a player using PlayerData.
  /// </summary>
  /// <param name="playerData">The player data containing platform ID</param>
  public static void AddAdmin(PlayerData playerData) {
    AddAdmin(playerData.PlatformId);
  }

  /// <summary>
  /// Adds admin privileges to a player by name.
  /// </summary>
  /// <param name="playerName">The name of the player to promote to admin</param>
  public static void AddAdmin(string playerName) {
    if (PlayerService.TryGetByName(playerName, out var player)) {
      AddAdmin(player.PlatformId);
    } else {
      Log.Warning($"AdminService: Player '{playerName}' not found for admin addition.");
    }
  }

  /// <summary>
  /// Adds admin privileges to a player by platform ID.
  /// Creates the necessary ECS events and updates the local admin list.
  /// </summary>
  /// <param name="playerId">The platform ID of the player to promote to admin</param>
  public static void AddAdmin(ulong playerId) {
    // Try to get player data to create the admin auth event
    if (PlayerService.TryGetById(playerId, out var player)) {
      // Create entity event for admin authentication
      var entityEvent = GameSystems.EntityManager.CreateEntity(
        ComponentType.ReadWrite<FromCharacter>(),
        ComponentType.ReadWrite<AdminAuthEvent>()
      );

      // Associate the event with the player's character and user entities
      entityEvent.Write(new FromCharacter() {
        Character = player.CharacterEntity,
        User = player.UserEntity
      });

      RoleService.AddRoleToPlayer(player, DefaultRoles.Admin);
    }

    // Update local admin list and persist changes
    GameSystems.AdminAuthSystem._LocalAdminList.Add(playerId);
    GameSystems.AdminAuthSystem._LocalAdminList.Save();
    GameSystems.AdminAuthSystem._LocalAdminList.Refresh();
  }

  /// <summary>
  /// Removes admin privileges from a player using PlayerData.
  /// </summary>
  /// <param name="playerData">The player data containing platform ID</param>
  public static void RemoveAdmin(PlayerData playerData) {
    RemoveAdmin(playerData.PlatformId);
  }

  /// <summary>
  /// Removes admin privileges from a player by name.
  /// </summary>
  /// <param name="playerName">The name of the player to demote from admin</param>
  public static void RemoveAdmin(string playerName) {
    if (PlayerService.TryGetByName(playerName, out var player)) {
      RemoveAdmin(player.PlatformId);
    } else {
      Log.Warning($"AdminService: Player '{playerName}' not found for removal.");
    }
  }

  /// <summary>
  /// Removes admin privileges from a player by platform ID.
  /// Removes AdminUser component and creates deauth event.
  /// </summary>
  /// <param name="playerId">The platform ID of the player to demote from admin</param>
  public static void RemoveAdmin(ulong playerId) {
    // Try to get player data to handle ECS components
    if (PlayerService.TryGetById(playerId, out var player)) {
      // Remove AdminUser component if present
      if (player.UserEntity.Has<AdminUser>()) {
        player.UserEntity.Remove<AdminUser>();
      }

      // Refresh user data to apply changes
      player.UserEntity.Write(player.UserEntity.Read<User>());

      // Create deauth admin event
      var entity = GameSystems.EntityManager.CreateEntity(
        ComponentType.ReadWrite<FromCharacter>(),
        ComponentType.ReadWrite<DeauthAdminEvent>()
      );
      entity.Write(new FromCharacter() {
        Character = player.CharacterEntity,
        User = player.UserEntity
      });

      RoleService.RemoveRoleFromPlayer(player, DefaultRoles.Admin);
    }

    // Update local admin list and persist changes
    GameSystems.AdminAuthSystem._LocalAdminList.Remove(playerId);
    GameSystems.AdminAuthSystem._LocalAdminList.Save();
    GameSystems.AdminAuthSystem._LocalAdminList.Refresh();
  }

  /// <summary>
  /// Checks if a player has admin privileges by platform ID.
  /// </summary>
  /// <param name="platformId">The platform ID to check</param>
  /// <returns>True if the player is an admin, false otherwise</returns>
  public static bool IsAdmin(ulong platformId) {
    return GameSystems.AdminAuthSystem._LocalAdminList.Contains(platformId);
  }

  /// <summary>
  /// Checks if a player has admin privileges by name.
  /// </summary>
  /// <param name="playerName">The name of the player to check</param>
  /// <returns>True if the player is an admin, false otherwise</returns>
  public static bool IsAdmin(string playerName) {
    if (PlayerService.TryGetByName(playerName, out var player)) {
      return IsAdmin(player.PlatformId);
    }

    Log.Warning($"AdminService: Player '{playerName}' not found for admin check.");
    return false;
  }
}