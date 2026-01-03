# Commanding System — Overview

The ScarletCore Commanding System provides a powerful, flexible, and localization-ready framework for creating chat commands in V Rising servers. It automatically handles argument parsing, type conversion, permission checking, and multi-language support.

## Key Features

### 🎯 Attribute-based command definition
Define commands using simple C# attributes on static methods. No manual registration or complex setup required.

### 🌍 Built-in localization support
Commands can be defined in multiple languages using `CommandAlias` and `CommandGroupAlias`. Players automatically see commands in their configured language.

### 🔄 Automatic type conversion
The system converts string arguments into strongly-typed parameters (int, float, PlayerData, PrefabGUID, vectors, enums, etc.) with intelligent best-match selection.

### 🎨 Command overloading
Define multiple commands with the same name but different parameter signatures. The system automatically selects the best match based on argument count and type priority.

### 🔐 Permission system
Built-in support for `adminOnly` and `requiredPermissions` checks. Fine-grained control over who can execute commands.

### 💬 Rich reply helpers
Convenient methods to send formatted messages (`Reply`, `ReplyError`, `ReplySuccess`, etc.) with support for localized keys.

## Quick Start

### Basic command
```csharp
[Command("heal", Language.English, description: "Heal yourself")]
public static void HealCommand(CommandContext ctx) {
  // Your implementation
  ctx.ReplySuccess("You have been healed!");
}
```

Usage: `.heal`

### Command with parameters
```csharp
[Command("teleport", Language.English, usage: "teleport <player> <x,y,z>")]
public static void TeleportCommand(CommandContext ctx, PlayerData player, float3 coords) {
  // Teleport player to coordinates
  ctx.ReplySuccess($"Teleported {player.Name} to {coords}");
}
```

Usage: `.teleport PlayerName 100,200,50`

### Localized command
```csharp
[Command("kick", Language.English, description: "Kick a player")]
[CommandAlias("expulsar", Language.Spanish, description: "Expulsar jugador")]
[CommandAlias("chutar", Language.Portuguese, description: "Chutar jogador")]
public static void KickCommand(CommandContext ctx, PlayerData player) {
  // Kick implementation
  ctx.ReplySuccess($"Kicked {player.Name}");
}
```

Usage:
- English: `.kick PlayerName`
- Spanish: `.expulsar PlayerName`
- Portuguese: `.chutar PlayerName`

### Command groups
```csharp
[CommandGroup("admin", Language.English, adminOnly: true)]
public static class AdminCommands {
  [Command("restart", Language.English, description: "Restart the server")]
  public static void RestartCommand(CommandContext ctx) {
    ctx.ReplyInfo("Restarting server...");
    // Restart logic
  }
}
```

Usage: `.admin restart`

## How it works

1. **Command prefix**: All commands start with `.` (dot) in chat
2. **Argument parsing**: The system tokenizes the message and matches arguments to method parameters
3. **Type conversion**: String tokens are converted to the target parameter types using TypeConverter
4. **Best match selection**: When multiple commands match, the system scores them by type priority and selects the best fit
5. **Permission check**: Validates `adminOnly` and `requiredPermissions` before execution
6. **Execution**: Invokes the method with converted parameters
7. **Reply**: Use `CommandContext` helpers to send formatted responses

## Documentation Structure

- **[CommandAttributes.md](CommandAttributes.md)** — How to define commands, groups, and localization
- **[CommandContext.md](CommandContext.md)** — Reply helpers and accessing sender information
- **[CommandHandler.md](CommandHandler.md)** — User-facing behavior, permissions, and validation
- **[TypeConverter.md](TypeConverter.md)** — Supported parameter types and input formats
- **[Examples.md](Examples.md)** — Practical examples and common patterns

## Supported Parameter Types

- Primitives: `string`, `int`, `float`, `double`, `bool`, `byte`, `short`, `long`, `uint`, `ulong`
- Game types: `PlayerData`, `PrefabGUID`
- Vector types: `float2`, `float3`, `float4`, `quaternion`
- Enums: Any enum type (case-insensitive matching)
- Optional parameters with default values

## Best Practices

✅ **Use clear descriptions and usage strings** for better help output
✅ **Provide localized versions** (`CommandAlias`) for multi-language servers
✅ **Use type-specific parameters** (e.g., `PlayerData` instead of `string`) for better type safety
✅ **Leverage command overloading** for flexible command signatures
✅ **Use `ReplyLocalized`** for messages that should be translated
✅ **Group related commands** with `CommandGroup` for better organization

❌ **Don't confuse `CommandAlias` (localization) with `aliases` parameter (shortcuts)**
❌ **Don't use generic `string` parameters when more specific types are available**
❌ **Don't forget to quote multi-word arguments** in chat (e.g., `"Player Name"`)

## Getting Started

Start with [CommandAttributes.md](CommandAttributes.md) to learn how to define your first commands, then explore [Examples.md](Examples.md) for practical patterns you can copy and adapt.