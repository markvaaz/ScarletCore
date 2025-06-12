# MessageService

MessageService provides comprehensive messaging functionality for V Rising servers, including colored messages, broadcasts, and templated notifications.

## Overview

```csharp
using ScarletCore.Services;

// Send a message to a player
MessageService.Send(player, "Hello World!");

// Send a colored message
MessageService.SendColored(player, "Success!", "green");

// Broadcast to all players
MessageService.SendAll("Server announcement!");
```

## Features

- Basic and colored messaging
- Broadcast capabilities to all players or specific groups
- Pre-formatted message types (error, success, warning, info)
- Template messages for common scenarios
- Conditional and formatted messaging
- Radius-based messaging
- Administrative messaging
- Rich text formatting support

## Basic Methods

### Send (User)
Sends a message to a specific user.

```csharp
MessageService.Send(user, "Hello from the server!");
```

**Parameters:**
- `user` - The user to send the message to
- `message` - The message content

### Send (PlayerData)
Sends a message to a specific player.

```csharp
if (PlayerService.TryGetByName("PlayerName", out PlayerData player)) {
  MessageService.Send(player, "Welcome back!");
}
```

**Parameters:**
- `player` - The player to send the message to
- `message` - The message content

### SendAll
Sends a message to all connected players.

```csharp
MessageService.SendAll("Server maintenance in 10 minutes!");
```

**Parameters:**
- `message` - The message to broadcast to all players

### SendAdmins
Sends a message to all online administrators.

```csharp
MessageService.SendAdmins("Admin notification: Player reported for griefing");
```

**Parameters:**
- `message` - The message to send to administrators

## Enhanced Send Methods

### SendColored (User)
Sends a colored message to a user.

```csharp
MessageService.SendColored(user, "Achievement unlocked!", "gold");
```

**Parameters:**
- `user` - The user to send the message to
- `message` - The message content
- `color` - The color to apply to the message

### SendColored (PlayerData)
Sends a colored message to a player.

```csharp
MessageService.SendColored(player, "Level up!", "yellow");
```

**Parameters:**
- `player` - The player to send the message to
- `message` - The message content
- `color` - The color to apply to the message

### SendAllColored
Sends a colored message to all players.

```csharp
MessageService.SendAllColored("Event starting soon!", "orange");
```

**Parameters:**
- `message` - The message to broadcast
- `color` - The color to apply to the message

### SendError (User/PlayerData)
Sends an error message (red color).

```csharp
MessageService.SendError(user, "Command failed: Invalid arguments");
MessageService.SendError(player, "Insufficient permissions");
```

**Parameters:**
- `user`/`player` - The target recipient
- `message` - The error message

### SendSuccess (User/PlayerData)
Sends a success message (green color).

```csharp
MessageService.SendSuccess(user, "Item purchased successfully!");
MessageService.SendSuccess(player, "Teleportation complete");
```

**Parameters:**
- `user`/`player` - The target recipient
- `message` - The success message

### SendWarning (User/PlayerData)
Sends a warning message (yellow color).

```csharp
MessageService.SendWarning(user, "PvP zone entered!");
MessageService.SendWarning(player, "Low health warning");
```

**Parameters:**
- `user`/`player` - The target recipient
- `message` - The warning message

### SendInfo (User/PlayerData)
Sends an info message (blue color).

```csharp
MessageService.SendInfo(user, "Server uptime: 24 hours");
MessageService.SendInfo(player, "Your current location: Castle");
```

**Parameters:**
- `user`/`player` - The target recipient
- `message` - The info message

## Broadcast Methods

### Announce
Sends an announcement to all players (orange color with prefix).

```csharp
MessageService.Announce("Weekly event is now active!");
```

**Parameters:**
- `message` - The announcement message

### BroadcastError
Sends a global error message to all players.

```csharp
MessageService.BroadcastError("Server database connection lost");
```

**Parameters:**
- `message` - The error message to broadcast

### BroadcastWarning
Sends a global warning to all players.

```csharp
MessageService.BroadcastWarning("Server restart in 5 minutes");
```

**Parameters:**
- `message` - The warning message to broadcast

### BroadcastInfo
Sends a global info message to all players.

```csharp
MessageService.BroadcastInfo("New features added in today's update");
```

**Parameters:**
- `message` - The info message to broadcast

## Template Messages

### SendWelcome
Sends a welcome message to a new player.

```csharp
MessageService.SendWelcome(player);
```

**Parameters:**
- `player` - The new player to welcome

**Behavior:**
- Sends multiple formatted welcome messages
- Includes server information and help instructions

### NotifyPlayerJoined
Sends a player join notification to all players.

```csharp
MessageService.NotifyPlayerJoined(player);
```

**Parameters:**
- `player` - The player who joined

### NotifyPlayerLeft
Sends a player leave notification to all players.

```csharp
MessageService.NotifyPlayerLeft(player);
```

**Parameters:**
- `player` - The player who left

### NotifyDeath
Sends a death notification.

```csharp
MessageService.NotifyDeath("PlayerName");
MessageService.NotifyDeath("VictimName", "KillerName");
```

**Parameters:**
- `victimName` - Name of the player who died
- `killerName` - Name of the killer (optional)

### NotifyRestart
Sends a server restart warning.

```csharp
MessageService.NotifyRestart(10); // 10 minutes remaining
MessageService.NotifyRestart(1);  // 1 minute remaining
```

**Parameters:**
- `minutesRemaining` - Minutes until server restart

## Utility Methods


**Parameters:**
- `user`/`player` - The target recipient
- `template` - The message template with placeholders
- `args` - Arguments to fill the placeholders

### SendToPlayers
Sends a message to multiple specific players.

```csharp
var targetPlayers = PlayerService.GetAdmins();
MessageService.SendToPlayers(targetPlayers, "Admin meeting in 5 minutes");
```

**Parameters:**
- `players` - Collection of players to send the message to
- `message` - The message to send

### SendToPlayersColored
Sends a colored message to multiple specific players.

```csharp
var guildMembers = ClanService.GetMembers("MyGuild");
MessageService.SendToPlayersColored(guildMembers, "Guild event starting!", "purple");
```

**Parameters:**
- `players` - Collection of players to send the message to
- `message` - The message to send
- `color` - The color to apply to the message

### SendToPlayersInRadius
Sends a message to players within a certain range of a position.

```csharp
var position = player.Position;
MessageService.SendToPlayersInRadius(position, 50f, "Local event triggered nearby!");
```

**Parameters:**
- `position` - The center position
- `radius` - The radius in units
- `message` - The message to send

### SendToPlayersInRadiusColored
Sends a colored message to players within a certain range of a position.

```csharp
MessageService.SendToPlayersInRadiusColored(position, 100f, "Danger zone activated!", "red");
```

**Parameters:**
- `position` - The center position
- `radius` - The radius in units
- `message` - The message to send
- `color` - The color to apply to the message

### SendList (User/PlayerData)
Creates a formatted list message.

```csharp
var items = new[] { "Sword", "Shield", "Potion" };
MessageService.SendList(user, "Inventory Items", items, "cyan");
```

**Parameters:**
- `user`/`player` - The target recipient
- `title` - The list title
- `items` - Collection of items to list
- `color` - Optional color for the list (default: white)

### SendProgressBar (User/PlayerData)
Sends a progress bar message.

```csharp
MessageService.SendProgressBar(user, "Download Progress", 75, 100, 20);
```

**Parameters:**
- `user`/`player` - The target recipient
- `label` - The progress bar label
- `current` - Current progress value
- `max` - Maximum progress value
- `barLength` - Length of the progress bar (default: 20)

### SendConditional (User/PlayerData)
Sends a message only if the condition is met.

```csharp
MessageService.SendConditional(user, "You have new mail!", () => player.HasUnreadMessages);
```

**Parameters:**
- `user`/`player` - The target recipient
- `message` - The message to send
- `condition` - Function that returns true if message should be sent

### SendConditional (Dual Message)
Sends different messages based on a condition.

```csharp
MessageService.SendConditional(user, "Access granted", "Access denied", () => player.IsAdmin);
```

**Parameters:**
- `user`/`player` - The target recipient
- `trueMessage` - Message to send if condition is true
- `falseMessage` - Message to send if condition is false
- `condition` - Function that determines which message to send

### SendBoxed (User/PlayerData)
Creates a boxed message with borders.

```csharp
MessageService.SendBoxed(user, "Server Rules", "1. No griefing\n2. Respect other players", "yellow");
```

**Parameters:**
- `user`/`player` - The target recipient
- `title` - The box title
- `content` - The box content
- `color` - Optional color for the box (default: white)

### SendSeparator (User/PlayerData)
Sends a separator line.

```csharp
MessageService.SendSeparator(user, '=', 50, "blue");
```

**Parameters:**
- `user`/`player` - The target recipient
- `character` - Character to use for separator (default: '-')
- `length` - Length of the separator (default: 40)
- `color` - Optional color for the separator (default: gray)

### SendCountdown
Sends a countdown message to all players.

```csharp
MessageService.SendCountdown(30, "Event Start");
```

**Parameters:**
- `seconds` - Number of seconds for countdown
- `action` - The action being counted down to

## Usage Examples

### Basic Messaging
```csharp
using ScarletCore.Services;

// Send simple messages
MessageService.Send(player, "Hello World!");
MessageService.SendAll("Server announcement");

// Send colored messages
MessageService.SendColored(player, "Success!", "green");
MessageService.SendError(player, "Something went wrong");
MessageService.SendSuccess(player, "Operation completed");
```

### Player Notifications
```csharp
// Welcome new players
MessageService.SendWelcome(player);

// Notify about player actions
MessageService.NotifyPlayerJoined(player);
MessageService.NotifyPlayerLeft(player);
MessageService.NotifyDeath("PlayerName", "KillerName");
```

### Administrative Messages
```csharp
// Send messages to admins only
MessageService.SendAdmins("Player reported for cheating");

// Broadcast server information
MessageService.Announce("Weekly event starting now!");
MessageService.BroadcastWarning("Server maintenance in 10 minutes");
```

### Formatted and Conditional Messages
```csharp
// Send formatted messages
MessageService.SendFormatted(player, "You have {0} coins and {1} experience", coins, exp);

// Send conditional messages
MessageService.SendConditional(player, "You have admin privileges!", () => player.IsAdmin);

// Send progress information
MessageService.SendProgressBar(player, "Quest Progress", 3, 5);
```

## Important Notes

- **Message length limit** - Messages are limited to 512 bytes due to FixedString512Bytes
- **Color support** - Uses RichTextFormatter for color formatting
- **Template formatting** - Template messages include automatic formatting and styling
- **Radius messaging** - Requires MathUtility for distance calculations
- **Admin targeting** - Admin messages automatically filter to players with admin privileges
- **Conditional logic** - Conditional methods support lambda expressions for dynamic messaging
