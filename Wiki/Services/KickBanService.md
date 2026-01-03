# KickBanService Documentation

## Overview

`KickBanService` is a utility service for managing player moderation on a V Rising server. It provides methods for kicking players from the server and managing the ban list. All operations are persistent and survive server restarts.

## Table of Contents

- [Ban Operations](#ban-operations)
- [Unban Operations](#unban-operations)
- [Ban Status Checks](#ban-status-checks)
- [Kick Operations](#kick-operations)
- [Method Overloads](#method-overloads)
- [Examples](#examples)
- [Best Practices](#best-practices)

---

## Ban Operations

### Ban (by PlatformId)

```csharp
public static void Ban(ulong platformId)
```

Adds a player to the ban list and immediately kicks them if they're currently online.

**Parameters:**
- `platformId` - The unique platform ID of the player

**Behavior:**
- If player is online, creates a `BanEvent` to immediately disconnect them
- Adds the platform ID to the persistent ban list
- Saves changes to disk
- Refreshes the ban list

**Example:**
```csharp
ulong steamId = 76561198012345678;
KickBanService.Ban(steamId);
Log.Info($"Player {steamId} has been banned");
```

### Ban (by PlayerData)

```csharp
public static void Ban(PlayerData playerData)
```

Adds a player to the ban list using their PlayerData object.

**Parameters:**
- `playerData` - The PlayerData object containing the platform ID

**Example:**
```csharp
if (PlayerService.TryGetById(platformId, out var player)) {
    KickBanService.Ban(player);
    Log.Info($"Banned {player.Name}");
}
```

### Ban (by Name)

```csharp
public static void Ban(string playerName)
```

Adds a player to the ban list by their character name.

**Parameters:**
- `playerName` - The character name of the player to ban

**Behavior:**
- Looks up the player by name using `PlayerService`
- If found, bans them by platform ID
- If not found, logs a warning

**Example:**
```csharp
KickBanService.Ban("PlayerName123");
```

---

## Unban Operations

### Unban (by PlatformId)

```csharp
public static void Unban(ulong platformId)
```

Removes a player from the ban list, allowing them to rejoin the server.

**Parameters:**
- `platformId` - The unique platform ID of the player

**Behavior:**
- Removes the platform ID from the ban list
- Saves changes to disk
- Refreshes the ban list

**Example:**
```csharp
ulong steamId = 76561198012345678;
KickBanService.Unban(steamId);
Log.Info($"Player {steamId} has been unbanned");
```

### Unban (by PlayerData)

```csharp
public static void Unban(PlayerData playerData)
```

Removes a player from the ban list using their PlayerData object.

**Parameters:**
- `playerData` - The PlayerData object containing the platform ID

**Example:**
```csharp
if (PlayerService.TryGetById(platformId, out var player)) {
    KickBanService.Unban(player);
    Log.Info($"Unbanned {player.Name}");
}
```

### Unban (by Name)

```csharp
public static void Unban(string playerName)
```

Removes a player from the ban list by their character name.

**Parameters:**
- `playerName` - The character name of the player to unban

**Behavior:**
- Looks up the player by name using `PlayerService`
- If found, unbans them by platform ID
- If not found, logs a warning

**Example:**
```csharp
KickBanService.Unban("PlayerName123");
```

---

## Ban Status Checks

### IsBanned (by PlatformId)

```csharp
public static bool IsBanned(ulong platformId)
```

Checks if a player is currently banned.

**Parameters:**
- `platformId` - The unique platform ID of the player

**Returns:** `true` if the player is banned, `false` otherwise

**Example:**
```csharp
ulong steamId = 76561198012345678;
if (KickBanService.IsBanned(steamId)) {
    Log.Info("This player is banned");
} else {
    Log.Info("This player is not banned");
}
```

### IsBanned (by PlayerData)

```csharp
public static bool IsBanned(PlayerData playerData)
```

Checks if a player is currently banned using their PlayerData object.

**Parameters:**
- `playerData` - The PlayerData object containing the platform ID

**Returns:** `true` if the player is banned, `false` otherwise

**Example:**
```csharp
if (PlayerService.TryGetById(platformId, out var player)) {
    if (KickBanService.IsBanned(player)) {
        Log.Info($"{player.Name} is banned");
    }
}
```

### IsBanned (by Name)

```csharp
public static bool IsBanned(string playerName)
```

Checks if a player is currently banned by their character name.

**Parameters:**
- `playerName` - The character name of the player to check

**Returns:** `true` if the player is banned, `false` if not banned or not found

**Behavior:**
- Looks up the player by name using `PlayerService`
- If found, checks ban status by platform ID
- If not found, logs a warning and returns `false`

**Example:**
```csharp
if (KickBanService.IsBanned("PlayerName123")) {
    Log.Info("PlayerName123 is banned");
}
```

---

## Kick Operations

### Kick (by PlatformId)

```csharp
public static void Kick(ulong platformId)
```

Kicks a player from the server without banning them. The player can reconnect immediately.

**Parameters:**
- `platformId` - The unique platform ID of the player

**Behavior:**
- Creates a `KickEvent` entity
- Sends the kick event to the target player
- Does NOT add the player to the ban list

**Example:**
```csharp
ulong steamId = 76561198012345678;
KickBanService.Kick(steamId);
Log.Info($"Player {steamId} has been kicked");
```

### Kick (by PlayerData)

```csharp
public static void Kick(PlayerData playerData)
```

Kicks a player from the server using their PlayerData object.

**Parameters:**
- `playerData` - The PlayerData object containing the platform ID

**Example:**
```csharp
if (PlayerService.TryGetById(platformId, out var player)) {
    KickBanService.Kick(player);
    Log.Info($"Kicked {player.Name}");
}
```

### Kick (by Name)

```csharp
public static void Kick(string playerName)
```

Kicks a player from the server by their character name.

**Parameters:**
- `playerName` - The character name of the player to kick

**Behavior:**
- Looks up the player by name using `PlayerService`
- If found, kicks them by platform ID
- If not found, logs a warning

**Example:**
```csharp
KickBanService.Kick("PlayerName123");
```

---

## Method Overloads

All major operations support three input types for convenience:

| Method | By PlatformId | By PlayerData | By Name |
|--------|---------------|---------------|---------|
| `Ban` | ✅ | ✅ | ✅ |
| `Unban` | ✅ | ✅ | ✅ |
| `IsBanned` | ✅ | ✅ | ✅ |
| `Kick` | ✅ | ✅ | ✅ |

**Choose the appropriate overload based on what data you have available:**

- **PlatformId** - Most direct, no lookup required
- **PlayerData** - When you already have the player object
- **Name** - Most user-friendly, but requires player lookup

---

## Examples

### Example 1: Simple Ban Command

```csharp
public class BanCommand {
    public static void Execute(string playerName, string reason) {
        if (string.IsNullOrEmpty(playerName)) {
            Log.Error("Player name is required");
            return;
        }
        
        // Check if player exists
        if (!PlayerService.TryGetByName(playerName, out var player)) {
            Log.Error($"Player '{playerName}' not found");
            return;
        }
        
        // Check if already banned
        if (KickBanService.IsBanned(player)) {
            Log.Info($"{playerName} is already banned");
            return;
        }
        
        // Ban the player
        KickBanService.Ban(player);
        
        // Log the action
        Log.Info($"Banned player: {playerName} | Reason: {reason}");
        
        // Notify other admins
        NotifyAdmins($"{playerName} has been banned. Reason: {reason}");
    }
}
```

### Example 2: Temporary Ban System

```csharp
public class TempBanSystem {
    private static Dictionary<ulong, DateTime> _tempBans = new();
    
    public static void TempBan(string playerName, int durationMinutes) {
        if (!PlayerService.TryGetByName(playerName, out var player)) {
            Log.Error($"Player '{playerName}' not found");
            return;
        }
        
        // Ban the player
        KickBanService.Ban(player);
        
        // Store unban time
        var unbanTime = DateTime.Now.AddMinutes(durationMinutes);
        _tempBans[player.PlatformId] = unbanTime;
        
        Log.Info($"Temp banned {playerName} for {durationMinutes} minutes");
    }
    
    public static void CheckTempBans() {
        var now = DateTime.Now;
        var toUnban = new List<ulong>();
        
        foreach (var kvp in _tempBans) {
            if (now >= kvp.Value) {
                toUnban.Add(kvp.Key);
            }
        }
        
        foreach (var platformId in toUnban) {
            KickBanService.Unban(platformId);
            _tempBans.Remove(platformId);
            Log.Info($"Temp ban expired for {platformId}");
        }
    }
}
```

### Example 3: Ban List Management

```csharp
public class BanListManager {
    public static void ListBannedPlayers() {
        var allPlayers = PlayerService.GetAll();
        var bannedPlayers = new List<string>();
        
        foreach (var player in allPlayers) {
            if (KickBanService.IsBanned(player)) {
                bannedPlayers.Add($"{player.Name} ({player.PlatformId})");
            }
        }
        
        if (bannedPlayers.Count == 0) {
            Log.Info("No players are currently banned");
        } else {
            Log.Info($"Banned players ({bannedPlayers.Count}):");
            foreach (var name in bannedPlayers) {
                Log.Info($"  - {name}");
            }
        }
    }
    
    public static void UnbanAll() {
        var allPlayers = PlayerService.GetAll();
        int unbanned = 0;
        
        foreach (var player in allPlayers) {
            if (KickBanService.IsBanned(player)) {
                KickBanService.Unban(player);
                unbanned++;
            }
        }
        
        Log.Info($"Unbanned {unbanned} players");
    }
}
```

### Example 4: Auto-Kick on Rule Violation

```csharp
public class AutoModeration {
    private static Dictionary<ulong, int> _violations = new();
    private const int MAX_VIOLATIONS = 3;
    
    public static void ReportViolation(PlayerData player, string reason) {
        if (!_violations.ContainsKey(player.PlatformId)) {
            _violations[player.PlatformId] = 0;
        }
        
        _violations[player.PlatformId]++;
        int count = _violations[player.PlatformId];
        
        Log.Warning($"{player.Name} violated rule: {reason} ({count}/{MAX_VIOLATIONS})");
        
        if (count >= MAX_VIOLATIONS) {
            // Permanent ban after max violations
            KickBanService.Ban(player);
            Log.Info($"{player.Name} has been banned after {MAX_VIOLATIONS} violations");
            
            // Clear violation count
            _violations.Remove(player.PlatformId);
        } else {
            // Just kick for now
            KickBanService.Kick(player);
            Log.Info($"{player.Name} has been kicked. Violations: {count}/{MAX_VIOLATIONS}");
        }
    }
    
    public static void ClearViolations(string playerName) {
        if (PlayerService.TryGetByName(playerName, out var player)) {
            if (_violations.Remove(player.PlatformId)) {
                Log.Info($"Cleared violations for {playerName}");
            } else {
                Log.Info($"{playerName} has no violations");
            }
        }
    }
}
```

### Example 5: Ban with Discord Integration

```csharp
public class DiscordModeration {
    public static void BanPlayerWithNotification(string playerName, string reason, string adminName) {
        if (!PlayerService.TryGetByName(playerName, out var player)) {
            Log.Error($"Player '{playerName}' not found");
            return;
        }
        
        // Check if already banned
        if (KickBanService.IsBanned(player)) {
            Log.Info($"{playerName} is already banned");
            return;
        }
        
        // Ban the player
        KickBanService.Ban(player);
        
        // Create ban record
        var banRecord = new BanRecord {
            PlayerName = player.Name,
            PlatformId = player.PlatformId,
            Reason = reason,
            AdminName = adminName,
            Timestamp = DateTime.Now
        };
        
        // Save to database
        SaveBanRecord(banRecord);
        
        // Send Discord notification
        SendDiscordWebhook(new {
            content = $"🔨 **Player Banned**\n" +
                     $"**Player:** {player.Name}\n" +
                     $"**Steam ID:** {player.PlatformId}\n" +
                     $"**Reason:** {reason}\n" +
                     $"**Banned by:** {adminName}\n" +
                     $"**Time:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}"
        });
        
        Log.Info($"Banned {playerName} and sent Discord notification");
    }
    
    public static void UnbanWithNotification(string playerName, string adminName) {
        if (!PlayerService.TryGetByName(playerName, out var player)) {
            Log.Error($"Player '{playerName}' not found");
            return;
        }
        
        if (!KickBanService.IsBanned(player)) {
            Log.Info($"{playerName} is not banned");
            return;
        }
        
        // Unban the player
        KickBanService.Unban(player);
        
        // Send Discord notification
        SendDiscordWebhook(new {
            content = $"✅ **Player Unbanned**\n" +
                     $"**Player:** {player.Name}\n" +
                     $"**Steam ID:** {player.PlatformId}\n" +
                     $"**Unbanned by:** {adminName}\n" +
                     $"**Time:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}"
        });
        
        Log.Info($"Unbanned {playerName} and sent Discord notification");
    }
    
    private static void SaveBanRecord(BanRecord record) { /* Implementation */ }
    private static void SendDiscordWebhook(object payload) { /* Implementation */ }
}

public class BanRecord {
    public string PlayerName { get; set; }
    public ulong PlatformId { get; set; }
    public string Reason { get; set; }
    public string AdminName { get; set; }
    public DateTime Timestamp { get; set; }
}
```

---

## Best Practices

### 1. Always Validate Player Existence

```csharp
// Good
if (PlayerService.TryGetByName(playerName, out var player)) {
    KickBanService.Ban(player);
} else {
    Log.Error($"Player '{playerName}' not found");
}

// Bad - may log warnings unnecessarily
KickBanService.Ban(playerName); // Will log warning if not found
```

### 2. Check Ban Status Before Banning

```csharp
// Good - avoid redundant operations
if (!KickBanService.IsBanned(player)) {
    KickBanService.Ban(player);
    Log.Info($"Banned {player.Name}");
} else {
    Log.Info($"{player.Name} is already banned");
}

// Less efficient - redundant ban operation
KickBanService.Ban(player);
```

### 3. Use PlatformId When Available

```csharp
// Most efficient - direct access
ulong platformId = 76561198012345678;
KickBanService.Ban(platformId);

// Less efficient - requires lookup
KickBanService.Ban("PlayerName123");
```

### 4. Log Moderation Actions

```csharp
// Good - always log moderation actions
KickBanService.Ban(player);
Log.Info($"[MODERATION] {adminName} banned {player.Name} | Reason: {reason}");

// Consider logging to a separate moderation log file
ModerationLogger.LogBan(adminName, player.Name, reason);
```

### 5. Handle Kick vs Ban Appropriately

```csharp
// Use Kick for temporary removal (minor violations)
if (minorViolation) {
    KickBanService.Kick(player);
    Log.Info($"Kicked {player.Name} for {reason}");
}

// Use Ban for serious violations or repeat offenders
if (seriousViolation || repeatOffender) {
    KickBanService.Ban(player);
    Log.Info($"Banned {player.Name} for {reason}");
}
```

### 6. Provide Feedback to Moderators

```csharp
public static void BanWithFeedback(string playerName, string reason) {
    if (!PlayerService.TryGetByName(playerName, out var player)) {
        return "❌ Player not found";
    }
    
    if (KickBanService.IsBanned(player)) {
        return $"ℹ️ {playerName} is already banned";
    }
    
    KickBanService.Ban(player);
    return $"✅ Successfully banned {playerName}";
}
```

### 7. Consider Implementing Ban Reasons

```csharp
// Store ban reasons in a separate data structure
private static Dictionary<ulong, BanInfo> _banReasons = new();

public class BanInfo {
    public string Reason { get; set; }
    public string AdminName { get; set; }
    public DateTime BanDate { get; set; }
}

public static void BanWithReason(PlayerData player, string reason, string adminName) {
    KickBanService.Ban(player);
    
    _banReasons[player.PlatformId] = new BanInfo {
        Reason = reason,
        AdminName = adminName,
        BanDate = DateTime.Now
    };
    
    // Save to persistent storage
    SaveBanReasons();
}
```

---

## Key Differences: Kick vs Ban

| Feature | Kick | Ban |
|---------|------|-----|
| **Immediate Effect** | Disconnects player | Disconnects player |
| **Can Reconnect** | ✅ Yes, immediately | ❌ No |
| **Persistent** | ❌ No | ✅ Yes (saved to disk) |
| **Use Case** | Minor violations, temporary removal | Serious violations, permanent removal |

**Example Decision Flow:**
```csharp
if (violation.Severity == Severity.Low) {
    KickBanService.Kick(player); // They can come back
} else if (violation.Severity == Severity.High) {
    KickBanService.Ban(player);  // They cannot come back
}
```

---

## Important Notes

### Persistence
- **Ban operations** are persistent and survive server restarts
- Ban list is automatically saved to disk
- **Kick operations** are temporary - no record is kept

### Platform ID
- Platform ID is typically a Steam ID (for Steam users)
- Platform IDs are unique and permanent identifiers
- Always prefer Platform ID over player names when possible (names can change)

### Player Lookup Warnings
- Methods that take player names will log warnings if the player is not found
- This is normal behavior and helps with debugging
- Consider handling these cases explicitly in your code

### Online vs Offline Players
- **Ban** works on both online and offline players
  - Online players are immediately kicked
  - Offline players are prevented from joining
- **Kick** only works on online players
  - Has no effect if player is offline

---

## Thread Safety

⚠️ **Warning:** This service is **NOT thread-safe**. All methods must be called from the main thread as they interact with Unity's EntityManager and game systems.

---

## Requirements

- Unity ECS (Entities package)
- ProjectM.Network namespace (game-specific)
- `PlayerService` - for player lookups by name
- `GameSystems` - for accessing KickBanSystem and EntityManager
- `KickBanSystem` - the underlying game system that manages bans

---

## License

Part of the ScarletCore framework.