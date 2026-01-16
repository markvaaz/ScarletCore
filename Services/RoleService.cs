using System;
using System.Collections.Generic;
using System.Linq;
using ScarletCore.Utils;
using ScarletCore.Commanding;
using ScarletCore.Localization;
using ScarletCore.Systems;

namespace ScarletCore.Services;

/// <summary>
/// Contains predefined role names for common use cases in the role system
/// </summary>
public static class DefaultRoles {
  /// <summary>
  /// Owner role with highest priority (200) and all permissions
  /// </summary>
  public const string Owner = "Owner";

  /// <summary>
  /// Admin role with full server administrator permissions (priority 100)
  /// </summary>
  public const string Admin = "Admin";

  /// <summary>
  /// Moderator role with moderation permissions (priority 50)
  /// </summary>
  public const string Moderator = "Moderator";

  /// <summary>
  /// Support role for support team members (priority 25)
  /// </summary>
  public const string Support = "Support";

  /// <summary>
  /// Default user role assigned to all players (priority 0)
  /// </summary>
  public const string Default = "Default";
}

/// <summary>
/// Manages roles and player role assignments with persistent storage.
/// Provides methods to create, modify, and assign roles to players.
/// </summary>
public static class RoleService {
  private const string ROLES_KEY = "system.roles";
  private const string EXPIRING_ROLES_KEY = "system.expiring_roles";
  private const string PLAYER_ROLES_PREFIX = "player.roles.";

  private static Dictionary<string, Role> _roleCache = [];

  /// <summary>
  /// Tracks a role assignment that has an expiration date
  /// </summary>
  [Serializable]
  private class ExpiringRoleReference {
    public ulong PlatformId { get; set; }
    public string RoleName { get; set; }
    public DateTime ExpiresAt { get; set; }

    public ExpiringRoleReference(ulong platformId, string roleName, DateTime expiresAt) {
      PlatformId = platformId;
      RoleName = roleName;
      ExpiresAt = expiresAt;
    }
  }


  /// <summary>
  /// Initializes the role service and creates default roles if they don't exist
  /// </summary>
  internal static void Initialize() {
    LoadRoles();

    // Create default roles if they don't exist
    if (!RoleExists(DefaultRoles.Owner)) {
      CreateRole(DefaultRoles.Owner, ["*"], 200, "Server owner with all permissions");
    }

    if (!RoleExists(DefaultRoles.Admin)) {
      CreateRole(DefaultRoles.Admin, ["*"], 100, "Full server administrator");
    }

    if (!RoleExists(DefaultRoles.Moderator)) {
      CreateRole(DefaultRoles.Moderator, ["role.list", "role.info", "role.assign", "role.check"], 50, "Server moderator");
    }

    if (!RoleExists(DefaultRoles.Support)) {
      CreateRole(DefaultRoles.Support, [], 25, "Support team member");
    }

    if (!RoleExists(DefaultRoles.Default)) {
      CreateRole(DefaultRoles.Default, ["basic"], 0, "Default user role");
    }

    ActionScheduler.Repeating(CheckForExpiredRoles, 60 * 10);

    CheckForExpiredRoles();
  }

  /// <summary>
  /// Loads all roles from the database into the cache
  /// </summary>
  private static void LoadRoles() {
    try {
      _roleCache.Clear();

      if (!Plugin.Database.Has(ROLES_KEY)) {
        return;
      }

      var roles = Plugin.Database.Get<List<Role>>(ROLES_KEY);

      if (roles != null) {
        foreach (var role in roles) {
          _roleCache[role.Name.ToLower()] = role;
        }
      }
    }
    catch (Exception ex) {
      Log.Error($"Failed to load roles from database: {ex.Message}");
      _roleCache = [];
    }
  }

  /// <summary>
  /// Saves all roles to the database
  /// </summary>
  private static void SaveRoles() {
    try {
      var rolesList = _roleCache.Values.ToList();
      Plugin.Database.Set(ROLES_KEY, rolesList);
    }
    catch (Exception ex) {
      Log.Error($"Failed to save roles to database: {ex.Message}");
    }
  }

  /// <summary>
  /// Creates a new role with the specified parameters
  /// </summary>
  /// <param name="name">Unique name for the role</param>
  /// <param name="permissions">Array of permission strings</param>
  /// <param name="priority">Priority level (default: 0)</param>
  /// <param name="description">Optional description (default: null)</param>
  /// <returns>The created role, or null if a role with that name already exists</returns>
  public static Role CreateRole(string name, string[] permissions = null, int priority = 0, string description = null) {
    if (string.IsNullOrWhiteSpace(name)) {
      Log.Warning("Cannot create role with empty name");
      return null;
    }

    var normalizedName = name.Trim();

    if (RoleExists(normalizedName)) {
      Log.Warning($"Role '{normalizedName}' already exists");
      return null;
    }

    // Validate priority: only Owner role can have priority > 100
    if (!normalizedName.Equals(DefaultRoles.Owner, StringComparison.OrdinalIgnoreCase)) {
      if (priority < 0 || priority > 100) {
        Log.Warning($"Cannot create role '{normalizedName}' with priority {priority}. Priority must be between 0 and 100.");
        return null;
      }
    }
    else {
      priority = 200;
      permissions = ["*"];
    }

    var role = new Role(normalizedName, permissions, priority) {
      Description = description
    };

    _roleCache[normalizedName.ToLower()] = role;
    SaveRoles();

    Log.Message($"Created role: {role}");
    return role;
  }

  /// <summary>
  /// Deletes a role by name
  /// </summary>
  /// <param name="roleName">Name of the role to delete</param>
  /// <returns>True if deleted, false if not found</returns>
  public static bool DeleteRole(string roleName) {
    if (string.IsNullOrWhiteSpace(roleName)) return false;

    // Cannot delete default roles
    if (roleName.Equals(DefaultRoles.Owner, StringComparison.OrdinalIgnoreCase) ||
        roleName.Equals(DefaultRoles.Admin, StringComparison.OrdinalIgnoreCase) ||
        roleName.Equals(DefaultRoles.Moderator, StringComparison.OrdinalIgnoreCase) ||
        roleName.Equals(DefaultRoles.Support, StringComparison.OrdinalIgnoreCase) ||
        roleName.Equals(DefaultRoles.Default, StringComparison.OrdinalIgnoreCase)) {
      Log.Warning($"Cannot delete default role '{roleName}'");
      return false;
    }

    var normalizedName = roleName.Trim().ToLower();

    if (!_roleCache.Remove(normalizedName)) {
      return false;
    }

    SaveRoles();

    // Remove this role from all players who have it
    RemoveRoleFromAllPlayers(roleName);

    Log.Message($"Deleted role: {roleName}");
    return true;
  }

  /// <summary>
  /// Gets a role by name
  /// </summary>
  /// <param name="roleName">Name of the role</param>
  /// <returns>The role if found, null otherwise</returns>
  public static Role GetRole(string roleName) {
    if (string.IsNullOrWhiteSpace(roleName)) return null;

    var normalizedName = roleName.Trim().ToLower();
    return _roleCache.TryGetValue(normalizedName, out var role) ? role : null;
  }

  /// <summary>
  /// Checks if a role exists
  /// </summary>
  /// <param name="roleName">Name of the role</param>
  /// <returns>True if exists, false otherwise</returns>
  public static bool RoleExists(string roleName) {
    if (string.IsNullOrWhiteSpace(roleName)) return false;
    return _roleCache.ContainsKey(roleName.Trim().ToLower());
  }

  /// <summary>
  /// Gets all available roles
  /// </summary>
  /// <returns>List of all roles</returns>
  public static List<Role> GetAllRoles() {
    return [.. _roleCache.Values.OrderByDescending(r => r.Priority)];
  }

  /// <summary>
  /// Updates a role's properties
  /// </summary>
  /// <param name="roleName">Name of the role to update</param>
  /// <param name="newPermissions">New permissions (null to keep existing)</param>
  /// <param name="newPriority">New priority (null to keep existing)</param>
  /// <param name="newDescription">New description (null to keep existing)</param>
  /// <returns>True if updated, false if role not found</returns>
  public static bool UpdateRole(string roleName, string[] newPermissions = null, int? newPriority = null, string newDescription = null) {
    var role = GetRole(roleName);
    if (role == null) return false;

    if (newPermissions != null) {
      role.Permissions = [.. newPermissions];
    }

    if (newPriority.HasValue) {
      // Validate priority: only Owner role can have priority > 100
      if (!roleName.Equals(DefaultRoles.Owner, StringComparison.OrdinalIgnoreCase)) {
        if (newPriority.Value < 0 || newPriority.Value > 100) {
          Log.Warning($"Cannot set priority {newPriority.Value} for role '{roleName}'. Priority must be between 0 and 100.");
          return false;
        }
      }
      role.Priority = newPriority.Value;
    }

    if (newDescription != null) {
      role.Description = newDescription;
    }

    role.UpdatedAt = DateTime.Now;
    SaveRoles();

    return true;
  }

  /// <summary>
  /// Adds a permission to a role
  /// </summary>
  /// <param name="roleName">Name of the role</param>
  /// <param name="permission">Permission to add</param>
  /// <returns>True if added, false otherwise</returns>
  public static bool AddPermissionToRole(string roleName, string permission) {
    var role = GetRole(roleName);
    if (role == null) return false;

    if (role.AddPermission(permission)) {
      SaveRoles();
      return true;
    }

    return false;
  }

  /// <summary>
  /// Removes a permission from a role
  /// </summary>
  /// <param name="roleName">Name of the role</param>
  /// <param name="permission">Permission to remove</param>
  /// <returns>True if removed, false otherwise</returns>
  public static bool RemovePermissionFromRole(string roleName, string permission) {
    var role = GetRole(roleName);
    if (role == null) return false;

    if (role.RemovePermission(permission)) {
      SaveRoles();
      return true;
    }

    return false;
  }

  /// <summary>
  /// Adds a reference to track an expiring role assignment
  /// </summary>
  private static void AddExpiringRoleReference(ulong platformId, string roleName, DateTime expiresAt) {
    try {
      var references = GetExpiringRoleReferences();
      references.Add(new ExpiringRoleReference(platformId, roleName, expiresAt));
      Plugin.Database.Set(EXPIRING_ROLES_KEY, references);
    }
    catch (Exception ex) {
      Log.Error($"Failed to add expiring role reference: {ex.Message}");
    }
  }

  /// <summary>
  /// Removes a reference for an expiring role assignment
  /// </summary>
  private static void RemoveExpiringRoleReference(ulong platformId, string roleName) {
    try {
      var references = GetExpiringRoleReferences();
      references.RemoveAll(r => r.PlatformId == platformId &&
                               r.RoleName.Equals(roleName, StringComparison.OrdinalIgnoreCase));
      Plugin.Database.Set(EXPIRING_ROLES_KEY, references);
    }
    catch (Exception ex) {
      Log.Error($"Failed to remove expiring role reference: {ex.Message}");
    }
  }

  /// <summary>
  /// Gets all expiring role references
  /// </summary>
  private static List<ExpiringRoleReference> GetExpiringRoleReferences() {
    try {
      if (!Plugin.Database.Has(EXPIRING_ROLES_KEY)) {
        return new List<ExpiringRoleReference>();
      }
      return Plugin.Database.Get<List<ExpiringRoleReference>>(EXPIRING_ROLES_KEY) ?? new List<ExpiringRoleReference>();
    }
    catch (Exception ex) {
      Log.Error($"Failed to get expiring role references: {ex.Message}");
      return new List<ExpiringRoleReference>();
    }
  }

  /// <summary>
  /// Parses a duration string into a TimeSpan
  /// Supported formats: number (seconds), Xs, Xm, Xh, Xd, Xw, Xmonth/Xmonths, Xy
  /// Also supports: X seconds/minutes/hours/days/weeks/months/years
  /// </summary>
  /// <param name="durationStr">Duration string to parse</param>
  /// <param name="duration">Parsed duration</param>
  /// <returns>True if parsed successfully, false otherwise</returns>
  private static bool ParseDuration(string durationStr, out TimeSpan duration) {
    duration = TimeSpan.Zero;
    if (string.IsNullOrWhiteSpace(durationStr)) return false;

    durationStr = durationStr.Trim().ToLower();

    // Try to parse as plain number (seconds)
    if (double.TryParse(durationStr, out var seconds)) {
      duration = TimeSpan.FromSeconds(seconds);
      return true;
    }

    // Parse number and unit
    var numStr = "";
    var unitStr = "";
    var parsingNumber = true;

    foreach (var c in durationStr) {
      if (parsingNumber && (char.IsDigit(c) || c == '.' || c == ',')) {
        numStr += c == ',' ? '.' : c;
      }
      else {
        parsingNumber = false;
        if (!char.IsWhiteSpace(c)) {
          unitStr += c;
        }
      }
    }

    if (!double.TryParse(numStr, out var value)) return false;
    if (string.IsNullOrEmpty(unitStr)) {
      duration = TimeSpan.FromSeconds(value);
      return true;
    }

    // Parse unit
    switch (unitStr) {
      case "s":
      case "sec":
      case "second":
      case "seconds":
        duration = TimeSpan.FromSeconds(value);
        return true;
      case "m":
      case "min":
      case "minute":
      case "minutes":
        duration = TimeSpan.FromMinutes(value);
        return true;
      case "h":
      case "hour":
      case "hours":
        duration = TimeSpan.FromHours(value);
        return true;
      case "d":
      case "day":
      case "days":
        duration = TimeSpan.FromDays(value);
        return true;
      case "w":
      case "week":
      case "weeks":
        duration = TimeSpan.FromDays(value * 7);
        return true;
      case "month":
      case "months":
        duration = TimeSpan.FromDays(value * 30);
        return true;
      case "y":
      case "year":
      case "years":
        duration = TimeSpan.FromDays(value * 365);
        return true;
      default:
        return false;
    }
  }

  /// <summary>
  /// Checks for and removes expired roles efficiently using tracked references
  /// </summary>
  private static void CheckForExpiredRoles() {
    try {
      var references = GetExpiringRoleReferences();
      if (references.Count == 0) return;

      var now = DateTime.Now;
      var expiredReferences = references.Where(r => r.ExpiresAt <= now).ToList();

      if (expiredReferences.Count == 0) return;

      var processedPlayers = new HashSet<ulong>();

      foreach (var expired in expiredReferences) {
        // Only load player data once per player
        if (!processedPlayers.Contains(expired.PlatformId)) {
          var key = GetPlayerRolesKey(expired.PlatformId);

          if (Plugin.Database.Has(key)) {
            var assignments = Plugin.Database.Get<List<PlayerRoleAssignment>>(key);
            if (assignments != null && assignments.Count > 0) {
              var removedAny = assignments.RemoveAll(a => a.IsExpired) > 0;
              if (removedAny) {
                Plugin.Database.Set(key, assignments);
              }
            }
          }

          processedPlayers.Add(expired.PlatformId);
        }

        Log.Message($"Role '{expired.RoleName}' expired for player {expired.PlatformId}");
      }

      // Remove expired references from tracking
      references.RemoveAll(r => r.ExpiresAt <= now);
      Plugin.Database.Set(EXPIRING_ROLES_KEY, references);

    }
    catch (Exception ex) {
      Log.Error($"Error checking for expired roles: {ex.Message}");
    }
  }

  // ========== Player Role Management ==========

  /// <summary>
  /// Gets the database key for a player's roles
  /// </summary>
  private static string GetPlayerRolesKey(ulong platformId) {
    return $"{PLAYER_ROLES_PREFIX}{platformId}";
  }

  /// <summary>
  /// Adds a role to a player with optional duration
  /// </summary>
  /// <param name="player">The player to add the role to</param>
  /// <param name="roleName">Name of the role to add</param>
  /// <param name="durationStr">Duration string (e.g., "30d", "2h", "1y"). Use "-1" or null for permanent</param>
  /// <returns>True if added, false if role doesn't exist or player already has it</returns>
  public static bool AddRoleToPlayer(PlayerData player, string roleName, string durationStr = null) {
    if (player == null || string.IsNullOrWhiteSpace(roleName)) return false;

    if (!RoleExists(roleName)) {
      Log.Warning($"Cannot add non-existent role '{roleName}' to player {player.PlatformId}");
      return false;
    }

    var assignments = GetPlayerRoleAssignments(player);

    // Check if player already has this role
    if (assignments.Any(a => a.RoleName.Equals(roleName, StringComparison.OrdinalIgnoreCase))) {
      return false;
    }

    // Parse duration
    DateTime? expiresAt = null;
    if (!string.IsNullOrWhiteSpace(durationStr) && durationStr != "-1") {
      if (ParseDuration(durationStr, out var duration)) {
        expiresAt = DateTime.Now.Add(duration);
      }
      else {
        Log.Warning($"Invalid duration format: {durationStr}");
        return false;
      }
    }

    var assignment = new PlayerRoleAssignment(roleName, expiresAt);
    assignments.Add(assignment);
    SavePlayerRoleAssignments(player, assignments);

    // Track expiring role for efficient cleanup
    if (expiresAt.HasValue) {
      AddExpiringRoleReference(player.PlatformId, roleName, expiresAt.Value);
    }

    var expInfo = expiresAt.HasValue ? $" (expires: {expiresAt.Value:dd/MM/yyyy HH:mm})" : " (permanent)";
    Log.Message($"Added role '{roleName}' to player {player.PlatformId}{expInfo}");
    return true;
  }

  /// <summary>
  /// Removes a role from a player
  /// </summary>
  /// <param name="player">The player to remove the role from</param>
  /// <param name="roleName">Name of the role to remove</param>
  /// <param name="removedBy">The player removing the role (required for Owner role)</param>
  /// <returns>True if removed, false if player didn't have the role</returns>
  public static bool RemoveRoleFromPlayer(PlayerData player, string roleName, PlayerData removedBy = null) {
    if (player == null || string.IsNullOrWhiteSpace(roleName)) return false;

    // Cannot remove User role
    if (roleName.Equals(DefaultRoles.Default, StringComparison.OrdinalIgnoreCase)) {
      Log.Warning($"Cannot remove the default User role");
      return false;
    }

    // Only IsAdmin can remove Owner role
    if (roleName.Equals(DefaultRoles.Owner, StringComparison.OrdinalIgnoreCase)) {
      if (removedBy == null || !removedBy.IsAdmin) {
        Log.Warning($"Only server admins can remove the Owner role");
        return false;
      }
    }

    var assignments = GetPlayerRoleAssignments(player);
    var removed = assignments.RemoveAll(a => a.RoleName.Equals(roleName, StringComparison.OrdinalIgnoreCase)) > 0;

    if (removed) {
      SavePlayerRoleAssignments(player, assignments);
      RemoveExpiringRoleReference(player.PlatformId, roleName);
      Log.Message($"Removed role '{roleName}' from player {player.PlatformId}");
    }

    return removed;
  }

  /// <summary>
  /// Checks if a player has a specific role
  /// </summary>
  /// <param name="player">The player to check</param>
  /// <param name="roleName">Name of the role</param>
  /// <returns>True if player has the role, false otherwise</returns>
  public static bool PlayerHasRole(PlayerData player, string roleName) {
    if (player == null || string.IsNullOrWhiteSpace(roleName)) return false;

    var playerRoles = GetPlayerRoles(player);
    return playerRoles.Any(r => r.Equals(roleName, StringComparison.OrdinalIgnoreCase));
  }

  /// <summary>
  /// Gets all role assignments for a player (including expired ones)
  /// </summary>
  /// <param name="player">The player</param>
  /// <returns>List of role assignments</returns>
  private static List<PlayerRoleAssignment> GetPlayerRoleAssignments(PlayerData player) {
    if (player == null) return [];

    try {
      var key = GetPlayerRolesKey(player.PlatformId);

      if (!Plugin.Database.Has(key)) {
        return [];
      }

      var assignments = Plugin.Database.Get<List<PlayerRoleAssignment>>(key);
      return assignments ?? [];
    }
    catch (Exception ex) {
      Log.Error($"Failed to get role assignments for player {player.PlatformId}: {ex.Message}");
      return [];
    }
  }

  /// <summary>
  /// Gets all active (non-expired) role names assigned to a player
  /// </summary>
  /// <param name="player">The player</param>
  /// <returns>List of role names</returns>
  public static List<string> GetPlayerRoles(PlayerData player) {
    var assignments = GetPlayerRoleAssignments(player);
    return [.. assignments.Where(a => !a.IsExpired).Select(a => a.RoleName)];
  }

  /// <summary>
  /// Gets all Role objects assigned to a player
  /// </summary>
  /// <param name="player">The player</param>
  /// <returns>List of Role objects</returns>
  public static List<Role> GetPlayerRoleObjects(PlayerData player) {
    var roleNames = GetPlayerRoles(player);
    var roles = new List<Role>();

    foreach (var roleName in roleNames) {
      var role = GetRole(roleName);
      if (role != null) {
        roles.Add(role);
      }
    }

    return [.. roles.OrderByDescending(r => r.Priority)];
  }

  /// <summary>
  /// Gets the highest priority role for a player
  /// </summary>
  /// <param name="player">The player</param>
  /// <returns>The highest priority role, or null if player has no roles</returns>
  public static Role GetPlayerPrimaryRole(PlayerData player) {
    var roles = GetPlayerRoleObjects(player);
    return roles.FirstOrDefault(); // Already sorted by priority
  }

  /// <summary>
  /// Checks if a player has a specific permission (from any of their roles)
  /// </summary>
  /// <param name="player">The player</param>
  /// <param name="permission">The permission to check</param>
  /// <returns>True if player has the permission, false otherwise</returns>
  public static bool PlayerHasPermission(PlayerData player, string permission) {
    if (player == null || string.IsNullOrWhiteSpace(permission)) return false;

    var roles = GetPlayerRoleObjects(player);

    // Check for wildcard permission (*)
    if (roles.Any(r => r.HasPermission("*"))) {
      return true;
    }

    // Check for specific permission
    return roles.Any(r => r.HasPermission(permission));
  }

  /// <summary>
  /// Saves player role assignments to the database
  /// </summary>
  private static void SavePlayerRoleAssignments(PlayerData player, List<PlayerRoleAssignment> assignments) {
    if (player == null) return;

    try {
      var key = GetPlayerRolesKey(player.PlatformId);
      Plugin.Database.Set(key, assignments);
    }
    catch (Exception ex) {
      Log.Error($"Failed to save role assignments for player {player.PlatformId}: {ex.Message}");
    }
  }

  /// <summary>
  /// Removes a specific role from all players
  /// </summary>
  private static void RemoveRoleFromAllPlayers(string roleName) {
    foreach (var player in PlayerService.AllPlayers) {
      RemoveRoleFromPlayer(player, roleName);
    }
  }

  /// <summary>
  /// Gets all players who have a specific role
  /// </summary>
  /// <param name="roleName">Name of the role</param>
  /// <returns>List of players with that role</returns>
  public static List<PlayerData> GetPlayersWithRole(string roleName) {
    if (string.IsNullOrWhiteSpace(roleName)) return [];

    var playersWithRole = new List<PlayerData>();

    foreach (var player in PlayerService.AllPlayers) {
      if (PlayerHasRole(player, roleName)) {
        playersWithRole.Add(player);
      }
    }

    return playersWithRole;
  }

  /// <summary>
  /// Gets all unique permissions from all roles in the system
  /// </summary>
  /// <returns>List of unique permission strings</returns>
  public static List<string> GetAllPermissions() {
    var permissions = new HashSet<string>();

    foreach (var role in _roleCache.Values) {
      foreach (var permission in role.Permissions) {
        permissions.Add(permission);
      }
    }

    return [.. permissions.OrderBy(p => p)];
  }

  /// <summary>
  /// Clears all roles from a player and assigns default User role
  /// </summary>
  /// <param name="player">The player to clear roles from</param>
  public static void ClearPlayerRoles(PlayerData player) {
    if (player == null) return;

    var defaultAssignment = new PlayerRoleAssignment(DefaultRoles.Default);
    SavePlayerRoleAssignments(player, [defaultAssignment]);
    Log.Message($"Cleared all roles from player {player.PlatformId}");
  }

  /// <summary>
  /// Gets role assignment information for a player including expiration details
  /// </summary>
  /// <param name="player">The player</param>
  /// <returns>List of active role assignments ordered by priority descending</returns>
  public static List<PlayerRoleAssignment> GetPlayerRoleAssignmentsInfo(PlayerData player) {
    var assignments = GetPlayerRoleAssignments(player);
    var activeAssignments = assignments.Where(a => !a.IsExpired).ToList();

    // Order by role priority (highest first)
    return [.. activeAssignments.OrderByDescending(a => GetRole(a.RoleName)?.Priority ?? 0)];
  }

  [Command("roles", Language.English, description: "Show roles of a player with expiration info", requiredPermissions: ["role.check"])]
  internal static void PlayerRolesCommand(CommandContext ctx, PlayerData player) {
    RoleCommands.PlayerRolesCommand(ctx, player);
  }

  [Command("roles", Language.English, description: "List all roles", requiredPermissions: ["role.list"])]
  internal static void ListRolesCommand(CommandContext ctx) {
    RoleCommands.ListRolesCommand(ctx);
  }

  [CommandGroup("roles", Language.English)]
  internal static class RoleCommands {

    [Command("create", Language.English, description: "Create a new role", requiredPermissions: ["role.create"])]
    public static void CreateRoleCommand(CommandContext ctx, string roleName, int priority = 0) {
      // Validate priority range
      if (priority < 0 || priority > 100) {
        ctx.ReplyError("Priority must be between 0 and 100.");
        return;
      }

      var sender = ctx.Sender;
      var mainRole = GetPlayerPrimaryRole(sender);

      if (mainRole != null && mainRole.Priority <= priority) {
        ctx.ReplyError("You cannot create a role with equal or higher priority than your own.");
        return;
      }

      var role = CreateRole(roleName, null, priority);
      if (role != null) {
        ctx.ReplySuccess($"Role ~{roleName}~ created successfully with priority ~{priority}~.");
      }
      else {
        ctx.ReplyError($"Failed to create role ~{roleName}~. It may already exist.");
      }
    }

    [Command("delete", Language.English, description: "Delete a role", requiredPermissions: ["role.delete"])]
    public static void DeleteRoleCommand(CommandContext ctx, string roleName) {
      if (!RoleExists(roleName)) {
        ctx.ReplyError($"Role ~{roleName}~ does not exist.");
        return;
      }

      // Cannot delete default roles
      if (roleName.Equals(DefaultRoles.Owner, StringComparison.OrdinalIgnoreCase) ||
          roleName.Equals(DefaultRoles.Admin, StringComparison.OrdinalIgnoreCase) ||
          roleName.Equals(DefaultRoles.Moderator, StringComparison.OrdinalIgnoreCase) ||
          roleName.Equals(DefaultRoles.Support, StringComparison.OrdinalIgnoreCase) ||
          roleName.Equals(DefaultRoles.Default, StringComparison.OrdinalIgnoreCase)) {
        ctx.ReplyError("Cannot delete default roles.");
        return;
      }

      var sender = ctx.Sender;
      var mainRole = GetPlayerPrimaryRole(sender);
      var targetRole = GetRole(roleName);

      if (mainRole != null && targetRole != null && mainRole.Priority <= targetRole.Priority) {
        ctx.ReplyError("You cannot delete a role with equal or higher priority than your own.");
        return;
      }

      if (DeleteRole(roleName)) {
        ctx.ReplySuccess($"Role ~{roleName}~ deleted successfully.");
      }
      else {
        ctx.ReplyError($"Failed to delete role ~{roleName}~. It may not exist.");
      }
    }

    [Command("list", Language.English, description: "List all roles", requiredPermissions: ["role.list"])]
    public static void ListRolesCommand(CommandContext ctx) {
      var roles = GetAllRoles();
      if (roles.Count == 0) {
        ctx.ReplyError("No roles found.");
        return;
      }

      ctx.Reply("~Available Roles:~".Bold());
      foreach (var role in roles) {
        var permissions = role.Permissions.Count > 0 ? string.Join(", ", role.Permissions) : "none";
        ctx.Reply($"• ~{role.Name}~ (Priority: ~{role.Priority}~)\n- Permissions: {permissions}");
      }
    }

    [Command("info", Language.English, description: "Show information about a role", requiredPermissions: ["role.info"])]
    public static void RoleInfoCommand(CommandContext ctx, string roleName) {
      var role = GetRole(roleName);
      if (role == null) {
        ctx.ReplyError($"Role ~{roleName}~ not found.");
        return;
      }

      ctx.Reply($"~Role Information: {role.Name}~".Bold());
      ctx.Reply($"- Priority: ~{role.Priority}~");
      ctx.Reply($"- Permissions: {(role.Permissions.Count > 0 ? string.Join(", ", role.Permissions) : "none")}");
      if (!string.IsNullOrEmpty(role.Description)) {
        ctx.Reply($"  Description: {role.Description}");
      }
      ctx.Reply($"- Created: {role.CreatedAt:dd/MM/yyyy HH:mm}");
      ctx.Reply($"- Updated: {role.UpdatedAt:dd/MM/yyyy HH:mm}");
    }

    [Command("addpermission", Language.English, aliases: ["addperm"], description: "Add a permission to a role", requiredPermissions: ["role.permission"])]
    public static void AddPermissionCommand(CommandContext ctx, string roleName, string permission) {
      if (!RoleExists(roleName)) {
        ctx.ReplyError($"Role ~{roleName}~ does not exist.");
        return;
      }

      var sender = ctx.Sender;
      var mainRole = GetPlayerPrimaryRole(sender);
      var targetRole = GetRole(roleName);

      if (mainRole != null && targetRole != null && mainRole.Priority <= targetRole.Priority) {
        ctx.ReplyError("You cannot modify a role with equal or higher priority than your own.");
        return;
      }

      if (AddPermissionToRole(roleName, permission)) {
        ctx.ReplySuccess($"Permission ~{permission}~ added to role ~{roleName}~.");
      }
      else {
        ctx.ReplyError($"Failed to add permission. Role may not exist or already has this permission.");
      }
    }

    [Command("removepermission", Language.English, aliases: ["remperm"], description: "Remove a permission from a role", requiredPermissions: ["role.permission"])]
    public static void RemovePermissionCommand(CommandContext ctx, string roleName, string permission) {
      if (!RoleExists(roleName)) {
        ctx.ReplyError($"Role ~{roleName}~ does not exist.");
        return;
      }

      var sender = ctx.Sender;
      var mainRole = GetPlayerPrimaryRole(sender);
      var targetRole = GetRole(roleName);

      if (mainRole != null && targetRole != null && mainRole.Priority <= targetRole.Priority) {
        ctx.ReplyError("You cannot modify a role with equal or higher priority than your own.");
        return;
      }

      if (RemovePermissionFromRole(roleName, permission)) {
        ctx.ReplySuccess($"Permission ~{permission}~ removed from role ~{roleName}~.");
      }
      else {
        ctx.ReplyError($"Failed to remove permission. Role may not exist or doesn't have this permission.");
      }
    }

    [Command("setpriority", Language.English, description: "Set the priority of a role", requiredPermissions: ["role.update"])]
    public static void SetPriorityCommand(CommandContext ctx, string roleName, int priority) {
      if (!RoleExists(roleName)) {
        ctx.ReplyError($"Role ~{roleName}~ does not exist.");
        return;
      }

      // Cannot modify Owner role priority
      if (roleName.Equals(DefaultRoles.Owner, StringComparison.OrdinalIgnoreCase)) {
        ctx.ReplyError("Cannot modify Owner role priority.");
        return;
      }

      // Validate priority range
      if (priority < 0 || priority > 100) {
        ctx.ReplyError("Priority must be between 0 and 100.");
        return;
      }

      var sender = ctx.Sender;
      var mainRole = GetPlayerPrimaryRole(sender);
      var targetRole = GetRole(roleName);

      // Check current priority and new priority
      if (mainRole != null && targetRole != null && mainRole.Priority <= targetRole.Priority) {
        ctx.ReplyError("You cannot modify a role with equal or higher priority than your own.");
        return;
      }

      if (mainRole != null && mainRole.Priority <= priority) {
        ctx.ReplyError("You cannot set a priority equal or higher than your own.");
        return;
      }

      if (UpdateRole(roleName, newPriority: priority)) {
        ctx.ReplySuccess($"Priority of role ~{roleName}~ set to ~{priority}~.");
      }
      else {
        ctx.ReplyError($"Failed to update role ~{roleName}~. It may not exist.");
      }
    }

    [Command("setdescription", Language.English, aliases: ["setdesc"], description: "Set the description of a role", requiredPermissions: ["role.update"])]
    public static void SetDescriptionCommand(CommandContext ctx, string roleName, string description) {
      if (!RoleExists(roleName)) {
        ctx.ReplyError($"Role ~{roleName}~ does not exist.");
        return;
      }

      var sender = ctx.Sender;
      var mainRole = GetPlayerPrimaryRole(sender);
      var targetRole = GetRole(roleName);

      if (mainRole != null && targetRole != null && mainRole.Priority <= targetRole.Priority) {
        ctx.ReplyError("You cannot modify a role with equal or higher priority than your own.");
        return;
      }

      if (UpdateRole(roleName, newDescription: description)) {
        ctx.ReplySuccess($"Description of role ~{roleName}~ updated.");
      }
      else {
        ctx.ReplyError($"Failed to update role ~{roleName}~. It may not exist.");
      }
    }

    [Command("assign", Language.English, aliases: ["add"], description: "Assign a role to a player with optional duration", requiredPermissions: ["role.assign"])]
    public static void AssignRoleCommand(CommandContext ctx, PlayerData player, string roleName, string duration = null) {
      if (!RoleExists(roleName)) {
        ctx.ReplyError($"Role ~{roleName}~ does not exist.");
        return;
      }

      var sender = ctx.Sender;

      // Only IsAdmin can assign Owner role
      if (roleName.Equals(DefaultRoles.Owner, StringComparison.OrdinalIgnoreCase)) {
        if (!sender.IsAdmin) {
          ctx.ReplyError("Only server admins can assign the Owner role.");
          return;
        }
      }

      var mainRole = GetPlayerPrimaryRole(sender);

      if (mainRole != null && mainRole.Priority <= GetRole(roleName)?.Priority) {
        ctx.ReplyError("You cannot assign a role with equal or higher priority than your own.");
        return;
      }

      if (AddRoleToPlayer(player, roleName, duration)) {
        var durationInfo = string.IsNullOrWhiteSpace(duration) || duration == "-1" ? "permanently" : $"for {duration}";
        ctx.ReplySuccess($"Role ~{roleName}~ assigned to player ~{player.Name}~ {durationInfo}.");
      }
      else {
        ctx.ReplyError($"Failed to assign role. Role may not exist, player already has it, or invalid duration format.");
      }
    }

    [Command("unassign", Language.English, aliases: ["remove"], description: "Remove a role from a player", requiredPermissions: ["role.assign"])]
    public static void UnassignRoleCommand(CommandContext ctx, PlayerData player, string roleName) {
      if (!RoleExists(roleName)) {
        ctx.ReplyError($"Role ~{roleName}~ does not exist.");
        return;
      }

      // Cannot remove User role
      if (roleName.Equals(DefaultRoles.Default, StringComparison.OrdinalIgnoreCase)) {
        ctx.ReplyError("Cannot remove the default User role.");
        return;
      }

      var sender = ctx.Sender;

      // Only IsAdmin can remove Owner role
      if (roleName.Equals(DefaultRoles.Owner, StringComparison.OrdinalIgnoreCase)) {
        if (!sender.IsAdmin) {
          ctx.ReplyError("Only server admins can remove the Owner role.");
          return;
        }
      }

      var mainRole = GetPlayerPrimaryRole(sender);

      if (mainRole != null && mainRole.Priority <= GetRole(roleName)?.Priority) {
        ctx.ReplyError("You cannot remove a role with equal or higher priority than your own.");
        return;
      }

      if (RemoveRoleFromPlayer(player, roleName, sender)) {
        ctx.ReplySuccess($"Role ~{roleName}~ removed from player ~{player.Name}~.");
      }
      else {
        ctx.ReplyError($"Failed to remove role. Player may not have this role.");
      }
    }

    [Command("playerroles", Language.English, aliases: ["check"], description: "Show roles of a player with expiration info", requiredPermissions: ["role.check"])]
    public static void PlayerRolesCommand(CommandContext ctx, PlayerData player) {
      var assignments = GetPlayerRoleAssignmentsInfo(player);
      if (assignments.Count == 0) {
        ctx.ReplyError($"Player ~{player.Name}~ has no roles.");
        return;
      }

      ctx.Reply($"~Roles of {player.Name}:~".Bold());
      foreach (var assignment in assignments) {
        var role = GetRole(assignment.RoleName);
        if (role == null) continue;

        var permissions = role.Permissions.Count > 0 ? string.Join(", ", role.Permissions) : "none";
        var expiration = assignment.ExpiresAt.HasValue
          ? $" - Expires: {assignment.ExpiresAt.Value:dd/MM/yyyy HH:mm}"
          : " - Permanent";
        ctx.Reply($"• ~{role.Name}~ (Priority: ~{role.Priority}~){expiration}\n**- Permissions**: {permissions}");
      }
    }

    [Command("playerswithRole", Language.English, aliases: ["who"], description: "Show players with a specific role", requiredPermissions: ["role.check"])]
    public static void PlayersWithRoleCommand(CommandContext ctx, string roleName) {
      if (!RoleExists(roleName)) {
        ctx.ReplyError($"Role ~{roleName}~ not found.");
        return;
      }

      var players = GetPlayersWithRole(roleName);
      if (players.Count == 0) {
        ctx.ReplyError($"No players have the role ~{roleName}~.");
        return;
      }

      ctx.Reply($"~Players with role {roleName}:~".Bold());
      foreach (var player in players) {
        var status = player.IsOnline ? "online".WithColor("green") : "offline".WithColor("gray");
        ctx.Reply($"• ~{player.Name}~ ({status})");
      }
    }

    [Command("clear", Language.English, description: "Clear all roles from a player", requiredPermissions: ["role.assign"])]
    public static void ClearPlayerRolesCommand(CommandContext ctx, PlayerData player) {
      var sender = ctx.Sender;
      var mainRole = GetPlayerPrimaryRole(sender);
      var playerRoles = GetPlayerRoleObjects(player);

      // Check if trying to clear roles from someone with higher priority
      foreach (var role in playerRoles) {
        if (mainRole != null && mainRole.Priority <= role.Priority) {
          ctx.ReplyError($"You cannot clear roles from a player with equal or higher priority role than your own.");
          return;
        }
      }

      ClearPlayerRoles(player);
      ctx.ReplySuccess($"All roles cleared from player ~{player.Name}~.");
    }

    [Command("listpermissions", Language.English, aliases: ["permissions", "perms"], description: "List all permissions in the system", requiredPermissions: ["role.info"])]
    public static void ListPermissionsCommand(CommandContext ctx) {
      var permissions = GetAllPermissions();
      if (permissions.Count == 0) {
        ctx.ReplyError("No permissions found in the system.");
        return;
      }

      ctx.Reply($"~Available Permissions ({permissions.Count} total):~".Bold());

      // Group by prefix for better organization
      var grouped = permissions.GroupBy(p => p.Contains('.') ? p.Split('.')[0] : "other")
                              .OrderBy(g => g.Key);

      foreach (var group in grouped) {
        ctx.Reply($"\n~{group.Key.ToUpper()}:~".Bold());
        foreach (var perm in group) {
          // Count how many roles have this permission
          var rolesWithPerm = GetAllRoles().Count(r => r.HasPermission(perm));
          ctx.Reply($"  • {perm} (used by {rolesWithPerm} role{(rolesWithPerm != 1 ? "s" : "")})");
        }
      }
    }
  }
}
