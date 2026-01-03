# MessageService

The `MessageService` provides comprehensive functionality for sending messages to players in V Rising. It supports system messages, formatted text, colored messages, localized content, broadcasts, and scrolling combat text (SCT).

## Namespace
```csharp
using ScarletCore.Services;
```

## Overview

The `MessageService` is a static utility class that provides methods for:
- Sending system messages to individual players or users
- Broadcasting messages to all players or specific groups
- Sending colored messages (error, success, warning, info)
- Creating announcements and notifications
- Sending localized messages
- Displaying scrolling combat text (SCT)
- Conditional and formatted messaging

## Table of Contents

- [Basic Messaging](#basic-messaging)
  - [Send (User)](#send-user)
  - [Send (PlayerData)](#send-playerdata)
  - [SendRaw](#sendraw)
  - [SendAll](#sendall)
  - [SendAdmins](#sendadmins)
- [Colored Messages](#colored-messages)
  - [SendError](#senderror-user)
  - [SendSuccess](#sendsuccess-user)
  - [SendWarning](#sendwarning-user)
  - [SendInfo](#sendinfo-user)
- [Broadcast Methods](#broadcast-methods)
  - [Announce](#announce)
  - [BroadcastError](#broadcasterror)
  - [BroadcastWarning](#broadcastwarning)
  - [BroadcastInfo](#broadcastinfo)
- [Template Messages](#template-messages)
  - [SendWelcome](#sendwelcome)
  - [NotifyPlayerJoined](#notifyplayerjoined)
  - [NotifyPlayerLeft](#notifyplayerleft)
  - [NotifyDeath](#notifydeath)
  - [NotifyRestart](#notifyrestart)
- [Scrolling Combat Text](#scrolling-combat-text-sct)
  - [SendSCT](#sendsct)
- [Formatted & Utility Methods](#formatted--utility-methods)
  - [SendFormatted](#sendformatted-user)
  - [SendToPlayers](#sendtoplayers)
  - [SendToPlayersColored](#sendtoplayerscolored)
  - [SendToPlayersInRadius](#sendtoplayersinradius)
  - [SendToPlayersInRadiusColored](#sendtoplayersinradiuscolored)
  - [SendList](#sendlist-user)
- [Localization Methods](#localization-methods)
  - [SendLocalized](#sendlocalized)
  - [SendAllLocalized](#sendalllocalized)
- [Conditional Messaging](#conditional-messaging)
  - [SendConditional](#sendconditional-user-simple)
- [Complete Example](#complete-example-comprehensive-messaging-system)
- [Available Colors](#available-colors-richtextformatter)
- [Best Practices](#best-practices)
- [Related Services](#related-services)

## Methods

### Basic Messaging

#### Send (User)
```csharp
public static void Send(User user, string message)
```

Sends a formatted system message to a specific user.

**Parameters:**
- `user` (User): The user to send the message to
- `message` (string): The message text to send

**Example:**
```csharp
var user = playerData.UserEntity.Read<User>();
MessageService.Send(user, "Welcome to the server!");
```

---

#### Send (PlayerData)
```csharp
public static void Send(PlayerData player, string message)
```

Sends a formatted system message to a specific player.

**Parameters:**
- `player` (PlayerData): The player to send the message to
- `message` (string): The message text to send

**Example:**
```csharp
MessageService.Send(playerData, "Your health has been restored!");
```

---

#### SendRaw
```csharp
public static void SendRaw(PlayerData player, string message)
```

Sends a raw (unformatted) system message to a specific player.

**Parameters:**
- `player` (PlayerData): The player to send the message to
- `message` (string): The raw message text to send (no formatting applied)

**Example:**
```csharp
MessageService.SendRaw(playerData, "<color=red>Raw HTML-like message</color>");
```

**Notes:**
- Message is sent without formatting processing
- Useful when you need exact control over message appearance

---

#### SendAll
```csharp
public static void SendAll(string message)
```

Sends a formatted system message to all connected players.

**Parameters:**
- `message` (string): The message text to send to all players

**Example:**
```csharp
MessageService.SendAll("Server will restart in 5 minutes!");
```

---

#### SendAdmins
```csharp
public static void SendAdmins(string message)
```

Sends a formatted system message to all administrators.

**Parameters:**
- `message` (string): The message text to send to all admins

**Example:**
```csharp
MessageService.SendAdmins("Player reported suspicious activity.");
```

---

### Colored Messages

#### SendError (User)
```csharp
public static void SendError(User user, string message)
```

Sends an error message with red color.

**Parameters:**
- `user` (User): The user to send the error to
- `message` (string): The error message text

**Example:**
```csharp
var user = playerData.UserEntity.Read<User>();
MessageService.SendError(user, "Invalid command syntax!");
```

---

#### SendError (PlayerData)
```csharp
public static void SendError(PlayerData player, string message)
```

Sends an error message to a player with red color.

**Parameters:**
- `player` (PlayerData): The player to send the error to
- `message` (string): The error message text

**Example:**
```csharp
MessageService.SendError(playerData, "You don't have permission to do that!");
```

---

#### SendSuccess (User)
```csharp
public static void SendSuccess(User user, string message)
```

Sends a success message with green color.

**Parameters:**
- `user` (User): The user to send the success message to
- `message` (string): The success message text

**Example:**
```csharp
var user = playerData.UserEntity.Read<User>();
MessageService.SendSuccess(user, "Item crafted successfully!");
```

---

#### SendSuccess (PlayerData)
```csharp
public static void SendSuccess(PlayerData player, string message)
```

Sends a success message to a player with green color.

**Parameters:**
- `player` (PlayerData): The player to send the success message to
- `message` (string): The success message text

**Example:**
```csharp
MessageService.SendSuccess(playerData, "Teleportation successful!");
```

---

#### SendWarning (User)
```csharp
public static void SendWarning(User user, string message)
```

Sends a warning message with yellow color.

**Parameters:**
- `user` (User): The user to send the warning to
- `message` (string): The warning message text

**Example:**
```csharp
var user = playerData.UserEntity.Read<User>();
MessageService.SendWarning(user, "Your inventory is almost full!");
```

---

#### SendWarning (PlayerData)
```csharp
public static void SendWarning(PlayerData player, string message)
```

Sends a warning message to a player with yellow color.

**Parameters:**
- `player` (PlayerData): The player to send the warning to
- `message` (string): The warning message text

**Example:**
```csharp
MessageService.SendWarning(playerData, "Low health! Find shelter!");
```

---

#### SendInfo (User)
```csharp
public static void SendInfo(User user, string message)
```

Sends an info message with blue color.

**Parameters:**
- `user` (User): The user to send the info to
- `message` (string): The info message text

**Example:**
```csharp
var user = playerData.UserEntity.Read<User>();
MessageService.SendInfo(user, "Server population: 25/50");
```

---

#### SendInfo (PlayerData)
```csharp
public static void SendInfo(PlayerData player, string message)
```

Sends an info message to a player with blue color.

**Parameters:**
- `player` (PlayerData): The player to send the info to
- `message` (string): The info message text

**Example:**
```csharp
MessageService.SendInfo(playerData, "New quest available in the village!");
```

---

### Broadcast Methods

#### Announce
```csharp
public static void Announce(string message)
```

Sends an announcement to all players with orange color and prefix.

**Parameters:**
- `message` (string): The announcement text

**Example:**
```csharp
MessageService.Announce("Blood Moon event starting in 10 minutes!");
```

---

#### BroadcastError
```csharp
public static void BroadcastError(string message)
```

Sends a global error message to all players.

**Parameters:**
- `message` (string): The error message text

**Example:**
```csharp
MessageService.BroadcastError("Server encountered an error. Please report if issues persist.");
```

---

#### BroadcastWarning
```csharp
public static void BroadcastWarning(string message)
```

Sends a global warning to all players.

**Parameters:**
- `message` (string): The warning message text

**Example:**
```csharp
MessageService.BroadcastWarning("Server restart in 10 minutes!");
```

---

#### BroadcastInfo
```csharp
public static void BroadcastInfo(string message)
```

Sends a global info message to all players.

**Parameters:**
- `message` (string): The info message text

**Example:**
```csharp
MessageService.BroadcastInfo("New server rules have been updated. Check /rules");
```

---

### Template Messages

#### SendWelcome
```csharp
public static void SendWelcome(PlayerData player)
```

Sends a welcome message sequence to a new player.

**Parameters:**
- `player` (PlayerData): The player to welcome

**Example:**
```csharp
MessageService.SendWelcome(playerData);
// Sends multiple messages:
// - Welcome message with player name
// - Help command information
// - Server rules reminder
```

---

#### NotifyPlayerJoined
```csharp
public static void NotifyPlayerJoined(PlayerData player)
```

Sends a player join notification to all players.

**Parameters:**
- `player` (PlayerData): The player who joined

**Example:**
```csharp
MessageService.NotifyPlayerJoined(playerData);
// All players see: "[PlayerName] joined the server"
```

---

#### NotifyPlayerLeft
```csharp
public static void NotifyPlayerLeft(PlayerData player)
```

Sends a player leave notification to all players.

**Parameters:**
- `player` (PlayerData): The player who left

**Example:**
```csharp
MessageService.NotifyPlayerLeft(playerData);
// All players see: "[PlayerName] left the server"
```

---

#### NotifyDeath
```csharp
public static void NotifyDeath(string victimName, string killerName = null)
```

Sends a death notification to all players.

**Parameters:**
- `victimName` (string): Name of the player who died
- `killerName` (string, optional): Name of the killer if applicable

**Example:**
```csharp
// Player died to environment/PvE
MessageService.NotifyDeath("DragonSlayer");

// Player killed by another player
MessageService.NotifyDeath("DragonSlayer", "VampireLord");
```

---

#### NotifyRestart
```csharp
public static void NotifyRestart(int minutesRemaining)
```

Sends a server restart warning to all players.

**Parameters:**
- `minutesRemaining` (int): Minutes until server restart

**Example:**
```csharp
// Send periodic warnings
MessageService.NotifyRestart(10); // "Server restart in 10 minutes!"
MessageService.NotifyRestart(5);  // "Server restart in 5 minutes!"
MessageService.NotifyRestart(1);  // "Server restart in 1 minute! Prepare for disconnection!"
```

---

### Scrolling Combat Text (SCT)

#### SendSCT
```csharp
public static void SendSCT(PlayerData player, PrefabGUID prefab, string assetGuid, float3 color, int value)
```

Sends a scrolling combat text (SCT) message to a player.

**Parameters:**
- `player` (PlayerData): The player to display the SCT to
- `prefab` (PrefabGUID): The prefab GUID for the SCT icon or effect
- `assetGuid` (string): The asset GUID string for the SCT
- `color` (float3): The RGB color of the SCT message
- `value` (int): The value to display in the SCT message

**Example:**
```csharp
using Unity.Mathematics;

// Display damage number
var redColor = new float3(1f, 0f, 0f);
MessageService.SendSCT(
    playerData,
    new PrefabGUID(0), // Use appropriate prefab
    "your-asset-guid",
    redColor,
    250 // Damage amount
);
```

---

### Formatted & Utility Methods

#### SendFormatted (User)
```csharp
public static void SendFormatted(User user, string template, params object[] args)
```

Sends a formatted message with placeholders.

**Parameters:**
- `user` (User): The user to send to
- `template` (string): Message template with placeholders
- `args` (params object[]): Values to insert into placeholders

**Example:**
```csharp
var user = playerData.UserEntity.Read<User>();
MessageService.SendFormatted(user, "You have {0} gold and {1} items", 1500, 25);
```

---

#### SendFormatted (PlayerData)
```csharp
public static void SendFormatted(PlayerData player, string template, params object[] args)
```

Sends a formatted message with placeholders to a player.

**Parameters:**
- `player` (PlayerData): The player to send to
- `template` (string): Message template with placeholders
- `args` (params object[]): Values to insert into placeholders

**Example:**
```csharp
MessageService.SendFormatted(playerData, "Level: {0} | XP: {1}/{2}", 25, 1500, 2000);
```

---

#### SendToPlayers
```csharp
public static void SendToPlayers(IEnumerable<PlayerData> players, string message)
```

Sends a message to multiple specific players.

**Parameters:**
- `players` (IEnumerable<PlayerData>): Collection of players to send to
- `message` (string): The message text

**Example:**
```csharp
var clanMembers = ClanService.GetMembers("DragonSlayers");
MessageService.SendToPlayers(clanMembers, "Clan meeting in 5 minutes!");
```

---

#### SendToPlayersColored
```csharp
public static void SendToPlayersColored(IEnumerable<PlayerData> players, string message, string color)
```

Sends a colored message to multiple specific players.

**Parameters:**
- `players` (IEnumerable<PlayerData>): Collection of players to send to
- `message` (string): The message text
- `color` (string): Color code for the message

**Example:**
```csharp
var admins = PlayerService.GetAdmins();
MessageService.SendToPlayersColored(admins, "Admin alert!", RichTextFormatter.Red);
```

---

#### SendToPlayersInRadius
```csharp
public static void SendToPlayersInRadius(float3 position, float radius, string message)
```

Sends a message to players within a certain range of a position.

**Parameters:**
- `position` (float3): Center position
- `radius` (float): Radius in units
- `message` (string): The message text

**Example:**
```csharp
using Unity.Mathematics;

var eventPosition = new float3(100f, 0f, 100f);
MessageService.SendToPlayersInRadius(eventPosition, 50f, "Event starting nearby!");
```

---

#### SendToPlayersInRadiusColored
```csharp
public static void SendToPlayersInRadiusColored(float3 position, float radius, string message, string color)
```

Sends a colored message to players within a certain range of a position.

**Parameters:**
- `position` (float3): Center position
- `radius` (float): Radius in units
- `message` (string): The message text
- `color` (string): Color code for the message

**Example:**
```csharp
var bossPosition = playerData.Position;
MessageService.SendToPlayersInRadiusColored(
    bossPosition, 
    100f, 
    "Boss battle initiated!", 
    RichTextFormatter.Red
);
```

---

#### SendList (User)
```csharp
public static void SendList(User user, string title, IEnumerable<string> items, string color = null)
```

Creates a formatted list message.

**Parameters:**
- `user` (User): The user to send to
- `title` (string): Title of the list
- `items` (IEnumerable<string>): List items
- `color` (string, optional): Color for the list

**Example:**
```csharp
var user = playerData.UserEntity.Read<User>();
var items = new[] { "Iron Sword", "Health Potion", "Gold Ore" };
MessageService.SendList(user, "Your Items", items, RichTextFormatter.Green);
```

---

#### SendList (PlayerData)
```csharp
public static void SendList(PlayerData player, string title, IEnumerable<string> items, string color = null)
```

Creates a formatted list message for a player.

**Parameters:**
- `player` (PlayerData): The player to send to
- `title` (string): Title of the list
- `items` (IEnumerable<string>): List items
- `color` (string, optional): Color for the list

**Example:**
```csharp
var quests = new[] { "Defeat 10 Skeletons", "Collect 5 Blood Roses", "Visit the Castle" };
MessageService.SendList(playerData, "Active Quests", quests);
```

---

### Localization Methods

#### SendLocalized
```csharp
public static void SendLocalized(PlayerData player, string localizationKey, params object[] parameters)
```

Sends a localized message to a player using the player's preferred language.

**Parameters:**
- `player` (PlayerData): The player to send to
- `localizationKey` (string): Localization key for translation
- `parameters` (params object[]): Parameters for string formatting

**Example:**
```csharp
// Assumes localization key exists in translation files
MessageService.SendLocalized(playerData, "welcome.message", playerData.Name);
MessageService.SendLocalized(playerData, "quest.complete", "Dragon Hunt", 500);
```

---

#### SendAllLocalized
```csharp
public static void SendAllLocalized(string localizationKey, params object[] parameters)
```

Sends a localized message to all players using each player's preferred language.

**Parameters:**
- `localizationKey` (string): Localization key for translation
- `parameters` (params object[]): Parameters for string formatting

**Example:**
```csharp
// Each player receives message in their language
MessageService.SendAllLocalized("server.restart.warning", 10);
MessageService.SendAllLocalized("event.started", "Blood Moon");
```

---

### Conditional Messaging

#### SendConditional (User, Simple)
```csharp
public static void SendConditional(User user, string message, Func<bool> condition)
```

Sends a message only if the condition is met.

**Parameters:**
- `user` (User): The user to send to
- `message` (string): The message text
- `condition` (Func<bool>): Condition function

**Example:**
```csharp
var user = playerData.UserEntity.Read<User>();
MessageService.SendConditional(
    user, 
    "VIP features unlocked!", 
    () => playerData.IsVIP
);
```

---

#### SendConditional (PlayerData, Simple)
```csharp
public static void SendConditional(PlayerData player, string message, Func<bool> condition)
```

Sends a message only if the condition is met.

**Parameters:**
- `player` (PlayerData): The player to send to
- `message` (string): The message text
- `condition` (Func<bool>): Condition function

**Example:**
```csharp
MessageService.SendConditional(
    playerData, 
    "You have new mail!", 
    () => HasUnreadMail(playerData)
);
```

---

#### SendConditional (User, True/False)
```csharp
public static void SendConditional(User user, string trueMessage, string falseMessage, Func<bool> condition)
```

Sends different messages based on a condition.

**Parameters:**
- `user` (User): The user to send to
- `trueMessage` (string): Message if condition is true
- `falseMessage` (string): Message if condition is false
- `condition` (Func<bool>): Condition function

**Example:**
```csharp
var user = playerData.UserEntity.Read<User>();
MessageService.SendConditional(
    user,
    "PvP enabled in this zone!",
    "You are in a safe zone.",
    () => IsInPvPZone(playerData)
);
```

---

#### SendConditional (PlayerData, True/False)
```csharp
public static void SendConditional(PlayerData player, string trueMessage, string falseMessage, Func<bool> condition)
```

Sends different messages based on a condition.

**Parameters:**
- `player` (PlayerData): The player to send to
- `trueMessage` (string): Message if condition is true
- `falseMessage` (string): Message if condition is false
- `condition` (Func<bool>): Condition function

**Example:**
```csharp
MessageService.SendConditional(
    playerData,
    "Buff active!",
    "Buff expired.",
    () => BuffService.HasBuff(playerData.CharacterEntity, buffGuid)
);
```

---

## Complete Example: Comprehensive Messaging System

```csharp
using ScarletCore.Services;
using ScarletCore.Commanding;
using Unity.Mathematics;

public class MessagingExamples {
    // Player join/leave handling
    public static void OnPlayerConnect(PlayerData player) {
        // Welcome the new player
        MessageService.SendWelcome(player);
        
        // Notify all other players
        MessageService.NotifyPlayerJoined(player);
        
        // Send admin notification
        MessageService.SendAdmins($"Player {player.Name} connected from {player.User.PlatformId}");
    }
    
    public static void OnPlayerDisconnect(PlayerData player) {
        MessageService.NotifyPlayerLeft(player);
    }
    
    // Command example with various message types
    [Command("status", description: "Check your status")]
    public static void StatusCommand(CommandContext ctx) {
        var player = ctx.Player;
        
        // Send formatted status
        MessageService.SendInfo(player, "=== Player Status ===");
        MessageService.SendFormatted(player, "Name: {0}", player.Name);
        MessageService.SendFormatted(player, "Level: {0}", player.Level);
        
        // Conditional messages
        MessageService.SendConditional(
            player,
            "You are in a clan!",
            "You are not in a clan.",
            () => !string.IsNullOrEmpty(ClanService.GetClanName(player))
        );
    }
    
    // Event announcements
    public static void AnnounceEvent(string eventName, float3 position) {
        // Global announcement
        MessageService.Announce($"{eventName} has started!");
        
        // Send colored message to nearby players
        MessageService.SendToPlayersInRadiusColored(
            position,
            100f,
            "Event is nearby! Head to the marker!",
            RichTextFormatter.Yellow
        );
    }
    
    // Admin tools
    [Command("broadcast", description: "Send announcement to all players", adminOnly: true)]
    public static void BroadcastCommand(CommandContext ctx, string message) {
        MessageService.Announce(message);
        MessageService.SendSuccess(ctx.Player, "Announcement sent!");
    }
    
    // Death handling
    public static void OnPlayerDeath(PlayerData victim, PlayerData killer = null) {
        if (killer != null) {
            MessageService.NotifyDeath(victim.Name, killer.Name);
            MessageService.SendSuccess(killer, $"You killed {victim.Name}!");
            MessageService.SendError(victim, $"You were killed by {killer.Name}!");
        } else {
            MessageService.NotifyDeath(victim.Name);
            MessageService.SendWarning(victim, "You died!");
        }
    }
    
    // Clan messaging
    public static void SendClanMessage(string clanName, string message) {
        var members = ClanService.GetMembers(clanName);
        MessageService.SendToPlayersColored(
            members, 
            $"[Clan] {message}", 
            RichTextFormatter.Green
        );
    }
    
    // List display example
    [Command("online", description: "List online players")]
    public static void OnlineCommand(CommandContext ctx) {
        var players = PlayerService.GetAllConnected();
        var playerNames = players.Select(p => p.Name);
        
        MessageService.SendList(
            ctx.Player, 
            "Online Players", 
            playerNames, 
            RichTextFormatter.Cyan
        );
    }
    
    // Server restart sequence
    public static void StartRestartSequence() {
        MessageService.NotifyRestart(10);
        ActionScheduler.Run(() => MessageService.NotifyRestart(5), 5000);
        ActionScheduler.Run(() => MessageService.NotifyRestart(1), 9000);
    }
    
    // Localized welcome
    public static void SendLocalizedWelcome(PlayerData player) {
        MessageService.SendLocalized(player, "welcome.title", player.Name);
        MessageService.SendLocalized(player, "welcome.help");
        MessageService.SendLocalized(player, "welcome.rules");
    }
    
    // Complex notification system
    public static void NotifyInventoryFull(PlayerData player, int itemsDropped) {
        MessageService.SendWarning(player, "Your inventory is full!");
        
        if (itemsDropped > 0) {
            MessageService.SendFormatted(
                player, 
                "{0} items were dropped at your feet.", 
                itemsDropped
            );
        }
        
        MessageService.SendInfo(player, "Use /sort to organize your inventory.");
    }
}
```

## Available Colors (RichTextFormatter)

The service works with `RichTextFormatter` for colored messages:

```csharp
RichTextFormatter.Red      // Error messages
RichTextFormatter.Green    // Success messages
RichTextFormatter.Yellow   // Warning messages
RichTextFormatter.Blue     // Info messages
RichTextFormatter.Orange   // Announcements
RichTextFormatter.White    // Default
RichTextFormatter.Cyan     // Accent color
RichTextFormatter.Purple   // Special messages
```

## Best Practices

1. **Message Types**: Use appropriate colored methods (`SendError`, `SendSuccess`, etc.) for consistent UX
2. **Broadcasts**: Use sparingly to avoid message spam
3. **Localization**: Use localized messages when possible for multilingual support
4. **Conditional Messages**: Reduce unnecessary messages with conditional methods
5. **Formatting**: Use `SendFormatted` for dynamic content instead of string concatenation
6. **Lists**: Use `SendList` for clean, organized list displays
7. **Radius Messages**: Use position-based messaging for location-specific notifications
8. **Templates**: Leverage template methods (`SendWelcome`, `NotifyDeath`) for consistent messaging

## Message Length Limits

- Maximum message length: 512 bytes (`FixedString512Bytes`)
- Messages exceeding this limit will be truncated
- Consider splitting long messages into multiple sends

## Related Services
- [PlayerService](PlayerService.md) - For player data and retrieval
- [ClanService](ClanService.md) - For clan-related messaging
- [LocalizationExtensions](../LocalizationExtensions.md) - For message localization

## Notes
- All messages are processed through the game's chat system
- Messages support rich text formatting (colors, styles)
- SCT messages appear as floating text above player characters
- Broadcast methods send to all connected players simultaneously
- Radius-based messaging requires player position data
