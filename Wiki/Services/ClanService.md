# ClanService

The `ClanService` provides comprehensive functionality for managing clan operations in V Rising. It handles clan membership, role assignments, clan information retrieval, and various administrative tasks.

## Namespace
```csharp
using ScarletCore.Services;
```

## Overview

The `ClanService` is a static utility class that provides methods for:
- Managing clan members (add, remove, list)
- Changing player roles within clans
- Retrieving clan information
- Clan administration (rename, disband, set motto)
- Managing clan tags for players

## Table of Contents

- [Member Management](#member-management)
  - [GetMembers](#getmembers)
  - [TryAddMember](#tryaddmember)
  - [TryRemoveFromClan](#tryremovefromclan)
- [Role Management](#role-management)
  - [TryChangeClanRole](#trychangeclanrole)
- [Clan Information Retrieval](#clan-information-retrieval)
  - [TryGetClanLeader](#trygetclanleader)
  - [ListClans](#listclans)
  - [GetClanName](#getclanname)
  - [TryGetClanEntity](#trygetclanentity)
  - [GetClanEntities](#getclanentities)
  - [GetClanTeams](#getclanteams)
- [Clan Administration](#clan-administration)
  - [RenameClan](#renameclan)
  - [SetMotto](#setmotto)
  - [DisbandClan](#disbandclan)
- [Clan Tag Management](#clan-tag-management)
  - [SetTagForPlayer](#settagforplayer)
  - [RemoveTagFromPlayer](#removetagfromplayer)
- [Complete Example](#complete-example-clan-management-system)
- [Best Practices](#best-practices)
- [Related Services](#related-services)

## Methods

### Member Management

#### GetMembers
```csharp
public static List<PlayerData> GetMembers(string clanName)
```

Retrieves all members of a specified clan.

**Parameters:**
- `clanName` (string): The name of the clan to get members from (case-insensitive)

**Returns:**
- `List<PlayerData>`: A list of PlayerData objects representing all clan members

**Example:**
```csharp
var clanName = "DragonSlayers";
var members = ClanService.GetMembers(clanName);

foreach (var member in members) {
    Log.Message($"Member: {member.Name}");
}
```

---

#### TryAddMember
```csharp
public static bool TryAddMember(PlayerData playerData, string clanName)
```

Attempts to add a player to a specified clan.

**Parameters:**
- `playerData` (PlayerData): The player data of the player to add
- `clanName` (string): The name of the clan to add the player to

**Returns:**
- `bool`: True if the player was successfully added, false otherwise

**Example:**
```csharp
if (PlayerService.TryGetById(platformId, out var playerData)) {
    if (ClanService.TryAddMember(playerData, "DragonSlayers")) {
        MessageService.Send(playerData, "You have been added to DragonSlayers!");
    } else {
        MessageService.Send(playerData, "Failed to add you to the clan.");
    }
}
```

**Notes:**
- Players already in a clan cannot be added to another clan
- The clan must exist for the operation to succeed
- New members are automatically assigned the "Member" role

---

#### TryRemoveFromClan
```csharp
public static bool TryRemoveFromClan(PlayerData playerData)
```

Attempts to remove a player from their current clan.

**Parameters:**
- `playerData` (PlayerData): The player data of the player to remove

**Returns:**
- `bool`: True if the player was successfully removed, false otherwise

**Example:**
```csharp
if (ClanService.TryRemoveFromClan(playerData)) {
    MessageService.Send(playerData, "You have been removed from your clan.");
} else {
    MessageService.Send(playerData, "Failed to remove you from the clan.");
}
```

**Notes:**
- Only clan leaders can remove members
- Clan leaders cannot be removed using this method
- The player must be in a clan for this operation to succeed

---

### Role Management

#### TryChangeClanRole
```csharp
public static bool TryChangeClanRole(PlayerData playerData, ClanRoleEnum newRole)
```

Attempts to change a player's role within their clan.

**Parameters:**
- `playerData` (PlayerData): The player whose role should be changed
- `newRole` (ClanRoleEnum): The new clan role to assign

**Returns:**
- `bool`: True if the role was successfully changed, false if the player is not in a clan

**Example:**
```csharp
// Promote player to officer
if (ClanService.TryChangeClanRole(playerData, ClanRoleEnum.Officer)) {
    MessageService.Send(playerData, "You have been promoted to Officer!");
}

// Available roles: Leader, Officer, Member
```

**Available Clan Roles:**
- `ClanRoleEnum.Leader`
- `ClanRoleEnum.Officer`
- `ClanRoleEnum.Member`

---

### Clan Information Retrieval

#### TryGetClanLeader
```csharp
public static bool TryGetClanLeader(string clanName, out PlayerData clanLeader)
```

Attempts to find and return the leader of a specified clan.

**Parameters:**
- `clanName` (string): The name of the clan to find the leader for (case-insensitive)
- `clanLeader` (out PlayerData): Output parameter containing the clan leader's PlayerData if found

**Returns:**
- `bool`: True if a clan leader was found, false otherwise

**Example:**
```csharp
if (ClanService.TryGetClanLeader("DragonSlayers", out var leader)) {
    Log.Message($"Clan leader: {leader.Name}");
} else {
    Log.Warning("Clan not found or has no leader.");
}
```

---

#### ListClans
```csharp
public static Dictionary<string, List<PlayerData>> ListClans()
```

Retrieves a dictionary of all clans with their members.

**Returns:**
- `Dictionary<string, List<PlayerData>>`: A dictionary where keys are clan names and values are lists of PlayerData for each clan

**Example:**
```csharp
var allClans = ClanService.ListClans();

foreach (var clan in allClans) {
    Log.Message($"Clan: {clan.Key} has {clan.Value.Count} members");
    
    foreach (var member in clan.Value) {
        Log.Message($"  - {member.Name}");
    }
}
```

---

#### GetClanName
```csharp
public static string GetClanName(PlayerData playerData)
```

Retrieves the name of the clan that the specified player belongs to.

**Parameters:**
- `playerData` (PlayerData): The player whose clan name is to be retrieved

**Returns:**
- `string`: The name of the player's clan, or an empty string if the player is not in a clan

**Example:**
```csharp
var clanName = ClanService.GetClanName(playerData);

if (string.IsNullOrEmpty(clanName)) {
    MessageService.Send(playerData, "You are not in a clan.");
} else {
    MessageService.Send(playerData, $"Your clan: {clanName}");
}
```

---

#### TryGetClanEntity
```csharp
public static bool TryGetClanEntity(string clanName, out Entity clanEntity)
```

Attempts to find a clan entity by name.

**Parameters:**
- `clanName` (string): The name of the clan to find (case-insensitive)
- `clanEntity` (out Entity): Output parameter containing the clan entity if found

**Returns:**
- `bool`: True if the clan was found, false otherwise

**Example:**
```csharp
if (ClanService.TryGetClanEntity("DragonSlayers", out var clanEntity)) {
    var clanTeam = clanEntity.Read<ClanTeam>();
    Log.Message($"Found clan: {clanTeam.Name}");
}
```

**Warning:**
- This method contains a known bug in the comparison logic

---

#### GetClanEntities
```csharp
public static NativeArray<Entity> GetClanEntities()
```

Creates a native array of all clan entities in the game.

**Returns:**
- `NativeArray<Entity>`: A NativeArray containing all clan entities with ClanTeam component

**Example:**
```csharp
var clanEntities = ClanService.GetClanEntities();

foreach (var clan in clanEntities) {
    var clanTeam = clan.Read<ClanTeam>();
    Log.Message($"Clan: {clanTeam.Name}");
}

// Important: Dispose the array to prevent memory leaks
clanEntities.Dispose();
```

**Important:**
- The caller is responsible for disposing the returned array to prevent memory leaks
- Always call `.Dispose()` on the returned array after use

---

#### GetClanTeams
```csharp
public static List<ClanTeam> GetClanTeams()
```

Retrieves a list of all clan team components from active clans in the game.

**Returns:**
- `List<ClanTeam>`: A list of ClanTeam components containing clan information

**Example:**
```csharp
var clanTeams = ClanService.GetClanTeams();

foreach (var team in clanTeams) {
    Log.Message($"Clan: {team.Name}, Motto: {team.Motto}");
}
```

---

### Clan Administration

#### RenameClan
```csharp
public static void RenameClan(string oldClanName, string newClanName)
```

Renames an existing clan and updates all member references.

**Parameters:**
- `oldClanName` (string): The current name of the clan
- `newClanName` (string): The new name for the clan

**Example:**
```csharp
ClanService.RenameClan("OldClanName", "NewClanName");
Log.Message("Clan renamed successfully!");
```

**Notes:**
- Automatically updates all member references to reflect the new name
- Updates display names for all members

---

#### SetMotto
```csharp
public static void SetMotto(string clanName, string motto)
```

Sets the motto for a specified clan.

**Parameters:**
- `clanName` (string): The name of the clan to update
- `motto` (string): The new motto text to set for the clan

**Example:**
```csharp
ClanService.SetMotto("DragonSlayers", "We slay dragons!");
```

---

#### DisbandClan
```csharp
public static void DisbandClan(string clanName)
```

Disbands a clan by destroying its entity.

**Parameters:**
- `clanName` (string): The name of the clan to disband

**Example:**
```csharp
ClanService.DisbandClan("InactiveClan");
Log.Message("Clan has been disbanded.");
```

**Warning:**
- This permanently removes the clan and all its data
- This operation cannot be undone

---

### Clan Tag Management

#### SetTagForPlayer
```csharp
public static void SetTagForPlayer(PlayerData playerData, string clanTag)
```

Sets a custom clan tag for a player's character name.

**Parameters:**
- `playerData` (PlayerData): The player data of the player to set the tag for
- `clanTag` (string): The clan tag text to display on the player's name

**Example:**
```csharp
ClanService.SetTagForPlayer(playerData, "[DS]");
// Player name will now display with the [DS] tag
```

**Notes:**
- This changes the visual tag display without affecting actual clan membership
- Can be used to give custom tags to players

---

#### RemoveTagFromPlayer
```csharp
public static void RemoveTagFromPlayer(PlayerData playerData)
```

Removes the clan tag from a player's character name.

**Parameters:**
- `playerData` (PlayerData): The player data of the player to remove the tag from

**Example:**
```csharp
ClanService.RemoveTagFromPlayer(playerData);
// Player's clan tag will be removed from display
```

**Notes:**
- This only removes the visual tag display
- Does not remove the player from the clan

---

## Complete Example: Clan Management System

```csharp
using ScarletCore.Services;
using ScarletCore.Commanding;

public class ClanCommands {
    [Command("clan list", description: "List all clans and their members")]
    public static void ListAllClans(CommandContext ctx) {
        var clans = ClanService.ListClans();
        
        if (clans.Count == 0) {
            MessageService.Send(ctx.User, "No clans exist on this server.");
            return;
        }
        
        foreach (var clan in clans) {
            MessageService.Send(ctx.User, $"<color=yellow>{clan.Key}</color> ({clan.Value.Count} members)");
            
            foreach (var member in clan.Value) {
                MessageService.Send(ctx.User, $"  - {member.Name}");
            }
        }
    }
    
    [Command("clan invite", description: "Invite a player to your clan")]
    public static void InvitePlayer(CommandContext ctx, string playerName) {
        // Get the target player
        if (!PlayerService.TryGetByName(playerName, out var targetPlayer)) {
            MessageService.Send(ctx.User, "Player not found.");
            return;
        }
        
        // Get the commander's clan name
        var clanName = ClanService.GetClanName(ctx.Player);
        
        if (string.IsNullOrEmpty(clanName)) {
            MessageService.Send(ctx.User, "You are not in a clan.");
            return;
        }
        
        // Add the player to the clan
        if (ClanService.TryAddMember(targetPlayer, clanName)) {
            MessageService.Send(ctx.User, $"{playerName} has been added to your clan!");
            MessageService.Send(targetPlayer.UserEntity, $"You have been invited to {clanName}!");
        } else {
            MessageService.Send(ctx.User, "Failed to add player. They may already be in a clan.");
        }
    }
    
    [Command("clan kick", description: "Remove a player from your clan")]
    public static void KickPlayer(CommandContext ctx, string playerName) {
        if (!PlayerService.TryGetByName(playerName, out var targetPlayer)) {
            MessageService.Send(ctx.User, "Player not found.");
            return;
        }
        
        if (ClanService.TryRemoveFromClan(targetPlayer)) {
            MessageService.Send(ctx.User, $"{playerName} has been removed from the clan.");
            MessageService.Send(targetPlayer.UserEntity, "You have been removed from your clan.");
        } else {
            MessageService.Send(ctx.User, "Failed to remove player from clan.");
        }
    }
    
    [Command("clan promote", description: "Promote a player to officer")]
    public static void PromotePlayer(CommandContext ctx, string playerName) {
        if (!PlayerService.TryGetByName(playerName, out var targetPlayer)) {
            MessageService.Send(ctx.User, "Player not found.");
            return;
        }
        
        if (ClanService.TryChangeClanRole(targetPlayer, ClanRoleEnum.Officer)) {
            MessageService.Send(ctx.User, $"{playerName} has been promoted to Officer!");
            MessageService.Send(targetPlayer.UserEntity, "You have been promoted to Officer!");
        } else {
            MessageService.Send(ctx.User, "Failed to promote player.");
        }
    }
    
    [Command("clan motto", description: "Set your clan's motto")]
    public static void SetClanMotto(CommandContext ctx, string motto) {
        var clanName = ClanService.GetClanName(ctx.Player);
        
        if (string.IsNullOrEmpty(clanName)) {
            MessageService.Send(ctx.User, "You are not in a clan.");
            return;
        }
        
        ClanService.SetMotto(clanName, motto);
        MessageService.Send(ctx.User, $"Clan motto updated to: {motto}");
    }
    
    [Command("clan members", description: "List all members of your clan")]
    public static void ListClanMembers(CommandContext ctx) {
        var clanName = ClanService.GetClanName(ctx.Player);
        
        if (string.IsNullOrEmpty(clanName)) {
            MessageService.Send(ctx.User, "You are not in a clan.");
            return;
        }
        
        var members = ClanService.GetMembers(clanName);
        MessageService.Send(ctx.User, $"<color=yellow>{clanName}</color> Members:");
        
        foreach (var member in members) {
            MessageService.Send(ctx.User, $"  - {member.Name}");
        }
    }
}
```

## Best Practices

1. **Memory Management**: Always dispose of `NativeArray<Entity>` returned by `GetClanEntities()` to prevent memory leaks
2. **Clan Validation**: Check if players are in clans before performing clan operations
3. **Leader Protection**: Be aware that clan leaders cannot be removed using `TryRemoveFromClan()`
4. **Case Insensitivity**: Clan name comparisons are case-insensitive throughout the service
5. **Empty Clan Check**: Methods automatically skip empty clans in their operations

## Related Services
- [PlayerService](PlayerService.md) - For managing player data and retrieval
- [MessageService](MessageService.md) - For sending messages to players
- [AdminService](AdminService.md) - For administrative actions

## Notes
- Clan operations require proper entity management
- Some methods use the game's internal clan event system
- The service handles clan entity lookups automatically
- Clan tags are purely cosmetic and don't affect actual clan membership
