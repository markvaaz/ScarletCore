# RoleService

The `RoleService` manages roles and player role assignments with persistent storage. It provides a comprehensive permission system with role priorities, temporary assignments, and fine-grained access control.

## Namespace
```csharp
using ScarletCore.Services;
```

## Overview

The `RoleService` is a static utility class that provides:
- Role creation, modification, and deletion
- Permission management per role
- Player role assignments with optional expiration
- Priority-based role hierarchy
- Permission checking system
- Automatic expiration handling

## Table of Contents

- [Default Roles](#default-roles)
- [Role Management](#role-management)
  - [CreateRole](#createrole)
  - [DeleteRole](#deleterole)
  - [GetRole](#getrole)
  - [RoleExists](#roleexists)
  - [GetAllRoles](#getallroles)
  - [UpdateRole](#updaterole)
- [Permission Management](#permission-management)
  - [AddPermissionToRole](#addpermissiontorole)
  - [RemovePermissionFromRole](#removepermissionfromrole)
  - [GetAllPermissions](#getallpermissions)
- [Player Role Management](#player-role-management)
  - [AddRoleToPlayer](#addrole toплayer)
  - [RemoveRoleFromPlayer](#removerolefromплayer)
  - [PlayerHasRole](#playerhasrole)
  - [GetPlayerRoles](#getplayerroles)
  - [GetPlayerRoleObjects](#getplayerroleobjects)
  - [GetPlayerPrimaryRole](#getplayerprimaryrole)
  - [GetPlayerRoleAssignmentsInfo](#getplayerroleassignmentsinfo)
  - [ClearPlayerRoles](#clearplayerroles)
- [Permission Checking](#permission-checking)
  - [PlayerHasPermission](#playerhaspermission)
- [Query Operations](#query-operations)
  - [GetPlayersWithRole](#getplayerswithrole)
- [Duration Format](#duration-format)
- [Built-in Commands](#built-in-commands)
- [Complete Examples](#complete-examples)
- [Best Practices](#best-practices)
- [Related Services](#related-services)

## Default Roles

The service provides predefined roles with specific priorities and permissions:

```csharp
public static class DefaultRoles {
    public const string Owner = "Owner";        // Priority: 200, Permissions: ["*"]
    public const string Admin = "Admin";        // Priority: 100, Permissions: ["*"]
    public const string Moderator = "Moderator"; // Priority: 50
    public const string Support = "Support";    // Priority: 25
    public const string Default = "Default";    // Priority: 0, Assigned to all players
}
```

**Priority Hierarchy:**
- **Owner (200)**: Highest priority, full access - **Can only be assigned via config file**
- **Admin (100)**: Administrative access
- **Moderator (50)**: Moderation capabilities
- **Support (25)**: Support team access
- **Default (0)**: Basic user permissions

**Important Notes:**
- The **Owner** role can only be assigned through the server configuration file, not through commands or code
- Owner role cannot be removed or modified through normal role management commands
- This restriction ensures server owner privileges remain secure

## Role Management

### CreateRole
```csharp
public static Role CreateRole(string name, string[] permissions = null, int priority = 0, string description = null)
```

Creates a new role with the specified parameters.

**Parameters:**
- `name` (string): Unique name for the role
- `permissions` (string[], optional): Array of permission strings
- `priority` (int, optional): Priority level (default: 0, max: 100, except Owner: 200)
- `description` (string, optional): Optional description

**Returns:**
- `Role`: The created role, or null if a role with that name already exists

**Example:**
```csharp
// Create a basic VIP role
var vipRole = RoleService.CreateRole(
    "VIP",
    new[] { "teleport.use", "kit.vip" },
    10,
    "VIP members with special perks"
);

// Create an advanced Builder role
var builderRole = RoleService.CreateRole(
    "Builder",
    new[] { "build.*", "spawner.use", "noclip" },
    30,
    "Server builders"
);

if (vipRole != null) {
    Log.Message("VIP role created successfully!");
}
```

**Notes:**
- Priority must be between 0-100 (Owner role is fixed at 200)
- Role names are case-insensitive
- Cannot create duplicate roles

---

### DeleteRole
```csharp
public static bool DeleteRole(string roleName)
```

Deletes a role by name and removes it from all players.

**Parameters:**
- `roleName` (string): Name of the role to delete

**Returns:**
- `bool`: True if deleted, false if not found

**Example:**
```csharp
if (RoleService.DeleteRole("VIP")) {
    MessageService.SendAdmins("VIP role has been deleted!");
} else {
    Log.Warning("Role not found or is a default role");
}
```

**Notes:**
- Cannot delete default roles (Owner, Admin, Moderator, Support, Default)
- Automatically removes the role from all players who have it

---

### GetRole
```csharp
public static Role GetRole(string roleName)
```

Gets a role by name.

**Parameters:**
- `roleName` (string): Name of the role

**Returns:**
- `Role`: The role if found, null otherwise

**Example:**
```csharp
var adminRole = RoleService.GetRole("Admin");
if (adminRole != null) {
    Log.Message($"Admin role has priority: {adminRole.Priority}");
    Log.Message($"Permissions: {string.Join(", ", adminRole.Permissions)}");
}
```

---

### RoleExists
```csharp
public static bool RoleExists(string roleName)
```

Checks if a role exists.

**Parameters:**
- `roleName` (string): Name of the role

**Returns:**
- `bool`: True if exists, false otherwise

**Example:**
```csharp
if (RoleService.RoleExists("VIP")) {
    MessageService.SendAll("VIP role is available!");
}
```

---

### GetAllRoles
```csharp
public static List<Role> GetAllRoles()
```

Gets all available roles ordered by priority (highest first).

**Returns:**
- `List<Role>`: List of all roles

**Example:**
```csharp
var allRoles = RoleService.GetAllRoles();

foreach (var role in allRoles) {
    Log.Message($"{role.Name} - Priority: {role.Priority}");
}
```

---

### UpdateRole
```csharp
public static bool UpdateRole(string roleName, string[] newPermissions = null, int? newPriority = null, string newDescription = null)
```

Updates a role's properties.

**Parameters:**
- `roleName` (string): Name of the role to update
- `newPermissions` (string[], optional): New permissions (null to keep existing)
- `newPriority` (int?, optional): New priority (null to keep existing)
- `newDescription` (string, optional): New description (null to keep existing)

**Returns:**
- `bool`: True if updated, false if role not found

**Example:**
```csharp
// Update VIP permissions
RoleService.UpdateRole(
    "VIP",
    newPermissions: new[] { "teleport.use", "kit.vip", "fly" }
);

// Update priority only
RoleService.UpdateRole("Builder", newPriority: 35);

// Update description only
RoleService.UpdateRole("VIP", newDescription: "Premium VIP members");
```

---

## Permission Management

### AddPermissionToRole
```csharp
public static bool AddPermissionToRole(string roleName, string permission)
```

Adds a permission to a role.

**Parameters:**
- `roleName` (string): Name of the role
- `permission` (string): Permission to add

**Returns:**
- `bool`: True if added, false otherwise

**Example:**
```csharp
if (RoleService.AddPermissionToRole("VIP", "noclip")) {
    MessageService.SendAdmins("Added noclip permission to VIP role");
}
```

---

### RemovePermissionFromRole
```csharp
public static bool RemovePermissionFromRole(string roleName, string permission)
```

Removes a permission from a role.

**Parameters:**
- `roleName` (string): Name of the role
- `permission` (string): Permission to remove

**Returns:**
- `bool`: True if removed, false otherwise

**Example:**
```csharp
if (RoleService.RemovePermissionFromRole("VIP", "fly")) {
    MessageService.SendAdmins("Removed fly permission from VIP role");
}
```

---

### GetAllPermissions
```csharp
public static List<string> GetAllPermissions()
```

Gets all unique permissions from all roles in the system.

**Returns:**
- `List<string>`: List of unique permission strings

**Example:**
```csharp
var allPermissions = RoleService.GetAllPermissions();

Log.Message("Available permissions:");
foreach (var perm in allPermissions) {
    Log.Message($"  - {perm}");
}
```

---

## Player Role Management

### AddRoleToPlayer
```csharp
public static bool AddRoleToPlayer(PlayerData player, string roleName, string durationStr = null)
```

Adds a role to a player with optional duration.

**Parameters:**
- `player` (PlayerData): The player to add the role to
- `roleName` (string): Name of the role to add
- `durationStr` (string, optional): Duration string (e.g., "30d", "2h", "1y"). Use "-1" or null for permanent

**Returns:**
- `bool`: True if added, false if role doesn't exist or player already has it

**Example:**
```csharp
// Permanent role
RoleService.AddRoleToPlayer(playerData, "VIP");

// Temporary role (30 days)
RoleService.AddRoleToPlayer(playerData, "VIP", "30d");

// Temporary role (2 hours)
RoleService.AddRoleToPlayer(playerData, "Builder", "2h");

// Different duration formats
RoleService.AddRoleToPlayer(playerData, "Support", "1 week");
RoleService.AddRoleToPlayer(playerData, "Moderator", "3 months");
```

**Duration Formats:** See [Duration Format](#duration-format) section

---

### RemoveRoleFromPlayer
```csharp
public static bool RemoveRoleFromPlayer(PlayerData player, string roleName, PlayerData removedBy = null)
```

Removes a role from a player.

**Parameters:**
- `player` (PlayerData): The player to remove the role from
- `roleName` (string): Name of the role to remove
- `removedBy` (PlayerData, optional): The player removing the role (required for Owner role)

**Returns:**
- `bool`: True if removed, false if player didn't have the role

**Example:**
```csharp
// Remove VIP role
if (RoleService.RemoveRoleFromPlayer(playerData, "VIP")) {
    MessageService.Send(playerData, "Your VIP role has been removed.");
}

// Remove Owner role (requires admin)
RoleService.RemoveRoleFromPlayer(targetPlayer, "Owner", adminPlayer);
```

**Notes:**
- Cannot remove the Default role
- Only admins can remove the Owner role

---

### PlayerHasRole
```csharp
public static bool PlayerHasRole(PlayerData player, string roleName)
```

Checks if a player has a specific role.

**Parameters:**
- `player` (PlayerData): The player to check
- `roleName` (string): Name of the role

**Returns:**
- `bool`: True if player has the role, false otherwise

**Example:**
```csharp
if (RoleService.PlayerHasRole(playerData, "VIP")) {
    MessageService.Send(playerData, "Welcome VIP member!");
}

if (RoleService.PlayerHasRole(playerData, "Moderator")) {
    // Grant moderator UI access
}
```

---

### GetPlayerRoles
```csharp
public static List<string> GetPlayerRoles(PlayerData player)
```

Gets all active (non-expired) role names assigned to a player.

**Parameters:**
- `player` (PlayerData): The player

**Returns:**
- `List<string>`: List of role names

**Example:**
```csharp
var roles = RoleService.GetPlayerRoles(playerData);

MessageService.Send(playerData, $"Your roles: {string.Join(", ", roles)}");
```

---

### GetPlayerRoleObjects
```csharp
public static List<Role> GetPlayerRoleObjects(PlayerData player)
```

Gets all Role objects assigned to a player, ordered by priority (highest first).

**Parameters:**
- `player` (PlayerData): The player

**Returns:**
- `List<Role>`: List of Role objects

**Example:**
```csharp
var roles = RoleService.GetPlayerRoleObjects(playerData);

foreach (var role in roles) {
    Log.Message($"Role: {role.Name}, Priority: {role.Priority}");
    Log.Message($"Permissions: {string.Join(", ", role.Permissions)}");
}
```

---

### GetPlayerPrimaryRole
```csharp
public static Role GetPlayerPrimaryRole(PlayerData player)
```

Gets the highest priority role for a player.

**Parameters:**
- `player` (PlayerData): The player

**Returns:**
- `Role`: The highest priority role, or null if player has no roles

**Example:**
```csharp
var primaryRole = RoleService.GetPlayerPrimaryRole(playerData);

if (primaryRole != null) {
    MessageService.Send(playerData, $"Your primary role: {primaryRole.Name}");
    PlayerService.SetNameTag(playerData, primaryRole.Name);
}
```

---

### GetPlayerRoleAssignmentsInfo
```csharp
public static List<PlayerRoleAssignment> GetPlayerRoleAssignmentsInfo(PlayerData player)
```

Gets role assignment information for a player including expiration details.

**Parameters:**
- `player` (PlayerData): The player

**Returns:**
- `List<PlayerRoleAssignment>`: List of active role assignments ordered by priority

**Example:**
```csharp
var assignments = RoleService.GetPlayerRoleAssignmentsInfo(playerData);

foreach (var assignment in assignments) {
    var expiry = assignment.ExpiresAt.HasValue 
        ? $"Expires: {assignment.ExpiresAt.Value:dd/MM/yyyy HH:mm}" 
        : "Permanent";
    
    MessageService.Send(playerData, $"Role: {assignment.RoleName} - {expiry}");
}
```

---

### ClearPlayerRoles
```csharp
public static void ClearPlayerRoles(PlayerData player)
```

Clears all roles from a player and assigns default User role.

**Parameters:**
- `player` (PlayerData): The player to clear roles from

**Example:**
```csharp
RoleService.ClearPlayerRoles(playerData);
MessageService.Send(playerData, "All your roles have been cleared.");
```

---

## Permission Checking

### PlayerHasPermission
```csharp
public static bool PlayerHasPermission(PlayerData player, string permission)
```

Checks if a player has a specific permission (from any of their roles).

**Parameters:**
- `player` (PlayerData): The player
- `permission` (string): The permission to check

**Returns:**
- `bool`: True if player has the permission, false otherwise

**Example:**
```csharp
if (RoleService.PlayerHasPermission(playerData, "teleport.use")) {
    // Allow teleportation
    TeleportService.Teleport(playerData, destination);
} else {
    MessageService.SendError(playerData, "You don't have permission to teleport!");
}

// Wildcard check (roles with "*" permission)
if (RoleService.PlayerHasPermission(playerData, "*")) {
    MessageService.Send(playerData, "You have full permissions!");
}
```

**Permission System:**
- Wildcard `*` grants all permissions
- Checks all player roles
- Case-sensitive permission names

---

## Query Operations

### GetPlayersWithRole
```csharp
public static List<PlayerData> GetPlayersWithRole(string roleName)
```

Gets all players who have a specific role.

**Parameters:**
- `roleName` (string): Name of the role

**Returns:**
- `List<PlayerData>`: List of players with that role

**Example:**
```csharp
var vipPlayers = RoleService.GetPlayersWithRole("VIP");

MessageService.SendAll($"VIP players online: {vipPlayers.Count(p => p.IsConnected)}");

foreach (var vip in vipPlayers) {
    if (vip.IsConnected) {
        MessageService.Send(vip, "Special VIP event starting!");
    }
}
```

---

## Duration Format

The service supports flexible duration strings for temporary role assignments:

### Supported Formats

```csharp
// Seconds
"30"        // 30 seconds
"30s"       // 30 seconds
"30 seconds"

// Minutes
"15m"       // 15 minutes
"15 minutes"

// Hours
"2h"        // 2 hours
"2 hours"

// Days
"7d"        // 7 days
"7 days"

// Weeks
"2w"        // 2 weeks
"2 weeks"

// Months
"3month"    // 3 months (30 days each)
"3 months"

// Years
"1y"        // 1 year (365 days)
"1 year"
```

### Examples

```csharp
// 30 day VIP
RoleService.AddRoleToPlayer(player, "VIP", "30d");

// 6 month subscription
RoleService.AddRoleToPlayer(player, "Premium", "6 months");

// 1 week trial
RoleService.AddRoleToPlayer(player, "Trial", "1w");

// Permanent
RoleService.AddRoleToPlayer(player, "Supporter", "-1");
RoleService.AddRoleToPlayer(player, "Supporter"); // null = permanent
```

---

## Built-in Commands

The service includes comprehensive built-in commands:

### Role Management Commands
- `/roles create <name> <priority>` - Create a new role
- `/roles delete <name>` - Delete a role
- `/roles list` - List all roles
- `/roles info <name>` - Show role information
- `/roles setpriority <name> <priority>` - Set role priority
- `/roles setdescription <name> <desc>` - Set role description

### Permission Commands
- `/roles addpermission <role> <permission>` - Add permission to role
- `/roles removepermission <role> <permission>` - Remove permission from role
- `/roles listpermissions` - List all available permissions

### Player Role Commands
- `/roles assign <player> <role> [duration]` - Assign role to player
- `/roles unassign <player> <role>` - Remove role from player
- `/roles playerroles <player>` - Show player's roles
- `/roles playerswithRole <role>` - List players with specific role
- `/roles clear <player>` - Clear all roles from player

---

## Complete Examples

### Example 1: VIP System

```csharp
using ScarletCore.Services;
using ScarletCore.Commanding;

public class VIPSystem {
    public static void InitializeVIPRole() {
        // Create VIP role if it doesn't exist
        if (!RoleService.RoleExists("VIP")) {
            RoleService.CreateRole(
                "VIP",
                new[] { "teleport.use", "kit.vip", "home.multiple", "no_cooldown" },
                15,
                "VIP members with premium features"
            );
        }
    }
    
    [Command("vip buy", description: "Purchase VIP for 30 days")]
    public static void BuyVIPCommand(CommandContext ctx) {
        var player = ctx.Player;
        
        // Check if already VIP
        if (RoleService.PlayerHasRole(player, "VIP")) {
            MessageService.SendWarning(player, "You already have VIP!");
            return;
        }
        
        // Check currency (example)
        if (!HasEnoughGold(player, 10000)) {
            MessageService.SendError(player, "You need 10,000 gold to purchase VIP!");
            return;
        }
        
        // Grant VIP for 30 days
        if (RoleService.AddRoleToPlayer(player, "VIP", "30d")) {
            RemoveGold(player, 10000);
            MessageService.SendSuccess(player, "VIP purchased for 30 days!");
            PlayerService.SetNameTag(player, "VIP");
        }
    }
}
```

### Example 2: Staff Management System

```csharp
public class StaffManagement {
    [Command("promote", description: "Promote a player to staff", adminOnly: true)]
    public static void PromoteCommand(CommandContext ctx, PlayerData target, string role) {
        var admin = ctx.Player;
        
        // Validate role
        if (!RoleService.RoleExists(role)) {
            MessageService.SendError(admin, $"Role '{role}' doesn't exist!");
            return;
        }
        
        // Check if admin can assign this role (priority check)
        var adminRole = RoleService.GetPlayerPrimaryRole(admin);
        var targetRole = RoleService.GetRole(role);
        
        if (adminRole.Priority <= targetRole.Priority) {
            MessageService.SendError(admin, "You can't assign a role with equal or higher priority!");
            return;
        }
        
        // Assign role
        if (RoleService.AddRoleToPlayer(target, role)) {
            MessageService.SendSuccess(admin, $"Promoted {target.Name} to {role}!");
            MessageService.Send(target, $"You have been promoted to {role}!");
            
            // Set name tag
            PlayerService.SetNameTag(target, role);
            
            // Log the action
            Log.Message($"{admin.Name} promoted {target.Name} to {role}");
        }
    }
    
    [Command("demote", description: "Remove staff role from a player", adminOnly: true)]
    public static void DemoteCommand(CommandContext ctx, PlayerData target, string role) {
        var admin = ctx.Player;
        
        if (RoleService.RemoveRoleFromPlayer(target, role, admin)) {
            MessageService.SendSuccess(admin, $"Removed {role} from {target.Name}!");
            MessageService.SendWarning(target, $"Your {role} role has been removed.");
            PlayerService.RemoveNameTag(target, role);
        } else {
            MessageService.SendError(admin, $"{target.Name} doesn't have the {role} role!");
        }
    }
}
```

### Example 3: Permission-Based Features

```csharp
public class PermissionFeatures {
    [Command("fly", description: "Toggle fly mode")]
    public static void FlyCommand(CommandContext ctx) {
        var player = ctx.Player;
        
        // Check permission
        if (!RoleService.PlayerHasPermission(player, "fly")) {
            MessageService.SendError(player, "You don't have permission to fly!");
            MessageService.SendInfo(player, "Get VIP to unlock this feature!");
            return;
        }
        
        // Toggle fly mode
        ToggleFlyMode(player);
        MessageService.SendSuccess(player, "Fly mode toggled!");
    }
    
    [Command("teleport", description: "Teleport to a player")]
    public static void TeleportCommand(CommandContext ctx, PlayerData target) {
        var player = ctx.Player;
        
        if (!RoleService.PlayerHasPermission(player, "teleport.use")) {
            MessageService.SendError(player, "You need VIP or higher to teleport!");
            return;
        }
        
        TeleportService.Teleport(player, target.Position);
        MessageService.SendSuccess(player, $"Teleported to {target.Name}!");
    }
    
    public static bool CanBypassCooldown(PlayerData player) {
        return RoleService.PlayerHasPermission(player, "no_cooldown") ||
               RoleService.PlayerHasPermission(player, "*");
    }
}
```

### Example 4: Role Expiration Notifications

```csharp
using ScarletCore.Systems;

public class RoleExpirationNotifier {
    public static void StartMonitoring() {
        // Check every hour
        ActionScheduler.Repeating(() => {
            CheckExpiringRoles();
        }, 3600);
    }
    
    private static void CheckExpiringRoles() {
        foreach (var player in PlayerService.GetAllConnected()) {
            var assignments = RoleService.GetPlayerRoleAssignmentsInfo(player);
            
            foreach (var assignment in assignments) {
                if (!assignment.ExpiresAt.HasValue) continue;
                
                var timeLeft = assignment.ExpiresAt.Value - DateTime.Now;
                
                // Notify if expiring in less than 24 hours
                if (timeLeft.TotalHours < 24 && timeLeft.TotalHours > 0) {
                    var hoursLeft = (int)timeLeft.TotalHours;
                    MessageService.SendWarning(
                        player,
                        $"Your {assignment.RoleName} role expires in {hoursLeft} hours!"
                    );
                }
                
                // Notify if expiring in less than 1 hour
                if (timeLeft.TotalMinutes < 60 && timeLeft.TotalMinutes > 0) {
                    var minutesLeft = (int)timeLeft.TotalMinutes;
                    MessageService.SendError(
                        player,
                        $"Your {assignment.RoleName} role expires in {minutesLeft} minutes!"
                    );
                }
            }
        }
    }
}
```

### Example 5: Dynamic Role System

```csharp
public class DynamicRoles {
    public static void GrantAchievementRole(PlayerData player, string achievementName) {
        var roleName = $"Achievement_{achievementName}";
        
        // Create role if it doesn't exist
        if (!RoleService.RoleExists(roleName)) {
            RoleService.CreateRole(
                roleName,
                new[] { $"achievement.{achievementName}" },
                5,
                $"Earned by completing {achievementName}"
            );
        }
        
        // Grant role permanently
        RoleService.AddRoleToPlayer(player, roleName);
        
        MessageService.SendSuccess(player, $"Achievement unlocked: {achievementName}!");
        MessageService.Announce($"{player.Name} earned the {achievementName} achievement!");
    }
    
    public static void GrantSeasonalRole(PlayerData player, string season) {
        var roleName = $"Season_{season}";
        
        if (!RoleService.RoleExists(roleName)) {
            RoleService.CreateRole(
                roleName,
                new[] { "seasonal.rewards", $"season.{season}" },
                8,
                $"{season} season participant"
            );
        }
        
        // Grant for 90 days
        RoleService.AddRoleToPlayer(player, roleName, "90d");
        
        MessageService.Send(player, $"Seasonal role granted for {season}!");
    }
}
```

### Example 6: Role-Based Chat System

```csharp
public class RoleChatSystem {
    public static string FormatChatMessage(PlayerData player, string message) {
        var primaryRole = RoleService.GetPlayerPrimaryRole(player);
        
        if (primaryRole == null) {
            return $"{player.Name}: {message}";
        }
        
        // Color based on role priority
        var color = primaryRole.Priority switch {
            >= 100 => "red",      // Admin+
            >= 50 => "yellow",    // Moderator
            >= 15 => "green",     // VIP
            _ => "white"          // Default
        };
        
        var roleTag = $"[{primaryRole.Name}]".WithColor(color);
        var playerName = player.Name.WithColor(color);
        
        return $"{roleTag} {playerName}: {message}";
    }
    
    public static void BroadcastToRole(string roleName, string message) {
        var players = RoleService.GetPlayersWithRole(roleName);
        
        foreach (var player in players.Where(p => p.IsConnected)) {
            MessageService.Send(player, $"[{roleName} Chat] {message}");
        }
    }
}
```

---

## Best Practices

### 1. Use Appropriate Priority Levels

```csharp
// Good - Clear hierarchy
RoleService.CreateRole("Owner", ["*"], 200);      // Max priority
RoleService.CreateRole("Admin", ["*"], 100);      // Full admin
RoleService.CreateRole("Moderator", null, 50);    // Mid-level
RoleService.CreateRole("VIP", null, 15);          // Above default
RoleService.CreateRole("Default", null, 0);       // Base level
```

### 2. Use Permission Wildcards Wisely

```csharp
// Good - Specific permissions
RoleService.CreateRole("Builder", new[] { "build.*", "spawner.use" }, 25);

// Good - Category wildcards
RoleService.CreateRole("EventManager", new[] { "event.*", "broadcast" }, 40);

// Caution - Full wildcard (only for admins)
RoleService.CreateRole("Admin", new[] { "*" }, 100);
```

### 3. Always Check Permissions

```csharp
// Good - Check before action
if (RoleService.PlayerHasPermission(player, "teleport.use")) {
    TeleportService.Teleport(player, destination);
} else {
    MessageService.SendError(player, "No permission!");
}

// Better - Centralized permission check
public static bool CanUseTeleport(PlayerData player) {
    return RoleService.PlayerHasPermission(player, "teleport.use") ||
           player.IsAdmin;
}
```

### 4. Handle Role Expiration

```csharp
// Good - Notify before expiration
var assignments = RoleService.GetPlayerRoleAssignmentsInfo(player);
foreach (var assignment in assignments) {
    if (assignment.ExpiresAt.HasValue) {
        var timeLeft = assignment.ExpiresAt.Value - DateTime.Now;
        if (timeLeft.TotalDays < 3) {
            MessageService.SendWarning(player, $"Role expires soon: {assignment.RoleName}");
        }
    }
}
```

### 5. Validate Role Operations

```csharp
// Good - Check role exists before operations
if (RoleService.RoleExists("VIP")) {
    RoleService.AddRoleToPlayer(player, "VIP", "30d");
} else {
    Log.Error("VIP role doesn't exist - creating it first");
    RoleService.CreateRole("VIP", new[] { "vip.perks" }, 15);
}
```

### 6. Use Primary Role for Display

```csharp
// Good - Show highest priority role
var primaryRole = RoleService.GetPlayerPrimaryRole(player);
if (primaryRole != null) {
    PlayerService.SetNameTag(player, primaryRole.Name);
}
```

---

## Technical Notes

### Role Priority System
- Priority determines role hierarchy (higher = more important)
- Only Owner role can have priority > 100
- Used for permission resolution and administrative checks
- Player's primary role is their highest priority role

### Permission Resolution
- Wildcard `*` grants all permissions
- Permissions are checked across all player roles
- First match wins (any role with permission grants it)
- Case-sensitive permission strings

### Expiration System
- Automatic checking every 60 seconds
- Expired roles are automatically removed
- Permanent roles have `ExpiresAt = null`
- Expiration is based on server time

### Data Persistence
- Roles stored in database with key: `system.roles`
- Player roles stored with key: `player.roles.{platformId}`
- Automatic save on all modifications
- Changes persist across server restarts

---

## Related Services
- [PlayerService](PlayerService.md) - For player data and retrieval
- [AdminService](AdminService.md) - For admin-specific operations
- [MessageService](MessageService.md) - For sending messages
- [CommandHandler](../Commanding/CommandHandler.md) - For command permissions

## Notes
- Default roles are created automatically on initialization
- Owner role priority is fixed at 200 and cannot be changed
- Default role cannot be removed from players
- Role expiration is checked automatically every minute
- All role names are case-insensitive
- Permission strings are case-sensitive
