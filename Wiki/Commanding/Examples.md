# Command Examples

Practical examples showing common patterns and user-facing usage.

## Kick (moderation)
Signature:
```csharp
[Command("kick", Language.English, aliases: new[] {"k"}, requiredPermissions: new[] {"moderate"}, usage: "kick <player> [reason]")]
public static void Kick(CommandContext ctx, PlayerData player, string reason = "") { ... }
```
User examples:
- `.kick PlayerName`
- `.kick "Player Name" Cheating`

## Teleport
Signature:
```csharp
[Command("teleport", Language.English, usage: "teleport <player> <x,y,z>")]
public static void Teleport(CommandContext ctx, PlayerData player, float3 coords) { ... }
```
User example:
- `.teleport PlayerName 100,200,50`
- `.teleport "Player Name" 100,200,50`

## Spawn
Signature:
```csharp
[Command("spawn", Language.English, usage: "spawn <prefabGuid> [count]")]
public static void Spawn(CommandContext ctx, PrefabGUID prefab, int count = 1) { ... }
```
User example:
- `.spawn abcde-12345 3`

## Simple info command
Signature:
```csharp
[Command("whoami", Language.English, description: "Show caller info")]
public static void WhoAmI(CommandContext ctx) {
  ctx.Reply($"You are ~{ctx.Sender.Name}~");
}
```

## Best match (command overloading with different parameters)
You can define multiple methods with the same command name but different parameter signatures. The system automatically selects the best match based on argument count and types.

```csharp
// Version 1: teleport caller to coordinates
[Command("tp", Language.English, usage: "tp <x,y,z>")]
public static void TeleportSelf(CommandContext ctx, float3 coords) {
  // teleport ctx.Sender to coords
  ctx.ReplySuccess($"Teleported to {coords}");
}

// Version 2: teleport another player to coordinates
[Command("tp", Language.English, usage: "tp <player> <x,y,z>")]
public static void TeleportPlayer(CommandContext ctx, PlayerData target, float3 coords) {
  // teleport target to coords
  ctx.ReplySuccess($"Teleported {target.Name} to {coords}");
}
```

User examples:
- `.tp 100,200,50` — calls `TeleportSelf` (1 argument: float3)
- `.tp PlayerName 100,200,50` — calls `TeleportPlayer` (2 arguments: PlayerData + float3)

The command system scores each candidate by parameter type match quality and selects the best fit.

## Best match with same parameter count (type priority)
You can also define commands with the same name and same parameter count but different types. The TypeConverter assigns priority scores to each type, and the system selects the command with the highest total score.

Type priority order (highest to lowest):
- Integers (`int`, `long`, `uint`, etc.): 100
- Booleans: 95
- Floats/Doubles: 90
- Enums: 85
- PrefabGUID: 80
- PlayerData (numeric ID): 75
- Vector types (float2/float3/float4): 70
- PlayerData (name): 60
- String: 10 (lowest, matches anything)

```csharp
// Version 1: give item by PrefabGUID
[Command("give", Language.English, usage: "give <prefabGuid>")]
public static void GiveItemByGuid(CommandContext ctx, PrefabGUID itemGuid) {
  // give item to ctx.Sender using prefabGuid
  ctx.ReplySuccess($"Gave item {itemGuid}");
}

// Version 2: give item by name (string)
[Command("give", Language.English, usage: "give <itemName>")]
public static void GiveItemByName(CommandContext ctx, string itemName) {
  // lookup item by name and give to ctx.Sender
  ctx.ReplySuccess($"Gave item named ~{itemName}~");
}
```

User examples:
- `.give -123456789` — calls `GiveItemByGuid` (PrefabGUID priority: 80 > string priority: 10)
- `.give IronSword` — calls `GiveItemByName` (not a valid GUID, so only string matches)

This allows flexible command signatures where more specific types (like PrefabGUID or int) are preferred over generic string parameters when the input matches.

## Localized commands (CommandAlias)
You can provide localized versions of commands using the `CommandAlias` attribute. Each player sees commands in their configured language.

**Important:** `CommandAlias` (the attribute) is different from the `aliases` parameter:
- `CommandAlias` — creates a separate localized version of the command for a different language
- `aliases` parameter — provides alternate shorthand names within the same language

```csharp
[Command("heal", Language.English, description: "Heal yourself", usage: "heal")]
[CommandAlias("curar", Language.Spanish, description: "Curarte", usage: "curar")]
[CommandAlias("curar", Language.Portuguese, description: "Se curar", usage: "curar")]
public static void HealCommand(CommandContext ctx) {
  // heal the player
  ctx.ReplySuccess("**Healed!**");
}
```

User examples:
- English player: `.heal`
- Spanish player: `.curar`
- Portuguese player: `.curar`

Each player automatically uses the command in their language without needing to know other language variants.

## Localized command groups (CommandGroupAlias)
Groups can also be localized for better organization across languages.

```csharp
[CommandGroup("admin", Language.English, adminOnly: true)]
[CommandGroupAlias("administrador", Language.Spanish)]
[CommandGroupAlias("admin", Language.Portuguese)]
public static class AdminCommands {
  
  [Command("kick", Language.English, description: "Kick a player")]
  [CommandAlias("expulsar", Language.Spanish, description: "Expulsar jugador")]
  [CommandAlias("chutar", Language.Portuguese, description: "Chutar jogador")]
  public static void KickCommand(CommandContext ctx, PlayerData player) {
    // kick the player
    ctx.ReplySuccess($"Kicked {player.Name}");
  }

  [Command("ban", Language.English, description: "Ban a player")]
  [CommandAlias("banear", Language.Spanish, description: "Banear jugador")]
  [CommandAlias("banir", Language.Portuguese, description: "Banir jogador")]
  public static void BanCommand(CommandContext ctx, PlayerData player) {
    // ban the player
    ctx.ReplySuccess($"Banned ~{player.Name}~");
  }
}
```

User examples:
- English: `.admin kick PlayerName`, `.admin ban PlayerName`
- Spanish: `.administrador expulsar PlayerName`, `.administrador banear PlayerName`
- Portuguese: `.admin chutar PlayerName`, `.admin banir PlayerName`

The system routes players to the correct group and command based on their language setting.

## Tips
- Quote multi-word player names and messages.
- Use `usage` text to clarify expected formats (especially comma-separated vectors).
- Use `ReplyLocalized` when returning messages that should be translated.
- Always provide matching `CommandAlias` entries for all supported languages to ensure consistent user experience.