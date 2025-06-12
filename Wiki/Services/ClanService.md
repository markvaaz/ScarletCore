# ClanService

ClanService provides functionality for managing clan operations in V Rising, including member management, role assignment, and clan information retrieval.

## Overview

```csharp
using ScarletCore.Services;
using ProjectM;

// Get all members of a clan
var members = ClanService.GetMembers("ClanName");

// Add a player to a clan
ClanService.TryAddMember(playerData, "ClanName");
```

## Features

- Clan member management (add/remove)
- Clan role assignment and modification
- Clan information retrieval
- Clan leader identification
- Clan listing and enumeration
- Clan renaming functionality
- Built-in error handling and validation

## Methods

### GetMembers
Retrieves all members of a specified clan.

```csharp
var members = ClanService.GetMembers("MyClan");
foreach (var member in members) {
  Log.Info($"Member: {member.Name}");
}
```

**Parameters:**
- `clanName` - The name of the clan to get members from (case-insensitive)

**Returns:** A list of PlayerData objects representing all clan members

### TryAddMember
Attempts to add a player to a specified clan.

```csharp
if (PlayerService.TryGetByName("PlayerName", out PlayerData player)) {
  bool success = ClanService.TryAddMember(player, "TargetClan");
  if (success) {
    Log.Info("Player added to clan successfully");
  }
}
```

**Parameters:**
- `playerData` - The player data of the player to add
- `clanName` - The name of the clan to add the player to

**Returns:** True if the player was successfully added, false otherwise

**Behavior:**
- Checks if player is already in a clan
- Verifies target clan exists
- Sets new member role to Member
- Currently has a bug and returns false even on success

### TryRemoveFromClan
Attempts to remove a player from their current clan.

```csharp
if (PlayerService.TryGetByName("PlayerName", out PlayerData player)) {
  bool removed = ClanService.TryRemoveFromClan(player);
  if (removed) {
    Log.Info("Player removed from clan");
  }
}
```

**Parameters:**
- `playerData` - The player data of the player to remove

**Returns:** True if the player was successfully removed, false otherwise

**Behavior:**
- Only clan leaders can remove members
- Clan leaders cannot be removed
- Creates kick request through game's event system

### TryGetClanLeader
Attempts to find and return the leader of a specified clan.

```csharp
if (ClanService.TryGetClanLeader("ClanName", out PlayerData leader)) {
  Log.Info($"Clan leader: {leader.Name}");
}
```

**Parameters:**
- `clanName` - The name of the clan to find the leader for (case-insensitive)
- `clanLeader` - Output parameter containing the clan leader's PlayerData if found

**Returns:** True if a clan leader was found, false otherwise

### ListClans
Retrieves a dictionary of all clans with their members.

```csharp
var allClans = ClanService.ListClans();
foreach (var clan in allClans) {
  Log.Info($"Clan: {clan.Key}, Members: {clan.Value.Count}");
}
```

**Returns:** A dictionary where keys are clan names and values are lists of PlayerData for each clan

### TryChangeClanRole
Attempts to change a player's role within their clan.

```csharp
if (PlayerService.TryGetByName("PlayerName", out PlayerData player)) {
  bool changed = ClanService.TryChangeClanRole(player, ClanRoleEnum.Officer);
  if (changed) {
    Log.Info("Player role changed to Officer");
  }
}
```

**Parameters:**
- `playerData` - The player whose role should be changed
- `newRole` - The new clan role to assign

**Returns:** True if the role was successfully changed, false if the player is not in a clan

### RenameClan
Renames an existing clan and updates all member references.

```csharp
ClanService.RenameClan("OldClanName", "NewClanName");
```

**Parameters:**
- `oldClanName` - The current name of the clan
- `newClanName` - The new name for the clan

**Behavior:**
- Updates clan name in ClanTeam component
- Updates smart clan name for all members
- Logs error if clan doesn't exist

### TryGetClanEntity
Attempts to find a clan entity by name.

```csharp
if (ClanService.TryGetClanEntity("ClanName", out Entity clanEntity)) {
  // Work with clan entity directly
}
```

**Parameters:**
- `clanName` - The name of the clan to find (case-insensitive)
- `clanEntity` - Output parameter containing the clan entity if found

**Returns:** True if the clan was found, false otherwise

**Warning:** This method has a bug in the comparison logic

### GetClanEntities
Creates a native array of all clan entities in the game.

```csharp
var clanEntities = ClanService.GetClanEntities();
// Use clan entities...
clanEntities.Dispose(); // Important: dispose to prevent memory leaks
```

**Returns:** A NativeArray containing all clan entities with ClanTeam component

**Important:** The caller must dispose the returned array to prevent memory leaks

### GetClanTeams
Retrieves a list of all clan team components from active clans.

```csharp
var clanTeams = ClanService.GetClanTeams();
foreach (var team in clanTeams) {
  Log.Info($"Clan: {team.Name}");
}
```

**Returns:** A list of ClanTeam components containing clan information

### GetClanName
Gets the clan name for a specific player.

```csharp
if (PlayerService.TryGetByName("PlayerName", out PlayerData player)) {
  string clanName = ClanService.GetClanName(player);
  if (!string.IsNullOrEmpty(clanName)) {
    Log.Info($"Player is in clan: {clanName}");
  }
}
```

**Parameters:**
- `playerData` - The player data to get clan name for

**Returns:** The clan name as string, or empty string if player is not in a clan

## Usage Examples

### Basic Clan Management
```csharp
using ScarletCore.Services;
using ProjectM;

// Get clan members
var members = ClanService.GetMembers("MyGuild");
Log.Info($"Clan has {members.Count} members");

// Add a player to clan
if (PlayerService.TryGetByName("NewMember", out PlayerData player)) {
  if (ClanService.TryAddMember(player, "MyGuild")) {
    Log.Info("Player added to clan");
  }
}

// Remove a player from their clan
if (ClanService.TryRemoveFromClan(player)) {
  Log.Info("Player removed from clan");
}
```

### Clan Information Retrieval
```csharp
// Get clan leader
if (ClanService.TryGetClanLeader("MyGuild", out PlayerData leader)) {
  Log.Info($"Clan leader: {leader.Name}");
}

// List all clans
var allClans = ClanService.ListClans();
foreach (var clan in allClans) {
  Log.Info($"Clan: {clan.Key} ({clan.Value.Count} members)");
}
```

### Role Management
```csharp
// Change player role
if (ClanService.TryChangeClanRole(player, ClanRoleEnum.Officer)) {
  Log.Info("Player promoted to Officer");
}

// Rename a clan
ClanService.RenameClan("OldName", "NewName");
Log.Info("Clan renamed successfully");
```

## Clan Roles

The available clan roles are defined in `ClanRoleEnum`:

- `Leader` - Clan leader with full permissions
- `Officer` - Officer with elevated permissions  
- `Member` - Regular clan member

## Important Notes

- **Memory management** - Always dispose NativeArray returned by `GetClanEntities()`
- **Case sensitivity** - Clan name comparisons are case-insensitive
- **Leader restrictions** - Clan leaders cannot be removed from clans
