# PlayerService

The `PlayerService` manages player data caching and retrieval with multiple indexing strategies for optimal performance. It handles player lifecycle including connection, disconnection, name changes, and provides comprehensive player tag management.

## Namespace
```csharp
using ScarletCore.Services;
```

## Overview

The `PlayerService` is a static utility class that provides:
- Fast player lookup by name, platform ID, or network ID
- Player data caching and indexing
- Player name and tag management
- Admin and role management
- Connected player tracking
- Automatic handling of player lifecycle events

## Table of Contents

- [Public Properties](#public-properties)
  - [AllPlayers](#allplayers)
  - [PlayerNames](#playernames)
  - [PlayerIds](#playerids)
  - [PlayerNetworkIds](#playernetworkids)
- [Player Retrieval](#player-retrieval)
  - [TryGetById](#trygetbyid)
  - [TryGetByNetworkId](#trygetbynetworkid)
  - [TryGetByName](#trygetbyname)
  - [GetAdmins](#getadmins)
  - [GetAllConnected](#getallconnected)
- [Player Name Management](#player-name-management)
  - [RenamePlayer](#renameplayer)
  - [ExtractCleanName](#extractcleanname)
- [Tag Management](#tag-management)
  - [SetNameTag](#setnametag-single-tag)
  - [SetNameTag (With Index)](#setnametag-with-index)
  - [SetTagAtIndex](#settagatindex)
  - [RemoveNameTag](#removenametag-all-tags)
  - [RemoveNameTag (By Index)](#removenametag-by-index)
  - [RemoveNameTag (By Text)](#removenametag-by-text)
  - [RemoveAllTags](#removealltags)
  - [ExtractTags](#extracttags)
  - [TryGetTag](#trygettag)
  - [GetAllTags](#getalltags)
- [Role Management](#role-management)
  - [EnsureOwnerRole](#ensureownerrole)
- [Complete Example](#complete-example-player-management-system)
- [Tag System Explanation](#tag-system-explanation)
- [Best Practices](#best-practices)
- [Caching System](#caching-system)
- [Related Services](#related-services)

## Public Properties

### AllPlayers
```csharp
public static readonly List<PlayerData> AllPlayers
```

Complete list of all known players (online and offline).

**Example:**
```csharp
foreach (var player in PlayerService.AllPlayers) {
    Log.Message($"Player: {player.Name}, Level: {player.Level}");
}
```

---

### PlayerNames
```csharp
public static readonly Dictionary<string, PlayerData> PlayerNames
```

Dictionary for fast player lookup by character name (case-insensitive).

**Example:**
```csharp
if (PlayerService.PlayerNames.TryGetValue("dragonslayer", out var player)) {
    MessageService.Send(player, "Found you by name!");
}
```

---

### PlayerIds
```csharp
public static readonly Dictionary<ulong, PlayerData> PlayerIds
```

Dictionary for fast player lookup by platform ID (Steam ID, etc.).

**Example:**
```csharp
ulong steamId = 76561198012345678;
if (PlayerService.PlayerIds.TryGetValue(steamId, out var player)) {
    MessageService.Send(player, "Found you by platform ID!");
}
```

---

### PlayerNetworkIds
```csharp
public static readonly Dictionary<NetworkId, PlayerData> PlayerNetworkIds
```

Dictionary for fast player lookup by network ID (active connections only).

**Example:**
```csharp
// Only contains currently connected players
var onlineCount = PlayerService.PlayerNetworkIds.Count;
MessageService.SendAll($"Players online: {onlineCount}");
```

---

## Methods

### Player Retrieval

#### TryGetById
```csharp
public static bool TryGetById(ulong platformId, out PlayerData playerData)
```

Attempts to retrieve a player by their platform ID (Steam ID, etc.).

**Parameters:**
- `platformId` (ulong): The platform ID to search for
- `playerData` (out PlayerData): The found player data, or null if not found

**Returns:**
- `bool`: True if player was found, false otherwise

**Example:**
```csharp
ulong steamId = 76561198012345678;
if (PlayerService.TryGetById(steamId, out var player)) {
    MessageService.Send(player, "Welcome back!");
} else {
    Log.Warning("Player not found by platform ID");
}
```

---

#### TryGetByNetworkId
```csharp
public static bool TryGetByNetworkId(NetworkId networkId, out PlayerData playerData)
```

Attempts to retrieve a player by their network ID (only works for online players).

**Parameters:**
- `networkId` (NetworkId): The network ID to search for
- `playerData` (out PlayerData): The found player data, or null if not found

**Returns:**
- `bool`: True if player was found, false otherwise

**Example:**
```csharp
if (PlayerService.TryGetByNetworkId(networkId, out var player)) {
    MessageService.Send(player, $"Hello {player.Name}!");
}
```

**Notes:**
- Only works for currently connected players
- Network IDs may change between sessions

---

#### TryGetByName
```csharp
public static bool TryGetByName(string name, out PlayerData playerData)
```

Attempts to retrieve a player by their character name. Automatically extracts clean names by removing tags.

**Parameters:**
- `name` (string): The character name to search for (case-insensitive, tags will be removed)
- `playerData` (out PlayerData): The found player data, or null if not found

**Returns:**
- `bool`: True if player was found, false otherwise

**Example:**
```csharp
// Works with or without tags
if (PlayerService.TryGetByName("DragonSlayer", out var player)) {
    MessageService.Send(player, "Found you!");
}

// Also works if player has tags like "ADM DragonSlayer"
if (PlayerService.TryGetByName("ADM DragonSlayer", out var player)) {
    MessageService.Send(player, "Found you with tag!");
}
```

**Notes:**
- Case-insensitive search
- Automatically handles discovery of unnamed players
- Strips tags before comparison

---

#### GetAdmins
```csharp
public static List<PlayerData> GetAdmins()
```

Gets all players with admin privileges.

**Returns:**
- `List<PlayerData>`: List of admin players

**Example:**
```csharp
var admins = PlayerService.GetAdmins();

foreach (var admin in admins) {
    MessageService.SendInfo(admin, "Server restart in 5 minutes!");
}

Log.Message($"Total admins: {admins.Count}");
```

---

#### GetAllConnected
```csharp
public static List<PlayerData> GetAllConnected()
```

Gets all currently connected players.

**Returns:**
- `List<PlayerData>`: List of online players

**Example:**
```csharp
var onlinePlayers = PlayerService.GetAllConnected();

MessageService.SendAll($"Players online: {onlinePlayers.Count}");

foreach (var player in onlinePlayers) {
    Log.Message($"Online: {player.Name} at position {player.Position}");
}
```

---

### Player Name Management

#### RenamePlayer
```csharp
public static void RenamePlayer(PlayerData player, FixedString64Bytes newName)
```

Renames the specified player to a new character name.

**Parameters:**
- `player` (PlayerData): The player whose character name will be changed
- `newName` (FixedString64Bytes): The new name to assign to the player

**Example:**
```csharp
using Unity.Collections;

var newName = new FixedString64Bytes("NewPlayerName");
PlayerService.RenamePlayer(playerData, newName);

MessageService.Send(playerData, $"Your name has been changed to: {newName}");
```

**Notes:**
- Automatically updates player cache
- Triggers `PlayerEvents.CharacterRenamed` event
- Updates map icons if player has them

---

#### ExtractCleanName
```csharp
public static string ExtractCleanName(string fullName)
```

Extracts the clean player name by removing all tags from the beginning of the name.

**Parameters:**
- `fullName` (string): The full name that may contain tags (e.g., "ADM GOD Mark")

**Returns:**
- `string`: The clean name without any tags (e.g., "Mark")

**Example:**
```csharp
var fullName = "ADM VIP DragonSlayer";
var cleanName = PlayerService.ExtractCleanName(fullName);
// cleanName = "DragonSlayer"

var noTags = "SimplePlayer";
var stillClean = PlayerService.ExtractCleanName(noTags);
// stillClean = "SimplePlayer"
```

**Notes:**
- Tags are considered everything before the last word
- Returns the whole name if no tags are present

---

### Tag Management

#### SetNameTag (Single Tag)
```csharp
public static bool SetNameTag(PlayerData player, string tag)
```

Sets a name tag for the specified player at tag index 0 (closest to name).

**Parameters:**
- `player` (PlayerData): The player to set the tag for
- `tag` (string): The tag to set (any text is allowed)

**Returns:**
- `bool`: True if the tag was set successfully, false if player or tag is null/empty

**Example:**
```csharp
// Set admin tag at index 0 (closest to name)
PlayerService.SetNameTag(playerData, "ADM");
// Player name becomes: "ADM PlayerName"
```

---

#### SetNameTag (With Index)
```csharp
public static bool SetNameTag(PlayerData player, string tag, int tagIndex)
```

Sets a tag at a specific index in the player's name, allowing for multiple tags or custom tag placement.

**Parameters:**
- `player` (PlayerData): The player whose name tag will be set
- `tag` (string): The tag text to insert
- `tagIndex` (int): The index at which to insert the tag (0-based, 0 is closest to name)

**Returns:**
- `bool`: True if the tag was successfully set; otherwise, false

**Example:**
```csharp
// Set multiple tags
PlayerService.SetNameTag(playerData, "VIP", 0);  // Result: "VIP PlayerName"
PlayerService.SetNameTag(playerData, "GOD", 1);  // Result: "GOD VIP PlayerName"
PlayerService.SetNameTag(playerData, "ADM", 2);  // Result: "ADM GOD VIP PlayerName"
```

**Notes:**
- Index 0 is closest to the name (rightmost tag)
- Higher indices push tags further left
- Replaces existing tag at the specified index

---

#### SetTagAtIndex
```csharp
public static bool SetTagAtIndex(PlayerData player, string tag, int tagIndex)
```

Sets a tag at a specific index. If a tag already exists at that index, it will be replaced.

**Parameters:**
- `player` (PlayerData): The player to set the tag for
- `tag` (string): The tag text to set
- `tagIndex` (int): The index where to set the tag (0 = closest to name)

**Returns:**
- `bool`: True if successful, false if player or tag is null/empty or index is negative

**Example:**
```csharp
// Replace specific tag
PlayerService.SetTagAtIndex(playerData, "ADMIN", 0);
PlayerService.SetTagAtIndex(playerData, "OWNER", 1);
```

---

#### RemoveNameTag (All Tags)
```csharp
public static void RemoveNameTag(PlayerData player)
```

Removes any existing tag from the player's name, leaving only the clean name.

**Parameters:**
- `player` (PlayerData): The player to remove the tag from

**Example:**
```csharp
// Player name: "ADM VIP DragonSlayer"
PlayerService.RemoveNameTag(playerData);
// Player name becomes: "DragonSlayer"
```

**Notes:**
- Removes ALL tags
- For removing specific tags, use `RemoveNameTag` with index or text parameter

---

#### RemoveNameTag (By Index)
```csharp
public static bool RemoveNameTag(PlayerData player, int tagIndex)
```

Removes a tag at a specific index. Tags at higher indices (left side) shift down to fill the gap.

**Parameters:**
- `player` (PlayerData): The player to remove the tag from
- `tagIndex` (int): The index of the tag to remove

**Returns:**
- `bool`: True if tag was removed, false if player is null or index is invalid

**Example:**
```csharp
// Player name: "ADM GOD VIP DragonSlayer"
PlayerService.RemoveNameTag(playerData, 1); // Remove "GOD"
// Player name becomes: "ADM VIP DragonSlayer"
```

---

#### RemoveNameTag (By Text)
```csharp
public static bool RemoveNameTag(PlayerData player, string tagText)
```

Removes the first occurrence of a tag that matches the specified text (case-insensitive).

**Parameters:**
- `player` (PlayerData): The player to remove the tag from
- `tagText` (string): The text of the tag to remove

**Returns:**
- `bool`: True if tag was found and removed, false otherwise

**Example:**
```csharp
// Player name: "ADM VIP MOD DragonSlayer"
PlayerService.RemoveNameTag(playerData, "VIP");
// Player name becomes: "ADM MOD DragonSlayer"
```

**Notes:**
- Case-insensitive search
- Only removes the first matching tag
- Searches from tag0 (rightmost) to higher indices (leftmost)

---

#### RemoveAllTags
```csharp
public static bool RemoveAllTags(PlayerData player)
```

Removes all tags from a player, leaving only the clean name.

**Parameters:**
- `player` (PlayerData): The player to remove all tags from

**Returns:**
- `bool`: True if successful, false if player is null

**Example:**
```csharp
// Player name: "ADM GOD VIP MOD DragonSlayer"
PlayerService.RemoveAllTags(playerData);
// Player name becomes: "DragonSlayer"
```

---

#### ExtractTags
```csharp
public static List<string> ExtractTags(string fullName)
```

Extracts all tags from the full name as a list. Tags are indexed from right to left (tag0 is closest to the name).

**Parameters:**
- `fullName` (string): The full name with tags

**Returns:**
- `List<string>`: List of tags indexed from right to left (tag0 first)

**Example:**
```csharp
var fullName = "ADM GOD VIP DragonSlayer";
var tags = PlayerService.ExtractTags(fullName);
// tags[0] = "VIP"   (tag0, closest to name)
// tags[1] = "GOD"   (tag1)
// tags[2] = "ADM"   (tag2, furthest from name)
```

---

#### TryGetTag
```csharp
public static bool TryGetTag(PlayerData player, int tagIndex, out string tag)
```

Gets a specific tag by index from a player's name.

**Parameters:**
- `player` (PlayerData): The player to get the tag from
- `tagIndex` (int): The index of the tag (0 = closest to name)
- `tag` (out string): The tag value if found

**Returns:**
- `bool`: True if tag exists at that index, false otherwise

**Example:**
```csharp
// Player name: "ADM VIP DragonSlayer"
if (PlayerService.TryGetTag(playerData, 0, out var tag0)) {
    Log.Message($"Tag0: {tag0}"); // "VIP"
}

if (PlayerService.TryGetTag(playerData, 1, out var tag1)) {
    Log.Message($"Tag1: {tag1}"); // "ADM"
}
```

---

#### GetAllTags
```csharp
public static List<string> GetAllTags(PlayerData player)
```

Gets all tags from a player as a list ordered by index (tag0, tag1, tag2...).

**Parameters:**
- `player` (PlayerData): The player to get tags from

**Returns:**
- `List<string>`: List of tags ordered by index

**Example:**
```csharp
var tags = PlayerService.GetAllTags(playerData);

for (int i = 0; i < tags.Count; i++) {
    Log.Message($"Tag{i}: {tags[i]}");
}
```

---

### Role Management

#### EnsureOwnerRole
```csharp
public static void EnsureOwnerRole(PlayerData player)
```

Ensures that the player is assigned the Owner role if their platform ID is listed in the Owners setting.

**Parameters:**
- `player` (PlayerData): The player data to check and assign the Owner role to

**Example:**
```csharp
// Automatically called during player cache setup
// Can also be called manually to recheck owner status
PlayerService.EnsureOwnerRole(playerData);
```

**Notes:**
- Checks the "Owners" setting for comma-separated platform IDs
- Automatically assigns Owner role if player ID matches

---

## Complete Example: Player Management System

```csharp
using ScarletCore.Services;
using ScarletCore.Commanding;
using Unity.Collections;

public class PlayerManagementCommands {
    // Find and display player info
    [Command("playerinfo", description: "Get information about a player")]
    public static void PlayerInfoCommand(CommandContext ctx, string playerName) {
        if (!PlayerService.TryGetByName(playerName, out var player)) {
            MessageService.SendError(ctx.Player, $"Player '{playerName}' not found.");
            return;
        }
        
        MessageService.SendInfo(ctx.Player, $"=== Player Info: {player.Name} ===");
        MessageService.SendFormatted(ctx.Player, "Platform ID: {0}", player.PlatformId);
        MessageService.SendFormatted(ctx.Player, "Is Online: {0}", player.IsConnected);
        MessageService.SendFormatted(ctx.Player, "Is Admin: {0}", player.IsAdmin);
        MessageService.SendFormatted(ctx.Player, "Level: {0}", player.Level);
        
        var clan = ClanService.GetClanName(player);
        if (!string.IsNullOrEmpty(clan)) {
            MessageService.SendFormatted(ctx.Player, "Clan: {0}", clan);
        }
    }
    
    // List all online players
    [Command("online", description: "List all online players")]
    public static void OnlineCommand(CommandContext ctx) {
        var onlinePlayers = PlayerService.GetAllConnected();
        
        MessageService.SendInfo(ctx.Player, $"=== Online Players ({onlinePlayers.Count}) ===");
        
        foreach (var player in onlinePlayers) {
            var tags = PlayerService.GetAllTags(player);
            var tagStr = tags.Count > 0 ? $" [{string.Join(", ", tags)}]" : "";
            MessageService.Send(ctx.Player, $"{player.Name}{tagStr}");
        }
    }
    
    // Rename player
    [Command("rename", description: "Rename a player", adminOnly: true)]
    public static void RenameCommand(CommandContext ctx, string playerName, string newName) {
        if (!PlayerService.TryGetByName(playerName, out var player)) {
            MessageService.SendError(ctx.Player, $"Player '{playerName}' not found.");
            return;
        }
        
        var fixedName = new FixedString64Bytes(newName);
        PlayerService.RenamePlayer(player, fixedName);
        
        MessageService.SendSuccess(ctx.Player, $"Renamed {playerName} to {newName}");
        MessageService.Send(player, $"Your name has been changed to: {newName}");
    }
    
    // Tag management
    [Command("settag", description: "Set a tag for a player", adminOnly: true)]
    public static void SetTagCommand(CommandContext ctx, string playerName, string tag) {
        if (!PlayerService.TryGetByName(playerName, out var player)) {
            MessageService.SendError(ctx.Player, $"Player '{playerName}' not found.");
            return;
        }
        
        if (PlayerService.SetNameTag(player, tag)) {
            MessageService.SendSuccess(ctx.Player, $"Set tag '{tag}' for {playerName}");
            MessageService.SendInfo(player, $"You received the '{tag}' tag!");
        } else {
            MessageService.SendError(ctx.Player, "Failed to set tag.");
        }
    }
    
    [Command("removetag", description: "Remove a tag from a player", adminOnly: true)]
    public static void RemoveTagCommand(CommandContext ctx, string playerName, string tag = null) {
        if (!PlayerService.TryGetByName(playerName, out var player)) {
            MessageService.SendError(ctx.Player, $"Player '{playerName}' not found.");
            return;
        }
        
        if (string.IsNullOrEmpty(tag)) {
            // Remove all tags
            PlayerService.RemoveAllTags(player);
            MessageService.SendSuccess(ctx.Player, $"Removed all tags from {playerName}");
        } else {
            // Remove specific tag
            if (PlayerService.RemoveNameTag(player, tag)) {
                MessageService.SendSuccess(ctx.Player, $"Removed tag '{tag}' from {playerName}");
            } else {
                MessageService.SendError(ctx.Player, $"Tag '{tag}' not found on {playerName}");
            }
        }
    }
    
    [Command("listtags", description: "List all tags on a player")]
    public static void ListTagsCommand(CommandContext ctx, string playerName) {
        if (!PlayerService.TryGetByName(playerName, out var player)) {
            MessageService.SendError(ctx.Player, $"Player '{playerName}' not found.");
            return;
        }
        
        var tags = PlayerService.GetAllTags(player);
        
        if (tags.Count == 0) {
            MessageService.SendInfo(ctx.Player, $"{playerName} has no tags.");
            return;
        }
        
        MessageService.SendInfo(ctx.Player, $"=== Tags for {playerName} ===");
        for (int i = 0; i < tags.Count; i++) {
            MessageService.Send(ctx.Player, $"Tag{i}: {tags[i]}");
        }
    }
    
    // Admin utilities
    [Command("admins", description: "List all admins")]
    public static void AdminsCommand(CommandContext ctx) {
        var admins = PlayerService.GetAdmins();
        
        MessageService.SendInfo(ctx.Player, $"=== Server Admins ({admins.Count}) ===");
        
        foreach (var admin in admins) {
            var status = admin.IsConnected ? "Online" : "Offline";
            MessageService.Send(ctx.Player, $"{admin.Name} - {status}");
        }
    }
    
    // Player search utilities
    public static void FindPlayersByTag(string tag) {
        var playersWithTag = new List<PlayerData>();
        
        foreach (var player in PlayerService.AllPlayers) {
            var tags = PlayerService.GetAllTags(player);
            if (tags.Contains(tag, StringComparer.CurrentCultureIgnoreCase)) {
                playersWithTag.Add(player);
            }
        }
        
        return playersWithTag;
    }
    
    // Bulk operations
    public static void BroadcastToOnlinePlayers(string message) {
        var onlinePlayers = PlayerService.GetAllConnected();
        MessageService.SendToPlayers(onlinePlayers, message);
    }
    
    public static void GrantTagToAdmins(string tag) {
        var admins = PlayerService.GetAdmins();
        
        foreach (var admin in admins) {
            PlayerService.SetNameTag(admin, tag);
        }
    }
}
```

## Tag System Explanation

The tag system uses a right-to-left indexing approach:

```
Full Name: "ADM GOD VIP DragonSlayer"
           [2]  [1] [0]  [clean name]

tag0 = "VIP"  (closest to name, rightmost tag)
tag1 = "GOD"  (middle tag)
tag2 = "ADM"  (furthest from name, leftmost tag)
```

**Benefits of this approach:**
- tag0 is always the primary/most important tag
- Higher priority tags are closer to the name
- Adding new tags doesn't shift existing tag indices

## Best Practices

1. **Player Lookup**: Use the most specific method available:
   - `TryGetById` for platform IDs (most reliable)
   - `TryGetByNetworkId` for online players (fastest)
   - `TryGetByName` for user-facing features (most convenient)

2. **Tag Management**: 
   - Use tag0 for primary roles (VIP, ADMIN, etc.)
   - Use higher indices for secondary tags
   - Clean tags regularly to avoid visual clutter

3. **Performance**: 
   - Cache player data instead of repeated lookups
   - Use `PlayerNetworkIds` for online-only operations
   - Avoid iterating `AllPlayers` in hot paths

4. **Name Changes**: 
   - Always use `RenamePlayer` instead of direct entity modifications
   - Consider notifying other players of name changes
   - Update any custom player references after renaming

5. **Tag Display**: 
   - Keep tags short (3-4 characters ideal)
   - Use consistent tag formatting across your mod
   - Consider color coding tags for better visibility

## Caching System

The service maintains multiple indexes for optimal performance:

- **PlayerNames**: O(1) lookup by character name
- **PlayerIds**: O(1) lookup by platform ID (persistent)
- **PlayerNetworkIds**: O(1) lookup by network ID (session-only)
- **AllPlayers**: Complete list for iteration

All indexes are automatically maintained during player lifecycle events.

## Related Services
- [RoleService](RoleService.md) - For player role management
- [MessageService](MessageService.md) - For sending messages to players
- [ClanService](ClanService.md) - For clan member management
- [AdminService](AdminService.md) - For admin-specific operations

## Notes
- Player data persists between sessions
- Network IDs change on each connection
- Name changes trigger cache updates automatically
- The service handles unnamed players gracefully
- Tag management is purely cosmetic and doesn't affect game mechanics
